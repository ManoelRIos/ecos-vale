using System.Collections.Generic;
using UnityEngine;
using Ecos.Haunt.Fps;

namespace Ecos.Haunt.LowPoly
{
    /// <summary>Physical, connected exits for the currently instantiated room.
    /// A dark outer threshold keeps the actor inside until interaction performs
    /// the room transfer; the announced inner seal locks during combat.</summary>
    public sealed class HauntLowPolyDoors
    {
        sealed class Door
        {
            public int RoomId;
            public Vector3 Position, DestinationSpawn;
            public GameObject Seal;
        }
        readonly List<Door> doors = new List<Door>();
        readonly Transform geometry;
        public bool IsOpen { get; private set; }
        public int Count => doors.Count;

        public HauntLowPolyDoors(HauntFpsGame game, Transform root, Material wall, Material trim, Material dark)
        {
            geometry = root;
            DungeonFloor floor = game.LowPolyFloor;
            DungeonRoom room = game.LowPolyRoom;
            for (int cardinal = 0; cardinal < 4; cardinal++)
            {
                int dx = cardinal == 1 ? 1 : cardinal == 3 ? -1 : 0;
                int dz = cardinal == 0 ? 1 : cardinal == 2 ? -1 : 0;
                DungeonRoom neighbor = null;
                if (floor != null && room != null)
                    foreach (int id in room.Neighbors)
                    {
                        DungeonRoom candidate = floor.GetRoom(id);
                        if (candidate.Position.X - room.Position.X == dx && candidate.Position.Y - room.Position.Y == dz)
                        { neighbor = candidate; break; }
                    }
                bool northSouth = dz != 0;
                float span = northSouth ? HauntFpsWorld.Width : HauntFpsWorld.Depth;
                Vector3 normal = new Vector3(dx, 0, dz);
                Vector3 tangent = northSouth ? Vector3.right : Vector3.forward;
                float edge = northSouth ? HauntFpsWorld.Depth * .5f : HauntFpsWorld.Width * .5f;
                string direction = cardinal == 0 ? "norte" : cardinal == 1 ? "leste" : cardinal == 2 ? "sul" : "oeste";
                Vector3 wallAt = normal * (edge + .2f) + Vector3.up * 2;
                if (neighbor == null)
                {
                    game.Polygons.Part("Parede " + direction, wallAt, SpanSize(northSouth, span, 4, .4f), wall, root, true, LowPolyShape.Box);
                    game.Polygons.Part("Cornija " + direction, normal * (edge + .15f) + Vector3.up * 4.1f,
                        SpanSize(northSouth, span + .2f, .2f, .65f), trim, root, false, LowPolyShape.BevelBox);
                    continue;
                }

                const float gap = 3;
                float segment = (span - gap) * .5f;
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 at = wallAt + tangent * (sign * (gap + segment) * .5f);
                    game.Polygons.Part("Parede " + direction + " / lateral", at, SpanSize(northSouth, segment, 4, .4f), wall, root, true, LowPolyShape.Box);
                    game.Polygons.Part("Cornija " + direction + " / lateral", at + Vector3.up * 2.1f,
                        SpanSize(northSouth, segment + .1f, .2f, .65f), trim, root, false, LowPolyShape.BevelBox);
                    game.Polygons.Part("Porta / coluna " + direction, normal * edge + tangent * (sign * 1.72f) + Vector3.up * 1.75f,
                        SpanSize(northSouth, .42f, 3.5f, .7f), trim, root, true, LowPolyShape.BevelBox);
                }
                game.Polygons.Part("Porta / arco " + direction, normal * edge + Vector3.up * 3.5f,
                    SpanSize(northSouth, 3.85f, .45f, .8f), trim, root, false, LowPolyShape.BevelBox);
                game.Polygons.Part("Porta / limiar " + direction, normal * (edge + .3f) + Vector3.up * 1.6f,
                    SpanSize(northSouth, 3.05f, 3.2f, .12f), dark, root, true, LowPolyShape.Box);
                Color accent = neighbor.Cleared ? new Color(.25f, .95f, .57f)
                    : neighbor.Kind == RoomKind.Boss ? new Color(1, .26f, .13f) : new Color(.52f, .4f, 1);
                Material marker = game.PixelMaterial("porta / destino " + neighbor.Id + " / " + game.State.Floor, accent);
                marker.SetColor("_BaseColor", accent);
                marker.SetColor("_EmissionColor", accent * 1.1f);
                game.Polygons.Part("Porta / cristal " + direction, normal * (edge - .28f) + Vector3.up * 3.58f,
                    new Vector3(.31f, .55f, .31f), marker, root, false, LowPolyShape.Crystal);
                Material lockMaterial = game.PixelMaterial("porta / combate fechado", new Color(.43f, .12f, .16f));
                lockMaterial.SetColor("_EmissionColor", new Color(.25f, .035f, .02f));
                GameObject seal = game.Polygons.Part("Porta / selo " + direction, normal * (edge - .1f) + Vector3.up * 1.6f,
                    SpanSize(northSouth, 3.02f, 3.2f, .24f), lockMaterial, root, true, LowPolyShape.BevelBox);
                doors.Add(new Door
                {
                    RoomId = neighbor.Id,
                    Position = normal * (edge - 1.4f),
                    DestinationSpawn = -normal * (edge - 2.2f),
                    Seal = seal
                });
            }
            SetOpen(false);
        }

        static Vector3 SpanSize(bool northSouth, float width, float height, float thickness)
            => northSouth ? new Vector3(width, height, thickness) : new Vector3(thickness, height, width);

        public void SetOpen(bool open)
        {
            IsOpen = open;
            foreach (var door in doors) if (door.Seal) door.Seal.SetActive(!open);
            Physics.SyncTransforms();
        }

        public bool TryGetNearby(Vector3 player, float distance, out int roomId, out Vector3 returnSpawn)
        {
            roomId = -1; returnSpawn = Vector3.zero;
            if (!IsOpen || !(distance > 0) || float.IsInfinity(distance)) return false;
            float nearest = distance * distance;
            foreach (var door in doors)
            {
                Vector3 center = geometry.TransformPoint(door.Position);
                Vector3 delta = player - center; delta.y = 0;
                if (delta.sqrMagnitude > nearest) continue;
                nearest = delta.sqrMagnitude; roomId = door.RoomId;
                returnSpawn = geometry.TransformPoint(door.DestinationSpawn);
            }
            return roomId >= 0;
        }

        public void Clear() { doors.Clear(); IsOpen = false; }
    }
}
