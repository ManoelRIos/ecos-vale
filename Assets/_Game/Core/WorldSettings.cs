using UnityEngine;

namespace Ecos
{
    [CreateAssetMenu(menuName = "Ecos/World settings")]
    public sealed class WorldSettings : ScriptableObject
    {
        public int seed = 71319;
        [Min(512)]
        public int worldSize = 8192;
        [Min(32)]
        public int chunkSize = 128;
        [Range(8, 64)]
        public int resolution = 24;
        [Range(1, 4)]
        public int viewRadius = 2;
        [Range(0, 50)]
        public int propsPerChunk = 32;
        [Min(60)]
        public float daySeconds = 900;
        public BiomeDefinition[] biomes;
        public static readonly string[] Names =
        {
            "Floresta Assombrada",
            "Planícies do Sol Esquecido",
            "Montanhas Congeladas",
            "Pântano Amaldiçoado",
            "Floresta Encantada",
            "Terras Vulcânicas"
        };
        public static readonly Color[] Colors =
        {
            new Color(.20f, .17f, .28f),
            new Color(.39f, .47f, .19f),
            new Color(.72f, .81f, .87f),
            new Color(.17f, .31f, .22f),
            new Color(.16f, .36f, .36f),
            new Color(.23f, .17f, .18f)
        };
        public static WorldSettings Defaults()
        {
            var w = CreateInstance<WorldSettings>();
            w.biomes = new BiomeDefinition[6];
            for (int i = 0; i < 6; i++)
            {
                var b = CreateInstance<BiomeDefinition>();
                b.index = i;
                b.displayName = Names[i];
                b.ground = Colors[i];
                b.foliage = Color.Lerp(Colors[i], new Color(.12f, .2f, .19f), .35f);
                b.fog = Color.Lerp(Colors[i], new Color(.15f, .12f, .23f), .5f);
                b.weather = i == 2 ? WeatherKind.Snow : i == 3 ? WeatherKind.Rain : i == 5 ? WeatherKind.Ash : i == 1 ? WeatherKind.Clear : i == 4 ? WeatherKind.Cloudy : WeatherKind.Fog;
                w.biomes[i] = b;
            }

            return w;
        }
    }
}
