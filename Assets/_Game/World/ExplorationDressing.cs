using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>Small authored scenes built from shared meshes/materials on the sampled terrain.</summary>
    public static class ExplorationDressing
    {
        static readonly Color Wood = new Color(.37f, .24f, .15f);
        static readonly Color DarkWood = new Color(.23f, .16f, .15f);
        static readonly Color Stone = new Color(.36f, .43f, .46f);
        static readonly Color Moss = new Color(.25f, .46f, .28f);
        static readonly Color Brass = new Color(.79f, .49f, .17f);
        static readonly Color Amber = new Color(.94f, .48f, .13f);
        static readonly Color Blue = new Color(.20f, .56f, .75f);

        public static void Create(Palette palette, Transform parent, Vector2 center, int seed, int kind)
        {
            var b = new Builder(palette, parent, center, seed, "Vestígios da expedição " + kind);
            switch (kind)
            {
                case 0: Wagon(b); break;
                case 1: RootArch(b); break;
                case 2: MoonGarden(b); break;
                case 3: Lookout(b); break;
                case 4: Observatory(b); break;
                default: BridgeShelter(b); break;
            }
            b.Finish();
        }

        static void Wagon(Builder b)
        {
            b.Part("Caixa da carroça", PrimitiveType.Cube, -3.2f, -1.3f, 1.05f, new Vector3(2.4f, .24f, 3.2f), Wood, true, solid: true);
            for (int side = -1; side <= 1; side += 2)
            {
                b.Part("Lateral de tábuas", PrimitiveType.Cube, -3.2f + side * 1.14f, -1.3f, 1.7f, new Vector3(.15f, 1.2f, 3.2f), Wood, true, solid: true);
                for (int wheel = 0; wheel < 2; wheel++)
                {
                    var part = b.Part("Roda de madeira", PrimitiveType.Cylinder, -3.2f + side * 1.37f,
                        -2.25f + wheel * 1.9f, .58f, new Vector3(1.1f, .12f, 1.1f), DarkWood, true, solid: true);
                    part.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    b.Part("Pino da roda", PrimitiveType.Cube, -3.2f + side * 1.52f,
                        -2.25f + wheel * 1.9f, .58f, new Vector3(.16f, .25f, .25f), Brass);
                }
            }
            b.Part("Caixote de provisões", PrimitiveType.Cube, -3.5f, -1.5f, 1.56f, new Vector3(.85f, .8f, .85f), DarkWood, solid: true);
            b.Mesh("Lona caída", LowPolyMesh.Cone, -3, -1.5f, 2.2f, new Vector3(1.7f, .8f, 2.5f), new Color(.57f, .31f, .23f));
            b.Beam("Haste da carroça", b.Point(-4.1f, .3f, .3f), b.Point(-4.1f, 3.7f, .14f), .16f, Wood);
            b.Beam("Haste partida", b.Point(-2.3f, .3f, .3f), b.Point(-1.5f, 2.2f, .15f), .16f, Wood);
            b.Lantern(2.4f, -2.3f, 1.5f);
            b.Mesh("Saco esquecido", LowPolyMesh.Crown, -4.8f, 1.3f, .45f, new Vector3(.75f, .85f, .75f), new Color(.57f, .46f, .25f));
        }

        static void RootArch(Builder b)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 foot = b.Point(side * 3.4f, -3.6f, 0);
                Vector3 elbow = b.Point(side * 2.9f, -3.6f, 4.4f);
                Vector3 top = b.Point(side * .8f, -3.6f, 6.1f);
                b.Beam("Raiz ancestral", foot, elbow, 1.2f, DarkWood, true, solid: true);
                b.Beam("Raiz arqueada", elbow, top, .85f, Wood, true, solid: true);
                b.Mesh("Coroa de folhas", LowPolyMesh.Crown, side * 2.8f, -3.7f, 5.2f,
                    new Vector3(3.1f, 1.8f, 2.4f), Moss, true);
                b.Beam("Raiz aparente", foot + Vector3.up * .5f, b.Point(side * 5.2f, -2.4f, .1f), .45f, DarkWood, solid: true);
            }
            b.Beam("Fecho do arco", b.Point(-.8f, -3.6f, 6.1f), b.Point(.8f, -3.6f, 6.1f), .75f, Wood, true, solid: true);
            for (int i = 0; i < 4; i++)
                b.Mesh("Cristal das raízes", LowPolyMesh.Octahedron, -4.3f + i * 2.8f, -1.2f,
                    .45f, new Vector3(.35f, .9f + i % 2 * .3f, .35f), Blue, false, true);
            b.Lantern(3.8f, 1.8f, 1.9f);
        }

        static void MoonGarden(Builder b)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = i * Mathf.PI * 2 / 9;
                float x = Mathf.Cos(angle) * 5.2f, z = Mathf.Sin(angle) * 5.2f;
                float height = 1.1f + i % 3 * .35f;
                b.Mesh("Caule do cogumelo lunar", LowPolyMesh.Trunk, x, z, height * .5f,
                    new Vector3(.22f, height, .22f), new Color(.44f, .55f, .43f));
                b.Mesh("Chapéu lunar", LowPolyMesh.MushroomCap, x, z, height,
                    new Vector3(1.5f, .85f, 1.5f), i % 2 == 0 ? Blue : new Color(.58f, .24f, .60f), true);
                b.Mesh("Esporos luminosos", LowPolyMesh.Octahedron, x, z, height - .15f,
                    new Vector3(.8f, .1f, .8f), new Color(.23f, .69f, .60f), false, true);
            }
            for (int i = 0; i < 6; i++)
            {
                float a = i * 2.4f;
                b.Mesh("Flor azul", LowPolyMesh.Crown, Mathf.Cos(a) * 2.5f, Mathf.Sin(a) * 2.5f, .3f,
                    new Vector3(.55f, .6f, .55f), i % 2 == 0 ? new Color(.43f, .68f, .35f) : new Color(.88f, .37f, .49f));
            }
            b.Mesh("Pedra da lua", LowPolyMesh.Crown, 2.4f, -1.4f, .75f, new Vector3(1.5f, 1.5f, 1.2f), Stone, true, solid: true);
        }

        static void Lookout(Builder b)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                b.Mesh("Coluna do mirante", LowPolyMesh.Trunk, side * 3.1f, -3.2f, 2.4f,
                    new Vector3(.9f, 4.8f, .9f), Stone, true, solid: true);
                b.Part("Capitel", PrimitiveType.Cube, side * 3.1f, -3.2f, 4.9f,
                    new Vector3(1.3f, .4f, 1.3f), new Color(.49f, .51f, .45f), true, solid: true);
                b.Beam("Guarda-corpo partido", b.Point(side * 3.1f, -3.2f, 1.25f),
                    b.Point(side * 4.2f, .1f, .9f), .2f, Wood, solid: true);
            }
            b.Part("Banco do peregrino", PrimitiveType.Cube, -3.2f, 1.2f, .7f, new Vector3(3, .25f, .8f), Wood, solid: true);
            b.Mesh("Pé do banco", LowPolyMesh.Trunk, -4.1f, 1.2f, .3f, new Vector3(.32f, .6f, .32f), DarkWood, solid: true);
            b.Mesh("Pé do banco", LowPolyMesh.Trunk, -2.3f, 1.2f, .3f, new Vector3(.32f, .6f, .32f), DarkWood, solid: true);
            b.Lantern(2.6f, 1.7f, 1.8f);
            b.Mesh("Marco do peregrino", LowPolyMesh.Crown, 4, 2.4f, 1.1f, new Vector3(1.2f, 2.2f, .9f), Stone, true, solid: true);
            b.Mesh("Marca dourada", LowPolyMesh.Octahedron, 4, 2.4f, 2.2f, new Vector3(.22f, .55f, .22f), Amber, false, true);
        }

        static void Observatory(Builder b)
        {
            for (int i = 0; i < 7; i++)
            {
                float angle = i * Mathf.PI * 2 / 7;
                float x = Mathf.Cos(angle) * 5.2f, z = Mathf.Sin(angle) * 5.2f;
                float height = i % 3 == 0 ? 4.8f : 1.2f + i % 3;
                b.Mesh("Coluna arruinada", LowPolyMesh.Trunk, x, z, height * .5f,
                    new Vector3(.8f, height, .8f), Stone, true, solid: true);
                b.Part("Pedra do círculo", PrimitiveType.Cube, x * .88f, z * .88f, .16f,
                    new Vector3(1.3f, .32f, 1.1f), new Color(.47f, .44f, .52f), solid: true);
            }
            b.Mesh("Pedestal celeste", LowPolyMesh.Trunk, 2.7f, -.5f, .8f,
                new Vector3(.9f, 1.6f, .9f), new Color(.44f, .42f, .52f), true, solid: true);
            var scope = b.Part("Tubo do telescópio", PrimitiveType.Cylinder, 2.7f, -.5f, 2,
                new Vector3(.40f, 1, .40f), Brass, true, solid: true);
            scope.transform.localRotation = Quaternion.Euler(65, -30, 0);
            b.Mesh("Lente do observatório", LowPolyMesh.Octahedron, 2.2f, .3f, 2.5f,
                new Vector3(.45f, .35f, .45f), Blue, false, true);
            b.Beam("Viga caída", b.Point(-4.3f, 1.4f, .3f), b.Point(-2.1f, 4.1f, .4f), .65f, Stone, true, solid: true);
            b.Lantern(-2.8f, -1.7f, 1.6f);
        }

        static void BridgeShelter(Builder b)
        {
            // The broken bridge is an archaeological remnant, not a crossing over invented water.
            for (int side = -1; side <= 1; side += 2)
            {
                b.Mesh("Pilar da ponte", LowPolyMesh.Trunk, side * 3.8f, -4.3f, 1.5f,
                    new Vector3(1.3f, 3, 1.3f), Stone, true, solid: true);
                b.Beam("Viga da ponte velha", b.Point(side * 3.8f, -4.3f, 1.7f),
                    b.Point(side * .7f, -4.3f, .35f), .5f, Wood, true, solid: true);
            }
            for (int i = 0; i < 5; i++)
            {
                var plank = b.Part("Tábua da ponte", PrimitiveType.Cube, -3.3f + i * .65f, -4.3f,
                    1.5f - i * .2f, new Vector3(.45f, .18f, 2.2f), DarkWood, solid: true);
                plank.transform.localRotation = Quaternion.Euler(i * 4, 0, -12);
            }
            b.Beam("Suporte do abrigo", b.Point(3.4f, 2.3f, 0), b.Point(3.4f, 2.3f, 2.8f), .2f, Wood, true, solid: true);
            b.Beam("Suporte do abrigo", b.Point(5.2f, 2.3f, 0), b.Point(5.2f, 2.3f, 2.8f), .2f, Wood, true, solid: true);
            b.Mesh("Lona do abrigo", LowPolyMesh.Cone, 4.3f, 2.3f, 2.7f,
                new Vector3(3.8f, 1.2f, 3.2f), new Color(.37f, .39f, .24f), true);
            b.Mesh("Cobertor enrolado", LowPolyMesh.Trunk, 4.2f, 1.1f, .3f,
                new Vector3(1.5f, .55f, .8f), new Color(.61f, .30f, .27f));
            b.Lantern(1.7f, 2.5f, 1.5f);
        }

        public static void CreateTrail(Palette palette, Transform parent, Vector2[] route, int seed)
        {
            for (int segment = 1; segment < route.Length; segment++)
            {
                Vector2 start = route[segment - 1], delta = route[segment] - start;
                int steps = Mathf.CeilToInt(delta.magnitude / 22);
                Vector2 side = new Vector2(-delta.y, delta.x).normalized;
                for (int step = 1; step < steps; step++)
                {
                    Vector2 point = start + delta * (step / (float)steps) + side * (step % 2 == 0 ? 2.3f : -2.3f);
                    var b = new Builder(palette, parent, point, seed, "Marco do caminho");
                    for (int stone = 0; stone < 3; stone++)
                        b.Mesh("Pedra empilhada", LowPolyMesh.Octahedron, 0, 0, .18f + stone * .25f,
                            new Vector3(.75f - stone * .15f, .37f, .65f - stone * .12f), Stone, stone == 0);
                    b.Mesh("Sinal de âmbar", LowPolyMesh.Octahedron, 0, 0, 1.05f,
                        new Vector3(.18f, .35f, .18f), Amber, false, true);
                    b.Finish();
                }
                Vector2 sign = start + delta.normalized * 12 + side * 2.3f;
                var post = new Builder(palette, parent, sign, seed, "Seta da expedição");
                float yaw = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
                post.root.localRotation = Quaternion.Euler(0, yaw, 0);
                post.Part("Poste", PrimitiveType.Cube, 0, 0, .95f, new Vector3(.19f, 1.9f, .19f), Wood, true);
                post.Part("Haste da seta", PrimitiveType.Cube, 0, .23f, 1.65f, new Vector3(.18f, .24f, .9f), Brass);
                var head = post.Mesh("Ponta da seta", LowPolyMesh.Cone, 0, .95f, 1.65f,
                    new Vector3(.52f, .65f, .45f), Amber, true);
                head.transform.localRotation = Quaternion.Euler(90, 0, 0);
                post.Finish();
            }
        }

        sealed class Builder
        {
            readonly Palette palette;
            readonly int seed;
            readonly Vector2 center;
            readonly List<Renderer> distant = new List<Renderer>();
            public readonly Transform root;

            public Builder(Palette p, Transform parent, Vector2 position, int worldSeed, string title)
            {
                palette = p; seed = worldSeed; center = position;
                root = new GameObject(title).transform;
                root.SetParent(parent, false);
                root.position = new Vector3(position.x, WorldMath.Height(position.x, position.y, seed), position.y);
            }

            public Vector3 Point(float x, float z, float height)
            {
                Vector3 planar = root.TransformDirection(new Vector3(x, 0, z));
                return new Vector3(x, WorldMath.Height(center.x + planar.x, center.y + planar.z, seed) - root.position.y + height, z);
            }

            public GameObject Part(string title, PrimitiveType type, float x, float z, float height,
                Vector3 scale, Color color, bool silhouette = false, bool glow = false, bool solid = false)
            {
                // A native cube already supplies an exact BoxCollider. Cylinders get a local
                // bounds box: their native CapsuleCollider is too thick for a thin wagon wheel.
                var part = palette.Part(root, title, type, Point(x, z, height), scale, color, glow,
                    collider: solid && type == PrimitiveType.Cube);
                if (solid && type != PrimitiveType.Cube)
                    AddBoundsCollider(part, part.GetComponent<MeshFilter>().sharedMesh);
                if (silhouette) distant.Add(part.GetComponent<Renderer>());
                return part;
            }

            public GameObject Mesh(string title, Mesh mesh, float x, float z, float height,
                Vector3 scale, Color color, bool silhouette = false, bool glow = false, bool solid = false)
            {
                return MeshAt(title, mesh, Point(x, z, height), scale, color, silhouette, glow, solid);
            }

            GameObject MeshAt(string title, Mesh mesh, Vector3 point, Vector3 scale, Color color,
                bool silhouette, bool glow, bool solid)
            {
                // These models use normalized facet meshes rather than sphere geometry, so create
                // only the renderer and an explicit bounds box when the structural piece is solid.
                var part = new GameObject(title);
                part.transform.SetParent(root, false);
                part.transform.localPosition = point;
                part.transform.localScale = scale;
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.AddComponent<MeshRenderer>().sharedMaterial = palette.Get(color, glow);
                if (solid) AddBoundsCollider(part, mesh);
                if (silhouette) distant.Add(part.GetComponent<Renderer>());
                return part;
            }

            static void AddBoundsCollider(GameObject part, Mesh mesh)
            {
                var collider = part.AddComponent<BoxCollider>();
                collider.center = mesh.bounds.center;
                collider.size = mesh.bounds.size;
            }

            public void Beam(string title, Vector3 from, Vector3 to, float width, Color color,
                bool silhouette = false, bool solid = false)
            {
                var part = MeshAt(title, LowPolyMesh.Trunk, (from + to) * .5f,
                    new Vector3(width, (to - from).magnitude, width), color, silhouette, false, solid);
                part.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
            }

            public void Lantern(float x, float z, float height)
            {
                Beam("Estaca da lanterna", Point(x, z, 0), Point(x, z, height + .2f), .15f, DarkWood);
                Part("Lanterna âmbar", PrimitiveType.Cube, x, z, height, new Vector3(.32f, .5f, .32f), Amber, false, true);
                Part("Tampa da lanterna", PrimitiveType.Cube, x, z, height + .31f, new Vector3(.48f, .12f, .48f), DarkWood);
            }

            public void Finish()
            {
                var renderers = root.GetComponentsInChildren<Renderer>();
                var lod = root.gameObject.AddComponent<LODGroup>();
                lod.SetLODs(new[] { new LOD(.045f, renderers), new LOD(.008f, distant.ToArray()) });
                lod.RecalculateBounds();
            }
        }
    }
}
