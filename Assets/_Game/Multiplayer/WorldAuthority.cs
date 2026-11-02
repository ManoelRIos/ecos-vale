using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public enum WorldCommandKind
    {
        Collect,
        Restore
    }

    public readonly struct WorldCommand
    {
        public readonly WorldCommandKind kind;
        public readonly string targetId;
        public WorldCommand(WorldCommandKind kind, string id)
        {
            this.kind = kind;
            targetId = id;
        }
    }

    public interface IWorldAuthority
    {
        bool Execute(WorldCommand command, Vector3 actor, out string message);
    }

    // Single-player server model. A future NGO server must resolve actor position from
    // its authenticated sender, never from a client's supplied position or item count.
    public sealed class OfflineWorldAuthority : IWorldAuthority
    {
        readonly SaveData state;
        readonly Dictionary<string, Interactable> targets;
        public OfflineWorldAuthority(SaveData s, Dictionary<string, Interactable> registry)
        {
            state = s;
            targets = registry;
        }

        public bool Execute(WorldCommand cmd, Vector3 actor, out string message)
        {
            message = "Alvo indisponível.";
            if (!targets.TryGetValue(cmd.targetId, out var t) || !t || Vector3.Distance(actor, t.transform.position) > 4.5f)
                return false;
            if (cmd.kind == WorldCommandKind.Collect && !t.shrine)
            {
                if (state.collected.Contains(t.id))
                {
                    message = "Já coletado.";
                    return false;
                }

                if (!state.inventory.Add(t.itemId, t.amount, ItemCatalog.Limit(t.itemId)))
                {
                    message = "Inventário cheio.";
                    return false;
                }

                state.collected.Add(t.id);
                message = "Coletado: " + t.itemId;
                t.gameObject.SetActive(false);
                return true;
            }

            if (cmd.kind == WorldCommandKind.Restore && t.shrine)
            {
                if (state.restored.Contains(t.id))
                {
                    message = "O eco já foi restaurado.";
                    return false;
                }

                if (!state.inventory.Remove("crystal", 1))
                {
                    message = "Encontre um cristal para restaurar este santuário.";
                    return false;
                }

                state.restored.Add(t.id);
                message = "Eco restaurado!";
                return true;
            }

            return false;
        }
    }
}
