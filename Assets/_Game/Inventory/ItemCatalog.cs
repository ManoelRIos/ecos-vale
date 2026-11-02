using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public static class ItemCatalog
    {
        static Dictionary<string, ItemDefinition> definitions;
        public static ItemDefinition Find(string id)
        {
            if (definitions == null)
            {
                definitions = new Dictionary<string, ItemDefinition>();
                foreach (var d in Resources.LoadAll<ItemDefinition>(""))
                    if (!string.IsNullOrEmpty(d.id))
                        definitions[d.id] = d;
            }

            return definitions.TryGetValue(id, out var item) ? item : null;
        }

        public static int Limit(string id)
        {
            var d = Find(id);
            return d ? Mathf.Clamp(d.maxStack, 1, 50) : 50;
        }

        public static string Name(string id)
        {
            var d = Find(id);
            return d ? d.displayName : id;
        }

        public static string Description(string id)
        {
            var d = Find(id);
            return d ? d.description : "Recurso do vale.";
        }
    }
}
