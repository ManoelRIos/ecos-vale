using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos.Haunt.Fps
{
    /// <summary>Presentation for the overhead view of the shared 3D encounter.
    /// Sprites and wall cutaways never replace actors, collision or combat state.</summary>
    public sealed class HauntFpsHybridArt : MonoBehaviour, IDisposable
    {
        HauntFpsGame game;
        HauntPixelArt art;
        Transform presentation, cutaways;
        SpriteRenderer hero, weapon, muzzle, heroShadow;
        HauntFpsWorld observedWorld;
        readonly List<WallView> walls = new List<WallView>();
        readonly Dictionary<HauntFpsEnemy, ActorView> actors = new Dictionary<HauntFpsEnemy, ActorView>();
        readonly List<HauntFpsEnemy> expired = new List<HauntFpsEnemy>();
        ActorView boss;
        MaterialPropertyBlock flash;
        float clock, dodgeClock, shotRemaining, heroFlash, wallLowering;
        int lastMagazine, lastHealth;
        bool initialized, disposed, topDown, wasDodging;

        sealed class WallView
        {
            public Renderer Source, Proxy;
            public bool Enabled;
            // Full-height pose of the source, so the proxy can sink from it to the floor.
            public float FullHeight, CenterY;
        }
        sealed class ActorView
        {
            public Component Actor;
            public SpriteRenderer Sprite, Shadow;
            public Renderer[] Model;
            public bool[] Enabled;
            public int Health;
            public float StateClock, Flash;
            public ActorAnimationState EnemyState;
            public BossAnimationState BossState;
        }

        /// <summary>Enemies and bosses use the same pixel-art sprites in both views, so each
        /// creature reads identically from the eye and from above. Set false to bring back the
        /// procedural voxel models in first person.</summary>
        public static bool SpriteActorsInFirstPerson = true;
        /// <summary>World size of the actor sprites. With the 32 px-per-unit sheets one sprite
        /// pixel covers one rendered pixel of the overhead view (see HauntFpsGame.PixelUnit).</summary>
        public const float ActorScale = 1.4f;

        public bool TopDownVisible => topDown && !disposed;
        bool ActorSpritesShown => topDown || SpriteActorsInFirstPerson;
        // Per-tick viewing context used to sort and shade the sprites.
        Vector3 viewer;
        float overheadBlend;
        public int ActorViewCount => actors.Count + (boss != null ? 1 : 0);
        public int CutawayCount => walls.Count;

        public void Initialize(HauntFpsGame owner)
        {
            if (initialized) throw new InvalidOperationException("A apresentação híbrida já foi criada.");
            if (disposed) throw new ObjectDisposedException(nameof(HauntFpsHybridArt));
            if (!owner) throw new ArgumentNullException(nameof(owner));
            game = owner; art = new HauntPixelArt(); flash = new MaterialPropertyBlock();
            presentation = new GameObject("Visão 2D / sprites compartilhados").transform;
            presentation.SetParent(transform, false);
            heroShadow = Sprite("Sombra da alma", art.Get("shadow"), presentation);
            heroShadow.transform.rotation = Quaternion.Euler(90, 0, 0);
            heroShadow.transform.localScale = Vector3.one * .65f;
            heroShadow.sortingOrder = -6000;
            hero = Sprite("Alma / folha original", art.CharacterFrame(CharacterArt(), CharacterClip.Idle, 0,
                CharacterFacing.Front), presentation);
            weapon = Sprite("Arma / visão 2D", art.Get(WeaponArt()), presentation);
            muzzle = Sprite("Clarão / visão 2D", art.Get("muzzle"), presentation);
            muzzle.transform.localScale = Vector3.one * .45f;
            lastMagazine = game.State.Magazine; lastHealth = game.State.Health;
            CaptureArchitecture();
            initialized = true;
            SetTopDown(false);
        }

        public void SetTopDown(bool value)
        {
            if (disposed || !presentation) return;
            topDown = value;
            presentation.gameObject.SetActive(ActorSpritesShown);
            // The hero is seen through its own eyes in first person; only actors stay sprites.
            if (hero) hero.enabled = value;
            if (heroShadow) heroShadow.enabled = value;
            if (!value) { if (weapon) weapon.enabled = false; if (muzzle) muzzle.enabled = false; }
            // Overhead, every front occluder is lowered, so sprites draw over the standing back
            // walls like Enter the Gungeon's layered rooms. In first person walls hide them normally.
            if (art != null) art.Material.SetFloat("_ZTest", (float)(value ? CompareFunction.Always : CompareFunction.LessEqual));
            foreach (var actor in actors.Values) SetActorVisible(actor, ActorSpritesShown);
            if (boss != null) SetActorVisible(boss, ActorSpritesShown);
        }

        /// <summary>0 = full 3D walls, 1 = low 2D cutaway. Values in between show the
        /// walls sinking into (or rising from) the floor while the camera moves.
        /// Colliders are never touched: only the source renderers and their proxies change.</summary>
        public void SetWallLowering(float value)
        {
            if (disposed) return;
            wallLowering = Mathf.Clamp01(value);
            bool lowered = wallLowering > 0;
            if (cutaways) cutaways.gameObject.SetActive(lowered);
            float floor = FloorHeight + CutawayHeight * .5f;
            foreach (var wall in walls)
            {
                if (wall.Source) wall.Source.enabled = lowered ? false : wall.Enabled;
                if (!wall.Proxy) continue;
                // A closed portal's source GameObject becomes inactive when opened;
                // its cutaway follows that state without touching the seal collider.
                wall.Proxy.enabled = lowered && wall.Enabled && wall.Source && wall.Source.gameObject.activeInHierarchy;
                if (!wall.Proxy.enabled) continue;
                Transform node = wall.Proxy.transform;
                Vector3 position = node.position, scale = node.localScale;
                scale.y = Mathf.Lerp(wall.FullHeight, CutawayHeight, wallLowering);
                position.y = Mathf.Lerp(wall.CenterY, floor, wallLowering);
                node.localScale = scale; node.position = position;
                // Shadows fade out with the wall instead of vanishing at the start.
                wall.Proxy.shadowCastingMode = wallLowering < .999f ? ShadowCastingMode.On : ShadowCastingMode.Off;
            }
        }

        /// <param name="overhead">Show sprites instead of 3D models.</param>
        /// <param name="lowering">Wall height blend (0 = full walls, 1 = cutaway);
        /// a negative value follows <paramref name="overhead"/>.</param>
        public void Tick(float dt, bool overhead, Camera camera, Vector3 aimDirection, bool moving, bool dodging, float lowering = -1)
        {
            if (!initialized || disposed || !game || !camera || float.IsNaN(dt) || float.IsInfinity(dt) || dt < 0) return;
            if (observedWorld != game.World) CaptureArchitecture();
            if (topDown != overhead) SetTopDown(overhead);
            SetWallLowering(lowering < 0 || float.IsNaN(lowering) ? overhead ? 1 : 0 : lowering);
            float step = game.CanSimulate ? Mathf.Min(dt, .05f) : 0;
            clock += step;
            shotRemaining = Mathf.Max(0, shotRemaining - step);
            heroFlash = Mathf.Max(0, heroFlash - step);
            if (game.State.Magazine < lastMagazine) shotRemaining = .13f;
            if (game.State.Health < lastHealth) heroFlash = .065f;
            lastMagazine = game.State.Magazine; lastHealth = game.State.Health;
            if (dodging) dodgeClock = wasDodging ? dodgeClock + step : 0;
            else dodgeClock = 0;
            wasDodging = dodging;
            SynchronizeActors();
            if (!ActorSpritesShown) return;
            Transform eye = camera.transform;
            viewer = eye.position;
            overheadBlend = Mathf.Clamp01(lowering < 0 || float.IsNaN(lowering) ? overhead ? 1 : 0 : lowering);
            Vector3 flat = eye.forward; flat.y = 0;
            Quaternion upright = flat.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(flat.normalized, Vector3.up)
                : Quaternion.Euler(0, eye.eulerAngles.y, 0);
            // Upright billboards in first person (Doom style), tilting with the camera into the
            // overhead plane, so the sprites follow the dolly path without a pop.
            Quaternion facingCamera = Quaternion.Slerp(upright, eye.rotation, overheadBlend);
            Quaternion toView = Quaternion.Inverse(upright);
            if (topDown) PresentHero(facingCamera, aimDirection, moving, dodging);
            foreach (var pair in actors)
            {
                var enemy = pair.Key;
                if (!enemy) continue;
                ActorView view = pair.Value;
                Observe(view, enemy.Health, step);
                if (view.EnemyState != enemy.AnimationState) { view.EnemyState = enemy.AnimationState; view.StateClock = 0; }
                int frame = Mathf.FloorToInt(view.StateClock * (enemy.IsDead ? 12 : 8));
                if (enemy.IsDead) frame = Mathf.Min(HauntPixelArt.ActorFrameCount - 1, frame);
                // Facing is judged from the viewer, so a creature walking away shows its back
                // in first person exactly as it does when walking north in the overhead view.
                Vector3 seen = toView * enemy.transform.forward;
                view.Sprite.sprite = art.ActorFrame(enemy.SpriteKey, enemy.AnimationState, frame, ActorDirection(seen));
                Vector3 anchor = enemy.transform.position + Vector3.up * .055f;
                bool footPivot = HauntPixelArt.HasActorSheet(enemy.SpriteKey);
                PresentActor(view, anchor, facingCamera, ActorScale, footPivot, seen.x < 0);
            }
            if (boss != null && game.Boss)
            {
                var guardian = game.Boss;
                Observe(boss, guardian.Health, step);
                if (boss.BossState != guardian.AnimationState) { boss.BossState = guardian.AnimationState; boss.StateClock = 0; }
                int frame = Mathf.FloorToInt(boss.StateClock * (guardian.IsDead ? 9 : 6));
                if (guardian.IsDead) frame = Mathf.Min(3, frame);
                boss.Sprite.sprite = art.BossFrame(guardian.SpriteKey, guardian.AnimationState, frame);
                PresentActor(boss, guardian.transform.position + Vector3.up * .07f, facingCamera, ActorScale, false, false);
            }
        }

        void PresentHero(Quaternion facingCamera, Vector3 aim, bool moving, bool dodging)
        {
            string id = CharacterArt();
            CharacterFacing direction = CharacterDirection(aim);
            CharacterClip clip = game.State.IsDead ? CharacterClip.Death : dodging ? CharacterClip.Dodge
                : game.State.IsReloading ? CharacterClip.Reload : shotRemaining > 0 ? CharacterClip.Attack
                : moving ? CharacterClip.Walk : CharacterClip.Idle;
            int count = art.CharacterFrameCount(id, clip, direction);
            int frame = dodging ? Mathf.Clamp(Mathf.FloorToInt(dodgeClock / HauntFpsPlayer.DodgeDuration * count), 0, count - 1)
                : game.State.IsReloading ? Mathf.Clamp(Mathf.FloorToInt(game.State.ReloadProgress * count), 0, count - 1)
                : Mathf.FloorToInt(clock * art.CharacterFrameRate(id, clip, direction));
            hero.sprite = art.CharacterFrame(id, clip, frame, direction);
            hero.transform.position = Settled(game.PlayerPosition + Vector3.up * .06f);
            hero.transform.rotation = facingCamera;
            // 16 px-per-unit sheet: one hero pixel spans exactly two actor pixels.
            hero.transform.localScale = Vector3.one * ActorScale;
            hero.flipX = aim.x < -.05f;
            hero.sortingOrder = Depth(game.PlayerPosition);
            Flash(hero, heroFlash);
            heroShadow.transform.position = new Vector3(game.PlayerPosition.x, FloorHeight + .035f, game.PlayerPosition.z);
            heroShadow.color = new Color(1, 1, 1, dodging ? .65f : .9f);
            Vector3 right = facingCamera * Vector3.right, up = facingCamera * Vector3.up;
            float angle = Mathf.Atan2(Vector3.Dot(aim, up), Vector3.Dot(aim, right)) * Mathf.Rad2Deg;
            weapon.sprite = art.Get(WeaponArt());
            weapon.transform.position = game.PlayerPosition + Vector3.up * 1.05f + aim * .24f;
            weapon.transform.rotation = facingCamera * Quaternion.Euler(0, 0, angle);
            weapon.transform.localScale = Vector3.one * .62f;
            weapon.flipY = aim.x < 0;
            weapon.sortingOrder = hero.sortingOrder + 2;
            weapon.enabled = !dodging && !game.State.IsDead;
            if (game.State.IsReloading)
                weapon.transform.rotation *= Quaternion.Euler(0, 0, Mathf.Sin(game.State.ReloadProgress * Mathf.PI) * -40);
            muzzle.transform.position = game.PlayerPosition + Vector3.up * 1.05f + aim * .71f;
            muzzle.transform.rotation = facingCamera * Quaternion.Euler(0, 0, angle);
            muzzle.sortingOrder = hero.sortingOrder + 3;
            muzzle.enabled = shotRemaining > .085f && !game.State.IsReloading && !dodging;
        }

        void SynchronizeActors()
        {
            expired.Clear();
            foreach (var pair in actors) if (!pair.Key) expired.Add(pair.Key);
            foreach (var enemy in expired) { Release(actors[enemy]); actors.Remove(enemy); }
            if (ActorSpritesShown)
            {
                foreach (var enemy in game.Enemies)
                {
                    if (!enemy || actors.ContainsKey(enemy)) continue;
                    var view = CreateActor(enemy, enemy.Health, "Inimigo / " + enemy.SpriteKey);
                    view.EnemyState = enemy.AnimationState; actors.Add(enemy, view); SetActorVisible(view, true);
                }
            }
            if (boss != null && (!boss.Actor || boss.Actor != game.Boss)) { Release(boss); boss = null; }
            if (ActorSpritesShown && game.Boss && boss == null)
            {
                boss = CreateActor(game.Boss, game.Boss.Health, "Chefe / " + game.Boss.SpriteKey);
                boss.BossState = game.Boss.AnimationState; SetActorVisible(boss, true);
            }
        }

        ActorView CreateActor(Component actor, int health, string name)
        {
            Transform model = actor.transform.Find("Visual");
            Renderer[] modelRenderers = model ? model.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            var view = new ActorView { Actor = actor, Health = health, Model = modelRenderers, Enabled = new bool[modelRenderers.Length] };
            for (int i = 0; i < modelRenderers.Length; i++) view.Enabled[i] = modelRenderers[i].enabled;
            view.Sprite = Sprite(name, null, presentation);
            view.Shadow = Sprite(name + " / sombra", art.Get("shadow"), presentation);
            view.Shadow.transform.rotation = Quaternion.Euler(90, 0, 0);
            view.Shadow.transform.localScale = Vector3.one * (actor is HauntFpsBoss ? 1.15f : .62f);
            view.Shadow.sortingOrder = -6000;
            return view;
        }

        static void SetActorVisible(ActorView actor, bool overhead)
        {
            if (actor.Sprite) actor.Sprite.enabled = overhead;
            if (actor.Shadow) actor.Shadow.enabled = overhead;
            for (int i = 0; i < actor.Model.Length; i++) if (actor.Model[i]) actor.Model[i].enabled = overhead ? false : actor.Enabled[i];
        }

        void PresentActor(ActorView view, Vector3 anchor, Quaternion facingCamera, float scale, bool footPivot, bool flip)
        {
            Vector3 offset = footPivot || !view.Sprite.sprite ? Vector3.zero
                : facingCamera * Vector3.up * (view.Sprite.sprite.rect.height / view.Sprite.sprite.pixelsPerUnit * scale * .46f);
            view.Sprite.transform.position = Settled(anchor + offset);
            view.Sprite.transform.rotation = facingCamera;
            float stretch = view.Flash > 0 ? .1f : 0;
            view.Sprite.transform.localScale = new Vector3(scale * (1 + stretch), scale * (1 - stretch), scale);
            view.Sprite.flipX = flip;
            view.Sprite.sortingOrder = topDown ? Depth(anchor) : 1000 - Mathf.RoundToInt(Vector3.Distance(viewer, anchor) * 30);
            view.Sprite.color = Atmosphere(anchor);
            Flash(view.Sprite, view.Flash);
            view.Shadow.transform.position = new Vector3(anchor.x, FloorHeight + .03f, anchor.z);
        }

        /// <summary>Sprites are unlit. In first person they take the arena's fog and a gentle
        /// distance falloff so they sit inside the room instead of glowing over it.</summary>
        Color Atmosphere(Vector3 position)
        {
            if (overheadBlend >= 1) return Color.white;
            float distance = Vector3.Distance(viewer, position);
            float density = RenderSettings.fog ? RenderSettings.fogDensity * distance : 0;
            float visible = Mathf.Exp(-density * density);
            Color lit = Color.Lerp(new Color(.7f, .68f, .78f, 1), Color.white, Mathf.Clamp01(1.3f - distance / 16));
            Color tinted = Color.Lerp(RenderSettings.fogColor, lit, visible); tinted.a = 1;
            return Color.Lerp(tinted, Color.white, overheadBlend);
        }

        /// <summary>Once fully overhead, sprites sit on whole pixels like the camera does.</summary>
        Vector3 Settled(Vector3 position) => overheadBlend >= 1 ? HauntFpsPerspective.SnapToPixels(position) : position;

        static void Observe(ActorView view, int health, float dt)
        {
            view.StateClock += dt; view.Flash = Mathf.Max(0, view.Flash - dt);
            if (health < view.Health) view.Flash = .065f;
            view.Health = health;
        }
        void Flash(SpriteRenderer sprite, float remaining)
        {
            flash.Clear(); flash.SetFloat("_HitFlash", remaining > 0 ? 1 : 0); sprite.SetPropertyBlock(flash);
        }

        void CaptureArchitecture()
        {
            foreach (var wall in walls) if (wall.Source) wall.Source.enabled = wall.Enabled;
            walls.Clear();
            if (cutaways) { cutaways.gameObject.SetActive(false); DestroyOwned(cutaways.gameObject); }
            observedWorld = game.World;
            cutaways = new GameObject("Visão 2D / corte das paredes").transform; cutaways.SetParent(transform, false);
            if (!observedWorld) return;
            foreach (var renderer in observedWorld.GetComponentsInChildren<Renderer>(true))
            {
                string name = renderer.gameObject.name;
                bool wall = name.StartsWith("Parede ", StringComparison.Ordinal);
                bool pillar = name == "Pilar / corpo";
                bool portal = name.StartsWith("Portal / pilar", StringComparison.Ordinal) || name == "Selo fechado";
                bool hide = wall || pillar || portal || name.StartsWith("Coroamento ", StringComparison.Ordinal)
                    || name == "Pilar / capitel" || name == "Pilar / osso" || name == "Portal / verga";
                if (!hide) continue;
                // Enter the Gungeon framing: the back and side walls (and what hangs on them,
                // like the portal) stay standing overhead. Only pieces in front of the action sink.
                Bounds placed = renderer.bounds;
                if (placed.center.z > HauntFpsWorld.Depth * .5f - .9f || Mathf.Abs(placed.center.x) > HauntFpsWorld.Width * .5f - .9f)
                    continue;
                var record = new WallView { Source = renderer, Enabled = renderer.enabled };
                if (wall || pillar || portal)
                {
                    Bounds bounds = renderer.bounds;
                    record.FullHeight = Mathf.Max(CutawayHeight, bounds.size.y); record.CenterY = bounds.center.y;
                    var proxy = game.Box("Corte / " + name, Vector3.zero,
                        new Vector3(Mathf.Max(.12f, bounds.size.x), CutawayHeight, Mathf.Max(.12f, bounds.size.z)), renderer.sharedMaterial, cutaways, false);
                    proxy.transform.position = new Vector3(bounds.center.x, FloorHeight + CutawayHeight * .5f, bounds.center.z);
                    record.Proxy = proxy.GetComponent<Renderer>(); record.Proxy.shadowCastingMode = ShadowCastingMode.Off;
                }
                walls.Add(record);
            }
            SetTopDown(topDown);
            SetWallLowering(wallLowering);
        }

        SpriteRenderer Sprite(string name, Sprite image, Transform parent)
        {
            var node = new GameObject(name); node.transform.SetParent(parent, false);
            var renderer = node.AddComponent<SpriteRenderer>(); renderer.sharedMaterial = art.Material;
            renderer.sprite = image; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return renderer;
        }
        const float CutawayHeight = .5f;
        float FloorHeight => game && game.World ? game.World.transform.position.y : 0;
        static int Depth(Vector3 position) => 1000 - Mathf.RoundToInt(position.z * 30);
        string CharacterArt() => game.State.Character == CharacterKind.Medium ? "medium"
            : game.State.Character == CharacterKind.Reaper ? "reaper" : game.State.Character == CharacterKind.Alchemist ? "alchemist" : "player";
        static CharacterFacing CharacterDirection(Vector3 aim)
        {
            float x = Mathf.Abs(aim.x), z = Mathf.Abs(aim.z);
            if (z > x * 1.8f) return aim.z > 0 ? CharacterFacing.Back : CharacterFacing.Front;
            if (aim.z > x * .25f) return CharacterFacing.BackRight;
            if (aim.z < -x * .25f) return CharacterFacing.FrontRight;
            return CharacterFacing.Right;
        }
        static ActorFacing ActorDirection(Vector3 aim) => Mathf.Abs(aim.z) > Mathf.Abs(aim.x) * 1.3f
            ? aim.z > 0 ? ActorFacing.Up : ActorFacing.Down : ActorFacing.Right;
        string WeaponArt()
        {
            switch (game.State.Weapon)
            {
                case WeaponKind.HunterCarbine: return "hunter-carbine";
                case WeaponKind.VeilWand: return "veil-wand";
                case WeaponKind.BoneCaster: return "bone-caster";
                case WeaponKind.ArcCoil: return "arc-coil";
                case WeaponKind.Scattergun: return "scattergun";
                case WeaponKind.Repeater: return "repeater";
                case WeaponKind.SoulCannon: return "cannon";
                case WeaponKind.Stapler: return "stapler";
                case WeaponKind.Banana: return "banana-gun";
                case WeaponKind.SpinningTop: return "spinning-top";
                case WeaponKind.MirrorDisc: return "mirror-disc";
                case WeaponKind.RayLantern: return "ray-lantern";
                case WeaponKind.BoneFlute: return "bone-flute";
                case WeaponKind.Umbrella: return "umbrella-gun";
                case WeaponKind.HiveLauncher: return "hive-launcher";
                case WeaponKind.BoxingGlove: return "boxing-glove";
                default: return "weapon";
            }
        }
        static void Release(ActorView view)
        {
            SetActorVisible(view, false);
            if (view.Sprite) DestroyOwned(view.Sprite.gameObject);
            if (view.Shadow) DestroyOwned(view.Shadow.gameObject);
        }
        public void Dispose()
        {
            if (disposed) return;
            SetTopDown(false); SetWallLowering(0); disposed = true;
            foreach (var actor in actors.Values) Release(actor); actors.Clear();
            if (boss != null) Release(boss); boss = null;
            if (presentation) { presentation.gameObject.SetActive(false); DestroyOwned(presentation.gameObject); }
            if (cutaways) { cutaways.gameObject.SetActive(false); DestroyOwned(cutaways.gameObject); }
            walls.Clear();
            if (art != null) art.Dispose(); art = null;
        }
        static void DestroyOwned(UnityEngine.Object value)
        {
            if (!value) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
        void OnDestroy() { Dispose(); }
    }
}
