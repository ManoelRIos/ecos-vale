using System;

namespace Ecos
{
    // Pure data: no Unity API, shared by worker tasks, map and authoritative placement.
    public static class WorldMath
    {
        public static uint Hash(int x, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + z * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177;
                return h ^ (h >> 16);
            }
        }

        public static float Random(int x, int z, int seed)
        {
            return (Hash(x, z, seed) & 0xffffff) / 16777215f;
        }

        static float Mix(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public static float Noise(float x, float z, int seed)
        {
            int ix = (int)Math.Floor(x), iz = (int)Math.Floor(z);
            float tx = x - ix, tz = z - iz;
            tx = tx * tx * (3 - 2 * tx);
            tz = tz * tz * (3 - 2 * tz);
            return Mix(Mix(Random(ix, iz, seed), Random(ix + 1, iz, seed), tx), Mix(Random(ix, iz + 1, seed), Random(ix + 1, iz + 1, seed), tx), tz) * 2 - 1;
        }

        public static float Fbm(float x, float z, int seed)
        {
            float n = 0, a = .57f;
            for (int i = 0; i < 4; i++)
            {
                n += Noise(x, z, seed + i * 71) * a;
                x *= 2.03f;
                z *= 2.03f;
                a *= .5f;
            }

            return n;
        }

        // Broad angular climate regions plus noise; all six appear within a few chunks of the origin.
        public static float Climate(float x, float z, int seed)
        {
            float a = (float)(Math.Atan2(z + 120, x + 100) / Math.PI * 3 + 3);
            a += Noise(x * .002f, z * .002f, seed + 71) * .28f;
            return (a + 6) % 6;
        }

        public static int Biome(float x, float z, int seed)
        {
            return (int)Climate(x, z, seed);
        }

        public static float Height(float x, float z, int seed)
        {
            float warp = Noise(x * .0014f, z * .0014f, seed + 33) * 95;
            float broad = Fbm((x + warp) * .003f, (z - warp) * .003f, seed);
            float climate = Climate(x, z, seed);
            float snow = Math.Max(0, 1 - Math.Abs(climate - 2.5f) / 1.1f);
            float mountain = (float)Math.Pow(Math.Max(0, Fbm(x * .005f, z * .005f, seed + 8) + .5f), 2) * snow * 70;
            float river = (float)Math.Exp(-Math.Pow((z - (Math.Sin(x * .003) * 65 + Noise(x * .001f, 0, seed) * 100)) / 14, 2)) * 9;
            float h = broad * 19 + mountain - river;
            float start = (float)Math.Exp(-(x * x + z * z) / 2200);
            return Mix(h, 2, start);
        }

        public static float PathDistance(float x, float z)
        {
            return Math.Min(Math.Abs(z - (float)Math.Sin(x * .009) * 17), Math.Abs(x - (float)Math.Sin(z * .008) * 15));
        }
    }
}
