using System;
using System.Collections.Generic;

namespace Ecos.Haunt
{
    public enum ShopItem { Heal, Damage, FireRate, MaxHealth, Scattergun, Repeater, SoulCannon, Stapler, Banana, SpinningTop }
    public enum CharacterKind { Hunter, Medium, Reaper, Alchemist }
    public enum WeaponKind { Pistol, Scattergun, Repeater, SoulCannon, Stapler, Banana, SpinningTop }

    /// <summary>Run-only progression: purchases survive floors, never a new run.</summary>
    public sealed class RunState
    {
        public int Seed, Floor = 1, Health = 6, MaxHealth = 6, Coins, Damage = 1;
        public float ShotInterval = .22f;
        public bool BossDefeated;
        public CharacterKind Character { get; }
        public WeaponKind Weapon { get; private set; }
        public float PowerCooldown { get; private set; }
        public float PowerDuration => Character == CharacterKind.Hunter ? 9f
            : Character == CharacterKind.Medium ? 7f : Character == CharacterKind.Reaper ? 8f : 18f;
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
                default: throw new ArgumentOutOfRangeException(nameof(weapon));
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
                Health = Math.Min(MaxHealth, Health + 2);
            }
            PowerCooldown = PowerDuration;
            return true;
        }
        public bool IsDead => Health <= 0;
        readonly HashSet<ShopItem> sold = new HashSet<ShopItem>();

        public RunState(int seed, CharacterKind character = CharacterKind.Hunter)
        {
            if (!Enum.IsDefined(typeof(CharacterKind), character)) throw new ArgumentOutOfRangeException(nameof(character));
            Seed = seed; Character = character;
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
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }

        public bool Sold(ShopItem item) => sold.Contains(item);

        public bool TryBuy(ShopItem item, out string message)
        {
            if (!Enum.IsDefined(typeof(ShopItem), item)) { message = "ITEM DESCONHECIDO."; return false; }
            if (IsDead) { message = "A JORNADA TERMINOU."; return false; }
            if (!BossDefeated) { message = "MERCADO FECHADO."; return false; }
            if ((int)item >= (int)ShopItem.Scattergun && Owns((WeaponKind)((int)item - 3))) { message = "ARMA JA ADQUIRIDA."; return false; }
            if (Sold(item)) { message = "ESGOTADO NESTA VISITA."; return false; }
            if (item == ShopItem.Heal && Health >= MaxHealth) { message = "VIDA COMPLETA."; return false; }
            if (item == ShopItem.Damage && Damage >= 10) { message = "DANO MAXIMO."; return false; }
            if (item == ShopItem.FireRate && ShotInterval <= .0701f) { message = "CADENCIA MAXIMA."; return false; }
            if (item == ShopItem.MaxHealth && MaxHealth >= 20) { message = "VIDA MAXIMA."; return false; }
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
                default:
                    Weapon = (WeaponKind)((int)item - 3);
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
        }
    }
}
