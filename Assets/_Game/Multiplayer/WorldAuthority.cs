using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public enum WorldCommandKind { Collect, Restore, BuildCamp, CraftLens, OpenCache, ReadClue }

    public readonly struct WorldCommand
    {
        public readonly WorldCommandKind kind;
        public readonly string targetId;
        public WorldCommand(WorldCommandKind kind, string id) { this.kind = kind; targetId = id; }
    }

    public interface IWorldAuthority
    {
        bool Execute(WorldCommand command, Vector3 actor, out string message);
    }

    // A future NGO gateway must resolve actor position from its authenticated sender.
    // Clients send only target IDs/commands; resource costs and loot belong to these server rules.
    public sealed class OfflineWorldAuthority : IWorldAuthority
    {
        readonly SaveData state;
        readonly Dictionary<string, Interactable> targets;
        public OfflineWorldAuthority(SaveData state, Dictionary<string, Interactable> registry)
        {
            this.state = state;
            targets = registry;
        }

        public bool Execute(WorldCommand command, Vector3 actor, out string message)
        {
            message = "Alvo indisponível.";
            if (string.IsNullOrEmpty(command.targetId) || !targets.TryGetValue(command.targetId, out var target)
                || !target || target.id != command.targetId || !target.isActiveAndEnabled || !target.gameObject.activeInHierarchy
                || float.IsNaN(actor.x) || float.IsNaN(actor.y) || float.IsNaN(actor.z)
                || float.IsInfinity(actor.x) || float.IsInfinity(actor.y) || float.IsInfinity(actor.z)
                || Vector3.Distance(actor, target.transform.position) > 4.5f)
                return false;
            if (command.kind != target.CommandKind)
            {
                message = "Ação incompatível com este objeto.";
                return false;
            }
            switch (command.kind)
            {
                case WorldCommandKind.Collect:
                    if (state.collected.Contains(target.id)) { message = "Já coletado."; return false; }
                    if (string.IsNullOrEmpty(target.itemId) || target.amount < 1) return false;
                    if (!state.inventory.Add(target.itemId, target.amount, ItemCatalog.Limit(target.itemId)))
                    {
                        message = "Inventário cheio.";
                        return false;
                    }
                    state.collected.Add(target.id);
                    message = "Coletado: " + ItemCatalog.Name(target.itemId) + " × " + target.amount;
                    target.gameObject.SetActive(false);
                    return true;
                case WorldCommandKind.Restore:
                    return ProgressionRules.Restore(state, target.id, out message);
                case WorldCommandKind.BuildCamp:
                    return ProgressionRules.BuildCamp(state, out message);
                case WorldCommandKind.CraftLens:
                    return ProgressionRules.CraftLens(state, out message);
                case WorldCommandKind.OpenCache:
                    return ProgressionRules.OpenCache(state, target.id, out message, ItemCatalog.Limit);
                case WorldCommandKind.ReadClue:
                    bool firstReading = ProgressionRules.ReadClue(state, target.id, out message);
                    if (!string.IsNullOrEmpty(target.loreText))
                        message = target.loreText + (firstReading ? "\nFragmento registrado. +8 experiência." : "");
                    return true;
                default:
                    return false;
            }
        }
    }
}
