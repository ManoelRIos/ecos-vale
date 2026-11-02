using UnityEngine;

namespace Ecos
{
    public enum ItemCategory
    {
        Resource,
        Herb,
        Artifact,
        Key,
        Quest
    }

    [CreateAssetMenu(menuName = "Ecos/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string id, displayName;
        [TextArea]
        public string description;
        public ItemCategory category;
        public int maxStack = 50;
    }
}
