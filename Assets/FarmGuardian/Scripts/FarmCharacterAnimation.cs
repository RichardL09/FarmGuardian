using UnityEngine;

namespace FarmGuardian
{
    /// <summary>Passes gameplay state to the rig's Animator Controller. Clips animate the bones.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class FarmCharacterAnimation : MonoBehaviour
    {
        public Transform body, leftLeg, rightLeg, leftKnee, rightKnee, leftArm, rightArm;
        public Animator Animator { get; private set; }
        public float Speed { get; private set; }
        public bool Stunned { get; private set; }
        public bool Carrying { get; private set; }
        static readonly int SpeedParameter = UnityEngine.Animator.StringToHash("Speed");
        static readonly int WalkRateParameter = UnityEngine.Animator.StringToHash("WalkRate");
        static readonly int CarryingParameter = UnityEngine.Animator.StringToHash("Carrying");
        static readonly int StunnedParameter = UnityEngine.Animator.StringToHash("Stunned");
        void Awake()
        {
            Animator = GetComponent<Animator>();
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        public void Drive(float speed, bool stunned = false, bool carrying = false)
        {
            Speed = Mathf.Clamp(speed, 0, 10);
            Stunned = stunned; Carrying = carrying;
            if (Animator == null || Animator.runtimeAnimatorController == null) return;
            Animator.SetFloat(SpeedParameter, Stunned ? 0 : Speed);
            Animator.SetFloat(WalkRateParameter, Mathf.Max(.1f, Speed * 3.6f / (2 * Mathf.PI)));
            Animator.SetBool(CarryingParameter, Carrying);
            Animator.SetBool(StunnedParameter, Stunned);
        }
    }
}
