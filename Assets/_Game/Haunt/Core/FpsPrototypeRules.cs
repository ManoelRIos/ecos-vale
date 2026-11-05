using System;

namespace Ecos.Haunt
{
    /// <summary>A single encounter wave; timing varies without changing its roster.</summary>
    public readonly struct FpsWaveSpec
    {
        public readonly int Skeletons, Lanterns;
        public readonly float SpawnInterval;
        public int Total => Skeletons + Lanterns;

        internal FpsWaveSpec(int skeletons, int lanterns, float spawnInterval)
        {
            Skeletons = skeletons;
            Lanterns = lanterns;
            SpawnInterval = spawnInterval;
        }
    }

    /// <summary>Shared FPS tuning and encounter mathematics, independent of Unity.</summary>
    public static class FpsPrototypeRules
    {
        public const int WaveCount = 3;
        public const float PlayerSpeed = 6f, DodgeDuration = .22f, DodgeCooldown = .7f, ShotRange = 45f;

        public static FpsWaveSpec Wave(int wave, int seed)
        {
            if (wave < 0 || wave >= WaveCount) throw new ArgumentOutOfRangeException(nameof(wave));
            // One local hash keeps replay stable without consuming combat/world RNG.
            uint hash = unchecked((uint)seed * 0x9e3779b9u);
            hash ^= hash >> 16;
            float spacing = .4f - wave * .07f + (hash % 7) * .01f;
            return new FpsWaveSpec(wave == 0 ? 2 : 3, wave == 2 ? 2 : 1, spacing);
        }

        /// <summary>Linear radial falloff rounded up to whole hit points. Occlusion
        /// and eligibility are resolved by the caller before applying this value.</summary>
        public static int ExplosionDamage(float distance, float radius, int maxDamage)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0
                || float.IsNaN(radius) || float.IsInfinity(radius) || radius <= 0
                || maxDamage <= 0 || distance > radius) return 0;
            // Double precision also handles tiny positive radii and int.MaxValue
            // damage without float rounding overflowing the final integer cast.
            double scaled = maxDamage * (1 - (double)distance / radius);
            return Math.Max(1, Math.Min(maxDamage, (int)Math.Ceiling(scaled)));
        }
    }
}
