using UnityEngine;
namespace FarmGuardian
{
    public sealed class FarmWindmillRotor : MonoBehaviour
    {
        public FarmSession Session;public Vector3 Axis=Vector3.forward;float angle;
        public float Angle=>angle;
        void Update(){if(Session==null||!Session.Playing)return;angle=Mathf.Repeat(angle+Time.deltaTime*12,360);transform.localRotation=Quaternion.AngleAxis(angle,Axis);}
    }
}
