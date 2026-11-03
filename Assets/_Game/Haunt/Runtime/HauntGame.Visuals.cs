using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        static readonly string[] EnemyArt = { "pumpkin", "ghost", "cultist", "bat", "boss", "larva", "sentinel", "acolyte" };
        static readonly Color PlayerAccent = new Color(.32f, 1f, .85f);
        static readonly Color HostileAccent = new Color(1f, .31f, .43f);
        readonly List<RoomAnimation> roomAnimations = new List<RoomAnimation>();
        readonly List<SpriteRenderer> roomGlows = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> doorGlows = new List<SpriteRenderer>();
        readonly Stack<SpriteRenderer> effectPool = new Stack<SpriteRenderer>();
        SpriteRenderer playerMarker, playerGlow, crosshair;
        float walkDistance, weaponKick, trailTimer, dustTimer, roomBannerTime;
        bool playerMoving;
        int roomTheme;
        static readonly string[] FloorThemes = { "JARDINS DA COLHEITA", "GALERIA DOS VÉUS", "CATEDRAL DAS CINZAS", "NINHO DA PESTE", "ABISMO DOS SINOS" };
        static readonly Color[] FloorColors = { new Color(.88f, .98f, .84f), new Color(.78f, .9f, 1), new Color(1, .78f, .72f), new Color(.72f, .91f, .55f), new Color(.83f, .65f, .92f) };
        string PlayerArt => State == null || State.Character == CharacterKind.Hunter ? "player" : State.Character == CharacterKind.Medium ? "medium" : State.Character == CharacterKind.Reaper ? "reaper" : "alchemist";
        Color CharacterAccent => State == null || State.Character == CharacterKind.Hunter ? PlayerAccent : State.Character == CharacterKind.Medium ? new Color(.82f, .58f, 1) : State.Character == CharacterKind.Reaper ? new Color(.6f, .8f, 1) : new Color(.65f, 1, .35f);
        string WeaponArt => State.Weapon == WeaponKind.Scattergun ? "scattergun" : State.Weapon == WeaponKind.Repeater ? "repeater" : State.Weapon == WeaponKind.SoulCannon ? "cannon" : "weapon";
        string RoomThemeName => InShop || State == null ? "MERCADO DAS ALMAS" : FloorThemes[BossVariant];

        sealed class RoomAnimation
        {
            public SpriteRenderer Sprite;
            public string Id;
            public float Offset;
        }

        static int Depth(float groundY) => 1000 - Mathf.RoundToInt(groundY * 16);

        void BuildRoom(bool shop)
        {
            if (roomRoot) { roomRoot.SetActive(false); Destroy(roomRoot); }
            roomRoot = new GameObject(shop ? "Mercado das Almas" : "Cripta • sala " + CurrentRoom.Id);
            roomRoot.transform.SetParent(transform, false);
            doors.Clear(); doorGlows.Clear(); obstacles.Clear(); roomAnimations.Clear(); roomGlows.Clear(); portal = null;
            int seed = State == null ? 71319 : unchecked(State.Seed * 397 ^ State.Floor * 7919 ^ CurrentRoom.Id * 104729);
            roomRandom = new System.Random(seed);
            roomTheme = shop ? 0 : BossVariant;
            Color floorTint = shop ? new Color(.95f, .89f, .78f) : Color.white * Mathf.Lerp(1, .88f, Mathf.Clamp01((State.Floor - 1) / 12f));
            floorTint.a = 1;
            int wallX = Mathf.CeilToInt(HalfWidth), wallY = Mathf.CeilToInt(HalfHeight);
            for (int y = -wallY; y <= wallY; y++)
                for (int x = -wallX; x <= wallX; x++)
                {
                    bool wall = Mathf.Abs(x) == wallX || Mathf.Abs(y) == wallY;
                    string id = wall ? "wall-theme" + roomTheme : "floor-theme" + roomTheme;
                    var tile = MakeSprite(id, new Vector2(x, y), roomRoot.transform, wall ? 300 : -200);
                    tile.flipX = (x + y) % 2 == 0;
                    tile.color = wall ? Color.white : floorTint;
                    if (!wall && (Mathf.Abs(x) == wallX - 1 || Mathf.Abs(y) == wallY - 1)) tile.color = floorTint * .85f;
                }
            if (shop)
            {
                var rug = MakeSprite("rug", new Vector2(0, 1), roomRoot.transform, -180);
                rug.transform.localScale = new Vector3(4.5f, 3.8f, 1);
                rug.color = new Color(.75f, .6f, .8f);
                AnimatedProp("merchant", new Vector2(0, 4), Depth(4));
                // The screen is the shop. World stalls no longer duplicate its seven cards.
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
                            Vector2 point = new Vector2(side * x, row * y);
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
                        var prop = MakeSprite("decor-theme" + roomTheme, point, roomRoot.transform, Depth(point.y));
                        prop.transform.localScale = Vector3.one * 1.1f;
                        AddGroundGlow(point, accent, 3.5f, .15f);
                    }
                for (int i = 0; i < 9 + Mathf.Min(State.Floor, 12); i++)
                {
                    Vector2 point = new Vector2((float)roomRandom.NextDouble() * 26 - 13, (float)roomRandom.NextDouble() * 14 - 7);
                    var stain = MakeSprite(roomTheme >= 3 ? "rune" : "dust", point, roomRoot.transform, -190);
                    stain.color = new Color(accent.r, accent.g, accent.b, .13f);
                    stain.transform.localScale = Vector3.one * (roomTheme >= 3 ? .6f : 1.5f);
                }
            }
            for (int side = -1; side <= 1; side += 2)
                for (int row = -1; row <= 1; row += 2)
                {
                    Vector2 point = new Vector2(side * 11, row * 7.2f);
                    AnimatedProp("candle", point, Depth(point.y));
                    AddGroundGlow(point, shop ? new Color(1, .7f, .3f) : FloorColors[roomTheme], 3.8f, .17f);
                }
            UpdateRoomAmbience(0);
        }

        SpriteRenderer AnimatedProp(string id, Vector2 position, int depth)
        {
            var sprite = MakeSprite(id, position, roomRoot.transform, depth);
            roomAnimations.Add(new RoomAnimation { Sprite = sprite, Id = id, Offset = (float)roomRandom.NextDouble() * 4 });
            return sprite;
        }

        void AddGroundGlow(Vector2 position, Color color, float size, float opacity)
        {
            var glow = MakeSprite("glow", position, roomRoot.transform, -170);
            glow.transform.localScale = Vector3.one * size;
            glow.color = new Color(color.r, color.g, color.b, opacity);
            roomGlows.Add(glow);
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
            if (Playing && !InShop && dt > 0 && Mathf.FloorToInt(simulationTime * 12) != Mathf.FloorToInt((simulationTime - dt) * 12))
            {
                // Time-derived positions keep visual ambience out of the seeded spawn RNG.
                float t = simulationTime;
                Vector2 p = new Vector2(Mathf.Sin(t * 5.13f) * 13, Mathf.Cos(t * 3.71f) * 7);
                Color c = FloorColors[BossVariant]; c.a = .32f;
                Emit("spark", p, Vector2.up * .25f, c, 1.3f, .16f, 0);
            }
            foreach (var prop in roomAnimations)
                if (prop.Sprite) prop.Sprite.sprite = art.Frame(prop.Id, Mathf.FloorToInt(simulationTime * 6 + prop.Offset));
            for (int i = 0; i < roomGlows.Count; i++)
            {
                var glow = roomGlows[i];
                if (!glow) continue;
                Color color = glow.color;
                color.a = .15f + .035f * Mathf.Sin(simulationTime * 3.7f + i * 2);
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
            Cursor.visible = InShop || PointerOverInterface();
            Vector2 aim = AimDirection();
            bool rolling = rollRemaining > 0;
            int frame = playerMoving ? Mathf.FloorToInt(walkDistance * 4.5f) : Mathf.FloorToInt(simulationTime * 2) % 2 * 2;
            if (muzzleTime > 0) frame = 2;
            player.sprite = art.Frame(PlayerArt, frame);
            gun.sprite = art.Get(WeaponArt);
            float bob = rolling ? 0 : playerMoving ? Mathf.Sin(walkDistance * 14) * .045f : Mathf.Sin(simulationTime * 2) * .02f;
            player.transform.position = Snap(playerPosition + Vector2.up * (.38f + bob));
            player.sortingOrder = Depth(playerPosition.y);
            player.flipX = aim.x < 0;
            player.color = flash > 0 ? new Color(1, .6f, .65f) : invincible > 0 && Mathf.Sin(simulationTime * 30) > 0
                ? new Color(.65f, 1, .9f, .72f) : Color.white;
            float tilt = rolling ? (1 - rollRemaining / RollTime) * (rollDirection.x < 0 ? -360 : 360) : playerMoving ? Mathf.Sin(walkDistance * 8) * 3 : 0;
            player.transform.rotation = Quaternion.Euler(0, 0, tilt);
            player.transform.localScale = rolling ? new Vector3(1.05f, .78f, 1) : new Vector3(1, 1 + Mathf.Sin(simulationTime * 2) * .015f, 1);
            shadow.transform.position = Snap(playerPosition + Vector2.down * .25f);
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
            muzzle.transform.localScale = Vector3.one * (.7f + weaponKick * .3f);
            muzzle.transform.rotation = gun.transform.rotation;
            if (crosshair)
            {
                crosshair.enabled = !PointerOverInterface() && !InShop;
                if (Mouse.current != null) crosshair.transform.position = Snap(view.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
                crosshair.transform.localScale = Vector3.one * .35f;
                crosshair.color = new Color(PlayerAccent.r, PlayerAccent.g, PlayerAccent.b, .85f);
            }

        }

        void PresentEnemy(Enemy enemy, Vector2 direction)
        {
            int frame = enemy.Kind == 4 && enemy.Cooldown < .5f ? 2 : Mathf.FloorToInt(enemy.Phase * (enemy.Kind == 4 ? 4 : enemy.Kind == 3 ? 10 : 6));
            enemy.Sprite.sprite = art.Frame(EnemySprite(enemy.Kind), frame);
            float floatHeight = enemy.Kind == 1 || enemy.Kind == 3 ? Mathf.Sin(enemy.Phase * 4) * .12f : Mathf.Sin(enemy.Phase * 5) * .025f;
            enemy.Sprite.transform.position = Snap(enemy.Position + Vector2.up * (.38f + floatHeight));
            enemy.Sprite.flipX = direction.x < 0;
            Color normal = enemy.Kind == 4 && BossVariant == 1 ? new Color(.72f, 1, .97f)
                : enemy.Kind == 4 && BossVariant == 2 ? new Color(1, .74f, .91f) : Color.white;
            enemy.Sprite.color = enemy.HurtFlash > 0 ? new Color(1, .75f, .7f)
                : enemy.Tell > 0 ? Color.Lerp(normal, new Color(1, .65f, .65f), .45f + .2f * Mathf.Sin(simulationTime * 25)) : normal;
            enemy.Sprite.sortingOrder = Depth(enemy.Position.y);
            float stretch = enemy.Tell > 0 ? -.1f : Mathf.Sin(enemy.Phase * 8) * .035f;
            if (enemy.Aura)
            {
                enemy.Aura.transform.position = Snap(enemy.Position);
                enemy.Aura.transform.localScale = Vector3.one * (4 + Mathf.Sin(enemy.Phase * 3) * .35f + enemy.Tell);
            }
            float scale = enemy.Kind == 4 ? 1.15f : 1;
            enemy.Sprite.transform.localScale = new Vector3(scale * (1 - stretch), scale * (1 + stretch), 1);
            enemy.Shadow.transform.position = Snap(enemy.Position + Vector2.down * .25f);
            enemy.Ring.transform.position = Snap(enemy.Position + Vector2.down * .2f);
            enemy.Ring.color = new Color(HostileAccent.r, HostileAccent.g, HostileAccent.b, enemy.Tell > 0 ? .9f : .4f);
            enemy.Telegraph.enabled = enemy.Tell > 0;
            enemy.Telegraph.transform.position = Snap(enemy.Position + direction * .9f + Vector2.up * .15f);
            enemy.Telegraph.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            enemy.Telegraph.transform.localScale = new Vector3(enemy.Kind == 3 ? 3f : 1.5f, .6f, 1);
            enemy.Telegraph.color = new Color(1, .2f, .32f, .5f + .2f * Mathf.Sin(enemy.Phase * 25));
        }

        void UpdateVisualMotion(float dt)
        {
            roomBannerTime = Mathf.Max(0, roomBannerTime - dt);
            weaponKick = Mathf.Max(0, weaponKick - dt * 10);
            trailTimer -= dt; dustTimer -= dt;
            if (player && rollRemaining > 0 && trailTimer <= 0)
            {
                Emit("player", playerPosition + Vector2.up * .38f, Vector2.zero, new Color(.25f, 1, .85f, .32f), .18f, .95f, 0);
                trailTimer = .045f;
            }
            else if (player && playerMoving && dustTimer <= 0)
            {
                Emit("dust", playerPosition + Vector2.down * .3f, -lastMove * .4f, new Color(.68f, .65f, .63f, .5f), .25f, .5f, .5f);
                dustTimer = .13f;
            }
            UpdateRoomAmbience(dt);
            float height = Mathf.Max(HalfHeight + 2.2f, (HalfWidth + 2) / Mathf.Max(.5f, view.aspect));
            view.orthographicSize = height;
            Vector2 offset = new Vector2(Mathf.Sin(simulationTime * 143), Mathf.Cos(simulationTime * 157)) * (shake * .2f);
            view.transform.position = new Vector3(Mathf.Round(offset.x * 32) / 32f, Mathf.Round(offset.y * 32) / 32f + .3f, -20);
        }

        void Emit(string id, Vector2 position, Vector2 velocity, Color color, float duration, float scale, float gravity, float spin = 0)
        {
            if (sparks.Count >= 320 || !actorsRoot) return;
            var sprite = effectPool.Count > 0 ? effectPool.Pop() : MakeSprite(id, position, actorsRoot.transform, 1300);
            sprite.gameObject.SetActive(true);
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
            // Short impact core plus directional shards: readable against every floor.
            RingEffect(position, color, heavy ? .8f : .25f);
            Emit("glow", position, Vector2.zero, new Color(color.r, color.g, color.b, .55f), .1f, heavy ? 1.4f : .6f, 0);
            int count = heavy ? 15 : 7;
            for (int i = 0; i < count; i++)
                Emit(i % 3 == 0 ? "slash" : "spark", position, Rotate(normal, (i - count / 2) * 19) * (1.5f + i % 4), color, .2f + i % 3 * .04f, .16f + i % 2 * .12f, 3, i % 2 == 0 ? 160 : -160);
            if (heavy) shake = Mathf.Max(shake, .12f);
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
                spark.Sprite.transform.localScale = Vector3.one * size;
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
            Burst(enemy.Position + Vector2.up * .4f, enemy.Kind == 1 ? new Color(.85f, .95f, 1) : HostileAccent, enemy.Kind == 4 ? 22 : 10);
            RingEffect(enemy.Position, enemy.Kind == 4 ? new Color(1, .65f, .24f) : HostileAccent, enemy.Kind == 4 ? 2.2f : .6f);
            Emit(EnemySprite(enemy.Kind), enemy.Position + Vector2.up * .4f, Vector2.up * 1.2f, new Color(.7f, .5f, .7f, .45f), .35f, .75f, 6, 90);
            if (enemy.Aura) Destroy(enemy.Aura.gameObject);
            Destroy(enemy.Sprite.gameObject); Destroy(enemy.Shadow.gameObject); Destroy(enemy.Ring.gameObject); Destroy(enemy.Telegraph.gameObject);
        }
    }
}
