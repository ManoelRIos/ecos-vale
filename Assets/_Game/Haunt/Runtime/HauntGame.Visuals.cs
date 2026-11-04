using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        static readonly string[] EnemyArt = { "pumpkin", "ghost", "cultist", "bat", "boss", "larva", "sentinel", "acolyte", "skeleton", "slime", "spider", "miniboss", "lantern-wisp", "mushroom-brute", "clockwork-imp", "scarecrow", "gargoyle", "stalker-shadow", "fire-bat" };
        static readonly Color PlayerAccent = new Color(.32f, 1f, .85f);
        static readonly Color HostileAccent = new Color(1f, .31f, .43f);
        readonly List<RoomAnimation> roomAnimations = new List<RoomAnimation>();
        readonly List<SpriteRenderer> roomGlows = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> doorGlows = new List<SpriteRenderer>();
        readonly Stack<SpriteRenderer> effectPool = new Stack<SpriteRenderer>();
        readonly List<FogPatch> fogPatches = new List<FogPatch>();
        readonly List<SpriteRenderer> raisedWalls = new List<SpriteRenderer>();
        readonly List<ShopDisplay> shopDisplays = new List<ShopDisplay>();
        sealed class ShopDisplay
        {
            public SpriteRenderer Sprite;
            public ShopItem Item;
        }
        sealed class FogPatch
        {
            public SpriteRenderer Sprite;
            public Vector2 Anchor;
            public float Phase;
        }
        Material glowMaterial, projectileLightMaterial;
        Vector2 cameraPosition;
        SpriteRenderer playerMarker, playerGlow, crosshair;
        float walkDistance, weaponKick, trailTimer, dustTimer, roomBannerTime;
        bool playerMoving;
        int roomTheme;
        static readonly string[] FloorThemes = { "JARDINS DA COLHEITA", "GALERIA DOS VÉUS", "CATEDRAL DAS CINZAS", "NINHO DA PESTE", "ABISMO DOS SINOS" };
        static readonly Color[] FloorColors = { new Color(.88f, .98f, .84f), new Color(.78f, .9f, 1), new Color(1, .78f, .72f), new Color(.72f, .91f, .55f), new Color(.83f, .65f, .92f) };
        string PlayerArt => State == null || State.Character == CharacterKind.Hunter ? "player" : State.Character == CharacterKind.Medium ? "medium" : State.Character == CharacterKind.Reaper ? "reaper" : "alchemist";
        Color CharacterAccent => State == null || State.Character == CharacterKind.Hunter ? PlayerAccent : State.Character == CharacterKind.Medium ? new Color(.82f, .58f, 1) : State.Character == CharacterKind.Reaper ? new Color(.6f, .8f, 1) : new Color(.65f, 1, .35f);
        string WeaponArt => WeaponSpriteId(State.Weapon);
        static string WeaponSpriteId(WeaponKind weapon) => weapon == WeaponKind.Scattergun ? "scattergun" : weapon == WeaponKind.Repeater ? "repeater" : weapon == WeaponKind.SoulCannon ? "cannon" : weapon == WeaponKind.Stapler ? "stapler" : weapon == WeaponKind.Banana ? "banana-gun" : weapon == WeaponKind.SpinningTop ? "spinning-top"
            : weapon == WeaponKind.HunterCarbine ? "hunter-carbine" : weapon == WeaponKind.VeilWand ? "veil-wand"
            : weapon == WeaponKind.BoneCaster ? "bone-caster" : weapon == WeaponKind.ArcCoil ? "arc-coil"
            : weapon == WeaponKind.MirrorDisc ? "mirror-disc" : weapon == WeaponKind.RayLantern ? "ray-lantern"
            : weapon == WeaponKind.BoneFlute ? "bone-flute" : weapon == WeaponKind.Umbrella ? "umbrella-gun"
            : weapon == WeaponKind.HiveLauncher ? "hive-launcher" : weapon == WeaponKind.BoxingGlove ? "boxing-glove" : "weapon";
        string RoomThemeName => InShop || State == null ? "MERCADO DAS ALMAS" : FloorThemes[ThemeVariant];

        sealed class RoomAnimation
        {
            public SpriteRenderer Sprite;
            public string Id;
            public float Offset;
        }

        Material GlowMaterial
        {
            get
            {
                if (!glowMaterial)
                {
                    glowMaterial = new Material(art.Material) { name = "Haunt / additive light" };
                    glowMaterial.SetFloat("_SrcBlend", 5); // SrcAlpha
                    glowMaterial.SetFloat("_DstBlend", 1); // One
                    glowMaterial.SetFloat("_Emission", 2.2f);
                }
                return glowMaterial;
            }
        }

        Material ProjectileLightMaterial
        {
            get
            {
                if (!projectileLightMaterial)
                {
                    projectileLightMaterial = new Material(art.Material) { name = "Haunt / soft shot light" };
                    projectileLightMaterial.SetFloat("_SrcBlend", 5);
                    projectileLightMaterial.SetFloat("_DstBlend", 1);
                    projectileLightMaterial.SetFloat("_Emission", 1);
                }
                return projectileLightMaterial;
            }
        }

        static int Depth(float groundY) => 1000 - Mathf.RoundToInt(groundY * 16);

        void BuildRoom(bool shop)
        {
            ClearRoomFeel();
            ClearRoomLighting();
            ClearRoomProps();
            if (roomRoot) { roomRoot.SetActive(false); Destroy(roomRoot); }
            roomRoot = new GameObject(shop ? "Mercado das Almas" : "Cripta • sala " + CurrentRoom.Id);
            roomRoot.transform.SetParent(transform, false);
            doors.Clear(); doorGlows.Clear(); obstacles.Clear(); pits.Clear(); fogPatches.Clear(); raisedWalls.Clear(); shopDisplays.Clear(); roomAnimations.Clear(); roomGlows.Clear(); portal = null;
            int seed = State == null ? 71319 : unchecked(State.Seed * 397 ^ State.Floor * 7919 ^ CurrentRoom.Id * 104729);
            roomRandom = new System.Random(seed);
            roomTheme = shop ? 0 : ThemeVariant;
            Color floorTint = shop ? new Color(.95f, .89f, .78f) : Color.white * Mathf.Lerp(1, .88f, Mathf.Clamp01((State.Floor - 1) / 12f));
            floorTint.a = 1;
            InitializeRoomLayout(shop);
            for (int y = -RoomLayout.MaxY; y <= RoomLayout.MaxY; y++)
                for (int x = -RoomLayout.MaxX; x <= RoomLayout.MaxX; x++)
                {
                    if (!RoomTileInside(x, y) || roomLayout != null && roomLayout.IsPit(x, y)) continue;
                    uint tileHash = unchecked((uint)(x * 73856093 ^ y * 19349663 ^ seed));
                    tileHash ^= tileHash >> 13; tileHash *= 0x85ebca6bu; tileHash ^= tileHash >> 16;
                    var tile = MakeSprite("floor-theme" + roomTheme + "-v" + (tileHash % 4), new Vector2(x, y), roomRoot.transform, -200);
                    tile.flipX = (tileHash & 8) != 0;
                    tile.flipY = (tileHash & 16) != 0;
                    tile.transform.rotation = Quaternion.Euler(0, 0, 90 * ((tileHash >> 5) % 4));
                    tile.color = Color.Lerp(floorTint, floorTint * .9f, (tileHash % 7) / 7f);
                    if (!RoomTileInside(x - 1, y) || !RoomTileInside(x + 1, y)
                        || !RoomTileInside(x, y - 1) || !RoomTileInside(x, y + 1)) tile.color *= .85f;
                }
            RenderRoomPits();
            if (shop)
            {
                var rug = MakeSprite("rug", new Vector2(0, 1), roomRoot.transform, -180);
                rug.transform.localScale = new Vector3(4.5f, 3.8f, 1);
                rug.color = new Color(.75f, .6f, .8f);
                AnimatedProp("merchant", MerchantPosition + Vector2.up * .4f, Depth(MerchantPosition.y));
                MakeSprite("shop-counter", new Vector2(0, 2.7f), roomRoot.transform, Depth(2.4f));
                obstacles.Add(new Rect(-3, 1.95f, 6, 1.15f));
                obstacles.Add(new Rect(-.5f, MerchantPosition.y - .4f, 1, .8f));
                ShopItem[] stock = State == null ? new ShopItem[0] : State.ShopWeapons;
                for (int i = 0; i < stock.Length; i++)
                {
                    Vector2 point = ShopPosition(i);
                    MakeSprite("shop-case", point, roomRoot.transform, Depth(point.y));
                    RunState.TryWeaponForItem(stock[i], out WeaponKind displayed);
                    var display = MakeSprite(WeaponSpriteId(displayed), point + Vector2.up * .65f, roomRoot.transform, Depth(point.y) + 1);
                    display.color = State.Sold(stock[i]) ? new Color(1, 1, 1, .25f) : Color.white;
                    shopDisplays.Add(new ShopDisplay { Sprite = display, Item = stock[i] });
                    obstacles.Add(new Rect(point.x - 1, point.y - .4f, 2, .75f));
                    AddGroundGlow(point, new Color(1, .75f, .35f), 2.5f, .25f);
                }
                foreach (float x in new[] { -7f, 7f })
                {
                    MakeSprite("shop-shelf", new Vector2(x, 5.9f), roomRoot.transform, Depth(5.4f));
                    obstacles.Add(new Rect(x - 2, 5.2f, 4, 1));
                }
                portal = AnimatedProp("portal", new Vector2(HalfWidth - 2, 0), 450);
                AddGroundGlow(new Vector2(HalfWidth - 2, 0), PlayerAccent, 3, .2f);
            }
            else
            {
                Color accent = FloorColors[roomTheme];
                var rune = MakeSprite("rune", Vector2.zero, roomRoot.transform, -180);
                rune.transform.localScale = Vector3.one * (CurrentRoom.Kind == RoomKind.Boss ? 2.2f : 1.2f);
                rune.color = new Color(accent.r, accent.g, accent.b, .24f);
                if (CurrentRoom.Kind != RoomKind.Entrance)
                {
                    // Each theme has its own cover footprint; cardinal lanes stay clear.
                    for (int side = -1; side <= 1; side += 2)
                        for (int row = -1; row <= 1; row += 2)
                        {
                            float x = roomTheme == 0 ? 6.5f : roomTheme == 1 ? 8 : roomTheme == 2 ? 5.5f : roomTheme == 3 ? 5 : 7;
                            float y = roomTheme == 0 ? 4 : roomTheme == 1 ? 3 : roomTheme == 2 ? 4.5f : roomTheme == 3 ? 4.6f : 4;
                            x += ((CurrentRoom.Id + State.Floor) % 3 - 1) * .65f;
                            y += (CurrentRoom.Id % 2 == 0 ? -.3f : .3f);
                            Vector2 point = new Vector2(side * x, row * y);
                            if (!CanPlaceRoomDetail(point, .95f, true)) continue;
                            obstacles.Add(new Rect(point.x - .65f, point.y - .48f, 1.3f, .96f));
                            var prop = MakeSprite("decor-theme" + roomTheme, point + Vector2.up * .5f, roomRoot.transform, Depth(point.y));
                            prop.transform.localScale = Vector3.one * .85f;
                            MakeSprite("shadow", point + Vector2.down * .3f, roomRoot.transform, 0).transform.localScale = new Vector3(2, .9f, 1);
                        }
                }
                foreach (int id in CurrentRoom.Neighbors)
                {
                    DungeonRoom target = Floor.GetRoom(id);
                    int dx = target.Position.X - CurrentRoom.Position.X, dy = target.Position.Y - CurrentRoom.Position.Y;
                    Vector2 position = new Vector2(dx * (HalfWidth - .05f), dy * (HalfHeight - .05f));
                    var door = MakeSprite("door-open", position, roomRoot.transform, 500);
                    door.transform.rotation = Quaternion.Euler(0, 0, dx > 0 ? -90 : dx < 0 ? 90 : dy < 0 ? 180 : 0);
                    doors.Add(door);
                    var glow = MakeSprite("glow", position - new Vector2(dx, dy) * .35f, roomRoot.transform, -160);
                    glow.transform.localScale = Vector3.one * 3;
                    doorGlows.Add(glow);
                }
                if (CurrentRoom.Kind == RoomKind.Boss && CurrentRoom.Cleared)
                    portal = AnimatedProp("portal", new Vector2(0, 1.4f), 450);
                // Themed border ornaments distinguish silhouettes as well as palettes.
                for (int side = -1; side <= 1; side += 2)
                    for (int row = -1; row <= 1; row += 2)
                    {
                        Vector2 point = new Vector2(side * 13, row * 6.8f);
                        if (!CanPlaceRoomDetail(point, .7f, true)) continue;
                        var prop = MakeSprite("decor-theme" + roomTheme, point, roomRoot.transform, Depth(point.y));
                        prop.transform.localScale = Vector3.one * 1.1f;
                        if (roomTheme == 1 || roomTheme == 4) AttachFlickerLight(prop.transform, new Color(.6f, .3f, 1), 3, .18f, .13f, -110);
                        AddGroundGlow(point, accent, 3.5f, .15f);
                    }
                for (int i = 0; i < 9 + Mathf.Min(State.Floor, 12); i++)
                {
                    Vector2 point = new Vector2((float)roomRandom.NextDouble() * 26 - 13, (float)roomRandom.NextDouble() * 14 - 7);
                    if (!RoomContains(point, .2f)) continue;
                    var stain = MakeSprite(roomTheme >= 3 ? "rune" : "dust", point, roomRoot.transform, -190);
                    stain.color = new Color(accent.r, accent.g, accent.b, .13f);
                    stain.transform.localScale = Vector3.one * (roomTheme >= 3 ? .6f : 1.5f);
                }
            }
            if (!shop && CurrentRoom.Kind != RoomKind.Entrance && (roomTheme == 1 || roomTheme >= 3 || CurrentRoom.Id % 4 == 0))
                for (int i = 0; i < 8; i++)
                {
                    Vector2 point = new Vector2(-12 + i * 3.4f, i % 2 == 0 ? -4.2f : 4.2f);
                    if (!RoomContains(point, 1.35f)) continue;
                    var fog = MakeSprite("fog", point, roomRoot.transform, -75);
                    fog.transform.localScale = new Vector3(2.8f, 1.7f, 1);
                    fog.color = new Color(.6f, .75f, .8f, .33f);
                    fogPatches.Add(new FogPatch { Sprite = fog, Anchor = point, Phase = i * 1.7f });
                }
            for (int side = -1; side <= 1; side += 2)
                for (int row = -1; row <= 1; row += 2)
                {
                    Vector2 point = new Vector2(side * 11, row * 7.2f);
                    if (!CanPlaceRoomDetail(point, .4f)) continue;
                    AnimatedProp("candle", point, Depth(point.y));
                    AddGroundGlow(point, new Color(1, .45f, .15f), 3.8f, .24f);
                }
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 anchor = new Vector2(side * 9.8f, 3.8f);
                if (!RoomContains(anchor, 1.1f)) continue;
                var chandelier = AnimatedProp("chandelier", anchor + Vector2.up * 1.25f, Depth(anchor.y));
                chandelier.transform.localScale = Vector3.one * .85f;
                MakeSprite("shadow", anchor + new Vector2(.4f, -.4f), roomRoot.transform, -175)
                    .transform.localScale = new Vector3(3.6f, 1.7f, 1);
                AddGroundGlow(anchor, new Color(1, .6f, .24f), 4.2f, .3f);
            }
            BuildContourWalls();
            if (!shop) AddBorderDetails();
            if (!shop) InitializeRoomProps();
            if (!shop) TintDungeonRoom();
            UpdateRoomAmbience(0);
        }

        bool HasDoorDirection(int dx, int dy)
        {
            if (CurrentRoom == null || Floor == null) return false;
            foreach (int id in CurrentRoom.Neighbors)
            {
                GridPoint target = Floor.GetRoom(id).Position;
                if (target.X - CurrentRoom.Position.X == dx && target.Y - CurrentRoom.Position.Y == dy) return true;
            }
            return false;
        }

        void UpdateWallOcclusion()
        {
            foreach (var wall in raisedWalls)
            {
                Vector2 point = wall.transform.position;
                bool coversPlayer = Playing && point.y < playerPosition.y + 1.15f
                    && Mathf.Abs(point.x - playerPosition.x) < 1.3f && Mathf.Abs(point.y - playerPosition.y) < 2.2f;
                wall.color = new Color(1, 1, 1, coversPlayer ? .3f : 1);
            }
        }

        SpriteRenderer AnimatedProp(string id, Vector2 position, int depth)
        {
            var sprite = MakeSprite(id, position, roomRoot.transform, depth);
            roomAnimations.Add(new RoomAnimation { Sprite = sprite, Id = id, Offset = (float)roomRandom.NextDouble() * 4 });
            return sprite;
        }

        void AddGroundGlow(Vector2 position, Color color, float size, float opacity)
        {
            CreateRoomLight(null, position, color, size, opacity, .16f, -170);
        }

        void UpdateDoorArt()
        {
            bool closed = enemies.Count > 0;
            for (int i = 0; i < doors.Count; i++)
            {
                bool boss = Floor.GetRoom(CurrentRoom.Neighbors[i]).Kind == RoomKind.Boss;
                doors[i].sprite = art.Get(closed ? "door-locked" : boss ? "door-boss" : "door-open");
                doors[i].color = Color.white;
                Color c = closed ? HostileAccent : boss ? new Color(1, .65f, .2f) : PlayerAccent;
                doorGlows[i].color = new Color(c.r, c.g, c.b, closed ? .13f : .24f);
            }
        }

        void UpdateRoomAmbience(float dt)
        {
            if (State != null)
                foreach (var display in shopDisplays)
                    if (display.Sprite) display.Sprite.enabled = !State.Sold(display.Item);
            TickRoomLighting(dt);
            foreach (var fog in fogPatches)
                if (fog.Sprite)
                {
                    Vector2 drift = fog.Anchor + new Vector2(Mathf.Sin(simulationTime * .18f + fog.Phase) * 1.3f, Mathf.Cos(simulationTime * .12f + fog.Phase) * .4f);
                    fog.Sprite.transform.position = Snap(RoomContains(drift, 1.1f) ? drift : fog.Anchor);
                    Color tint = FloorColors[roomTheme];
                    fog.Sprite.color = new Color(tint.r * .65f, tint.g * .7f, tint.b, .16f + .07f * Mathf.PerlinNoise(fog.Phase + 2, simulationTime * .2f));
                }
            if (Playing && !InShop && dt > 0 && Mathf.FloorToInt(simulationTime * 12) != Mathf.FloorToInt((simulationTime - dt) * 12))
            {
                // Time-derived positions keep visual ambience out of the seeded spawn RNG.
                float t = simulationTime;
                Vector2 p = new Vector2(Mathf.Sin(t * 5.13f) * 13, Mathf.Cos(t * 3.71f) * 7);
                Color c = FloorColors[ThemeVariant]; c.a = .32f;
                if (RoomContains(p, .1f)) Emit("spark", p, Vector2.up * .25f, c, 1.3f, .16f, 0);
            }
            foreach (var prop in roomAnimations)
                if (prop.Sprite) prop.Sprite.sprite = art.Frame(prop.Id, Mathf.FloorToInt(simulationTime * 6 + prop.Offset));
            for (int i = 0; i < roomGlows.Count; i++)
            {
                var glow = roomGlows[i];
                if (!glow) continue;
                Color color = glow.color;
                color.a = .25f + .045f * Mathf.Sin(simulationTime * 3.7f + i * 2);
                glow.color = color;
            }
            for (int i = 0; i < doorGlows.Count; i++)
                if (doorGlows[i]) doorGlows[i].transform.localScale = Vector3.one * (2.65f + Mathf.Sin(simulationTime * 2 + i) * .12f);
            if (portal && Playing && !Paused && Mathf.FloorToInt(simulationTime * 24) != Mathf.FloorToInt((simulationTime - dt) * 24))
            {
                Vector2 p = portal.transform.position;
                float angle = simulationTime * 140;
                Emit("spark", p + Rotate(Vector2.right, angle) * .65f, Vector2.up * .6f, PlayerAccent, .4f, .3f, 0);
            }
        }

        void UpdatePresentation()
        {
            if (!player) return;
            Cursor.visible = !UsingController && (InShop || PointerOverInterface());
            Vector2 aim = AimDirection();
            bool rolling = rollRemaining > 0;
            int frame = playerMoving ? Mathf.FloorToInt(walkDistance * 4.5f) : Mathf.FloorToInt(simulationTime * 2) % 2 * 2;
            if (muzzleTime > 0) frame = 2;
            player.sprite = art.Frame(PlayerArt, frame);
            gun.sprite = art.Get(WeaponArt);
            float bob = rolling ? Mathf.Sin((1 - rollRemaining / RollTime) * Mathf.PI) * .65f : playerMoving ? Mathf.Sin(walkDistance * 14) * .045f : Mathf.Sin(simulationTime * 2) * .02f;
            player.transform.position = Snap(playerPosition + Vector2.up * (.38f + bob));
            player.sortingOrder = Depth(playerPosition.y);
            player.flipX = aim.x < 0;
            player.color = flash > 0 ? new Color(1, .6f, .65f) : invincible > 0 && Mathf.Sin(simulationTime * 30) > 0
                ? new Color(.65f, 1, .9f, .72f) : Color.white;
            float tilt = rolling ? (1 - rollRemaining / RollTime) * (rollDirection.x < 0 ? -360 : 360) : playerMoving ? Mathf.Sin(walkDistance * 8) * 3 : 0;
            player.transform.rotation = Quaternion.Euler(0, 0, tilt);
            player.transform.localScale = rolling ? new Vector3(1.05f, .78f, 1) : new Vector3(1, 1 + Mathf.Sin(simulationTime * 2) * .015f, 1);
            shadow.transform.position = Snap(playerPosition + new Vector2(.16f, -.25f));
            shadow.transform.localScale = new Vector3(1.8f - bob * .35f, .95f - bob * .15f, 1);
            shadow.color = new Color(1, 1, 1, .72f);
            playerMarker.transform.position = Snap(playerPosition + Vector2.down * .2f);
            playerMarker.color = new Color(PlayerAccent.r, PlayerAccent.g, PlayerAccent.b, rolling ? .65f : .95f);
            playerGlow.transform.position = Snap(playerPosition);
            playerGlow.color = new Color(CharacterAccent.r, CharacterAccent.g, CharacterAccent.b, .16f);
            gun.transform.position = Snap(playerPosition + Vector2.up * .35f + aim * (.6f - weaponKick * .2f));
            gun.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            gun.flipY = aim.x < 0;
            gun.sortingOrder = player.sortingOrder + (aim.y > .3f ? -1 : 1);
            gun.enabled = !rolling;
            muzzle.sortingOrder = gun.sortingOrder + 1;
            muzzle.enabled = muzzleTime > 0 && !rolling;
            muzzle.transform.position = Snap(playerPosition + Vector2.up * .35f + aim * 1.05f);
            muzzle.transform.localScale = Vector3.one * (.48f + weaponKick * .18f);
            muzzle.transform.rotation = gun.transform.rotation;
            if (crosshair)
            {
                crosshair.enabled = !PointerOverInterface() && !InShop;
                if (UsingController) crosshair.transform.position = Snap(playerPosition + Vector2.up * .35f + aim * 4);
                else if (Mouse.current != null) crosshair.transform.position = Snap(view.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
                crosshair.transform.localScale = Vector3.one * .35f;
                crosshair.color = new Color(PlayerAccent.r, PlayerAccent.g, PlayerAccent.b, .85f);
            }
            UpdateWallOcclusion();

        }

        void PresentEnemy(Enemy enemy, Vector2 direction)
        {
            bool stone = enemy.Kind == 16 && GargoyleIsStone(enemy);
            bool disguised = enemy.Kind == 15 && !enemy.Awakened;
            int frame = stone || disguised ? 0 : Mathf.FloorToInt(enemy.Phase * (enemy.Kind == 4 ? 6 : enemy.Kind == 3 || enemy.Kind == 18 ? 10 : 6));
            if (enemy.Kind == 4)
            {
                BossAnimationState state = enemy.Tell > 0 ? BossAnimationState.Telegraph
                    : enemy.AttackVisualTime > 0 ? BossAnimationState.Attack
                    : enemy.Health < enemy.MaxHealth * .5f ? BossAnimationState.Enrage : BossAnimationState.Idle;
                enemy.Sprite.sprite = art.BossFrame(EnemySprite(enemy.Kind), state, frame);
            }
            else enemy.Sprite.sprite = art.Frame(EnemySprite(enemy.Kind), frame);
            float floatHeight = stone || disguised ? 0 : enemy.Kind == 1 || enemy.Kind == 3 || enemy.Kind == 12 || enemy.Kind == 18
                ? Mathf.Sin(enemy.Phase * (enemy.Kind == 18 ? 8 : 4)) * .12f : Mathf.Sin(enemy.Phase * 5) * .025f;
            float emerged = Mathf.Clamp01(1 - enemy.SpawnRemaining / .9f);
            enemy.Sprite.transform.position = Snap(enemy.Position + Vector2.up * (.38f + floatHeight - (1 - emerged) * .65f));
            if (enemy.Rift)
            {
                enemy.Rift.enabled = enemy.SpawnRemaining > 0;
                enemy.Rift.sprite = art.Frame("spawn-rift", Mathf.FloorToInt((.9f - enemy.SpawnRemaining) * 10));
                enemy.Rift.transform.position = Snap(enemy.Position);
                enemy.Rift.transform.localScale = Vector3.one * (enemy.Kind == 11 || enemy.Kind == 4 ? 1.5f : 1);
            }
            enemy.Sprite.flipX = direction.x < 0;
            Color normal = enemy.Kind == 4 && BossVariant == 1 ? new Color(.72f, 1, .97f)
                : enemy.Kind == 4 && BossVariant == 2 ? new Color(1, .74f, .91f) : Color.white;
            if (stone) normal = new Color(.58f, .6f, .68f);
            enemy.Sprite.color = enemy.HurtFlash > 0 ? Color.white
                : enemy.Tell > 0 ? Color.Lerp(normal, new Color(1, .65f, .65f), .45f + .2f * Mathf.Sin(simulationTime * 25)) : normal;
            enemy.Sprite.sortingOrder = Depth(enemy.Position.y);
            float stretch = stone || disguised ? 0 : enemy.Tell > 0 ? -.1f : Mathf.Sin(enemy.Phase * 8) * .035f;
            if (enemy.Aura)
            {
                enemy.Aura.transform.position = Snap(enemy.Position);
                bool enragedAura = enemy.Health < enemy.MaxHealth * .5f;
                enemy.Aura.transform.localScale = Vector3.one * ((enragedAura ? 5.2f : 4) + Mathf.Sin(enemy.Phase * 3) * .35f + enemy.Tell);
                enemy.Aura.sharedMaterial = GlowMaterial;
                enemy.Aura.color = new Color(BossAccent.r, BossAccent.g, BossAccent.b, enragedAura ? .25f : .16f);
            }
            Color emergingColor = enemy.Sprite.color; emergingColor.a = emerged * (enemy.Kind == 17 ? .6f : 1); enemy.Sprite.color = emergingColor;
            float scale = enemy.Kind == 4 ? 1.15f : enemy.Kind == 11 ? 1.1f : enemy.Kind == 13 ? 1.15f : 1;
            enemy.Sprite.transform.localScale = new Vector3(scale * (1 - stretch), scale * (1 + stretch) * (.2f + .8f * emerged), 1);
            enemy.Shadow.transform.position = Snap(enemy.Position + Vector2.down * .25f);
            enemy.Shadow.color = new Color(1, 1, 1, enemy.Kind == 17 ? .28f : .65f);
            enemy.Ring.enabled = !disguised;
            enemy.Ring.transform.position = Snap(enemy.Position + Vector2.down * .2f);
            enemy.Ring.color = new Color(HostileAccent.r, HostileAccent.g, HostileAccent.b, enemy.Tell > 0 ? .9f : enemy.Kind == 17 ? .24f : .4f);
            enemy.Telegraph.enabled = enemy.Tell > 0;
            enemy.Telegraph.transform.position = Snap(enemy.Position + direction * .9f + Vector2.up * .15f);
            enemy.Telegraph.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            enemy.Telegraph.transform.localScale = new Vector3(enemy.Kind == 3 ? 3f : 1.5f, .6f, 1);
            enemy.Telegraph.color = new Color(1, .2f, .32f, .5f + .2f * Mathf.Sin(enemy.Phase * 25));
            PresentEnemyHitFeedback(enemy);
        }

        void UpdateVisualMotion(float dt)
        {
            roomBannerTime = Mathf.Max(0, roomBannerTime - dt);
            weaponKick = Mathf.Max(0, weaponKick - dt * 10);
            trailTimer -= dt; dustTimer -= dt;
            if (player && rollRemaining > 0 && trailTimer <= 0)
            {
                EmitRollAfterimage();
                trailTimer = .035f;
            }
            else if (player && playerMoving && dustTimer <= 0)
            {
                Emit("dust", playerPosition + Vector2.down * .3f, -lastMove * .4f, new Color(.68f, .65f, .63f, .5f), .25f, .5f, .5f);
                dustTimer = .13f;
            }
            UpdateRoomAmbience(dt);
            UpdateCamera(dt, false);
        }

        void UpdateCamera(float dt, bool snap) => UpdateDynamicCamera(dt, snap);

        void Emit(string id, Vector2 position, Vector2 velocity, Color color, float duration, float scale, float gravity, float spin = 0)
        {
            if (sparks.Count >= 320 || !actorsRoot) return;
            var sprite = effectPool.Count > 0 ? effectPool.Pop() : MakeSprite(id, position, actorsRoot.transform, 1300);
            sprite.gameObject.SetActive(true);
            sprite.sharedMaterial = id == "shot-halo" ? ProjectileLightMaterial : id == "glow" || id == "spark" || id == "muzzle" ? GlowMaterial : art.Material;
            sprite.sprite = id == "player" && player ? player.sprite : art.Get(id == "player" ? PlayerArt : id);
            sprite.flipX = id == "player" && player && player.flipX;
            sprite.flipY = false;
            sprite.transform.position = Snap(position);
            sprite.transform.rotation = id == "player" && player ? player.transform.rotation : Quaternion.identity;
            sprite.transform.localScale = Vector3.one * scale;
            sprite.color = color;
            sprite.sortingOrder = id == "dust" ? 10 : id == "player" ? Depth(position.y) - 2 : 1300;
            sparks.Add(new Spark { Sprite = sprite, Velocity = velocity, Life = duration, Duration = duration,
                Color = color, Scale = scale, Gravity = gravity, Spin = spin, Expanding = id == "effect-ring" });
        }

        void Burst(Vector2 position, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * 360f / count + simulationTime * 7;
                Emit(i % 3 == 0 ? "slash" : "spark", position, Rotate(Vector2.right, angle) * (1.5f + i % 3), color,
                    .18f + .04f * (i % 3), .25f + .08f * (i % 2), 1.5f, i % 2 == 0 ? 120 : -120);
            }
        }

        void Impact(Vector2 position, Vector2 normal, Color color, bool heavy)
        {
            PlaySound(SoundCue.Impact, heavy ? .7f : .35f);
            Color tint = Color.Lerp(color, new Color(1, .95f, .83f), .35f);
            if (heavy) RingEffect(position, tint, .55f);
            Emit("shot-halo", position, Vector2.zero, new Color(tint.r, tint.g, tint.b, heavy ? .38f : .25f),
                heavy ? .11f : .065f, heavy ? .8f : .5f, 0);
            int count = heavy ? 7 : 3;
            for (int i = 0; i < count; i++)
            {
                Color dust = new Color(tint.r, tint.g, tint.b, .55f);
                Emit("impact-dust", position, Rotate(normal, (i - count / 2) * 24) * (.8f + i % 3 * .45f), dust,
                    .10f + i % 3 * .025f, .24f + i % 2 * .05f, 2, i % 2 == 0 ? 75 : -75);
            }
            if (heavy) AddCameraTrauma(.08f);
        }

        void ShotTrail(Shot shot, Vector2 origin)
        {
            // A short translucent stroke follows travel instead of spawning stars.
            Vector2 direction = shot.Velocity.normalized;
            Color tint = Color.Lerp(shot.Color, new Color(1, .95f, .85f), .25f);
            tint.a = shot.Friendly ? .32f : .24f;
            int before = sparks.Count;
            Emit("shot-trail", origin + Vector2.up * .38f - direction * .16f, -direction * .45f,
                tint, shot.Friendly ? .1f : .08f, shot.Friendly ? .42f : .28f, 0);
            if (sparks.Count == before) return;
            var tail = sparks[sparks.Count - 1];
            tail.Aspect = Mathf.Clamp(shot.Velocity.magnitude * .08f, 1, 2.2f);
            tail.Sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            tail.Sprite.transform.localScale = new Vector3(tail.Scale * tail.Aspect, tail.Scale, 1);
        }

        void RingEffect(Vector2 position, Color color, float size = 1)
        {
            Emit("effect-ring", position, Vector2.zero, color, .35f, size, 0);
        }

        void UpdateSparks(float dt)
        {
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var spark = sparks[i];
                spark.Life -= dt;
                if (spark.Life <= 0) { RecycleSpark(i); continue; }
                spark.Velocity += Vector2.down * (spark.Gravity * dt);
                spark.Sprite.transform.position += (Vector3)(spark.Velocity * dt);
                spark.Sprite.transform.Rotate(0, 0, spark.Spin * dt);
                float fraction = spark.Life / spark.Duration;
                float size = spark.Expanding ? spark.Scale * (2 - fraction) : spark.Scale * (.6f + .4f * fraction);
                spark.Sprite.transform.localScale = new Vector3(size * spark.Aspect, size, 1);
                spark.Sprite.color = new Color(spark.Color.r, spark.Color.g, spark.Color.b, spark.Color.a * fraction);
            }
        }

        void RecycleSpark(int index)
        {
            var spark = sparks[index];
            if (spark.Sprite) { spark.Sprite.gameObject.SetActive(false); effectPool.Push(spark.Sprite); }
            sparks.RemoveAt(index);
        }

        void EnemyDeath(Enemy enemy)
        {
            if (enemy.Kind == 4) PlaySound(SoundCue.BossDeath);
            Burst(enemy.Position + Vector2.up * .4f, enemy.Kind == 1 ? new Color(.85f, .95f, 1) : HostileAccent, enemy.Kind == 4 ? 22 : 10);
            RingEffect(enemy.Position, enemy.Kind == 4 ? new Color(1, .65f, .24f) : HostileAccent, enemy.Kind == 4 ? 2.2f : .6f);
            Emit(EnemySprite(enemy.Kind), enemy.Position + Vector2.up * .4f, Vector2.up * 1.2f, new Color(.7f, .5f, .7f, .45f), .35f, .75f, 6, 90);
            if (enemy.Rift) Destroy(enemy.Rift.gameObject);
            if (enemy.Aura) Destroy(enemy.Aura.gameObject);
            Destroy(enemy.Sprite.gameObject); Destroy(enemy.Shadow.gameObject); Destroy(enemy.Ring.gameObject); Destroy(enemy.Telegraph.gameObject);
        }
    }
}
