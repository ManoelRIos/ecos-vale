using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ecos.Haunt
{
    /// <summary>Single-player vertical slice. The room graph/economy are engine-independent;
    /// this component owns the live combat simulation and its generated presentation.</summary>
    public sealed partial class HauntGame : MonoBehaviour
    {
        public RunState State { get; private set; }
        public DungeonFloor Floor { get; private set; }
        public DungeonRoom CurrentRoom { get; private set; }
        public bool InShop { get; private set; }
        public bool Playing { get; private set; }
        public bool Paused { get; private set; }
        public int LivingEnemies => enemies.Count;
        public int ActiveProjectiles => shots.Count;
        public Vector2 PlayerPosition => playerPosition;

        const float HalfWidth = 10.5f, HalfHeight = 5.5f, PlayerRadius = .27f;
        const int MaxShots = 420;
        const float RollTime = .24f, RollCooldown = .85f;
        static readonly string[] BossNames = { "O REI DAS ABÓBORAS", "A VIÚVA DO VÉU", "O BISPO DAS CINZAS" };
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Rect> obstacles = new List<Rect>();
        readonly List<SpriteRenderer> doors = new List<SpriteRenderer>();
        readonly HashSet<int> visited = new HashSet<int>();
        readonly List<Spark> sparks = new List<Spark>();
        readonly Stack<SpriteRenderer> shotPool = new Stack<SpriteRenderer>();
        HauntPixelArt art;
        Camera view;
        GameObject roomRoot, actorsRoot;
        SpriteRenderer player, gun, shadow, portal, muzzle;
        Vector2 playerPosition, lastMove = Vector2.down, rollDirection;
        float shotCooldown, rollRemaining, rollReady, invincible, doorGrace, roomElapsed, flash, shake, muzzleTime;
        float simulationTime;
        bool showMap;
        string seedText = "", notice = "", renderingFailure;
        float noticeTime;
        int roomsCleared, enemiesDefeated;
        System.Random roomRandom;

        sealed class Enemy
        {
            public SpriteRenderer Sprite;
            public Vector2 Position, Dash;
            public int Kind, Health, MaxHealth;
            public float Cooldown, Phase, Tell, HurtFlash, DashTime;
        }
        sealed class Shot
        {
            public SpriteRenderer Sprite;
            public Vector2 Position, Velocity;
            public float Life, Radius;
            public int Damage;
            public bool Friendly;
        }
        sealed class Spark
        {
            public SpriteRenderer Sprite;
            public Vector2 Velocity;
            public float Life, Duration;
            public Color Color;
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            art = new HauntPixelArt();
            var cameraObject = new GameObject("Câmera ortográfica • Meia-Noite");
            cameraObject.transform.SetParent(transform, false);
            view = cameraObject.AddComponent<Camera>();
            view.tag = "MainCamera";
            view.orthographic = true;
            view.orthographicSize = 7.25f;
            view.nearClipPlane = .1f;
            view.farClipPlane = 50;
            view.transform.position = new Vector3(0, 0, -20);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.025f, .018f, .05f);
            cameraObject.AddComponent<AudioListener>();
            actorsRoot = new GameObject("Atores e projéteis");
            actorsRoot.transform.SetParent(transform, false);
            try
            {
                if (!RenderingSetup.TryApply(out string pipelineError))
                    throw new InvalidOperationException(pipelineError);
                if (!art.Material || !art.Material.shader)
                    throw new InvalidOperationException("O material HauntSprites ou seu shader não está disponível.");
                BuildRoom(true);
            }
            catch (Exception error)
            {
                renderingFailure = "Não foi possível preparar a arte. Confira o Console do Unity.";
                Debug.LogException(error, this);
            }
        }

        public void BeginRun(int seed)
        {
            if (!string.IsNullOrEmpty(renderingFailure)) return;
            ClearActors();
            State = new RunState(seed);
            Floor = DungeonFloor.Generate(seed, State.Floor);
            CurrentRoom = Floor.Entrance;
            Playing = true;
            Paused = InShop = showMap = false;
            roomsCleared = enemiesDefeated = 0;
            visited.Clear();
            visited.Add(CurrentRoom.Id);
            playerPosition = Vector2.zero;
            shotCooldown = rollRemaining = rollReady = invincible = shake = simulationTime = 0;
            player = MakeSprite("player", playerPosition, actorsRoot.transform, 20);
            shadow = MakeSprite("muzzle", playerPosition, actorsRoot.transform, 10);
            shadow.color = new Color(0, 0, 0, .35f);
            shadow.transform.localScale = new Vector3(1.7f, .5f, 1);
            gun = MakeSprite("bullet-player", playerPosition, actorsRoot.transform, 30);
            gun.transform.localScale = new Vector3(2.6f, 1.3f, 1);
            muzzle = MakeSprite("muzzle", playerPosition, actorsRoot.transform, 31);
            muzzle.enabled = false;
            EnterRoom(CurrentRoom, Vector2.zero);
            Notify("As portas se fecham durante o combate. Encontre e derrote o guardião.");
        }

        void StartFromMenu()
        {
            int seed;
            if (string.IsNullOrWhiteSpace(seedText)) seed = Guid.NewGuid().GetHashCode();
            else if (!int.TryParse(seedText, out seed))
            {
                Notify("Use um número inteiro como semente, ou deixe o campo vazio.");
                return;
            }
            BeginRun(seed);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            float realDelta = Time.unscaledDeltaTime;
            noticeTime = Mathf.Max(0, noticeTime - realDelta);
            if (!Playing)
            {
                if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) StartFromMenu();
                return;
            }
            if (State.IsDead) return;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Paused = !Paused;
                showMap = false;
            }
            if (Paused) return;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame) showMap = !showMap;
            // Clamp large frame gaps so a stalled frame cannot jump across an entire room.
            float dt = Mathf.Min(Time.deltaTime, .05f);
            simulationTime += dt;
            roomElapsed += dt;
            doorGrace -= dt;
            invincible = Mathf.Max(0, invincible - dt);
            flash = Mathf.Max(0, flash - dt);
            shake = Mathf.Max(0, shake - dt * 2);
            rollReady = Mathf.Max(0, rollReady - dt);
            shotCooldown -= dt;
            muzzleTime -= dt;
            UpdatePlayer(keyboard, dt);
            if (!InShop) UpdateEnemies(dt);
            UpdateShots(dt);
            UpdateSparks(dt);
            if (State.IsDead)
            {
                RollEnd();
                Notify("A noite levou mais uma alma. Uma nova incursão terá outra semente.");
                return;
            }
            UpdatePresentation();
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) Interact();
            if (!InShop && enemies.Count == 0 && doorGrace <= 0) CheckDoors();
        }

        void UpdatePlayer(Keyboard keyboard, float dt)
        {
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            }
            if (input.sqrMagnitude > 0) lastMove = input.normalized;
            var mouse = Mouse.current;
            bool dodge = (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                || (mouse != null && mouse.rightButton.wasPressedThisFrame);
            if (dodge && rollReady <= 0 && rollRemaining <= 0)
            {
                rollRemaining = RollTime;
                rollReady = RollCooldown;
                rollDirection = input.sqrMagnitude > 0 ? input.normalized : lastMove;
                Burst(playerPosition, new Color(.35f, .95f, .8f), 6);
            }
            if (rollRemaining > 0)
            {
                playerPosition = Move(playerPosition, rollDirection * (12.5f * dt), PlayerRadius);
                rollRemaining = Mathf.Max(0, rollRemaining - dt);
                if (rollRemaining <= 0) RollEnd();
            }
            else playerPosition = Move(playerPosition, Vector2.ClampMagnitude(input, 1) * (5.1f * dt), PlayerRadius);
            if (!InShop && mouse != null && mouse.leftButton.isPressed && shotCooldown <= 0 && rollRemaining <= 0)
            {
                Vector2 aim = AimDirection();
                Fire(playerPosition + aim * .48f, aim * 17f, true, State.Damage);
                shotCooldown = State.ShotInterval;
                muzzleTime = .045f;
            }
        }

        Vector2 AimDirection()
        {
            var mouse = Mouse.current;
            if (mouse == null) return lastMove;
            Vector2 delta = (Vector2)view.ScreenToWorldPoint(mouse.position.ReadValue()) - playerPosition;
            return delta.sqrMagnitude > .001f ? delta.normalized : lastMove;
        }

        void RollEnd()
        {
            rollRemaining = 0;
            if (player) player.transform.localRotation = Quaternion.identity;
        }

        Vector2 Move(Vector2 origin, Vector2 delta, float radius)
        {
            // Small substeps and independent axes slide along cover without tunnelling.
            int count = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .12f));
            Vector2 step = delta / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 next = origin + new Vector2(step.x, 0);
                if (!Blocked(next, radius)) origin = next;
                next = origin + new Vector2(0, step.y);
                if (!Blocked(next, radius)) origin = next;
            }
            origin.x = Mathf.Clamp(origin.x, -HalfWidth + radius, HalfWidth - radius);
            origin.y = Mathf.Clamp(origin.y, -HalfHeight + radius, HalfHeight - radius);
            return origin;
        }

        bool Blocked(Vector2 point, float radius)
        {
            foreach (Rect block in obstacles)
            {
                float dx = point.x - Mathf.Clamp(point.x, block.xMin, block.xMax);
                float dy = point.y - Mathf.Clamp(point.y, block.yMin, block.yMax);
                if (dx * dx + dy * dy < radius * radius) return true;
            }
            return false;
        }

        bool CoverBetween(Vector2 start, Vector2 end, float radius = 0)
        {
            foreach (Rect block in obstacles)
                if (CombatGeometry.SegmentHitsBox(start.x, start.y, end.x, end.y,
                    block.xMin - radius, block.yMin - radius, block.xMax + radius, block.yMax + radius))
                    return true;
            return false;
        }

        void EnterRoom(DungeonRoom room, Vector2 arrival)
        {
            ClearCombat();
            CurrentRoom = room;
            visited.Add(room.Id);
            playerPosition = arrival;
            doorGrace = .8f;
            roomElapsed = 0;
            RollEnd();
            BuildRoom(false);
            if (!room.Cleared)
            {
                if (room.Kind == RoomKind.Boss)
                {
                    SpawnEnemy(4, new Vector2(0, 2.4f));
                    Notify(BossNames[(State.Floor - 1) % 3] + " • desvie dos padrões e use as colunas.");
                }
                else
                {
                    // Spawn away from every doorway and from the arrival point.
                    var points = new List<Vector2> { new Vector2(-6, 2.7f), new Vector2(6, 2.7f),
                        new Vector2(-6, -2.7f), new Vector2(6, -2.7f), new Vector2(0, 3.5f), new Vector2(0, -3.5f) };
                    int count = Mathf.Min(6, 3 + State.Floor / 2 + room.Id % 2);
                    while (points.Count > 0 && enemies.Count < count)
                    {
                        int index = roomRandom.Next(points.Count);
                        Vector2 point = points[index];
                        points.RemoveAt(index);
                        if (Vector2.Distance(point, arrival) < 3) continue;
                        SpawnEnemy(roomRandom.Next(4), point);
                    }
                }
            }
            UpdateDoorArt();
            UpdatePresentation();
        }

        void SpawnEnemy(int kind, Vector2 position)
        {
            string[] ids = { "pumpkin", "ghost", "cultist", "bat", "boss" };
            int health = kind == 4 ? 65 + State.Floor * 18 : (kind == 0 ? 5 : kind == 3 ? 2 : 4) + State.Floor;
            var enemy = new Enemy { Kind = kind, Position = position, Health = health, MaxHealth = health,
                Cooldown = 1.2f + (float)roomRandom.NextDouble(), Phase = (float)roomRandom.NextDouble() * 6 };
            enemy.Sprite = MakeSprite(ids[kind], position, actorsRoot.transform, 18);
            if (kind == 4)
            {
                enemy.Sprite.transform.localScale = Vector3.one * 1.7f;
                if (State.Floor % 3 == 2) enemy.Sprite.color = new Color(.65f, 1, .9f);
                if (State.Floor % 3 == 0) enemy.Sprite.color = new Color(1, .6f, .9f);
            }
            enemies.Add(enemy);
        }

        void UpdateEnemies(float dt)
        {
            foreach (var enemy in enemies)
            {
                enemy.Cooldown -= dt;
                enemy.Phase += dt;
                enemy.HurtFlash = Mathf.Max(0, enemy.HurtFlash - dt);
                Vector2 towards = playerPosition - enemy.Position;
                float distance = towards.magnitude;
                Vector2 direction = distance > .01f ? towards / distance : Vector2.down;
                if (enemy.Kind == 0) // Jack-o'-lantern: persistent melee pressure.
                    enemy.Position = Move(enemy.Position, direction * (1.7f * dt), .35f);
                else if (enemy.Kind == 1) // Wisp: strafes and sends aimed shots.
                {
                    Vector2 strafe = new Vector2(-direction.y, direction.x) * Mathf.Sin(enemy.Phase * 1.4f);
                    Vector2 motion = strafe + direction * (distance > 5 ? 1 : distance < 3 ? -1 : 0);
                    enemy.Position = Move(enemy.Position, motion * (1.1f * dt), .3f);
                    if (enemy.Cooldown <= 0 && !CoverBetween(enemy.Position, playerPosition))
                    {
                        Fire(enemy.Position, direction * 4.5f, false, 1);
                        enemy.Cooldown = Mathf.Max(1.0f, 2.0f - State.Floor * .08f);
                    }
                }
                else if (enemy.Kind == 2) // Cultist: readable three-shot fan.
                {
                    if (distance > 6) enemy.Position = Move(enemy.Position, direction * dt, .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        for (int i = -1; i <= 1; i++) Fire(enemy.Position, Rotate(direction, i * 17) * 4, false, 1);
                        enemy.Cooldown = 2.5f;
                    }
                }
                else if (enemy.Kind == 3) // Bat: telegraph then rush along a fixed line.
                {
                    if (enemy.DashTime > 0)
                    {
                        enemy.Position = Move(enemy.Position, enemy.Dash * (8 * dt), .25f);
                        enemy.DashTime -= dt;
                    }
                    else if (enemy.Cooldown <= .5f)
                    {
                        enemy.Tell = .5f;
                        if (enemy.Cooldown <= 0)
                        {
                            enemy.Dash = direction;
                            enemy.DashTime = .45f;
                            enemy.Cooldown = 2.4f;
                            enemy.Tell = 0;
                        }
                    }
                    else enemy.Position = Move(enemy.Position, direction * (1.2f * dt), .25f);
                }
                else UpdateBoss(enemy, direction, dt);
                if (enemy.Kind != 3 || enemy.Cooldown > .5f) enemy.Tell = enemy.Cooldown < .45f ? .45f - enemy.Cooldown : 0;
                if (distance < (enemy.Kind == 4 ? .95f : .6f)) HurtPlayer();
                enemy.Sprite.transform.position = Snap(enemy.Position + Vector2.up * Mathf.Sin(enemy.Phase * 5) * .055f);
                enemy.Sprite.flipX = direction.x < 0;
                Color normal = enemy.Kind == 4 && State.Floor % 3 == 2 ? new Color(.65f, 1, .9f)
                    : enemy.Kind == 4 && State.Floor % 3 == 0 ? new Color(1, .6f, .9f) : Color.white;
                enemy.Sprite.color = enemy.HurtFlash > 0 ? new Color(1, .4f, .3f)
                    : enemy.Tell > 0 ? Color.Lerp(normal, new Color(1, .35f, .45f), .5f + .5f * Mathf.Sin(simulationTime * 25)) : normal;
                enemy.Sprite.sortingOrder = 20 - Mathf.RoundToInt(enemy.Position.y * 2);
            }
        }

        void UpdateBoss(Enemy boss, Vector2 direction, float dt)
        {
            int variant = (State.Floor - 1) % 3;
            bool enraged = boss.Health <= boss.MaxHealth / 2;
            boss.Position = Move(boss.Position, new Vector2(Mathf.Sin(boss.Phase * .7f), Mathf.Cos(boss.Phase)) * (dt * .7f), .8f);
            if (boss.Cooldown > 0) return;
            int count = enraged ? 20 : 14;
            float offset = boss.Phase * 21;
            if (variant == 0) // Pumpkin King: rotating rings with space between bullets.
            {
                for (int i = 0; i < count; i++) Fire(boss.Position, Rotate(Vector2.right, offset + 360f * i / count) * 3.5f, false, 1);
                for (int i = -1; i <= 1; i++) Fire(boss.Position, Rotate(direction, 12 * i) * 5, false, 1);
            }
            else if (variant == 1) // Widow: wide fans, alternating aim.
            {
                for (int i = -3; i <= 3; i++)
                {
                    Fire(boss.Position, Rotate(direction, i * 15 + Mathf.Sin(boss.Phase) * 20) * 4.3f, false, 1);
                    if (enraged) Fire(boss.Position, Rotate(-direction, i * 15) * 3.3f, false, 1);
                }
            }
            else // Bishop: rotating cross and aimed burst.
            {
                for (int i = 0; i < 4; i++)
                    for (int j = -1; j <= 1; j++)
                        Fire(boss.Position, Rotate(Vector2.right, offset + i * 90 + j * 9) * (enraged ? 4.5f : 3.5f), false, 1);
                Fire(boss.Position, direction * 6, false, 1);
            }
            Burst(boss.Position, new Color(1, .35f, .2f), 5);
            boss.Cooldown = enraged ? 1.1f : 1.65f;
        }

        static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(vector.x * Mathf.Cos(radians) - vector.y * Mathf.Sin(radians),
                vector.x * Mathf.Sin(radians) + vector.y * Mathf.Cos(radians));
        }

        void Fire(Vector2 origin, Vector2 velocity, bool friendly, int damage)
        {
            if (shots.Count >= MaxShots) return;
            SpriteRenderer sprite = shotPool.Count > 0 ? shotPool.Pop() : MakeSprite("bullet-player", origin, actorsRoot.transform, 45);
            sprite.gameObject.SetActive(true);
            sprite.sprite = art.Get(friendly ? "bullet-player" : "bullet-enemy");
            sprite.color = Color.white;
            sprite.transform.position = Snap(origin);
            sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            shots.Add(new Shot { Sprite = sprite, Position = origin, Velocity = velocity,
                Friendly = friendly, Damage = damage, Life = 4, Radius = friendly ? .10f : .14f });
        }

        void UpdateShots(float dt)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i];
                Vector2 old = shot.Position;
                Vector2 next = old + shot.Velocity * dt;
                shot.Life -= dt;
                if (shot.Life <= 0 || (!shot.Friendly && CurrentRoom.Cleared))
                {
                    RecycleShot(i);
                    continue;
                }
                // Compare contact fractions, rather than letting collection order decide
                // which target (or cover behind it) receives the shot.
                float nearest = float.PositiveInfinity;
                if (next.x > HalfWidth) nearest = Mathf.Min(nearest, (HalfWidth - old.x) / (next.x - old.x));
                if (next.x < -HalfWidth) nearest = Mathf.Min(nearest, (-HalfWidth - old.x) / (next.x - old.x));
                if (next.y > HalfHeight) nearest = Mathf.Min(nearest, (HalfHeight - old.y) / (next.y - old.y));
                if (next.y < -HalfHeight) nearest = Mathf.Min(nearest, (-HalfHeight - old.y) / (next.y - old.y));
                foreach (Rect block in obstacles)
                    if (CombatGeometry.BoxHitFraction(old.x, old.y, next.x, next.y,
                        block.xMin - shot.Radius, block.yMin - shot.Radius,
                        block.xMax + shot.Radius, block.yMax + shot.Radius, out float contact))
                        nearest = Mathf.Min(nearest, contact);
                int targetIndex = -1;
                if (shot.Friendly)
                {
                    for (int e = 0; e < enemies.Count; e++)
                    {
                        var enemy = enemies[e];
                        float radius = (enemy.Kind == 4 ? .85f : .36f) + shot.Radius;
                        if (!CombatGeometry.CircleHitFraction(old.x, old.y, next.x, next.y,
                            enemy.Position.x, enemy.Position.y, radius, out float contact) || contact >= nearest) continue;
                        nearest = contact;
                        targetIndex = e;
                    }
                    if (targetIndex >= 0)
                    {
                        var enemy = enemies[targetIndex];
                        enemy.Health -= shot.Damage;
                        enemy.HurtFlash = .09f;
                        Burst(enemy.Position, new Color(1, .48f, .18f), 3);
                        if (enemy.Health <= 0)
                        {
                            bool boss = enemy.Kind == 4;
                            Destroy(enemy.Sprite.gameObject);
                            enemies.RemoveAt(targetIndex);
                            enemiesDefeated++;
                            shake = boss ? .3f : .07f;
                            if (enemies.Count == 0) CompleteRoom(boss);
                        }
                    }
                }
                else if (rollRemaining <= 0 && CombatGeometry.CircleHitFraction(old.x, old.y, next.x, next.y,
                    playerPosition.x, playerPosition.y, PlayerRadius + shot.Radius, out float playerContact) && playerContact < nearest)
                {
                    nearest = playerContact;
                    HurtPlayer();
                }
                if (nearest <= 1) RecycleShot(i);
                else
                {
                    shot.Position = next;
                    shot.Sprite.transform.position = Snap(next);
                }
            }
        }

        void RecycleShot(int index)
        {
            var shot = shots[index];
            shot.Sprite.gameObject.SetActive(false);
            shotPool.Push(shot.Sprite);
            shots.RemoveAt(index);
        }

        void HurtPlayer()
        {
            if (invincible > 0 || rollRemaining > 0 || State.IsDead || InShop) return;
            State.Hurt(1);
            invincible = 1.1f;
            flash = .18f;
            shake = .22f;
            Burst(playerPosition, new Color(1, .2f, .35f), 8);
        }

        void CompleteRoom(bool boss)
        {
            if (CurrentRoom.Cleared) return;
            CurrentRoom.Cleared = true;
            roomsCleared++;
            // Invalidate, rather than remove, hostile shots while UpdateShots is iterating.
            foreach (var shot in shots) if (!shot.Friendly) shot.Life = 0;
            State.RewardRoom(boss);
            UpdateDoorArt();
            if (boss)
            {
                portal = MakeSprite("portal", new Vector2(0, 1.4f), roomRoot.transform, 3);
                Notify("Guardião derrotado! Aproxime-se do portal e pressione E para ir ao Mercado das Almas.");
            }
            else Notify("Sala purificada • moedas recebidas. As portas estão abertas.");
        }

        void CheckDoors()
        {
            foreach (int id in CurrentRoom.Neighbors)
            {
                DungeonRoom target = Floor.GetRoom(id);
                Vector2 direction = new Vector2(target.Position.X - CurrentRoom.Position.X, target.Position.Y - CurrentRoom.Position.Y);
                bool near = direction.x != 0 ? Mathf.Abs(playerPosition.y) < .85f && playerPosition.x * direction.x > HalfWidth - .65f
                    : Mathf.Abs(playerPosition.x) < .85f && playerPosition.y * direction.y > HalfHeight - .65f;
                if (!near) continue;
                Vector2 arrival = direction.x != 0 ? new Vector2(-direction.x * (HalfWidth - 1.4f), 0)
                    : new Vector2(0, -direction.y * (HalfHeight - 1.4f));
                EnterRoom(target, arrival);
                return;
            }
        }

        void Interact()
        {
            if (!InShop && State.BossDefeated && CurrentRoom.Kind == RoomKind.Boss && Vector2.Distance(playerPosition, new Vector2(0, 1.4f)) < 1.8f)
            {
                OpenShop();
                return;
            }
            if (InShop && Vector2.Distance(playerPosition, new Vector2(8.4f, 0)) < 1.7f) LeaveShop();
        }

        void OpenShop()
        {
            ClearCombat();
            InShop = true;
            playerPosition = new Vector2(0, -2.7f);
            RollEnd();
            BuildRoom(true);
            Notify("Mercado das Almas • aproxime-se de uma banca e compre com E ou pelo botão. Depois siga ao portal à direita.");
        }

        void LeaveShop()
        {
            int previous = State.Floor;
            State.NextFloor();
            if (previous == State.Floor) return;
            InShop = false;
            Floor = DungeonFloor.Generate(State.Seed, State.Floor);
            visited.Clear();
            CurrentRoom = Floor.Entrance;
            invincible = 1;
            EnterRoom(CurrentRoom, Vector2.zero);
            Notify("Andar " + State.Floor + " • a noite ficou mais perigosa. As melhorias continuam com você.");
        }

        void Buy(ShopItem item)
        {
            if (!InShop || !Playing || Paused || State.IsDead) return;
            State.TryBuy(item, out string message);
            Notify(message);
        }

        void BuildRoom(bool shop)
        {
            if (roomRoot)
            {
                roomRoot.SetActive(false);
                Destroy(roomRoot);
            }
            roomRoot = new GameObject(shop ? "Mercado das Almas" : "Cripta procedural");
            roomRoot.transform.SetParent(transform, false);
            doors.Clear(); obstacles.Clear(); portal = null;
            int seed = State == null ? 71319 : unchecked(State.Seed * 397 ^ State.Floor * 7919 ^ CurrentRoom.Id * 104729);
            roomRandom = new System.Random(seed);
            for (int y = -6; y <= 6; y++)
                for (int x = -11; x <= 11; x++)
                {
                    bool wall = Mathf.Abs(x) == 11 || Mathf.Abs(y) == 6;
                    var tile = MakeSprite(wall ? "wall" : "floor" + roomRandom.Next(3), new Vector2(x, y), roomRoot.transform, wall ? 2 : -20);
                    if (wall && y == -6) tile.color = new Color(.65f, .65f, .8f);
                }
            // Symmetric cover leaves the center and all cardinal entrance lanes open.
            if (!shop && CurrentRoom != null && CurrentRoom.Kind != RoomKind.Entrance)
            {
                bool boss = CurrentRoom.Kind == RoomKind.Boss;
                int layout = roomRandom.Next(3);
                for (int side = -1; side <= 1; side += 2)
                    for (int row = -1; row <= 1; row += 2)
                    {
                        Vector2 p = new Vector2(side * (layout == 1 ? 4.5f : 3.5f), row * (boss ? 2.9f : layout == 2 ? 2.8f : 2.2f));
                        obstacles.Add(new Rect(p.x - .45f, p.y - .38f, .9f, .76f));
                        MakeSprite(layout == 2 && !boss ? "grave" : "pillar", p, roomRoot.transform, 5 - Mathf.RoundToInt(p.y * 2));
                    }
            }
            for (int side = -1; side <= 1; side += 2)
                for (int row = -1; row <= 1; row += 2)
                {
                    MakeSprite("candle", new Vector2(side * 9.3f, row * 4.6f), roomRoot.transform, 5);
                    MakeSprite("pumpkin", new Vector2(side * 8.2f, row * 4.8f), roomRoot.transform, 4).color = new Color(.75f, .75f, .85f);
                }
            if (shop)
            {
                MakeSprite("merchant", new Vector2(0, 2.3f), roomRoot.transform, 10).transform.localScale = Vector3.one * 1.3f;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = ShopPosition(i);
                    MakeSprite("grave", p + Vector2.down * .35f, roomRoot.transform, 4);
                    string[] icons = { "heart", "bullet-player", "muzzle", "heart" };
                    MakeSprite(icons[i], p + Vector2.up * .3f, roomRoot.transform, 6).transform.localScale = Vector3.one * 1.5f;
                }
                portal = MakeSprite("portal", new Vector2(8.4f, 0), roomRoot.transform, 3);
            }
            else if (CurrentRoom != null)
            {
                foreach (int id in CurrentRoom.Neighbors)
                {
                    DungeonRoom target = Floor.GetRoom(id);
                    int dx = target.Position.X - CurrentRoom.Position.X, dy = target.Position.Y - CurrentRoom.Position.Y;
                    var door = MakeSprite("door", new Vector2(dx * 10.45f, dy * 5.45f), roomRoot.transform, 8);
                    if (dx != 0) door.transform.rotation = Quaternion.Euler(0, 0, dx > 0 ? -90 : 90);
                    else if (dy < 0) door.transform.rotation = Quaternion.Euler(0, 0, 180);
                    doors.Add(door);
                }
                if (CurrentRoom.Kind == RoomKind.Boss && CurrentRoom.Cleared)
                    portal = MakeSprite("portal", new Vector2(0, 1.4f), roomRoot.transform, 3);
            }
        }

        static Vector2 ShopPosition(int index) => new Vector2(-5.4f + index * 3.6f, .5f);

        void UpdateDoorArt()
        {
            bool closed = enemies.Count > 0;
            foreach (var door in doors) door.color = closed ? new Color(1, .2f, .35f) : new Color(.35f, 1, .8f);
        }

        void UpdatePresentation()
        {
            if (!player) return;
            Vector2 aim = AimDirection();
            float bob = rollRemaining > 0 ? 0 : Mathf.Sin(simulationTime * 12) * .035f;
            player.transform.position = Snap(playerPosition + Vector2.up * bob);
            player.sortingOrder = 20 - Mathf.RoundToInt(playerPosition.y * 2);
            player.flipX = aim.x < 0;
            player.color = invincible > 0 && Mathf.Sin(simulationTime * 35) > 0 ? new Color(.4f, 1, .9f, .4f) : Color.white;
            if (rollRemaining > 0) player.transform.rotation = Quaternion.Euler(0, 0, (1 - rollRemaining / RollTime) * (rollDirection.x < 0 ? -360 : 360));
            shadow.transform.position = Snap(playerPosition + Vector2.down * .4f);
            gun.transform.position = Snap(playerPosition + aim * .4f + Vector2.down * .08f);
            gun.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            gun.sortingOrder = player.sortingOrder + 1;
            muzzle.enabled = muzzleTime > 0;
            muzzle.transform.position = Snap(playerPosition + aim * .7f);
            if (portal) portal.color = Color.Lerp(new Color(.45f, 1, .8f), Color.white, (Mathf.Sin(simulationTime * 3) + 1) * .5f);
            float height = Mathf.Max(7.25f, 12.6f / Mathf.Max(.5f, view.aspect));
            view.orthographicSize = height;
            view.transform.position = new Vector3(Mathf.Sin(simulationTime * 143) * shake * .3f, Mathf.Cos(simulationTime * 157) * shake * .3f, -20);
            if (InShop)
            {
                var keyboard = Keyboard.current;
                if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                    for (int i = 0; i < 4; i++)
                        if (Vector2.Distance(playerPosition, ShopPosition(i)) < 1.5f) { Buy((ShopItem)i); break; }
            }
        }

        SpriteRenderer MakeSprite(string id, Vector2 position, Transform parent, int order)
        {
            var obj = new GameObject(id);
            obj.transform.SetParent(parent, false);
            obj.transform.position = Snap(position);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = art.Get(id);
            renderer.sharedMaterial = art.Material;
            renderer.sortingOrder = order;
            return renderer;
        }

        static Vector3 Snap(Vector2 position) => new Vector3(Mathf.Round(position.x * 16) / 16f, Mathf.Round(position.y * 16) / 16f, 0);

        void Burst(Vector2 position, Color color, int count)
        {
            for (int i = 0; i < count && sparks.Count < 90; i++)
            {
                var sprite = MakeSprite("muzzle", position, actorsRoot.transform, 50);
                sprite.transform.localScale = Vector3.one * .35f;
                sprite.color = color;
                float angle = i * 360f / count + simulationTime * 7;
                float life = .18f + .04f * (i % 3);
                sparks.Add(new Spark { Sprite = sprite, Velocity = Rotate(Vector2.right, angle) * (2 + i % 3),
                    Life = life, Duration = life, Color = color });
            }
        }

        void UpdateSparks(float dt)
        {
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var spark = sparks[i];
                spark.Life -= dt;
                if (spark.Life <= 0) { Destroy(spark.Sprite.gameObject); sparks.RemoveAt(i); continue; }
                spark.Sprite.transform.position += (Vector3)(spark.Velocity * dt);
                spark.Sprite.color = new Color(spark.Color.r, spark.Color.g, spark.Color.b, spark.Life / spark.Duration);
            }
        }

        void ClearCombat()
        {
            foreach (var enemy in enemies) if (enemy.Sprite) Destroy(enemy.Sprite.gameObject);
            enemies.Clear();
            for (int i = shots.Count - 1; i >= 0; i--) RecycleShot(i);
            foreach (var spark in sparks) if (spark.Sprite) Destroy(spark.Sprite.gameObject);
            sparks.Clear();
        }

        void ClearActors()
        {
            ClearCombat();
            if (player) Destroy(player.gameObject);
            if (shadow) Destroy(shadow.gameObject);
            if (gun) Destroy(gun.gameObject);
            if (muzzle) Destroy(muzzle.gameObject);
            player = shadow = gun = muzzle = null;
        }

        void Notify(string text) { notice = text; noticeTime = 7; }

        void ReturnToMenu()
        {
            ClearActors();
            Playing = Paused = InShop = false;
            State = null; Floor = null; CurrentRoom = null;
            BuildRoom(true);
        }

        void OnDestroy()
        {
            if (art != null) art.Dispose();
            if (panelTexture) Destroy(panelTexture);
        }
    }
}
