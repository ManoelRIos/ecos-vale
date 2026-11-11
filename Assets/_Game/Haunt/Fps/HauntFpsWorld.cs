using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos.Haunt.Fps
{
    public enum FpsRoomRole { Entrance, Combat, Shop, Treasure, Boss }

    /// <summary>One room of a floor. Rooms are linked by corridors; combat and boss rooms
    /// lock their doors while an encounter is active, as in classic dungeon crawlers.</summary>
    public sealed class FpsRoom
    {
        public int Id { get; internal set; }
        public FpsRoomRole Role { get; internal set; }
        public GridPoint Cell { get; internal set; }
        public Vector3 Center { get; internal set; }
        /// <summary>Half extents of the walkable interior on X and Z.</summary>
        public Vector2 HalfSize { get; internal set; }
        public EncounterKind Encounter { get; internal set; }
        public bool HasMiniBoss { get; internal set; }
        public bool Locked { get; internal set; }
        public bool Visited, Cleared;
        public readonly List<int> Neighbors = new List<int>();
        internal readonly List<FpsDoor> Doors = new List<FpsDoor>();
        public bool Contains(Vector3 point, float margin = 0) => Mathf.Abs(point.x - Center.x) <= HalfSize.x - margin
            && Mathf.Abs(point.z - Center.z) <= HalfSize.y - margin;
        public float MinX => Center.x - HalfSize.x;
        public float MaxX => Center.x + HalfSize.x;
        public float MinZ => Center.z - HalfSize.y;
        public float MaxZ => Center.z + HalfSize.y;
    }

    sealed class FpsDoor
    {
        public Transform Bars;
        public Collider Blocker;
        public float Raise, Target;
    }

    /// <summary>The shared 3D floor for both perspectives: a tree of rooms joined by
    /// corridors, built from textured blocks whose texels line up with the overhead pixel grid.
    /// Back and side walls show tall brick faces from above; front walls are lowered by
    /// <see cref="HauntFpsHybridArt"/> in the overhead view.</summary>
    public sealed class HauntFpsWorld : MonoBehaviour, IDisposable
    {
        /// <summary>One floor tile: 16 texels, two rendered pixels per texel overhead.</summary>
        public const float Tile = 1.4f;
        /// <summary>Walls stand three and a half tiles tall, so overhead the back wall shows
        /// about two tiles of brick face above its base, like classic top-down dungeon rooms.</summary>
        public const float WallHeight = Tile * 3.5f;
        /// <summary>Typical combat room size, kept for older callers.</summary>
        public const float Width = 17 * Tile, Depth = 13 * Tile;
        /// <summary>Clear height of every doorway; a brick lintel closes the wall above it in first person.</summary>
        public const float DoorHeight = Tile * 2.4f;
        const int PitchX = 28, PitchZ = 22, DoorTiles = 3;
        const int TexelsPerTile = 16;

        public int ThemeIndex { get; private set; }
        public int FloorNumber { get; private set; } = 1;
        public string ThemeName => HauntFpsCampaign.ThemeName(FloorNumber);
        public string LayoutName => ThemeIndex == 0 ? "JARDIM FUNERARIO" : ThemeIndex == 1 ? "ALCOVAS DO VEU"
            : ThemeIndex == 2 ? "NAVE DA CATEDRAL" : ThemeIndex == 3 ? "PONTES DO NINHO" : "ANEL DO ABISMO";
        public bool ExitOpen { get; private set; }
        public int GeneratedTextureCount => textures.Count;
        public int OwnedMaterialCount => materials.Count;
        public IReadOnlyList<FpsRoom> Rooms => rooms;
        public FpsRoom EntranceRoom { get; private set; }
        public FpsRoom BossRoom { get; private set; }
        public FpsRoom ShopRoom { get; private set; }
        public FpsRoom TreasureRoom { get; private set; }

        public Vector3 PlayerSpawn => EntranceRoom.Center + new Vector3(0, 0, -Tile);
        /// <summary>Where a boss-practice run begins: just inside the boss room's south door.</summary>
        public Vector3 BossDoorSpawn => new Vector3(BossRoom.Center.x, 0, BossRoom.MinZ + Tile * 1.6f);
        public Vector3 ExitPosition => new Vector3(BossRoom.Center.x, 0, BossRoom.MaxZ - Tile * 1.3f);
        public Vector3 BossSpawn => BossRoom.Center + new Vector3(0, .05f, Tile * 2);
        public Vector3 MerchantPosition => ShopRoom != null ? ShopRoom.Center + new Vector3(0, 0, ShopRoom.HalfSize.y - Tile * 1.7f) : Vector3.zero;
        public Vector3[] ShopSlots => ShopRoom == null ? Array.Empty<Vector3>() : new[]
        {
            ShopRoom.Center + new Vector3(-Tile * 3, 0, Tile * .5f), ShopRoom.Center + new Vector3(0, 0, Tile * .5f),
            ShopRoom.Center + new Vector3(Tile * 3, 0, Tile * .5f)
        };
        public Vector3 ChestPosition => TreasureRoom != null ? TreasureRoom.Center + new Vector3(0, 0, Tile * .5f) : EntranceRoom.Center;
        public Vector3[] BarrelSpawns => barrelSpots.ToArray();

        sealed class Flame
        {
            public Light Light;
            public Transform Shape;
            public float Phase, Strength;
            public Vector3 Scale;
        }
        struct Lane { public float MinX, MaxX, MinZ, MaxZ; }
        sealed class Palette
        {
            public Color32 FloorA, FloorB, FloorLight, FloorDark, Grout;
            public Color32 Brick, BrickLight, BrickDark, Mortar, Cap, Base;
            public Color32 Top, Rim, Wood, WoodDark, Cloth, ClothDark, Accent;
            public Color Torch, Ambient, Fog, Moon;
        }

        static HauntFpsWorld environmentOwner;
        readonly List<Material> materials = new List<Material>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        readonly List<Mesh> meshes = new List<Mesh>();
        readonly List<Flame> flames = new List<Flame>();
        readonly List<FpsRoom> rooms = new List<FpsRoom>();
        readonly List<Lane> corridors = new List<Lane>();
        readonly List<Vector3> barrelSpots = new List<Vector3>();
        readonly List<FpsDoor> doors = new List<FpsDoor>();
        Transform geometry, dressing;
        readonly List<Vector3> flamePoints = new List<Vector3>();
        readonly List<Vector2> wallFixtures = new List<Vector2>();
        Texture2D floorTexture, floorRelief;
        Material ceilingMat;
        float flameGlow = 1;
        GameObject seal;
        Material exitMaterial;
        Light exitLight;
        Palette palette;
        System.Random random;
        Material floorMat, wallMat, topMat, rimMat, woodMat, woodTopMat, ironMat, clothMat, shelfMat, darkMat, boneMat,
            flameMat, stoneMat, stoneTopMat, goldMat;
        float clock;
        bool initialized, disposed;
        Color previousAmbient, previousFog, previousEquator, previousGround;
        AmbientMode previousAmbientMode;
        bool previousFogEnabled;
        FogMode previousFogMode;
        float previousFogDensity, previousFogStart, previousFogEnd;

        public void Initialize(HauntFpsGame game)
        {
            if (initialized) throw new InvalidOperationException("A masmorra já foi construída.");
            if (disposed) throw new ObjectDisposedException(nameof(HauntFpsWorld));
            initialized = true;
            FloorNumber = game && game.State != null ? Mathf.Max(1, game.State.Floor) : 1;
            int seed = game && game.State != null ? game.State.Seed : 1;
            ThemeIndex = HauntFpsCampaign.ThemeIndex(FloorNumber);
            random = new System.Random(unchecked(seed * 486187739 + FloorNumber * 7919));
            geometry = new GameObject(ThemeName + " / masmorra").transform;
            geometry.SetParent(transform, false);
            // Ceilings, rafters, lintels, pilasters and chandeliers exist only for the eye-level
            // view; the overhead camera looks down through where they would be.
            dressing = new GameObject("Arquitetura da primeira pessoa").transform;
            dressing.SetParent(geometry, false);
            palette = BuildPalette(ThemeIndex);
            ConfigureAtmosphere();
            CreateMaterials();
            PlanRooms(DungeonFloor.Generate(seed, FloorNumber));
            foreach (var room in rooms) BuildRoom(room);
            BuildCorridors();
            BuildMoon();
            SetExitOpen(false);
        }

        // ----------------------------------------------------------------- queries

        /// <summary>True inside any room interior or corridor, shrunk by <paramref name="margin"/>.</summary>
        public bool IsInsideBounds(Vector3 position, float margin = 0)
        {
            foreach (var room in rooms) if (room.Contains(position, margin)) return true;
            foreach (var lane in corridors)
                if (position.x >= lane.MinX + margin && position.x <= lane.MaxX - margin
                    && position.z >= lane.MinZ + margin && position.z <= lane.MaxZ - margin) return true;
            return false;
        }

        /// <summary>The room whose interior holds the point, or null in corridors and doorways.</summary>
        public FpsRoom RoomAt(Vector3 position, float margin = 0)
        {
            foreach (var room in rooms) if (room.Contains(position, margin)) return room;
            return null;
        }

        public FpsRoom GetRoom(int id) => id >= 0 && id < rooms.Count ? rooms[id] : null;

        /// <summary>The colour of this theme's firelight.</summary>
        public Color TorchColor => palette != null ? palette.Torch : new Color(1, .62f, .28f);
        /// <summary>Where torches, braziers and candles burn (ember sources).</summary>
        public IReadOnlyList<Vector3> FlamePoints => flamePoints;
        public bool FirstPersonDressingVisible => dressing && dressing.gameObject.activeSelf;
        public int FirstPersonPieceCount => dressing ? dressing.childCount : 0;

        /// <summary>Shows the first-person-only architecture (ceilings and what hangs from them).</summary>
        public void SetFirstPersonDressing(bool visible)
        {
            if (dressing && dressing.gameObject.activeSelf != visible) dressing.gameObject.SetActive(visible);
        }

        /// <summary>Emission of every flame. Raised when bloom is available so fire glows;
        /// kept modest otherwise so it never clips to white.</summary>
        public void SetFlameGlow(float glow)
        {
            glow = Mathf.Clamp(glow, .2f, 6);
            if (Mathf.Approximately(glow, flameGlow) || !flameMat) return;
            flameGlow = glow;
            flameMat.SetColor("_EmissionColor", palette.Torch * (1.1f * glow));
        }

        /// <summary>A deterministic candidate spawn point inside the room, away from its walls.</summary>
        public Vector3 SpawnCandidate(FpsRoom room, int serial, int attempt)
        {
            float u = Fraction(serial * 0.618034f + attempt * 0.381966f + room.Id * .2134f);
            float v = Fraction(serial * 0.754877f + attempt * 0.569840f + room.Id * .7311f);
            float margin = Tile * 1.6f;
            return new Vector3(Mathf.Lerp(room.MinX + margin, room.MaxX - margin, u), .06f,
                Mathf.Lerp(room.MinZ + margin, room.MaxZ - margin, v));
        }

        static float Fraction(float value) => value - Mathf.Floor(value);

        public void SetRoomLocked(FpsRoom room, bool locked)
        {
            if (room == null) return;
            room.Locked = locked;
            foreach (var door in room.Doors)
            {
                door.Target = locked ? 1 : 0;
                if (door.Blocker) door.Blocker.enabled = locked;
            }
        }

        // ----------------------------------------------------------------- plan

        void PlanRooms(DungeonFloor plan)
        {
            // Leaves away from the entrance and the boss become the shop and the treasure room.
            var leaves = new List<DungeonRoom>();
            foreach (var source in plan.Rooms)
                if (source.Kind == RoomKind.Combat && source.Neighbors.Count == 1) leaves.Add(source);
            DungeonRoom shop = leaves.Count > 0 ? leaves[random.Next(leaves.Count)] : null;
            if (shop != null) leaves.Remove(shop);
            DungeonRoom treasure = leaves.Count > 0 ? leaves[random.Next(leaves.Count)] : null;
            foreach (var source in plan.Rooms)
            {
                var room = new FpsRoom
                {
                    Id = source.Id, Cell = source.Position, Encounter = source.Encounter, HasMiniBoss = source.HasMiniBoss,
                    Role = source.Kind == RoomKind.Entrance ? FpsRoomRole.Entrance : source.Kind == RoomKind.Boss ? FpsRoomRole.Boss
                        : source == shop ? FpsRoomRole.Shop : source == treasure ? FpsRoomRole.Treasure : FpsRoomRole.Combat,
                };
                room.Neighbors.AddRange(source.Neighbors);
                Vector2Int tiles = RoomTiles(room.Role);
                room.HalfSize = new Vector2(tiles.x * Tile * .5f, tiles.y * Tile * .5f);
                room.Center = new Vector3(source.Position.X * PitchX * Tile, 0, source.Position.Y * PitchZ * Tile);
                room.Cleared = room.Role != FpsRoomRole.Combat && room.Role != FpsRoomRole.Boss;
                rooms.Add(room);
                if (room.Role == FpsRoomRole.Entrance) EntranceRoom = room;
                else if (room.Role == FpsRoomRole.Boss) BossRoom = room;
                else if (room.Role == FpsRoomRole.Shop) ShopRoom = room;
                else if (room.Role == FpsRoomRole.Treasure) TreasureRoom = room;
            }
            EntranceRoom.Visited = true;
        }

        Vector2Int RoomTiles(FpsRoomRole role)
        {
            switch (role)
            {
                case FpsRoomRole.Entrance: return new Vector2Int(13, 11);
                case FpsRoomRole.Shop: return new Vector2Int(15, 11);
                case FpsRoomRole.Treasure: return new Vector2Int(11, 9);
                case FpsRoomRole.Boss: return new Vector2Int(23, 17);
                default:
                    var options = new[] { new Vector2Int(15, 11), new Vector2Int(17, 13), new Vector2Int(19, 13),
                        new Vector2Int(13, 13), new Vector2Int(21, 15), new Vector2Int(17, 11) };
                    return options[random.Next(options.Length)];
            }
        }

        bool HasNeighbor(FpsRoom room, int dx, int dy)
        {
            foreach (int id in room.Neighbors)
            {
                var other = rooms[id];
                if (other.Cell.X == room.Cell.X + dx && other.Cell.Y == room.Cell.Y + dy) return true;
            }
            return false;
        }

        // ----------------------------------------------------------------- rooms

        void BuildRoom(FpsRoom room)
        {
            float x0 = room.MinX, x1 = room.MaxX, z0 = room.MinZ, z1 = room.MaxZ;
            var node = new GameObject("Sala " + room.Id + " / " + room.Role).transform; node.SetParent(geometry, false);
            // The floor is laid in slabs of a few tiles so each one is lit by the lights near it.
            int slabsX = Mathf.Max(1, Mathf.RoundToInt((x1 - x0 + Tile * 2) / (Tile * 7))), slabsZ = Mathf.Max(1, Mathf.RoundToInt((z1 - z0 + Tile * 2) / (Tile * 7)));
            for (int ix = 0; ix < slabsX; ix++) for (int iz = 0; iz < slabsZ; iz++)
                Block("Piso da sala", new Vector3(Mathf.Lerp(x0 - Tile, x1 + Tile, ix / (float)slabsX), -.5f, Mathf.Lerp(z0 - Tile, z1 + Tile, iz / (float)slabsZ)),
                    new Vector3(Mathf.Lerp(x0 - Tile, x1 + Tile, (ix + 1) / (float)slabsX), 0, Mathf.Lerp(z0 - Tile, z1 + Tile, (iz + 1) / (float)slabsZ)), darkMat, floorMat, true, node);
            wallFixtures.Clear();
            bool north = HasNeighbor(room, 0, 1), south = HasNeighbor(room, 0, -1);
            bool west = HasNeighbor(room, -1, 0), east = HasNeighbor(room, 1, 0);
            float door = DoorTiles * Tile * .5f;
            // North and south walls span the corners; west and east walls fit between them.
            WallRun("Parede fundo", x0 - Tile, x1 + Tile, z1, z1 + Tile, true, north ? room.Center.x : float.NaN, door, node, false);
            WallRun("Parede frente", x0 - Tile, x1 + Tile, z0 - Tile, z0, true, south ? room.Center.x : float.NaN, door, node, true);
            WallRun("Parede lateral", x0 - Tile, x0, z0, z1, false, west ? room.Center.z : float.NaN, door, node, false);
            WallRun("Parede lateral", x1, x1 + Tile, z0, z1, false, east ? room.Center.z : float.NaN, door, node, false);
            if (north) AddDoor(room, new Vector3(room.Center.x, 0, z1 + Tile * .5f), true, node);
            if (south) AddDoor(room, new Vector3(room.Center.x, 0, z0 - Tile * .5f), true, node);
            if (west) AddDoor(room, new Vector3(x0 - Tile * .5f, 0, room.Center.z), false, node);
            if (east) AddDoor(room, new Vector3(x1 + Tile * .5f, 0, room.Center.z), false, node);
            Torches(room, node, north);
            switch (room.Role)
            {
                case FpsRoomRole.Entrance: DecorateEntrance(room, node); break;
                case FpsRoomRole.Shop: DecorateShop(room, node); break;
                case FpsRoomRole.Treasure: DecorateTreasure(room, node); break;
                case FpsRoomRole.Boss: DecorateBoss(room, node); break;
                default: DecorateCombat(room, node, north, south, west, east); break;
            }
            DressRoom(room, north, south, west, east);
        }

        // ----------------------------------------------------------------- first-person architecture

        /// <summary>A block that belongs to the eye-level view only.</summary>
        GameObject Dress(string name, Vector3 min, Vector3 max, Material sides, Material top, float faceHeight = 0, bool castShadows = false)
        {
            var node = Block(name, min, max, sides, top, false, dressing, faceHeight);
            var renderer = node.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return node;
        }

        void DressRoom(FpsRoom room, bool north, bool south, bool west, bool east)
        {
            float x0 = room.MinX, x1 = room.MaxX, z0 = room.MinZ, z1 = room.MaxZ, door = DoorTiles * Tile * .5f;
            Vector3 c = room.Center;
            // Stone ceiling over the room and its walls, with timber rafters across it.
            Dress("Teto", new Vector3(x0 - Tile, WallHeight, z0 - Tile), new Vector3(x1 + Tile, WallHeight + .3f, z1 + Tile), ceilingMat, darkMat);
            int rafters = Mathf.Max(2, Mathf.RoundToInt((z1 - z0) / (Tile * 3)));
            for (int i = 0; i < rafters; i++)
            {
                float z = Mathf.Lerp(z0, z1, (i + .5f) / rafters);
                Dress("Viga", new Vector3(x0, WallHeight - .36f, z - .17f), new Vector3(x1, WallHeight, z + .17f), woodMat, woodMat);
                for (int side = 0; side < 2; side++)
                {
                    float x = side == 0 ? x0 : x1 - .3f;
                    Dress("Viga / mísula", new Vector3(x, WallHeight - .7f, z - .13f), new Vector3(x + .3f, WallHeight - .36f, z + .13f), stoneMat, stoneMat);
                }
            }
            // Brick lintels close the walls above every doorway.
            if (north) Lintel(new Vector3(c.x, 0, z1 + Tile * .5f), true, door);
            if (south) Lintel(new Vector3(c.x, 0, z0 - Tile * .5f), true, door);
            if (west) Lintel(new Vector3(x0 - Tile * .5f, 0, c.z), false, door);
            if (east) Lintel(new Vector3(x1 + Tile * .5f, 0, c.z), false, door);
            // Stone pilasters break up the long walls.
            int alongX = Mathf.RoundToInt((x1 - x0) / (Tile * 4)), alongZ = Mathf.RoundToInt((z1 - z0) / (Tile * 4));
            for (int i = 1; i < alongX; i++)
            {
                float x = Mathf.Lerp(x0, x1, i / (float)alongX);
                bool atDoor = Mathf.Abs(x - c.x) < door + .7f;
                if (!(north && atDoor) && !NearFixture(x)) Pilaster(new Vector3(x, 0, z1), Vector3.back);
                if (!(south && atDoor)) Pilaster(new Vector3(x, 0, z0), Vector3.forward);
            }
            for (int i = 1; i < alongZ; i++)
            {
                float z = Mathf.Lerp(z0, z1, i / (float)alongZ);
                bool atDoor = Mathf.Abs(z - c.z) < door + .7f;
                if (!(west && atDoor)) Pilaster(new Vector3(x0, 0, z), Vector3.right);
                if (!(east && atDoor)) Pilaster(new Vector3(x1, 0, z), Vector3.left);
            }
            Chandelier(c, room.Role == FpsRoomRole.Boss ? 1.5f : 1);
        }

        bool NearFixture(float x)
        {
            foreach (Vector2 fixture in wallFixtures) if (Mathf.Abs(x - fixture.x) < fixture.y + .55f) return true;
            return false;
        }

        /// <summary>Brick above a doorway, with a stone course and keystone under it.</summary>
        void Lintel(Vector3 at, bool alongX, float half)
        {
            Vector3 axis = alongX ? Vector3.right : Vector3.forward, depth = alongX ? Vector3.forward : Vector3.right;
            Vector3 thick = depth * (Tile * .5f);
            Dress("Verga", Min(at - axis * half - thick + Vector3.up * DoorHeight, at + axis * half + thick + Vector3.up * WallHeight),
                Max(at - axis * half - thick + Vector3.up * DoorHeight, at + axis * half + thick + Vector3.up * WallHeight), wallMat, topMat, WallHeight, true);
            Vector3 proud = depth * (Tile * .5f + .05f);
            Dress("Verga / fiada", Min(at - axis * half - proud + Vector3.up * (DoorHeight - .16f), at + axis * half + proud + Vector3.up * DoorHeight),
                Max(at - axis * half - proud + Vector3.up * (DoorHeight - .16f), at + axis * half + proud + Vector3.up * DoorHeight), stoneMat, stoneMat);
            Vector3 prouder = depth * (Tile * .5f + .09f);
            Dress("Verga / chave", Min(at - axis * .3f - prouder + Vector3.up * (DoorHeight - .3f), at + axis * .3f + prouder + Vector3.up * (DoorHeight + .5f)),
                Max(at - axis * .3f - prouder + Vector3.up * (DoorHeight - .3f), at + axis * .3f + prouder + Vector3.up * (DoorHeight + .5f)), stoneMat, stoneTopMat);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 jamb = at + axis * (side * (half - .15f));
                Dress("Verga / imposta", Min(jamb - axis * .15f - proud + Vector3.up * (DoorHeight - .5f), jamb + axis * .15f + proud + Vector3.up * (DoorHeight - .16f)),
                    Max(jamb - axis * .15f - proud + Vector3.up * (DoorHeight - .5f), jamb + axis * .15f + proud + Vector3.up * (DoorHeight - .16f)), stoneMat, stoneMat);
            }
        }

        static Vector3 Min(Vector3 a, Vector3 b) => Vector3.Min(a, b);
        static Vector3 Max(Vector3 a, Vector3 b) => Vector3.Max(a, b);

        /// <summary>A shallow stone column against a wall: plinth, shaft and capital.</summary>
        void Pilaster(Vector3 foot, Vector3 inward)
        {
            Vector3 across = new Vector3(Mathf.Abs(inward.z), 0, Mathf.Abs(inward.x));
            float top = WallHeight - .36f;
            void Piece(string name, float half, float outBy, float y0, float y1, Material sides, Material cap)
            {
                Vector3 a = foot - across * half + Vector3.up * y0, b = foot + across * half + inward * outBy + Vector3.up * y1;
                Dress(name, Min(a, b), Max(a, b), sides, cap, 0, true);
            }
            Piece("Pilastra / base", .5f, .3f, 0, .42f, stoneMat, stoneTopMat);
            Piece("Pilastra / fuste", .38f, .2f, .42f, top - .32f, stoneMat, stoneTopMat);
            Piece("Pilastra / capitel", .5f, .3f, top - .32f, top, stoneMat, stoneTopMat);
        }

        /// <summary>An iron ring of candles hanging from the ceiling; it carries the room's fill light.</summary>
        void Chandelier(Vector3 center, float scale)
        {
            float y = WallHeight * .68f, r = .62f * scale, bar = .05f;
            Dress("Lustre / corrente", new Vector3(center.x - .025f, y + .1f, center.z - .025f), new Vector3(center.x + .025f, WallHeight, center.z + .025f), ironMat, ironMat);
            Dress("Lustre / aro", new Vector3(center.x - r, y, center.z - r - bar), new Vector3(center.x + r, y + .09f, center.z - r + bar), ironMat, ironMat);
            Dress("Lustre / aro", new Vector3(center.x - r, y, center.z + r - bar), new Vector3(center.x + r, y + .09f, center.z + r + bar), ironMat, ironMat);
            Dress("Lustre / aro", new Vector3(center.x - r - bar, y, center.z - r), new Vector3(center.x - r + bar, y + .09f, center.z + r), ironMat, ironMat);
            Dress("Lustre / aro", new Vector3(center.x + r - bar, y, center.z - r), new Vector3(center.x + r + bar, y + .09f, center.z + r), ironMat, ironMat);
            Dress("Lustre / travessa", new Vector3(center.x - r, y + .02f, center.z - bar), new Vector3(center.x + r, y + .07f, center.z + bar), ironMat, ironMat);
            Dress("Lustre / travessa", new Vector3(center.x - bar, y + .02f, center.z - r), new Vector3(center.x + bar, y + .07f, center.z + r), ironMat, ironMat);
            for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
            {
                Vector3 at = new Vector3(center.x + sx * r, y + .09f, center.z + sz * r);
                Dress("Lustre / vela", at + new Vector3(-.04f, 0, -.04f), at + new Vector3(.04f, .15f, .04f), boneMat, boneMat);
                Transform fire = Dress("Lustre / chama", at + new Vector3(-.035f, .15f, -.035f), at + new Vector3(.035f, .27f, .035f), flameMat, flameMat).transform;
                flames.Add(new Flame { Shape = fire, Scale = fire.localScale, Phase = at.x * .73f + at.z * 1.31f });
                flamePoints.Add(at + Vector3.up * .3f);
            }
        }

        /// <summary>A straight wall from min to max with an optional centered doorway.</summary>
        void WallRun(string name, float minX, float maxX, float minZ, float maxZ, bool alongX, float gapCenter, float gapHalf,
            Transform parent, bool front)
        {
            if (float.IsNaN(gapCenter))
            {
                WallBlock(name, new Vector3(minX, 0, minZ), new Vector3(maxX, WallHeight, maxZ), parent, front, alongX);
                return;
            }
            if (alongX)
            {
                WallBlock(name, new Vector3(minX, 0, minZ), new Vector3(gapCenter - gapHalf, WallHeight, maxZ), parent, front, true);
                WallBlock(name, new Vector3(gapCenter + gapHalf, 0, minZ), new Vector3(maxX, WallHeight, maxZ), parent, front, true);
            }
            else
            {
                WallBlock(name, new Vector3(minX, 0, minZ), new Vector3(maxX, WallHeight, gapCenter - gapHalf), parent, front, false);
                WallBlock(name, new Vector3(minX, 0, gapCenter + gapHalf), new Vector3(maxX, WallHeight, maxZ), parent, front, false);
            }
        }

        void WallBlock(string name, Vector3 min, Vector3 max, Transform parent, bool front, bool alongX)
        {
            if (max.x - min.x < .01f || max.z - min.z < .01f) return;
            Block(name, min, max, wallMat, topMat, true, parent, WallHeight);
            // A light rim outlines the top of every wall, the signature of layered top-down rooms.
            float rim = .09f;
            string rimName = front ? "Coroamento frente" : "Coroamento";
            if (alongX)
            {
                Block(rimName, new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y + .03f, min.z + rim), rimMat, rimMat, false, parent);
                Block(rimName, new Vector3(min.x, max.y, max.z - rim), new Vector3(max.x, max.y + .03f, max.z), rimMat, rimMat, false, parent);
            }
            else
            {
                Block(rimName, new Vector3(min.x, max.y, min.z), new Vector3(min.x + rim, max.y + .03f, max.z), rimMat, rimMat, false, parent);
                Block(rimName, new Vector3(max.x - rim, max.y, min.z), new Vector3(max.x, max.y + .03f, max.z), rimMat, rimMat, false, parent);
            }
        }

        void AddDoor(FpsRoom room, Vector3 at, bool alongX, Transform parent)
        {
            var root = new GameObject("Porta / grade").transform; root.SetParent(parent, false); root.position = at;
            var bars = new GameObject("Porta / barras").transform; bars.SetParent(root, false);
            float half = DoorTiles * Tile * .5f;
            Vector3 axis = alongX ? Vector3.right : Vector3.forward, depth = alongX ? Vector3.forward : Vector3.right;
            for (int i = 0; i < 7; i++)
            {
                float offset = -half + (i + .5f) * (half * 2 / 7);
                Vector3 c = axis * offset + Vector3.up * (WallHeight * .45f);
                Block("Porta / barra", c - axis * .06f - depth * .06f - Vector3.up * WallHeight * .45f,
                    c + axis * .06f + depth * .06f + Vector3.up * WallHeight * .45f, ironMat, ironMat, false, bars, 0, true);
            }
            Block("Porta / travessa", -axis * half - depth * .09f + Vector3.up * (WallHeight * .55f),
                axis * half + depth * .09f + Vector3.up * (WallHeight * .55f + .14f), ironMat, ironMat, false, bars, 0, true);
            var blocker = root.gameObject.AddComponent<BoxCollider>();
            blocker.center = Vector3.up * WallHeight * .5f;
            blocker.size = axis * half * 2 + depth * Tile + Vector3.up * WallHeight;
            blocker.size = new Vector3(Mathf.Abs(blocker.size.x), blocker.size.y, Mathf.Abs(blocker.size.z));
            blocker.enabled = false;
            var door = new FpsDoor { Bars = bars, Blocker = blocker, Raise = 0, Target = 0 };
            bars.localPosition = Vector3.down * (WallHeight + .2f);
            bars.gameObject.SetActive(false);
            room.Doors.Add(door); doors.Add(door);
        }

        void Torches(FpsRoom room, Transform parent, bool northDoor)
        {
            float span = room.HalfSize.x * 2;
            int count = Mathf.Max(2, Mathf.RoundToInt(span / (Tile * 5)));
            for (int i = 0; i < count; i++)
            {
                float t = (i + .5f) / count;
                float x = Mathf.Lerp(room.MinX, room.MaxX, t);
                // Keep the sconces clear of the north doorway.
                if (northDoor && Mathf.Abs(x - room.Center.x) < Tile * 2.2f) x += x < room.Center.x ? -Tile * 1.6f : Tile * 1.6f;
                Vector3 at = new Vector3(x, WallHeight * .62f, room.MaxZ - .02f);
                Block("Tocha / suporte", at + new Vector3(-.12f, -.32f, -.28f), at + new Vector3(.12f, -.12f, 0), ironMat, ironMat, false, parent);
                Block("Tocha / haste", at + new Vector3(-.07f, -.2f, -.3f), at + new Vector3(.07f, .08f, -.16f), woodMat, woodMat, false, parent);
                var flame = Block("Tocha / chama", at + new Vector3(-.11f, .08f, -.33f), at + new Vector3(.11f, .38f, -.13f), flameMat, flameMat, false, parent).transform;
                Point(at + new Vector3(0, .1f, -.9f), palette.Torch, 2.3f, Tile * 6.5f, flame, room.Id * 3.7f + i * 1.9f);
                flamePoints.Add(at + new Vector3(0, .4f, -.23f));
            }
            // The chandelier's glow: a fill light under the rafters keeps the room readable
            // between the torches.
            Point(room.Center + Vector3.up * WallHeight * .8f, palette.Torch * .55f + new Color(.15f, .15f, .2f), 1.3f,
                Mathf.Max(room.HalfSize.x, room.HalfSize.y) * 2.1f, null, room.Id * 5.1f);
        }

        // ----------------------------------------------------------------- dressing

        void DecorateEntrance(FpsRoom room, Transform parent)
        {
            Rug(room.Center + new Vector3(0, 0, -Tile * .5f), 5, 3, parent);
            Bookshelf(new Vector3(room.MinX + Tile * 2.2f, 0, room.MaxZ), parent);
            Bookshelf(new Vector3(room.MaxX - Tile * 2.2f, 0, room.MaxZ), parent);
            Crate(new Vector3(room.MinX + Tile * .9f, 0, room.MinZ + Tile * .9f), parent);
            Crate(new Vector3(room.MinX + Tile * 1.9f, 0, room.MinZ + Tile * .9f), parent);
            Pot(new Vector3(room.MaxX - Tile * .9f, 0, room.MinZ + Tile * .9f), parent);
            Pot(new Vector3(room.MaxX - Tile * .9f, 0, room.MinZ + Tile * 1.9f), parent);
        }

        void DecorateShop(FpsRoom room, Transform parent)
        {
            Rug(room.Center + new Vector3(0, 0, -Tile * .5f), 9, 5, parent);
            Vector3 counter = room.Center + new Vector3(0, 0, room.HalfSize.y - Tile * 2.6f);
            Block("Loja / balcao", counter + new Vector3(-Tile * 3, 0, -Tile * .45f), counter + new Vector3(Tile * 3, Tile * .75f, Tile * .45f), woodMat, woodTopMat, true, parent);
            Block("Loja / tampo", counter + new Vector3(-Tile * 3.1f, Tile * .75f, -Tile * .5f), counter + new Vector3(Tile * 3.1f, Tile * .82f, Tile * .5f), woodTopMat, woodTopMat, false, parent);
            for (int side = -1; side <= 1; side += 2)
            {
                Bookshelf(new Vector3(room.Center.x + side * Tile * 4.6f, 0, room.MaxZ), parent);
                Crate(new Vector3(room.Center.x + side * (room.HalfSize.x - Tile * .9f), 0, room.MinZ + Tile * 1.2f), parent);
            }
            foreach (Vector3 slot in ShopSlots)
            {
                Block("Loja / pedestal", slot + new Vector3(-Tile * .45f, 0, -Tile * .45f), slot + new Vector3(Tile * .45f, Tile * .55f, Tile * .45f), stoneMat, stoneTopMat, true, parent);
                Block("Loja / pano", slot + new Vector3(-Tile * .5f, Tile * .55f, -Tile * .5f), slot + new Vector3(Tile * .5f, Tile * .6f, Tile * .5f), clothMat, clothMat, false, parent);
            }
            Point(room.Center + new Vector3(0, WallHeight * .74f, Tile * 2), new Color(1, .82f, .55f), 1.6f, Tile * 10, null, 41);
        }

        void DecorateTreasure(FpsRoom room, Transform parent)
        {
            Rug(room.Center + new Vector3(0, 0, Tile * .5f), 5, 3, parent);
            for (int side = -1; side <= 1; side += 2)
            {
                Pillar(room.Center + new Vector3(side * Tile * 3, 0, Tile * 2), parent, Tile * .8f);
                Brazier(room.Center + new Vector3(side * Tile * 2, 0, -Tile * 1.5f), parent);
            }
        }

        void DecorateBoss(FpsRoom room, Transform parent)
        {
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    Pillar(room.Center + new Vector3(sx * (room.HalfSize.x - Tile * 3.5f), 0, sz * (room.HalfSize.y - Tile * 3.5f)), parent, Tile);
                    Brazier(room.Center + new Vector3(sx * (room.HalfSize.x - Tile * 1.2f), 0, sz * (room.HalfSize.y - Tile * 1.2f)), parent);
                }
            Rug(room.Center + new Vector3(0, 0, -Tile * 2), 3, 9, parent);
            for (int side = -1; side <= 1; side += 2) Banner(new Vector3(room.Center.x + side * Tile * 4, 0, room.MaxZ), parent);
            BuildExit(parent);
        }

        void DecorateCombat(FpsRoom room, Transform parent, bool north, bool south, bool west, bool east)
        {
            int layout = random.Next(5);
            float hx = room.HalfSize.x, hz = room.HalfSize.y;
            Vector3 c = room.Center;
            switch (layout)
            {
                case 0:
                    for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                        Pillar(c + new Vector3(sx * Snap(hx * .5f), 0, sz * Snap(hz * .45f)), parent, Tile);
                    break;
                case 1:
                    for (int sx = -1; sx <= 1; sx += 2)
                    {
                        Crate(c + new Vector3(sx * Snap(hx * .45f), 0, Tile), parent);
                        Crate(c + new Vector3(sx * Snap(hx * .45f) + sx * Tile, 0, Tile), parent);
                        Crate(c + new Vector3(sx * Snap(hx * .45f), 0, 0), parent);
                        Crate(c + new Vector3(-sx * Snap(hx * .3f), 0, -Snap(hz * .45f)), parent);
                    }
                    break;
                case 2:
                    for (int sx = -1; sx <= 1; sx += 2)
                    {
                        Table(c + new Vector3(sx * Snap(hx * .42f), 0, Snap(hz * .3f)), parent);
                        Table(c + new Vector3(sx * Snap(hx * .42f), 0, -Snap(hz * .35f)), parent);
                    }
                    break;
                case 3:
                    Pillar(c + new Vector3(-Tile, 0, 0), parent, Tile);
                    Pillar(c + new Vector3(Tile, 0, 0), parent, Tile);
                    for (int sx = -1; sx <= 1; sx += 2) Crate(c + new Vector3(sx * Snap(hx * .6f), 0, -Snap(hz * .4f)), parent);
                    break;
                default:
                    for (int sx = -1; sx <= 1; sx += 2)
                    {
                        Pillar(c + new Vector3(sx * Snap(hx * .55f), 0, Snap(hz * .4f)), parent, Tile);
                        Table(c + new Vector3(sx * Snap(hx * .3f), 0, -Snap(hz * .3f)), parent);
                    }
                    break;
            }
            // Furniture against the back wall, kept out of the north doorway.
            for (int side = -1; side <= 1; side += 2)
            {
                float x = c.x + side * Snap(hx * .7f);
                if (random.Next(2) == 0) Bookshelf(new Vector3(x, 0, room.MaxZ), parent); else Banner(new Vector3(x, 0, room.MaxZ), parent);
            }
            Pot(new Vector3(room.MinX + Tile * .8f, 0, room.MinZ + Tile * .8f), parent);
            Pot(new Vector3(room.MaxX - Tile * .8f, 0, room.MaxZ - Tile * .8f), parent);
            Bones(c + new Vector3(Snap(hx * .2f), 0, -Snap(hz * .6f)), parent);
            barrelSpots.Add(new Vector3(room.MinX + Tile * 1.1f, 0, room.MaxZ - Tile * 1.6f));
            if (room.HalfSize.x > Tile * 8) barrelSpots.Add(new Vector3(room.MaxX - Tile * 1.1f, 0, room.MinZ + Tile * 1.6f));
        }

        static float Snap(float value) => Mathf.Round(value / Tile) * Tile;

        void Pillar(Vector3 at, Transform parent, float size)
        {
            float h = size * .5f;
            Block("Pilar / corpo", at + new Vector3(-h, 0, -h), at + new Vector3(h, WallHeight * .85f, h), wallMat, topMat, true, parent, WallHeight);
            Block("Pilar / capitel", at + new Vector3(-h - .08f, WallHeight * .85f, -h - .08f), at + new Vector3(h + .08f, WallHeight * .85f + .14f, h + .08f), rimMat, rimMat, false, parent);
        }

        void Crate(Vector3 at, Transform parent)
        {
            float h = Tile * .45f;
            Block("Caixa", at + new Vector3(-h, 0, -h), at + new Vector3(h, Tile * .85f, h), woodMat, woodTopMat, true, parent);
        }

        void Table(Vector3 at, Transform parent)
        {
            Block("Mesa / tampo", at + new Vector3(-Tile, Tile * .5f, -Tile * .45f), at + new Vector3(Tile, Tile * .62f, Tile * .45f), woodTopMat, woodTopMat, true, parent);
            Block("Mesa / saia", at + new Vector3(-Tile * .95f, Tile * .2f, -Tile * .4f), at + new Vector3(Tile * .95f, Tile * .5f, Tile * .4f), woodMat, woodMat, true, parent);
            for (int s = -1; s <= 1; s += 2)
                Block("Mesa / perna", at + new Vector3(s * Tile * .85f - .07f, 0, -.07f), at + new Vector3(s * Tile * .85f + .07f, Tile * .2f, .07f), woodMat, woodMat, false, parent);
        }

        void Bookshelf(Vector3 wallPoint, Transform parent)
        {
            Vector3 at = wallPoint - new Vector3(0, 0, Tile * .35f);
            wallFixtures.Add(new Vector2(wallPoint.x, Tile));
            Block("Estante", at + new Vector3(-Tile, 0, -Tile * .3f), at + new Vector3(Tile, Tile * 1.9f, Tile * .3f), woodMat, woodTopMat, true, parent);
            Block("Estante / livros", at + new Vector3(-Tile * .9f, Tile * .15f, -Tile * .32f), at + new Vector3(Tile * .9f, Tile * 1.8f, -Tile * .3f), shelfMat, shelfMat, false, parent);
        }

        void Banner(Vector3 wallPoint, Transform parent)
        {
            Vector3 at = wallPoint - new Vector3(0, 0, .04f);
            wallFixtures.Add(new Vector2(wallPoint.x, Tile * .6f));
            Block("Estandarte / haste", at + new Vector3(-Tile * .6f, WallHeight * .86f, -.08f), at + new Vector3(Tile * .6f, WallHeight * .86f + .08f, 0), ironMat, ironMat, false, parent);
            Block("Estandarte / pano", at + new Vector3(-Tile * .5f, WallHeight * .3f, -.06f), at + new Vector3(Tile * .5f, WallHeight * .86f, -.02f), clothMat, clothMat, false, parent);
        }

        void Pot(Vector3 at, Transform parent)
        {
            float h = Tile * .28f;
            Block("Vaso", at + new Vector3(-h, 0, -h), at + new Vector3(h, Tile * .55f, h), stoneMat, stoneTopMat, true, parent);
            Block("Vaso / borda", at + new Vector3(-h * .7f, Tile * .55f, -h * .7f), at + new Vector3(h * .7f, Tile * .65f, h * .7f), darkMat, darkMat, false, parent);
        }

        void Bones(Vector3 at, Transform parent)
        {
            Block("Ossos", at + new Vector3(-.3f, 0, -.05f), at + new Vector3(.3f, .06f, .05f), boneMat, boneMat, false, parent);
            var b = Block("Ossos", at + new Vector3(-.25f, 0, -.04f), at + new Vector3(.25f, .06f, .04f), boneMat, boneMat, false, parent);
            b.transform.RotateAround(at, Vector3.up, 60);
            Block("Cranio", at + new Vector3(.2f, 0, .1f), at + new Vector3(.45f, .2f, .32f), boneMat, boneMat, false, parent);
        }

        void Rug(Vector3 at, int tilesX, int tilesZ, Transform parent)
        {
            // Each rug owns a texture of its exact size, so its border frames the whole rug.
            var material = Surface("tapete " + tilesX + "x" + tilesZ, Color.white, RugTexture(tilesX, tilesZ), 1);
            material.mainTextureScale = new Vector2(1f / tilesX, 1f / tilesZ);
            if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", material.mainTextureScale);
            Block("Tapete", at - new Vector3(tilesX * Tile * .5f, 0, tilesZ * Tile * .5f), at + new Vector3(tilesX * Tile * .5f, .02f, tilesZ * Tile * .5f),
                darkMat, material, false, parent, 0, false, true);
        }

        void Brazier(Vector3 at, Transform parent)
        {
            float h = Tile * .35f;
            Block("Braseiro / pe", at + new Vector3(-h * .4f, 0, -h * .4f), at + new Vector3(h * .4f, Tile * .6f, h * .4f), ironMat, ironMat, true, parent);
            Block("Braseiro / bacia", at + new Vector3(-h, Tile * .6f, -h), at + new Vector3(h, Tile * .85f, h), ironMat, darkMat, false, parent);
            var fire = Block("Braseiro / fogo", at + new Vector3(-h * .7f, Tile * .85f, -h * .7f), at + new Vector3(h * .7f, Tile * 1.2f, h * .7f), flameMat, flameMat, false, parent).transform;
            Point(at + Vector3.up * Tile * 1.6f, palette.Torch, 2.6f, Tile * 7, fire, at.x * .37f + at.z);
            flamePoints.Add(at + Vector3.up * Tile * 1.25f);
        }

        void BuildExit(Transform parent)
        {
            Vector3 center = ExitPosition;
            Block("Portal / pilar esquerdo", center + new Vector3(-Tile * 1.6f, 0, -.35f), center + new Vector3(-Tile * 1.1f, WallHeight * .95f, .35f), stoneMat, stoneTopMat, true, parent);
            Block("Portal / pilar direito", center + new Vector3(Tile * 1.1f, 0, -.35f), center + new Vector3(Tile * 1.6f, WallHeight * .95f, .35f), stoneMat, stoneTopMat, true, parent);
            Block("Portal / verga", center + new Vector3(-Tile * 1.75f, WallHeight * .95f, -.42f), center + new Vector3(Tile * 1.75f, WallHeight * 1.08f, .42f), stoneMat, rimMat, false, parent);
            seal = Block("Selo fechado", center + new Vector3(-Tile * 1.1f, 0, -.08f), center + new Vector3(Tile * 1.1f, WallHeight * .95f, .08f), exitMaterial, exitMaterial, true, parent);
            exitLight = Point(center + new Vector3(0, WallHeight * .6f, -1.2f), new Color(.5f, .2f, .9f), 1.6f, Tile * 6, null, 27);
        }

        void BuildCorridors()
        {
            float half = DoorTiles * Tile * .5f;
            foreach (var room in rooms)
                foreach (int id in room.Neighbors)
                {
                    var other = rooms[id];
                    if (other.Id < room.Id) continue;
                    var node = new GameObject("Corredor " + room.Id + "-" + other.Id).transform; node.SetParent(geometry, false);
                    if (other.Cell.Y == room.Cell.Y)
                    {
                        var west = other.Cell.X > room.Cell.X ? room : other; var east = west == room ? other : room;
                        float a = west.MaxX + Tile, b = east.MinX - Tile, z = west.Center.z;
                        Block("Piso do corredor", new Vector3(a - Tile, -.5f, z - half - Tile), new Vector3(b + Tile, 0, z + half + Tile), darkMat, floorMat, true, node);
                        WallBlock("Parede fundo", new Vector3(a, 0, z + half), new Vector3(b, WallHeight, z + half + Tile), node, false, true);
                        WallBlock("Parede frente", new Vector3(a, 0, z - half - Tile), new Vector3(b, WallHeight, z - half), node, true, true);
                        corridors.Add(new Lane { MinX = a - Tile, MaxX = b + Tile, MinZ = z - half, MaxZ = z + half });
                        Dress("Teto do corredor", new Vector3(a, WallHeight, z - half - Tile), new Vector3(b, WallHeight + .3f, z + half + Tile), ceilingMat, darkMat);
                        Dress("Viga", new Vector3((a + b) * .5f - .17f, WallHeight - .36f, z - half), new Vector3((a + b) * .5f + .17f, WallHeight, z + half), woodMat, woodMat);
                        Point(new Vector3((a + b) * .5f, WallHeight * .8f, z), palette.Torch * .8f, 1.1f, Tile * 4, null, a);
                    }
                    else
                    {
                        var south = other.Cell.Y > room.Cell.Y ? room : other; var north = south == room ? other : room;
                        float a = south.MaxZ + Tile, b = north.MinZ - Tile, x = south.Center.x;
                        Block("Piso do corredor", new Vector3(x - half - Tile, -.5f, a - Tile), new Vector3(x + half + Tile, 0, b + Tile), darkMat, floorMat, true, node);
                        WallBlock("Parede lateral", new Vector3(x - half - Tile, 0, a), new Vector3(x - half, WallHeight, b), node, false, false);
                        WallBlock("Parede lateral", new Vector3(x + half, 0, a), new Vector3(x + half + Tile, WallHeight, b), node, false, false);
                        corridors.Add(new Lane { MinX = x - half, MaxX = x + half, MinZ = a - Tile, MaxZ = b + Tile });
                        Dress("Teto do corredor", new Vector3(x - half - Tile, WallHeight, a), new Vector3(x + half + Tile, WallHeight + .3f, b), ceilingMat, darkMat);
                        Dress("Viga", new Vector3(x - half, WallHeight - .36f, (a + b) * .5f - .17f), new Vector3(x + half, WallHeight, (a + b) * .5f + .17f), woodMat, woodMat);
                        Point(new Vector3(x, WallHeight * .8f, (a + b) * .5f), palette.Torch * .8f, 1.1f, Tile * 4, null, b);
                    }
                }
        }

        void BuildMoon()
        {
            // A high key light from the upper left throws every shadow down and to the right.
            var sunObject = new GameObject("Luz principal"); sunObject.transform.SetParent(geometry, false);
            sunObject.transform.localRotation = Quaternion.Euler(58, 140, 0);
            var moon = sunObject.AddComponent<Light>(); moon.type = LightType.Directional;
            moon.color = palette.Moon; moon.intensity = .55f;
            moon.shadows = LightShadows.Soft; moon.shadowStrength = .8f;
        }

        // ----------------------------------------------------------------- materials

        void ConfigureAtmosphere()
        {
            if (environmentOwner)
            {
                previousAmbient = environmentOwner.previousAmbient;
                previousAmbientMode = environmentOwner.previousAmbientMode;
                previousFog = environmentOwner.previousFog;
                previousEquator = environmentOwner.previousEquator; previousGround = environmentOwner.previousGround;
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
                previousEquator = RenderSettings.ambientEquatorColor; previousGround = RenderSettings.ambientGroundColor;
                previousFogEnabled = RenderSettings.fog;
                previousFogMode = RenderSettings.fogMode;
                previousFogDensity = RenderSettings.fogDensity;
                previousFogStart = RenderSettings.fogStartDistance;
                previousFogEnd = RenderSettings.fogEndDistance;
            }
            environmentOwner = this;
            // Floors and wall faces keep the ambient they had under flat lighting; only what
            // faces down (ceilings, rafters, undersides) falls into shade.
            Color ambient = palette.Ambient * 1.35f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambient;
            RenderSettings.ambientEquatorColor = ambient;
            RenderSettings.ambientGroundColor = ambient * .55f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = palette.Fog;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .014f;
            RenderSettings.fogStartDistance = 10;
            RenderSettings.fogEndDistance = 60;
        }

        static Palette BuildPalette(int theme)
        {
            switch (theme)
            {
                case 1: // Galeria dos Véus: violet stone and cold light
                    return new Palette
                    {
                        FloorA = Hex(0x4a4560), FloorB = Hex(0x423d57), FloorLight = Hex(0x65608a), FloorDark = Hex(0x2c2840), Grout = Hex(0x1f1b2e),
                        Brick = Hex(0x5a4c86), BrickLight = Hex(0x7d6db3), BrickDark = Hex(0x3b3160), Mortar = Hex(0x231d3a), Cap = Hex(0x9a8bd0), Base = Hex(0x1d182e),
                        Top = Hex(0x17121f), Rim = Hex(0xc2b6e8), Wood = Hex(0x5c4060), WoodDark = Hex(0x372540), Cloth = Hex(0x6e3fa0), ClothDark = Hex(0x3d2260), Accent = Hex(0xd7c46a),
                        Torch = new Color(.7f, .55f, 1), Ambient = new Color(.3f, .28f, .42f), Fog = new Color(.06f, .05f, .12f), Moon = new Color(.62f, .62f, 1)
                    };
                case 2: // Catedral das Cinzas: crimson brick
                    return new Palette
                    {
                        FloorA = Hex(0x553236), FloorB = Hex(0x4b2b30), FloorLight = Hex(0x75484a), FloorDark = Hex(0x321c20), Grout = Hex(0x241316),
                        Brick = Hex(0x8a3b35), BrickLight = Hex(0xb85a48), BrickDark = Hex(0x5c2525), Mortar = Hex(0x2a1214), Cap = Hex(0xd88a72), Base = Hex(0x220f12),
                        Top = Hex(0x1c0e10), Rim = Hex(0xe8b49c), Wood = Hex(0x6b3a26), WoodDark = Hex(0x3e2016), Cloth = Hex(0xa22a2a), ClothDark = Hex(0x5e1418), Accent = Hex(0xf0c060),
                        Torch = new Color(1, .5f, .25f), Ambient = new Color(.36f, .25f, .26f), Fog = new Color(.1f, .04f, .05f), Moon = new Color(1, .7f, .65f)
                    };
                case 3: // Ninho da Peste: mossy stone
                    return new Palette
                    {
                        FloorA = Hex(0x3f4a38), FloorB = Hex(0x384230), FloorLight = Hex(0x5a6a4a), FloorDark = Hex(0x252e22), Grout = Hex(0x1a2018),
                        Brick = Hex(0x56663e), BrickLight = Hex(0x7a8c55), BrickDark = Hex(0x36442a), Mortar = Hex(0x1e2618), Cap = Hex(0xa4bc78), Base = Hex(0x161c12),
                        Top = Hex(0x121810), Rim = Hex(0xbcd49a), Wood = Hex(0x5a4a2a), WoodDark = Hex(0x352a18), Cloth = Hex(0x5e7a2a), ClothDark = Hex(0x344416), Accent = Hex(0xd8e070),
                        Torch = new Color(.75f, 1, .45f), Ambient = new Color(.27f, .33f, .26f), Fog = new Color(.04f, .08f, .05f), Moon = new Color(.7f, .9f, .7f)
                    };
                case 4: // Abismo dos Sinos: deep blue
                    return new Palette
                    {
                        FloorA = Hex(0x33364f), FloorB = Hex(0x2d3047), FloorLight = Hex(0x4c5070), FloorDark = Hex(0x1e2033), Grout = Hex(0x151627),
                        Brick = Hex(0x404672), BrickLight = Hex(0x5e66a0), BrickDark = Hex(0x2a2e50), Mortar = Hex(0x161830), Cap = Hex(0x8e98d8), Base = Hex(0x111226),
                        Top = Hex(0x0e0f1c), Rim = Hex(0xa8b0e8), Wood = Hex(0x4a3c50), WoodDark = Hex(0x2c2234), Cloth = Hex(0x3a4ca0), ClothDark = Hex(0x202a60), Accent = Hex(0xe0c070),
                        Torch = new Color(.55f, .7f, 1), Ambient = new Color(.25f, .26f, .38f), Fog = new Color(.04f, .03f, .1f), Moon = new Color(.6f, .7f, 1)
                    };
                default: // Jardins da Colheita: warm orange brick, like a castle keep
                    return new Palette
                    {
                        FloorA = Hex(0x5a4a44), FloorB = Hex(0x52433e), FloorLight = Hex(0x76645a), FloorDark = Hex(0x3a2e2b), Grout = Hex(0x2a201e),
                        Brick = Hex(0xb06a3a), BrickLight = Hex(0xd8904e), BrickDark = Hex(0x7e4626), Mortar = Hex(0x3e2418), Cap = Hex(0xe8b070), Base = Hex(0x2e1a14),
                        Top = Hex(0x1e1416), Rim = Hex(0xe8c8a8), Wood = Hex(0x7a4a2a), WoodDark = Hex(0x48281a), Cloth = Hex(0x8a2f2a), ClothDark = Hex(0x521a18), Accent = Hex(0xf0c060),
                        Torch = new Color(1, .62f, .28f), Ambient = new Color(.36f, .3f, .3f), Fog = new Color(.07f, .05f, .05f), Moon = new Color(1, .82f, .7f)
                    };
            }
        }

        static Color32 Hex(int value) => new Color32((byte)(value >> 16), (byte)(value >> 8), (byte)value, 255);

        void CreateMaterials()
        {
            floorTexture = FloorTexture(out float[] floorHeight);
            floorMat = Surface("piso", Color.white, floorTexture, 4);
            Texture2D brick = BrickTexture(out float[] brickHeight);
            wallMat = Surface("tijolos", Color.white, brick, 4);
            topMat = Surface("topo da parede", Color.white, SolidTexture("topo", palette.Top, palette.Top, 1), 1);
            rimMat = Surface("borda clara", Color.white, SolidTexture("borda", palette.Rim, palette.Cap, 2), 1);
            Texture2D plank = PlankTexture(false), plankTop = PlankTexture(true);
            woodMat = Surface("madeira", Color.white, plank, 1);
            woodTopMat = Surface("madeira / topo", Color.white, plankTop, 1);
            ironMat = Surface("ferro", Color.white, SolidTexture("ferro", Hex(0x3a3c48), Hex(0x24252e), 3), 1);
            clothMat = Surface("tecido", Color.white, ClothTexture(), 1);
            shelfMat = Surface("livros", Color.white, BookTexture(), 1);
            darkMat = Surface("sombra", Color.white, SolidTexture("sombra", palette.Base, palette.Top, 4), 1);
            boneMat = Surface("osso", Color.white, SolidTexture("osso", Hex(0xd8ccb0), Hex(0xa89c80), 5), 1);
            Texture2D stone = StoneTexture(false), stoneTop = StoneTexture(true);
            stoneMat = Surface("pedra", Color.white, stone, 1);
            stoneTopMat = Surface("pedra / topo", Color.white, stoneTop, 1);
            goldMat = Surface("ouro", Color.white, SolidTexture("ouro", palette.Accent, Hex(0xa07028), 6), 1, new Color(.3f, .2f, .05f));
            // The ceiling reuses the floor slabs, darker and cooler.
            ceilingMat = Surface("teto", new Color(.62f, .6f, .66f), floorTexture, 4);
            // Flames keep their hue: a bright core texture with a warm edge, moderate emission.
            flameMat = Surface("chama", Color.white, SolidTexture("chama", Hex(0xffd27a), Hex(0xff8a2a), 7), 1, palette.Torch * 1.1f);
            exitMaterial = Surface("selo da saída", new Color(.33f, .16f, .5f), null, 1, new Color(.3f, .08f, .5f));

            // Relief and surface response only exist on the stylized shader.
            if (!HauntFpsGame.UsingStylizedShader) return;
            int floorSize = TexelsPerTile * 4;
            floorRelief = ReliefTexture("piso", floorSize, floorSize, floorHeight, 1.6f);
            Relief(floorMat, floorRelief, .9f); Relief(ceilingMat, floorRelief, .9f);
            Relief(wallMat, ReliefTexture("tijolos", brick.width, brick.height, brickHeight, 1.8f), 1);
            Relief(woodMat, ReliefFromColors("madeira", plank, 2.2f), .7f);
            Relief(woodTopMat, ReliefFromColors("madeira topo", plankTop, 2.2f), .7f);
            Relief(stoneMat, ReliefFromColors("pedra", stone, 2.4f), .7f);
            Relief(stoneTopMat, ReliefFromColors("pedra topo", stoneTop, 2.4f), .7f);
            Color warm = Color.Lerp(Color.white, palette.Torch, .5f);
            HauntFpsGame.Polish(floorMat, .28f, warm);
            HauntFpsGame.Polish(ceilingMat, .05f, warm);
            HauntFpsGame.Polish(wallMat, .06f, warm, null, .6f, 1.3f);
            HauntFpsGame.Polish(woodMat, .1f, warm, null, .35f, .9f);
            HauntFpsGame.Polish(woodTopMat, .16f, warm);
            HauntFpsGame.Polish(stoneMat, .14f, warm, null, .4f, .8f);
            HauntFpsGame.Polish(stoneTopMat, .2f, warm);
            HauntFpsGame.Polish(ironMat, .6f, new Color(.8f, .9f, 1));
            HauntFpsGame.Polish(goldMat, .8f, new Color(1, .82f, .45f));
            HauntFpsGame.Polish(boneMat, .2f, warm);
        }

        static void Relief(Material material, Texture2D normals, float strength)
        {
            if (!material || !normals || !material.HasProperty("_ReliefMap")) return;
            material.SetTexture("_ReliefMap", normals);
            material.SetFloat("_ReliefScale", strength);
        }

        /// <summary>Tangent-space normal map of a height field, one texel per colour texel,
        /// wrapping like the colour texture. Stored as plain RGB in a linear texture.</summary>
        Texture2D ReliefTexture(string name, int width, int height, float[] heights, float strength)
        {
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                float left = heights[y * width + (x + width - 1) % width], right = heights[y * width + (x + 1) % width];
                float down = heights[(y + height - 1) % height * width + x], up = heights[(y + 1) % height * width + x];
                Vector3 n = new Vector3((left - right) * strength, (down - up) * strength, 1).normalized;
                pixels[y * width + x] = new Color32((byte)Mathf.RoundToInt((n.x * .5f + .5f) * 255), (byte)Mathf.RoundToInt((n.y * .5f + .5f) * 255),
                    (byte)Mathf.RoundToInt((n.z * .5f + .5f) * 255), 255);
            }
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            { name = "Masmorra / relevo " + name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, anisoLevel = 0 };
            texture.SetPixels32(pixels); texture.Apply(false, true); textures.Add(texture); return texture;
        }

        /// <summary>Relief for painted textures: darker texels (seams, grain, cracks) sit lower.</summary>
        Texture2D ReliefFromColors(string name, Texture2D colors, float strength)
        {
            // The painters keep their pixels only on the GPU, so repaint from the known size.
            Color32[] pixels;
            if (!paintedPixels.TryGetValue(colors, out pixels)) return null;
            var heights = new float[pixels.Length];
            for (int i = 0; i < pixels.Length; i++) heights[i] = (pixels[i].r * .3f + pixels[i].g * .59f + pixels[i].b * .11f) / 255f;
            return ReliefTexture(name, colors.width, colors.height, heights, strength);
        }

        readonly Dictionary<Texture2D, Color32[]> paintedPixels = new Dictionary<Texture2D, Color32[]>();

        Material Surface(string name, Color color, Texture2D texture, float tilesPerTexture, Color? emission = null)
        {
            var template = HauntFpsGame.LitTemplate();
            Material material;
            if (template) material = new Material(template);
            else
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (!shader) throw new InvalidOperationException("Shader URP da masmorra não encontrado.");
                material = new Material(shader);
            }
            material.name = "Masmorra / " + name;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .05f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0);
            if (texture)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            }
            // Block UVs are measured in tiles; the texture spans tilesPerTexture tiles.
            material.mainTextureScale = Vector2.one / Mathf.Max(.01f, tilesPerTexture);
            if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", material.mainTextureScale);
            material.SetColor("_EmissionColor", emission ?? Color.black);
            if (emission.HasValue) material.EnableKeyword("_EMISSION");
            materials.Add(material); return material;
        }

        // ----------------------------------------------------------------- pixel textures (16 texels per tile)

        uint Hash(int x, int y, int salt)
        {
            uint h = unchecked((uint)(x * 73856093 ^ y * 19349663 ^ salt * 83492791 ^ ThemeIndex * 2654435761u));
            h ^= h >> 13; h *= 1274126177; h ^= h >> 16; return h;
        }

        static Color32 Shade(Color32 c, int amount) => new Color32((byte)Mathf.Clamp(c.r + amount, 0, 255),
            (byte)Mathf.Clamp(c.g + amount, 0, 255), (byte)Mathf.Clamp(c.b + amount, 0, 255), 255);

        Texture2D Finish(string name, int width, int height, Color32[] pixels)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            { name = "Masmorra / " + name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, anisoLevel = 0 };
            texture.SetPixels32(pixels); texture.Apply(false, true); textures.Add(texture);
            paintedPixels[texture] = pixels;
            return texture;
        }

        /// <summary>4×4 stone tiles with a lit upper-left bevel, a dark lower-right edge,
        /// per-tile tone changes and the odd crack, like hand-placed dungeon floors.</summary>
        Texture2D FloorTexture(out float[] heights)
        {
            const int n = TexelsPerTile * 4;
            var px = new Color32[n * n];
            heights = new float[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int tx = x / TexelsPerTile, ty = y / TexelsPerTile, lx = x % TexelsPerTile, ly = y % TexelsPerTile;
                uint tile = Hash(tx, ty, 11);
                Color32 c = (tile & 3) == 0 ? palette.FloorB : palette.FloorA;
                if ((tile >> 3) % 7 == 0) c = Shade(c, -8);
                // Texture row 0 is the bottom; the "upper" bevel is the high rows.
                if (lx == 0 || ly == 0) c = palette.Grout;
                else if (ly == TexelsPerTile - 1 || lx == 1) c = palette.FloorLight;
                else if (ly == 1 || lx == TexelsPerTile - 1) c = palette.FloorDark;
                else
                {
                    uint grain = Hash(x, y, 3);
                    if (grain % 23 == 0) c = Shade(c, -10); else if (grain % 31 == 0) c = Shade(c, 8);
                    // Cracks: a short diagonal run of dark texels on some tiles.
                    if ((tile >> 5) % 5 == 0)
                    {
                        int k = (int)((tile >> 9) % 8) + 4;
                        if (lx + ly == k + 6 && lx > 3 && lx < 12) c = palette.FloorDark;
                        if (lx + ly == k + 7 && lx > 5 && lx < 10) c = palette.Grout;
                    }
                }
                px[y * n + x] = c;
                // Slabs stand proud of the grout, with a chamfer one texel wide; cracks cut in.
                bool grout = lx == 0 || ly == 0, chamfer = lx == 1 || ly == 1 || lx == TexelsPerTile - 1 || ly == TexelsPerTile - 1;
                bool crack = !grout && !chamfer && (c.Equals(palette.FloorDark) || c.Equals(palette.Grout));
                heights[y * n + x] = grout ? 0 : crack ? .35f : chamfer ? .7f : 1 + ((tile >> 12) % 5) * .04f;
            }
            return Finish("piso", n, n, px);
        }

        /// <summary>Bricks for wall faces. The texture covers four tiles across and one full
        /// wall height: a dark base, staggered bricks lit from above and a bright cap course.</summary>
        Texture2D BrickTexture(out float[] heights)
        {
            int w = TexelsPerTile * 4, h = Mathf.RoundToInt(WallHeight / Tile * TexelsPerTile);
            var px = new Color32[w * h];
            heights = new float[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                Color32 c;
                // Height: a plinth at the base, a proud cap course, bricks above recessed mortar.
                float lift = 1;
                if (y < 4) { c = y == 3 ? palette.BrickDark : palette.Base; lift = y == 3 ? .9f : 1.2f; }
                else if (y >= h - 3) { c = y == h - 1 ? palette.Rim : palette.Cap; lift = 1.3f; }
                else if (y == h - 4) { c = palette.Mortar; lift = 0; }
                else
                {
                    int row = (y - 4) / 6, ly = (y - 4) % 6;
                    int offset = row % 2 == 0 ? 0 : 8, lx = (x + offset) % 16;
                    int brick = (x + offset) / 16;
                    uint tone = Hash(brick, row, 21);
                    Color32 body = tone % 5 == 0 ? palette.BrickDark : tone % 7 == 0 ? Shade(palette.Brick, 10) : palette.Brick;
                    if (ly == 0 || lx == 0) c = palette.Mortar;
                    else if (ly == 5) c = palette.BrickLight;
                    else if (ly == 1 || lx == 15) c = Shade(body, -22);
                    else c = body;
                    if (c.Equals(body) && Hash(x, y, 4) % 29 == 0) c = Shade(body, -14);
                    // Some bricks sit a little deeper or prouder than their neighbours.
                    lift = ly == 0 || lx == 0 ? 0 : .85f + (tone % 4) * .07f;
                }
                heights[y * w + x] = lift;
                px[y * w + x] = c;
            }
            return Finish("tijolos", w, h, px);
        }

        Texture2D SolidTexture(string name, Color32 main, Color32 speck, int salt)
        {
            const int n = TexelsPerTile;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++) px[y * n + x] = Hash(x, y, salt) % 9 == 0 ? speck : main;
            return Finish(name, n, n, px);
        }

        Texture2D PlankTexture(bool top)
        {
            const int n = TexelsPerTile;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int ly = y % 4;
                Color32 c = ly == 0 ? palette.WoodDark : ly == 3 ? Shade(palette.Wood, top ? 26 : 14) : palette.Wood;
                if (x == 0 || x == n - 1 || y == 0 || y == n - 1) c = palette.WoodDark;
                if (!top && (x == 2 || x == n - 3) && ly != 0) c = Shade(palette.WoodDark, 10);
                if (Hash(x, y, 9) % 17 == 0 && ly != 0) c = Shade(c, -12);
                px[y * n + x] = c;
            }
            return Finish(top ? "madeira topo" : "madeira", n, n, px);
        }

        Texture2D StoneTexture(bool top)
        {
            const int n = TexelsPerTile;
            var px = new Color32[n * n];
            Color32 baseColor = Shade(palette.FloorLight, 18);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                Color32 c = baseColor;
                if (x == 0 || y == 0) c = Shade(baseColor, -40);
                else if (y == n - 1 || x == 1) c = Shade(baseColor, top ? 30 : 20);
                else if (Hash(x, y, 13) % 13 == 0) c = Shade(baseColor, -14);
                px[y * n + x] = c;
            }
            return Finish(top ? "pedra topo" : "pedra", n, n, px);
        }

        Texture2D ClothTexture()
        {
            const int w = TexelsPerTile, h = TexelsPerTile;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                Color32 c = x == 0 || x == w - 1 ? palette.ClothDark : palette.Cloth;
                int cx = x - w / 2, cy = y - h / 2;
                if (Mathf.Abs(cx) + Mathf.Abs(cy) <= 3) c = palette.Accent;
                if (y < 3 && (x + y) % 3 == 0) c = palette.ClothDark;
                if (y >= h - 2) c = palette.Accent;
                px[y * w + x] = c;
            }
            return Finish("tecido", w, h, px);
        }

        Texture2D RugTexture(int tilesX, int tilesZ)
        {
            int w = TexelsPerTile * tilesX, hgt = TexelsPerTile * tilesZ;
            var px = new Color32[w * hgt];
            for (int y = 0; y < hgt; y++) for (int x = 0; x < w; x++)
            {
                int ex = Mathf.Min(x, w - 1 - x), ey = Mathf.Min(y, hgt - 1 - y), edge = Mathf.Min(ex, ey);
                Color32 c = edge == 0 ? palette.ClothDark : edge == 1 ? palette.Cloth : edge == 2 ? palette.Accent : palette.Cloth;
                if (edge > 4 && ((x / 4 + y / 4) % 2 == 0)) c = Shade(palette.Cloth, -14);
                if (edge > 4 && Mathf.Abs(x - w / 2) + Mathf.Abs(y - hgt / 2) == Mathf.Min(w, hgt) / 4) c = palette.Accent;
                if (edge == 0 && (x + y) % 2 == 0) c = palette.Accent;
                px[y * w + x] = c;
            }
            return Finish("tapete", w, hgt, px);
        }

        Texture2D BookTexture()
        {
            const int n = TexelsPerTile;
            var px = new Color32[n * n];
            Color32[] spines = { palette.Cloth, palette.Accent, Hex(0x3a5a8a), Hex(0x4a7a3a), palette.ClothDark, Hex(0xd8ccb0) };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int shelfRow = y / 8, ly = y % 8;
                Color32 c = ly == 0 ? palette.WoodDark : spines[(int)(Hash(x / 2, shelfRow, 7) % (uint)spines.Length)];
                if (ly >= 6 + (int)(Hash(x / 2, shelfRow, 8) % 2)) c = palette.WoodDark;
                if (x % 2 == 1 && ly != 0) c = Shade(c, -20);
                px[y * n + x] = c;
            }
            return Finish("livros", n, n, px);
        }

        // ----------------------------------------------------------------- blocks

        /// <summary>An axis-aligned textured box. UVs are taken from world coordinates in tiles,
        /// so neighbouring blocks continue the same pattern and texels stay square.
        /// Side faces use the first material, the top face the second.</summary>
        GameObject Block(string name, Vector3 min, Vector3 max, Material sides, Material top, bool solid, Transform parent,
            float faceHeight = 0, bool local = false, bool localUv = false)
        {
            var node = new GameObject(name);
            node.transform.SetParent(parent ? parent : geometry, false);
            Vector3 size = max - min, center = (min + max) * .5f;
            if (local) node.transform.localPosition = center; else node.transform.position = center;
            Vector3 worldCenter = node.transform.position;
            var mesh = BoxMesh(size, worldCenter, faceHeight, localUv);
            meshes.Add(mesh);
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { sides, top };
            if (solid) { var box = node.AddComponent<BoxCollider>(); box.size = size; }
            return node;
        }

        /// <summary>Box mesh with tile UVs. World UVs are offset by half a tile so the floor
        /// grid meets the walls (rooms have an odd number of tiles). A positive faceHeight maps
        /// the full height of side faces onto one repeat of a texture tiled every four tiles
        /// (bricks with base and cap). Local UVs start at the block's corner (rugs).</summary>
        static Mesh BoxMesh(Vector3 size, Vector3 worldCenter, float faceHeight, bool localUv)
        {
            var vertices = new List<Vector3>(24); var normals = new List<Vector3>(24); var uvs = new List<Vector2>(24);
            var sides = new List<int>(30); var tops = new List<int>(6);
            Vector3 h = size * .5f;
            void Face(Vector3 n, Vector3 a, Vector3 b, float ha, float hb, float hn, bool top)
            {
                int start = vertices.Count;
                Vector3 c = n * hn;
                Vector3[] corners = { c - a * ha - b * hb, c + a * ha - b * hb, c + a * ha + b * hb, c - a * ha + b * hb };
                foreach (var p in corners)
                {
                    vertices.Add(p); normals.Add(n);
                    Vector3 w = localUv ? p + h : p + worldCenter + new Vector3(Tile * .5f, 0, Tile * .5f);
                    Vector2 uv;
                    if (Mathf.Abs(n.y) > .5f) uv = new Vector2(w.x, w.z) / Tile;
                    else
                    {
                        float horizontal = Mathf.Abs(n.x) > .5f ? w.z : w.x;
                        // Brick faces map the wall's full height from the floor up, so a lintel
                        // high on the wall continues the courses of the wall beside it.
                        float vertical = faceHeight > 0 ? (p.y + worldCenter.y) / faceHeight * 4f : (p.y + worldCenter.y) / Tile;
                        uv = new Vector2(horizontal / Tile, vertical);
                    }
                    uvs.Add(uv);
                }
                var list = top ? tops : sides;
                list.Add(start); list.Add(start + 1); list.Add(start + 2);
                list.Add(start); list.Add(start + 2); list.Add(start + 3);
            }
            Face(Vector3.up, Vector3.forward, Vector3.right, h.z, h.x, h.y, true);
            Face(Vector3.down, Vector3.right, Vector3.forward, h.x, h.z, h.y, false);
            Face(Vector3.right, Vector3.up, Vector3.forward, h.y, h.z, h.x, false);
            Face(Vector3.left, Vector3.forward, Vector3.up, h.z, h.y, h.x, false);
            Face(Vector3.forward, Vector3.right, Vector3.up, h.x, h.y, h.z, false);
            Face(Vector3.back, Vector3.up, Vector3.right, h.y, h.x, h.z, false);
            var mesh = new Mesh { name = "Bloco" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(sides, 0); mesh.SetTriangles(tops, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        Light Point(Vector3 position, Color color, float intensity, float range, Transform shape, float phase)
        {
            var go = new GameObject("Luz"); go.transform.SetParent(geometry, false); go.transform.position = position;
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
                exitLight.intensity = open ? 2.6f : 1.4f;
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
                if (flame.Light && flame.Light == exitLight) continue;
                if (!flame.Light && !flame.Shape) continue;
                float wobble = Mathf.PerlinNoise(flame.Phase, clock * 2.6f) * 2 - 1;
                if (flame.Light) flame.Light.intensity = flame.Strength * (1 + wobble * .16f);
                if (flame.Shape) flame.Shape.localScale = new Vector3(flame.Scale.x, flame.Scale.y * (1 + wobble * .2f), flame.Scale.z);
            }
            // Door grates slide up from the floor while a room is locked.
            foreach (var door in doors)
            {
                if (!door.Bars || Mathf.Approximately(door.Raise, door.Target)) continue;
                door.Raise = Mathf.MoveTowards(door.Raise, door.Target, dt * 5);
                door.Bars.gameObject.SetActive(door.Raise > .001f);
                door.Bars.localPosition = Vector3.down * ((1 - door.Raise) * (WallHeight + .2f));
            }
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            if (environmentOwner == this)
            {
                environmentOwner = null;
                RenderSettings.ambientEquatorColor = previousEquator; RenderSettings.ambientGroundColor = previousGround;
                RenderSettings.ambientMode = previousAmbientMode; RenderSettings.ambientLight = previousAmbient;
                RenderSettings.fog = previousFogEnabled; RenderSettings.fogColor = previousFog;
                RenderSettings.fogMode = previousFogMode; RenderSettings.fogDensity = previousFogDensity;
                RenderSettings.fogStartDistance = previousFogStart; RenderSettings.fogEndDistance = previousFogEnd;
            }
            foreach (var material in materials) if (material) DestroyOwned(material);
            foreach (var texture in textures) if (texture) DestroyOwned(texture);
            foreach (var mesh in meshes) if (mesh) DestroyOwned(mesh);
            materials.Clear(); textures.Clear(); meshes.Clear(); flames.Clear(); doors.Clear(); paintedPixels.Clear(); flamePoints.Clear();
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
