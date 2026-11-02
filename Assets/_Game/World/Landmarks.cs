using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public sealed class Landmarks : MonoBehaviour
    {
        public readonly struct Location
        {
            public readonly string id, title;
            public readonly Vector2 position;
            public readonly float radius;
            public Location(string id, string title, float x, float z, float radius = 15)
            { this.id = id; this.title = title; position = new Vector2(x, z); this.radius = radius; }
        }

        public static readonly Vector2[] Sites = { new Vector2(28, 20), new Vector2(-65, 80), new Vector2(105, -60) };
        public static readonly string[] Names = { "Torre da Vigília", "Cemitério das Lanternas", "Cabana do Último Eco" };
        public static readonly Location[] Locations =
        {
            new Location("shrine-0", Names[0], 28, 20, 18),
            new Location("shrine-1", Names[1], -65, 80, 18),
            new Location("shrine-2", Names[2], 105, -60, 18),
            new Location("camp", "Acampamento das Brasas", -12, -9, 19),
            new Location("cache-0", "Tesouro entre as raízes", 52, -15, 10),
            new Location("cache-1", "Baú do caminho musgoso", -46, 44, 10),
            new Location("cache-2", "Esconderijo das flores", 10, 118, 10),
            new Location("journal-0", "Memórias do vigia", 20, 12, 7),
            new Location("journal-1", "Jardim das lembranças", -56, 68, 7),
            new Location("journal-2", "Último viajante", 88, -42, 7)
        };
        public readonly Dictionary<string, Interactable> registry = new Dictionary<string, Interactable>();
        Palette palette;
        Transform fire;
        Light fireLight;

        public static bool Near(float x, float z, float radius)
        {
            foreach (var p in Locations)
                if ((new Vector2(x, z) - p.position).sqrMagnitude < radius * radius) return true;
            return x * x + z * z < 64;
        }
        public static Vector3 Ground(float x, float z, int seed) => new Vector3(x, WorldMath.Height(x, z, seed) + .3f, z);

        public void Initialize(Palette p, SaveData state)
        {
            palette = p;
            for (int i = 0; i < Sites.Length; i++)
            {
                var g = LowPolyFactory.Shrine(p, i);
                g.transform.SetParent(transform);
                g.transform.position = Ground(Sites[i].x, Sites[i].y, state.seed);
                Register(g, "shrine-" + i, Names[i], InteractionKind.Shrine).shrine = true;
            }
            CreateCamp(state);
            for (int i = 0; i < 3; i++)
            {
                CreateCache(Locations[4 + i], state);
                CreateJournal(Locations[7 + i], i, state);
            }
            // Stable v1 pickup IDs remain consumed after migration.
            for (int i = 0; i < 18; i++)
            {
                float a = i * 2.39996f, radius = 8 + i * 5;
                CreatePickup(p, state, "crystal-" + i, "crystal", Ground(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, state.seed));
            }
            for (int i = 0; i < 12; i++)
                CreatePickup(p, state, "wood-" + i, i % 2 == 0 ? "wood" : "stone", Ground(-15 + i * 4, 10 + i * 2, state.seed));
            // Close clusters support the complete camp/lens/shrine loop without long walks.
            SpawnResources(state, "wood", 20, 16, 52, 7);
            SpawnResources(state, "stone", 18, 12, 62, 19);
            SpawnResources(state, "herb", 26, 10, 82, 31);
            RefreshState(state);
        }

        void SpawnResources(SaveData state, string item, int count, float inner, float outer, int salt)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.39996f + salt;
                float radius = Mathf.Lerp(inner, outer, WorldMath.Random(i, salt, state.seed));
                float x = Mathf.Cos(angle) * radius, z = Mathf.Sin(angle) * radius;
                CreatePickup(palette, state, "gather-" + item + "-" + i, item, Ground(x, z, state.seed), item == "herb" ? 1 : 2);
            }
        }

        Interactable Register(GameObject g, string id, string title, InteractionKind kind)
        {
            var t = g.AddComponent<Interactable>();
            t.id = id; t.title = title; t.kind = kind; registry[id] = t;
            return t;
        }

        GameObject Place(string title, float x, float z, SaveData state)
        {
            var g = new GameObject(title);
            g.transform.SetParent(transform);
            g.transform.position = Ground(x, z, state.seed);
            return g;
        }

        void CreateCamp(SaveData state)
        {
            var camp = Place("Acampamento das Brasas", -12, -9, state);
            Register(camp, "camp", camp.name, InteractionKind.Camp);
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * .25f;
                palette.Part(camp.transform, "Pedra da fogueira", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 1.2f, .18f, Mathf.Sin(a) * 1.2f), new Vector3(.55f, .35f, .45f), new Color(.34f, .39f, .42f));
            }
            for (int i = 0; i < 3; i++)
            {
                var log = palette.Part(camp.transform, "Lenha", PrimitiveType.Cylinder, new Vector3(0, .27f, (i - 1) * .23f), new Vector3(.24f, .85f, .24f), new Color(.31f, .19f, .11f));
                log.transform.localRotation = Quaternion.Euler(0, i * 55, 90);
            }
            fire = new GameObject("Chama restaurada").transform;
            fire.SetParent(camp.transform, false);
            var flame = palette.Part(fire, "Brasa", PrimitiveType.Sphere, Vector3.up * .8f, new Vector3(.8f, 1.4f, .8f), new Color(1, .34f, .055f), true);
            fireLight = flame.AddComponent<Light>();
            fireLight.color = new Color(1, .49f, .16f); fireLight.range = 14; fireLight.intensity = 3;
            palette.Part(camp.transform, "Banco", PrimitiveType.Cube, new Vector3(0, .4f, 2.5f), new Vector3(3, .45f, .65f), new Color(.37f, .25f, .16f), false, true);

            var bench = Place("Bancada de ressonância", -6, -14, state);
            Register(bench, "workbench", bench.name, InteractionKind.Workbench);
            palette.Part(bench.transform, "Tampo", PrimitiveType.Cube, Vector3.up, new Vector3(2.4f, .22f, 1.3f), new Color(.39f, .25f, .16f), false, true);
            for (int i = 0; i < 4; i++)
                palette.Part(bench.transform, "Pé", PrimitiveType.Cube, new Vector3(i % 2 == 0 ? -.9f : .9f, .5f, i < 2 ? -.43f : .43f), new Vector3(.18f, 1, .18f), new Color(.24f, .15f, .12f));
            palette.Part(bench.transform, "Lente", PrimitiveType.Sphere, new Vector3(0, 1.4f, 0), new Vector3(.55f, .7f, .55f), new Color(.16f, .65f, .88f), true);
        }

        void CreateCache(Location location, SaveData state)
        {
            var chest = Place(location.title, location.position.x, location.position.y, state);
            Register(chest, location.id, location.title, InteractionKind.Chest);
            palette.Part(chest.transform, "Baú", PrimitiveType.Cube, Vector3.up * .45f, new Vector3(1.5f, .9f, .9f), new Color(.43f, .25f, .12f), false, true);
            var lid = new GameObject("Tampa").transform; lid.SetParent(chest.transform, false); lid.localPosition = new Vector3(0, .9f, -.45f);
            palette.Part(lid, "Madeira", PrimitiveType.Cube, new Vector3(0, .06f, .45f), new Vector3(1.6f, .18f, 1), new Color(.58f, .33f, .15f));
            palette.Part(chest.transform, "Selo", PrimitiveType.Cube, new Vector3(0, .66f, .48f), new Vector3(.24f, .28f, .06f), new Color(.22f, .7f, .8f), true);
        }

        void CreateJournal(Location location, int index, SaveData state)
        {
            var journal = Place(location.title, location.position.x, location.position.y, state);
            var t = Register(journal, location.id, location.title, InteractionKind.Journal);
            string[] lore = {
                "O vigia guardou sua última chama no acampamento. Madeira e pedra reacendem a fogueira; ervas e cristais alimentam a lente. A torre responde à pedra.",
                "Entre as lanternas, sementes ainda florescem. Traga ervas ao cemitério. O pulso da lente também revela os selos escondidos entre as raízes.",
                "O último viajante deixou madeira na cabana. Três vozes restauradas devolvem o canto ao vale. Ler, descobrir e ajudar ampliam sua mochila."
            };
            t.loreText = lore[index];
            palette.Part(journal.transform, "Pedestal", PrimitiveType.Cube, Vector3.up * .5f, new Vector3(.65f, 1, .65f), new Color(.32f, .37f, .39f));
            var book = palette.Part(journal.transform, "Livro", PrimitiveType.Cube, new Vector3(0, 1.08f, 0), new Vector3(.8f, .12f, .55f), new Color(.87f, .72f, .39f));
            book.transform.localRotation = Quaternion.Euler(0, 15, -8);
        }

        public void RefreshState(SaveData state)
        {
            if (fire) fire.gameObject.SetActive(state.campBuilt);
            for (int i = 0; i < Sites.Length; i++)
                if (registry.TryGetValue("shrine-" + i, out var shrine))
                {
                    bool restored = state.restored.Contains(shrine.id);
                    var gem = shrine.transform.Find("Eco");
                    if (gem) gem.GetComponent<Renderer>().sharedMaterial = palette.Get(restored ? new Color(.95f, .55f, .12f) : new Color(.25f, .68f, .88f), true);
                    var light = shrine.GetComponentInChildren<Light>();
                    if (light) light.color = restored ? new Color(1, .59f, .18f) : new Color(.3f, .6f, 1);
                }
            for (int i = 0; i < 3; i++)
                if (registry.TryGetValue("cache-" + i, out var chest))
                {
                    var lid = chest.transform.Find("Tampa");
                    if (lid) lid.localRotation = state.openedCaches.Contains(chest.id) ? Quaternion.Euler(-65, 0, 0) : Quaternion.identity;
                }
        }

        public void CreatePickup(Palette p, SaveData state, string id, string item, Vector3 position, int amount = 1)
        {
            if (state.collected.Contains(id)) return;
            var g = new GameObject("Coleta " + id); g.transform.SetParent(transform); g.transform.position = position;
            if (item == "wood")
            {
                for (int i = 0; i < 2; i++)
                {
                    var log = p.Part(g.transform, "Lenha coletável", PrimitiveType.Cylinder,
                        new Vector3(0, .2f + i * .18f, i * .12f), new Vector3(.18f, .42f, .18f), new Color(.57f, .32f, .14f));
                    log.transform.localRotation = Quaternion.Euler(0, i * 55, 90);
                }
            }
            else if (item == "herb")
            {
                p.Part(g.transform, "Caule da erva", PrimitiveType.Cylinder, Vector3.up * .3f,
                    new Vector3(.08f, .3f, .08f), new Color(.18f, .39f, .12f));
                for (int side = -1; side <= 1; side += 2)
                {
                    var leaf = p.Part(g.transform, "Folha coletável", PrimitiveType.Sphere,
                        new Vector3(side * .13f, .44f, 0), new Vector3(.22f, .50f, .15f), new Color(.27f, .75f, .24f));
                    leaf.transform.localRotation = Quaternion.Euler(0, 0, side * -35);
                }
            }
            else if (item == "stone")
            {
                p.Part(g.transform, "Pedra coletável", PrimitiveType.Sphere, Vector3.up * .25f,
                    new Vector3(.65f, .48f, .52f), new Color(.4f, .5f, .61f));
                p.Part(g.transform, "Fragmento de pedra", PrimitiveType.Sphere, new Vector3(.25f, .14f, .1f),
                    Vector3.one * .32f, new Color(.45f, .55f, .66f));
            }
            else
            {
                var gem = p.Part(g.transform, "Cristal coletável", PrimitiveType.Sphere, Vector3.up * .55f,
                    new Vector3(.45f, .85f, .45f), new Color(.15f, .63f, .88f), true);
                gem.transform.localRotation = Quaternion.Euler(10, 30, 15);
            }
            var t = Register(g, id, ItemCatalog.Name(item), InteractionKind.Pickup); t.itemId = item; t.amount = amount;
        }
    }
}
