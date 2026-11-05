using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        const float HeroAttackPoseTime = .24f, HeroCastPoseTime = .32f, MobDeathPoseTime = .42f;
        float heroAttackRemaining, heroAttackClock, heroCastRemaining, heroDeathClock, observedMuzzleTime, observedPowerCooldown;
        RunState observedCharacterRun;
        WeaponKind observedCharacterWeapon;
        float characterDrawRemaining, characterEntranceRemaining;

        static CharacterFacing CharacterDirection(Vector2 direction)
        {
            float x = Mathf.Abs(direction.x), y = Mathf.Abs(direction.y);
            if (y > x * 1.8f) return direction.y > 0 ? CharacterFacing.Back : CharacterFacing.Front;
            if (direction.y > x * .25f) return CharacterFacing.BackRight;
            if (direction.y < -x * .25f) return CharacterFacing.FrontRight;
            return CharacterFacing.Right;
        }

        CharacterWeaponPose EquippedCharacterPose()
        {
            if (State == null) return CharacterWeaponPose.Empty;
            switch (State.Weapon)
            {
                case WeaponKind.HunterCarbine: case WeaponKind.Scattergun: case WeaponKind.Repeater:
                case WeaponKind.SoulCannon: case WeaponKind.BoneCaster: case WeaponKind.ArcCoil:
                case WeaponKind.RayLantern: case WeaponKind.Umbrella: case WeaponKind.HiveLauncher:
                    return CharacterWeaponPose.TwoHanded;
                default: return CharacterWeaponPose.OneHanded;
            }
        }

        void TickCharacterPresentation(float dt)
        {
            if (State == null) return;
            if (!ReferenceEquals(observedCharacterRun, State))
            {
                observedCharacterRun = State; observedCharacterWeapon = State.Weapon;
                characterDrawRemaining = 0;
                characterEntranceRemaining = art.HasCharacterSheet(PlayerArt)
                    ? art.CharacterClipDuration(PlayerArt, CharacterClip.Entrance, CharacterFacing.Front, CharacterWeaponPose.Empty) : 0;
            }
            else if (observedCharacterWeapon != State.Weapon)
            {
                observedCharacterWeapon = State.Weapon;
                characterDrawRemaining = art.HasCharacterSheet(PlayerArt)
                    ? art.CharacterClipDuration(PlayerArt, CharacterClip.Draw, CharacterFacing.Front, EquippedCharacterPose()) : 0;
            }
            if (playerMoving || muzzleTime > 0 || rollRemaining > 0 || State.IsReloading || heroCastRemaining > 0)
                characterDrawRemaining = characterEntranceRemaining = 0;
            characterDrawRemaining = Mathf.Max(0, characterDrawRemaining - dt);
            characterEntranceRemaining = Mathf.Max(0, characterEntranceRemaining - dt);
        }

        CharacterClip PlayerCharacterClip(Vector2 aim, out int frame, out CharacterFacing facing, out CharacterWeaponPose pose)
        {
            facing = CharacterDirection(rollRemaining > 0 ? rollDirection : aim);
            pose = EquippedCharacterPose();
            CharacterClip clip;
            if (rollRemaining > 0)
            {
                clip = CharacterClip.Dodge;
                int count = art.CharacterFrameCount(PlayerArt, clip, facing, pose);
                frame = Mathf.Clamp(Mathf.FloorToInt((1 - rollRemaining / RollTime) * count), 0, count - 1);
            }
            else if (heroCastRemaining > 0)
            { clip = CharacterClip.Cast; frame = Mathf.FloorToInt((HeroCastPoseTime - heroCastRemaining) * art.CharacterFrameRate(PlayerArt, clip, facing, pose)); }
            else if (State.IsReloading)
            { clip = CharacterClip.Reload; frame = Mathf.FloorToInt(State.ReloadProgress * art.CharacterFrameCount(PlayerArt, clip, facing, pose)); }
            else if (heroAttackRemaining > 0 || muzzleTime > 0)
            { clip = CharacterClip.Attack; frame = Mathf.FloorToInt(heroAttackClock * art.CharacterFrameRate(PlayerArt, clip, facing, pose)); }
            else if (!playerMoving && characterDrawRemaining > 0)
            {
                clip = CharacterClip.Draw;
                frame = Mathf.FloorToInt((art.CharacterClipDuration(PlayerArt, clip, facing, pose) - characterDrawRemaining)
                    * art.CharacterFrameRate(PlayerArt, clip, facing, pose));
            }
            else if (!playerMoving && characterEntranceRemaining > 0)
            {
                clip = CharacterClip.Entrance; pose = CharacterWeaponPose.Empty;
                frame = Mathf.FloorToInt((art.CharacterClipDuration(PlayerArt, clip, facing, pose) - characterEntranceRemaining)
                    * art.CharacterFrameRate(PlayerArt, clip, facing, pose));
            }
            else
            {
                clip = playerMoving ? CharacterClip.Walk : CharacterClip.Idle;
                frame = Mathf.FloorToInt(simulationTime * art.CharacterFrameRate(PlayerArt, clip, facing, pose));
            }
            return clip;
        }

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
            TickCharacterPresentation(dt);
            heroAttackRemaining = Mathf.Max(0, heroAttackRemaining - dt);
            heroCastRemaining = Mathf.Max(0, heroCastRemaining - dt);
            if (heroAttackRemaining > 0) heroAttackClock += dt;
            if (State != null && State.IsDead && player)
            {
                CharacterFacing facing = CharacterDirection(lastMove);
                bool native = art.HasCharacterSheet(PlayerArt);
                float duration = native ? art.CharacterClipDuration(PlayerArt, CharacterClip.Death, facing, CharacterWeaponPose.Empty) : .42f;
                float fadeEnd = duration + .18f;
                heroDeathClock = Mathf.Min(fadeEnd, heroDeathClock + dt);
                if (native)
                    player.sprite = art.CharacterFrame(PlayerArt, CharacterClip.Death,
                        Mathf.FloorToInt(heroDeathClock * art.CharacterFrameRate(PlayerArt, CharacterClip.Death, facing, CharacterWeaponPose.Empty)),
                        facing, CharacterWeaponPose.Empty);
                else
                    player.sprite = art.ActorFrame(PlayerArt, ActorAnimationState.Death,
                        Mathf.Clamp(Mathf.FloorToInt(heroDeathClock / .42f * HauntPixelArt.ActorFrameCount), 0, HauntPixelArt.ActorFrameCount - 1), SpriteFacing(lastMove), false);
                player.transform.position = Snap(playerPosition);
                player.transform.localScale = Vector3.one; player.transform.rotation = Quaternion.identity;
                player.sortingOrder = Depth(playerPosition.y);
                float alpha = Mathf.Clamp01((fadeEnd - heroDeathClock) / .18f);
                player.color = new Color(1, 1, 1, alpha);
                if (gun) gun.enabled = false;
                if (muzzle) muzzle.enabled = false;
                if (playerGlow) playerGlow.enabled = false;
                if (playerMarker) playerMarker.enabled = false;
                if (shadow) shadow.color = new Color(1, 1, 1, .72f * alpha);
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
            observedCharacterRun = null; characterDrawRemaining = characterEntranceRemaining = 0;
        }
    }
}
