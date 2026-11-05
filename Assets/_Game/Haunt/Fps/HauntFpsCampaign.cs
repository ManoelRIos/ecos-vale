using System;

namespace Ecos.Haunt.Fps
{
    /// <summary>Shared catalog for the five 3D themes and their encounters. No
    /// presentation state or global random generator is consumed by these rules.</summary>
    public static class HauntFpsCampaign
    {
        public const int ThemeCount = 5, BossCount = 7, EnemyCount = 17;
        static readonly string[] Names =
        {
            "JARDINS DA COLHEITA", "GALERIA DOS VEUS", "CATEDRAL DAS CINZAS",
            "NINHO DA PESTE", "ABISMO DOS SINOS"
        };
        static readonly FpsEnemyKind[][] Rosters =
        {
            new[] { FpsEnemyKind.Scarecrow, FpsEnemyKind.Skeleton, FpsEnemyKind.Pumpkin,
                FpsEnemyKind.FireBat, FpsEnemyKind.MushroomBrute },
            new[] { FpsEnemyKind.Ghost, FpsEnemyKind.Spider, FpsEnemyKind.Lantern,
                FpsEnemyKind.Cultist, FpsEnemyKind.StalkerShadow, FpsEnemyKind.Gargoyle },
            new[] { FpsEnemyKind.Acolyte, FpsEnemyKind.Skeleton, FpsEnemyKind.Sentinel,
                FpsEnemyKind.Gargoyle, FpsEnemyKind.ClockworkImp },
            new[] { FpsEnemyKind.MushroomBrute, FpsEnemyKind.Slime, FpsEnemyKind.Larva,
                FpsEnemyKind.Spider, FpsEnemyKind.FireBat },
            new[] { FpsEnemyKind.Sentinel, FpsEnemyKind.Ghost, FpsEnemyKind.Bat,
                FpsEnemyKind.ClockworkImp, FpsEnemyKind.StalkerShadow, FpsEnemyKind.Lantern }
        };

        public static int ThemeIndex(int floor) => (Math.Max(1, floor) - 1) % ThemeCount;
        public static string ThemeName(int floor) => Names[ThemeIndex(floor)];

        // Boss choice follows the existing 2D theme pools and the same salted
        // seed, so neither camera changes nor presentation can reroll the boss.
        public static int BossForFloor(int seed, int floor) => DungeonFloor.Generate(seed, floor).BossArchetype;

        public static FpsEnemyKind[] EnemyRosterForTheme(int floor)
            => (FpsEnemyKind[])Rosters[ThemeIndex(floor)].Clone();

        public static FpsEnemyKind EnemyForWave(int floor, int wave, int index)
        {
            if (wave < 0 || wave >= FpsPrototypeRules.WaveCount) throw new ArgumentOutOfRangeException(nameof(wave));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            var roster = Rosters[ThemeIndex(floor)];
            return roster[(int)(((long)wave * 2 + index) % roster.Length)];
        }
    }
}
