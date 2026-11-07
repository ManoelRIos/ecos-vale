using System;
using UnityEngine;

namespace Ecos.Haunt.Fps
{
    public sealed partial class HauntFpsGame
    {
        string LowPolyEncounterLabel => bossSpawned ? "GUARDIAO"
            : LowPolyRoom == null ? "TREINO" : LowPolyRoom.Kind == RoomKind.Entrance ? "ENTRADA"
            : LowPolyRoom.Cleared ? "SALA LIVRE" : "ONDA " + (wave + 1) + "/2";

        void DrawLowPolyShop(float width, float height)
        {
            Fill(new Rect(0, 0, width, height), new Color(.025f, .02f, .05f, .94f));
            Text(new Rect(30, 48, width - 60, 35), "MERCADO DAS ALMAS", 3, Gold, TextAnchor.MiddleCenter);
            Text(new Rect(30, 98, width - 60, 25), "ALMAS " + state.Coins, 2, Ivory, TextAnchor.MiddleCenter);
            var items = LowPolyShopItems();
            const float cardWidth = 250;
            float left = width * .5f - (items.Count * 270 - 20) * .5f;
            for (int i = 0; i < items.Count; i++)
            {
                ShopItem item = items[i]; float x = left + i * 270;
                Fill(new Rect(x, 166, cardWidth, 155), i == lowPolyShopSelection ? new Color(.2f, .17f, .25f) : new Color(.09f, .07f, .13f));
                bool isWeapon = RunState.TryWeaponForItem(item, out WeaponKind weapon);
                string name = isWeapon ? RunState.WeaponName(weapon) : "ELIXIR VITAL";
                Text(new Rect(x + 12, 185, cardWidth - 24, 43), name, 2, Ivory, TextAnchor.MiddleCenter);
                string detail = isWeapon ? WeaponAmmo.Stats(weapon).MagazineSize + " TIROS / RECARGA MANUAL" : "RECUPERA 3 DE VIDA";
                Text(new Rect(x + 12, 242, cardWidth - 24, 20), detail, 1, new Color(.65f, .76f, .73f), TextAnchor.MiddleCenter);
                bool sold = state.Sold(item) || isWeapon && state.Owns(weapon);
                string action = sold ? "ESGOTADO" : "[" + (i + 1) + "] " + state.Price(item) + " ALMAS";
                if (Button(new Rect(x + 12, 280, cardWidth - 24, 30), action, 1, i == lowPolyShopSelection) && !sold) TryBuyLowPolySlot(i);
            }
            if (noticeRemaining > 0) Text(new Rect(40, 350, width - 80, 30), notice, 2, Ivory, TextAnchor.MiddleCenter);
            Text(new Rect(30, height - 100, width - 60, 22), "CONTROLE: DIRECIONAL ESCOLHE / X COMPRA / CIRCULO FECHA", 1, Ivory, TextAnchor.MiddleCenter);
            if (Button(new Rect(width * .5f - 170, height - 65, 340, 32), "SAIR [E / ESC]", 1))
            { lowPolyShopOpen = false; UpdateSuspension(); }
        }

        void DrawLowPolyNavigation(float width, float height)
        {
            if (lowPolyShopRoot && state.BossDefeated && Vector3.Distance(PlayerPosition, ShopPosition) < 3)
                Text(new Rect(width * .5f - 220, height - 112, 440, 24), "E / QUADRADO - FALAR COM O COLETOR", 1, Gold, TextAnchor.MiddleCenter);
            else if (!exitReady && world.TryGetNearbyDoor(PlayerPosition, 2.6f, out int destination, out Vector3 spawn))
            {
                string action = LowPolyFloor.GetRoom(destination).Kind == RoomKind.Boss ? "ENTRAR NO CHEFE" : "ATRAVESSAR A PORTA";
                Text(new Rect(width * .5f - 220, height - 112, 440, 24), "E / QUADRADO - " + action, 1, Gold, TextAnchor.MiddleCenter);
            }
            else if (exitReady || LowPolyRoom != null && LowPolyRoom.Cleared)
            {
                Vector3 target = world.ExitPosition; bool found = exitReady; float nearest = float.MaxValue;
                if (!exitReady)
                    foreach (int id in LowPolyRoom.Neighbors)
                    {
                        var next = LowPolyFloor.GetRoom(id);
                        Vector3 door = world.transform.TransformPoint(new Vector3((next.Position.X - LowPolyRoom.Position.X) * 12,
                            0, (next.Position.Y - LowPolyRoom.Position.Y) * 10));
                        float distance = Vector3.Distance(PlayerPosition, door);
                        if (distance >= nearest) continue;
                        nearest = distance; target = door; found = true;
                    }
                if (found && Vector3.Distance(PlayerPosition, target) > 2.8f)
                {
                    Vector3 direction = (target - PlayerPosition).normalized;
                    float forward = Vector3.Dot(player.transform.forward, direction);
                    string arrow = forward > .55f ? "^" : forward < -.55f ? "V" : Vector3.Dot(player.transform.right, direction) < 0 ? "<" : ">";
                    Text(new Rect(width * .5f - 120, height - 128, 240, 24), arrow + (exitReady ? " PORTAL" : " PORTA"), 2, Gold, TextAnchor.MiddleCenter);
                }
            }
            if (!lowPolyMapOpen || LowPolyFloor == null || LowPolyRoom == null) return;
            Rect panel = new Rect(width - 360, 88, 336, 266);
            Fill(panel, new Color(.025f, .02f, .045f, .94f));
            Text(new Rect(panel.x + 12, panel.y + 12, panel.width - 24, 18), "MAPA / TAB FECHA", 1, Ivory);
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            foreach (var room in LowPolyFloor.Rooms)
            {
                minX = Math.Min(minX, room.Position.X); maxX = Math.Max(maxX, room.Position.X);
                minY = Math.Min(minY, room.Position.Y); maxY = Math.Max(maxY, room.Position.Y);
            }
            float spacing = Mathf.Min(44, Mathf.Min(270f / (maxX - minX + 1), 188f / (maxY - minY + 1)));
            Vector2 center = new Vector2(panel.x + panel.width * .5f, panel.y + 147);
            Vector2 Node(DungeonRoom room) => center + new Vector2((room.Position.X - (minX + maxX) * .5f) * spacing,
                -(room.Position.Y - (minY + maxY) * .5f) * spacing);
            bool Known(DungeonRoom room)
            {
                if (lowPolyVisited.Contains(room.Id)) return true;
                foreach (int neighbor in room.Neighbors) if (lowPolyVisited.Contains(neighbor)) return true;
                return false;
            }
            foreach (var room in LowPolyFloor.Rooms)
            {
                if (!Known(room)) continue;
                Vector2 a = Node(room);
                foreach (int neighbor in room.Neighbors)
                {
                    var next = LowPolyFloor.GetRoom(neighbor); if (!Known(next)) continue;
                    Vector2 b = Node(next);
                    Fill(new Rect(Mathf.Min(a.x, b.x) - 1, Mathf.Min(a.y, b.y) - 1,
                        Mathf.Max(3, Mathf.Abs(a.x - b.x)), Mathf.Max(3, Mathf.Abs(a.y - b.y))), new Color(.32f, .35f, .43f));
                }
            }
            foreach (var room in LowPolyFloor.Rooms)
            {
                if (!Known(room)) continue;
                Vector2 point = Node(room);
                Color tint = room == LowPolyRoom ? Gold : room.Cleared ? new Color(.28f, .72f, .55f)
                    : room.Kind == RoomKind.Boss ? new Color(.8f, .22f, .3f) : new Color(.36f, .4f, .5f);
                Fill(new Rect(point.x - 8, point.y - 6, 16, 12), tint);
                if (room.Kind == RoomKind.Boss) Text(new Rect(point.x - 6, point.y - 6, 12, 12), "!", 1, Ivory, TextAnchor.MiddleCenter);
            }
        }
    }
}
