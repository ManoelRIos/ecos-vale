using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        RoomLayout roomLayout;
        readonly Dictionary<int, RoomLayout> mapRoomLayouts = new Dictionary<int, RoomLayout>();
        readonly Dictionary<int, Texture2D> roomMapTextures = new Dictionary<int, Texture2D>();
        int roomCacheSeed, roomCacheFloor = -1;
        static readonly Vector2[] RoomSteps = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

        void InitializeRoomLayout(bool shop)
        {
            if (State != null && (roomCacheSeed != State.Seed || roomCacheFloor != State.Floor))
            {
                DisposeRoomLayoutCache();
                roomCacheSeed = State.Seed; roomCacheFloor = State.Floor;
            }
            roomLayout = shop || State == null || CurrentRoom == null ? null : LayoutForRoom(CurrentRoom);
            if (roomLayout == null) return;
            foreach (LayoutRect wall in roomLayout.Walls)
                obstacles.Add(new Rect(wall.X, wall.Y, wall.Width, wall.Height));
            foreach (LayoutRect pit in roomLayout.Pits)
                pits.Add(new Rect(pit.X, pit.Y, pit.Width, pit.Height));
        }

        RoomLayout LayoutForRoom(DungeonRoom room)
        {
            if (!mapRoomLayouts.TryGetValue(room.Id, out RoomLayout layout))
            {
                layout = RoomLayout.Generate(State.Seed, State.Floor, room.Id, room.Kind);
                mapRoomLayouts.Add(room.Id, layout);
            }
            return layout;
        }

        bool RoomContains(Vector2 point, float radius, bool allowPits = false)
        {
            if (roomLayout != null) return roomLayout.ContainsCircle(point.x, point.y, radius, allowPits);
            return Mathf.Abs(point.x) + radius <= HalfWidth && Mathf.Abs(point.y) + radius <= HalfHeight;
        }

        bool RoomTileInside(int x, int y)
            => roomLayout != null ? roomLayout.IsInside(x, y)
                : x >= -RoomLayout.MaxX && x <= RoomLayout.MaxX && y >= -RoomLayout.MaxY && y <= RoomLayout.MaxY;

        bool CanPlaceRoomDetail(Vector2 point, float radius, bool reserveRoutes = false)
        {
            if (reserveRoutes && (Mathf.Abs(point.x) < 2 || Mathf.Abs(point.y) < 1.8f)) return false;
            return RoomContains(point, radius) && !Blocked(point, radius);
        }

        Vector2 SafeSpawnPosition(Vector2 wanted, float radius, bool allowPits = false)
        {
            if (RoomContains(wanted, radius, allowPits) && !Blocked(wanted, radius)) return wanted;
            Vector2 best = Vector2.zero;
            float nearest = float.PositiveInfinity;
            for (int y = -RoomLayout.MaxY; y <= RoomLayout.MaxY; y++)
                for (int x = -RoomLayout.MaxX; x <= RoomLayout.MaxX; x++)
                {
                    var point = new Vector2(x, y);
                    if (!RoomContains(point, radius, allowPits) || Blocked(point, radius)) continue;
                    float distance = (point - wanted).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance; best = point;
                }
            // The shared central hub and its cardinal routes are kept free of cover.
            return best;
        }

        Vector2 SeekEnemyDirection(Enemy enemy, Vector2 direct, float dt)
        {
            if (roomLayout == null || enemy.Kind == 17 || direct.sqrMagnitude < .0001f) return direct;
            float radius = enemy.Kind == 4 ? .8f : enemy.Kind == 11 || enemy.Kind == 13 ? .55f
                : enemy.Kind == 15 || enemy.Kind == 16 ? .4f : enemy.Kind == 18 ? .28f : enemy.Kind == 3 ? .25f : .35f;
            bool flying = enemy.Kind == 1 || enemy.Kind == 3 || enemy.Kind == 12 || enemy.Kind == 16 || enemy.Kind == 18;
            enemy.NavTimer = Mathf.Max(0, enemy.NavTimer - dt);
            if (RoomSegmentClear(enemy.Position, playerPosition, radius, flying)) return direct;
            if (enemy.NavTimer <= 0 || Vector2.Distance(enemy.Position, enemy.NavStep) < .2f
                || !RoomContains(enemy.NavStep, radius) || Blocked(enemy.NavStep, radius))
            {
                enemy.NavTimer = .3f + enemy.Kind % 3 * .045f;
                Vector2 start = ClosestRoomNavCell(enemy.Position, radius);
                Vector2 goal = ClosestRoomNavCell(playerPosition, radius);
                int sx = Mathf.RoundToInt(start.x), sy = Mathf.RoundToInt(start.y);
                int tx = Mathf.RoundToInt(goal.x), ty = Mathf.RoundToInt(goal.y);
                if (roomLayout.TryNextStep(sx, sy, tx, ty, out GridPoint step))
                {
                    enemy.NavStep = new Vector2(step.X, step.Y);
                    if (!RoomContains(enemy.NavStep, radius) || Blocked(enemy.NavStep, radius))
                        enemy.NavStep = RuntimeRoomStep(sx, sy, tx, ty, radius, start);
                }
                else enemy.NavStep = start;
            }
            Vector2 direction = enemy.NavStep - enemy.Position;
            return direction.sqrMagnitude > .01f ? direction.normalized : direct;
        }

        Vector2 ClosestRoomNavCell(Vector2 wanted, float radius)
        {
            Vector2 best = Vector2.zero;
            float nearest = float.PositiveInfinity;
            for (int y = -RoomLayout.MaxY; y <= RoomLayout.MaxY; y++)
                for (int x = -RoomLayout.MaxX; x <= RoomLayout.MaxX; x++)
                {
                    var point = new Vector2(x, y);
                    if (!RoomContains(point, radius) || Blocked(point, radius)) continue;
                    float distance = (point - wanted).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance; best = point;
                }
            return best;
        }

        bool RoomSegmentClear(Vector2 from, Vector2 to, float radius, bool allowPits)
        {
            if (CoverBetween(from, to, radius)) return false;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) * 3));
            for (int i = 0; i <= steps; i++)
                if (!RoomContains(Vector2.Lerp(from, to, (float)i / steps), radius, allowPits)) return false;
            return true;
        }

        Vector2 RuntimeRoomStep(int sx, int sy, int tx, int ty, float radius, Vector2 fallback)
        {
            const int count = RoomLayout.Width * RoomLayout.Height;
            var parents = new int[count]; var queue = new int[count];
            for (int i = 0; i < count; i++) parents[i] = -1;
            int start = (sy + RoomLayout.MaxY) * RoomLayout.Width + sx + RoomLayout.MaxX;
            int target = (ty + RoomLayout.MaxY) * RoomLayout.Width + tx + RoomLayout.MaxX;
            if (start < 0 || start >= count || target < 0 || target >= count) return fallback;
            int head = 0, tail = 1; queue[0] = start; parents[start] = start;
            while (head < tail)
            {
                int current = queue[head++];
                if (current == target)
                {
                    if (current == start) return fallback;
                    while (parents[current] != start) current = parents[current];
                    return new Vector2(current % RoomLayout.Width - RoomLayout.MaxX, current / RoomLayout.Width - RoomLayout.MaxY);
                }
                int x = current % RoomLayout.Width - RoomLayout.MaxX, y = current / RoomLayout.Width - RoomLayout.MaxY;
                foreach (Vector2 side in RoomSteps)
                {
                    int nx = x + (int)side.x, ny = y + (int)side.y;
                    if (!roomLayout.IsInside(nx, ny) || roomLayout.IsPit(nx, ny)) continue;
                    int index = (ny + RoomLayout.MaxY) * RoomLayout.Width + nx + RoomLayout.MaxX;
                    var point = new Vector2(nx, ny);
                    if (parents[index] >= 0 || !RoomContains(point, radius) || Blocked(point, radius)) continue;
                    parents[index] = current; queue[tail++] = index;
                }
            }
            return fallback;
        }

        void RenderRoomPits()
        {
            foreach (Rect pit in pits)
            {
                Vector2 center = new Vector2(pit.center.x, pit.center.y);
                var sprite = MakeSprite("pit", center, roomRoot.transform, -165);
                sprite.transform.localScale = new Vector3(pit.width / 2, pit.height / 1.25f, 1);
                AddGroundGlow(center + Vector2.down * .35f, new Color(.45f, .6f, 1), 1.5f, .15f);
            }
        }

        void BuildContourWalls()
        {
            for (int y = -RoomLayout.MaxY - 1; y <= RoomLayout.MaxY + 1; y++)
                for (int x = -RoomLayout.MaxX - 1; x <= RoomLayout.MaxX + 1; x++)
                {
                    if (RoomTileInside(x, y)) continue;
                    bool floorBelow = RoomTileInside(x, y - 1), floorAbove = RoomTileInside(x, y + 1);
                    bool floorLeft = RoomTileInside(x - 1, y), floorRight = RoomTileInside(x + 1, y);
                    if (!floorBelow && !floorAbove && !floorLeft && !floorRight) continue;
                    bool doorGap = !InShop && (Mathf.Abs(x) > RoomLayout.MaxX && Mathf.Abs(y) < 2 && HasDoorDirection(x > 0 ? 1 : -1, 0)
                        || Mathf.Abs(y) > RoomLayout.MaxY && Mathf.Abs(x) < 2 && HasDoorDirection(0, y > 0 ? 1 : -1));
                    if (doorGap) continue;
                    Vector2 ground = new Vector2(x, y);
                    var tile = MakeSprite("wall-theme" + roomTheme, ground, roomRoot.transform, 250);
                    tile.color = Color.white;
                    var face = MakeSprite("wall-side" + roomTheme, ground + Vector2.up * .43f,
                        roomRoot.transform, Depth(ground.y));
                    face.transform.localScale = new Vector3(.5f, floorAbove ? .36f : .57f, 1);
                    face.flipX = floorRight;
                    var crown = MakeSprite("wall-crown" + roomTheme, ground + Vector2.up * (floorAbove ? .82f : 1.04f),
                        roomRoot.transform, Depth(ground.y) + 1);
                    crown.transform.localScale = new Vector3(.5f, .22f, 1);
                    raisedWalls.Add(face); raisedWalls.Add(crown);
                    if (floorBelow && (x * 17 + y * 31 + 401) % 7 == 0)
                    {
                        var relief = MakeSprite("wall-relief" + roomTheme, ground + Vector2.up * .72f,
                            roomRoot.transform, Depth(ground.y) + 2);
                        relief.transform.localScale = Vector3.one * .65f;
                        raisedWalls.Add(relief);
                    }
                }
        }

        void AddBorderDetails()
        {
            int placed = 0;
            for (int y = -7; y <= 7 && placed < 16; y += 2)
                for (int x = -13; x <= 13 && placed < 16; x += 2)
                {
                    Vector2 point = new Vector2(x, y);
                    int hash = unchecked(x * 73856093 ^ y * 19349663 ^ State.Seed ^ CurrentRoom.Id * 104729) & int.MaxValue;
                    if (hash % 3 == 0 || !CanPlaceRoomDetail(point, .8f, true)) continue;
                    bool boundary = !RoomTileInside(x - 2, y) || !RoomTileInside(x + 2, y)
                        || !RoomTileInside(x, y - 2) || !RoomTileInside(x, y + 2);
                    if (!boundary) continue;
                    string id = hash % 3 == 1 ? "gravestone" : (hash / 3) % 2 == 0 ? "fence" : "ossuary-candle";
                    var sprite = AnimatedProp(id, point + Vector2.up * .42f, Depth(point.y));
                    sprite.transform.localScale = Vector3.one * .65f;
                    obstacles.Add(new Rect(point.x - .37f, point.y - .25f, .74f, .5f));
                    MakeSprite("shadow", point + new Vector2(.18f, -.2f), roomRoot.transform, -70)
                        .transform.localScale = new Vector3(1.35f, .7f, 1);
                    if (id == "ossuary-candle") AttachFlickerLight(sprite.transform, new Color(1, .46f, .16f), 2.6f, .22f);
                    placed++;
                }
        }

        void RoomMapShape(Rect rect, DungeonRoom room, float alpha)
        {
            if (State == null) return;
            if (!roomMapTextures.TryGetValue(room.Id, out Texture2D texture))
            {
                RoomLayout layout = LayoutForRoom(room);
                const int width = RoomLayout.Width + 2, height = RoomLayout.Height + 2;
                var pixels = new Color32[width * height];
                Color theme = FloorColors[ThemeVariant];
                for (int y = -RoomLayout.MaxY; y <= RoomLayout.MaxY; y++)
                    for (int x = -RoomLayout.MaxX; x <= RoomLayout.MaxX; x++)
                    {
                        if (!layout.IsInside(x, y)) continue;
                        bool edge = !layout.IsInside(x - 1, y) || !layout.IsInside(x + 1, y)
                            || !layout.IsInside(x, y - 1) || !layout.IsInside(x, y + 1);
                        Color color = layout.IsPit(x, y) ? new Color(.07f, .04f, .14f)
                            : edge ? Color.Lerp(theme, Color.white, .35f) : theme * ((x + y) % 3 == 0 ? .55f : .45f);
                        color.a = 1;
                        pixels[(y + RoomLayout.MaxY + 1) * width + x + RoomLayout.MaxX + 1] = color;
                    }
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    name = "Haunt / room footprint " + room.Id,
                    filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixels32(pixels); texture.Apply(false, true);
                roomMapTextures.Add(room.Id, texture);
            }
            Color previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * Mathf.Clamp01(alpha));
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        void DisposeRoomLayoutCache()
        {
            foreach (Texture2D texture in roomMapTextures.Values) if (texture) Destroy(texture);
            roomMapTextures.Clear(); mapRoomLayouts.Clear();
        }
    }
}
