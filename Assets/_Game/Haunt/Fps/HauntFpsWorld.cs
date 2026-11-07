using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Ecos.Haunt.LowPoly;

namespace Ecos.Haunt.Fps
{
    /// <summary>Shared 3D geometry for both perspectives. Each campaign theme owns
    /// its palette, cover plan, decorations, lights and generated textures.</summary>
    public sealed class HauntFpsWorld : MonoBehaviour, IDisposable
    {
        public const float Width = 28, Depth = 24;
        public Vector3 PlayerSpawn => new Vector3(0, 0, -8);
        public Vector3 ExitPosition => new Vector3(0, 0, lowPoly ? 8.5f : 10.55f);
        public Vector3 BossSpawn => new Vector3(0, 0, 5.5f);
        public Vector3[] MeleeSpawns => new[] { new Vector3(-5.5f, 0, 5.5f), new Vector3(5.5f, 0, -.5f), new Vector3(-5.5f, 0, -.5f) };
        public Vector3[] RangedSpawns => new[] { new Vector3(6, 0, 6.5f), new Vector3(-6, 0, 7.5f), new Vector3(5.5f, 0, -4.5f) };
        public Vector3[] BarrelSpawns => new[] { new Vector3(3.2f, 0, 2.4f), new Vector3(5.2f, 0, 3.2f) };
        public int ThemeIndex { get; private set; }
        public int FloorNumber { get; private set; } = 1;
        public string ThemeName => HauntFpsCampaign.ThemeName(FloorNumber);
        public string LayoutName => ThemeIndex == 0 ? "JARDIM FUNERARIO" : ThemeIndex == 1 ? "ALCOVAS DO VEU"
            : ThemeIndex == 2 ? "NAVE DA CATEDRAL" : ThemeIndex == 3 ? "PONTES DO NINHO" : "ANEL DO ABISMO";
        public bool ExitOpen { get; private set; }
        public int GeneratedTextureCount => textures.Count;
        public int OwnedMaterialCount => materials.Count;
        public bool RoomDoorsOpen => roomDoors != null && roomDoors.IsOpen;
        public int ConnectedDoorCount => roomDoors == null ? 0 : roomDoors.Count;

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
        HauntFpsGame owner;
        HauntLowPolyDoors roomDoors;
        bool lowPoly;
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
            owner = game; lowPoly = game && game.LowPolyMode;
            FloorNumber = game && game.State != null ? Mathf.Max(1, game.State.Floor) : 1;
            ThemeIndex = HauntFpsCampaign.ThemeIndex(FloorNumber);
            geometry = new GameObject(ThemeName + " / geometria e luzes").transform;
            geometry.SetParent(transform, false);
            ConfigureAtmosphere();
            if (lowPoly) BuildLowPolyArena(); else BuildArena();
            SetExitOpen(false);
            SetRoomDoorsOpen(lowPoly && game.LowPolyRoom != null && game.LowPolyRoom.Cleared);
        }

        public void SetRoomDoorsOpen(bool open)
        {
            if (roomDoors != null) roomDoors.SetOpen(open);
        }

        public bool TryGetNearbyDoor(Vector3 player, float distance, out int roomId, out Vector3 returnSpawn)
        {
            roomId = -1; returnSpawn = Vector3.zero;
            return roomDoors != null && roomDoors.TryGetNearby(player, distance, out roomId, out returnSpawn);
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
            RenderSettings.ambientLight = ThemeIndex == 0 ? new Color(.22f, .19f, .27f)
                : ThemeIndex == 1 ? new Color(.20f, .19f, .34f) : ThemeIndex == 2 ? new Color(.28f, .16f, .20f)
                : ThemeIndex == 3 ? new Color(.16f, .25f, .19f) : new Color(.17f, .13f, .28f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = ThemeIndex == 0 ? new Color(.055f, .045f, .065f)
                : ThemeIndex == 1 ? new Color(.065f, .055f, .13f) : ThemeIndex == 2 ? new Color(.095f, .035f, .05f)
                : ThemeIndex == 3 ? new Color(.035f, .085f, .055f) : new Color(.04f, .025f, .10f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = ThemeIndex == 0 ? .018f : ThemeIndex == 1 ? .025f : ThemeIndex == 2 ? .02f : ThemeIndex == 3 ? .029f : .032f;
            RenderSettings.fogStartDistance = 10;
            RenderSettings.fogEndDistance = 45;
            if (lowPoly)
            {
                RenderSettings.ambientLight *= 1.35f;
                RenderSettings.fogDensity *= .78f;
            }
        }

        void BuildLowPolyArena()
        {
            Color floorTint = ThemeIndex == 0 ? new Color(.25f, .29f, .29f) : ThemeIndex == 1 ? new Color(.24f, .24f, .36f)
                : ThemeIndex == 2 ? new Color(.34f, .27f, .28f) : ThemeIndex == 3 ? new Color(.23f, .31f, .23f) : new Color(.22f, .22f, .3f);
            Color wallTint = ThemeIndex == 0 ? new Color(.40f, .44f, .43f) : ThemeIndex == 1 ? new Color(.34f, .31f, .48f)
                : ThemeIndex == 2 ? new Color(.47f, .36f, .37f) : ThemeIndex == 3 ? new Color(.32f, .4f, .29f) : new Color(.32f, .31f, .43f);
            Color accent = ThemeIndex == 0 ? new Color(1, .48f, .12f) : ThemeIndex == 1 ? new Color(.6f, .36f, 1)
                : ThemeIndex == 2 ? new Color(1, .25f, .2f) : ThemeIndex == 3 ? new Color(.37f, 1, .38f) : new Color(.46f, .43f, 1);
            Material floor = Surface("lowpoly / piso", floorTint);
            Material wall = Surface("lowpoly / alvenaria", wallTint);
            Material stone = Surface("lowpoly / pedra clara", wallTint * 1.25f);
            Material dark = Surface("lowpoly / recorte", new Color(.035f, .03f, .055f));
            Material bone = Surface("lowpoly / osso", new Color(.78f, .76f, .63f));
            Material iron = Surface("lowpoly / ferro", new Color(.16f, .18f, .23f));
            Material wood = Surface("lowpoly / madeira", new Color(.3f, .22f, .16f));
            Material wax = Surface("lowpoly / cera", new Color(.83f, .72f, .48f));
            Material glow = Surface("lowpoly / chama", accent, null, accent * 2);
            exitMaterial = Surface("lowpoly / portal", new Color(.26f, .14f, .45f), null, new Color(.3f, .1f, .55f));
            Cube("Chão sólido", new Vector3(0, -.2f, 0), new Vector3(Width + .8f, .4f, Depth + .8f), floor, true);

            roomDoors = new HauntLowPolyDoors(owner, geometry, wall, stone, dark);
            // Separate flat floor facets add material variation without textures,
            // tiny colliders, or changes to the shared walking plane.
            int roomSeed = owner.LowPolyRoom == null ? 0 : owner.LowPolyRoom.Id;
            for (int tile = 0; tile < 12; tile++)
            {
                float x = -10 + (tile % 4) * 6.4f, z = -8 + (tile / 4) * 7.2f;
                var facet = Facet("Piso / placa facetada", new Vector3(x, .006f, z), new Vector3(3.7f, .012f, 3.2f),
                    tile % 3 == 0 ? stone : wall, false, LowPolyShape.Cylinder);
                facet.transform.localRotation = Quaternion.Euler(0, (tile * 29 + roomSeed * 17) % 90, 0);
            }
            BuildLowPolyCover(wall, stone, bone, wood);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 lantern = new Vector3(side * 11, 0, -6);
                Facet("Pedestal / base", lantern + Vector3.up * .13f, new Vector3(.9f, .26f, .9f), stone, false, LowPolyShape.Cylinder);
                Facet("Candelabro / haste", lantern + Vector3.up * .9f, new Vector3(.16f, 1.6f, .16f), iron, false, LowPolyShape.Cylinder);
                Facet("Candelabro / braco", lantern + Vector3.up * 1.65f, new Vector3(1, .1f, .12f), bone, false, LowPolyShape.BevelBox);
                Transform centerFlame = null;
                for (int candle = -1; candle <= 1; candle++)
                {
                    Vector3 at = lantern + new Vector3(candle * .38f, 1.73f, 0);
                    Facet("Vela facetada", at + Vector3.up * .16f, new Vector3(.14f, .32f, .14f), wax, false, LowPolyShape.Cylinder);
                    Transform flame = Facet("Chama facetada", at + Vector3.up * .43f, new Vector3(.2f, .36f, .2f), glow, false, LowPolyShape.Cone).transform;
                    if (candle == 0) centerFlame = flame;
                }
                Point(lantern + Vector3.up * 2.2f, accent, 3.1f, 8, centerFlame, side < 0 ? 2 : 6);
                BuildLowPolyThemeOrnament(new Vector3(side * 11.4f, 0, 5.8f), wall, stone, bone, wood, iron, dark, glow, accent);
            }
            bool bossRoom = owner.LowPolyRoom == null || owner.LowPolyRoom.Kind == RoomKind.Boss;
            if (bossRoom)
            {
                Facet("Portal / circulo do andar", ExitPosition + Vector3.up * .055f, new Vector3(3.7f, .11f, 3.7f), stone, false, LowPolyShape.Cylinder);
                for (int rune = 0; rune < 8; rune++)
                {
                    float angle = rune * Mathf.PI * .25f;
                    Facet("Portal / cristal", ExitPosition + new Vector3(Mathf.Cos(angle) * 1.7f, .42f, Mathf.Sin(angle) * 1.7f), new Vector3(.23f, .8f, .23f), exitMaterial, false, LowPolyShape.Crystal);
                }
                seal = Facet("Selo do andar", ExitPosition + Vector3.up * 1.15f, new Vector3(.76f, 1.8f, .76f), exitMaterial, false, LowPolyShape.Crystal);
                exitLight = Point(ExitPosition + Vector3.up * 1.6f, accent, 1.5f, 8, null, 27);
            }
            var sunObject = new GameObject("Luar / low poly"); sunObject.transform.SetParent(geometry, false);
            sunObject.transform.localRotation = Quaternion.Euler(52, -28, 0);
            var moon = sunObject.AddComponent<Light>(); moon.type = LightType.Directional;
            moon.color = ThemeIndex == 2 ? new Color(.8f, .53f, .53f) : ThemeIndex == 3 ? new Color(.54f, .72f, .6f) : new Color(.6f, .67f, .87f);
            moon.intensity = .8f - ThemeIndex * .035f; moon.shadows = LightShadows.Soft; moon.shadowStrength = .78f;
        }

        void BuildLowPolyCover(Material wall, Material stone, Material bone, Material wood)
        {
            // All doors, four central approaches, and the boss's staging area stay
            // clear. Deterministic rotations change the silhouette from room to room.
            int roomId = owner.LowPolyRoom == null ? 0 : owner.LowPolyRoom.Id;
            for (int side = -1; side <= 1; side += 2)
                for (int row = 0; row < 2; row++)
                {
                    Vector3 at = new Vector3(side * (7.5f + (roomId % 2) * .6f), 0, -4.8f + row * 10.2f);
                    if (ThemeIndex == 0)
                    {
                        Facet("Tumulo / base", at + Vector3.up * .16f, new Vector3(1.9f, .32f, 1.3f), stone, true, LowPolyShape.BevelBox);
                        Facet("Tumulo / lapide", at + new Vector3(0, 1.05f, -.24f), new Vector3(1.2f, 1.65f, .45f), wall, true, LowPolyShape.BevelBox);
                        Facet("Tumulo / cruz", at + new Vector3(0, 1.15f, -.49f), new Vector3(.12f, .7f, .06f), bone, false, LowPolyShape.BevelBox);
                        Facet("Tumulo / cruz", at + new Vector3(0, 1.3f, -.49f), new Vector3(.55f, .12f, .06f), bone, false, LowPolyShape.BevelBox);
                    }
                    else if (ThemeIndex == 3)
                    {
                        var trunk = Facet("Tronco / cover", at + Vector3.up * .55f, new Vector3(1.1f, 2.8f, 1.1f), wood, true, LowPolyShape.Cylinder);
                        trunk.transform.localRotation = Quaternion.Euler(0, roomId % 2 == 0 ? 22 : -22, 90);
                        Facet("Raiz / cover", at + new Vector3(.5f, .42f, .35f), new Vector3(.8f, .8f, .85f), wall, false, LowPolyShape.Cone);
                    }
                    else
                    {
                        Facet("Pilar / base facetada", at + Vector3.up * .15f, new Vector3(1.6f, .3f, 1.6f), stone, true, LowPolyShape.Cylinder);
                        Facet("Pilar / corpo facetado", at + Vector3.up * 1.75f, new Vector3(1.1f, 3.2f, 1.1f), wall, true, LowPolyShape.Cylinder);
                        Facet("Pilar / capitel facetado", at + Vector3.up * 3.4f, new Vector3(1.8f, .25f, 1.8f), stone, false, LowPolyShape.Cylinder);
                        if (ThemeIndex == 4)
                            Facet("Pilar / cristal", at + new Vector3(0, 2.3f, -.61f), new Vector3(.4f, .9f, .3f), exitMaterial, false, LowPolyShape.Crystal);
                    }
                }
        }

        void BuildLowPolyThemeOrnament(Vector3 at, Material wall, Material stone, Material bone, Material wood,
            Material iron, Material dark, Material glow, Color accent)
        {
            if (ThemeIndex == 0)
            {
                var orange = Surface("lowpoly / abobora", new Color(.85f, .32f, .075f));
                Facet("Abobora facetada", at + Vector3.up * .5f, new Vector3(1.1f, .9f, 1), orange, false, LowPolyShape.Sphere);
                Facet("Abobora / caule", at + Vector3.up * 1.05f, new Vector3(.13f, .3f, .13f), wood, false, LowPolyShape.Cylinder);
                for (int side = -1; side <= 1; side += 2)
                    Facet("Abobora / olho", at + new Vector3(side * .2f, .57f, -.48f), new Vector3(.16f, .16f, .06f), glow, false, LowPolyShape.Crystal);
                Facet("Arvore morta / tronco", at + new Vector3(.8f, 1.4f, 1.3f), new Vector3(.3f, 2.8f, .3f), wood, false, LowPolyShape.Cylinder);
                for (int branch = -1; branch <= 1; branch += 2)
                    Facet("Arvore morta / galho", at + new Vector3(.8f + branch * .4f, 2.6f, 1.3f), new Vector3(.15f, 1.3f, .15f), wood, false, LowPolyShape.Cylinder).transform.localRotation = Quaternion.Euler(0, 0, branch * 40);
            }
            else if (ThemeIndex == 1)
            {
                Facet("Santuario / pedestal", at + Vector3.up * .5f, new Vector3(1.4f, 1, 1.4f), stone, false, LowPolyShape.TaperedPrism);
                var crystal = Facet("Cristal do veu", at + Vector3.up * 1.55f, new Vector3(.7f, 1.4f, .7f), glow, false, LowPolyShape.Crystal);
                crystal.transform.localRotation = Quaternion.Euler(0, 20, 12);
                for (int side = -1; side <= 1; side += 2)
                    Facet("Véu / estandarte", at + new Vector3(side * .83f, 1.4f, .2f), new Vector3(.36f, 2.4f, .07f), wall, false, LowPolyShape.TaperedPrism);
            }
            else if (ThemeIndex == 2)
            {
                Facet("Altar / pedestal", at + Vector3.up * .5f, new Vector3(2.1f, 1, 1.2f), stone, false, LowPolyShape.BevelBox);
                Facet("Cruz das cinzas / haste", at + Vector3.up * 1.8f, new Vector3(.23f, 2.2f, .23f), bone, false, LowPolyShape.BevelBox);
                Facet("Cruz das cinzas / braco", at + Vector3.up * 2.15f, new Vector3(1.3f, .23f, .23f), bone, false, LowPolyShape.BevelBox);
                for (int candle = -1; candle <= 1; candle += 2)
                    Facet("Cinza / cristal", at + new Vector3(candle * .75f, 1.2f, -.1f), new Vector3(.22f, .7f, .22f), glow, false, LowPolyShape.Crystal);
            }
            else if (ThemeIndex == 3)
            {
                var fungus = Surface("lowpoly / fungo", new Color(.46f, .46f, .18f));
                for (int stem = -1; stem <= 1; stem++)
                {
                    Vector3 p = at + new Vector3(stem * .63f, 0, stem % 2 * .35f);
                    Facet("Fungo / talo facetado", p + Vector3.up * .55f, new Vector3(.18f, 1.1f, .18f), bone, false, LowPolyShape.Cylinder);
                    Facet("Fungo / copa facetada", p + Vector3.up * 1.15f, new Vector3(.95f, .5f, .95f), fungus, false, LowPolyShape.Cone);
                }
                Facet("Icor / faceta", at + new Vector3(0, .02f, -.7f), new Vector3(2.6f, .04f, 1.7f), glow, false, LowPolyShape.Cylinder);
            }
            else
            {
                var bronze = Surface("lowpoly / bronze", new Color(.48f, .35f, .16f));
                Facet("Sino / borda facetada", at + Vector3.up * 1.8f, new Vector3(1.65f, .22f, 1.65f), bronze, false, LowPolyShape.Cylinder);
                Facet("Sino / corpo facetado", at + Vector3.up * 2.25f, new Vector3(1.35f, .85f, 1.35f), bronze, false, LowPolyShape.TaperedPrism);
                Facet("Sino / badalo", at + Vector3.up * 1.58f, new Vector3(.18f, .4f, .18f), iron, false, LowPolyShape.Cylinder);
                Facet("Sino / corrente", at + Vector3.up * 3.35f, new Vector3(.08f, 1.4f, .08f), iron, false, LowPolyShape.Cylinder);
                for (int shard = -1; shard <= 1; shard++)
                    Facet("Cristal abissal", at + new Vector3(shard * .65f, .6f, -.9f), new Vector3(.35f, 1.3f - Mathf.Abs(shard) * .35f, .35f), glow, false, LowPolyShape.Crystal).transform.localRotation = Quaternion.Euler(shard * 14, 0, shard * 17);
            }
            Point(at + Vector3.up * 1.5f, accent, 1.7f, 7, null, 11 + ThemeIndex * 3);
        }

        GameObject Facet(string name, Vector3 position, Vector3 size, Material material, bool solid, LowPolyShape shape)
            => owner.Polygons.Part(name, position, size, material, geometry, solid, shape);

        void BuildArena()
        {
            var floorTint = ThemeIndex == 0 ? new Color(.48f, .5f, .53f) : ThemeIndex == 1 ? new Color(.43f, .4f, .57f)
                : ThemeIndex == 2 ? new Color(.56f, .39f, .38f) : ThemeIndex == 3 ? new Color(.36f, .47f, .33f) : new Color(.31f, .29f, .43f);
            var wallTint = ThemeIndex == 0 ? new Color(.45f, .43f, .52f) : ThemeIndex == 1 ? new Color(.40f, .35f, .58f)
                : ThemeIndex == 2 ? new Color(.51f, .36f, .37f) : ThemeIndex == 3 ? new Color(.34f, .43f, .30f) : new Color(.31f, .28f, .44f);
            var floor = Surface("piso", floorTint, PixelTexture("piso", 64, ThemeIndex == 0 ? 0 : 3 + ThemeIndex));
            floor.mainTextureScale = new Vector2(14, 12);
            var wall = Surface("alvenaria", wallTint, PixelTexture("parede", 32, 1));
            var trim = Surface("pedra fria", wallTint * .72f, PixelTexture("pedra", 32, 2));
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
            // Distinct physical cover plans keep the central route and the boss
            // staging area open. Perspectives share these exact colliders.
            BuildThemeCover(wall, trim, bone, wood, iron);
            if (ThemeIndex == 0 || ThemeIndex == 3)
                foreach (var p in new[] { new Vector3(-11, 0, 3.5f), new Vector3(11, 0, 3.5f), new Vector3(-11, 0, -5), new Vector3(11, 0, -5) })
                    Gravestone(p, trim, bone);
            // Decorative objects are non-solid; only cover and architecture stop movement or shots.
            for (int side = -1; side <= 1; side += 2)
            {
                Fence(new Vector3(side * 11.5f, 0, 9.8f), iron);
                for (int n = 0; n < 3; n++)
                    Cube("Tábua caída", new Vector3(side * (11.8f - n * .45f), .035f, -9.7f + n * .25f), new Vector3(.17f, .07f, 1.1f), wood, false).transform.localRotation = Quaternion.Euler(0, side * (n * 19 + 8), 0);
                if (ThemeIndex == 0)
                {
                    Pumpkin(new Vector3(side * 10.8f, .38f, -9.2f), pumpkin, black, amber);
                    Pumpkin(new Vector3(side * 12.2f, .3f, 8.5f), pumpkin, black, green);
                }
            }
            Color themeLight = ThemeIndex == 0 ? new Color(1, .47f, .13f) : ThemeIndex == 1 ? new Color(.61f, .40f, 1)
                : ThemeIndex == 2 ? new Color(1, .24f, .20f) : ThemeIndex == 3 ? new Color(.38f, 1, .31f) : new Color(.48f, .30f, 1);
            var themeFlame = ThemeIndex == 0 ? amber : Surface("chama do tema", themeLight, null, themeLight * 2);
            Candelabrum(new Vector3(-11.8f, 0, -3.5f), iron, bone, wax, themeFlame, themeLight, 1);
            Candelabrum(new Vector3(11.8f, 0, -3.5f), iron, bone, wax, themeFlame, themeLight, 4);
            Candelabrum(new Vector3(-11.8f, 0, 6.5f), iron, bone, wax, ThemeIndex == 0 ? green : themeFlame, ThemeIndex == 0 ? new Color(.35f, 1, .6f) : themeLight, 8);
            Candelabrum(new Vector3(11.8f, 0, 6.5f), iron, bone, wax, themeFlame, themeLight, 12);
            Candle(new Vector3(-2.6f, .03f, 9.9f), wax, amber, .34f);
            Candle(new Vector3(2.6f, .03f, 9.9f), wax, amber, .46f);
            Skull(new Vector3(-12.6f, .16f, -7.4f), bone, black, .28f);
            Skull(new Vector3(12.6f, .16f, 1.1f), bone, black, .24f);
            BuildThemeDecor(wall, trim, bone, iron, wood, black, themeFlame, themeLight);
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
            Point(new Vector3(-11, 1.05f, -9.2f), themeLight, 1.35f, 5.5f, null, 16);
            Point(new Vector3(11, 1.05f, -9.2f), themeLight, 1.35f, 5.5f, null, 20);
            var sunObject = new GameObject("Luz da lua"); sunObject.transform.SetParent(geometry, false);
            sunObject.transform.localRotation = Quaternion.Euler(52, -28, 0);
            var moon = sunObject.AddComponent<Light>(); moon.type = LightType.Directional;
            moon.color = ThemeIndex == 2 ? new Color(.75f, .43f, .48f) : ThemeIndex == 3 ? new Color(.44f, .66f, .54f) : new Color(.48f, .59f, .82f);
            moon.intensity = .58f - ThemeIndex * .035f;
            moon.shadows = LightShadows.Soft; moon.shadowStrength = .7f;
        }

        void BuildThemeCover(Material wall, Material trim, Material bone, Material wood, Material iron)
        {
            if (ThemeIndex == 0)
            {
                foreach (var p in new[] { new Vector3(-8, 0, -1), new Vector3(8, 0, -1), new Vector3(-9, 0, 7), new Vector3(9, 0, 7) })
                    Pillar(p, wall, trim, bone);
            }
            else if (ThemeIndex == 1)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Pillar(new Vector3(side * 8, 0, -4), wall, trim, bone);
                    Pillar(new Vector3(side * 8, 0, 5), wall, trim, bone);
                    Cube("Parede interna / alcova", new Vector3(side * 11, 1.5f, 0), new Vector3(3, 3, .45f), wall, true);
                    Cube("Coroamento / alcova", new Vector3(side * 11, 3.08f, 0), new Vector3(3.25f, .18f, .7f), trim, false);
                }
            }
            else if (ThemeIndex == 2)
            {
                for (int side = -1; side <= 1; side += 2)
                    for (int row = 0; row < 3; row++)
                    {
                        Vector3 at = new Vector3(side * 8.5f, 0, -5 + row * 6);
                        Pillar(at, wall, trim, bone);
                        // Broken ribs give the nave real height without creating
                        // a ceiling over the overhead camera or a blocked route.
                        Cube("Coroamento / arco da nave", at + new Vector3(side * -1.1f, 3.75f, 0),
                            new Vector3(2.5f, .24f, .42f), trim, false).transform.localRotation = Quaternion.Euler(0, 0, side * 15);
                    }
            }
            else if (ThemeIndex == 3)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Pillar(new Vector3(side * 9, 0, 7), wall, trim, bone);
                    for (int row = 0; row < 2; row++)
                    {
                        Vector3 at = new Vector3(side * 8.5f, 0, -4 + row * 6);
                        Cube("Cover / tronco putrido", at + Vector3.up * .42f, new Vector3(3.3f, .84f, .85f), wood, true);
                        for (int stave = -1; stave <= 1; stave++)
                            Cube("Cover / estaca", at + new Vector3(stave * 1.2f, .9f, 0), new Vector3(.18f, 1.2f, .18f), wood, false);
                    }
                }
            }
            else
            {
                for (int side = -1; side <= 1; side += 2)
                    for (int end = -1; end <= 1; end += 2)
                    {
                        var corner = Cube("Parede interna / canto abissal", new Vector3(side * 12.2f, 1.8f, end * 10.2f),
                            new Vector3(3.8f, 3.6f, .6f), wall, true);
                        corner.transform.localRotation = Quaternion.Euler(0, side * end * -45, 0);
                        Pillar(new Vector3(side * 8, 0, end * 2.2f), wall, trim, bone);
                    }
            }
        }

        void BuildThemeDecor(Material wall, Material stone, Material bone, Material iron, Material wood,
            Material dark, Material glow, Color accent)
        {
            if (ThemeIndex == 0)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 at = new Vector3(side * 12.5f, 0, 4);
                    Cube("Arvore morta / tronco", at + Vector3.up * 1.1f, new Vector3(.32f, 2.2f, .3f), wood, false);
                    for (int branch = -1; branch <= 1; branch += 2)
                        Cube("Arvore morta / galho", at + new Vector3(branch * .38f, 2.1f, 0), new Vector3(.15f, 1.15f, .16f), wood, false)
                            .transform.localRotation = Quaternion.Euler(0, 0, branch * 42);
                }
            }
            else if (ThemeIndex == 1)
            {
                var cloth = Surface("tecido dos veus", new Color(.31f, .16f, .43f));
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int panel = 0; panel < 7; panel++)
                    {
                        float length = 2.3f - Mathf.Abs(panel - 3) * .24f;
                        Cube("Parede / veu rasgado", new Vector3(side * 13.85f, 3.6f - length * .5f, -7 + panel * .42f),
                            new Vector3(.06f, length, .4f), panel % 2 == 0 ? cloth : dark, false);
                    }
                    for (int frame = 0; frame < 3; frame++)
                    {
                        Vector3 at = new Vector3(side * 13.88f, 2, 1.2f + frame * 2.4f);
                        Cube("Parede / espelho do veu", at, new Vector3(.055f, 1.35f, .86f), dark, false);
                        Cube("Parede / moldura do veu", at + new Vector3(0, .75f, 0), new Vector3(.09f, .12f, 1.05f), glow, false);
                        Skull(at + new Vector3(side * -.11f, 0, 0), bone, dark, .19f);
                    }
                }
            }
            else if (ThemeIndex == 2)
            {
                var ash = Surface("cinzas brancas", new Color(.57f, .52f, .48f));
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 at = new Vector3(side * 13.83f, 2, 2.4f);
                    Cube("Parede / vitral rubro", at, new Vector3(.08f, 2.6f, 2.4f), glow, false);
                    Cube("Parede / cruz do vitral", at + new Vector3(side * -.06f, 0, 0), new Vector3(.11f, 2.7f, .14f), iron, false);
                    Cube("Parede / braco do vitral", at + new Vector3(side * -.06f, .3f, 0), new Vector3(.11f, .12f, 2.5f), iron, false);
                    for (int heap = 0; heap < 4; heap++)
                        Cube("Cinzas / monte", new Vector3(side * (10.4f + heap * .65f), .06f, -8 + heap * .27f), new Vector3(.72f, .12f, 1.35f), ash, false);
                }
                for (int step = 0; step < 4; step++)
                    Cube("Passadeira ritual", new Vector3(0, .014f, -5.4f + step * 3.1f), new Vector3(2.3f, .018f, 2), dark, false);
            }
            else if (ThemeIndex == 3)
            {
                var cap = Surface("fungo putrido", new Color(.4f, .55f, .17f));
                var pus = Surface("icor da peste", new Color(.16f, .4f, .08f), null, new Color(.1f, .45f, .04f));
                for (int side = -1; side <= 1; side += 2)
                    for (int colony = 0; colony < 5; colony++)
                    {
                        Vector3 at = new Vector3(side * (10.8f + colony % 2), 0, -8 + colony * 3.6f);
                        Cube("Icor / margem", at + Vector3.up * .025f, new Vector3(1.5f, .045f, 2), pus, false);
                        for (int stem = 0; stem < 3; stem++)
                        {
                            Vector3 p = at + new Vector3((stem - 1) * .44f, .18f, stem * .22f);
                            Cube("Fungo / talo", p + Vector3.up * .22f, new Vector3(.13f, .45f, .13f), bone, false);
                            Cube("Fungo / chapeu", p + Vector3.up * .48f, new Vector3(.63f, .18f, .55f), cap, false);
                            Cube("Fungo / esporo", p + new Vector3(.12f, .59f, .06f), Vector3.one * .09f, glow, false);
                        }
                    }
            }
            else
            {
                var bronze = Surface("bronze funerario", new Color(.45f, .34f, .21f));
                for (int side = -1; side <= 1; side += 2)
                    for (int shrine = 0; shrine < 3; shrine++)
                    {
                        Vector3 at = new Vector3(side * 11.5f, 0, -6 + shrine * 6);
                        Cube("Santuario / pedestal", at + Vector3.up * .3f, new Vector3(1.4f, .6f, 1.4f), stone, false);
                        for (int crystal = -1; crystal <= 1; crystal++)
                        {
                            var shard = Cube("Cristal abissal", at + new Vector3(crystal * .32f, .95f, crystal * .22f), new Vector3(.22f, 1.35f - Mathf.Abs(crystal) * .45f, .24f), glow, false);
                            shard.transform.localRotation = Quaternion.Euler(crystal * 12, 0, -crystal * 18);
                        }
                        Cube("Sino / corrente", at + Vector3.up * 3, new Vector3(.055f, 1.5f, .055f), iron, false);
                        Cube("Sino / corpo", at + Vector3.up * 2.25f, new Vector3(.7f, .7f, .6f), bronze, false);
                        Cube("Sino / borda", at + Vector3.up * 1.88f, new Vector3(.92f, .13f, .79f), bronze, false);
                        Cube("Sino / badalo", at + Vector3.up * 1.71f, new Vector3(.13f, .25f, .13f), iron, false);
                    }
            }
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
            if (lowPoly) return owner.Polygons.Part(name, position, size, material, parent ? parent : geometry, solid);
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
            var template = Resources.Load<Material>(lowPoly ? "LowPolyPalette" : "FpsPalette");
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
                else if (style == 4)
                    baseShade = (x + y) % 16 == 0 || (x - y + 64) % 16 == 0 ? 53 : ((x / 8 + y / 8) % 2 == 0 ? 139 : 111);
                else if (style == 5)
                    baseShade = x % 12 == 0 || (y + x / 12 * 6) % 12 == 0 ? 51 : (x % 12 == 1 ? 157 : 132);
                else if (style == 6)
                    baseShade = x % 16 == 0 || y % 16 == 0 ? 47 : (hash % 9 < 3 ? 88 : 131);
                else if (style == 7)
                    baseShade = (x / 8 + y / 8) % 2 == 0 ? 99 : 122;
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
            if (roomDoors != null) roomDoors.Clear(); roomDoors = null;
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
