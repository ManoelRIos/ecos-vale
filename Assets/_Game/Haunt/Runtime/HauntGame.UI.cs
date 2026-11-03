using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        static readonly Color Ink = new Color(.035f, .025f, .07f, .97f);
        static readonly Color Muted = new Color(.72f, .72f, .8f);
        static readonly Color Mint = new Color(.48f, .96f, .77f);
        static readonly Color Orange = new Color(1, .58f, .24f);
        Texture2D panelTexture;
        GUIStyle titleStyle, headingStyle, bodyStyle, smallStyle, hudStyle, buttonStyle, fieldStyle;
        bool stylesReady;

        void EnsureStyles()
        {
            if (stylesReady) return;
            stylesReady = true;
            panelTexture = new Texture2D(1, 1) { name = "Haunt UI pixel", filterMode = FilterMode.Point };
            panelTexture.SetPixel(0, 0, Color.white);
            panelTexture.Apply();
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 56, fontStyle = FontStyle.Bold, wordWrap = false };
            headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
            smallStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true };
            hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, wordWrap = false };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, border = new RectOffset(0, 0, 0, 0) };
            buttonStyle.normal.background = panelTexture;
            buttonStyle.hover.background = panelTexture;
            buttonStyle.active.background = panelTexture;
            buttonStyle.focused.background = panelTexture;
            buttonStyle.normal.textColor = Ink;
            buttonStyle.hover.textColor = Ink;
            buttonStyle.active.textColor = Ink;
            fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 17, padding = new RectOffset(12, 12, 10, 10) };
        }

        void OnGUI()
        {
            EnsureStyles();
            Matrix4x4 previous = GUI.matrix;
            Color color = GUI.color;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            float offsetX = (Screen.width - 1280 * scale) * .5f;
            float offsetY = (Screen.height - 720 * scale) * .5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            GUI.color = Color.white;
            if (!string.IsNullOrEmpty(renderingFailure))
            {
                Panel(new Rect(150, 240, 980, 240), Orange);
                Text(new Rect(180, 270, 920, 50), "ARTE INDISPONÍVEL", headingStyle, Orange);
                Text(new Rect(180, 335, 920, 100), renderingFailure, bodyStyle, Color.white);
            }
            else if (!Playing) DrawMenu();
            else
            {
                DrawHud();
                if (InShop && !Paused && !State.IsDead) DrawShop();
                if (showMap && !InShop && !Paused && !State.IsDead) DrawMap(new Rect(425, 145, 430, 430), true);
                if (State.IsDead) DrawDeath();
                else if (Paused) DrawPause();
                if (flash > 0) Fill(new Rect(0, 0, 1280, 720), new Color(.8f, .1f, .2f, flash * 1.4f));
            }
            if (noticeTime > 0 && !Paused)
            {
                Panel(new Rect(22, 616, 472, 66), new Color(.18f, .4f, .35f));
                Text(new Rect(36, 624, 444, 54), notice, smallStyle, Mint);
            }
            GUI.matrix = previous;
            GUI.color = color;
        }

        void DrawMenu()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(.02f, .012f, .045f, .48f));
            Fill(new Rect(60, 86, 5, 526), Orange);
            Text(new Rect(90, 82, 570, 25), "UM ROGUELIKE DE PÓLVORA E ASSOMBRAÇÕES", smallStyle, Orange);
            Text(new Rect(86, 127, 650, 72), "ECOS DA", titleStyle, new Color(.95f, .92f, .85f));
            Text(new Rect(86, 185, 660, 80), "MEIA-NOITE", titleStyle, Orange);
            Text(new Rect(91, 284, 520, 78), "A cripta muda. A noite nunca termina.\nSobreviva ao guardião. Negocie com as almas. Desça mais um andar.", bodyStyle, new Color(.8f, .78f, .85f));
            Text(new Rect(91, 384, 450, 24), "SEMENTE DA INCURSÃO  /  vazio = aleatória", smallStyle, Muted);
            seedText = GUI.TextField(new Rect(92, 418, 435, 44), seedText, 11, fieldStyle);
            if (Button(new Rect(92, 480, 435, 56), "INICIAR INCURSÃO     →", Orange)) StartFromMenu();
            Text(new Rect(92, 552, 460, 32), "ENTER para começar  •  PC / teclado e mouse", smallStyle, Muted);
            Text(new Rect(92, 592, 470, 48), "SALAS PROCEDURAIS  •  COMBATE  •  MERCADO", smallStyle, Mint);
            DrawIcon(new Rect(570, 398, 110, 138), "player");
            Text(new Rect(557, 550, 145, 40), "VOCÊ\nCAÇADOR", smallStyle, Mint);

            Panel(new Rect(740, 100, 446, 504), new Color(.25f, .22f, .35f));
            Text(new Rect(770, 126, 380, 40), "O QUE HABITA A CRIPTA", headingStyle, Mint);
            string[] ids = { "pumpkin", "ghost", "cultist", "bat" };
            string[] names = { "ABÓBORA VORAZ", "ALMA ERRANTE", "CULTISTA DAS CINZAS", "MORCEGO FUNESTO" };
            string[] details = { "Persegue até encurralar.", "Flutua e dispara à distância.", "Lança um leque de maldições.", "Prepara o bote. Depois avança." };
            for (int i = 0; i < 4; i++)
            {
                float y = 186 + i * 77;
                DrawIcon(new Rect(773, y, 48, 56), ids[i]);
                Text(new Rect(840, y + 3, 310, 24), names[i], smallStyle, Orange);
                Text(new Rect(840, y + 28, 310, 38), details[i], smallStyle, new Color(.8f, .78f, .85f));
            }
            Text(new Rect(773, 525, 370, 52), "Cada guardião vencido abre um mercado seguro. Suas melhorias duram até a morte.", smallStyle, Muted);
            Text(new Rect(740, 623, 460, 40), "WASD mover  •  mouse mirar  •  clique atirar\nEspaço / clique direito esquivar  •  E interagir", smallStyle, Color.white);
        }

        void DrawHud()
        {
            // Keep the entire header in one 64px strip; no title or boss bar sits
            // over a doorway. The interface uses the same bounds for pointer capture.
            Fill(new Rect(0, 0, 1280, 64), Ink);
            Fill(new Rect(0, 63, 1280, 2), new Color(.24f, .3f, .34f));
            Text(new Rect(22, 8, 290, 27), "ECOS DA MEIA-NOITE", hudStyle, Orange);
            Text(new Rect(23, 35, 290, 25), "ANDAR " + State.Floor + (InShop ? "  /  MERCADO" : "  /  INCURSÃO"), smallStyle, Muted);
            for (int i = 0; i < State.MaxHealth; i++)
            {
                Rect heart = new Rect(329 + (i % 10) * 22, i < 10 ? 8 : 33, 19, 21);
                Color previous = GUI.color;
                GUI.color = i < State.Health ? Color.white : new Color(.3f, .25f, .33f);
                DrawIcon(heart, "heart");
                GUI.color = previous;
            }
            DrawIcon(new Rect(582, 18, 24, 26), "coin");
            Text(new Rect(616, 17, 112, 30), State.Coins.ToString("000"), headingStyle, Orange);
            Enemy boss = null;
            foreach (var enemy in enemies) if (enemy.Kind == 4) { boss = enemy; break; }
            if (boss != null)
            {
                Text(new Rect(748, 8, 305, 27), BossNames[(State.Floor - 1) % 3], smallStyle, Orange);
                Fill(new Rect(750, 40, 294, 8), new Color(.23f, .1f, .18f));
                Fill(new Rect(750, 40, 294f * boss.Health / boss.MaxHealth, 8), new Color(.96f, .26f, .38f));
            }
            else
            {
                Text(new Rect(748, 8, 304, 25), "DANO " + State.Damage + "   /   " + (1f / State.ShotInterval).ToString("0.0") + " TIROS/S", smallStyle, Mint);
                Text(new Rect(748, 35, 304, 25), State.Health + " / " + State.MaxHealth + " VIDA  •  M PARA MAPA", smallStyle, Muted);
            }
            Text(new Rect(1080, 8, 190, 25), rollReady <= 0 ? "ESQUIVA PRONTA" : "RECARREGANDO", smallStyle, rollReady <= 0 ? Mint : Muted);
            Fill(new Rect(1082, 38, 168, 7), new Color(.16f, .13f, .22f));
            Fill(new Rect(1082, 38, 168 * (1 - Mathf.Clamp01(rollReady / RollCooldown)), 7), Mint);

            if (!InShop)
            {
                string roomTitle = CurrentRoom.Kind == RoomKind.Boss ? "CÂMARA DO GUARDIÃO"
                    : CurrentRoom.Kind == RoomKind.Entrance ? "LIMIAR DA CRIPTA" : "SALA " + CurrentRoom.Id.ToString("00");
                Color accent = enemies.Count > 0 ? Orange : Mint;
                Color border = Color.Lerp(new Color(.15f, .2f, .25f), accent, Mathf.Clamp01(roomBannerTime));
                Panel(new Rect(22, 82, 320, 89), border);
                Text(new Rect(36, 89, 296, 24), roomTitle, hudStyle, accent);
                Text(new Rect(36, 115, 296, 23), RoomThemeName, smallStyle, Color.white);
                Text(new Rect(36, 142, 296, 22), enemies.Count > 0 ? enemies.Count + " AMEAÇAS  /  PORTAS SELADAS" : "PURIFICADA  /  SAÍDAS ABERTAS", smallStyle, accent);
                DrawMap(new Rect(1110, 538, 146, 140), false);
                DrawDoorLabels();
                if (roomBannerTime > 0 && !Paused && !State.IsDead)
                    WorldTag(playerPosition, "VOCÊ", Mint, 2.05f, 92);
                if (CurrentRoom.Kind == RoomKind.Boss && CurrentRoom.Cleared)
                    WorldTag(new Vector2(0, 1.4f), "[E] MERCADO DAS ALMAS", Mint);
            }
            else
            {
                Panel(new Rect(22, 82, 430, 59), new Color(.3f, .5f, .4f));
                Text(new Rect(36, 89, 403, 25), "MERCADO DAS ALMAS", hudStyle, Mint);
                Text(new Rect(36, 116, 403, 23), "ÁREA SEGURA  /  ESCOLHA SUAS MELHORIAS", smallStyle, Muted);
                WorldTag(new Vector2(8.4f, .1f), "[E] PRÓXIMO ANDAR", Mint, 1.6f, 216);
            }
            Fill(new Rect(0, 692, 1280, 28), Ink);
            Text(new Rect(22, 695, 1018, 25), "WASD mover  •  Mouse mirar / atirar  •  Espaço esquivar  •  E interagir  •  M mapa  •  Esc pausa", smallStyle, Muted);
            Text(new Rect(1056, 695, 220, 25), "SEED " + State.Seed, smallStyle, Muted);
        }

        void DrawDoorLabels()
        {
            if (Paused || State.IsDead) return;
            for (int i = 0; i < doors.Count && i < CurrentRoom.Neighbors.Count; i++)
            {
                Vector2 position = doors[i].transform.position;
                if (Vector2.Distance(position, playerPosition) > 3.7f) continue;
                DungeonRoom next = Floor.GetRoom(CurrentRoom.Neighbors[i]);
                Vector2 direction = new Vector2(next.Position.X - CurrentRoom.Position.X, next.Position.Y - CurrentRoom.Position.Y);
                bool locked = enemies.Count > 0;
                string arrow = direction.x > 0 ? "→" : direction.x < 0 ? "←" : direction.y > 0 ? "↑" : "↓";
                string label = locked ? "×  SELADA" : arrow + (next.Kind == RoomKind.Boss && !next.Cleared ? "  GUARDIÃO" : "  SAÍDA ABERTA");
                float rise = direction.y > 0 ? -.25f : direction.y < 0 ? 1.7f : 1.45f;
                WorldTag(position - direction * 1.15f, label, locked ? new Color(1, .48f, .48f) : Mint, rise, 190);
            }
        }

        // The world should not fire through a click on an interface panel.
        bool PointerOverInterface()
        {
            if (!Playing || Paused || InShop || State.IsDead || showMap) return true;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return false;
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            Vector2 p = mouse.position.ReadValue();
            p.x = (p.x - (Screen.width - 1280 * scale) * .5f) / scale;
            p.y = (Screen.height - p.y - (Screen.height - 720 * scale) * .5f) / scale;
            return new Rect(0, 0, 1280, 64).Contains(p) || new Rect(0, 692, 1280, 28).Contains(p)
                || new Rect(22, 82, 320, 89).Contains(p) || new Rect(1110, 538, 146, 140).Contains(p);
        }

        void DrawShop()
        {
            string[] names = { "TÔNICO ESPECTRAL", "PÓLVORA MALDITA", "GATILHO FANTASMA", "CORAÇÃO DE CERA" };
            string[] descriptions = { "Recupera até 3 pontos de vida.", "+1 de dano por projétil.", "Intervalo entre tiros 15% menor.", "+2 de vida máxima e recupera 2." };
            for (int i = 0; i < 4; i++)
            {
                ShopItem item = (ShopItem)i;
                float x = 55 + i * 296;
                bool sold = State.Sold(item);
                bool affordable = State.Coins >= State.Price(item);
                Panel(new Rect(x, 492, 282, 135), sold ? new Color(.2f, .2f, .25f) : new Color(.32f, .25f, .37f));
                Text(new Rect(x + 13, 502, 260, 22), names[i], smallStyle, sold ? Muted : Orange);
                Text(new Rect(x + 13, 530, 256, 36), descriptions[i], smallStyle, Color.white);
                bool previous = GUI.enabled;
                GUI.enabled = !sold && affordable;
                if (Button(new Rect(x + 13, 578, 256, 34), sold ? "ESGOTADO" : "COMPRAR  /  " + State.Price(item) + " MOEDAS", Mint)) Buy(item);
                GUI.enabled = previous;
            }
        }

        void DrawMap(Rect rect, bool expanded)
        {
            Panel(rect, new Color(.23f, .2f, .3f));
            if (expanded) Text(new Rect(rect.x + 20, rect.y + 14, rect.width - 40, 30), "ATLAS DA CRIPTA", headingStyle, Mint);
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var room in Floor.Rooms)
            {
                minX = Mathf.Min(minX, room.Position.X); maxX = Mathf.Max(maxX, room.Position.X);
                minY = Mathf.Min(minY, room.Position.Y); maxY = Mathf.Max(maxY, room.Position.Y);
            }
            float top = expanded ? 65 : 12;
            float bottom = expanded ? 70 : 26;
            float spacing = Mathf.Min((rect.width - 40) / (maxX - minX + 1), (rect.height - top - bottom) / (maxY - minY + 1));
            spacing = Mathf.Min(spacing, expanded ? 48 : 24);
            Vector2 center = new Vector2(rect.center.x - (minX + maxX) * .5f * spacing,
                rect.y + top + (rect.height - top - bottom) * .5f + (minY + maxY) * .5f * spacing);
            foreach (var room in Floor.Rooms)
            {
                Vector2 p = center + new Vector2(room.Position.X, -room.Position.Y) * spacing;
                foreach (int id in room.Neighbors)
                {
                    var next = Floor.GetRoom(id);
                    Vector2 n = center + new Vector2(next.Position.X, -next.Position.Y) * spacing;
                    Fill(new Rect(Mathf.Min(p.x, n.x) - 1, Mathf.Min(p.y, n.y) - 1, Mathf.Abs(p.x - n.x) + 2, Mathf.Abs(p.y - n.y) + 2), new Color(.26f, .23f, .33f));
                }
            }
            foreach (var room in Floor.Rooms)
            {
                Vector2 p = center + new Vector2(room.Position.X, -room.Position.Y) * spacing;
                Color c = room == CurrentRoom ? Color.white : room.Kind == RoomKind.Boss ? new Color(.9f, .28f, .38f)
                    : room.Cleared ? Mint : visited.Contains(room.Id) ? Orange : new Color(.35f, .3f, .43f);
                float size = expanded ? 20 : 9;
                Fill(new Rect(p.x - size * .5f, p.y - size * .5f, size, size), c);
            }
            if (expanded)
            {
                Text(new Rect(rect.x + 20, rect.yMax - 53, rect.width - 40, 44), "Branco: você  •  Verde: purificada\nVermelho: guardião  •  M para fechar", smallStyle, Muted);
            }
            else Text(new Rect(rect.x + 15, rect.yMax - 26, rect.width - 30, 23), "M  /  MAPA", smallStyle, Muted);
        }

        void DrawPause()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(0, 0, .025f, .78f));
            Panel(new Rect(410, 170, 460, 370), Orange);
            Text(new Rect(445, 197, 390, 45), "A NOITE ESPERA", headingStyle, Orange);
            Text(new Rect(445, 255, 390, 50), "A incursão está pausada. Sair para o menu encerra esta tentativa.", bodyStyle, Color.white);
            if (Button(new Rect(445, 330, 390, 52), "CONTINUAR", Mint)) Paused = false;
            if (Button(new Rect(445, 401, 390, 48), "ENCERRAR INCURSÃO", new Color(.65f, .58f, .73f))) ReturnToMenu();
            Text(new Rect(445, 484, 390, 28), "ESC para voltar", smallStyle, Muted);
        }

        void DrawDeath()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(.035f, .005f, .025f, .88f));
            Panel(new Rect(355, 132, 570, 450), new Color(.7f, .22f, .32f));
            Text(new Rect(392, 165, 500, 44), "A CRIPTA RECLAMOU SUA ALMA", headingStyle, new Color(1, .4f, .4f));
            Text(new Rect(392, 232, 490, 80), "Andar alcançado: " + State.Floor + "\nSalas purificadas: " + roomsCleared + "   •   Inimigos vencidos: " + enemiesDefeated + "\nSemente: " + State.Seed, bodyStyle, Color.white);
            if (Button(new Rect(392, 342, 496, 54), "NOVA INCURSÃO  /  NOVA SEMENTE", Orange)) BeginRun(System.Guid.NewGuid().GetHashCode());
            if (Button(new Rect(392, 415, 496, 44), "REPETIR ESTA SEMENTE", Mint)) BeginRun(State.Seed);
            if (Button(new Rect(392, 479, 496, 44), "VOLTAR AO MENU", new Color(.65f, .58f, .73f))) ReturnToMenu();
        }

        void WorldTag(Vector2 world, string label, Color color, float rise = 1.3f, float width = 248)
        {
            Vector3 screen = view.WorldToScreenPoint(world + Vector2.up * rise);
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            float x = (screen.x - (Screen.width - 1280 * scale) * .5f) / scale;
            float y = (Screen.height - screen.y - (Screen.height - 720 * scale) * .5f) / scale;
            x = Mathf.Clamp(x, width * .5f + 8, 1272 - width * .5f);
            y = Mathf.Clamp(y, 68, 660);
            Fill(new Rect(x - width * .5f, y - 2, width, 29), Ink);
            var old = smallStyle.alignment;
            smallStyle.alignment = TextAnchor.MiddleCenter;
            Text(new Rect(x - width * .5f + 3, y, width - 6, 26), label, smallStyle, color);
            smallStyle.alignment = old;
        }

        void DrawIcon(Rect rect, string id)
        {
            Sprite sprite = art.Get(id);
            if (!sprite) return;
            GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, true);
        }

        void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, panelTexture);
            GUI.color = previous;
        }

        void Panel(Rect rect, Color border)
        {
            Fill(rect, border);
            Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), Ink);
        }

        static void Text(Rect rect, string text, GUIStyle style, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Label(rect, text, style);
            GUI.color = previous;
        }

        bool Button(Rect rect, string label, Color background)
        {
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = background;
            bool clicked = GUI.Button(rect, label, buttonStyle);
            GUI.backgroundColor = old;
            return clicked;
        }
    }
}
