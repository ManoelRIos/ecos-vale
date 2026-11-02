using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    // Shared, untextured meshes. Vertices are split per triangle for deliberate flat shading.
    public static class LowPolyMesh
    {
        static Mesh octa, cone, trunk, crown, mushroomCap;

        public static Mesh Octahedron
        {
            get
            {
                if (octa) return octa;
                Vector3[] points = { Vector3.up * .5f, Vector3.down * .5f,
                    Vector3.left * .5f, Vector3.right * .5f,
                    Vector3.forward * .5f, Vector3.back * .5f };
                int[] faces = { 0,4,3, 0,2,4, 0,5,2, 0,3,5,
                    1,3,4, 1,4,2, 1,2,5, 1,5,3 };
                var vertices = new List<Vector3>(24);
                for (int i = 0; i < faces.Length; i++) vertices.Add(points[faces[i]]);
                octa = CreateMesh("Octaedro low poly", vertices);
                return octa;
            }
        }

        public static Mesh Cone => cone ? cone : cone = Profile("Cone facetado", 7,
            new[] { -.5f, .5f }, new[] { .5f, 0f });
        public static Mesh Trunk => trunk ? trunk : trunk = Profile("Tronco afilado", 6,
            new[] { -.5f, .5f }, new[] { .5f, .28f });
        public static Mesh Crown => crown ? crown : crown = Profile("Copa facetada", 7,
            new[] { -.5f, -.23f, .17f, .43f, .55f }, new[] { 0f, .39f, .52f, .33f, 0f });
        public static Mesh MushroomCap => mushroomCap ? mushroomCap : mushroomCap = Profile("Chapeu de cogumelo", 8,
            new[] { -.2f, -.08f, .26f, .45f }, new[] { .45f, .54f, .32f, 0f });

        public static bool IsGenerated(Mesh mesh)
        {
            return mesh && (mesh == octa || mesh == cone || mesh == trunk || mesh == crown || mesh == mushroomCap);
        }

        static Vector3 RingPoint(float angle, float y, float radius)
        {
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        static Mesh Profile(string name, int sides, float[] heights, float[] radii)
        {
            var vertices = new List<Vector3>(sides * heights.Length * 6);
            for (int ring = 0; ring < heights.Length - 1; ring++)
            {
                for (int side = 0; side < sides; side++)
                {
                    float a = side * Mathf.PI * 2 / sides;
                    float b = (side + 1) * Mathf.PI * 2 / sides;
                    var lowerA = RingPoint(a, heights[ring], radii[ring]);
                    var lowerB = RingPoint(b, heights[ring], radii[ring]);
                    var upperA = RingPoint(a, heights[ring + 1], radii[ring + 1]);
                    var upperB = RingPoint(b, heights[ring + 1], radii[ring + 1]);
                    if (radii[ring + 1] > 0) Triangle(vertices, lowerA, upperA, upperB);
                    if (radii[ring] > 0) Triangle(vertices, lowerA, upperB, lowerB);
                }
            }
            // Bottom is clockwise from above; top is counterclockwise, both outward.
            for (int side = 0; side < sides; side++)
            {
                float a = side * Mathf.PI * 2 / sides;
                float b = (side + 1) * Mathf.PI * 2 / sides;
                if (radii[0] > 0)
                    Triangle(vertices, Vector3.up * heights[0], RingPoint(a, heights[0], radii[0]), RingPoint(b, heights[0], radii[0]));
                int top = heights.Length - 1;
                if (radii[top] > 0)
                    Triangle(vertices, Vector3.up * heights[top], RingPoint(b, heights[top], radii[top]), RingPoint(a, heights[top], radii[top]));
            }
            return CreateMesh(name, vertices);
        }

        static void Triangle(List<Vector3> vertices, Vector3 a, Vector3 b, Vector3 c)
        {
            vertices.Add(a); vertices.Add(b); vertices.Add(c);
        }

        static Mesh CreateMesh(string name, List<Vector3> vertices)
        {
            var triangles = new int[vertices.Count];
            for (int i = 0; i < triangles.Length; i++) triangles[i] = i;
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
