using UnityEngine;
using UnityEngine.InputSystem;

namespace FarmGuardian
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FarmPlayer : MonoBehaviour
    {
        public FarmSession Session;
        public Transform Visual;
        public FarmCharacterAnimation WalkAnimation;
        CharacterController controller;
        float vertical;

        void Start() { controller = GetComponent<CharacterController>(); }

        void Update()
        {
            if (!Session.Playing) { if (WalkAnimation != null) WalkAnimation.Drive(0,false,Session.CarriedAnimal!=null); return; }
            Vector2 input = Session.Hud.MoveInput;
            var keys = Keyboard.current;
            if (keys != null)
            {
                input += new Vector2((keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) -
                    (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0),
                    (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) -
                    (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0));
            }
            input = Vector2.ClampMagnitude(input, 1);
            Vector3 direction = new Vector3(input.x, 0, input.y);
            vertical = controller.isGrounded && vertical < 0 ? -2 : Mathf.Max(-25, vertical - 20 * Time.deltaTime);
            float speed = Session.config.playerSpeed * (1 + Session.Save.speed * Session.config.movementUpgradePerLevel);
            if(Session.CarriedAnimal!=null)speed*=Session.CarriedAnimal.CarrySpeedMultiplier;
            Vector3 before = transform.position;
            controller.Move((direction * speed + Vector3.up * vertical) * Time.deltaTime);
            Vector3 moved = transform.position - before; moved.y = 0;
            if (WalkAnimation != null) WalkAnimation.Drive(moved.magnitude / Mathf.Max(0.0001f, Time.deltaTime),false,Session.CarriedAnimal!=null);
            if (direction.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 16 * Time.deltaTime);
        }
    }
}
