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
                foreach (string id in new[] { "player", "medium", "reaper", "alchemist", "widow", "bishop", "larva", "sentinel", "acolyte", "matriarch", "bell", "pumpkin", "ghost", "cultist", "bat", "boss", "merchant", "candle", "portal" })
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
                foreach (string id in new[] { "chandelier", "exit-arrow", "floor3", "floor4", "floor5", "door-open", "door-locked", "door-boss", "shadow", "player-ring", "enemy-ring", "glow", "rune", "rug", "weapon", "scattergun", "repeater", "cannon", "slash", "dust", "spark", "effect-ring" })
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
                    StartFloor(game, 42, floor);
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
                Assert.IsFalse(NearestExit(game, out _), "A modal shop must not retain the guide");
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
