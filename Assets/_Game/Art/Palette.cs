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
            terrain = new Material(RequireShader("Ecos/Terrain"));
            terrain.name = "Ecos terreno (runtime)";
        }

        static Shader RequireShader(string name)
        {
            var shader = Shader.Find(name);
            if (!shader)
                throw new System.InvalidOperationException("Shader ausente: " + name + ". Pare o Play e use Ecos > Reparar renderização URP.");
            return shader;
        }

        public Material Get(Color c, bool glow = false)
        {
            Color key = glow ? new Color(c.r, c.g, c.b, .99f) : c;
            if (materials.TryGetValue(key, out var m))
                return m;
            m = new Material(RequireShader("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", .12f);
            m.enableInstancing = true;
            if (glow)
            {
                m.EnableKeyword("_EMISSION");
                // A bright Lit surface plus HDR emission used to clip every channel to white.
                // Preserve the hue with a low emission budget; lights provide the actual lighting.
                Color emission = c * .55f;
                emission.a = 1;
                m.SetColor("_EmissionColor", emission);
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
            foreach (var m in materials.Values) Release(m);
            Release(terrain);
        }

        static void Release(Material material)
        {
            if (!material) return;
            if (Application.isPlaying) Object.Destroy(material);
            else Object.DestroyImmediate(material);
        }
    }
}
