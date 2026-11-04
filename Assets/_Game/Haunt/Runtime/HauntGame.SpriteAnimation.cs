using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        const float HeroAttackPoseTime = .24f, HeroCastPoseTime = .32f, MobDeathPoseTime = .42f;
        float heroAttackRemaining, heroAttackClock, heroCastRemaining, heroDeathClock, observedMuzzleTime, observedPowerCooldown;
        sealed class ActorPoseClock
        {
            public Vector2 Position;
            public ActorAnimationState State;
            public float Started;
        }
        sealed class MobRemnant
        {
            public SpriteRenderer Sprite;
            public string Art;
            public ActorFacing Facing;
            public float Remaining = MobDeathPoseTime;
        }
        readonly Dictionary<Enemy, ActorPoseClock> actorPoseClocks = new Dictionary<Enemy, ActorPoseClock>();
        readonly List<MobRemnant> mobRemnants = new List<MobRemnant>();

        static ActorFacing SpriteFacing(Vector2 direction)
            => Mathf.Abs(direction.y) > Mathf.Abs(direction.x) * 1.15f
                ? direction.y > 0 ? ActorFacing.Up : ActorFacing.Down : ActorFacing.Right;

        void UpdateSpriteAnimations(float dt)
        {
            if (State != null)
            {
                if (State.PowerCooldown > observedPowerCooldown + .05f) heroCastRemaining = HeroCastPoseTime;
                observedPowerCooldown = State.PowerCooldown;
                if (muzzleTime > observedMuzzleTime + .001f)
                {
                    if (heroAttackRemaining <= 0) heroAttackClock = 0;
                    heroAttackRemaining = HeroAttackPoseTime;
                }
            }
            observedMuzzleTime = muzzleTime;
            heroAttackRemaining = Mathf.Max(0, heroAttackRemaining - dt);
            heroCastRemaining = Mathf.Max(0, heroCastRemaining - dt);
            if (heroAttackRemaining > 0) heroAttackClock += dt;
            if (State != null && State.IsDead && player)
            {
                heroDeathClock = Mathf.Min(.6f, heroDeathClock + dt);
                int frame = Mathf.Clamp(Mathf.FloorToInt(heroDeathClock / .42f * HauntPixelArt.ActorFrameCount), 0, HauntPixelArt.ActorFrameCount - 1);
                player.sprite = art.ActorFrame(PlayerArt, ActorAnimationState.Death, frame, SpriteFacing(lastMove), false);
                player.transform.position = Snap(playerPosition);
                player.transform.localScale = Vector3.one; player.transform.rotation = Quaternion.identity;
                player.sortingOrder = Depth(playerPosition.y);
                player.color = new Color(1, 1, 1, Mathf.Clamp01((.6f - heroDeathClock) / .18f));
                if (gun) gun.enabled = false;
                if (muzzle) muzzle.enabled = false;
                if (playerGlow) playerGlow.enabled = false;
                if (playerMarker) playerMarker.enabled = false;
                if (shadow) shadow.color = new Color(1, 1, 1, .72f * Mathf.Clamp01((.6f - heroDeathClock) / .18f));
            }
            for (int i = mobRemnants.Count - 1; i >= 0; i--)
            {
                var remnant = mobRemnants[i]; remnant.Remaining -= dt;
                int frame = Mathf.Clamp(Mathf.FloorToInt((1 - remnant.Remaining / MobDeathPoseTime) * HauntPixelArt.ActorFrameCount), 0, HauntPixelArt.ActorFrameCount - 1);
                remnant.Sprite.sprite = art.ActorFrame(remnant.Art, ActorAnimationState.Death, frame, remnant.Facing);
                remnant.Sprite.color = new Color(1, 1, 1, Mathf.Clamp01(remnant.Remaining / .1f));
                if (remnant.Remaining > 0) continue;
                Destroy(remnant.Sprite.gameObject); mobRemnants.RemoveAt(i);
            }
        }

        ActorAnimationState PlayerSpriteState(out int frame)
        {
            ActorAnimationState state;
            if (rollRemaining > 0)
            {
                state = ActorAnimationState.Dodge;
                frame = Mathf.FloorToInt((1 - rollRemaining / RollTime) * HauntPixelArt.ActorFrameCount);
            }
            else if (heroCastRemaining > 0)
            {
                state = ActorAnimationState.Cast;
                frame = Mathf.FloorToInt((1 - heroCastRemaining / HeroCastPoseTime) * HauntPixelArt.ActorFrameCount);
            }
            else if (State.IsReloading)
            {
                state = ActorAnimationState.Reload;
                frame = Mathf.FloorToInt(State.ReloadProgress * HauntPixelArt.ActorFrameCount);
            }
            else if (heroAttackRemaining > 0 || muzzleTime > 0)
            {
                state = ActorAnimationState.Attack;
                frame = Mathf.FloorToInt(heroAttackClock * HauntPixelArt.ActorFrameCount / HeroAttackPoseTime);
            }
            else
            {
                state = playerMoving ? ActorAnimationState.Walk : ActorAnimationState.Idle;
                frame = Mathf.FloorToInt(playerMoving ? walkDistance * 4.5f : simulationTime * 4);
            }
            return state;
        }

        ActorAnimationState EnemySpriteState(Enemy enemy, bool stone, bool disguised, out int frame)
        {
            if (!actorPoseClocks.TryGetValue(enemy, out var clock))
            {
                clock = new ActorPoseClock { Position = enemy.Position, Started = simulationTime };
                actorPoseClocks.Add(enemy, clock);
            }
            bool moved = (enemy.Position - clock.Position).sqrMagnitude > .000001f;
            ActorAnimationState state = enemy.SpawnRemaining > 0 ? ActorAnimationState.Spawn
                : stone ? ActorAnimationState.Petrified : disguised ? ActorAnimationState.Idle
                : enemy.AttackVisualTime > 0 || enemy.DashTime > 0 ? ActorAnimationState.Attack
                : enemy.Tell > 0 ? ActorAnimationState.Cast
                : moved ? ActorAnimationState.Walk : ActorAnimationState.Idle;
            if (clock.State != state) { clock.State = state; clock.Started = simulationTime; }
            clock.Position = enemy.Position;
            frame = state == ActorAnimationState.Spawn
                ? Mathf.Clamp(Mathf.FloorToInt((1 - enemy.SpawnRemaining / .9f) * HauntPixelArt.ActorFrameCount), 0, HauntPixelArt.ActorFrameCount - 1)
                : Mathf.FloorToInt((simulationTime - clock.Started) * (state == ActorAnimationState.Attack ? 14 : 9));
            return state;
        }

        void PlayMobDeathPresentation(Enemy enemy)
        {
            string id = EnemySprite(enemy.Kind);
            actorPoseClocks.Remove(enemy);
            if (enemy.Kind == 4 || !HauntPixelArt.HasActorSheet(id) || mobRemnants.Count >= 32) return;
            var sprite = MakeSprite(id, enemy.Sprite.transform.position, actorsRoot.transform, Depth(enemy.Position.y) + 1);
            ActorFacing facing = SpriteFacing(playerPosition - enemy.Position);
            sprite.sprite = art.ActorFrame(id, ActorAnimationState.Death, 0, facing);
            sprite.transform.localScale = Vector3.one;
            sprite.flipX = enemy.Sprite.flipX;
            mobRemnants.Add(new MobRemnant { Sprite = sprite, Art = id, Facing = facing });
        }

        void ClearSpriteAnimations()
        {
            foreach (var remnant in mobRemnants) if (remnant.Sprite) Destroy(remnant.Sprite.gameObject);
            mobRemnants.Clear(); actorPoseClocks.Clear();
            heroAttackRemaining = heroAttackClock = heroCastRemaining = heroDeathClock = observedMuzzleTime = 0;
            muzzleTime = 0;
            observedPowerCooldown = State == null ? 0 : State.PowerCooldown;
        }
    }
}
