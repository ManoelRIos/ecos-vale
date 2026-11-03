using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        SpriteRenderer beamVisual;
        float beamPulse;

        // The input path calls this every frame, including release, so a held ray
        // follows the cursor smoothly while damage still respects the weapon rate.
        void UpdateWeaponTrigger(Vector2 aim, bool pressed, float dt)
        {
            if (!pressed || InShop || shopMenuOpen || Paused || State == null || State.IsDead || rollRemaining > 0)
            {
                StopBeam();
                return;
            }
            if (aim.sqrMagnitude < .0001f) { StopBeam(); return; }
            aim.Normalize();
            beamPulse = Mathf.Repeat(beamPulse + Mathf.Max(0, dt) * 35, Mathf.PI * 2);
            bool ray = State.Weapon == WeaponKind.ArcCoil || State.Weapon == WeaponKind.RayLantern;
            if (ray) UpdateBeam(aim, shotCooldown <= 0);
            else
            {
                StopBeam();
                if (shotCooldown > 0) return;
                FireWeapon(aim);
            }
            if (shotCooldown > 0) return;
            weaponKick = 1;
            muzzleTime = .045f;
            shotCooldown = State.WeaponInterval;
            Emit("glow", playerPosition + Vector2.up * .35f + aim * .65f, Vector2.zero,
                new Color(CharacterAccent.r, CharacterAccent.g, CharacterAccent.b, .3f), .08f, ray ? .45f : .6f, 0);
        }

        void StopBeam()
        {
            if (beamVisual != null) beamVisual.gameObject.SetActive(false);
        }

        void FireWeapon(Vector2 aim)
        {
            if (InShop || shopMenuOpen || Paused || State == null || State.IsDead) return;
            aim = aim.sqrMagnitude > .0001f ? aim.normalized : Vector2.right;
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
                        Fire(origin, Rotate(aim, pellet * 9) * 15, true, State.Damage);
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
                case WeaponKind.ArcCoil:
                case WeaponKind.RayLantern:
                    UpdateBeam(aim, true);
                    break;
                default:
                    Fire(origin, aim * (State.Weapon == WeaponKind.SoulCannon ? 11 : 17), true,
                        State.Damage * (State.Weapon == WeaponKind.SoulCannon ? 5 : 1));
                    break;
            }
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
                case WeaponKind.Stapler:
                    id = "staple-shot"; scale = .55f; shot.Color = new Color(.8f, .9f, 1);
                    shot.Radius = .07f; shot.Life = 4;
                    break;
                case WeaponKind.Banana:
                    id = "banana-shot"; scale = .5f; shot.Color = new Color(1, .85f, .2f);
                    shot.Curve = 52; shot.Spin = 540; shot.Radius = .1f; shot.Life = 2.8f;
                    break;
                case WeaponKind.SpinningTop:
                    id = "top-shot"; scale = .6f; shot.Color = new Color(.4f, 1, .95f);
                    shot.Spin = 960; shot.Radius = .14f; shot.Life = 2.8f;
                    shot.Pierces = 3; shot.HitEnemies = new HashSet<Enemy>();
                    break;
                case WeaponKind.MirrorDisc:
                    id = "shot-disc"; scale = .65f; shot.Color = new Color(.45f, .9f, 1);
                    shot.Spin = 780; shot.Radius = .12f; shot.Life = 5; shot.Bounces = 2;
                    break;
                case WeaponKind.BoneCaster:
                case WeaponKind.BoneFlute:
                    id = "shot-bone"; scale = .7f; shot.Color = new Color(.95f, .86f, .64f);
                    shot.Radius = .07f; shot.Life = 4;
                    break;
                default:
                    id = "shot-dot"; scale = style == WeaponKind.VeilWand ? .55f : .65f;
                    shot.Color = style == WeaponKind.VeilWand ? new Color(.8f, .55f, 1) : new Color(.65f, 1, .82f);
                    shot.Radius = .075f; shot.Life = 4;
                    break;
            }
            shot.Sprite.sprite = art.Get(id);
            shot.Sprite.color = Color.white;
            shot.Sprite.transform.localScale = Vector3.one * scale;
            shot.Glow.color = new Color(shot.Color.r, shot.Color.g, shot.Color.b, .65f);
            shot.Glow.transform.localScale = new Vector3(.75f / scale, .5f / scale, 1);
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
                light = MakeSprite("glow", origin, sprite.transform, -145);
                light.sharedMaterial = GlowMaterial;
            }
            bool heavy = friendly && State.Weapon == WeaponKind.SoulCannon;
            sprite.gameObject.SetActive(true);
            sprite.sprite = art.Get(friendly ? "bullet-player" : "bullet-enemy");
            sprite.color = heavy ? new Color(.6f, 1, .85f) : friendly ? CharacterAccent : Color.Lerp(HostileAccent, FloorColors[ThemeVariant], .22f);
            float scale = heavy ? 1.05f : friendly ? .75f : .85f;
            sprite.transform.localScale = Vector3.one * scale;
            sprite.transform.position = Snap(origin + Vector2.up * .38f);
            sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            light.gameObject.SetActive(true);
            light.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, friendly ? .7f : .5f);
            float haloSize = heavy ? 1.65f : friendly ? .85f : .75f;
            light.transform.rotation = Quaternion.identity;
            light.transform.position = Snap(origin);
            light.transform.localScale = new Vector3(haloSize / scale, haloSize * .7f / scale, 1);
            shots.Add(new Shot { Sprite = sprite, Glow = light, Position = origin, Velocity = velocity,
                Friendly = friendly, Damage = damage, Life = 6, Radius = heavy ? .16f : friendly ? .09f : .12f,
                Color = sprite.color, Heavy = heavy, TrailTimer = friendly ? 0 : .02f * (shots.Count % 4) });
            if (!friendly) Emit("muzzle", origin + Vector2.up * .35f, Vector2.zero, HostileAccent, .12f, .5f, 0);
        }

        void UpdateBeam(Vector2 aim, bool damageTick)
        {
            bool lantern = State.Weapon == WeaponKind.RayLantern;
            Color color = lantern ? new Color(.8f, .5f, 1) : new Color(.35f, .85f, 1);
            Vector2 origin = playerPosition;
            Vector2 end = origin + aim * (lantern ? 11 : 7);
            FindSolidContact(origin, end, .035f, out float nearest, out _);
            int targetIndex = -1;
            for (int e = 0; e < enemies.Count; e++)
            {
                var enemy = enemies[e];
                if (enemy.SpawnRemaining > 0) continue;
                if (!CombatGeometry.CircleHitFraction(origin.x, origin.y, end.x, end.y,
                    enemy.Position.x, enemy.Position.y, EnemyHitRadius(enemy) + .035f, out float contact) || contact >= nearest) continue;
                nearest = contact;
                targetIndex = e;
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
                beamVisual.sharedMaterial = GlowMaterial;
            }
            beamVisual.gameObject.SetActive(length > .01f);
            if (length > .01f)
            {
                beamVisual.transform.position = Snap((start + end) * .5f + Vector2.up * .38f);
                beamVisual.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
                Sprite sprite = beamVisual.sprite;
                float width = sprite.rect.width / sprite.pixelsPerUnit;
                float height = sprite.rect.height / sprite.pixelsPerUnit;
                float pulse = .85f + Mathf.Sin(beamPulse) * .15f;
                beamVisual.transform.localScale = new Vector3(length / width, (lantern ? .12f : .085f) * pulse / height, 1);
                beamVisual.color = new Color(color.r, color.g, color.b, .8f + pulse * .15f);
                muzzleTime = .035f;
            }
            if (!damageTick) return;
            if (targetIndex >= 0)
                DamageEnemy(targetIndex, State.Damage * (lantern ? 2 : 1), -aim, color, false);
            else if (nearest <= 1) Impact(end + Vector2.up * .38f, -aim, color, false);
            Emit("spark", end + Vector2.up * .38f, -aim * .6f, color, .12f, .2f, 0);
        }

        static float EnemyHitRadius(Enemy enemy)
        {
            return enemy.Kind == 4 ? .85f : enemy.Kind == 11 ? .65f : .36f;
        }

        void DamageEnemy(int index, int damage, Vector2 normal, Color color, bool heavy)
        {
            var enemy = enemies[index];
            enemy.Health -= damage;
            enemy.HurtFlash = .09f;
            Impact(enemy.Position + Vector2.up * .38f, normal, color, heavy);
            if (enemy.Health > 0) return;
            bool boss = enemy.Kind == 4;
            EnemyDeath(enemy);
            enemies.RemoveAt(index);
            enemiesDefeated++;
            shake = boss ? .3f : enemy.Kind == 11 ? .16f : .07f;
            if (enemies.Count == 0) CompleteRoom(boss);
        }

        void UpdateShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i];
                shot.Age += dt;
                if (shot.Curve != 0) shot.Velocity = Rotate(shot.Velocity, shot.Curve * dt);
                if (shot.Spin != 0) shot.Sprite.transform.rotation = Quaternion.Euler(0, 0, shot.Age * shot.Spin);
                if (shot.Style == WeaponKind.SpinningTop) shot.Sprite.sprite = art.Frame("top-shot", Mathf.FloorToInt(shot.Age * 16));
                if (shot.Style == WeaponKind.MirrorDisc) shot.Sprite.sprite = art.Frame("shot-disc", Mathf.FloorToInt(shot.Age * 20));
                Vector2 old = shot.Position;
                Vector2 next = old + shot.Velocity * dt;
                shot.Life -= dt;
                if (shot.Life <= 0 || (!shot.Friendly && CurrentRoom.Cleared))
                {
                    RecycleShot(i);
                    continue;
                }
                shot.TrailTimer -= dt;
                if (shot.TrailTimer <= 0)
                {
                    Emit(shot.Heavy ? "glow" : "spark", old + Vector2.up * .38f, -shot.Velocity.normalized * .35f,
                        new Color(shot.Color.r, shot.Color.g, shot.Color.b, .5f), shot.Heavy ? .25f : .13f, shot.Heavy ? .5f : .13f, 0);
                    shot.TrailTimer = shot.Friendly ? .055f : .16f;
                }
                FindSolidContact(old, next, shot.Radius, out float nearest, out Vector2 solidNormal);
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
                    }
                    if (targetIndex >= 0)
                    {
                        if (shot.HitEnemies != null)
                        {
                            shot.HitEnemies.Add(enemies[targetIndex]);
                            piercing = --shot.Pierces > 0;
                        }
                        DamageEnemy(targetIndex, shot.Damage, -shot.Velocity.normalized, shot.Color, shot.Heavy);
                    }
                }
                else if (rollRemaining <= 0 && CombatGeometry.CircleHitFraction(old.x, old.y, next.x, next.y,
                    playerPosition.x, playerPosition.y, PlayerRadius + shot.Radius, out float playerContact) && playerContact < nearest)
                {
                    nearest = playerContact;
                    playerHit = true;
                    HurtPlayer();
                }
                if (nearest <= 1)
                {
                    Vector2 contact = Vector2.Lerp(old, next, Mathf.Clamp01(nearest));
                    bool solidHit = targetIndex < 0 && !playerHit;
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
                        if (!piercing) { RecycleShot(i); continue; }
                        // Check the remaining segment next frame before a piercing
                        // projectile is allowed to reach another enemy behind cover.
                        next = contact + shot.Velocity.normalized * .02f;
                    }
                }
                shot.Position = next;
                shot.Sprite.transform.position = Snap(next + Vector2.up * .38f);
                shot.Glow.transform.position = Snap(next);
                shot.Glow.transform.rotation = Quaternion.identity;
            }
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
