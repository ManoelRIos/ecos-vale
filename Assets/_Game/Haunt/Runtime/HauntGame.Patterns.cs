using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        enum PatternRecipe { Mob, SentinelSecond, Spiral, Bomb, WebFan, Cross, Bell, Iron, Oracle, Death }
        sealed class PendingPattern
        {
            public Enemy Source;
            public Vector2 Origin;
            public PatternRecipe Recipe;
            public float Remaining, Angle;
            public int Step, Epoch, Kind;
        }
        readonly List<PendingPattern> pendingPatterns = new List<PendingPattern>();
        int patternEpoch;

        void ClearBulletPatterns()
        {
            pendingPatterns.Clear();
            unchecked { patternEpoch++; }
        }

        void SchedulePattern(Enemy source, Vector2 origin, PatternRecipe recipe, float angle,
            float delay, int step = 0, int kind = 0)
        {
            if (pendingPatterns.Count >= 96 || CurrentRoom == null || CurrentRoom.Cleared || State.IsDead) return;
            pendingPatterns.Add(new PendingPattern { Source = source, Origin = origin, Recipe = recipe,
                Angle = angle, Remaining = delay, Step = step, Kind = kind, Epoch = patternEpoch });
            if (source != null && source.AttackVisualTime <= 0) source.Tell = Mathf.Max(source.Tell, Mathf.Min(delay, .35f));
        }

        void UpdateBulletPatterns(float dt)
        {
            if (CurrentRoom == null || CurrentRoom.Cleared || State == null || State.IsDead || InShop || Paused) return;
            int count = pendingPatterns.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var pattern = pendingPatterns[i];
                if (pattern.Epoch != patternEpoch || (pattern.Source != null
                    && (pattern.Source.Health <= 0 || !enemies.Contains(pattern.Source))))
                { pendingPatterns.RemoveAt(i); continue; }
                if (pattern.Source != null && pattern.Source.SpawnRemaining > 0) continue;
                pattern.Remaining -= dt;
                if (pattern.Remaining > 0) continue;
                pendingPatterns.RemoveAt(i);
                EmitPattern(pattern);
            }
        }

        void PrepareMobPattern(Enemy enemy, Vector2 direction)
        {
            if (enemy.SpawnRemaining > 0) return;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            if (enemy.Kind == 10)
                angle = BulletPatternGenerator.PredictiveAim(enemy.Position.x, enemy.Position.y,
                    playerPosition.x, playerPosition.y, playerVelocity.x, playerVelocity.y, 5.4f, .7f);
            SchedulePattern(enemy, enemy.Position, PatternRecipe.Mob, angle, .22f, enemy.Attack++, enemy.Kind);
            enemy.Cooldown = enemy.Kind == 6 || enemy.Kind == 9 || enemy.Kind == 13 ? 2.7f
                : enemy.Kind == 11 ? 2.8f : enemy.Kind == 10 ? 2.4f : enemy.Kind == 5 ? 3 : 1.7f;
        }

        void PrepareBossPattern(Enemy boss, Vector2 direction)
        {
            if (boss.SpawnRemaining > 0) return;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            int step = boss.Attack++;
            PatternRecipe recipe = BossVariant == 0 ? PatternRecipe.Spiral : BossVariant == 1 || BossVariant == 3
                ? PatternRecipe.WebFan : BossVariant == 2 ? PatternRecipe.Cross : BossVariant == 4
                ? PatternRecipe.Bell : BossVariant == 5 ? PatternRecipe.Iron : PatternRecipe.Oracle;
            SchedulePattern(boss, boss.Position, recipe, angle, .32f, step);
            if (BossVariant == 0) SchedulePattern(boss, boss.Position, PatternRecipe.Bomb, angle, .8f);
            if (step % 2 == 1) WarnGround(playerPosition, BossVariant == 5 ? .9f : 1.15f);
            boss.Cooldown = boss.Health <= boss.MaxHealth / 2 ? 1.9f : 2.55f;
        }

        Shot PatternProjectile(Vector2 origin, float angle, float speed, int kind = 0)
        {
            if (State.IsDead || CurrentRoom.Cleared) return null;
            int before = shots.Count;
            FireHostile(origin, Rotate(Vector2.right, angle) * speed, 1, kind);
            if (shots.Count == before) return null;
            var shot = shots[shots.Count - 1];
            shot.PatternEpoch = patternEpoch;
            shot.InitialSpeed = speed;
            shot.Life = 5;
            return shot;
        }

        void FanPattern(Vector2 origin, float angle, int count, float spread, float speed, int kind = 0)
        {
            foreach (float ray in BulletPatternGenerator.Fan(count, angle, spread)) PatternProjectile(origin, ray, speed, kind);
        }

        void EmitPattern(PendingPattern pattern)
        {
            Vector2 origin = pattern.Source != null ? pattern.Source.Position : pattern.Origin;
            if (pattern.Source != null) { pattern.Source.AttackVisualTime = .3f; pattern.Source.Tell = 0; }
            float angle = pattern.Angle;
            bool enraged = pattern.Source != null && pattern.Source.Health <= pattern.Source.MaxHealth / 2;
            if (pattern.Recipe == PatternRecipe.Mob)
            {
                if (EmitHalloweenAttack(pattern.Kind, origin, angle, pattern.Step)) return;
                switch (pattern.Kind)
                {
                    case 6:
                        foreach (float ray in BulletPatternGenerator.Ring(8, angle)) PatternProjectile(origin, ray, 4);
                        SchedulePattern(pattern.Source, origin, PatternRecipe.SentinelSecond, angle + 22.5f, .24f);
                        break;
                    case 8:
                        foreach (float ray in BulletPatternGenerator.Fan(pattern.Step % 2 == 0 ? 3 : 1, angle, 22))
                        {
                            var bone = PatternProjectile(origin, ray, 5.5f);
                            if (bone == null) continue;
                            bone.Sprite.sprite = art.Get("shot-bone"); bone.Sprite.color = new Color(1, .55f, .65f);
                            bone.Spin = 300; bone.Bounces = pattern.Step % 2 == 1 ? 1 : 0;
                        }
                        break;
                    case 9: case 13:
                        foreach (float ray in BulletPatternGenerator.Ring(8, pattern.Step * 22.5f))
                        {
                            var shot = PatternProjectile(origin, ray, 7, pattern.Kind);
                            if (shot != null) { shot.MinimumSpeed = 2.2f; shot.Deceleration = 4; }
                        }
                        break;
                    case 10: FanPattern(origin, angle, 5, 64, 5.4f); break;
                    case 12:
                        foreach (float ray in BulletPatternGenerator.Fan(2, angle, 30))
                        {
                            var ghost = PatternProjectile(origin, ray, 4.8f, 12);
                            if (ghost != null) { ghost.HomingRemaining = 1.1f; ghost.TurnRate = 38; }
                        }
                        break;
                    default:
                        FanPattern(origin, angle, pattern.Kind == 1 ? 1 : pattern.Kind == 7 || pattern.Kind == 14 ? 2
                            : pattern.Kind == 11 ? 5 : 3, pattern.Kind == 11 ? 68 : 34, 4.8f, pattern.Kind);
                        if (pattern.Kind == 11) WarnGround(playerPosition, .85f);
                        break;
                }
            }
            else if (pattern.Recipe == PatternRecipe.SentinelSecond || pattern.Recipe == PatternRecipe.Death)
            {
                foreach (float ray in BulletPatternGenerator.Ring(pattern.Recipe == PatternRecipe.Death ? 6 : 8, angle))
                    PatternProjectile(origin, ray, pattern.Recipe == PatternRecipe.Death ? 3.3f : 4);
            }
            else if (pattern.Recipe == PatternRecipe.Spiral)
            {
                foreach (float ray in BulletPatternGenerator.Spiral(2, angle))
                {
                    var seed = PatternProjectile(origin, ray, 4.3f);
                    if (seed != null) { seed.Color = new Color(1, .5f, .2f); ConfigureProjectileLight(seed); }
                }
                if (pattern.Kind < 11) SchedulePattern(pattern.Source, origin, PatternRecipe.Spiral,
                    angle + (enraged ? 24 : 19), .1f, pattern.Step, pattern.Kind + 1);
            }
            else if (pattern.Recipe == PatternRecipe.Bomb)
            {
                var bomb = PatternProjectile(origin, angle, 3.2f);
                if (bomb != null) { bomb.ExpirePattern = 1; bomb.Life = 1.15f; bomb.Spin = 150; EnlargeOrb(bomb, 1.2f, .27f); }
            }
            else if (pattern.Recipe == PatternRecipe.WebFan)
            {
                FanPattern(origin, angle + (pattern.Step % 2 == 0 ? -16 : 16), enraged ? 11 : 9, 130, 4.2f);
                foreach (float ray in BulletPatternGenerator.Fan(3, angle, 70))
                {
                    var web = PatternProjectile(origin, ray, 2.2f);
                    if (web == null) continue;
                    web.WebRadius = 1.45f; web.WebDuration = 5; web.Life = 1.8f;
                    web.Color = new Color(.85f, .4f, 1); EnlargeOrb(web, .95f, .19f);
                }
            }
            else if (pattern.Recipe == PatternRecipe.Cross)
            {
                foreach (var part in BulletPatternGenerator.CrossFormation(2, .5f, angle, enraged ? 4.4f : 3.5f))
                {
                    Vector2 start = origin + new Vector2(part.OffsetX, part.OffsetY);
                    if (!CoverBetween(origin, start, .14f)) PatternProjectile(start, angle, new Vector2(part.VelocityX, part.VelocityY).magnitude);
                }
            }
            else if (pattern.Recipe == PatternRecipe.Bell)
            {
                float gap = angle + pattern.Step * 37 + pattern.Kind * 20;
                foreach (float ray in BulletPatternGenerator.GapRing(24, pattern.Step * 7, gap, 52)) PatternProjectile(origin, ray, 3.3f);
                if (pattern.Kind < 2) SchedulePattern(pattern.Source, origin, PatternRecipe.Bell, angle,
                    .32f, pattern.Step, pattern.Kind + 1);
            }
            else if (pattern.Recipe == PatternRecipe.Iron)
            {
                foreach (float ray in BulletPatternGenerator.Fan(enraged ? 11 : 9, angle, 180))
                {
                    var shard = PatternProjectile(origin, ray, 4.1f);
                    if (shard == null) continue;
                    shard.ExpirePattern = 2; shard.SplitDistance = 3.2f; EnlargeOrb(shard, 1.05f, .22f);
                }
            }
            else if (pattern.Recipe == PatternRecipe.Oracle)
            {
                foreach (float ray in BulletPatternGenerator.Fan(enraged ? 3 : 2, angle, 45))
                {
                    var orb = PatternProjectile(origin, ray, 3.1f);
                    if (orb == null) continue;
                    orb.ExpirePattern = 3; orb.SplitDistance = 4.5f; EnlargeOrb(orb, 1.65f, .4f);
                }
            }
        }

        void EnlargeOrb(Shot shot, float scale, float radius)
        {
            shot.Sprite.transform.localScale = Vector3.one * scale;
            shot.Radius = radius; ConfigureProjectileLight(shot);
        }

        void OnEnemyDeathPattern(Enemy enemy)
        {
            HalloweenDeath(enemy);
            if ((enemy.Kind != 9 && enemy.Kind != 13) || enemies.Count <= 1 || State.IsDead) return;
            RingEffect(enemy.Position, HostileAccent, .9f);
            SchedulePattern(null, enemy.Position, PatternRecipe.Death, enemy.Phase * 20, .18f);
        }

        bool TryTriggerDistancePattern(Shot shot)
        {
            if (shot.PatternTriggered || shot.PatternEpoch != patternEpoch || shot.SplitDistance <= 0
                || shot.Travelled < shot.SplitDistance) return false;
            TriggerShotChildren(shot);
            return true;
        }

        void OnHostileShotEnd(Shot shot, bool hitSolid, bool hitPlayer)
        {
            if (shot.PatternTriggered || shot.PatternEpoch != patternEpoch || State.IsDead || CurrentRoom.Cleared) return;
            if (shot.WebRadius > 0) AddCombatSlowZone(shot.Position, shot.WebRadius, shot.WebDuration, new Color(.7f, .3f, .95f));
            if (shot.ExpirePattern == 1 || (shot.ExpirePattern == 2 && !hitPlayer)) TriggerShotChildren(shot);
            shot.PatternTriggered = true;
        }

        void TriggerShotChildren(Shot shot)
        {
            shot.PatternTriggered = true;
            if (shot.PatternEpoch != patternEpoch || CurrentRoom.Cleared || State.IsDead) return;
            float angle = Mathf.Atan2(shot.Velocity.y, shot.Velocity.x) * Mathf.Rad2Deg;
            if (shot.ExpirePattern == 1)
            {
                foreach (float ray in BulletPatternGenerator.Ring(4, angle)) PatternProjectile(shot.Position, ray, 4.6f);
                RingEffect(shot.Position, new Color(1, .6f, .2f), .8f); AddCameraTrauma(.24f);
            }
            else if (shot.ExpirePattern == 2) FanPattern(shot.Position, angle, 5, 75, 5.2f);
            else if (shot.ExpirePattern == 3)
            {
                foreach (float ray in BulletPatternGenerator.Ring(12, angle)) PatternProjectile(shot.Position, ray, 4.2f);
                RingEffect(shot.Position, new Color(.5f, 1, .6f), 1.1f);
            }
        }
    }
}
