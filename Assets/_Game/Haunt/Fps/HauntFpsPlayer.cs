using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ecos.Haunt.Fps
{
    /// <summary>Shared first-person/top-down input, CharacterController movement and weapon presentation.
    /// HauntFpsGame owns ammunition, damage, pause/focus and the simulation clock.</summary>
    public sealed class HauntFpsPlayer : MonoBehaviour
    {
        public const float DodgeDuration = FpsPrototypeRules.DodgeDuration, DodgeCooldown = FpsPrototypeRules.DodgeCooldown;
        const float WalkSpeed = FpsPrototypeRules.PlayerSpeed, DodgeSpeed = 13.5f, Gravity = 23f, EyeHeight = 1.6f, BaseFov = 78f;
        const int ViewModelLayer = 31, CasingLimit = 40;
        readonly List<Casing> casings = new List<Casing>();
        HauntFpsGame game;
        CharacterController body;
        HauntFpsPerspective perspective;
        Camera view, modelCamera;
        Transform weaponRoot, flashRoot, worldFlashRoot;
        Light flashLight, worldFlashLight;
        Material casingMaterial;
        Vector3 muzzleLocal = new Vector3(0, .025f, .62f);
        Color muzzleTint;
        int originalCullingMask;
        float yaw, pitch, verticalSpeed, dodgeRemaining, dodgeReady, shotReady;
        float recoilAngle, gunKick, flashRemaining, walkPhase, footstepRemaining, trauma, clock;
        Vector3 dodgeDirection;
        bool initialized, disposed, fireNeedsRelease = true;

        sealed class Casing
        {
            public Transform Transform;
            public Vector3 Position, Velocity;
            public float Remaining = 18, Spin;
            public bool Grounded;
        }

        public Camera View => view;
        /// <summary>The overlay camera that draws the weapon and hands in first person.</summary>
        public Camera ModelCamera => modelCamera;
        public HauntFpsPerspective Perspective => perspective;
        public bool IsTopDown => perspective && perspective.IsTopDown;
        public bool IsPerspectiveTransition => perspective && perspective.IsTransitioning;
        /// <summary>Where the camera actually is along the 2D/FPS path (0 = eye, 1 = overhead).</summary>
        public float OverheadAmount => perspective ? perspective.OverheadAmount : 0;
        /// <summary>Sprites replace models once the camera is far enough along the path.</summary>
        public bool ShowsTopDownArt => perspective && perspective.ShowsTopDownArt;
        public Vector3 Position => transform.position;
        public Vector3 MuzzlePosition => IsTopDown ? perspective.MuzzlePosition
            : weaponRoot ? weaponRoot.TransformPoint(muzzleLocal) : view ? view.transform.position : Position;
        public bool IsDodging => dodgeRemaining > 0;
        public bool IsInvulnerable => DodgeRules.IsInvulnerable(dodgeRemaining, DodgeDuration);
        public float DodgeCooldownRemaining => Mathf.Max(0, dodgeReady);
        public float Pitch => pitch;
        /// <summary>Freezes movement, aim, dodge, reload and firing (boss introductions). The
        /// camera keeps presenting, so cutscenes can steer it.</summary>
        public bool InputLocked { get; set; }

        /// <summary>Turns the first-person view toward a point (used by the boss introduction).</summary>
        public void FaceTowards(Vector3 point, float dt)
        {
            if (!initialized || IsTopDown || IsPerspectiveTransition) return;
            Vector3 delta = point - view.transform.position;
            if (delta.sqrMagnitude < .01f) return;
            float targetYaw = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
            float targetPitch = -Mathf.Atan2(delta.y, new Vector2(delta.x, delta.z).magnitude) * Mathf.Rad2Deg;
            float blend = 1 - Mathf.Exp(-4 * dt);
            yaw = Mathf.LerpAngle(yaw, targetYaw, blend); pitch = Mathf.Lerp(pitch, Mathf.Clamp(targetPitch, -40, 40), blend);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        public float CameraTrauma => trauma;

        public void Initialize(HauntFpsGame owner, Camera camera, CharacterController controller)
        {
            if (initialized) return;
            game = owner; view = camera; body = controller;
            body.height = 1.8f; body.radius = .32f; body.center = new Vector3(0, .9f, 0);
            body.skinWidth = .035f; body.stepOffset = .26f; body.slopeLimit = 48;
            view.transform.SetParent(transform, false);
            view.transform.localPosition = new Vector3(0, EyeHeight, 0);
            view.transform.localRotation = Quaternion.identity;
            view.nearClipPlane = .04f; view.farClipPlane = 80; view.fieldOfView = BaseFov;
            yaw = transform.eulerAngles.y;
            BuildViewModel();
            perspective = gameObject.AddComponent<HauntFpsPerspective>();
            perspective.Initialize(game, this, view, modelCamera, weaponRoot);
            initialized = true;
        }

        public void Tick(float dt)
        {
            if (!initialized || disposed || !game || !game.CanSimulate || !(dt > 0)) return;
            dt = Mathf.Min(dt, .05f);
            clock += dt;
            shotReady = Mathf.Max(0, shotReady - dt);
            dodgeReady = Mathf.Max(0, dodgeReady - dt);
            flashRemaining = Mathf.Max(0, flashRemaining - dt);
            recoilAngle = Mathf.Lerp(recoilAngle, 0, 1 - Mathf.Exp(-15 * dt));
            gunKick = Mathf.Lerp(gunKick, 0, 1 - Mathf.Exp(-17 * dt));
            trauma = Mathf.Max(0, trauma - dt * 1.65f);
            bool locked = InputLocked;
            if (!locked && PerspectivePressed()) TryTogglePerspective();
            bool wasTransitioning = IsPerspectiveTransition;
            if (!locked && !IsTopDown && !wasTransitioning) ReadLook(dt);
            Vector2 input = locked ? Vector2.zero : MoveInput();
            Vector3 wish = perspective ? perspective.Movement(input) : transform.right * input.x + transform.forward * input.y;
            if (!locked && ReloadPressed() && game.State.TryReload())
            {
                flashRemaining = 0;
                game.PlayCue(FpsCue.Reload, Position);
            }
            if (!locked && DodgePressed()) TryDodge(wish);
            Move(wish, input.magnitude, dt);
            if (perspective) perspective.Tick(dt);
            if (IsTopDown)
            {
                Vector3 facing = perspective.AimDirection; facing.y = 0;
                if (facing.sqrMagnitude > .001f)
                {
                    // The camera is parented to the actor. Turning the body must
                    // preserve the world-space north-up camera just presented.
                    Vector3 cameraPosition = view.transform.position;
                    Quaternion cameraRotation = view.transform.rotation;
                    transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
                    view.transform.position = cameraPosition; view.transform.rotation = cameraRotation;
                }
            }
            bool held = !locked && FireHeld();
            if (wasTransitioning || IsPerspectiveTransition) fireNeedsRelease = true;
            else if (!held) fireNeedsRelease = false;
            if (held && !fireNeedsRelease && shotReady <= 0 && !IsDodging && !IsPerspectiveTransition)
            {
                Ray shot = perspective ? perspective.AimRay : new Ray(view.transform.position, view.transform.forward);
                if (game.TryFire(shot))
                {
                    shotReady = Mathf.Max(.05f, game.State.WeaponInterval);
                    recoilAngle = Mathf.Min(4.5f, recoilAngle + 1.4f);
                    gunKick = .065f; flashRemaining = .045f;
                    EjectCasing();
                }
                else if (!game.State.IsReloading && game.State.Magazine == 0)
                    shotReady = .25f;
            }
            PresentView(input.magnitude, dt);
            TickCasings(dt);
        }

        void ReadLook(float dt)
        {
            var mouse = Mouse.current;
            Vector2 delta = mouse == null ? Vector2.zero : mouse.delta.ReadValue();
            yaw += delta.x * .09f; pitch -= delta.y * .09f;
            var pad = Gamepad.current;
            Vector2 stick = pad == null ? Vector2.zero : Deadzone(pad.rightStick.ReadValue());
            yaw += stick.x * 155 * dt; pitch -= stick.y * 125 * dt;
            pitch = Mathf.Clamp(pitch, -78, 78);
            yaw = Mathf.Repeat(yaw, 360);
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            // The shot in this same Tick must use the current mouse/stick pitch.
            view.transform.localRotation = Quaternion.Euler(pitch - recoilAngle, 0, 0);
        }

        static Vector2 Deadzone(Vector2 value)
        {
            float magnitude = value.magnitude;
            return magnitude <= .15f ? Vector2.zero : value / magnitude * Mathf.Clamp01((magnitude - .15f) / .85f);
        }

        static Vector2 MoveInput()
        {
            Vector2 result = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) result.y++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) result.y--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) result.x++;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) result.x--;
            }
            if (Gamepad.current != null) result += Deadzone(Gamepad.current.leftStick.ReadValue());
            return Vector2.ClampMagnitude(result, 1);
        }

        static bool ReloadPressed() => (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
        static bool DodgePressed() => (Keyboard.current != null
                && (Keyboard.current.leftAltKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame))
            || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        static bool JumpPressed() => (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        static bool FireHeld() => (Mouse.current != null && Mouse.current.leftButton.isPressed)
            || (Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > .2f);
        static bool PerspectivePressed() => (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame);

        public bool TryTogglePerspective()
        {
            if (!initialized || disposed || !game || !game.CanSimulate || !perspective || !perspective.TryToggle()) return false;
            // Top-down facing never changes the stored FPS yaw/pitch. Restore the
            // original heading after the camera captures its transition start.
            if (!perspective.IsTopDown) transform.rotation = Quaternion.Euler(0, yaw, 0);
            SuspendInput();
            return true;
        }

        public bool TryDodge(Vector3 direction)
        {
            if (!initialized || disposed || !game || !game.CanSimulate || game.State.IsDead || dodgeReady > 0 || IsDodging) return false;
            direction.y = 0;
            dodgeDirection = direction.sqrMagnitude > .001f ? direction.normalized : transform.forward;
            dodgeRemaining = DodgeDuration; dodgeReady = DodgeCooldown;
            flashRemaining = 0;
            game.PlayCue(FpsCue.Dodge, Position);
            return true;
        }

        void Move(Vector3 wish, float amount, float dt)
        {
            bool grounded = body.isGrounded;
            if (grounded && verticalSpeed < 0) verticalSpeed = -2;
            if (grounded && JumpPressed() && !IsDodging) verticalSpeed = 6.6f;
            verticalSpeed = Mathf.Max(-30, verticalSpeed - Gravity * dt);
            float dodgeStep = Mathf.Min(dodgeRemaining, dt);
            Vector3 horizontal = dodgeDirection * (DodgeSpeed * dodgeStep)
                + wish * (WalkSpeed * game.State.MoveSpeedMultiplier * (dt - dodgeStep));
            CollisionFlags contact = body.Move(horizontal + Vector3.up * (verticalSpeed * dt));
            if ((contact & CollisionFlags.Above) != 0 && verticalSpeed > 0) verticalSpeed = 0;
            if (IsDodging) dodgeRemaining = Mathf.Max(0, dodgeRemaining - dt);
            if (amount > .1f && grounded && !IsDodging)
            {
                walkPhase += dt * amount * 10;
                footstepRemaining -= dt;
                if (footstepRemaining <= 0)
                {
                    footstepRemaining = .36f;
                    game.PlayCue(FpsCue.Step, Position);
                }
            }
            else footstepRemaining = Mathf.Min(footstepRemaining, .08f);
        }

        void PresentView(float movement, float dt)
        {
            float shake = trauma * trauma;
            float bob = body.isGrounded && movement > .1f && !IsDodging ? Mathf.Sin(walkPhase * 2) * .022f * movement : 0;
            float roll = IsDodging ? Mathf.Sin((1 - dodgeRemaining / DodgeDuration) * Mathf.PI) * -Vector3.Dot(dodgeDirection, transform.right) * 3 : 0;
            Vector3 offset = new Vector3((Mathf.PerlinNoise(13, clock * 29) * 2 - 1) * .028f * shake,
                EyeHeight + bob + (Mathf.PerlinNoise(71, clock * 31) * 2 - 1) * .024f * shake, 0);
            // The perspective component owns the world camera during the switch
            // and in top-down view; weapon animation continues on its own clock.
            if (!IsTopDown && !IsPerspectiveTransition)
            {
                view.transform.localPosition = offset;
                view.transform.localRotation = Quaternion.Euler(pitch - recoilAngle, 0, roll);
                view.fieldOfView = Mathf.Lerp(view.fieldOfView, BaseFov + (IsDodging ? 3 : 0), 1 - Mathf.Exp(-12 * dt));
                if (modelCamera) modelCamera.fieldOfView = view.fieldOfView;
            }
            float reload = game.State.IsReloading ? Mathf.Sin(game.State.ReloadProgress * Mathf.PI) : 0;
            weaponRoot.localPosition = new Vector3(.24f + Mathf.Sin(walkPhase) * .006f * movement,
                -.225f - reload * .15f - (IsDodging ? .08f : 0) + bob * .35f, .42f - gunKick - reload * .045f);
            weaponRoot.localRotation = Quaternion.Euler(reload * 18 + gunKick * -85, 0, reload * -48);
            bool flashing = flashRemaining > 0 && !game.State.IsReloading && !IsDodging && !IsPerspectiveTransition;
            flashRoot.gameObject.SetActive(flashing && !IsTopDown);
            if (flashLight) flashLight.enabled = flashing && !IsTopDown;
            if (worldFlashRoot)
            {
                worldFlashRoot.gameObject.SetActive(flashing && IsTopDown);
                if (flashing && IsTopDown)
                {
                    worldFlashRoot.position = MuzzlePosition;
                    worldFlashRoot.rotation = Quaternion.LookRotation(perspective.AimDirection, Vector3.up);
                }
            }
            if (worldFlashLight) worldFlashLight.enabled = flashing && IsTopDown;
        }

        public void AddTrauma(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return;
            trauma = Mathf.Clamp01(trauma + amount);
        }

        public void SuspendInput()
        {
            fireNeedsRelease = true;
            flashRemaining = 0;
            if (flashRoot) flashRoot.gameObject.SetActive(false);
            if (flashLight) flashLight.enabled = false;
            if (worldFlashRoot) worldFlashRoot.gameObject.SetActive(false);
            if (worldFlashLight) worldFlashLight.enabled = false;
            if (perspective) perspective.SuspendInput();
        }

        public void ResetView()
        {
            pitch = recoilAngle = gunKick = trauma = verticalSpeed = 0;
            dodgeRemaining = dodgeReady = shotReady = flashRemaining = 0;
            walkPhase = footstepRemaining = 0;
            ResetViewTransforms();
        }

        void ResetViewTransforms()
        {
            yaw = transform.eulerAngles.y;
            fireNeedsRelease = true;
            if (view)
            {
                view.transform.localPosition = new Vector3(0, EyeHeight, 0);
                view.transform.localRotation = Quaternion.identity;
                view.fieldOfView = BaseFov;
            }
            if (modelCamera) modelCamera.fieldOfView = BaseFov;
            if (weaponRoot)
            {
                weaponRoot.localPosition = new Vector3(.24f, -.225f, .42f);
                weaponRoot.localRotation = Quaternion.identity;
            }
            if (flashRoot) flashRoot.gameObject.SetActive(false);
            if (flashLight) flashLight.enabled = false;
            if (worldFlashRoot) worldFlashRoot.gameObject.SetActive(false);
            if (worldFlashLight) worldFlashLight.enabled = false;
        }

        void BuildViewModel()
        {
            originalCullingMask = view.cullingMask;
            view.cullingMask &= ~(1 << ViewModelLayer);
            var modelObject = new GameObject("FPS viewmodel overlay");
            modelObject.transform.SetParent(view.transform, false);
            modelCamera = modelObject.AddComponent<Camera>();
            modelCamera.nearClipPlane = .01f; modelCamera.farClipPlane = 2;
            modelCamera.fieldOfView = BaseFov; modelCamera.cullingMask = 1 << ViewModelLayer;
            modelCamera.clearFlags = CameraClearFlags.Depth;
            var baseData = view.GetUniversalAdditionalCameraData();
            baseData.renderType = CameraRenderType.Base;
            var modelData = modelCamera.GetUniversalAdditionalCameraData();
            modelData.renderType = CameraRenderType.Overlay; modelData.renderPostProcessing = false;
            baseData.cameraStack.Add(modelCamera);
            weaponRoot = new GameObject("Revolver and gloved hands").transform;
            weaponRoot.SetParent(modelObject.transform, false);
            weaponRoot.localPosition = new Vector3(.24f, -.225f, .42f);
            Material metal = game.PixelMaterial("fps-gun-steel", new Color(.24f, .3f, .33f));
            Material dark = game.PixelMaterial("fps-gun-dark", new Color(.07f, .09f, .12f));
            Material trim = game.PixelMaterial("fps-gun-brass", new Color(.69f, .46f, .2f));
            CharacterKind character = game.State.Character;
            string characterId = character.ToString();
            Color gloveColor = character == CharacterKind.Reaper ? new Color(.67f, .65f, .54f)
                : character == CharacterKind.Medium ? new Color(.35f, .23f, .47f)
                : character == CharacterKind.Alchemist ? new Color(.21f, .36f, .25f) : new Color(.23f, .25f, .18f);
            Color sleeveColor = character == CharacterKind.Reaper ? new Color(.35f, .07f, .1f)
                : character == CharacterKind.Medium ? new Color(.23f, .11f, .38f)
                : character == CharacterKind.Alchemist ? new Color(.2f, .31f, .23f) : new Color(.14f, .2f, .22f);
            muzzleTint = character == CharacterKind.Medium ? new Color(.72f, .38f, 1)
                : character == CharacterKind.Reaper ? new Color(.75f, .95f, .65f)
                : character == CharacterKind.Alchemist ? new Color(.3f, 1, .65f) : new Color(1, .82f, .31f);
            Material glove = game.PixelMaterial("fps-gloves-" + characterId, gloveColor);
            Material sleeve = game.PixelMaterial("fps-sleeves-" + characterId, sleeveColor);
            Material bone = game.PixelMaterial("fps-gun-bone", new Color(.8f, .78f, .64f));
            Material energy = game.PixelMaterial("fps-gun-energy-" + characterId, muzzleTint);
            energy.SetColor("_EmissionColor", muzzleTint * .7f);
            casingMaterial = character == CharacterKind.Hunter ? trim : character == CharacterKind.Reaper ? bone : energy;
            if (character == CharacterKind.Medium) BuildVeilWand(dark, trim, energy);
            else if (character == CharacterKind.Reaper) BuildBoneScythe(dark, metal, bone, energy);
            else if (character == CharacterKind.Alchemist) BuildArcCoil(dark, metal, trim, energy);
            else BuildHunterCarbine(metal, dark, trim, bone, energy);
            Part("Right glove", new Vector3(0, -.14f, -.02f), new Vector3(.14f, .14f, .17f), glove);
            Part("Right sleeve", new Vector3(.035f, -.255f, -.125f), new Vector3(.17f, .19f, .32f), sleeve, new Vector3(-22, -8, -8));
            Part("Support glove", new Vector3(-.11f, -.08f, .23f), new Vector3(.13f, .12f, .18f), glove, new Vector3(0, 0, 15));
            Part("Support sleeve", new Vector3(-.24f, -.18f, .1f), new Vector3(.17f, .18f, .3f), sleeve, new Vector3(-20, 18, -30));
            flashRoot = new GameObject("Muzzle flash").transform;
            flashRoot.SetParent(weaponRoot, false); flashRoot.localPosition = muzzleLocal;
            Material flash = game.PixelMaterial("fps-muzzle-" + characterId, muzzleTint);
            flash.EnableKeyword("_EMISSION"); flash.SetColor("_EmissionColor", muzzleTint * 2.2f);
            game.Box("Flash core", Vector3.zero, new Vector3(.055f, .055f, .12f), flash, flashRoot, false);
            game.Box("Flash cross", new Vector3(0, 0, .055f), new Vector3(.12f, .025f, .035f), flash, flashRoot, false);
            flashLight = flashRoot.gameObject.AddComponent<Light>(); flashLight.type = LightType.Point;
            flashLight.color = muzzleTint; flashLight.range = 2.7f; flashLight.intensity = 3;
            flashLight.shadows = LightShadows.None; flashLight.enabled = false;
            SetLayer(modelObject, ViewModelLayer);
            flashRoot.gameObject.SetActive(false);
            worldFlashRoot = new GameObject("Top-down muzzle flash").transform;
            worldFlashRoot.SetParent(game.transform, false);
            game.Box("World flash core", Vector3.zero, new Vector3(.11f, .11f, .2f), flash, worldFlashRoot, false);
            game.Box("World flash cross", new Vector3(0, 0, .08f), new Vector3(.22f, .05f, .05f), flash, worldFlashRoot, false);
            worldFlashLight = worldFlashRoot.gameObject.AddComponent<Light>();
            worldFlashLight.type = LightType.Point; worldFlashLight.color = muzzleTint;
            worldFlashLight.range = 2.7f; worldFlashLight.intensity = 2; worldFlashLight.shadows = LightShadows.None;
            worldFlashRoot.gameObject.SetActive(false); worldFlashLight.enabled = false;
        }

        void BuildHunterCarbine(Material metal, Material dark, Material trim, Material bone, Material energy)
        {
            weaponRoot.name = "Carabina da Aurora / luvas do Caçador";
            muzzleLocal = new Vector3(0, .025f, .78f);
            Part("Receiver", new Vector3(0, 0, .1f), new Vector3(.19f, .14f, .34f), metal);
            Part("Rifled barrel", new Vector3(0, .025f, .47f), new Vector3(.105f, .1f, .49f), dark);
            Part("Muzzle ring", new Vector3(0, .025f, .725f), new Vector3(.13f, .12f, .045f), trim);
            Part("Cylinder", new Vector3(.02f, -.005f, .075f), new Vector3(.215f, .13f, .17f), trim);
            Part("Top rail", new Vector3(0, .09f, .2f), new Vector3(.12f, .035f, .46f), dark);
            Part("Front sight", new Vector3(0, .09f, .67f), new Vector3(.025f, .04f, .035f), energy);
            Part("Rear sight", new Vector3(0, .115f, -.02f), new Vector3(.065f, .035f, .025f), metal);
            Part("Grip", new Vector3(0, -.14f, -.015f), new Vector3(.105f, .22f, .13f), dark, new Vector3(-15, 0, 0));
            for (int i = 0; i < 3; i++)
                Part("Grip notch " + i, new Vector3(.056f, -.09f - i * .05f, -.015f), new Vector3(.012f, .018f, .09f), trim);
            Part("Soul chamber", new Vector3(-.102f, .015f, .13f), new Vector3(.018f, .065f, .12f), energy);
            Part("Bone charm", new Vector3(.115f, .005f, .1f), new Vector3(.012f, .06f, .055f), bone);
            Part("Charm eye A", new Vector3(.123f, .018f, .087f), new Vector3(.006f, .012f, .012f), dark);
            Part("Charm eye B", new Vector3(.123f, .018f, .113f), new Vector3(.006f, .012f, .012f), dark);
            Part("Shoulder stock", new Vector3(0, -.025f, -.2f), new Vector3(.17f, .16f, .22f), dark);
            for (int band = 0; band < 3; band++)
                Part("Barrel band", new Vector3(0, .025f, .35f + band * .13f), new Vector3(.125f, .125f, .025f), trim);
        }

        void BuildVeilWand(Material dark, Material trim, Material energy)
        {
            weaponRoot.name = "Varinha do Véu / mangas do Médium";
            muzzleLocal = new Vector3(0, .07f, .66f);
            Part("Obsidian staff", new Vector3(0, -.015f, .18f), new Vector3(.075f, .075f, .69f), dark);
            for (int band = 0; band < 4; band++)
                Part("Arcane binding", new Vector3(0, -.015f, -.08f + band * .15f), new Vector3(.092f, .092f, .035f), trim);
            Part("Crystal cradle left", new Vector3(-.085f, .025f, .48f), new Vector3(.04f, .04f, .22f), trim, new Vector3(0, 18, 0));
            Part("Crystal cradle right", new Vector3(.085f, .025f, .48f), new Vector3(.04f, .04f, .22f), trim, new Vector3(0, -18, 0));
            Part("Veil crystal", new Vector3(0, .065f, .55f), new Vector3(.13f, .13f, .19f), energy, new Vector3(0, 0, 45));
            Part("Crystal facet", new Vector3(0, .09f, .62f), new Vector3(.055f, .055f, .12f), energy, new Vector3(0, 0, 45));
            Part("Runic seal", new Vector3(0, .035f, .18f), new Vector3(.032f, .018f, .13f), energy);
            for (int orb = 0; orb < 3; orb++)
                Part("Bound orb", new Vector3((orb - 1) * .11f, .16f + (orb % 2) * .04f, .43f), Vector3.one * .055f, energy, new Vector3(0, 0, 45));
        }

        void BuildBoneScythe(Material dark, Material metal, Material bone, Material energy)
        {
            weaponRoot.name = "Ceifador de Ossos / foice do Ceifador";
            muzzleLocal = new Vector3(0, .045f, .68f);
            Part("Scythe haft", new Vector3(0, -.045f, .18f), new Vector3(.075f, .08f, .78f), dark);
            for (int grip = 0; grip < 4; grip++)
                Part("Bone grip", new Vector3(0, -.045f, -.06f + grip * .13f), new Vector3(.1f, .1f, .045f), bone);
            Part("Blade heel", new Vector3(-.07f, .06f, .5f), new Vector3(.24f, .06f, .13f), metal, new Vector3(0, 15, 0));
            Part("Crescent blade", new Vector3(-.22f, .065f, .55f), new Vector3(.21f, .045f, .1f), bone, new Vector3(0, 32, 0));
            Part("Blade tip", new Vector3(-.36f, .055f, .48f), new Vector3(.17f, .025f, .055f), bone, new Vector3(0, 65, 0));
            Part("Spectral edge", new Vector3(-.2f, .09f, .58f), new Vector3(.25f, .012f, .025f), energy, new Vector3(0, 26, 0));
            Part("Bone launcher skull", new Vector3(0, .005f, .57f), new Vector3(.15f, .16f, .17f), bone);
            Part("Skull socket left", new Vector3(-.038f, .025f, .66f), new Vector3(.034f, .045f, .012f), dark);
            Part("Skull socket right", new Vector3(.038f, .025f, .66f), new Vector3(.034f, .045f, .012f), dark);
            Part("Soul aperture", new Vector3(0, -.035f, .665f), new Vector3(.045f, .04f, .015f), energy);
        }

        void BuildArcCoil(Material dark, Material metal, Material trim, Material energy)
        {
            weaponRoot.name = "Bobina Voltaica / frascos do Alquimista";
            muzzleLocal = new Vector3(0, .04f, .66f);
            Part("Induction housing", new Vector3(0, -.005f, .18f), new Vector3(.19f, .14f, .37f), metal);
            Part("Coil core", new Vector3(0, .02f, .45f), new Vector3(.09f, .09f, .29f), dark);
            for (int winding = 0; winding < 5; winding++)
                Part("Copper winding", new Vector3(0, .02f, .33f + winding * .055f), new Vector3(.17f, .15f, .025f), trim);
            Part("Arc tip", new Vector3(0, .04f, .6f), new Vector3(.09f, .08f, .09f), energy);
            Part("Right glowing flask", new Vector3(.14f, -.045f, .16f), new Vector3(.1f, .16f, .12f), energy);
            Part("Left glowing flask", new Vector3(-.15f, -.045f, .23f), new Vector3(.1f, .17f, .12f), energy);
            Part("Right bottle stopper", new Vector3(.14f, .06f, .16f), new Vector3(.07f, .05f, .075f), dark);
            Part("Left bottle stopper", new Vector3(-.15f, .065f, .23f), new Vector3(.07f, .05f, .075f), dark);
            Part("Pressure gauge", new Vector3(.11f, .075f, .16f), new Vector3(.06f, .035f, .09f), trim, new Vector3(15, 0, 0));
            Part("Voltaic reservoir", new Vector3(0, -.055f, -.02f), new Vector3(.15f, .13f, .16f), dark);
        }

        void Part(string name, Vector3 position, Vector3 scale, Material material, Vector3? rotation = null)
        {
            GameObject part = game.Box(name, position, scale, material, weaponRoot, false);
            if (rotation.HasValue) part.transform.localRotation = Quaternion.Euler(rotation.Value);
            var renderer = part.GetComponent<Renderer>();
            if (renderer) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; }
        }

        static void SetLayer(GameObject root, int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform) SetLayer(child.gameObject, layer);
        }

        void EjectCasing()
        {
            if (casings.Count >= CasingLimit)
            {
                if (casings[0].Transform) Destroy(casings[0].Transform.gameObject);
                casings.RemoveAt(0);
            }
            Vector3 position = IsTopDown ? MuzzlePosition : weaponRoot.TransformPoint(new Vector3(.14f, .04f, .08f));
            var shell = game.Box("Spent brass", Vector3.zero, new Vector3(.025f, .045f, .025f), casingMaterial, game.transform, false);
            shell.transform.position = position;
            float variation = Mathf.Sin(clock * 31) * .25f;
            Vector3 forward = IsTopDown ? perspective.AimDirection : view.transform.forward;
            Vector3 right = IsTopDown ? Vector3.Cross(Vector3.up, forward).normalized : view.transform.right;
            casings.Add(new Casing { Transform = shell.transform, Position = position,
                Velocity = right * (1.6f + variation) + Vector3.up * 1.8f + forward * .4f,
                Spin = 270 + variation * 180 });
        }

        void TickCasings(float dt)
        {
            for (int i = casings.Count - 1; i >= 0; i--)
            {
                var shell = casings[i]; shell.Remaining -= dt;
                if (!shell.Transform || shell.Remaining <= 0)
                {
                    if (shell.Transform) Destroy(shell.Transform.gameObject);
                    casings.RemoveAt(i); continue;
                }
                if (shell.Grounded) continue;
                shell.Velocity += Vector3.down * (9 * dt);
                Vector3 delta = shell.Velocity * dt;
                if (Physics.Raycast(shell.Position, delta.normalized, out RaycastHit hit, delta.magnitude, ~(1 << ViewModelLayer), QueryTriggerInteraction.Ignore))
                {
                    shell.Position = hit.point + hit.normal * .025f;
                    shell.Velocity = Vector3.Reflect(shell.Velocity, hit.normal) * .2f;
                    if (hit.normal.y > .5f && shell.Velocity.sqrMagnitude < .4f) shell.Grounded = true;
                }
                else shell.Position += delta;
                if (shell.Position.y < .03f) { shell.Position.y = .03f; shell.Grounded = true; }
                shell.Transform.position = shell.Position;
                shell.Transform.Rotate(new Vector3(1, .5f, .3f), shell.Spin * dt, Space.Self);
            }
        }

        public void DisposeView()
        {
            if (disposed) return;
            disposed = true;
            if (perspective) perspective.Dispose();
            if (view)
            {
                view.cullingMask = originalCullingMask;
                var data = view.GetUniversalAdditionalCameraData();
                if (modelCamera) data.cameraStack.Remove(modelCamera);
            }
            if (modelCamera) Destroy(modelCamera.gameObject);
            if (worldFlashRoot) { worldFlashRoot.gameObject.SetActive(false); Destroy(worldFlashRoot.gameObject); }
            foreach (var casing in casings) if (casing.Transform) Destroy(casing.Transform.gameObject);
            casings.Clear();
        }

        void OnDestroy() => DisposeView();
    }
}
