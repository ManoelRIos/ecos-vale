using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ecos
{
    public sealed class ChunkManager : MonoBehaviour
    {
        sealed class Loaded
        {
            public GameObject root;
            public Mesh mesh;
            public List<GameObject> props = new List<GameObject>();
        }

        sealed class Pending
        {
            public CancellationTokenSource cancel;
            public Task<ChunkData> task;
        }

        readonly Dictionary<Vector2Int, Loaded> loaded = new Dictionary<Vector2Int, Loaded>();
        readonly Dictionary<Vector2Int, Pending> pending = new Dictionary<Vector2Int, Pending>();
        readonly Dictionary<Vector2Int, ChunkData> cache = new Dictionary<Vector2Int, ChunkData>();
        readonly Queue<Vector2Int> cacheOrder = new Queue<Vector2Int>();
        readonly HashSet<Vector2Int> wanted = new HashSet<Vector2Int>();
        readonly List<Vector2Int> scratch = new List<Vector2Int>();
        WorldSettings settings;
        Palette palette;
        PropPool pool;
        Color[] colors;
        Transform target;
        readonly List<Transform> observers = new List<Transform>();
        int previousInterestHash;
        Vector2Int last = new Vector2Int(int.MinValue, 0);
        int lastRadius = -1;
        bool initialized;
        public int ActiveCount => loaded.Count;
        public int PendingCount => pending.Count;

        public int ObjectCount
        {
            get
            {
                int n = 0;
                foreach (var l in loaded.Values)
                    n += l.props.Count;
                return n;
            }
        }

        public float lastGenerationMs { get; private set; }

        public void Initialize(WorldSettings s, Palette p, Transform player)
        {
            settings = s;
            palette = p;
            target = player;
            observers.Add(player);
            pool = new PropPool(p, s, transform);
            colors = new Color[6];
            for (int i = 0; i < 6; i++)
                colors[i] = s.biomes[i].ground;
            initialized = true;
        }

        public void SetObservers(IEnumerable<Transform> players)
        {
            observers.Clear();
            foreach (var player in players) if (player && !observers.Contains(player)) observers.Add(player);
            if (observers.Count == 0 && target) observers.Add(target);
            lastRadius = -1;
        }

        Vector2Int Cell(Transform observer) => new Vector2Int(Mathf.FloorToInt(observer.position.x / settings.chunkSize), Mathf.FloorToInt(observer.position.z / settings.chunkSize));

        void Update()
        {
            if (!initialized)
                return;
            var center = new Vector2Int(Mathf.FloorToInt(target.position.x / settings.chunkSize), Mathf.FloorToInt(target.position.z / settings.chunkSize));
            int interestHash = 17;
            foreach (var observer in observers) if (observer) { var cell = Cell(observer); unchecked { interestHash = interestHash * 31 + cell.GetHashCode(); } }
            if (center != last || lastRadius != settings.viewRadius || previousInterestHash != interestHash)
            {
                last = center;
                previousInterestHash = interestHash;
                lastRadius = settings.viewRadius;
                wanted.Clear();
                int limit = settings.worldSize / 2 / settings.chunkSize;
                foreach (var observer in observers)
                {
                    if (!observer) continue;
                    var observerCell = Cell(observer);
                    for (int z = -lastRadius; z <= lastRadius; z++)
                        for (int x = -lastRadius; x <= lastRadius; x++)
                        {
                            var k = observerCell + new Vector2Int(x, z);
                            if (k.x >= -limit && k.x < limit && k.y >= -limit && k.y < limit) wanted.Add(k);
                        }
                }

                scratch.Clear();
                foreach (var kv in loaded)
                    if (!wanted.Contains(kv.Key))
                        scratch.Add(kv.Key);
                foreach (var k in scratch)
                    Unload(k);
                foreach (var kv in pending)
                    if (!wanted.Contains(kv.Key))
                        kv.Value.cancel.Cancel();
            }

            // At most two data workers and one main-thread mesh/prop upload per frame.
            scratch.Clear();
            foreach (var kv in pending)
                if (kv.Value.task.IsCompleted)
                    scratch.Add(kv.Key);
            bool uploaded = false;
            foreach (var k in scratch)
            {
                var p = pending[k];
                pending.Remove(k);
                if (p.task.Status == TaskStatus.RanToCompletion && wanted.Contains(k))
                {
                    Remember(k, p.task.Result);
                    Upload(k, p.task.Result);
                    uploaded = true;
                }
                else if (p.task.IsFaulted)
                    Debug.LogException(p.task.Exception);
                p.cancel.Dispose();
                break;
            }

            if (pending.Count >= 2)
                return;
            Vector2Int best = default;
            float dist = float.MaxValue;
            bool found = false;
            foreach (var k in wanted)
            {
                if (loaded.ContainsKey(k) || pending.ContainsKey(k))
                    continue;
                float d = (k - center).sqrMagnitude;
                if (d < dist)
                {
                    dist = d;
                    best = k;
                    found = true;
                }
            }

            if (!found)
                return;
            if (cache.TryGetValue(best, out var data))
            {
                if (uploaded) return;
                Upload(best, data);
                return;
            }

            var cts = new CancellationTokenSource();
            int cx = best.x, cz = best.y, size = settings.chunkSize, res = settings.resolution, seed = settings.seed;
            var token = cts.Token;
            pending[best] = new Pending
            {
                cancel = cts,
                task = Task.Run(() => TerrainMeshGenerator.Generate(cx, cz, size, res, seed, colors, token), token)
            };
        }

        void Remember(Vector2Int k, ChunkData d)
        {
            if (cache.ContainsKey(k))
                return;
            cache[k] = d;
            cacheOrder.Enqueue(k);
            while (cacheOrder.Count > 96)
                cache.Remove(cacheOrder.Dequeue());
        }

        void Upload(Vector2Int key, ChunkData data)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var root = new GameObject($"Chunk {key.x},{key.y}");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(key.x * settings.chunkSize, 0, key.y * settings.chunkSize);
            var mesh = new Mesh
            {
                name = root.name
            };
            mesh.vertices = data.vertices;
            mesh.colors = data.colors;
            mesh.triangles = data.triangles;
            mesh.normals = data.normals;
            mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = palette.terrain;
            root.AddComponent<MeshCollider>().sharedMesh = mesh;
            var chunk = new Loaded
            {
                root = root,
                mesh = mesh
            };
            loaded[key] = chunk;
            for (int i = 0; i < settings.propsPerChunk; i++)
            {
                float x = (WorldMath.Random(key.x * 101 + i, key.y, settings.seed) * .94f + .03f) * settings.chunkSize, z = (WorldMath.Random(key.x, key.y * 101 + i, settings.seed + 51) * .94f + .03f) * settings.chunkSize;
                float wx = root.transform.position.x + x, wz = root.transform.position.z + z;
                if (WorldMath.PathDistance(wx, wz) < 5 || Landmarks.Near(wx, wz, 15))
                    continue;
                float h = WorldMath.Height(wx, wz, settings.seed);
                if (Mathf.Abs(WorldMath.Height(wx + 2, wz, settings.seed) - h) > 3)
                    continue;
                int biome = WorldMath.Biome(wx, wz, settings.seed);
                if (WorldMath.Random(i, key.x, settings.seed + 99) > settings.biomes[biome].density)
                    continue;
                int kind = i % 4 == 0 ? 1 : i % 4 == 1 ? 2 : 0;
                var g = pool.Rent(biome, kind, root.transform);
                g.transform.localPosition = new Vector3(x, h, z);
                g.transform.localRotation = Quaternion.Euler(0, WorldMath.Random(i, key.y, settings.seed) * 360, 0);
                g.transform.localScale = Vector3.one * (.75f + WorldMath.Random(key.x, i, settings.seed));
                chunk.props.Add(g);
            }

            timer.Stop();
            lastGenerationMs = (float)timer.Elapsed.TotalMilliseconds;
        }

        void Unload(Vector2Int k)
        {
            var c = loaded[k];
            foreach (var g in c.props)
                pool.Return(g);
            Destroy(c.root);
            Destroy(c.mesh);
            loaded.Remove(k);
        }

        public bool HasGround(Vector3 p)
        {
            return loaded.ContainsKey(new Vector2Int(Mathf.FloorToInt(p.x / settings.chunkSize), Mathf.FloorToInt(p.z / settings.chunkSize)));
        }

        void OnDestroy()
        {
            foreach (var p in pending.Values)
            {
                p.cancel.Cancel();
                var source = p.cancel;
                _ = p.task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        _ = t.Exception;
                    source.Dispose();
                });
            }

            foreach (var c in loaded.Values)
                if (c.mesh)
                    Destroy(c.mesh);
        }
    }
}
