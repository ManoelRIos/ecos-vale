using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ecos.Haunt.Fps
{
    public sealed partial class HauntFpsGame
    {
        static readonly Color Ivory = new Color(.94f, .88f, .7f), Gold = new Color(1, .68f, .28f);

        void OnGUI()
        {
            if (disposed) return;
            float scale = Mathf.Max(.35f, Mathf.Min(Screen.width / 960f, Screen.height / 540f));
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float width = Screen.width / scale, height = Screen.height / scale;
            try
            {
                DrawVignette(width, height);
                if (!string.IsNullOrEmpty(failure))
                {
                    Fill(new Rect(0, 0, width, height), new Color(.03f, .02f, .06f, .93f));
                    Text(new Rect(60, height * .4f, width - 120, 80), failure, 2, Gold, TextAnchor.MiddleCenter); return;
                }
                if (state == null) return;
                if (mode == ScreenMode.Playing)
                {
                    DrawHearts(20, 16);
                    DrawSouls(20, 44);
                    DrawMinimap(width);
                    Text(new Rect(width - 420, 134, 396, 16), "FASE " + state.Floor + "/5  " + HauntFpsCampaign.ThemeName(state.Floor), 1, Ivory, TextAnchor.UpperRight);
                    if (encounterRoom != null && !bossSpawned)
                        Text(new Rect(width - 250, 152, 226, 18), "ONDA " + (wave + 1) + "/" + encounterWaves, 1, Gold, TextAnchor.UpperRight);
                    int capacity = WeaponAmmo.Stats(state.Weapon).MagazineSize;
                    Text(new Rect(width - 290, height - 60, 266, 24), state.Magazine + "/" + capacity + "  RESERVA INF.", 2, Ivory, TextAnchor.UpperRight);
                    Text(new Rect(width - 300, height - 32, 276, 18), RunState.WeaponName(state.Weapon), 1, CharacterColor(state.Character), TextAnchor.UpperRight);
                    Text(new Rect(24, height - 60, 360, 20), "Q / L1 " + (state.PowerCooldown > 0 ? "PODER " + Mathf.CeilToInt(state.PowerCooldown) + "S" : state.PowerName), 1, CharacterColor(state.Character));
                    Text(new Rect(24, height - 32, 390, 20), "TAB / R1 " + (player.IsTopDown ? "VISAO 2D" : "PRIMEIRA PESSOA") + "   ESC PAUSA", 1, Ivory);
                    if (state.IsReloading)
                    {
                        Text(new Rect(width * .5f - 150, height * .64f, 300, 22), "RECARREGANDO", 2, Ivory, TextAnchor.MiddleCenter);
                        Fill(new Rect(width * .5f - 70, height * .64f + 24, 140, 3), new Color(.15f, .13f, .18f));
                        Fill(new Rect(width * .5f - 70, height * .64f + 24, 140 * state.ReloadProgress, 3), Gold);
                    }
                    else if (state.Magazine == 0)
                        Text(new Rect(width * .5f - 180, height * .64f, 360, 20), "R / TRIANGULO - RECARREGAR", 1, Gold, TextAnchor.MiddleCenter);
                    float cx = width * .5f, cy = height * .5f;
                    if (player.IsTopDown && player.Perspective)
                    {
                        Vector2 aim = player.Perspective.ScreenAimPosition;
                        cx = Mathf.Clamp(aim.x / scale, 8, width - 8); cy = Mathf.Clamp((Screen.height - aim.y) / scale, 8, height - 8);
                    }
                    if (!player.IsPerspectiveTransition)
                    {
                        Color aim = hitMarker > 0 ? Gold : new Color(.8f, .92f, .87f, .9f);
                        Fill(new Rect(cx - 7, cy - 1, 4, 2), aim); Fill(new Rect(cx + 3, cy - 1, 4, 2), aim);
                        Fill(new Rect(cx - 1, cy - 7, 2, 4), aim); Fill(new Rect(cx - 1, cy + 3, 2, 4), aim);
                    }
                    else
                    {
                        float flash = Mathf.Sin(player.Perspective.TransitionProgress * Mathf.PI) * .3f;
                        Fill(new Rect(0, 0, width, height), new Color(.32f, .16f, .65f, flash));
                    }
                    if (noticeRemaining > 0 && !(boss && boss.IntroRemaining > 0))
                        Text(new Rect(80, 70, width - 160, 48), notice, 2, Ivory, TextAnchor.MiddleCenter);
                    if (exitReady && world != null && Vector3.Distance(PlayerPosition, world.ExitPosition) < 2.8f)
                        Text(new Rect(width * .5f - 260, height - 115, 520, 28), "E / QUADRADO - " + (state.Floor < 5 && practiceBoss < 0 ? "PROXIMA FASE" : "ATRAVESSAR"), 2, Gold, TextAnchor.MiddleCenter);
                    else
                    {
                        string prompt = InteractionPrompt();
                        if (prompt != null) Text(new Rect(width * .5f - 300, height - 115, 600, 28), prompt, 2, Gold, TextAnchor.MiddleCenter);
                    }
                    DrawBossHud(width, height);
                    if (!player.IsTopDown) DrawIncomingWarnings(width, height);
                    if (!paused && !focusLost) return;
                }
                DrawMenu(width, height);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }

        void DrawBossHud(float width, float height)
        {
            if (!boss || boss.IsDead) return;
            Color accent = CharacterColor((CharacterKind)(boss.Archetype % 4));
            if (boss.IntroRemaining > 0 || InBossIntro)
            {
                // Letterbox bars slide in, then the name card sweeps across the screen.
                float elapsed = BossIntroDuration - bossIntro;
                float bars = Mathf.Clamp01(elapsed / .35f) * Mathf.Clamp01(bossIntro / .3f + (bossIntro > 0 ? 0 : 1));
                Fill(new Rect(0, 0, width, 64 * bars), Color.black); Fill(new Rect(0, height - 96 * bars, width, 96 * bars), Color.black);
                float sweep = Mathf.Clamp01((elapsed - .4f) / .45f);
                float slide = (1 - sweep) * (1 - sweep) * width * .6f;
                Fill(new Rect(-slide, height * .56f, width * .62f, 54), new Color(0, 0, 0, .78f));
                Fill(new Rect(-slide, height * .56f + 54, width * .62f, 3), accent);
                Text(new Rect(28 - slide, height * .56f + 6, width * .6f, 30), boss.DisplayName, 3, accent);
                Text(new Rect(30 - slide, height * .56f + 36, width * .6f, 16), "GUARDIAO DOS ECOS DA MEIA-NOITE", 1, Ivory);
                return;
            }
            float left = width * .5f - 220, y = height - 79;
            Text(new Rect(left, y - 18, 440, 16), boss.DisplayName, 1, Ivory, TextAnchor.MiddleCenter);
            Fill(new Rect(left - 3, y - 3, 446, 12), new Color(.055f, .03f, .08f, .95f));
            Fill(new Rect(left, y, 440 * Mathf.Clamp01(boss.Health / (float)boss.MaxHealth), 6), accent);
            for (int i = 1; i < 10; i++) Fill(new Rect(left + i * 44, y, 1, 6), new Color(.05f, .03f, .07f, .6f));
            Fill(new Rect(left - 7, y - 5, 4, 16), accent); Fill(new Rect(left + 443, y - 5, 4, 16), accent);
        }

        // ------------------------------------------------------------------ dungeon HUD

        Texture2D heartFull, heartHalf, heartEmpty, soulIcon;

        static Texture2D IconTexture(string[] rows, Color[] colors)
        {
            int h = rows.Length, w = rows[0].Length;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                char c = rows[h - 1 - y][x];
                texture.SetPixel(x, y, c == '.' ? Color.clear : colors[c - '0']);
            }
            texture.Apply(false, true);
            return texture;
        }

        void EnsureIcons()
        {
            if (heartFull) return;
            string[] heart = { ".00.00.", "0110110", "0122110", "0111110", ".01110.", "..010..", "...0..." };
            string[] half = { ".00.00.", "0110330", "0122330", "0111330", ".01330.", "..030..", "...0..." };
            string[] empty = { ".00.00.", "0330330", "0333330", "0333330", ".03330.", "..030..", "...0..." };
            string[] soul = { "..00..", ".0110.", "011210", "011110", ".0110.", "..00.." };
            var heartColors = new[] { new Color(.08f, .03f, .05f), new Color(.86f, .14f, .2f), new Color(1, .7f, .7f), new Color(.24f, .12f, .16f) };
            heartFull = IconTexture(heart, heartColors); heartHalf = IconTexture(half, heartColors); heartEmpty = IconTexture(empty, heartColors);
            soulIcon = IconTexture(soul, new[] { new Color(.06f, .05f, .1f), new Color(.45f, .95f, .85f), new Color(1, 1, 1) });
            textures.Add(heartFull); textures.Add(heartHalf); textures.Add(heartEmpty); textures.Add(soulIcon);
        }

        /// <summary>Two health points per heart, drawn as chunky pixel hearts.</summary>
        void DrawHearts(float x, float y)
        {
            EnsureIcons();
            int hearts = (state.MaxHealth + 1) / 2;
            for (int i = 0; i < hearts; i++)
            {
                int points = state.Health - i * 2;
                Texture2D icon = points >= 2 ? heartFull : points == 1 ? heartHalf : heartEmpty;
                GUI.DrawTexture(new Rect(x + i * 24, y, 21, 21), icon);
            }
        }

        void DrawSouls(float x, float y)
        {
            EnsureIcons();
            GUI.DrawTexture(new Rect(x, y, 15, 15), soulIcon);
            Text(new Rect(x + 21, y + 1, 160, 16), state.Coins.ToString(), 2, new Color(.6f, 1, .9f));
        }

        /// <summary>Rooms seen so far, linked like the floor's tree; the boss, merchant and
        /// treasure rooms are marked once they are visited or next to a visited room.</summary>
        void DrawMinimap(float width)
        {
            if (world == null || world.Rooms.Count == 0) return;
            const float cell = 15, gap = 4;
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            foreach (var room in world.Rooms)
            {
                minX = Mathf.Min(minX, room.Cell.X); maxX = Mathf.Max(maxX, room.Cell.X);
                minY = Mathf.Min(minY, room.Cell.Y); maxY = Mathf.Max(maxY, room.Cell.Y);
            }
            float mapWidth = (maxX - minX + 1) * (cell + gap), mapHeight = (maxY - minY + 1) * (cell + gap);
            float scale = Mathf.Min(1, 150 / Mathf.Max(mapWidth, mapHeight));
            float c = cell * scale, step = (cell + gap) * scale;
            float left = width - 24 - (maxX - minX + 1) * step, top = 18;
            Fill(new Rect(left - 6, top - 6, (maxX - minX + 1) * step + 8, (maxY - minY + 1) * step + 8), new Color(0, 0, 0, .45f));
            foreach (var room in world.Rooms)
            {
                bool known = room.Visited;
                if (!known) foreach (int id in room.Neighbors) if (world.GetRoom(id).Visited) { known = true; break; }
                if (!known) continue;
                float x = left + (room.Cell.X - minX) * step, y = top + (maxY - room.Cell.Y) * step;
                foreach (int id in room.Neighbors)
                {
                    var other = world.GetRoom(id);
                    if (other.Id < room.Id || !(room.Visited || other.Visited)) continue;
                    float ox = left + (other.Cell.X - minX) * step, oy = top + (maxY - other.Cell.Y) * step;
                    Fill(new Rect(Mathf.Min(x, ox) + c * .4f, Mathf.Min(y, oy) + c * .4f, Mathf.Abs(ox - x) + c * .2f, Mathf.Abs(oy - y) + c * .2f), new Color(.55f, .5f, .45f, .8f));
                }
                Color fill = room == currentRoom ? Gold : room.Visited ? new Color(.62f, .56f, .52f) : new Color(.22f, .2f, .22f);
                Fill(new Rect(x, y, c, c), new Color(.05f, .03f, .05f));
                Fill(new Rect(x + 1, y + 1, c - 2, c - 2), fill);
                Color mark = room.Role == FpsRoomRole.Boss ? new Color(.95f, .2f, .25f) : room.Role == FpsRoomRole.Shop ? new Color(.45f, .95f, .85f)
                    : room.Role == FpsRoomRole.Treasure ? new Color(1, .82f, .3f) : Color.clear;
                if (mark.a > 0) Fill(new Rect(x + c * .3f, y + c * .3f, c * .4f, c * .4f), mark);
            }
        }

        void DrawIncomingWarnings(float width, float height)
        {
            if (!player || player.IsPerspectiveTransition) return;
            int count = 0;
            foreach (var shot in projectiles)
            {
                Vector3 delta = shot.Position - (PlayerPosition + Vector3.up);
                float distance = delta.magnitude;
                if (distance > 8 || distance < .1f || Vector3.Dot(shot.Direction, -delta.normalized) < .35f) continue;
                float ahead = Vector3.Dot(player.transform.forward, delta.normalized);
                if (ahead > .55f) continue;
                float side = Vector3.Dot(player.transform.right, delta.normalized);
                float x = side < -.15f ? 38 : side > .15f ? width - 62 : width * .5f - 12;
                float y = Mathf.Abs(side) > .15f ? height * .5f : height - 130;
                Text(new Rect(x, y, 24, 24), "!", 3, new Color(1, .36f, .2f), TextAnchor.MiddleCenter);
                if (++count >= 3) break;
            }
        }

        void DrawMenu(float width, float height)
        {
            Fill(new Rect(0, 0, width, height), new Color(.018f, .012f, .035f, .9f));
            float cx = width * .5f, top = Mathf.Max(20, height * .075f);
            Text(new Rect(30, top, width - 60, 34), "ECOS DA MEIA-NOITE", 4, Gold, TextAnchor.MiddleCenter);
            string title = mode == ScreenMode.Menu ? "DOIS OLHARES. UMA UNICA MASMORRA."
                : mode == ScreenMode.Dead ? "A MEIA-NOITE RECLAMOU SUA ALMA"
                : mode == ScreenMode.Victory ? "VOCE ATRAVESSOU A MEIA-NOITE" : "PAUSA";
            Text(new Rect(30, top + 45, width - 60, 32), title, 2, Ivory, TextAnchor.MiddleCenter);
            float y;
            if (mode == ScreenMode.Menu)
            {
                // Four souls as cards: each idles in its frame; the chosen one walks toward you.
                float cardHeight = Mathf.Clamp(height * .25f, 118, 230), cardWidth = Mathf.Clamp(cardHeight * .82f, 100, 186);
                float gap = 14, start = cx - (cardWidth * 4 + gap * 3) * .5f, cardTop = top + 84;
                int zoom = Mathf.Max(2, Mathf.FloorToInt((cardHeight - 34) / 40));
                for (int i = 0; i < 4; i++)
                {
                    CharacterKind character = (CharacterKind)i;
                    var card = new Rect(start + i * (cardWidth + gap), cardTop, cardWidth, cardHeight);
                    bool chosen = selectedCharacter == character, hover = card.Contains(Event.current.mousePosition);
                    Fill(card, chosen ? new Color(.2f, .13f, .2f, .96f) : hover ? new Color(.15f, .1f, .16f, .95f) : new Color(.08f, .06f, .1f, .92f));
                    Color edge = chosen ? Gold : hover ? new Color(.62f, .5f, .4f) : new Color(.3f, .22f, .28f);
                    Fill(new Rect(card.x, card.y, card.width, 2), edge); Fill(new Rect(card.x, card.yMax - 2, card.width, 2), edge);
                    Fill(new Rect(card.x, card.y, 2, card.height), edge); Fill(new Rect(card.xMax - 2, card.y, 2, card.height), edge);
                    // Floor glow under the hero.
                    Fill(new Rect(card.x + card.width * .2f, card.yMax - 40, card.width * .6f, 6), new Color(0, 0, 0, .35f));
                    DrawHeroPortrait(new Rect(card.x, card.y + 6, card.width, card.height - 34), character, chosen, zoom);
                    Text(new Rect(card.x, card.yMax - 26, card.width, 18), CharacterName(character), 1, chosen ? Gold : Ivory, TextAnchor.MiddleCenter);
                    if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0) { selectedCharacter = character; Event.current.Use(); }
                }
                float info = cardTop + cardHeight + 10;
                Text(new Rect(cx - 400, info, 800, 18), RunState.WeaponName(RunState.StartingWeaponFor(selectedCharacter)) + " | "
                    + (selectedCharacter == CharacterKind.Hunter ? "TIRO PRECISO / PULSO" : selectedCharacter == CharacterKind.Medium ? "LEQUE ARCANO / VEU" : selectedCharacter == CharacterKind.Reaper ? "OSSOS PERFURANTES / COLHEITA" : "CHOQUE EM CADEIA / ELIXIR"), 1, CharacterColor(selectedCharacter), TextAnchor.MiddleCenter);
                Text(new Rect(cx - 400, info + 20, 800, 16), CharacterLore(selectedCharacter), 1, new Color(.72f, .68f, .74f), TextAnchor.MiddleCenter);
                float modeTop = info + 46;
                if (Button(new Rect(cx - 350, modeTop, 700, 28), practiceBoss < 0 ? "MODO: CAMPANHA - CINCO FASES" : "TREINO: " + HauntFpsBoss.NameFor(practiceBoss), 1))
                    practiceBoss = practiceBoss < 6 ? practiceBoss + 1 : -1;
                float next = modeTop + 34;
                if (practiceBoss >= 0)
                {
                    if (Button(new Rect(cx - 350, next, 700, 24), "CENARIO: " + HauntFpsCampaign.ThemeName(practiceTheme + 1), 1)) practiceTheme = (practiceTheme + 1) % 5;
                    next += 30;
                }
                Text(new Rect(20, height - 46, width - 40, 30), "TAB / R1 ALTERNA 2D E 3D | WASD MOVER | MOUSE MIRAR | ESQUERDO ATIRAR | R RECARGA | Q PODER\nDIREITO / CIRCULO ROLAR | E COMPRAR E ABRIR | ESPACO SALTO | M SOM | L1/R1 PERSONAGEM | TRIANGULO TREINO", 1, new Color(.6f, .62f, .68f), TextAnchor.MiddleCenter);
                y = next + 4;
            }
            else
            {
                Text(new Rect(cx - 370, top + 104, 740, 54), "FASE " + state.Floor + "/5 | INIMIGOS DERROTADOS " + kills
                    + "\nTAB / R1 ALTERNA A VISAO | Q / L1 PODER", 1, Ivory, TextAnchor.MiddleCenter);
                y = top + 183;
            }
            string action = mode == ScreenMode.Playing ? "CONTINUAR [ENTER / X]" : mode == ScreenMode.Menu ? "INICIAR [ENTER / X]" : "TENTAR NOVAMENTE [ENTER / X]";
            if (Button(new Rect(cx - 235, y, 470, 34), action) && !focusLost)
            {
                if (mode == ScreenMode.Playing) SetPaused(false);
                else BeginRun(mode == ScreenMode.Menu ? NewMenuSeed() : unchecked(seed + 1));
            }
            if (Button(new Rect(cx - 235, y + 43, 470, 28), sound && sound.SoundEnabled ? "SOM: LIGADO [M]" : "SOM: DESLIGADO [M]", 1))
                if (sound) sound.SetEnabled(!sound.SoundEnabled);
            if (mode != ScreenMode.Menu && Button(new Rect(cx - 235, y + 79, 470, 28), "VOLTAR A SELECAO", 1))
            { mode = ScreenMode.Menu; paused = false; UpdateSuspension(); }
            float legacyY = y + (mode == ScreenMode.Menu ? 79 : 115);
            if (Button(new Rect(cx - 235, legacyY, 470, 28), "ABRIR O ROGUELIKE 2D ORIGINAL", 1)) SceneManager.LoadScene("Haunt");
        }

        HauntPixelArt menuArt;

        static string CharacterLore(CharacterKind character)
        {
            switch (character)
            {
                case CharacterKind.Medium: return "OUVE OS MORTOS E RESPONDE COM LEQUES DE LUZ.";
                case CharacterKind.Reaper: return "COLHE ALMAS PERDIDAS COM OSSOS AFIADOS.";
                case CharacterKind.Alchemist: return "DESTILA RAIOS EM FRASCOS INSTAVEIS.";
                default: return "RASTREIA ECOS NA ESCURIDAO COM A CARABINA DA AURORA.";
            }
        }

        /// <summary>The soul drawn large with whole-pixel zoom: idling, or walking when chosen.</summary>
        void DrawHeroPortrait(Rect area, CharacterKind character, bool chosen, int zoom)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (menuArt == null) menuArt = new HauntPixelArt();
            string id = character == CharacterKind.Medium ? "medium" : character == CharacterKind.Reaper ? "reaper"
                : character == CharacterKind.Alchemist ? "alchemist" : "player";
            CharacterClip clip = chosen ? CharacterClip.Walk : CharacterClip.Idle;
            float rate = menuArt.CharacterFrameRate(id, clip, CharacterFacing.Front, CharacterWeaponPose.Empty);
            Sprite sprite = menuArt.CharacterFrame(id, clip, Mathf.FloorToInt(Time.unscaledTime * Mathf.Max(1, rate)), CharacterFacing.Front, CharacterWeaponPose.Empty);
            if (!sprite || !sprite.texture) return;
            Rect source = sprite.textureRect; Texture texture = sprite.texture;
            float w = source.width * zoom, h = source.height * zoom;
            // Feet on a fixed line near the bottom of the area, whatever the frame height.
            float footX = area.center.x, footY = area.yMax - 8;
            var target = new Rect(Mathf.Round(footX - sprite.pivot.x * zoom), Mathf.Round(footY - (source.height - sprite.pivot.y) * zoom), w, h);
            target.y = Mathf.Round(footY - h + sprite.pivot.y * zoom);
            GUI.DrawTextureWithTexCoords(target, texture,
                new Rect(source.x / texture.width, source.y / texture.height, source.width / texture.width, source.height / texture.height), true);
        }

        bool Button(Rect rect, string label, int size = 2, bool selected = false)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Fill(rect, hover || selected ? new Color(.23f, .16f, .24f, 1) : new Color(.1f, .075f, .13f, 1));
            Fill(new Rect(rect.x, rect.yMax - 2, rect.width, 2), hover || selected ? Gold : new Color(.34f, .24f, .32f));
            Text(rect, label, size, hover || selected ? Gold : Ivory, TextAnchor.MiddleCenter);
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || !hover) return false;
            Event.current.Use(); return true;
        }

        void DrawVignette(float width, float height)
        {
            for (int i = 0; i < 12; i++)
            {
                float inset = i * 5;
                Color tint = damageOverlay > 0 ? new Color(.45f, .015f, .04f, .035f + damageOverlay * .035f) : new Color(.01f, .005f, .025f, .035f);
                Fill(new Rect(inset, inset, width - inset * 2, 5), tint); Fill(new Rect(inset, height - inset - 5, width - inset * 2, 5), tint);
                Fill(new Rect(inset, inset + 5, 5, height - inset * 2 - 10), tint); Fill(new Rect(width - inset - 5, inset + 5, 5, height - inset * 2 - 10), tint);
            }
        }
        void Text(Rect rect, string text, int size, Color tint, TextAnchor anchor = TextAnchor.UpperLeft) => lettering.Draw(rect, text, size, tint, anchor);
        static void Fill(Rect rect, Color tint)
        { Color old = GUI.color; GUI.color = tint; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
    }
}
