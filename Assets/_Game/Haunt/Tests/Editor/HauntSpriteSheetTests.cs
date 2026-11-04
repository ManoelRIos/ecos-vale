using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ecos.Haunt.Tests
{
    /// <summary>Atlas import, slicing, ownership and presentation contracts.
    /// These tests require Unity; a source parser does not execute them.</summary>
    public sealed class HauntSpriteSheetTests
    {
        const string Root = "Assets/_Game/Resources/ReferenceSheets/";
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] Heroes = { "player", "medium", "reaper", "alchemist" };
        static readonly string[] Creatures = { "scarecrow", "gargoyle", "stalker-shadow", "fire-bat", "skeleton", "slime" };
        static readonly string[] Bosses = { "boss", "widow", "bishop", "matriarch", "bell", "iron-executioner", "bog-oracle" };
        static readonly string[] Props = { "barrel-explosive", "barrel-acid", "decrepit-chest" };

        static Texture2D AssertSheet(string relative, int cellSize, int rows)
        {
            string path = Root + relative + ".png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.IsNotNull(texture, "The distributable must contain the authored sheet: " + path);
            Assert.AreSame(texture, Resources.Load<Texture2D>("ReferenceSheets/" + relative));
            Assert.AreEqual(cellSize * 4, texture.width, path);
            Assert.AreEqual(cellSize * rows, texture.height, path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.IsNotNull(importer, path);
            Assert.AreEqual(TextureImporterType.Default, importer.textureType, "Runtime slicing creates the Sprite objects");
            Assert.AreEqual(FilterMode.Point, importer.filterMode, "Pixel edges must not be bilinearly filtered");
            Assert.IsFalse(importer.mipmapEnabled, "Mipmaps would blend adjacent poses");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, "Compression must not alter pixel clusters");
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, "Non-square sheets must keep their source pixel grid");
            Assert.IsTrue(importer.alphaIsTransparency, path);
            Assert.IsFalse(importer.isReadable, "Play Mode only needs GPU atlas slices, not a permanent CPU duplicate");
            return texture;
        }

        static void AssertSlice(Sprite sprite, Texture2D texture, int cellSize, int column, int row, float pivotY)
        {
            Assert.IsNotNull(sprite);
            Assert.AreSame(texture, sprite.texture, "Animation frames must share the shipped sheet instead of allocating per-pose textures");
            Assert.AreEqual(new Rect(column * cellSize, texture.height - (row + 1) * cellSize, cellSize, cellSize), sprite.rect);
            Assert.AreEqual(new Vector2(cellSize * .5f, pivotY), sprite.pivot);
            Assert.AreEqual(16, sprite.pixelsPerUnit);
        }

        [Test]
        public void ReferenceSheetsKeepTheUnscaledTransparentPointFilteredPixelGrid()
        {
            foreach (string id in Heroes.Concat(Creatures)) AssertSheet("actors/" + id, 24, 27);
            foreach (string id in Heroes) AssertSheet("actors/" + id + "-body", 24, 27);
            foreach (string id in Bosses) AssertSheet("bosses/" + id, 48, 5);
            foreach (string id in Props) AssertSheet("props/" + id, 16, 4);
        }

        [Test]
        public void ActorStatesAndDirectionsSliceTheExpectedCellsWithAStableFootPivot()
        {
            using (var art = new HauntPixelArt())
            {
                foreach (string id in Heroes.Concat(Creatures))
                {
                    var texture = Resources.Load<Texture2D>("ReferenceSheets/actors/" + id);
                    Assert.IsNotNull(texture, id);
                    foreach (ActorAnimationState state in Enum.GetValues(typeof(ActorAnimationState)))
                    foreach (ActorFacing facing in Enum.GetValues(typeof(ActorFacing)))
                    for (int frame = 0; frame < 4; frame++)
                    {
                        Sprite sprite = art.ActorFrame(id, state, frame, facing);
                        AssertSlice(sprite, texture, 24, frame, (int)state * 3 + (int)facing, 2);
                        Assert.AreSame(sprite, art.ActorFrame(id, state, frame + 240, facing), "Repeated animation must reuse the Sprite cache");
                    }
                    Assert.AreSame(art.ActorFrame(id, ActorAnimationState.Attack, 3), art.ActorFrame(id, ActorAnimationState.Attack, -1));
                }
                foreach (string id in Heroes)
                {
                    var texture = Resources.Load<Texture2D>("ReferenceSheets/actors/" + id + "-body");
                    Sprite body = art.ActorFrame(id, ActorAnimationState.Reload, 2, ActorFacing.Down, false);
                    AssertSlice(body, texture, 24, 2, (int)ActorAnimationState.Reload * 3 + (int)ActorFacing.Down, 2);
                    Assert.AreNotSame(body, art.ActorFrame(id, ActorAnimationState.Reload, 2, ActorFacing.Down),
                        "Runtime bodies and exported equipment poses must not overwrite each other's cache entry");
                }
            }
        }

        [Test]
        public void BossStatesAndInteractivePropStagesUseTheirOwnAtlasRows()
        {
            using (var art = new HauntPixelArt())
            {
                for (int variant = 0; variant < Bosses.Length; variant++)
                {
                    var texture = Resources.Load<Texture2D>("ReferenceSheets/bosses/" + Bosses[variant]);
                    foreach (BossAnimationState state in Enum.GetValues(typeof(BossAnimationState)))
                    for (int frame = 0; frame < 4; frame++)
                    {
                        Sprite sprite = art.ReferenceBossFrame(variant, state, frame);
                        AssertSlice(sprite, texture, 48, frame, (int)state, 24);
                        Assert.AreSame(sprite, art.ReferenceBossFrame(variant, state, frame + 4));
                    }
                    Assert.AreSame(art.ReferenceBossFrame(variant, BossAnimationState.Death, 3),
                        art.ReferenceBossFrame(variant, BossAnimationState.Death, -1));
                }
                foreach (string id in Props)
                {
                    var texture = Resources.Load<Texture2D>("ReferenceSheets/props/" + id);
                    for (int stage = 0; stage < 4; stage++)
                    for (int frame = 0; frame < 4; frame++)
                    {
                        Sprite sprite = art.PropFrame(id, stage, frame);
                        AssertSlice(sprite, texture, 16, frame, stage, 8);
                        Assert.AreSame(sprite, art.PropFrame(id, stage, frame + 4));
                    }
                    Assert.AreNotSame(art.PropFrame(id, 0), art.PropFrame(id, 2), "Ignition/leak/open stages must not reuse the intact pose");
                }
            }
        }

        [Test]
        public void BakingBypassesResourceSheetsAndKeepsReadableProceduralPixels()
        {
            using (var art = new HauntPixelArt(false))
            {
                foreach (string id in Heroes.Concat(Creatures))
                {
                    var idle = art.ActorFrame(id, ActorAnimationState.Idle, 0);
                    var attack = art.ActorFrame(id, ActorAnimationState.Attack, 2);
                    Assert.AreEqual(new Rect(0, 0, 24, 24), idle.rect);
                    Assert.IsTrue(idle.texture.isReadable, "The exporter needs the generated pixels without GPU readback");
                    Assert.AreNotSame(Resources.Load<Texture2D>("ReferenceSheets/actors/" + id), idle.texture);
                    Color32[] pixels = idle.texture.GetPixels32();
                    Assert.IsTrue(pixels.Any(pixel => pixel.a > 0), "A baked actor cannot be empty: " + id);
                    Assert.IsTrue(pixels.Any(pixel => pixel.a == 0), "A baked actor must preserve transparent surroundings: " + id);
                    CollectionAssert.AreNotEqual(pixels, attack.texture.GetPixels32(), "The action row must describe an actual pose change: " + id);
                }
                foreach (string id in Props)
                {
                    var sprite = art.PropFrame(id, 0);
                    Assert.AreEqual(new Rect(0, 0, 16, 16), sprite.rect);
                    Assert.IsTrue(sprite.texture.GetPixels32().Any(pixel => pixel.a > 0), id);
                }
            }
        }

        [Test]
        public void DisposingAnArtLibraryReleasesItsSlicesButKeepsSharedResourceTexturesAlive()
        {
            Texture2D texture = Resources.Load<Texture2D>("ReferenceSheets/actors/player");
            Assert.IsNotNull(texture);
            var art = new HauntPixelArt();
            Sprite sprite = art.ActorFrame("player", ActorAnimationState.Walk, 1);
            art.Dispose();
            Assert.IsFalse(sprite, "Library-owned slices must be released in Edit Mode");
            Assert.IsTrue(texture, "A library must not destroy a Resource texture shared by other libraries");
            Assert.AreSame(texture, Resources.Load<Texture2D>("ReferenceSheets/actors/player"));
            Assert.Throws<ObjectDisposedException>(() => art.ActorFrame("player", ActorAnimationState.Walk, 1));
            art.Dispose();
            using (var next = new HauntPixelArt())
                Assert.AreSame(texture, next.ActorFrame("player", ActorAnimationState.Walk, 1).texture);
        }

        static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, PrivateInstance).GetValue(target);
        static void Set(object target, string name, object value) => target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
        static object Invoke(object target, string name, params object[] arguments) => target.GetType().GetMethod(name, PrivateInstance).Invoke(target, arguments);

        [UnityTest]
        public IEnumerator ThePlayerSelectsActionRowsForMovementFireReloadRollAndPower()
        {
            var root = new GameObject("Haunt sprite sheet action integration test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                var player = Field<SpriteRenderer>(game, "player");
                Set(game, "playerMoving", true);
                Invoke(game, "UpdatePresentation");
                StringAssert.Contains("/Walk/", player.sprite.name);
                Assert.Less(Mathf.Abs(player.transform.position.y), .1f, "The new foot pivot belongs on the floor, not at the old sprite-center offset");
                Set(game, "playerMoving", false);
                Set(game, "muzzleTime", .04f);
                Invoke(game, "UpdateSpriteAnimations", .01f);
                Invoke(game, "UpdatePresentation");
                StringAssert.Contains("/Attack/", player.sprite.name);
                Invoke(game, "ClearSpriteAnimations");
                Set(game, "muzzleTime", 0f);
                Assert.IsTrue(game.State.TryConsumeShot());
                Assert.IsTrue(game.State.TryReload());
                Invoke(game, "UpdatePresentation");
                StringAssert.Contains("/Reload/", player.sprite.name);
                game.State.CancelReload();
                Set(game, "rollRemaining", .12f);
                Invoke(game, "UpdatePresentation");
                StringAssert.Contains("/Dodge/", player.sprite.name);
                Set(game, "rollRemaining", 0f);
                Invoke(game, "UsePower");
                Invoke(game, "UpdateSpriteAnimations", .01f);
                Invoke(game, "UpdatePresentation");
                StringAssert.Contains("/Cast/", player.sprite.name);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }
    }
}
