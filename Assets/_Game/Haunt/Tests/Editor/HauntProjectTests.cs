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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
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
                foreach (string id in new[] { "player", "medium", "reaper", "alchemist", "widow", "bishop", "larva", "sentinel", "acolyte", "matriarch", "bell", "pumpkin", "ghost", "cultist", "bat", "boss", "merchant", "candle", "portal", "top-shot", "skeleton", "slime", "spider", "miniboss", "spawn-rift", "shot-disc", "lantern-wisp", "mushroom-brute", "clockwork-imp", "iron-executioner", "bog-oracle" })
                {
                    int count = art.HasCharacterSheet(id)
                        ? art.CharacterFrameCount(id, CharacterClip.Idle, CharacterFacing.Front, CharacterWeaponPose.Empty) : 4;
                    var frames = new Sprite[count];
                    for (int i = 0; i < count; i++)
                    {
                        frames[i] = art.Frame(id, i);
                        Assert.AreEqual(frames[0].rect.size, frames[i].rect.size, "Animation bounds must not jitter when atlas origins change: " + id);
                        Assert.AreEqual(frames[0].pivot, frames[i].pivot, id);
                        Assert.AreEqual(FilterMode.Point, frames[i].texture.filterMode, id);
                    }
                    Assert.AreNotSame(frames[0], frames[1], "Poses need distinct cached sprites: " + id);
                    Assert.AreSame(frames[count - 1], art.Frame(id, -1), id);
                    for (int i = 0; i < 240; i++)
                        Assert.AreSame(frames[i % count], art.Frame(id, i), "Repeated animation must not allocate more textures: " + id);
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
                        Assert.AreEqual(prefix == "decor-theme" ? 32 : 16, sprite.pixelsPerUnit, id);
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
                for (int boss = 0; boss < 7; boss++)
                {
                    var frame = art.Get("boss-bar" + boss);
                    var fill = art.Get("boss-fill" + boss);
                    Assert.AreEqual(new Rect(0, 0, 240, 24), frame.rect, "All boss frames must fit the same health window");
                    Assert.AreEqual(new Rect(0, 0, 16, 8), fill.rect);
                    Assert.AreEqual(FilterMode.Point, frame.texture.filterMode);
                    Assert.AreSame(frame, art.Get("boss-bar" + boss));
                }
                foreach (string id in new[] { "stapler", "banana-gun", "spinning-top", "staple-shot", "banana-shot", "top-shot", "shop-counter", "shop-shelf", "shop-case", "shop-board", "umbrella-gun", "hive-launcher", "boxing-glove", "shot-carbine", "shot-veil", "shot-petal", "shot-stinger", "shot-fist", "barrel-explosive", "water-urn", "shattered-prop", "wet-puddle", "giant-rounds", "character-relic", "quick-boots" })
                {
                    var sprite = art.Get(id);
                    Assert.IsNotNull(sprite, id);
                    Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, id);
                    Assert.AreSame(sprite, art.Get(id), id);
                }
            }
        }

        [Test]
        public void CombatProjectileBodiesRemainReadableBesideTheirSeparateSoftHalo()
        {
            using (var art = new HauntPixelArt())
            {
                foreach (string id in new[] { "bullet-player", "bullet-enemy", "staple-shot", "banana-shot", "top-shot",
                    "shot-carbine", "shot-veil", "shot-bone", "shot-disc", "shot-petal", "shot-stinger", "shot-fist" })
                {
                    Sprite body = art.Get(id);
                    Assert.GreaterOrEqual(body.rect.width, 12, "Combat projectiles must not regress to tiny pebble silhouettes: " + id);
                    Assert.LessOrEqual(body.rect.width, 16, "Bodies must remain compact enough for dense combat: " + id);
                    Assert.Less(body.rect.width, art.Get("shot-halo").rect.width,
                        "The light corona must stay separate from the opaque body: " + id);
                    Assert.AreEqual(16, body.pixelsPerUnit);
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
                    int expectedPpu = id == "boss" || id == "merchant" || id == "grave" ? 32 : 16;
                    Assert.AreEqual(expectedPpu, sprite.pixelsPerUnit, id);
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
                if (Field<int>(enemies[0], "Kind") == 16) SetField(enemies[0], "SpecialTimer", 1.6f);
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

        static int SeedOffering(ShopItem item)
        {
            return Enumerable.Range(0, 1000).First(seed => new RunState(seed).IsOffered(item));
        }

        // Weapon collision fixtures acquire equipment through the same limited stock
        // as normal play. Advance both the run and room plan before another visit.
        static void PurchaseInAvailableVisit(HauntGame game, ShopItem item)
        {
            for (int visit = 0; visit < 100; visit++)
            {
                game.State.BossDefeated = true;
                if (game.State.IsOffered(item))
                {
                    Assert.IsTrue(game.State.TryBuy(item, out _));
                    return;
                }
                game.State.NextFloor();
                var floor = DungeonFloor.Generate(game.State.Seed, game.State.Floor);
                Set(game, "<Floor>k__BackingField", floor);
                Invoke(game, "EnterRoom", floor.Entrance, Vector2.zero);
            }
            Assert.Fail("No available visit offered " + item);
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
                Invoke(game, "UpdateConversations", 3.01f);
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
                var room = game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat && RoomLayout.Generate(42, 2, r.Id, r.Kind).Pits.Length > 0);
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
                game.BeginRun(SeedOffering(ShopItem.MirrorDisc));
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
                Invoke(game, "UpdateConversations", 3.01f);
                    Assert.AreEqual(1, game.LivingEnemies);
                    FinishEmergence(game);
                    Invoke(game, "UpdateEnemies", 3f);
                    Invoke(game, "UpdateBulletPatterns", .35f);
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
                string[] ids = { "boss", "widow", "bishop", "matriarch", "bell", "iron-executioner", "bog-oracle" };
                for (int archetype = 0; archetype < ids.Length; archetype++)
                {
                    int number = Enumerable.Range(1, 5).First(n => DungeonFloor.BossCandidatesForTheme(n - 1).Contains(archetype));
                    int seed = Enumerable.Range(0, 2000).First(n => DungeonFloor.Generate(n, number).BossArchetype == archetype);
                    StartFloor(game, seed, number);
                    Invoke(game, "EnterRoom", game.Floor.Boss, Vector2.zero);
                Invoke(game, "UpdateConversations", 3.01f);
                    Assert.AreEqual(archetype, game.Floor.BossArchetype);
                    FinishEmergence(game);
                    var enemies = (IList)typeof(HauntGame).GetField("enemies", PrivateInstance).GetValue(game);
                    var boss = enemies[0];
                    var type = boss.GetType();
                    var sprite = (SpriteRenderer)type.GetField("Sprite").GetValue(boss);
                    Assert.AreSame(Resources.Load<Texture2D>("ReferenceSheets/bosses/" + ids[archetype]), sprite.sprite.texture,
                        "The selected guardian must render from its own authored atlas: " + ids[archetype]);
                    type.GetField("Cooldown").SetValue(boss, 0f);
                    Invoke(game, "UpdateBoss", boss, Vector2.down, 0f);
                    Assert.AreEqual(0, game.ActiveProjectiles, "Preparation must precede the first projectile");
                    Invoke(game, "UpdateBulletPatterns", .31f);
                    Assert.AreEqual(0, game.ActiveProjectiles, "The visible warning cannot fire early");
                    Invoke(game, "UpdateBulletPatterns", .02f);
                    Assert.Greater(game.ActiveProjectiles, 0, "Every guardian emits its distinctive pattern");
                    type.GetField("Cooldown").SetValue(boss, 0f);
                    Invoke(game, "UpdateBoss", boss, Vector2.down, 0f);
                    Assert.AreEqual(2, type.GetField("Attack").GetValue(boss));
                    var warnings = Field<IList>(game, "warnings");
                    Assert.Greater(warnings.Count, 0, "Alternating patterns retain a marked ground attack");
                    Invoke(game, "ClearBulletPatterns");
                    Assert.AreEqual(0, Field<IList>(game, "pendingPatterns").Count, "Sequences cancel without a delayed volley");
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
                Invoke(game, "UpdateConversations", 3.01f);
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
                Assert.IsTrue(glow.enabled, "Physical projectiles now carry a visible corona as well as energy rounds");
                var body = Field<SpriteRenderer>(shot, "Sprite");
                Assert.AreEqual(body.transform.position, glow.transform.position, "The corona follows the elevated projectile, not the floor");
                Assert.Less(glow.sortingOrder, body.sortingOrder, "The opaque pixel body must remain above its own light");
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
                game.BeginRun(SeedOffering(ShopItem.Stapler));
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
                Assert.IsTrue((bool)typeof(HauntGame).GetProperty("ConversationActive", PrivateInstance).GetValue(game));
                Assert.IsFalse(Field<bool>(game, "shopMenuOpen"), "The merchant talks before opening the catalog");
                Set(game, "conversationOpenedFrame", Time.frameCount - 1);
                Invoke(game, "RevealDialogueImmediately");
                Invoke(game, "SelectDialogueChoice", 0);
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
                    PurchaseInAvailableVisit(game, item);
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
                game.BeginRun(SeedOffering(ShopItem.SpinningTop));
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
        public IEnumerator GiantRoundsGrowFriendlyCollisionAndLightWithoutEnlargingHostileShots()
        {
            var root = new GameObject("Haunt giant-round integration test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                Invoke(game, "FireWeapon", Vector2.right);
                var shots = Field<IList>(game, "shots");
                float radius = Field<float>(shots[0], "Radius");
                float width = Field<SpriteRenderer>(shots[0], "Sprite").bounds.size.x;
                float lightWidth = Field<SpriteRenderer>(shots[0], "Glow").bounds.size.x;
                Invoke(game, "Fire", Vector2.zero, Vector2.left * 5, false, 1);
                float hostileRadius = Field<float>(shots[1], "Radius");
                Invoke(game, "ClearCombat");
                game.State.BossDefeated = true;
                game.State.Coins = 1000;
                Assert.IsTrue(game.State.TryBuy(ShopItem.GiantRounds, out _));
                Invoke(game, "FireWeapon", Vector2.right);
                var large = shots[0];
                Assert.AreEqual(radius * game.State.ProjectileScale, Field<float>(large, "Radius"), .0001f,
                    "Larger bullets need matching collision, not just a larger drawing");
                var body = Field<SpriteRenderer>(large, "Sprite");
                var light = Field<SpriteRenderer>(large, "Glow");
                Assert.AreEqual(width * game.State.ProjectileScale, body.bounds.size.x, .0001f);
                Assert.AreEqual(lightWidth * game.State.ProjectileScale, light.bounds.size.x, .0001f);
                Assert.IsTrue(light.enabled);
                Assert.AreEqual(body.transform.position, light.transform.position);
                Invoke(game, "Fire", Vector2.zero, Vector2.left * 5, false, 1);
                Assert.AreEqual(hostileRadius, Field<float>(shots[1], "Radius"), .0001f,
                    "A player relic must never enlarge hostile collision circles");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ExplosivePropsRespectCoverAndWarnBeforeTheirBlastDamagesEnemies()
        {
            var root = new GameObject("Haunt barrel collision integration test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                Invoke(game, "EnterRoom", game.Floor.Rooms.First(room => room.Kind == RoomKind.Combat), Vector2.zero);
                Invoke(game, "ClearCombat");
                var props = Field<IList>(game, "roomProps");
                int index = Enumerable.Range(0, props.Count).First(i => Field<bool>(props[i], "Explosive"));
                var barrel = props[index];
                Vector2 position = Field<Vector2>(barrel, "Position");
                int barrelHealth = Field<int>(barrel, "Health");
                var cover = new Rect(position.x - 1.2f, position.y - .7f, .3f, 1.4f);
                var obstacles = Field<IList>(game, "obstacles");
                obstacles.Add(cover);
                Invoke(game, "Fire", position + Vector2.left * 2, Vector2.right * 20, true, 100);
                Invoke(game, "UpdateShots", .2f);
                Assert.AreEqual(barrelHealth, Field<int>(barrel, "Health"), "A shot stopped by nearer cover cannot damage the barrel behind it");
                Assert.AreEqual(0, game.ActiveProjectiles);
                obstacles.Remove(cover);
                Invoke(game, "SpawnEnemy", 0, position + Vector2.down);
                FinishEmergence(game);
                var enemy = Field<IList>(game, "enemies")[0];
                SetField(enemy, "Health", 100);
                Invoke(game, "Fire", position + Vector2.left * 2, Vector2.right * 20, true, 100);
                Invoke(game, "UpdateShots", .2f);
                Assert.AreEqual(0, Field<int>(barrel, "Health"));
                Assert.IsFalse(Field<bool>(barrel, "Detonated"));
                Assert.IsNotNull(Field<SpriteRenderer>(barrel, "Warning"));
                Assert.AreEqual(100, Field<int>(enemy, "Health"), "Breaking the barrel starts a fuse rather than instant invisible damage");
                Invoke(game, "UpdateRoomProps", .3f);
                Assert.AreEqual(100, Field<int>(enemy, "Health"));
                Invoke(game, "UpdateRoomProps", .3f);
                Assert.IsTrue(Field<bool>(barrel, "Detonated"));
                Assert.Less(Field<int>(enemy, "Health"), 100, "The completed explosion damages a nearby unprotected enemy");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator BrokenUrnsCreateTemporarySlowGroundAndRemainBrokenOnRoomReentry()
        {
            var root = new GameObject("Haunt water and prop persistence test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                var room = game.Floor.Rooms.First(value => value.Kind == RoomKind.Combat);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Invoke(game, "ClearCombat");
                var props = Field<IList>(game, "roomProps");
                int index = Enumerable.Range(0, props.Count).First(i => !Field<bool>(props[i], "Explosive"));
                var urn = props[index];
                Vector2 position = Field<Vector2>(urn, "Position");
                long key = Field<long>(urn, "Key");
                var propSprite = Field<SpriteRenderer>(urn, "Sprite");
                Set(game, "playerPosition", position + Vector2.up);
                Invoke(game, "UpdatePresentation");
                Assert.Less(Field<SpriteRenderer>(game, "player").sortingOrder, propSprite.sortingOrder,
                    "The actor passes visually behind a raised prop when its feet are farther away");
                Set(game, "playerPosition", position + Vector2.down);
                Invoke(game, "UpdatePresentation");
                Assert.Greater(Field<SpriteRenderer>(game, "player").sortingOrder, propSprite.sortingOrder,
                    "The actor passes visually in front after crossing the prop's ground position");
                Set(game, "playerPosition", Vector2.zero);
                Invoke(game, "Fire", position + Vector2.left * 2, Vector2.right * 20, true, 100);
                Invoke(game, "UpdateShots", .2f);
                Assert.AreEqual(0, Field<int>(urn, "Health"));
                Assert.AreEqual(1, Field<IList>(game, "wetPatches").Count);
                Assert.Less((float)Invoke(game, "RoomMovementMultiplier", position), 1);
                Invoke(game, "SpawnEnemy", 0, position);
                Invoke(game, "SpawnEnemy", 12, position);
                FinishEmergence(game);
                var enemies = Field<IList>(game, "enemies");
                Vector2 grounded = (Vector2)Invoke(game, "MoveEnemy", enemies[0], Vector2.right, .36f);
                Vector2 floating = (Vector2)Invoke(game, "MoveEnemy", enemies[1], Vector2.right, .36f);
                Assert.Less(Vector2.Distance(position, grounded), Vector2.Distance(position, floating),
                    "Wet ground slows walking enemies while the floating lantern keeps its movement");
                Invoke(game, "UpdateRoomProps", 15f);
                Assert.AreEqual(0, Field<IList>(game, "wetPatches").Count);
                Assert.AreEqual(1f, (float)Invoke(game, "RoomMovementMultiplier", position));
                room.Cleared = true;
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Assert.IsFalse(Field<IList>(game, "roomProps").Cast<object>().Any(prop => Field<long>(prop, "Key") == key),
                    "Revisiting the room must not recreate a destroyed interactive urn");
                game.BeginRun(42);
                Invoke(game, "EnterRoom", game.Floor.GetRoom(room.Id), Vector2.zero);
                Assert.IsTrue(Field<IList>(game, "roomProps").Cast<object>().Any(prop => Field<long>(prop, "Key") == key
                    && Field<int>(prop, "Health") > 0), "A fresh run restores its own seeded props");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        static void InjectPad(HauntGame game, Gamepad pad, GamepadState state)
        {
            InputSystem.QueueStateEvent(pad, state);
            InputSystem.Update();
            Set(game, "controlFrame", -1);
            Invoke(game, "PollControlInput");
        }

        [UnityTest]
        public IEnumerator SyntheticGamepadMovesAimsFiresDodgesAndDisconnectsWithoutStuckInput()
        {
            var root = new GameObject("Haunt synthetic controller integration test");
            var previousPad = Gamepad.current;
            Gamepad pad = null;
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                pad = InputSystem.AddDevice<Gamepad>();
                pad.MakeCurrent();
                InjectPad(game, pad, new GamepadState { leftStick = new Vector2(.6f, .8f), rightStick = Vector2.right, rightTrigger = 1 });
                Invoke(game, "UpdatePlayer", null, .1f);
                Assert.Greater(game.PlayerPosition.x, 0);
                Assert.Greater(game.PlayerPosition.y, 0);
                Assert.Greater(game.ActiveProjectiles, 0, "The right trigger drives the real weapon path");
                Assert.Greater(Vector2.Dot((Vector2)Invoke(game, "AimDirection"), Vector2.right), .99f);
                InjectPad(game, pad, new GamepadState());
                Assert.IsFalse((bool)Invoke(game, "FireHeld"));
                Assert.Greater(Vector2.Dot((Vector2)Invoke(game, "AimDirection"), Vector2.right), .99f,
                    "Releasing the aim stick retains its last direction");
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.East));
                Invoke(game, "UpdatePlayer", null, .01f);
                Assert.Greater(Field<float>(game, "rollRemaining"), 0, "The east button starts the actual invulnerable dodge");
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.Select));
                Assert.IsTrue((bool)Invoke(game, "MapPressed"));
                Gamepad disconnected = pad;
                InputSystem.RemoveDevice(pad);
                pad = null;
                Assert.AreNotSame(disconnected, Gamepad.current);
                if (Gamepad.current == null)
                {
                    Set(game, "controlFrame", -1);
                    Invoke(game, "PollControlInput");
                    Assert.IsFalse(Field<bool>(game, "controllerActive"));
                    Assert.AreEqual(Vector2.zero, (Vector2)Invoke(game, "MovementInput", new object[] { null }),
                        "Disconnecting the last controller cannot leave a virtual movement stick held down");
                }
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                UnityEngine.Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator ControllerConfirmOpeningMerchantCannotAlsoSpendMoney()
        {
            var root = new GameObject("Haunt controller merchant confirmation test");
            var previousPad = Gamepad.current;
            Gamepad pad = null;
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                game.State.BossDefeated = true;
                game.State.Coins = 1000;
                Invoke(game, "OpenShop");
                Set(game, "playerPosition", new Vector2(0, 1.3f));
                Set(game, "shopWeapons", true);
                pad = InputSystem.AddDevice<Gamepad>();
                pad.MakeCurrent();
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.South));
                Assert.IsTrue((bool)Invoke(game, "InteractPressed"));
                Invoke(game, "Interact");
                Assert.IsTrue((bool)typeof(HauntGame).GetProperty("ConversationActive", PrivateInstance).GetValue(game));
                int coins = game.State.Coins;
                yield return null;
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.South));
                Invoke(game, "UpdateConversations", .05f);
                Assert.AreEqual(coins, game.State.Coins, "Opening dialogue cannot spend money");
                InjectPad(game, pad, new GamepadState());
                Invoke(game, "RevealDialogueImmediately");
                yield return null;
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.South));
                Invoke(game, "UpdateConversations", 0f);
                Assert.IsTrue(Field<bool>(game, "shopMenuOpen"));
                Assert.AreEqual(coins, game.State.Coins, "Negotiating cannot buy on the same press");
                yield return null;
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.South));
                Assert.AreEqual(coins, game.State.Coins, "Held confirm must be released before a purchase");
                InjectPad(game, pad, new GamepadState());
                yield return null;
                ShopItem offer = game.State.ShopWeapons[0];
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.South));
                Assert.AreEqual(coins - game.State.Price(offer), game.State.Coins);
                Assert.IsTrue(game.State.Sold(offer));
                yield return null;
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.East));
                Assert.IsFalse(Field<bool>(game, "shopMenuOpen"));
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                UnityEngine.Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator NewCuriousWeaponsRicochetGuideAndPushEnemiesWithoutCrossingCover()
        {
            var root = new GameObject("Haunt new weapon mechanics test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                game.State.Coins = 10000;
                foreach (ShopItem item in new[] { ShopItem.Umbrella, ShopItem.HiveLauncher, ShopItem.BoxingGlove })
                    PurchaseInAvailableVisit(game, item);
                var obstacles = Field<IList>(game, "obstacles");
                obstacles.Clear();
                obstacles.Add(new Rect(2, -.7f, .3f, 1.4f));
                game.State.Equip(WeaponKind.Umbrella);
                Invoke(game, "FireWeapon", Vector2.right);
                var shots = Field<IList>(game, "shots");
                Assert.AreEqual(5, shots.Count);
                var centralPetal = shots[2];
                Invoke(game, "UpdateShots", .15f);
                Assert.Less(Field<Vector2>(centralPetal, "Velocity").x, 0, "The umbrella's central petal actually reflects from cover");
                Assert.AreEqual(1, Field<int>(centralPetal, "Bounces"));
                Invoke(game, "ClearCombat");
                obstacles.Clear();
                game.State.Equip(WeaponKind.HiveLauncher);
                Invoke(game, "SpawnEnemy", 0, new Vector2(3, 2));
                FinishEmergence(game);
                Invoke(game, "FireWeapon", Vector2.right);
                Assert.AreEqual(3, shots.Count);
                var stinger = shots[1];
                Invoke(game, "UpdateShots", .05f);
                Vector2 guided = Field<Vector2>(stinger, "Velocity");
                Assert.Greater(guided.y, 0, "The central stinger bends toward an exposed target");
                Assert.AreEqual(16, guided.magnitude, .001f, "Guidance turns a stinger without accelerating it");
                obstacles.Add(new Rect(1.8f, -.5f, .4f, 3.5f));
                Invoke(game, "GuideStinger", stinger, .1f);
                Assert.AreEqual(guided, Field<Vector2>(stinger, "Velocity"), "Guidance cannot lock onto an enemy through cover");
                Invoke(game, "ClearCombat");
                obstacles.Clear();
                game.State.Equip(WeaponKind.BoxingGlove);
                Invoke(game, "SpawnEnemy", 0, new Vector2(2, 0));
                FinishEmergence(game);
                var target = Field<IList>(game, "enemies")[0];
                SetField(target, "Health", 100);
                obstacles.Add(new Rect(2.6f, -.6f, .4f, 1.2f));
                Invoke(game, "FireWeapon", Vector2.right);
                Invoke(game, "UpdateShots", .2f);
                Assert.AreEqual(100 - game.State.Damage * 4, Field<int>(target, "Health"));
                float pushedX = Field<Vector2>(target, "Position").x;
                Assert.Greater(pushedX, 2, "The glove's impact physically pushes an ordinary enemy");
                Assert.LessOrEqual(pushedX, 2.6f - .36f + .001f, "A punch cannot push its target through nearby cover");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator DestroyingAClusterPropCannotCreateAnotherPropWhenTheRoomIsRebuilt()
        {
            var root = new GameObject("Haunt destroyed prop layout regression");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                // Destruction must preserve the seeded candidate sequence on a revisit.
                game.BeginRun(21);
                var room = game.Floor.GetRoom(1);
                Assert.AreEqual(RoomKind.Combat, room.Kind);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Invoke(game, "ClearCombat");
                var props = Field<IList>(game, "roomProps");
                var expected = props.Cast<object>().Select(prop => Field<long>(prop, "Key")).ToList();
                Assert.Greater(expected.Count, 0, "The generated layout must include destructible props");
                long broken = Field<long>(props[0], "Key");
                Vector2 brokenPosition = Field<Vector2>(props[0], "Position");
                Invoke(game, "DamageRoomProp", 0, 100);
                expected.Remove(broken);
                room.Cleared = true;
                Invoke(game, "EnterRoom", room, Vector2.zero);
                var actual = Field<IList>(game, "roomProps").Cast<object>().Select(prop => Field<long>(prop, "Key")).ToArray();
                CollectionAssert.AreEquivalent(expected, actual,
                    "Room reconstruction must preserve all surviving props without creating a formerly blocked candidate");
                Assert.IsFalse((bool)Invoke(game, "Blocked", brokenPosition, .1f),
                    "Temporary layout reservations cannot leave an invisible collider at the destroyed prop");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator SpectralDodgeLetsBulletsPassOnlyDuringItsFirstSixtyPercent()
        {
            var root = new GameObject("Haunt spectral recovery test");
            try
            {
                var game = root.AddComponent<HauntGame>(); game.enabled = false;
                game.BeginRun(42);
                Invoke(game, "EnterRoom", game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat), Vector2.zero);
                Invoke(game, "ClearCombat");
                int health = game.State.Health;
                Set(game, "rollRemaining", .2f);
                Invoke(game, "Fire", new Vector2(-1, 0), Vector2.right * 20, false, 1);
                Invoke(game, "UpdateShots", .06f);
                Assert.AreEqual(health, game.State.Health);
                Assert.AreEqual(1, game.ActiveProjectiles, "I-frames pass through the projectile instead of consuming it");
                Set(game, "rollRemaining", .05f); Set(game, "invincible", 0f);
                Invoke(game, "UpdateShots", .01f);
                Assert.AreEqual(health - 1, game.State.Health, "Recovery is vulnerable while the animation is still rolling");
                Assert.AreEqual(0, game.ActiveProjectiles);
                yield return null; LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator OracleOrbsTelegraphAndSplitOnceAtTheirTravelDistance()
        {
            var root = new GameObject("Haunt oracle split test");
            try
            {
                var game = root.AddComponent<HauntGame>(); game.enabled = false;
                int seed = Enumerable.Range(0, 2000).First(n => DungeonFloor.Generate(n, 2).BossArchetype == 6);
                StartFloor(game, seed, 2);
                Invoke(game, "EnterRoom", game.Floor.Boss, Vector2.zero);
                Invoke(game, "UpdateConversations", 3.01f);
                FinishEmergence(game);
                var boss = Field<IList>(game, "enemies")[0]; SetField(boss, "Cooldown", 0f);
                Set(game, "playerPosition", new Vector2(10, -6));
                Invoke(game, "UpdateBoss", boss, Vector2.down, 0f);
                Assert.AreEqual(0, game.ActiveProjectiles);
                Invoke(game, "UpdateBulletPatterns", .35f);
                Assert.AreEqual(2, game.ActiveProjectiles);
                Invoke(game, "UpdateShots", 1.5f);
                Assert.AreEqual(24, game.ActiveProjectiles, "Two orbs each split into twelve projectiles");
                foreach (object shot in Field<IList>(game, "shots"))
                    Assert.AreEqual(0, Field<float>(shot, "Age"), "Children wait for the next simulation tick");
                Invoke(game, "ClearCombat");
                Invoke(game, "UpdateBulletPatterns", 10f);
                Assert.AreEqual(0, game.ActiveProjectiles, "Cancelled sequences cannot reappear after combat");
                yield return null; LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator HitFlashIsIndividualAndCasingsAfterimagesAndKickAreBounded()
        {
            var root = new GameObject("Haunt feel feedback test");
            try
            {
                var game = root.AddComponent<HauntGame>(); game.enabled = false;
                game.BeginRun(42);
                Invoke(game, "SpawnEnemy", 0, new Vector2(-1, 2));
                Invoke(game, "SpawnEnemy", 0, new Vector2(1, 2)); FinishEmergence(game);
                var enemies = Field<IList>(game, "enemies");
                Invoke(game, "DamageEnemy", 0, 1, Vector2.left, Color.white, false);
                Assert.IsTrue(Field<HauntPixelArt>(game, "art").Material.HasProperty("_HitFlash"), "The active shader must support the per-renderer flash");
                var block = new MaterialPropertyBlock();
                Field<SpriteRenderer>(enemies[0], "Sprite").GetPropertyBlock(block);
                Assert.AreEqual(1, block.GetFloat("_HitFlash"));
                Field<SpriteRenderer>(enemies[1], "Sprite").GetPropertyBlock(block);
                Assert.AreEqual(0, block.GetFloat("_HitFlash"), "One target's flash cannot change another target's material");
                Invoke(game, "UpdateFeel", .061f);
                Invoke(game, "PresentEnemyHitFeedback", enemies[0]);
                Field<SpriteRenderer>(enemies[0], "Sprite").GetPropertyBlock(block);
                Assert.AreEqual(0, block.GetFloat("_HitFlash"));
                for (int i = 0; i < 120; i++) Invoke(game, "OnPlayerWeaponFired", Vector2.right, false, false);
                Assert.Less(Field<Vector2>(game, "cameraKick").x, 0);
                Assert.LessOrEqual(Field<Vector2>(game, "cameraKick").magnitude, .3501f);
                Assert.LessOrEqual(Field<IList>(game, "casings").Count, 96);
                Invoke(game, "EmitRollAfterimage");
                Assert.AreEqual(1, Field<IList>(game, "afterimages").Count);
                Invoke(game, "UpdateFeel", .21f);
                Assert.AreEqual(0, Field<IList>(game, "afterimages").Count);
                Invoke(game, "ResetGameFeel");
                Assert.AreEqual(0, Field<IList>(game, "casings").Count);
                Assert.AreEqual(Vector2.zero, Field<Vector2>(game, "cameraKick"));
                yield return null; LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator BossIntroductionOwnsThreeSecondsAndNegotiationOwnsTheCatalog()
        {
            var root = new GameObject("Haunt cinematic and dialogue test");
            try
            {
                var game = root.AddComponent<HauntGame>(); game.enabled = false;
                game.BeginRun(42);
                Invoke(game, "EnterRoom", game.Floor.Boss, Vector2.zero);
                var intro = typeof(HauntGame).GetProperty("BossIntroActive", PrivateInstance);
                Assert.IsTrue((bool)intro.GetValue(game));
                Vector2 position = game.PlayerPosition;
                int health = game.State.Health;
                Invoke(game, "UpdateConversations", 1.5f);
                Assert.IsTrue((bool)intro.GetValue(game));
                Assert.AreEqual(position, game.PlayerPosition);
                Assert.AreEqual(health, game.State.Health);
                Assert.AreEqual(0, game.ActiveProjectiles);
                Invoke(game, "UpdateMusic", .1f);
                Assert.AreEqual(5, Field<int>(game, "musicTheme"), "An exclusive faster theme replaces the ambient track");
                Invoke(game, "UpdateConversations", 1.499f);
                Assert.IsTrue((bool)intro.GetValue(game), "No input may shorten the mandatory three seconds");
                Invoke(game, "UpdateConversations", .002f);
                Assert.IsFalse((bool)intro.GetValue(game));
                var boss = Field<IList>(game, "enemies")[0];
                Assert.AreEqual(0, Field<float>(boss, "SpawnRemaining"));
                Invoke(game, "DamageEnemy", 0, 10000, Vector2.up, Color.white, true);
                Assert.IsTrue(game.State.BossDefeated);
                Assert.AreEqual(1, Field<IList>(game, "bossRemnants").Count);
                Invoke(game, "UpdateBossPresentation", .91f);
                Assert.AreEqual(0, Field<IList>(game, "bossRemnants").Count);
                Invoke(game, "OpenShop");
                Set(game, "playerPosition", new Vector2(0, 1.3f));
                Invoke(game, "OpenMerchant");
                Assert.IsFalse(Field<bool>(game, "shopMenuOpen"));
                Set(game, "conversationOpenedFrame", Time.frameCount - 1);
                Invoke(game, "RevealDialogueImmediately");
                Invoke(game, "SelectDialogueChoice", 0);
                Assert.IsTrue(Field<bool>(game, "shopMenuOpen"));
                yield return null; LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator AcidDamagesOverTimeAndPillarsRemoveTheirActualCoverWhenBroken()
        {
            var root = new GameObject("Haunt acid and cover test");
            try
            {
                var game = root.AddComponent<HauntGame>(); game.enabled = false;
                game.BeginRun(42);
                Invoke(game, "EnterRoom", game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat), Vector2.zero);
                Invoke(game, "ClearCombat");
                var props = Field<IList>(game, "roomProps");
                int acidIndex = Enumerable.Range(0, props.Count).First(i => Field<bool>(props[i], "Acid"));
                Vector2 point = Field<Vector2>(props[acidIndex], "Position");
                Invoke(game, "DamageRoomProp", acidIndex, 100);
                Assert.AreEqual(1, Field<IList>(game, "damageZones").Count);
                Set(game, "playerPosition", point); Set(game, "invincible", 0f);
                int health = game.State.Health;
                Invoke(game, "UpdateDamageZones", .74f);
                Assert.AreEqual(health, game.State.Health, "The glowing spill gives time to move");
                Invoke(game, "UpdateDamageZones", .02f);
                Assert.AreEqual(health - 1, game.State.Health);
                int pillarIndex = Enumerable.Range(0, props.Count).First(i => Field<bool>(props[i], "Pillar"));
                Rect cover = Field<Rect>(props[pillarIndex], "Bounds");
                Invoke(game, "DamageRoomProp", pillarIndex, 100);
                Assert.IsFalse(Field<IList>(game, "obstacles").Contains(cover));
                Assert.IsFalse((bool)Invoke(game, "Blocked", cover.center, .1f));
                yield return null; LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator GargoyleStoneIgnoresDamageAndTheShadowCrossesRealRoomWalls()
        {
            var root = new GameObject("Haunt new Halloween mechanics test");
            try
            {
                var game = root.AddComponent<HauntGame>(); game.enabled = false;
                game.BeginRun(42);
                Invoke(game, "EnterRoom", game.Floor.Rooms.First(r => r.Kind == RoomKind.Combat), Vector2.zero);
                Invoke(game, "ClearCombat");
                Invoke(game, "SpawnEnemy", 16, new Vector2(3, 0)); FinishEmergence(game);
                var gargoyle = Field<IList>(game, "enemies")[0];
                int health = Field<int>(gargoyle, "Health");
                Invoke(game, "DamageEnemy", 0, 1, Vector2.left, Color.white, false);
                Assert.AreEqual(health, Field<int>(gargoyle, "Health"));
                SetField(gargoyle, "SpecialTimer", 1.6f);
                Invoke(game, "DamageEnemy", 0, 1, Vector2.left, Color.white, false);
                Assert.AreEqual(health - 1, Field<int>(gargoyle, "Health"));
                Invoke(game, "SpawnEnemy", 17, Vector2.zero); FinishEmergence(game);
                var shadow = Field<IList>(game, "enemies")[1];
                SetField(shadow, "Position", Vector2.zero);
                Field<IList>(game, "obstacles").Add(new Rect(.5f, -1, .5f, 2));
                Vector2 next = (Vector2)Invoke(game, "MoveEnemy", shadow, Vector2.right * 2, .3f);
                Assert.Greater(next.x, 1, "The spirit's traversal intentionally ignores a physical wall");
                Invoke(game, "SpawnEnemy", 15, new Vector2(-10, 0)); FinishEmergence(game);
                var scarecrow = Field<IList>(game, "enemies")[2];
                SetField(scarecrow, "Position", new Vector2(-10, 0));
                Invoke(game, "UpdateHalloweenEnemy", scarecrow, Vector2.right, 10f, .1f);
                Invoke(game, "PresentEnemy", scarecrow, Vector2.right);
                Assert.IsFalse(Field<bool>(scarecrow, "Awakened"));
                Assert.IsFalse(Field<SpriteRenderer>(scarecrow, "Ring").enabled, "A disguised scarecrow must not advertise an enemy marker");
                Invoke(game, "UpdateHalloweenEnemy", scarecrow, Vector2.right, 2f, .1f);
                Assert.IsTrue(Field<bool>(scarecrow, "Awakened"));
                yield return null; LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator InfiniteStarterReserveStillNeedsAManualReloadAndCannotFireDuringIt()
        {
            var root = new GameObject("Haunt finite starter magazine test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                WeaponAmmoStats stats = WeaponAmmo.Stats(game.State.StartingWeapon);
                Assert.AreEqual(stats.MagazineSize, game.State.Magazine);
                Assert.AreEqual(-1, game.State.ReserveAmmo, "Infinite reserve does not mean an infinite loaded magazine");
                for (int round = 0; round < stats.MagazineSize; round++)
                    Assert.IsTrue((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(0, game.State.Magazine);
                int projectiles = game.ActiveProjectiles;
                Assert.IsFalse((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(projectiles, game.ActiveProjectiles, "An empty trigger cannot create a free projectile");
                Invoke(game, "TickAmmunition", stats.ReloadDuration + 1);
                Assert.AreEqual(0, game.State.Magazine, "Waiting alone cannot bypass manual reload");
                Assert.IsFalse(game.State.IsReloading);
                Assert.IsTrue(game.State.TryReload());
                Invoke(game, "TickAmmunition", stats.ReloadDuration - .01f);
                Assert.IsTrue(game.State.IsReloading);
                Assert.IsFalse((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(0, game.State.Magazine);
                Assert.AreEqual(projectiles, game.ActiveProjectiles, "Reloading cannot emit or charge an interrupted shot");
                Invoke(game, "TickAmmunition", .02f);
                Assert.IsFalse(game.State.IsReloading);
                Assert.AreEqual(stats.MagazineSize, game.State.Magazine);
                Assert.AreEqual(-1, game.State.ReserveAmmo);
                Assert.IsTrue((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(stats.MagazineSize - 1, game.State.Magazine);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ShotgunSpendsOneRoundPerFanAndRejectsAnIncompleteVolleyWithoutCharging()
        {
            var root = new GameObject("Haunt atomic volley ammunition test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(SeedOffering(ShopItem.Scattergun));
                game.State.BossDefeated = true;
                game.State.Coins = 1000;
                Assert.IsTrue(game.State.TryBuy(ShopItem.Scattergun, out _));
                int loaded = game.State.Magazine, reserve = game.State.ReserveAmmo;
                Assert.IsTrue((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(5, game.ActiveProjectiles, "A shotgun round produces its whole five-pellet fan");
                Assert.AreEqual(loaded - 1, game.State.Magazine, "Pellets share one ammunition transaction");
                Assert.AreEqual(reserve, game.State.ReserveAmmo, "Firing spends loaded ammunition, not reserve ammunition");
                Invoke(game, "ClearCombat");
                int limit = (int)typeof(HauntGame).GetField("MaxShots", BindingFlags.Static | BindingFlags.NonPublic).GetRawConstantValue();
                for (int shot = 0; shot < limit - 4; shot++)
                    Invoke(game, "Fire", Vector2.zero, Vector2.right, true, 1);
                Assert.AreEqual(limit - 4, game.ActiveProjectiles);
                Assert.IsFalse((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(limit - 4, game.ActiveProjectiles, "Four free slots must not create a partial five-pellet fan");
                Assert.AreEqual(loaded - 1, game.State.Magazine, "A capacity refusal cannot charge ammunition");
                Invoke(game, "ClearCombat");
                Assert.AreEqual(loaded - 1, game.State.Magazine, "Clearing a room cannot refill the magazine");
                Assert.IsTrue((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(5, game.ActiveProjectiles);
                Assert.AreEqual(loaded - 2, game.State.Magazine);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator HeldRayChargesOnlyDamageTicksAndItsLastRoundStillDealsDamage()
        {
            var root = new GameObject("Haunt ray ammunition transaction test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                Set(game, "selectedCharacter", CharacterKind.Alchemist);
                game.BeginRun(42);
                Invoke(game, "SpawnEnemy", 0, new Vector2(3, 0));
                FinishEmergence(game);
                var enemy = Field<IList>(game, "enemies")[0];
                SetField(enemy, "Health", 1000);
                int loaded = game.State.Magazine;
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.AreEqual(loaded - 1, game.State.Magazine, "A ray damage tick is charged exactly once");
                Assert.AreEqual(1000 - game.State.Damage, Field<int>(enemy, "Health"));
                for (int frame = 0; frame < 10; frame++)
                    Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.IsTrue((bool)Invoke(game, "UpdateBeam", Vector2.right, false));
                Assert.AreEqual(loaded - 1, game.State.Magazine, "Following the aim during cooldown is visual only");
                Assert.AreEqual(1000 - game.State.Damage, Field<int>(enemy, "Health"));
                while (game.State.Magazine > 1) Assert.IsTrue(game.State.TryConsumeShot());
                int health = Field<int>(enemy, "Health");
                Set(game, "shotCooldown", 0f);
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                var beam = Field<SpriteRenderer>(game, "beamVisual");
                Assert.AreEqual(0, game.State.Magazine);
                Assert.AreEqual(health - game.State.Damage, Field<int>(enemy, "Health"), "The accepted last round must finish its damage tick");
                Assert.IsTrue(beam.gameObject.activeSelf, "The last charged pulse remains visible during its accepted tick");
                health = Field<int>(enemy, "Health");
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.IsFalse(beam.gameObject.activeSelf, "The next empty frame stops the ray");
                Assert.AreEqual(health, Field<int>(enemy, "Health"));
                Assert.IsTrue(game.State.TryReload());
                Set(game, "shotCooldown", 0f);
                Invoke(game, "UpdateWeaponTrigger", Vector2.right, true, .016f);
                Assert.AreEqual(health, Field<int>(enemy, "Health"), "Reloading cannot leak a ray damage tick");
                Assert.IsFalse(beam.gameObject.activeSelf);
                Invoke(game, "TickAmmunition", WeaponAmmo.Stats(game.State.Weapon).ReloadDuration + .01f);
                loaded = game.State.Magazine;
                Assert.IsTrue((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(loaded - 1, game.State.Magazine, "The direct weapon entry delegates to the ray without a second charge");
                Assert.AreEqual(health - game.State.Damage, Field<int>(enemy, "Health"));
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator KeyboardRAndControllerNorthReloadWithoutRestartingOrResettingShotCadence()
        {
            var root = new GameObject("Haunt reload input integration test");
            var previousPad = Gamepad.current;
            var previousKeyboard = Keyboard.current;
            Gamepad pad = null;
            Keyboard keyboard = null;
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                Assert.IsTrue((bool)Invoke(game, "FireWeapon", Vector2.right));
                int loaded = game.State.Magazine;
                Set(game, "shotCooldown", .4f);
                pad = InputSystem.AddDevice<Gamepad>();
                pad.MakeCurrent();
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.North));
                Invoke(game, "HandleReloadInput");
                Assert.IsTrue(game.State.IsReloading, "The north button maps to Triangle on a recognized DualShock");
                Assert.AreEqual(.4f, Field<float>(game, "shotCooldown"), "Manual reload cannot reset the attack cooldown");
                Invoke(game, "TickAmmunition", .2f);
                float remaining = game.State.ReloadRemaining;
                InjectPad(game, pad, new GamepadState());
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.North));
                Invoke(game, "HandleReloadInput");
                Assert.AreEqual(remaining, game.State.ReloadRemaining, .0001f, "Repeated presses cannot restart an accepted reload");
                Assert.IsFalse((bool)Invoke(game, "FireWeapon", Vector2.right));
                Assert.AreEqual(loaded, game.State.Magazine);
                Invoke(game, "TickAmmunition", WeaponAmmo.Stats(game.State.Weapon).ReloadDuration);
                Assert.IsTrue((bool)Invoke(game, "FireWeapon", Vector2.right));
                InjectPad(game, pad, new GamepadState());
                keyboard = InputSystem.AddDevice<Keyboard>();
                keyboard.MakeCurrent();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                InputSystem.Update();
                Set(game, "controlFrame", -1);
                Invoke(game, "PollControlInput");
                Invoke(game, "HandleReloadInput");
                Assert.IsTrue(game.State.IsReloading, "R uses the same real reload input path");
                Assert.AreEqual(.4f, Field<float>(game, "shotCooldown"));
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
                if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                UnityEngine.Object.Destroy(root);
            }
        }

        [UnityTest]
        public IEnumerator CharacterPowerVolleysDoNotSpendWeaponAmmunition()
        {
            var root = new GameObject("Haunt power ammunition independence test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                foreach (CharacterKind hero in new[] { CharacterKind.Medium, CharacterKind.Reaper })
                {
                    Set(game, "selectedCharacter", hero);
                    game.BeginRun(42);
                    int loaded = game.State.Magazine, reserve = game.State.ReserveAmmo;
                    Invoke(game, "UsePower");
                    Assert.AreEqual(hero == CharacterKind.Medium ? 9 : 12, game.ActiveProjectiles);
                    Assert.Greater(game.State.PowerCooldown, 0, "The power was accepted rather than silently rejected");
                    Assert.AreEqual(loaded, game.State.Magazine, "Power projectiles must not share the normal weapon debit");
                    Assert.AreEqual(reserve, game.State.ReserveAmmo);
                }
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator SeededAmmoDropsComeFromRealKillsPersistUntilCollectedAndCannotBeCollectedTwice()
        {
            var root = new GameObject("Haunt ammunition drop persistence test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                game.State.BossDefeated = true;
                game.State.Coins = 1000;
                Assert.IsTrue(game.State.TryBuy(game.State.ShopWeapons[0], out _));
                Assert.GreaterOrEqual(game.State.ReserveAmmo, 0, "Loot is relevant only after acquiring a weapon with finite reserve");
                DungeonRoom room = game.Floor.GetRoom(1);
                Assert.AreEqual(RoomKind.Combat, room.Kind);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Invoke(game, "ClearCombat");
                Invoke(game, "SpawnEnemy", 0, new Vector2(-3, 0));
                // Keep one enemy alive so the four deaths stay in the same wave.
                for (int ordinal = 1; ordinal <= 4; ordinal++)
                {
                    Invoke(game, "SpawnEnemy", 0, new Vector2(3, 0));
                    FinishEmergence(game);
                    Invoke(game, "DamageEnemy", 1, 1000, Vector2.left, Color.white, false);
                    Assert.AreEqual(ordinal, Field<int>(game, "enemiesDefeated"));
                    Assert.AreEqual(ordinal < 4 ? 0 : 1, Field<IList>(game, "ammoPickups").Count,
                        "Seed 42, floor 1, room 1 has no drop on kills 1-3 and a drop on kill 4");
                }
                var pickups = Field<IList>(game, "ammoPickups");
                object record = Field<object>(pickups[0], "Record");
                Vector2 position = Field<Vector2>(record, "Position");
                Assert.IsFalse(Field<bool>(record, "Collected"));
                room.Cleared = true;
                Invoke(game, "EnterRoom", game.Floor.Entrance, Vector2.zero);
                Assert.AreEqual(0, pickups.Count, "A different room cannot show the previous room's pickup");
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Assert.AreEqual(1, pickups.Count, "An uncollected drop returns when its room is revisited");
                Assert.AreSame(record, Field<object>(pickups[0], "Record"));
                Assert.AreEqual(position, Field<Vector2>(record, "Position"));
                Assert.IsTrue(game.State.TryConsumeShot());
                int loaded = game.State.Magazine, reserve = game.State.ReserveAmmo;
                Set(game, "playerPosition", position);
                Invoke(game, "UpdateAmmoPickups");
                Assert.AreEqual(0, pickups.Count);
                Assert.IsTrue(Field<bool>(record, "Collected"));
                Assert.AreEqual(reserve + WeaponAmmo.Stats(game.State.Weapon).AmmoPickupAmount, game.State.ReserveAmmo);
                Assert.AreEqual(loaded, game.State.Magazine, "Picking up reserve ammunition cannot skip a magazine reload");
                reserve = game.State.ReserveAmmo;
                Invoke(game, "EnterRoom", game.Floor.Entrance, Vector2.zero);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Assert.AreEqual(0, pickups.Count, "Collected drops must remain absent on another revisit");
                Assert.AreEqual(reserve, game.State.ReserveAmmo, "Room transitions cannot duplicate collected ammunition");
                game.BeginRun(42);
                Assert.AreEqual(0, Field<IDictionary>(game, "ammoDropsByRoom").Count, "A fresh run clears persistent room drop records");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }

        [UnityTest]
        public IEnumerator ControllerWeaponCycleCanReachTheInfiniteReserveBackupAfterTheStarterRunsDry()
        {
            var root = new GameObject("Haunt backup revolver accessibility test");
            var previousPad = Gamepad.current;
            Gamepad pad = null;
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                while (game.State.Magazine > 0) Assert.IsTrue(game.State.TryConsumeShot());
                Assert.IsTrue(game.State.TryReload());
                pad = InputSystem.AddDevice<Gamepad>();
                pad.MakeCurrent();
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.RightShoulder));
                Invoke(game, "UpdateWeaponSelection", new object[] { null });
                Assert.AreEqual(WeaponKind.Pistol, game.State.Weapon, "Cycling must expose the owned backup rather than repeatedly selecting the starter");
                Assert.AreEqual(-1, game.State.ReserveAmmo);
                Assert.IsFalse(game.State.IsReloading, "Selecting another weapon cancels the old reload without completing it");
                Assert.IsTrue((bool)Invoke(game, "FireWeapon", Vector2.right));
                InjectPad(game, pad, new GamepadState());
                InjectPad(game, pad, new GamepadState().WithButton(GamepadButton.RightShoulder));
                Invoke(game, "UpdateWeaponSelection", new object[] { null });
                Assert.AreEqual(game.State.StartingWeapon, game.State.Weapon);
                Assert.AreEqual(0, game.State.Magazine, "Switching away and back cannot refill the empty starter");
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
                if (previousPad != null && previousPad.added) previousPad.MakeCurrent();
                UnityEngine.Object.Destroy(root);
            }
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
