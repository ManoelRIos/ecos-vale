using UnityEngine;

namespace Ecos
{
    public static class LowPolyMesh
    {
        static Mesh octa;
        public static Mesh Octahedron
        {
            get
            {
                if (octa)
                    return octa;
                Vector3[] points =
                {
                    Vector3.up * .5f,
                    Vector3.down * .5f,
                    Vector3.left * .5f,
                    Vector3.right * .5f,
                    Vector3.forward * .5f,
                    Vector3.back * .5f
                };
                int[] faces =
                {
                    0,
                    4,
                    3,
                    0,
                    2,
                    4,
                    0,
                    5,
                    2,
                    0,
                    3,
                    5,
                    1,
                    3,
                    4,
                    1,
                    4,
                    2,
                    1,
                    2,
                    5,
                    1,
                    5,
                    3
                };
                var v = new Vector3[24];
                var t = new int[24];
                for (int i = 0; i < 24; i++)
                {
                    v[i] = points[faces[i]];
                    t[i] = i;
                }

                octa = new Mesh
                {
                    name = "Octaedro low poly"
                };
                octa.vertices = v;
                octa.triangles = t;
                octa.RecalculateNormals();
                octa.RecalculateBounds();
                return octa;
            }
        }
    }
}
