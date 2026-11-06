using System;
using System.Collections.Generic;
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
        public void CampaignKeepsAllThemesEnemiesAndBossesAvailableWithoutRerollingTheCorePools()
        {
            var names = new HashSet<string>();
            var kinds = new HashSet<FpsEnemyKind>();
            var bosses = new HashSet<int>();
            Assert.AreEqual(17, Enum.GetValues(typeof(FpsEnemyKind)).Length);
            for (int floor = 1; floor <= HauntFpsCampaign.ThemeCount; floor++)
            {
                Assert.AreEqual(floor - 1, HauntFpsCampaign.ThemeIndex(floor));
                Assert.IsTrue(names.Add(HauntFpsCampaign.ThemeName(floor)), "Each theme has its own title.");
                var roster = HauntFpsCampaign.EnemyRosterForTheme(floor);
                foreach (var kind in roster)
                {
                    kinds.Add(kind);
                    Assert.IsNotEmpty(HauntFpsEnemy.SpriteForKind(kind));
                }
                var first = roster[0];
                roster[0] = (FpsEnemyKind)int.MaxValue;
                Assert.AreEqual(first, HauntFpsCampaign.EnemyRosterForTheme(floor)[0], "Callers cannot mutate the shared roster.");
                for (int wave = 0; wave < FpsPrototypeRules.WaveCount; wave++)
                    for (int member = 0; member < 20; member++)
                        CollectionAssert.Contains(HauntFpsCampaign.EnemyRosterForTheme(floor),
                            HauntFpsCampaign.EnemyForWave(floor, wave, member));
                for (int seed = -30; seed < 30; seed++)
                {
                    int boss = HauntFpsCampaign.BossForFloor(seed, floor);
                    bosses.Add(boss);
                    Assert.AreEqual(DungeonFloor.Generate(seed, floor).BossArchetype, boss);
                    CollectionAssert.Contains(DungeonFloor.BossCandidatesForTheme(floor - 1), boss);
                }
            }
            Assert.AreEqual(5, names.Count);
            Assert.AreEqual(HauntFpsCampaign.EnemyCount, kinds.Count);
            Assert.AreEqual(HauntFpsCampaign.BossCount, bosses.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => HauntFpsCampaign.EnemyForWave(1, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => HauntFpsCampaign.EnemyForWave(1, 0, -1));
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
                Assert.IsTrue(world.BarrelSpawns.All(p => world.IsInsideBounds(p, .6f)));
                Assert.IsNotNull(world.EntranceRoom); Assert.IsNotNull(world.BossRoom);
                Assert.AreSame(world.EntranceRoom, world.RoomAt(world.PlayerSpawn));
                Assert.AreSame(world.BossRoom, world.RoomAt(world.ExitPosition));
                // Inside the entrance's east wall, beside its doorway: neither a room nor a corridor.
                var entrance = world.EntranceRoom;
                Assert.IsFalse(world.IsInsideBounds(entrance.Center + new Vector3(entrance.HalfSize.x + HauntFpsWorld.Tile * .5f, 0, entrance.HalfSize.y * .5f)));
                // Every room is reachable through doors from the entrance.
                var seen = new HashSet<int> { entrance.Id }; var open = new Queue<FpsRoom>(); open.Enqueue(entrance);
                while (open.Count > 0) foreach (int id in open.Dequeue().Neighbors) if (seen.Add(id)) open.Enqueue(world.GetRoom(id));
                Assert.AreEqual(world.Rooms.Count, seen.Count);
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
