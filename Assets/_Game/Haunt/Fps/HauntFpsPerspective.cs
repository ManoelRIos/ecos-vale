using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt.Fps
{
    /// <summary>Two views over one physical encounter. This component changes only
    /// camera, aim and input basis; combat and movement timers remain with their owners.</summary>
    public sealed class HauntFpsPerspective : MonoBehaviour
    {
        public const float TransitionDuration = .4f, ToggleCooldown = .7f, ShotHeight = 1.05f;
        /// <summary>Path fraction where sprites replace models. Past this point the dolly
        /// zoom has flattened the image enough for the swap to read as one continuous view.</summary>
        public const float ArtSwapPoint = .55f;
        const float DefaultTopSize = 8.3f, TopPitch = 62, TopDistance = 14.5f;
        /// <summary>Half height of the overhead frame. With the low-resolution frame on it is
        /// sized so one rendered pixel equals one actor-sprite pixel (pixel-perfect art),
        /// kept within a range that preserves the playable view on unusual resolutions.</summary>
        static float TopSize => HauntFpsGame.RenderPixelHeight > 0
            ? Mathf.Clamp(HauntFpsGame.RenderPixelHeight * HauntFpsGame.PixelUnit * .5f, 7.5f, 9.5f) : DefaultTopSize;
        // The dolly path ends on a near-orthographic lens; the final hand-off to the real
        // orthographic projection then changes the image by less than a pixel.
        const float DollyEndFov = 6, TransitionFarClip = 260;
        static readonly Quaternion TopRotation = Quaternion.Euler(TopPitch, 0, 0);
        static readonly Vector3 TopOffset = -(TopRotation * Vector3.forward) * TopDistance;
        HauntFpsGame game;
        HauntFpsPlayer player;
        Camera view, modelCamera;
        Transform weaponRoot;
        Vector3 fpsLocalPosition, focus, flatAim = Vector3.forward;
        Quaternion fpsLocalRotation;
        Vector2 lastMousePosition, warpedMousePosition;
        float fpsFov, fpsNearClip, fpsFarClip, elapsed, cooldown;
        int tickSerial, warpTick;
        bool initialized, disposed, topDown, transitioning, controllerAim, pointerWarpPending;

        /// <summary>The requested destination, including while a transition runs.</summary>
        public bool IsTopDown => topDown;
        public bool IsTransitioning => transitioning;
        public bool UsesControllerAim => topDown && controllerAim;
        public float TransitionProgress => transitioning ? Mathf.Clamp01(elapsed / TransitionDuration) : 1;
        /// <summary>0 = first-person eye, 1 = overhead view, along the eased camera path.
        /// Presentation (sprites, models, wall height) follows this value instead of the
        /// requested destination, so nothing pops while the camera is still at eye level.</summary>
        public float OverheadAmount
        {
            get
            {
                if (!transitioning) return topDown ? 1 : 0;
                float eased = Ease(TransitionProgress);
                return topDown ? eased : 1 - eased;
            }
        }
        /// <summary>True when sprite art should be shown instead of 3D models.</summary>
        public bool ShowsTopDownArt => OverheadAmount >= ArtSwapPoint;
        public Vector3 AimDirection => topDown ? flatAim : view ? view.transform.forward : transform.forward;
        public Ray AimRay => topDown
            ? new Ray(ShotOrigin, flatAim)
            : new Ray(view ? view.transform.position : ShotOrigin, AimDirection);
        public Vector3 MuzzlePosition => topDown ? ShotOrigin + flatAim * .45f
            : weaponRoot ? weaponRoot.TransformPoint(new Vector3(0, .025f, .62f)) : ShotOrigin;
        Vector3 ShotOrigin => (player ? player.Position : transform.position) + Vector3.up * ShotHeight;

        /// <summary>Screen pixels with Unity's lower-left origin, ready for a UI Y flip.</summary>
        public Vector2 ScreenAimPosition
        {
            get
            {
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
            fpsLocalPosition = view.transform.localPosition;
            fpsLocalRotation = view.transform.localRotation; fpsFov = view.fieldOfView;
            fpsNearClip = view.nearClipPlane; fpsFarClip = view.farClipPlane;
            focus = GroundFocus();
            flatAim = Horizontal(player.transform.forward, Vector3.forward);
            lastMousePosition = Mouse.current == null ? new Vector2(Screen.width * .5f, Screen.height * .5f)
                : Mouse.current.position.ReadValue();
            controllerAim = Mouse.current == null && Gamepad.current != null;
            initialized = true;
        }

        public bool TryToggle()
        {
            if (!initialized || disposed || !game || !game.CanSimulate || transitioning || cooldown > 0) return false;
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
            // The dolly path is drawn with an ordinary perspective lens; Tick places it.
            view.orthographic = false; view.ResetProjectionMatrix();
            view.farClipPlane = Mathf.Max(fpsFarClip, TransitionFarClip);
            SetViewModel(false);
            game.Notify(topDown ? "VISAO 2D - A MESMA LUTA, OUTRA PERSPECTIVA" : "VISAO 3D - DE VOLTA AO OSSUARIO", 1.5f);
            return true;
        }

        public Vector3 Movement(Vector2 input)
        {
            if (topDown || transitioning) return Vector3.right * input.x + Vector3.forward * input.y;
            Transform basis = player ? player.transform : transform;
            return basis.right * input.x + basis.forward * input.y;
        }

        public void Tick(float dt)
        {
            if (!initialized || disposed || !game || !game.CanSimulate || !(dt > 0) || float.IsInfinity(dt)) return;
            dt = Mathf.Min(dt, .05f); cooldown = Mathf.Max(0, cooldown - dt); tickSerial++;
            if (transitioning)
            {
                elapsed = Mathf.Min(TransitionDuration, elapsed + dt);
                PlaceOnDollyPath(OverheadAmount);
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
            view.transform.position = SnapToPixels(focus + TopOffset + tremor);
            view.transform.rotation = TopRotation;
            view.orthographicSize = TopSize;
            SetViewModel(false);
        }

        /// <summary>Moves a point onto the overhead view's pixel grid, measured in the camera
        /// plane, so the low-resolution frame scrolls by whole pixels and the art never shimmers.</summary>
        public static Vector3 SnapToPixels(Vector3 point)
        {
            int height = HauntFpsGame.RenderPixelHeight;
            if (height <= 0) return point;
            float unit = 2 * TopSize / height;
            Vector3 right = TopRotation * Vector3.right, up = TopRotation * Vector3.up, forward = TopRotation * Vector3.forward;
            float x = Mathf.Round(Vector3.Dot(point, right) / unit) * unit;
            float y = Mathf.Round(Vector3.Dot(point, up) / unit) * unit;
            return right * x + up * y + forward * Vector3.Dot(point, forward);
        }

        Vector3 GroundFocus()
        {
            Vector3 position = player ? player.Position : transform.position;
            // Jumping changes the actor's height without jerking the entire overhead view.
            return new Vector3(position.x, .4f, position.z);
        }

        static float Ease(float t) => t * t * (3 - 2 * t);

        /// <summary>Dolly zoom between the eye and the overhead view. The lens narrows
        /// from the FPS field of view to an almost orthographic one while the camera backs
        /// away so the framed area grows to exactly the overhead frame. Amount 0 is the eye
        /// pose and 1 matches the orthographic view, so both ends join without a jump.</summary>
        void PlaceOnDollyPath(float amount)
        {
            amount = Mathf.Clamp01(amount);
            Vector3 eye = player.transform.TransformPoint(fpsLocalPosition);
            Quaternion eyeRotation = player.transform.rotation * fpsLocalRotation;
            // Turning toward the overhead pitch leads the pull-back slightly, so the
            // player sees the arena floor open up before the walls sink.
            float turn = 1 - (1 - amount) * (1 - amount);
            Quaternion rotation = Quaternion.Slerp(eyeRotation, TopRotation, turn);
            float fov = Mathf.Exp(Mathf.Lerp(Mathf.Log(Mathf.Max(1, fpsFov)), Mathf.Log(DollyEndFov), amount));
            float halfHeight = TopSize * amount;
            float distance = halfHeight / Mathf.Tan(fov * .5f * Mathf.Deg2Rad);
            Vector3 pivot = Vector3.Lerp(eye, GroundFocus(), amount);
            view.transform.position = pivot - rotation * Vector3.forward * distance;
            view.transform.rotation = rotation;
            view.fieldOfView = fov;
            // Keep the depth range proportional to the pull-back so distant floor
            // decals do not shimmer while the lens is long.
            view.nearClipPlane = Mathf.Max(fpsNearClip, distance * .1f);
        }

        void FinishProjection()
        {
            view.orthographic = topDown;
            view.orthographicSize = TopSize;
            view.fieldOfView = fpsFov;
            view.nearClipPlane = fpsNearClip;
            view.farClipPlane = fpsFarClip;
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
            pointerWarpPending = false;
            if (Mouse.current != null) lastMousePosition = Mouse.current.position.ReadValue();
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (view)
            {
                view.orthographic = false; view.fieldOfView = fpsFov;
                if (fpsFarClip > 0) { view.nearClipPlane = fpsNearClip; view.farClipPlane = fpsFarClip; }
                view.ResetProjectionMatrix();
            }
        }

        void OnDestroy() => Dispose();
    }
}
