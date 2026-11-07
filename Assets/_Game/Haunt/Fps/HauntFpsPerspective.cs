using UnityEngine;
using UnityEngine.InputSystem;
using Ecos.Haunt.LowPoly;

namespace Ecos.Haunt.Fps
{
    /// <summary>Two views over one physical encounter. This component changes only
    /// camera, aim and input basis; combat and movement timers remain with their owners.</summary>
    public sealed class HauntFpsPerspective : MonoBehaviour
    {
        public const float TransitionDuration = .4f, ToggleCooldown = .7f, ShotHeight = 1.05f;
        const float TopSize = 8.3f, TopPitch = 62, TopDistance = 14.5f;
        static readonly Quaternion TopRotation = Quaternion.Euler(TopPitch, 0, 0);
        static readonly Vector3 TopOffset = -(TopRotation * Vector3.forward) * TopDistance;
        HauntFpsGame game;
        HauntFpsPlayer player;
        HauntLowPolyCamera lowPolyCamera;
        Camera view, modelCamera;
        Transform weaponRoot;
        Vector3 fpsLocalPosition, startOffset, focus, flatAim = Vector3.forward;
        Quaternion fpsLocalRotation, startRotation;
        Matrix4x4 startProjection;
        Vector2 lastMousePosition, warpedMousePosition;
        float fpsFov, elapsed, cooldown;
        int tickSerial, warpTick;
        bool initialized, disposed, topDown, transitioning, controllerAim, pointerWarpPending;

        /// <summary>The requested destination, including while a transition runs.</summary>
        public bool IsTopDown => !lowPolyCamera && topDown;
        public bool IsThirdPerson => lowPolyCamera && lowPolyCamera.IsThirdPerson;
        public bool IsTransitioning => lowPolyCamera ? lowPolyCamera.IsTransitioning : transitioning;
        public bool UsesControllerAim => topDown && controllerAim;
        public float TransitionProgress => lowPolyCamera ? lowPolyCamera.TransitionProgress : transitioning ? Mathf.Clamp01(elapsed / TransitionDuration) : 1;
        public Vector3 AimDirection => lowPolyCamera ? lowPolyCamera.AimDirection : topDown ? flatAim : view ? view.transform.forward : transform.forward;
        public Ray AimRay => lowPolyCamera ? lowPolyCamera.AimRay : topDown
            ? new Ray(ShotOrigin, flatAim)
            : new Ray(view ? view.transform.position : ShotOrigin, AimDirection);
        public Vector3 MuzzlePosition => lowPolyCamera ? lowPolyCamera.MuzzlePosition : topDown ? ShotOrigin + flatAim * .45f
            : weaponRoot ? weaponRoot.TransformPoint(new Vector3(0, .025f, .62f)) : ShotOrigin;
        Vector3 ShotOrigin => (player ? player.Position : transform.position) + Vector3.up * ShotHeight;

        /// <summary>Screen pixels with Unity's lower-left origin, ready for a UI Y flip.</summary>
        public Vector2 ScreenAimPosition
        {
            get
            {
                if (lowPolyCamera) return lowPolyCamera.ScreenAimPosition;
                if (!topDown || !view) return new Vector2(Screen.width * .5f, Screen.height * .5f);
                if (pointerWarpPending) return warpedMousePosition;
                if (!controllerAim && Mouse.current != null)
                    return ClampScreen(Mouse.current.position.ReadValue());
                Vector3 projected = view.WorldToScreenPoint(ShotOrigin + flatAim * 6);
                return ClampScreen(new Vector2(projected.x, projected.y));
            }
        }

        public void Initialize(HauntFpsGame owner, HauntFpsPlayer actor, Camera camera, Camera overlay, Transform weapon)
        {
            if (initialized || disposed) return;
            game = owner; player = actor; view = camera; modelCamera = overlay; weaponRoot = weapon;
            if (!view || !player) return;
            if (owner.LowPolyMode)
            {
                lowPolyCamera = gameObject.AddComponent<HauntLowPolyCamera>();
                lowPolyCamera.Initialize(owner, actor, camera, overlay, weapon);
                initialized = true; return;
            }
            fpsLocalPosition = view.transform.localPosition;
            fpsLocalRotation = view.transform.localRotation; fpsFov = view.fieldOfView;
            focus = GroundFocus();
            flatAim = Horizontal(player.transform.forward, Vector3.forward);
            lastMousePosition = Mouse.current == null ? new Vector2(Screen.width * .5f, Screen.height * .5f)
                : Mouse.current.position.ReadValue();
            controllerAim = Mouse.current == null && Gamepad.current != null;
            initialized = true;
        }

        public bool TryToggle()
        {
            if (lowPolyCamera) return lowPolyCamera.TryToggle();
            if (!initialized || disposed || !game || !game.CanSimulate || transitioning || cooldown > 0) return false;
            startOffset = view.transform.position - player.Position;
            startRotation = view.transform.rotation;
            startProjection = view.projectionMatrix;
            if (!topDown)
            {
                // Save the FPS pose once. Top-down aim may rotate the player body
                // without replacing the first-person yaw/pitch held by the player.
                fpsLocalPosition = view.transform.localPosition;
                fpsLocalRotation = view.transform.localRotation;
                fpsFov = view.fieldOfView;
                flatAim = Horizontal(view.transform.forward, flatAim);
                focus = GroundFocus();
            }
            topDown = !topDown; transitioning = true; elapsed = 0; cooldown = ToggleCooldown;
            if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > .05f) controllerAim = true;
            lastMousePosition = Mouse.current == null ? lastMousePosition : Mouse.current.position.ReadValue();
            // Preserve the exact source projection until Tick blends toward its destination.
            view.orthographic = false; view.projectionMatrix = startProjection;
            SetViewModel(false);
            game.Notify(topDown ? "VISAO 2D - A MESMA LUTA, OUTRA PERSPECTIVA" : "VISAO 3D - DE VOLTA AO OSSUARIO", 1.5f);
            return true;
        }

        public Vector3 Movement(Vector2 input)
        {
            if (lowPolyCamera) return lowPolyCamera.Movement(input);
            if (topDown || transitioning) return Vector3.right * input.x + Vector3.forward * input.y;
            Transform basis = player ? player.transform : transform;
            return basis.right * input.x + basis.forward * input.y;
        }

        public void Tick(float dt)
        {
            if (lowPolyCamera) { lowPolyCamera.Tick(dt); return; }
            if (!initialized || disposed || !game || !game.CanSimulate || !(dt > 0) || float.IsInfinity(dt)) return;
            dt = Mathf.Min(dt, .05f); cooldown = Mathf.Max(0, cooldown - dt); tickSerial++;
            if (transitioning)
            {
                elapsed = Mathf.Min(TransitionDuration, elapsed + dt);
                float t = Mathf.Clamp01(elapsed / TransitionDuration);
                t = t * t * (3 - 2 * t);
                Vector3 targetPosition = topDown ? GroundFocus() + TopOffset : player.transform.TransformPoint(fpsLocalPosition);
                Quaternion targetRotation = topDown ? TopRotation : player.transform.rotation * fpsLocalRotation;
                view.transform.position = Vector3.Lerp(player.Position + startOffset, targetPosition, t);
                view.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                view.projectionMatrix = BlendProjection(startProjection, DestinationProjection(), t);
                SetViewModel(false);
                if (elapsed >= TransitionDuration)
                {
                    transitioning = false; focus = GroundFocus();
                    FinishProjection();
                    if (topDown)
                    {
                        PresentTopDown(dt, true);
                        PlacePointerAhead();
                    }
                    else
                    {
                        view.transform.localPosition = fpsLocalPosition;
                        view.transform.localRotation = fpsLocalRotation;
                        SetViewModel(true);
                    }
                }
            }
            else if (topDown) PresentTopDown(dt, false);
            else SetViewModel(true);
            if (topDown && !transitioning) ReadTopDownAim();
        }

        void PresentTopDown(float dt, bool snap)
        {
            focus = snap ? GroundFocus() : Vector3.Lerp(focus, GroundFocus(), 1 - Mathf.Exp(-12 * dt));
            float shake = player.CameraTrauma * player.CameraTrauma;
            Vector3 tremor = new Vector3((Mathf.PerlinNoise(13, Time.unscaledTime * 29) * 2 - 1) * .16f,
                0, (Mathf.PerlinNoise(71, Time.unscaledTime * 31) * 2 - 1) * .12f) * shake;
            view.transform.position = focus + TopOffset + tremor;
            view.transform.rotation = TopRotation;
            SetViewModel(false);
        }

        Vector3 GroundFocus()
        {
            Vector3 position = player ? player.Position : transform.position;
            // Jumping changes the actor's height without jerking the entire overhead view.
            return new Vector3(position.x, .4f, position.z);
        }

        Matrix4x4 DestinationProjection()
        {
            float aspect = Mathf.Max(.1f, view.aspect);
            return topDown ? Matrix4x4.Ortho(-TopSize * aspect, TopSize * aspect, -TopSize, TopSize, view.nearClipPlane, view.farClipPlane)
                : Matrix4x4.Perspective(fpsFov, aspect, view.nearClipPlane, view.farClipPlane);
        }

        static Matrix4x4 BlendProjection(Matrix4x4 from, Matrix4x4 to, float t)
        {
            var result = new Matrix4x4();
            for (int index = 0; index < 16; index++) result[index] = Mathf.Lerp(from[index], to[index], t);
            return result;
        }

        void FinishProjection()
        {
            view.orthographic = topDown;
            view.orthographicSize = TopSize;
            view.fieldOfView = fpsFov;
            view.ResetProjectionMatrix();
        }

        void ReadTopDownAim()
        {
            // WarpCursorPosition reaches Input System state at its next update.
            // Keep the saved aim and reticle for this final transition frame.
            if (pointerWarpPending)
            {
                if (tickSerial == warpTick) return;
                pointerWarpPending = false;
            }
            Mouse mouse = Mouse.current;
            Vector2 mousePosition = mouse == null ? lastMousePosition : ClampScreen(mouse.position.ReadValue());
            if (mouse != null && ((mousePosition - lastMousePosition).sqrMagnitude > .25f
                || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame)) controllerAim = false;
            lastMousePosition = mousePosition;
            Vector2 stick = Gamepad.current == null ? Vector2.zero : Gamepad.current.rightStick.ReadValue();
            if (stick.sqrMagnitude > .05f)
            {
                controllerAim = true;
                flatAim = Horizontal(new Vector3(stick.x, 0, stick.y), flatAim);
            }
            if (controllerAim || mouse == null) return;
            Ray ray = view.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0));
            var plane = new Plane(Vector3.up, ShotOrigin);
            if (!plane.Raycast(ray, out float distance) || distance < 0 || float.IsInfinity(distance) || float.IsNaN(distance)) return;
            Vector3 point = ray.GetPoint(distance);
            // A pointer beyond the room still aims toward its nearest boundary;
            // cover and enemies are resolved by the shared physical firing ray.
            point.x = Mathf.Clamp(point.x, -HauntFpsWorld.Width * .5f, HauntFpsWorld.Width * .5f);
            point.z = Mathf.Clamp(point.z, -HauntFpsWorld.Depth * .5f, HauntFpsWorld.Depth * .5f);
            flatAim = Horizontal(point - ShotOrigin, flatAim);
        }

        void PlacePointerAhead()
        {
            if (controllerAim || Mouse.current == null || !game || !game.CanSimulate) return;
            Vector3 projected = view.WorldToScreenPoint(ShotOrigin + flatAim * 4);
            warpedMousePosition = ClampScreen(new Vector2(projected.x, projected.y));
            lastMousePosition = warpedMousePosition;
            pointerWarpPending = true; warpTick = tickSerial;
            Mouse.current.WarpCursorPosition(warpedMousePosition);
        }

        static Vector3 Horizontal(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0;
            return direction.sqrMagnitude > .0025f ? direction.normalized : fallback;
        }

        static Vector2 ClampScreen(Vector2 position) => new Vector2(Mathf.Clamp(position.x, 0, Mathf.Max(1, Screen.width - 1)),
            Mathf.Clamp(position.y, 0, Mathf.Max(1, Screen.height - 1)));

        void SetViewModel(bool visible)
        {
            if (modelCamera) modelCamera.enabled = visible;
            if (weaponRoot) weaponRoot.gameObject.SetActive(visible);
        }

        public void SuspendInput()
        {
            if (lowPolyCamera) { lowPolyCamera.SuspendInput(); return; }
            pointerWarpPending = false;
            if (Mouse.current != null) lastMousePosition = Mouse.current.position.ReadValue();
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (lowPolyCamera) { lowPolyCamera.Dispose(); return; }
            if (view)
            {
                view.orthographic = false; view.fieldOfView = fpsFov;
                view.ResetProjectionMatrix();
            }
        }

        void OnDestroy() => Dispose();
    }
}
