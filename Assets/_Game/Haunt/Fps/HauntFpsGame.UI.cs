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
                    Text(new Rect(60, height * .4f, width - 120, 80), failure, 2, Gold, TextAnchor.MiddleCenter);
                    return;
                }
                if (state == null) return;
                if (mode == ScreenMode.Playing)
                {
                    Text(new Rect(24, 22, 300, 22), "VIDA " + state.Health + " / " + state.MaxHealth, 2, Ivory);
                    Text(new Rect(width - 264, 22, 240, 22), "ONDA " + (wave + 1) + " / 3", 2, Ivory, TextAnchor.UpperRight);
                    Text(new Rect(width - 290, height - 60, 266, 24), state.Magazine + " / 8  |  RESERVA INF.", 2, Ivory, TextAnchor.UpperRight);
                    Text(new Rect(width - 300, height - 32, 276, 18), "REVOLVER DO OSSUARIO", 1, Gold, TextAnchor.UpperRight);
                    if (state.IsReloading)
                    {
                        Text(new Rect(width * .5f - 150, height * .64f, 300, 22), "RECARREGANDO", 2, Ivory, TextAnchor.MiddleCenter);
                        Fill(new Rect(width * .5f - 70, height * .64f + 24, 140, 3), new Color(.15f, .13f, .18f));
                        Fill(new Rect(width * .5f - 70, height * .64f + 24, 140 * state.ReloadProgress, 3), Gold);
                    }
                    else if (state.Magazine == 0)
                        Text(new Rect(width * .5f - 180, height * .64f, 360, 20), "R / TRIANGULO - RECARREGAR", 1, Gold, TextAnchor.MiddleCenter);
                    Color aim = hitMarker > 0 ? Gold : new Color(.8f, .92f, .87f, .8f);
                    float cx = width * .5f, cy = height * .5f;
                    Fill(new Rect(cx - 7, cy - 1, 4, 2), aim); Fill(new Rect(cx + 3, cy - 1, 4, 2), aim);
                    Fill(new Rect(cx - 1, cy - 7, 2, 4), aim); Fill(new Rect(cx - 1, cy + 3, 2, 4), aim);
                    if (noticeRemaining > 0)
                        Text(new Rect(80, 70, width - 160, 48), notice, 2, Ivory, TextAnchor.MiddleCenter);
                    if (exitReady && world != null && Vector3.Distance(PlayerPosition, world.ExitPosition) < 2.8f)
                        Text(new Rect(cx - 220, height - 115, 440, 28), "E / QUADRADO - ATRAVESSAR", 2, Gold, TextAnchor.MiddleCenter);
                    Text(new Rect(24, height - 32, 340, 20), "ESC PAUSA   M SOM", 1, new Color(.6f, .65f, .69f));
                    if (!paused && !focusLost) return;
                }
                DrawMenu(width, height);
            }
            finally { GUI.matrix = matrix; GUI.color = color; }
        }

        void DrawMenu(float width, float height)
        {
            Fill(new Rect(0, 0, width, height), new Color(.018f, .012f, .035f, .86f));
            float cx = width * .5f, top = Mathf.Max(24, height * .13f);
            Text(new Rect(30, top, width - 60, 32), "ECOS DA MEIA-NOITE", 4, Gold, TextAnchor.MiddleCenter);
            string title = mode == ScreenMode.Menu ? "O OSSUARIO - PROTOTIPO EM PRIMEIRA PESSOA"
                : mode == ScreenMode.Dead ? "O OSSUARIO RECLAMOU SUA ALMA"
                : mode == ScreenMode.Victory ? "VOCE ATRAVESSOU A MEIA-NOITE" : "PAUSA";
            Text(new Rect(30, top + 51, width - 60, 36), title, 2, Ivory, TextAnchor.MiddleCenter);
            if (mode == ScreenMode.Menu)
            {
                Text(new Rect(cx - 360, top + 100, 720, 68),
                    "WASD MOVER | MOUSE MIRAR | ESQUERDO ATIRAR\nR RECARREGAR | DIREITO / ALT EVADIR | ESPACO PULAR\nCONTROLE: STICKS | R2 TIRO | TRIANGULO RECARGA | CIRCULO EVASAO", 1, Ivory, TextAnchor.MiddleCenter);
            }
            else if (mode == ScreenMode.Dead || mode == ScreenMode.Victory)
                Text(new Rect(cx - 300, top + 108, 600, 36), "INIMIGOS DERROTADOS " + kills + " / 12", 2, Ivory, TextAnchor.MiddleCenter);
            else Text(new Rect(cx - 340, top + 100, 680, 48), "USE AS COLUNAS COMO COBERTURA.\nA EVASAO ATRAVESSA ORBES SOMENTE NO INICIO.", 1, Ivory, TextAnchor.MiddleCenter);
            float y = top + 185;
            string action = mode == ScreenMode.Playing ? "CONTINUAR [ENTER / X]" : mode == ScreenMode.Menu ? "ENTRAR NO OSSUARIO [ENTER / X]" : "TENTAR NOVAMENTE [ENTER / X]";
            if (Button(new Rect(cx - 235, y, 470, 38), action) && !focusLost)
            {
                if (mode == ScreenMode.Playing) SetPaused(false);
                else BeginRun(mode == ScreenMode.Menu ? seed : unchecked(seed + 1));
            }
            if (Button(new Rect(cx - 235, y + 49, 470, 32), sound && sound.SoundEnabled ? "SOM: LIGADO [M]" : "SOM: DESLIGADO [M]"))
                if (sound) sound.SetEnabled(!sound.SoundEnabled);
            if (Button(new Rect(cx - 235, y + 93, 470, 32), "ABRIR A AVENTURA 2D")) SceneManager.LoadScene("Haunt");
        }

        bool Button(Rect rect, string label)
        {
            bool hover = rect.Contains(Event.current.mousePosition);
            Fill(rect, hover ? new Color(.23f, .16f, .24f, 1) : new Color(.1f, .075f, .13f, 1));
            Fill(new Rect(rect.x, rect.yMax - 2, rect.width, 2), hover ? Gold : new Color(.34f, .24f, .32f));
            Text(rect, label, 2, hover ? Gold : Ivory, TextAnchor.MiddleCenter);
            if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || !hover) return false;
            Event.current.Use(); return true;
        }

        void DrawVignette(float width, float height)
        {
            for (int i = 0; i < 12; i++)
            {
                float inset = i * 5;
                Color tint = damageOverlay > 0 ? new Color(.45f, .015f, .04f, .035f + damageOverlay * .035f) : new Color(.01f, .005f, .025f, .035f);
                Fill(new Rect(inset, inset, width - inset * 2, 5), tint);
                Fill(new Rect(inset, height - inset - 5, width - inset * 2, 5), tint);
                Fill(new Rect(inset, inset + 5, 5, height - inset * 2 - 10), tint);
                Fill(new Rect(width - inset - 5, inset + 5, 5, height - inset * 2 - 10), tint);
            }
        }
        void Text(Rect rect, string text, int size, Color tint, TextAnchor anchor = TextAnchor.UpperLeft)
            => lettering.Draw(rect, text, size, tint, anchor);
        static void Fill(Rect rect, Color tint)
        {
            Color old = GUI.color; GUI.color = tint; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
        }
    }
}
