using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Ecos.Tests
{
    public sealed class VegetationTests
    {
        static readonly Color TestFoliage = new Color(.22f, .51f, .31f);

        [Test]
        public void EveryBiomeHasFourDifferentTreeGeometriesWithSharedMeshesAndBoundedLod()
        {
            var roots = new List<GameObject>();
            Palette palette = null;
            try
            {
                palette = new Palette();
                for (int biome = 0; biome < 6; biome++)
                {
                    var silhouettes = new HashSet<string>();
                    for (int variant = 0; variant < 4; variant++)
                    {
                        var tree = LowPolyFactory.Prop(palette, biome, 0, TestFoliage, variant);
                        roots.Add(tree);
                        ValidateLod(tree);
                        // Geometry excludes labels and materials: changing only a name/color cannot pass.
                        silhouettes.Add(GeometrySignature(tree));

                        var duplicate = LowPolyFactory.Prop(palette, biome, 0, TestFoliage, variant);
                        roots.Add(duplicate);
                        var originalMeshes = tree.GetComponentsInChildren<MeshFilter>();
                        var duplicateMeshes = duplicate.GetComponentsInChildren<MeshFilter>();
                        Assert.AreEqual(originalMeshes.Length, duplicateMeshes.Length);
                        for (int i = 0; i < originalMeshes.Length; i++)
                        {
                            Assert.AreSame(originalMeshes[i].sharedMesh, duplicateMeshes[i].sharedMesh,
                                "Repeated trees must reuse meshes rather than allocate geometry per tree.");
                            Assert.AreSame(originalMeshes[i].GetComponent<Renderer>().sharedMaterial,
                                duplicateMeshes[i].GetComponent<Renderer>().sharedMaterial);
                        }
                    }
                    Assert.AreEqual(4, silhouettes.Count, "Biome " + biome + " needs four actual tree shapes.");
                }
            }
            finally
            {
                foreach (var root in roots) if (root) Object.DestroyImmediate(root);
                if (palette != null) palette.Dispose();
            }
        }

        [Test]
        public void ForestFloorFamiliesKeepFourShapesAndPaintedMushroomCapsWithoutEmission()
        {
            var roots = new List<GameObject>();
            Palette palette = null;
            try
            {
                palette = new Palette();
                for (int biome = 0; biome < 6; biome++)
                    for (int kind = 1; kind <= 3; kind++)
                    {
                        var shapes = new HashSet<string>();
                        for (int variant = 0; variant < 4; variant++)
                        {
                            var detail = LowPolyFactory.Prop(palette, biome, kind, TestFoliage, variant);
                            roots.Add(detail);
                            ValidateLod(detail);
                            shapes.Add(GeometrySignature(detail));
                            foreach (var filter in detail.GetComponentsInChildren<MeshFilter>())
                                if (filter.sharedMesh == LowPolyMesh.MushroomCap)
                                    Assert.IsFalse(filter.GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("_EMISSION"),
                                        "The broad painted cap must keep its color; only small underside details glow.");
                        }
                        Assert.AreEqual(4, shapes.Count, "Biome " + biome + ", ground family " + kind + " needs four shapes.");
                    }
            }
            finally
            {
                foreach (var root in roots) if (root) Object.DestroyImmediate(root);
                if (palette != null) palette.Dispose();
            }
        }

        static void ValidateLod(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            Assert.Greater(renderers.Length, 0, root.name);
            Assert.LessOrEqual(renderers.Length, 12, root.name + " exceeds the decoration rendering budget.");
            Assert.IsEmpty(root.GetComponentsInChildren<Collider>(), "Decorative flora must not create physical blockers.");
            var group = root.GetComponent<LODGroup>();
            Assert.IsNotNull(group, root.name);
            var levels = group.GetLODs();
            Assert.AreEqual(2, levels.Length, root.name);
            var near = new HashSet<Renderer>(levels[0].renderers);
            Assert.AreEqual(renderers.Length, near.Count, "The near LOD must contain every visual part.");
            Assert.Greater(levels[1].renderers.Length, 0, root.name);
            Assert.Less(levels[1].renderers.Length, levels[0].renderers.Length,
                "The distant LOD must actually reduce the number of parts.");
            Assert.Greater(levels[0].screenRelativeTransitionHeight, levels[1].screenRelativeTransitionHeight);
            foreach (var renderer in renderers)
            {
                Assert.IsTrue(near.Contains(renderer));
                Assert.IsNotNull(renderer.sharedMaterial);
                var filter = renderer.GetComponent<MeshFilter>();
                Assert.IsNotNull(filter);
                Assert.IsNotNull(filter.sharedMesh);
                Assert.Greater(filter.sharedMesh.vertexCount, 0);
            }
            foreach (var renderer in levels[1].renderers)
            {
                Assert.IsNotNull(renderer);
                Assert.IsTrue(near.Contains(renderer), "Far LOD must refer to existing parts of this prop.");
            }
        }

        static string GeometrySignature(GameObject root)
        {
            var pieces = new List<string>();
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                var matrix = root.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                var piece = new StringBuilder().Append(filter.sharedMesh.GetInstanceID()).Append(':');
                for (int row = 0; row < 4; row++)
                    for (int column = 0; column < 4; column++)
                        piece.Append(matrix[row, column].ToString("G9", CultureInfo.InvariantCulture)).Append(',');
                pieces.Add(piece.ToString());
            }
            pieces.Sort(System.StringComparer.Ordinal);
            return string.Join("|", pieces);
        }
    }
}
