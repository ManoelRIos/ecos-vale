using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public Camera view;
        public ChunkManager world;
        public WorldSettings settings;
        public bool blocked;
        CharacterController controller;
        Vector3 velocity;
        float vertical;
        Transform leftLeg, rightLeg, leftArm, rightArm;
        float stride;
        void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = 1.85f;
            controller.center = new Vector3(0, .94f, 0);
            controller.radius = .3f;
            controller.stepOffset = .4f;
            controller.slopeLimit = 48;
            leftLeg = transform.Find("Leg-1");
            rightLeg = transform.Find("Leg1");
            leftArm = transform.Find("Arm-1");
            rightArm = transform.Find("Arm1");
        }

        void Update()
        {
            if (!world || !world.HasGround(transform.position))
                return;
            var k = Keyboard.current;
            if (k == null)
                return;
            float dt = Time.deltaTime;
            Vector2 input = blocked ? Vector2.zero : new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0), (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1);
            bool crouch = !blocked && k.leftCtrlKey.isPressed;
            float speed = crouch ? 2 : k.leftShiftKey.isPressed ? 8 : 4.5f;
            Vector3 f = view.transform.forward;
            f.y = 0;
            f.Normalize();
            Vector3 r = view.transform.right;
            r.y = 0;
            r.Normalize();
            Vector3 desired = (f * input.y + r * input.x) * speed;
            velocity = Vector3.MoveTowards(velocity, desired, dt * 18);
            if (controller.isGrounded && vertical < 0)
                vertical = -2;
            if (!blocked && controller.isGrounded && k.spaceKey.wasPressedThisFrame)
                vertical = 7;
            vertical -= 20 * dt;
            controller.Move((velocity + Vector3.up * vertical) * dt);
            float half = settings.worldSize * .5f - 2;
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, -half, half);
            pos.z = Mathf.Clamp(pos.z, -half, half);
            if (pos != transform.position)
            {
                controller.enabled = false;
                transform.position = pos;
                controller.enabled = true;
            }

            if (velocity.sqrMagnitude > .15f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), dt * 12);
            stride += dt * velocity.magnitude * 2;
            float swing = Mathf.Sin(stride) * Mathf.Min(velocity.magnitude * 6, 32);
            if (leftLeg)
            {
                leftLeg.localRotation = Quaternion.Euler(swing, 0, 0);
                rightLeg.localRotation = Quaternion.Euler(-swing, 0, 0);
                leftArm.localRotation = Quaternion.Euler(-swing, 0, 0);
                rightArm.localRotation = Quaternion.Euler(swing, 0, 0);
            }

            Vector3 scale = transform.localScale;
            scale.y = Mathf.Lerp(scale.y, crouch ? .72f : 1, dt * 12);
            transform.localScale = scale;
        }

        public void Teleport(Vector3 p)
        {
            controller.enabled = false;
            transform.position = p;
            controller.enabled = true;
            velocity = Vector3.zero;
            vertical = 0;
        }
    }
}
