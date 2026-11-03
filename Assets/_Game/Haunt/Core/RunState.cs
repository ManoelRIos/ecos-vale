using System;
using System.Collections.Generic;

namespace Ecos.Haunt
{
    public enum ShopItem { Heal, Damage, FireRate, MaxHealth, Scattergun, Repeater, SoulCannon }
    public enum CharacterKind { Hunter, Medium, Reaper, Alchemist }
    public enum WeaponKind { Pistol, Scattergun, Repeater, SoulCannon }

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
        public float WeaponInterval => ShotInterval * (Weapon == WeaponKind.Scattergun ? 2.8f : Weapon == WeaponKind.Repeater ? .55f : Weapon == WeaponKind.SoulCannon ? 3.5f : 1f);
        public static string WeaponName(WeaponKind weapon) => weapon == WeaponKind.Pistol ? "REVÓLVER" : weapon == WeaponKind.Scattergun ? "BACAMARTE" : weapon == WeaponKind.Repeater ? "REPETIDORA" : "CANHÃO DAS ALMAS";
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
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }

        public bool Sold(ShopItem item) => sold.Contains(item);

        public bool TryBuy(ShopItem item, out string message)
        {
            if (!Enum.IsDefined(typeof(ShopItem), item)) { message = "Item desconhecido."; return false; }
            if (IsDead) { message = "A jornada terminou."; return false; }
            if (!BossDefeated) { message = "Derrote o chefe para abrir o mercado."; return false; }
            if ((int)item >= (int)ShopItem.Scattergun && Owns((WeaponKind)((int)item - 3))) { message = "Arma já adquirida. Use 1–4 para equipar."; return false; }
            if (Sold(item)) { message = "Este item já foi comprado nesta visita."; return false; }
            if (item == ShopItem.Heal && Health >= MaxHealth) { message = "Sua vida já está completa."; return false; }
            if (item == ShopItem.Damage && Damage >= 10) { message = "Dano máximo atingido."; return false; }
            if (item == ShopItem.FireRate && ShotInterval <= .0701f) { message = "Cadência máxima atingida."; return false; }
            if (item == ShopItem.MaxHealth && MaxHealth >= 20) { message = "Vida máxima atingida."; return false; }
            int price = Price(item);
            if (Coins < price) { message = "Faltam moedas."; return false; }

            Coins -= price;
            sold.Add(item);
            switch (item)
            {
                case ShopItem.Heal:
                    Health = Math.Min(MaxHealth, Health + 3);
                    message = "Elixir: até 3 pontos de vida recuperados.";
                    break;
                case ShopItem.Damage:
                    Damage++;
                    message = "Munição maldita: +1 de dano.";
                    break;
                case ShopItem.FireRate:
                    ShotInterval = Math.Max(.07f, ShotInterval * .85f);
                    message = "Gatilho espectral: tiros 15% mais rápidos.";
                    break;
                case ShopItem.MaxHealth:
                    int gain = Math.Min(2, 20 - MaxHealth);
                    MaxHealth += gain;
                    Health = Math.Min(MaxHealth, Health + gain);
                    message = "Coração de abóbora: +2 de vida máxima.";
                    break;
                default:
                    Weapon = (WeaponKind)((int)item - 3);
                    weapons.Add(Weapon);
                    message = WeaponName(Weapon) + " adquirido e equipado. Troque com 1–4.";
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
