using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public static class LowPolyFactory
    {
        static readonly Color bark = new Color(.19f, .12f, .16f);
        static readonly Color stone = new Color(.32f, .35f, .39f);
        static readonly Color wood = new Color(.32f, .20f, .13f);
        static readonly Color gold = new Color(1, .53f, .12f);

        static GameObject Shape(Palette p, Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Color color, bool glow = false)
        {
            var part = p.Part(parent, name, PrimitiveType.Sphere, position, scale, color, glow);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            return part;
        }

        static GameObject Limb(Palette p, Transform parent, string name, Vector3 position, float length, Color color, bool leg)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = position;
            p.Part(pivot.transform, "Tecido", PrimitiveType.Cube, new Vector3(0, -length * .5f, 0),
                new Vector3(leg ? .20f : .17f, length, .22f), color);
            p.Part(pivot.transform, leg ? "Bota" : "Luva", PrimitiveType.Cube,
                new Vector3(0, -length, leg ? .055f : 0),
                leg ? new Vector3(.25f, .17f, .34f) : new Vector3(.19f, .15f, .22f), bark);
            return pivot;
        }

        public static GameObject Player(Palette p)
        {
            var g = new GameObject("Viajante");
            var root = g.transform;
            Shape(p, root, "Casaco", LowPolyMesh.Trunk, new Vector3(0, 1.02f, 0), new Vector3(.63f, .78f, .48f), new Color(.24f, .19f, .36f));
            p.Part(root, "Capa dos ombros", PrimitiveType.Cube, new Vector3(0, 1.30f, 0), new Vector3(.72f, .18f, .47f), new Color(.30f, .23f, .42f));
            Shape(p, root, "Rosto", LowPolyMesh.Crown, new Vector3(0, 1.59f, .015f), new Vector3(.40f, .43f, .38f), new Color(.78f, .61f, .46f));
            p.Part(root, "Aba do chapéu", PrimitiveType.Cylinder, new Vector3(0, 1.79f, 0), new Vector3(.76f, .035f, .66f), bark);
            var hat = Shape(p, root, "Chapéu pontudo", LowPolyMesh.Cone, new Vector3(.035f, 1.98f, 0), new Vector3(.44f, .42f, .44f), new Color(.16f, .13f, .23f));
            hat.transform.localRotation = Quaternion.Euler(0, 0, -9);
            p.Part(root, "Lenço", PrimitiveType.Cube, new Vector3(0, 1.40f, .20f), new Vector3(.42f, .10f, .13f), new Color(.8f, .31f, .13f));
            p.Part(root, "Mochila", PrimitiveType.Cube, new Vector3(0, 1.12f, -.27f), new Vector3(.40f, .45f, .23f), wood);
            p.Part(root, "Fivela", PrimitiveType.Cube, new Vector3(0, .96f, .25f), new Vector3(.12f, .12f, .04f), new Color(.68f, .48f, .19f));
            for (int side = -1; side <= 1; side += 2)
            {
                p.Part(root, "Olho", PrimitiveType.Cube, new Vector3(side * .075f, 1.62f, .20f), new Vector3(.034f, .045f, .02f), bark);
                var arm = Limb(p, root, "Arm" + side, new Vector3(side * .36f, 1.30f, 0), .48f, new Color(.29f, .25f, .40f), false);
                if (side > 0) Lantern(p, arm.transform, new Vector3(.07f, -.61f, .12f), .28f);
                Limb(p, root, "Leg" + side, new Vector3(side * .15f, .77f, 0), .63f, new Color(.18f, .21f, .26f), true);
            }
            return g;
        }

        static void Lantern(Palette p, Transform root, Vector3 position, float size)
        {
            p.Part(root, "Luz da lanterna", PrimitiveType.Cube, position, new Vector3(size * .7f, size, size * .7f), gold, true);
            p.Part(root, "Topo da lanterna", PrimitiveType.Cube, position + Vector3.up * size * .58f, new Vector3(size, size * .18f, size), bark);
            p.Part(root, "Base da lanterna", PrimitiveType.Cube, position - Vector3.up * size * .58f, new Vector3(size, size * .18f, size), bark);
        }

        static Renderer Segment(Palette p, Transform root, string name, Vector3 from, Vector3 to, float width, Color color)
        {
            var branch = Shape(p, root, name, LowPolyMesh.Trunk, (from + to) * .5f, new Vector3(width, (to - from).magnitude, width), color);
            branch.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
            return branch.GetComponent<Renderer>();
        }

        static Renderer Crown(Palette p, Transform root, Vector3 position, Vector3 scale, Color color, bool pine = false)
        {
            return Shape(p, root, pine ? "Ramo de pinheiro" : "Copa", pine ? LowPolyMesh.Cone : LowPolyMesh.Crown, position, scale, color).GetComponent<Renderer>();
        }

        static void Tree(Palette p, Transform root, int biome, Color foliage, List<Renderer> far)
        {
            bool haunted = biome == 0, swamp = biome == 3, volcanic = biome == 5;
            Color trunkColor = volcanic ? new Color(.12f, .11f, .13f) : bark;
            float height = biome == 4 ? 6 : biome == 2 ? 5.2f : 4.5f;
            far.Add(Segment(p, root, "Tronco", Vector3.zero, new Vector3(.18f, height * .68f, 0), swamp ? .85f : .64f, trunkColor));
            far.Add(Segment(p, root, "Tronco inclinado", new Vector3(.18f, height * .60f, 0), new Vector3(-.26f, height, .12f), .43f, trunkColor));

            if (biome == 2)
            {
                for (int i = 0; i < 3; i++)
                {
                    float width = 4.0f - i * .87f;
                    var pos = new Vector3(0, 2.5f + i * 1.18f, 0);
                    Crown(p, root, pos, new Vector3(width, 3, width), new Color(.14f, .29f, .30f), true);
                    far.Add(Crown(p, root, pos + Vector3.up * .31f, new Vector3(width * .79f, 2.5f, width * .79f), new Color(.80f, .88f, .91f), true));
                }
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                float angle = i * Mathf.PI * 2 / 3 + .3f;
                Vector3 start = new Vector3(.1f, height * (.55f + i * .08f), 0);
                Vector3 tip = new Vector3(Mathf.Cos(angle) * 1.6f, height * (.82f + i * .08f), Mathf.Sin(angle) * 1.6f);
                var limb = Segment(p, root, "Galho", start, tip, .27f, trunkColor);
                if (haunted || volcanic)
                    far.Add(limb);
                if (haunted || volcanic)
                {
                    var twig = tip + new Vector3(Mathf.Cos(angle) * .55f, .95f, Mathf.Sin(angle) * .55f);
                    far.Add(Segment(p, root, "Ponta retorcida", tip, twig, .16f, trunkColor));
                }
                else
                {
                    Color leaves = biome == 1 ? Color.Lerp(foliage, new Color(.76f, .58f, .22f), .62f)
                        : swamp ? new Color(.22f, .37f, .27f) : Color.Lerp(foliage, new Color(.15f, .49f, .37f), .55f);
                    far.Add(Crown(p, root, tip + Vector3.up * .5f, new Vector3(biome == 4 ? 4.2f : 3.2f, 2.5f, 3.2f), leaves));
                }
            }
            if (swamp || haunted)
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = i * Mathf.PI * 2 / 3;
                    Segment(p, root, "Raiz", new Vector3(0, .8f, 0), new Vector3(Mathf.Cos(angle) * 1.3f, .08f, Mathf.Sin(angle) * 1.3f), .3f, trunkColor);
                }
            }
            if (haunted) Lantern(p, root, new Vector3(.9f, 2.55f, .3f), .32f);
            if (volcanic)
                Shape(p, root, "Brasa", LowPolyMesh.Octahedron, new Vector3(.1f, .8f, .25f), new Vector3(.19f, 1.2f, .12f), new Color(.95f, .24f, .035f), true);
            if (biome == 4)
                Shape(p, root, "Cristal entre raizes", LowPolyMesh.Octahedron, new Vector3(.8f, .55f, .2f), new Vector3(.36f, 1.05f, .36f), new Color(.22f, .78f, .72f), true);
        }

        static void Plant(Palette p, Transform root, int biome)
        {
            if (biome == 0)
            {
                Shape(p, root, "Abóbora", LowPolyMesh.Crown, new Vector3(0, .45f, 0), new Vector3(.9f, .85f, .9f), new Color(.79f, .26f, .045f));
                Segment(p, root, "Talo", new Vector3(0, .84f, 0), new Vector3(.08f, 1.02f, 0), .13f, new Color(.22f, .31f, .14f));
                for (int side = -1; side <= 1; side += 2)
                    p.Part(root, "Olho luminoso", PrimitiveType.Cube, new Vector3(side * .16f, .57f, .43f), new Vector3(.13f, .13f, .055f), gold, true);
                p.Part(root, "Sorriso luminoso", PrimitiveType.Cube, new Vector3(0, .34f, .43f), new Vector3(.30f, .07f, .055f), gold, true);
                return;
            }
            Color cap = biome == 5 ? new Color(.92f, .24f, .05f)
                : biome == 4 ? new Color(.3f, .76f, .75f)
                : biome == 3 ? new Color(.35f, .59f, .32f)
                : biome == 2 ? new Color(.65f, .82f, .87f)
                : new Color(.71f, .42f, .23f);
            Segment(p, root, "Caule", Vector3.zero, Vector3.up * .65f, .16f, new Color(.67f, .68f, .48f));
            Shape(p, root, "Chapéu do cogumelo", LowPolyMesh.MushroomCap, new Vector3(0, .65f, 0), new Vector3(1.1f, .7f, 1.1f), cap, biome == 3 || biome == 4 || biome == 5);
            if (biome == 4)
            {
                p.Part(root, "Pinta", PrimitiveType.Cube, new Vector3(.18f, .88f, .08f), Vector3.one * .12f, new Color(.83f, .96f, .78f));
                p.Part(root, "Pinta", PrimitiveType.Cube, new Vector3(-.16f, .83f, .15f), Vector3.one * .10f, new Color(.83f, .96f, .78f));
            }
        }

        public static GameObject Prop(Palette p, int biome, int kind, Color foliage)
        {
            var g = new GameObject(kind == 0 ? "Árvore" : "Rocha / vegetação");
            var far = new List<Renderer>();
            if (kind == 0) Tree(p, g.transform, biome, foliage, far);
            else if (kind == 1)
            {
                Color rockColor = biome == 2 ? new Color(.57f, .67f, .73f) : biome == 5 ? new Color(.20f, .18f, .20f) : stone;
                var rock = Shape(p, g.transform, "Rocha", LowPolyMesh.Octahedron, new Vector3(0, .58f, 0), new Vector3(1.8f, 1.5f, 1.4f), rockColor);
                rock.transform.localRotation = Quaternion.Euler(15, 27, -12);
                far.Add(rock.GetComponent<Renderer>());
                Shape(p, g.transform, "Fragmento", LowPolyMesh.Octahedron, new Vector3(.73f, .24f, .18f), new Vector3(.8f, .7f, .9f), rockColor * .84f);
                Shape(p, g.transform, "Musgo / cinza", LowPolyMesh.Octahedron, new Vector3(-.23f, .82f, .05f), new Vector3(.9f, .25f, .8f), biome == 5 ? new Color(.40f, .27f, .22f) : foliage);
            }
            else Plant(p, g.transform, biome);
            var renderers = g.GetComponentsInChildren<Renderer>();
            var lod = g.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(kind == 0 ? .045f : .035f, renderers), new LOD(kind == 0 ? .006f : .008f, far.Count > 0 ? far.ToArray() : renderers) });
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
