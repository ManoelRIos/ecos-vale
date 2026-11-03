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
        static readonly string[] BossNames = { "O REI DAS ABÓBORAS", "A VIÚVA DO VÉU", "O BISPO DAS CINZAS", "A MATRIARCA DA PESTE", "O SINO DO ABISMO" };
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
        CharacterKind selectedCharacter;
        int BossVariant => (State.Floor - 1) % BossNames.Length;
        string EnemySprite(int kind) => kind == 4 ? (BossVariant == 3 ? "matriarch" : BossVariant == 4 ? "bell" : "boss") : EnemyArt[kind];
        System.Random roomRandom;

        sealed class Enemy
        {
            public SpriteRenderer Sprite, Shadow, Ring, Telegraph;
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
            public float Life, Duration, Scale, Gravity, Spin;
            public bool Expanding;
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
            view.orthographicSize = 7.9f;
            view.nearClipPlane = .1f;
            view.farClipPlane = 50;
            view.transform.position = new Vector3(0, .3f, -20);
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.025f, .018f, .05f);
            cameraObject.AddComponent<AudioListener>();
            actorsRoot = new GameObject("Atores e projéteis");
            actorsRoot.transform.SetParent(transform, false);
            try
            {
                if (!HauntRendering.TryApply(out string pipelineError))
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
            State = new RunState(seed, selectedCharacter);
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
            shadow = MakeSprite("shadow", playerPosition, actorsRoot.transform, 0);
            shadow.color = new Color(1, 1, 1, .75f);
            shadow.transform.localScale = new Vector3(1.6f, .8f, 1);
            playerMarker = MakeSprite("player-ring", playerPosition, actorsRoot.transform, 20);
            playerGlow = MakeSprite("glow", playerPosition, actorsRoot.transform, -160);
            playerGlow.color = new Color(.3f, 1, .85f, .13f);
            playerGlow.transform.localScale = Vector3.one * 2;
            crosshair = MakeSprite("effect-ring", playerPosition, actorsRoot.transform, 1400);
            walkDistance = weaponKick = 0;
            gun = MakeSprite("weapon", playerPosition, actorsRoot.transform, 30);
            gun.transform.localScale = Vector3.one * .75f;
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
                Cursor.visible = true;
                simulationTime += Mathf.Min(realDelta, .05f);
                UpdateRoomAmbience(Mathf.Min(realDelta, .05f));
                if (keyboard != null && keyboard.enterKey.wasPressedThisFrame) StartFromMenu();
                return;
            }
            if (State.IsDead)
            {
                Cursor.visible = true;
                flash = Mathf.Max(0, flash - realDelta);
                UpdateSparks(Mathf.Min(realDelta, .05f));
                return;
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Paused = !Paused;
                showMap = false;
            }
            if (Paused) { Cursor.visible = true; return; }
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
            State.TickPower(dt);
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) State.Equip(WeaponKind.Pistol);
                if (keyboard.digit2Key.wasPressedThisFrame) State.Equip(WeaponKind.Scattergun);
                if (keyboard.digit3Key.wasPressedThisFrame) State.Equip(WeaponKind.Repeater);
                if (keyboard.digit4Key.wasPressedThisFrame) State.Equip(WeaponKind.SoulCannon);
                if (!InShop && keyboard.qKey.wasPressedThisFrame) UsePower();
            }
            UpdatePlayer(keyboard, dt);
            if (!InShop) UpdateEnemies(dt);
            UpdateShots(dt);
            UpdateSparks(dt);
            UpdateVisualMotion(dt);
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
            Vector2 previousPosition = playerPosition;
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
            playerMoving = Vector2.Distance(previousPosition, playerPosition) > .001f;
            walkDistance += Vector2.Distance(previousPosition, playerPosition);
            if (!InShop && !PointerOverInterface() && mouse != null && mouse.leftButton.isPressed && shotCooldown <= 0 && rollRemaining <= 0)
            {
                Vector2 aim = AimDirection();
                if (State.Weapon == WeaponKind.Scattergun)
                    for (int pellet = -2; pellet <= 2; pellet++) Fire(playerPosition + aim * .48f, Rotate(aim, pellet * 9) * 15, true, State.Damage);
                else Fire(playerPosition + aim * .48f, aim * (State.Weapon == WeaponKind.SoulCannon ? 11 : 17), true,
                    State.Damage * (State.Weapon == WeaponKind.SoulCannon ? 5 : 1));
                weaponKick = 1;
                Emit("glow", playerPosition + Vector2.up * .35f + aim, Vector2.zero, new Color(.4f, 1, .85f, .3f), .08f, .7f, 0);
                shotCooldown = State.WeaponInterval;
                muzzleTime = .045f;
            }
        }

        void UsePower()
        {
            if (State.Character == CharacterKind.Medium && shots.Count > MaxShots - 9)
            {
                Notify("Aguarde espaço para a rajada espectral.");
                return;
            }
            if (!State.TryUsePower()) return;
            bool hunter = State.Character == CharacterKind.Hunter;
            // Invalidate shots without modifying collections during their update.
            if (hunter)
            {
                foreach (var shot in shots)
                    if (!shot.Friendly && Vector2.Distance(shot.Position, playerPosition) <= 5) shot.Life = 0;
                invincible = Mathf.Max(invincible, .65f);
            }
            else
                for (int i = -4; i <= 4; i++)
                    Fire(playerPosition + AimDirection() * .48f, Rotate(AimDirection(), i * 9) * 13, true, State.Damage * 3);
            Color color = hunter ? PlayerAccent : new Color(.8f, .5f, 1);
            RingEffect(playerPosition, color, hunter ? 5 : 2);
            Burst(playerPosition + Vector2.up * .4f, color, 28);
            Notify(State.PowerName);
        }

        Vector2 AimDirection()
        {
            var mouse = Mouse.current;
            if (mouse == null) return lastMove;
            Vector2 delta = (Vector2)view.ScreenToWorldPoint(mouse.position.ReadValue()) - (playerPosition + Vector2.up * .35f);
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
            roomBannerTime = 2.5f;
            RollEnd();
            BuildRoom(false);
            if (!room.Cleared)
            {
                if (room.Kind == RoomKind.Boss)
                {
                    SpawnEnemy(4, new Vector2(0, 2.4f));
                    Notify(BossNames[BossVariant] + " • desvie dos padrões e use as colunas.");
                }
                else
                {
                    // Spawn away from every doorway and from the arrival point.
                    var points = new List<Vector2> { new Vector2(-6, 2.7f), new Vector2(6, 2.7f),
                        new Vector2(-6, -2.7f), new Vector2(6, -2.7f), new Vector2(0, 3.5f), new Vector2(0, -3.5f), new Vector2(-7, 0), new Vector2(7, 0) };
                    int count = Mathf.Min(8, 3 + State.Floor / 2 + room.Id % 2);
                    while (points.Count > 0 && enemies.Count < count)
                    {
                        int index = roomRandom.Next(points.Count);
                        Vector2 point = points[index];
                        points.RemoveAt(index);
                        if (Vector2.Distance(point, arrival) < 3) continue;
                        int roll = roomRandom.Next(6);
                        int themed = BossVariant == 0 ? 0 : BossVariant == 1 ? 1 : BossVariant == 2 ? 2 : BossVariant == 3 ? 5 : 7;
                        SpawnEnemy(enemies.Count == 0 || roll < 3 ? themed : roll == 3 ? 3 : roll == 4 && State.Floor >= 2 ? 6 : roomRandom.Next(4), point);
                    }
                }
            }
            UpdateDoorArt();
            UpdatePresentation();
        }

        void SpawnEnemy(int kind, Vector2 position)
        {
            int health = kind == 4 ? 65 + State.Floor * 18 : (kind == 0 ? 5 : kind == 3 ? 2 : 4) + State.Floor;
            var enemy = new Enemy { Kind = kind, Position = position, Health = health, MaxHealth = health,
                Cooldown = 1.2f + (float)roomRandom.NextDouble(), Phase = (float)roomRandom.NextDouble() * 6 };
            enemy.Sprite = MakeSprite(EnemySprite(kind), position, actorsRoot.transform, Depth(position.y));
            enemy.Shadow = MakeSprite("shadow", position, actorsRoot.transform, 0);
            enemy.Shadow.transform.localScale = new Vector3(kind == 4 ? 3 : 1.5f, kind == 4 ? 1.4f : .7f, 1);
            enemy.Ring = MakeSprite("enemy-ring", position, actorsRoot.transform, 15);
            enemy.Ring.transform.localScale = Vector3.one * (kind == 4 ? 2 : 1);
            enemy.Telegraph = MakeSprite("slash", position, actorsRoot.transform, 25);
            enemy.Telegraph.enabled = false;
            RingEffect(position, HostileAccent, kind == 4 ? 2 : .8f);
            if (kind == 4)
            {
                enemy.Sprite.transform.localScale = Vector3.one * 1.15f;
                if (BossVariant == 1) enemy.Sprite.color = new Color(.65f, 1, .9f);
                if (BossVariant == 2) enemy.Sprite.color = new Color(1, .6f, .9f);
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
                else if (enemy.Kind == 5) // Plague larva: zigzag pursuit, short venom spread.
                {
                    enemy.Position = Move(enemy.Position, (direction + new Vector2(-direction.y, direction.x) * Mathf.Sin(enemy.Phase * 5) * .6f) * (2.2f * dt), .3f);
                    if (enemy.Cooldown <= 0 && distance < 5)
                    {
                        for (int i = -1; i <= 1; i++) Fire(enemy.Position, Rotate(direction, i * 28) * 3.5f, false, 1);
                        enemy.Cooldown = 3;
                    }
                }
                else if (enemy.Kind == 6) // Bone sentinel: stationary rotating volleys.
                {
                    if (enemy.Cooldown <= 0)
                    {
                        for (int i = 0; i < 4; i++) Fire(enemy.Position, Rotate(direction, i * 90) * 4, false, 1);
                        enemy.Cooldown = 2.7f;
                    }
                }
                else if (enemy.Kind == 7) // Bell acolyte: advances between paired volleys.
                {
                    if (distance > 3) enemy.Position = Move(enemy.Position, direction * (1.4f * dt), .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        Fire(enemy.Position, Rotate(direction, -8) * 5.5f, false, 1);
                        Fire(enemy.Position, Rotate(direction, 8) * 5.5f, false, 1);
                        enemy.Cooldown = 1.8f;
                    }
                }
                else UpdateBoss(enemy, direction, dt);
                if (enemy.Kind != 3 || enemy.Cooldown > .5f) enemy.Tell = enemy.Cooldown < .45f ? .45f - enemy.Cooldown : 0;
                if (distance < (enemy.Kind == 4 ? .95f : .6f)) HurtPlayer();
                PresentEnemy(enemy, direction);
            }
        }

        void UpdateBoss(Enemy boss, Vector2 direction, float dt)
        {
            int variant = BossVariant;
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
            else if (variant == 2) // Bishop: rotating cross and aimed burst.
            {
                for (int i = 0; i < 4; i++)
                    for (int j = -1; j <= 1; j++)
                        Fire(boss.Position, Rotate(Vector2.right, offset + i * 90 + j * 9) * (enraged ? 4.5f : 3.5f), false, 1);
                Fire(boss.Position, direction * 6, false, 1);
            }
            else if (variant == 3) // Matriarch: two interleaved spirals and a venom fan.
            {
                for (int i = 0; i < 10; i++)
                    Fire(boss.Position, Rotate(Vector2.right, i * 36 + offset) * (i % 2 == 0 ? 3 : 5), false, 1);
                for (int i = -2; i <= 2; i++) Fire(boss.Position, Rotate(direction, i * 20) * 4.5f, false, 1);
            }
            else // Abyssal bell: concentric pulses with a rotating escape sector.
            {
                for (int i = 0; i < 18; i++)
                {
                    if (i % 6 == Mathf.FloorToInt(boss.Phase) % 6) continue;
                    Vector2 ray = Rotate(Vector2.right, i * 20 + offset);
                    Fire(boss.Position, ray * 3, false, 1);
                    if (enraged) Fire(boss.Position, ray * 4.6f, false, 1);
                }
            }
            Burst(boss.Position, new Color(1, .35f, .2f), 12);
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
            SpriteRenderer sprite = shotPool.Count > 0 ? shotPool.Pop() : MakeSprite("bullet-player", origin, actorsRoot.transform, 1250);
            sprite.gameObject.SetActive(true);
            sprite.sprite = art.Get(friendly ? "bullet-player" : "bullet-enemy");
            sprite.color = friendly ? (State.Character == CharacterKind.Medium ? new Color(.85f, .6f, 1) : Color.white) : Color.white;
            if (friendly && State.Weapon == WeaponKind.SoulCannon) sprite.color = new Color(.6f, 1, .85f);
            sprite.transform.localScale = Vector3.one * (friendly && State.Weapon == WeaponKind.SoulCannon ? 1.7f : 1);
            sprite.transform.position = Snap(origin + Vector2.up * .38f);
            sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
            shots.Add(new Shot { Sprite = sprite, Position = origin, Velocity = velocity,
                Friendly = friendly, Damage = damage, Life = 4, Radius = friendly ? .10f : .14f });
            if (!friendly) Emit("muzzle", origin + Vector2.up * .35f, Vector2.zero, HostileAccent, .12f, .5f, 0);
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
                        Burst(enemy.Position + Vector2.up * .38f, new Color(1, .48f, .18f), 3);
                        if (enemy.Health <= 0)
                        {
                            bool boss = enemy.Kind == 4;
                            EnemyDeath(enemy);
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
                if (nearest <= 1)
                {
                    if (targetIndex < 0) Burst(Vector2.Lerp(old, next, Mathf.Clamp01(nearest)) + Vector2.up * .38f, shot.Friendly ? PlayerAccent : HostileAccent, 2);
                    RecycleShot(i);
                }
                else
                {
                    shot.Position = next;
                    shot.Sprite.transform.position = Snap(next + Vector2.up * .38f);
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
            RingEffect(playerPosition, HostileAccent, 1.4f);
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
            RingEffect(playerPosition, PlayerAccent, boss ? 3 : 1.8f);
            Burst(playerPosition + Vector2.up, new Color(1, .76f, .3f), boss ? 18 : 10);
            roomBannerTime = 2;
            UpdateDoorArt();
            if (boss)
            {
                portal = AnimatedProp("portal", new Vector2(0, 1.4f), 450);
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
            Notify("Mercado das Almas • compre melhorias e armas pelos botões. Troque armas com 1–4. Portal à direita: E.");
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
            bool bought = State.TryBuy(item, out string message);
            if (bought) { RingEffect(playerPosition, PlayerAccent, 1.5f); Burst(playerPosition + Vector2.up, new Color(1, .75f, .2f), 10); }
            Notify(message);
        }

        static Vector2 ShopPosition(int index) => new Vector2(-5.4f + index * 3.6f, .5f);

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

        void ClearCombat()
        {
            foreach (var enemy in enemies)
            {
                if (enemy.Sprite) Destroy(enemy.Sprite.gameObject);
                if (enemy.Shadow) Destroy(enemy.Shadow.gameObject);
                if (enemy.Ring) Destroy(enemy.Ring.gameObject);
                if (enemy.Telegraph) Destroy(enemy.Telegraph.gameObject);
            }
            enemies.Clear();
            for (int i = shots.Count - 1; i >= 0; i--) RecycleShot(i);
            for (int i = sparks.Count - 1; i >= 0; i--) RecycleSpark(i);
        }

        void ClearActors()
        {
            ClearCombat();
            if (player) Destroy(player.gameObject);
            if (shadow) Destroy(shadow.gameObject);
            if (gun) Destroy(gun.gameObject);
            if (muzzle) Destroy(muzzle.gameObject);
            if (playerMarker) Destroy(playerMarker.gameObject);
            if (playerGlow) Destroy(playerGlow.gameObject);
            if (crosshair) Destroy(crosshair.gameObject);
            playerMarker = playerGlow = crosshair = null;
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
            Cursor.visible = true;
            if (art != null) art.Dispose();
            if (panelTexture) Destroy(panelTexture);
        }
    }
}
