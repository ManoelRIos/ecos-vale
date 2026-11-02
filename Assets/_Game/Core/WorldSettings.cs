using UnityEngine;

namespace Ecos
{
    [CreateAssetMenu(menuName = "Ecos/World settings")]
    public sealed class WorldSettings : ScriptableObject
    {
        public int seed = 71319;
        [Min(512)]
        public int worldSize = 1024;
        [Min(32)]
        public int chunkSize = 128;
        [Range(8, 64)]
        public int resolution = 24;
        [Range(1, 2)]
        public int viewRadius = 1;
        [Range(0, 768)]
        public int propsPerChunk = 280;
        [Range(1, 24)]
        public int propsPerFrame = 8;
        [Range(.5f, 8)]
        public float propBudgetMs = 2;
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
            new Color(.31f, .23f, .42f),
            new Color(.34f, .57f, .20f),
            new Color(.56f, .72f, .82f),
            new Color(.23f, .43f, .27f),
            new Color(.18f, .49f, .42f),
            new Color(.37f, .22f, .28f)
        };
        static readonly Color[] Foliage =
        {
            new Color(.40f, .23f, .57f), new Color(.42f, .65f, .22f),
            new Color(.17f, .43f, .47f), new Color(.34f, .52f, .24f),
            new Color(.15f, .62f, .42f), new Color(.46f, .25f, .24f)
        };
        static readonly Color[] Fog =
        {
            new Color(.31f, .24f, .44f), new Color(.45f, .58f, .47f),
            new Color(.44f, .61f, .75f), new Color(.29f, .43f, .35f),
            new Color(.29f, .51f, .52f), new Color(.40f, .25f, .34f)
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
                b.foliage = Foliage[i];
                b.fog = Fog[i];
                b.density = i == 1 ? .85f : 1;
                b.weather = i == 2 ? WeatherKind.Snow : i == 3 ? WeatherKind.Rain : i == 5 ? WeatherKind.Ash : i == 1 ? WeatherKind.Clear : i == 4 ? WeatherKind.Cloudy : WeatherKind.Fog;
                w.biomes[i] = b;
            }

            return w;
        }
    }
}
