using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Ecos.Haunt.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Ecos.Haunt.Tests
{
    public sealed class HauntProjectTests
    {
        [Test]
        public void BuildContainsOnlyRoguelike()
        {
            var scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(HauntProject.ScenePath, scenes.First(s => s.enabled).path);
            Assert.AreEqual(1, scenes.Length);
            Assert.IsTrue(scenes[0].enabled);
            Assert.AreEqual(1, scenes.Count(s => s.path == HauntProject.ScenePath));
        }

        [Test]
        public void AuthoredSceneHasExactlyOneBootstrap()
        {
            var previous = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(HauntProject.ScenePath);
            bool opened = !existing.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(HauntProject.ScenePath, OpenSceneMode.Additive) : existing;
            try
            {
                Assert.AreEqual(1, scene.GetRootGameObjects().Sum(g => g.GetComponentsInChildren<HauntGame>(true).Length));
                Assert.AreEqual(1, scene.GetRootGameObjects().Length);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        [Test]
        public void AnimationFramesReuseTexturesAndKeepStableBounds()
        {
            using (var art = new HauntPixelArt())
            {
                foreach (string id in new[] { "player", "medium", "reaper", "alchemist", "widow", "bishop", "larva", "sentinel", "acolyte", "matriarch", "bell", "pumpkin", "ghost", "cultist", "bat", "boss", "merchant", "candle", "portal", "top-shot", "skeleton", "slime", "spider", "miniboss", "spawn-rift", "shot-disc" })
                {
                    var frames = new Sprite[4];
                    for (int i = 0; i < 4; i++)
                    {
                        frames[i] = art.Frame(id, i);
                        Assert.AreEqual(frames[0].rect, frames[i].rect, "Animation bounds must not jitter: " + id);
                        Assert.AreEqual(frames[0].pivot, frames[i].pivot, id);
                        Assert.AreEqual(FilterMode.Point, frames[i].texture.filterMode, id);
                    }
                    Assert.AreNotSame(frames[0], frames[1], "Poses need distinct cached sprites: " + id);
                    Assert.AreSame(frames[3], art.Frame(id, -1), id);
                    for (int i = 0; i < 240; i++)
                        Assert.AreSame(frames[i % 4], art.Frame(id, i), "Repeated animation must not allocate more textures: " + id);
                }
                foreach (string id in new[] { "chandelier", "exit-arrow", "floor3", "floor4", "floor5", "door-open", "door-locked", "door-boss", "shadow", "player-ring", "enemy-ring", "glow", "rune", "rug", "weapon", "scattergun", "repeater", "cannon", "slash", "dust", "spark", "effect-ring", "pit", "fog", "hunter-carbine", "veil-wand", "bone-caster", "arc-coil", "mirror-disc", "ray-lantern", "bone-flute", "shot-dot", "shot-bone", "shot-shock", "shot-ray" })
                    Assert.IsNotNull(art.Get(id), id);
            }
        }

        [Test]
        public void FloorThemesProvideRoomTilesOrnamentsAndAtlasMiniatures()
        {
            using (var art = new HauntPixelArt())
            {
                for (int theme = 0; theme < 5; theme++)
                {
                    foreach (string prefix in new[] { "floor-theme", "wall-theme", "decor-theme", "map-theme", "wall-relief" })
                    {
                        string id = prefix + theme;
                        var sprite = art.Get(id);
                        Assert.IsNotNull(sprite, id);
                        Assert.AreSame(sprite, art.Get(id), "Repeated themed lookups must reuse textures: " + id);
                        Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, id);
                        Assert.AreEqual(16, sprite.pixelsPerUnit, id);
                        if (prefix == "floor-theme" || prefix == "wall-theme")
                        {
                            Assert.AreEqual(16, sprite.rect.width, "The one-unit room grid needs 16px tiles: " + id);
                            Assert.AreEqual(16, sprite.rect.height, id);
                        }
                        if (prefix == "map-theme")
                            Assert.Greater(sprite.rect.width, sprite.rect.height, "Atlas miniature should show an arena silhouette: " + id);
                    }
                    for (int variant = 0; variant < 4; variant++)
                    {
                        string id = "floor-theme" + theme + "-v" + variant;
                        var tile = art.Get(id);
                        Assert.AreEqual(16, tile.rect.width, id);
                        Assert.AreEqual(16, tile.rect.height, id);
                        Assert.AreSame(tile, art.Get(id), "Floor variants must remain cached: " + id);
                    }
                }
            }
        }

        [Test]
        public void BossBarsAndWhimsicalShopArtUseStablePixelBounds()
        {
            using (var art = new HauntPixelArt())
            {
                for (int boss = 0; boss < 5; boss++)
                {
                    var frame = art.Get("boss-bar" + boss);
                    var fill = art.Get("boss-fill" + boss);
                    Assert.AreEqual(new Rect(0, 0, 240, 24), frame.rect, "All boss frames must fit the same health window");
                    Assert.AreEqual(new Rect(0, 0, 16, 8), fill.rect);
                    Assert.AreEqual(FilterMode.Point, frame.texture.filterMode);
                    Assert.AreSame(frame, art.Get("boss-bar" + boss));
                }
                foreach (string id in new[] { "stapler", "banana-gun", "spinning-top", "staple-shot", "banana-shot", "top-shot", "shop-counter", "shop-shelf", "shop-case", "shop-board" })
                {
                    var sprite = art.Get(id);
                    Assert.IsNotNull(sprite, id);
                    Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, id);
                    Assert.AreSame(sprite, art.Get(id), id);
                }
            }
        }

        [Test]
        public void BitmapTextNormalizesPortugueseAndWrapsWordsWithoutDroppingLetters()
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
            var normalize = typeof(HauntPixelText).GetMethod("Normalize", flags);
            var layout = typeof(HauntPixelText).GetMethod("Layout", flags);
            Assert.IsNotNull(normalize);
            Assert.IsNotNull(layout);
            string normalized = (string)normalize.Invoke(null, new object[] { "Coração · banana → 25% ç" });
            Assert.AreEqual("CORACAO * BANANA > 25% C", normalized);
            var words = ((IEnumerable)layout.Invoke(null, new object[] { "COMPRE UMA BANANA", 7, true })).Cast<string>().ToArray();
            CollectionAssert.AreEqual(new[] { "COMPRE", "UMA", "BANANA" }, words);
            var split = ((IEnumerable)layout.Invoke(null, new object[] { "GRAMPEADOR", 4, true })).Cast<string>().ToArray();
            Assert.IsTrue(split.All(line => line.Length <= 4));
            Assert.AreEqual("GRAMPEADOR", string.Concat(split));
            var lines = ((IEnumerable)layout.Invoke(null, new object[] { "ALMAS\n50", 3, false })).Cast<string>().ToArray();
            CollectionAssert.AreEqual(new[] { "ALMAS", "50" }, lines);
            var text = new HauntPixelText();
            text.Dispose();
            Assert.DoesNotThrow(text.Dispose);
            Assert.Throws<ObjectDisposedException>(() => text.Draw(new Rect(0, 0, 10, 10), "A", 1, Color.white));
        }

        [Test]
        public void PixelArtHasPointFilteredStableSpritesAndMaterial()
        {
            using (var art = new HauntPixelArt())
            {
                string[] ids = { "player", "pumpkin", "ghost", "cultist", "bat", "boss", "floor0", "floor1", "floor2", "wall", "pillar", "candle", "grave", "door", "portal", "merchant", "coin", "bullet-player", "bullet-enemy", "muzzle", "heart" };
                foreach (string id in ids)
                {
                    var sprite = art.Get(id);
                    Assert.IsNotNull(sprite, id);
                    Assert.AreSame(sprite, art.Get(id), "Repeated lookup must reuse the sprite: " + id);
                    Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, id);
                    Assert.AreEqual(16, sprite.pixelsPerUnit, id);
                    Assert.Greater(sprite.rect.width, 0, id);
                    Assert.Greater(sprite.rect.height, 0, id);
                }
                Assert.IsNotNull(art.Material);
                Assert.IsNotNull(art.Material.shader);
                Assert.IsTrue(art.Material.HasProperty("_MainTex"));
            }
        }
    }

    /// <summary>These check generated objects and simulation state, not rendered image correctness.</summary>
    // Unity's custom NUnit omits the parallel-execution attributes. Editor
    // coroutine tests use the Unity Test Runner and keep their per-test scene isolation.
    public sealed class HauntRuntimeTests
    {
        const string SceneSnapshotKey = "Ecos.Haunt.Tests.SceneSnapshot";
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        [Serializable]
        sealed class SavedScene
        {
            public string path;
            public bool loaded, active;
        }
        [Serializable]
        sealed class SavedWorkspace
        {
            public bool empty;
            public SavedScene[] scenes;
        }

        [UnitySetUp]
        public IEnumerator IsolateRuntimeScene()
        {
            // Never discard or silently save the user's unsaved work to run a test.
            var setup = EditorSceneManager.GetSceneManagerSetup();
            bool empty = setup.Length == 1 && string.IsNullOrEmpty(setup[0].path)
                && SceneManager.GetActiveScene().rootCount == 0 && !SceneManager.GetActiveScene().isDirty;
            if (!empty)
            {
                foreach (var entry in setup)
                {
                    if (string.IsNullOrEmpty(entry.path))
                        Assert.Ignore("Runtime scene isolation requires saved scenes; save untitled scene edits before running this fixture.");
                    var scene = SceneManager.GetSceneByPath(entry.path);
                    if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                        Assert.Ignore("Runtime scene isolation preserves unsaved work; save scene edits before running this fixture.");
                }
            }
            var saved = new SavedWorkspace
            {
                empty = empty,
                scenes = setup.Select(s => new SavedScene { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray()
            };
            // SessionState survives the domain reload caused by EnterPlayMode/ExitPlayMode.
            UnityEditor.SessionState.SetString(SceneSnapshotKey, JsonUtility.ToJson(saved));
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
        }

        [UnityTearDown]
        public IEnumerator RestoreRuntimeScene()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            string json = UnityEditor.SessionState.GetString(SceneSnapshotKey, "");
            if (string.IsNullOrEmpty(json)) yield break;
            var saved = JsonUtility.FromJson<SavedWorkspace>(json);
            if (saved.empty) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            else EditorSceneManager.RestoreSceneManagerSetup(saved.scenes.Select(s => new SceneSetup
                { path = s.path, isLoaded = s.loaded, isActive = s.active }).ToArray());
            UnityEditor.SessionState.EraseString(SceneSnapshotKey);
        }

        static object Invoke(HauntGame game, string method, params object[] args)
        {
            var info = typeof(HauntGame).GetMethod(method, PrivateInstance);
            Assert.IsNotNull(info, "Runtime operation missing: " + method);
            return info.Invoke(game, args);
        }

        static void Set(HauntGame game, string field, object value)
        {
            var info = typeof(HauntGame).GetField(field, PrivateInstance);
            Assert.IsNotNull(info, "Runtime field missing: " + field);
            info.SetValue(game, value);
        }

        static T Field<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing state field: " + name);
            return (T)field.GetValue(target);
        }

        static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing state field: " + name);
            field.SetValue(target, value);
        }

        static void FinishEmergence(HauntGame game)
        {
            foreach (object enemy in Field<IList>(game, "enemies"))
                SetField(enemy, "SpawnRemaining", 0f);
        }

        static void DefeatCurrentWave(HauntGame game)
        {
            var enemies = Field<IList>(game, "enemies");
            int count = enemies.Count;
            Assert.Greater(count, 0, "Every wave must contain an encounter");
            FinishEmergence(game);
            for (int i = 0; i < count; i++)
            {
                Vector2 position = Field<Vector2>(enemies[0], "Position");
                // Hit the actual collision circle instead of calling the reward path directly.
                Invoke(game, "Fire", position, Vector2.right, true, 10000);
                Invoke(game, "UpdateShots", .01f);
                if (i < count - 1) Assert.AreEqual(count - i - 1, game.LivingEnemies);
            }
        }

        static void StartFloor(HauntGame game, int seed, int number)
        {
            game.BeginRun(seed);
            game.State.Floor = number;
            var floor = DungeonFloor.Generate(seed, number);
            Set(game, "<Floor>k__BackingField", floor);
            Invoke(game, "EnterRoom", floor.Entrance, Vector2.zero);
        }

        static bool NearestExit(HauntGame game, out Vector2 destination)
        {
            object[] args = { Vector2.zero };
            bool found = (bool)Invoke(game, "TryNearestExit", args);
            destination = (Vector2)args[0];
            return found;
        }

        [UnityTest]
        public IEnumerator StartingAndRestartingRunConstructsFreshRuntimeState()
        {
            var root = new GameObject("Haunt runtime test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(71319);
                Assert.IsTrue(game.Playing);
                Assert.IsFalse(game.InShop);
                Assert.IsNotNull(game.State);
                Assert.IsNotNull(game.Floor);
                Assert.AreEqual(71319, game.State.Seed);
                Assert.AreEqual(1, game.State.Floor);
                Assert.AreSame(game.Floor.Entrance, game.CurrentRoom);
                Assert.IsTrue(game.CurrentRoom.Cleared);
                Assert.IsFalse(game.State.IsDead);
                Assert.Greater(game.GetComponentsInChildren<SpriteRenderer>().Length, 0,
                    "A run must construct room/player objects; this does not verify their rendered appearance.");
                var first = game.State;
                first.Hurt(first.Health);
                Assert.IsTrue(first.IsDead);

                game.BeginRun(71319);
                Assert.AreNotSame(first, game.State);
                Assert.AreEqual(game.State.MaxHealth, game.State.Health);
                Assert.AreEqual(1, game.State.Floor);
                Assert.AreSame(game.Floor.Entrance, game.CurrentRoom);
                Assert.IsFalse(game.InShop);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator CombatClearsAwardsOnceAndDoesNotRespawnOnReentry()
        {
            var root = new GameObject("Haunt encounter test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(71319);
                var room = game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                int coins = game.State.Coins;
                Assert.AreEqual(3, room.TotalWaves, "A room includes the first wave and two reinforcements");
                for (int wave = 0; wave < room.TotalWaves; wave++)
                {
                    Assert.AreEqual(wave, room.WavesCleared);
                    Assert.Greater(game.LivingEnemies, 0);
                    Assert.IsFalse(room.Cleared);
                    Assert.IsFalse(NearestExit(game, out _), "Doors remain locked during every wave");
                    Assert.AreEqual(coins, game.State.Coins, "A reinforcement must not award the room reward");
                    Assert.IsTrue(Field<IList>(game, "enemies").Cast<object>().All(enemy => Field<float>(enemy, "SpawnRemaining") > 0),
                        "Every reinforcement must use the emergence period");
                    Invoke(game, "WarnGround", new Vector2(-13, 7), 1.1f);
                    Invoke(game, "Fire", new Vector2(-13, 7), Vector2.zero, false, 1);
                    DefeatCurrentWave(game);
                    Assert.AreEqual(wave + 1, room.WavesCleared);
                    Assert.AreEqual(0, Field<IList>(game, "warnings").Count, "A completed wave clears pending ground attacks");
                    Assert.AreEqual(0, game.ActiveProjectiles, "Hostile shots cannot leak into the reinforcement or exit phase");
                }
                Assert.IsTrue(room.Cleared);
                Assert.AreEqual(0, game.LivingEnemies);
                Assert.IsTrue(NearestExit(game, out _), "The final wave unlocks the exit guide");
                Assert.Greater(game.State.Coins, coins);
                int reward = game.State.Coins;
                Invoke(game, "CompleteRoom", false);
                Assert.AreEqual(reward, game.State.Coins, "Duplicate clear callbacks cannot reward twice");
                Invoke(game, "EnterRoom", game.Floor.Entrance, Vector2.zero);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Assert.AreEqual(0, game.LivingEnemies);
                Assert.AreEqual(room.TotalWaves, room.WavesCleared);
                Assert.AreEqual(reward, game.State.Coins);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator EmergingCreaturesCannotMoveAttackOrTakeProjectileDamage()
        {
            var root = new GameObject("Haunt emerging creatures test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                Invoke(game, "EnterRoom", game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat), Vector2.zero);
                Field<IList>(game, "obstacles").Clear();
                for (int kind = 8; kind <= 11; kind++)
                {
                    Invoke(game, "ClearCombat");
                    Set(game, "invincible", 0f);
                    Invoke(game, "SpawnEnemy", kind, Vector2.zero);
                    var enemy = Field<IList>(game, "enemies")[0];
                    int enemyHealth = Field<int>(enemy, "Health");
                    int playerHealth = game.State.Health;
                    SetField(enemy, "Cooldown", 0f);
                    Assert.Greater(Field<float>(enemy, "SpawnRemaining"), .3f);
                    Invoke(game, "UpdateEnemies", .3f);
                    Assert.AreEqual(Vector2.zero, Field<Vector2>(enemy, "Position"), "Emerging creature moved: " + kind);
                    Assert.AreEqual(0f, Field<float>(enemy, "Cooldown"), "An emerging creature must not advance its attack: " + kind);
                    Assert.AreEqual(playerHealth, game.State.Health, "Emerging contact must not hurt: " + kind);
                    Invoke(game, "Fire", Vector2.zero, Vector2.right, true, 1);
                    Invoke(game, "UpdateShots", .01f);
                    Assert.AreEqual(enemyHealth, Field<int>(enemy, "Health"), "Emerging creatures must not be targeted: " + kind);
                    foreach (object shot in Field<IList>(game, "shots")) SetField(shot, "Life", 0f);
                    Invoke(game, "UpdateShots", .01f);
                    Invoke(game, "UpdateEnemies", Field<float>(enemy, "SpawnRemaining") + .01f);
                    Assert.AreEqual(0f, Field<float>(enemy, "SpawnRemaining"));
                    Invoke(game, "Fire", Field<Vector2>(enemy, "Position"), Vector2.right, true, 1);
                    Invoke(game, "UpdateShots", .01f);
                    Assert.AreEqual(enemyHealth - 1, Field<int>(enemy, "Health"), "Fully emerged creatures become vulnerable: " + kind);
                }
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator SentinelRoomAddsAMiniBossWithoutReplacingTheFloorBoss()
        {
            var root = new GameObject("Haunt sentinel room test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                var room = game.Floor.Rooms.First(r => r.HasMiniBoss);
                Assert.AreEqual(RoomKind.Combat, room.Kind);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                bool sawSentinel = false;
                for (int wave = 0; wave < room.TotalWaves; wave++)
                {
                    var enemies = Field<IList>(game, "enemies").Cast<object>().ToArray();
                    Assert.IsFalse(enemies.Any(enemy => Field<int>(enemy, "Kind") == 4), "Only the main boss room spawns a floor boss");
                    bool hasSentinel = enemies.Any(enemy => Field<int>(enemy, "Kind") == 11);
                    Assert.AreEqual(wave == room.TotalWaves - 1, hasSentinel, "The sentinel joins the final reinforcement");
                    if (hasSentinel)
                    {
                        sawSentinel = true;
                        Assert.IsTrue(enemies.Any(enemy => Field<int>(enemy, "Kind") != 11), "The sentinel arrives with ordinary creatures");
                    }
                    DefeatCurrentWave(game);
                }
                Assert.IsTrue(sawSentinel, "A marked room must actually spawn its miniboss");
                Assert.IsTrue(room.Cleared);
                Assert.IsFalse(game.State.BossDefeated, "A miniboss cannot unlock the floor portal or shop");
                Invoke(game, "EnterRoom", game.Floor.Boss, Vector2.zero);
                var boss = Field<IList>(game, "enemies");
                Assert.AreEqual(1, boss.Count);
                Assert.AreEqual(4, Field<int>(boss[0], "Kind"));
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator DodgeCrossesGeneratedPitsButLandingInsideFallsBackToSafeGround()
        {
            var root = new GameObject("Haunt pit traversal test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                StartFloor(game, 42, 2);
                var room = game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat && r.Id % 3 == 0);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                var pits = Field<IList>(game, "pits");
                Assert.Greater(pits.Count, 0, "The test must use a generated pit room");
                Rect pit = (Rect)pits[0];
                Assert.IsTrue((bool)Invoke(game, "IsPit", pit.center));
                int health = game.State.Health;
                Set(game, "playerPosition", new Vector2(pit.xMin - .35f, pit.center.y));
                Invoke(game, "ResolvePitFall");
                Set(game, "rollRemaining", .24f);
                Set(game, "rollDirection", Vector2.right);
                Invoke(game, "UpdatePlayer", null, .1f);
                Assert.IsTrue((bool)Invoke(game, "IsPit", game.PlayerPosition), "The roll must actually pass over the hole");
                Assert.AreEqual(health, game.State.Health, "The airborne part of a dodge crosses the pit safely");
                Invoke(game, "UpdatePlayer", null, .15f);
                Assert.Greater(game.PlayerPosition.x, pit.xMax);
                Assert.AreEqual(health, game.State.Health, "Reaching the other bank avoids fall damage");
                Vector2 safe = Field<Vector2>(game, "safeGround");
                Set(game, "playerPosition", pit.center);
                Set(game, "rollRemaining", 0f);
                Set(game, "invincible", 0f);
                Invoke(game, "ResolvePitFall");
                Assert.AreEqual(health - 1, game.State.Health);
                Assert.AreEqual(safe, game.PlayerPosition, "A failed crossing returns to the last safe bank");
                Assert.IsFalse((bool)Invoke(game, "IsPit", game.PlayerPosition));
                Assert.Greater(Field<float>(game, "invincible"), 0, "The recovery must protect against immediate contact damage");
                Invoke(game, "HurtPlayer");
                Assert.AreEqual(health - 1, game.State.Health);
                Invoke(game, "EnterRoom", game.Floor.Entrance, Vector2.zero);
                Assert.AreEqual(0, pits.Count, "Room transitions must remove the previous pit collision areas");
                Assert.IsFalse((bool)Invoke(game, "IsPit", pit.center));
                Assert.AreEqual(Vector2.zero, Field<Vector2>(game, "safeGround"));
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Assert.Greater(pits.Count, 0);
                Invoke(game, "OpenShop");
                Assert.AreEqual(0, pits.Count, "The walkable market cannot retain combat pits");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator SelectedHeroesEquipTheirOwnWeaponAndAttackPattern()
        {
            var root = new GameObject("Haunt signature weapons test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                WeaponKind[] expected = { WeaponKind.HunterCarbine, WeaponKind.VeilWand, WeaponKind.BoneCaster, WeaponKind.ArcCoil };
                int[] projectiles = { 1, 3, 1, 0 };
                for (int hero = 0; hero < expected.Length; hero++)
                {
                    Set(game, "selectedCharacter", (CharacterKind)hero);
                    game.BeginRun(42);
                    Assert.AreEqual(expected[hero], game.State.StartingWeapon);
                    Assert.AreEqual(expected[hero], game.State.Weapon);
                    Assert.IsTrue(game.State.Owns(expected[hero]));
                    Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                    Assert.AreEqual(projectiles[hero], game.ActiveProjectiles);
                    foreach (object shot in Field<IList>(game, "shots"))
                        Assert.AreEqual(expected[hero], Field<WeaponKind>(shot, "Style"));
                    var beam = Field<SpriteRenderer>(game, "beamVisual");
                    if (hero == 3) Assert.IsTrue(beam != null && beam.gameObject.activeSelf, "The Alchemist holds a continuous electric ray");
                    else Assert.IsTrue(beam == null || !beam.gameObject.activeSelf);
                }
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator HeldRayUsesDamageTicksStopsAtCoverAndCancelsOnReleaseDodgeOrTransition()
        {
            var root = new GameObject("Haunt continuous ray test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                Set(game, "selectedCharacter", CharacterKind.Alchemist);
                game.BeginRun(42);
                Field<IList>(game, "obstacles").Clear();
                Invoke(game, "SpawnEnemy", 0, new Vector2(3, 0));
                FinishEmergence(game);
                var enemy = Field<IList>(game, "enemies")[0];
                SetField(enemy, "Health", 30);
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                var beam = Field<SpriteRenderer>(game, "beamVisual");
                Assert.IsTrue(beam.gameObject.activeSelf);
                Assert.AreEqual(0, game.ActiveProjectiles, "A held ray must not create a projectile every frame");
                Assert.AreEqual(30 - game.State.Damage, Field<int>(enemy, "Health"));
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.AreEqual(30 - game.State.Damage, Field<int>(enemy, "Health"), "Frames inside the weapon cooldown cannot deal extra damage");
                Set(game, "shotCooldown", 0f);
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                int health = Field<int>(enemy, "Health");
                Assert.AreEqual(30 - game.State.Damage * 2, health);
                Field<IList>(game, "obstacles").Add(new Rect(1.5f, -.5f, .3f, 1));
                Set(game, "shotCooldown", 0f);
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.AreEqual(health, Field<int>(enemy, "Health"), "The continuous ray cannot damage through cover");
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, false, .016f);
                Assert.IsFalse(beam.gameObject.activeSelf, "Releasing fire stops the visible ray immediately");
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.IsTrue(beam.gameObject.activeSelf);
                Set(game, "rollRemaining", .2f);
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.IsFalse(beam.gameObject.activeSelf, "Dodging interrupts a held ray");
                Set(game, "rollRemaining", 0f);
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.IsTrue(beam.gameObject.activeSelf);
                Invoke(game, "EnterRoom", game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat), Vector2.zero);
                Assert.IsFalse(beam.gameObject.activeSelf, "A room transition cannot retain an attack from the previous arena");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator MirrorDiscReflectsFromCoverAndRoomCornersThenExhaustsItsBounces()
        {
            var root = new GameObject("Haunt ricochet geometry test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                game.State.BossDefeated = true;
                game.State.Coins = 1000;
                Assert.IsTrue(game.State.TryBuy(ShopItem.MirrorDisc, out _));
                var obstacles = Field<IList>(game, "obstacles");
                obstacles.Clear();
                obstacles.Add(new Rect(2, -.5f, .3f, 1));
                Invoke(game, "FireWeapon", Vector2.right);
                var shots = Field<IList>(game, "shots");
                Assert.AreEqual(1, shots.Count);
                var disc = shots[0];
                Assert.AreEqual(2, Field<int>(disc, "Bounces"));
                Invoke(game, "UpdateShots", .12f);
                Assert.AreEqual(1, shots.Count);
                Assert.AreEqual(1, Field<int>(disc, "Bounces"));
                Assert.Less(Field<Vector2>(disc, "Velocity").x, 0, "Cover must reflect the incoming horizontal velocity");
                Assert.Less(Field<Vector2>(disc, "Position").x, 2, "A ricochet stays on the free side of cover");
                obstacles.Clear();
                SetField(disc, "Position", new Vector2(13.9f, 7.9f));
                SetField(disc, "Velocity", new Vector2(15, 15));
                Invoke(game, "UpdateShots", .05f);
                Assert.AreEqual(1, shots.Count);
                Assert.AreEqual(0, Field<int>(disc, "Bounces"));
                Vector2 reflected = Field<Vector2>(disc, "Velocity");
                Assert.Less(reflected.x, 0, "An exact room corner reflects both axes");
                Assert.Less(reflected.y, 0);
                SetField(disc, "Position", new Vector2(14.2f, 0));
                SetField(disc, "Velocity", Vector2.right * 15);
                Invoke(game, "UpdateShots", .03f);
                Assert.AreEqual(0, shots.Count, "The next impact consumes a disc with no bounces remaining");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator DodgeInvulnerabilityBlocksDamageAndNormalContactHurts()
        {
            var root = new GameObject("Haunt dodge test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(71319);
                int health = game.State.Health;
                Set(game, "rollRemaining", .2f);
                Invoke(game, "HurtPlayer");
                Assert.AreEqual(health, game.State.Health);
                Set(game, "rollRemaining", 0f);
                Set(game, "invincible", 0f);
                Invoke(game, "HurtPlayer");
                Assert.AreEqual(health - 1, game.State.Health);
                Invoke(game, "HurtPlayer");
                Assert.AreEqual(health - 1, game.State.Health, "Post-hit grace should block repeated immediate contact.");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ExpansionBuildsAllFloorThemesAndDistinctPowers()
        {
            var root = new GameObject("Haunt expansion test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                for (int floor = 1; floor <= 5; floor++)
                {
                    StartFloor(game, 42, floor);
                    Invoke(game, "EnterRoom", game.Floor.Boss, Vector2.zero);
                    Assert.AreEqual(1, game.LivingEnemies);
                    FinishEmergence(game);
                    Invoke(game, "UpdateEnemies", 3f);
                    Assert.Greater(game.ActiveProjectiles, 0, "Every boss must attack on floor " + floor);
                }
                game.BeginRun(42);
                var combat = game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat);
                Invoke(game, "EnterRoom", combat, Vector2.zero);
                Invoke(game, "ClearCombat");
                Invoke(game, "Fire", Vector2.right, Vector2.left, false, 1);
                Invoke(game, "UsePower");
                Invoke(game, "UpdateShots", .01f);
                Assert.AreEqual(0, game.ActiveProjectiles, "Hunter dispels nearby hostile projectiles");
                Invoke(game, "HurtPlayer");
                Assert.AreEqual(game.State.MaxHealth, game.State.Health, "Hunter grants temporary protection");
                Set(game, "selectedCharacter", CharacterKind.Medium);
                game.BeginRun(42);
                Invoke(game, "UsePower");
                Assert.AreEqual(9, game.ActiveProjectiles, "Medium fires nine spectral shots");
                Invoke(game, "UsePower");
                Assert.AreEqual(9, game.ActiveProjectiles, "Power cooldown blocks duplicate burst");
                Invoke(game, "ClearCombat");
                for (int kind = 5; kind <= 7; kind++) Invoke(game, "SpawnEnemy", kind, new Vector2(kind - 6, 2));
                Invoke(game, "UpdateEnemies", .01f);
                Assert.AreEqual(3, game.LivingEnemies, "All new creatures instantiate and simulate");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator LargerArenaBoundsDoorsAndArrivalsShareTheSameGeometry()
        {
            var root = new GameObject("Haunt expanded arena test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                Vector2 corner = (Vector2)Invoke(game, "Move", Vector2.zero, new Vector2(100, 100), .27f);
                Assert.AreEqual(14.5f - .27f, corner.x, .001f);
                Assert.AreEqual(8.5f - .27f, corner.y, .001f);
                Vector2 opposite = (Vector2)Invoke(game, "Move", Vector2.zero, new Vector2(-100, -100), .27f);
                Assert.AreEqual(-corner.x, opposite.x, .001f);
                Assert.AreEqual(-corner.y, opposite.y, .001f);
                var doors = (IList)typeof(HauntGame).GetField("doors", PrivateInstance).GetValue(game);
                Assert.AreEqual(game.CurrentRoom.Neighbors.Count, doors.Count);
                var entrance = game.CurrentRoom;
                for (int i = 0; i < doors.Count; i++)
                {
                    var target = game.Floor.GetRoom(entrance.Neighbors[i]);
                    Vector2 direction = new Vector2(target.Position.X - entrance.Position.X, target.Position.Y - entrance.Position.Y);
                    Vector2 position = ((SpriteRenderer)doors[i]).transform.position;
                    Assert.AreEqual(direction.x * (14.5f - .05f), position.x, .04f);
                    Assert.AreEqual(direction.y * (8.5f - .05f), position.y, .04f);
                }
                var next = game.Floor.GetRoom(entrance.Neighbors[0]);
                Vector2 exitDirection = new Vector2(next.Position.X - entrance.Position.X, next.Position.Y - entrance.Position.Y);
                Set(game, "playerPosition", new Vector2(exitDirection.x * 14.1f, exitDirection.y * 8.1f));
                Invoke(game, "CheckDoors");
                Assert.AreSame(next, game.CurrentRoom, "The enlarged doorway must still transition rooms");
                Vector2 arrival = game.PlayerPosition;
                Assert.AreEqual(-exitDirection.x * (14.5f - 1.4f), arrival.x, .001f);
                Assert.AreEqual(-exitDirection.y * (8.5f - 1.4f), arrival.y, .001f);
                Assert.IsFalse((bool)Invoke(game, "Blocked", arrival, .27f), "Arrival must not intersect themed cover");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator RedesignedGuardiansUseDistinctSpritesAndAlternateAttacks()
        {
            var root = new GameObject("Haunt guardian identity test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                string[] ids = { "boss", "widow", "bishop", "matriarch", "bell" };
                for (int archetype = 0; archetype < ids.Length; archetype++)
                {
                    int number = Enumerable.Range(1, 5).First(n => DungeonFloor.BossCandidatesForTheme(n - 1).Contains(archetype));
                    int seed = Enumerable.Range(0, 2000).First(n => DungeonFloor.Generate(n, number).BossArchetype == archetype);
                    StartFloor(game, seed, number);
                    Invoke(game, "EnterRoom", game.Floor.Boss, Vector2.zero);
                    Assert.AreEqual(archetype, game.Floor.BossArchetype);
                    var enemies = (IList)typeof(HauntGame).GetField("enemies", PrivateInstance).GetValue(game);
                    var boss = enemies[0];
                    var type = boss.GetType();
                    var sprite = (SpriteRenderer)type.GetField("Sprite").GetValue(boss);
                    StringAssert.Contains("/ " + ids[archetype] + "/", sprite.sprite.name);
                    type.GetField("Cooldown").SetValue(boss, 0f);
                    Invoke(game, "UpdateBoss", boss, Vector2.down, 0f);
                    int firstVolley = game.ActiveProjectiles;
                    Assert.Greater(firstVolley, 0);
                    type.GetField("Cooldown").SetValue(boss, 0f);
                    Invoke(game, "UpdateBoss", boss, Vector2.down, 0f);
                    Assert.AreEqual(2, type.GetField("Attack").GetValue(boss));
                    if (archetype > 0)
                        Assert.AreNotEqual(firstVolley, game.ActiveProjectiles - firstVolley, "Guardian alternates different volleys: " + ids[archetype]);
                    if (archetype == 1)
                        Assert.Greater((float)type.GetField("DashTime").GetValue(boss), 0, "The Widow's alternate attack starts a charge");
                    if (archetype >= 2)
                    {
                        var warnings = (IList)typeof(HauntGame).GetField("warnings", PrivateInstance).GetValue(game);
                        Assert.Greater(warnings.Count, 0, "The alternate attack must mark the ground: " + ids[archetype]);
                    }
                    Assert.AreEqual(number - 1, game.Floor.ThemeIndex, "Encounter choice must not replace the floor theme");
                }
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator GroundAttacksTelegraphBeforeDamageAndCancelWhenCombatEnds()
        {
            var root = new GameObject("Haunt ground warning test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                var room = game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Invoke(game, "ClearCombat");
                var warnings = (IList)typeof(HauntGame).GetField("warnings", PrivateInstance).GetValue(game);
                int health = game.State.Health;
                Invoke(game, "WarnGround", Vector2.zero, 1.1f);
                Invoke(game, "UpdateWarnings", 1.09f);
                Assert.AreEqual(health, game.State.Health, "A marked tile must give the player time to react");
                Assert.AreEqual(1, warnings.Count);
                Invoke(game, "UpdateWarnings", .02f);
                Assert.AreEqual(health - 1, game.State.Health, "Standing in the expired telegraph causes damage");
                Assert.AreEqual(0, warnings.Count);
                Set(game, "invincible", 0f);
                health = game.State.Health;
                Invoke(game, "WarnGround", Vector2.zero, 1.1f);
                room.WavesCleared = room.TotalWaves - 1;
                Invoke(game, "CompleteRoom", false);
                Invoke(game, "UpdateWarnings", .01f);
                Assert.AreEqual(0, warnings.Count, "Purification cancels pending ground attacks");
                Assert.AreEqual(health, game.State.Health);
                Invoke(game, "WarnGround", Vector2.zero, 1.1f);
                Invoke(game, "ClearCombat");
                Assert.AreEqual(0, warnings.Count, "Leaving combat cannot retain an invisible hazard");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ReaperAndAlchemistHaveDifferentAtomicPowerEffects()
        {
            var root = new GameObject("Haunt new character powers test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                Set(game, "selectedCharacter", CharacterKind.Reaper);
                game.BeginRun(42);
                Invoke(game, "UsePower");
                Assert.AreEqual(12, game.ActiveProjectiles, "The Reaper emits a radial volley");
                Invoke(game, "HurtPlayer");
                Assert.AreEqual(game.State.MaxHealth, game.State.Health, "The Reaper has brief protection");
                Invoke(game, "UsePower");
                Assert.AreEqual(12, game.ActiveProjectiles, "Cooldown blocks a duplicate radial volley");
                Set(game, "selectedCharacter", CharacterKind.Alchemist);
                game.BeginRun(42);
                Invoke(game, "UsePower");
                Assert.AreEqual(0, game.State.PowerCooldown, "Full health does not spend the elixir");
                game.State.Hurt(3);
                int wounded = game.State.Health;
                Invoke(game, "UsePower");
                Assert.AreEqual(wounded + 2, game.State.Health, "Runtime applies exactly one core heal");
                Assert.AreEqual(18, game.State.PowerCooldown);
                Invoke(game, "UsePower");
                Assert.AreEqual(wounded + 2, game.State.Health, "Repeated input cannot heal through cooldown");
                Assert.AreEqual(0, game.ActiveProjectiles, "The healing power must not spawn an attack");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator CloseCameraFollowsThePlayerWithoutShowingTheEntireArena()
        {
            var root = new GameObject("Haunt close camera test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                var camera = (Camera)typeof(HauntGame).GetField("view", PrivateInstance).GetValue(game);
                Invoke(game, "UpdateCamera", 0f, true);
                Assert.AreEqual(6.3f, camera.orthographicSize, .001f);
                Assert.Less(camera.orthographicSize * 2, 17, "The close view must crop the full arena height");
                Vector3 origin = camera.transform.position;
                Set(game, "playerPosition", new Vector2(9, 5));
                Invoke(game, "UpdateCamera", 0f, true);
                Vector3 moved = camera.transform.position;
                Assert.Greater(moved.x, origin.x, "Camera must follow horizontal travel");
                Assert.Greater(moved.y, origin.y, "Camera must follow vertical travel");
                Vector3 screen = camera.WorldToViewportPoint(new Vector3(9, 5, 0));
                Assert.That(screen.x, Is.InRange(0f, 1f));
                Assert.That(screen.y, Is.InRange(0f, 1f));
                Set(game, "playerPosition", new Vector2(-9, -5));
                Invoke(game, "UpdateCamera", 0f, true);
                Assert.Less(camera.transform.position.x, moved.x);
                Assert.Less(camera.transform.position.y, moved.y);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ExitGuideSelectsNearestDoorAndBossPortalAfterPurification()
        {
            var root = new GameObject("Haunt exit guide test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                var room = game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Assert.IsFalse(NearestExit(game, out _), "Combat must not display an open-exit guide");
                Invoke(game, "ClearCombat");
                room.Cleared = true;
                Set(game, "playerPosition", new Vector2(7, -3));
                Assert.IsTrue(NearestExit(game, out Vector2 destination));
                var doors = ((IList)typeof(HauntGame).GetField("doors", PrivateInstance).GetValue(game)).Cast<SpriteRenderer>().ToArray();
                Assert.Greater(doors.Length, 0);
                Vector2 expected = doors.OrderBy(d => Vector2.SqrMagnitude((Vector2)d.transform.position - game.PlayerPosition)).First().transform.position;
                Assert.Less(Vector2.Distance(expected, destination), .05f, "Guide should choose the physically closest doorway");
                Invoke(game, "EnterRoom", game.Floor.Boss, Vector2.zero);
                Invoke(game, "ClearCombat");
                Invoke(game, "CompleteRoom", true);
                Set(game, "playerPosition", new Vector2(12, 0));
                Assert.IsTrue(NearestExit(game, out destination));
                Assert.Less(Vector2.Distance(new Vector2(0, 1.4f), destination), .05f, "After a boss, the market portal takes priority over a nearby door");
                Invoke(game, "OpenShop");
                Assert.IsFalse(NearestExit(game, out _), "The market must not retain the combat exit guide");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ProjectileGlowRecyclesWithItsShot()
        {
            var root = new GameObject("Haunt luminous shot test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                Invoke(game, "Fire", Vector2.zero, Vector2.right * 17, true, 1);
                var shots = (IList)typeof(HauntGame).GetField("shots", PrivateInstance).GetValue(game);
                var shot = shots[0];
                var glow = (SpriteRenderer)shot.GetType().GetField("Glow").GetValue(shot);
                Assert.IsNotNull(glow, "A shot must carry its luminous halo");
                Assert.IsTrue(glow.gameObject.activeInHierarchy);
                Assert.AreEqual(1f, glow.sharedMaterial.GetFloat("_DstBlend"), "Halo RGB uses additive blending");
                Invoke(game, "RecycleShot", 0);
                Assert.IsFalse(glow.gameObject.activeInHierarchy, "Recycling must also hide the halo");
                Invoke(game, "Fire", Vector2.one, Vector2.up * 12, true, 1);
                var next = shots[0];
                Assert.AreSame(glow, next.GetType().GetField("Glow").GetValue(next), "Repeated shots should reuse their halo");
                Assert.IsTrue(glow.gameObject.activeInHierarchy);
                Invoke(game, "ClearCombat");
                Assert.IsFalse(glow.gameObject.activeInHierarchy, "Room cleanup must leave no luminous projectile behind");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator WalkableMarketRequiresMerchantInteractionBeforeBuying()
        {
            var root = new GameObject("Haunt walkable market test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                game.State.BossDefeated = true;
                game.State.Coins = 1000;
                Invoke(game, "OpenShop");
                Assert.IsTrue(game.InShop);
                Assert.IsFalse(Field<bool>(game, "shopMenuOpen"), "Entering the market must show the room, not the catalog");
                Assert.IsFalse((bool)Invoke(game, "PointerOverInterface"), "The walkable shop must not act as a modal screen");
                int coins = game.State.Coins;
                Invoke(game, "Buy", ShopItem.Stapler);
                Assert.AreEqual(coins, game.State.Coins, "No purchase can occur with the catalog closed");
                Assert.IsFalse(game.State.Owns(WeaponKind.Stapler));
                Assert.IsFalse((bool)Invoke(game, "TryOpenMerchantAt", new Vector2(0, 3.6f)), "A distant click cannot open the catalog");
                Set(game, "playerPosition", new Vector2(0, 1.3f));
                Assert.IsTrue((bool)Invoke(game, "Blocked", new Vector2(0, 2.4f), .27f), "The counter is real cover, not a walk-through painting");
                Assert.IsFalse((bool)Invoke(game, "TryOpenMerchantAt", new Vector2(6, 0)), "Clicking a display does not open or buy the catalog");
                Assert.IsTrue((bool)Invoke(game, "TryOpenMerchantAt", new Vector2(0, 3.6f)));
                Assert.IsTrue(Field<bool>(game, "shopMenuOpen"));
                Assert.IsTrue((bool)Invoke(game, "PointerOverInterface"));
                Vector2 standing = game.PlayerPosition;
                Set(game, "rollRemaining", .2f);
                Set(game, "rollDirection", Vector2.right);
                Invoke(game, "UpdatePlayer", null, .02f);
                Assert.AreEqual(standing, game.PlayerPosition, "The catalog stops player movement");
                Invoke(game, "Buy", ShopItem.Stapler);
                Assert.IsTrue(game.State.Owns(WeaponKind.Stapler));
                Assert.AreEqual(coins - game.State.Price(ShopItem.Stapler), game.State.Coins);
                Invoke(game, "Buy", ShopItem.Stapler);
                Assert.AreEqual(coins - game.State.Price(ShopItem.Stapler), game.State.Coins, "The same weapon cannot charge twice");
                int number = game.State.Floor;
                Invoke(game, "LeaveShop");
                Assert.AreEqual(number, game.State.Floor, "The catalog cannot skip the room's exit portal");
                Invoke(game, "CloseMerchant");
                Assert.IsFalse(Field<bool>(game, "shopMenuOpen"));
                Set(game, "rollRemaining", .2f);
                Set(game, "rollDirection", Vector2.right);
                Invoke(game, "UpdatePlayer", null, .02f);
                Assert.Greater(game.PlayerPosition.x, standing.x, "Movement resumes after closing the catalog");
                Invoke(game, "FireWeapon", Vector2.right);
                Assert.AreEqual(0, game.ActiveProjectiles, "The market stays safe even while the room is walkable");
                Set(game, "playerPosition", new Vector2(12.5f, 0));
                Invoke(game, "Interact");
                Assert.IsFalse(game.InShop);
                Assert.AreEqual(number + 1, game.State.Floor);
                Assert.IsTrue(game.State.Owns(WeaponKind.Stapler), "Purchases survive the next floor");
                Assert.AreEqual(0, game.ActiveProjectiles);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator NewWeaponsProducePairedCurvedAndPiercingProjectiles()
        {
            var root = new GameObject("Haunt playful weapons test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                game.State.BossDefeated = true;
                game.State.Coins = 1000;
                foreach (var item in new[] { ShopItem.Stapler, ShopItem.Banana, ShopItem.SpinningTop })
                    Assert.IsTrue(game.State.TryBuy(item, out _));
                game.State.Equip(WeaponKind.Stapler);
                Invoke(game, "FireWeapon", Vector2.right);
                var shots = Field<IList>(game, "shots");
                Assert.AreEqual(2, shots.Count, "The stapler emits a pair of staples");
                Assert.AreEqual(WeaponKind.Stapler, Field<WeaponKind>(shots[0], "Style"));
                Assert.AreEqual(WeaponKind.Stapler, Field<WeaponKind>(shots[1], "Style"));
                Assert.Greater(Vector2.Distance(Field<Vector2>(shots[0], "Position"), Field<Vector2>(shots[1], "Position")), .2f);
                StringAssert.Contains("staple-shot", Field<SpriteRenderer>(shots[0], "Sprite").sprite.name);
                Invoke(game, "ClearCombat");
                game.State.Equip(WeaponKind.Banana);
                Invoke(game, "FireWeapon", Vector2.right);
                Assert.AreEqual(1, shots.Count);
                var banana = shots[0];
                Assert.AreEqual(game.State.Damage * 3, Field<int>(banana, "Damage"));
                Invoke(game, "UpdateShots", .1f);
                Assert.Greater(Field<Vector2>(banana, "Velocity").y, 0, "The banana's flight actually bends after launch");
                Assert.Greater(Field<Vector2>(banana, "Position").y, 0);
                Invoke(game, "ClearCombat");
                game.State.Equip(WeaponKind.SpinningTop);
                for (int x = 1; x <= 3; x++) Invoke(game, "SpawnEnemy", 0, new Vector2(x, 0));
                var enemies = Field<IList>(game, "enemies");
                FinishEmergence(game);
                object[] targets = enemies.Cast<object>().ToArray();
                int[] health = targets.Select(enemy => Field<int>(enemy, "Health")).ToArray();
                Invoke(game, "FireWeapon", Vector2.right);
                Invoke(game, "UpdateShots", .01f);
                Assert.AreEqual(health[0] - game.State.Damage * 2, Field<int>(targets[0], "Health"));
                Invoke(game, "UpdateShots", .01f);
                Assert.AreEqual(health[0] - game.State.Damage * 2, Field<int>(targets[0], "Health"), "An overlapping top must not hit the same enemy twice");
                for (int step = 0; step < 15; step++) Invoke(game, "UpdateShots", .05f);
                for (int i = 0; i < targets.Length; i++)
                    Assert.AreEqual(health[i] - game.State.Damage * 2, Field<int>(targets[i], "Health"), "The top pierces three distinct enemies");
                Assert.AreEqual(0, shots.Count, "The top is consumed after its third target");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator SpinningTopStillStopsAtCoverBeforeAnEnemy()
        {
            var root = new GameObject("Haunt top cover test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.BeginRun(42);
                game.State.BossDefeated = true;
                game.State.Coins = 1000;
                Assert.IsTrue(game.State.TryBuy(ShopItem.SpinningTop, out _));
                Invoke(game, "SpawnEnemy", 0, new Vector2(3, 0));
                var enemy = Field<IList>(game, "enemies")[0];
                FinishEmergence(game);
                int health = Field<int>(enemy, "Health");
                Field<IList>(game, "obstacles").Add(new Rect(1.5f, -.5f, .3f, 1));
                Invoke(game, "FireWeapon", Vector2.right);
                for (int step = 0; step < 10; step++) Invoke(game, "UpdateShots", .05f);
                Assert.AreEqual(0, game.ActiveProjectiles, "Piercing actors must not permit tunnelling through cover");
                Assert.AreEqual(health, Field<int>(enemy, "Health"));
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator CommandLineOpeningDuringPlayPreservesCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            LogAssert.Expect(LogType.Warning, new Regex("^Ecos: pare o Play Mode"));
            HauntProject.OpenFromCommandLine();
            yield return null;
            yield return null;
            Assert.AreEqual(scene.handle, SceneManager.GetActiveScene().handle);
        }

        [UnityTest]
        public IEnumerator OpeningRoguelikeDuringPlayPreservesCurrentScene()
        {
            var scene = SceneManager.GetActiveScene();
            Assert.IsFalse(HauntProject.CanOpen());
            LogAssert.Expect(LogType.Warning, new Regex("^Ecos: pare o Play Mode"));
            Assert.DoesNotThrow(HauntProject.Open);
            Assert.AreEqual(scene.handle, SceneManager.GetActiveScene().handle);
            yield return null;
        }
    }
}
