using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    public sealed class PropPool
    {
        const int MaxPooledTotal = 256;
        const int MaxPooledPerType = 48;
        readonly Dictionary<int, Stack<GameObject>> available = new Dictionary<int, Stack<GameObject>>();
        readonly Transform storage;
        readonly Palette palette;
        readonly WorldSettings settings;
        public int Count { get; private set; }

        public PropPool(Palette p, WorldSettings s, Transform parent)
        {
            palette = p;
            settings = s;
            storage = new GameObject("Pool de decoração").transform;
            storage.SetParent(parent);
        }

        public GameObject Rent(int biome, int kind, Transform parent, int variant = 0)
        {
            if (biome < 0 || biome >= 6) throw new ArgumentOutOfRangeException(nameof(biome));
            if (kind < 0 || kind >= 4) throw new ArgumentOutOfRangeException(nameof(kind));
            if (variant < 0 || variant >= 4) throw new ArgumentOutOfRangeException(nameof(variant));
            int key = (biome * 4 + kind) * 4 + variant;
            if (!available.TryGetValue(key, out var list))
                available[key] = list = new Stack<GameObject>();
            GameObject go = null;
            while (list.Count > 0 && !go)
            {
                go = list.Pop();
                Count--;
            }
            if (!go) go = LowPolyFactory.Prop(palette, biome, kind, settings.biomes[biome].foliage, variant);
            go.name = key.ToString();
            go.transform.SetParent(parent, false);
            go.SetActive(true);
            return go;
        }

        public void Return(GameObject go)
        {
            if (!go) return;
            int key = int.Parse(go.name);
            if (!available.TryGetValue(key, out var list))
                available[key] = list = new Stack<GameObject>();
            go.SetActive(false);
            if (Count >= MaxPooledTotal || list.Count >= MaxPooledPerType)
            {
                UnityEngine.Object.Destroy(go);
                return;
            }
            go.transform.SetParent(storage, false);
            list.Push(go);
            Count++;
        }
    }
}
