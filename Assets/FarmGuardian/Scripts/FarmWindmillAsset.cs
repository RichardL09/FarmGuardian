using UnityEngine;
namespace FarmGuardian
{
    public sealed class FarmWindmillAsset : ScriptableObject
    {
        public Mesh Body,Rotor;public Vector3 Pivot,Axis=Vector3.forward;
    }
}
