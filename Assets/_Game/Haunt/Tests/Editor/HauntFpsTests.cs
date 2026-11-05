using System;
using System.Linq;
using Ecos.Haunt.Fps;
using NUnit.Framework;
using UnityEngine;

namespace Ecos.Haunt.Tests
{
    public sealed class HauntFpsTests
    {
        [Test]
        public void WavesIncreaseCombatDensityWithoutChangingRosterWhenSeedChanges()
        {
            int previous = 0;
            for (int wave = 0; wave < FpsPrototypeRules.WaveCount; wave++)
            {
                var baseline = FpsPrototypeRules.Wave(wave, 123);
                Assert.Greater(baseline.Total, previous, "Each wave should escalate the encounter.");
                Assert.Greater(baseline.Skeletons, 0);
                Assert.Greater(baseline.Lanterns, 0, "Every wave needs both close and ranged pressure.");
                for (int seed = -20; seed < 20; seed++)
                {
                    var spec = FpsPrototypeRules.Wave(wave, seed);
                    Assert.AreEqual(baseline.Skeletons, spec.Skeletons);
                    Assert.AreEqual(baseline.Lanterns, spec.Lanterns);
                    Assert.GreaterOrEqual(spec.SpawnInterval, .2f);
                    Assert.LessOrEqual(spec.SpawnInterval, .5f);
                    Assert.AreEqual(spec.SpawnInterval, FpsPrototypeRules.Wave(wave, seed).SpawnInterval);
                }
                previous = baseline.Total;
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => FpsPrototypeRules.Wave(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => FpsPrototypeRules.Wave(FpsPrototypeRules.WaveCount, 1));
        }

        [Test]
        public void FpsDodgeProtectsTheOpeningSixtyPercentThenAllowsRecoveryDamage()
        {
            float duration = HauntFpsPlayer.DodgeDuration;
            Assert.AreEqual(FpsPrototypeRules.DodgeDuration, duration);
            Assert.Greater(HauntFpsPlayer.DodgeCooldown, duration);
            Assert.IsTrue(DodgeRules.IsInvulnerable(duration, duration));
            Assert.IsTrue(DodgeRules.IsInvulnerable(duration * .4001f, duration));
            Assert.IsFalse(DodgeRules.IsInvulnerable(duration * .4f, duration));
            Assert.IsFalse(DodgeRules.IsInvulnerable(duration * .2f, duration));
            Assert.IsFalse(DodgeRules.IsInvulnerable(0, duration));
            Assert.IsFalse(DodgeRules.IsInvulnerable(float.NaN, duration));
            Assert.IsFalse(DodgeRules.IsInvulnerable(float.PositiveInfinity, duration));
        }

        [Test]
        public void BlastFalloffIsMonotoneAndRejectsInvalidOrOutOfRangeTargets()
        {
            int previous = 6;
            for (int i = 0; i <= 34; i++)
            {
                int damage = FpsPrototypeRules.ExplosionDamage(i * .1f, 3.4f, 5);
                Assert.GreaterOrEqual(damage, 1);
                Assert.LessOrEqual(damage, previous);
                previous = damage;
            }
            Assert.AreEqual(5, FpsPrototypeRules.ExplosionDamage(0, 3.4f, 5));
            Assert.AreEqual(0, FpsPrototypeRules.ExplosionDamage(3.41f, 3.4f, 5));
            Assert.AreEqual(0, FpsPrototypeRules.ExplosionDamage(float.NaN, 3.4f, 5));
            Assert.AreEqual(0, FpsPrototypeRules.ExplosionDamage(1, 0, 5));
            Assert.AreEqual(0, FpsPrototypeRules.ExplosionDamage(-1, 3.4f, 5));
            Assert.AreEqual(int.MaxValue, FpsPrototypeRules.ExplosionDamage(0, float.Epsilon, int.MaxValue));
        }

        [Test]
        public void GeneratedArenaUsesRealFloorWallsAndCoverWhileDecorationsStayNonSolid()
        {
            // Keep the fixture away from any loaded authoring scene. All generated
            // geometry uses local coordinates and follows its owning transform.
            var root = new GameObject("FPS physics fixture");
            root.transform.position = new Vector3(1000, 0, 1000);
            try
            {
                var world = root.AddComponent<HauntFpsWorld>();
                world.Initialize(null);
                Physics.SyncTransforms();
                Vector3 offset = root.transform.position;
                Assert.IsTrue(Physics.Raycast(offset + Vector3.up * 2, Vector3.down, out var floor, 3));
                Assert.AreEqual("Chão sólido", floor.collider.name);
                Assert.IsTrue(Physics.Raycast(offset + new Vector3(0, 1, -10), Vector3.back, out var wall, 4));
                Assert.AreEqual("Parede sul", wall.collider.name);
                Assert.IsTrue(Physics.Raycast(offset + new Vector3(-8, 1, -4), Vector3.forward, out var cover, 4));
                Assert.AreEqual("Pilar / corpo", cover.collider.name);
                Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(true)
                    .Count(c => c.enabled && (c.name.StartsWith("Lápide") || c.name.StartsWith("Chama") || c.name.StartsWith("Caveira"))),
                    "Small decorative details must not unexpectedly block movement or shots.");
                Assert.IsTrue(world.IsInsideBounds(world.PlayerSpawn, .4f));
                Assert.IsTrue(world.IsInsideBounds(world.ExitPosition, .4f));
                Assert.IsTrue(world.MeleeSpawns.Concat(world.RangedSpawns).Concat(world.BarrelSpawns)
                    .All(p => world.IsInsideBounds(p, .6f)));
                Assert.IsFalse(world.IsInsideBounds(new Vector3(HauntFpsWorld.Width, 0, 0)));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [Test]
        public void ExitSealStopsShotsUntilOpenedAndWorldDisposalRestoresAtmosphere()
        {
            Color ambient = RenderSettings.ambientLight, fog = RenderSettings.fogColor;
            bool fogEnabled = RenderSettings.fog;
            float density = RenderSettings.fogDensity;
            var root = new GameObject("FPS exit fixture");
            root.transform.position = new Vector3(1000, 0, 1000);
            try
            {
                var world = root.AddComponent<HauntFpsWorld>();
                world.Initialize(null);
                Physics.SyncTransforms();
                Vector3 origin = root.transform.position + new Vector3(0, 1.2f, 9);
                Assert.IsFalse(world.ExitOpen);
                Assert.IsTrue(Physics.Raycast(origin, Vector3.forward, out var closed, 2));
                Assert.AreEqual("Selo fechado", closed.collider.name);
                world.SetExitOpen(true);
                Physics.SyncTransforms();
                Assert.IsTrue(world.ExitOpen);
                Assert.IsFalse(Physics.Raycast(origin, Vector3.forward, 2), "Opening the seal must also remove its physical blocker.");
                Assert.Greater(world.GeneratedTextureCount, 0);
                Assert.Greater(world.OwnedMaterialCount, 0);
                Assert.Throws<InvalidOperationException>(() => world.Initialize(null));
                world.Dispose();
                Assert.AreEqual(0, world.GeneratedTextureCount);
                Assert.AreEqual(0, world.OwnedMaterialCount);
                Assert.AreEqual(ambient, RenderSettings.ambientLight);
                Assert.AreEqual(fog, RenderSettings.fogColor);
                Assert.AreEqual(fogEnabled, RenderSettings.fog);
                Assert.AreEqual(density, RenderSettings.fogDensity);
                Assert.DoesNotThrow(world.Dispose);
                var disposedBeforeInit = root.AddComponent<HauntFpsWorld>();
                disposedBeforeInit.Dispose();
                Assert.Throws<ObjectDisposedException>(() => disposedBeforeInit.Initialize(null));
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

    }
}
