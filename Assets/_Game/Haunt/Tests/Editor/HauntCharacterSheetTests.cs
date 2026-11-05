using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Ecos.Haunt.Tests
{
    /// <summary>Native frame data and imported asset contracts. Requires the Unity Editor;
    /// parsing this file does not execute the tests or validate rendered animation.</summary>
    public sealed class HauntCharacterSheetTests
    {
        const string SourceSha256 = "cd640095da82003f8f44902950382692518393bfa57e61bc9bba1f3abec8f469";
        const string ResourceRoot = "Assets/_Game/Resources/CharacterSheets/";
        const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] Characters = { "player", "medium", "reaper", "alchemist" };

        [Serializable] sealed class Catalog
        {
            public int version, pixelsPerUnit, cellSize, textureWidth, textureHeight;
            public string sourceSha256;
            public Frame[] frames;
            public Clip[] clips;
        }
        [Serializable] sealed class Frame
        {
            public int x, y, width, height;
            public float pivotX, pivotY;
        }
        [Serializable] sealed class Clip
        {
            public string name, facing, pose;
            public bool alternate, loop;
            public float fps;
            public int[] frames;
        }

        static Catalog ReadCatalog()
        {
            var asset = Resources.Load<TextAsset>("CharacterSheets/catalog");
            Assert.IsNotNull(asset, "The original sprite clips must ship with the game");
            var data = JsonUtility.FromJson<Catalog>(asset.text);
            Assert.IsNotNull(data);
            Assert.AreEqual(1, data.version);
            Assert.AreEqual(16, data.pixelsPerUnit, "Original pixels must keep their native world scale");
            Assert.AreEqual(48, data.cellSize, "Atlas cells add padding rather than resizing the original art");
            Assert.AreEqual(SourceSha256, data.sourceSha256);
            Assert.IsNotEmpty(data.frames);
            Assert.IsNotEmpty(data.clips);
            return data;
        }

        static string ResourceName(string id) => id == "player" ? "pilot" : id;

        [Test]
        public void UploadedPilotSourceIsPreservedByteForByte()
        {
            string path = Path.Combine(Application.dataPath, "_Game/ArtSources/PilotOriginal.png");
            Assert.IsTrue(File.Exists(path), "Preserve the supplied source independently of derived atlases");
            using (var sha = SHA256.Create())
                Assert.AreEqual(SourceSha256, BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)))
                    .Replace("-", string.Empty).ToLowerInvariant(), "Do not redraw, resize or recolor the original PNG");
        }

        [Test]
        public void NativeAtlasesRetainSourceScaleAndExactPixelImportSettings()
        {
            Catalog catalog = ReadCatalog();
            foreach (string id in Characters)
            {
                string resource = ResourceName(id);
                string path = ResourceRoot + resource + ".png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.IsNotNull(texture, path);
                Assert.AreSame(texture, Resources.Load<Texture2D>("CharacterSheets/" + resource));
                Assert.AreEqual(catalog.textureWidth, texture.width, id);
                Assert.AreEqual(catalog.textureHeight, texture.height, id);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.IsNotNull(importer, path);
                Assert.AreEqual(TextureImporterType.Default, importer.textureType);
                Assert.AreEqual(FilterMode.Point, importer.filterMode);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression);
                Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale);
                Assert.AreEqual(16, importer.spritePixelsPerUnit);
                Assert.IsFalse(importer.mipmapEnabled, "Do not blend adjacent poses into lower mip levels");
                Assert.IsFalse(importer.isReadable, "Rendering should share a GPU atlas without a CPU copy");
                Assert.IsTrue(importer.alphaIsTransparency);
            }
        }

        [Test]
        public void AllCharactersUseEveryCataloguedClipWithIdenticalCropGeometry()
        {
            Catalog catalog = ReadCatalog();
            var keys = new HashSet<string>();
            using (var art = new HauntPixelArt())
            {
                foreach (Clip clip in catalog.clips)
                {
                    string key = clip.name + "/" + clip.facing + "/" + clip.pose + "/" + clip.alternate;
                    Assert.IsTrue(keys.Add(key), "Ambiguous source animation: " + key);
                    var kind = (CharacterClip)Enum.Parse(typeof(CharacterClip), clip.name);
                    var facing = (CharacterFacing)Enum.Parse(typeof(CharacterFacing), clip.facing);
                    var pose = (CharacterWeaponPose)Enum.Parse(typeof(CharacterWeaponPose), clip.pose);
                    Assert.Greater(clip.frames.Length, 0, key);
                    Assert.Greater(clip.fps, 0, key);
                    foreach (string id in Characters)
                    {
                        Assert.IsTrue(art.HasCharacterSheet(id), id);
                        Assert.AreEqual(clip.frames.Length, art.CharacterFrameCount(id, kind, facing, pose, clip.alternate), key);
                        Assert.AreEqual(clip.fps, art.CharacterFrameRate(id, kind, facing, pose, clip.alternate), .0001f, key);
                        for (int sample = 0; sample < clip.frames.Length; sample++)
                        {
                            int index = clip.frames[sample];
                            Assert.That(index, Is.InRange(0, catalog.frames.Length - 1), key);
                            Frame frame = catalog.frames[index];
                            Sprite sprite = art.CharacterFrame(id, kind, sample, facing, pose, clip.alternate);
                            Assert.AreSame(Resources.Load<Texture2D>("CharacterSheets/" + ResourceName(id)), sprite.texture, key);
                            Assert.AreEqual(new Rect(frame.x, frame.y, frame.width, frame.height), sprite.rect, key);
                            Assert.AreEqual(frame.pivotX * frame.width, sprite.pivot.x, .0001f, key);
                            Assert.AreEqual(frame.pivotY * frame.height, sprite.pivot.y, .0001f, key);
                            Assert.AreEqual(16, sprite.pixelsPerUnit, key);
                            Assert.AreEqual(frame.width / 16f, sprite.bounds.size.x, .0001f, key);
                            Assert.AreEqual(frame.height / 16f, sprite.bounds.size.y, .0001f, key);
                            Assert.AreSame(sprite, art.CharacterFrame(id, kind, sample, facing, pose, clip.alternate), key);
                        }
                    }
                }
            }
        }

        [Test]
        public void NativeSequencesRetainTheirOwnLengthsAndTimingInsteadOfFourFrameLoops()
        {
            using (var art = new HauntPixelArt())
            {
                foreach (string id in Characters)
                {
                    Assert.AreEqual(6, art.CharacterFrameCount(id, CharacterClip.Idle, CharacterFacing.Front, CharacterWeaponPose.Empty));
                    Assert.AreEqual(4, art.CharacterFrameCount(id, CharacterClip.Idle, CharacterFacing.Right, CharacterWeaponPose.Empty));
                    Assert.AreEqual(6, art.CharacterFrameCount(id, CharacterClip.Walk, CharacterFacing.Front));
                    Assert.AreEqual(9, art.CharacterFrameCount(id, CharacterClip.Dodge, CharacterFacing.Front));
                    Assert.AreEqual(19, art.CharacterFrameCount(id, CharacterClip.Death, CharacterFacing.Front));
                    Assert.AreEqual(18, art.CharacterFrameCount(id, CharacterClip.Death, CharacterFacing.Front, CharacterWeaponPose.Empty, true));
                    Assert.AreEqual(19f / 20, art.CharacterClipDuration(id, CharacterClip.Death, CharacterFacing.Front), .0001f);
                    Sprite fifth = art.CharacterFrame(id, CharacterClip.Idle, 4, CharacterFacing.Front);
                    Assert.AreNotSame(art.CharacterFrame(id, CharacterClip.Idle, 0, CharacterFacing.Front), fifth,
                        "A fifth original frame must remain addressable");
                    Sprite lastIdle = art.CharacterFrame(id, CharacterClip.Idle, 5, CharacterFacing.Front);
                    Assert.AreSame(lastIdle, art.CharacterFrame(id, CharacterClip.Idle, -1, CharacterFacing.Front));
                    Assert.AreSame(lastIdle, art.CharacterFrame(id, CharacterClip.Idle, 11, CharacterFacing.Front));
                    Sprite lastDeath = art.CharacterFrame(id, CharacterClip.Death, 18, CharacterFacing.Front);
                    Assert.AreSame(lastDeath, art.CharacterFrame(id, CharacterClip.Death, int.MaxValue, CharacterFacing.Front),
                        "One-shot clips hold their final frame instead of restarting");
                    Assert.AreSame(art.CharacterFrame(id, CharacterClip.Death, 0, CharacterFacing.Front),
                        art.CharacterFrame(id, CharacterClip.Death, -1, CharacterFacing.Front));
                }
            }
        }

        [Test]
        public void MissingDirectionsAndActionAliasesReuseOriginalPosesWithoutInventingFrames()
        {
            using (var art = new HauntPixelArt())
            {
                foreach (string id in Characters)
                {
                    Sprite front = art.CharacterFrame(id, CharacterClip.Idle, 2, CharacterFacing.Front, CharacterWeaponPose.Empty);
                    Sprite diagonal = art.CharacterFrame(id, CharacterClip.Idle, 2, CharacterFacing.FrontRight, CharacterWeaponPose.Empty);
                    Assert.AreSame(front.texture, diagonal.texture);
                    Assert.AreEqual(front.rect, diagonal.rect, "The empty-handed diagonal uses the original front row");
                    Sprite idle = art.CharacterFrame(id, CharacterClip.Idle, 1, CharacterFacing.Right, CharacterWeaponPose.TwoHanded);
                    foreach (CharacterClip alias in new[] { CharacterClip.Attack, CharacterClip.Reload, CharacterClip.Cast })
                    {
                        Sprite sprite = art.CharacterFrame(id, alias, 1, CharacterFacing.Right, CharacterWeaponPose.TwoHanded);
                        Assert.AreSame(idle.texture, sprite.texture);
                        Assert.AreEqual(idle.rect, sprite.rect, "Absent source actions retain the original weapon pose: " + alias);
                        StringAssert.Contains("/" + alias + "/", sprite.name);
                    }
                }
            }
            using (var bake = new HauntPixelArt(false))
            {
                Assert.IsFalse(bake.HasCharacterSheet("player"));
                Assert.AreSame(bake.ActorFrame("player", ActorAnimationState.Walk, 2, ActorFacing.Right, false),
                    bake.CharacterFrame("player", CharacterClip.Walk, 2, CharacterFacing.Right));
            }
        }

        [Test]
        public void DisposingCharacterSlicesKeepsSharedNativeTexturesAndCatalogAlive()
        {
            var texture = Resources.Load<Texture2D>("CharacterSheets/pilot");
            var catalog = Resources.Load<TextAsset>("CharacterSheets/catalog");
            Assert.IsNotNull(texture);
            Assert.IsNotNull(catalog);
            var art = new HauntPixelArt();
            Sprite sprite = art.CharacterFrame("player", CharacterClip.Walk, 5, CharacterFacing.Right);
            art.Dispose();
            Assert.IsFalse(sprite, "Only the sliced Sprite belongs to this library");
            Assert.IsTrue(texture, "Other libraries share the imported texture");
            Assert.IsTrue(catalog, "Other libraries share the imported catalog");
            Assert.AreSame(texture, Resources.Load<Texture2D>("CharacterSheets/pilot"));
            Assert.Throws<ObjectDisposedException>(() => art.CharacterFrame("player", CharacterClip.Idle, 0, CharacterFacing.Front));
            art.Dispose();
        }

        static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, PrivateInstance).GetValue(target);
        static void Set(object target, string name, object value) => target.GetType().GetField(name, PrivateInstance).SetValue(target, value);
        static object Invoke(object target, string name, params object[] arguments) => target.GetType().GetMethod(name, PrivateInstance).Invoke(target, arguments);

        [UnityTest]
        public IEnumerator NativePlayerPosesKeepTheirScaleAndFullDeathSequence()
        {
            var root = new GameObject("Native Pilot presentation test");
            try
            {
                var game = root.AddComponent<HauntGame>();
                game.enabled = false;
                game.BeginRun(42);
                Set(game, "flash", 0f); Set(game, "invincible", 0f); Set(game, "simulationTime", 0f);
                Set(game, "playerMoving", true);
                Invoke(game, "UpdatePresentation");
                var player = Field<SpriteRenderer>(game, "player");
                StringAssert.Contains("/Walk/", player.sprite.name);
                Assert.AreSame(Resources.Load<Texture2D>("CharacterSheets/pilot"), player.sprite.texture);
                Assert.AreEqual(16, player.sprite.pixelsPerUnit);
                Assert.AreEqual(Vector3.one, player.transform.localScale, "Do not squash or stretch the original artwork");
                Assert.AreEqual(Quaternion.identity, player.transform.rotation, "Do not rotate the original walk poses");
                Assert.AreEqual(Color.white, player.color, "Normal presentation must preserve the original RGB colors");
                Set(game, "playerMoving", false); Set(game, "rollRemaining", .12f);
                Invoke(game, "UpdatePresentation");
                StringAssert.Contains("/Dodge/", player.sprite.name);
                Assert.AreEqual(Vector3.one, player.transform.localScale, "The sheet already contains the complete roll pose");
                Assert.AreEqual(Quaternion.identity, player.transform.rotation);
                Set(game, "rollRemaining", 0f);
                game.State.Hurt(1000);
                Invoke(game, "UpdateBossPresentation", .7f);
                StringAssert.Contains("/Death/", player.sprite.name);
                Assert.Greater(player.color.a, 0, "A 19-frame native death must not be cut at the former 0.6-second timeout");
                Invoke(game, "UpdateBossPresentation", 3f);
                Assert.AreEqual(0, player.color.a, .0001f);
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(root); }
        }
    }
}
