using UnityEngine;

namespace Ecos
{
    [CreateAssetMenu(menuName = "Ecos/Wildlife settings")]
    public sealed class WildlifeSettings : ScriptableObject
    {
        [Range(0, 24)] public int maxActive = 18;
        [Range(20, 48)] public float habitatCellSize = 28;
        [Range(50, 160)] public float populationRadius = 100;
        [Range(4, 16)] public float wanderRadius = 8;
        [Range(.1f, 1)] public float spawnInterval = .16f;
        [Range(.3f, 3)] public float refreshInterval = .8f;
        public static WildlifeSettings Defaults() => CreateInstance<WildlifeSettings>();
    }
}
