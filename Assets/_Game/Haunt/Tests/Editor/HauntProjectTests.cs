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
                foreach (string id in new[] { "player", "medium", "larva", "sentinel", "acolyte", "matriarch", "bell", "pumpkin", "ghost", "cultist", "bat", "boss", "merchant", "candle", "portal" })
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
                foreach (string id in new[] { "floor3", "floor4", "floor5", "door-open", "door-locked", "door-boss", "shadow", "player-ring", "enemy-ring", "glow", "rune", "rug", "weapon", "scattergun", "repeater", "cannon", "slash", "dust", "spark", "effect-ring" })
                    Assert.IsNotNull(art.Get(id), id);
            }
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

        static void Invoke(HauntGame game, string method, params object[] args)
        {
            var info = typeof(HauntGame).GetMethod(method, PrivateInstance);
            Assert.IsNotNull(info, "Runtime operation missing: " + method);
            info.Invoke(game, args);
        }

        static void Set(HauntGame game, string field, object value)
        {
            var info = typeof(HauntGame).GetField(field, PrivateInstance);
            Assert.IsNotNull(info, "Runtime field missing: " + field);
            info.SetValue(game, value);
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
                Assert.Greater(game.LivingEnemies, 0);
                Assert.IsFalse(room.Cleared);
                int coins = game.State.Coins;
                var list = (IList)typeof(HauntGame).GetField("enemies", PrivateInstance).GetValue(game);
                int count = game.LivingEnemies;
                for (int i = 0; i < count; i++)
                {
                    var enemy = list[0];
                    Vector2 position = (Vector2)enemy.GetType().GetField("Position").GetValue(enemy);
                    // A high-damage friendly shot starts inside the enemy's collision circle.
                    // This exercises actual projectile damage and clear/reward code.
                    Invoke(game, "Fire", position, Vector2.right, true, 10000);
                    Invoke(game, "UpdateShots", .01f);
                    Assert.AreEqual(count - i - 1, game.LivingEnemies);
                }
                Assert.IsTrue(room.Cleared);
                Assert.Greater(game.State.Coins, coins);
                int reward = game.State.Coins;
                Invoke(game, "EnterRoom", game.Floor.Entrance, Vector2.zero);
                Invoke(game, "EnterRoom", room, Vector2.zero);
                Assert.AreEqual(0, game.LivingEnemies);
                Assert.AreEqual(reward, game.State.Coins);
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
                    game.BeginRun(42);
                    game.State.Floor = floor;
                    Invoke(game, "EnterRoom", game.Floor.Boss, Vector2.zero);
                    Assert.AreEqual(1, game.LivingEnemies);
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
