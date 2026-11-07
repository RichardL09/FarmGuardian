using UnityEngine;
using UnityEngine.AI;

namespace FarmGuardian
{
    public enum ThiefState { Inactive, Searching, MovingToTarget, Stealing, Escaping, Stunned, ReturningAfterStun }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class FarmThief : MonoBehaviour
    {
        public FarmSession Session;
        public Transform Visual;
        public FarmCharacterAnimation WalkAnimation;
        public ThiefState State { get; private set; } = ThiefState.Inactive;
        public FarmAnimal StolenAnimal { get; private set; }
        public float StunRemaining { get; private set; }
        public float EscapeRemaining { get; private set; }
        public Vector3 StunPosition {get;private set;}
        public bool Catchable => State != ThiefState.Inactive && State != ThiefState.Stunned;
        NavMeshAgent agent;
        FarmAnimal target;
        float actionTime, searchTime, repathTime;
        int patrolPoint;

        void Awake() { agent = GetComponent<NavMeshAgent>(); }
        void Start() { if (Visual != null) Visual.gameObject.SetActive(State != ThiefState.Inactive); }

        public void Activate()
        {
            if (State != ThiefState.Inactive) return;
            Session.World.RandomizeThiefSpawn(agent);
            Search();
            if (Visual != null) Visual.gameObject.SetActive(true);
            Session.Beep();
        }

        void Update()
        {
            if (!agent.isOnNavMesh) return;
            agent.isStopped = !Session.Playing || State == ThiefState.Inactive || State == ThiefState.Stunned || State == ThiefState.Stealing;
            if (WalkAnimation != null) WalkAnimation.Drive(Session.Playing && !agent.isStopped ? agent.velocity.magnitude : 0,
                State == ThiefState.Stunned, StolenAnimal != null);
            if (!Session.Playing || State == ThiefState.Inactive) return;
            switch (State)
            {
                case ThiefState.Searching:
                    searchTime -= Time.deltaTime;
                    if (searchTime > 0) break;
                    if(!PursueSecuredAnimal())
                    {
                        Patrol();
                        searchTime=Mathf.Clamp(Session.config.thiefStartDelay,.05f,.25f);
                    }
                    break;
                case ThiefState.MovingToTarget:
                    if (target == null || target.State != AnimalState.Secured) { Search(); break; }
                    repathTime -= Time.deltaTime;
                    if (repathTime <= 0) { agent.SetDestination(target.transform.position); repathTime = 0.75f; }
                    if (Vector3.Distance(transform.position, target.transform.position) <= 1.25f)
                    { State = ThiefState.Stealing; actionTime = Session.config.theftSeconds; Session.Beep(); }
                    break;
                case ThiefState.Stealing:
                    if (target == null || target.State != AnimalState.Secured) { Search(); break; }
                    actionTime -= Time.deltaTime;
                    if (actionTime <= 0)
                    {
                        StolenAnimal = target; target.Steal();target.AttachToThief(this);
                        WalkAnimation?.Drive(agent.velocity.magnitude,false,true);
                        State = ThiefState.Escaping;
                        EscapeRemaining = Session.config.escapeDeadline;
                        agent.isStopped = false;
                        agent.SetDestination(Session.EscapePosition);
                        Session.Beep();
                    }
                    break;
                case ThiefState.Escaping:
                    EscapeRemaining = Mathf.Max(0, EscapeRemaining - Time.deltaTime);
                    if (StolenAnimal!=null&&Session.World.ExitZone!=null&&Session.World.ExitZone.Contains(transform.position))
                        EscapeWithAnimal();
                    break;
                case ThiefState.Stunned:
                    StunRemaining = Mathf.Max(0, StunRemaining - Time.deltaTime);
                    if (StunRemaining <= 0)
                    {
                        State = ThiefState.ReturningAfterStun; actionTime = 1f;
                    }
                    break;
                case ThiefState.ReturningAfterStun:
                    actionTime -= Time.deltaTime;
                    if (actionTime <= 0) Search();
                    break;
            }
        }

        void Search()
        {
            target=null;State=ThiefState.Searching;searchTime=0;
            if(!agent.enabled||!agent.isOnNavMesh)return;
            agent.ResetPath();agent.isStopped=false;
            if(!PursueSecuredAnimal())Patrol();
        }
        bool PursueSecuredAnimal()
        {
            target=Session.Animals.Find(a=>a.State==AnimalState.Secured);
            if(target==null)return false;
            State=ThiefState.MovingToTarget;repathTime=.75f;
            agent.isStopped=false;agent.SetDestination(target.transform.position);
            return true;
        }
        void Patrol()
        {
            if(agent.pathPending||agent.hasPath&&agent.remainingDistance>1)return;
            var layout=Session.World.Layout;
            Vector3 destination=patrolPoint==0?layout.ResponseHub:layout.Pens[patrolPoint-1];
            patrolPoint=(patrolPoint+1)%(layout.Pens.Length+1);
            agent.isStopped=false;agent.SetDestination(destination);
        }

        void EscapeWithAnimal()
        {
            var animal=StolenAnimal;StolenAnimal=null;target=null;EscapeRemaining=0;
            WalkAnimation?.Drive(0,false,false);
            if(agent.isOnNavMesh){agent.ResetPath();agent.velocity=Vector3.zero;}
            Session.AnimalLost(animal);
            if(!Session.Playing)return;
            Session.World.RandomizeThiefSpawn(agent);
            Search();
        }

        public void Catch()
        {
            if (!Catchable || !Session.Playing) return;
            State = ThiefState.Stunned;
            StunPosition=transform.position;
            StunRemaining = Session.config.thiefStunSeconds;
            if (agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath();agent.velocity=Vector3.zero; }
            var animal = StolenAnimal;
            StolenAnimal = null; target = null;
            WalkAnimation?.Drive(0,true,false);
            if (animal != null) animal.SecureRecovered();
            Session.Beep();
        }
    }
}
