using System;
using UnityEngine;

namespace Ecos.Haunt
{
    public enum ActorAnimationState { Idle, Walk, Attack, Dodge, Cast, Reload, Spawn, Death, Petrified }
    public enum ActorFacing { Right, Up, Down }

    public sealed partial class HauntPixelArt
    {
        public const int ActorFrameCount = 4;
        public static bool HasActorSheet(string id) => id == "player" || id == "medium" || id == "reaper" || id == "alchemist"
            || id == "scarecrow" || id == "gargoyle" || id == "stalker-shadow" || id == "fire-bat" || id == "skeleton" || id == "slime";

        /// <summary>Reference-sheet poses: 24px, shared foot pivot, independent state/direction cache.</summary>
        public Sprite ActorFrame(string id, ActorAnimationState state, int frame, ActorFacing facing = ActorFacing.Right, bool includeEquipment = true)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (!Enum.IsDefined(typeof(ActorAnimationState), state)) throw new ArgumentOutOfRangeException(nameof(state));
            if (!Enum.IsDefined(typeof(ActorFacing), facing)) throw new ArgumentOutOfRangeException(nameof(facing));
            if (!HasActorSheet(id)) return Frame(id, frame);
            frame = ((frame % ActorFrameCount) + ActorFrameCount) % ActorFrameCount;
            string key = "actor-sheet/" + id + "/" + state + "/" + facing + "/" + frame + "/" + includeEquipment;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            var loaded = TryLoadSheetSprite(key, "actors/" + id + (!includeEquipment && SheetHero(id) ? "-body" : ""),
                frame, (int)state * 3 + (int)facing, 24, 2f / 24);
            if (loaded) return loaded;
            Canvas c = SheetActorPixels(id, state, frame, facing, includeEquipment);
            var texture = new Texture2D(24, 24, TextureFormat.RGBA32, false)
            { name = "Haunt / " + key, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0 };
            texture.SetPixels32(c.Pixels); texture.Apply(false, useReferenceSheets);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 24, 24), new Vector2(.5f, 2f / 24), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Haunt / " + key; textures.Add(texture); sprites.Add(key, sprite); return sprite;
        }

        static readonly Color32 SheetSkin = Hex(0xf3cd91), SheetSkinShade = Hex(0xc58b58), SheetHair = Hex(0x6c3027),
            SheetHairLight = Hex(0xa94d2c), SheetBlue = Hex(0x3a5668), SheetBoot = Hex(0x242436), SheetPurple = Hex(0x704780),
            SheetPurpleShade = Hex(0x392c4c), SheetPurpleLight = Hex(0x9b6cb0), SheetRed = Hex(0x8d292b),
            SheetRedLight = Hex(0xc34839), SheetSteel = Hex(0x8b969d), SheetSteelLight = Hex(0xd4d8cd);

        static bool SheetHero(string id) => id == "player" || id == "medium" || id == "reaper" || id == "alchemist";
        static Canvas SheetActorPixels(string id, ActorAnimationState state, int frame, ActorFacing facing, bool equipment)
        {
            if (state == ActorAnimationState.Death) return SheetActorDeath(id, frame, facing, equipment);
            Canvas c;
            if (SheetHero(id)) c = SheetHeroPose(id, state, frame, facing, equipment);
            else if (id == "scarecrow") c = SheetScarecrow(state, frame);
            else if (id == "gargoyle") c = SheetGargoyle(state, frame);
            else if (id == "stalker-shadow") c = SheetShadow(state, frame);
            else if (id == "fire-bat") c = SheetFireBat(state, frame);
            else if (id == "skeleton") c = SheetSkeleton(state, frame);
            else c = SheetSlime(state, frame);
            if (state == ActorAnimationState.Spawn) c = SheetEmerge(c, id, frame);
            c.Outline(Ink); return c;
        }

        static Canvas SheetHeroPose(string id, ActorAnimationState state, int frame, ActorFacing facing, bool equipment)
        {
            if (state == ActorAnimationState.Dodge) return SheetHeroDodge(id, frame, facing, equipment);
            var c = new Canvas(24, 24);
            bool walk = state == ActorAnimationState.Walk;
            int bob = walk && (frame == 1 || frame == 3) ? 1 : 0;
            if (state == ActorAnimationState.Cast) bob = frame == 1 || frame == 2 ? -1 : 0;
            int stride = walk ? (frame == 0 ? -1 : frame == 2 ? 1 : 0) : 0;
            Color32 cloth = id == "player" ? Hex(0x53604a) : id == "reaper" ? SheetRed : id == "alchemist" ? SheetPurpleShade : SheetPurple;
            Color32 clothLight = id == "player" ? Hex(0x77816a) : id == "reaper" ? SheetRedLight : SheetPurpleLight;
            Color32 clothShade = id == "player" ? Hex(0x34413e) : id == "reaper" ? Hex(0x4d202c) : SheetPurpleShade;
            // Boots alternate under a stable foot pivot; robes and coat tails sway separately.
            c.Rect(8 + stride, 19, 3, 3, SheetBoot); c.Rect(13 - stride, 19, 3, 3, SheetBoot);
            c.Set(8 + stride, 21, SheetBlue); c.Set(14 - stride, 21, SheetBlue);
            c.Polygon(new[] { 8, 12 + bob, 15, 12 + bob, 17 + stride, 19, 7 - stride, 19 }, clothShade);
            c.Rect(9, 12 + bob, 6, 7, cloth); c.Rect(9, 13 + bob, 2, 5, clothLight);
            if (id == "player")
            {
                c.Rect(9, 18 + bob, 6, 2, SheetBlue); c.Rect(8, 13 + bob, 2, 4, clothLight);
                c.Line(9, 13 + bob, 14, 17 + bob, Hex(0x9c7853)); c.Set(12, 18 + bob, Gold);
                c.Polygon(new[] { 8, 12 + bob, 6, 14 + bob, 4 - stride, 17 + bob, 7, 18 + bob, 9, 16 + bob }, Hex(0x375658));
                SheetHumanHead(c, bob, facing, false, frame);
            }
            else if (id == "medium")
            {
                c.Polygon(new[] { 7, 13 + bob, 4 - stride, 19, 8, 18, 10, 14 + bob }, SheetPurpleShade);
                c.Line(12, 12 + bob, 12, 19, Hex(0xc8a3d0)); c.Rect(10, 17 + bob, 4, 1, Gold);
                SheetHumanHead(c, bob, facing, true, frame);
                c.Set(12, 13 + bob, Pink); c.Set(13, 13 + bob, Gold);
            }
            else if (id == "reaper")
            {
                c.Polygon(new[] { 8, 10 + bob, 5 - stride, 18, 4, 20, 9, 19, 11, 14 + bob }, Hex(0x6a242c));
                c.Line(8, 14 + bob, 6 - stride, 18, SheetRedLight);
                c.Polygon(new[] { 7, 6 + bob, 10, 3 + bob, 15, 4 + bob, 18, 10 + bob, 16, 13 + bob, 7, 12 + bob }, SheetRed);
                c.Line(8, 6 + bob, 11, 4 + bob, SheetRedLight); c.Line(11, 4 + bob, 15, 5 + bob, SheetRedLight);
                if (facing != ActorFacing.Up)
                {
                    c.Rect(9, 7 + bob, 7, 5, Hex(0xe7cfa1)); c.Rect(10, 11 + bob, 5, 2, Hex(0xc39465));
                    c.Rect(10, 9 + bob, 2, 2, Ink); c.Set(15, 9 + bob, Ink); c.Set(13, 12 + bob, Ink);
                    c.Set(9, 8 + bob, SheetSkin); c.Set(14, 11 + bob, Pale);
                }
                else { c.Line(9, 5 + bob, 13, 8 + bob, SheetRedLight); c.Line(13, 8 + bob, 15, 12 + bob, Hex(0x681f29)); }
            }
            else
            {
                c.Rect(10, 13 + bob, 4, 4, Hex(0x74855b)); c.Set(11, 14 + bob, Gold); c.Set(13, 16 + bob, Gold);
                c.Rect(8, 18 + bob, 8, 1, Hex(0x5c493b));
                c.Polygon(new[] { 8, 7 + bob, 10, 1 + bob, 13, 4 + bob, 17, 7 + bob, 19, 8 + bob, 5, 8 + bob }, SheetPurpleShade);
                c.Line(10, 2 + bob, 11, 6 + bob, Hex(0x596879)); c.Rect(7, 7 + bob, 10, 1, SheetPurpleLight);
                c.Rect(9, 9 + bob, 7, 4, SheetSkinShade); c.Rect(10, 9 + bob, 6, 3, SheetSkin);
                if (facing != ActorFacing.Up) { c.Set(11, 10 + bob, Ink); c.Set(15, 10 + bob, Ink); c.Rect(10, 12 + bob, 5, 1, SheetHair); }
                else c.Rect(9, 9 + bob, 7, 4, SheetHair);
            }
            int armY = state == ActorAnimationState.Reload ? 17 - (frame == 2 ? 3 : frame == 3 ? 1 : 0)
                : state == ActorAnimationState.Attack ? (frame == 0 ? 13 : frame == 1 ? 12 : frame == 2 ? 14 : 15)
                : 14 + bob + (walk && frame == 3 ? 1 : 0);
            int rightX = state == ActorAnimationState.Attack ? 16 - (frame == 1 ? 2 : 0) : 16;
            c.Rect(7, armY, 3, 2, clothLight); c.Set(7, armY + 2, SheetSkinShade);
            c.Rect(rightX, armY, 3, 2, SheetSkin); c.Set(rightX + 1, armY + 1, SheetSkinShade);
            if (equipment) SheetHeroEquipment(c, id, state, frame, facing, armY, rightX);
            if (state == ActorAnimationState.Attack && id == "reaper") SheetScytheArc(c, frame);
            if (state == ActorAnimationState.Cast) SheetCast(c, id, frame);
            if (state == ActorAnimationState.Reload)
            {
                c.Rect(17 + (frame & 1), 18 + frame, 1, 2, id == "alchemist" ? Mint : Gold);
                if (frame == 1 || frame == 2) { c.Set(19, 18 + frame, Pale); c.Set(6, 15 - frame, clothLight); }
            }
            if (state == ActorAnimationState.Idle && frame > 0) c.Set(7 + frame, 14 + bob, clothShade);
            if (state == ActorAnimationState.Petrified) c = SheetStoneFilter(c, frame);
            return c;
        }

        static void SheetHumanHead(Canvas c, int bob, ActorFacing facing, bool medium, int frame)
        {
            Color32 hair = medium ? SheetPurpleShade : SheetHair;
            Color32 light = medium ? SheetPurpleLight : SheetHairLight;
            c.Rect(7, 5 + bob, 10, 7, hair); c.Rect(9, 3 + bob, 6, 3, hair);
            c.Rect(8, 5 + bob, 8, 7, SheetSkinShade); c.Rect(9, 6 + bob, 7, 5, SheetSkin);
            c.Rect(7, 5 + bob, 3, 5, hair); c.Rect(8, 4 + bob, 7, 3, hair); c.Rect(9, 3 + bob, 4, 2, light);
            c.Set(14, 5 + bob, light); c.Set(8, 6 + bob, light);
            if (medium)
            {
                c.Line(7, 6 + bob, 6, 12 + bob, hair); c.Line(17, 6 + bob, 17, 13 + bob, hair);
                c.Set(6, 11 + bob, light); c.Set(16, 5 + bob, light);
            }
            if (facing == ActorFacing.Up)
            {
                c.Rect(8, 6 + bob, 9, 6, hair); c.Line(10, 5 + bob, 10, 9 + bob, light); c.Set(14, 10 + bob, light);
            }
            else
            {
                int eyeY = 9 + bob;
                if (frame == 3) { c.Set(11, eyeY, Ink); c.Set(15, eyeY, Ink); }
                else { c.Rect(11, eyeY - 1, 1, 2, Ink); c.Rect(15, eyeY - 1, 1, 2, Ink); }
                c.Set(13, 11 + bob, SheetSkinShade); c.Set(16, 9 + bob, SheetSkin);
                if (facing == ActorFacing.Down) c.Set(9, 10 + bob, SheetSkinShade);
            }
        }

        static void SheetHeroEquipment(Canvas c, string id, ActorAnimationState state, int frame, ActorFacing facing, int armY, int handX)
        {
            if (id == "player")
            {
                int x = handX + 1, y = armY - 1;
                if (state == ActorAnimationState.Reload) { c.Line(x, y, x + 2, y - 3, SheetSteel); c.Set(x + 1, y + 2, SheetSteelLight); }
                else if (facing == ActorFacing.Up) { c.Rect(16, 6, 2, 8, SheetSteel); c.Rect(16, 6, 1, 5, SheetSteelLight); }
                else if (facing == ActorFacing.Down) { c.Rect(16, 14, 2, 6, SheetSteel); c.Set(17, 19, Ink); }
                else
                {
                    c.Rect(x, y, 5, 2, SheetSteel); c.Rect(x, y, 4, 1, SheetSteelLight); c.Rect(x, y + 2, 2, 2, SheetHair);
                    c.Set(x + 4, y + 1, Ink);
                    if (state == ActorAnimationState.Attack && (frame == 1 || frame == 2))
                    { c.Set(23, y, Gold); c.Set(22, y - 1, Pale); c.Set(22, y + 2, Gold); }
                }
            }
            else if (id == "medium")
            {
                c.Line(19, 9, 18, 20, Hex(0x74616e)); c.Line(20, 10, 19, 15, SheetPurpleLight);
                c.Ellipse(18, 6, 4, 4, SheetPurple); c.Set(19, 7, Pink); c.Set(20, 8, Pale);
                if (state == ActorAnimationState.Attack) { c.Ellipse(20, 10 - frame, 3, 3, Violet); c.Set(21, 10 - frame, Pale); }
            }
            else if (id == "reaper")
            {
                int shaftX = state == ActorAnimationState.Attack ? 17 - frame : 18;
                c.Line(shaftX, 4, shaftX - 2, 21, Hex(0x8c6e55)); c.Line(shaftX + 1, 5, shaftX - 1, 20, Hex(0x594738));
                c.Polygon(new[] { shaftX - 1, 4, shaftX + 2, 4, 23, 8 + frame, 23, 11 + frame, 20, 8 + frame, shaftX + 1, 6 }, SheetSteel);
                c.Line(shaftX + 1, 4, 22, 8 + frame, SheetSteelLight);
            }
            else
            {
                int offset = state == ActorAnimationState.Reload ? (frame - 1) : state == ActorAnimationState.Attack ? -frame : 0;
                SheetBottle(c, 5, armY - 1, Hex(0x75c341)); SheetBottle(c, 19, armY - 1 + offset, Hex(0xa973cd));
                if (state == ActorAnimationState.Attack && frame > 0) SheetBottle(c, 20 + (frame & 1), 10 - frame, Hex(0x7bdc4b));
            }
        }

        static void SheetBottle(Canvas c, int x, int y, Color32 liquid)
        {
            c.Rect(x, y - 2, 2, 2, SheetSteelLight); c.Ellipse(x - 1, y, 4, 4, Hex(0x3c7761));
            c.Rect(x, y + 1, 2, 2, liquid); c.Set(x - 1, y + 1, Pale); c.Set(x, y, Pale);
        }

        static void SheetScytheArc(Canvas c, int frame)
        {
            if (frame == 0) { c.Line(17, 4, 21, 6, SheetSteelLight); return; }
            if (frame == 3) { c.Line(16, 17, 20, 14, Alpha(SheetSteelLight, 130)); return; }
            c.Polygon(frame == 1 ? new[] { 17, 2, 22, 5, 24, 11, 21, 16, 15, 19, 19, 14, 21, 9, 20, 5 }
                : new[] { 7, 19, 16, 19, 21, 16, 23, 11, 22, 17, 17, 21, 9, 22, 3, 20 }, Alpha(SheetSteelLight, 210));
            c.Line(10, 21, 17, 20, Pale); c.Line(17, 20, 21, 17, Pale);
        }

        static void SheetCast(Canvas c, string id, int frame)
        {
            Color32 color = id == "alchemist" ? Hex(0x8bf44f) : id == "reaper" ? Hex(0xd25858) : Violet;
            int radius = 3 + frame;
            c.Ellipse(18 - frame / 2, 7 - frame / 2, radius, radius, Alpha(color, 180));
            c.Set(20, 8, Pale); c.Set(21, 7 + (frame & 1), color); c.Set(17 - frame, 6, color);
            if (frame > 1) { c.Set(22, 5, color); c.Set(18, 3, Pale); c.Set(16, 11, color); }
        }

        static Canvas SheetHeroDodge(string id, int frame, ActorFacing facing, bool equipment)
        {
            if (id == "medium")
            {
                var ghost = SheetHeroPose(id, ActorAnimationState.Cast, frame, facing, equipment);
                var dissolve = new Canvas(24, 24); byte alpha = (byte)(205 - frame * 40);
                for (int y = 0; y < 24; y++) for (int x = 0; x < 24; x++)
                {
                    Color32 p = ghost.Pixels[(23 - y) * 24 + x];
                    if (p.a > 0 && (frame == 0 || (x * 3 + y + frame) % 7 > frame))
                    {
                        int lightness = (p.r + p.g + p.b) / 3;
                        Color32 tint = lightness < 65 ? Hex(0x512662) : lightness > 155 ? Hex(0xe5b7ef) : Hex(0xa65ac3);
                        dissolve.Set(x, y, Alpha(tint, alpha));
                    }
                }
                for (int i = 0; i < 5; i++) dissolve.Set(3 + i * 4, 21 - ((i * 3 + frame) % 8), Alpha(Violet, (byte)(110 + frame * 20)));
                return dissolve;
            }
            var c = new Canvas(24, 24); Color32 cloth = id == "player" ? Hex(0x5d694e) : id == "reaper" ? SheetRed : SheetPurple;
            if (frame == 0 || frame == 3)
            {
                c.Polygon(new[] { 6, 20, 8, 14, 15, 13, 18, 20, 14, 22 }, cloth);
                c.Rect(10 + (frame == 3 ? 2 : 0), 11, 6, 6, SheetSkin); c.Rect(10 + (frame == 3 ? 2 : 0), 10, 6, 3, id == "reaper" ? SheetRed : SheetHair);
                c.Set(14 + (frame == 3 ? 2 : 0), 14, Ink); c.Rect(7, 20, 4, 2, SheetBoot);
            }
            else
            {
                c.Ellipse(5, 13, 15, 9, cloth); c.Rect(7, 14, 8, 3, id == "player" ? SheetBlue : SheetPurpleShade);
                c.Line(8, 18, 14, 19, id == "player" ? Hex(0x89916b) : id == "reaper" ? SheetRedLight : SheetPurpleLight);
                int headX = frame == 1 ? 15 : 4;
                c.Rect(headX, 13, 6, 7, SheetSkinShade); c.Rect(headX + 1, 14, 5, 5, SheetSkin);
                Color32 hair = id == "reaper" ? SheetRed : id == "alchemist" ? SheetPurpleShade : SheetHair;
                c.Rect(headX, 12, 6, 3, hair); c.Rect(headX, 13, 2, 5, hair); c.Set(headX + 1, 12, id == "reaper" ? SheetRedLight : SheetHairLight);
                c.Rect(frame == 1 ? 3 : 18, 15, 4, 3, SheetBoot); c.Set(headX + 4, 16, Ink); c.Set(headX + 4, 19, SheetSkinShade);
                c.Rect(frame == 1 ? 12 : 10, 19, 3, 2, SheetSkin); c.Set(frame == 1 ? 12 : 10, 19, SheetSkinShade);
            }
            c.Line(2, 21, 7, 21, Alpha(SheetPurpleLight, 110)); c.Set(2 + frame, 19, Alpha(Violet, 130));
            return c;
        }

        static Canvas SheetScarecrow(ActorAnimationState state, int frame)
        {
            var c = new Canvas(24, 24); bool awake = state != ActorAnimationState.Petrified;
            int sway = state == ActorAnimationState.Walk ? (frame == 0 ? -1 : frame == 2 ? 1 : 0) : 0;
            int head = state == ActorAnimationState.Attack ? frame % 2 : frame == 1 ? 1 : 0;
            Color32 straw = Hex(0xb7a36a), darkStraw = Hex(0x776849);
            c.Line(10, 16, 9 + sway, 21, darkStraw); c.Line(13, 16, 14 - sway, 21, darkStraw);
            c.Set(8 + sway, 21, straw); c.Set(10 + sway, 21, straw); c.Set(13 - sway, 21, straw); c.Set(15 - sway, 21, straw);
            c.Rect(8, 12, 7, 6, Hex(0x4c6252)); c.Rect(9, 12, 3, 5, Hex(0x6a7959));
            c.Rect(11, 13, 2, 2, Hex(0x8c563e)); c.Set(14, 17, straw); c.Set(8, 17, straw);
            int arm = state == ActorAnimationState.Attack ? 10 + frame : frame == 3 ? 14 : 13;
            c.Line(8, 13, 3, arm, darkStraw); c.Line(8, 14, 3, arm + 1, straw);
            c.Line(15, 13, 18, arm + 1, straw); c.Set(2, arm - 1, straw); c.Set(3, arm + 2, straw);
            c.Ellipse(7 + sway, 5 + head, 10, 8, Hex(0xb95825));
            c.Ellipse(8 + sway, 5 + head, 8, 7, Hex(0xe38b2d)); c.Line(10 + sway, 6 + head, 9 + sway, 11 + head, Hex(0xc96a22));
            c.Line(14 + sway, 6 + head, 15 + sway, 10 + head, Hex(0xffb84c)); c.Rect(11 + sway, 3 + head, 2, 3, Hex(0x56683b));
            c.Set(12 + sway, 3 + head, Hex(0x94a45d));
            c.Set(9 + sway, 8 + head, Ink); c.Set(14 + sway, 8 + head, Ink);
            c.Line(10 + sway, 10 + head, 14 + sway, 10 + head, Ink); c.Set(11 + sway, 11 + head, Ink);
            if (awake) { c.Set(9 + sway, 9 + head, frame == 2 ? Pale : Gold); c.Set(14 + sway, 9 + head, Gold); }
            c.Line(19, 6, 17, 21, Hex(0x7b684e)); c.Line(20, 6, 18, 21, darkStraw);
            c.Polygon(new[] { 19, 5, 22, 6, 23, 10, 21, 8, 18, 7 }, SheetSteel);
            c.Line(20, 5, 22, 7, SheetSteelLight);
            if (state == ActorAnimationState.Attack && frame > 0)
            {
                c.Ellipse(18 + frame / 2, 10 + frame, 3, 3, SheetSteelLight); c.Set(22, 12 + frame, SheetSteel);
                c.Set(17, 8 + frame, Gold);
            }
            if (state == ActorAnimationState.Idle || state == ActorAnimationState.Petrified) c.Set(8 + frame, 15, darkStraw);
            return c;
        }

        static Canvas SheetGargoyle(ActorAnimationState state, int frame)
        {
            var c = new Canvas(24, 24); bool stone = state == ActorAnimationState.Petrified || state == ActorAnimationState.Idle;
            Color32 dark = Hex(0x444c52), mid = Hex(0x737a7a), light = Hex(0xa1a6a0);
            int bob = stone ? 1 : frame == 1 || frame == 3 ? -1 : 0;
            if (!stone)
            {
                int wingY = frame == 0 ? 4 : frame == 1 ? 1 : frame == 2 ? 9 : 12;
                c.Polygon(new[] { 8, 11 + bob, 3, wingY + 1, 1, wingY, 2, 15, 5, 13, 7, 17 }, dark);
                c.Line(7, 11 + bob, 2, wingY + 1, light); c.Line(2, wingY + 1, 3, 14, mid);
                c.Polygon(new[] { 16, 11 + bob, 20, wingY + 1, 22, wingY, 22, 15, 18, 13, 17, 17 }, dark);
                c.Line(17, 11 + bob, 21, wingY + 1, light); c.Line(21, wingY + 1, 21, 14, mid);
            }
            else
            {
                c.Rect(5, 20, 14, 2, dark); c.Rect(6, 20, 12, 1, light);
                c.Polygon(new[] { 7, 8, 4, 6, 4, 17, 8, 18 }, dark); c.Line(5, 8, 5, 15, mid);
                c.Polygon(new[] { 17, 8, 20, 6, 20, 17, 16, 18 }, dark); c.Line(19, 8, 19, 15, mid);
            }
            c.Polygon(new[] { 8, 11 + bob, 16, 11 + bob, 18, 17 + bob, 15, 20, 8, 20, 6, 17 + bob }, mid);
            c.Rect(9, 13 + bob, 6, 5, dark); c.Line(9, 13 + bob, 14, 13 + bob, light);
            c.Line(9, 16 + bob, 14, 16 + bob, Hex(0x909691)); c.Set(12, 18 + bob, light);
            c.Rect(7, 18 + bob, 3, 3, mid); c.Rect(15, 18 + bob, 3, 3, mid);
            c.Set(7, 21, light); c.Set(9, 21, light); c.Set(15, 21, light); c.Set(17, 21, light);
            c.Rect(8, 5 + bob, 8, 7, mid); c.Rect(9, 5 + bob, 6, 3, light);
            c.Polygon(new[] { 8, 7 + bob, 6, 2 + bob, 10, 5 + bob }, dark);
            c.Polygon(new[] { 15, 7 + bob, 18, 2 + bob, 15, 4 + bob }, dark);
            c.Line(7, 3 + bob, 9, 5 + bob, light); c.Line(17, 3 + bob, 16, 5 + bob, light);
            c.Rect(9, 8 + bob, 2, 1, stone ? dark : Hex(0xe78c4a)); c.Rect(14, 8 + bob, 2, 1, stone ? dark : Hex(0xe78c4a));
            c.Rect(10, 10 + bob, 5, 2, dark); c.Set(11, 10 + bob, light); c.Set(14, 10 + bob, light);
            if (state == ActorAnimationState.Attack && frame > 0)
            {
                c.Polygon(new[] { 16, 11, 19, 9 - frame, 22, 8 + frame, 23, 12, 21, 14, 18, 13 }, Hex(0xe96928));
                c.Ellipse(18, 10, 5, 3 + (frame & 1), Gold); c.Rect(18, 11, 4, 1, Pale);
                c.Set(22 - frame, 7, Orange);
            }
            c.Set(9 + frame, 14 + bob, light);
            return c;
        }

        static Canvas SheetShadow(ActorAnimationState state, int frame)
        {
            var c = new Canvas(24, 24); int sway = frame % 2, bob = frame == 1 || frame == 3 ? 1 : 0;
            Color32 dark = Hex(0x35203f), mid = Hex(0x672883), light = Hex(0xa550b7);
            c.Polygon(new[] { 7, 18 + bob, 8, 8 + bob, 12, 4 + bob, 17, 7 + bob, 18, 15, 15, 19, 18, 20,
                21 - sway, 20, 18, 22, 12, 21, 9, 22, 4 + sway, 21, 7, 19 }, Alpha(mid, 225));
            c.Polygon(new[] { 9, 8 + bob, 13, 6 + bob, 16, 9 + bob, 16, 14 + bob, 11, 15 + bob, 8, 12 + bob }, dark);
            c.Line(8, 10 + bob, 9, 7 + bob, light); c.Line(9, 7 + bob, 12, 5 + bob, light);
            c.Line(8, 17 + bob, 6 + sway, 20, light); c.Line(11, 20, 14, 19 + sway, light);
            c.Set(12, 10 + bob, frame == 3 ? light : Pink); c.Set(15, 10 + bob, Pale);
            c.Set(14, 13 + bob, light); c.Set(11, 15 + bob, Alpha(Violet, 180));
            c.Line(15, 15 + bob, 18, 13 + bob, mid); c.Set(19, 13 + bob, light);
            if (state == ActorAnimationState.Attack || state == ActorAnimationState.Cast)
            {
                int size = 2 + frame; c.Ellipse(19, 12 - frame / 2, size, size, Alpha(Violet, 180));
                c.Set(20, 13 - frame / 2, Pale); c.Set(22, 10 + frame, Pink);
            }
            c.Set(5 + frame, 20, Alpha(Violet, 150));
            if (state == ActorAnimationState.Petrified) c = SheetStoneFilter(c, frame);
            return c;
        }

        static Canvas SheetFireBat(ActorAnimationState state, int frame)
        {
            var c = new Canvas(24, 24); int wing = frame == 0 ? 3 : frame == 1 ? 0 : frame == 2 ? 7 : 12;
            Color32 membrane = Hex(0x6b2928), rim = Hex(0xcc552d), fire = Hex(0xffad3d);
            c.Polygon(new[] { 10, 13, 6, wing + 4, 1, wing + 1, 3, 16, 6, 15, 8, 18 }, membrane);
            c.Line(9, 12, 3, wing + 4, rim); c.Line(3, wing + 4, 1, wing + 1, fire); c.Line(1, wing + 1, 3, 15, rim);
            c.Line(5, wing + 7, 7, 16, rim);
            c.Polygon(new[] { 14, 13, 18, wing + 4, 22, wing + 1, 21, 16, 18, 15, 16, 18 }, membrane);
            c.Line(15, 12, 20, wing + 4, rim); c.Line(20, wing + 4, 22, wing + 1, fire); c.Line(22, wing + 1, 21, 15, rim);
            c.Line(19, wing + 7, 17, 16, rim);
            c.Polygon(new[] { 9, 9, 8, 6, 11, 8, 14, 8, 16, 6, 15, 11, 16, 15, 14, 18, 11, 19, 8, 15 }, Hex(0x3f2931));
            c.Rect(10, 11, 5, 3, membrane); c.Set(10, 12, Gold); c.Set(14, 12, Gold);
            c.Set(11, 15, Pale); c.Set(13, 15, Pale); c.Set(12, 17, rim);
            c.Set(5 + frame, 19, fire); c.Set(18 - frame, 21, rim);
            if (state == ActorAnimationState.Attack && frame > 0)
            { c.Ellipse(17, 15 - frame, 6, 4, rim); c.Ellipse(18, 16 - frame, 5, 2, fire); c.Set(21, 15 - frame, Pale); }
            return c;
        }

        static Canvas SheetSkeleton(ActorAnimationState state, int frame)
        {
            var c = new Canvas(24, 24); int bob = frame == 1 || frame == 3 ? 1 : 0;
            int stride = state == ActorAnimationState.Walk ? frame == 0 ? -2 : frame == 2 ? 2 : 0 : 0;
            Color32 bone = Hex(0xd4cfac), light = Hex(0xf1e9cc), shade = Hex(0x8c8978);
            c.Line(11, 16 + bob, 8 + stride, 21, bone); c.Line(13, 16 + bob, 15 - stride, 21, bone);
            c.Line(7 + stride, 22, 10 + stride, 22, shade); c.Line(14 - stride, 22, 17 - stride, 22, shade);
            c.Rect(11, 11 + bob, 2, 7, bone); c.Line(8, 13 + bob, 15, 13 + bob, bone); c.Line(9, 15 + bob, 15, 15 + bob, shade);
            c.Rect(10, 17 + bob, 5, 1, light); c.Set(12, 15 + bob, light);
            c.Line(9, 12 + bob, 6, 15 + bob, shade); c.Line(6, 15 + bob, 8, 17 + bob, bone);
            int handY = state == ActorAnimationState.Attack ? 12 - frame : 15 + bob;
            c.Line(15, 12 + bob, 18, handY, bone); c.Line(18, handY, 20, handY, light);
            c.Ellipse(7, 3 + bob, 10, 8, bone); c.Rect(9, 3 + bob, 6, 2, light);
            c.Rect(9, 6 + bob, 2, 2, Ink); c.Rect(14, 6 + bob, 2, 2, Ink); c.Set(12, 8 + bob, Ink);
            c.Rect(10, 9 + bob, 5, 2, shade); c.Set(10, 9 + bob, light); c.Set(12, 9 + bob, light); c.Set(14, 9 + bob, light);
            if (state == ActorAnimationState.Attack)
            {
                int boneX = 18 + (frame > 1 ? 1 : -frame);
                c.Line(boneX, handY - 1, 22, handY - 1, light); c.Set(boneX, handY - 2, bone); c.Set(22, handY, bone);
            }
            else { c.Line(19, 13 + bob, 19, 19 + bob, bone); c.Set(18, 13 + bob, light); c.Set(20, 19 + bob, light); }
            c.Set(11 + frame % 3, 12 + bob, light);
            return c;
        }

        static Canvas SheetSlime(ActorAnimationState state, int frame)
        {
            var c = new Canvas(24, 24); int x, y, w, h;
            if (state == ActorAnimationState.Walk || state == ActorAnimationState.Dodge || state == ActorAnimationState.Attack)
            {
                x = frame == 0 ? 3 : frame == 1 ? 6 : frame == 2 ? 7 : 2;
                y = frame == 0 ? 15 : frame == 1 ? 11 : frame == 2 ? 5 : 17;
                w = frame == 0 ? 18 : frame == 1 ? 12 : frame == 2 ? 10 : 20;
                h = frame == 0 ? 6 : frame == 1 ? 10 : frame == 2 ? 12 : 5;
            }
            else { x = 4; y = 12 - (frame & 1); w = 16; h = 10 + (frame & 1); }
            Color32 green = Hex(0x75b943), shade = Hex(0x3b7136), light = Hex(0xb8dd72);
            c.Ellipse(x, y, w, h, shade); c.Ellipse(x + 1, y, w - 2, h - 2, green);
            c.Rect(x + 2, y + h - 2, w - 4, 1, Hex(0x55913c));
            c.Ellipse(x + 3, y + 2, 4, 3, light); c.Set(x + 4, y + 2, Pale);
            int eyeY = y + h - 4;
            c.Rect(x + w - 7, eyeY, 1, 2, Ink); c.Rect(x + w - 4, eyeY, 1, 2, Ink);
            c.Set(x + w - 5, eyeY + 2, shade); c.Set(x + 2 + frame, y + h - 3, light);
            if (state == ActorAnimationState.Attack && frame == 2)
            { c.Ellipse(19, 8, 4, 4, green); c.Set(20, 9, light); c.Set(3, 11, light); }
            if (state == ActorAnimationState.Petrified) c = SheetStoneFilter(c, frame);
            return c;
        }

        static Canvas SheetEmerge(Canvas source, string id, int frame)
        {
            var c = new Canvas(24, 24); int rise = (3 - frame) * 5;
            for (int y = 0; y < 24 - rise; y++) for (int x = 0; x < 24; x++)
            {
                Color32 p = source.Pixels[(23 - y) * 24 + x];
                if (p.a > 0) c.Set(x, y + rise, p);
            }
            Color32 color = id == "slime" ? Hex(0x75b943) : id == "fire-bat" ? Orange : SheetPurpleLight;
            c.Ellipse(5, 20, 14, 3, Alpha(color, (byte)(160 - frame * 30)));
            c.Set(3 + frame, 19, Alpha(color, 150)); c.Set(19 - frame, 20, Alpha(color, 180));
            return c;
        }

        static Canvas SheetStoneFilter(Canvas source, int frame)
        {
            var c = new Canvas(24, 24);
            for (int y = 0; y < 24; y++) for (int x = 0; x < 24; x++)
            {
                var p = source.Pixels[(23 - y) * 24 + x]; if (p.a == 0) continue;
                byte gray = (byte)((p.r * 3 + p.g * 6 + p.b) / 10);
                c.Set(x, y, new Color32(gray, gray, (byte)Math.Min(255, gray + 5), p.a));
            }
            c.Set(9 + frame, 14, SheetSteelLight); return c;
        }

        static Canvas SheetActorDeath(string id, int frame, ActorFacing facing, bool equipment)
        {
            var c = new Canvas(24, 24); Color32 color = id == "slime" ? Hex(0x75b943)
                : id == "stalker-shadow" ? Violet : id == "fire-bat" || id == "scarecrow" ? Orange : SheetHero(id) ? SheetPurpleLight : SheetSteel;
            if (id == "slime")
            {
                c.Ellipse(2, 19 + frame / 2, 20, 3, Alpha(Hex(0x3d7736), (byte)(230 - frame * 35)));
                int h = 7 - frame; c.Ellipse(4, 20 - h, 7, h, color); c.Ellipse(15, 21 - h, 6, h - 1, color);
                c.Set(8, 17 - frame, Hex(0xc2dd81)); c.Set(17, 18 - frame, Pale); return c;
            }
            if (id == "stalker-shadow")
            {
                int shrink = frame * 2;
                c.Ellipse(4 + shrink / 2, 8 + shrink / 2, 15 - shrink, 12 - shrink, Alpha(Violet, (byte)(230 - frame * 40)));
                c.Ellipse(6 + shrink / 2, 10 + shrink / 2, 11 - shrink, 8 - shrink, Ink);
                c.Line(9, 16, 14 - frame, 16 - frame, Alpha(Pink, 180)); c.Set(4 + frame, 21 - frame, Violet);
                c.Set(19 - frame, 6 + frame, Pink); return c;
            }
            Canvas source = SheetHero(id) ? SheetHeroPose(id, ActorAnimationState.Idle, 0, facing, equipment)
                : id == "scarecrow" ? SheetScarecrow(ActorAnimationState.Idle, 0)
                : id == "gargoyle" ? SheetGargoyle(ActorAnimationState.Idle, 0)
                : id == "fire-bat" ? SheetFireBat(ActorAnimationState.Idle, 0) : SheetSkeleton(ActorAnimationState.Idle, 0);
            int crop = frame * 5;
            for (int y = crop; y < 23; y++) for (int x = 0; x < 24; x++)
            {
                var p = source.Pixels[(23 - y) * 24 + x];
                if (p.a > 0 && (x * 3 + y + frame) % (frame + 3) != 0)
                    c.Set(x, Math.Min(22, y + frame * 2), Alpha(p, (byte)(255 - frame * 55)));
            }
            for (int i = 0; i < 6; i++)
            {
                int x = 2 + (i * 5 + frame * 3) % 20, y = 22 - (i * 3 + frame) % (8 - frame);
                c.Set(x, y, Alpha(color, (byte)(220 - frame * 30)));
                if (frame < 2) c.Set(x + 1, y, Gold);
            }
            return c;
        }
    }
}
