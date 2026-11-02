using System.Collections.Generic;
using UnityEngine;

namespace Ecos
{
    /// <summary>Bounded, gradual local population. Static habitats are seed-based; brains are local offline state.</summary>
    public sealed class WildlifeDirector : MonoBehaviour
    {
        readonly struct Habitat
        {
            public readonly int x, z;
            public readonly string id, species;
            public readonly Vector3 position;
            public Habitat(int x, int z, string species, Vector3 position)
            {
                this.x = x; this.z = z; this.species = species; this.position = position;
                id = "wildlife-" + x + "-" + z + "-0";
            }
        }

        readonly Dictionary<string, CreatureActor> active = new Dictionary<string, CreatureActor>();
        readonly Dictionary<string, Habitat> wanted = new Dictionary<string, Habitat>();
        readonly List<Habitat> candidates = new List<Habitat>();
        readonly List<string> retire = new List<string>();
        GameBootstrap game;
        Palette palette;
        WildlifeSettings settings;
        Dictionary<string, Interactable> registry;
        float refreshAt, workAt;
        bool initialized, lastNight;
        public int ActiveCount => active.Count;
        public WildlifeSettings Settings => settings;

        public void Initialize(GameBootstrap context, Palette sharedPalette)
        {
            game = context; palette = sharedPalette;
            var source = Resources.Load<WildlifeSettings>("WildlifeSettings");
            settings = source ? Instantiate(source) : WildlifeSettings.Defaults();
            settings.maxActive = Mathf.Clamp(settings.maxActive, 0, 24);
            settings.habitatCellSize = Mathf.Clamp(settings.habitatCellSize, 20, 48);
            settings.populationRadius = Mathf.Clamp(settings.populationRadius, 50, 160);
            settings.wanderRadius = Mathf.Clamp(settings.wanderRadius, 4, 16);
            settings.spawnInterval = Mathf.Clamp(settings.spawnInterval, .1f, 1);
            settings.refreshInterval = Mathf.Clamp(settings.refreshInterval, .3f, 3);
            registry = game.Sites.registry;
            lastNight = game.Clock.IsNight;
            initialized = true;
        }

        void Update()
        {
            if (!initialized || !game || !game.Playing || game.Blocked) return;
            bool night = game.Clock.IsNight;
            if (Time.time >= refreshAt || night != lastNight)
            {
                lastNight = night;
                refreshAt = Time.time + settings.refreshInterval;
                RefreshHabitats(night);
            }
            if (Time.time < workAt) return;
            workAt = Time.time + settings.spawnInterval;
            retire.Clear();
            foreach (var pair in active)
                if (!pair.Value || !wanted.TryGetValue(pair.Key, out var habitat) || pair.Value.SpeciesId != habitat.species)
                    retire.Add(pair.Key);
            // Destruction/creation is spread over frames, never an entire population burst.
            for (int i = 0; i < Mathf.Min(2, retire.Count); i++) Remove(retire[i]);
            if (active.Count >= settings.maxActive) return;
            foreach (var habitat in candidates)
            {
                if (!wanted.ContainsKey(habitat.id) || active.ContainsKey(habitat.id) || !game.Chunks.HasGround(habitat.position)) continue;
                Spawn(habitat);
                break;
            }
        }

        void RefreshHabitats(bool night)
        {
            candidates.Clear(); wanted.Clear();
            Vector3 observer = game.Player.transform.position;
            float cell = settings.habitatCellSize;
            int centerX = Mathf.FloorToInt(observer.x / cell), centerZ = Mathf.FloorToInt(observer.z / cell);
            int reach = Mathf.CeilToInt(settings.populationRadius / cell);
            float half = game.Settings.worldSize * .5f - 8;
            for (int z = centerZ - reach; z <= centerZ + reach; z++)
                for (int x = centerX - reach; x <= centerX + reach; x++)
                {
                    float px = (x + .23f + WorldMath.Random(x, z, game.State.seed + 31) * .54f) * cell;
                    float pz = (z + .23f + WorldMath.Random(x, z, game.State.seed + 97) * .54f) * cell;
                    float dx = px - observer.x, dz = pz - observer.z;
                    if (Mathf.Abs(px) > half || Mathf.Abs(pz) > half || dx * dx + dz * dz > settings.populationRadius * settings.populationRadius
                        || Landmarks.Near(px, pz, 5)) continue;
                    float y = WorldMath.Height(px, pz, game.State.seed);
                    float slope = Mathf.Abs(WorldMath.Height(px + 2, pz, game.State.seed) - y)
                                + Mathf.Abs(WorldMath.Height(px, pz + 2, game.State.seed) - y);
                    if (slope > 2.5f) continue;
                    string species = CreatureCatalog.ChooseSpecies(x, z, game.State.seed, WorldMath.Biome(px, pz, game.State.seed), night);
                    candidates.Add(new Habitat(x, z, species, new Vector3(px, y, pz)));
                }
            candidates.Sort((a, b) => (a.position - observer).sqrMagnitude.CompareTo((b.position - observer).sqrMagnitude));
            int count = Mathf.Min(settings.maxActive, candidates.Count);
            for (int i = 0; i < count; i++) wanted[candidates[i].id] = candidates[i];
        }

        void Spawn(Habitat habitat)
        {
            var root = new GameObject(CreatureCatalog.Name(habitat.species));
            root.transform.SetParent(transform, false);
            var actor = root.AddComponent<CreatureActor>();
            actor.Initialize(game, settings, palette, habitat.species, habitat.position,
                unchecked((int)WorldMath.Hash(habitat.x, habitat.z, game.State.seed)));
            var interaction = root.AddComponent<Interactable>();
            interaction.id = habitat.id;
            interaction.title = CreatureCatalog.Name(habitat.species);
            interaction.itemId = habitat.species;
            interaction.kind = InteractionKind.Observe;
            interaction.loreText = CreatureCatalog.Description(habitat.species);
            registry[habitat.id] = interaction;
            active.Add(habitat.id, actor);
        }

        void Remove(string id)
        {
            if (registry != null) registry.Remove(id);
            if (active.TryGetValue(id, out var actor) && actor) Destroy(actor.gameObject);
            active.Remove(id);
        }

        void OnDestroy()
        {
            if (registry != null)
                foreach (var id in active.Keys) registry.Remove(id);
            if (settings) Destroy(settings);
        }
    }
}
