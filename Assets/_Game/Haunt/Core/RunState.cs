using System;
using System.Collections.Generic;

namespace Ecos.Haunt
{
    public enum ShopItem { Heal, Damage, FireRate, MaxHealth, Scattergun, Repeater, SoulCannon, Stapler, Banana, SpinningTop, MirrorDisc, RayLantern, BoneFlute, Umbrella, HiveLauncher, BoxingGlove, GiantRounds, CharacterRelic, QuickBoots }
    public enum CharacterKind { Hunter, Medium, Reaper, Alchemist }
    public enum WeaponKind { Pistol, Scattergun, Repeater, SoulCannon, Stapler, Banana, SpinningTop, HunterCarbine, VeilWand, BoneCaster, ArcCoil, MirrorDisc, RayLantern, BoneFlute, Umbrella, HiveLauncher, BoxingGlove }

    /// <summary>Run-only progression: purchases survive floors, never a new run.</summary>
    public sealed class RunState
    {
        public int Seed, Floor = 1, Health = 6, MaxHealth = 6, Coins, Damage = 1;
        public float ShotInterval = .22f;
        public bool BossDefeated;
        public CharacterKind Character { get; }
        public WeaponKind StartingWeapon => StartingWeaponFor(Character);
        public WeaponKind Weapon { get; private set; }
        public float ProjectileScale { get; private set; } = 1;
        public float MoveSpeedMultiplier { get; private set; } = 1;
        public bool HasCharacterRelic { get; private set; }
        public int ProjectileDamageBonus => HasCharacterRelic
            && (Character == CharacterKind.Hunter || Character == CharacterKind.Reaper) ? 1 : 0;
        public string CharacterRelicName => Character == CharacterKind.Hunter ? "OLHO DA AURORA"
            : Character == CharacterKind.Medium ? "FITA DO VEU" : Character == CharacterKind.Reaper ? "DENTE LUNAR" : "CORACAO VOLTAICO";
        public string CharacterRelicDescription => Character == CharacterKind.Hunter ? "TIROS +1 DANO"
            : Character == CharacterKind.Medium ? "PODER RECARREGA 30% ANTES"
            : Character == CharacterKind.Reaper ? "OSSOS +1 DANO" : "ELIXIR CURA 3 / RECARGA -15%";
        public float PowerCooldown { get; private set; }
        public float PowerDuration => (Character == CharacterKind.Hunter ? 9f
            : Character == CharacterKind.Medium ? 7f : Character == CharacterKind.Reaper ? 8f : 18f)
            * (HasCharacterRelic && Character == CharacterKind.Medium ? .7f
                : HasCharacterRelic && Character == CharacterKind.Alchemist ? .85f : 1);
        public string PowerName => Character == CharacterKind.Hunter ? "PULSO DA AURORA"
            : Character == CharacterKind.Medium ? "LAMENTO ESPECTRAL"
            : Character == CharacterKind.Reaper ? "COLHEITA LUNAR" : "ELIXIR VITAL";
        readonly HashSet<WeaponKind> weapons = new HashSet<WeaponKind> { WeaponKind.Pistol };
        public bool Owns(WeaponKind weapon) => weapons.Contains(weapon);
        public bool Equip(WeaponKind weapon)
        {
            if (IsDead || !Owns(weapon)) return false;
            Weapon = weapon;
            return true;
        }
        public float WeaponInterval
        {
            get
            {
                switch (Weapon)
                {
                    case WeaponKind.Pistol: return ShotInterval;
                    case WeaponKind.Scattergun: return ShotInterval * 2.8f;
                    case WeaponKind.Repeater: return ShotInterval * .55f;
                    case WeaponKind.SoulCannon: return ShotInterval * 3.5f;
                    case WeaponKind.Stapler: return ShotInterval * 1.4f;
                    case WeaponKind.Banana: return ShotInterval * 2.2f;
                    case WeaponKind.SpinningTop: return ShotInterval * 3.5f;
                    case WeaponKind.HunterCarbine: return ShotInterval;
                    case WeaponKind.VeilWand: return ShotInterval * 1.4f;
                    case WeaponKind.BoneCaster: return ShotInterval * 1.8f;
                    case WeaponKind.ArcCoil: return ShotInterval * .65f;
                    case WeaponKind.MirrorDisc: return ShotInterval * 1.7f;
                    case WeaponKind.RayLantern: return ShotInterval * .6f;
                    case WeaponKind.BoneFlute: return ShotInterval * 2f;
                    case WeaponKind.Umbrella: return ShotInterval * 2.1f;
                    case WeaponKind.HiveLauncher: return ShotInterval * 1.15f;
                    case WeaponKind.BoxingGlove: return ShotInterval * 2.6f;
                    default: throw new ArgumentOutOfRangeException(nameof(Weapon));
                }
            }
        }
        public static string WeaponName(WeaponKind weapon)
        {
            switch (weapon)
            {
                case WeaponKind.Pistol: return "REVOLVER";
                case WeaponKind.Scattergun: return "BACAMARTE";
                case WeaponKind.Repeater: return "REPETIDORA";
                case WeaponKind.SoulCannon: return "CANHAO DAS ALMAS";
                case WeaponKind.Stapler: return "GRAMPEADOR";
                case WeaponKind.Banana: return "BANANA";
                case WeaponKind.SpinningTop: return "PIAO DO CAOS";
                case WeaponKind.HunterCarbine: return "CARABINA DA AURORA";
                case WeaponKind.VeilWand: return "VARINHA DO VEU";
                case WeaponKind.BoneCaster: return "CEIFADOR DE OSSOS";
                case WeaponKind.ArcCoil: return "BOBINA VOLTAICA";
                case WeaponKind.MirrorDisc: return "DISCO ESPELHO";
                case WeaponKind.RayLantern: return "LANTERNA DE RAIOS";
                case WeaponKind.BoneFlute: return "FLAUTA DE OSSOS";
                case WeaponKind.Umbrella: return "GUARDA-CHUVA";
                case WeaponKind.HiveLauncher: return "COLMEIA DE BOLSO";
                case WeaponKind.BoxingGlove: return "LUVA DE BOXE";
                default: throw new ArgumentOutOfRangeException(nameof(weapon));
            }
        }
        public static WeaponKind StartingWeaponFor(CharacterKind character)
        {
            switch (character)
            {
                case CharacterKind.Hunter: return WeaponKind.HunterCarbine;
                case CharacterKind.Medium: return WeaponKind.VeilWand;
                case CharacterKind.Reaper: return WeaponKind.BoneCaster;
                case CharacterKind.Alchemist: return WeaponKind.ArcCoil;
                default: throw new ArgumentOutOfRangeException(nameof(character));
            }
        }

        public static bool TryWeaponForItem(ShopItem item, out WeaponKind weapon)
        {
            switch (item)
            {
                case ShopItem.Scattergun: weapon = WeaponKind.Scattergun; return true;
                case ShopItem.Repeater: weapon = WeaponKind.Repeater; return true;
                case ShopItem.SoulCannon: weapon = WeaponKind.SoulCannon; return true;
                case ShopItem.Stapler: weapon = WeaponKind.Stapler; return true;
                case ShopItem.Banana: weapon = WeaponKind.Banana; return true;
                case ShopItem.SpinningTop: weapon = WeaponKind.SpinningTop; return true;
                case ShopItem.MirrorDisc: weapon = WeaponKind.MirrorDisc; return true;
                case ShopItem.RayLantern: weapon = WeaponKind.RayLantern; return true;
                case ShopItem.BoneFlute: weapon = WeaponKind.BoneFlute; return true;
                case ShopItem.Umbrella: weapon = WeaponKind.Umbrella; return true;
                case ShopItem.HiveLauncher: weapon = WeaponKind.HiveLauncher; return true;
                case ShopItem.BoxingGlove: weapon = WeaponKind.BoxingGlove; return true;
                default: weapon = default; return false;
            }
        }
        public void TickPower(float dt) { if (dt > 0 && !float.IsInfinity(dt)) PowerCooldown = Math.Max(0, PowerCooldown - dt); }
        public bool TryUsePower()
        {
            if (IsDead || PowerCooldown > 0) return false;
            // A rejected heal does not spend the power; the accepted heal and cooldown
            // are committed together so repeated input cannot grant free health.
            if (Character == CharacterKind.Alchemist)
            {
                if (Health >= MaxHealth) return false;
                Health = Math.Min(MaxHealth, Health + (HasCharacterRelic ? 3 : 2));
            }
            PowerCooldown = PowerDuration;
            return true;
        }
        public bool IsDead => Health <= 0;
        readonly HashSet<ShopItem> sold = new HashSet<ShopItem>();
        static readonly ShopItem[] WeaponCatalog =
        {
            ShopItem.Scattergun, ShopItem.Repeater, ShopItem.SoulCannon,
            ShopItem.Stapler, ShopItem.Banana, ShopItem.SpinningTop,
            ShopItem.MirrorDisc, ShopItem.RayLantern, ShopItem.BoneFlute,
            ShopItem.Umbrella, ShopItem.HiveLauncher, ShopItem.BoxingGlove
        };
        ShopItem[] shopWeapons;
        int stockedFloor = int.MinValue, stockedSeed;

        // The displayed cases and accepted purchases share one stable stock list.
        // Buying a weapon never rerolls its neighbour; a new floor refreshes stock.
        public ShopItem[] ShopWeapons
        {
            get { EnsureShopStock(); return (ShopItem[])shopWeapons.Clone(); }
        }

        public bool IsOffered(ShopItem item)
        {
            if (!Enum.IsDefined(typeof(ShopItem), item)) return false;
            if (!TryWeaponForItem(item, out _)) return true;
            EnsureShopStock();
            return Array.IndexOf(shopWeapons, item) >= 0;
        }

        void EnsureShopStock()
        {
            if (shopWeapons != null && stockedFloor == Floor && stockedSeed == Seed) return;
            stockedFloor = Floor; stockedSeed = Seed;
            var available = new List<ShopItem>();
            foreach (ShopItem item in WeaponCatalog)
                if (TryWeaponForItem(item, out WeaponKind weapon) && !Owns(weapon)) available.Add(item);
            uint random = unchecked((uint)Seed * 0x85ebca6bu ^ (uint)Floor * 0xc2b2ae35u ^ 0x71f36a9du);
            if (random == 0) random = 0x6d2b79f5u;
            int count = Math.Min(available.Count, 1 + StockNext(ref random, 2));
            shopWeapons = new ShopItem[count];
            for (int i = 0; i < count; i++)
            {
                int index = StockNext(ref random, available.Count);
                shopWeapons[i] = available[index];
                available.RemoveAt(index);
            }
        }

        static int StockNext(ref uint state, int maximum)
        {
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            return (int)(state % (uint)maximum);
        }

        public RunState(int seed, CharacterKind character = CharacterKind.Hunter)
        {
            if (!Enum.IsDefined(typeof(CharacterKind), character)) throw new ArgumentOutOfRangeException(nameof(character));
            Seed = seed; Character = character;
            Weapon = StartingWeapon;
            weapons.Add(Weapon);
        }

        public bool Hurt(int amount)
        {
            if (IsDead || amount <= 0) return false;
            Health = Math.Max(0, Health - amount);
            return true;
        }

        // Caller awards each combat room once, after marking DungeonRoom.Cleared.
        public void RewardRoom(bool boss)
        {
            if (IsDead || BossDefeated) return;
            int scaling = Math.Min(Math.Max(1, Floor), 100);
            int reward = boss ? 50 + (scaling - 1) * 5 : 12 + (scaling - 1) * 2;
            Coins = Math.Min(999999, Coins + reward);
            if (boss) BossDefeated = true;
        }

        public int Price(ShopItem item)
        {
            int scaling = (Math.Min(Math.Max(1, Floor), 20) - 1) * 3;
            switch (item)
            {
                case ShopItem.Heal: return 20 + scaling;
                case ShopItem.Damage: return 45 + scaling;
                case ShopItem.FireRate: return 35 + scaling;
                case ShopItem.MaxHealth: return 40 + scaling;
                case ShopItem.Scattergun: return 55 + scaling;
                case ShopItem.Repeater: return 65 + scaling;
                case ShopItem.SoulCannon: return 80 + scaling;
                case ShopItem.Stapler: return 55 + scaling;
                case ShopItem.Banana: return 65 + scaling;
                case ShopItem.SpinningTop: return 80 + scaling;
                case ShopItem.MirrorDisc: return 70 + scaling;
                case ShopItem.RayLantern: return 90 + scaling;
                case ShopItem.BoneFlute: return 75 + scaling;
                case ShopItem.Umbrella: return 65 + scaling;
                case ShopItem.HiveLauncher: return 85 + scaling;
                case ShopItem.BoxingGlove: return 75 + scaling;
                case ShopItem.GiantRounds: return 35 + scaling;
                case ShopItem.CharacterRelic: return 50 + scaling;
                case ShopItem.QuickBoots: return 30 + scaling;
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }

        public bool Sold(ShopItem item) => sold.Contains(item);

        public bool TryBuy(ShopItem item, out string message)
        {
            if (!Enum.IsDefined(typeof(ShopItem), item)) { message = "ITEM DESCONHECIDO."; return false; }
            if (IsDead) { message = "A JORNADA TERMINOU."; return false; }
            if (!BossDefeated) { message = "MERCADO FECHADO."; return false; }
            bool weaponItem = TryWeaponForItem(item, out WeaponKind purchasedWeapon);
            if (weaponItem && Owns(purchasedWeapon)) { message = "ARMA JA ADQUIRIDA."; return false; }
            if (weaponItem && !IsOffered(item)) { message = "ARMA FORA DESTA VISITA."; return false; }
            if (Sold(item)) { message = "ESGOTADO NESTA VISITA."; return false; }
            if (item == ShopItem.Heal && Health >= MaxHealth) { message = "VIDA COMPLETA."; return false; }
            if (item == ShopItem.Damage && Damage >= 10) { message = "DANO MAXIMO."; return false; }
            if (item == ShopItem.FireRate && ShotInterval <= .0701f) { message = "CADENCIA MAXIMA."; return false; }
            if (item == ShopItem.MaxHealth && MaxHealth >= 20) { message = "VIDA MAXIMA."; return false; }
            if (item == ShopItem.GiantRounds && ProjectileScale >= 1.5999f) { message = "BALAS NO TAMANHO MAXIMO."; return false; }
            if (item == ShopItem.CharacterRelic && HasCharacterRelic) { message = "RELIQUIA JA ADQUIRIDA."; return false; }
            if (item == ShopItem.QuickBoots && MoveSpeedMultiplier >= 1.2499f) { message = "PASSOS NO MAXIMO."; return false; }
            int price = Price(item);
            if (Coins < price) { message = "FALTAM ALMAS."; return false; }

            Coins -= price;
            sold.Add(item);
            switch (item)
            {
                case ShopItem.Heal:
                    Health = Math.Min(MaxHealth, Health + 3);
                    message = "VIDA RECUPERADA.";
                    break;
                case ShopItem.Damage:
                    Damage++;
                    message = "DANO +1.";
                    break;
                case ShopItem.FireRate:
                    ShotInterval = Math.Max(.07f, ShotInterval * .85f);
                    message = "DISPARO MAIS RAPIDO.";
                    break;
                case ShopItem.MaxHealth:
                    int gain = Math.Min(2, 20 - MaxHealth);
                    MaxHealth += gain;
                    Health = Math.Min(MaxHealth, Health + gain);
                    message = "VIDA MAXIMA +2.";
                    break;
                case ShopItem.GiantRounds:
                    ProjectileScale = Math.Min(1.6f, ProjectileScale + .3f);
                    message = "BALAS MAIORES.";
                    break;
                case ShopItem.CharacterRelic:
                    HasCharacterRelic = true;
                    message = CharacterRelicName + " ADQUIRIDO.";
                    break;
                case ShopItem.QuickBoots:
                    MoveSpeedMultiplier = Math.Min(1.25f, MoveSpeedMultiplier + .125f);
                    message = "PASSOS MAIS RAPIDOS.";
                    break;
                default:
                    Weapon = purchasedWeapon;
                    weapons.Add(Weapon);
                    message = WeaponName(Weapon) + " EQUIPADO.";
                    break;
            }
            return true;
        }

        public void NextFloor()
        {
            if (IsDead || !BossDefeated) return;
            if (Floor < int.MaxValue) Floor++;
            BossDefeated = false;
            sold.Clear();
            shopWeapons = null;
        }
    }
}
