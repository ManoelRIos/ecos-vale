using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        const int AmmoDropChancePercent = 12;
        sealed class AmmoDropRecord { public Vector2 Position; public bool Collected; }
        sealed class AmmoPickup { public AmmoDropRecord Record; public SpriteRenderer Sprite, Shadow; }
        readonly Dictionary<int, List<AmmoDropRecord>> ammoDropsByRoom = new Dictionary<int, List<AmmoDropRecord>>();
        readonly List<AmmoPickup> ammoPickups = new List<AmmoPickup>();
        int ammoDropFloor = -1;
        float emptyWeaponNoticeCooldown;

        bool ReloadPressed() => !ControlsConsumed && !ConversationCapturesInput && !shopMenuOpen
            && ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame));

        void TickAmmunition(float dt)
        {
            emptyWeaponNoticeCooldown = Mathf.Max(0, emptyWeaponNoticeCooldown - dt);
            bool wasReloading = State.IsReloading;
            State.TickReload(dt);
            if (wasReloading && !State.IsReloading && !State.IsDead) PlaySound(SoundCue.ReloadEnd, .65f);
            UpdateAmmoPickups();
        }

        void HandleReloadInput()
        {
            if (!ReloadPressed() || !State.TryReload()) return;
            StopBeam(); PlaySound(SoundCue.ReloadStart, .65f);
        }

        void ReportEmptyWeapon()
        {
            if (State == null || State.IsReloading || State.Magazine > 0 || emptyWeaponNoticeCooldown > 0) return;
            emptyWeaponNoticeCooldown = .32f;
            PlaySound(SoundCue.DryFire, .5f);
        }

        bool HasFiniteWeapon()
        {
            foreach (WeaponKind weapon in Enum.GetValues(typeof(WeaponKind)))
                if (State.Owns(weapon) && weapon != State.StartingWeapon && weapon != WeaponKind.Pistol) return true;
            return false;
        }

        bool AmmoDropRoll(int ordinal)
        {
            uint value = unchecked((uint)State.Seed ^ (uint)ordinal * 0x9e3779b9u
                ^ (uint)State.Floor * 0x85ebca6bu ^ (uint)CurrentRoom.Id * 0xc2b2ae35u);
            value ^= value >> 16; value *= 0x7feb352du; value ^= value >> 15; value *= 0x846ca68bu; value ^= value >> 16;
            return value % 100 < AmmoDropChancePercent;
        }

        void TryDropAmmunition(Vector2 position)
        {
            if (State == null || State.IsDead || !Playing || InShop || CurrentRoom == null
                || ammoPickups.Count >= 12 || !HasFiniteWeapon() || !AmmoDropRoll(enemiesDefeated)) return;
            if (!ammoDropsByRoom.TryGetValue(CurrentRoom.Id, out var records))
            { records = new List<AmmoDropRecord>(); ammoDropsByRoom.Add(CurrentRoom.Id, records); }
            if (records.Count >= 32) return;
            var record = new AmmoDropRecord { Position = SafeSpawnPosition(position, .2f) };
            records.Add(record); CreateAmmoPickup(record);
        }

        void CreateAmmoPickup(AmmoDropRecord record)
        {
            var sprite = MakeSprite("ammo-cache", record.Position + Vector2.up * .22f, roomRoot.transform, Depth(record.Position.y) - 2);
            sprite.transform.localScale = Vector3.one * .5f;
            var dropShadow = MakeSprite("shadow", record.Position, roomRoot.transform, -60);
            dropShadow.transform.localScale = new Vector3(.85f,.4f,1);
            dropShadow.color = new Color(1,1,1,.6f);
            ammoPickups.Add(new AmmoPickup { Record = record, Sprite = sprite, Shadow = dropShadow });
            AttachFlickerLight(sprite.transform, new Color(.8f,1,.45f), 1.6f, .14f, .08f, -90);
        }

        void RestoreAmmunitionDrops()
        {
            if (State == null || InShop) return;
            if (ammoDropFloor != State.Floor)
            { ammoDropsByRoom.Clear(); ammoDropFloor = State.Floor; }
            if (ammoDropsByRoom.TryGetValue(CurrentRoom.Id, out var records))
                foreach (var record in records) if (!record.Collected) CreateAmmoPickup(record);
        }

        bool CollectAmmunition(out WeaponKind filled)
        {
            filled = State.Weapon;
            if (State.AddAmmo(filled, WeaponAmmo.Stats(filled).AmmoPickupAmount)) return true;
            foreach (WeaponKind weapon in Enum.GetValues(typeof(WeaponKind)))
                if (State.AddAmmo(weapon, WeaponAmmo.Stats(weapon).AmmoPickupAmount)) { filled = weapon; return true; }
            return false;
        }

        void UpdateAmmoPickups()
        {
            for (int i = ammoPickups.Count - 1; i >= 0; i--)
            {
                var pickup = ammoPickups[i];
                pickup.Sprite.transform.position = Snap(pickup.Record.Position + Vector2.up * (.22f + .04f * Mathf.Sin(simulationTime * 3 + i)));
                if (rollRemaining > 0 || Vector2.Distance(playerPosition, pickup.Record.Position) > .7f
                    || CoverBetween(playerPosition, pickup.Record.Position) || !CollectAmmunition(out WeaponKind filled)) continue;
                pickup.Record.Collected = true;
                Destroy(pickup.Sprite.gameObject); Destroy(pickup.Shadow.gameObject); ammoPickups.RemoveAt(i);
                PlaySound(SoundCue.AmmoPickup, .7f);
                Notify("MUNICAO · " + RunState.WeaponName(filled));
            }
        }

        void ClearAmmunitionVisuals()
        {
            foreach (var pickup in ammoPickups)
            { if (pickup.Sprite) Destroy(pickup.Sprite.gameObject); if (pickup.Shadow) Destroy(pickup.Shadow.gameObject); }
            ammoPickups.Clear();
        }

        void ResetAmmunitionDrops()
        {
            ClearAmmunitionVisuals(); ammoDropsByRoom.Clear(); ammoDropFloor = -1; emptyWeaponNoticeCooldown = 0;
        }
    }
}
