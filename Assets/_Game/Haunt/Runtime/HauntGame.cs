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
        static readonly string[] BossNames = { "O REI DAS ABÓBORAS", "A VIÚVA DO VÉU", "O BISPO DAS CINZAS", "A MATRIARCA DA PESTE", "O SINO DO ABISMO", "O CARRASCO DE FERRO", "O ORÁCULO DO BREJO" };
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
        Color BossAccent => BossVariant == 5 ? new Color(1, .53f, .28f)
            : BossVariant == 6 ? new Color(.48f, 1, .65f) : FloorColors[BossVariant];
        string EnemySprite(int kind) => kind == 4 ? (BossVariant == 1 ? "widow" : BossVariant == 2 ? "bishop" : BossVariant == 3 ? "matriarch" : BossVariant == 4 ? "bell" : BossVariant == 5 ? "iron-executioner" : BossVariant == 6 ? "bog-oracle" : "boss")
            : kind == 12 ? "lantern-wisp" : kind == 13 ? "mushroom-brute" : kind == 14 ? "clockwork-imp" : EnemyArt[kind];
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
            public float Aspect = 1;
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
            PauseAudio(false);
            ClearActors();
            ResetRoomProps();
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
            PollControlInput();
            UpdateMusic(Time.unscaledDeltaTime);
            var keyboard = Keyboard.current;
            float realDelta = Time.unscaledDeltaTime;
            noticeTime = Mathf.Max(0, noticeTime - realDelta);
            if (!Playing)
            {
                Cursor.visible = !UsingController;
                simulationTime += Mathf.Min(realDelta, .05f);
                UpdateRoomAmbience(Mathf.Min(realDelta, .05f));
                if (!ControlsConsumed && keyboard != null && keyboard.enterKey.wasPressedThisFrame) StartFromMenu();
                return;
            }
            if (State.IsDead)
            {
                StopBeam();
                Cursor.visible = !UsingController;
                flash = Mathf.Max(0, flash - realDelta);
                UpdateSparks(Mathf.Min(realDelta, .05f));
                return;
            }
            if (PausePressed())
            {
                if (shopMenuOpen) { CloseMerchant(); return; }
                Paused = !Paused;
                showMap = false;
            }
            if (Paused) { PauseAudio(true); StopBeam(); Cursor.visible = !UsingController; return; }
            PauseAudio(false);
            if (!InShop && MapPressed()) showMap = !showMap;
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
            UpdateWeaponSelection(keyboard);
            if (!InShop && PowerPressed()) UsePower();
            UpdatePlayer(keyboard, dt);
            if (!InShop) UpdateEnemies(dt);
            UpdateWarnings(dt);
            UpdateShots(dt);
            UpdateRoomProps(dt);
            UpdateSparks(dt);
            UpdateVisualMotion(dt);
            if (State.IsDead)
            {
                StopBeam();
                PlaySound(SoundCue.PlayerDeath);
                RollEnd();
                Notify("A noite levou mais uma alma. Uma nova incursão terá outra semente.");
                return;
            }
            UpdatePresentation();
            if (InShop && !shopMenuOpen && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                TryOpenMerchantAt(view.ScreenToWorldPoint(Mouse.current.position.ReadValue()));
            if (InteractPressed()) Interact();
            if (!InShop && enemies.Count == 0 && doorGrace <= 0) CheckDoors();
        }

        void UpdatePlayer(Keyboard keyboard, float dt)
        {
            if (shopMenuOpen) { StopBeam(); playerMoving = false; return; }
            Vector2 input = MovementInput(keyboard);
            Vector2 previousPosition = playerPosition;
            if (input.sqrMagnitude > 0) lastMove = input.normalized;
            bool dodge = DodgePressed();
            if (dodge && rollReady <= 0 && rollRemaining <= 0)
            {
                rollRemaining = RollTime;
                rollReady = RollCooldown;
                rollDirection = input.sqrMagnitude > 0 ? input.normalized : lastMove;
                Burst(playerPosition, new Color(.35f, .95f, .8f), 6);
                PlaySound(SoundCue.Dodge);
            }
            if (rollRemaining > 0)
            {
                playerPosition = Move(playerPosition, rollDirection * (12.5f * dt), PlayerRadius);
                rollRemaining = Mathf.Max(0, rollRemaining - dt);
                if (rollRemaining <= 0) RollEnd();
            }
            else playerPosition = Move(playerPosition, input * (5.1f * State.MoveSpeedMultiplier * RoomMovementMultiplier(playerPosition) * dt), PlayerRadius);
            playerMoving = Vector2.Distance(previousPosition, playerPosition) > .001f;
            walkDistance += Vector2.Distance(previousPosition, playerPosition);
            ResolvePitFall();
            UpdateWeaponTrigger(AimDirection(), !InShop && !PointerOverInterface() && FireHeld() && rollRemaining <= 0 && !State.IsDead, dt);
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
            PlaySound(SoundCue.MagicShot, .75f);
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
            if (mouse == null) return ControllerAimOr(lastMove);
            Vector2 delta = (Vector2)view.ScreenToWorldPoint(mouse.position.ReadValue()) - (playerPosition + Vector2.up * .35f);
            return ControllerAimOr(delta.sqrMagnitude > .001f ? delta.normalized : lastMove);
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
            PlaySound(SoundCue.Door, .6f);
        }

        void SpawnWave()
        {
            PlaySound(SoundCue.Wave, .6f);
            // Each profile changes roles and formation, rather than just adding health.
            var points = new List<Vector2>
            {
                new Vector2(-11, 5), new Vector2(-7, 6), new Vector2(0, 6),
                new Vector2(7, 6), new Vector2(11, 5), new Vector2(11, 0),
                new Vector2(11, -5), new Vector2(7, -6), new Vector2(0, -6),
                new Vector2(-7, -6), new Vector2(-11, -5), new Vector2(-11, 0)
            };
            int wave = CurrentRoom.WavesCleared;
            int density = Mathf.Min(2, (State.Floor - 1) / 3);
            int count = CurrentRoom.Encounter == EncounterKind.Swarm ? 6 + density + wave / 2
                : CurrentRoom.Encounter == EncounterKind.Elite ? 4 + density
                : 5 + density + wave / 2;
            int spawned = 0;
            while (points.Count > 0 && spawned < count)
            {
                int index;
                if (CurrentRoom.Encounter == EncounterKind.Crossfire)
                    index = (wave * 3 + spawned * 4) % points.Count;
                else if (CurrentRoom.Encounter == EncounterKind.Ambush)
                    index = (wave * 4 + spawned * 2 + roomRandom.Next(2)) % points.Count;
                else index = roomRandom.Next(points.Count);
                Vector2 point = points[index];
                points.RemoveAt(index);
                if (Vector2.Distance(point, playerPosition) < 3 || Blocked(point, .65f) || PitTouches(point, .55f)) continue;
                SpawnEnemy(EncounterEnemy(spawned, wave), point);
                spawned++;
            }
            if (spawned == 0)
            {
                // Search the open perimeter instead of placing an enemy on the player.
                for (int i = 0; i < 16; i++)
                {
                    Vector2 point = Rotate(Vector2.right, i * 22.5f);
                    point = new Vector2(point.x * 11, point.y * 6);
                    if (Vector2.Distance(point, playerPosition) < 3 || Blocked(point, .65f) || PitTouches(point, .55f)) continue;
                    SpawnEnemy(8, point);
                    break;
                }
            }
            if (CurrentRoom.HasMiniBoss && wave == CurrentRoom.TotalWaves - 1)
            {
                Vector2 position = new Vector2(0, playerPosition.y < 0 ? 5 : -5);
                if (!Blocked(position, .8f) && !PitTouches(position, .8f)) SpawnEnemy(11, position);
            }
            roomBannerTime = 2;
            UpdateDoorArt();
        }

        int EncounterEnemy(int index, int wave)
        {
            int[] signature = { 0, 12, 14, 13, 7 };
            if (index == 0) return signature[ThemeVariant];
            switch (CurrentRoom.Encounter)
            {
                case EncounterKind.Swarm:
                    int[] swarm = { 0, 3, 5, 9 };
                    return index % 3 == 0 ? signature[ThemeVariant] : swarm[(index + wave) % swarm.Length];
                case EncounterKind.Crossfire:
                    int[] shooters = { 1, 2, 6, 8, 12, 14 };
                    return shooters[(index + wave + ThemeVariant) % shooters.Length];
                case EncounterKind.Elite:
                    int[] guards = { 13, 6, 10, 12, 14 };
                    return guards[(index + wave) % guards.Length];
                default:
                    int[] ambushers = { 3, 10, 14, 5, 12 };
                    return ambushers[(index + wave + ThemeVariant) % ambushers.Length];
            }
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
                PlaySound(SoundCue.PlayerHit);
                playerPosition = safeGround;
                invincible = Mathf.Max(invincible, .9f); flash = .15f;
                StopBeam();
                Notify("QUEDA -1 VIDA");
            }
            else if (!PitTouches(playerPosition, .5f)) safeGround = playerPosition;
        }

        Vector2 MoveEnemy(Enemy enemy, Vector2 delta, float radius)
        {
            if (enemy.Kind == 1 || enemy.Kind == 3 || enemy.Kind == 12) return Move(enemy.Position, delta, radius);
            delta *= RoomMovementMultiplier(enemy.Position);
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
            int depth = Mathf.Min(12, Mathf.Max(1, State.Floor));
            int health = kind == 11 ? 18 + depth * 5 : kind == 4 ? 50 + depth * 12
                : (kind == 13 ? 8 : kind == 0 || kind == 6 ? 4 : kind == 3 || kind == 5 ? 2 : 3) + (depth - 1) / 3;
            if (kind != 4 && kind != 11 && kind != 13 && CurrentRoom.Encounter == EncounterKind.Swarm)
                health = Mathf.Max(2, health - 1);
            var enemy = new Enemy { Kind = kind, Position = position, Health = health, MaxHealth = health,
                SpawnRemaining = .9f, Cooldown = .45f + (float)roomRandom.NextDouble() * .65f, Phase = (float)roomRandom.NextDouble() * 6 };
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
                enemy.Aura.color = new Color(BossAccent.r, BossAccent.g, BossAccent.b, .16f);
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
                    enemy.Position = MoveEnemy(enemy, direction * (2.35f * dt), .35f);
                else if (enemy.Kind == 1) // Wisp: strafes and sends aimed shots.
                {
                    Vector2 strafe = new Vector2(-direction.y, direction.x) * Mathf.Sin(enemy.Phase * 1.4f);
                    Vector2 motion = strafe + direction * (distance > 5 ? 1 : distance < 3 ? -1 : 0);
                    enemy.Position = MoveEnemy(enemy, motion * (1.1f * dt), .3f);
                    if (enemy.Cooldown <= 0 && !CoverBetween(enemy.Position, playerPosition))
                    {
                        FireHostile(enemy.Position, direction * 4.5f);
                        enemy.Cooldown = Mathf.Max(.95f, 1.7f - State.Floor * .06f);
                    }
                }
                else if (enemy.Kind == 2) // Cultist: readable three-shot fan.
                {
                    if (distance > 6) enemy.Position = MoveEnemy(enemy, direction * dt, .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        for (int i = -1; i <= 1; i++) FireHostile(enemy.Position, Rotate(direction, i * 17) * 4);
                        enemy.Cooldown = 2.1f;
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
                    else enemy.Position = MoveEnemy(enemy, direction * (1.8f * dt), .25f);
                }
                else if (enemy.Kind == 5) // Plague larva: zigzag pursuit, short venom spread.
                {
                    enemy.Position = MoveEnemy(enemy, (direction + new Vector2(-direction.y, direction.x) * Mathf.Sin(enemy.Phase * 5) * .6f) * (2.2f * dt), .3f);
                    if (enemy.Cooldown <= 0 && distance < 5)
                    {
                        for (int i = -1; i <= 1; i++) FireHostile(enemy.Position, Rotate(direction, i * 28) * 3.5f);
                        enemy.Cooldown = 3;
                    }
                }
                else if (enemy.Kind == 6) // Bone sentinel: stationary rotating volleys.
                {
                    if (enemy.Cooldown <= 0)
                    {
                        for (int i = 0; i < 4; i++) FireHostile(enemy.Position, Rotate(direction, i * 90) * 4);
                        enemy.Cooldown = 2.7f;
                    }
                }
                else if (enemy.Kind == 7) // Bell acolyte: advances between paired volleys.
                {
                    if (distance > 3) enemy.Position = MoveEnemy(enemy, direction * (1.4f * dt), .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        FireHostile(enemy.Position, Rotate(direction, -8) * 5.5f);
                        FireHostile(enemy.Position, Rotate(direction, 8) * 5.5f);
                        enemy.Cooldown = 1.8f;
                    }
                }
                else if (enemy.Kind == 8)
                {
                    enemy.Position = MoveEnemy(enemy, direction * dt, .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        FireHostile(enemy.Position, direction * 5.5f);
                        enemy.Cooldown = 1.65f;
                    }
                }
                else if (enemy.Kind == 9)
                {
                    float hop = Mathf.Max(0, Mathf.Sin(enemy.Phase * 4));
                    enemy.Position = MoveEnemy(enemy, direction * (hop * dt * 3), .35f);
                    if (enemy.Cooldown <= 0)
                    {
                        for (int i = 0; i < 6; i++) FireHostile(enemy.Position, Rotate(Vector2.right, i * 60) * 3);
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
                        for (int i = -2; i <= 2; i++) FireHostile(enemy.Position, Rotate(direction, i * 17) * 4.5f);
                        WarnGround(playerPosition, .85f);
                        enemy.Cooldown = 2.8f;
                    }
                }
                else if (enemy.Kind == 12) // Lantern wisp: orbiting pairs leave the aim line open.
                {
                    Vector2 orbit = new Vector2(-direction.y, direction.x) * (enemy.Attack % 2 == 0 ? 1 : -1);
                    Vector2 motion = orbit + direction * (distance > 5.5f ? .75f : distance < 3.2f ? -1 : 0);
                    enemy.Position = MoveEnemy(enemy, motion * (dt * 1.75f), .3f);
                    if (enemy.Cooldown <= 0 && !CoverBetween(enemy.Position, playerPosition))
                    {
                        FireHostile(enemy.Position, Rotate(direction, -17) * 5.2f, 1, enemy.Kind);
                        FireHostile(enemy.Position, Rotate(direction, 17) * 5.2f, 1, enemy.Kind);
                        enemy.Attack++;
                        enemy.Cooldown = 1.6f;
                    }
                }
                else if (enemy.Kind == 13) // Fungus brute: deliberate slam with a full ground warning.
                {
                    if (enemy.DashTime > 0)
                    {
                        enemy.Position = MoveEnemy(enemy, enemy.Dash * (dt * 5.8f), .55f);
                        enemy.DashTime = Mathf.Max(0, enemy.DashTime - dt);
                    }
                    else if (enemy.Cooldown <= 0)
                    {
                        enemy.Dash = direction;
                        enemy.DashTime = .42f;
                        WarnGround(playerPosition, 1.05f);
                        for (int i = 0; i < 6; i++) FireHostile(enemy.Position, Rotate(Vector2.right, i * 60 + 30) * 2.8f, 1, enemy.Kind);
                        enemy.Cooldown = 3.0f;
                    }
                    else if (enemy.Cooldown > .65f)
                        enemy.Position = MoveEnemy(enemy, direction * (dt * 1.35f), .55f);
                }
                else if (enemy.Kind == 14) // Clockwork imp: quick lateral movement and narrow needle bursts.
                {
                    Vector2 side = new Vector2(-direction.y, direction.x) * (enemy.Attack % 2 == 0 ? 1 : -1);
                    Vector2 motion = direction * (distance > 6 ? .8f : distance < 3 ? -1 : 0) + side * .7f;
                    if (enemy.Cooldown > .4f) enemy.Position = MoveEnemy(enemy, motion * (dt * 2.1f), .3f);
                    if (enemy.Cooldown <= 0 && !CoverBetween(enemy.Position, playerPosition))
                    {
                        FireHostile(enemy.Position, direction * 6.3f, 1, enemy.Kind);
                        FireHostile(enemy.Position, Rotate(direction, enemy.Attack % 2 == 0 ? 12 : -12) * 5.3f, 1, enemy.Kind);
                        enemy.Attack++;
                        enemy.Cooldown = 1.45f;
                    }
                }
                else UpdateBoss(enemy, direction, dt);
                SeparateEnemy(enemy, dt);
                if (enemy.Kind != 3 || enemy.Cooldown > .5f) enemy.Tell = enemy.Cooldown < .55f ? .55f - enemy.Cooldown : 0;
                if (Vector2.Distance(enemy.Position, playerPosition) < (enemy.Kind == 4 || enemy.Kind == 11 ? .95f : enemy.Kind == 13 ? .78f : .6f)) HurtPlayer();
                PresentEnemy(enemy, direction);
            }
        }

        void SeparateEnemy(Enemy enemy, float dt)
        {
            if (enemy.Kind == 4 || enemy.DashTime > 0) return;
            Vector2 separation = Vector2.zero;
            foreach (var other in enemies)
            {
                if (other == enemy || other.SpawnRemaining > 0) continue;
                Vector2 away = enemy.Position - other.Position;
                float squared = away.sqrMagnitude;
                if (squared > .001f && squared < .85f * .85f)
                    separation += away.normalized * (1 - Mathf.Sqrt(squared) / .85f);
            }
            if (separation.sqrMagnitude > .001f)
                enemy.Position = MoveEnemy(enemy, Vector2.ClampMagnitude(separation, 1) * (dt * 1.8f), enemy.Kind == 13 || enemy.Kind == 11 ? .55f : .3f);
        }

        void FireHostile(Vector2 position, Vector2 velocity, int damage = 1, int kind = 0)
        {
            // Leave capacity for the player even during dense boss patterns.
            if (shots.Count >= MaxShots - 72) return;
            int active = 0;
            foreach (var shot in shots)
                if (!shot.Friendly && shot.Life > 0 && ++active >= 140) return;
            int before = shots.Count;
            Fire(position, velocity, false, damage);
            if (shots.Count > before) StyleLastHostileShot(kind);
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
            int pattern = boss.Attack++;
            bool alternate = pattern % 2 == 1;
            int count = enraged ? 20 : 14;
            float offset = boss.Phase * 21;
            if (variant == 0) // Keep the Pumpkin King's readable signature.
            {
                for (int i = 0; i < count; i++) FireHostile(boss.Position, Rotate(Vector2.right, offset + 360f * i / count) * 3.5f);
                for (int i = -1; i <= 1; i++) FireHostile(boss.Position, Rotate(direction, 12 * i) * 5);
            }
            else if (variant == 1)
            {
                if (alternate) // Veil charge followed by crossed silk fans.
                {
                    boss.Dash = direction; boss.DashTime = .6f;
                    for (int i = -3; i <= 3; i++)
                    {
                        FireHostile(boss.Position, Rotate(direction, i * 16 + 35) * 4.3f);
                        FireHostile(boss.Position, Rotate(direction, i * 16 - 35) * 3.3f);
                    }
                }
                else for (int arm = 0; arm < 6; arm++)
                    for (int bead = 0; bead < 3; bead++) FireHostile(boss.Position, Rotate(Vector2.right, arm * 60 + offset) * (2.8f + bead));
            }
            else if (variant == 2)
            {
                for (int i = 0; i < 4; i++)
                    for (int j = -1; j <= 1; j++) FireHostile(boss.Position, Rotate(Vector2.right, offset + i * 90 + j * 9) * (enraged ? 4.5f : 3.5f));
                if (alternate) // Consecration: marked ground erupts after a full second.
                    for (int i = -1; i <= 1; i++) WarnGround(playerPosition + Vector2.right * (i * 2.7f), 1.05f);
                else FireHostile(boss.Position, direction * 6);
            }
            else if (variant == 3)
            {
                if (alternate)
                {
                    for (int i = -2; i <= 2; i++) FireHostile(boss.Position, Rotate(direction, i * 18) * 5);
                    WarnGround(playerPosition, 1.4f);
                    WarnGround(playerPosition + new Vector2(3, 2), 1.15f);
                    WarnGround(playerPosition - new Vector2(3, 2), 1.15f);
                }
                else for (int i = 0; i < 14; i++) FireHostile(boss.Position, Rotate(Vector2.right, i * 360f / 14 + offset) * (i % 2 == 0 ? 3 : 5));
            }
            else if (variant == 4)
            {
                if (alternate) // Tolling lanes: parallel volleys with room to pass.
                {
                    for (int i = -4; i <= 4; i++)
                        if (i != 0) FireHostile(new Vector2(boss.Position.x + i * 1.2f, boss.Position.y), Vector2.down * 4.5f);
                    WarnGround(playerPosition, 1.25f);
                }
                else for (int i = 0; i < 18; i++)
                {
                    if (i % 6 == boss.Attack % 6) continue;
                    Vector2 ray = Rotate(Vector2.right, i * 20 + offset);
                    FireHostile(boss.Position, ray * 3);
                    if (enraged) FireHostile(boss.Position, ray * 4.6f);
                }
            }
            else if (variant == 5) // Iron executioner: charge, chain grid, then an axe-shaped fan.
            {
                switch (pattern % 3)
                {
                    case 0:
                        boss.Dash = direction;
                        boss.DashTime = enraged ? .8f : .6f;
                        for (int i = -3; i <= 3; i++)
                            if (i != 0) FireHostile(boss.Position, Rotate(direction, i * 20) * 3.7f);
                        break;
                    case 1:
                        for (int i = -2; i <= 2; i++)
                        {
                            Vector2 chain = pattern % 2 == 0 ? Vector2.right : Vector2.up;
                            WarnGround(playerPosition + chain * (i * 2.6f), .85f);
                        }
                        break;
                    default:
                        for (int i = -5; i <= 5; i++)
                            if (i != 0) FireHostile(boss.Position, Rotate(direction, i * 13) * (enraged ? 4.8f : 3.8f));
                        WarnGround(boss.Position, 1.5f);
                        break;
                }
            }
            else // Bog oracle: blooming rings, growing roots and a three-headed serpent fan.
            {
                switch (pattern % 3)
                {
                    case 0:
                        for (int i = 0; i < 16; i++)
                        {
                            if (i % 8 == pattern % 8 || i % 8 == (pattern + 1) % 8) continue;
                            FireHostile(boss.Position, Rotate(Vector2.right, i * 22.5f + offset) * (i % 2 == 0 ? 2.8f : 4.0f));
                        }
                        break;
                    case 1:
                        WarnGround(playerPosition, 1.0f);
                        for (int i = 0; i < (enraged ? 5 : 3); i++)
                            WarnGround(playerPosition + Rotate(Vector2.right, i * 72 + offset) * 2.7f, .8f);
                        break;
                    default:
                        for (int head = -1; head <= 1; head++)
                            for (int bead = 0; bead < 3; bead++)
                                FireHostile(boss.Position, Rotate(direction, head * 28 + bead * 7 - 7) * (3.2f + bead * .65f));
                        break;
                }
            }
            RingEffect(boss.Position, BossAccent, 2.1f);
            Burst(boss.Position + Vector2.up * .6f, BossAccent, 16);
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
            PlaySound(SoundCue.PlayerHit);
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
            PlaySound(SoundCue.Door, .3f);
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
            if (bought) PlaySound(SoundCue.Purchase);
            if (bought && (item == ShopItem.GiantRounds || item == ShopItem.CharacterRelic || item == ShopItem.QuickBoots))
                PlaySound(SoundCue.RelicPickup, .7f);
            if (bought) { RingEffect(playerPosition, PlayerAccent, 1.5f); Burst(playerPosition + Vector2.up, new Color(1, .75f, .2f), 10); }
            Notify(message);
        }

        static Vector2 ShopPosition(int index) => new Vector2(-3.5f + index * 7, -1.8f);

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
            PauseAudio(false);
            ClearActors();
            ResetRoomProps();
            Playing = Paused = InShop = shopMenuOpen = false;
            State = null; Floor = null; CurrentRoom = null;
            BuildRoom(true);
        }

        void OnDestroy()
        {
            DisposeAudio();
            Cursor.visible = true;
            if (glowMaterial) Destroy(glowMaterial);
            if (projectileLightMaterial) Destroy(projectileLightMaterial);
            if (pixelText != null) pixelText.Dispose();
            if (art != null) art.Dispose();
            if (panelTexture) Destroy(panelTexture);
        }
    }
}
