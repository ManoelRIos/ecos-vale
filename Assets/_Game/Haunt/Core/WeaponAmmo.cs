using System;

namespace Ecos.Haunt
{
    /// <summary>Finite magazine and default shop-weapon reserve settings.</summary>
    public readonly struct WeaponAmmoStats
    {
        public readonly int MagazineSize, ReserveCapacity, InitialReserve, AmmoPickupAmount;
        public readonly float ReloadDuration;
        public WeaponAmmoStats(int magazineSize, float reloadDuration, int reserveCapacity, int initialReserve, int ammoPickupAmount)
        {
            MagazineSize = magazineSize; ReloadDuration = reloadDuration;
            ReserveCapacity = reserveCapacity; InitialReserve = initialReserve; AmmoPickupAmount = ammoPickupAmount;
        }
    }

    public static class WeaponAmmo
    {
        // RunState applies infinite reserve to the selected hero's starting weapon
        // and the revolver. Those weapons still use these finite magazine settings.
        public static WeaponAmmoStats Stats(WeaponKind weapon)
        {
            switch (weapon)
            {
                case WeaponKind.Pistol: return Make(8, .95f);
                case WeaponKind.Scattergun: return Make(5, 1.35f);
                case WeaponKind.Repeater: return Make(24, 1.25f);
                case WeaponKind.SoulCannon: return Make(3, 1.8f);
                case WeaponKind.Stapler: return Make(10, 1.05f);
                case WeaponKind.Banana: return Make(6, 1.1f);
                case WeaponKind.SpinningTop: return Make(4, 1.4f);
                case WeaponKind.HunterCarbine: return Make(12, .85f);
                case WeaponKind.VeilWand: return Make(6, 1.05f);
                case WeaponKind.BoneCaster: return Make(6, 1.25f);
                case WeaponKind.ArcCoil: return Make(30, 1.5f);
                case WeaponKind.MirrorDisc: return Make(8, 1.15f);
                case WeaponKind.RayLantern: return Make(40, 1.8f);
                case WeaponKind.BoneFlute: return Make(6, 1.35f);
                case WeaponKind.Umbrella: return Make(5, 1.25f);
                case WeaponKind.HiveLauncher: return Make(9, 1.15f);
                case WeaponKind.BoxingGlove: return Make(4, 1.45f);
                default: throw new ArgumentOutOfRangeException(nameof(weapon));
            }
        }

        static WeaponAmmoStats Make(int magazine, float reload)
            => new WeaponAmmoStats(magazine, reload, magazine * 4, magazine * 3, magazine);
    }
}
