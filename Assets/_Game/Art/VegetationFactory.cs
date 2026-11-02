using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>Four reusable silhouettes per prop family. Uses only shared meshes and Palette materials.</summary>
    public static class VegetationFactory
    {
        static readonly Color Bark = new Color(.23f, .15f, .18f);
        static readonly Color CutWood = new Color(.66f, .43f, .23f);
        static readonly Color Rock = new Color(.34f, .39f, .43f);
        static readonly Color Snow = new Color(.71f, .85f, .90f);
        static readonly Color Ember = new Color(.91f, .25f, .06f);
        static readonly Color Amber = new Color(.95f, .47f, .11f);
        static readonly string[] FloorNames = { "Pedras musgosas", "Tronco caído", "Toco antigo", "Pedra e cristais" };
        static readonly string[,] TreeNames =
        {
            { "Carvalho das lanternas", "Espinheiro violeta", "Tronco retorcido", "Árvore dos corvos" },
            { "Carvalho dourado", "Bétula do sol", "Carvalho jovem", "Árvore inclinada" },
            { "Pinheiro nevado", "Abeto esguio", "Pinheiro de copa aberta", "Pinheiro jovem" },
            { "Cipreste de raízes", "Álamo do brejo", "Mangue jovem", "Salgueiro chorão" },
            { "Carvalho esmeralda", "Árvore ametista", "Broto encantado", "Salgueiro turquesa" },
            { "Carvalho carbonizado", "Espinheiro de cinzas", "Tronco da fornalha", "Árvore das brasas" }
        };

        static Renderer Shape(Palette p, Transform parent, string name, Mesh mesh,
            Vector3 position, Vector3 scale, Color color, bool glow = false)
        {
            var part = p.Part(parent, name, PrimitiveType.Sphere, position, scale, color, glow);
            part.GetComponent<MeshFilter>().sharedMesh = mesh;
            return part.GetComponent<Renderer>();
        }

        static Renderer Branch(Palette p, Transform parent, string name, Vector3 start, Vector3 end,
            float width, Color color)
        {
            var r = Shape(p, parent, name, LowPolyMesh.Trunk, (start + end) * .5f,
                new Vector3(width, (end - start).magnitude, width), color);
            r.transform.localRotation = Quaternion.FromToRotation(Vector3.up, end - start);
            return r;
        }

        static Renderer Leaves(Palette p, Transform root, Vector3 position, Vector3 scale, Color color,
            bool conifer = false)
        {
            return Shape(p, root, conifer ? "Ramo de conífera" : "Copa facetada",
                conifer ? LowPolyMesh.Cone : LowPolyMesh.Crown, position, scale, color);
        }

        static Color LeafColor(Color foliage, int biome, int variant)
        {
            Color accent = biome == 1 ? new Color(.79f, .53f, .17f)
                : biome == 4 ? new Color(.49f, .24f, .66f)
                : biome == 3 ? new Color(.21f, .42f, .29f) : foliage;
            // Only four stable colors per biome, never a material per individual tree.
            return Color.Lerp(foliage, accent, variant == 1 ? .68f : variant == 3 ? .28f : .08f);
        }

        public static GameObject Create(Palette p, int biome, int kind, Color foliage, int variant)
        {
            biome = Mathf.Clamp(biome, 0, 5);
            kind = Mathf.Clamp(kind, 0, 3);
            variant &= 3;
            var g = new GameObject(kind == 0 ? TreeNames[biome, variant]
                : kind == 1 ? FloorNames[variant]
                : kind == 2 ? "Colônia de cogumelos e abóboras" : "Vegetação rasteira");
            var far = new List<Renderer>(5);
            if (kind == 0)
            {
                if (biome == 2) Conifer(p, g.transform, variant, far);
                else if (biome == 0 || biome == 5) BareTree(p, g.transform, biome, foliage, variant, far);
                else LeafTree(p, g.transform, biome, foliage, variant, far);
            }
            else if (kind == 1) ForestFloor(p, g.transform, biome, foliage, variant, far);
            else if (kind == 2) Fungi(p, g.transform, biome, variant, far);
            else Understory(p, g.transform, biome, foliage, variant, far);

            var renderers = g.GetComponentsInChildren<Renderer>();
            var lod = g.AddComponent<LODGroup>();
            lod.SetLODs(new[]
            {
                new LOD(kind == 0 ? .045f : .035f, renderers),
                new LOD(kind == 0 ? .006f : kind == 3 ? .014f : .008f, far.ToArray())
            });
            lod.RecalculateBounds();
            return g;
        }

        static void Conifer(Palette p, Transform root, int variant, List<Renderer> far)
        {
            float height = variant == 1 ? 7.8f : variant == 3 ? 3.7f : 5.9f;
            float lean = variant == 2 ? .7f : variant == 3 ? -.3f : .08f;
            far.Add(Branch(p, root, "Tronco de pinheiro", Vector3.zero,
                new Vector3(lean, height * .85f, 0), variant == 3 ? .26f : .48f, Bark));
            int tiers = variant == 1 ? 4 : variant == 3 ? 2 : 3;
            Color needle = new Color(.13f, .39f, .36f);
            for (int i = 0; i < tiers; i++)
            {
                float t = i / (float)tiers;
                float width = (variant == 1 ? 2.5f : variant == 2 ? 4.8f : variant == 3 ? 2.4f : 3.9f) * (1 - t * .65f);
                var pos = new Vector3(lean * (.4f + t * .5f), height * (.39f + t * .5f), 0);
                var crown = Leaves(p, root, pos, new Vector3(width, height * .46f, width), needle, true);
                if (i == 0 || i == tiers - 1) far.Add(crown);
                if ((i + variant) % 2 == 0)
                    Leaves(p, root, pos + Vector3.up * height * .055f,
                        new Vector3(width * .77f, height * .37f, width * .77f), Snow, true);
            }
            if (variant == 2)
                Branch(p, root, "Galho horizontal", new Vector3(.18f, height * .37f, 0), new Vector3(-2, height * .43f, .3f), .19f, Bark);
        }

        static void BareTree(Palette p, Transform root, int biome, Color foliage, int variant, List<Renderer> far)
        {
            bool burnt = biome == 5;
            Color bark = burnt ? new Color(.14f, .12f, .16f) : Bark;
            float h = variant == 1 ? 6.8f : variant == 2 ? 3.9f : variant == 3 ? 5.4f : 4.9f;
            float lean = variant == 2 ? 1.4f : variant == 3 ? -.9f : .18f;
            Vector3 fork = new Vector3(lean * .45f, h * .57f, 0);
            far.Add(Branch(p, root, "Tronco antigo", Vector3.zero, fork, variant == 2 ? .95f : .58f, bark));
            far.Add(Branch(p, root, "Espinha retorcida", fork, new Vector3(lean, h, .25f), .34f, bark));
            int branches = variant == 1 ? 3 : 2;
            for (int i = 0; i < branches; i++)
            {
                float a = i * (Mathf.PI * 2 / branches) + variant * .7f;
                float length = variant == 1 ? 1.1f : variant == 2 ? 2.5f : 1.85f;
                Vector3 start = fork + new Vector3(0, (i - .5f) * .45f, 0);
                Vector3 elbow = start + new Vector3(Mathf.Cos(a) * length, h * .13f, Mathf.Sin(a) * length);
                var branch = Branch(p, root, "Galho bifurcado", start, elbow, .24f, bark);
                if (i == 0) far.Add(branch);
                Vector3 tip = elbow + new Vector3(Mathf.Cos(a + .7f) * .55f, variant == 2 ? -.2f : .9f, Mathf.Sin(a + .7f) * .55f);
                Branch(p, root, "Ponta irregular", elbow, tip, .12f, bark);
            }
            if (variant != 1)
                for (int i = 0; i < 2; i++)
                    Branch(p, root, "Raiz exposta", new Vector3(0, .55f, 0),
                        new Vector3(i == 0 ? -1.3f : .8f, .06f, i == 0 ? .6f : -.8f), .27f, bark);

            if (burnt)
            {
                Shape(p, root, "Brasa no tronco", LowPolyMesh.Octahedron,
                    new Vector3(.18f, .8f, .3f), new Vector3(.18f, 1.1f, .13f), Ember, true);
                if (variant == 2 || variant == 3)
                    Shape(p, root, "Cristal de escória", LowPolyMesh.Octahedron,
                        new Vector3(-.6f, .4f, .3f), new Vector3(.32f, .8f, .32f), Amber, true);
            }
            else if (variant == 0)
            {
                Shape(p, root, "Lanterna suspensa", LowPolyMesh.Crown, new Vector3(1.2f, 2.6f, 0),
                    new Vector3(.32f, .52f, .32f), Amber, true);
                Branch(p, root, "Cordão da lanterna", new Vector3(1.2f, 2.82f, 0), new Vector3(.85f, 3.2f, 0), .05f, bark);
            }
            else if (variant == 1)
            {
                far.Add(Leaves(p, root, new Vector3(.5f, h * .81f, .2f), new Vector3(2, 2.4f, 1.8f), foliage));
                Leaves(p, root, new Vector3(-.6f, h * .61f, -.5f), new Vector3(1.5f, 1.7f, 1.8f), foliage);
            }
        }

        static void LeafTree(Palette p, Transform root, int biome, Color foliage, int variant, List<Renderer> far)
        {
            float height = variant == 1 ? 7 : variant == 2 ? 3.1f : variant == 3 ? 5.1f : 5.6f;
            if (biome == 4) height *= 1.18f;
            float lean = variant == 3 ? 1.35f : variant == 2 ? -.28f : .12f;
            Color bark = variant == 1 && biome == 1 ? new Color(.64f, .62f, .50f) : Bark;
            Color leaves = LeafColor(foliage, biome, variant);
            Vector3 fork = new Vector3(lean * .6f, height * .61f, 0);
            far.Add(Branch(p, root, "Tronco", Vector3.zero, fork, variant == 2 ? .3f : biome == 3 ? .79f : .62f, bark));
            Branch(p, root, "Tronco superior", fork, new Vector3(lean, height * .9f, 0), .30f, bark);
            int lobes = variant == 2 ? 2 : 3;
            for (int i = 0; i < lobes; i++)
            {
                float angle = i * Mathf.PI * 2 / lobes + .4f;
                float width = variant == 1 ? 1.5f : variant == 2 ? 1.65f : 2.0f;
                Vector3 tip = variant == 1
                    ? new Vector3(lean + (i % 2 == 0 ? -.35f : .35f), height * (.52f + i * .18f), .1f)
                    : new Vector3(lean + Mathf.Cos(angle) * width * .63f, height * (.72f + i * .08f), Mathf.Sin(angle) * width * .63f);
                if (variant != 1 && i < 2) Branch(p, root, "Galho de copa", fork, tip, .18f, bark);
                Vector3 scale = variant == 1 ? new Vector3(2.6f, 3.4f, 2.2f)
                    : variant == 2 ? new Vector3(2.0f, 1.75f, 2.1f)
                    : new Vector3(biome == 4 ? 4.6f : 3.7f, variant == 3 ? 2.8f : 2.5f, 3.7f);
                var crown = Leaves(p, root, tip, scale, i == 1 ? Color.Lerp(leaves, foliage, .35f) : leaves);
                if (i != 1 || lobes == 2) far.Add(crown);
            }
            if (biome == 3 || variant == 0)
                for (int i = 0; i < 2; i++)
                    Branch(p, root, "Raiz arqueada", new Vector3(.1f, .65f, 0),
                        new Vector3(i == 0 ? -1.2f : 1.1f, .06f, i == 0 ? .6f : -.5f), .27f, Bark);
            if (variant == 1 && biome == 1)
                for (int i = 0; i < 3; i++)
                    p.Part(root, "Marca da bétula", PrimitiveType.Cube, new Vector3(.025f, 1.2f + i * .8f, .24f),
                        new Vector3(.32f, .12f, .035f), Bark);
            else if (variant == 3)
                for (int i = 0; i < 3; i++)
                    Branch(p, root, "Folhagem pendente", new Vector3(lean - 1.2f + i * 1.1f, height * .81f, .8f),
                        new Vector3(lean - 1.25f + i * 1.1f, height * .43f, .8f), .19f, leaves);
            else if (biome == 4)
                Shape(p, root, "Cristal entre raízes", LowPolyMesh.Octahedron,
                    new Vector3(.6f, .5f, .15f), new Vector3(.32f, .95f, .32f), new Color(.15f, .67f, .69f), true);
        }

        static Color FloorColor(int biome) => biome == 2 ? new Color(.51f, .65f, .72f)
            : biome == 5 ? new Color(.23f, .20f, .25f) : Rock;

        static void ForestFloor(Palette p, Transform root, int biome, Color foliage, int variant, List<Renderer> far)
        {
            Color bark = biome == 5 ? new Color(.15f, .12f, .16f) : Bark;
            Color moss = biome == 2 ? Snow : biome == 5 ? new Color(.43f, .27f, .23f) : Color.Lerp(foliage, new Color(.30f, .46f, .19f), .5f);
            if (variant == 1)
            {
                far.Add(Branch(p, root, "Tronco tombado", new Vector3(-1.6f, .34f, -.2f), new Vector3(1.5f, .51f, .3f), .73f, bark));
                var cut = p.Part(root, "Madeira exposta", PrimitiveType.Cylinder, new Vector3(-1.62f, .34f, -.2f), new Vector3(.56f, .045f, .56f), CutWood);
                cut.transform.localRotation = Quaternion.Euler(0, 0, 90);
                Branch(p, root, "Galho partido", new Vector3(.3f, .52f, .12f), new Vector3(.65f, 1.04f, -.27f), .2f, bark);
                Leaves(p, root, new Vector3(-.25f, .68f, .02f), new Vector3(1.2f, .18f, .5f), moss);
                Mushroom(p, root, new Vector3(.8f, .63f, .1f), .36f, biome, false);
            }
            else if (variant == 2)
            {
                far.Add(Branch(p, root, "Toco oco", Vector3.zero, Vector3.up * .8f, .98f, bark));
                p.Part(root, "Cerne antigo", PrimitiveType.Cylinder, Vector3.up * .81f, new Vector3(.55f, .025f, .55f), CutWood);
                Branch(p, root, "Raiz do toco", Vector3.up * .25f, new Vector3(.95f, .05f, .45f), .25f, bark);
                Branch(p, root, "Raiz partida", Vector3.up * .2f, new Vector3(-.75f, .05f, -.4f), .20f, bark);
                Mushroom(p, root, new Vector3(.4f, .06f, .15f), .5f, biome, false);
            }
            else
            {
                var r = Shape(p, root, variant == 3 ? "Menir natural" : "Pedra grande", LowPolyMesh.Octahedron,
                    new Vector3(0, variant == 3 ? 1.1f : .53f, 0),
                    variant == 3 ? new Vector3(1.2f, 2.8f, .95f) : new Vector3(1.9f, 1.4f, 1.45f), FloorColor(biome));
                r.transform.localRotation = Quaternion.Euler(12, 23, variant == 3 ? 9 : -13);
                far.Add(r);
                Shape(p, root, "Fragmento", LowPolyMesh.Octahedron, new Vector3(.78f, .22f, .3f), new Vector3(.76f, .6f, .95f), FloorColor(biome));
                Shape(p, root, "Musgo ou neve", LowPolyMesh.Crown, new Vector3(-.16f, variant == 3 ? 1.75f : .84f, .04f),
                    new Vector3(.8f, .19f, .66f), moss);
                if (variant == 3)
                    Shape(p, root, "Cristal mineral", LowPolyMesh.Octahedron, new Vector3(-.48f, .5f, .4f),
                        new Vector3(.35f, 1.1f, .35f), biome == 5 ? Ember : new Color(.24f, .53f, .73f), biome == 4 || biome == 5);
            }
        }

        static Color CapColor(int biome) => biome == 5 ? new Color(.78f, .18f, .055f)
            : biome == 4 ? new Color(.17f, .43f, .78f) : biome == 3 ? new Color(.48f, .19f, .62f)
            : biome == 2 ? new Color(.13f, .42f, .62f) : new Color(.79f, .27f, .15f);

        static Renderer Mushroom(Palette p, Transform root, Vector3 pos, float size, int biome, bool glow)
        {
            Branch(p, root, "Caule do cogumelo", pos, pos + Vector3.up * .65f * size, .15f * size, new Color(.48f, .58f, .34f));
            if (glow)
                Shape(p, root, "Lâminas luminosas", LowPolyMesh.Octahedron, pos + Vector3.up * .49f * size,
                    new Vector3(.7f, .07f, .7f) * size, biome == 5 ? Ember : new Color(.14f, .60f, .58f), true);
            return Shape(p, root, "Chapéu pintado", LowPolyMesh.MushroomCap, pos + Vector3.up * .65f * size,
                new Vector3(1.1f, .7f, 1.1f) * size, CapColor(biome));
        }

        static Renderer Pumpkin(Palette p, Transform root, Vector3 pos, float size, bool face)
        {
            var body = Shape(p, root, "Abóbora", LowPolyMesh.Crown, pos + Vector3.up * .4f * size,
                new Vector3(.9f, .8f, .9f) * size, new Color(.79f, .26f, .045f));
            Branch(p, root, "Talo da abóbora", pos + Vector3.up * .75f * size,
                pos + new Vector3(.1f, .94f, 0) * size, .13f * size, new Color(.26f, .37f, .15f));
            if (face)
            {
                for (int side = -1; side <= 1; side += 2)
                    p.Part(root, "Olho luminoso", PrimitiveType.Cube, pos + new Vector3(side * .15f, .51f, .42f) * size,
                        new Vector3(.12f, .12f, .035f) * size, Amber, true);
                p.Part(root, "Sorriso luminoso", PrimitiveType.Cube, pos + new Vector3(0, .31f, .43f) * size,
                    new Vector3(.28f, .055f, .035f) * size, Amber, true);
            }
            return body;
        }

        static void Fungi(Palette p, Transform root, int biome, int variant, List<Renderer> far)
        {
            if (biome == 0 && (variant == 0 || variant == 2))
            {
                far.Add(Pumpkin(p, root, Vector3.zero, variant == 2 ? 1.25f : 1, true));
                if (variant == 2)
                {
                    Pumpkin(p, root, new Vector3(.75f, 0, .2f), .58f, false);
                    Pumpkin(p, root, new Vector3(-.52f, 0, -.18f), .43f, false);
                }
                return;
            }
            int count = variant == 0 ? 1 : variant == 1 ? 3 : variant == 2 ? 2 : 3;
            for (int i = 0; i < count; i++)
            {
                float size = variant == 3 ? (i == 0 ? 1.75f : .42f) : i == 0 ? .98f : .48f + i * .14f;
                Vector3 pos = i == 0 ? Vector3.zero : new Vector3(i == 1 ? .61f : -.55f, 0, i == 1 ? .25f : -.15f);
                var cap = Mushroom(p, root, pos, size, biome, (biome == 3 || biome == 4 || biome == 5) && i == 0);
                if (i == 0 || count == 3 && i == 2) far.Add(cap);
                if (i == 0 && variant != 1)
                    for (int spot = 0; spot < 2; spot++)
                        Shape(p, root, "Pinta dourada", LowPolyMesh.Octahedron,
                            pos + new Vector3(spot == 0 ? -.19f : .15f, .86f, .06f) * size,
                            Vector3.one * .12f * size, new Color(.86f, .62f, .20f));
            }
        }

        static void Understory(Palette p, Transform root, int biome, Color foliage, int variant, List<Renderer> far)
        {
            Color green = biome == 5 ? new Color(.48f, .23f, .16f)
                : biome == 2 ? new Color(.24f, .47f, .49f) : Color.Lerp(foliage, new Color(.21f, .55f, .24f), .45f);
            Color flower = biome == 0 || biome == 5 ? Amber : biome == 1 ? new Color(.93f, .64f, .15f)
                : biome == 2 ? new Color(.61f, .35f, .76f) : new Color(.84f, .29f, .53f);
            if (variant == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    float a = i * Mathf.PI * 2 / 5;
                    var leaf = Shape(p, root, "Fronda de samambaia", LowPolyMesh.Octahedron,
                        new Vector3(Mathf.Cos(a) * .28f, .4f, Mathf.Sin(a) * .28f), new Vector3(.24f, .86f, .16f), green);
                    leaf.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 40, 0, -Mathf.Cos(a) * 40);
                    if (i == 0 || i == 3) far.Add(leaf);
                }
            }
            else if (variant == 1)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = new Vector3((i - 1) * .32f, .5f + i * .1f, i % 2 == 0 ? .15f : -.15f);
                    Branch(p, root, "Haste de flor", new Vector3(pos.x, 0, pos.z), pos, .07f, green);
                    far.Add(Shape(p, root, "Flor silvestre", LowPolyMesh.Crown, pos,
                        new Vector3(.38f, .16f, .38f), flower));
                }
            }
            else if (variant == 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float h = .85f + (i % 3) * .18f;
                    Vector3 basePos = new Vector3((i % 2 - .5f) * .38f, 0, (i / 2 - .5f) * .35f);
                    var reed = Branch(p, root, "Caniço", basePos, basePos + new Vector3(.12f, h, .06f), .065f, green);
                    if (i % 2 == 0) far.Add(reed);
                    if (i < 3)
                        Shape(p, root, "Espiga", LowPolyMesh.Trunk, basePos + new Vector3(.12f, h, .06f),
                            new Vector3(.17f, .4f, .17f), biome == 2 ? Snow : CutWood);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    var shrub = Leaves(p, root, new Vector3((i - 1) * .42f, .45f + i % 2 * .25f, i % 2 * .12f),
                        new Vector3(.95f, .82f, .86f), green);
                    if (i != 1) far.Add(shrub);
                }
                for (int i = 0; i < 2; i++)
                    Shape(p, root, "Baga do vale", LowPolyMesh.Octahedron, new Vector3((i * 2 - 1) * .31f, .63f, .39f),
                        Vector3.one * .14f, flower);
            }
        }
    }
}
