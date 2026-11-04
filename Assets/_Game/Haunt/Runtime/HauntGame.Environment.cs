using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        // Props use their own seeded stream, so decoration cannot change the waves.
        readonly List<RoomProp> roomProps = new List<RoomProp>();
        readonly List<WetPatch> wetPatches = new List<WetPatch>();
        readonly List<DamageZone> damageZones = new List<DamageZone>();
        readonly HashSet<long> brokenRoomProps = new HashSet<long>();
        sealed class RoomProp
        {
            public SpriteRenderer Sprite, Warning;
            public Vector2 Position;
            public Rect Bounds;
            public int Health;
            public bool Explosive, Acid, Pillar, Detonated;
            public float ParticleClock;
            public float Fuse, Flash;
            public long Key;
        }
        sealed class DamageZone
        {
            public SpriteRenderer Sprite;
            public Vector2 Position;
            public float Radius, Remaining, Tick = .75f;
            public bool Acid;
        }
        sealed class WetPatch
        {
            public SpriteRenderer Sprite;
            public Vector2 Position;
            public float Remaining, Phase, RadiusX = 1.65f, RadiusY = .95f;
            public Color Tint = new Color(.55f, .9f, .72f, .8f);
            public bool CombatZone;
        }

        void ResetRoomProps() { brokenRoomProps.Clear(); ClearRoomProps(); }
        void ClearRoomProps() { roomProps.Clear(); wetPatches.Clear(); damageZones.Clear(); }

        void ClearCombatSlowZones()
        {
            foreach (var zone in damageZones) if (!zone.Acid) { zone.Remaining = 0; if (zone.Sprite) zone.Sprite.enabled = false; }
            for (int i = wetPatches.Count - 1; i >= 0; i--)
                if (wetPatches[i].CombatZone)
                {
                    if (wetPatches[i].Sprite) Destroy(wetPatches[i].Sprite.gameObject);
                    wetPatches.RemoveAt(i);
                }
        }

        void InitializeRoomProps()
        {
            ClearRoomProps();
            if (State == null || InShop || CurrentRoom == null || CurrentRoom.Kind == RoomKind.Entrance) return;
            var random = new System.Random(unchecked(State.Seed ^ State.Floor * 27773 ^ CurrentRoom.Id * 86467));
            var candidates = new List<Vector2>();
            for (int y = -6; y <= 6; y++) for (int x = -12; x <= 12; x++)
            {
                if (Mathf.Abs(x) < 2.5f || Mathf.Abs(y) < 2 || !RoomContains(new Vector2(x, y), .85f, false)) continue;
                candidates.Add(new Vector2(x, y));
            }
            var brokenReservations = new List<Rect>();
            int count = CurrentRoom.Kind == RoomKind.Boss ? 4 : 8;
            for (int i = 0; i < count && candidates.Count > 0;)
            {
                int chosen = random.Next(candidates.Count);
                Vector2 position = candidates[chosen]; candidates.RemoveAt(chosen);
                if (Blocked(position, 1.05f) || PitTouches(position, .85f)) continue;
                int slot = i++;
                long key = ((long)State.Floor << 32) | (uint)(CurrentRoom.Id * 16 + slot);
                bool explosive = slot % 4 == 0, acid = slot % 4 == 1, pillar = slot % 4 == 2;
                var bounds = new Rect(position.x - (pillar ? .55f : .42f), position.y - .35f, pillar ? 1.1f : .84f, .7f);
                // Reserve even destroyed footprints until all slots have been chosen.
                // The candidate sequence and surviving objects remain stable on revisits.
                obstacles.Add(bounds);
                if (brokenRoomProps.Contains(key))
                {
                    brokenReservations.Add(bounds);
                    MakeSprite("shattered-prop", position, roomRoot.transform, -130).color = new Color(1,1,1,.7f);
                    continue;
                }
                string id = explosive ? "barrel-explosive" : acid ? "barrel-acid" : pillar ? "destructible-pillar" : "water-urn";
                var prop = new RoomProp { Position = position, Key = key, Bounds = bounds,
                    Explosive = explosive, Acid = acid, Pillar = pillar, Health = pillar ? 7 : explosive ? 3 : 2,
                    Sprite = MakeSprite(id, position + Vector2.up * (pillar ? .65f : .55f), roomRoot.transform, Depth(position.y)) };
                var propShadow = MakeSprite("shadow", position + new Vector2(.2f, -.2f), roomRoot.transform, -70);
                propShadow.transform.localScale = new Vector3(pillar ? 2 : 1.6f, .8f, 1);
                propShadow.color = new Color(1,1,1,.72f);
                roomProps.Add(prop);
            }
            foreach (Rect reservation in brokenReservations) obstacles.Remove(reservation);
        }

        // Read-only contact selection lets a closer wall or enemy win before damage.
        bool FindRoomPropContact(Vector2 from, Vector2 to, float radius, out float fraction, out int index)
        {
            fraction = float.PositiveInfinity; index = -1;
            for (int i = 0; i < roomProps.Count; i++)
            {
                var prop = roomProps[i];
                if (prop.Health <= 0) continue;
                Rect b = prop.Bounds;
                if (CombatGeometry.BoxHitFraction(from.x, from.y, to.x, to.y,
                    b.xMin - radius, b.yMin - radius, b.xMax + radius, b.yMax + radius,
                    out float contact) && contact < fraction)
                { fraction = contact; index = i; }
            }
            return index >= 0;
        }

        void DamageRoomProp(int index, int damage)
        {
            if (index < 0 || index >= roomProps.Count || damage <= 0) return;
            var prop = roomProps[index];
            if (prop.Health <= 0) return;
            prop.Health = Mathf.Max(0, prop.Health - damage);
            prop.Flash = .12f;
            Emit("impact-dust", prop.Position + Vector2.up * .4f, Vector2.up * .4f,
                prop.Explosive ? new Color(1, .67f, .3f) : new Color(.45f, .8f, 1), .2f, .4f, .2f);
            if (prop.Health > 0) return;
            brokenRoomProps.Add(prop.Key);
            obstacles.Remove(prop.Bounds);
            if (prop.Explosive)
            {
                // A readable fuse also gives the player time to roll out of a chain.
                prop.Fuse = .55f;
                prop.Warning = MakeSprite("enemy-ring", prop.Position, roomRoot.transform, 35);
                prop.Warning.transform.localScale = new Vector3(2.4f, 4.8f, 1);
                prop.Warning.color = new Color(1, .65f, .2f, .8f);
            }
            else
            {
                prop.Detonated = true;
                BreakPropArt(prop);
                if (prop.Acid) AddDamageZone(prop.Position, 1.85f, 8, true);
                else if (!prop.Pillar) AddWetPatch(prop.Position);
                PlaySound(prop.Pillar ? SoundCue.Impact : SoundCue.Splash, .7f);
                if (prop.Pillar) Burst(prop.Position + Vector2.up * .6f, new Color(.65f,.65f,.7f), 8);
            }
        }

        void BreakPropArt(RoomProp prop)
        {
            prop.Sprite.sprite = art.Get("shattered-prop");
            prop.Sprite.transform.position = Snap(prop.Position);
            prop.Sprite.sortingOrder = -130;
            prop.Sprite.color = new Color(1, 1, 1, .8f);
            if (prop.Warning) { Destroy(prop.Warning.gameObject); prop.Warning = null; }
        }

        void ExplodeRoomProp(RoomProp prop)
        {
            prop.Detonated = true;
            BreakPropArt(prop);
            var orange = new Color(1, .57f, .22f);
            RingEffect(prop.Position, orange, 2.4f);
            Burst(prop.Position + Vector2.up * .4f, orange, 18);
            PlaySound(SoundCue.Explosion, .85f);
            AddCameraTrauma(.7f);
            // Snapshot the current wave. Killing its last enemy may start a new wave.
            Enemy[] victims = enemies.ToArray();
            foreach (var enemy in victims)
            {
                if (enemy.SpawnRemaining > 0 || Vector2.Distance(prop.Position, enemy.Position) > 2.4f + EnemyHitRadius(enemy)
                    || CoverBetween(prop.Position, enemy.Position)) continue;
                int index = enemies.IndexOf(enemy);
                if (index >= 0 && IsEnemyDamageable(enemy))
                {
                    Vector2 away = (enemy.Position - prop.Position).normalized;
                    enemy.Position = MoveEnemy(enemy, away * (enemy.Kind == 4 ? .18f : .8f), EnemyHitRadius(enemy));
                    DamageEnemyWithFeedback(index, 7 + Mathf.Min(8, State.Floor),
                    (prop.Position - enemy.Position).normalized, orange, true, false);
                }
            }
            if (Vector2.Distance(prop.Position, playerPosition) < 2.4f + PlayerRadius
                && !CoverBetween(prop.Position, playerPosition))
            {
                int health = State.Health;
                HurtPlayer();
                if (State.Health < health)
                {
                    Vector2 pushed = Move(playerPosition, (playerPosition - prop.Position).normalized * .65f, PlayerRadius);
                    if (!PitTouches(pushed, PlayerRadius)) playerPosition = pushed;
                }
            }
            // No list removals: projectiles and the prop update can safely keep iterating.
            for (int i = 0; i < roomProps.Count; i++)
                if (roomProps[i].Health > 0 && Vector2.Distance(roomProps[i].Position, prop.Position) < 2.8f
                    && PropChainVisible(prop.Position, roomProps[i]))
                    DamageRoomProp(i, 10);
        }

        bool PropChainVisible(Vector2 from, RoomProp target)
        {
            foreach (Rect block in obstacles)
            {
                // The destination's own collider is the intended target.
                if (block.Equals(target.Bounds)) continue;
                if (CombatGeometry.SegmentHitsBox(from.x, from.y, target.Position.x, target.Position.y,
                    block.xMin, block.yMin, block.xMax, block.yMax)) return false;
            }
            return true;
        }

        void AddWetPatch(Vector2 position)
        {
            CreateSlowZone(position, 1.65f, .95f, 14, new Color(.45f, .92f, .68f, .8f));
            RingEffect(position, new Color(.45f, .92f, .68f), 1.5f);
        }

        // Enemy webs and water share bounded ground effects, with independent
        // radii and lifetime. Their lights remain below the readable projectiles.
        void AddCombatSlowZone(Vector2 position, float radius, float duration, Color color)
        {
            if (!Playing || InShop || State == null || State.IsDead || CurrentRoom == null || CurrentRoom.Cleared
                || float.IsNaN(radius) || float.IsInfinity(radius) || float.IsNaN(duration) || float.IsInfinity(duration)) return;
            radius = Mathf.Clamp(radius, .4f, 2.8f);
            duration = Mathf.Clamp(duration, .2f, 18);
            position.x = Mathf.Clamp(position.x, -HalfWidth + radius, HalfWidth - radius);
            position.y = Mathf.Clamp(position.y, -HalfHeight + radius, HalfHeight - radius);
            color.a = .75f;
            CreateSlowZone(position, radius, radius * .65f, duration, color, true);
        }

        void CreateSlowZone(Vector2 position, float radiusX, float radiusY, float duration, Color color, bool combatZone = false)
        {
            if (wetPatches.Count >= 10 || !roomRoot) return;
            var sprite = MakeSprite("wet-puddle", position, roomRoot.transform, -145);
            sprite.transform.localScale = new Vector3(1.2f * radiusX / 1.65f, 1.25f * radiusY / .95f, 1);
            sprite.color = color;
            wetPatches.Add(new WetPatch { Sprite = sprite, Position = position, Remaining = duration,
                Phase = simulationTime, RadiusX = radiusX, RadiusY = radiusY, Tint = color, CombatZone = combatZone });
            AttachFlickerLight(sprite.transform, color, radiusX * 1.7f, .16f, .1f, -110);
        }

        float RoomMovementMultiplier(Vector2 position)
        {
            foreach (var patch in wetPatches)
            {
                Vector2 offset = position - patch.Position;
                if (patch.Remaining > 0 && offset.x * offset.x / (patch.RadiusX * patch.RadiusX)
                    + offset.y * offset.y / (patch.RadiusY * patch.RadiusY) < 1) return .6f;
            }
            return 1;
        }

        void AddEmberZone(Vector2 position, float radius, float duration) => AddDamageZone(position, radius, duration, false);

        void AddDamageZone(Vector2 position, float radius, float duration, bool acid)
        {
            if (!Playing || InShop || State == null || State.IsDead || !roomRoot || !RoomContains(position, .1f, false)) return;
            if (damageZones.Count >= 16)
            {
                if (!acid) return;
                int oldestEmber = damageZones.FindIndex(zone => !zone.Acid);
                if (oldestEmber < 0) return;
                Destroy(damageZones[oldestEmber].Sprite.gameObject); damageZones.RemoveAt(oldestEmber);
            }
            radius = Mathf.Clamp(radius, .25f, 2.8f); duration = Mathf.Clamp(duration, .5f, 12);
            var sprite = MakeSprite("wet-puddle", position, roomRoot.transform, -135);
            sprite.transform.localScale = new Vector3(radius * .75f, radius * .99f, 1);
            var color = acid ? new Color(.35f,1,.12f,.9f) : new Color(1,.38f,.08f,.8f);
            sprite.color = color;
            damageZones.Add(new DamageZone { Position = position, Radius = radius, Remaining = duration, Acid = acid, Sprite = sprite });
            AttachFlickerLight(sprite.transform, color, radius * 2.5f, acid ? .25f : .15f, .15f, -105);
        }

        bool InDamageZone(Vector2 position, DamageZone zone, float actorRadius = 0)
        {
            Vector2 offset = position - zone.Position;
            float rx = zone.Radius + actorRadius, ry = zone.Radius * .65f + actorRadius;
            return offset.x * offset.x / (rx * rx) + offset.y * offset.y / (ry * ry) < 1
                && !CoverBetween(zone.Position, position);
        }

        void UpdateDamageZones(float dt)
        {
            for (int i = damageZones.Count - 1; i >= 0; i--)
            {
                var zone = damageZones[i]; zone.Remaining -= dt; zone.Tick -= dt;
                Color color = zone.Acid ? new Color(.35f,1,.12f) : new Color(1,.38f,.08f);
                zone.Sprite.color = new Color(color.r,color.g,color.b,Mathf.Clamp01(zone.Remaining)*.85f);
                if (zone.Remaining <= 0) { Destroy(zone.Sprite.gameObject); damageZones.RemoveAt(i); continue; }
                if (zone.Tick > 0) continue;
                zone.Tick = .75f;
                if (rollRemaining <= 0 && InDamageZone(playerPosition, zone, PlayerRadius)) HurtPlayer();
                if (!zone.Acid) continue;
                Enemy[] victims = enemies.ToArray();
                foreach (var enemy in victims)
                {
                    if (!IsEnemyDamageable(enemy) || !InDamageZone(enemy.Position, zone, EnemyHitRadius(enemy))) continue;
                    int index = enemies.IndexOf(enemy);
                    if (index >= 0) DamageEnemyWithFeedback(index, 1, Vector2.zero, color, false, false);
                }
            }
        }

        void UpdateRoomProps(float dt)
        {
            UpdateDamageZones(dt);
            foreach (var prop in roomProps)
            {
                prop.Flash = Mathf.Max(0, prop.Flash - dt);
                if (prop.Health > 0)
                {
                    prop.Sprite.color = prop.Flash > 0 ? new Color(1, .65f, .5f) : DungeonAmbientTint;
                    int frame = prop.Pillar ? Mathf.Clamp((7 - prop.Health) / 2, 0, 3) : Mathf.FloorToInt(simulationTime * 7);
                    if (prop.Explosive || prop.Acid || prop.Pillar)
                        prop.Sprite.sprite = art.Frame(prop.Explosive ? "barrel-explosive" : prop.Acid ? "barrel-acid" : "destructible-pillar", frame);
                    prop.ParticleClock -= dt;
                    if (prop.ParticleClock <= 0 && (prop.Explosive || prop.Acid))
                    {
                        prop.ParticleClock = prop.Explosive ? .18f : .35f;
                        Emit(prop.Explosive ? "spark" : "dust", prop.Position + Vector2.up * (prop.Explosive ? 1.5f : 1.1f),
                            prop.Explosive ? new Vector2(.12f, .55f) : Vector2.down * .15f,
                            prop.Explosive ? new Color(1,.65f,.2f,.8f) : new Color(.35f,1,.12f,.8f),
                            .35f, prop.Explosive ? .15f : .12f, prop.Explosive ? 1.2f : .4f);
                    }
                    continue;
                }
                if (!prop.Explosive || prop.Detonated) continue;
                prop.Fuse -= dt;
                prop.Sprite.color = Mathf.Sin(prop.Fuse * 45) > 0 ? Color.white : new Color(1, .35f, .15f);
                if (prop.Warning) prop.Warning.color = new Color(1, .5f, .12f, .6f + .2f * Mathf.Sin(prop.Fuse * 35));
                if (prop.Fuse <= 0) ExplodeRoomProp(prop);
            }
            for (int i = wetPatches.Count - 1; i >= 0; i--)
            {
                var patch = wetPatches[i];
                patch.Remaining -= dt;
                float flicker = .92f + .08f * Mathf.PerlinNoise(patch.Phase + 17, simulationTime * .4f);
                patch.Sprite.color = new Color(patch.Tint.r, patch.Tint.g, patch.Tint.b,
                    patch.Tint.a * Mathf.Clamp01(patch.Remaining / 3) * flicker);
                patch.Sprite.transform.localScale = new Vector3((1.2f + .02f * Mathf.Sin(simulationTime * 2 + patch.Phase)) * patch.RadiusX / 1.65f,
                    1.25f * patch.RadiusY / .95f, 1);
                if (patch.Remaining > 0) continue;
                Destroy(patch.Sprite.gameObject); wetPatches.RemoveAt(i);
            }
        }
    }
}
