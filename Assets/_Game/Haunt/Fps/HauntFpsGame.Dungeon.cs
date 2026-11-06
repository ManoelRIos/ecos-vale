using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt.Fps
{
    /// <summary>Room-to-room flow of a floor: walking into an uncleared room shuts its grates
    /// and starts its waves; clearing it opens the way and drops souls. One room sells goods,
    /// another holds a chest, and the boss room opens with a short introduction.</summary>
    public sealed partial class HauntFpsGame
    {
        /// <summary>Length of the boss introduction: camera pan, letterbox and title card.</summary>
        public const float BossIntroDuration = 3.2f;
        const float PickupMagnet = 3.4f, PickupReach = .75f, InteractReach = 1.9f;
        static readonly ShopItem[] ShopStock = { ShopItem.Heal, ShopItem.MaxHealth, ShopItem.Damage, ShopItem.FireRate,
            ShopItem.QuickBoots, ShopItem.CharacterRelic, ShopItem.GiantRounds };
        static readonly ShopItem[] ChestRewards = { ShopItem.Damage, ShopItem.FireRate, ShopItem.MaxHealth, ShopItem.QuickBoots };

        enum PickupKind { Soul, Heart }
        sealed class Pickup
        {
            public SpriteRenderer Sprite;
            public PickupKind Kind;
            public int Value;
            public Vector3 Position, Velocity;
            public float Age;
        }
        sealed class Offer
        {
            public ShopItem Item;
            public Vector3 Position;
            public SpriteRenderer Icon;
            public bool Sold;
        }

        readonly List<Pickup> pickups = new List<Pickup>();
        readonly List<Offer> offers = new List<Offer>();
        FpsRoom currentRoom, encounterRoom;
        int encounterWaves;
        float bossIntro;
        SpriteRenderer chestSprite, merchantSprite;
        bool chestOpened;

        public FpsRoom CurrentRoom => currentRoom;
        public bool InBossIntro => bossIntro > 0;
        public float BossIntroRemaining => bossIntro;
        public int PickupCount => pickups.Count;

        void ResetDungeon()
        {
            pickups.Clear(); offers.Clear(); playerShots.Clear();
            currentRoom = encounterRoom = null; encounterWaves = 0; bossIntro = 0;
            chestSprite = merchantSprite = null; chestOpened = false;
        }

        /// <summary>Places the merchant, his goods and the treasure chest once the floor exists.</summary>
        void FurnishFloor()
        {
            if (!hybridArt || world == null) return;
            if (world.ShopRoom != null)
            {
                merchantSprite = hybridArt.AddProp("Mercador", hybridArt.ArtSprite("merchant"), world.MerchantPosition, HauntFpsHybridArt.ActorScale);
                var stock = new List<ShopItem>(ShopStock);
                var order = new System.Random(unchecked(seed * 31 + state.Floor * 977));
                Vector3[] slots = world.ShopSlots;
                for (int i = 0; i < slots.Length && stock.Count > 0; i++)
                {
                    int pick = order.Next(stock.Count);
                    ShopItem item = stock[pick]; stock.RemoveAt(pick);
                    var offer = new Offer { Item = item, Position = slots[i] };
                    offer.Icon = hybridArt.AddProp("Mercadoria / " + item, hybridArt.ArtSprite(ItemIcon(item)),
                        slots[i] + Vector3.up * HauntFpsWorld.Tile * .62f, 1.6f, false, .12f);
                    offers.Add(offer);
                }
            }
            if (world.TreasureRoom != null)
                chestSprite = hybridArt.AddProp("Bau", hybridArt.ChestSprite(0), world.ChestPosition, HauntFpsHybridArt.ActorScale * 1.15f);
        }

        // ------------------------------------------------------------------ rooms

        void UpdateRooms(float dt)
        {
            if (world == null) return;
            FpsRoom inside = world.RoomAt(PlayerPosition, HauntFpsWorld.Tile * .9f);
            if (inside != null && inside != currentRoom)
            {
                currentRoom = inside;
                if (!inside.Visited) { inside.Visited = true; AnnounceRoom(inside); }
                if (!inside.Cleared && encounterRoom == null)
                {
                    if (inside.Role == FpsRoomRole.Combat) BeginEncounter(inside);
                    else if (inside.Role == FpsRoomRole.Boss) BeginBossRoom(inside);
                }
            }
            if (bossIntro > 0)
            {
                bossIntro = Mathf.Max(0, bossIntro - dt);
                if (boss) player.FaceTowards(boss.AimPosition, dt);
                if (bossIntro <= 0) EndBossIntro();
            }
        }

        void AnnounceRoom(FpsRoom room)
        {
            if (room.Role == FpsRoomRole.Shop) Notify("MERCADO DAS ALMAS - E PARA COMPRAR", 2.5f);
            else if (room.Role == FpsRoomRole.Treasure) Notify("CAMARA DO TESOURO", 2);
        }

        void BeginEncounter(FpsRoom room)
        {
            encounterRoom = room;
            encounterWaves = room.HasMiniBoss ? 3 : 2;
            world.SetRoomLocked(room, true);
            PlayCue(FpsCue.DoorSlam, PlayerPosition); AddShake(.14f);
            StartWave(0);
        }

        void FinishEncounter()
        {
            FpsRoom room = encounterRoom;
            encounterRoom = null;
            if (room == null) return;
            room.Cleared = true;
            world.SetRoomLocked(room, false);
            ClearProjectiles();
            state.RewardRoom(false);
            int souls = 3 + state.Floor;
            for (int i = 0; i < souls; i++) DropPickup(PickupKind.Soul, room.Center + Vector3.up * .3f, 1, i);
            if (Hash01(room.Id * 7 + seed) < .3f) DropPickup(PickupKind.Heart, room.Center + Vector3.up * .3f, 2, souls);
            Notify("SALA PURIFICADA", 1.8f);
            PlayCue(FpsCue.RoomClear, PlayerPosition);
        }

        void BeginBossRoom(FpsRoom room)
        {
            encounterRoom = room;
            world.SetRoomLocked(room, true);
            PlayCue(FpsCue.DoorSlam, PlayerPosition); PlayCue(FpsCue.BossSting, PlayerPosition);
            SpawnBoss();
            bossIntro = BossIntroDuration;
            player.InputLocked = true;
            if (player.Perspective && boss) player.Perspective.FocusOverride = boss.transform.position;
        }

        void EndBossIntro()
        {
            player.InputLocked = false;
            player.SuspendInput();
            if (player.Perspective) player.Perspective.FocusOverride = null;
        }

        void OpenBossRoom()
        {
            if (world.BossRoom == null) return;
            world.BossRoom.Cleared = true;
            world.SetRoomLocked(world.BossRoom, false);
            encounterRoom = null;
            PlayCue(FpsCue.RoomClear, PlayerPosition);
            Vector3 center = world.BossRoom.Center + Vector3.up * .3f;
            for (int i = 0; i < 10 + state.Floor * 2; i++) DropPickup(PickupKind.Soul, center, 2, i);
            DropPickup(PickupKind.Heart, center, 2, 99);
        }

        /// <summary>Picks a spawn point inside the active room, away from the player and props.</summary>
        bool TrySpawnInRoom(FpsEnemyKind kind)
        {
            FpsRoom room = encounterRoom ?? currentRoom ?? world.EntranceRoom;
            if (room == null) return false;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                Vector3 position = world.SpawnCandidate(room, spawnSerial, attempt);
                if (Vector3.Distance(position, PlayerPosition) < 4.5f) continue;
                if (Physics.CheckCapsule(position + Vector3.up * .4f, position + Vector3.up * 1.45f,
                    .38f, ~(1 << ViewLayer), QueryTriggerInteraction.Ignore)) continue;
                var obj = new GameObject("Criatura / " + kind);
                obj.transform.SetParent(runRoot.transform, false); obj.layer = EnemyLayer;
                var enemy = obj.AddComponent<HauntFpsEnemy>(); enemy.Initialize(this, kind, position);
                SetLayer(obj.transform, EnemyLayer); enemies.Add(enemy); spawnSerial++;
                Burst(position + Vector3.up * .3f, new Color(.4f, .85f, .72f), 10, 2);
                Physics.SyncTransforms(); return true;
            }
            return false;
        }

        static float Hash01(int value)
        {
            uint h = unchecked((uint)value * 2654435761u);
            h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
            return (h & 0xffffff) / (float)0x1000000;
        }

        // ------------------------------------------------------------------ pickups

        void DropPickup(PickupKind kind, Vector3 origin, int value, int serial)
        {
            if (!hybridArt) return;
            float angle = serial * 2.39996f + Hash01(serial + spawnSerial * 13) * 1.3f;
            float speed = 2.2f + Hash01(serial * 31 + kills) * 2.4f;
            var pickup = new Pickup
            {
                Kind = kind, Value = value, Position = origin,
                Velocity = new Vector3(Mathf.Cos(angle) * speed, 3.2f, Mathf.Sin(angle) * speed)
            };
            pickup.Sprite = hybridArt.AddProp(kind == PickupKind.Heart ? "Coracao" : "Alma",
                hybridArt.ArtSprite(kind == PickupKind.Heart ? "heart" : "coin"), origin, kind == PickupKind.Heart ? 1.5f : 1.25f, true);
            pickups.Add(pickup);
        }

        void TickPickups(float dt)
        {
            Vector3 body = PlayerPosition + Vector3.up * .5f;
            for (int i = pickups.Count - 1; i >= 0; i--)
            {
                var pickup = pickups[i];
                pickup.Age += dt;
                Vector3 toPlayer = body - pickup.Position;
                float distance = toPlayer.magnitude;
                bool wanted = pickup.Kind == PickupKind.Soul || state.Health < state.MaxHealth;
                if (wanted && pickup.Age > .45f && distance < PickupMagnet)
                {
                    // Souls drift to the player once they have landed, faster as they get close.
                    pickup.Velocity = toPlayer.normalized * Mathf.Lerp(14, 5, distance / PickupMagnet);
                    pickup.Position += pickup.Velocity * dt;
                }
                else
                {
                    pickup.Velocity += Vector3.down * 14 * dt;
                    Vector3 next = pickup.Position + pickup.Velocity * dt;
                    if (next.y < .05f) { next.y = .05f; pickup.Velocity = new Vector3(pickup.Velocity.x * .5f, -pickup.Velocity.y * .35f, pickup.Velocity.z * .5f); }
                    if (!world.IsInsideBounds(next, .3f)) { next.x = pickup.Position.x; next.z = pickup.Position.z; pickup.Velocity.x = pickup.Velocity.z = 0; }
                    pickup.Position = next;
                }
                if (pickup.Sprite) hybridArt.MoveProp(pickup.Sprite, pickup.Position);
                if (wanted && pickup.Age > .3f && distance < PickupReach)
                {
                    if (pickup.Kind == PickupKind.Soul) state.Coins += pickup.Value;
                    else state.Health = Mathf.Min(state.MaxHealth, state.Health + pickup.Value);
                    PlayCue(pickup.Kind == PickupKind.Soul ? FpsCue.Coin : FpsCue.Purchase, pickup.Position);
                    if (hybridArt && pickup.Sprite) hybridArt.RemoveProp(pickup.Sprite);
                    pickups.RemoveAt(i);
                }
            }
        }

        // ------------------------------------------------------------------ shop and chest

        Offer NearestOffer()
        {
            Offer best = null; float bestDistance = InteractReach;
            foreach (var offer in offers)
            {
                if (offer.Sold) continue;
                float distance = Vector3.Distance(Flat(PlayerPosition), Flat(offer.Position));
                if (distance < bestDistance) { best = offer; bestDistance = distance; }
            }
            return best;
        }

        bool NearChest => chestSprite && !chestOpened && world.TreasureRoom != null
            && Vector3.Distance(Flat(PlayerPosition), Flat(world.ChestPosition)) < InteractReach;

        static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0, value.z);

        /// <summary>E / Square: buys the nearest good or opens the chest. Returns true when used.</summary>
        bool TryInteract()
        {
            Offer offer = NearestOffer();
            if (offer != null)
            {
                state.ShopOpenBeforeBoss = true;
                if (state.TryBuy(offer.Item, out string message))
                {
                    offer.Sold = true;
                    if (offer.Icon) hybridArt.RemoveProp(offer.Icon);
                    Burst(offer.Position + Vector3.up, Gold, 18, 3);
                    PlayCue(FpsCue.Purchase, offer.Position);
                }
                else PlayCue(FpsCue.DryFire, offer.Position);
                Notify(message, 2);
                return true;
            }
            if (NearChest)
            {
                chestOpened = true;
                chestSprite.sprite = hybridArt.ChestSprite(3);
                Vector3 at = world.ChestPosition + Vector3.up * .6f;
                for (int i = 0; i < 8 + state.Floor * 2; i++) DropPickup(PickupKind.Soul, at, 2, i + 40);
                DropPickup(PickupKind.Heart, at, 2, 70);
                ShopItem reward = ChestRewards[(int)(Hash01(seed + state.Floor * 17) * ChestRewards.Length) % ChestRewards.Length];
                Notify(state.Grant(reward, out string message) ? "TESOURO: " + message : "O BAU ESTAVA CHEIO DE ALMAS.", 3);
                Burst(at, Gold, 30, 4); AddShake(.25f); PlayCue(FpsCue.Chest, at);
                return true;
            }
            return false;
        }

        static bool InteractPressed() => Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
            || Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;

        string InteractionPrompt()
        {
            Offer offer = NearestOffer();
            if (offer != null)
                return "E - " + ItemName(offer.Item) + " (" + state.Price(offer.Item) + " ALMAS)";
            if (NearChest) return "E - ABRIR BAU";
            return null;
        }

        string ItemName(ShopItem item)
        {
            switch (item)
            {
                case ShopItem.GiantRounds: return "BALAS GIGANTES";
                case ShopItem.CharacterRelic: return state.CharacterRelicName;
                case ShopItem.QuickBoots: return "BOTAS LIGEIRAS";
                case ShopItem.Damage: return "POLVORA +1 DANO";
                case ShopItem.FireRate: return "GATILHO RAPIDO";
                case ShopItem.MaxHealth: return "CORACAO EXTRA";
                default: return "TONICO DE VIDA";
            }
        }

        static string ItemIcon(ShopItem item)
        {
            switch (item)
            {
                case ShopItem.GiantRounds: return "giant-rounds";
                case ShopItem.CharacterRelic: return "character-relic";
                case ShopItem.QuickBoots: return "quick-boots";
                case ShopItem.Damage: return "ammo-cache";
                case ShopItem.FireRate: return "repeater";
                default: return "heart";
            }
        }
    }
}
