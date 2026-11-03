using System;
using System.Collections.Generic;

namespace Ecos.Haunt
{
    public enum ShopItem { Heal, Damage, FireRate, MaxHealth }

    /// <summary>Run-only progression: purchases survive floors, never a new run.</summary>
    public sealed class RunState
    {
        public int Seed, Floor = 1, Health = 6, MaxHealth = 6, Coins, Damage = 1;
        public float ShotInterval = .22f;
        public bool BossDefeated;
        public bool IsDead => Health <= 0;
        readonly HashSet<ShopItem> sold = new HashSet<ShopItem>();

        public RunState(int seed) { Seed = seed; }

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
                default: throw new ArgumentOutOfRangeException(nameof(item));
            }
        }

        public bool Sold(ShopItem item) => sold.Contains(item);

        public bool TryBuy(ShopItem item, out string message)
        {
            if (!Enum.IsDefined(typeof(ShopItem), item)) { message = "Item desconhecido."; return false; }
            if (IsDead) { message = "A jornada terminou."; return false; }
            if (!BossDefeated) { message = "Derrote o chefe para abrir o mercado."; return false; }
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
                default:
                    int gain = Math.Min(2, 20 - MaxHealth);
                    MaxHealth += gain;
                    Health = Math.Min(MaxHealth, Health + gain);
                    message = "Coração de abóbora: +2 de vida máxima.";
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
