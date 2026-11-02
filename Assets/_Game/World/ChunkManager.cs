using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ecos
{
    public sealed class ChunkManager : MonoBehaviour
    {
        sealed class Generated
        {
            public ChunkData terrain;
            public PropPlacement[] props;
            public float workerMs;
        }

        sealed class Loaded
        {
            public GameObject root;
            public Mesh mesh;
            public PropPlacement[] candidates;
            public int nextCandidate;
            public bool dense;
            public Transform decorationRoot;
            public List<GameObject> props = new List<GameObject>();
        }

        sealed class Retirement
        {
            public GameObject root;
            public List<GameObject> props;
            public int index;
        }

        sealed class Pending
        {
            public CancellationTokenSource cancel;
            public Task<Generated> task;
        }

        readonly Dictionary<Vector2Int, Loaded> loaded = new Dictionary<Vector2Int, Loaded>();
        readonly Dictionary<Vector2Int, Pending> pending = new Dictionary<Vector2Int, Pending>();
        readonly Dictionary<Vector2Int, Generated> cache = new Dictionary<Vector2Int, Generated>();
        readonly Queue<Vector2Int> cacheOrder = new Queue<Vector2Int>();
        readonly Queue<Retirement> retiring = new Queue<Retirement>();
        readonly HashSet<Vector2Int> wanted = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> denseWanted = new HashSet<Vector2Int>();
        readonly List<Vector2Int> scratch = new List<Vector2Int>();
        readonly List<Transform> observers = new List<Transform>();
        readonly System.Diagnostics.Stopwatch decorationTimer = new System.Diagnostics.Stopwatch();
        WorldSettings settings;
        Palette palette;
        PropPool pool;
        Color[] colors;
        float[] densities;
        Transform target;
        Vector2Int last = new Vector2Int(int.MinValue, 0);
        int previousInterestHash, lastRadius = -1, objectCount, retiredProps;
        bool initialized;

        public int ActiveCount => loaded.Count;
        public int PendingCount => pending.Count;
        public int ObjectCount => objectCount;
        public int DecoratedChunkCount
        {
            get { int count = 0; foreach (var chunk in loaded.Values) if (chunk.dense) count++; return count; }
        }
        public int PooledCount => pool == null ? 0 : pool.Count;
        public int RetiringPropCount => retiredProps;
        public int PendingPropCount
        {
            get
            {
                int count = 0;
                foreach (var chunk in loaded.Values)
                    if (chunk.dense) count += chunk.candidates.Length - chunk.nextCandidate;
                return count;
            }
        }
        // Existing HUD reads this as upload time, not worker generation time.
        public float lastGenerationMs { get; private set; }
        public float LastWorkerMs { get; private set; }
        public float LastPropFrameMs { get; private set; }

        public void Initialize(WorldSettings configuration, Palette sharedPalette, Transform player)
        {
            settings = configuration;
            palette = sharedPalette;
            target = player;
            observers.Add(player);
            pool = new PropPool(palette, settings, transform);
            colors = new Color[6];
            densities = new float[6];
            for (int i = 0; i < 6; i++)
            {
                colors[i] = settings.biomes[i].ground;
                densities[i] = settings.biomes[i].density;
            }
            initialized = true;
        }

        public void SetObservers(IEnumerable<Transform> players)
        {
            observers.Clear();
            foreach (var player in players)
                if (player && !observers.Contains(player)) observers.Add(player);
            if (observers.Count == 0 && target) observers.Add(target);
            lastRadius = -1;
        }

        Vector2Int Cell(Transform observer) => new Vector2Int(
            Mathf.FloorToInt(observer.position.x / settings.chunkSize),
            Mathf.FloorToInt(observer.position.z / settings.chunkSize));

        void Update()
        {
            if (!initialized || !target) return;
            settings.viewRadius = Mathf.Clamp(settings.viewRadius, 1, 2);
            var center = Cell(target);
            UpdateInterests(center);
            bool uploaded = CompleteOneWorker();
            ScheduleNearestChunk(center, uploaded);
            RetireSomeProps();
            DecorateSomeProps(center);
        }

        void UpdateInterests(Vector2Int center)
        {
            int interestHash = 17;
            foreach (var observer in observers)
                if (observer) unchecked { interestHash = interestHash * 31 + Cell(observer).GetHashCode(); }
            if (center == last && lastRadius == settings.viewRadius && previousInterestHash == interestHash) return;
            last = center;
            previousInterestHash = interestHash;
            lastRadius = settings.viewRadius;
            wanted.Clear();
            denseWanted.Clear();
            int limit = Mathf.CeilToInt(settings.worldSize / (2f * settings.chunkSize));
            foreach (var observer in observers)
            {
                if (!observer) continue;
                var cell = Cell(observer);
                for (int z = -lastRadius - 1; z <= lastRadius + 1; z++)
                    for (int x = -lastRadius - 1; x <= lastRadius + 1; x++)
                    {
                        var key = cell + new Vector2Int(x, z);
                        if (key.x >= -limit && key.x < limit && key.y >= -limit && key.y < limit)
                        {
                            wanted.Add(key);
                            if (Mathf.Abs(x) <= lastRadius && Mathf.Abs(z) <= lastRadius) denseWanted.Add(key);
                        }
                    }
            }
            scratch.Clear();
            foreach (var pair in loaded)
                if (!wanted.Contains(pair.Key)) scratch.Add(pair.Key);
            foreach (var key in scratch) Unload(key);
            foreach (var pair in loaded)
                SetDecorationEnabled(pair.Value, denseWanted.Contains(pair.Key));
            foreach (var pair in pending)
                if (!wanted.Contains(pair.Key)) pair.Value.cancel.Cancel();
        }

        bool CompleteOneWorker()
        {
            scratch.Clear();
            foreach (var pair in pending)
                if (pair.Value.task.IsCompleted) scratch.Add(pair.Key);
            if (scratch.Count == 0) return false;
            Vector2Int key = scratch[0];
            var work = pending[key];
            pending.Remove(key);
            bool uploaded = false;
            if (work.task.Status == TaskStatus.RanToCompletion && wanted.Contains(key))
            {
                var data = work.task.Result;
                LastWorkerMs = data.workerMs;
                Remember(key, data);
                Upload(key, data);
                uploaded = true;
            }
            else if (work.task.IsFaulted) Debug.LogException(work.task.Exception);
            work.cancel.Dispose();
            return uploaded;
        }

        void ScheduleNearestChunk(Vector2Int center, bool uploaded)
        {
            if (pending.Count >= 2) return;
            Vector2Int best = default;
            float distance = float.MaxValue;
            bool found = false;
            foreach (var key in wanted)
            {
                if (loaded.ContainsKey(key) || pending.ContainsKey(key)) continue;
                float d = (key - center).sqrMagnitude;
                if (d < distance) { distance = d; best = key; found = true; }
            }
            if (!found) return;
            if (cache.TryGetValue(best, out var cached))
            {
                if (!uploaded) Upload(best, cached);
                return;
            }
            var source = new CancellationTokenSource();
            int x = best.x, z = best.y, size = settings.chunkSize, resolution = settings.resolution;
            int seed = settings.seed, budget = Mathf.Clamp(settings.propsPerChunk, 0, 768);
            var token = source.Token;
            pending[best] = new Pending
            {
                cancel = source,
                task = Task.Run(() =>
                {
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    var terrain = TerrainMeshGenerator.Generate(x, z, size, resolution, seed, colors, token);
                    var placements = PropDistribution.Generate(x, z, size, budget, seed, densities, token);
                    timer.Stop();
                    return new Generated { terrain = terrain, props = placements, workerMs = (float)timer.Elapsed.TotalMilliseconds };
                }, token)
            };
        }

        void Remember(Vector2Int key, Generated data)
        {
            if (cache.ContainsKey(key)) return;
            cache[key] = data;
            cacheOrder.Enqueue(key);
            while (cacheOrder.Count > 32) cache.Remove(cacheOrder.Dequeue());
        }

        void Upload(Vector2Int key, Generated data)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var root = new GameObject($"Chunk {key.x},{key.y}");
            root.transform.SetParent(transform);
            root.transform.position = new Vector3(key.x * settings.chunkSize, 0, key.y * settings.chunkSize);
            var mesh = new Mesh { name = root.name };
            mesh.vertices = data.terrain.vertices;
            mesh.colors = data.terrain.colors;
            mesh.triangles = data.terrain.triangles;
            mesh.normals = data.terrain.normals;
            mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = palette.terrain;
            root.AddComponent<MeshCollider>().sharedMesh = mesh;
            var chunk = new Loaded { root = root, mesh = mesh, candidates = (PropPlacement[])data.props.Clone() };
            loaded[key] = chunk;
            SetDecorationEnabled(chunk, denseWanted.Contains(key));
            timer.Stop();
            lastGenerationMs = (float)timer.Elapsed.TotalMilliseconds;
        }

        void SetDecorationEnabled(Loaded chunk, bool enabled)
        {
            if (chunk.dense == enabled) return;
            chunk.dense = enabled;
            chunk.nextCandidate = 0;
            if (enabled)
            {
                chunk.decorationRoot = new GameObject("Vegetação").transform;
                chunk.decorationRoot.SetParent(chunk.root.transform, false);
                Vector3 local = target.position - chunk.root.transform.position;
                Array.Sort(chunk.candidates, (a, b) =>
                {
                    float da = (a.x - local.x) * (a.x - local.x) + (a.z - local.z) * (a.z - local.z);
                    float db = (b.x - local.x) * (b.x - local.x) + (b.z - local.z) * (b.z - local.z);
                    return da.CompareTo(db);
                });
            }
            else if (chunk.decorationRoot)
            {
                chunk.decorationRoot.gameObject.SetActive(false);
                objectCount -= chunk.props.Count;
                retiredProps += chunk.props.Count;
                retiring.Enqueue(new Retirement { root = chunk.decorationRoot.gameObject, props = chunk.props });
                chunk.props = new List<GameObject>();
                chunk.decorationRoot = null;
            }
        }

        void DecorateSomeProps(Vector2Int center)
        {
            decorationTimer.Restart();
            int budget = Mathf.Clamp(settings.propsPerFrame, 1, 24);
            for (int i = 0; i < budget; i++)
            {
                Loaded best = null;
                float priority = float.MaxValue;
                foreach (var pair in loaded)
                {
                    var chunk = pair.Value;
                    if (!chunk.dense || chunk.nextCandidate >= chunk.candidates.Length) continue;
                    // Progress penalty spreads decoration among neighboring chunks instead of
                    // completely filling one forest while its neighbors remain bare.
                    float p = (pair.Key - center).sqrMagnitude + chunk.nextCandidate * .02f;
                    if (p < priority) { priority = p; best = chunk; }
                }
                if (best == null) break;
                var placement = best.candidates[best.nextCandidate++];
                float wx = best.root.transform.position.x + placement.x;
                float wz = best.root.transform.position.z + placement.z;
                float clearance = placement.kind == 0 ? 12 : placement.kind == 1 ? 8 : 5;
                if (!Landmarks.Near(wx, wz, clearance))
                {
                    var prop = pool.Rent(placement.biome, placement.kind, best.decorationRoot, placement.variant);
                    prop.transform.localPosition = new Vector3(placement.x, placement.y, placement.z);
                    prop.transform.localRotation = Quaternion.Euler(0, placement.yaw, 0);
                    prop.transform.localScale = Vector3.one * placement.scale;
                    best.props.Add(prop);
                    objectCount++;
                }
                // Soft wall-clock bound: a single prefab creation can itself exceed this budget.
                if (decorationTimer.Elapsed.TotalMilliseconds >= Mathf.Max(.5f, settings.propBudgetMs)) break;
            }
            decorationTimer.Stop();
            LastPropFrameMs = (float)decorationTimer.Elapsed.TotalMilliseconds;
        }

        void Unload(Vector2Int key)
        {
            var chunk = loaded[key];
            loaded.Remove(key);
            objectCount -= chunk.props.Count;
            chunk.root.SetActive(false);
            if (chunk.mesh) Destroy(chunk.mesh);
            retiredProps += chunk.props.Count;
            retiring.Enqueue(new Retirement { root = chunk.root, props = chunk.props });
        }

        void RetireSomeProps()
        {
            int budget = Mathf.Clamp(settings.propsPerFrame * 2, 2, 48);
            while (retiring.Count > 0 && budget > 0)
            {
                var chunk = retiring.Peek();
                while (chunk.index < chunk.props.Count && budget > 0)
                {
                    pool.Return(chunk.props[chunk.index++]);
                    retiredProps--;
                    budget--;
                }
                if (chunk.index < chunk.props.Count) break;
                retiring.Dequeue();
                Destroy(chunk.root);
            }
        }

        public bool HasGround(Vector3 position) => loaded.ContainsKey(new Vector2Int(
            Mathf.FloorToInt(position.x / settings.chunkSize), Mathf.FloorToInt(position.z / settings.chunkSize)));

        void OnDestroy()
        {
            foreach (var work in pending.Values)
            {
                work.cancel.Cancel();
                var source = work.cancel;
                _ = work.task.ContinueWith(task =>
                {
                    if (task.IsFaulted) _ = task.Exception;
                    source.Dispose();
                });
            }
            foreach (var chunk in loaded.Values)
                if (chunk.mesh) Destroy(chunk.mesh);
        }
    }
}
