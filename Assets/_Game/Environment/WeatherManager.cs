using UnityEngine;

namespace Ecos
{
    public sealed class WeatherManager : MonoBehaviour
    {
        public Transform player;
        public WorldClock clock;
        public WorldSettings settings;
        public bool paused;
        ParticleSystem rain;
        float intensity, nextChange;
        WeatherKind kind;
        Material material;
        public WeatherKind Current => kind;

        void Start()
        {
            var g = new GameObject("Partículas climáticas");
            g.transform.SetParent(transform);
            rain = g.AddComponent<ParticleSystem>();
            rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = rain.main;
            main.maxParticles = 1400;
            main.startLifetime = 2;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startSize = .045f;
            main.startSpeed = 0;
            var shape = rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28, 1, 28);
            var velocity = rain.velocityOverLifetime;
            velocity.enabled = true;
            velocity.y = -18;
            var renderer = g.GetComponent<ParticleSystemRenderer>();
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            renderer.sharedMaterial = material;
            rain.Play();
        }

        void Update()
        {
            if (!rain || !player)
                return;
            int b = WorldMath.Biome(player.position.x, player.position.z, settings.seed);
            var biome = settings.biomes[b];
            if (Time.time >= nextChange)
            {
                kind = biome.weather;
                nextChange = Time.time + 45;
            }

            if (kind != biome.weather && Time.time + 30 < nextChange)
                nextChange = Time.time;
            float target = kind == WeatherKind.Clear ? 0 : biome.weatherIntensity;
            intensity = Mathf.MoveTowards(intensity, target, Time.deltaTime * .08f);
            clock.weather = intensity;
            clock.biomeFog = Color.Lerp(clock.biomeFog, biome.fog, Time.deltaTime * .3f);
            rain.transform.position = player.position + Vector3.up * 15;
            bool snow = kind == WeatherKind.Snow || kind == WeatherKind.Blizzard, ash = kind == WeatherKind.Ash;
            var emission = rain.emission;
            emission.rateOverTime = paused ? 0 : (kind == WeatherKind.Fog || kind == WeatherKind.Cloudy ? 8 : 120) * intensity;
            var main = rain.main;
            main.startColor = snow ? Color.white : ash ? new Color(.45f, .3f, .25f) : new Color(.49f, .64f, .85f, .6f);
            main.startSize = snow ? .12f : ash ? .08f : .035f;
            var vel = rain.velocityOverLifetime;
            vel.y = snow || ash ? -2 : -18;
            vel.x = Mathf.Sin(Time.time * .2f) * 2;
        }

        void OnDestroy()
        {
            if (material)
                Destroy(material);
        }
    }
}
