using UnityEngine;

namespace FarmGuardian
{
    public sealed class FarmCamera : MonoBehaviour
    {
        public Transform Target;
        public Vector3 Offset = new Vector3(0, 11.5f, -9.5f);
        Vector3 velocity;
        void LateUpdate()
        {
            if (Target == null) return;
            Vector3 focus = Target.position + Vector3.up * .8f;
            Vector3 desired = Target.position + Offset;
            Vector3 delta = desired - focus;
            if (Physics.SphereCast(focus, 0.3f, delta.normalized, out var hit, delta.magnitude, 1 << FarmWorld.SolidLayer))
                desired = focus + delta.normalized * Mathf.Max(2, hit.distance - 0.3f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.12f, Mathf.Infinity, Time.unscaledDeltaTime);
            transform.LookAt(focus);
        }
    }
}
