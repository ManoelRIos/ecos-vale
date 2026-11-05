using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Ecos.Haunt.Fps
{
    /// <summary>Independent first-person vertical slice. The 2D run remains in Haunt.unity.</summary>
    public sealed partial class HauntFpsGame : MonoBehaviour
    {
        const int PlayerLayer = 8, EnemyLayer = 9, BarrelLayer = 10, ViewLayer = 31;
        const int ProjectileLimit = 96, SparkLimit = 144, TraceLimit = 24;
        enum ScreenMode { Menu, Playing, Victory, Dead }
        ScreenMode mode;
        GameObject runRoot;
        HauntFpsPlayer player;
        HauntFpsWorld world;
        HauntFpsAudio sound;
        RunState state;
        readonly List<HauntFpsEnemy> enemies = new List<HauntFpsEnemy>();
        readonly List<HauntFpsBarrel> barrels = new List<HauntFpsBarrel>();
        readonly List<Hostile> projectiles = new List<Hostile>();
        readonly List<Spark> sparks = new List<Spark>();
        readonly List<Trace> traces = new List<Trace>();
        readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        RenderPipelineAsset savedGraphicsPipeline, savedQualityPipeline, appliedPipeline;
        CursorLockMode savedCursor;
        bool savedCursorVisible, paused, focusLost, disposed, exitReady, bootstrapped;
        float damageGrace, hitMarker, damageOverlay, waveDelay, spawnDelay, noticeRemaining;
        int wave, pendingSkeletons, pendingLanterns, kills, spawnSerial, seed = 1105;
        string notice = "", failure;
        readonly HauntPixelText lettering = new HauntPixelText();

        sealed class Hostile
        {
            public Transform Visual;
            public LineRenderer Trail;
            public Vector3 Position, Direction;
            public float Speed, Life = 5;
            public int Damage;
            public Color Color;
        }
        sealed class Spark
        {
            public Transform Visual;
            public Vector3 Velocity;
            public float Remaining, Duration;
        }
        sealed class Trace { public LineRenderer Line; public float Remaining; }

        public RunState State => state;
        public HauntFpsPlayer Player => player;
        public HauntFpsWorld World => world;
        public Vector3 PlayerPosition => player ? player.Position : Vector3.zero;
        public bool CanSimulate => !disposed && string.IsNullOrEmpty(failure) && mode == ScreenMode.Playing
            && !paused && !focusLost && state != null && !state.IsDead;
        public int WaveNumber => wave + 1;
        public int KillCount => kills;
        public int ActiveProjectileCount => projectiles.Count;

        void Awake()
        {
            bootstrapped = true;
            Application.targetFrameRate = 60;
            savedCursor = Cursor.lockState; savedCursorVisible = Cursor.visible;
            savedGraphicsPipeline = GraphicsSettings.defaultRenderPipeline;
            savedQualityPipeline = QualitySettings.renderPipeline;
            try
            {
                appliedPipeline = Resources.Load<RenderPipelineAsset>("FpsPipeline");
                if (!appliedPipeline) throw new InvalidOperationException("FpsPipeline ausente. Execute Ecos > Preparar projeto no Editor.");
                GraphicsSettings.defaultRenderPipeline = appliedPipeline;
                QualitySettings.renderPipeline = appliedPipeline;
                sound = gameObject.AddComponent<HauntFpsAudio>();
                BuildRun(seed);
            }
            catch (Exception error)
            {
                failure = "FALHA AO PREPARAR O FPS. VEJA O CONSOLE DO UNITY.";
                Debug.LogException(error, this);
            }
            SetCursor(false);
        }

        void BuildRun(int newSeed)
        {
            ClearRun();
            seed = newSeed; state = new RunState(seed); state.Equip(WeaponKind.Pistol);
            wave = kills = spawnSerial = 0; pendingSkeletons = pendingLanterns = 0;
            damageGrace = hitMarker = damageOverlay = waveDelay = spawnDelay = 0;
            exitReady = false; paused = false; noticeRemaining = 0;
            runRoot = new GameObject("Protótipo FPS • arena");
            runRoot.transform.SetParent(transform, false);
            var worldObject = new GameObject("Cripta tridimensional");
            worldObject.transform.SetParent(runRoot.transform, false);
            world = worldObject.AddComponent<HauntFpsWorld>(); world.Initialize(this);
            var avatar = new GameObject("Caçador • primeira pessoa");
            avatar.layer = PlayerLayer; avatar.transform.SetParent(runRoot.transform, false);
            avatar.transform.position = world.PlayerSpawn;
            var body = avatar.AddComponent<CharacterController>();
            var cameraObject = new GameObject("Câmera FPS");
            cameraObject.transform.SetParent(avatar.transform, false);
            var camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .018f, .05f);
            cameraObject.AddComponent<AudioListener>();
            player = avatar.AddComponent<HauntFpsPlayer>(); player.Initialize(this, camera, body);
            foreach (Vector3 position in world.BarrelSpawns)
            {
                var obj = new GameObject("Barril explosivo"); obj.transform.SetParent(runRoot.transform, false);
                var barrel = obj.AddComponent<HauntFpsBarrel>(); barrel.Initialize(this, position);
                SetLayer(obj.transform, BarrelLayer); barrels.Add(barrel);
            }
            Physics.SyncTransforms();
            sound.Initialize(camera.transform); sound.SetSuspended(true);
        }

        public void BeginRun(int newSeed)
        {
            if (disposed || !string.IsNullOrEmpty(failure)) return;
            BuildRun(newSeed); mode = ScreenMode.Playing;
            StartWave(0); player.SuspendInput(); UpdateSuspension();
        }

        void Update()
        {
            if (disposed || state == null || !string.IsNullOrEmpty(failure)) return;
            var keyboard = Keyboard.current; var pad = Gamepad.current;
            bool confirm = keyboard != null && keyboard.enterKey.wasPressedThisFrame
                || pad != null && pad.buttonSouth.wasPressedThisFrame;
            bool escape = keyboard != null && keyboard.escapeKey.wasPressedThisFrame
                || pad != null && pad.startButton.wasPressedThisFrame;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame) sound.SetEnabled(!sound.SoundEnabled);
            if (mode != ScreenMode.Playing)
            {
                if (sound && mode != ScreenMode.Menu && !focusLost)
                    sound.Tick(Mathf.Min(Time.unscaledDeltaTime, .05f), false);
                if (confirm && !focusLost) BeginRun(mode == ScreenMode.Menu ? seed : unchecked(seed + 1));
                return;
            }
            if (escape && !focusLost) SetPaused(!paused);
            else if (paused && confirm && !focusLost) SetPaused(false);
            if (!CanSimulate) return;
            TickSimulation(Time.unscaledDeltaTime);
            if (!CanSimulate) return;
            bool interact = keyboard != null && keyboard.eKey.wasPressedThisFrame
                || pad != null && pad.buttonWest.wasPressedThisFrame;
            if (exitReady && interact && Vector3.Distance(PlayerPosition, world.ExitPosition) < 2.8f)
            {
                mode = ScreenMode.Victory; PlayCue(FpsCue.Victory, PlayerPosition); UpdateSuspension();
            }
        }

        // A single clock advances reloads, actors and effects. Pause/focus guards
        // live here as well as in Update so no caller can advance a suspended run.
        void TickSimulation(float dt)
        {
            if (!CanSimulate || !(dt > 0) || float.IsInfinity(dt)) return;
            dt = Mathf.Min(dt, .05f);
            damageGrace = Mathf.Max(0, damageGrace - dt);
            hitMarker = Mathf.Max(0, hitMarker - dt); damageOverlay = Mathf.Max(0, damageOverlay - dt * 2);
            noticeRemaining = Mathf.Max(0, noticeRemaining - dt);
            bool reloading = state.IsReloading;
            state.TickReload(dt);
            if (reloading && !state.IsReloading) PlayCue(FpsCue.ReloadEnd, PlayerPosition);
            player.Tick(dt);
            if (!CanSimulate) return;
            world.Tick(dt);
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (!enemies[i]) enemies.RemoveAt(i);
                else enemies[i].Tick(dt);
                if (!CanSimulate) return;
            }
            for (int i = 0; i < barrels.Count; i++) if (barrels[i]) barrels[i].Tick(dt);
            if (!CanSimulate) return;
            TickProjectiles(dt); TickEffects(dt); TickWaves(dt);
            sound.Tick(dt, !exitReady);
        }

        void StartWave(int index)
        {
            wave = index;
            FpsWaveSpec spec = FpsPrototypeRules.Wave(index, seed);
            pendingSkeletons = spec.Skeletons; pendingLanterns = spec.Lanterns;
            waveDelay = 0; spawnDelay = .75f;
            Notify("ONDA " + (wave + 1) + " / " + FpsPrototypeRules.WaveCount, 2.1f);
        }

        void TickWaves(float dt)
        {
            if (exitReady) return;
            if (pendingSkeletons + pendingLanterns > 0)
            {
                spawnDelay -= dt;
                if (spawnDelay > 0) return;
                FpsEnemyKind kind = pendingSkeletons > 0 ? FpsEnemyKind.Skeleton : FpsEnemyKind.Lantern;
                if (!TrySpawn(kind)) { spawnDelay = .25f; return; }
                if (kind == FpsEnemyKind.Skeleton) pendingSkeletons--; else pendingLanterns--;
                spawnDelay = FpsPrototypeRules.Wave(wave, seed).SpawnInterval;
                return;
            }
            foreach (var enemy in enemies) if (enemy && !enemy.IsDead) return;
            waveDelay += dt;
            if (waveDelay < 1.5f) return;
            ClearProjectiles();
            if (wave + 1 < FpsPrototypeRules.WaveCount) StartWave(wave + 1);
            else
            {
                exitReady = true; world.SetExitOpen(true);
                Notify("SALA PURIFICADA. SIGA A LUZ VERDE E PRESSIONE E NO PORTAL.", 6);
                PlayCue(FpsCue.Victory, world.ExitPosition);
            }
        }

        bool TrySpawn(FpsEnemyKind kind)
        {
            Vector3[] bases = kind == FpsEnemyKind.Skeleton ? world.MeleeSpawns : world.RangedSpawns;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float angle = (spawnSerial * 2.39996f + attempt * .65f);
                float radius = attempt == 0 ? 0 : .65f + attempt * .18f;
                Vector3 position = bases[(spawnSerial + attempt) % bases.Length]
                    + new Vector3(Mathf.Cos(angle) * radius, .06f, Mathf.Sin(angle) * radius);
                if (!world.IsInsideBounds(position, 1.3f) || Vector3.Distance(position, PlayerPosition) < 4) continue;
                if (Physics.CheckCapsule(position + Vector3.up * .4f, position + Vector3.up * 1.45f,
                    .38f, ~(1 << ViewLayer), QueryTriggerInteraction.Ignore)) continue;
                var obj = new GameObject(kind == FpsEnemyKind.Skeleton ? "Esqueleto" : "Lanterna errante");
                obj.transform.SetParent(runRoot.transform, false); obj.layer = EnemyLayer;
                var enemy = obj.AddComponent<HauntFpsEnemy>(); enemy.Initialize(this, kind, position);
                SetLayer(obj.transform, EnemyLayer); enemies.Add(enemy); spawnSerial++;
                Burst(position + Vector3.up * .3f, new Color(.4f, .85f, .72f), 10, 2);
                Physics.SyncTransforms(); return true;
            }
            return false;
        }

        public bool TryFire(Ray ray)
        {
            if (!CanSimulate) return false;
            if (!state.TryConsumeShot())
            {
                if (!state.IsReloading) { PlayCue(FpsCue.DryFire, PlayerPosition); Notify("SEM BALAS NO TAMBOR. R PARA RECARREGAR.", 1.2f); }
                return false;
            }
            Vector3 end = ray.GetPoint(FpsPrototypeRules.ShotRange);
            int mask = ~((1 << PlayerLayer) | (1 << ViewLayer));
            if (Physics.Raycast(ray, out RaycastHit hit, FpsPrototypeRules.ShotRange, mask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var enemy = hit.collider.GetComponentInParent<HauntFpsEnemy>();
                var barrel = hit.collider.GetComponentInParent<HauntFpsBarrel>();
                if (enemy && !enemy.IsDead)
                {
                    enemy.TakeDamage(state.Damage, hit.point, ray.direction * 4);
                    PlayCue(FpsCue.EnemyHit, hit.point); hitMarker = .12f;
                }
                else if (barrel && !barrel.IsDestroyed) { barrel.TakeDamage(state.Damage, hit.point); hitMarker = .12f; }
                Burst(end, enemy ? new Color(.8f, .3f, .95f) : new Color(1, .65f, .22f), 5, 1.8f);
            }
            AddTrace(player.MuzzlePosition, end, new Color(1, .8f, .33f));
            PlayCue(FpsCue.Shot, PlayerPosition); AddShake(.035f); return true;
        }

        public bool TryDamagePlayer(int amount, Vector3 force)
        {
            if (!CanSimulate || amount <= 0 || damageGrace > 0 || player.IsInvulnerable) return false;
            if (!state.Hurt(amount)) return false;
            damageGrace = .7f; damageOverlay = .7f; AddShake(.3f);
            var body = player.GetComponent<CharacterController>();
            if (body && body.enabled) body.Move(Vector3.ClampMagnitude(force, 6) * .04f);
            PlayCue(FpsCue.PlayerHit, PlayerPosition);
            if (state.IsDead) { mode = ScreenMode.Dead; UpdateSuspension(); }
            return true;
        }

        public void FireHostile(Vector3 origin, Vector3 direction, float speed, int damage, Color color)
        {
            if (!CanSimulate || projectiles.Count >= ProjectileLimit || direction.sqrMagnitude < .001f || !(speed > 0)) return;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Orbe espectral"; visual.transform.SetParent(runRoot.transform, false);
            visual.transform.position = origin; visual.transform.localScale = Vector3.one * .23f;
            var collider = visual.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
            Material material = PixelMaterial("orbe espectral", color);
            material.SetColor("_EmissionColor", color * 1.4f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            var trail = new GameObject("Rastro").AddComponent<LineRenderer>(); trail.transform.SetParent(visual.transform, false);
            SetupLine(trail, material, .065f); trail.positionCount = 2;
            trail.SetPosition(0, origin); trail.SetPosition(1, origin);
            projectiles.Add(new Hostile { Visual = visual.transform, Trail = trail, Position = origin,
                Direction = direction.normalized, Speed = Mathf.Min(speed, 40), Damage = Mathf.Max(1, damage), Color = color });
            PlayCue(FpsCue.HostileShot, origin);
        }

        void TickProjectiles(float dt)
        {
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Hostile shot = projectiles[i];
                float distance = shot.Speed * dt;
                int mask = ~((1 << EnemyLayer) | (1 << ViewLayer));
                if (player.IsInvulnerable) mask &= ~(1 << PlayerLayer);
                bool remove = false;
                if (Physics.SphereCast(shot.Position, .1f, shot.Direction, out RaycastHit hit,
                    distance, mask, QueryTriggerInteraction.Ignore))
                {
                    var avatar = hit.collider.GetComponentInParent<HauntFpsPlayer>();
                    var barrel = hit.collider.GetComponentInParent<HauntFpsBarrel>();
                    if (avatar) TryDamagePlayer(shot.Damage, shot.Direction * 3);
                    else if (barrel) barrel.TakeDamage(shot.Damage, hit.point);
                    Burst(hit.point, shot.Color, 5, 1.4f); remove = true;
                }
                shot.Position += shot.Direction * distance; shot.Life -= dt;
                if (shot.Life <= 0 || !world.IsInsideBounds(shot.Position, -.5f)) remove = true;
                if (remove) { if (shot.Visual) Destroy(shot.Visual.gameObject); projectiles.RemoveAt(i); }
                else
                {
                    shot.Visual.position = shot.Position;
                    shot.Trail.SetPosition(0, shot.Position);
                    shot.Trail.SetPosition(1, shot.Position - shot.Direction * .6f);
                }
            }
        }

        public void OnEnemyKilled(HauntFpsEnemy enemy)
        {
            if (!enemy || !enemies.Contains(enemy)) return;
            kills++; state.Coins += 5;
            PlayCue(FpsCue.EnemyDeath, enemy.transform.position);
            Burst(enemy.AimPosition, new Color(.55f, .84f, .7f), 12, 2.8f);
        }

        public void Explode(Vector3 position, float radius, int damage)
        {
            if (!CanSimulate || !(radius > 0) || damage <= 0) return;
            AddShake(.85f); PlayCue(FpsCue.Explosion, position);
            Burst(position, new Color(1, .4f, .07f), 36, 7);
            // Snapshot registries: chained damage can change actor state and fire callbacks.
            foreach (var enemy in enemies.ToArray())
            {
                if (!enemy || enemy.IsDead) continue;
                int hit = FpsPrototypeRules.ExplosionDamage(Vector3.Distance(position, enemy.AimPosition), radius, damage);
                if (hit > 0 && BlastVisible(position, enemy.AimPosition))
                    enemy.TakeDamage(hit, enemy.AimPosition, (enemy.AimPosition - position).normalized * 8);
            }
            foreach (var barrel in barrels.ToArray())
            {
                if (!barrel || barrel.IsDestroyed) continue;
                int hit = FpsPrototypeRules.ExplosionDamage(Vector3.Distance(position, barrel.BlastPosition), radius, damage);
                if (hit > 0 && BlastVisible(position, barrel.BlastPosition)) barrel.TakeDamage(hit, position);
            }
            int playerHit = FpsPrototypeRules.ExplosionDamage(Vector3.Distance(position, PlayerPosition + Vector3.up), radius, damage);
            if (playerHit > 0 && BlastVisible(position, PlayerPosition + Vector3.up))
                TryDamagePlayer(Mathf.Min(3, playerHit), (PlayerPosition - position).normalized * 6);
        }

        static bool BlastVisible(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            // Static walls and pillars are layer zero; living actors do not absorb the blast ray.
            return !Physics.Raycast(from, delta.normalized, delta.magnitude, 1, QueryTriggerInteraction.Ignore);
        }

        public void PlayCue(FpsCue cue, Vector3 position) { if (sound) sound.Play(cue, 1, position); }
        public void AddShake(float amount) { if (player) player.AddTrauma(amount); }
        public void Notify(string text, float seconds) { notice = text ?? ""; noticeRemaining = Mathf.Max(0, seconds); }

        public Material PixelMaterial(string id, Color tint)
        {
            if (materials.TryGetValue(id, out Material existing)) return existing;
            Material template = Resources.Load<Material>("FpsPalette");
            if (!template || !template.shader) throw new InvalidOperationException("Material Resources/FpsPalette ausente.");
            var material = new Material(template) { name = "FPS • " + id };
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { name = id + " • pixels", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                int noise = (x * 73 + y * 191 + x * y * 13) & 15;
                float v = .78f + noise / 70f; pixels[y * 32 + x] = new Color(v, v, v, 1);
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            material.SetTexture("_BaseMap", texture); material.SetColor("_BaseColor", tint);
            material.SetColor("_EmissionColor", Color.black);
            materials.Add(id, material); textures.Add(texture); return material;
        }

        public GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) { var collider = obj.GetComponent<Collider>(); collider.enabled = false; Destroy(collider); }
            return obj;
        }

        static void SetLayer(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            for (int i = 0; i < node.childCount; i++) SetLayer(node.GetChild(i), layer);
        }

        void Burst(Vector3 position, Color color, int count, float speed)
        {
            Material material = PixelMaterial(color.r > .8f ? "faísca quente" : "faísca espectral", color);
            material.SetColor("_EmissionColor", color * .7f);
            for (int i = 0; i < count && sparks.Count < SparkLimit; i++)
            {
                float angle = (i + spawnSerial) * 2.39996f;
                var obj = Box("Estilhaço", position, Vector3.one * (.035f + (i % 3) * .014f), material, runRoot.transform, false);
                sparks.Add(new Spark { Visual = obj.transform, Duration = .3f + (i % 5) * .08f, Remaining = .3f + (i % 5) * .08f,
                    Velocity = new Vector3(Mathf.Cos(angle), .25f + (i % 4) * .22f, Mathf.Sin(angle)) * speed });
            }
        }

        static void SetupLine(LineRenderer line, Material material, float width)
        {
            line.sharedMaterial = material; line.useWorldSpace = true;
            line.startWidth = width; line.endWidth = width * .35f;
            line.numCapVertices = 0; line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
        }

        void AddTrace(Vector3 from, Vector3 to, Color color)
        {
            if (traces.Count >= TraceLimit) { if (traces[0].Line) Destroy(traces[0].Line.gameObject); traces.RemoveAt(0); }
            var obj = new GameObject("Traçante"); obj.transform.SetParent(runRoot.transform, false);
            var line = obj.AddComponent<LineRenderer>();
            Material material = PixelMaterial("traçante dourado", color); material.SetColor("_EmissionColor", color * 1.5f);
            SetupLine(line, material, .035f); line.positionCount = 2; line.SetPosition(0, from); line.SetPosition(1, to);
            traces.Add(new Trace { Line = line, Remaining = .045f });
        }

        void TickEffects(float dt)
        {
            for (int i = traces.Count - 1; i >= 0; i--)
            {
                traces[i].Remaining -= dt;
                if (traces[i].Remaining <= 0) { if (traces[i].Line) Destroy(traces[i].Line.gameObject); traces.RemoveAt(i); }
            }
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                Spark spark = sparks[i]; spark.Remaining -= dt;
                if (spark.Remaining <= 0 || !spark.Visual) { if (spark.Visual) Destroy(spark.Visual.gameObject); sparks.RemoveAt(i); continue; }
                spark.Velocity += Vector3.down * 8 * dt;
                Vector3 pos = spark.Visual.position + spark.Velocity * dt;
                if (pos.y < .04f) { pos.y = .04f; spark.Velocity = Vector3.zero; }
                spark.Visual.position = pos; spark.Visual.localScale = Vector3.one * .065f * Mathf.Clamp01(spark.Remaining / spark.Duration);
            }
        }

        void SetPaused(bool value) { paused = value; if (player) player.SuspendInput(); UpdateSuspension(); }
        void UpdateSuspension()
        {
            bool suspended = !CanSimulate;
            // Death/victory stop gameplay while their final cue and the quieter
            // ambience finish. An explicit pause or lost focus silences both.
            if (sound) sound.SetSuspended(paused || focusLost || mode == ScreenMode.Menu || !string.IsNullOrEmpty(failure));
            if (player) player.SuspendInput();
            SetCursor(!suspended);
        }
        static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnApplicationFocus(bool focus)
        {
            focusLost = !focus;
            if (!focus && mode == ScreenMode.Playing) paused = true;
            UpdateSuspension();
        }
        void OnApplicationPause(bool value) { if (value) { focusLost = true; paused = true; } else focusLost = false; UpdateSuspension(); }

        void ClearProjectiles()
        {
            foreach (var shot in projectiles) if (shot.Visual) { shot.Visual.gameObject.SetActive(false); Destroy(shot.Visual.gameObject); }
            projectiles.Clear();
        }
        void ClearRun()
        {
            if (runRoot) runRoot.SetActive(false);
            if (player) player.DisposeView();
            ClearProjectiles(); enemies.Clear(); barrels.Clear(); sparks.Clear(); traces.Clear();
            if (world != null) world.Dispose(); world = null;
            if (runRoot) Destroy(runRoot); runRoot = null; player = null;
        }
        void OnDestroy()
        {
            if (disposed) return; disposed = true;
            ClearRun(); if (sound) sound.Dispose(); lettering.Dispose();
            foreach (var material in materials.Values) if (material) Destroy(material);
            foreach (var texture in textures) if (texture) Destroy(texture);
            materials.Clear(); textures.Clear();
            if (appliedPipeline && GraphicsSettings.defaultRenderPipeline == appliedPipeline) GraphicsSettings.defaultRenderPipeline = savedGraphicsPipeline;
            if (appliedPipeline && QualitySettings.renderPipeline == appliedPipeline) QualitySettings.renderPipeline = savedQualityPipeline;
            if (bootstrapped) { Cursor.lockState = savedCursor; Cursor.visible = savedCursorVisible; }
        }
    }
}
