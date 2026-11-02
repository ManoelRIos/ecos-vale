using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public sealed class Landmarks : MonoBehaviour
    {
        public static readonly Vector2[] Sites =
        {
            new Vector2(28, 20),
            new Vector2(-65, 80),
            new Vector2(105, -60)
        };
        public static readonly string[] Names =
        {
            "Torre da Vigília",
            "Cemitério das Lanternas",
            "Cabana do Último Eco"
        };
        public readonly Dictionary<string, Interactable> registry = new Dictionary<string, Interactable>();
        public static bool Near(float x, float z, float radius)
        {
            foreach (var p in Sites)
                if ((new Vector2(x, z) - p).sqrMagnitude < radius * radius)
                    return true;
            return x * x + z * z < 64;
        }

        public static Vector3 Ground(float x, float z, int seed) => new Vector3(x, WorldMath.Height(x, z, seed) + .3f, z);
        public void Initialize(Palette p, SaveData state)
        {
            for (int i = 0; i < Sites.Length; i++)
            {
                var site = Sites[i];
                var g = LowPolyFactory.Shrine(p, i);
                g.transform.SetParent(transform);
                g.transform.position = Ground(site.x, site.y, state.seed);
                var t = g.AddComponent<Interactable>();
                t.id = "shrine-" + i;
                t.title = Names[i];
                t.shrine = true;
                registry[t.id] = t;
            }

            // An approachable trail of crystals, including enough to finish the quest.
            for (int i = 0; i < 18; i++)
            {
                float a = i * 2.39996f;
                float radius = 8 + i * 5;
                var pos = Ground(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, state.seed);
                CreatePickup(p, state, "crystal-" + i, "crystal", pos);
            }

            for (int i = 0; i < 12; i++)
                CreatePickup(p, state, "wood-" + i, i % 2 == 0 ? "wood" : "stone", Ground(-15 + i * 4, 10 + i * 2, state.seed));
        }

        public void CreatePickup(Palette p, SaveData state, string id, string item, Vector3 pos, int amount = 1)
        {
            if (state.collected.Contains(id))
                return;
            var g = new GameObject("Coleta " + id);
            g.transform.SetParent(transform);
            g.transform.position = pos;
            var model = p.Part(g.transform, "Recurso", PrimitiveType.Cube, Vector3.up * .7f, Vector3.one * .5f, item == "crystal" ? new Color(.3f, .82f, 1) : new Color(.56f, .4f, .25f), item == "crystal");
            model.transform.localRotation = Quaternion.Euler(35, 30, 20);
            var t = g.AddComponent<Interactable>();
            t.id = id;
            t.itemId = item;
            t.amount = amount;
            t.title = item == "crystal" ? "Cristal de eco" : item == "wood" ? "Madeira" : "Pedra";
            registry[id] = t;
        }
    }
}
