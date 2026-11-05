using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public enum CharacterClip
    {
        Idle, Walk, Attack, Reload, Cast, Dodge, Death, Entrance, SelectIdle, SelectIntro,
        Draw, Dance, ThumbsUp, TableFlip, TableSlide, Kick, OpenChest, OpenMimic, ObtainItem,
        Tumble, PitFall, PitRespawn, Fly, GhostIdle, MiniBlank, Stairs, Pet, Revive, SpitOut, Hand, Shadow
    }
    public enum CharacterFacing { Front, FrontRight, Right, BackRight, Back }
    public enum CharacterWeaponPose { Empty, OneHanded, TwoHanded }

    public sealed partial class HauntPixelArt
    {
        // These fields are populated by Unity JsonUtility.
#pragma warning disable CS0649
        [Serializable] sealed class NativeCharacterCatalog
        {
            public int version, pixelsPerUnit, cellSize, textureWidth, textureHeight;
            public string sourceSha256;
            public NativeCharacterFrame[] frames;
            public NativeCharacterClip[] clips;
        }
        [Serializable] sealed class NativeCharacterFrame
        {
            public int x, y, width, height;
            public float pivotX, pivotY;
        }
        [Serializable] sealed class NativeCharacterClip
        {
            public string name, facing, pose;
            public bool alternate, loop;
            public float fps;
            public int[] frames;
        }
#pragma warning restore CS0649
        readonly struct CharacterKey : IEquatable<CharacterKey>
        {
            readonly CharacterClip clip;
            readonly CharacterFacing facing;
            readonly CharacterWeaponPose pose;
            readonly bool alternate;
            public CharacterKey(CharacterClip clip, CharacterFacing facing, CharacterWeaponPose pose, bool alternate)
            { this.clip = clip; this.facing = facing; this.pose = pose; this.alternate = alternate; }
            public bool Equals(CharacterKey other) => clip == other.clip && facing == other.facing && pose == other.pose && alternate == other.alternate;
            public override bool Equals(object obj) => obj is CharacterKey other && Equals(other);
            public override int GetHashCode() => (((int)clip * 7 + (int)facing) * 5 + (int)pose) * 2 + (alternate ? 1 : 0);
        }
        readonly Dictionary<CharacterKey, NativeCharacterClip> characterClips = new Dictionary<CharacterKey, NativeCharacterClip>();
        NativeCharacterCatalog characterCatalog;
        bool characterCatalogChecked;

        static string CharacterResource(string id) => id == "player" ? "pilot"
            : id == "medium" || id == "reaper" || id == "alchemist" ? id : null;

        bool LoadCharacterCatalog()
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (!useReferenceSheets) return false;
            if (characterCatalogChecked) return characterCatalog != null;
            characterCatalogChecked = true;
            var source = Resources.Load<TextAsset>("CharacterSheets/catalog");
            if (!source) return false;
            NativeCharacterCatalog data;
            try { data = JsonUtility.FromJson<NativeCharacterCatalog>(source.text); }
            catch (ArgumentException) { return false; }
            if (data == null || data.version != 1 || data.pixelsPerUnit != 16 || data.cellSize != 48
                || data.textureWidth <= 0 || data.textureHeight <= 0 || data.frames == null || data.clips == null
                || data.frames.Length == 0 || data.frames.Length > 4096) return false;
            foreach (var frame in data.frames)
                if (frame == null || frame.width <= 0 || frame.height <= 0 || frame.x < 0 || frame.y < 0
                    || frame.x + frame.width > data.textureWidth || frame.y + frame.height > data.textureHeight
                    || !(frame.pivotX >= 0 && frame.pivotX <= 1) || !(frame.pivotY >= 0 && frame.pivotY <= 1)) return false;
            foreach (var clip in data.clips)
            {
                if (clip == null || !Enum.TryParse(clip.name, out CharacterClip kind)
                    || !Enum.IsDefined(typeof(CharacterClip), kind)
                    || !Enum.TryParse(clip.facing, out CharacterFacing facing)
                    || !Enum.IsDefined(typeof(CharacterFacing), facing)
                    || !Enum.TryParse(clip.pose, out CharacterWeaponPose pose)
                    || !Enum.IsDefined(typeof(CharacterWeaponPose), pose)
                    || !(clip.fps > 0 && clip.fps <= 60) || clip.frames == null || clip.frames.Length == 0) continue;
                bool valid = true;
                foreach (int frame in clip.frames) if (frame < 0 || frame >= data.frames.Length) { valid = false; break; }
                var key = new CharacterKey(kind, facing, pose, clip.alternate);
                if (valid && !characterClips.ContainsKey(key)) characterClips.Add(key, clip);
            }
            if (characterClips.Count == 0) return false;
            characterCatalog = data;
            return true;
        }

        Texture2D CharacterTexture(string id)
        {
            string name = CharacterResource(id);
            if (name == null || !LoadCharacterCatalog()) return null;
            string key = "character/" + name;
            if (!referenceTextures.TryGetValue(key, out Texture2D texture))
            { texture = Resources.Load<Texture2D>("CharacterSheets/" + name); referenceTextures.Add(key, texture); }
            return texture && texture.width == characterCatalog.textureWidth && texture.height == characterCatalog.textureHeight ? texture : null;
        }

        public bool HasCharacterSheet(string id) => CharacterTexture(id);

        NativeCharacterClip ResolveCharacterClip(CharacterClip requested, CharacterFacing facing, CharacterWeaponPose pose, bool alternate)
        {
            if (!LoadCharacterCatalog()) return null;
            CharacterClip actual = requested == CharacterClip.Attack || requested == CharacterClip.Reload || requested == CharacterClip.Cast ? CharacterClip.Idle : requested;
            CharacterFacing closest = facing == CharacterFacing.FrontRight ? CharacterFacing.Front
                : facing == CharacterFacing.BackRight ? CharacterFacing.Back : facing;
            // Prefer the requested direction before falling back to another weapon
            // grip. Missing diagonals use the corresponding original front/back row.
            for (int costume = 0; costume < (alternate ? 2 : 1); costume++)
            for (int direction = 0; direction < 5; direction++)
            for (int grip = 0; grip < 4; grip++)
            {
                CharacterFacing candidateFacing = direction == 0 ? facing : direction == 1 ? closest
                    : direction == 2 ? CharacterFacing.Front : direction == 3 ? CharacterFacing.Right : CharacterFacing.Back;
                CharacterWeaponPose candidatePose = grip == 0 ? pose : grip == 1 ? CharacterWeaponPose.Empty
                    : grip == 2 ? CharacterWeaponPose.OneHanded : CharacterWeaponPose.TwoHanded;
                if (characterClips.TryGetValue(new CharacterKey(actual, candidateFacing, candidatePose, alternate && costume == 0), out var clip)) return clip;
            }
            if (actual != CharacterClip.Idle) return ResolveCharacterClip(CharacterClip.Idle, facing, pose, alternate);
            return null;
        }

        public int CharacterFrameCount(string id, CharacterClip clip, CharacterFacing facing, CharacterWeaponPose pose = CharacterWeaponPose.OneHanded, bool alternate = false)
            => HasCharacterSheet(id) ? ResolveCharacterClip(clip, facing, pose, alternate)?.frames.Length ?? ActorFrameCount : ActorFrameCount;
        public float CharacterFrameRate(string id, CharacterClip clip, CharacterFacing facing, CharacterWeaponPose pose = CharacterWeaponPose.OneHanded, bool alternate = false)
            => HasCharacterSheet(id) ? ResolveCharacterClip(clip, facing, pose, alternate)?.fps ?? 8 : 8;
        public float CharacterClipDuration(string id, CharacterClip clip, CharacterFacing facing, CharacterWeaponPose pose = CharacterWeaponPose.OneHanded, bool alternate = false)
            => CharacterFrameCount(id, clip, facing, pose, alternate) / CharacterFrameRate(id, clip, facing, pose, alternate);

        public Sprite CharacterFrame(string id, CharacterClip clip, int frame, CharacterFacing facing, CharacterWeaponPose pose = CharacterWeaponPose.OneHanded, bool alternate = false)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (!Enum.IsDefined(typeof(CharacterClip), clip) || !Enum.IsDefined(typeof(CharacterFacing), facing)
                || !Enum.IsDefined(typeof(CharacterWeaponPose), pose)) throw new ArgumentOutOfRangeException(nameof(clip));
            var texture = CharacterTexture(id);
            var animation = texture ? ResolveCharacterClip(clip, facing, pose, alternate) : null;
            if (animation == null)
            {
                ActorAnimationState legacy = clip == CharacterClip.Walk ? ActorAnimationState.Walk
                    : clip == CharacterClip.Dodge ? ActorAnimationState.Dodge : clip == CharacterClip.Death ? ActorAnimationState.Death
                    : clip == CharacterClip.Reload ? ActorAnimationState.Reload : clip == CharacterClip.Cast ? ActorAnimationState.Cast
                    : clip == CharacterClip.Attack ? ActorAnimationState.Attack : ActorAnimationState.Idle;
                return ActorFrame(id, legacy, frame, facing == CharacterFacing.Back || facing == CharacterFacing.BackRight ? ActorFacing.Up
                    : facing == CharacterFacing.Right ? ActorFacing.Right : ActorFacing.Down, false);
            }
            int sample = animation.loop ? ((frame % animation.frames.Length) + animation.frames.Length) % animation.frames.Length
                : Math.Max(0, Math.Min(animation.frames.Length - 1, frame));
            int index = animation.frames[sample];
            string key = "character/" + id + "/" + clip + "/" + facing + "/" + pose + "/" + alternate + "/" + index;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            var data = characterCatalog.frames[index];
            var sprite = Sprite.Create(texture, new Rect(data.x, data.y, data.width, data.height),
                new Vector2(data.pivotX, data.pivotY), characterCatalog.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = "Haunt / " + key; sprites.Add(key, sprite);
            return sprite;
        }

        void ClearCharacterCatalog()
        { characterCatalog = null; characterCatalogChecked = false; characterClips.Clear(); }
    }
}
