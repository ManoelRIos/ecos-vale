using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt.Fps
{
    /// <summary>Turns a creature's 2D sprite into a real 3D body. The silhouette is inflated
    /// into a rounded volume (thick in the middle, thin at the edges), the front is painted with
    /// the front pose and the back with the back pose, so the 3D creature keeps exactly the
    /// shape, palette and features of its 2D drawing. One closed mesh per animation frame,
    /// built on first use and shared by every creature of that kind.</summary>
    public sealed class HauntFpsSculpture : IDisposable
    {
        /// <summary>Half thickness, in sprite pixels, reached by the thickest parts.</summary>
        public const float MaxBulge = 11;
        readonly Dictionary<Texture2D, Color32[]> texels = new Dictionary<Texture2D, Color32[]>();
        readonly Dictionary<long, Mesh> meshes = new Dictionary<long, Mesh>();
        readonly Dictionary<Texture2D, Material> materials = new Dictionary<Texture2D, Material>();
        bool disposed;

        public int MeshCount => meshes.Count;

        /// <summary>The lit material that paints a sculpture with its sprite sheet.</summary>
        public Material MaterialFor(Sprite sprite)
        {
            if (disposed || !sprite || !sprite.texture) return null;
            if (materials.TryGetValue(sprite.texture, out var existing) && existing) return existing;
            var template = Resources.Load<Material>("FpsPalette");
            if (!template) return null;
            var material = new Material(template) { name = "Escultura / " + sprite.texture.name };
            material.SetTexture("_BaseMap", sprite.texture);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_EmissionColor", new Color(.05f, .045f, .06f));
            materials[sprite.texture] = material;
            return material;
        }

        /// <summary>A closed 3D body for a front sprite, painted on its back with
        /// <paramref name="back"/> (mirrored) or with the front itself.</summary>
        /// <param name="scale">World size of one sprite pixel times its pixels per unit (the
        /// same scale the flat sprite would use).</param>
        public Mesh Build(Sprite front, Sprite back, float scale)
        {
            if (disposed || !front || !front.texture) return null;
            long key = ((long)front.GetInstanceID() << 32) ^ (uint)(back ? back.GetInstanceID() : 0) ^ ((long)Mathf.RoundToInt(scale * 1000) << 20);
            if (meshes.TryGetValue(key, out var cached) && cached) return cached;
            var mesh = Sculpt(front, back && back.texture == front.texture && back.rect.size == front.rect.size ? back : null, scale);
            meshes[key] = mesh;
            return mesh;
        }

        Color32[] Texels(Texture2D texture)
        {
            if (texels.TryGetValue(texture, out var pixels)) return pixels;
            if (texture.isReadable) pixels = texture.GetPixels32();
            else
            {
                // Sheets are imported without CPU access: read them back once through the GPU.
                var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(texture, target);
                var previous = RenderTexture.active;
                RenderTexture.active = target;
                var copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                copy.Apply(false);
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                pixels = copy.GetPixels32();
                Destroy(copy);
            }
            texels[texture] = pixels;
            return pixels;
        }

        Mesh Sculpt(Sprite front, Sprite back, float scale)
        {
            Texture2D texture = front.texture;
            Color32[] source = Texels(texture);
            Rect rect = front.textureRect;
            int x0 = Mathf.RoundToInt(rect.x), y0 = Mathf.RoundToInt(rect.y);
            int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
            Rect backRect = back ? back.textureRect : rect;
            var solid = new bool[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int sx = x0 + x, sy = y0 + y;
                solid[y * w + x] = sx >= 0 && sy >= 0 && sx < texture.width && sy < texture.height && source[sy * texture.width + sx].a > 96;
            }
            float[] distance = DistanceToEdge(solid, w, h);
            float deepest = 0;
            foreach (float d in distance) deepest = Mathf.Max(deepest, d);
            float radius = Mathf.Clamp(deepest, 2, MaxBulge);
            // Circular profile: rises quickly from the silhouette, flattens in the middle.
            var bulge = new float[w * h];
            for (int i = 0; i < bulge.Length; i++)
            {
                if (!solid[i]) continue;
                float t = Mathf.Min(distance[i], radius) / radius;
                bulge[i] = radius * Mathf.Sqrt(Mathf.Max(0, 1 - (1 - t) * (1 - t)));
            }
            float pixel = scale / front.pixelsPerUnit;
            Vector2 pivot = front.pivot;
            var vertices = new List<Vector3>(); var uvs = new List<Vector2>();
            var frontIndex = new int[(w + 1) * (h + 1)]; var backIndex = new int[(w + 1) * (h + 1)];
            for (int i = 0; i < frontIndex.Length; i++) frontIndex[i] = backIndex[i] = -1;
            int Corner(int cx, int cy, bool isFront)
            {
                int slot = cy * (w + 1) + cx;
                int[] table = isFront ? frontIndex : backIndex;
                if (table[slot] >= 0) return table[slot];
                // A corner on the silhouette has no thickness, so front and back meet there.
                float sum = 0; int count = 0; bool edge = false;
                for (int oy = -1; oy <= 0; oy++) for (int ox = -1; ox <= 0; ox++)
                {
                    int px = cx + ox, py = cy + oy;
                    if (px < 0 || py < 0 || px >= w || py >= h || !solid[py * w + px]) { edge = true; continue; }
                    sum += bulge[py * w + px]; count++;
                }
                float depth = edge || count == 0 ? 0 : sum / count;
                var position = new Vector3((cx - pivot.x) * pixel, (cy - pivot.y) * pixel, (isFront ? depth : -depth) * pixel);
                vertices.Add(position);
                if (isFront) uvs.Add(new Vector2((x0 + cx) / (float)texture.width, (y0 + cy) / (float)texture.height));
                // Seen from behind, world X still grows to the viewer's right, so the back
                // pose is painted unmirrored.
                else uvs.Add(new Vector2((backRect.x + cx) / texture.width, (backRect.y + cy) / texture.height));
                table[slot] = vertices.Count - 1;
                return table[slot];
            }
            var triangles = new List<int>();
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (!solid[y * w + x]) continue;
                int a = Corner(x, y, true), b = Corner(x + 1, y, true), c = Corner(x + 1, y + 1, true), d = Corner(x, y + 1, true);
                // The sculpture faces +Z, the direction its creature looks (Unity front faces
                // satisfy cross(b - a, c - a) pointing at the viewer).
                triangles.Add(a); triangles.Add(b); triangles.Add(c); triangles.Add(a); triangles.Add(c); triangles.Add(d);
                a = Corner(x, y, false); b = Corner(x + 1, y, false); c = Corner(x + 1, y + 1, false); d = Corner(x, y + 1, false);
                triangles.Add(a); triangles.Add(c); triangles.Add(b); triangles.Add(a); triangles.Add(d); triangles.Add(c);
            }
            var mesh = new Mesh { name = "Escultura / " + front.name };
            if (vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Approximate Euclidean distance (in pixels) from each solid pixel to the
        /// nearest empty pixel, by two chamfer passes.</summary>
        static float[] DistanceToEdge(bool[] solid, int w, int h)
        {
            const float Straight = 1, Diagonal = 1.4142f;
            var d = new float[w * h];
            for (int i = 0; i < d.Length; i++) d[i] = solid[i] ? 1e6f : 0;
            float At(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? 0 : d[y * w + x];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int i = y * w + x; if (!solid[i]) continue;
                d[i] = Mathf.Min(d[i], Mathf.Min(At(x - 1, y) + Straight, Mathf.Min(At(x, y - 1) + Straight,
                    Mathf.Min(At(x - 1, y - 1) + Diagonal, At(x + 1, y - 1) + Diagonal))));
            }
            for (int y = h - 1; y >= 0; y--) for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x; if (!solid[i]) continue;
                d[i] = Mathf.Min(d[i], Mathf.Min(At(x + 1, y) + Straight, Mathf.Min(At(x, y + 1) + Straight,
                    Mathf.Min(At(x + 1, y + 1) + Diagonal, At(x - 1, y + 1) + Diagonal))));
            }
            return d;
        }

        static void Destroy(UnityEngine.Object value)
        {
            if (!value) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (var mesh in meshes.Values) Destroy(mesh);
            foreach (var material in materials.Values) Destroy(material);
            meshes.Clear(); materials.Clear(); texels.Clear();
        }
    }
}
