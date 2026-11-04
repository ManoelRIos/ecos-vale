using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        // Props use their own seeded stream, so decoration cannot change the waves.
        readonly List<RoomProp> roomProps = new List<RoomProp>();
        readonly List<WetPatch> wetPatches = new List<WetPatch>();
        readonly HashSet<long> brokenRoomProps = new HashSet<long>();
        sealed class RoomProp
        {
            public SpriteRenderer Sprite, Warning;
            public Vector2 Position;
            public Rect Bounds;
            public int Health;
            public bool Explosive, Detonated;
            public float Fuse, Flash;
            public long Key;
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
        void ClearRoomProps() { roomProps.Clear(); wetPatches.Clear(); }

        void ClearCombatSlowZones()
        {
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
            var brokenReservations = new List<Rect>();
            int count = CurrentRoom.Kind == RoomKind.Boss ? 4 : 6 + CurrentRoom.Id % 3;
            for (int i = 0; i < count; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (i < 4 ? 3.6f : 10.2f);
                float y = ((i / 2) % 2 == 0 ? -1 : 1) * (i < 4 ? 4.8f : 3.3f);
                // A few rooms place a second prop beside the inner barrels, inviting
                // deliberate chain reactions while keeping the cardinal routes free.
                if (i >= 6) { x = -1.9f; y = i == 6 ? -4.8f : 4.8f; }
                Vector2 position = new Vector2(x + (float)random.NextDouble() * .8f - .4f,
                    y + (float)random.NextDouble() * .8f - .4f);
                if (Blocked(position, .8f) || PitTouches(position, .75f)) continue;
                long key = ((long)State.Floor << 32) | (uint)(CurrentRoom.Id * 16 + i);
                var bounds = new Rect(position.x - .42f, position.y - .32f, .84f, .64f);
                if (brokenRoomProps.Contains(key))
                {
                    // Reserve the original footprint while laying out later props.
                    // A destroyed barrel must not make a formerly rejected prop spawn.
                    obstacles.Add(bounds); brokenReservations.Add(bounds);
                    var debris = MakeSprite("shattered-prop", position, roomRoot.transform, -130);
                    debris.color = new Color(1, 1, 1, .7f);
                    continue;
                }
                bool explosive = i % 3 != 1;
                var prop = new RoomProp
                {
                    Position = position, Key = key, Explosive = explosive, Health = explosive ? 3 : 2,
                    Bounds = bounds,
                    Sprite = MakeSprite(explosive ? "barrel-explosive" : "water-urn",
                        position + Vector2.up * .42f, roomRoot.transform, Depth(position.y))
                };
                var propShadow = MakeSprite("shadow", position + new Vector2(.2f, -.2f), roomRoot.transform, -70);
                propShadow.transform.localScale = new Vector3(1.6f, .8f, 1);
                propShadow.color = new Color(1, 1, 1, .72f);
                obstacles.Add(prop.Bounds);
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
                AddWetPatch(prop.Position);
                PlaySound(SoundCue.Splash, .7f);
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
                if (index >= 0) DamageEnemy(index, 7 + Mathf.Min(8, State.Floor),
                    (prop.Position - enemy.Position).normalized, orange, true);
            }
            if (Vector2.Distance(prop.Position, playerPosition) < 2.4f + PlayerRadius
                && !CoverBetween(prop.Position, playerPosition)) HurtPlayer();
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

        void UpdateRoomProps(float dt)
        {
            foreach (var prop in roomProps)
            {
                prop.Flash = Mathf.Max(0, prop.Flash - dt);
                if (prop.Health > 0)
                {
                    prop.Sprite.color = prop.Flash > 0 ? new Color(1, .65f, .5f) : DungeonAmbientTint;
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
