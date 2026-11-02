using UnityEngine;

namespace Ecos
{
    /// <summary>Immediate-mode presentation with cached styles and a consistent theme.</summary>
    public sealed class GameUI : MonoBehaviour
    {
        static readonly Color Ink = new Color(.025f, .055f, .075f, .94f);
        static readonly Color Card = new Color(.055f, .11f, .135f, .94f);
        static readonly Color Frame = new Color(.21f, .40f, .39f, .9f);
        static readonly Color Paper = new Color(.94f, .94f, .84f);
        static readonly Color Muted = new Color(.65f, .76f, .74f);
        static readonly Color Amber = new Color(1, .65f, .24f);
        static readonly Color Mint = new Color(.37f, .89f, .71f);
        static readonly string[] Visibility = { "Próxima e aconchegante", "Mais distante" };
        static readonly string[] Quality = { "Suave", "Equilibrada", "Detalhada" };
        static readonly string[] Directions = { "N", "NE", "L", "SE", "S", "SO", "O", "NO" };

        GameBootstrap game;
        GUIStyle title, heading, body, small, eyebrow, action, primary, quiet, field, center, caption, iconButton;
        Texture2D normalTexture, hoverTexture, pressedTexture, primaryTexture, primaryHoverTexture, gradient, map;
        Vector2 menuScroll, settingsScroll, inventoryScroll, mapCenter;
        bool settings, confirmNewGame, mapWasOpen;
        string seedText;
        int selected = -1, mapSeed, drawnWorldSize;
        float mapZoom = 2, drawnZoom;
        Vector2 drawnCenter;

        void Awake()
        {
            game = GetComponent<GameBootstrap>();
        }

        static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        void CreateStyles()
        {
            if (title != null) return;
            normalTexture = Solid(Card);
            hoverTexture = Solid(new Color(.12f, .25f, .26f, .98f));
            pressedTexture = Solid(new Color(.20f, .39f, .36f));
            primaryTexture = Solid(new Color(.88f, .43f, .14f));
            primaryHoverTexture = Solid(new Color(1, .59f, .20f));
            gradient = new Texture2D(256, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[256];
            for (int i = 0; i < pixels.Length; i++)
            {
                float t = i / 255f;
                pixels[i] = new Color(.018f, .042f, .057f, Mathf.Lerp(.96f, 0, Mathf.SmoothStep(0, 1, t)));
            }
            gradient.SetPixels(pixels);
            gradient.Apply();
            title = LabelStyle(66, Paper, FontStyle.Bold);
            title.wordWrap = false;
            title.richText = true;
            heading = LabelStyle(25, Paper, FontStyle.Bold);
            body = LabelStyle(19, Paper);
            small = LabelStyle(16, Muted);
            eyebrow = LabelStyle(13, Mint, FontStyle.Bold);
            caption = LabelStyle(14, Muted);
            center = LabelStyle(18, Paper, FontStyle.Bold);
            center.alignment = TextAnchor.MiddleCenter;
            action = ButtonStyle(normalTexture, hoverTexture, pressedTexture, Paper);
            primary = ButtonStyle(primaryTexture, primaryHoverTexture, primaryTexture, new Color(.13f, .09f, .055f));
            quiet = ButtonStyle(normalTexture, hoverTexture, pressedTexture, Muted);
            quiet.fontSize = 16;
            quiet.padding = new RectOffset(14, 14, 6, 6);
            iconButton = new GUIStyle(quiet) { fontSize = 22, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(0, 0, 0, 0) };
            field = new GUIStyle(GUI.skin.textField) { fontSize = 19, padding = new RectOffset(12, 12, 9, 9) };
            field.normal.textColor = Paper;
            field.focused.textColor = Paper;
            field.normal.background = normalTexture;
            field.focused.background = hoverTexture;
        }

        static GUIStyle LabelStyle(int size, Color color, FontStyle weight = FontStyle.Normal)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, wordWrap = true, richText = true };
            style.normal.textColor = color;
            return style;
        }

        static GUIStyle ButtonStyle(Texture2D normal, Texture2D hover, Texture2D active, Color text)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft,
                richText = true, padding = new RectOffset(18, 18, 9, 9), wordWrap = true,
                border = new RectOffset(0, 0, 0, 0)
            };
            style.normal.background = normal;
            style.hover.background = hover;
            style.active.background = active;
            style.focused.background = hover;
            style.onNormal.background = active;
            style.onHover.background = hover;
            style.onActive.background = active;
            style.onFocused.background = hover;
            style.onNormal.textColor = style.onHover.textColor = style.onActive.textColor = style.onFocused.textColor = text;
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = text;
            return style;
        }

        static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Panel(Rect rect, Color accent)
        {
            Fill(rect, Ink);
            Fill(new Rect(rect.x, rect.y, rect.width, 2), accent);
            Fill(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Frame * new Color(1, 1, 1, .5f));
        }

        void OnGUI()
        {
            CreateStyles();
            var previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            bool previousEnabled = GUI.enabled;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .82f, 1.5f);
            try
            {
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
                GUI.color = Color.white;
                float width = Screen.width / scale, height = Screen.height / scale;
                if (!game.MapOpen) mapWasOpen = false;
                if (game.Menu)
                {
                    DrawMenu(width, height);
                    return;
                }
                if (!game.Playing) return;
                DrawHud(width, height);
                if (game.InventoryOpen) DrawInventory(width, height);
                if (game.MapOpen) DrawMap(width, height);
            }
            finally
            {
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
                GUI.enabled = previousEnabled;
            }
        }

        void DrawMenu(float width, float height)
        {
            bool interactive = !settings && !confirmNewGame;
            GUI.enabled = interactive;
            if (game.Playing) Fill(new Rect(0, 0, width, height), new Color(.02f, .04f, .06f, .7f));
            GUI.DrawTexture(new Rect(0, 0, Mathf.Max(620, width * .8f), height), gradient);
            float left = Mathf.Clamp(width * .055f, 28, 84);
            float cardWidth = Mathf.Min(420, width - left * 2);
            bool compact = height < 650;
            GUI.Label(new Rect(left, 25, cardWidth, 24), game.Playing ? "UMA PAUSA NA JORNADA" : "EXPLORAÇÃO  /  MISTÉRIO  /  DESCOBERTA", eyebrow);
            title.fontSize = compact ? 53 : 66;
            GUI.Label(new Rect(left - 3, 54, cardWidth + 10, compact ? 132 : 160), "ECOS\n<color=#FFBC68>DO VALE</color>", title);
            float descriptionTop = compact ? 185 : 212;
            GUI.Label(new Rect(left, descriptionTop, cardWidth, 52), game.Playing ? "O vale guarda o seu lugar.\nRespire. Sua história continua." : "Pequenos caminhos. Grandes descobertas.\nAcenda sua lanterna e siga os ecos.", body);
            Fill(new Rect(left, descriptionTop + 65, 52, 3), Amber);
            Fill(new Rect(left + 60, descriptionTop + 65, 105, 1), Frame);

            float top = descriptionTop + 86;
            Rect viewport = new Rect(left, top, cardWidth, Mathf.Max(120, height - top - 75));
            float contentHeight = (game.Playing ? 64 : 0) + 226;
            menuScroll = GUI.BeginScrollView(viewport, menuScroll, new Rect(0, 0, cardWidth - 18, contentHeight), false, false);
            float y = 0;
            float inner = cardWidth - 20;
            if (game.Playing)
            {
                if (MenuAction(new Rect(0, y, inner, 56), "Retomar jornada", "Voltar à exploração", false, "Retome a jornada exatamente onde parou.", true))
                    game.MenuToggle();
                y += 64;
            }
            bool canStart = string.IsNullOrEmpty(game.RenderingError);
            GUI.enabled = interactive && canStart;
            if (MenuAction(new Rect(0, y, inner, 64), "Nova jornada", "Um novo mundo espera por você", true, "Monte um acampamento, siga as pistas e devolva a voz aos santuários."))
            {
                if (game.Playing || SaveStore.Exists) confirmNewGame = true;
                else StartJourney(false);
            }
            y += 72;
            GUI.enabled = interactive && canStart && SaveStore.Exists;
            if (MenuAction(new Rect(0, y, inner, 56), "Continuar", SaveStore.Exists ? "Seus caminhos e descobertas estão guardados" : "Sua primeira história ainda vai começar", false, "Retome seu progresso, seus itens e os lugares restaurados."))
                StartJourney(true);
            y += 64;
            GUI.enabled = interactive;
            if (MenuAction(new Rect(0, y, inner, 50), "Configurações", "Deixe a viagem do seu jeito", false, "Ajuste a visibilidade, a qualidade e o volume."))
            {
                settings = !settings;
                seedText = game.NewSeed.ToString();
                settingsScroll = Vector2.zero;
            }
            y += 58;
            GUI.enabled = false;
            GUI.Button(new Rect(0, y, inner, 32), new GUIContent("Cooperativo  ·  em breve", "Explore com amigos em uma atualização futura."), quiet);
            GUI.enabled = interactive;
            GUI.EndScrollView();

            if (GUI.Button(new Rect(left, height - 54, 92, 34), "Sair", quiet)) game.Quit();
            GUI.Label(new Rect(left + 108, height - 49, cardWidth - 110, 27), "Um mundo para explorar no seu ritmo", caption);
            if (width > 1000 && !settings && !confirmNewGame)
            {
                float infoWidth = Mathf.Min(360, width - left - cardWidth - 120);
                var info = new Rect(width - infoWidth - 42, height - 173, infoWidth, 128);
                Panel(info, Mint);
                GUI.Label(new Rect(info.x + 18, info.y + 13, info.width - 36, 22), "UM VALE CHEIO DE PEQUENAS HISTÓRIAS", eyebrow);
                string detail = string.IsNullOrEmpty(GUI.tooltip) ? "Bosques vivos, lugares esquecidos e recompensas a poucos passos da trilha." : GUI.tooltip;
                GUI.Label(new Rect(info.x + 18, info.y + 42, info.width - 36, 68), detail, body);
            }
            if (game.HasNotification)
            {
                float noticeWidth = Mathf.Min(610, width - 48);
                float noticeHeight = Mathf.Clamp(small.CalcHeight(new GUIContent(game.Notice), noticeWidth - 24) + 22, 54, 144);
                var notice = new Rect(width / 2 - noticeWidth / 2, 16, noticeWidth, noticeHeight);
                if (game.HasNotification)
                {
                    Panel(notice, Amber);
                    GUI.Label(new Rect(notice.x + 12, notice.y + 10, notice.width - 24, notice.height - 18), game.Notice, small);
                }
            }
            GUI.enabled = true;
            if (settings) DrawSettings(width, height);
            if (confirmNewGame) DrawNewGameConfirmation(width, height);
        }

        bool MenuAction(Rect rect, string label, string detail, bool isPrimary, string tooltip, bool resume = false)
        {
            if (resume) label = "Retomar jornada";
            string color = isPrimary ? "#49311C" : "#AEC7BE";
            string text = label + "\n<size=13><color=" + color + ">" + detail + "</color></size>";
            return GUI.Button(rect, new GUIContent(text, tooltip), isPrimary ? primary : action);
        }

        void StartJourney(bool resume)
        {
            game.Begin(resume);
            if (game.Playing && !game.Menu)
            {
                settings = confirmNewGame = false;
                menuScroll = Vector2.zero;
                selected = -1;
                mapWasOpen = false;
            }
        }

        void DrawNewGameConfirmation(float w, float h)
        {
            Fill(new Rect(0, 0, w, h), new Color(.015f, .035f, .05f, .78f));
            var panel = new Rect(w / 2 - 235, h / 2 - 125, 470, 250);
            Panel(panel, Amber);
            GUI.Label(new Rect(panel.x + 24, panel.y + 22, 420, 36), "Começar uma nova história?", heading);
            GUI.Label(new Rect(panel.x + 24, panel.y + 75, 420, 80), "A jornada atual será substituída no próximo salvamento. Seus novos passos começam com o mundo escolhido nas configurações.", body);
            if (GUI.Button(new Rect(panel.x + 24, panel.y + 184, 150, 42), "Voltar", quiet)) confirmNewGame = false;
            if (GUI.Button(new Rect(panel.x + 190, panel.y + 184, 255, 42), "Criar novo mundo", primary)) StartJourney(false);
        }

        void DrawSettings(float w, float h)
        {
            Fill(new Rect(0, 0, w, h), new Color(.015f, .035f, .05f, .80f));
            float width = Mathf.Min(620, w - 48), height = Mathf.Min(610, h - 48);
            var panel = new Rect(w / 2 - width / 2, h / 2 - height / 2, width, height);
            Panel(panel, Mint);
            GUI.Label(new Rect(panel.x + 24, panel.y + 18, width - 90, 36), "Sua viagem, seu ritmo", heading);
            if (GUI.Button(new Rect(panel.xMax - 58, panel.y + 17, 36, 33), "×", iconButton)) settings = false;
            var viewport = new Rect(panel.x + 24, panel.y + 70, width - 48, height - 94);
            settingsScroll = GUI.BeginScrollView(viewport, settingsScroll, new Rect(0, 0, viewport.width - 18, 447), false, false);
            float cw = viewport.width - 22;
            GUI.Label(new Rect(0, 0, cw, 24), "MUNDO DA PRÓXIMA JORNADA", eyebrow);
            GUI.Label(new Rect(0, 30, cw, 26), "Semente do mundo", body);
            seedText = GUI.TextField(new Rect(0, 62, cw, 44), seedText ?? game.NewSeed.ToString(), field);
            bool validSeed = int.TryParse(seedText, out int seed);
            if (validSeed) game.NewSeed = seed;
            GUI.Label(new Rect(0, 111, cw, 40), validSeed ? "A mesma semente permite revisitar o mesmo vale." : "Use um número inteiro para escolher o mundo.", small);
            GUI.Label(new Rect(0, 166, cw, 24), "APARÊNCIA", eyebrow);
            int current = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Quality.Length - 1);
            int chosen = GUI.SelectionGrid(new Rect(0, 202, cw, 42), current, Quality, 3, quiet);
            if (chosen != QualitySettings.GetQualityLevel()) game.SetQuality(chosen);
            GUI.Label(new Rect(0, 260, cw, 24), "Visibilidade", body);
            game.Settings.viewRadius = Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(0, 299, cw, 20), game.Settings.viewRadius, 1, 2));
            GUI.Label(new Rect(0, 324, cw, 26), Visibility[Mathf.Clamp(game.Settings.viewRadius - 1, 0, 1)], small);
            GUI.Label(new Rect(0, 371, cw, 24), "Volume", body);
            AudioListener.volume = GUI.HorizontalSlider(new Rect(0, 411, cw, 20), AudioListener.volume, 0, 1);
            GUI.EndScrollView();
        }

        void DrawHud(float w, float h)
        {
            var state = game.State;
            Vector3 position = game.Player.transform.position;
            int biome = WorldMath.Biome(position.x, position.z, state.seed);
            float leftWidth = Mathf.Min(375, w * .34f);
            string objective = ExplorationQuest.Objective(state);
            float objectiveHeight = Mathf.Clamp(small.CalcHeight(new GUIContent(objective), leftWidth - 36), 65, 138);
            var quest = new Rect(20, 20, leftWidth, objectiveHeight + 84);
            Panel(quest, Mint);
            GUI.Label(new Rect(quest.x + 16, quest.y + 12, quest.width - 32, 23), "SEUS PASSOS NO VALE", eyebrow);
            GUI.Label(new Rect(quest.x + 16, quest.y + 40, quest.width - 32, 29), game.Settings.biomes[biome].displayName, body);
            GUI.Label(new Rect(quest.x + 16, quest.y + 77, quest.width - 32, objectiveHeight), objective, small);

            int hour = (int)(game.Clock.time * 24), minute = (int)(game.Clock.time * 1440) % 60;
            float yaw = game.Orbit.transform.eulerAngles.y;
            string direction = Directions[Mathf.RoundToInt(yaw / 45) % Directions.Length];
            float compassWidth = w < 900 ? 150 : 198;
            var compass = new Rect(w / 2 - compassWidth / 2, 20, compassWidth, 42);
            Panel(compass, Frame);
            GUI.Label(compass, direction + "   ·   " + hour.ToString("00") + ":" + minute.ToString("00"), center);

            float resourcesWidth = Mathf.Min(335, w * .29f);
            var resources = new Rect(w - resourcesWidth - 20, 20, resourcesWidth, 101);
            Panel(resources, Amber);
            GUI.Label(new Rect(resources.x + 16, resources.y + 12, resources.width - 32, 25), "VIAJANTE  ·  NÍVEL " + ProgressionRules.Level(state), eyebrow);
            GUI.Label(new Rect(resources.x + 16, resources.y + 43, resources.width - 32, 26), "Madeira " + state.inventory.Count("wood") + "    Pedra " + state.inventory.Count("stone"), small);
            GUI.Label(new Rect(resources.x + 16, resources.y + 69, resources.width - 32, 25), "Cristais " + state.inventory.Count("crystal") + "    Ervas " + state.inventory.Count("herb") + "    XP " + state.experience, small);

            string controls = w < 1150 ? "E interagir   ·   F lanterna   ·   I mochila   ·   M mapa   ·   Esc pausa" : "WASD mover   ·   mouse olhar   ·   Shift correr   ·   Espaço pular   ·   E interagir   ·   F lanterna   ·   I mochila   ·   M mapa   ·   Esc pausa";
            GUI.Label(new Rect(25, h - 39, w - 50, 28), controls, caption);
            if (game.Focus && !game.Blocked)
            {
                string promptText = "[E]  " + game.Focus.Prompt(state);
                float promptHeight = Mathf.Clamp(center.CalcHeight(new GUIContent(promptText), 442) + 24, 56, 80);
                var prompt = new Rect(w / 2 - 235, h - 61 - promptHeight, 470, promptHeight);
                Panel(prompt, Amber);
                GUI.Label(new Rect(prompt.x + 14, prompt.y + 12, prompt.width - 28, prompt.height - 20), promptText, center);
            }
            if (game.HasNotification)
            {
                float noticeWidth = Mathf.Min(660, w - 60);
                float noticeHeight = Mathf.Clamp(body.CalcHeight(new GUIContent(game.Notice), noticeWidth - 32) + 26, 65, 166);
                var notice = new Rect(w / 2 - noticeWidth / 2, h - 162 - noticeHeight, noticeWidth, noticeHeight);
                Panel(notice, Mint);
                GUI.Label(new Rect(notice.x + 16, notice.y + 13, notice.width - 32, notice.height - 22), game.Notice, body);
            }
            if (state.lensCrafted)
            {
                string lens = game.ScanDurationRemaining > 0 ? "Ecos revelados  ·  " + Mathf.CeilToInt(game.ScanDurationRemaining) + "s"
                    : game.ScanCooldownRemaining > 0 ? "A lente se recompõe  ·  " + Mathf.CeilToInt(game.ScanCooldownRemaining) + "s" : "[Q]  Revelar ecos próximos";
                GUI.Label(new Rect(w - resourcesWidth - 20, 131, resourcesWidth, 40), lens, small);
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f3Key.isPressed)
                GUI.Label(new Rect(w - 370, h - 91, 350, 45), "Chunks " + game.Chunks.ActiveCount + " · props " + game.Chunks.ObjectCount + " · upload " + game.Chunks.lastGenerationMs.ToString("F1") + " ms", caption);
#endif
        }

        void DrawInventory(float w, float h)
        {
            Fill(new Rect(0, 0, w, h), new Color(.015f, .035f, .05f, .64f));
            float width = Mathf.Min(800, w - 48), height = Mathf.Min(570, h - 56);
            var panel = new Rect(w / 2 - width / 2, h / 2 - height / 2, width, height);
            Panel(panel, Amber);
            GUI.Label(new Rect(panel.x + 24, panel.y + 18, width - 100, 36), "Sua mochila", heading);
            if (GUI.Button(new Rect(panel.xMax - 60, panel.y + 18, 36, 32), "×", iconButton)) game.InventoryOpen = false;
            GUI.Label(new Rect(panel.x + 24, panel.y + 59, width - 48, 25), game.State.inventory.stacks.Count + " / " + game.State.inventory.capacity + " espaços  ·  " + game.State.readClues.Count + " fragmentos lidos", small);
            float listWidth = width * .50f - 30;
            var viewport = new Rect(panel.x + 24, panel.y + 103, listWidth, height - 132);
            inventoryScroll = GUI.BeginScrollView(viewport, inventoryScroll, new Rect(0, 0, listWidth - 18, Mathf.Max(viewport.height, game.State.inventory.stacks.Count * 66)), false, false);
            for (int i = 0; i < game.State.inventory.stacks.Count; i++)
            {
                var stack = game.State.inventory.stacks[i];
                string text = ItemCatalog.Name(stack.id) + "\n<size=13><color=#AEC7BE>" + stack.count + " na mochila</color></size>";
                if (GUI.Button(new Rect(0, i * 66, listWidth - 20, 58), text, selected == i ? primary : action)) selected = i;
            }
            GUI.EndScrollView();
            if (game.State.inventory.stacks.Count == 0)
                GUI.Label(new Rect(viewport.x + 14, viewport.y + 25, viewport.width - 28, 110), "Sua mochila está leve.\nProcure recursos e pequenos tesouros ao redor da trilha.", body);
            var detail = new Rect(panel.x + width * .5f + 12, panel.y + 103, width * .5f - 36, height - 132);
            Fill(detail, new Color(.08f, .15f, .16f, .88f));
            if (selected >= game.State.inventory.stacks.Count) selected = game.State.inventory.stacks.Count - 1;
            if (selected >= 0)
            {
                var stack = game.State.inventory.stacks[selected];
                GUI.Label(new Rect(detail.x + 18, detail.y + 19, detail.width - 36, 64), ItemCatalog.Name(stack.id), heading);
                GUI.Label(new Rect(detail.x + 18, detail.y + 94, detail.width - 36, Mathf.Max(65, detail.height - 188)), ItemCatalog.Description(stack.id), body);
                if (GUI.Button(new Rect(detail.x + 18, detail.yMax - 65, detail.width - 36, 44), "Descartar uma unidade", quiet)) game.Drop(stack.id);
            }
            else
                GUI.Label(new Rect(detail.x + 20, detail.y + 25, detail.width - 40, 100), "Selecione um item para conhecer sua história.", body);
        }

        void RebuildMap()
        {
            if (map) Destroy(map);
            mapSeed = game.State.seed;
            drawnZoom = mapZoom;
            drawnCenter = mapCenter;
            drawnWorldSize = game.Settings.worldSize;
            map = new Texture2D(192, 192, TextureFormat.RGB24, false) { filterMode = FilterMode.Bilinear };
            var colors = new Color[192 * 192];
            for (int z = 0; z < 192; z++)
                for (int x = 0; x < 192; x++)
                {
                    float wx = mapCenter.x + (x / 191f - .5f) * game.Settings.worldSize / mapZoom;
                    float wz = mapCenter.y + (z / 191f - .5f) * game.Settings.worldSize / mapZoom;
                    float half = game.Settings.worldSize * .5f;
                    colors[z * 192 + x] = Mathf.Abs(wx) > half || Mathf.Abs(wz) > half ? new Color(.035f, .07f, .085f)
                        : game.Settings.biomes[WorldMath.Biome(wx, wz, mapSeed)].ground * Mathf.Clamp(1 + WorldMath.Height(wx, wz, mapSeed) * .005f, .65f, 1.2f);
                }
            map.SetPixels(colors);
            map.Apply();
        }

        void DrawMap(float w, float h)
        {
            if (!mapWasOpen)
            {
                Vector3 position = game.Player.transform.position;
                mapCenter = new Vector2(Mathf.Round(position.x / 32) * 32, Mathf.Round(position.z / 32) * 32);
                mapWasOpen = true;
            }
            if (!map || mapSeed != game.State.seed || drawnZoom != mapZoom || drawnCenter != mapCenter || drawnWorldSize != game.Settings.worldSize)
                RebuildMap();
            Fill(new Rect(0, 0, w, h), new Color(.015f, .035f, .05f, .68f));
            float width = Mathf.Min(900, w - 48), height = Mathf.Min(710, h - 44);
            var panel = new Rect(w / 2 - width / 2, h / 2 - height / 2, width, height);
            Panel(panel, Mint);
            GUI.Label(new Rect(panel.x + 22, panel.y + 16, width - 110, 38), "Cartografia do vale", heading);
            if (GUI.Button(new Rect(panel.xMax - 58, panel.y + 16, 36, 32), "×", iconButton)) game.MapOpen = false;
            float legendWidth = width > 700 ? 190 : 0;
            float mapSize = Mathf.Min(height - 122, width - 44 - legendWidth);
            float groupWidth = mapSize + (legendWidth > 0 ? legendWidth + 24 : 0);
            var canvas = new Rect(panel.x + (width - groupWidth) / 2, panel.y + 71, mapSize, mapSize);
            GUI.DrawTexture(canvas, map);
            Fill(new Rect(canvas.x, canvas.y, canvas.width, 2), Frame);
            Fill(new Rect(canvas.x, canvas.yMax - 2, canvas.width, 2), Frame);
            Vector3 player = game.Player.transform.position;
            DrawMapMarker(canvas, player.x, player.z, "Você", Amber, true);
            foreach (var location in Landmarks.Locations)
                if (game.State.discovered.Contains(location.id))
                {
                    Color marker = location.id.StartsWith("shrine-") ? Mint : location.id == "camp" ? Amber : new Color(.83f, .61f, .88f);
                    DrawMapMarker(canvas, location.position.x, location.position.y, location.title, marker, false);
                }
            if (legendWidth > 0)
            {
                float x = canvas.xMax + 24;
                GUI.Label(new Rect(x, canvas.y, legendWidth, 24), "NORTE  ↑", eyebrow);
                GUI.Label(new Rect(x, canvas.y + 33, legendWidth, 46), (game.Settings.worldSize / mapZoom).ToString("0") + " m de paisagem", body);
                GUI.Label(new Rect(x, canvas.y + 92, legendWidth, 23), "BIOMAS", eyebrow);
                for (int i = 0; i < game.Settings.biomes.Length; i++)
                {
                    float y = canvas.y + 125 + i * 35;
                    Fill(new Rect(x, y + 5, 12, 12), game.Settings.biomes[i].ground);
                    GUI.Label(new Rect(x + 22, y, legendWidth - 22, 34), game.Settings.biomes[i].displayName, caption);
                }
            }
            float controlsY = panel.yMax - 43;
            GUI.Label(new Rect(panel.x + 22, controlsY + 4, 180, 27), "M para fechar  ·  N ↑", caption);
            if (GUI.Button(new Rect(panel.xMax - 262, controlsY, 108, 31), "Aproximar", quiet)) mapZoom = Mathf.Min(16, mapZoom * 2);
            if (GUI.Button(new Rect(panel.xMax - 144, controlsY, 122, 31), "Afastar", quiet)) mapZoom = Mathf.Max(1, mapZoom / 2);
        }

        void DrawMapMarker(Rect mapRect, float x, float z, string label, Color color, bool player)
        {
            float extent = game.Settings.worldSize / mapZoom;
            float nx = (x - mapCenter.x) / extent + .5f, nz = .5f - (z - mapCenter.y) / extent;
            if (nx < 0 || nx > 1 || nz < 0 || nz > 1) return;
            float px = mapRect.x + nx * mapRect.width, py = mapRect.y + nz * mapRect.height;
            float size = player ? 10 : 7;
            Fill(new Rect(px - size / 2 - 2, py - size / 2 - 2, size + 4, size + 4), Ink);
            Fill(new Rect(px - size / 2, py - size / 2, size, size), color);
            GUI.Label(new Rect(px - 9, py - 9, 18, 18), new GUIContent("", label), caption);
            if (player || mapZoom >= 4)
            {
                float lx = Mathf.Clamp(px + 10, mapRect.x + 4, mapRect.xMax - 170);
                GUI.Label(new Rect(lx, py - 10, 166, 36), label, caption);
            }
        }

        void OnDestroy()
        {
            foreach (var texture in new[] { normalTexture, hoverTexture, pressedTexture, primaryTexture, primaryHoverTexture, gradient, map })
                if (texture) Destroy(texture);
        }
    }
}
