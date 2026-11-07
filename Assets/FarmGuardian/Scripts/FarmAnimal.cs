using UnityEngine;
using UnityEngine.AI;

namespace FarmGuardian
{
    public enum AnimalState { Escaping, Wandering, BeingCaptured, ReturningToPen, Secured, Stolen, Recovered, Carried, Lost }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class FarmAnimal : MonoBehaviour
    {
        public FarmSession Session;public int TypeIndex;public Vector3 Home,WanderOrigin;
        public const float WanderRadius=2.4f;
        public AnimalState State {get;private set;}=AnimalState.Escaping;
        public Animator Animator;public Transform Visual;
        NavMeshAgent agent;float nextWander,captureRemaining,nextDodge,hopAge=-1,nextPenWander;
        Vector3 visualRest,liftStart;Quaternion liftRotation;float liftTime,carryPhase;
        FarmCharacterAnimation carrierAnimation;
        Transform carrier;
        static readonly int Moving=UnityEngine.Animator.StringToHash("Moving");
        public float CaptureMultiplier=>TypeIndex==0?3f:TypeIndex==1?1.6f:TypeIndex==2?1.25f:1.4f;
        public float CarrySpeedMultiplier=>TypeIndex==0?.70f:TypeIndex==1?.9f:1;
        public bool Airborne=>hopAge>=0;
        public bool CanCapture=>(State==AnimalState.Wandering||State==AnimalState.Escaping)&&!Airborne;
        void Awake(){agent=GetComponent<NavMeshAgent>();}
        void Start()
        {
            if(State==AnimalState.Escaping)State=AnimalState.Wandering;
            if(Visual!=null)visualRest=Visual.localPosition;
            nextDodge=Time.time+2+TypeIndex*.6f;
        }
        void Update()
        {
            if(Animator!=null)Animator.speed=Session.Playing&&State!=AnimalState.Carried&&State!=AnimalState.Stolen?1:0;
            if(!Session.Playing||State==AnimalState.Carried||State==AnimalState.Stolen)return;
            if(!agent.enabled||!agent.isOnNavMesh)return;
            agent.isStopped=State==AnimalState.BeingCaptured;
            if(State==AnimalState.BeingCaptured)
            {
                if(Session.Player==null||Vector3.Distance(Session.Player.transform.position,transform.position)>Session.config.interactionRange+.35f)
                {State=AnimalState.Wandering;return;}
                captureRemaining-=Time.deltaTime;
                if(captureRemaining<=0&&!Session.TryCarry(this))State=AnimalState.Wandering;
            }
            else if(State==AnimalState.Secured)WanderInsidePen();
            else if(State==AnimalState.Wandering)
            {
                float nearby=Session.Player!=null?Vector3.Distance(transform.position,Session.Player.transform.position):100;
                if(hopAge>=0)
                {
                    hopAge+=Time.deltaTime;float duration=TypeIndex==2?.9f:.65f;
                    if(Visual!=null)Visual.localPosition=visualRest+Vector3.up*Mathf.Sin(Mathf.Clamp01(hopAge/duration)*Mathf.PI)*(TypeIndex==2?.8f:.35f);
                    if(hopAge>=duration){hopAge=-1;if(Visual!=null)Visual.localPosition=visualRest;}
                }
                if(nearby<5&&Time.time>=nextDodge)
                {
                    if(TypeIndex==2||TypeIndex==3)hopAge=0;
                    if(TypeIndex==1)MoveLocally(true);
                    nextDodge=Time.time+(TypeIndex==2?4.8f:TypeIndex==1?2.8f:5.5f);
                }
                if(Time.time>=nextWander){MoveLocally(TypeIndex==1&&nearby<5);nextWander=Time.time+(TypeIndex==1?2.5f:Random.Range(3,6));}
            }
            if(Animator!=null)Animator.SetBool(Moving,agent.velocity.sqrMagnitude>.03f);
        }
        public bool InsidePen(Vector3 point,float padding=0)
        {
            var center=Session.World.Layout.Pens[TypeIndex];
            return Mathf.Abs(point.x-center.x)<=1.1f+padding&&Mathf.Abs(point.z-center.z)<=1.35f+padding;
        }
        void WanderInsidePen()
        {
            var center=Session.World.Layout.Pens[TypeIndex];
            // Keep the whole animal well away from walls and the open doorway. The same
            // bounds apply while gates are open for a player or thief; state stays Secured.
            if(!InsidePen(transform.position))
            {
                var clamped=new Vector3(Mathf.Clamp(transform.position.x,center.x-1.05f,center.x+1.05f),transform.position.y,Mathf.Clamp(transform.position.z,center.z-1.3f,center.z+1.3f));
                if(NavMesh.SamplePosition(clamped,out var recovery,.12f,NavMesh.AllAreas)&&InsidePen(recovery.position))agent.Warp(recovery.position);
                agent.ResetPath();
            }
            if(Time.time<nextPenWander)return;
            nextPenWander=Time.time+Random.Range(2,4);
            agent.speed=TypeIndex==0?.45f:TypeIndex==1?.65f:.75f;
            for(int attempt=0;attempt<5;attempt++)
            {
                var candidate=center+new Vector3(Random.Range(-1.02f,1.02f),0,Random.Range(-1.25f,1.25f));
                if(!NavMesh.SamplePosition(candidate,out var hit,.12f,NavMesh.AllAreas)||!InsidePen(hit.position))continue;
                var path=new NavMeshPath();if(!agent.CalculatePath(hit.position,path)||path.status!=NavMeshPathStatus.PathComplete)continue;
                bool inside=true;foreach(var corner in path.corners)if(!InsidePen(corner,.01f)){inside=false;break;}
                if(!inside)continue;agent.SetPath(path);break;
            }
        }
        void MoveLocally(bool flee)
        {
            agent.speed=TypeIndex==0?.65f:TypeIndex==1?1.8f:1.1f;
            for(int attempt=0;attempt<4;attempt++)
            {
                Vector2 offset=Random.insideUnitCircle*WanderRadius;
                Vector3 candidate=WanderOrigin+new Vector3(offset.x,0,offset.y);
                if(flee&&Session.Player!=null)
                {var away=transform.position-Session.Player.transform.position;away.y=0;candidate=WanderOrigin+Vector3.ClampMagnitude(transform.position+away.normalized*1.8f-WanderOrigin,WanderRadius);}
                if(!NavMesh.SamplePosition(candidate,out var hit,.4f,NavMesh.AllAreas)||Vector3.Distance(hit.position,WanderOrigin)>WanderRadius)continue;
                if(NavMesh.Raycast(WanderOrigin,hit.position,out var blocked,NavMesh.AllAreas)||FarmWorld.RouteLength(WanderOrigin,hit.position)>WanderRadius*1.4f)continue;
                agent.SetDestination(hit.position);break;
            }
        }
        public void Capture(float seconds)
        {
            if(!CanCapture||Session.CarriedAnimal!=null||Session.CaptureInProgress)return;
            State=AnimalState.BeingCaptured;captureRemaining=Mathf.Max(.05f,seconds*CaptureMultiplier);
            if(agent.enabled&&agent.isOnNavMesh)agent.ResetPath();
        }
        public void AttachToPlayer(Transform player)
        {AttachToCarrier(player,Session.Player!=null?Session.Player.WalkAnimation:null,AnimalState.Carried);}
        public void AttachToThief(FarmThief thief)
        {AttachToCarrier(thief.transform,thief.WalkAnimation,AnimalState.Stolen);}
        void AttachToCarrier(Transform carrier,FarmCharacterAnimation animation,AnimalState state)
        {
            if(agent.enabled&&agent.isOnNavMesh)agent.ResetPath();agent.enabled=false;
            hopAge=-1;if(Visual!=null)Visual.localPosition=visualRest;
            this.carrier=carrier;carrierAnimation=animation;State=state;transform.SetParent(carrier,false);
            transform.localPosition=new Vector3(0,.75f,.75f);transform.localRotation=Quaternion.Euler(0,90,0);
            liftStart=transform.localPosition-Vector3.up*.65f;liftRotation=transform.localRotation;liftTime=0;carryPhase=0;
            transform.localPosition=liftStart;
            if(Animator!=null){Animator.SetBool(Moving,false);Animator.speed=0;}
        }
        public bool Deposit()
        {
            if(State!=AnimalState.Carried||!Session.CanDeposit||!NavMesh.SamplePosition(Home,out var hit,1,NavMesh.AllAreas))return false;
            transform.SetParent(Session.World.Root,true);transform.rotation=Quaternion.identity;transform.position=hit.position;
            carrierAnimation=null;carrier=null;
            agent.enabled=true;agent.Warp(hit.position);agent.ResetPath();agent.isStopped=true;
            State=AnimalState.Secured;Session.World.GateFor(TypeIndex)?.Delivered();Session.OnSecured(this);return true;
        }
        void LateUpdate()
        {
            if((State!=AnimalState.Carried&&State!=AnimalState.Stolen)||!Session.Playing||carrier==null||transform.parent!=carrier)return;
            liftTime+=Time.deltaTime;carryPhase+=Time.deltaTime;
            float speed=carrierAnimation!=null?carrierAnimation.Speed:0;
            float bob=Mathf.Sin(carryPhase*(2+speed*2))*.014f*Mathf.Clamp01(speed);
            transform.localPosition=Vector3.Lerp(liftStart,new Vector3(0,.75f+bob,.75f),Mathf.SmoothStep(0,1,Mathf.Clamp01(liftTime/.3f)));
            transform.localRotation=liftRotation*Quaternion.Euler(0,0,Mathf.Sin(carryPhase*2)*.6f);
        }
        // Recovery now also requires carrying; no automatic trip into a pen.
        public void ReturnHome(bool recovered){if(recovered)SecureRecovered();else Session.TryCarry(this);}
        public void Steal()
        {
            if(State==AnimalState.Lost)return;
            carrierAnimation=null;carrier=null;
            State=AnimalState.Stolen;if(agent.enabled&&agent.isOnNavMesh)agent.ResetPath();agent.enabled=false;
        }
        internal bool MarkLost()
        {
            if(State!=AnimalState.Stolen)return false;
            State=AnimalState.Lost;
            carrierAnimation=null;carrier=null;
            if(agent.enabled&&agent.isOnNavMesh)agent.ResetPath();agent.enabled=false;
            transform.SetParent(Session.World.Root,true);gameObject.SetActive(false);
            return true;
        }
        public void SecureRecovered()
        {
            if(State==AnimalState.Lost||!Session.Playing)return;
            carrierAnimation=null;carrier=null;
            transform.SetParent(Session.World.Root,true);
            if(NavMesh.SamplePosition(transform.position,out var hit,3,NavMesh.AllAreas))transform.position=hit.position;
            WanderOrigin=transform.position;State=AnimalState.Wandering;
            agent.enabled=true;if(agent.isOnNavMesh){agent.Warp(transform.position);agent.isStopped=false;}
            Session.TryCarry(this);
        }
    }
}
