using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos.Haunt.Fps
{
    /// <summary>A deliberately small 3D combat arena. Geometry, texture pixels,
    /// lights and decorations belong to this component and share its lifetime.</summary>
    public sealed class HauntFpsWorld : MonoBehaviour, IDisposable
    {
        public const float Width = 28, Depth = 24;
        public Vector3 PlayerSpawn => new Vector3(0, 0, -8);
        public Vector3 ExitPosition => new Vector3(0, 0, 10.55f);
        public Vector3[] MeleeSpawns => new[] { new Vector3(-5.5f, 0, 5.5f) };
        public Vector3[] RangedSpawns => new[] { new Vector3(6, 0, 6.5f) };
        public Vector3[] BarrelSpawns => new[] { new Vector3(3.2f, 0, 2.4f), new Vector3(5.2f, 0, 3.2f) };
        public bool ExitOpen { get; private set; }
        public int GeneratedTextureCount => textures.Count;
        public int OwnedMaterialCount => materials.Count;

        sealed class Flame
        {
            public Light Light;
            public Transform Shape;
            public float Phase, Strength;
            public Vector3 Scale;
        }
        static HauntFpsWorld environmentOwner;
        readonly List<Material> materials = new List<Material>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        readonly List<Flame> flames = new List<Flame>();
        Transform geometry;
        GameObject seal;
        Material exitMaterial;
        Light exitLight;
        float clock;
        bool initialized, disposed;
        Color previousAmbient, previousFog;
        AmbientMode previousAmbientMode;
        bool previousFogEnabled;
        FogMode previousFogMode;
        float previousFogDensity, previousFogStart, previousFogEnd;

        public void Initialize(HauntFpsGame game)
        {
            if (initialized) throw new InvalidOperationException("A arena FPS já foi construída.");
            if (disposed) throw new ObjectDisposedException(nameof(HauntFpsWorld));
            initialized = true;
            geometry = new GameObject("Cripta / geometria e luzes").transform;
            geometry.SetParent(transform, false);
            ConfigureAtmosphere();
            BuildArena();
            SetExitOpen(false);
        }

        public bool IsInsideBounds(Vector3 position, float margin = 0)
        {
            margin = Mathf.Max(0, margin);
            return position.x >= -Width * .5f + margin && position.x <= Width * .5f - margin
                && position.z >= -Depth * .5f + margin && position.z <= Depth * .5f - margin;
        }

        void ConfigureAtmosphere()
        {
            if (environmentOwner)
            {
                previousAmbient = environmentOwner.previousAmbient;
                previousAmbientMode = environmentOwner.previousAmbientMode;
                previousFog = environmentOwner.previousFog;
                previousFogEnabled = environmentOwner.previousFogEnabled;
                previousFogMode = environmentOwner.previousFogMode;
                previousFogDensity = environmentOwner.previousFogDensity;
                previousFogStart = environmentOwner.previousFogStart;
                previousFogEnd = environmentOwner.previousFogEnd;
            }
            else
            {
                previousAmbient = RenderSettings.ambientLight;
                previousAmbientMode = RenderSettings.ambientMode;
                previousFog = RenderSettings.fogColor;
                previousFogEnabled = RenderSettings.fog;
                previousFogMode = RenderSettings.fogMode;
                previousFogDensity = RenderSettings.fogDensity;
                previousFogStart = RenderSettings.fogStartDistance;
                previousFogEnd = RenderSettings.fogEndDistance;
            }
            environmentOwner = this;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.22f, .19f, .31f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.055f, .045f, .09f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .018f;
            RenderSettings.fogStartDistance = 10;
            RenderSettings.fogEndDistance = 45;
        }

        void BuildArena()
        {
            var floor = Surface("piso", new Color(.48f, .5f, .53f), PixelTexture("piso", 64, 0));
            floor.mainTextureScale = new Vector2(14, 12);
            var wall = Surface("alvenaria", new Color(.45f, .43f, .52f), PixelTexture("parede", 32, 1));
            var trim = Surface("pedra fria", new Color(.31f, .34f, .41f), PixelTexture("pedra", 32, 2));
            var bone = Surface("osso", new Color(.81f, .77f, .61f));
            var iron = Surface("ferro", new Color(.16f, .19f, .23f));
            var wood = Surface("madeira", new Color(.27f, .19f, .15f), PixelTexture("madeira", 32, 3));
            var wax = Surface("cera", new Color(.75f, .67f, .47f));
            var pumpkin = Surface("abóbora", new Color(.78f, .27f, .075f));
            var black = Surface("recorte negro", new Color(.035f, .025f, .045f));
            var amber = Surface("chama âmbar", new Color(1, .48f, .09f), null, new Color(2.5f, .8f, .15f));
            var green = Surface("chama espectral", new Color(.38f, .95f, .62f), null, new Color(.25f, 1.3f, .65f));
            exitMaterial = Surface("selo da saída", new Color(.33f, .16f, .5f), null, new Color(.3f, .08f, .5f));

            Cube("Chão sólido", new Vector3(0, -.2f, 0), new Vector3(Width + .8f, .4f, Depth + .8f), floor, true);
            // Segments retain clear texels instead of stretching one brick over an entire wall.
            for (int x = -7; x < 7; x++)
            {
                Cube("Parede sul", new Vector3(x * 2 + 1, 2, -12.2f), new Vector3(2, 4, .4f), wall, true);
                Cube("Parede norte", new Vector3(x * 2 + 1, 2, 12.2f), new Vector3(2, 4, .4f), wall, true);
                Cube("Coroamento sul", new Vector3(x * 2 + 1, 4.08f, -12.18f), new Vector3(2.08f, .2f, .65f), trim, false);
                Cube("Coroamento norte", new Vector3(x * 2 + 1, 4.08f, 12.18f), new Vector3(2.08f, .2f, .65f), trim, false);
            }
            for (int z = -6; z < 6; z++)
            {
                Cube("Parede oeste", new Vector3(-14.2f, 2, z * 2 + 1), new Vector3(.4f, 4, 2), wall, true);
                Cube("Parede leste", new Vector3(14.2f, 2, z * 2 + 1), new Vector3(.4f, 4, 2), wall, true);
            }
            // Cover sits away from the center route and the two enemy approaches.
            foreach (var p in new[] { new Vector3(-8, 0, -1), new Vector3(8, 0, -1), new Vector3(-9, 0, 7), new Vector3(9, 0, 7) })
                Pillar(p, wall, trim, bone);
            foreach (var p in new[] { new Vector3(-11, 0, 3.5f), new Vector3(11, 0, 3.5f), new Vector3(-11, 0, -5), new Vector3(11, 0, -5) })
                Gravestone(p, trim, bone);
            // Decorative objects are non-solid; only cover and architecture stop movement or shots.
            for (int side = -1; side <= 1; side += 2)
            {
                Fence(new Vector3(side * 11.5f, 0, 9.8f), iron);
                for (int n = 0; n < 3; n++)
                    Cube("Tábua caída", new Vector3(side * (11.8f - n * .45f), .035f, -9.7f + n * .25f), new Vector3(.17f, .07f, 1.1f), wood, false).transform.localRotation = Quaternion.Euler(0, side * (n * 19 + 8), 0);
                Pumpkin(new Vector3(side * 10.8f, .38f, -9.2f), pumpkin, black, amber);
                Pumpkin(new Vector3(side * 12.2f, .3f, 8.5f), pumpkin, black, green);
            }
            Candelabrum(new Vector3(-11.8f, 0, -3.5f), iron, bone, wax, amber, new Color(1, .47f, .13f), 1);
            Candelabrum(new Vector3(11.8f, 0, -3.5f), iron, bone, wax, amber, new Color(1, .47f, .13f), 4);
            Candelabrum(new Vector3(-11.8f, 0, 6.5f), iron, bone, wax, green, new Color(.35f, 1, .6f), 8);
            Candelabrum(new Vector3(11.8f, 0, 6.5f), iron, bone, wax, amber, new Color(1, .47f, .13f), 12);
            Candle(new Vector3(-2.6f, .03f, 9.9f), wax, amber, .34f);
            Candle(new Vector3(2.6f, .03f, 9.9f), wax, amber, .46f);
            Skull(new Vector3(-12.6f, .16f, -7.4f), bone, black, .28f);
            Skull(new Vector3(12.6f, .16f, 1.1f), bone, black, .24f);
            BuildExit(trim, bone, iron);
            Cobweb(new Vector3(-13.96f, 3.8f, 11.94f), bone, false);
            Cobweb(new Vector3(13.96f, 3.8f, 11.94f), bone, true);
            // A broken stone path and a flat ritual mark help orient the player.
            for (int i = 0; i < 7; i++)
                Cube("Junta do caminho", new Vector3((i % 2 == 0 ? -.05f : .05f), .012f, -7 + i * 2.35f), new Vector3(2.4f, .016f, .045f), trim, false);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * .25f;
                var rune = Cube("Runas do ossuário", new Vector3(Mathf.Sin(angle) * 2.25f, .018f, Mathf.Cos(angle) * 2.25f), new Vector3(.1f, .02f, .54f), exitMaterial, false);
                rune.transform.localRotation = Quaternion.Euler(0, -i * 45, 0);
            }
            Point(new Vector3(-11, 1.05f, -9.2f), new Color(1, .37f, .08f), 1.35f, 5.5f, null, 16);
            Point(new Vector3(11, 1.05f, -9.2f), new Color(1, .37f, .08f), 1.35f, 5.5f, null, 20);
            var sunObject = new GameObject("Luz da lua"); sunObject.transform.SetParent(geometry, false);
            sunObject.transform.localRotation = Quaternion.Euler(52, -28, 0);
            var moon = sunObject.AddComponent<Light>(); moon.type = LightType.Directional;
            moon.color = new Color(.48f, .59f, .82f); moon.intensity = .58f;
            moon.shadows = LightShadows.Soft; moon.shadowStrength = .7f;
        }

        void BuildExit(Material stone, Material bone, Material iron)
        {
            var center = ExitPosition;
            Cube("Portal / pilar esquerdo", center + new Vector3(-1.65f, 1.7f, .3f), new Vector3(.55f, 3.4f, .7f), stone, true);
            Cube("Portal / pilar direito", center + new Vector3(1.65f, 1.7f, .3f), new Vector3(.55f, 3.4f, .7f), stone, true);
            Cube("Portal / verga", center + new Vector3(0, 3.5f, .3f), new Vector3(3.9f, .5f, .85f), stone, false);
            Skull(center + new Vector3(0, 3.65f, -.18f), bone, iron, .39f);
            seal = Cube("Selo fechado", center + new Vector3(0, 1.6f, .25f), new Vector3(2.7f, 3.2f, .18f), exitMaterial, true);
            for (int side = -1; side <= 1; side += 2)
                Cube("Marca da saída", center + new Vector3(side * .65f, 2.4f, .13f), new Vector3(.08f, 1.1f, .04f), bone, false).transform.localRotation = Quaternion.Euler(0, 0, side * 24);
            exitLight = Point(center + new Vector3(0, 2.5f, -.8f), new Color(.5f, .2f, .9f), 1.2f, 7, null, 27);
        }

        void Pillar(Vector3 at, Material stone, Material trim, Material bone)
        {
            Cube("Pilar / corpo", at + Vector3.up * 1.7f, new Vector3(1.15f, 3.4f, 1.15f), stone, true);
            Cube("Pilar / base", at + Vector3.up * .13f, new Vector3(1.55f, .26f, 1.55f), trim, true);
            Cube("Pilar / capitel", at + Vector3.up * 3.5f, new Vector3(1.65f, .32f, 1.65f), trim, false);
            for (int side = -1; side <= 1; side += 2)
                Cube("Pilar / osso", at + new Vector3(side * .35f, 2.35f, -.588f), new Vector3(.09f, .7f, .045f), bone, false).transform.localRotation = Quaternion.Euler(0, 0, side * 11);
        }

        void Gravestone(Vector3 at, Material stone, Material bone)
        {
            var root = new GameObject("Lápide / cenário").transform; root.SetParent(geometry, false); root.localPosition = at;
            Cube("Lápide / base", new Vector3(0, .08f, 0), new Vector3(1, .16f, .7f), stone, false, root);
            Cube("Lápide / corpo", new Vector3(0, .64f, 0), new Vector3(.7f, 1.1f, .24f), stone, false, root);
            Cube("Lápide / topo", new Vector3(0, 1.2f, 0), new Vector3(.48f, .2f, .27f), stone, false, root);
            Cube("Lápide / cruz", new Vector3(0, .76f, -.13f), new Vector3(.07f, .55f, .03f), bone, false, root);
            Cube("Lápide / cruz", new Vector3(0, .85f, -.13f), new Vector3(.34f, .07f, .03f), bone, false, root);
        }

        void Fence(Vector3 at, Material iron)
        {
            for (int n = 0; n < 6; n++)
            {
                var spike = Cube("Grade / ponta", at + new Vector3(n * .3f - .75f, .9f, 0), new Vector3(.075f, 1.8f, .075f), iron, false);
                spike.transform.localRotation = Quaternion.Euler(0, 0, n % 2 == 0 ? 3 : -3);
            }
            Cube("Grade / travessa", at + Vector3.up * .62f, new Vector3(1.8f, .07f, .09f), iron, false);
        }

        void Pumpkin(Vector3 at, Material orange, Material dark, Material glow)
        {
            var root = new GameObject("Abóbora / cenário").transform; root.SetParent(geometry, false); root.localPosition = at;
            var body = Cube("Abóbora", Vector3.zero, new Vector3(.72f, .58f, .58f), orange, false, root);
            body.transform.localRotation = Quaternion.Euler(0, 13, 0);
            Cube("Abóbora / gomo", new Vector3(-.29f, 0, 0), new Vector3(.18f, .46f, .49f), orange, false, root);
            Cube("Abóbora / gomo", new Vector3(.29f, 0, 0), new Vector3(.18f, .46f, .49f), orange, false, root);
            Cube("Abóbora / caule", new Vector3(0, .38f, 0), new Vector3(.1f, .2f, .11f), dark, false, root);
            Cube("Abóbora / olho", new Vector3(-.15f, .11f, -.3f), new Vector3(.11f, .095f, .035f), glow, false, root);
            Cube("Abóbora / olho", new Vector3(.15f, .11f, -.3f), new Vector3(.11f, .095f, .035f), glow, false, root);
            Cube("Abóbora / boca", new Vector3(0, -.1f, -.3f), new Vector3(.34f, .07f, .035f), dark, false, root);
        }

        void Candelabrum(Vector3 at, Material iron, Material bone, Material wax, Material flame, Color color, float phase)
        {
            Cube("Candelabro / haste", at + Vector3.up * .8f, new Vector3(.13f, 1.6f, .13f), iron, false);
            Cube("Candelabro / base", at + Vector3.up * .07f, new Vector3(.65f, .14f, .65f), iron, false);
            Cube("Candelabro / braço", at + Vector3.up * 1.5f, new Vector3(.95f, .09f, .13f), bone, false);
            Transform centerFlame = null;
            for (int branch = -1; branch <= 1; branch++)
            {
                Transform shape = Candle(at + new Vector3(branch * .4f, 1.52f, 0), wax, flame, branch == 0 ? .45f : .31f);
                if (branch == 0) centerFlame = shape;
            }
            Point(at + Vector3.up * 2.1f, color, 3, 8, centerFlame, phase);
        }

        Transform Candle(Vector3 at, Material wax, Material flame, float height)
        {
            Cube("Vela", at + Vector3.up * height * .5f, new Vector3(.13f, height, .13f), wax, false);
            Cube("Vela / cera", at + new Vector3(.073f, height * .62f, 0), new Vector3(.025f, height * .45f, .08f), wax, false);
            var shape = Cube("Chama", at + Vector3.up * (height + .1f), new Vector3(.12f, .23f, .12f), flame, false).transform;
            shape.localRotation = Quaternion.Euler(0, 0, 12);
            return shape;
        }

        void Skull(Vector3 at, Material bone, Material dark, float size)
        {
            Cube("Caveira", at, new Vector3(size * 1.3f, size, size * .85f), bone, false);
            Cube("Caveira / mandíbula", at + Vector3.down * size * .5f, new Vector3(size * .85f, size * .25f, size * .65f), bone, false);
            for (int side = -1; side <= 1; side += 2)
                Cube("Caveira / olho", at + new Vector3(side * size * .3f, 0, -size * .44f), new Vector3(size * .24f, size * .23f, .018f), dark, false);
        }

        void Cobweb(Vector3 corner, Material material, bool mirrored)
        {
            float direction = mirrored ? -1 : 1;
            for (int ray = 0; ray < 4; ray++)
            {
                float x = direction * (ray + 1) * .36f, z = (4 - ray) * -.36f;
                ThinSegment("Teia / raio", corner, corner + new Vector3(x, -.7f, z), .014f, material);
            }
            for (int ring = 1; ring < 4; ring++)
            {
                float t = ring / 4f;
                for (int section = 0; section < 3; section++)
                {
                    var a = corner + new Vector3(direction * (section + 1) * .36f * t, -.7f * t, (4 - section) * -.36f * t);
                    var b = corner + new Vector3(direction * (section + 2) * .36f * t, -.7f * t, (3 - section) * -.36f * t);
                    ThinSegment("Teia / arco", a, b, .014f, material);
                }
            }
        }

        void ThinSegment(string name, Vector3 from, Vector3 to, float width, Material material)
        {
            Vector3 difference = to - from;
            var shape = Cube(name, (from + to) * .5f, new Vector3(width, width, difference.magnitude), material, false);
            shape.transform.rotation = Quaternion.LookRotation(difference);
        }

        GameObject Cube(string name, Vector3 position, Vector3 size, Material material, bool solid, Transform parent = null)
        {
            var shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shape.name = name; shape.transform.SetParent(parent ? parent : geometry, false);
            shape.transform.localPosition = position; shape.transform.localScale = size;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            var collision = shape.GetComponent<Collider>();
            if (!solid && collision) { collision.enabled = false; DestroyOwned(collision); }
            return shape;
        }

        Material Surface(string name, Color color, Texture2D texture = null, Color? emission = null)
        {
            var template = Resources.Load<Material>("FpsPalette");
            Material material;
            if (template) material = new Material(template);
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (!shader) throw new InvalidOperationException("Shader URP do protótipo FPS não encontrado.");
                material = new Material(shader);
            }
            material.name = "FPS / " + name;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .05f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
            if (texture)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            }
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission.Value);
            }
            materials.Add(material); return material;
        }

        Texture2D PixelTexture(string name, int size, int style)
        {
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                uint hash = unchecked((uint)(x * 73856093 ^ y * 19349663 ^ style * 83492791));
                hash ^= hash >> 13; hash *= 1274126177;
                int grain = (int)(hash % 13) - 6;
                int baseShade;
                if (style == 0)
                {
                    bool joint = x % 16 == 0 || y % 16 == 0;
                    bool edge = x % 16 == 1 || y % 16 == 1;
                    baseShade = joint ? 61 : edge ? 158 : ((x / 16 + y / 16) % 2 == 0 ? 137 : 121);
                }
                else if (style == 1)
                {
                    int row = y / 8, stagger = row % 2 * 8;
                    bool mortar = y % 8 == 0 || (x + stagger) % 16 == 0;
                    baseShade = mortar ? 50 : y % 8 == 1 ? 151 : (int)(hash % 3) * 8 + 111;
                }
                else if (style == 3)
                    baseShade = x % 8 == 0 ? 52 : 132 + ((y + (x % 5) * 3) % 17 == 0 ? -27 : 0);
                else baseShade = 137 + ((x + y * 2) % 19 == 0 ? -26 : 0);
                byte r = (byte)Mathf.Clamp(baseShade + grain, 0, 255);
                byte g = (byte)Mathf.Clamp(baseShade + grain + (style == 0 ? 3 : 0), 0, 255);
                byte b = (byte)Mathf.Clamp(baseShade + grain + (style == 0 || style == 1 ? 9 : -5), 0, 255);
                pixels[y * size + x] = new Color32(r, g, b, 255);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { name = "FPS / texels / " + name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, anisoLevel = 0 };
            texture.SetPixels32(pixels); texture.Apply(false, true); textures.Add(texture); return texture;
        }

        Light Point(Vector3 position, Color color, float intensity, float range, Transform shape, float phase)
        {
            var go = new GameObject("Luz tremeluzente"); go.transform.SetParent(geometry, false); go.transform.localPosition = position;
            var light = go.AddComponent<Light>(); light.type = LightType.Point; light.color = color;
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
            flames.Add(new Flame { Light = light, Shape = shape, Scale = shape ? shape.localScale : Vector3.one, Strength = intensity, Phase = phase });
            return light;
        }

        public void SetExitOpen(bool open)
        {
            ExitOpen = open;
            if (seal) seal.SetActive(!open);
            if (exitLight)
            {
                exitLight.color = open ? new Color(.3f, 1, .65f) : new Color(.5f, .2f, .9f);
                exitLight.intensity = open ? 2.2f : 1.2f;
            }
            if (exitMaterial)
            {
                var color = open ? new Color(.2f, .85f, .55f) : new Color(.33f, .16f, .5f);
                exitMaterial.color = color;
                if (exitMaterial.HasProperty("_BaseColor")) exitMaterial.SetColor("_BaseColor", color);
                if (exitMaterial.HasProperty("_EmissionColor")) exitMaterial.SetColor("_EmissionColor", color * (open ? 1.35f : 1));
            }
        }

        public void Tick(float dt)
        {
            if (disposed || !initialized || !(dt > 0) || float.IsInfinity(dt)) return;
            clock += Mathf.Min(dt, .1f);
            for (int i = 0; i < flames.Count; i++)
            {
                var flame = flames[i];
                if (!flame.Light) continue;
                float wobble = Mathf.PerlinNoise(flame.Phase, clock * 2.1f) * 2 - 1;
                // The exit remains a steady landmark, including after the fight.
                if (flame.Light == exitLight) continue;
                flame.Light.intensity = flame.Strength * (1 + wobble * .13f);
                if (flame.Shape) flame.Shape.localScale = new Vector3(flame.Scale.x, flame.Scale.y * (1 + wobble * .13f), flame.Scale.z);
            }
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (environmentOwner == this)
            {
                environmentOwner = null;
                RenderSettings.ambientMode = previousAmbientMode; RenderSettings.ambientLight = previousAmbient;
                RenderSettings.fog = previousFogEnabled; RenderSettings.fogColor = previousFog;
                RenderSettings.fogMode = previousFogMode; RenderSettings.fogDensity = previousFogDensity;
                RenderSettings.fogStartDistance = previousFogStart; RenderSettings.fogEndDistance = previousFogEnd;
            }
            foreach (var material in materials) if (material) DestroyOwned(material);
            foreach (var texture in textures) if (texture) DestroyOwned(texture);
            materials.Clear(); textures.Clear(); flames.Clear();
            if (geometry) { geometry.gameObject.SetActive(false); DestroyOwned(geometry.gameObject); }
        }
        static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
        void OnDestroy() { Dispose(); }
    }
}
