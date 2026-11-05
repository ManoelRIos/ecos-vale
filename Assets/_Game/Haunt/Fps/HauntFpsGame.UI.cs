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
                    Text(new Rect(24, 20, 360, 22), CharacterName(state.Character) + "  VIDA " + state.Health + "/" + state.MaxHealth, 2, Ivory);
                    Text(new Rect(width - 420, 20, 396, 22), "FASE " + state.Floor + "/5  " + HauntFpsCampaign.ThemeName(state.Floor), 1, Ivory, TextAnchor.UpperRight);
                    Text(new Rect(width - 250, 44, 226, 22), bossSpawned ? "GUARDIAO" : "ONDA " + (wave + 1) + "/3", 2, Gold, TextAnchor.UpperRight);
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
            if (boss.IntroRemaining > 0)
            {
                Fill(new Rect(0, 0, width, 60), Color.black); Fill(new Rect(0, height - 90, width, 90), Color.black);
                Text(new Rect(30, height - 73, width - 60, 32), boss.DisplayName, 3, accent, TextAnchor.MiddleCenter);
                Text(new Rect(30, height - 35, width - 60, 20), "GUARDIAO DOS ECOS DA MEIA-NOITE", 1, Ivory, TextAnchor.MiddleCenter);
                return;
            }
            float left = width * .5f - 220, y = height - 79;
            Text(new Rect(left, y - 18, 440, 16), boss.DisplayName, 1, Ivory, TextAnchor.MiddleCenter);
            Fill(new Rect(left - 3, y - 3, 446, 12), new Color(.055f, .03f, .08f, .95f));
            Fill(new Rect(left, y, 440 * Mathf.Clamp01(boss.Health / (float)boss.MaxHealth), 6), accent);
            for (int i = 1; i < 10; i++) Fill(new Rect(left + i * 44, y, 1, 6), new Color(.05f, .03f, .07f, .6f));
            Fill(new Rect(left - 7, y - 5, 4, 16), accent); Fill(new Rect(left + 443, y - 5, 4, 16), accent);
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
                float start = cx - 360;
                for (int i = 0; i < 4; i++)
                {
                    CharacterKind character = (CharacterKind)i;
                    if (Button(new Rect(start + i * 182, top + 90, 174, 30), CharacterName(character), 1, selectedCharacter == character)) selectedCharacter = character;
                }
                Text(new Rect(cx - 360, top + 132, 720, 38), RunState.WeaponName(RunState.StartingWeaponFor(selectedCharacter)) + " | "
                    + (selectedCharacter == CharacterKind.Hunter ? "TIRO PRECISO / PULSO" : selectedCharacter == CharacterKind.Medium ? "LEQUE ARCANO / VEU" : selectedCharacter == CharacterKind.Reaper ? "OSSOS PERFURANTES / COLHEITA" : "CHOQUE EM CADEIA / ELIXIR"), 1, CharacterColor(selectedCharacter), TextAnchor.MiddleCenter);
                if (Button(new Rect(cx - 350, top + 177, 700, 30), practiceBoss < 0 ? "MODO: CAMPANHA - CINCO FASES" : "TREINO: " + HauntFpsBoss.NameFor(practiceBoss), 1))
                    practiceBoss = practiceBoss < 6 ? practiceBoss + 1 : -1;
                if (practiceBoss >= 0 && Button(new Rect(cx - 350, top + 214, 700, 26), "CENARIO: " + HauntFpsCampaign.ThemeName(practiceTheme + 1), 1)) practiceTheme = (practiceTheme + 1) % 5;
                Text(new Rect(cx - 400, top + 250, 800, 42), "TAB / R1 ALTERNA 2D E 3D | WASD / STICK MOVER | MOUSE / STICK MIRAR\nTIRO: ESQUERDO / R2 | RECARGA: R / TRIANGULO | PODER: Q / L1\nEVASAO: DIREITO / CIRCULO | SALTO: ESPACO / X | SOM: M", 1, Ivory, TextAnchor.MiddleCenter);
                Text(new Rect(cx - 400, top + 294, 800, 10), "MENU: L1/R1 PERSONAGEM | TRIANGULO TREINO | SHARE CENARIO", 1, new Color(.6f, .65f, .69f), TextAnchor.MiddleCenter);
                y = top + 305;
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
