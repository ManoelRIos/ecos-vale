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
            var objects = new System.Collections.Generic.List<GameObject>();
            var targets = new System.Collections.Generic.Dictionary<string, Interactable>();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var pickup = new GameObject("crystal");
                    objects.Add(pickup);
                    var item = pickup.AddComponent<Interactable>();
                    item.id = "test-pickup-" + i;
                    item.itemId = "crystal";
                    targets[item.id] = item;
                    var shrine = new GameObject("shrine");
                    objects.Add(shrine);
                    var altar = shrine.AddComponent<Interactable>();
                    altar.id = "shrine-" + i;
                    altar.shrine = true;
                    targets[altar.id] = altar;
                }

                var authority = new OfflineWorldAuthority(state, targets);
                for (int i = 0; i < 3; i++)
                {
                    Assert.IsTrue(authority.Execute(new WorldCommand(WorldCommandKind.Collect, "test-pickup-" + i), Vector3.zero, out _));
                    Assert.IsTrue(authority.Execute(new WorldCommand(WorldCommandKind.Restore, "shrine-" + i), Vector3.zero, out _));
                    Assert.IsFalse(authority.Execute(new WorldCommand(WorldCommandKind.Restore, "shrine-" + i), Vector3.zero, out _));
                }

                Assert.IsTrue(ExplorationQuest.Evaluate(state));
                Assert.AreEqual(0, state.inventory.Count("crystal"));
                Assert.AreEqual(1, state.inventory.Count("artifact"));
            }
            finally
            {
                foreach (var go in objects)
                    Object.DestroyImmediate(go);
            }
        }
    }
}
