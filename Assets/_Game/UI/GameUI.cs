using UnityEngine;

namespace Ecos
{
    public sealed class GameUI : MonoBehaviour
    {
        GameBootstrap game;
        GUIStyle title, body, small;
        Texture2D map;
        int mapSeed;
        float mapZoom = 8, drawnZoom;
        bool settings;
        Vector2 scroll;
        int selected = -1;
        readonly string[] quality =
        {
            "Baixa",
            "Média",
            "Alta"
        };
        void Awake()
        {
            game = GetComponent<GameBootstrap>();
        }

        void Styles()
        {
            if (title != null)
                return;
            title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            title.normal.textColor = new Color(1, .69f, .3f);
            body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                wordWrap = true
            };
            body.normal.textColor = new Color(.86f, .88f, .94f);
            small = new GUIStyle(body)
            {
                fontSize = 14
            };
            GUI.skin.button.fontSize = 16;
            GUI.skin.button.fixedHeight = 38;
        }

        void OnGUI()
        {
            Styles();
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            GUI.color = Color.white;
            if (game.Menu)
            {
                GUI.Box(new Rect(0, 0, width, height), "");
                GUILayout.BeginArea(new Rect(width / 2 - 235, 55, 470, height - 80));
                GUILayout.Label("ECOS DO VALE", title);
                GUILayout.Label("O silêncio guarda três vozes. Encontre-as.", body);
                GUILayout.Space(24);
                if (game.Playing && GUILayout.Button("Retomar"))
                    game.MenuToggle();
                if (GUILayout.Button("Novo jogo — seed " + game.NewSeed))
                {
                    game.Begin(false);
                    settings = false;
                }

                GUI.enabled = SaveStore.Exists;
                if (GUILayout.Button("Continuar"))
                {
                    game.Begin(true);
                    settings = false;
                }

                GUI.enabled = true;
                if (GUILayout.Button("Configurações"))
                    settings = !settings;
                if (settings)
                {
                    GUILayout.Label("Distância de visão: " + game.Settings.viewRadius + " chunks", small);
                    game.Settings.viewRadius = Mathf.RoundToInt(GUILayout.HorizontalSlider(game.Settings.viewRadius, 1, 4));
                    GUILayout.Label("Seed para novo jogo", small);
                    string seed = GUILayout.TextField(game.NewSeed.ToString());
                    if (int.TryParse(seed, out int number))
                        game.NewSeed = number;
                    int q = GUILayout.SelectionGrid(QualitySettings.GetQualityLevel(), quality, 3);
                    if (q != QualitySettings.GetQualityLevel())
                        QualitySettings.SetQualityLevel(q, true);
                    AudioListener.volume = GUILayout.HorizontalSlider(AudioListener.volume, 0, 1);
                }

                GUILayout.Label("Multiplayer: arquitetura preparada; conexão online ainda indisponível.", small);
                if (GUILayout.Button("Salvar e sair"))
                    game.Quit();
                GUILayout.Space(10);
                GUILayout.Label(game.Notice, body);
                GUILayout.EndArea();
                return;
            }

            if (!game.Playing)
                return;
            var s = game.State;
            var p = game.Player.transform.position;
            int b = WorldMath.Biome(p.x, p.z, s.seed);
            GUI.Box(new Rect(18, 18, 390, 125), "");
            GUI.Label(new Rect(32, 28, 365, 30), game.Settings.biomes[b].displayName, body);
            GUI.Label(new Rect(32, 62, 360, 72), ExplorationQuest.Objective(s), small);
            int hour = (int)(game.Clock.time * 24);
            int min = (int)(game.Clock.time * 1440) % 60;
            float yaw = game.Orbit.transform.eulerAngles.y;
            GUI.Label(new Rect(width / 2 - 160, 20, 320, 40), $"N  ·  {yaw:000}°  ·  {hour:00}:{min:00}", body);
            GUI.Label(new Rect(25, height - 52, width - 50, 40), "WASD mover · Mouse câmera · Shift correr · Espaço pular · Ctrl agachar · E interagir · F lanterna · I inventário · M mapa · Esc menu", small);
            if (game.Focus && !game.Blocked)
            {
                string prompt = game.Focus.shrine ? (s.restored.Contains(game.Focus.id) ? "Eco restaurado" : "Restaurar — 1 cristal") : "Coletar " + game.Focus.title;
                GUI.Box(new Rect(width / 2 - 210, height - 145, 420, 55), "");
                GUI.Label(new Rect(width / 2 - 195, height - 134, 390, 45), "[E] " + prompt, body);
            }

            if (game.Notice != "")
                GUI.Label(new Rect(width / 2 - 300, height - 225, 600, 70), game.Notice, body);
            if (game.InventoryOpen)
                DrawInventory(width, height);
            if (game.MapOpen)
                DrawMap(width, height);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
 if(UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.f3Key.isPressed)GUI.Label(new Rect(width-330,70,320,100),$"Chunks {game.Chunks.ActiveCount} · workers {game.Chunks.PendingCount}\nProps {game.Chunks.ObjectCount} · upload {game.Chunks.lastGenerationMs:F1} ms\nSeed {s.seed}",small);
#endif
        }

        void DrawInventory(float w, float h)
        {
            GUI.Box(new Rect(w / 2 - 320, 140, 640, 440), "");
            GUILayout.BeginArea(new Rect(w / 2 - 300, 155, 600, 410));
            GUILayout.Label("Inventário", title);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(225));
            for (int i = 0; i < game.State.inventory.stacks.Count; i++)
            {
                var stack = game.State.inventory.stacks[i];
                if (GUILayout.Button($"{Label(stack.id)} × {stack.count}"))
                    selected = i;
            }

            GUILayout.EndScrollView();
            if (selected >= 0 && selected < game.State.inventory.stacks.Count)
            {
                string id = game.State.inventory.stacks[selected].id;
                GUILayout.Label(Description(id), small);
                if (GUILayout.Button("Descartar uma unidade"))
                    game.Drop(id);
            }

            GUILayout.Label($"{game.State.inventory.stacks.Count}/{game.State.inventory.capacity} espaços · I para fechar", small);
            GUILayout.EndArea();
        }

        static string Label(string id)
        {
            return ItemCatalog.Name(id);
        }

        static string Description(string id)
        {
            return ItemCatalog.Description(id);
        }

        void DrawMap(float w, float h)
        {
            if (!map || mapSeed != game.State.seed || drawnZoom != mapZoom)
            {
                if (map)
                    Destroy(map);
                mapSeed = game.State.seed;
                drawnZoom = mapZoom;
                map = new Texture2D(192, 192, TextureFormat.RGB24, false);
                var colors = new Color[192 * 192];
                for (int z = 0; z < 192; z++)
                    for (int x = 0; x < 192; x++)
                    {
                        float wx = (x / 191f - .5f) * game.Settings.worldSize / mapZoom, wz = (z / 191f - .5f) * game.Settings.worldSize / mapZoom;
                        float elev = WorldMath.Height(wx, wz, mapSeed);
                        colors[z * 192 + x] = game.Settings.biomes[WorldMath.Biome(wx, wz, mapSeed)].ground * Mathf.Clamp(.9f + elev * .006f, .45f, 1.4f);
                    }

                map.SetPixels(colors);
                map.Apply();
            }

            var rect = new Rect(w / 2 - 250, 100, 500, 500);
            GUI.Box(new Rect(rect.x - 15, rect.y - 35, 530, 565), "");
            GUI.Label(new Rect(rect.x, rect.y - 30, 500, 30), $"Mapa · {game.Settings.worldSize / mapZoom:0} m · N ↑", body);
            if (GUI.Button(new Rect(rect.x + 320, rect.y - 34, 85, 30), "Zoom +"))
                mapZoom = Mathf.Min(32, mapZoom * 4);
            if (GUI.Button(new Rect(rect.x + 410, rect.y - 34, 85, 30), "Zoom −"))
                mapZoom = Mathf.Max(1, mapZoom / 4);
            GUI.DrawTexture(rect, map);
            Vector3 p = game.Player.transform.position;
            DrawMarker(rect, p.x, p.z, "● Você", Color.yellow);
            for (int i = 0; i < Landmarks.Sites.Length; i++)
                if (game.State.discovered.Contains("shrine-" + i))
                {
                    var pos = Landmarks.Sites[i];
                    DrawMarker(rect, pos.x, pos.y, "◆ " + Landmarks.Names[i], Color.cyan);
                }

            GUI.Label(new Rect(rect.x, rect.yMax + 4, 500, 28), "M para fechar · santuários próximos à origem", small);
        }

        void DrawMarker(Rect r, float x, float z, string label, Color c)
        {
            if (Mathf.Abs(x) > game.Settings.worldSize / mapZoom / 2 || Mathf.Abs(z) > game.Settings.worldSize / mapZoom / 2)
                return;
            GUI.color = c;
            float px = r.x + (x / (game.Settings.worldSize / mapZoom) + .5f) * r.width, py = r.y + (.5f - z / (game.Settings.worldSize / mapZoom)) * r.height;
            GUI.Label(new Rect(px, py, 220, 26), label, small);
            GUI.color = Color.white;
        }

        void OnDestroy()
        {
            if (map)
                Destroy(map);
        }
    }
}
