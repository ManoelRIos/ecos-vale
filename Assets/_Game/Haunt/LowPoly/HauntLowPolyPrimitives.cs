using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt.LowPoly
{
    public enum LowPolyShape { BevelBox, Box, Cylinder, Cone, Sphere, Crystal, TaperedPrism }

    /// <summary>Small shared meshes with independent face normals. A game owns
    /// one factory; decorative parts never create physical collision by accident.</summary>
    public sealed class HauntLowPolyPrimitives : IDisposable
    {
        readonly Dictionary<LowPolyShape, Mesh> meshes = new Dictionary<LowPolyShape, Mesh>();
        bool disposed;
        public int MeshCount => meshes.Count;
        public bool IsDisposed => disposed;

        public GameObject Part(string name, Vector3 localPosition, Vector3 scale,
            Material material, Transform parent, bool solid = false, LowPolyShape? shape = null)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntLowPolyPrimitives));
            if (!material) throw new ArgumentNullException(nameof(material));
            LowPolyShape selected = shape ?? (solid ? LowPolyShape.Box : InferShape(name));
            Mesh mesh = GetMesh(selected);
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            // Architecture keeps cheap, predictable volume collision. The visual
            // bevel never reduces wall coverage or permits bullets through seams.
            if (solid) part.AddComponent<BoxCollider>();
            return part;
        }

        public static LowPolyShape InferShape(string name)
        {
            string key = (name ?? string.Empty).ToLowerInvariant();
            if (key.Contains("cristal") || key.Contains("crystal") || key.Contains("runa flutuante")) return LowPolyShape.Crystal;
            if (key.Contains("chama") || key.Contains("flame") || key.Contains("flaming") || key.Contains("spike") || key.Contains("espinho")) return LowPolyShape.Cone;
            if (key.Contains("orbe") || key.Contains("orb") || key.Contains("globo") || key.Contains("bolha") || key.Contains("bubble")) return LowPolyShape.Sphere;
            if (key.Contains("cajado") || key.Contains("staff") || key.Contains("haste") || key.Contains("shaft")
                || key.Contains("frasco") || key.Contains("flask") || key.Contains("bottle") || key.Contains("barrel") || key.Contains("barril"))
                return LowPolyShape.Cylinder;
            if (key.Contains("tronco") || key.Contains("torso") || key.Contains("manto") || key.Contains("robe") || key.Contains("crown"))
                return LowPolyShape.TaperedPrism;
            return LowPolyShape.BevelBox;
        }

        Mesh GetMesh(LowPolyShape shape)
        {
            if (meshes.TryGetValue(shape, out Mesh cached)) return cached;
            var builder = new Builder();
            switch (shape)
            {
                case LowPolyShape.Box: BuildBox(builder, false); break;
                case LowPolyShape.BevelBox: BuildBox(builder, true); break;
                case LowPolyShape.Cylinder: BuildColumn(builder, .5f, .5f); break;
                case LowPolyShape.TaperedPrism: BuildColumn(builder, .37f, .5f); break;
                case LowPolyShape.Cone: BuildColumn(builder, 0, .5f); break;
                case LowPolyShape.Sphere: BuildIcosahedron(builder); break;
                case LowPolyShape.Crystal: BuildCrystal(builder); break;
                default: throw new ArgumentOutOfRangeException(nameof(shape));
            }
            Mesh mesh = builder.Create("LowPoly / " + shape);
            meshes.Add(shape, mesh); return mesh;
        }

        sealed class Builder
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Vector3> normals = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> indices = new List<int>();

            public void Face(params Vector3[] points)
            {
                if (points.Length < 3) throw new ArgumentException("A mesh face needs three corners.");
                Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
                if (normal.sqrMagnitude < .5f) throw new InvalidOperationException("Degenerate low-poly face.");
                Vector3 center = Vector3.zero;
                foreach (Vector3 point in points) center += point;
                if (Vector3.Dot(normal, center) < 0)
                {
                    points = (Vector3[])points.Clone();
                    Array.Reverse(points); normal = -normal;
                }
                int first = vertices.Count;
                for (int i = 0; i < points.Length; i++)
                {
                    vertices.Add(points[i]); normals.Add(normal);
                    uv.Add(new Vector2(points[i].x + .5f, points[i].z + .5f));
                }
                for (int i = 1; i < points.Length - 1; i++)
                { indices.Add(first); indices.Add(first + i); indices.Add(first + i + 1); }
            }

            public Mesh Create(string name)
            {
                var mesh = new Mesh { name = name, hideFlags = HideFlags.HideAndDontSave };
                mesh.vertices = vertices.ToArray(); mesh.normals = normals.ToArray();
                mesh.uv = uv.ToArray(); mesh.triangles = indices.ToArray(); mesh.RecalculateBounds();
                return mesh;
            }
        }

        static void BuildBox(Builder mesh, bool beveled)
        {
            const float extent = .5f;
            float edge = beveled ? .37f : extent;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                mesh.Face(new Vector3(sign * extent, -edge, -edge), new Vector3(sign * extent, edge, -edge),
                    new Vector3(sign * extent, edge, edge), new Vector3(sign * extent, -edge, edge));
                mesh.Face(new Vector3(-edge, sign * extent, -edge), new Vector3(edge, sign * extent, -edge),
                    new Vector3(edge, sign * extent, edge), new Vector3(-edge, sign * extent, edge));
                mesh.Face(new Vector3(-edge, -edge, sign * extent), new Vector3(edge, -edge, sign * extent),
                    new Vector3(edge, edge, sign * extent), new Vector3(-edge, edge, sign * extent));
            }
            if (!beveled) return;
            for (int a = -1; a <= 1; a += 2) for (int b = -1; b <= 1; b += 2)
            {
                // Twelve edge quads connect the six inset square faces.
                mesh.Face(new Vector3(-edge, a * extent, b * edge), new Vector3(edge, a * extent, b * edge),
                    new Vector3(edge, a * edge, b * extent), new Vector3(-edge, a * edge, b * extent));
                mesh.Face(new Vector3(a * extent, -edge, b * edge), new Vector3(a * extent, edge, b * edge),
                    new Vector3(a * edge, edge, b * extent), new Vector3(a * edge, -edge, b * extent));
                mesh.Face(new Vector3(a * extent, b * edge, -edge), new Vector3(a * extent, b * edge, edge),
                    new Vector3(a * edge, b * extent, edge), new Vector3(a * edge, b * extent, -edge));
            }
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                mesh.Face(new Vector3(x * extent, y * edge, z * edge), new Vector3(x * edge, y * extent, z * edge),
                    new Vector3(x * edge, y * edge, z * extent));
        }

        static void BuildColumn(Builder mesh, float topRadius, float bottomRadius)
        {
            const int sides = 8;
            var top = new Vector3[sides]; var bottom = new Vector3[sides];
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2 / sides;
                top[side] = new Vector3(Mathf.Cos(angle) * topRadius, .5f, Mathf.Sin(angle) * topRadius);
                bottom[side] = new Vector3(Mathf.Cos(angle) * bottomRadius, -.5f, Mathf.Sin(angle) * bottomRadius);
            }
            mesh.Face(bottom);
            if (topRadius > 0) mesh.Face(top);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                if (topRadius > 0) mesh.Face(bottom[side], bottom[next], top[next], top[side]);
                else mesh.Face(bottom[side], bottom[next], new Vector3(0, .5f, 0));
            }
        }

        static void BuildCrystal(Builder mesh)
        {
            Vector3[] ring = { Vector3.right * .5f, Vector3.forward * .5f, Vector3.left * .5f, Vector3.back * .5f };
            for (int i = 0; i < ring.Length; i++)
            {
                Vector3 next = ring[(i + 1) % ring.Length];
                mesh.Face(ring[i], next, Vector3.up * .5f);
                mesh.Face(ring[i], next, Vector3.down * .5f);
            }
        }

        static void BuildIcosahedron(Builder mesh)
        {
            float phi = (1 + Mathf.Sqrt(5)) * .5f;
            Vector3[] vertices = {
                new Vector3(-1,phi,0),new Vector3(1,phi,0),new Vector3(-1,-phi,0),new Vector3(1,-phi,0),
                new Vector3(0,-1,phi),new Vector3(0,1,phi),new Vector3(0,-1,-phi),new Vector3(0,1,-phi),
                new Vector3(phi,0,-1),new Vector3(phi,0,1),new Vector3(-phi,0,-1),new Vector3(-phi,0,1)
            };
            // Equal axis extents keep the unit bounds useful to every caller.
            for (int i = 0; i < vertices.Length; i++) vertices[i] /= phi * 2;
            int[] faces = {0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,
                3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
            for (int i = 0; i < faces.Length; i += 3)
                mesh.Face(vertices[faces[i]], vertices[faces[i + 1]], vertices[faces[i + 2]]);
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (Mesh mesh in meshes.Values)
            {
                if (!mesh) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
                else UnityEngine.Object.DestroyImmediate(mesh);
            }
            meshes.Clear();
        }
    }
}
