using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace Ecos.Tests
{
    public sealed class WorldTests
    {
        static readonly Color[] Palette =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.gray,
            Color.cyan,
            Color.magenta
        };
        [Test]
        public void SharedVisualMeshesHaveOutwardNonDegenerateFaces()
        {
            var meshes = new[] { LowPolyMesh.Octahedron, LowPolyMesh.Cone, LowPolyMesh.Trunk, LowPolyMesh.Crown, LowPolyMesh.MushroomCap };
            Assert.AreSame(LowPolyMesh.Cone, LowPolyMesh.Cone);
            foreach (var mesh in meshes)
            {
                Assert.IsTrue(LowPolyMesh.IsGenerated(mesh), mesh.name);
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Assert.AreEqual(vertices.Length, mesh.normals.Length, mesh.name);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = vertices[triangles[i]], b = vertices[triangles[i + 1]], c = vertices[triangles[i + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    Assert.Greater(normal.sqrMagnitude, .00000001f, mesh.name + " face " + i);
                    Assert.Greater(Vector3.Dot(normal, (a + b + c) / 3), 0, mesh.name + " winding " + i);
                }
            }
        }

        [Test]
        public void NeighborChunksShareVerticesAndNormals()
        {
            var a = TerrainMeshGenerator.Generate(-1, 0, 128, 24, 71319, Palette, CancellationToken.None);
            var b = TerrainMeshGenerator.Generate(0, 0, 128, 24, 71319, Palette, CancellationToken.None);
            for (int z = 0; z <= 24; z++)
            {
                int ai = z * 25 + 24, bi = z * 25;
                Assert.AreEqual(a.vertices[ai] + new Vector3(-128, 0, 0), b.vertices[bi]);
                Assert.AreEqual(a.normals[ai], b.normals[bi]);
                Assert.AreEqual(a.colors[ai], b.colors[bi]);
            }
        }

        [Test]
        public void CancelledGenerationThrows()
        {
            using (var c = new CancellationTokenSource())
            {
                c.Cancel();
                Assert.Throws<System.OperationCanceledException>(() => TerrainMeshGenerator.Generate(0, 0, 128, 24, 1, Palette, c.Token));
            }
        }

        [Test]
        public void TerrainHasValidTriangles()
        {
            var d = TerrainMeshGenerator.Generate(0, 0, 128, 24, 71319, Palette, CancellationToken.None);
            Assert.AreEqual(625, d.vertices.Length);
            Assert.AreEqual(3456, d.triangles.Length);
            foreach (int i in d.triangles)
                Assert.That(i, Is.InRange(0, 624));
        }

        [Test]
        public void InventoryFullAdditionIsAtomic()
        {
            var i = new InventoryState
            {
                capacity = 1
            };
            Assert.IsTrue(i.Add("crystal", 49));
            Assert.IsFalse(i.Add("crystal", 2));
            Assert.AreEqual(49, i.Count("crystal"));
        }

        [Test]
        public void QuestRewardIsGrantedOnce()
        {
            var s = new SaveData();
            s.restored.AddRange(new[] { "shrine-0", "shrine-1", "shrine-2" });
            Assert.IsTrue(ExplorationQuest.Evaluate(s));
            Assert.IsFalse(ExplorationQuest.Evaluate(s));
            Assert.AreEqual(1, s.inventory.Count("artifact"));
        }

        [Test]
        public void SaveJsonRoundTripPreservesProgress()
        {
            var s = new SaveData
            {
                seed = 42,
                time = .85f,
                position = new Vector3(-20, 8, 80)
            };
            s.inventory.Add("wood", 60);
            s.collected.Add("crystal-2");
            s.discovered.Add("shrine-0");
            s.restored.Add("shrine-0");
            var result = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(s));
            Assert.AreEqual(42, result.seed);
            Assert.AreEqual(s.position, result.position);
            Assert.AreEqual(60, result.inventory.Count("wood"));
            CollectionAssert.AreEqual(s.collected, result.collected);
            CollectionAssert.AreEqual(s.restored, result.restored);
            Assert.AreEqual(.85f, result.time);
        }

        [Test]
        public void AuthorityRejectsDistanceAndDuplicateCollection()
        {
            var go = new GameObject("resource");
            try
            {
                var t = go.AddComponent<Interactable>();
                t.id = "test-crystal";
                t.itemId = "crystal";
                var state = new SaveData();
                var authority = new OfflineWorldAuthority(state, new System.Collections.Generic.Dictionary<string, Interactable> { { t.id, t } });
                Assert.IsFalse(authority.Execute(new WorldCommand(WorldCommandKind.Collect, t.id), Vector3.one * 50, out _));
                Assert.IsTrue(authority.Execute(new WorldCommand(WorldCommandKind.Collect, t.id), Vector3.zero, out _));
                Assert.IsFalse(authority.Execute(new WorldCommand(WorldCommandKind.Collect, t.id), Vector3.zero, out _));
                Assert.AreEqual(1, state.inventory.Count("crystal"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CompleteMissionThroughAuthoritativeInteractions()
        {
            var state = new SaveData();
            state.inventory.Add("wood", 6); state.inventory.Add("stone", 7);
            state.inventory.Add("herb", 5); state.inventory.Add("crystal", 5);
            var objects = new System.Collections.Generic.List<GameObject>();
            var targets = new System.Collections.Generic.Dictionary<string, Interactable>();
            try
            {
                string[] ids = { "camp", "workbench", "shrine-0", "shrine-1", "shrine-2" };
                for (int i = 0; i < ids.Length; i++)
                {
                    var go = new GameObject(ids[i]); objects.Add(go);
                    var target = go.AddComponent<Interactable>(); target.id = ids[i];
                    target.kind = i == 0 ? InteractionKind.Camp : i == 1 ? InteractionKind.Workbench : InteractionKind.Shrine;
                    targets[target.id] = target;
                }
                var authority = new OfflineWorldAuthority(state, targets);
                Assert.IsFalse(authority.Execute(new WorldCommand(WorldCommandKind.CraftLens, "workbench"), Vector3.zero, out _));
                Assert.IsTrue(authority.Execute(new WorldCommand(WorldCommandKind.BuildCamp, "camp"), Vector3.zero, out _));
                Assert.IsTrue(authority.Execute(new WorldCommand(WorldCommandKind.CraftLens, "workbench"), Vector3.zero, out _));
                for (int i = 0; i < 3; i++)
                {
                    Assert.IsTrue(authority.Execute(new WorldCommand(WorldCommandKind.Restore, "shrine-" + i), Vector3.zero, out _));
                    Assert.IsFalse(authority.Execute(new WorldCommand(WorldCommandKind.Restore, "shrine-" + i), Vector3.zero, out _));
                }
                Assert.IsTrue(ExplorationQuest.Evaluate(state));
                Assert.AreEqual(1, state.inventory.Count("artifact"));
                Assert.AreEqual(0, state.inventory.Count("crystal"));
                Assert.IsFalse(ExplorationQuest.Evaluate(state));
            }
            finally { foreach (var go in objects) Object.DestroyImmediate(go); }
        }

        [Test]
        public void LegacySaveMigratesWithoutLosingProgress()
        {
            const string json = "{\"version\":1,\"seed\":42,\"position\":{\"x\":1,\"y\":2,\"z\":3},\"time\":0.4,\"inventory\":{\"capacity\":12,\"stacks\":[{\"id\":\"wood\",\"count\":5}]},\"collected\":[\"crystal-0\"],\"discovered\":[\"shrine-0\"],\"restored\":[\"shrine-0\"],\"rewarded\":false}";
            Assert.IsTrue(SaveStore.TryParse(json, out var state, out string error), error);
            Assert.AreEqual(SaveStore.CurrentVersion, state.version);
            Assert.AreEqual(42, state.seed);
            Assert.AreEqual(new Vector3(1, 2, 3), state.position);
            Assert.AreEqual(5, state.inventory.Count("wood"));
            Assert.IsTrue(state.campBuilt && state.lensCrafted);
            CollectionAssert.Contains(state.collected, "crystal-0");
            CollectionAssert.Contains(state.restored, "shrine-0");
            Assert.IsNotNull(state.openedCaches); Assert.IsNotNull(state.readClues); Assert.IsEmpty(state.observedSpecies);
        }

        [Test]
        public void SaveVersionThreeRetainsInheritedProgressionAndFieldGuide()
        {
            var original = new SaveData { campBuilt = true, lensCrafted = true };
            original.openedCaches.Add("cache-0"); original.readClues.Add("journal-0");
            original.observedSpecies.Add("hare");
            ProgressionRules.GainExperience(original, 80);
            original.inventory.Add("herb", 7);
            Assert.IsTrue(SaveStore.TryParse(JsonUtility.ToJson(original), out var restored, out string error), error);
            Assert.AreEqual(SaveStore.CurrentVersion, restored.version);
            Assert.AreEqual(80, restored.experience);
            Assert.AreEqual(14, restored.inventory.capacity);
            Assert.IsTrue(restored.campBuilt && restored.lensCrafted);
            Assert.AreEqual(7, restored.inventory.Count("herb"));
            CollectionAssert.Contains(restored.openedCaches, "cache-0");
            CollectionAssert.Contains(restored.readClues, "journal-0");
            CollectionAssert.Contains(restored.observedSpecies, "hare");
        }

        [Test]
        public void VersionTwoMigrationPreservesAdventureAndAddsEmptyFieldGuide()
        {
            const string json = "{\"version\":2,\"seed\":77,\"position\":{\"x\":12,\"y\":8,\"z\":-14},\"time\":0.4,\"inventory\":{\"capacity\":14,\"stacks\":[{\"id\":\"herb\",\"count\":9}]},\"collected\":[\"gather-herb-0\"],\"discovered\":[\"camp\"],\"restored\":[],\"openedCaches\":[\"cache-0\"],\"readClues\":[\"journal-0\"],\"campBuilt\":true,\"lensCrafted\":true,\"experience\":81}";
            Assert.IsTrue(SaveStore.TryParse(json, out var state, out string error), error);
            Assert.AreEqual(3, state.version);
            Assert.AreEqual(77, state.seed);
            Assert.AreEqual(81, state.experience);
            Assert.AreEqual(9, state.inventory.Count("herb"));
            Assert.IsTrue(state.campBuilt && state.lensCrafted);
            CollectionAssert.Contains(state.openedCaches, "cache-0");
            CollectionAssert.Contains(state.readClues, "journal-0");
            Assert.IsEmpty(state.observedSpecies);
            Assert.IsFalse(state.guideRewarded);
        }

        [Test]
        public void AuthorityObservesSpeciesOnceAcrossDifferentCreaturesAndRejectsRemoteObservation()
        {
            var state = new SaveData();
            var first = new GameObject("hare-1"); var second = new GameObject("hare-2");
            try
            {
                var a = first.AddComponent<Interactable>(); a.kind = InteractionKind.Observe; a.id = "wildlife-1"; a.itemId = "hare";
                var b = second.AddComponent<Interactable>(); b.kind = InteractionKind.Observe; b.id = "wildlife-2"; b.itemId = "hare";
                var targets = new System.Collections.Generic.Dictionary<string, Interactable> { { a.id, a }, { b.id, b } };
                var authority = new OfflineWorldAuthority(state, targets);
                Assert.IsFalse(authority.Execute(new WorldCommand(WorldCommandKind.Observe, a.id), Vector3.one * 100, out _));
                Assert.IsFalse(authority.Execute(new WorldCommand(WorldCommandKind.Collect, a.id), Vector3.zero, out _));
                Assert.IsTrue(authority.Execute(new WorldCommand(WorldCommandKind.Observe, a.id), Vector3.zero, out _));
                Assert.IsFalse(authority.Execute(new WorldCommand(WorldCommandKind.Observe, b.id), Vector3.zero, out _));
                Assert.AreEqual(12, state.experience);
                Assert.AreEqual(1, state.observedSpecies.Count);
                Assert.IsTrue(first.activeSelf && second.activeSelf);
                Assert.IsEmpty(state.collected);
            }
            finally { Object.DestroyImmediate(first); Object.DestroyImmediate(second); }
        }
    }
}
