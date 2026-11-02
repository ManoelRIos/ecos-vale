using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public sealed class PropPool
    {
        readonly Dictionary<int, Stack<GameObject>> available = new Dictionary<int, Stack<GameObject>>();
        readonly Transform storage;
        readonly Palette palette;
        readonly WorldSettings settings;
        public PropPool(Palette p, WorldSettings s, Transform parent)
        {
            palette = p;
            settings = s;
            storage = new GameObject("Pool de decoração").transform;
            storage.SetParent(parent);
        }

        public GameObject Rent(int biome, int kind, Transform parent)
        {
            int key = biome * 3 + kind;
            if (!available.TryGetValue(key, out var list))
                available[key] = list = new Stack<GameObject>();
            var g = list.Count > 0 ? list.Pop() : LowPolyFactory.Prop(palette, biome, kind, settings.biomes[biome].foliage);
            g.name = key.ToString();
            g.transform.SetParent(parent, false);
            g.SetActive(true);
            return g;
        }

        public void Return(GameObject g)
        {
            int key = int.Parse(g.name);
            g.SetActive(false);
            g.transform.SetParent(storage, false);
            available[key].Push(g);
        }
    }
}
