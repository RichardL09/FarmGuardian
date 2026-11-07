using UnityEngine;

namespace FarmGuardian
{
    public sealed class FarmPenGate : MonoBehaviour
    {
        public FarmSession Session;public int TypeIndex;public Vector3 PenCenter;
        public Transform Left,Right;public Collider Barrier;
        public float Openness {get;private set;}
        public bool Passable=>Openness>.88f;
        float deliveryHold;
        public void Delivered(){deliveryHold=1.3f;}
        void Update()
        {
            if(!Session.Playing)return;
            deliveryHold=Mathf.Max(0,deliveryHold-Time.deltaTime);
            bool open=deliveryHold>0;
            var player=Session.Player;
            if(player!=null)
            {
                var p=player.transform.position;
                bool inside=Mathf.Abs(p.x-PenCenter.x)<1.8f&&Mathf.Abs(p.z-PenCenter.z)<2.3f;
                float distance=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(transform.position.x,transform.position.z));
                open|=inside||distance<1.6f||Session.CarriedAnimal!=null&&Session.CarriedAnimal.TypeIndex==TypeIndex&&distance<3.8f;
            }
            if(Session.Thief!=null&&Session.Thief.State!=ThiefState.Inactive)
                open|=Vector3.Distance(Session.Thief.transform.position,transform.position)<3.2f;
            foreach(var a in Session.Animals)
                if(a.State==AnimalState.Wandering)open|=Vector3.Distance(a.transform.position,transform.position)<1.8f;
            Openness=Mathf.MoveTowards(Openness,open?1:0,Time.deltaTime/ .55f);
            Left.localRotation=Quaternion.Euler(0,-95*Mathf.SmoothStep(0,1,Openness),0);
            Right.localRotation=Quaternion.Euler(0,95*Mathf.SmoothStep(0,1,Openness),0);
            // Disable the threshold barrier only after the leaves clear the doorway.
            // Occupancy sensors keep it open while anyone is crossing or standing inside.
            Barrier.enabled=!Passable;
        }
    }
}
