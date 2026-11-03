using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        static readonly string[] EnemyArt = { "pumpkin", "ghost", "cultist", "bat", "boss" };
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
        string RoomThemeName => InShop || State == null ? "MERCADO DAS ALMAS" : CurrentRoom.Kind == RoomKind.Boss
            ? "SANTUÁRIO DO GUARDIÃO" : CurrentRoom.Kind == RoomKind.Entrance ? "LIMIAR DAS LANTERNAS"
            : roomTheme == 0 ? "CRIPTA DAS RAÍZES" : roomTheme == 1 ? "CAPELA DAS CINZAS" : "OSSUÁRIO LUNAR";

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
            roomTheme = shop ? 1 : CurrentRoom.Kind == RoomKind.Boss ? 2 : CurrentRoom.Id % 3;
            Color floorTint = shop ? new Color(1, .89f, .76f) : roomTheme == 0 ? new Color(.78f, .91f, .83f)
                : roomTheme == 1 ? new Color(.94f, .83f, .86f) : new Color(.78f, .84f, 1f);
            Color wallTint = Color.Lerp(floorTint, Color.white, .5f);
            for (int y = -6; y <= 6; y++)
                for (int x = -11; x <= 11; x++)
                {
                    bool wall = Mathf.Abs(x) == 11 || Mathf.Abs(y) == 6;
                    var tile = MakeSprite(wall ? "wall" : "floor" + roomRandom.Next(6), new Vector2(x, y), roomRoot.transform, wall ? 300 : -200);
                    tile.color = wall ? wallTint : floorTint;
                    if (!wall && (Mathf.Abs(x) == 10 || Mathf.Abs(y) == 5)) tile.color = floorTint * .82f;
                    if (wall && y == -6) tile.color = wallTint * .8f;
                }
            // A distinct center and border give the eye a room shape without filling
            // the combat lanes with clutter or changing the collision layout.
            if (shop)
            {
                var rug = MakeSprite("rug", new Vector2(0, -.6f), roomRoot.transform, -180);
                rug.transform.localScale = new Vector3(2.6f, 2.5f, 1);
                rug.color = new Color(.82f, .68f, .8f);
            }
            else
            {
                var rune = MakeSprite("rune", Vector2.zero, roomRoot.transform, -180);
                rune.transform.localScale = Vector3.one * (CurrentRoom.Kind == RoomKind.Boss ? 1.65f : 1.05f);
                rune.color = roomTheme == 0 ? new Color(.35f, .68f, .54f, .22f) : roomTheme == 1
                    ? new Color(.84f, .39f, .39f, .22f) : new Color(.59f, .53f, .95f, .24f);
                if (CurrentRoom.Kind == RoomKind.Entrance)
                {
                    var runner = MakeSprite("rug", new Vector2(0, -2.3f), roomRoot.transform, -179);
                    runner.transform.localScale = new Vector3(.65f, 2.5f, 1);
                    runner.color = new Color(.46f, .74f, .7f, .8f);
                }
            }
            if (!shop && CurrentRoom.Kind != RoomKind.Entrance)
            {
                bool boss = CurrentRoom.Kind == RoomKind.Boss;
                int layout = roomRandom.Next(3);
                for (int side = -1; side <= 1; side += 2)
                    for (int row = -1; row <= 1; row += 2)
                    {
                        Vector2 p = new Vector2(side * (layout == 1 ? 4.5f : 3.5f), row * (boss ? 2.9f : layout == 2 ? 2.8f : 2.2f));
                        obstacles.Add(new Rect(p.x - .45f, p.y - .38f, .9f, .76f));
                        MakeSprite("shadow", p + Vector2.down * .35f, roomRoot.transform, 0).transform.localScale = new Vector3(1.8f, .8f, 1);
                        var prop = MakeSprite(layout == 2 && !boss ? "grave" : "pillar", p + Vector2.up * .4f, roomRoot.transform, Depth(p.y));
                        prop.color = wallTint;
                    }
            }
            for (int side = -1; side <= 1; side += 2)
                for (int row = -1; row <= 1; row += 2)
                {
                    Vector2 p = new Vector2(side * 9f, row * 4.35f);
                    AnimatedProp("candle", p, Depth(p.y));
                    var glow = MakeSprite("glow", p + Vector2.up * .35f, roomRoot.transform, -170);
                    glow.transform.localScale = Vector3.one * 3.3f;
                    glow.color = new Color(1, .49f, .18f, .17f);
                    roomGlows.Add(glow);
                    // Graves remain stone silhouettes: no decorative enemy sprites.
                    MakeSprite("grave", new Vector2(side * 7.8f, row * 4.75f), roomRoot.transform, Depth(row * 4.75f)).color = wallTint;
                }
            if (shop)
            {
                AnimatedProp("merchant", new Vector2(0, 2.7f), Depth(2.3f));
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = ShopPosition(i);
                    MakeSprite("pillar", p + Vector2.down * .2f, roomRoot.transform, Depth(p.y)).transform.localScale = new Vector3(.85f, .6f, 1);
                    string[] icons = { "heart", "weapon", "muzzle", "heart" };
                    MakeSprite(icons[i], p + Vector2.up * .6f, roomRoot.transform, Depth(p.y) + 1).transform.localScale = Vector3.one * 1.3f;
                    AddGroundGlow(p, PlayerAccent, 1.5f, .13f);
                }
                portal = AnimatedProp("portal", new Vector2(8.4f, 0), 450);
                AddGroundGlow(new Vector2(8.4f, 0), PlayerAccent, 3, .2f);
            }
            else
            {
                foreach (int id in CurrentRoom.Neighbors)
                {
                    DungeonRoom target = Floor.GetRoom(id);
                    int dx = target.Position.X - CurrentRoom.Position.X, dy = target.Position.Y - CurrentRoom.Position.Y;
                    Vector2 position = new Vector2(dx * 10.45f, dy * 5.45f);
                    var door = MakeSprite("door-open", position, roomRoot.transform, 500);
                    door.transform.rotation = Quaternion.Euler(0, 0, dx > 0 ? -90 : dx < 0 ? 90 : dy < 0 ? 180 : 0);
                    doors.Add(door);
                    var glow = MakeSprite("glow", position - new Vector2(dx, dy) * .35f, roomRoot.transform, -160);
                    glow.transform.localScale = Vector3.one * 2.7f;
                    doorGlows.Add(glow);
                }
                if (CurrentRoom.Kind == RoomKind.Boss && CurrentRoom.Cleared)
                    portal = AnimatedProp("portal", new Vector2(0, 1.4f), 450);
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
            player.sprite = art.Frame("player", frame);
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
            if (InShop && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                for (int i = 0; i < 4; i++)
                    if (Vector2.Distance(playerPosition, ShopPosition(i)) < 1.5f) { Buy((ShopItem)i); break; }
        }

        void PresentEnemy(Enemy enemy, Vector2 direction)
        {
            int frame = Mathf.FloorToInt(enemy.Phase * (enemy.Kind == 3 ? 10 : 6));
            enemy.Sprite.sprite = art.Frame(EnemyArt[enemy.Kind], frame);
            float floatHeight = enemy.Kind == 1 || enemy.Kind == 3 ? Mathf.Sin(enemy.Phase * 4) * .12f : Mathf.Sin(enemy.Phase * 5) * .025f;
            enemy.Sprite.transform.position = Snap(enemy.Position + Vector2.up * (.38f + floatHeight));
            enemy.Sprite.flipX = direction.x < 0;
            Color normal = enemy.Kind == 4 && State.Floor % 3 == 2 ? new Color(.72f, 1, .97f)
                : enemy.Kind == 4 && State.Floor % 3 == 0 ? new Color(1, .74f, .91f) : Color.white;
            enemy.Sprite.color = enemy.HurtFlash > 0 ? new Color(1, .75f, .7f)
                : enemy.Tell > 0 ? Color.Lerp(normal, new Color(1, .65f, .65f), .45f + .2f * Mathf.Sin(simulationTime * 25)) : normal;
            enemy.Sprite.sortingOrder = Depth(enemy.Position.y);
            float stretch = enemy.Tell > 0 ? -.1f : Mathf.Sin(enemy.Phase * 8) * .035f;
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
            float height = Mathf.Max(7.9f, 12.4f / Mathf.Max(.5f, view.aspect));
            view.orthographicSize = height;
            Vector2 offset = new Vector2(Mathf.Sin(simulationTime * 143), Mathf.Cos(simulationTime * 157)) * (shake * .2f);
            view.transform.position = new Vector3(Mathf.Round(offset.x * 32) / 32f, Mathf.Round(offset.y * 32) / 32f + .3f, -20);
        }

        void Emit(string id, Vector2 position, Vector2 velocity, Color color, float duration, float scale, float gravity, float spin = 0)
        {
            if (sparks.Count >= 160 || !actorsRoot) return;
            var sprite = effectPool.Count > 0 ? effectPool.Pop() : MakeSprite(id, position, actorsRoot.transform, 1300);
            sprite.gameObject.SetActive(true);
            sprite.sprite = id == "player" && player ? player.sprite : art.Get(id);
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
            Emit(EnemyArt[enemy.Kind], enemy.Position + Vector2.up * .4f, Vector2.up * 1.2f, new Color(.7f, .5f, .7f, .45f), .35f, .75f, 6, 90);
            Destroy(enemy.Sprite.gameObject); Destroy(enemy.Shadow.gameObject); Destroy(enemy.Ring.gameObject); Destroy(enemy.Telegraph.gameObject);
        }
    }
}
