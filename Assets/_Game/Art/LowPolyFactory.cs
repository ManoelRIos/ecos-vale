using UnityEngine;

namespace Ecos
{
    public static class LowPolyFactory
    {
        static readonly Color bark = new Color(.18f, .12f, .16f), stone = new Color(.31f, .33f, .39f), wood = new Color(.28f, .19f, .14f), gold = new Color(1, .44f, .07f);
        public static GameObject Player(Palette p)
        {
            var g = new GameObject("Viajante");
            p.Part(g.transform, "Casaco", PrimitiveType.Cylinder, new Vector3(0, .95f, 0), new Vector3(.56f, .38f, .4f), new Color(.23f, .17f, .34f));
            p.Part(g.transform, "Rosto", PrimitiveType.Sphere, new Vector3(0, 1.58f, 0), Vector3.one * .34f, new Color(.71f, .55f, .43f));
            p.Part(g.transform, "Chapéu", PrimitiveType.Cylinder, new Vector3(0, 1.78f, 0), new Vector3(.62f, .05f, .6f), bark);
            p.Part(g.transform, "Mochila", PrimitiveType.Cube, new Vector3(0, 1.1f, -.24f), new Vector3(.35f, .4f, .22f), wood);
            for (int i = -1; i <= 1; i += 2)
            {
                p.Part(g.transform, "Arm" + i, PrimitiveType.Cube, new Vector3(i * .36f, 1.02f, 0), new Vector3(.16f, .58f, .18f), new Color(.24f, .23f, .32f));
                p.Part(g.transform, "Leg" + i, PrimitiveType.Cube, new Vector3(i * .15f, .35f, 0), new Vector3(.21f, .66f, .23f), bark);
            }

            return g;
        }

        public static GameObject Prop(Palette p, int biome, int kind, Color foliage)
        {
            var g = new GameObject(kind == 0 ? "Árvore" : "Rocha / vegetação");
            if (kind == 0)
            {
                p.Part(g.transform, "Tronco", PrimitiveType.Cylinder, new Vector3(0, 2, 0), new Vector3(.5f, 2, .5f), bark);
                for (int i = 0; i < 3; i++)
                {
                    var branch = p.Part(g.transform, "Galho", PrimitiveType.Cube, new Vector3((i - 1) * .65f, 3 + i * .6f, 0), new Vector3(.3f, 2, .3f), bark);
                    branch.transform.localRotation = Quaternion.Euler(0, i * 70, 35 * (i - 1));
                    if (biome != 0 && biome != 5)
                        p.Part(g.transform, "Copa", PrimitiveType.Sphere, new Vector3((i - 1) * .8f, 4 + i * .6f, 0), new Vector3(3.5f, biome == 2 ? 3.7f : 2, 2.7f), foliage);
                }
            }
            else if (kind == 1)
            {
                var rock = p.Part(g.transform, "Pedra", PrimitiveType.Sphere, new Vector3(0, .55f, 0), new Vector3(1.7f, 1.2f, 1.3f), stone);
                rock.transform.localRotation = Quaternion.Euler(20, 37, 15);
            }
            else
            {
                p.Part(g.transform, "Caule", PrimitiveType.Cylinder, new Vector3(0, .3f, 0), new Vector3(.12f, .3f, .12f), wood);
                p.Part(g.transform, biome == 0 ? "Abóbora" : "Cogumelo", PrimitiveType.Sphere, new Vector3(0, .65f, 0), new Vector3(.8f, .4f, .8f), biome == 0 ? gold : biome == 5 ? new Color(1, .18f, .03f) : new Color(.24f, .85f, .78f), true);
            }

            var renderers = g.GetComponentsInChildren<Renderer>();
            var lod = g.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(.025f, renderers), new LOD(.006f, kind == 0 ? new Renderer[] { renderers[0] } : renderers) });
            lod.RecalculateBounds();
            return g;
        }

        public static GameObject Shrine(Palette p, int index)
        {
            var g = new GameObject("Santuário " + index);
            p.Part(g.transform, "Degrau", PrimitiveType.Cylinder, new Vector3(0, .15f, 0), new Vector3(8, .15f, 8), stone, false, true);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * .5f;
                p.Part(g.transform, "Coluna", PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 2.8f, 2, Mathf.Sin(a) * 2.8f), new Vector3(.65f, 4, .65f), stone, false, true);
            }

            if (index % 3 == 0)
            {
                p.Part(g.transform, "Arco", PrimitiveType.Cube, new Vector3(0, 4, 0), new Vector3(6, .7f, 1), stone);
                p.Part(g.transform, "Torre", PrimitiveType.Cylinder, new Vector3(-4, 3, -3), new Vector3(2, 3, 2), stone, false, true);
            }
            else if (index % 3 == 1)
            {
                for (int i = 0; i < 5; i++)
                    p.Part(g.transform, "Lápide", PrimitiveType.Cube, new Vector3(i * 1.2f - 2, .8f, 4), new Vector3(.6f, 1.6f, .25f), stone, false, true);
            }
            else
            {
                p.Part(g.transform, "Cabana", PrimitiveType.Cube, new Vector3(0, 1.5f, 6), new Vector3(5, 3, 3), wood, false, true);
                var roof = p.Part(g.transform, "Telhado torto", PrimitiveType.Cube, new Vector3(0, 3.5f, 6), new Vector3(5.6f, .35f, 4), bark);
                roof.transform.localRotation = Quaternion.Euler(0, 0, -12);
            }

            p.Part(g.transform, "Altar", PrimitiveType.Cube, new Vector3(0, .7f, 0), new Vector3(1.6f, 1.3f, 1.6f), stone, false, true);
            var gem = p.Part(g.transform, "Eco", PrimitiveType.Cube, new Vector3(0, 2, 0), Vector3.one * .65f, new Color(.35f, .74f, 1), true);
            gem.transform.localRotation = Quaternion.Euler(35, 45, 20);
            var light = gem.AddComponent<Light>();
            light.color = new Color(.3f, .6f, 1);
            light.range = 12;
            light.intensity = 2;
            return g;
        }
    }
}
