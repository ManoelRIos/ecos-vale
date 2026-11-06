using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos.Haunt.Fps
{
    public sealed partial class HauntFpsGame
    {
        int NewMenuSeed() => unchecked(seed * 1664525 + Environment.TickCount + 1013904223);

        public static string CharacterName(CharacterKind character) => character == CharacterKind.Hunter ? "CACADOR"
            : character == CharacterKind.Medium ? "MEDIUM" : character == CharacterKind.Reaper ? "CEIFADOR" : "ALQUIMISTA";

        static Color CharacterColor(CharacterKind character) => character == CharacterKind.Hunter ? new Color(1, .76f, .31f)
            : character == CharacterKind.Medium ? new Color(.77f, .38f, 1)
            : character == CharacterKind.Reaper ? new Color(.75f, .94f, .78f) : new Color(.32f, 1, .59f);

        void SpawnBoss()
        {
            if (bossSpawned || !CanSimulate) return;
            bossSpawned = true; ClearProjectiles();
            int archetype = practiceBoss >= 0 ? practiceBoss : HauntFpsCampaign.BossForFloor(seed, state.Floor);
            var obj = new GameObject("Guardião da fase"); obj.transform.SetParent(runRoot.transform, false);
            boss = obj.AddComponent<HauntFpsBoss>();
            Vector3 position = world.BossSpawn;
            boss.Initialize(this, archetype, position, state.Floor);
            SetLayer(obj.transform, EnemyLayer); Physics.SyncTransforms();
            PlayCue(FpsCue.Explosion, position); AddShake(.35f);
            Notify(boss.DisplayName, 3);
        }

        public void OnBossKilled(HauntFpsBoss defeated)
        {
            if (!defeated || defeated != boss || exitReady) return;
            state.RewardRoom(true); kills++; exitReady = true;
            ClearProjectiles(); world.SetExitOpen(true); OpenBossRoom();
            state.Health = Mathf.Min(state.MaxHealth, state.Health + 2);
            Notify("GUARDIAO DERROTADO. E / QUADRADO NO PORTAL.", 5);
            PlayCue(FpsCue.Victory, defeated.transform.position); AddShake(.55f);
        }

        void AdvanceFloor()
        {
            if (!exitReady || state == null || state.IsDead || !state.BossDefeated) return;
            if (practiceBoss >= 0 || state.Floor >= HauntFpsCampaign.ThemeCount)
            {
                mode = ScreenMode.Victory; PlayCue(FpsCue.Victory, PlayerPosition); UpdateSuspension(); return;
            }
            bool topDown = player.IsTopDown;
            state.NextFloor();
            BuildRun(seed, true, topDown);
            mode = ScreenMode.Playing; player.SuspendInput(); UpdateSuspension();
            Notify("FASE " + state.Floor + " - " + HauntFpsCampaign.ThemeName(state.Floor), 3);
        }

        /// <summary>Speed of the player's bullets: fast, but visible in flight, as in twin-stick
        /// dungeon shooters. Each soul fires its own projectile sprite.</summary>
        public const float PlayerShotSpeed = 26;

        sealed class PlayerShot
        {
            public Transform Visual;
            public Renderer Orb;
            public SpriteRenderer Sprite;
            public LineRenderer Trail;
            public Vector3 Position, Direction;
            public float Life = 1.7f, Radius;
            public int Pierces;
            public bool Chain;
            public Color Color;
            public readonly HashSet<Component> Struck = new HashSet<Component>();
        }
        readonly List<PlayerShot> playerShots = new List<PlayerShot>();
        public int PlayerShotCount => playerShots.Count;

        void FireCharacterWeapon(Ray ray)
        {
            Color color = CharacterColor(state.Character);
            Vector3 origin = player.MuzzlePosition;
            Vector3 direction = ray.direction;
            if (!player.IsTopDown)
            {
                // In first person the bullet leaves the gun but converges on what the crosshair covers.
                int mask = ~((1 << PlayerLayer) | (1 << ViewLayer));
                Vector3 target = ray.GetPoint(FpsPrototypeRules.ShotRange);
                if (Physics.Raycast(ray, out RaycastHit aim, FpsPrototypeRules.ShotRange, mask, QueryTriggerInteraction.Ignore)) target = aim.point;
                direction = (target - origin).normalized;
            }
            int pierces = state.Character == CharacterKind.Reaper ? 3 : 1;
            bool chain = state.Character == CharacterKind.Alchemist;
            if (state.Character == CharacterKind.Medium)
                for (int i = -1; i <= 1; i++) LaunchShot(origin, Quaternion.AngleAxis(i * 6, Vector3.up) * direction, color, pierces, chain);
            else LaunchShot(origin, direction, color, pierces, chain);
        }

        string ShotSprite() => state.Character == CharacterKind.Medium ? "shot-petal" : state.Character == CharacterKind.Reaper ? "shot-bone"
            : state.Character == CharacterKind.Alchemist ? "shot-shock" : "shot-carbine";

        void LaunchShot(Vector3 origin, Vector3 direction, Color color, int pierces, bool chain)
        {
            if (direction.sqrMagnitude < .001f || playerShots.Count > 64) return;
            float size = state.ProjectileScale;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Bala da alma"; visual.transform.SetParent(runRoot.transform, false);
            visual.transform.position = origin; visual.transform.localScale = Vector3.one * .14f * size;
            var collider = visual.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            string palette = Mathf.RoundToInt(color.r * 15) + ":" + Mathf.RoundToInt(color.g * 15) + ":" + Mathf.RoundToInt(color.b * 15);
            Material material = PixelMaterial("bala da alma " + palette, color);
            material.SetColor("_EmissionColor", color * 1.6f);
            var shot = new PlayerShot { Visual = visual.transform, Orb = visual.GetComponent<Renderer>(), Position = origin,
                Direction = direction.normalized, Pierces = pierces, Chain = chain, Color = color, Radius = .12f * size };
            shot.Orb.sharedMaterial = material;
            var trail = new GameObject("Rastro").AddComponent<LineRenderer>(); trail.transform.SetParent(visual.transform, false);
            SetupLine(trail, material, .05f * size); trail.positionCount = 2;
            trail.SetPosition(0, origin); trail.SetPosition(1, origin);
            shot.Trail = trail;
            if (hybridArt && hybridArt.SpriteMaterial)
            {
                var image = new GameObject("Bala da alma 2D").AddComponent<SpriteRenderer>();
                image.transform.SetParent(visual.transform, false);
                image.sprite = hybridArt.ArtSprite(ShotSprite());
                image.sharedMaterial = hybridArt.SpriteMaterial;
                float extent = image.sprite ? Mathf.Max(image.sprite.rect.width, image.sprite.rect.height) / image.sprite.pixelsPerUnit : 1;
                image.transform.localScale = Vector3.one * (.7f / Mathf.Max(.05f, extent) / .14f);
                image.sortingOrder = 5001; image.shadowCastingMode = ShadowCastingMode.Off; image.receiveShadows = false;
                shot.Sprite = image;
            }
            ShowShotStyle(shot, bulletsOverSprites);
            playerShots.Add(shot);
        }

        void ShowShotStyle(PlayerShot shot, bool overhead)
        {
            if (shot.Sprite) shot.Sprite.enabled = overhead;
            if (shot.Orb) shot.Orb.enabled = !overhead || !shot.Sprite;
            if (shot.Trail) shot.Trail.enabled = !overhead || !shot.Sprite;
        }

        void TickPlayerShots(float dt)
        {
            int mask = ~((1 << PlayerLayer) | (1 << ViewLayer));
            Transform eye = player ? player.View.transform : null;
            for (int i = playerShots.Count - 1; i >= 0; i--)
            {
                PlayerShot shot = playerShots[i];
                float step = PlayerShotSpeed * dt;
                bool remove = false;
                RaycastHit[] hits = Physics.SphereCastAll(shot.Position, shot.Radius, shot.Direction, step, mask, QueryTriggerInteraction.Ignore);
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit hit in hits)
                {
                    var enemy = hit.collider.GetComponentInParent<HauntFpsEnemy>();
                    var chief = hit.collider.GetComponentInParent<HauntFpsBoss>();
                    var barrel = hit.collider.GetComponentInParent<HauntFpsBarrel>();
                    Vector3 point = hit.distance <= 0 ? shot.Position : hit.point;
                    if (enemy)
                    {
                        if (enemy.IsDead || !shot.Struck.Add(enemy)) continue;
                        enemy.TakeDamage(state.Damage + state.ProjectileDamageBonus, point, shot.Direction * 4);
                        hitMarker = .12f; PlayCue(FpsCue.EnemyHit, point);
                        if (shot.Chain) ChainLightning(enemy, shot.Color);
                        Burst(point, shot.Color, 5, 2);
                        if (--shot.Pierces > 0) continue;
                    }
                    else if (chief)
                    {
                        if (!shot.Struck.Add(chief)) continue;
                        chief.TakeDamage(state.Damage + state.ProjectileDamageBonus, point, shot.Direction * 2);
                        hitMarker = .12f; Burst(point, shot.Color, 6, 2.5f); PlayCue(FpsCue.EnemyHit, point);
                    }
                    else
                    {
                        if (barrel && !barrel.IsDestroyed) { barrel.TakeDamage(state.Damage, point); hitMarker = .12f; }
                        Burst(point, new Color(1, .65f, .22f), 5, 1.8f);
                    }
                    shot.Position = point; remove = true; break;
                }
                if (!remove) shot.Position += shot.Direction * step;
                shot.Life -= dt;
                if (shot.Life <= 0 || !world.IsInsideBounds(shot.Position, -.5f)) remove = true;
                if (remove)
                {
                    if (shot.Visual) Destroy(shot.Visual.gameObject);
                    playerShots.RemoveAt(i);
                    continue;
                }
                shot.Visual.position = shot.Position;
                if (shot.Trail && shot.Trail.enabled)
                {
                    shot.Trail.SetPosition(0, shot.Position);
                    shot.Trail.SetPosition(1, shot.Position - shot.Direction * .45f);
                }
                if (shot.Sprite && shot.Sprite.enabled && eye)
                {
                    // Point the sprite's long axis along the flight direction as seen on screen.
                    Vector3 right = eye.rotation * Vector3.right, up = eye.rotation * Vector3.up;
                    float angle = Mathf.Atan2(Vector3.Dot(shot.Direction, up), Vector3.Dot(shot.Direction, right)) * Mathf.Rad2Deg;
                    shot.Sprite.transform.rotation = eye.rotation * Quaternion.Euler(0, 0, angle);
                }
            }
        }

        void ChainLightning(HauntFpsEnemy first, Color color)
        {
            HauntFpsEnemy next = null; float nearest = 4.5f;
            foreach (var enemy in enemies)
            {
                if (!enemy || enemy == first || enemy.IsDead) continue;
                float distance = Vector3.Distance(first.AimPosition, enemy.AimPosition);
                if (distance >= nearest || !BlastVisible(first.AimPosition, enemy.AimPosition)) continue;
                next = enemy; nearest = distance;
            }
            if (!next) return;
            AddTrace(first.AimPosition, next.AimPosition, color);
            next.TakeDamage(1, next.AimPosition, Vector3.zero);
        }

        public bool TryPower()
        {
            if (!CanSimulate || player.IsPerspectiveTransition || !state.TryUsePower()) return false;
            Vector3 center = PlayerPosition + Vector3.up * 1.05f;
            float radius = state.Character == CharacterKind.Medium ? 9 : state.Character == CharacterKind.Reaper ? 4.8f : 6;
            if (state.Character != CharacterKind.Alchemist)
            {
                foreach (var enemy in enemies.ToArray())
                {
                    if (!enemy || enemy.IsDead || Vector3.Distance(center, enemy.AimPosition) > radius
                        || !BlastVisible(center, enemy.AimPosition)) continue;
                    int damage = state.Character == CharacterKind.Reaper ? 6 : state.Character == CharacterKind.Hunter ? 4 : 2;
                    enemy.TakeDamage(damage, enemy.AimPosition, (enemy.AimPosition - center).normalized * 7);
                }
                if (boss && !boss.IsDead && Vector3.Distance(center, boss.AimPosition) <= radius && BlastVisible(center, boss.AimPosition))
                    boss.TakeDamage(state.Character == CharacterKind.Reaper ? 8 : 5, boss.AimPosition, Vector3.zero);
            }
            // The Medium's veil removes incoming orbs; other powers retain their
            // original class roles: damage pulse, close harvest or a vital elixir.
            if (state.Character == CharacterKind.Medium) ClearProjectiles();
            Burst(center, CharacterColor(state.Character), 32, 5);
            PlayCue(FpsCue.Dodge, center); AddShake(.18f); Notify(state.PowerName, 1.5f); return true;
        }

        void UpdateCursorMode()
        {
            if (!CanSimulate) { SetCursor(false); return; }
            Cursor.lockState = player && player.IsTopDown ? CursorLockMode.Confined : CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
