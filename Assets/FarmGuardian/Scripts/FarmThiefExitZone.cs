using UnityEngine;
namespace FarmGuardian
{
    public sealed class FarmThiefExitZone : MonoBehaviour
    {
        public bool Contains(Vector3 position)
        {
            var p=transform.InverseTransformPoint(position);
            // The visible square is 3m wide. Require the thief's body to enter fully.
            return Mathf.Abs(p.x)<=.95f&&Mathf.Abs(p.z)<=.95f&&p.y>=-.5f&&p.y<=2;
        }
    }
}
