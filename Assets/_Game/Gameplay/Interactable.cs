using UnityEngine;

namespace Ecos
{
    public enum InteractionKind { Pickup, Shrine, Camp, Workbench, Chest, Journal, Observe }

    public sealed class Interactable : MonoBehaviour
    {
        public string id, title, itemId = "crystal";
        public bool shrine;
        public int amount = 1;
        public InteractionKind kind;
        [TextArea] public string loreText;
        public InteractionKind EffectiveKind => shrine ? InteractionKind.Shrine : kind;
        public WorldCommandKind CommandKind
        {
            get
            {
                switch (EffectiveKind)
                {
                    case InteractionKind.Shrine: return WorldCommandKind.Restore;
                    case InteractionKind.Camp: return WorldCommandKind.BuildCamp;
                    case InteractionKind.Workbench: return WorldCommandKind.CraftLens;
                    case InteractionKind.Chest: return WorldCommandKind.OpenCache;
                    case InteractionKind.Journal: return WorldCommandKind.ReadClue;
                    case InteractionKind.Observe: return WorldCommandKind.Observe;
                    default: return WorldCommandKind.Collect;
                }
            }
        }

        public string Prompt(SaveData state)
        {
            if (state == null) return title;
            switch (EffectiveKind)
            {
                case InteractionKind.Shrine:
                    return state.restored.Contains(id) ? "Eco restaurado" : !state.lensCrafted
                        ? "Restaurar — prepare a lente na bancada" : "Restaurar — " + ProgressionRules.ShrineCost(id);
                case InteractionKind.Camp:
                    return state.campBuilt ? "Acampamento restaurado" : "Acender fogueira — 4 madeiras + 3 pedras";
                case InteractionKind.Workbench:
                    return !state.campBuilt ? "Bancada — acenda a fogueira primeiro" : state.lensCrafted
                        ? "Lente pronta — use Q para revelar ecos" : "Criar lente — 2 cristais + 3 ervas + 2 pedras";
                case InteractionKind.Chest:
                    return state.openedCaches.Contains(id) ? "Baú aberto" : state.lensCrafted ? "Abrir baú selado" : "Baú selado — precisa da lente";
                case InteractionKind.Journal:
                    return state.readClues.Contains(id) ? "Reler fragmento" : "Ler fragmento antigo";
                case InteractionKind.Observe:
                    return state.observedSpecies.Contains(itemId) ? title + " · espécie registrada" : "Observar " + title + " · aproxime-se agachado";
                default:
                    return "Coletar " + ItemCatalog.Name(itemId) + (amount > 1 ? " × " + amount : "");
            }
        }

        public string Description(SaveData state)
        {
            if (EffectiveKind == InteractionKind.Journal && !string.IsNullOrEmpty(loreText)) return loreText;
            if (EffectiveKind == InteractionKind.Observe) return CreatureCatalog.Description(itemId);
            return Prompt(state);
        }

        void Update()
        {
            if (EffectiveKind == InteractionKind.Pickup)
                transform.Rotate(0, 30 * Time.deltaTime, 0, Space.World);
        }
    }
}
