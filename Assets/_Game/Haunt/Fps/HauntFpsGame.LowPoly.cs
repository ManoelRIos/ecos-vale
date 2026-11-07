using System;
using System.Collections.Generic;
using Ecos.Haunt.LowPoly;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt.Fps
{
    public sealed partial class HauntFpsGame
    {
        bool lowPolyShopOpen, lowPolyMapOpen;
        int lowPolyShopSelection;
        float lowPolyDoorCooldown, lowPolyClock;
        GameObject lowPolyShopRoot;
        readonly Dictionary<int, HashSet<Vector3>> lowPolyBrokenBarrels = new Dictionary<int, HashSet<Vector3>>();
        sealed class AmmoDrop { public Vector3 Position; public bool Collected; }
        readonly Dictionary<int, List<AmmoDrop>> lowPolyDrops = new Dictionary<int, List<AmmoDrop>>();
        readonly HashSet<int> lowPolyVisited = new HashSet<int>();
        readonly Dictionary<AmmoDrop, Transform> lowPolyDropVisuals = new Dictionary<AmmoDrop, Transform>();
        static readonly Vector3 ShopPosition = new Vector3(7, 0, 8.5f);

        void BeginLowPolyRoom()
        {
            if (!lowPolyMode || LowPolyRoom == null) return;
            lowPolyVisited.Add(LowPolyRoom.Id);
            world.SetRoomDoorsOpen(LowPolyRoom.Cleared);
            if (LowPolyRoom.Cleared)
            {
                if (LowPolyRoom.Kind == RoomKind.Boss && state.BossDefeated)
                { bossSpawned = true; exitReady = true; world.SetExitOpen(true); CreateLowPolyShop(); }
                else Notify(LowPolyRoom.Kind == RoomKind.Entrance ? "EXPLORE AS PORTAS. TAB MOSTRA O MAPA." : "SALA PURIFICADA", 2);
            }
            else if (LowPolyRoom.Kind == RoomKind.Boss) SpawnBoss();
            else StartWave(0);
        }

        void ClearLowPolyRoom()
        {
            if (LowPolyRoom == null || LowPolyRoom.Cleared) return;
            LowPolyRoom.Cleared = true; LowPolyRoom.WavesCleared = 2;
            state.RewardRoom(false); world.SetRoomDoorsOpen(true);
            Notify("SALA PURIFICADA. AS PORTAS ESTAO ABERTAS.", 2);
            PlayCue(FpsCue.ReloadEnd, PlayerPosition);
        }

        bool EnterLowPolyRoom(int roomId, Vector3 spawn)
        {
            if (!lowPolyMode || !CanSimulate || LowPolyRoom == null || !LowPolyRoom.Cleared
                || LowPolyFloor == null || !LowPolyRoom.Neighbors.Contains(roomId)) return false;
            bool third = player.IsThirdPerson;
            LowPolyRoom = LowPolyFloor.GetRoom(roomId);
            BuildRun(seed, true, third);
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = spawn; controller.enabled = true; Physics.SyncTransforms();
            Vector3 heading = new Vector3(-spawn.x, 0, -spawn.z);
            if (heading.sqrMagnitude > .001f) player.transform.rotation = Quaternion.LookRotation(heading);
            player.ResetView();
            lastPlayerPosition = PlayerPosition;
            mode = ScreenMode.Playing; BeginLowPolyRoom(); player.SuspendInput(); UpdateSuspension();
            return true;
        }

        void InteractLowPolyRoom()
        {
            if (!CanSimulate || lowPolyDoorCooldown > 0) return;
            if (state.BossDefeated && lowPolyShopRoot && Vector3.Distance(PlayerPosition, ShopPosition) < 3)
            {
                lowPolyShopOpen = true; lowPolyShopSelection = 0; UpdateSuspension(); return;
            }
            if (world.TryGetNearbyDoor(PlayerPosition, 2.6f, out int roomId, out Vector3 spawn)) EnterLowPolyRoom(roomId, spawn);
        }

        public void OnBarrelDestroyed(HauntFpsBarrel barrel)
        {
            if (!lowPolyMode || LowPolyRoom == null || !barrel) return;
            if (!lowPolyBrokenBarrels.TryGetValue(LowPolyRoom.Id, out var positions))
                lowPolyBrokenBarrels.Add(LowPolyRoom.Id, positions = new HashSet<Vector3>());
            positions.Add(barrel.transform.position);
        }

        void UpdateLowPolyWeaponSelection()
        {
            var keyboard = Keyboard.current; var pad = Gamepad.current;
            float scroll = Mouse.current == null ? 0 : Mouse.current.scroll.ReadValue().y;
            int direction = scroll > .1f ? 1 : scroll < -.1f ? -1 : 0;
            if (pad != null && pad.dpad.right.wasPressedThisFrame) direction = 1;
            if (pad != null && pad.dpad.left.wasPressedThisFrame) direction = -1;
            if (keyboard != null && keyboard.digit2Key.wasPressedThisFrame) direction = 1;
            if (keyboard != null && keyboard.digit1Key.wasPressedThisFrame) direction = -1;
            if (direction == 0) return;
            var owned = new List<WeaponKind>();
            foreach (WeaponKind kind in Enum.GetValues(typeof(WeaponKind))) if (state.Owns(kind)) owned.Add(kind);
            int index = owned.IndexOf(state.Weapon);
            WeaponKind next = owned[(index + direction + owned.Count) % owned.Count];
            if (next != state.Weapon && state.Equip(next)) { player.SuspendInput(); Notify(RunState.WeaponName(next), 1.3f); }
        }

        void DropLowPolyAmmo(HauntFpsEnemy enemy)
        {
            if (LowPolyRoom == null || !enemy) return;
            uint hash = unchecked((uint)seed ^ (uint)(kills * 73856093) ^ (uint)(LowPolyRoom.Id * 19349663));
            hash ^= hash >> 13;
            if (hash % 100 >= 12) return;
            bool finite = false;
            foreach (WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))
                if (state.Owns(kind) && kind != WeaponKind.Pistol && kind != state.StartingWeapon) { finite = true; break; }
            if (!finite) return;
            if (!lowPolyDrops.TryGetValue(LowPolyRoom.Id, out var drops)) lowPolyDrops.Add(LowPolyRoom.Id, drops = new List<AmmoDrop>());
            if (drops.Count >= 24) return;
            var drop = new AmmoDrop { Position = new Vector3(enemy.transform.position.x, .2f, enemy.transform.position.z) };
            drops.Add(drop); SpawnLowPolyDrop(drop);
        }

        void RestoreLowPolyDrops()
        {
            if (LowPolyRoom != null && lowPolyDrops.TryGetValue(LowPolyRoom.Id, out var drops))
                foreach (var drop in drops) if (!drop.Collected) SpawnLowPolyDrop(drop);
        }

        void SpawnLowPolyDrop(AmmoDrop drop)
        {
            Material material = PixelMaterial("municao lowpoly", new Color(.3f, .86f, .72f));
            material.SetColor("_EmissionColor", new Color(.12f, .4f, .3f));
            var obj = polygons.Part("Municao", drop.Position, new Vector3(.4f, .3f, .32f), material, runRoot.transform, false);
            lowPolyDropVisuals.Add(drop, obj.transform);
        }

        void TickLowPolyDrops(float dt)
        {
            lowPolyClock += dt;
            foreach (var pair in lowPolyDropVisuals)
            {
                var drop = pair.Key; var visual = pair.Value;
                if (drop.Collected || !visual) continue;
                visual.position = drop.Position + Vector3.up * (Mathf.Sin(lowPolyClock * 3) * .06f);
                visual.localRotation = Quaternion.Euler(0, lowPolyClock * 35, 0);
                if (Vector3.Distance(PlayerPosition + Vector3.up * .3f, drop.Position) > 1.3f) continue;
                bool accepted = state.AddAmmo(state.Weapon, WeaponAmmo.Stats(state.Weapon).AmmoPickupAmount);
                if (!accepted)
                    foreach (WeaponKind kind in Enum.GetValues(typeof(WeaponKind)))
                        if (state.AddAmmo(kind, WeaponAmmo.Stats(kind).AmmoPickupAmount)) { accepted = true; break; }
                if (!accepted) continue;
                drop.Collected = true; visual.gameObject.SetActive(false); Destroy(visual.gameObject);
                PlayCue(FpsCue.ReloadEnd, PlayerPosition); Notify("MUNICAO RECOLHIDA", 1);
            }
        }

        void CreateLowPolyShop()
        {
            if (lowPolyShopRoot) return;
            lowPolyShopRoot = new GameObject("Mercado das Almas"); lowPolyShopRoot.transform.SetParent(runRoot.transform, false);
            lowPolyShopRoot.transform.localPosition = ShopPosition;
            Material stone = PixelMaterial("mercado pedra", new Color(.2f, .23f, .29f));
            Material coat = PixelMaterial("mercado coletor", new Color(.24f, .29f, .48f));
            Material bone = PixelMaterial("mercado mascara", new Color(.82f, .79f, .63f));
            Material black = PixelMaterial("mercado olhos", new Color(.045f, .035f, .07f));
            Transform parent = lowPolyShopRoot.transform;
            polygons.Part("Balcao", new Vector3(0, .65f, -.25f), new Vector3(3.2f, 1.3f, 1.1f), stone, parent, true);
            polygons.Part("Manto do coletor", new Vector3(0, 1.05f, .6f), new Vector3(.65f, 1.55f, .6f), coat, parent, false, LowPolyShape.TaperedPrism);
            polygons.Part("Capuz do coletor", new Vector3(0, 1.92f, .6f), Vector3.one * .7f, coat, parent, false, LowPolyShape.Sphere);
            polygons.Part("Mascara do coletor", new Vector3(0, 1.92f, .25f), new Vector3(.4f, .42f, .12f), bone, parent, false);
            for (int side = -1; side <= 1; side += 2)
                polygons.Part("Olho do coletor", new Vector3(side * .11f, 1.96f, .175f), new Vector3(.08f, .085f, .035f), black, parent);
            var stock = state.ShopWeapons;
            for (int i = 0; i < stock.Length; i++)
            {
                if (!RunState.TryWeaponForItem(stock[i], out WeaponKind weapon)) continue;
                var model = HauntLowPolyArsenal.Build(weapon, parent, this);
                model.transform.localPosition = new Vector3(i == 0 ? -.8f : .8f, 1.55f, -.4f);
                model.transform.localRotation = Quaternion.Euler(0, i == 0 ? 70 : -70, 0);
                model.transform.localScale = Vector3.one * .65f;
            }
        }

        List<ShopItem> LowPolyShopItems()
        {
            var items = new List<ShopItem>(state.ShopWeapons); items.Add(ShopItem.Heal); return items;
        }

        bool TryBuyLowPolySlot(int slot)
        {
            if (!lowPolyShopOpen || focusLost || paused || state == null || state.IsDead) return false;
            var items = LowPolyShopItems(); if (slot < 0 || slot >= items.Count) return false;
            bool bought = state.TryBuy(items[slot], out string message);
            Notify(message, 2.5f);
            if (bought) { player.SuspendInput(); PlayCue(FpsCue.ReloadEnd, PlayerPosition); }
            return bought;
        }

        void UpdateLowPolyShopInput(bool escape)
        {
            if (focusLost) return;
            var keyboard = Keyboard.current; var pad = Gamepad.current;
            if (escape || keyboard != null && keyboard.eKey.wasPressedThisFrame || pad != null && pad.buttonEast.wasPressedThisFrame)
            { lowPolyShopOpen = false; UpdateSuspension(); return; }
            int count = LowPolyShopItems().Count;
            if (pad != null && pad.dpad.right.wasPressedThisFrame) lowPolyShopSelection = (lowPolyShopSelection + 1) % count;
            if (pad != null && pad.dpad.left.wasPressedThisFrame) lowPolyShopSelection = (lowPolyShopSelection + count - 1) % count;
            if (pad != null && pad.buttonSouth.wasPressedThisFrame) TryBuyLowPolySlot(lowPolyShopSelection);
            if (keyboard != null && keyboard.digit1Key.wasPressedThisFrame) TryBuyLowPolySlot(0);
            if (keyboard != null && keyboard.digit2Key.wasPressedThisFrame) TryBuyLowPolySlot(1);
            if (keyboard != null && keyboard.digit3Key.wasPressedThisFrame) TryBuyLowPolySlot(2);
            noticeRemaining = Mathf.Max(0, noticeRemaining - Mathf.Min(Time.unscaledDeltaTime, .05f));
            if (sound) sound.Tick(Time.unscaledDeltaTime, false);
        }

        void FireLowPolyWeapon(Ray ray)
        {
            Color color = CharacterColor(state.Character);
            switch (state.Weapon)
            {
                case WeaponKind.Scattergun: LowPolyFan(ray, color, 7, 22, 1); break;
                case WeaponKind.VeilWand: LowPolyFan(ray, color, 3, 10, 1); break;
                case WeaponKind.Umbrella: LowPolyFan(ray, color, 5, 34, 1); break;
                case WeaponKind.HiveLauncher: LowPolyFan(ray, color, 5, 14, 1); break;
                case WeaponKind.Banana: LowPolyFan(ray, new Color(1, .8f, .23f), 3, 24, 1); break;
                case WeaponKind.Stapler: LowPolyFan(ray, color, 2, 2, 1); break;
                case WeaponKind.BoneCaster: FireRay(ray, color, 3, false); break;
                case WeaponKind.BoneFlute: LowPolyFan(ray, color, 3, 20, 3); break;
                case WeaponKind.ArcCoil: FireRay(ray, new Color(.3f, .85f, 1), 1, true); break;
                case WeaponKind.RayLantern: FireRay(ray, new Color(.55f, 1, .6f), 2, false); break;
                case WeaponKind.MirrorDisc: RicochetLowPoly(ray, color, 1); break;
                case WeaponKind.SpinningTop: RicochetLowPoly(ray, color, 2); break;
                case WeaponKind.SoulCannon:
                    FireRay(ray, new Color(.7f, .4f, 1), 1, false);
                    if (Physics.Raycast(ray, out RaycastHit hit, FpsPrototypeRules.ShotRange, ~((1 << PlayerLayer) | (1 << ViewLayer)), QueryTriggerInteraction.Ignore))
                        Explode(hit.point - ray.direction * .08f, 2.5f, 4);
                    break;
                case WeaponKind.BoxingGlove:
                    foreach (var enemy in enemies.ToArray())
                        if (enemy && !enemy.IsDead && Vector3.Distance(ray.origin, enemy.AimPosition) < 3.5f
                            && Vector3.Dot(ray.direction, (enemy.AimPosition - ray.origin).normalized) > .5f && BlastVisible(ray.origin, enemy.AimPosition))
                            enemy.TakeDamage(state.Damage + 3, enemy.AimPosition, ray.direction * 8);
                    if (boss && !boss.IsDead && Vector3.Distance(ray.origin, boss.AimPosition) < 4
                        && Vector3.Dot(ray.direction, (boss.AimPosition - ray.origin).normalized) > .5f && BlastVisible(ray.origin, boss.AimPosition))
                        boss.TakeDamage(state.Damage + 3, boss.AimPosition, ray.direction * 3);
                    AddTrace(player.MuzzlePosition, ray.GetPoint(2.5f), color); AddShake(.12f); break;
                default: FireRay(ray, color, 1, false); break;
            }
        }

        void LowPolyFan(Ray ray, Color color, int count, float arc, int pierces)
        {
            for (int i = 0; i < count; i++)
                FireRay(new Ray(ray.origin, Quaternion.AngleAxis(-arc * .5f + i * arc / (count - 1), Vector3.up) * ray.direction), color, pierces, false);
        }

        void RicochetLowPoly(Ray ray, Color color, int bounces)
        {
            for (int i = 0; i <= bounces; i++)
            {
                FireRay(ray, color, 1, false);
                if (i == bounces || !Physics.Raycast(ray, out RaycastHit hit, FpsPrototypeRules.ShotRange,
                    ~((1 << PlayerLayer) | (1 << ViewLayer)), QueryTriggerInteraction.Ignore)
                    || hit.collider.gameObject.layer != 0) break;
                Vector3 direction = Vector3.Reflect(ray.direction, hit.normal).normalized;
                ray = new Ray(hit.point + direction * .05f, direction);
            }
        }
    }
}
