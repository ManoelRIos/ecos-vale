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
        Material sky;

        void Start()
        {
            var source = Resources.Load<Material>("ValleySkyMaterial");
            if (source)
            {
                sky = new Material(source);
                RenderSettings.skybox = sky;
            }
        }

        void Update()
        {
            if (!paused)
                time = Mathf.Repeat(time + Time.deltaTime / daySeconds, 1);
            Daylight = Mathf.SmoothStep(0, 1, Mathf.Clamp01(Mathf.Sin((time - .25f) * Mathf.PI * 2) * 2));
            sun.transform.rotation = Quaternion.Euler(time * 360 - 90, 25, 0);
            moon.transform.rotation = Quaternion.Euler(time * 360 + 90, 205, 0);
            sun.intensity = Mathf.Lerp(.015f, 1.5f, Daylight) * (1 - weather * .45f);
            sun.color = Color.Lerp(new Color(1, .42f, .24f), new Color(1, .9f, .74f), Daylight);
            moon.intensity = (1 - Daylight) * .28f;
            moon.color = new Color(.4f, .52f, 1);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(new Color(.12f, .16f, .26f), new Color(.49f, .53f, .57f), Daylight);

            var horizon = Color.Lerp(new Color(.065f, .085f, .15f), new Color(.58f, .63f, .64f), Daylight);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = Mathf.Lerp(.0015f, .0045f, weather) + (1 - Daylight) * .0008f;
            RenderSettings.fogColor = Color.Lerp(horizon, biomeFog, .32f);
            if (sky)
            {
                sky.SetColor("_ZenithColor", Color.Lerp(new Color(.015f, .025f, .075f), new Color(.12f, .27f, .43f), Daylight));
                sky.SetColor("_HorizonColor", horizon);
                sky.SetColor("_GroundColor", RenderSettings.fogColor);
                sky.SetColor("_SunColor", sun.color);
                sky.SetVector("_SunDirection", -sun.transform.forward);
                sky.SetVector("_MoonDirection", -moon.transform.forward);
                sky.SetFloat("_Daylight", Daylight);
            }
            var camera = Camera.main;
            if (camera)
            {
                camera.clearFlags = sky ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                camera.backgroundColor = RenderSettings.fogColor;
            }
            if (night != IsNight)
            {
                night = IsNight;
                NightChanged?.Invoke(night);
            }
        }

        void OnDestroy()
        {
            if (sky)
            {
                if (RenderSettings.skybox == sky) RenderSettings.skybox = null;
                Destroy(sky);
            }
        }
    }
}
