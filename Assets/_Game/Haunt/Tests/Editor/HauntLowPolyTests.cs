using System;
using System.Linq;
using Ecos.Haunt.Editor;
using Ecos.Haunt.Fps;
using Ecos.Haunt.LowPoly;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ecos.Haunt.Tests
{
    /// <summary>Native Editor checks. They require a licensed Unity installation;
    /// the command-line C# helpers used in cloud do not execute these fixtures.</summary>
    public sealed class HauntLowPolyTests
    {
        [Test]
        public void LowPolySceneSelectsTheNewProfileWithoutChangingTheArchivedScenes()
        {
            var previous = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(HauntLowPolyProject.ScenePath);
            bool opened = !existing.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(HauntLowPolyProject.ScenePath, OpenSceneMode.Additive) : existing;
            try
            {
                var games = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<HauntFpsGame>(true)).ToArray();
                Assert.AreEqual(1, games.Length);
                Assert.IsTrue(games[0].enabled);
                Assert.IsTrue(games[0].LowPolyMode);
                Assert.AreEqual(1, scene.GetRootGameObjects().Length);
                Assert.AreEqual(HauntLowPolyProject.ScenePath, HauntProject.DefaultScenePath);
                Assert.AreEqual(0, scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<HauntGame>(true).Length));
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void FacetedMeshFactorySharesMeshesKeepsDecorationsNonSolidAndReleasesOwnedResources()
        {
            var shader = Shader.Find("Ecos/HauntLowPolyToon");
            Assert.IsNotNull(shader, "The low-poly palette shader must import before these tests run.");
            var root = new GameObject("Low-poly mesh fixture");
            var material = new Material(shader);
            var polygons = new HauntLowPolyPrimitives();
            try
            {
                foreach (LowPolyShape shape in Enum.GetValues(typeof(LowPolyShape)))
                {
                    var first = polygons.Part("Decorative " + shape, Vector3.zero, Vector3.one, material, root.transform, false, shape);
                    var second = polygons.Part("Second " + shape, Vector3.right * 2, Vector3.one, material, root.transform, false, shape);
                    var mesh = first.GetComponent<MeshFilter>().sharedMesh;
                    Assert.IsNotNull(mesh);
                    Assert.AreSame(mesh, second.GetComponent<MeshFilter>().sharedMesh, "Repeated parts reuse their normalized shape mesh.");
                    Assert.Greater(mesh.vertexCount, 0);
                    Assert.AreEqual(0, mesh.triangles.Length % 3);
                    Assert.AreEqual(mesh.vertexCount, mesh.normals.Length, "Faceted faces need explicit flat normals.");
                    Assert.IsTrue(mesh.vertices.All(v => !float.IsNaN(v.x) && !float.IsInfinity(v.x)
                        && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z)));
                    Assert.AreEqual(0, first.GetComponentsInChildren<Collider>(true).Count(c => c.enabled));
                }
                var cover = polygons.Part("Cover", Vector3.zero, new Vector3(2, 3, 1), material, root.transform, true, LowPolyShape.Box);
                Assert.AreEqual(1, cover.GetComponentsInChildren<Collider>(true).Count(c => c.enabled));
                var retainedMesh = cover.GetComponent<MeshFilter>().sharedMesh;
                polygons.Dispose();
                Assert.IsTrue(retainedMesh == null, "Disposal releases generated meshes even if a fixture still references them.");
                Assert.DoesNotThrow(polygons.Dispose);
            }
            finally
            {
                polygons.Dispose();
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
