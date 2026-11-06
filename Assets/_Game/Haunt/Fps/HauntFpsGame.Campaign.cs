using System;
using System.Collections.Generic;
using UnityEngine;

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

        void FireCharacterWeapon(Ray ray)
        {
            Color color = CharacterColor(state.Character);
            if (state.Character == CharacterKind.Medium)
            {
                for (int i = -1; i <= 1; i++)
                    FireRay(new Ray(ray.origin, Quaternion.AngleAxis(i * 5, Vector3.up) * ray.direction), color, 1, false);
            }
            else FireRay(ray, color, state.Character == CharacterKind.Reaper ? 3 : 1, state.Character == CharacterKind.Alchemist);
        }

        void FireRay(Ray ray, Color color, int pierces, bool chain)
        {
            int mask = ~((1 << PlayerLayer) | (1 << ViewLayer));
            RaycastHit[] hits = Physics.RaycastAll(ray, FpsPrototypeRules.ShotRange, mask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            Vector3 end = ray.GetPoint(FpsPrototypeRules.ShotRange);
            var struck = new HashSet<HauntFpsEnemy>();
            foreach (RaycastHit hit in hits)
            {
                end = hit.point;
                var enemy = hit.collider.GetComponentInParent<HauntFpsEnemy>();
                var chief = hit.collider.GetComponentInParent<HauntFpsBoss>();
                var barrel = hit.collider.GetComponentInParent<HauntFpsBarrel>();
                if (enemy)
                {
                    if (enemy.IsDead || !struck.Add(enemy)) continue;
                    enemy.TakeDamage(state.Damage + state.ProjectileDamageBonus, hit.point, ray.direction * 4);
                    hitMarker = .12f; PlayCue(FpsCue.EnemyHit, hit.point);
                    if (chain) ChainLightning(enemy, color);
                    Burst(hit.point, color, 5, 2);
                    if (--pierces > 0) continue;
                }
                else if (chief)
                {
                    chief.TakeDamage(state.Damage + state.ProjectileDamageBonus, hit.point, ray.direction * 2);
                    hitMarker = .12f; Burst(hit.point, color, 6, 2.5f); PlayCue(FpsCue.EnemyHit, hit.point);
                }
                else
                {
                    if (barrel && !barrel.IsDestroyed) { barrel.TakeDamage(state.Damage, hit.point); hitMarker = .12f; }
                    Burst(end, new Color(1, .65f, .22f), 5, 1.8f);
                }
                break;
            }
            AddTrace(player.MuzzlePosition, end, color);
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
