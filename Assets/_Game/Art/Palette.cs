using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public sealed class Palette
    {
        readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        public Material terrain;
        public Palette()
        {
            var saved = Resources.Load<Material>("TerrainMaterial");
            terrain = saved ? new Material(saved) : new Material(Shader.Find("Ecos/Terrain"));
        }

        public Material Get(Color c, bool glow = false)
        {
            Color key = glow ? new Color(c.r, c.g, c.b, .99f) : c;
            if (materials.TryGetValue(key, out var m))
                return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.color = c;
            m.enableInstancing = true;
            if (glow)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", c * 2.5f);
            }

            materials[key] = m;
            return m;
        }

        public GameObject Part(Transform parent, string name, PrimitiveType shape, Vector3 p, Vector3 scale, Color c, bool glow = false, bool collider = false)
        {
            var g = GameObject.CreatePrimitive(shape);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = p;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = Get(c, glow);
            if (shape == PrimitiveType.Sphere)
                g.GetComponent<MeshFilter>().sharedMesh = LowPolyMesh.Octahedron;
            if (!collider)
            {
                if (Application.isPlaying) Object.Destroy(g.GetComponent<Collider>());
                else Object.DestroyImmediate(g.GetComponent<Collider>());
            }
            return g;
        }

        public void Dispose()
        {
            foreach (var m in materials.Values)
                Object.Destroy(m);
            Object.Destroy(terrain);
        }
    }
}
