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
        bool stylesReady, shopWeapons;
        static readonly string[] CharacterIcons = { "player", "medium", "reaper", "alchemist" };
        static readonly string[] CharacterNames = { "CAÇADOR DA AURORA", "MÉDIUM DO VÉU", "CEIFADOR ERRANTE", "ALQUIMISTA DE CINZAS" };
        static readonly string[] CharacterPowers = {
            "Q · Pulso da Aurora\nDissipa balas próximas e concede proteção breve. Recarga: 9s.",
            "Q · Lamento Espectral\nNove projéteis em leque com dano triplo. Recarga: 7s.",
            "Q · Colheita Lunar\nDoze tiros ao redor e proteção breve. Recarga: 8s.",
            "Q · Elixir Vital\nRecupera até 2 pontos de vida. Recarga: 18s."
        };

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
                if (InShop) DrawShop();
                else
                {
                    // Passive atlas below the text HUD; it never captures input.
                    if (!Paused && !State.IsDead)
                        DrawMap(showMap ? new Rect(355, 130, 570, 470) : new Rect(1040, 478, 216, 200), showMap);
                    DrawHud();
                }
                if (State.IsDead) DrawDeath();
                else if (Paused) DrawPause();
                if (flash > 0) Fill(new Rect(0, 0, 1280, 720), new Color(.8f, .1f, .2f, flash * 1.4f));
            }
            if (noticeTime > 0 && !Paused && !InShop)
            {
                Color tint = Mint;
                tint.a = Mathf.Clamp01(noticeTime / .65f);
                HudText(new Rect(24, 638, 680, 54), notice, smallStyle, tint);
            }
            GUI.matrix = previous;
            GUI.color = color;
        }

        void DrawMenu()
        {
            Fill(new Rect(0, 0, 1280, 720), new Color(.02f, .012f, .045f, .7f));
            Fill(new Rect(60, 86, 5, 526), Orange);
            Text(new Rect(86, 82, 480, 25), "PÓLVORA, RELÍQUIAS E ASSOMBRAÇÕES", smallStyle, Orange);
            Text(new Rect(82, 129, 500, 72), "ECOS DA", titleStyle, new Color(.95f, .92f, .85f));
            Text(new Rect(82, 187, 500, 80), "MEIA-NOITE", titleStyle, Orange);
            Text(new Rect(86, 291, 440, 78), "Escolha uma alma. Atravesse os santuários.\nCada guardião deixa a noite mais profunda.", bodyStyle, Muted);
            Text(new Rect(86, 399, 440, 24), "SEMENTE  /  vazio = aleatória", smallStyle, Muted);
            seedText = GUI.TextField(new Rect(87, 433, 435, 44), seedText, 11, fieldStyle);
            if (Button(new Rect(87, 495, 435, 56), "INICIAR INCURSÃO     →", Orange)) StartFromMenu();
            Text(new Rect(87, 571, 440, 32), "ENTER para começar", smallStyle, Muted);
            Text(new Rect(87, 623, 455, 52), "WASD mover  •  Mouse mirar e atirar\nQ poder  •  1–4 armas  •  Espaço esquivar", smallStyle, Color.white);

            Panel(new Rect(598, 65, 604, 560), new Color(.35f, .25f, .45f));
            Text(new Rect(624, 88, 550, 36), "ESCOLHA SUA ALMA", headingStyle, Mint);
            for (int i = 0; i < CharacterNames.Length; i++)
            {
                bool chosen = (int)selectedCharacter == i;
                float x = 616 + (i % 2) * 287;
                float y = 142 + (i / 2) * 231;
                Panel(new Rect(x, y, 276, 218), chosen ? Mint : new Color(.27f, .23f, .35f));
                Text(new Rect(x + 14, y + 10, 251, 26), CharacterNames[i], smallStyle, chosen ? Mint : Orange);
                DrawIcon(new Rect(x + 14, y + 48, 65, 95), CharacterIcons[i]);
                Text(new Rect(x + 93, y + 45, 169, 121), CharacterPowers[i], smallStyle, Color.white);
                if (Button(new Rect(x + 14, y + 176, 248, 29), chosen ? "SELECIONADO" : "SELECIONAR", chosen ? Mint : Muted))
                    selectedCharacter = (CharacterKind)i;
            }
            Text(new Rect(617, 646, 583, 36), "Quatro caminhos. Um só destino: o fundo da cripta.", smallStyle, Muted);
        }

        void DrawHud()
        {
            // Compact corner information leaves the close camera's play area clear.
            for (int i = 0; i < State.MaxHealth; i++)
            {
                Color previous = GUI.color;
                GUI.color = i < State.Health ? Color.white : new Color(.35f, .29f, .38f, .65f);
                DrawIcon(new Rect(24 + (i % 10) * 22, i < 10 ? 18 : 43, 19, 21), "heart");
                GUI.color = previous;
            }
            float detailsY = State.MaxHealth > 10 ? 73 : 49;
            HudText(new Rect(24, detailsY, 290, 25), State.Coins + " ALMAS", smallStyle, Orange);
            HudText(new Rect(24, detailsY + 27, 330, 25), RunState.WeaponName(State.Weapon), smallStyle, Mint);
            HudText(new Rect(24, detailsY + 54, 330, 43), "Q · " + State.PowerName
                + (State.PowerCooldown <= 0 ? " · PRONTO" : " · " + Mathf.CeilToInt(State.PowerCooldown) + "s"), smallStyle,
                State.PowerCooldown <= 0 ? Mint : Muted);
            HudText(new Rect(1010, 18, 246, 25), "ANDAR " + State.Floor, hudStyle, Orange);
            HudText(new Rect(1010, 47, 246, 25), rollReady <= 0 ? "ESQUIVA PRONTA" : "ESQUIVA · " + rollReady.ToString("0.0") + "s",
                smallStyle, rollReady <= 0 ? Mint : Muted);

            Enemy boss = null;
            foreach (var enemy in enemies) if (enemy.Kind == 4) { boss = enemy; break; }
            if (boss != null)
            {
                HudText(new Rect(460, 18, 470, 28), BossNames[BossVariant], hudStyle, Orange);
                HudText(new Rect(460, 47, 470, 25), boss.Health + " / " + boss.MaxHealth + " VIDA", smallStyle, Muted);
            }
            else if (roomBannerTime > 0 && !Paused && !State.IsDead)
            {
                Color tint = Color.white;
                tint.a = Mathf.Clamp01(roomBannerTime);
                HudText(new Rect(460, 18, 470, 28), RoomThemeName, hudStyle, tint);
                tint = enemies.Count > 0 ? Orange : Mint;
                tint.a = Mathf.Clamp01(roomBannerTime);
                HudText(new Rect(460, 47, 470, 25), enemies.Count > 0 ? enemies.Count + " AMEAÇAS" : "SALA PURIFICADA", smallStyle, tint);
            }
            DrawDoorLabels();
            DrawExitGuide();
            if (CurrentRoom.Kind == RoomKind.Boss && CurrentRoom.Cleared && !Paused && !State.IsDead)
                WorldTag(new Vector2(0, 1.4f), "[E] MERCADO DAS ALMAS", Mint);
        }

        // Keep navigation on screen when the closer camera cannot show the doors.
        // This is a direction cue, not an interactive panel or a pathfinding route.
        void DrawExitGuide()
        {
            if (Paused || !TryNearestExit(out Vector2 destination)) return;
            Vector2 from = WorldToHudPoint(playerPosition + Vector2.up * .35f);
            Vector2 target = WorldToHudPoint(destination + Vector2.up * .35f);
            Vector2 direction = target - from;
            if (direction.sqrMagnitude < 1) return;
            direction.Normalize();
            Vector2 center = from + direction * 76;
            center.x = Mathf.Clamp(center.x, 60, 1220);
            center.y = Mathf.Clamp(center.y, 150, 580);
            if (center.x < 360) center.y = Mathf.Max(center.y, 200);
            float pulse = 1 + .09f * Mathf.Sin(simulationTime * 5);
            float size = 38 * pulse;
            Matrix4x4 previous = GUI.matrix;
            Color previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, center);
            GUI.color = Color.white;
            DrawIcon(new Rect(center.x - size * .5f, center.y - size * .5f, size, size), "exit-arrow");
            GUI.matrix = previous;
            GUI.color = previousColor;
            bool portalExit = CurrentRoom.Kind == RoomKind.Boss && State.BossDefeated;
            Rect caption = new Rect(Mathf.Clamp(center.x - 110, 24, 1036), center.y + 26, 220, 27);
            HudText(caption, portalExit ? "PORTAL DO MERCADO" : "SAÍDA MAIS PRÓXIMA", smallStyle, Mint);
        }

        Vector2 WorldToHudPoint(Vector2 position)
        {
            Vector3 screen = view.WorldToScreenPoint(position);
            float scale = Mathf.Max(.01f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            return new Vector2((screen.x - (Screen.width - 1280 * scale) * .5f) / scale,
                (Screen.height - screen.y - (Screen.height - 720 * scale) * .5f) / scale);
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

        // Only modal screens capture input. HUD labels and both atlas sizes are passive.
        bool PointerOverInterface() => !Playing || Paused || InShop || State.IsDead;

        void DrawShop()
        {
            // A dedicated screen replaces combat HUD, minimap and world labels.
            Fill(new Rect(0, 0, 1280, 720), new Color(.025f, .016f, .045f, .86f));
            Panel(new Rect(66, 54, 1148, 608), new Color(.44f, .36f, .3f));
            Text(new Rect(96, 76, 730, 42), "MERCADO DAS ALMAS", headingStyle, Orange);
            Text(new Rect(97, 119, 760, 26), "Um refúgio antes da próxima descida.", smallStyle, Muted);
            DrawIcon(new Rect(1018, 82, 30, 32), "coin");
            Text(new Rect(1062, 82, 130, 34), State.Coins.ToString() + " ALMAS", hudStyle, Orange);
            Text(new Rect(894, 126, 296, 28), "VIDA  " + State.Health + " / " + State.MaxHealth, smallStyle, Mint);
            bool enabled = GUI.enabled;
            GUI.enabled = !Paused && !State.IsDead;
            if (Button(new Rect(96, 164, 258, 42), "MELHORIAS", shopWeapons ? Muted : Mint)) shopWeapons = false;
            if (Button(new Rect(366, 164, 258, 42), "ARMAS", shopWeapons ? Mint : Muted)) shopWeapons = true;
            Text(new Rect(688, 175, 500, 27), "EM MÃOS: " + RunState.WeaponName(State.Weapon), smallStyle, Muted);

            string[] names = { "TÔNICO ESPECTRAL", "PÓLVORA MALDITA", "GATILHO FANTASMA", "CORAÇÃO DE CERA", "BACAMARTE", "REPETIDORA", "CANHÃO DAS ALMAS" };
            string[] descriptions = { "Recupera até 3 pontos de vida.", "+1 de dano por projétil.", "Intervalo entre tiros 15% menor.", "+2 de vida máxima e recupera 2.", "Cinco projéteis em leque.\nDisparo pesado. Tecla 2.", "Uma chuva de balas rápidas.\nTecla 3.", "Projétil pesado com dano ×5.\nTecla 4." };
            string[] icons = { "heart", "weapon", "muzzle", "heart", "scattergun", "repeater", "cannon" };
            int first = shopWeapons ? 4 : 0;
            int count = shopWeapons ? 3 : 4;
            float cardWidth = shopWeapons ? 346 : 258;
            float stride = cardWidth + 18;
            float start = 96;
            for (int column = 0; column < count; column++)
            {
                int i = first + column;
                float x = start + column * stride;
                ShopItem item = (ShopItem)i;
                bool owned = i >= 4 && State.Owns((WeaponKind)(i - 3));
                bool equipped = owned && State.Weapon == (WeaponKind)(i - 3);
                bool sold = State.Sold(item) && !owned;
                bool affordable = State.Coins >= State.Price(item);
                Panel(new Rect(x, 228, cardWidth, 294), equipped ? Mint : new Color(.3f, .25f, .33f));
                DrawIcon(new Rect(x + cardWidth * .5f - 42, 246, 84, 67), icons[i]);
                Text(new Rect(x + 16, 331, cardWidth - 32, 27), names[i], smallStyle, Orange);
                Text(new Rect(x + 16, 369, cardWidth - 32, 62), descriptions[i], smallStyle, Color.white);
                bool canBuy = GUI.enabled;
                GUI.enabled = canBuy && !sold && !equipped && (owned || affordable);
                string action = equipped ? "EQUIPADA" : owned ? "EQUIPAR" : sold ? "ESGOTADO" : State.Price(item) + " ALMAS · COMPRAR";
                if (Button(new Rect(x + 16, 466, cardWidth - 32, 38), action, owned ? Mint : affordable ? Orange : Muted))
                {
                    if (owned) State.Equip((WeaponKind)(i - 3));
                    else Buy(item);
                }
                GUI.enabled = canBuy;
            }
            string status = noticeTime > 0 ? notice : shopWeapons ? "Armas adquiridas acompanham você até o fim da incursão." : "Cada melhoria pode ser comprada uma vez nesta visita.";
            Text(new Rect(97, 543, 1078, 42), status, smallStyle, Mint);
            Text(new Rect(97, 609, 660, 31), "Escolha o que levar. A próxima fase espera.", smallStyle, Muted);
            if (Button(new Rect(818, 597, 367, 43), "DESCER AO PRÓXIMO ANDAR     →", Orange)) LeaveShop();
            GUI.enabled = enabled;
        }

        void DrawMap(Rect rect, bool expanded)
        {
            float opacity = expanded ? .30f : .16f;
            Color theme = FloorColors[ThemeVariant];
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var room in Floor.Rooms)
            {
                minX = Mathf.Min(minX, room.Position.X); maxX = Mathf.Max(maxX, room.Position.X);
                minY = Mathf.Min(minY, room.Position.Y); maxY = Mathf.Max(maxY, room.Position.Y);
            }
            float spacing = Mathf.Min((rect.width - 30) / (maxX - minX + 1), (rect.height - 30) / (maxY - minY + 1));
            spacing = Mathf.Min(spacing, expanded ? 72 : 36);
            Vector2 center = new Vector2(rect.center.x - (minX + maxX) * .5f * spacing,
                rect.center.y + (minY + maxY) * .5f * spacing);
            float corridor = expanded ? 5 : 3;
            foreach (var room in Floor.Rooms)
            {
                Vector2 point = center + new Vector2(room.Position.X, -room.Position.Y) * spacing;
                foreach (int id in room.Neighbors)
                {
                    if (id <= room.Id) continue;
                    var next = Floor.GetRoom(id);
                    Vector2 end = center + new Vector2(next.Position.X, -next.Position.Y) * spacing;
                    Color route = theme; route.a = opacity;
                    Fill(new Rect(Mathf.Min(point.x, end.x) - corridor * .5f, Mathf.Min(point.y, end.y) - corridor * .5f,
                        Mathf.Abs(point.x - end.x) + corridor, Mathf.Abs(point.y - end.y) + corridor), route);
                }
            }
            foreach (var room in Floor.Rooms)
            {
                Vector2 point = center + new Vector2(room.Position.X, -room.Position.Y) * spacing;
                float width = spacing * .81f, height = width * .67f;
                Rect tile = new Rect(point.x - width * .5f, point.y - height * .5f, width, height);
                bool known = visited.Contains(room.Id) || room.Kind == RoomKind.Boss;
                Color tint = room == CurrentRoom ? Color.white : room.Cleared ? Mint : known ? theme : theme * .58f;
                MapIcon(tile, "map-theme" + ThemeVariant, tint, opacity);
                if (room == CurrentRoom)
                {
                    Color marker = Mint; marker.a = opacity + .10f;
                    float mark = expanded ? 7 : 4;
                    Fill(new Rect(tile.x - 3, tile.y - 3, mark, 2), marker);
                    Fill(new Rect(tile.x - 3, tile.y - 3, 2, mark), marker);
                    Fill(new Rect(tile.xMax - mark + 3, tile.yMax + 1, mark, 2), marker);
                    Fill(new Rect(tile.xMax + 1, tile.yMax - mark + 3, 2, mark), marker);
                }
                if (room.Kind == RoomKind.Boss)
                    MapIcon(new Rect(point.x - width * .23f, point.y - height * .42f, width * .46f, height * .85f),
                        EnemySprite(4), Color.white, opacity);
                else if (room.Kind == RoomKind.Entrance)
                    MapIcon(new Rect(point.x - height * .32f, point.y - height * .32f, height * .64f, height * .64f), "portal", Mint, opacity);
                else if (room.Cleared)
                    MapIcon(new Rect(tile.xMax - height * .35f, tile.y - 1, height * .4f, height * .4f), "spark", Mint, opacity);
            }
        }

        void MapIcon(Rect rect, string id, Color tint, float opacity)
        {
            Color previous = GUI.color;
            GUI.color = new Color(tint.r, tint.g, tint.b, opacity);
            DrawIcon(rect, id);
            GUI.color = previous;
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
            var old = smallStyle.alignment;
            smallStyle.alignment = TextAnchor.MiddleCenter;
            HudText(new Rect(x - width * .5f + 3, y, width - 6, 26), label, smallStyle, color);
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

        static void HudText(Rect rect, string text, GUIStyle style, Color color)
        {
            // Glyph shadow provides contrast on bright tiles without a rectangle.
            Text(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style, new Color(0, 0, 0, color.a * .9f));
            Text(rect, text, style, color);
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
