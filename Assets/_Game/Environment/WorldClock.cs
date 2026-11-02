using UnityEngine;

namespace Ecos
{
    public sealed class WorldClock : MonoBehaviour
    {
        public float time = .3f;
        public float daySeconds = 900;
        public bool paused;
        public Light sun, moon;
        public Color biomeFog = new Color(.25f, .2f, .32f);
        public float weather;
        public bool IsNight => time < .23f || time > .77f;
        public float Daylight { get; private set; }

        public event System.Action<bool> NightChanged;
        bool night;
        void Update()
        {
            if (!paused)
                time = Mathf.Repeat(time + Time.deltaTime / daySeconds, 1);
            Daylight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Sin((time - .25f) * Mathf.PI * 2) * 2));
            sun.transform.rotation = Quaternion.Euler(time * 360 - 90, 25, 0);
            moon.transform.rotation = Quaternion.Euler(time * 360 + 90, 205, 0);
            sun.intensity = Mathf.Lerp(.015f, 1.5f, Daylight) * (1 - weather * .55f);
            sun.color = Color.Lerp(new Color(1, .39f, .18f), new Color(1, .88f, .71f), Daylight);
            moon.intensity = (1 - Daylight) * .35f;
            moon.color = new Color(.38f, .49f, 1);
            RenderSettings.ambientLight = Color.Lerp(new Color(.12f, .13f, .25f), new Color(.48f, .47f, .53f), Daylight);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Lerp(.003f, .009f, weather) + (1 - Daylight) * .002f;
            RenderSettings.fogColor = Color.Lerp(biomeFog * .28f, biomeFog, Daylight);
            if (Camera.main)
                Camera.main.backgroundColor = RenderSettings.fogColor;
            if (night != IsNight)
            {
                night = IsNight;
                NightChanged?.Invoke(night);
            }
        }
    }
}
