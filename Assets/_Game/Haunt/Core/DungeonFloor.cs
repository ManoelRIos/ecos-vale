using System;
using System.Collections.Generic;

namespace Ecos.Haunt
{
    public enum RoomKind { Entrance, Combat, Boss }

    public readonly struct GridPoint : IEquatable<GridPoint>
    {
        public readonly int X, Y;
        public GridPoint(int x, int y) { X = x; Y = y; }
        public bool Equals(GridPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is GridPoint other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
    }

    public sealed class DungeonRoom
    {
        public int Id;
        public GridPoint Position;
        public RoomKind Kind;
        public readonly List<int> Neighbors = new List<int>();
        public bool Cleared;
    }

    /// <summary>
    /// Seeded room plan. Adjacent rooms share exactly one cardinal doorway;
    /// the boss is a farthest leaf, so the entrance can never lead straight to it.
    /// Only one room is instantiated at a time by the Unity presentation.
    /// </summary>
    public sealed class DungeonFloor
    {
        public int Seed, Number;
        public int ThemeIndex => (Math.Max(1, Number) - 1) % BossPools.Length;
        public int BossArchetype { get; private set; }

        // Three possible encounters per theme. These archetypes share the current
        // floor's setting without changing its room graph or ordinary enemy roster.
        static readonly int[][] BossPools =
        {
            new[] { 0, 1, 3 }, new[] { 1, 2, 4 }, new[] { 2, 4, 0 },
            new[] { 3, 1, 0 }, new[] { 4, 3, 2 }
        };

        public static int[] BossCandidatesForTheme(int themeIndex)
        {
            if (themeIndex < 0 || themeIndex >= BossPools.Length)
                throw new ArgumentOutOfRangeException(nameof(themeIndex));
            // Never let callers change the shared encounter catalog.
            return (int[])BossPools[themeIndex].Clone();
        }
        public readonly List<DungeonRoom> Rooms = new List<DungeonRoom>();
        public DungeonRoom Entrance, Boss;

        static readonly GridPoint[] Directions =
        {
            new GridPoint(0, 1), new GridPoint(1, 0),
            new GridPoint(0, -1), new GridPoint(-1, 0)
        };

        public DungeonRoom GetRoom(int id)
        {
            if (id < 0 || id >= Rooms.Count)
                throw new ArgumentOutOfRangeException(nameof(id));
            return Rooms[id];
        }

        public static DungeonFloor Generate(int seed, int number)
        {
            number = Math.Max(1, number);
            var plan = new DungeonFloor { Seed = seed, Number = number };
            // A separate salted stream preserves all existing seeded room layouts.
            // Visual ambience and room generation never advance this encounter RNG.
            var encounterRandom = new SeededRandom(unchecked((uint)seed * 0x85ebca6bu
                ^ (uint)number * 0xc2b2ae35u ^ 0x4d595df4u));
            int[] pool = BossPools[plan.ThemeIndex];
            plan.BossArchetype = pool[encounterRandom.Next(pool.Length)];
            var random = new SeededRandom(unchecked((uint)seed ^ ((uint)number * 0x9e3779b9u)));
            int count = 7 + Math.Min(number - 1, 4);
            var occupied = new Dictionary<GridPoint, DungeonRoom>();
            plan.Entrance = new DungeonRoom
            {
                Id = 0, Position = new GridPoint(0, 0),
                Kind = RoomKind.Entrance, Cleared = true
            };
            plan.Rooms.Add(plan.Entrance);
            occupied.Add(plan.Entrance.Position, plan.Entrance);

            while (plan.Rooms.Count < count)
            {
                // A candidate touches just its parent. This prevents accidental
                // doorways or loops from making the boss shortcuttable.
                var candidates = new List<Candidate>();
                foreach (DungeonRoom parent in plan.Rooms)
                    foreach (GridPoint direction in Directions)
                    {
                        var point = new GridPoint(parent.Position.X + direction.X, parent.Position.Y + direction.Y);
                        if (occupied.ContainsKey(point)) continue;
                        int neighbors = 0;
                        foreach (GridPoint side in Directions)
                            if (occupied.ContainsKey(new GridPoint(point.X + side.X, point.Y + side.Y))) neighbors++;
                        if (neighbors == 1) candidates.Add(new Candidate(parent, point));
                    }
                // A finite tree on an unbounded grid always has an exposed edge.
                if (candidates.Count == 0) throw new InvalidOperationException("Não há espaço para expandir a masmorra.");
                Candidate selected = candidates[random.Next(candidates.Count)];
                var room = new DungeonRoom
                {
                    Id = plan.Rooms.Count, Position = selected.Position, Kind = RoomKind.Combat
                };
                room.Neighbors.Add(selected.Parent.Id);
                selected.Parent.Neighbors.Add(room.Id);
                plan.Rooms.Add(room);
                occupied.Add(room.Position, room);
            }

            // IDs are topological because every new room attaches to an older one.
            var distance = new int[count];
            var farthest = new List<DungeonRoom>();
            int maximum = 0;
            for (int i = 1; i < count; i++)
            {
                DungeonRoom room = plan.Rooms[i];
                distance[i] = distance[room.Neighbors[0]] + 1;
                if (distance[i] > maximum)
                {
                    maximum = distance[i];
                    farthest.Clear();
                }
                if (distance[i] == maximum) farthest.Add(room);
            }
            plan.Boss = farthest[random.Next(farthest.Count)];
            plan.Boss.Kind = RoomKind.Boss;
            return plan;
        }

        readonly struct Candidate
        {
            public readonly DungeonRoom Parent;
            public readonly GridPoint Position;
            public Candidate(DungeonRoom parent, GridPoint position) { Parent = parent; Position = position; }
        }

        // Keep maps reproducible between Unity's runtime and the standalone checks.
        struct SeededRandom
        {
            uint state;
            public SeededRandom(uint seed) { state = seed == 0 ? 0x6d2b79f5u : seed; }
            public int Next(int maximum)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return (int)(state % (uint)maximum);
            }
        }
    }
}
