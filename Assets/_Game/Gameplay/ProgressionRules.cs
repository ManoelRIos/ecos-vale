using System;
using System.Collections.Generic;

namespace Ecos
{
    public readonly struct ItemAmount
    {
        public readonly string id;
        public readonly int count;
        public ItemAmount(string id, int count) { this.id = id; this.count = count; }
    }

    /// <summary>Pure, authoritative progression. No scene, input, transport or Unity API dependency.</summary>
    public static class ProgressionRules
    {
        static readonly ItemAmount[] None = Array.Empty<ItemAmount>();
        static readonly ItemAmount[] CampCost = { new ItemAmount("wood", 4), new ItemAmount("stone", 3) };
        static readonly ItemAmount[] LensCost = { new ItemAmount("crystal", 2), new ItemAmount("herb", 3), new ItemAmount("stone", 2) };
        static readonly ItemAmount[] CacheReward = { new ItemAmount("crystal", 2), new ItemAmount("herb", 2), new ItemAmount("artifact", 1) };
        static readonly ItemAmount[] QuestReward = { new ItemAmount("artifact", 1) };
        static readonly string[] ShrineIds = { "shrine-0", "shrine-1", "shrine-2" };

        public static int Level(ProgressionState state) => 1 + Math.Max(0, state.experience) / 40;

        public static void GainExperience(ProgressionState state, int amount)
        {
            if (amount <= 0) return;
            state.experience = (int)Math.Min(1000000L, Math.Max(0L, state.experience) + amount);
            ApplyCapacity(state);
        }

        public static bool Discover(ProgressionState state, string id)
        {
            if (string.IsNullOrEmpty(id) || state.discovered.Contains(id)) return false;
            state.discovered.Add(id);
            GainExperience(state, 10);
            return true;
        }

        /// <summary>Simulates the complete exchange before touching the original inventory.</summary>
        public static bool TryTrade(InventoryState inventory, IReadOnlyList<ItemAmount> costs,
            IReadOnlyList<ItemAmount> rewards, out string message, Func<string, int> stackLimit = null)
        {
            message = "Troca inválida.";
            if (inventory == null || inventory.stacks == null || costs == null || rewards == null) return false;
            var trial = new InventoryState { capacity = inventory.capacity };
            foreach (var stack in inventory.stacks)
            {
                if (stack == null || string.IsNullOrEmpty(stack.id) || stack.count <= 0) return false;
                trial.stacks.Add(new ItemStack(stack.id, stack.count));
            }
            foreach (var cost in costs)
            {
                if (string.IsNullOrEmpty(cost.id) || cost.count <= 0) return false;
                if (!trial.Remove(cost.id, cost.count))
                {
                    message = "Faltam recursos. " + CostLabel(cost.id) + ": precisa de " + cost.count + ".";
                    return false;
                }
            }
            foreach (var reward in rewards)
            {
                if (string.IsNullOrEmpty(reward.id) || reward.count <= 0) return false;
                int limit = stackLimit == null ? 50 : Math.Max(1, Math.Min(50, stackLimit(reward.id)));
                if (!trial.Add(reward.id, reward.count, limit))
                {
                    message = "Inventário cheio. Libere espaço para receber todos os itens.";
                    return false;
                }
            }
            inventory.stacks = trial.stacks;
            message = null;
            return true;
        }

        public static bool BuildCamp(ProgressionState state, out string message)
        {
            if (state.campBuilt) { message = "A fogueira já está acesa. Use a bancada para preparar a lente."; return false; }
            if (!TryTrade(state.inventory, CampCost, None, out message)) return false;
            state.campBuilt = true;
            GainExperience(state, 15);
            message = "Acampamento restaurado! A bancada de ressonância está disponível. +15 experiência.";
            return true;
        }

        public static bool CraftLens(ProgressionState state, out string message)
        {
            if (!state.campBuilt) { message = "Acenda a fogueira antes de usar a bancada."; return false; }
            if (state.lensCrafted) { message = "A lente já está pronta. Use Q para revelar ecos próximos."; return false; }
            if (!TryTrade(state.inventory, LensCost, None, out message)) return false;
            state.lensCrafted = true;
            GainExperience(state, 20);
            message = "Lente criada! Use Q para revelar ecos. Baús selados agora podem ser abertos. +20 experiência.";
            return true;
        }

        public static string ShrineCost(string id)
        {
            switch (id)
            {
                case "shrine-0": return "1 cristal + 2 pedras";
                case "shrine-1": return "1 cristal + 2 ervas";
                case "shrine-2": return "1 cristal + 2 madeiras";
                default: return "Santuário desconhecido";
            }
        }

        public static bool Restore(ProgressionState state, string shrineId, out string message)
        {
            if (Array.IndexOf(ShrineIds, shrineId) < 0) { message = "Santuário desconhecido."; return false; }
            if (state.restored.Contains(shrineId)) { message = "O eco já foi restaurado."; return false; }
            if (!state.lensCrafted) { message = "Prepare a lente na bancada antes de restaurar os ecos."; return false; }
            string resource = shrineId == "shrine-0" ? "stone" : shrineId == "shrine-1" ? "herb" : "wood";
            var costs = new[] { new ItemAmount("crystal", 1), new ItemAmount(resource, 2) };
            if (!TryTrade(state.inventory, costs, None, out message)) return false;
            state.restored.Add(shrineId);
            GainExperience(state, 25);
            message = "Eco restaurado! " + CompletedShrines(state) + "/3 vozes recuperadas. +25 experiência.";
            return true;
        }

        public static bool OpenCache(ProgressionState state, string id, out string message, Func<string, int> stackLimit = null)
        {
            if (string.IsNullOrEmpty(id)) { message = "Baú inválido."; return false; }
            if (state.openedCaches.Contains(id)) { message = "Este baú já foi aberto."; return false; }
            if (!state.lensCrafted) { message = "Este selo responde à lente de ressonância. Prepare-a na bancada."; return false; }
            if (!TryTrade(state.inventory, None, CacheReward, out message, stackLimit)) return false;
            state.openedCaches.Add(id);
            GainExperience(state, 12);
            message = "Tesouro encontrado: 2 cristais, 2 ervas e 1 artefato. +12 experiência.";
            return true;
        }

        public static bool ReadClue(ProgressionState state, string id, out string message)
        {
            if (string.IsNullOrEmpty(id)) { message = "Fragmento inválido."; return false; }
            if (state.readClues.Contains(id)) { message = "Este fragmento já está registrado no diário."; return false; }
            state.readClues.Add(id);
            GainExperience(state, 8);
            message = "Fragmento registrado. +8 experiência.";
            return true;
        }

        public static int CompletedShrines(ProgressionState state)
        {
            int count = 0;
            foreach (string id in ShrineIds) if (state.restored.Contains(id)) count++;
            return count;
        }

        public static bool CompleteQuest(ProgressionState state)
        {
            if (state.rewarded || CompletedShrines(state) != 3) return false;
            if (!TryTrade(state.inventory, None, QuestReward, out _)) return false;
            state.rewarded = true;
            GainExperience(state, 40);
            return true;
        }

        public static bool ObserveSpecies(ProgressionState state, string species, out string message)
        {
            if (!CreatureCatalog.IsSpecies(species)) { message = "Espécie desconhecida."; return false; }
            if (state.observedSpecies.Contains(species))
            {
                message = CreatureCatalog.Name(species) + " já está no seu guia de campo.";
                return false;
            }
            state.observedSpecies.Add(species);
            GainExperience(state, 12);
            message = CreatureCatalog.Name(species) + " registrado no guia! +12 experiência.\n" + CreatureCatalog.Description(species);
            return true;
        }

        public static int ObservedSpeciesCount(ProgressionState state)
        {
            int count = 0;
            foreach (string id in CreatureCatalog.SpeciesIds) if (state.observedSpecies.Contains(id)) count++;
            return count;
        }

        public static bool CompleteFieldGuide(ProgressionState state)
        {
            if (state.guideRewarded || ObservedSpeciesCount(state) != CreatureCatalog.SpeciesIds.Length) return false;
            if (!TryTrade(state.inventory, None, QuestReward, out _)) return false;
            state.guideRewarded = true;
            GainExperience(state, 25);
            return true;
        }

        /// <summary>Preserves v1 discoveries/restorations; upgraded players keep access to their progress.</summary>
        public static void MigrateLegacy(ProgressionState state)
        {
            if (state.openedCaches == null) state.openedCaches = new List<string>();
            if (state.readClues == null) state.readClues = new List<string>();
            if (state.restored.Count > 0 || state.rewarded)
            {
                state.campBuilt = true;
                state.lensCrafted = true;
            }
            long earned = (long)state.discovered.Count * 10 + CompletedShrines(state) * 25 + (state.rewarded ? 40 : 0);
            state.experience = (int)Math.Min(1000000L, Math.Max(state.experience, earned));
            ApplyCapacity(state);
        }

        static void ApplyCapacity(ProgressionState state)
        {
            // Never shrink an existing/custom inventory or remove occupied slots.
            state.inventory.capacity = Math.Max(state.inventory.capacity, Math.Min(20, 12 + Level(state) - 1));
        }

        static string CostLabel(string id)
        {
            switch (id)
            {
                case "wood": return "madeira";
                case "stone": return "pedra";
                case "crystal": return "cristal";
                case "herb": return "erva";
                default: return id;
            }
        }
    }
}
