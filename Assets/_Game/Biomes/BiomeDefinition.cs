using UnityEngine;

namespace Ecos
{
    public enum WeatherKind
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Snow,
        Blizzard,
        Fog,
        Ash
    }

    [CreateAssetMenu(menuName = "Ecos/Biome")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Range(0, 5)]
        public int index;
        public string displayName;
        public Color ground, foliage, fog;
        [Range(.1f, 2)]
        public float density = 1;
        public WeatherKind weather;
        [Range(0, 1)]
        public float weatherIntensity = .65f;
    }
}
