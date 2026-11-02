using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public Camera view;
        public ChunkManager world;
        public WorldSettings settings;
        public bool blocked;

        [Header("Movimento")]
        [SerializeField] float walkSpeed = 4.5f;
        [SerializeField] float runSpeed = 8f;
        [SerializeField] float crouchSpeed = 2f;
        [SerializeField] float acceleration = 18f;
        [SerializeField] float braking = 25f;
        [SerializeField] float jumpSpeed = 7f;

        const float StandingHeight = 1.85f;
        const float CrouchingHeight = 1.50f;
        const float FootOffset = .015f;
        const float JumpBufferSeconds = .12f;
        const float CoyoteSeconds = .09f;
        CharacterController controller;
        PlayerVisualAnimator visuals;
        Vector3 velocity;
        float vertical, jumpBuffer, coyote;
        bool crouching, wasGrounded;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            controller.height = StandingHeight;
            controller.center = Vector3.up * (StandingHeight * .5f + FootOffset);
            controller.radius = .3f;
            controller.stepOffset = .34f;
            controller.slopeLimit = 48;
            visuals = GetComponent<PlayerVisualAnimator>();
            if (!visuals) visuals = gameObject.AddComponent<PlayerVisualAnimator>();
            visuals.Initialize(transform);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .05f);
            if (dt <= 0)
                return;
            if (!world || !world.HasGround(transform.position))
            {
                visuals.Tick(Vector3.zero, true, crouching, 0, 0, 0, dt);
                return;
            }

            bool grounded = controller.isGrounded && vertical <= 0;
            var keyboard = Keyboard.current;
            Vector2 input = Vector2.zero;
            bool wantsCrouch = false, sprint = false;
            if (!blocked && keyboard != null)
            {
                input = new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
                input = Vector2.ClampMagnitude(input, 1);
                wantsCrouch = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
                sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                if (keyboard.spaceKey.wasPressedThisFrame)
                    jumpBuffer = JumpBufferSeconds;
            }
            else
                jumpBuffer = 0;

            // Keep the physics root unscaled. Standing up is denied while a ceiling
            // overlaps the standing capsule; only the visual rig changes its pose.
            crouching = wantsCrouch || (crouching && !CanStand());
            float targetHeight = crouching ? CrouchingHeight : StandingHeight;
            float nextHeight = Mathf.MoveTowards(controller.height, targetHeight, dt * 4);
            if (!Mathf.Approximately(nextHeight, controller.height))
            {
                controller.height = nextHeight;
                controller.center = Vector3.up * (nextHeight * .5f + FootOffset);
            }

            Vector3 forward = view ? view.transform.forward : transform.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < .001f) forward = transform.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float speed = crouching ? crouchSpeed : sprint ? runSpeed : walkSpeed;
            Vector3 desired = (forward * input.y + right * input.x) * speed;
            float rate = desired.sqrMagnitude > velocity.sqrMagnitude ? acceleration : braking;
            velocity = blocked ? Vector3.zero : Vector3.MoveTowards(velocity, desired, rate * dt);

            if (grounded)
            {
                vertical = -2;
                coyote = CoyoteSeconds;
            }
            else
                coyote = Mathf.Max(0, coyote - dt);
            jumpBuffer = Mathf.Max(0, jumpBuffer - dt);
            if (!blocked && !crouching && jumpBuffer > 0 && coyote > 0)
            {
                vertical = jumpSpeed;
                jumpBuffer = coyote = 0;
                grounded = false;
            }

            vertical = Mathf.Max(-32, vertical - 20 * dt);
            float landingSpeed = Mathf.Max(0, -vertical);
            Vector3 previous = transform.position;
            Vector3 previousForward = transform.forward;
            controller.Move(ClampDisplacement((velocity + Vector3.up * vertical) * dt));

            Vector3 traveled = transform.position - previous;
            traveled.y = 0;
            Vector3 actualVelocity = traveled / dt;
            if (actualVelocity.sqrMagnitude > .12f && !blocked)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(actualVelocity), 1 - Mathf.Exp(-dt * 13));
            float turnRate = Vector3.SignedAngle(previousForward, transform.forward, Vector3.up) / dt;
            grounded = controller.isGrounded && vertical <= 0;
            float impact = !wasGrounded && grounded ? landingSpeed : 0;
            visuals.Tick(actualVelocity, grounded, crouching, vertical, turnRate, impact, dt);
            wasGrounded = grounded;
        }

        bool CanStand()
        {
            float radius = controller.radius * .95f;
            // Probe the upper expansion, not the standing capsule's foot: contact
            // with the floor must not trap the character in a crouched stance.
            Vector3 bottom = transform.position + Vector3.up * (FootOffset + controller.height - radius);
            Vector3 top = transform.position + Vector3.up * (FootOffset + StandingHeight - radius);
            // The runtime player and its visual parts use Ignore Raycast (layer 2).
            int environmentMask = ~(1 << 2);
            return !Physics.CheckCapsule(bottom, top, radius, environmentMask, QueryTriggerInteraction.Ignore);
        }

        Vector3 ClampDisplacement(Vector3 movement)
        {
            if (!settings)
                return movement;
            float half = settings.worldSize * .5f - 2;
            Vector3 position = transform.position;
            movement.x = Mathf.Clamp(position.x + movement.x, -half, half) - position.x;
            movement.z = Mathf.Clamp(position.z + movement.z, -half, half) - position.z;
            return movement;
        }

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            controller.height = StandingHeight;
            controller.center = Vector3.up * (StandingHeight * .5f + FootOffset);
            transform.position += ClampDisplacement(position - transform.position);
            controller.enabled = true;
            velocity = Vector3.zero;
            vertical = jumpBuffer = coyote = 0;
            crouching = wasGrounded = false;
            if (visuals) visuals.ResetPose();
        }
    }
}
