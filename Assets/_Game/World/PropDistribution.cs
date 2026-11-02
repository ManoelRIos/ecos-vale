using System;
using System.Collections.Generic;
using System.Threading;

namespace Ecos
{
    /// <summary>Pure deterministic decoration data; no Unity objects or worker-side scene access.</summary>
    public readonly struct PropPlacement
    {
        public readonly float x, y, z, yaw, scale;
        public readonly int biome, kind;

        public PropPlacement(float x, float y, float z, float yaw, float scale, int biome, int kind)
        {
            this.x = x; this.y = y; this.z = z; this.yaw = yaw; this.scale = scale;
            this.biome = biome; this.kind = kind;
        }
    }

    public static class PropDistribution
    {
        public static PropPlacement[] Generate(int chunkX, int chunkZ, int size, int budget, int seed,
            float[] biomeDensity, CancellationToken token)
        {
            if (size <= 0 || budget < 0 || budget > 1024)
                throw new ArgumentOutOfRangeException(nameof(budget));
            if (biomeDensity == null || biomeDensity.Length != 6)
                throw new ArgumentException("Six biome density values are required.", nameof(biomeDensity));
            if (budget == 0) return Array.Empty<PropPlacement>();
            var placements = new List<PropPlacement>(budget);
            int treeSide = Math.Max(1, (int)Math.Round(Math.Sqrt(budget * .5)));
            int treeCells = treeSide * treeSide;
            float spacing = size / (float)treeSide;
            int understory = Math.Max(0, budget - treeCells);
            int offset = (int)(WorldMath.Hash(chunkX, chunkZ, seed + 311) % (uint)treeCells);
            // Interleave tree and groundcover candidates so a newly streamed grove fills gradually.
            int total = Math.Max(treeCells, understory);
            for (int i = 0; i < total; i++)
            {
                token.ThrowIfCancellationRequested();
                if (i < treeCells)
                {
                    int cellX = i % treeSide, cellZ = i / treeSide;
                    Anchor(chunkX, chunkZ, cellX, cellZ, spacing, seed, out float x, out float z);
                    Add(placements, chunkX, chunkZ, size, seed, i, x, z, 0, biomeDensity);
                }
                if (i < understory)
                {
                    // 53 is coprime with the default 12x12 grid: nearby details spread through the grove.
                    int anchor = (i * 53 + offset) % treeCells;
                    Anchor(chunkX, chunkZ, anchor % treeSide, anchor / treeSide, spacing, seed, out float x, out float z);
                    float angle = WorldMath.Random(i + chunkX * 151, chunkZ, seed + 914) * (float)Math.PI * 2;
                    float radius = 1.7f + WorldMath.Random(chunkX, i + chunkZ * 137, seed + 303) * 2.4f;
                    x += (float)Math.Cos(angle) * radius;
                    z += (float)Math.Sin(angle) * radius;
                    if (x < .5f || z < .5f || x >= size - .5f || z >= size - .5f) continue;
                    float choice = WorldMath.Random(i, chunkX + chunkZ * 101, seed + 605);
                    int kind = choice < .12f ? 1 : choice < .39f ? 2 : 3;
                    Add(placements, chunkX, chunkZ, size, seed, i + treeCells, x, z, kind, biomeDensity);
                }
            }
            return placements.ToArray();
        }

        static void Anchor(int cx, int cz, int x, int z, float spacing, int seed, out float px, out float pz)
        {
            // Limited jitter keeps neighboring trunks apart while avoiding a visibly regular grid.
            px = (x + .33f + WorldMath.Random(cx * 173 + x, cz * 179 + z, seed + 119) * .34f) * spacing;
            pz = (z + .33f + WorldMath.Random(cx * 181 + x, cz * 191 + z, seed + 717) * .34f) * spacing;
        }

        static void Add(List<PropPlacement> output, int cx, int cz, int size, int seed, int id,
            float x, float z, int kind, float[] densities)
        {
            float wx = cx * size + x, wz = cz * size + z;
            float pathWidth = kind == 0 ? 3.7f : kind == 1 ? 2.2f : 1.5f;
            if (WorldMath.PathDistance(wx, wz) < pathWidth) return;
            int biome = WorldMath.Biome(wx, wz, seed);
            float density = Math.Max(0, Math.Min(1, densities[biome]));
            float cluster = .92f + WorldMath.Noise(wx * .025f, wz * .025f, seed + 407) * .08f;
            if (WorldMath.Random(id + cx * 193, cz, seed + 887) > density * cluster) return;
            float height = WorldMath.Height(wx, wz, seed);
            float slope = Math.Abs(WorldMath.Height(wx + 2, wz, seed) - height)
                        + Math.Abs(WorldMath.Height(wx, wz + 2, seed) - height);
            if (slope > (kind == 0 ? 3.5f : 5.5f)) return;
            float scale = kind == 0 ? .8f + WorldMath.Random(cx, id + cz * 157, seed + 163) * .5f
                : .8f + WorldMath.Random(cx, id + cz * 157, seed + 163) * .65f;
            output.Add(new PropPlacement(x, height, z,
                WorldMath.Random(cx + id, cz, seed + 809) * 360, scale, biome, kind));
        }
    }
}
