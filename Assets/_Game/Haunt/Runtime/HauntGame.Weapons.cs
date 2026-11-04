using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        SpriteRenderer beamVisual, beamGlow;
        float beamPulse;

        // The input path calls this every frame, including release, so a held ray
        // follows the cursor smoothly while damage still respects the weapon rate.
        void UpdateWeaponTrigger(Vector2 aim, bool pressed, float dt)
        {
            if (!pressed || InShop || shopMenuOpen || ConversationCapturesInput || Paused || State == null || State.IsDead || rollRemaining > 0)
            {
                StopBeam();
                return;
            }
            if (aim.sqrMagnitude < .0001f) { StopBeam(); return; }
            aim.Normalize();
            beamPulse = Mathf.Repeat(beamPulse + Mathf.Max(0, dt) * 5, Mathf.PI * 2);
            bool ray = State.Weapon == WeaponKind.ArcCoil || State.Weapon == WeaponKind.RayLantern;
            if (ray)
            {
                if (!UpdateBeam(aim, shotCooldown <= 0)) { ReportEmptyWeapon(); return; }
            }
            else
            {
                StopBeam();
                if (shotCooldown > 0) return;
                if (!FireWeapon(aim)) { ReportEmptyWeapon(); return; }
                PlaySound(State.Weapon == WeaponKind.VeilWand ? SoundCue.MagicShot : SoundCue.Shot, .8f);
            }
            if (shotCooldown > 0) return;
            bool heavy = State.Weapon == WeaponKind.SoulCannon || State.Weapon == WeaponKind.BoxingGlove;
            OnPlayerWeaponFired(aim, heavy, ray);
            weaponKick = 1;
            muzzleTime = .045f;
            shotCooldown = State.WeaponInterval;
            Emit("shot-halo", playerPosition + Vector2.up * .35f + aim * .65f, Vector2.zero,
                new Color(1, .82f, .56f, ray ? .12f : .18f), .045f, ray ? .3f : .45f, 0);
        }

        void StopBeam()
        {
            UpdateRaySound(false);
            if (beamVisual != null) beamVisual.gameObject.SetActive(false);
            if (beamGlow != null) beamGlow.gameObject.SetActive(false);
        }

        bool FireWeapon(Vector2 aim)
        {
            if (InShop || shopMenuOpen || ConversationCapturesInput || Paused || State == null || State.IsDead) return false;
            aim = aim.sqrMagnitude > .0001f ? aim.normalized : Vector2.right;
            if (State.Weapon == WeaponKind.ArcCoil || State.Weapon == WeaponKind.RayLantern) return UpdateBeam(aim, true);
            int required = State.Weapon == WeaponKind.Umbrella || State.Weapon == WeaponKind.Scattergun ? 5
                : State.Weapon == WeaponKind.VeilWand || State.Weapon == WeaponKind.BoneFlute || State.Weapon == WeaponKind.HiveLauncher ? 3
                : State.Weapon == WeaponKind.Stapler ? 2 : 1;
            if (shots.Count > MaxShots - required || !State.TryConsumeShot()) return false;
            Vector2 origin = playerPosition + aim * .48f;
            switch (State.Weapon)
            {
                case WeaponKind.Stapler:
                    Vector2 side = new Vector2(-aim.y, aim.x) * .12f;
                    FireSpecial(origin + side, aim * 19, State.Damage, WeaponKind.Stapler);
                    FireSpecial(origin - side, aim * 19, State.Damage, WeaponKind.Stapler);
                    break;
                case WeaponKind.Banana:
                    FireSpecial(origin, aim * 12, State.Damage * 3, WeaponKind.Banana);
                    break;
                case WeaponKind.SpinningTop:
                    FireSpecial(origin, aim * 8, State.Damage * 2, WeaponKind.SpinningTop);
                    break;
                case WeaponKind.Scattergun:
                    for (int pellet = -2; pellet <= 2; pellet++)
                        FireSpecial(origin, Rotate(aim, pellet * 9) * 15, State.Damage, WeaponKind.Scattergun);
                    break;
                case WeaponKind.Pistol:
                    FireSpecial(origin, aim * 17, State.Damage, WeaponKind.Pistol);
                    break;
                case WeaponKind.Repeater:
                    FireSpecial(origin, aim * 25, State.Damage, WeaponKind.Repeater);
                    break;
                case WeaponKind.SoulCannon:
                    FireSpecial(origin, aim * 11, State.Damage * 5, WeaponKind.SoulCannon);
                    break;
                case WeaponKind.HunterCarbine:
                    FireSpecial(origin, aim * 22, State.Damage, WeaponKind.HunterCarbine);
                    break;
                case WeaponKind.VeilWand:
                    for (int pellet = -1; pellet <= 1; pellet++)
                        FireSpecial(origin, Rotate(aim, pellet * 11) * 15, State.Damage, WeaponKind.VeilWand);
                    break;
                case WeaponKind.BoneCaster:
                    FireSpecial(origin, aim * 17, State.Damage * 2, WeaponKind.BoneCaster);
                    break;
                case WeaponKind.MirrorDisc:
                    FireSpecial(origin, aim * 15, State.Damage * 2, WeaponKind.MirrorDisc);
                    break;
                case WeaponKind.BoneFlute:
                    for (int pellet = -1; pellet <= 1; pellet++)
                        FireSpecial(origin, Rotate(aim, pellet * 13) * 16, State.Damage * 2, WeaponKind.BoneFlute);
                    break;
                case WeaponKind.Umbrella:
                    for (int petal = -2; petal <= 2; petal++)
                        FireSpecial(origin, Rotate(aim, petal * 10) * 14, State.Damage, WeaponKind.Umbrella);
                    break;
                case WeaponKind.HiveLauncher:
                    for (int stinger = -1; stinger <= 1; stinger++)
                        FireSpecial(origin, Rotate(aim, stinger * 16) * 16, State.Damage, WeaponKind.HiveLauncher);
                    break;
                case WeaponKind.BoxingGlove:
                    FireSpecial(origin, aim * 11, State.Damage * 4, WeaponKind.BoxingGlove);
                    break;
                case WeaponKind.ArcCoil:
                case WeaponKind.RayLantern:
                    UpdateBeam(aim, true);
                    break;
                default:
                    Fire(origin, aim * (State.Weapon == WeaponKind.SoulCannon ? 11 : 17), true,
                        State.Damage * (State.Weapon == WeaponKind.SoulCannon ? 5 : 1));
                    break;
            }
            return true;
        }

        void FireSpecial(Vector2 position, Vector2 velocity, int damage, WeaponKind style)
        {
            int before = shots.Count;
            Fire(position, velocity, true, damage);
            if (shots.Count == before) return;
            var shot = shots[shots.Count - 1];
            shot.Style = style;
            string id;
            float scale;
            switch (style)
            {
                case WeaponKind.Repeater:
                    id = "shot-stinger"; scale = .8f; shot.Color = new Color(1,.7f,.3f); shot.Radius = .11f; shot.Life = 3;
                    break;
                case WeaponKind.SoulCannon:
                    id = "shot-shock"; scale = 1.35f; shot.Color = new Color(.7f,1,.45f); shot.Radius = .26f; shot.Life = 4; shot.Heavy = true;
                    break;
                case WeaponKind.Scattergun:
                    id = "shot-dot"; scale = .65f; shot.Color = new Color(1,.65f,.4f); shot.Radius = .1f; shot.Life = 2;
                    break;
                case WeaponKind.Stapler:
                    id = "staple-shot"; scale = .78f; shot.Color = new Color(.65f, .87f, 1);
                    shot.Radius = .12f; shot.Life = 4;
                    break;
                case WeaponKind.Banana:
                    id = "banana-shot"; scale = .8f; shot.Color = new Color(1, .85f, .2f);
                    shot.Curve = 52; shot.Spin = 360; shot.Radius = .14f; shot.Life = 2.8f;
                    break;
                case WeaponKind.SpinningTop:
                    id = "top-shot"; scale = .88f; shot.Color = new Color(.4f, 1, .95f);
                    shot.Spin = 720; shot.Radius = .18f; shot.Life = 2.8f;
                    shot.Pierces = 3; shot.HitEnemies = new HashSet<Enemy>();
                    break;
                case WeaponKind.MirrorDisc:
                    id = "shot-disc"; scale = .85f; shot.Color = new Color(.45f, .9f, 1);
                    shot.Spin = 660; shot.Radius = .17f; shot.Life = 5; shot.Bounces = 2;
                    break;
                case WeaponKind.BoneCaster:
                case WeaponKind.BoneFlute:
                    id = "shot-bone"; scale = .85f; shot.Color = new Color(.95f, .86f, .64f);
                    shot.Radius = .14f; shot.Life = 4;
                    break;
                case WeaponKind.Umbrella:
                    id = "shot-petal"; scale = .82f; shot.Color = new Color(1, .5f, .8f);
                    shot.Radius = .13f; shot.Life = 3.2f; shot.Bounces = 2;
                    break;
                case WeaponKind.HiveLauncher:
                    id = "shot-stinger"; scale = .85f; shot.Color = new Color(1, .78f, .25f);
                    shot.Radius = .12f; shot.Life = 2.4f;
                    break;
                case WeaponKind.BoxingGlove:
                    id = "shot-fist"; scale = 1.05f; shot.Color = new Color(.35f, .86f, 1);
                    shot.Radius = .23f; shot.Life = 2.2f; shot.Heavy = true;
                    break;
                case WeaponKind.VeilWand:
                    id = "shot-veil"; scale = .8f; shot.Color = new Color(.85f, .55f, 1);
                    shot.Radius = .14f; shot.Life = 4;
                    break;
                case WeaponKind.HunterCarbine:
                    id = "shot-carbine"; scale = .85f; shot.Color = new Color(.55f, 1, .85f);
                    shot.Radius = .13f; shot.Life = 4;
                    break;
                default:
                    id = "shot-dot"; scale = .85f; shot.Color = new Color(1, .85f, .4f);
                    shot.Radius = .14f; shot.Life = 4;
                    break;
            }
            shot.Sprite.sprite = art.Get(id);
            shot.Sprite.color = Color.white;
            float relicScale = Mathf.Clamp(State.ProjectileScale, 1, 1.6f);
            shot.Radius *= relicScale;
            shot.Sprite.transform.localScale = Vector3.one * (scale * relicScale);
            ConfigureProjectileLight(shot);
        }

        // Keep this signature stable: encounter checks and powers also use it.
        void Fire(Vector2 origin, Vector2 velocity, bool friendly, int damage)
        {
            if (shots.Count >= MaxShots) return;
            SpriteRenderer sprite;
            SpriteRenderer light;
            if (shotPool.Count > 0)
            {
                sprite = shotPool.Pop();
                light = sprite.transform.GetChild(0).GetComponent<SpriteRenderer>();
            }
            else
            {
                sprite = MakeSprite("bullet-player", origin, actorsRoot.transform, 1250);
                light = MakeSprite("shot-halo", origin, sprite.transform, 1249);
                light.sharedMaterial = ProjectileLightMaterial;
            }
            bool heavy = friendly && State.Weapon == WeaponKind.SoulCannon;
            sprite.gameObject.SetActive(true);
            sprite.sprite = art.Get(friendly ? "bullet-player" : "bullet-enemy");
            sprite.color = heavy ? new Color(.6f, 1, .85f) : friendly ? CharacterAccent : Color.Lerp(HostileAccent, FloorColors[ThemeVariant], .22f);
            float relicScale = friendly ? Mathf.Clamp(State.ProjectileScale, 1, 1.6f) : 1;
            float scale = (heavy ? 1.15f : friendly ? .85f : .8f) * relicScale;
            sprite.transform.localScale = Vector3.one * scale;
            sprite.transform.position = Snap(origin + Vector2.up * .38f);
            sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            light.gameObject.SetActive(true);
            light.transform.rotation = Quaternion.identity;
            light.transform.position = Snap(origin + Vector2.up * .38f);
            Color shotColor = sprite.color;
            sprite.color = Color.white;
            var shot = new Shot { Sprite = sprite, Glow = light, Position = origin, Velocity = velocity,
                Friendly = friendly, Damage = damage + (friendly ? State.ProjectileDamageBonus : 0), Life = 6,
                Radius = (heavy ? .23f : friendly ? .14f : .16f) * relicScale,
                Color = shotColor, Heavy = heavy, TrailTimer = friendly ? 0 : .02f * (shots.Count % 4) };
            ConfigureProjectileLight(shot);
            shots.Add(shot);
            if (!friendly) Emit("shot-halo", origin + Vector2.up * .35f, Vector2.zero, new Color(1, .7f, .55f, .12f), .05f, .3f, 0);
        }

        void StyleLastHostileShot(int kind)
        {
            if (shots.Count == 0) return;
            var shot = shots[shots.Count - 1];
            if (shot.Friendly) return;
            // All three retain the hostile red rim/ivory core. Shape and size add
            // information without making enemy rounds resemble the player's ammo.
            if (kind == 12)
                shot.Sprite.transform.localScale = new Vector3(.76f, .9f, 1);
            else if (kind == 13)
            {
                shot.Sprite.transform.localScale = Vector3.one * .95f;
                shot.Radius = .19f;
            }
            else if (kind == 14)
            {
                shot.Sprite.transform.localScale = new Vector3(1.05f, .48f, 1);
                shot.Radius = .13f;
            }
            ConfigureProjectileLight(shot);
        }

        void ConfigureProjectileLight(Shot shot)
        {
            // A small corona belongs to the projectile itself, at the same height.
            // The opaque pixel core carries its silhouette; no light decal follows it.
            shot.Glow.enabled = true;
            float relicScale = shot.Friendly ? Mathf.Clamp(State.ProjectileScale, 1, 1.6f) : 1;
            float size = (shot.Heavy ? 1.1f : shot.Friendly ? .85f : .7f) * relicScale;
            Color tint = Color.Lerp(shot.Color, new Color(1, .92f, .8f), .25f);
            shot.Glow.color = new Color(tint.r, tint.g, tint.b, shot.Heavy ? .36f : shot.Friendly ? .29f : .18f);
            float scale = Mathf.Max(.01f, shot.Sprite.transform.localScale.x);
            shot.Glow.transform.localScale = Vector3.one * (size / scale);
            shot.Glow.transform.rotation = Quaternion.identity;
            shot.Glow.transform.position = Snap(shot.Position + Vector2.up * .38f);
        }

        bool UpdateBeam(Vector2 aim, bool damageTick)
        {
            if (State == null || State.IsDead || State.IsReloading || State.Magazine <= 0)
            { StopBeam(); return false; }
            if (damageTick && !State.TryConsumeShot()) { StopBeam(); return false; }
            bool lantern = State.Weapon == WeaponKind.RayLantern;
            Color color = lantern ? new Color(.8f, .5f, 1) : new Color(.4f, 1, .45f);
            Vector2 origin = playerPosition;
            Vector2 end = origin + aim * (lantern ? 11 : 7);
            float beamRadius = .055f * Mathf.Clamp(State.ProjectileScale, 1, 1.6f);
            FindSolidContact(origin, end, beamRadius, out float nearest, out _);
            int propIndex = -1;
            if (FindRoomPropContact(origin, end, beamRadius, out float propContact, out int candidateProp)
                && propContact <= nearest + .00001f)
            {
                nearest = propContact;
                propIndex = candidateProp;
            }
            int targetIndex = -1;
            for (int e = 0; e < enemies.Count; e++)
            {
                var enemy = enemies[e];
                if (enemy.SpawnRemaining > 0) continue;
                if (!CombatGeometry.CircleHitFraction(origin.x, origin.y, end.x, end.y,
                    enemy.Position.x, enemy.Position.y, EnemyHitRadius(enemy) + beamRadius, out float contact) || contact >= nearest) continue;
                nearest = contact;
                targetIndex = e;
                propIndex = -1;
            }
            if (nearest <= 1) end = Vector2.Lerp(origin, end, Mathf.Clamp01(nearest));
            float distance = Vector2.Distance(origin, end);
            // Trace from the player so a muzzle touching cover cannot shoot through it.
            // The visible beam begins at the gun, or stops entirely if cover is closer.
            float muzzleOffset = Mathf.Min(.5f, distance);
            Vector2 start = origin + aim * muzzleOffset;
            float length = distance - muzzleOffset;
            if (beamVisual == null)
            {
                beamVisual = MakeSprite("shot-ray", start, actorsRoot.transform, 1248);
                beamVisual.sharedMaterial = art.Material;
                beamGlow = MakeSprite("shot-ray", start, actorsRoot.transform, 1247);
                beamGlow.sharedMaterial = ProjectileLightMaterial;
            }
            UpdateRaySound(length > .01f, lantern);
            beamVisual.gameObject.SetActive(length > .01f);
            beamGlow.gameObject.SetActive(length > .01f);
            if (length > .01f)
            {
                beamVisual.transform.position = Snap((start + end) * .5f + Vector2.up * .38f);
                beamVisual.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
                Sprite sprite = beamVisual.sprite;
                float width = sprite.rect.width / sprite.pixelsPerUnit;
                float height = sprite.rect.height / sprite.pixelsPerUnit;
                float pulse = .97f + Mathf.Sin(beamPulse) * .03f;
                float relicScale = Mathf.Clamp(State.ProjectileScale, 1, 1.6f);
                beamVisual.transform.localScale = new Vector3(length / width, (lantern ? .105f : .08f) * relicScale * pulse / height, 1);
                beamVisual.color = Color.Lerp(color, Color.white, .55f);
                beamGlow.transform.position = beamVisual.transform.position;
                beamGlow.transform.rotation = beamVisual.transform.rotation;
                beamGlow.transform.localScale = new Vector3(length / width, (lantern ? .22f : .18f) * relicScale / height, 1);
                beamGlow.color = new Color(color.r, color.g, color.b, .24f);
                muzzleTime = .035f;
            }
            if (!damageTick) return true;
            if (targetIndex >= 0)
                DamageEnemy(targetIndex, State.Damage * (lantern ? 2 : 1) + State.ProjectileDamageBonus, -aim, color, false);
            else if (propIndex >= 0)
            {
                DamageRoomProp(propIndex, State.Damage * (lantern ? 2 : 1) + State.ProjectileDamageBonus);
                Impact(end + Vector2.up * .38f, -aim, color, false);
            }
            else if (nearest <= 1) Impact(end + Vector2.up * .38f, -aim, color, false);
            Emit("impact-dust", end + Vector2.up * .38f, -aim * .35f, new Color(color.r, color.g, color.b, .4f), .07f, .2f, 0);
            return true;
        }

        static float EnemyHitRadius(Enemy enemy)
        {
            return enemy.Kind == 4 ? .85f : enemy.Kind == 11 ? .65f : enemy.Kind == 13 ? .48f : .36f;
        }

        void DamageEnemy(int index, int damage, Vector2 normal, Color color, bool heavy)
            => DamageEnemyWithFeedback(index, damage, normal, color, heavy, true);

        void DamageEnemyWithFeedback(int index, int damage, Vector2 normal, Color color, bool heavy, bool applyKnockback)
        {
            var enemy = enemies[index];
            if (!IsEnemyDamageable(enemy))
            { Impact(enemy.Position + Vector2.up * .38f, normal, new Color(.6f,.62f,.7f), false); return; }
            enemy.Health -= damage;
            PlaySound(SoundCue.EnemyHit, .45f);
            ApplyEnemyHitFeedback(enemy, -normal, damage, heavy, applyKnockback);
            Impact(enemy.Position + Vector2.up * .38f, normal, color, heavy);
            if (enemy.Health > 0) return;
            bool boss = enemy.Kind == 4;
            OnEnemyDeathPattern(enemy);
            if (boss) PlayBossDeathPresentation(enemy);
            EnemyDeath(enemy);
            enemies.RemoveAt(index);
            enemiesDefeated++;
            TryDropAmmunition(enemy.Position);
            AddCameraTrauma(boss ? .7f : enemy.Kind == 11 ? .3f : .08f);
            if (enemies.Count == 0) CompleteRoom(boss);
        }

        void UpdateShots(float dt)
        {
            // Appended pattern children wait until the next frame. Every removal is
            // confined to the current index, so child emission cannot shift work backward.
            int snapshotCount = shots.Count;
            for (int i = snapshotCount - 1; i >= 0; i--)
            {
                var shot = shots[i];
                bool cancelled = State == null || State.IsDead || (!shot.Friendly && CurrentRoom.Cleared);
                if (cancelled)
                {
                    RecycleShot(i);
                    continue;
                }
                shot.Age += dt;
                shot.Life -= dt;
                if (shot.Life <= 0)
                {
                    if (!shot.Friendly) OnHostileShotEnd(shot, false, false);
                    RecycleShot(i);
                    continue;
                }
                if (shot.Curve != 0) shot.Velocity = Rotate(shot.Velocity, shot.Curve * dt);
                if (shot.Friendly && shot.Style == WeaponKind.HiveLauncher) GuideStinger(shot, dt);
                else if (!shot.Friendly) UpdateHostileShotMotion(shot, dt);
                if (shot.Spin != 0) shot.Sprite.transform.rotation = Quaternion.Euler(0, 0, shot.Age * shot.Spin);
                if (shot.Style == WeaponKind.SpinningTop) shot.Sprite.sprite = art.Frame("top-shot", Mathf.FloorToInt(shot.Age * 16));
                if (shot.Style == WeaponKind.MirrorDisc) shot.Sprite.sprite = art.Frame("shot-disc", Mathf.FloorToInt(shot.Age * 20));
                Vector2 old = shot.Position;
                Vector2 next = old + shot.Velocity * dt;
                bool distanceEnd = !shot.Friendly && !shot.PatternTriggered && shot.SplitDistance > 0
                    && shot.Travelled + Vector2.Distance(old, next) >= shot.SplitDistance;
                if (distanceEnd)
                {
                    // Split at the requested distance rather than beyond a nearby wall
                    // or across the player when a long frame crosses the threshold.
                    float remaining = Mathf.Max(0, shot.SplitDistance - shot.Travelled);
                    next = old + shot.Velocity.normalized * remaining;
                }
                shot.TrailTimer -= dt;
                if (shot.TrailTimer <= 0)
                {
                    ShotTrail(shot, old);
                    shot.TrailTimer = shot.Friendly ? .045f : .07f;
                }
                FindSolidContact(old, next, shot.Radius, out float nearest, out Vector2 solidNormal);
                int propIndex = -1;
                if (FindRoomPropContact(old, next, shot.Radius, out float propContact, out int candidateProp)
                    && propContact <= nearest + .00001f)
                {
                    nearest = propContact;
                    propIndex = candidateProp;
                }
                int targetIndex = -1;
                bool piercing = false, playerHit = false;
                if (shot.Friendly)
                {
                    for (int e = 0; e < enemies.Count; e++)
                    {
                        var enemy = enemies[e];
                        if (enemy.SpawnRemaining > 0 || (shot.HitEnemies != null && shot.HitEnemies.Contains(enemy))) continue;
                        if (!CombatGeometry.CircleHitFraction(old.x, old.y, next.x, next.y,
                            enemy.Position.x, enemy.Position.y, EnemyHitRadius(enemy) + shot.Radius, out float contact) || contact >= nearest) continue;
                        nearest = contact;
                        targetIndex = e;
                        propIndex = -1;
                    }
                    if (targetIndex >= 0)
                    {
                        if (shot.HitEnemies != null)
                        {
                            shot.HitEnemies.Add(enemies[targetIndex]);
                            piercing = --shot.Pierces > 0;
                        }
                        bool glove = shot.Style == WeaponKind.BoxingGlove;
                        if (glove)
                        {
                            var enemy = enemies[targetIndex];
                            if (enemy.Kind != 4 && IsEnemyDamageable(enemy))
                                enemy.Position = MoveEnemy(enemy, shot.Velocity.normalized * (enemy.Kind == 11 ? .3f : .85f), EnemyHitRadius(enemy));
                        }
                        DamageEnemyWithFeedback(targetIndex, shot.Damage, -shot.Velocity.normalized, shot.Color, shot.Heavy, !glove || enemies[targetIndex].Kind == 4);
                    }
                }
                else if (!RollInvulnerable && CombatGeometry.CircleHitFraction(old.x, old.y, next.x, next.y,
                    playerPosition.x, playerPosition.y, PlayerRadius + shot.Radius, out float playerContact) && playerContact < nearest)
                {
                    nearest = playerContact;
                    playerHit = true;
                    propIndex = -1;
                    HurtPlayer();
                }
                if (nearest <= 1)
                {
                    Vector2 contact = Vector2.Lerp(old, next, Mathf.Clamp01(nearest));
                    shot.Travelled += Vector2.Distance(old, contact);
                    shot.Position = contact;
                    bool solidHit = targetIndex < 0 && !playerHit && propIndex < 0;
                    if (propIndex >= 0) DamageRoomProp(propIndex, shot.Damage);
                    if (targetIndex < 0) Impact(contact + Vector2.up * .38f, solidHit ? solidNormal : -shot.Velocity.normalized, shot.Color, shot.Heavy);
                    if (solidHit && shot.Bounces > 0)
                    {
                        shot.Bounces--;
                        shot.Velocity = Vector2.Reflect(shot.Velocity, solidNormal);
                        next = contact + solidNormal * .025f;
                        if (shot.Spin == 0) shot.Sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shot.Velocity.y, shot.Velocity.x) * Mathf.Rad2Deg);
                    }
                    else
                    {
                        if (!piercing)
                        {
                            if (!shot.Friendly) OnHostileShotEnd(shot, solidHit || propIndex >= 0, playerHit);
                            RecycleShot(i);
                            continue;
                        }
                        // Check the remaining segment next frame before a piercing
                        // projectile is allowed to reach another enemy behind cover.
                        next = contact + shot.Velocity.normalized * .02f;
                    }
                }
                else
                {
                    shot.Travelled += Vector2.Distance(old, next);
                    if (distanceEnd) shot.Travelled = Mathf.Max(shot.Travelled, shot.SplitDistance);
                }
                shot.Position = next;
                if (!shot.Friendly && TryTriggerDistancePattern(shot))
                {
                    RecycleShot(i);
                    continue;
                }
                shot.Sprite.transform.position = Snap(next + Vector2.up * .38f);
                shot.Glow.transform.position = Snap(next + Vector2.up * .38f);
                shot.Glow.transform.rotation = Quaternion.identity;
            }
        }

        void UpdateHostileShotMotion(Shot shot, float dt)
        {
            float speed = shot.Velocity.magnitude;
            if (shot.Deceleration > 0)
            {
                speed = BulletPatternGenerator.SpeedAt(shot.Age, shot.InitialSpeed, shot.MinimumSpeed, shot.Deceleration);
                shot.Velocity = shot.Velocity.normalized * speed;
            }
            if (shot.HomingRemaining <= 0 || speed <= .001f) return;
            float window = Mathf.Min(dt, shot.HomingRemaining);
            shot.HomingRemaining = Mathf.Max(0, shot.HomingRemaining - dt);
            Vector2 direction = playerPosition - shot.Position;
            if (direction.sqrMagnitude < .0001f) return;
            float angle = Mathf.Atan2(shot.Velocity.y, shot.Velocity.x) * Mathf.Rad2Deg;
            float target = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float turned = BulletPatternGenerator.TurnTowardsDegrees(angle, target, Mathf.Max(0, shot.TurnRate) * window);
            shot.Velocity = Rotate(Vector2.right, turned) * speed;
            if (shot.Spin == 0)
                shot.Sprite.transform.rotation = Quaternion.Euler(0, 0, turned);
        }

        void GuideStinger(Shot shot, float dt)
        {
            Enemy target = null;
            float nearest = 64;
            Vector2 forward = shot.Velocity.normalized;
            foreach (var enemy in enemies)
            {
                if (!IsEnemyDamageable(enemy)) continue;
                Vector2 delta = enemy.Position - shot.Position;
                float distance = delta.sqrMagnitude;
                // Limited turning preserves an aimed shot and cannot track through cover.
                if (distance >= nearest || distance < .001f
                    || Vector2.Dot(forward, delta.normalized) < .05f || CoverBetween(shot.Position, enemy.Position, shot.Radius)) continue;
                nearest = distance;
                target = enemy;
            }
            if (target == null) return;
            float angle = Vector2.SignedAngle(forward, target.Position - shot.Position);
            shot.Velocity = Rotate(shot.Velocity, Mathf.Clamp(angle, -115 * dt, 115 * dt));
            shot.Sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(shot.Velocity.y, shot.Velocity.x) * Mathf.Rad2Deg);
        }

        // Swept cover tests are shared by rays and projectiles. Normals point back
        // into free space, including both axes when a disc strikes an exact corner.
        void FindSolidContact(Vector2 origin, Vector2 end, float radius, out float nearest, out Vector2 normal)
        {
            nearest = float.PositiveInfinity;
            normal = Vector2.zero;
            Vector2 delta = end - origin;
            float right = HalfWidth - radius, top = HalfHeight - radius;
            if (delta.x > 0 && end.x >= right) SelectSolidContact((right - origin.x) / delta.x, Vector2.left, ref nearest, ref normal);
            if (delta.x < 0 && end.x <= -right) SelectSolidContact((-right - origin.x) / delta.x, Vector2.right, ref nearest, ref normal);
            if (delta.y > 0 && end.y >= top) SelectSolidContact((top - origin.y) / delta.y, Vector2.down, ref nearest, ref normal);
            if (delta.y < 0 && end.y <= -top) SelectSolidContact((-top - origin.y) / delta.y, Vector2.up, ref nearest, ref normal);
            foreach (Rect block in obstacles)
            {
                float minX = block.xMin - radius, maxX = block.xMax + radius;
                float minY = block.yMin - radius, maxY = block.yMax + radius;
                if (!CombatGeometry.BoxHitFraction(origin.x, origin.y, end.x, end.y, minX, minY, maxX, maxY, out float contact)) continue;
                Vector2 point = Vector2.Lerp(origin, end, contact);
                float leftDistance = Mathf.Abs(point.x - minX), rightDistance = Mathf.Abs(point.x - maxX);
                float downDistance = Mathf.Abs(point.y - minY), upDistance = Mathf.Abs(point.y - maxY);
                float closest = Mathf.Min(Mathf.Min(leftDistance, rightDistance), Mathf.Min(downDistance, upDistance));
                Vector2 face = Vector2.zero;
                if (leftDistance <= closest + .0001f) face += Vector2.left;
                if (rightDistance <= closest + .0001f) face += Vector2.right;
                if (downDistance <= closest + .0001f) face += Vector2.down;
                if (upDistance <= closest + .0001f) face += Vector2.up;
                if (face.sqrMagnitude < .001f) face = -delta.normalized;
                SelectSolidContact(contact, face.normalized, ref nearest, ref normal);
            }
        }

        static void SelectSolidContact(float contact, Vector2 face, ref float nearest, ref Vector2 normal)
        {
            contact = Mathf.Max(0, contact);
            if (contact < nearest - .00001f) { nearest = contact; normal = face; }
            else if (Mathf.Abs(contact - nearest) <= .00001f) normal = (normal + face).normalized;
        }

        void RecycleShot(int index)
        {
            var shot = shots[index];
            shot.Sprite.gameObject.SetActive(false);
            shotPool.Push(shot.Sprite);
            shots.RemoveAt(index);
        }
    }
}
