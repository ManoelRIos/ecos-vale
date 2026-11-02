using System.Threading;
using UnityEngine;

namespace Ecos
{
    public sealed class ChunkData
    {
        public Vector3[] vertices;
        public Color[] colors;
        public Vector3[] normals;
        public int[] triangles;
    }

    public static class TerrainMeshGenerator
    {
        public static ChunkData Generate(int cx, int cz, int size, int resolution, int seed, Color[] palette, CancellationToken token)
        {
            int width = resolution + 1;
            var d = new ChunkData
            {
                vertices = new Vector3[width * width],
                colors = new Color[width * width],
                normals = new Vector3[width * width],
                triangles = new int[resolution * resolution * 6]
            };
            for (int z = 0; z <= resolution; z++)
            {
                token.ThrowIfCancellationRequested();
                for (int x = 0; x <= resolution; x++)
                {
                    int i = z * width + x;
                    float lx = x * (float)size / resolution, lz = z * (float)size / resolution, wx = cx * size + lx, wz = cz * size + lz;
                    d.vertices[i] = new Vector3(lx, WorldMath.Height(wx, wz, seed), lz);
                    float dx = WorldMath.Height(wx - 1, wz, seed) - WorldMath.Height(wx + 1, wz, seed), dz = WorldMath.Height(wx, wz - 1, seed) - WorldMath.Height(wx, wz + 1, seed);
                    d.normals[i] = new Vector3(dx, 2, dz).normalized;
                    float c = WorldMath.Climate(wx, wz, seed);
                    int b = (int)c;
                    float f = c - b;
                    Color col = Color.Lerp(palette[(b + 5) % 6], palette[b], Mathf.SmoothStep(0, 1, Mathf.Clamp01(f * 5)));
                    if (WorldMath.PathDistance(wx, wz) < 3)
                        col = Color.Lerp(col, new Color(.40f, .31f, .25f), .7f);
                    d.colors[i] = col;
                }
            }

            int t = 0;
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                {
                    int i = z * width + x;
                    d.triangles[t++] = i;
                    d.triangles[t++] = i + width;
                    d.triangles[t++] = i + 1;
                    d.triangles[t++] = i + 1;
                    d.triangles[t++] = i + width;
                    d.triangles[t++] = i + width + 1;
                }

            return d;
        }
    }
}
