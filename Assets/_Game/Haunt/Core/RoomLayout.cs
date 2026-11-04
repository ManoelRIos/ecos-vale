using System;
using System.Collections.Generic;

namespace Ecos.Haunt
{
    public enum RoomShape { L, T, H, Octagon, Alcoves }

    /// <summary>A world-space, axis-aligned region, independent of Unity's Rect.</summary>
    public readonly struct LayoutRect
    {
        public readonly float X, Y, Width, Height;
        public LayoutRect(float x, float y, float width, float height)
        { X = x; Y = y; Width = width; Height = height; }
    }

    /// <summary>
    /// Seeded one-unit room cells centered on integer coordinates. L and T rooms
    /// retain three-cell-wide cardinal door approaches and a shared central hub.
    /// IsFloor/IsInside describe the footprint, including any internal pits.
    /// </summary>
    public sealed class RoomLayout
    {
        public const int Width = 29, Height = 17, MaxX = 14, MaxY = 8;
        public RoomShape Shape { get; private set; }
        readonly bool[,] floor = new bool[Width, Height], pits = new bool[Width, Height];
        LayoutRect[] walls = Array.Empty<LayoutRect>(), pitRegions = Array.Empty<LayoutRect>();
        public LayoutRect[] Walls => (LayoutRect[])walls.Clone();
        public LayoutRect[] Pits => (LayoutRect[])pitRegions.Clone();
        static readonly int[] StepX = { 0, 1, 0, -1 }, StepY = { 1, 0, -1, 0 };

        RoomLayout() { }

        public static RoomLayout Generate(int seed, int floor, int roomId, RoomKind kind)
        {
            floor = Math.Max(1, floor);
            uint state = Mix(unchecked((uint)seed * 0x85ebca6bu ^ (uint)floor * 0xc2b2ae35u
                ^ (uint)roomId * 0x27d4eb2du ^ 0x318b7a65u));
            var result = new RoomLayout
            {
                Shape = kind == RoomKind.Combat ? (RoomShape)(state % 5) : RoomShape.Octagon
            };
            bool flipX = (state & 16) != 0, flipY = (state & 32) != 0;
            for (int y = -MaxY; y <= MaxY; y++)
                for (int x = -MaxX; x <= MaxX; x++)
                {
                    int tx = flipX ? -x : x, ty = flipY ? -y : y;
                    bool inside;
                    switch (result.Shape)
                    {
                        case RoomShape.L: inside = tx <= -5 || ty <= -3; break;
                        case RoomShape.T: inside = ty >= 3 || Math.Abs(tx) <= 4; break;
                        case RoomShape.H: inside = Math.Abs(tx) >= 7 || Math.Abs(ty) <= 2; break;
                        case RoomShape.Alcoves:
                            inside = Math.Abs(tx) <= 9 && Math.Abs(ty) <= 5
                                || Math.Abs(ty) >= 3 && Math.Abs(ty) <= 6
                                || Math.Abs(tx) >= 3 && Math.Abs(tx) <= 6;
                            break;
                        default: inside = Math.Abs(tx) + Math.Abs(ty) <= 18; break;
                    }
                    // A central fighting area and all four approaches remain valid
                    // for existing arrivals, portals and enemies up to radius .85.
                    inside |= Math.Abs(x) <= 1 || Math.Abs(y) <= 1
                        || Math.Abs(x) <= 4 && Math.Abs(y) <= 3;
                    result.floor[x + MaxX, y + MaxY] = inside;
                }
            if (kind == RoomKind.Combat && floor >= 2 && (state & 7) != 0)
                result.GeneratePits(ref state);
            result.walls = result.Regions(false);
            result.pitRegions = result.Regions(true);
            return result;
        }

        public bool InBounds(int x, int y) => x >= -MaxX && x <= MaxX && y >= -MaxY && y <= MaxY;
        public bool IsFloor(int x, int y) => InBounds(x, y) && floor[x + MaxX, y + MaxY];
        public bool IsInside(int x, int y) => IsFloor(x, y);
        public bool IsPit(int x, int y) => InBounds(x, y) && pits[x + MaxX, y + MaxY];
        bool Safe(int x, int y) => IsFloor(x, y) && !IsPit(x, y);

        public bool ContainsCircle(float x, float y, float radius, bool allowPits = true)
        {
            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y)
                || float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0) return false;
            if ((double)x - radius < -MaxX - .5 || (double)x + radius > MaxX + .5
                || (double)y - radius < -MaxY - .5 || (double)y + radius > MaxY + .5) return false;
            int cellX = (int)Math.Floor(x + .5), cellY = (int)Math.Floor(y + .5);
            if (!IsFloor(cellX, cellY) || !allowPits && IsPit(cellX, cellY)) return false;
            foreach (LayoutRect wall in walls)
                if (CircleIntersects(x, y, radius, wall)) return false;
            if (!allowPits)
                foreach (LayoutRect pit in pitRegions)
                    if (CircleIntersects(x, y, radius, pit)) return false;
            return true;
        }

        /// <summary>Next cardinal tile on the shortest safe path; pits and void are blocked.</summary>
        public bool TryNextStep(int startX, int startY, int targetX, int targetY, out GridPoint next)
        {
            next = new GridPoint(startX, startY);
            if (!Safe(startX, startY) || !Safe(targetX, targetY)) return false;
            if (startX == targetX && startY == targetY) return true;
            int[] queue = new int[Width * Height], parent = new int[Width * Height];
            for (int i = 0; i < parent.Length; i++) parent[i] = -1;
            int start = Index(startX, startY), target = Index(targetX, targetY);
            int head = 0, tail = 1;
            queue[0] = start; parent[start] = start;
            while (head < tail)
            {
                int current = queue[head++], x = current % Width - MaxX, y = current / Width - MaxY;
                for (int side = 0; side < StepX.Length; side++)
                {
                    int nx = x + StepX[side], ny = y + StepY[side];
                    if (!Safe(nx, ny)) continue;
                    int index = Index(nx, ny);
                    if (parent[index] >= 0) continue;
                    parent[index] = current;
                    if (index == target)
                    {
                        int first = target;
                        while (parent[first] != start) first = parent[first];
                        next = new GridPoint(first % Width - MaxX, first / Width - MaxY);
                        return true;
                    }
                    queue[tail++] = index;
                }
            }
            return false;
        }

        void GeneratePits(ref uint state)
        {
            int wanted = 1 + (int)((state >> 8) & 1), accepted = 0;
            for (int attempt = 0; attempt < 24 && accepted < wanted; attempt++)
            {
                int x = 5 + Next(ref state, 6);
                if (Next(ref state, 2) == 0) x = -x - 1;
                int y = 4 + Next(ref state, 3);
                if (Next(ref state, 2) == 0) y = -y;
                // Two cells of surrounding floor preserve passing space and stop
                // holes from cutting a narrow bridge or another pit's approach.
                bool clear = true;
                for (int py = y - 2; py <= y + 2 && clear; py++)
                    for (int px = x - 2; px <= x + 3; px++)
                        if (!Safe(px, py)) { clear = false; break; }
                if (!clear) continue;
                pits[x + MaxX, y + MaxY] = pits[x + 1 + MaxX, y + MaxY] = true;
                if (!SafeConnected())
                {
                    pits[x + MaxX, y + MaxY] = pits[x + 1 + MaxX, y + MaxY] = false;
                    continue;
                }
                accepted++;
            }
        }

        bool SafeConnected()
        {
            int[] queue = new int[Width * Height];
            var visited = new bool[Width * Height];
            int head = 0, tail = 1, total = 0;
            queue[0] = Index(0, 0); visited[queue[0]] = true;
            for (int y = -MaxY; y <= MaxY; y++)
                for (int x = -MaxX; x <= MaxX; x++) if (Safe(x, y)) total++;
            while (head < tail)
            {
                int current = queue[head++], x = current % Width - MaxX, y = current / Width - MaxY;
                for (int side = 0; side < StepX.Length; side++)
                {
                    int nx = x + StepX[side], ny = y + StepY[side];
                    if (!Safe(nx, ny)) continue;
                    int index = Index(nx, ny);
                    if (visited[index]) continue;
                    visited[index] = true; queue[tail++] = index;
                }
            }
            return tail == total;
        }

        // Merge horizontal runs with identical extents on consecutive rows.
        // The resulting rectangles are disjoint and cover exactly void or pit cells.
        LayoutRect[] Regions(bool pit)
        {
            var regions = new List<LayoutRect>();
            var previous = new Dictionary<int, int>();
            for (int y = -MaxY; y <= MaxY; y++)
            {
                var current = new Dictionary<int, int>();
                int x = -MaxX;
                while (x <= MaxX)
                {
                    bool blocked = pit ? IsPit(x, y) : !IsFloor(x, y);
                    if (!blocked) { x++; continue; }
                    int start = x++;
                    while (x <= MaxX && (pit ? IsPit(x, y) : !IsFloor(x, y))) x++;
                    int length = x - start, key = (start + MaxX) * (Width + 1) + length;
                    int index;
                    if (previous.TryGetValue(key, out index))
                    {
                        LayoutRect old = regions[index];
                        regions[index] = new LayoutRect(old.X, old.Y, old.Width, old.Height + 1);
                    }
                    else
                    {
                        index = regions.Count;
                        regions.Add(new LayoutRect(start - .5f, y - .5f, length, 1));
                    }
                    current.Add(key, index);
                }
                previous = current;
            }
            return regions.ToArray();
        }

        static bool CircleIntersects(float x, float y, float radius, LayoutRect rect)
        {
            double dx = x - Math.Max(rect.X, Math.Min((double)x, rect.X + rect.Width));
            double dy = y - Math.Max(rect.Y, Math.Min((double)y, rect.Y + rect.Height));
            return dx * dx + dy * dy < (double)radius * radius;
        }
        static int Index(int x, int y) => (y + MaxY) * Width + x + MaxX;
        static int Next(ref uint state, int maximum) { state = Mix(unchecked(state + 0x9e3779b9u)); return (int)(state % (uint)maximum); }
        static uint Mix(uint value)
        {
            unchecked
            {
                value ^= value >> 16; value *= 0x85ebca6bu;
                value ^= value >> 13; value *= 0xc2b2ae35u;
                return value ^ value >> 16;
            }
        }
    }
}
