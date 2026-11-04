using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        const int ScarecrowKind = 15, GargoyleKind = 16, StalkerShadowKind = 17, FireBatKind = 18;
        const float GargoyleStoneTime = 1.5f, GargoyleCycleTime = 3;
        static readonly Color EmberAccent = new Color(1, .43f, .14f);
        static readonly Color ShadowAccent = new Color(.68f, .36f, .9f);

        // The main simulation owns cooldowns, emergence, separation and contact damage.
        // These creatures use the same delayed pattern queue as the rest of the roster.
        bool UpdateHalloweenEnemy(Enemy enemy, Vector2 direction, float distance, float dt)
        {
            switch (enemy.Kind)
            {
                case ScarecrowKind:
                    if (!enemy.Awakened)
                    {
                        // A distant shot also reveals the disguise; the creature never
                        // remains inert while the player damages it from outside its radius.
                        if (distance >= 4.5f && enemy.Health >= enemy.MaxHealth) return true;
                        enemy.Awakened = true;
                        enemy.Cooldown = Mathf.Max(enemy.Cooldown, .35f);
                        enemy.Tell = .35f;
                        RingEffect(enemy.Position, EmberAccent, .85f);
                        Burst(enemy.Position + Vector2.up * .3f, EmberAccent, 6);
                    }
                    enemy.Position = MoveEnemy(enemy, direction * (2.5f * dt), .4f);
                    if (enemy.Cooldown <= 0 && !CoverBetween(enemy.Position, playerPosition))
                        PrepareMobPattern(enemy, direction);
                    return true;

                case GargoyleKind:
                    enemy.SpecialTimer = Mathf.Repeat(enemy.SpecialTimer + Mathf.Max(0, dt), GargoyleCycleTime);
                    if (GargoyleIsStone(enemy))
                    {
                        enemy.DashTime = 0;
                        enemy.Tell = 0;
                        enemy.Cooldown = Mathf.Max(enemy.Cooldown, .22f);
                        return true;
                    }
                    // Short low flights alternate with a predictable stone interval.
                    Vector2 gargoyleSide = new Vector2(-direction.y, direction.x);
                    Vector2 flight = distance > 2 ? direction : gargoyleSide;
                    enemy.Position = MoveEnemy(enemy, flight * (3.6f * dt), .4f);
                    if (enemy.Cooldown <= 0 && enemy.SpecialTimer < GargoyleCycleTime - .25f
                        && !CoverBetween(enemy.Position, playerPosition))
                        PrepareMobPattern(enemy, direction);
                    return true;

                case StalkerShadowKind:
                    Vector2 shadowSide = new Vector2(-direction.y, direction.x);
                    Vector2 stalk = direction + shadowSide * (Mathf.Sin(enemy.Phase * 2.2f) * .4f);
                    // MoveEnemy gives this kind its dedicated wall/void traversal rules.
                    enemy.Position = MoveEnemy(enemy, stalk.normalized * (2.65f * dt), .3f);
                    if (enemy.Cooldown <= 0) PrepareMobPattern(enemy, direction);
                    return true;

                case FireBatKind:
                    Vector2 fireSide = new Vector2(-direction.y, direction.x);
                    Vector2 zigzag = direction * (distance > 2 ? 1 : .35f)
                        + fireSide * (Mathf.Sin(enemy.Phase * 7) * 1.1f);
                    enemy.Position = MoveEnemy(enemy, zigzag.normalized * (4.1f * dt), .28f);
                    enemy.SpecialTimer += Mathf.Max(0, dt);
                    if (enemy.SpecialTimer >= .5f)
                    {
                        enemy.SpecialTimer %= .5f;
                        AddEmberZone(enemy.Position, .45f, 2.2f);
                    }
                    if (enemy.Cooldown <= 0 && !CoverBetween(enemy.Position, playerPosition))
                        PrepareMobPattern(enemy, direction);
                    return true;

                default:
                    return false;
            }
        }

        static bool GargoyleIsStone(Enemy enemy)
            => Mathf.Repeat(enemy.SpecialTimer, GargoyleCycleTime) < GargoyleStoneTime;

        bool IsEnemyDamageable(Enemy enemy)
        {
            if (enemy == null || enemy.Health <= 0 || enemy.SpawnRemaining > 0) return false;
            return enemy.Kind != GargoyleKind || !GargoyleIsStone(enemy);
        }

        // Called only when the shared .22-second telegraph expires. Geometry stays
        // in the deterministic core; this layer chooses speed, art and behaviour.
        bool EmitHalloweenAttack(int kind, Vector2 origin, float angle, int step)
        {
            switch (kind)
            {
                case ScarecrowKind:
                    foreach (float ray in BulletPatternGenerator.Fan(3, angle, 38))
                    {
                        var blade = PatternProjectile(origin, ray, 5.6f, kind);
                        if (blade == null) continue;
                        blade.Sprite.sprite = art.Get("shot-bone");
                        blade.Color = new Color(1, .68f, .4f);
                        blade.Sprite.color = new Color(1, .57f, .43f);
                        blade.Spin = 420;
                        blade.Bounces = 1;
                        blade.Life = 3.4f;
                        ConfigureProjectileLight(blade);
                    }
                    return true;

                case GargoyleKind:
                    foreach (float ray in BulletPatternGenerator.Fan(5, angle, 68))
                    {
                        var flame = PatternProjectile(origin, ray, 4.7f, kind);
                        if (flame == null) continue;
                        flame.Color = EmberAccent;
                        flame.Sprite.color = new Color(1, .74f, .48f);
                        flame.Life = 3.5f;
                        ConfigureProjectileLight(flame);
                    }
                    return true;

                case StalkerShadowKind:
                    foreach (float ray in BulletPatternGenerator.Fan(3, angle + (step % 2 == 0 ? -8 : 8), 30))
                    {
                        var dark = PatternProjectile(origin, ray, 4.8f, kind);
                        if (dark == null) continue;
                        dark.Color = ShadowAccent;
                        dark.Sprite.color = new Color(.86f, .67f, 1);
                        dark.Life = 3.8f;
                        ConfigureProjectileLight(dark);
                    }
                    return true;

                case FireBatKind:
                    if (step < 0)
                    {
                        foreach (float ray in BulletPatternGenerator.Ring(4, angle))
                            EmitEmberProjectile(origin, ray, 3.7f);
                    }
                    else
                        foreach (float ray in BulletPatternGenerator.Fan(3, angle, 28))
                            EmitEmberProjectile(origin, ray, 5.1f);
                    return true;

                default:
                    return false;
            }
        }

        void EmitEmberProjectile(Vector2 origin, float angle, float speed)
        {
            var ember = PatternProjectile(origin, angle, speed, FireBatKind);
            if (ember == null) return;
            ember.Color = EmberAccent;
            ember.Sprite.color = new Color(1, .7f, .4f);
            ember.Life = 3.1f;
            ConfigureProjectileLight(ember);
        }

        void HalloweenDeath(Enemy enemy)
        {
            if (enemy.Kind != FireBatKind || State == null || State.IsDead || CurrentRoom == null
                || CurrentRoom.Cleared || InShop || enemies.Count <= 1) return;
            RingEffect(enemy.Position, EmberAccent, .85f);
            // A source-free death volley retains the current wave epoch. Clearing
            // the last remaining foe cancels it before the next wave can begin.
            SchedulePattern(null, enemy.Position, PatternRecipe.Mob, enemy.Phase * 20, .18f, -1, FireBatKind);
        }
    }
}
