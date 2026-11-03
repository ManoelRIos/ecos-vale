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

        const float HalfWidth = 14.5f, HalfHeight = 8.5f, PlayerRadius = .27f;
        const int MaxShots = 420;
        const float CameraSize = 6.3f;
        const float RollTime = .24f, RollCooldown = .85f;
        static readonly string[] BossNames = { "O REI DAS ABÓBORAS", "A VIÚVA DO VÉU", "O BISPO DAS CINZAS", "A MATRIARCA DA PESTE", "O SINO DO ABISMO" };
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<GroundWarning> warnings = new List<GroundWarning>();
        readonly List<Shot> shots = new List<Shot>();
        readonly List<Rect> obstacles = new List<Rect>();
        readonly List<Rect> pits = new List<Rect>();
        Vector2 safeGround;
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
        bool showMap, shopMenuOpen;
        static Vector2 MerchantPosition => new Vector2(0, 3.6f);
        string seedText = "", notice = "", renderingFailure;
        float noticeTime;
        int roomsCleared, enemiesDefeated;
        CharacterKind selectedCharacter;
        int BossVariant => Floor == null ? 0 : Floor.BossArchetype;
        int ThemeVariant => Floor == null ? 0 : Floor.ThemeIndex;
        string EnemySprite(int kind) => kind == 4 ? (BossVariant == 1 ? "widow" : BossVariant == 2 ? "bishop" : BossVariant == 3 ? "matriarch" : BossVariant == 4 ? "bell" : "boss") : EnemyArt[kind];
        System.Random roomRandom;

        sealed class Enemy
        {
            public SpriteRenderer Sprite, Shadow, Ring, Telegraph, Aura, Rift;
            public Vector2 Position, Dash;
            public int Kind, Health, MaxHealth, Attack;
            public float Cooldown, Phase, Tell, HurtFlash, DashTime, SpawnRemaining;
        }
        sealed class Shot
        {
            public SpriteRenderer Sprite, Glow;
            public Vector2 Position, Velocity;
            public float Life, Radius, TrailTimer;
            public Color Color;
            public bool Heavy;
            public WeaponKind Style;
            public float Age, Curve, Spin;
            public int Pierces, Bounces;
            public HashSet<Enemy> HitEnemies;
            public int Damage;
            public bool Friendly;
        }
        sealed class GroundWarning
        {
            public SpriteRenderer Sprite;
            public Vector2 Position;
            public float Remaining, Radius;
            public Color Color;
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
            view.orthographicSize = CameraSize;
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
            Paused = InShop = showMap = shopMenuOpen = false;
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
            Notify("ENCONTRE O GUARDIAO");
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
                StopBeam();
                Cursor.visible = true;
                flash = Mathf.Max(0, flash - realDelta);
                UpdateSparks(Mathf.Min(realDelta, .05f));
                return;
            }
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (shopMenuOpen) { CloseMerchant(); return; }
                Paused = !Paused;
                showMap = false;
            }
            if (Paused) { StopBeam(); Cursor.visible = true; return; }
            if (!InShop && keyboard != null && keyboard.mKey.wasPressedThisFrame) showMap = !showMap;
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
                if (keyboard.digit1Key.wasPressedThisFrame) State.Equip(State.StartingWeapon);
                if (keyboard.digit2Key.wasPressedThisFrame) State.Equip(WeaponKind.Stapler);
                if (keyboard.digit3Key.wasPressedThisFrame) State.Equip(WeaponKind.Banana);
                if (keyboard.digit4Key.wasPressedThisFrame) State.Equip(WeaponKind.SpinningTop);
                if (keyboard.digit5Key.wasPressedThisFrame) State.Equip(WeaponKind.MirrorDisc);
                if (keyboard.digit6Key.wasPressedThisFrame) State.Equip(WeaponKind.RayLantern);
                if (keyboard.digit7Key.wasPressedThisFrame) State.Equip(WeaponKind.BoneFlute);
                if (!InShop && keyboard.qKey.wasPressedThisFrame) UsePower();
            }
            UpdatePlayer(keyboard, dt);
            if (!InShop) UpdateEnemies(dt);
            UpdateWarnings(dt);
            UpdateShots(dt);
            UpdateSparks(dt);
            UpdateVisualMotion(dt);
            if (State.IsDead)
            {
                StopBeam();
                RollEnd();
                Notify("A noite levou mais uma alma. Uma nova incursão terá outra semente.");
                return;
            }
            UpdatePresentation();
            if (InShop && !shopMenuOpen && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                TryOpenMerchantAt(view.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame) Interact();
            if (!InShop && enemies.Count == 0 && doorGrace <= 0) CheckDoors();
        }

        void UpdatePlayer(Keyboard keyboard, float dt)
        {
            if (shopMenuOpen) { StopBeam(); playerMoving = false; return; }
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
            ResolvePitFall();
            UpdateWeaponTrigger(AimDirection(), !InShop && !PointerOverInterface() && mouse != null && mouse.leftButton.isPressed && rollRemaining <= 0 && !State.IsDead, dt);
        }

        void UsePower()
        {
            int needed = State.Character == CharacterKind.Medium ? 9 : State.Character == CharacterKind.Reaper ? 12 : 0;
            if (shots.Count > MaxShots - needed && needed > 0)
            {
                Notify("Aguarde espaço para a rajada.");
                return;
            }
            if (!State.TryUsePower())
            {
                if (State.Character == CharacterKind.Alchemist && State.Health == State.MaxHealth) Notify("Sua vida já está completa.");
                return;
            }
            if (State.Character == CharacterKind.Hunter)
            {
                foreach (var shot in shots)
                    if (!shot.Friendly && Vector2.Distance(shot.Position, playerPosition) <= 5) shot.Life = 0;
                invincible = Mathf.Max(invincible, .65f);
            }
            else if (State.Character == CharacterKind.Medium)
                for (int i = -4; i <= 4; i++) Fire(playerPosition + AimDirection() * .48f, Rotate(AimDirection(), i * 9) * 13, true, State.Damage * 3);
            else if (State.Character == CharacterKind.Reaper)
            {
                for (int i = 0; i < 12; i++) Fire(playerPosition, Rotate(Vector2.right, i * 30) * 12, true, State.Damage * 2);
                invincible = Mathf.Max(invincible, .4f);
            }
            // Alchemist healing is applied atomically by RunState.TryUsePower.
            Color color = CharacterAccent;
            RingEffect(playerPosition, color, State.Character == CharacterKind.Hunter ? 5 : 2);
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
            playerPosition = safeGround = arrival;
            doorGrace = .8f;
            roomElapsed = 0;
            roomBannerTime = 2.5f;
            RollEnd();
            BuildRoom(false);
            if (!room.Cleared)
            {
                if (room.Kind == RoomKind.Boss)
                {
                    SpawnEnemy(4, new Vector2(0, 3.8f));
                    Notify(BossNames[BossVariant]);
                }
                else SpawnWave();
            }
            UpdateDoorArt();
            UpdateCamera(0, true);
            UpdatePresentation();
        }

        void SpawnWave()
        {
            var points = new List<Vector2> { new Vector2(-10, 5.5f), new Vector2(10, 5.5f), new Vector2(-10, -5.5f), new Vector2(10, -5.5f), new Vector2(-3, 6), new Vector2(3, -6), new Vector2(-10, 0), new Vector2(10, 0) };
            int count = Mathf.Min(6, 3 + State.Floor / 3 + CurrentRoom.WavesCleared + CurrentRoom.Id % 2);
            while (points.Count > 0 && enemies.Count < count)
            {
                int index = roomRandom.Next(points.Count);
                Vector2 point = points[index]; points.RemoveAt(index);
                if (Vector2.Distance(point, playerPosition) < 3 || Blocked(point, .65f) || IsPit(point)) continue;
                int[] themed = { 0, 1, 2, 5, 7 };
                int roll = roomRandom.Next(6);
                int kind = roll < 2 ? themed[ThemeVariant] : roll == 2 ? 8 : roll == 3 ? 9 : roll == 4 ? 10 : new[] { 0, 1, 2, 3, 5, 6, 7 }[roomRandom.Next(7)];
                SpawnEnemy(kind, point);
            }
            if (enemies.Count == 0) SpawnEnemy(8, new Vector2(playerPosition.x < 0 ? 10 : -10, 0));
            if (CurrentRoom.HasMiniBoss && CurrentRoom.WavesCleared == CurrentRoom.TotalWaves - 1)
                SpawnEnemy(11, new Vector2(0, playerPosition.y < 0 ? 5 : -5));
            roomBannerTime = 2;
            UpdateDoorArt();
        }

        bool IsPit(Vector2 point)
        {
            foreach (var pit in pits) if (pit.Contains(point)) return true;
            return false;
        }

        bool PitTouches(Vector2 point, float radius)
        {
            foreach (var pit in pits)
                if (new Rect(pit.xMin - radius, pit.yMin - radius, pit.width + radius * 2, pit.height + radius * 2).Contains(point)) return true;
            return false;
        }

        void ResolvePitFall()
        {
            if (InShop || State.IsDead || rollRemaining > 0) return;
            if (IsPit(playerPosition))
            {
                RingEffect(playerPosition, HostileAccent, 1.2f);
                State.Hurt(1);
                playerPosition = safeGround;
                invincible = Mathf.Max(invincible, .9f); flash = .15f;
                StopBeam();
                Notify("QUEDA -1 VIDA");
            }
            else if (!PitTouches(playerPosition, .5f)) safeGround = playerPosition;
        }

        Vector2 MoveEnemy(Enemy enemy, Vector2 delta, float radius)
        {
            if (enemy.Kind == 1 || enemy.Kind == 3) return Move(enemy.Position, delta, radius);
            Vector2 origin = enemy.Position;
            int count = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .12f));
            Vector2 step = delta / count;
            for (int i = 0; i < count; i++)
            {
                Vector2 next = origin + new Vector2(step.x, 0);
                if (!Blocked(next, radius) && !PitTouches(next, radius)) origin = next;
                next = origin + new Vector2(0, step.y);
                if (!Blocked(next, radius) && !PitTouches(next, radius)) origin = next;
            }
            return new Vector2(Mathf.Clamp(origin.x, -HalfWidth + radius, HalfWidth - radius), Mathf.Clamp(origin.y, -HalfHeight + radius, HalfHeight - radius));
        }

        void SpawnEnemy(int kind, Vector2 position)
        {
            int health = kind == 11 ? 24 + State.Floor * 5 : kind == 4 ? 65 + State.Floor * 18 : (kind == 0 ? 5 : kind == 3 ? 2 : 4) + State.Floor;
            var enemy = new Enemy { Kind = kind, Position = position, Health = health, MaxHealth = health,
                SpawnRemaining = .9f, Cooldown = 1.2f + (float)roomRandom.NextDouble(), Phase = (float)roomRandom.NextDouble() * 6 };
            enemy.Sprite = MakeSprite(EnemySprite(kind), position, actorsRoot.transform, Depth(position.y));
            enemy.Shadow = MakeSprite("shadow", position, actorsRoot.transform, 0);
            enemy.Shadow.transform.localScale = new Vector3(kind == 4 || kind == 11 ? 3 : 1.5f, kind == 4 || kind == 11 ? 1.4f : .7f, 1);
            enemy.Ring = MakeSprite("enemy-ring", position, actorsRoot.transform, 15);
            enemy.Ring.transform.localScale = Vector3.one * (kind == 4 || kind == 11 ? 2 : 1);
            enemy.Telegraph = MakeSprite("slash", position, actorsRoot.transform, 25);
            enemy.Telegraph.enabled = false;
            enemy.Rift = MakeSprite("spawn-rift", position, actorsRoot.transform, 35);
            RingEffect(position, HostileAccent, kind == 4 ? 2 : .8f);
            if (kind == 4)
            {
                enemy.Aura = MakeSprite("glow", position, actorsRoot.transform, -150);
                enemy.Aura.transform.localScale = Vector3.one * 4;
                enemy.Aura.color = new Color(FloorColors[BossVariant].r, FloorColors[BossVariant].g, FloorColors[BossVariant].b, .16f);
                enemy.Sprite.transform.localScale = Vector3.one * 1.15f;
                if (BossVariant == 1) enemy.Sprite.color = new Color(.65f, 1, .9f);
                if (BossVariant == 2) enemy.Sprite.color = new Color(1, .6f, .9f);
            }
            enemies.Add(enemy);
            PresentEnemy(enemy, Vector2.down);
        }

        void UpdateEnemies(float dt)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.SpawnRemaining > 0)
                {
                    enemy.SpawnRemaining = Mathf.Max(0, enemy.SpawnRemaining - dt);
                    PresentEnemy(enemy, Vector2.down);
                    continue;
                }
                enemy.Cooldown -= dt;
                enemy.Phase += dt;
                enemy.HurtFlash = Mathf.Max(0, enemy.HurtFlash - dt);
                Vector2 towards = playerPosition - enemy.Position;
                float distance = towards.magnitude;
                Vector2 direction = distance > .01f ? towards / distance : Vector2.down;
                if (enemy.Kind == 0) // Jack-o'-lantern: persistent melee pressure.
                    enemy.Position = MoveEnemy(enemy, direction * (1.7f * dt), .35f);
                else if (enemy.Kind == 1) // Wisp: strafes and sends aimed shots.
                {
                    Vector2 strafe = new Vector2(-direction.y, direction.x) * Mathf.Sin(enemy.Phase * 1.4f);
                    Vector2 motion = strafe + direction * (distance > 5 ? 1 : distance < 3 ? -1 : 0);
                    enemy.Position = MoveEnemy(enemy, motion * (1.1f * dt), .3f);
                    if (enemy.Cooldown <= 0 && !CoverBetween(enemy.Position, playerPosition))
                    {
                        Fire(enemy.Position, direction * 4.5f, false, 1);
                        enemy.Cooldown = Mathf.Max(1.0f, 2.0f - State.Floor * .08f);
                    }
                }
                else if (enemy.Kind == 2) // Cultist: readable three-shot fan.
                {
                    if (distance > 6) enemy.Position = MoveEnemy(enemy, direction * dt, .35f);
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
                        enemy.Position = MoveEnemy(enemy, enemy.Dash * (8 * dt), .25f);
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
                    else enemy.Position = MoveEnemy(enemy, direction * (1.2f * dt), .25f);
                }
                else if (enemy.Kind == 5) // Plague larva: zigzag pursuit, short venom spread.
                {
                    enemy.Position = MoveEnemy(enemy, (direction + new Vector2(-direction.y, direction.x) * Mathf.Sin(enemy.Phase * 5) * .6f) * (2.2f * dt), .3f);
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
                    if (distance > 3) enemy.Position = MoveEnemy(enemy, direction * (1.4f * dt), .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        Fire(enemy.Position, Rotate(direction, -8) * 5.5f, false, 1);
                        Fire(enemy.Position, Rotate(direction, 8) * 5.5f, false, 1);
                        enemy.Cooldown = 1.8f;
                    }
                }
                else if (enemy.Kind == 8)
                {
                    enemy.Position = MoveEnemy(enemy, direction * dt, .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        Fire(enemy.Position, direction * 5.5f, false, 1);
                        enemy.Cooldown = 2;
                    }
                }
                else if (enemy.Kind == 9)
                {
                    float hop = Mathf.Max(0, Mathf.Sin(enemy.Phase * 4));
                    enemy.Position = MoveEnemy(enemy, direction * (hop * dt * 3), .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        for (int i = 0; i < 6; i++) Fire(enemy.Position, Rotate(Vector2.right, i * 60) * 3, false, 1);
                        enemy.Cooldown = 3;
                    }
                }
                else if (enemy.Kind == 10)
                {
                    if (enemy.DashTime > 0)
                    {
                        enemy.Position = MoveEnemy(enemy, enemy.Dash * (dt * 7), .3f);
                        enemy.DashTime -= dt;
                    }
                    else if (enemy.Cooldown <= 0)
                    {
                        enemy.Dash = direction; enemy.DashTime = .45f; enemy.Cooldown = 2.7f;
                    }
                    else enemy.Position = MoveEnemy(enemy, new Vector2(-direction.y, direction.x) * (dt * 1.3f), .3f);
                }
                else if (enemy.Kind == 11)
                {
                    enemy.Position = MoveEnemy(enemy, direction * (dt * .65f), .65f);
                    if (enemy.Cooldown <= 0)
                    {
                        for (int i = -2; i <= 2; i++) Fire(enemy.Position, Rotate(direction, i * 17) * 4.5f, false, 1);
                        WarnGround(playerPosition, .85f);
                        enemy.Cooldown = 2.8f;
                    }
                }
                else UpdateBoss(enemy, direction, dt);
                if (enemy.Kind != 3 || enemy.Cooldown > .5f) enemy.Tell = enemy.Cooldown < .45f ? .45f - enemy.Cooldown : 0;
                if (distance < (enemy.Kind == 4 || enemy.Kind == 11 ? .95f : .6f)) HurtPlayer();
                PresentEnemy(enemy, direction);
            }
        }

        void UpdateBoss(Enemy boss, Vector2 direction, float dt)
        {
            int variant = BossVariant;
            bool enraged = boss.Health <= boss.MaxHealth / 2;
            if (boss.DashTime > 0)
            {
                boss.Position = Move(boss.Position, boss.Dash * (dt * 6.5f), .8f);
                boss.DashTime -= dt;
            }
            else boss.Position = Move(boss.Position, new Vector2(Mathf.Sin(boss.Phase * .7f), Mathf.Cos(boss.Phase)) * (dt * .7f), .8f);
            if (boss.Cooldown > 0) return;
            bool alternate = boss.Attack++ % 2 == 1;
            int count = enraged ? 20 : 14;
            float offset = boss.Phase * 21;
            if (variant == 0) // Keep the Pumpkin King's readable signature.
            {
                for (int i = 0; i < count; i++) Fire(boss.Position, Rotate(Vector2.right, offset + 360f * i / count) * 3.5f, false, 1);
                for (int i = -1; i <= 1; i++) Fire(boss.Position, Rotate(direction, 12 * i) * 5, false, 1);
            }
            else if (variant == 1)
            {
                if (alternate) // Veil charge followed by crossed silk fans.
                {
                    boss.Dash = direction; boss.DashTime = .6f;
                    for (int i = -3; i <= 3; i++)
                    {
                        Fire(boss.Position, Rotate(direction, i * 16 + 35) * 4.3f, false, 1);
                        Fire(boss.Position, Rotate(direction, i * 16 - 35) * 3.3f, false, 1);
                    }
                }
                else for (int arm = 0; arm < 6; arm++)
                    for (int bead = 0; bead < 3; bead++) Fire(boss.Position, Rotate(Vector2.right, arm * 60 + offset) * (2.8f + bead), false, 1);
            }
            else if (variant == 2)
            {
                for (int i = 0; i < 4; i++)
                    for (int j = -1; j <= 1; j++) Fire(boss.Position, Rotate(Vector2.right, offset + i * 90 + j * 9) * (enraged ? 4.5f : 3.5f), false, 1);
                if (alternate) // Consecration: marked ground erupts after a full second.
                    for (int i = -1; i <= 1; i++) WarnGround(playerPosition + Vector2.right * (i * 2.7f), 1.05f);
                else Fire(boss.Position, direction * 6, false, 1);
            }
            else if (variant == 3)
            {
                if (alternate)
                {
                    for (int i = -2; i <= 2; i++) Fire(boss.Position, Rotate(direction, i * 18) * 5, false, 1);
                    WarnGround(playerPosition, 1.4f);
                    WarnGround(playerPosition + new Vector2(3, 2), 1.15f);
                    WarnGround(playerPosition - new Vector2(3, 2), 1.15f);
                }
                else for (int i = 0; i < 14; i++) Fire(boss.Position, Rotate(Vector2.right, i * 360f / 14 + offset) * (i % 2 == 0 ? 3 : 5), false, 1);
            }
            else
            {
                if (alternate) // Tolling lanes: parallel volleys with room to pass.
                {
                    for (int i = -4; i <= 4; i++)
                        if (i != 0) Fire(new Vector2(boss.Position.x + i * 1.2f, boss.Position.y), Vector2.down * 4.5f, false, 1);
                    WarnGround(playerPosition, 1.25f);
                }
                else for (int i = 0; i < 18; i++)
                {
                    if (i % 6 == boss.Attack % 6) continue;
                    Vector2 ray = Rotate(Vector2.right, i * 20 + offset);
                    Fire(boss.Position, ray * 3, false, 1);
                    if (enraged) Fire(boss.Position, ray * 4.6f, false, 1);
                }
            }
            RingEffect(boss.Position, FloorColors[variant], 2.1f);
            Burst(boss.Position + Vector2.up * .6f, FloorColors[variant], 16);
            boss.Cooldown = enraged ? 1.35f : 1.9f;
        }

        void WarnGround(Vector2 position, float radius)
        {
            if (warnings.Count >= 16) return;
            position.x = Mathf.Clamp(position.x, -HalfWidth + radius, HalfWidth - radius);
            position.y = Mathf.Clamp(position.y, -HalfHeight + radius, HalfHeight - radius);
            var marker = MakeSprite("enemy-ring", position, actorsRoot.transform, 40);
            marker.transform.localScale = new Vector3(radius, radius * 2, 1);
            var color = new Color(1, .43f, .25f, .9f);
            marker.color = color;
            warnings.Add(new GroundWarning { Sprite = marker, Position = position, Remaining = 1.1f, Radius = radius, Color = color });
        }

        void UpdateWarnings(float dt)
        {
            for (int i = warnings.Count - 1; i >= 0; i--)
            {
                var warning = warnings[i];
                warning.Remaining -= dt;
                warning.Sprite.color = new Color(1, .4f + .2f * Mathf.Sin(warning.Remaining * 25), .15f, .85f);
                if (warning.Remaining > 0 && !CurrentRoom.Cleared) continue;
                if (!CurrentRoom.Cleared)
                {
                    Impact(warning.Position + Vector2.up * .2f, Vector2.up, warning.Color, true);
                    if (Vector2.Distance(playerPosition, warning.Position) < warning.Radius + PlayerRadius) HurtPlayer();
                }
                Destroy(warning.Sprite.gameObject);
                warnings.RemoveAt(i);
            }
        }

        static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(vector.x * Mathf.Cos(radians) - vector.y * Mathf.Sin(radians),
                vector.x * Mathf.Sin(radians) + vector.y * Mathf.Cos(radians));
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
            if (CurrentRoom.Cleared || enemies.Count > 0) return;
            if (CurrentRoom.Kind == RoomKind.Combat && CurrentRoom.WavesCleared + 1 < CurrentRoom.TotalWaves)
            {
                CurrentRoom.WavesCleared++;
                foreach (var shot in shots) if (!shot.Friendly) shot.Life = 0;
                foreach (var warning in warnings) if (warning.Sprite) Destroy(warning.Sprite.gameObject);
                warnings.Clear();
                StopBeam();
                SpawnWave();
                Notify("ONDA " + (CurrentRoom.WavesCleared + 1) + "/" + CurrentRoom.TotalWaves);
                return;
            }
            CurrentRoom.WavesCleared = CurrentRoom.TotalWaves;
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
                Notify("PORTAL ABERTO");
            }
            else Notify("SALA PURIFICADA");
        }

        bool TryNearestExit(out Vector2 destination)
        {
            destination = Vector2.zero;
            if (!Playing || InShop || State == null || State.IsDead || CurrentRoom == null || !CurrentRoom.Cleared || enemies.Count > 0) return false;
            if (CurrentRoom.Kind == RoomKind.Boss && State.BossDefeated)
            {
                destination = new Vector2(0, 1.4f);
                return true;
            }
            float nearest = float.PositiveInfinity;
            foreach (int id in CurrentRoom.Neighbors)
            {
                var next = Floor.GetRoom(id);
                Vector2 point = new Vector2((next.Position.X - CurrentRoom.Position.X) * (HalfWidth - .05f),
                    (next.Position.Y - CurrentRoom.Position.Y) * (HalfHeight - .05f));
                float distance = (point - playerPosition).sqrMagnitude;
                if (distance < nearest) { nearest = distance; destination = point; }
            }
            return !float.IsPositiveInfinity(nearest);
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
            if (InShop)
            {
                if (shopMenuOpen) { CloseMerchant(); return; }
                if (Vector2.Distance(playerPosition, new Vector2(HalfWidth - 2, 0)) < 1.7f) LeaveShop();
                else OpenMerchant();
                return;
            }
            if (State.BossDefeated && CurrentRoom.Kind == RoomKind.Boss && Vector2.Distance(playerPosition, new Vector2(0, 1.4f)) < 1.8f)
                OpenShop();
        }

        void OpenShop()
        {
            ClearCombat();
            InShop = true;
            showMap = shopMenuOpen = shopWeapons = false;
            playerPosition = new Vector2(0, -3.5f);
            RollEnd();
            BuildRoom(true);
            UpdateCamera(0, true);
            UpdatePresentation();
            Notify("");
        }

        bool TryOpenMerchantAt(Vector2 worldPoint)
        {
            if (!InShop || shopMenuOpen || !new Rect(MerchantPosition.x - 1, MerchantPosition.y - .7f, 2, 2.3f).Contains(worldPoint)) return false;
            OpenMerchant();
            return shopMenuOpen;
        }

        void OpenMerchant()
        {
            if (!Playing || !InShop || Paused || State.IsDead || Vector2.Distance(playerPosition, MerchantPosition) > 2.8f) return;
            shopMenuOpen = true;
            playerMoving = false;
            RollEnd();
            noticeTime = 0;
            Cursor.visible = true;
        }

        void CloseMerchant()
        {
            shopMenuOpen = false;
            noticeTime = 0;
        }

        void LeaveShop()
        {
            if (!InShop || shopMenuOpen || Paused || State.IsDead) return;
            int previous = State.Floor;
            State.NextFloor();
            if (previous == State.Floor) return;
            InShop = shopMenuOpen = false;
            Floor = DungeonFloor.Generate(State.Seed, State.Floor);
            visited.Clear();
            CurrentRoom = Floor.Entrance;
            invincible = 1;
            EnterRoom(CurrentRoom, Vector2.zero);
            Notify("ANDAR " + State.Floor);
        }

        void Buy(ShopItem item)
        {
            if (!InShop || !shopMenuOpen || !Playing || Paused || State.IsDead) return;
            bool bought = State.TryBuy(item, out string message);
            if (bought) { RingEffect(playerPosition, PlayerAccent, 1.5f); Burst(playerPosition + Vector2.up, new Color(1, .75f, .2f), 10); }
            Notify(message);
        }

        static Vector2 ShopPosition(int index) => new Vector2(-6 + (index % 3) * 6, index < 3 ? -.8f : -4.5f);

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
            StopBeam();
            foreach (var warning in warnings) if (warning.Sprite) Destroy(warning.Sprite.gameObject);
            warnings.Clear();
            foreach (var enemy in enemies)
            {
                if (enemy.Rift) Destroy(enemy.Rift.gameObject);
                if (enemy.Aura) Destroy(enemy.Aura.gameObject);
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
            Playing = Paused = InShop = shopMenuOpen = false;
            State = null; Floor = null; CurrentRoom = null;
            BuildRoom(true);
        }

        void OnDestroy()
        {
            Cursor.visible = true;
            if (glowMaterial) Destroy(glowMaterial);
            if (pixelText != null) pixelText.Dispose();
            if (art != null) art.Dispose();
            if (panelTexture) Destroy(panelTexture);
        }
    }
}
