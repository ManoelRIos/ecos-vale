using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        public WorldSettings configuration;
        public SaveData State { get; private set; }
        public WorldSettings Settings { get; private set; }
        public PlayerMotor Player { get; private set; }
        public ThirdPersonCamera Orbit { get; private set; }
        public ChunkManager Chunks { get; private set; }
        public WorldClock Clock { get; private set; }
        public Landmarks Sites { get; private set; }
        public bool Playing { get; private set; }
        public bool Menu { get; private set; } = true;

        public int NewSeed = 71319;
        public bool InventoryOpen, MapOpen;
        public string Notice = "Bem-vindo ao vale. Inicie uma jornada.";
        public Interactable Focus { get; private set; }

        Camera menuCamera;
        Palette palette;
        GameObject session;
        IWorldAuthority authority;
        Light torch;
        float noticeUntil, autosave;
        bool applicationQuitting;
        public bool Blocked => Menu || InventoryOpen || MapOpen;

        void Awake()
        {
            Application.targetFrameRate = 60;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.skybox = null;
            Settings = configuration ? Instantiate(configuration) : Resources.Load<WorldSettings>("WorldSettings");
            if (!Settings)
                Settings = WorldSettings.Defaults();
            else if (!configuration)
                Settings = Instantiate(Settings);
            var menuObject = new GameObject("Câmera do menu");
            menuObject.transform.SetParent(transform);
            menuCamera = menuObject.AddComponent<Camera>();
            menuCamera.clearFlags = CameraClearFlags.SolidColor;
            menuCamera.backgroundColor = new Color(.035f,.025f,.065f);
            NewSeed = Settings.seed;
            gameObject.AddComponent<AmbientAudio>();
            gameObject.AddComponent<GameUI>();
        }

        public void Begin(bool resume)
        {
            SaveData state;
            if (resume)
            {
                if (!SaveStore.TryLoad(out state, out string error))
                {
                    Notify("Não foi possível abrir o save: " + error);
                    return;
                }
            }
            else
                state = new SaveData
                {
                    seed = NewSeed
                };
            if (session)
                Destroy(session);
            if (palette != null)
                palette.Dispose();
            menuCamera.gameObject.SetActive(false);
            State = state;
            Settings.seed = state.seed;
            session = new GameObject("Sessão offline");
            palette = new Palette();
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(session.transform);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 900;
            cameraObject.AddComponent<AudioListener>();
            Orbit = cameraObject.AddComponent<ThirdPersonCamera>();
            var actor = LowPolyFactory.Player(palette);
            actor.transform.SetParent(session.transform);
            actor.layer = 2;
            foreach (var t in actor.GetComponentsInChildren<Transform>())
                t.gameObject.layer = 2;
            actor.AddComponent<CharacterController>();
            Player = actor.AddComponent<PlayerMotor>();
            Player.view = camera;
            Player.settings = Settings;
            float half = Settings.worldSize * .5f - 3;
            Vector3 pos = State.position;
            pos.x = Mathf.Clamp(pos.x, -half, half);
            pos.z = Mathf.Clamp(pos.z, -half, half);
            pos.y = Mathf.Max(pos.y, WorldMath.Height(pos.x, pos.z, State.seed) + 1.5f);
            Player.Teleport(pos);
            Orbit.target = actor.transform;
            cameraObject.transform.position = pos + new Vector3(0, 5, -6);
            var terrain = new GameObject("World streaming");
            terrain.transform.SetParent(session.transform);
            Chunks = terrain.AddComponent<ChunkManager>();
            Chunks.Initialize(Settings, palette, actor.transform);
            Player.world = Chunks;
            var environment = new GameObject("Ambiente");
            environment.transform.SetParent(session.transform);
            Clock = environment.AddComponent<WorldClock>();
            Clock.time = State.time;
            Clock.daySeconds = Settings.daySeconds;
            Clock.sun = CreateLight("Sol", environment.transform);
            Clock.moon = CreateLight("Lua", environment.transform);
            Clock.sun.shadows = LightShadows.Soft;
            var weather = environment.AddComponent<WeatherManager>();
            weather.player = actor.transform;
            weather.clock = Clock;
            weather.settings = Settings;
            var night = environment.AddComponent<NightAtmosphere>();
            night.clock = Clock;
            night.player = actor.transform;
            night.palette = palette;
            var lamp = new GameObject("Lanterna");
            lamp.transform.SetParent(actor.transform, false);
            lamp.transform.localPosition = new Vector3(.4f, 1.2f, .3f);
            torch = lamp.AddComponent<Light>();
            torch.type = LightType.Point;
            torch.color = new Color(1, .53f, .17f);
            torch.intensity = 4;
            torch.range = 15;
            torch.shadows = LightShadows.None;
            var sites = new GameObject("Pontos de interesse");
            sites.transform.SetParent(session.transform);
            Sites = sites.AddComponent<Landmarks>();
            Sites.Initialize(palette, State);
            authority = new OfflineWorldAuthority(State, Sites.registry);
            Playing = true;
            Menu = false;
            InventoryOpen = MapOpen = false;
            autosave = Time.unscaledTime + 30;
            Notify("Vozes do Vale: encontre cristais e restaure os três santuários.");
        }

        Light CreateLight(string name, Transform parent)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent);
            var l = g.AddComponent<Light>();
            l.type = LightType.Directional;
            return l;
        }

        void Update()
        {
            var k = Keyboard.current;
            if (k != null && Playing)
            {
                if (k.escapeKey.wasPressedThisFrame)
                {
                    if (InventoryOpen || MapOpen)
                    {
                        InventoryOpen = MapOpen = false;
                    }
                    else
                        Menu = !Menu;
                }

                if (!Menu && k.iKey.wasPressedThisFrame)
                {
                    InventoryOpen = !InventoryOpen;
                    MapOpen = false;
                }

                if (!Menu && k.mKey.wasPressedThisFrame)
                {
                    MapOpen = !MapOpen;
                    InventoryOpen = false;
                }
            }

            Cursor.lockState = Playing && !Blocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !Playing || Blocked;
            if (!Playing)
                return;
            Player.blocked = Blocked;
            Orbit.blocked = Blocked;
            Clock.paused = Menu;
            Focus = null;
            float best = 4;
            foreach (var pair in Sites.registry)
            {
                var t = pair.Value;
                if (!t || !t.gameObject.activeSelf)
                    continue;
                float d = Vector3.Distance(Player.transform.position, t.transform.position);
                if (d < best)
                {
                    best = d;
                    Focus = t;
                }
            }

            if (!Blocked && k != null)
            {
                if (k.fKey.wasPressedThisFrame)
                    torch.enabled = !torch.enabled;
                if (Focus && k.eKey.wasPressedThisFrame)
                {
                    var command = new WorldCommand(Focus.shrine ? WorldCommandKind.Restore : WorldCommandKind.Collect, Focus.id);
                    authority.Execute(command, Player.transform.position, out string msg);
                    Notify(msg);
                }
            }

            for (int i = 0; i < Landmarks.Sites.Length; i++)
            {
                var p = Landmarks.Sites[i];
                if (Vector2.Distance(new Vector2(Player.transform.position.x, Player.transform.position.z), p) < 18)
                {
                    string id = "shrine-" + i;
                    if (!State.discovered.Contains(id))
                    {
                        State.discovered.Add(id);
                        Notify("Local descoberto: " + Landmarks.Names[i]);
                    }
                }
            }

            if (ExplorationQuest.Evaluate(State))
            {
                Notify("Missão concluída! Recebeu o Artefato do Vale.");
                Save();
            }

            if (Time.unscaledTime >= autosave)
            {
                Save();
                autosave = Time.unscaledTime + 30;
            }

            if (Notice != "" && Time.unscaledTime > noticeUntil)
                Notice = "";
        }

        public void MenuToggle()
        {
            Menu = !Menu;
        }

        public void Notify(string message)
        {
            Notice = message;
            noticeUntil = Time.unscaledTime + 7;
        }

        public void Save()
        {
            if (!Playing)
                return;
            State.position = Player.transform.position;
            State.time = Clock.time;
            if (!SaveStore.Write(State, out string e))
                Notify("Falha ao salvar: " + e);
        }

        public void Drop(string id)
        {
            if (State.inventory.Remove(id, 1))
                Notify("Item descartado: " + id + " (removido permanentemente).");
        }

        public void Quit()
        {
            Save();
            applicationQuitting = true;
            Application.Quit();
        }

        void OnApplicationQuit()
        {
            if (!applicationQuitting)
                Save();
        }

        void OnDestroy()
        {
            if (palette != null)
                palette.Dispose();
            if (Settings)
                Destroy(Settings);
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
