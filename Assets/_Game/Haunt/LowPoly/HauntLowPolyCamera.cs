using UnityEngine;
using Ecos.Haunt.Fps;

namespace Ecos.Haunt.LowPoly
{
    /// <summary>First/third-person camera over one actor and one combat state.
    /// Input and weapon clocks stay with the player; only presentation changes.</summary>
    public sealed class HauntLowPolyCamera : MonoBehaviour
    {
        public const float TransitionDuration = .35f, ToggleCooldown = .7f;
        const float EyeHeight = 1.6f, PivotHeight = 1.45f, Distance = 3.5f, Shoulder = .5f;
        const float FirstFov = 78, ThirdFov = 68, CollisionRadius = .24f, Skin = .055f;
        const int EnvironmentMask = (1 << 0) | (1 << 10);
        const int AimMask = ~((1 << 8) | (1 << 31));
        HauntFpsGame game;
        HauntFpsPlayer player;
        HauntFpsHeroBody heroBody;
        Camera view, overlay;
        Transform weapon;
        Vector3 startOffset, previousPosition;
        Quaternion startRotation;
        float elapsed, cooldown, startFov;
        bool initialized, disposed, thirdPerson, transitioning;

        public bool IsThirdPerson => thirdPerson;
        public bool IsTransitioning => transitioning;
        public float TransitionProgress => transitioning ? Mathf.Clamp01(elapsed / TransitionDuration) : 1;
        public Vector2 ScreenAimPosition => new Vector2(Screen.width * .5f, Screen.height * .5f);
        public Vector3 AimDirection => AimRay.direction;
        public Vector3 MuzzlePosition => thirdPerson ? SafeWorldMuzzle()
            : weapon ? weapon.TransformPoint(HauntLowPolyArsenal.MuzzleFor(game.State.Weapon))
            : player ? player.Position + Vector3.up * EyeHeight : transform.position;

        public Ray AimRay
        {
            get
            {
                Vector3 origin = view ? view.transform.position : transform.position;
                Vector3 direction = view ? view.transform.forward : transform.forward;
                if (!thirdPerson || !player) return new Ray(origin, direction);
                Vector3 target = origin + direction * FpsPrototypeRules.ShotRange;
                if (Physics.Raycast(origin, direction, out RaycastHit hit,
                    FpsPrototypeRules.ShotRange, AimMask, QueryTriggerInteraction.Ignore)) target = hit.point;
                Vector3 muzzle = SafeWorldMuzzle();
                Vector3 toTarget = target - muzzle;
                // A very close obstruction between camera and actor must not
                // reverse a shot into the actor's back. The muzzle ray still
                // resolves cover in front of the actual weapon in the game.
                if (Vector3.Dot(toTarget, direction) <= .05f) toTarget = direction;
                return new Ray(muzzle, toTarget.normalized);
            }
        }

        public void Initialize(HauntFpsGame owner, HauntFpsPlayer actor, Camera camera, Camera modelCamera, Transform weaponRoot)
        {
            if (initialized || disposed) return;
            game = owner; player = actor; view = camera; overlay = modelCamera; weapon = weaponRoot;
            if (!game || !player || !view) return;
            view.orthographic = false; view.fieldOfView = FirstFov; view.ResetProjectionMatrix();
            previousPosition = view.transform.position;
            initialized = true;
        }

        public bool TryToggle()
        {
            if (!initialized || disposed || !game || !game.CanSimulate || transitioning || cooldown > 0) return false;
            startOffset = view.transform.position - player.Position;
            startRotation = view.transform.rotation; startFov = view.fieldOfView;
            thirdPerson = !thirdPerson; transitioning = true; elapsed = 0; cooldown = ToggleCooldown;
            SetViewModel(false);
            game.Notify(thirdPerson ? "TERCEIRA PESSOA" : "PRIMEIRA PESSOA", 1.2f);
            return true;
        }

        public Vector3 Movement(Vector2 input)
        {
            Transform actor = player ? player.transform : transform;
            return actor.right * input.x + actor.forward * input.y;
        }

        public void Tick(float dt)
        {
            if (!initialized || disposed || !game || !game.CanSimulate || !(dt > 0) || float.IsInfinity(dt)) return;
            dt = Mathf.Min(dt, .05f); cooldown = Mathf.Max(0, cooldown - dt);
            if (!thirdPerson && !transitioning)
            {
                // FPS bob, recoil and FOV remain owned by the player. In this
                // branch the world camera is left untouched.
                previousPosition = view.transform.position; SetViewModel(true); return;
            }
            Quaternion rotation = Quaternion.Euler(player.Pitch, player.transform.eulerAngles.y, 0);
            Vector3 pivot = player.Position + Vector3.up * PivotHeight;
            Vector3 target = thirdPerson ? pivot - rotation * Vector3.forward * Distance + rotation * Vector3.right * Shoulder
                : player.Position + Vector3.up * EyeHeight;
            float fov = thirdPerson ? ThirdFov : FirstFov;
            if (transitioning)
            {
                elapsed = Mathf.Min(TransitionDuration, elapsed + dt);
                float t = Mathf.Clamp01(elapsed / TransitionDuration); t = t * t * (3 - 2 * t);
                target = Vector3.Lerp(player.Position + startOffset, target, t);
                rotation = Quaternion.Slerp(startRotation, rotation, t);
                view.fieldOfView = Mathf.Lerp(startFov, fov, t);
                if (elapsed >= TransitionDuration) transitioning = false;
            }
            else
            {
                target = Vector3.Lerp(previousPosition, target, 1 - Mathf.Exp(-18 * dt));
                view.fieldOfView = Mathf.Lerp(view.fieldOfView, fov, 1 - Mathf.Exp(-14 * dt));
            }
            float trauma = player.CameraTrauma * player.CameraTrauma;
            target += rotation * new Vector3((Mathf.PerlinNoise(19, Time.unscaledTime * 29) * 2 - 1) * .055f,
                (Mathf.PerlinNoise(73, Time.unscaledTime * 31) * 2 - 1) * .04f, 0) * trauma;
            target = CollisionSafe(pivot, target);
            view.transform.position = target; view.transform.rotation = rotation;
            previousPosition = target;
            view.orthographic = false;
            SetViewModel(!thirdPerson && !transitioning);
            if (overlay) overlay.fieldOfView = view.fieldOfView;
        }

        Vector3 CollisionSafe(Vector3 pivot, Vector3 target)
        {
            Vector3 offset = target - pivot; float length = offset.magnitude;
            if (length <= .001f) return target;
            if (Physics.SphereCast(pivot, CollisionRadius, offset / length, out RaycastHit hit,
                length, EnvironmentMask, QueryTriggerInteraction.Ignore))
            {
                // No minimum boom length: forcing one can push the camera
                // through the wall that the sweep just found.
                return pivot + offset / length * Mathf.Max(0, hit.distance - Skin);
            }
            return target;
        }

        Vector3 SafeWorldMuzzle()
        {
            if (!player) return transform.position;
            Vector3 shoulder = player.Position + Vector3.up * 1.2f + player.transform.right * .2f;
            if (!heroBody) heroBody = player.GetComponent<HauntFpsHeroBody>();
            if (heroBody) heroBody.RefreshWeapon();
            Vector3 tip = heroBody ? heroBody.MuzzlePosition : shoulder + player.transform.forward * .65f;
            Vector3 segment = tip - shoulder;
            float distance = segment.magnitude;
            if (distance <= .001f) return shoulder;
            if (Physics.Raycast(shoulder, segment / distance, out RaycastHit hit, distance,
                EnvironmentMask, QueryTriggerInteraction.Ignore)) return shoulder;
            return tip;
        }

        void SetViewModel(bool visible)
        {
            if (overlay) overlay.enabled = visible;
            if (weapon) weapon.gameObject.SetActive(visible);
        }
        // There is no absolute mouse cursor state or warp in either 3D mode.
        public void SuspendInput() { }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (view) { view.orthographic = false; view.fieldOfView = FirstFov; view.ResetProjectionMatrix(); }
        }
        void OnDestroy() => Dispose();
    }
}
