using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public enum BossAnimationState { Idle, Telegraph, Attack, Enrage, Death }

    /// <summary>Original pixel art and four-pose animation. Runtime textures belong to this instance.</summary>
    public sealed partial class HauntPixelArt : IDisposable
    {
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        Material material;
        bool disposed;
        public Material Material
        {
            get
            {
                if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
                if (!material)
                {
                    var source = Resources.Load<Material>("HauntSprites");
                    if (!source) throw new InvalidOperationException("Material HauntSprites não encontrado em Resources.");
                    material = new Material(source) { name = "Haunt / pixel sprites (runtime)" };
                }
                return material;
            }
        }

        public Sprite Get(string id) => Frame(id, 0);

        public Sprite Frame(string id, int frame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (SheetHero(id) && HasCharacterSheet(id))
                return CharacterFrame(id, CharacterClip.Idle, frame, CharacterFacing.Front, CharacterWeaponPose.Empty);
            if (HasActorSheet(id)) return ActorFrame(id, ActorAnimationState.Idle, frame);
            int referenceBoss = ReferenceBossIndex(id);
            if (referenceBoss >= 0) return ReferenceBossFrame(referenceBoss, BossAnimationState.Idle, frame);
            if (id == "barrel-explosive" || id == "barrel-acid" || id == "decrepit-chest") return PropFrame(id, 0, frame);
            bool animated = id == "scarecrow" || id == "gargoyle" || id == "stalker-shadow" || id == "fire-bat"
                || id == "barrel-explosive" || id == "barrel-acid" || id == "destructible-pillar" || id == "ossuary-candle"
                || id == "lantern-wisp" || id == "mushroom-brute" || id == "clockwork-imp"
                || id == "iron-executioner" || id == "bog-oracle" || id == "skeleton" || id == "slime" || id == "spider" || id == "miniboss"
                || id == "spawn-rift" || id == "shot-disc" || id == "top-shot"
                || id == "player" || id == "medium" || id == "reaper" || id == "alchemist"
                || id == "pumpkin" || id == "ghost" || id == "cultist" || id == "bat"
                || id == "larva" || id == "sentinel" || id == "acolyte"
                || id == "boss" || id == "widow" || id == "bishop" || id == "matriarch" || id == "bell"
                || id == "chandelier" || id == "merchant" || id == "candle" || id == "portal";
            frame = animated ? ((frame % 4) + 4) % 4 : 0;
            string key = id + "/" + frame;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            Canvas pixels;
            switch (id)
            {
                case "scarecrow": pixels = Scarecrow(frame); break;
                case "gargoyle": pixels = Gargoyle(frame); break;
                case "stalker-shadow": pixels = StalkerShadow(frame); break;
                case "fire-bat": pixels = FireBat(frame); break;
                case "barrel-acid": pixels = AcidBarrel(frame); break;
                case "destructible-pillar": pixels = DestructiblePillar(frame); break;
                case "gravestone": pixels = BorderGravestone(); break;
                case "fence": pixels = BorderFence(); break;
                case "ossuary-candle": pixels = OssuaryCandle(frame); break;
                case "lantern-wisp": pixels = LanternWisp(frame); break;
                case "mushroom-brute": pixels = MushroomBrute(frame); break;
                case "clockwork-imp": pixels = ClockworkImp(frame); break;
                case "iron-executioner": pixels = IronExecutioner(frame); break;
                case "bog-oracle": pixels = BogOracle(frame); break;
                case "umbrella-gun": pixels = CuriousWeapon(0); break;
                case "hive-launcher": pixels = CuriousWeapon(1); break;
                case "boxing-glove": pixels = CuriousWeapon(2); break;
                case "shot-carbine": pixels = ShotCapsule(0); break;
                case "shot-veil": pixels = SpectralPetal(true); break;
                case "shot-petal": pixels = SpectralPetal(false); break;
                case "shot-stinger": pixels = ShotCapsule(2); break;
                case "shot-fist": pixels = ShotFist(); break;
                case "barrel-explosive": pixels = ExplosiveBarrel(frame); break;
                case "water-urn": pixels = WaterUrn(); break;
                case "shattered-prop": pixels = ShatteredProp(); break;
                case "wet-puddle": pixels = WetPuddle(); break;
                case "ammo-cache": pixels = RitualAmmoCache(); break;
                case "giant-rounds": pixels = Relic(0); break;
                case "character-relic": pixels = Relic(1); break;
                case "quick-boots": pixels = Relic(2); break;
                case "wall-side0": pixels = DeepWall(0, false); break;
                case "wall-side1": pixels = DeepWall(1, false); break;
                case "wall-side2": pixels = DeepWall(2, false); break;
                case "wall-side3": pixels = DeepWall(3, false); break;
                case "wall-side4": pixels = DeepWall(4, false); break;
                case "wall-crown0": pixels = DeepWall(0, true); break;
                case "wall-crown1": pixels = DeepWall(1, true); break;
                case "wall-crown2": pixels = DeepWall(2, true); break;
                case "wall-crown3": pixels = DeepWall(3, true); break;
                case "wall-crown4": pixels = DeepWall(4, true); break;
                case "skeleton": pixels = Skeleton(frame); break;
                case "slime": pixels = Slime(frame); break;
                case "spider": pixels = Spider(frame); break;
                case "miniboss": pixels = MiniBoss(frame); break;
                case "spawn-rift": pixels = SpawnRift(frame); break;
                case "pit": pixels = Pit(); break;
                case "fog": pixels = Fog(); break;
                case "hunter-carbine": pixels = SignatureWeapon(0); break;
                case "veil-wand": pixels = SignatureWeapon(1); break;
                case "bone-caster": pixels = SignatureWeapon(2); break;
                case "arc-coil": pixels = SignatureWeapon(3); break;
                case "mirror-disc": pixels = SignatureWeapon(4); break;
                case "ray-lantern": pixels = SignatureWeapon(5); break;
                case "bone-flute": pixels = SignatureWeapon(6); break;
                case "shot-dot": pixels = ShotDot(); break;
                case "shot-bone": pixels = ShotBone(); break;
                case "shot-shock": pixels = ShotShock(); break;
                case "shot-disc": pixels = ShotDisc(frame); break;
                case "shot-ray": pixels = ShotRay(); break;
                case "shot-halo": pixels = ShotHalo(); break;
                case "shot-trail": pixels = ShotTrail(); break;
                case "impact-dust": pixels = ImpactDust(); break;
                case "boss-bar0": pixels = BossBar(0); break;
                case "boss-bar1": pixels = BossBar(1); break;
                case "boss-bar2": pixels = BossBar(2); break;
                case "boss-bar3": pixels = BossBar(3); break;
                case "boss-bar4": pixels = BossBar(4); break;
                case "boss-bar5": pixels = BossBar(5); break;
                case "boss-bar6": pixels = BossBar(6); break;
                case "boss-fill0": pixels = BossFill(0); break;
                case "boss-fill1": pixels = BossFill(1); break;
                case "boss-fill2": pixels = BossFill(2); break;
                case "boss-fill3": pixels = BossFill(3); break;
                case "boss-fill4": pixels = BossFill(4); break;
                case "boss-fill5": pixels = BossFill(5); break;
                case "boss-fill6": pixels = BossFill(6); break;
                case "stapler": pixels = Stapler(); break;
                case "banana-gun": pixels = BananaGun(); break;
                case "spinning-top": pixels = SpinningTop(); break;
                case "staple-shot": pixels = StapleShot(); break;
                case "banana-shot": pixels = BananaShot(); break;
                case "top-shot": pixels = TopShot(frame); break;
                case "shop-counter": pixels = ShopCounter(); break;
                case "shop-shelf": pixels = ShopShelf(); break;
                case "shop-case": pixels = ShopCase(); break;
                case "shop-board": pixels = ShopBoard(); break;
                case "chandelier": pixels = Chandelier(frame); break;
                case "exit-arrow": pixels = ExitArrow(); break;
                case "player": pixels = Player(frame); break;
                case "medium": pixels = Medium(frame); break;
                case "reaper": pixels = Reaper(frame); break;
                case "alchemist": pixels = Alchemist(frame); break;
                case "widow": pixels = Widow(frame); break;
                case "bishop": pixels = Bishop(frame); break;
                case "larva": pixels = NewCreature(0, frame); break;
                case "sentinel": pixels = NewCreature(1, frame); break;
                case "acolyte": pixels = NewCreature(2, frame); break;
                case "matriarch": pixels = NewGuardian(false, frame); break;
                case "bell": pixels = NewGuardian(true, frame); break;
                case "scattergun": pixels = NewWeapon(0); break;
                case "repeater": pixels = NewWeapon(1); break;
                case "cannon": pixels = NewWeapon(2); break;
                case "pumpkin": pixels = Pumpkin(frame); break;
                case "ghost": pixels = Ghost(frame); break;
                case "cultist": pixels = Cultist(frame); break;
                case "bat": pixels = Bat(frame); break;
                case "boss": pixels = Boss(frame); break;
                case "wall-relief0": pixels = WallRelief(0); break;
                case "floor-theme0-v0": pixels = ThemeFloor(0, 0); break;
                case "floor-theme0-v1": pixels = ThemeFloor(0, 1); break;
                case "floor-theme0-v2": pixels = ThemeFloor(0, 2); break;
                case "floor-theme0-v3": pixels = ThemeFloor(0, 3); break;
                case "wall-relief1": pixels = WallRelief(1); break;
                case "floor-theme1-v0": pixels = ThemeFloor(1, 0); break;
                case "floor-theme1-v1": pixels = ThemeFloor(1, 1); break;
                case "floor-theme1-v2": pixels = ThemeFloor(1, 2); break;
                case "floor-theme1-v3": pixels = ThemeFloor(1, 3); break;
                case "wall-relief2": pixels = WallRelief(2); break;
                case "floor-theme2-v0": pixels = ThemeFloor(2, 0); break;
                case "floor-theme2-v1": pixels = ThemeFloor(2, 1); break;
                case "floor-theme2-v2": pixels = ThemeFloor(2, 2); break;
                case "floor-theme2-v3": pixels = ThemeFloor(2, 3); break;
                case "wall-relief3": pixels = WallRelief(3); break;
                case "floor-theme3-v0": pixels = ThemeFloor(3, 0); break;
                case "floor-theme3-v1": pixels = ThemeFloor(3, 1); break;
                case "floor-theme3-v2": pixels = ThemeFloor(3, 2); break;
                case "floor-theme3-v3": pixels = ThemeFloor(3, 3); break;
                case "wall-relief4": pixels = WallRelief(4); break;
                case "floor-theme4-v0": pixels = ThemeFloor(4, 0); break;
                case "floor-theme4-v1": pixels = ThemeFloor(4, 1); break;
                case "floor-theme4-v2": pixels = ThemeFloor(4, 2); break;
                case "floor-theme4-v3": pixels = ThemeFloor(4, 3); break;
                case "floor-theme0": pixels = ThemeFloor(0); break;
                case "wall-theme0": pixels = ThemeWall(0); break;
                case "decor-theme0": pixels = ThemeDecor(0); break;
                case "map-theme0": pixels = ThemeMap(0); break;
                case "floor-theme1": pixels = ThemeFloor(1); break;
                case "wall-theme1": pixels = ThemeWall(1); break;
                case "decor-theme1": pixels = ThemeDecor(1); break;
                case "map-theme1": pixels = ThemeMap(1); break;
                case "floor-theme2": pixels = ThemeFloor(2); break;
                case "wall-theme2": pixels = ThemeWall(2); break;
                case "decor-theme2": pixels = ThemeDecor(2); break;
                case "map-theme2": pixels = ThemeMap(2); break;
                case "floor-theme3": pixels = ThemeFloor(3); break;
                case "wall-theme3": pixels = ThemeWall(3); break;
                case "decor-theme3": pixels = ThemeDecor(3); break;
                case "map-theme3": pixels = ThemeMap(3); break;
                case "floor-theme4": pixels = ThemeFloor(4); break;
                case "wall-theme4": pixels = ThemeWall(4); break;
                case "decor-theme4": pixels = ThemeDecor(4); break;
                case "map-theme4": pixels = ThemeMap(4); break;
                case "floor0": pixels = Floor(0); break;
                case "floor1": pixels = Floor(1); break;
                case "floor2": pixels = Floor(2); break;
                case "floor3": pixels = Floor(3); break;
                case "floor4": pixels = Floor(4); break;
                case "floor5": pixels = Floor(5); break;
                case "wall": pixels = Wall(); break;
                case "pillar": pixels = Pillar(); break;
                case "candle": pixels = Candle(frame); break;
                case "grave": pixels = Grave(); break;
                case "door": case "door-locked": pixels = Door(false, false); break;
                case "door-open": pixels = Door(true, false); break;
                case "door-boss": pixels = Door(true, true); break;
                case "portal": pixels = Portal(frame); break;
                case "merchant": pixels = Merchant(frame); break;
                case "coin": pixels = Coin(); break;
                case "bullet-player": pixels = Bullet(false); break;
                case "bullet-enemy": pixels = Bullet(true); break;
                case "muzzle": pixels = Muzzle(); break;
                case "heart": pixels = Heart(); break;
                case "shadow": pixels = Shadow(); break;
                case "player-ring": pixels = Ring(32, false); break;
                case "enemy-ring": pixels = Ring(32, true); break;
                case "effect-ring": pixels = Ring(32, false); break;
                case "glow": pixels = Glow(); break;
                case "rune": pixels = Rune(); break;
                case "rug": pixels = Rug(); break;
                case "weapon": pixels = Weapon(); break;
                case "slash": pixels = Slash(); break;
                case "dust": pixels = Dust(); break;
                case "spark": pixels = Spark(); break;
                default: throw new ArgumentException("Sprite desconhecido: " + id, nameof(id));
            }
            pixels = NightmareCreature(id, pixels, frame);
            pixels = IsGuardian(id) ? NightmareGuardian(id, pixels, frame) : VolumeProp(id, pixels);
            if (HasDetailedEquipment(id)) return CacheSprite(key, RefineEquipment(id, pixels), ReferencePixelsPerUnit);
            return CacheSprite(key, pixels);
        }

        /// <summary>Cached pixel poses for the five visible phases of every guardian.
        /// Geometry changes before upload; the unreadable texture is never modified.</summary>
        public Sprite BossFrame(string id, BossAnimationState state, int frame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (!Enum.IsDefined(typeof(BossAnimationState), state)) throw new ArgumentOutOfRangeException(nameof(state));
            int referenceBoss = ReferenceBossIndex(id);
            if (referenceBoss >= 0) return ReferenceBossFrame(referenceBoss, state, frame);
            frame = ((frame % 4) + 4) % 4;
            string key = "boss-state/" + id + "/" + state + "/" + frame;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            return CacheSprite(key, BossPose(id, state, frame));
        }

        Sprite CacheSprite(string key, Canvas pixels, float pixelsPerUnit = 16)
        {
            var texture = new Texture2D(pixels.Width, pixels.Height, TextureFormat.RGBA32, false)
            {
                name = "Haunt / " + key, filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
            };
            texture.SetPixels32(pixels.Pixels);
            texture.Apply(false, useReferenceSheets);
            var sprite = Sprite.Create(texture, new Rect(0, 0, pixels.Width, pixels.Height),
                new Vector2(.5f, .5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = "Haunt / " + key;
            textures.Add(texture); sprites.Add(key, sprite);
            return sprite;
        }

        static readonly Color32 Ink = Hex(0x151426), Shade = Hex(0x303044), Stone = Hex(0x565563),
            LightStone = Hex(0x8b8590), Teal = Hex(0x278d8d), Mint = Hex(0x78f5dc), Pale = Hex(0xeaffed),
            Rust = Hex(0x9e3f47), Orange = Hex(0xf68132), Gold = Hex(0xffd875),
            Bone = Hex(0xeadbc3), Violet = Hex(0xb36bcc), Pink = Hex(0xff76ad);
        static Color32 Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        static Color32 Alpha(Color32 color, byte alpha) => new Color32(color.r, color.g, color.b, alpha);

        // All guardian frames share a transparent HP window [24,216) x [8,16).
        // UI can fill that region without changing geometry for each guardian.
        static Canvas BossBar(int theme)
        {
            var c = new Canvas(240,24);
            Color32 trim = theme == 0 ? Hex(0x9c743d) : theme == 1 ? Hex(0x9eabc8)
                : theme == 2 ? Gold : theme == 3 ? Hex(0x7da26a) : theme == 5 ? Hex(0xb7bdc4) : theme == 6 ? Hex(0x93c995) : Hex(0xae8ac1);
            Color32 dark = theme == 0 ? Hex(0x3a3729) : theme == 1 ? Hex(0x38344d)
                : theme == 2 ? Hex(0x583342) : theme == 3 ? Hex(0x2e4945) : theme == 5 ? Hex(0x58353e) : theme == 6 ? Hex(0x234b48) : Hex(0x352d4b);
            c.Rect(23,5,194,3,Ink); c.Rect(23,16,194,4,Ink);
            c.Rect(24,6,192,1,trim); c.Rect(24,17,192,1,trim);
            c.Line(28,4,212,4,dark); c.Line(28,20,212,20,dark);
            c.Rect(22,7,2,10,trim); c.Rect(216,7,2,10,trim);
            for (int side=0;side<2;side++)
            {
                int x = side == 0 ? 0 : 216;
                if (theme == 0)
                {
                    c.Polygon(new[]{x+2,18,x+5,5,x+12,1,x+19,5,x+23,18,x+17,22,x+8,21},Hex(0x633f34));
                    c.Ellipse(x+4,4,17,16,Orange); c.Ellipse(x+8,4,9,15,Hex(0xd35e27));
                    c.Rect(x+10,1,3,5,Hex(0x647e4b)); c.Line(x+12,1,x+17,0,Hex(0x93a45b));
                    c.Polygon(new[]{x+6,9,x+10,7,x+10,11},Ink); c.Polygon(new[]{x+18,9,x+14,7,x+14,11},Ink);
                    c.Line(x+7,14,x+16,14,Gold); c.Set(x+11,15,Ink);
                    c.Line(x+2,18,x,22,trim); c.Line(x+20,18,x+23,22,trim);
                }
                else if (theme == 1)
                {
                    for(int ray=0;ray<4;ray++) { c.Line(x+12,12,x+ray*7,0,trim); c.Line(x+12,12,x+ray*7,23,trim); }
                    c.Line(x+1,6,x+22,6,Hex(0x626e8e)); c.Line(x+1,18,x+22,18,Hex(0x626e8e));
                    c.Ellipse(x+7,6,11,13,Hex(0xd3d9dc)); c.Ellipse(x+9,9,7,7,Ink);
                    c.Set(x+10,11,Pink); c.Set(x+14,11,Pink); c.Rect(x+11,15,3,3,Hex(0x9a759e));
                }
                else if (theme == 2)
                {
                    c.Polygon(new[]{x+3,19,x+3,6,x+12,0,x+21,6,x+21,19,x+12,23},dark);
                    c.Line(x+3,6,x+12,1,Gold); c.Line(x+12,1,x+21,6,Gold);
                    c.Rect(x+10,4,5,16,Gold); c.Rect(x+5,8,15,4,Gold);
                    c.Rect(x+11,5,2,14,Bone); c.Rect(x+6,9,13,2,Bone); c.Set(x+12,9,Pale);
                    c.Rect(x+1,20,22,2,trim); c.Set(x+12,22,Orange);
                }
                else if (theme == 3)
                {
                    c.Polygon(new[]{x+1,20,x+4,3,x+9,6,x+12,0,x+15,6,x+20,3,x+23,20},dark);
                    c.Ellipse(x+4,7,16,13,Hex(0x5f896a));
                    for(int eye=0;eye<3;eye++) { c.Ellipse(x+5+eye*5,9,4,5,Gold); c.Set(x+6+eye*5,11,Ink); }
                    c.Polygon(new[]{x+6,17,x+9,21,x+12,18,x+15,21,x+18,17},Bone);
                    c.Set(x+3,22,Mint); c.Set(x+20,23,Teal); c.Line(x+1,6,x+4,13,trim);
                }
                else if (theme == 5)
                {
                    c.Polygon(new[]{x+2,22,x+4,6,x+12,1,x+20,6,x+22,22},Hex(0x383643));
                    c.Rect(x+6,6,13,13,Hex(0x8c8a94)); c.Rect(x+7,7,10,3,Hex(0xc7c7c5));
                    c.Rect(x+7,11,4,3,Ink); c.Rect(x+14,11,4,3,Ink);
                    c.Set(x+8,12,Orange); c.Set(x+15,12,Orange); c.Rect(x+11,15,3,4,Ink);
                    c.Line(x+3,3,x+3,20,trim); c.Line(x+21,3,x+21,20,trim);
                    c.Rect(x+1,2,4,3,Gold); c.Rect(x+19,18,4,3,Gold);
                }
                else if (theme == 6)
                {
                    c.Ellipse(x+3,7,18,14,Hex(0x436d63)); c.Ellipse(x+4,4,7,8,Hex(0x9fbc72));
                    c.Ellipse(x+14,4,7,8,Hex(0x9fbc72)); c.Rect(x+6,6,3,4,Ink); c.Rect(x+16,6,3,4,Ink);
                    c.Set(x+7,6,Gold); c.Set(x+17,6,Gold); c.Line(x+7,17,x+18,17,Mint);
                    c.Line(x+3,8,x,1,Teal); c.Line(x+21,8,x+23,1,Teal);
                    c.Set(x+1,1,Pale); c.Set(x+22,1,Pale); c.Rect(x+9,20,7,2,Gold);
                }
                else
                {
                    c.Line(x+3,0,x+6,8,trim); c.Line(x+21,0,x+18,8,trim);
                    c.Polygon(new[]{x+3,18,x+7,13,x+7,7,x+12,3,x+17,7,x+17,13,x+22,18},Hex(0x886178));
                    c.Polygon(new[]{x+6,17,x+10,12,x+10,8,x+13,6,x+15,9,x+15,13,x+19,17},Gold);
                    c.Rect(x+3,18,19,3,trim); c.Ellipse(x+10,21,5,3,Gold);
                    c.Rect(x+9,11,2,3,Ink); c.Rect(x+15,11,2,3,Ink); c.Rect(x+11,15,5,2,Ink);
                }
            }
            if(theme==0)
                for(int x=30;x<212;x+=18) { c.Line(x,20,x+6,22,trim); c.Line(x+6,22,x+10,19,trim); c.Set(x+3,2,Teal); }
            else if(theme==1)
                for(int x=28;x<211;x+=12) { c.Line(x,3,x+6,0,trim); c.Line(x+6,0,x+12,3,trim); c.Set(x+6,21,trim); }
            else if(theme==2)
                for(int x=31;x<210;x+=24) { c.Rect(x,2,3,3,Gold); c.Set(x+1,0,Bone); c.Rect(x-1,21,5,2,Gold); }
            else if(theme==3)
                for(int x=30;x<212;x+=15) { c.Ellipse(x,1,5,3,Teal); c.Set(x+2,0,Mint); c.Line(x,21,x+3,23,trim); }
            else if(theme==5)
                for(int x=28;x<212;x+=16) { c.Rect(x,1,5,3,trim); c.Set(x+1,1,Pale); c.Rect(x+6,20,5,3,trim); c.Set(x+7,21,Orange); }
            else if(theme==6)
                for(int x=28;x<212;x+=18) { c.Line(x,3,x+5,0,Teal); c.Line(x+5,0,x+9,3,trim); c.Ellipse(x+7,20,5,3,Mint); }
            else
                for(int x=28;x<212;x+=18) { c.Polygon(new[]{x,3,x+4,0,x+8,3},trim); c.Set(x+4,22,Violet); }
            return c;
        }

        static Canvas BossFill(int theme)
        {
            var c = new Canvas(16,8);
            Color32 baseColor = theme == 0 ? Hex(0xe5762e) : theme == 1 ? Hex(0x9d8dc5)
                : theme == 2 ? Hex(0xd65d69) : theme == 3 ? Hex(0x6bb563) : theme == 5 ? Hex(0xe7794c) : theme == 6 ? Hex(0x4da993) : Hex(0xa863b5);
            Color32 light = theme == 0 ? Gold : theme == 1 ? Hex(0xe0e4f1)
                : theme == 2 ? Hex(0xffa395) : theme == 3 ? Mint : theme == 5 ? Hex(0xffd394) : theme == 6 ? Hex(0xb6f3b2) : Hex(0xe2adf0);
            c.Rect(0,0,16,8,baseColor); c.Rect(0,0,16,1,light); c.Rect(0,6,16,2,Alpha(Ink,80));
            c.Line(1,2,5,2,light); c.Line(9,3,13,3,light); c.Set(7,4,Alpha(Ink,90)); c.Set(14,5,light);
            return c;
        }

        // Bright, continuous silhouettes remain readable over textured floors. The
        // dark rim belongs to the body; runtime adds only a small local light halo.
        static Canvas Player(int frame) => EnhancedHero(0, frame);
        static Canvas Medium(int frame) => EnhancedHero(1, frame);
        static Canvas Reaper(int frame) => EnhancedHero(2, frame);
        static Canvas Alchemist(int frame) => EnhancedHero(3, frame);

        // Nearest-neighbour canvas composition keeps poses crisp and their pivot
        // unchanged. It is done only on the first request for each cached sprite.
        static void Stamp(Canvas target, Canvas source, int offsetX = 0, int offsetY = 0,
            float scaleX = 1, float scaleY = 1, float shear = 0)
        {
            float halfX=source.Width*.5f, halfY=source.Height*.5f;
            for(int y=0;y<target.Height;y++) for(int x=0;x<target.Width;x++)
            {
                int py=(int)Math.Round((y-offsetY-halfY)/scaleY+halfY);
                int px=(int)Math.Round((x-offsetX-halfX-shear*(y-offsetY-halfY))/scaleX+halfX);
                if(px<0||py<0||px>=source.Width||py>=source.Height) continue;
                Color32 pixel=source.Pixels[(source.Height-1-py)*source.Width+px];
                if(pixel.a!=0) target.Set(x,y,pixel);
            }
        }

        static Canvas EnhancedHero(int kind, int frame)
        {
            // Compact silhouettes and a few broad ivory/black clusters, rather
            // than surface noise, keep the macabre equipment readable in play.
            var c=new Canvas(32,36); int stride=frame==1?1:frame==3?-1:0, wind=stride*2;
            Color32 mourning=Hex(0x272739), edge=Hex(0x645774), blood=Hex(0x963a4e), ivory=Hex(0xe5dcc7);
            c.Ellipse(5,30,23,4,Alpha(Ink,80));
            c.Polygon(new[]{9,16,23,16,28+wind,30,23,28,22,33,17,30,13,34,10,29,3+wind,32,7,23},mourning);
            c.Polygon(new[]{9,18,14,18,11,28,7+wind,29},edge); c.Line(23,21,26+wind,28,Hex(0x40394f));
            c.Rect(10,27+stride,4,6,Hex(0x444353)); c.Rect(19,27-stride,4,6,Hex(0x343342));
            c.Rect(9,32+stride,6,2,Hex(0x737080)); c.Rect(18,32-stride,6,2,Hex(0x696476));
            c.Polygon(new[]{10,17,22,17,24,28,17,30,9,27},kind==3?Hex(0x3e514b):kind==1?Hex(0x554067):kind==2?Hex(0x5e344a):Hex(0x3c4755));
            c.Rect(10,26,13,3,Hex(0x211f30)); c.Rect(15,26,4,4,Hex(0x8d7a65)); c.Rect(16,27,2,2,Ink);
            c.Line(9,19,6,26+stride,edge); c.Line(23,19,27,25-stride,Hex(0x413846));
            c.Rect(4,25+stride,4,3,ivory); c.Rect(25,24-stride,4,3,ivory);
            if(kind==0)
            {
                // The hunter's face is an antlerless bone mask under a mourning hat.
                c.Polygon(new[]{7,13,9,5,15,2,22,6,25,14,22,19,10,19},mourning);
                c.Polygon(new[]{11,8,21,8,23,14,20,20,13,20,10,15},ivory);
                c.Polygon(new[]{11,11,16,12,14,15,11,15},Ink); c.Polygon(new[]{22,11,17,12,19,15,22,15},Ink);
                c.Set(12,12,Hex(0x9ed8c9)); c.Set(20,12,Hex(0x9ed8c9));
                c.Polygon(new[]{16,14,18,14,19,17,15,17},Ink); c.Line(13,18,20,18,Hex(0x7e6a68));
                c.Rect(15,18,1,2,Ink); c.Rect(18,18,1,2,Ink);
                c.Polygon(new[]{7,9,10,2,20,3,24,10},Hex(0x38384e)); c.Rect(9,8,15,3,Hex(0x754455));
                c.Polygon(new[]{3,11,9,9,25,10,29,13,23,14,5,13},Hex(0x272c40)); c.Line(5,11,24,11,Hex(0x82939c));
                c.Line(23,8,28,3,Hex(0xb2c3be)); c.Set(29,2,ivory); c.Set(26,6,Hex(0x78b4af));
                c.Rect(10,21,3,4,ivory); c.Line(11,22,12,24,Ink); c.Rect(21,23,4,6,Hex(0x67434c));
                c.Line(7+wind,28,4+wind,31,Hex(0x718d91));
            }
            else if(kind==1)
            {
                // A face-sized void, an organic staff and orbiting lidless eyes.
                c.Polygon(new[]{8,16,9,7,16,1,24,7,25,17,21,21,11,20},Hex(0x6c567f));
                c.Polygon(new[]{11,15,12,8,16,5,21,9,23,16,19,19,13,18},Hex(0x0d1121));
                c.Rect(13,11,2,1,Hex(0xc1b0e0)); c.Rect(20,12,2,1,Hex(0xc1b0e0));
                c.Line(14,20,16,25,ivory); c.Line(20,20,18,25,ivory); c.Rect(15,24,4,4,Hex(0x9e739a));
                c.Line(2,31,4,8,Hex(0x715164)); c.Line(3,30,5,9,Hex(0xb5a0a3));
                c.Polygon(new[]{3,10,0,7,2,2,6,1,9,5,7,10},Hex(0x46374f));
                c.Ellipse(2,3,5,5,ivory); c.Rect(4,4,1,3,Ink); c.Set(4,4,Mint);
                c.Line(4,14,1,17,blood); c.Line(3,22,6,19,blood); c.Rect(2,24,4,2,ivory);
                int[][] orbit={new[]{28,10,8,9},new[]{27,7,9,11},new[]{25,6,8,15},new[]{28,13,6,12}};
                int[] q=orbit[frame]; c.Ellipse(q[0]-2,q[1]-2,5,4,ivory); c.Set(q[0],q[1]-1,Ink); c.Set(q[0],q[1],Mint);
                c.Ellipse(q[2]-1,q[3]-1,3,3,ivory); c.Set(q[2],q[3],Violet);
            }
            else if(kind==2)
            {
                // Exposed ribs and a long, fractured lower jaw beneath the hood.
                c.Polygon(new[]{7,16,9,6,15,2,23,5,26,16,22,21,11,20},mourning);
                c.Polygon(new[]{12,7,21,6,24,12,21,17,12,17,10,12},ivory);
                c.Polygon(new[]{12,10,16,11,14,14,11,14},Ink); c.Polygon(new[]{22,10,18,11,19,14,23,13},Ink);
                c.Set(13,11,Hex(0xffa27c)); c.Set(21,11,Hex(0xffa27c)); c.Rect(16,13,2,3,Ink);
                c.Polygon(new[]{12,16,15,18,21,16,22,20,19,23,14,22,11,19},Hex(0xa3958e));
                c.Rect(13,17,8,3,Ink); for(int x=13;x<21;x+=3)c.Rect(x,17,1,2,ivory); c.Line(14,21,19,22,ivory);
                c.Rect(14,23,7,6,Hex(0x171d2a)); c.Line(17,22,17,29,ivory);
                for(int y=23;y<29;y+=2){c.Line(12,y,15,y+1,ivory); c.Line(19,y+1,22,y,ivory);}
                c.Line(3,32,5,7,Hex(0x9b8b92)); c.Line(4,30,6,8,Hex(0x544656));
                c.Polygon(new[]{3,8,7,2,17,1,25,4,28,10,22,6,17,5,12,6,10,9,7,8,5,13},Hex(0x9ba6af));
                c.Polygon(new[]{4,7,8,3,17,2,23,4,17,3,10,5,7,9},ivory); c.Line(8,6,13,4,Hex(0x608b91));
                c.Rect(3,24,4,2,ivory); c.Set(23,28,blood);
            }
            else
            {
                // A plague beak replaces the human face; glass reservoirs show veins.
                c.Polygon(new[]{8,16,9,7,15,3,23,6,25,16,21,20,11,20},Hex(0x2c3a3e));
                c.Polygon(new[]{10,9,21,8,25,13,21,15,16,15,13,13},ivory);
                c.Polygon(new[]{20,12,28,16,23,19,17,16},Hex(0xb6b4a2)); c.Line(21,14,26,16,Hex(0x777b78));
                c.Ellipse(10,8,7,7,Hex(0x7e7a60)); c.Ellipse(11,9,5,5,Ink); c.Set(13,11,Hex(0x95df71)); c.Set(12,10,ivory);
                c.Rect(7,7,16,2,Hex(0x5c6256)); c.Polygon(new[]{9,7,11,2,19,2,23,7},Hex(0x3a4544)); c.Line(11,3,18,3,Hex(0x929b85));
                c.Rect(4,15,5,15,Hex(0x52615b)); c.Rect(5,16,3,11,Hex(0x86b977)); c.Line(6,17,5,22,Hex(0xd5e7af));
                c.Line(5,22,7,25,Hex(0x336644)); c.Rect(24,17,4,12,Hex(0x566c64)); c.Rect(25,18,2,8,Hex(0xa3cf68));
                for(int i=0;i<3;i++){int x=11+i*4;c.Rect(x,28,3,5,Hex(0x768a81));c.Rect(x+1,27,1,2,ivory);c.Rect(x+1,30,1,2,i==1?Hex(0xd590b1):Mint);}
                c.Line(13,20,17,24,Hex(0x6b945a)); c.Line(17,24,22,21,Hex(0x6b945a)); c.Rect(26,24,4,4,ivory);
            }
            if(frame==2) { c.Rect(4,22,4,3,ivory); c.Rect(25,21,4,3,ivory); }
            c.Outline(Ink);
            if(kind==3)for(int i=0;i<3;i++){int x=4+(frame+i*2)%5,y=11-i*3-frame%2;c.Ellipse(x,y,4-i,3,Alpha(Hex(0x9db986),(byte)(125-i*25)));}
            return c;
        }

        static readonly Color32 GraveIvory=Hex(0xdcd7c7), GraveShade=Hex(0x8d8291), BloodRed=Hex(0x9b3a51), DeepWound=Hex(0x462438);

        static bool IsGuardian(string id) => id=="boss"||id=="widow"||id=="bishop"||id=="matriarch"||id=="bell"||id=="iron-executioner"||id=="bog-oracle";

        static void NightmareMaw(Canvas c,int x,int y,int width,int height,int frame,bool vertical=false)
        {
            height += frame==2?1:0;
            c.Polygon(new[]{x,y+2,x+width/3,y,x+width-2,y+1,x+width,y+height/2,x+width-2,y+height,x+2,y+height-1},DeepWound);
            c.Polygon(new[]{x+1,y+2,x+width/3,y+1,x+width-2,y+2,x+width-1,y+height/2,x+width-3,y+height-1,x+2,y+height-2},Ink);
            int count=Math.Max(2,width/4);
            for(int i=0;i<count;i++)
            {
                int px=x+2+i*(width-4)/count, length=2+(i+frame)%2;
                c.Polygon(new[]{px,y+1,px+2,y+1,px+1,y+length+1},GraveIvory);
                c.Polygon(new[]{px+1,y+height-2,px+3,y+height-2,px+2,y+height-4-(i+frame)%2},GraveIvory);
            }
            if(vertical)c.Line(x+width/2,y+height/2,x+width/2+1,y+height-3,BloodRed);
            else c.Line(x+3,y+height-2,x+width-4,y+height-2,BloodRed);
        }

        static void HollowEyes(Canvas c,int x,int y,int gap,Color32 glow)
        {
            c.Polygon(new[]{x-1,y-1,x+4,y,x+3,y+3,x,y+3},GraveIvory);
            c.Polygon(new[]{x+gap-1,y,x+gap+4,y-1,x+gap+4,y+3,x+gap,y+3},GraveIvory);
            c.Line(x,y+1,x+3,y+1,Ink); c.Line(x+gap,y+1,x+gap+3,y+1,Ink);
            c.Set(x+1,y+1,glow); c.Set(x+gap+2,y+1,glow);
        }

        static void ExposedRibs(Canvas c,int x,int y,int width,int rows)
        {
            c.Rect(x,y,width,rows*3,DeepWound); c.Line(x+width/2,y,x+width/2,y+rows*3-1,GraveIvory);
            for(int i=0;i<rows;i++){int py=y+i*3;c.Line(x,py,x+width/2-2,py+1,GraveIvory);c.Line(x+width/2+2,py+1,x+width-1,py,GraveIvory);}
        }

        static Canvas NightmareCreature(string id,Canvas c,int frame)
        {
            int twitch=frame==1?1:frame==3?-1:0;
            switch(id)
            {
                case "pumpkin":
                    c.Line(9,5,8,10,DeepWound);c.Line(8,10,11,13,DeepWound);
                    c.Polygon(new[]{7,8,10,10,8,12,5,11},Ink);c.Polygon(new[]{15,10,18,8,20,11,16,12},Ink);
                    c.Set(7,10,Gold);c.Set(17,10,Gold);NightmareMaw(c,7,13,12,8,frame,true);
                    c.Line(3,16,1,19+twitch,GraveIvory);c.Line(20,15,23,19-twitch,GraveIvory);break;
                case "ghost":
                    c.Polygon(new[]{8,6,17,6,19,13,16,21,10,21,6,13},Hex(0xcbc5d0));
                    c.Polygon(new[]{9,8,12,9,11,12,8,12},Ink);c.Polygon(new[]{15,9,18,8,18,12,15,12},Ink);
                    c.Set(9,10,Hex(0xe389a8));c.Set(16,10,Hex(0xe389a8));NightmareMaw(c,10,12,7,11+twitch,frame,true);
                    c.Line(4,16,1,23+twitch,GraveIvory);c.Line(19,16,22,23-twitch,GraveIvory);
                    c.Rect(7,23,2,3,Ink);c.Rect(15,24,2,3,Ink);break;
                case "cultist":
                    c.Polygon(new[]{8,9,17,9,18,15,13,18,7,15},GraveIvory);
                    c.Line(8,11,11,12,Ink);c.Line(14,12,17,11,Ink);c.Set(10,12,Hex(0xd86b8e));c.Set(15,12,Hex(0xd86b8e));
                    NightmareMaw(c,10,13,6,6,frame,true);c.Line(12,3,13,7,GraveIvory);
                    c.Line(4,19,1,25+twitch,GraveIvory);c.Line(20,18,23,25-twitch,GraveIvory);c.Rect(7,24,3,3,Ink);break;
                case "bat":
                    c.Rect(11,8,7,5,Hex(0x572b44));HollowEyes(c,11,8,4,Hex(0xf8ab89));NightmareMaw(c,11,12,7,6,frame);
                    c.Line(5,9,7,13,GraveIvory);c.Line(22,9,20,13,GraveIvory);c.Polygon(new[]{3,8,6,11,3,12},Ink);break;
                case "larva":
                    for(int i=0;i<4;i++){int x=3+i*5;c.Line(x,14,x+1,19,Hex(0xc2b9a0));c.Line(x,14,x-2,10-twitch,GraveIvory);}
                    c.Ellipse(17,8,10,12,Hex(0x8a9c7b));NightmareMaw(c,19,10,7,11,frame,true);
                    c.Set(18,9,Hex(0xe991a2));c.Set(25,8,Hex(0xe991a2));break;
                case "sentinel":
                    c.Polygon(new[]{8,3,14,1,22,4,21,12,16,17,10,14},GraveIvory);
                    HollowEyes(c,9,5,8,Hex(0xffac85));NightmareMaw(c,11,10,9,7,frame);
                    ExposedRibs(c,10,17,10,2);c.Line(9,13,5,9-twitch,GraveIvory);c.Line(20,13,24,9+twitch,GraveIvory);break;
                case "acolyte":
                    c.Polygon(new[]{11,10,17,9,19,13,16,19,11,17},GraveIvory);c.Rect(12,11,5,3,Ink);c.Set(14,12,Hex(0xf19b9c));
                    NightmareMaw(c,12,14,6,7,frame,true);c.Line(9,20,7,26,GraveIvory);c.Line(20,20,22,26,GraveIvory);
                    c.Line(1,8,6,11,DeepWound);c.Set(2+frame%2,9,GraveIvory);break;
                case "skeleton":
                    c.Polygon(new[]{8,3,14,1,20,4,19,11,13,15,8,11},GraveIvory);HollowEyes(c,9,5,7,Hex(0xff9b9b));
                    NightmareMaw(c,10,10,11,8+twitch,frame,true);c.Line(11,4,14,6,Hex(0x666079));
                    ExposedRibs(c,9,17,12,2);c.Line(8,15,4,11+twitch,GraveIvory);c.Line(20,15,23,12-twitch,GraveIvory);break;
                case "slime":
                    c.Polygon(new[]{4,10,8,7,12,9,17,4,24,11,25,17,20,20,8,20,2,17},Hex(0x788275));
                    c.Ellipse(4,8,10,8,GraveIvory);c.Rect(6,10,3,3,Ink);c.Line(16,8,21,10,DeepWound);
                    NightmareMaw(c,8,10+twitch,16,11,frame);c.Line(3,15,5,18,BloodRed);c.Line(24,15,22,20,BloodRed);break;
                case "spider":
                    c.Polygon(new[]{11,2,19,1,24,7,20,13,12,13,8,7},GraveIvory);
                    c.Rect(12,5,3,4,Ink);c.Rect(18,4,4,4,Ink);c.Set(13,6,Hex(0xe7799d));c.Set(20,5,Hex(0xe7799d));
                    NightmareMaw(c,11,13,12,9,frame);c.Line(10,17,6,23-twitch,GraveIvory);c.Line(22,17,26,23+twitch,GraveIvory);break;
                case "lantern-wisp":
                    c.Rect(9,10,14,12,Hex(0x6a453d));c.Polygon(new[]{11,11,20,11,21,16,19,21,13,21,10,16},GraveIvory);
                    c.Rect(12,13,3,3,Ink);c.Rect(18,13,3,3,Ink);c.Set(13,14,Orange);c.Set(19,14,Orange);
                    NightmareMaw(c,13,17,7,6,frame,true);c.Line(9,24,8,30+twitch,GraveIvory);c.Line(21,24,23,30-twitch,GraveIvory);
                    c.Line(6,7,3,4,GraveIvory);c.Line(24,7,27,4,GraveIvory);break;
                case "mushroom-brute":
                    c.Polygon(new[]{5,12,10,5,16,2,25,4,32,12,27,16,10,16},Hex(0x80506d));
                    c.Line(12,5,13,11,GraveIvory);c.Line(22,5,24,12,GraveIvory);c.Ellipse(18,4,5,4,DeepWound);
                    c.Polygon(new[]{11,17,26,17,27,23,22,30,14,29,10,23},GraveIvory);HollowEyes(c,12,19,10,Hex(0xa1df86));
                    NightmareMaw(c,14,23,12,9,frame,true);c.Line(3,26,1,31-twitch,GraveIvory);c.Line(32,25,35,30+twitch,GraveIvory);break;
                case "clockwork-imp":
                    c.Polygon(new[]{10,7,18,5,25,8,24,15,19,20,11,16},Hex(0x98938e));HollowEyes(c,11,9,9,Hex(0xff9d92));
                    NightmareMaw(c,13,13,11,8,frame);c.Line(17,15,18,22,DeepWound);
                    c.Polygon(new[]{10,7,8,1,13,6},GraveIvory);c.Polygon(new[]{23,7,27,0,26,9},GraveIvory);
                    c.Line(4,23,1,27+twitch,GraveIvory);c.Line(29,23,32,27-twitch,GraveIvory);break;
                case "scarecrow":
                    c.Line(12,12,10,17,DeepWound);c.Line(10,17,13,20,DeepWound);
                    c.Polygon(new[]{11,12,16,14,12,17},Ink);c.Polygon(new[]{24,12,19,14,23,17},Ink);c.Set(12,14,Gold);c.Set(22,14,Gold);
                    NightmareMaw(c,13,17,11,9,frame,true);c.Line(12,25,9,34,GraveIvory);c.Line(23,25,26,34,GraveIvory);
                    c.Line(2,28,0,23+twitch,GraveIvory);c.Line(32,27,35,22-twitch,GraveIvory);c.Rect(16,29,4,6,DeepWound);break;
                case "gargoyle":
                    c.Polygon(new[]{14,11,19,8,27,11,28,19,21,25,15,21},Hex(0x9a9fa0));HollowEyes(c,15,12,9,Hex(0xb4dfbf));
                    NightmareMaw(c,17,18,11,10,frame);c.Line(13,10,11,2,GraveIvory);c.Line(28,10,31,1,GraveIvory);
                    ExposedRibs(c,15,25,13,2);c.Line(7,12,10,17,GraveIvory);c.Line(35,12,32,17,GraveIvory);break;
                case "stalker-shadow":
                    c.Polygon(new[]{10,9,15,5,23,8,24,17,20,24,13,22,9,16},Hex(0x0b0f1f));
                    c.Line(11,10,14,11,GraveIvory);c.Line(19,11,23,9,GraveIvory);c.Set(12,11,Mint);c.Set(21,10,Mint);
                    NightmareMaw(c,13,14,9,13,frame,true);c.Line(7,23,2,31+twitch,GraveIvory);c.Line(25,22,30,30-twitch,GraveIvory);
                    c.Line(10,28,8,35,Hex(0xb497c4));c.Line(22,28,25,35,Hex(0xb497c4));break;
                case "fire-bat":
                    c.Polygon(new[]{14,11,18,8,24,11,25,19,21,25,15,22},Hex(0xbdb0a0));HollowEyes(c,15,13,6,Gold);
                    NightmareMaw(c,16,17,9,9,frame,true);c.Line(7,11,12,16,GraveIvory);c.Line(30,11,25,16,GraveIvory);
                    c.Polygon(new[]{5,12,8,15,5,17},Ink);c.Polygon(new[]{32,12,29,15,32,17},Ink);break;
                case "miniboss":
                    c.Polygon(new[]{16,10,24,6,34,12,33,23,26,30,18,25},GraveIvory);HollowEyes(c,18,14,10,Hex(0xb1e0cf));
                    NightmareMaw(c,20,20,13,12,frame,true);ExposedRibs(c,17,31,17,3);
                    c.Polygon(new[]{11,26,8,17,16,24},GraveIvory);c.Polygon(new[]{37,25,41,15,40,29},GraveIvory);break;
                default:return c;
            }
            // One moving tear/bone fragment per pose makes old two-pose creatures
            // expressive as well, without adding decorative texture noise.
            int markX=Math.Max(1,c.Width/2+twitch),markY=c.Height-3-frame;
            c.Set(markX,markY,GraveIvory);c.Outline(Ink);return c;
        }

        static Canvas NightmareGuardian(string id,Canvas c,int frame)
        {
            int motion=frame==1?1:frame==3?-1:0;
            switch(id)
            {
                case "boss":
                    c.Polygon(new[]{10,15,15,10,21,11,18,22,13,23},Hex(0xae4c2e));c.Line(12,12,11,21,DeepWound);
                    c.Polygon(new[]{12,16,21,18,18,22,12,21},Ink);c.Polygon(new[]{35,16,26,18,29,22,35,21},Ink);
                    c.Line(14,19,18,19,Gold);c.Line(29,19,33,19,Gold);NightmareMaw(c,17,22,15,14,frame,true);
                    c.Line(12,31,7,39+motion,GraveIvory);c.Line(35,31,42,39-motion,GraveIvory);
                    c.Polygon(new[]{7,41,10,33,12,38},GraveIvory);c.Polygon(new[]{39,40,36,33,43,37},GraveIvory);break;
                case "widow":
                    c.Polygon(new[]{22,12,29,10,35,16,35,28,29,34,22,27},GraveIvory);
                    c.Rect(23,16,11,6,Ink);c.Set(24,17,Hex(0xe4adc5));c.Set(28,19,Hex(0xe4adc5));c.Set(32,16,Hex(0xe4adc5));
                    NightmareMaw(c,25,21,10,17,frame,true);c.Line(20,14,17,27,DeepWound);c.Line(36,18,40,30,DeepWound);
                    c.Polygon(new[]{19,35,22,41,19,48},Ink);c.Polygon(new[]{37,34,35,43,39,47},Ink);
                    c.Line(12,32,8,26+motion,GraveIvory);c.Line(44,32,48,26-motion,GraveIvory);break;
                case "bishop":
                    c.Polygon(new[]{21,15,29,14,35,19,34,28,28,35,21,28},GraveIvory);HollowEyes(c,22,19,8,Hex(0xffac8b));
                    NightmareMaw(c,25,24,9,12,frame,true);c.Line(24,17,29,21,DeepWound);
                    ExposedRibs(c,23,36,13,3);c.Line(21,10,20,4,GraveIvory);c.Line(34,10,35,3,GraveIvory);
                    c.Ellipse(44,6-motion,9,9,GraveIvory);c.Rect(46,8-motion,2,3,Ink);c.Rect(50,8-motion,2,3,Ink);c.Rect(48,11-motion,2,3,DeepWound);break;
                case "matriarch":
                    c.Polygon(new[]{15,15,22,11,28,13,36,10,42,17,39,28,32,33,21,30},Hex(0xa9b4a0));
                    c.Rect(18,16,6,5,Ink);c.Rect(28,15,4,4,Ink);c.Rect(35,15,6,6,Ink);
                    c.Set(21,18,Gold);c.Set(29,16,Gold);c.Set(38,17,Gold);NightmareMaw(c,20,23,20,13,frame,true);
                    c.Ellipse(20,35,18,15,DeepWound);ExposedRibs(c,21,36,16,3);c.Ellipse(25,42+motion,6,7,Hex(0xa3bf89));
                    c.Line(12,28,6,20,GraveIvory);c.Line(44,28,50,19,GraveIvory);break;
                case "bell":
                    c.Polygon(new[]{20,21,28,18,37,22,38,34,31,41,21,36},Hex(0x807278));
                    c.Polygon(new[]{21,24,27,26,24,30,20,28},Ink);c.Polygon(new[]{36,24,30,26,33,30,38,28},Ink);
                    c.Set(23,27,Hex(0xe7bad7));c.Set(34,27,Hex(0xe7bad7));NightmareMaw(c,25,29,10,14,frame,true);
                    c.Line(14,20,16,29,Hex(0x5b3e52));c.Line(16,29,13,36,Hex(0x5b3e52));
                    c.Rect(25+motion,46,6,6,GraveIvory);c.Rect(27+motion,48,2,3,DeepWound);break;
                case "iron-executioner":
                    c.Polygon(new[]{20,13,29,10,39,14,40,25,33,33,23,28},Hex(0xa3a4a6));
                    c.Polygon(new[]{23,18,36,16,35,21,24,23},Ink);c.Line(26,20,34,18,Hex(0xffb38a));
                    c.Rect(28,23,6,8,DeepWound);for(int x=28;x<35;x+=3)c.Rect(x,24,1,5,GraveIvory);
                    c.Line(19,15,17,7,GraveIvory);c.Line(41,17,44,8,GraveIvory);
                    c.Polygon(new[]{21,36,39,36,42,54,36,50,32,57,27,52,20,55},Hex(0x633443));
                    c.Line(27,39,28,49,BloodRed);c.Line(34,39,36,50,BloodRed);c.Polygon(new[]{56,14,60,16,56,20},Ink);break;
                case "bog-oracle":
                    c.Polygon(new[]{13,15,22,11,29,16,38,11,49,16,46,29,37,34,23,32,15,25},Hex(0x9caa8e));
                    c.Polygon(new[]{15,16,24,16,22,23,15,22},Ink);c.Polygon(new[]{38,16,47,15,47,22,39,23},Ink);
                    c.Line(17,19,21,19,Hex(0xc5e2a0));c.Line(40,18,44,18,Hex(0xc5e2a0));
                    NightmareMaw(c,24,24,18,20,frame,true);c.Line(15,26,18,32,DeepWound);c.Line(46,24,45,32,DeepWound);
                    c.Line(17,13,10,6,GraveIvory);c.Line(10,6,9,1,GraveIvory);c.Line(45,13,54,6,GraveIvory);c.Line(54,6,57,1,GraveIvory);
                    c.Ellipse(10,34+motion,7,7,Hex(0x838e75));c.Set(12,35+motion,GraveIvory);break;
                default:return c;
            }
            c.Outline(Ink);return c;
        }

        static void NightmareAttackFace(string id,Canvas c,int frame)
        {
            int cx=c.Width/2,cy=c.Height/2;
            if(id=="boss")NightmareMaw(c,cx-8,cy+1,16,11,frame,true);
            else if(id=="widow")NightmareMaw(c,cx-4,cy-5,9,14,frame,true);
            else if(id=="bog-oracle")NightmareMaw(c,cx-7,cy-6,17,17,frame,true);
        }

        static Canvas VolumeProp(string id,Canvas source)
        {
            bool themed=id!=null&&id.StartsWith("decor-theme",StringComparison.Ordinal);
            if(!themed&&id!="barrel-explosive"&&id!="barrel-acid"&&id!="water-urn"&&id!="destructible-pillar"&&id!="gravestone"&&id!="grave"&&id!="fence")return source;
            var c=new Canvas(source.Width,source.Height);
            c.Ellipse(2,source.Height-6,source.Width-4,5,Alpha(Ink,105));
            // One stable directional key light and a separate right-hand plane.
            for(int y=0;y<source.Height;y++)for(int x=0;x<source.Width;x++)
            {
                Color32 p=source.Pixels[(source.Height-1-y)*source.Width+x];if(p.a==0)continue;
                float plane=x>source.Width*.67f?.70f:x<source.Width*.38f?1.07f:.94f;
                if(p.r+p.g+p.b<115||p.a<200)plane=1;
                c.Set(x,y,new Color32((byte)Math.Min(255,p.r*plane),(byte)Math.Min(255,p.g*plane),(byte)Math.Min(255,p.b*plane),p.a));
            }
            if(id=="barrel-explosive")
            {
                c.Ellipse(6,4,20,8,Hex(0x3a2c31));c.Ellipse(7,4,18,6,Hex(0xa47d55));c.Ellipse(9,5,14,4,Hex(0x5c4037));
                c.Line(10,6,22,6,Hex(0x886047));c.Line(7,5,16,4,GraveIvory);c.Line(5,16,5,27,Hex(0xb18b61));
                c.Ellipse(4,30,24,5,Hex(0x33303e));c.Line(6,30,24,30,Hex(0x8d9199));c.Line(7,32,21,33,Hex(0x565766));
            }
            else if(id=="barrel-acid")
            {
                c.Ellipse(4,4,24,9,Hex(0x34464c));c.Ellipse(5,4,21,7,Hex(0xaab6ac));c.Ellipse(7,5,17,5,Hex(0x93db72));
                c.Line(8,5,19,5,Hex(0xd9f3bb));c.Line(6,15,6,26,Hex(0xb2bfb7));c.Polygon(new[]{23,17,27,19,25,26,22,23},Hex(0x354750));
                c.Ellipse(4,30,24,5,Hex(0x37444c));c.Line(6,30,23,30,Hex(0x9daaa7));
            }
            else if(id=="water-urn")
            {
                c.Ellipse(8,3,14,7,Hex(0x3e5d73));c.Ellipse(9,3,12,5,Hex(0xb0c6c5));c.Ellipse(11,4,8,3,Hex(0x25465f));
                c.Polygon(new[]{19,12,23,15,23,27,19,30,20,21},Hex(0x2e5772));c.Line(9,13,8,21,Hex(0xc3dddd));
                c.Ellipse(9,28,12,4,Hex(0x2f4b60));c.Line(10,28,19,28,Hex(0x81a9b3));
            }
            else if(id=="destructible-pillar")
            {
                c.Polygon(new[]{4,5,9,1,25,1,29,5,24,8,7,8},Hex(0x8f96a0));c.Line(9,1,24,1,GraveIvory);
                c.Polygon(new[]{24,8,28,5,28,11,25,12,25,37,22,37,22,11},Hex(0x41485c));
                c.Polygon(new[]{2,40,7,36,25,36,30,40,26,43,4,43},Hex(0x656e80));c.Line(7,36,24,36,Hex(0xb0b4b9));c.Line(4,40,26,40,Hex(0x858d9d));
                c.Line(9,12,9,33,Hex(0xc0c3c2));
            }
            else if(id=="gravestone"||id=="grave")
            {
                int w=c.Width,h=c.Height;c.Polygon(new[]{w-9,4,w-4,8,w-4,h-7,w-8,h-5,w-8,9},Hex(0x414b61));
                c.Line(7,7,6,h-9,Hex(0xc0c4c5));c.Polygon(new[]{2,h-6,6,h-9,w-5,h-9,w-1,h-6,w-4,h-3,3,h-3},Hex(0x626e80));c.Line(6,h-9,w-6,h-9,Hex(0xadb5ba));
            }
            else if(id=="fence")
            {
                for(int x=5;x<46;x+=9){c.Polygon(new[]{x-1,10,x+1,3,x+1,10},GraveIvory);c.Rect(x+2,11,1,20,Hex(0x323849));c.Line(x,11,x,29,Hex(0xa3a7ae));}
                c.Line(3,17,44,17,Hex(0x6d7182));
            }
            else if(themed)
            {
                int theme=id[id.Length-1]-'0';
                if(theme==0){c.Ellipse(17,36,17,7,Hex(0x352c33));c.Ellipse(18,36,14,4,Hex(0x8d7054));c.Line(21,37,29,37,Hex(0xc0a07a));c.Line(20,20,19,31,Hex(0xaf8b69));}
                else if(theme==1){c.Polygon(new[]{7,8,14,2,34,2,41,8,34,11,13,11},Hex(0x9fa9bd));c.Line(14,2,33,2,GraveIvory);c.Polygon(new[]{36,11,41,8,43,43,38,44},Hex(0x404b65));c.Line(10,19,10,38,GraveIvory);}
                else if(theme==2){c.Polygon(new[]{6,27,11,23,37,23,42,27,37,31,10,31},Hex(0xac8271));c.Line(12,24,35,24,GraveIvory);c.Polygon(new[]{37,31,42,27,42,41,37,44},Hex(0x3d2c40));c.Line(11,32,11,40,Hex(0xb38b79));}
                else if(theme==3){c.Ellipse(4,38,40,7,Hex(0x253c35));for(int i=0;i<4;i++){int x=7+i*8,y=16+i%2*8;c.Line(x+3,y+3,x+2,y+12,Hex(0xc1c79c));c.Line(x+11,y+5,x+12,y+15,Hex(0x334b3f));}c.Line(6,40,32,42,Hex(0x718a64));}
                else{c.Polygon(new[]{6,42,11,38,38,38,43,42,38,46,9,46},Hex(0x746277));c.Line(12,39,36,39,GraveIvory);c.Polygon(new[]{38,9,42,7,42,41,38,43},Hex(0x33283f));c.Line(8,11,8,37,Hex(0xb59aa9));}
            }
            return c;
        }

        static Canvas RitualAmmoCache()
        {
            var c=new Canvas(28,26);
            c.Ellipse(2,20,24,5,Alpha(Ink,105));c.Polygon(new[]{3,8,9,3,24,3,26,8,26,21,20,24,3,21},Hex(0x343647));
            c.Polygon(new[]{3,8,9,3,24,3,19,8},Hex(0x818277));c.Line(9,3,23,3,GraveIvory);
            c.Rect(4,9,15,12,Hex(0x60685f));c.Polygon(new[]{20,8,25,4,25,21,20,24},Hex(0x292e40));
            c.Rect(5,11,13,2,Hex(0xa1977b));c.Rect(5,19,13,2,Hex(0x8e8976));c.Rect(10,10,3,13,BloodRed);
            c.Ellipse(8,12,8,7,GraveIvory);c.Rect(9,14,2,2,Ink);c.Rect(13,14,2,2,Ink);c.Rect(11,17,2,2,Ink);
            c.Rect(8,4,3,4,Gold);c.Set(9,4,GraveIvory);c.Rect(14,3,3,4,Hex(0xb7b6a3));c.Set(15,3,Pale);
            c.Outline(Ink);return c;
        }

        static Canvas Scarecrow(int frame)
        {
            var c=new Canvas(36,42); int sway=frame==1?2:frame==3?-2:0;
            Color32 straw=Hex(0xdfb365), cloth=Hex(0x65715b);
            c.Line(17,22,15,37+sway,Hex(0x6c5147)); c.Line(21,22,24,37-sway,Hex(0x6c5147));
            c.Rect(11,37+sway,6,3,Hex(0x8c6b4d)); c.Rect(22,37-sway,7,3,Hex(0x8c6b4d));
            c.Rect(4,22+sway,28,3,Hex(0x6e544b)); c.Line(5,22+sway,30,22+sway,Gold);
            c.Polygon(new[]{10,19,24,19,27,33,22,31,18,35,12,32,7,33},cloth);
            c.Polygon(new[]{13,20,19,20,18,31,13,30},Hex(0x93a277)); c.Line(21,20,24,30,Hex(0x3f534d));
            c.Rect(10,27,15,3,Hex(0x715044)); c.Rect(16,27,4,4,Gold); c.Rect(17,28,2,2,Ink);
            c.Line(10,22,4,28+sway,cloth); c.Line(24,22,31,27-sway,cloth);
            for(int i=0;i<3;i++) { c.Line(3,26+sway,1+i*2,32+sway,straw); c.Line(31,25-sway,31+i,32-sway,straw); }
            // A carved pumpkin, with curved orange lobes and amber light under the hat.
            c.Ellipse(9,8,18,15,Hex(0x9e452d)); c.Ellipse(10,8,15,14,Orange);
            c.Ellipse(11,9,4,12,Hex(0xffb24e)); c.Ellipse(20,9,5,12,Hex(0xd6632c));
            c.Line(17,9,16,12,Hex(0xa9492a)); c.Line(17,18,18,21,Hex(0xb45128));
            c.Line(23,11,24,16,Hex(0x9e452d)); c.Line(12,10,11,13,Gold);
            c.Polygon(new[]{11,12,15,14,11,16},Ink); c.Polygon(new[]{24,12,20,14,24,16},Ink);
            c.Rect(12,14,3,1,Gold); c.Rect(21,14,3,1,Gold); c.Set(12,14,Pale); c.Set(23,14,Pale);
            c.Polygon(new[]{17,15,19,15,18,18},Hex(0x82362b));
            c.Polygon(new[]{11,18,14,19,17,18,19,20,22,18,25,18,23,22,14,22},Ink);
            c.Line(14,20,22,20,Gold); c.Set(16,19,Orange); c.Set(20,21,Orange);
            c.Polygon(new[]{7,9,11,5,12,1,22,3,25,8,30,10,27,12,8,12},Hex(0x47384b));
            c.Polygon(new[]{12,6,14,2,20,4,21,7},Hex(0x756277)); c.Line(10,9,25,9,Hex(0xae8658));
            c.Line(25,6,28,3,straw); c.Set(29,2,Gold); c.Rect(11,29,4,3,Hex(0x7f8a69)); c.Set(12,29,Bone);
            if(frame==2) { c.Rect(11,14,4,2,Gold); c.Rect(21,14,4,2,Gold); c.Line(2,26,0,20,straw); c.Line(32,26,35,20,straw); }
            c.Outline(Ink); return c;
        }

        static Canvas Gargoyle(int frame)
        {
            var c=new Canvas(42,38); int wing=frame==1?2:frame==2?-3:frame==3?1:0;
            Color32 rock=Hex(0x697784), lit=Hex(0x9ea9aa), dark=Hex(0x3e485c);
            c.Polygon(new[]{14,16,7,8,1,3+wing,3,18,9,22,14,24},dark);
            c.Polygon(new[]{27,16,34,8,40,3+wing,38,18,32,22,27,24},dark);
            c.Line(2,5+wing,12,16,lit); c.Line(39,5+wing,29,16,lit);
            c.Line(5,12,8,19,rock); c.Line(36,12,33,19,rock);
            c.Polygon(new[]{12,18,28,18,31,30,24,33,16,33,10,29},rock);
            c.Ellipse(14,17,13,14,lit); c.Rect(20,19,6,10,Hex(0x758592));
            c.Line(14,23,8,30,rock); c.Line(28,23,34,30,dark);
            c.Rect(7,28,7,7,rock); c.Rect(28,28,7,7,rock);
            for(int i=0;i<3;i++) { c.Line(7+i*3,33,6+i*3,36,Bone); c.Line(29+i*3,33,29+i*3,36,Bone); }
            c.Polygon(new[]{12,15,12,4,17,9,24,8,29,3,29,16,25,23,17,23},rock);
            c.Polygon(new[]{14,10,18,9,21,11,25,9,27,11,26,17,16,17},lit);
            c.Rect(15,13,4,4,Ink); c.Rect(23,13,4,4,Ink); c.Set(16,14,Mint); c.Set(24,14,Mint);
            c.Polygon(new[]{18,17,24,17,25,21,17,21},dark); c.Rect(18,18,2,3,Bone); c.Rect(23,18,2,3,Bone);
            c.Line(28,27,36,26,dark); c.Line(36,26,38,22,rock); c.Polygon(new[]{36,21,40,19,40,25},lit);
            c.Line(22,7,21,11,dark); c.Line(21,11,24,13,dark); c.Set(15,11,Pale);
            if(frame==2) { c.Line(13,25,11,19,lit); c.Line(28,25,31,19,lit); c.Rect(16,14,3,2,Pale); c.Rect(24,14,3,2,Pale); }
            c.Outline(Ink); return c;
        }

        static Canvas StalkerShadow(int frame)
        {
            var c=new Canvas(32,38); int sway=frame==1?2:frame==3?-2:0;
            c.Polygon(new[]{10,12,21,11,24,20,27+sway,31,22,30,24,36,17,32,13,37,10,32,5+sway,35,8,25},Hex(0x39324f));
            c.Polygon(new[]{11,15,18,13,19,25,21+sway,31,16,29,12,33,10,26},Alpha(Hex(0x675486),235));
            c.Line(8,23,3,28+sway,Hex(0x9375b7)); c.Line(23,21,29,27-sway,Hex(0x69518e));
            for(int i=0;i<3;i++) { c.Line(3+i,26+sway,i,31+sway,Mint); c.Line(28+i,26-sway,30+i,30-sway,Hex(0xb9a6dd)); }
            c.Polygon(new[]{8,14,10,6,16,1,23,7,25,15,20,20,12,20},Hex(0x4c3b66));
            c.Polygon(new[]{10,13,13,6,16,4,19,7,22,13,21,17,11,17},Ink);
            c.Rect(11,11,4,3,Mint); c.Rect(19,11,4,3,Mint); c.Set(12,11,Pale); c.Set(20,11,Pale);
            c.Line(14,17,18,17,Hex(0x83669d)); c.Set(16,19,Violet);
            c.Line(10,7,15,3,Hex(0xb292ce)); c.Line(8,24,7+sway,31,Hex(0xb292ce));
            if(frame==2) { c.Rect(11,10,5,4,Pale); c.Rect(19,10,5,4,Pale); c.Line(5,25,1,21,Mint); c.Line(27,24,31,20,Mint); }
            c.Outline(Ink); return c;
        }

        static Canvas FireBat(int frame)
        {
            var c=new Canvas(38,30); int tip=frame==0?4:frame==1?10:frame==2?17:7;
            Color32 ember=Hex(0x9c3f4c), hot=Hex(0xf48c48), wing=Hex(0xc65f4d);
            c.Polygon(new[]{14,14,8,9,1,tip,3,17,7,19,10,22,15,18},ember);
            c.Polygon(new[]{23,14,29,9,36,tip,34,17,30,19,27,22,22,18},ember);
            c.Line(2,tip+1,13,15,Gold); c.Line(35,tip+1,24,15,Gold);
            c.Polygon(new[]{4,tip+3,9,12,13,16,10,19,7,17},wing);
            c.Polygon(new[]{33,tip+3,28,12,24,16,27,19,30,17},wing);
            c.Polygon(new[]{13,15,14,8,17,4,19,10,22,4,24,12,25,19,22,24,16,24},Hex(0x6d394c));
            c.Ellipse(15,11,9,11,hot); c.Rect(15,13,3,3,Ink); c.Rect(21,13,3,3,Ink);
            c.Set(16,14,Gold); c.Set(22,14,Gold); c.Rect(18,19,3,2,Bone);
            c.Polygon(new[]{17,9,15,5,18,1+frame%2,21,7,23,3,24,9,21,12},Orange);
            c.Polygon(new[]{18,8,18,4,20,7,21,10},Gold); c.Set(19,7,Pale);
            c.Line(17,23,15-frame%2,28,Orange); c.Line(22,23,24+frame%2,27,Gold);
            c.Set(3+frame*2,3+frame%2,Orange); c.Set(33-frame*2,5-frame%2,Gold);
            c.Outline(Ink); return c;
        }

        static Canvas HalloweenBarrel(bool acid, int frame)
        {
            var c=new Canvas(32,38); int drip=frame==1?2:frame==2?4:frame==3?1:0;
            if(!acid)
            {
                c.Polygon(new[]{7,7,24,7,28,15,28,28,24,34,7,34,3,27,3,15},Hex(0x503c3b));
                c.Polygon(new[]{8,7,12,7,10,33,7,32,5,26,5,15},Hex(0x8c6047));
                c.Polygon(new[]{14,8,20,8,22,33,12,33},Hex(0x72513e));
                c.Polygon(new[]{22,8,24,9,26,16,26,27,23,32,22,22},Hex(0x3b3036));
                c.Line(11,9,9,31,Hex(0x302b32)); c.Line(19,9,21,32,Hex(0x302b32));
                c.Line(7,16,6,23,Hex(0xb18156)); c.Line(15,11,14,17,Hex(0x9e7050)); c.Line(17,25,18,31,Hex(0x9e7050));
                c.Ellipse(6,4,20,8,Hex(0x9d704f)); c.Ellipse(8,5,16,5,Hex(0x4d3a37));
                c.Line(9,7,22,7,Hex(0x7a5641)); c.Ellipse(20,5,4,3,Shade);
                c.Rect(4,13,24,4,Hex(0x636570)); c.Line(5,13,26,13,Hex(0xaab0b0));
                c.Rect(4,28,24,4,Hex(0x535763)); c.Line(5,28,26,28,Hex(0x9d9e9f));
                c.Set(7,15,Bone); c.Set(24,15,Bone); c.Set(7,30,LightStone); c.Set(24,30,LightStone);
                c.Ellipse(11,18,11,10,Bone); c.Rect(13,20,3,3,Ink); c.Rect(18,20,3,3,Ink); c.Rect(16,23,2,3,Ink);
                c.Line(23,6,24,2,Gold); c.Set(24,2,Orange); c.Set(23+frame%3,frame%2,Pale);
                c.Set(27-frame%2,2+frame%3,Orange); c.Set(20+frame%3,1+frame%2,Gold);
            }
            else
            {
                c.Polygon(new[]{6,7,25,7,27,12,28,27,25,34,7,34,3,27,4,15},Hex(0x4d5964));
                c.Rect(6,9,19,23,Hex(0x7e8b91)); c.Rect(7,9,5,22,Hex(0xa2b0ad));
                c.Polygon(new[]{21,10,25,11,26,27,23,32,21,24},Hex(0x46545d));
                c.Polygon(new[]{5,18,12,19,10,24,5,25},Hex(0x4a5b66)); c.Line(6,19,11,20,Hex(0xb7c1b5));
                c.Ellipse(4,4,24,9,Hex(0xb2bdb4)); c.Ellipse(6,5,20,6,Hex(0x2d5f46));
                c.Ellipse(7,5,18,5,Hex(0x9bec57)); c.Line(9,5,22,5,Hex(0xddffab));
                c.Rect(4,12,24,3,Hex(0x505e6a)); c.Line(5,12,26,12,Hex(0xbfc5bd));
                c.Rect(4,28,24,4,Hex(0x4c5966)); c.Line(5,28,26,28,LightStone);
                c.Polygon(new[]{16,17,23,26,9,26},Hex(0xc4ca7a)); c.Rect(15,20,3,3,Ink); c.Set(16,24,Ink);
                c.Rect(7,8,3,9+drip,Hex(0x89dc4e)); c.Rect(8,8,1,6+drip,Mint);
                c.Rect(23,9,3,6+drip/2,Hex(0x9bec57)); c.Set(24,16+drip/2,Hex(0xb4f576));
                c.Ellipse(11+frame*3,2+frame%2,3,3,Alpha(Mint,190)); c.Set(9+frame*4,1,Pale);
            }
            c.Outline(Ink); return c;
        }

        // Four damage poses are available; callers can choose a pose from remaining HP.
        static Canvas DestructiblePillar(int frame)
        {
            var c=new Canvas(32,44); Color32 rock=Hex(0x687284), lit=Hex(0xa7acb3);
            c.Rect(7,8,18,30,Hex(0x43495e)); c.Rect(8,9,15,28,rock); c.Rect(9,10,4,27,lit);
            c.Rect(19,10,3,26,Hex(0x4c5369)); c.Rect(4,5,24,6,rock); c.Line(5,5,26,5,lit);
            c.Rect(6,2,20,4,Hex(0x8c939d)); c.Line(7,2,24,2,Bone);
            c.Rect(4,37,24,4,rock); c.Rect(2,41,28,2,Hex(0x8e959d)); c.Line(4,37,26,37,lit);
            c.Line(14,12,16,19,Hex(0x34394f)); c.Line(16,19,13,24,Hex(0x34394f));
            for(int i=0;i<=frame;i++) { int y=16+i*5; c.Line(15,y,21,y+3,Ink); c.Line(15,y,11,y+4,Ink); }
            if(frame>=2) { c.Polygon(new[]{22,25,25,29,23,34,20,30},Ink); c.Set(26,39,lit); }
            if(frame==3) { c.Rect(5,6,6,3,new Color32()); c.Polygon(new[]{8,5,14,4,12,10},Shade); c.Rect(14,40,3,2,Bone); }
            c.Outline(Ink); return c;
        }

        static Canvas BorderGravestone()
        {
            var c=new Canvas(28,36);
            c.Ellipse(5,2,19,20,Hex(0x868b98)); c.Rect(5,12,19,18,Hex(0x626c7e)); c.Rect(7,11,4,17,Hex(0xa5acb3));
            c.Rect(13,8,3,14,Ink); c.Rect(9,12,11,3,Ink); c.Line(14,8,14,19,Hex(0xb9bdbe));
            c.Line(19,21,16,25,Hex(0x32384f)); c.Line(16,25,19,29,Hex(0x32384f));
            c.Rect(2,30,25,3,Hex(0x4d586c)); c.Line(4,30,25,30,LightStone);
            c.Line(4,29,6,23,Teal); c.Ellipse(1,29,6,4,Hex(0x577966)); c.Set(3,29,Mint);
            c.Outline(Ink); return c;
        }

        static Canvas BorderFence()
        {
            var c=new Canvas(48,34);
            c.Rect(2,16,44,3,Hex(0x555365)); c.Line(2,16,45,16,Hex(0xa3a2ad)); c.Rect(2,27,44,3,Hex(0x484659));
            for(int x=5;x<46;x+=9)
            {
                c.Rect(x,9,3,24,Hex(0x77768a)); c.Rect(x+1,10,1,20,Hex(0xa4a2ac));
                c.Polygon(new[]{x-2,10,x+1,2,x+5,10},Hex(0x7c7d8e)); c.Line(x+1,3,x+1,8,Bone);
            }
            c.Line(12,17,16,23,Hex(0x78718a)); c.Line(16,23,21,18,Hex(0x78718a));
            c.Line(30,17,34,23,Hex(0x78718a)); c.Line(34,23,39,18,Hex(0x78718a));
            c.Ellipse(21,11,8,9,Bone); c.Rect(23,14,2,2,Ink); c.Rect(26,14,2,2,Ink); c.Rect(24,17,2,2,Ink);
            c.Outline(Ink); return c;
        }

        static Canvas OssuaryCandle(int frame)
        {
            var c=new Canvas(32,42); int flicker=frame==1?-1:frame==3?1:0;
            c.Ellipse(3,33,26,7,Hex(0x5b5367)); c.Rect(7,32,19,5,Hex(0x8c8190));
            for(int i=0;i<3;i++) { int x=4+i*8; c.Ellipse(x,25-i%2,9,9,Bone); c.Rect(x+2,28-i%2,2,2,Ink); c.Rect(x+6,28-i%2,2,2,Ink); c.Set(x+4,32-i%2,Ink); }
            c.Line(9,22,22,23,Bone); c.Line(11,25,23,19,Hex(0xa39c9a));
            c.Rect(12,15,7,15,Bone); c.Rect(13,15,2,12,Pale); c.Rect(17,17,2,6,Hex(0xb49f93));
            c.Rect(10,23,3,5,Bone); c.Rect(18,19,3,6,Bone); c.Set(11,29,Gold);
            c.Rect(15,12,1,5,Ink); c.Ellipse(11+flicker,6,9,11,Orange);
            c.Polygon(new[]{15+flicker,1+frame%2,19+flicker,11,16,16,12,11},Gold); c.Rect(14+flicker,9,3,5,Pale);
            c.Set(22-frame,5+frame,Orange); c.Outline(Ink); return c;
        }

        static Canvas BossSource(string id, int frame)
        {
            switch(id)
            {
                case "boss": return NightmareGuardian(id,Boss(frame),frame);
                case "widow": return NightmareGuardian(id,Widow(frame),frame);
                case "bishop": return NightmareGuardian(id,Bishop(frame),frame);
                case "matriarch": return NightmareGuardian(id,NewGuardian(false,frame),frame);
                case "bell": return NightmareGuardian(id,NewGuardian(true,frame),frame);
                case "iron-executioner": return NightmareGuardian(id,IronExecutioner(frame),frame);
                case "bog-oracle": return NightmareGuardian(id,BogOracle(frame),frame);
                default: throw new ArgumentException("Guardiao desconhecido: " + id,nameof(id));
            }
        }

        static Canvas BossPose(string id, BossAnimationState state, int frame)
        {
            Canvas source=BossSource(id,state==BossAnimationState.Idle?(frame==2?0:frame):2);
            int w=source.Width,h=source.Height,cx=w/2,cy=h/2;
            var c=new Canvas(w,h);
            Color32 energy=id=="boss"?Gold:id=="widow"?Hex(0xc4b3fa):id=="bishop"?Orange
                :id=="matriarch"?Mint:id=="bell"?Violet:id=="iron-executioner"?Hex(0xffb475):Hex(0xa9eeb0);
            if(state==BossAnimationState.Death)
            {
                float collapse=frame==0?.84f:frame==1?.63f:frame==2?.39f:.15f;
                Stamp(c,source,frame%2,(int)(h*(1-collapse)*.5f),1-frame*.08f,collapse,(frame%2==0?1:-1)*.05f);
                for(int y=0;y<h;y++) for(int x=0;x<w;x++)
                {
                    int at=(h-1-y)*w+x;
                    if((x*37+y*17+frame*11)%19<frame*4) c.Pixels[at]=new Color32();
                }
                for(int i=0;i<8-frame;i++)
                {
                    int x=3+(i*11+frame*7)%(w-6),y=h-4-(i*7+frame*3)%Math.Max(8,h/2);
                    c.Rect(x,y,frame<2?2:1,2,i%2==0?energy:Bone);
                }
                if(id=="boss") { c.Polygon(new[]{cx-9,h-7,cx-4,h-11,cx-1,h-5},Orange); c.Rect(cx+4,h-8,5,4,Rust); }
                else if(id=="widow") { c.Line(cx-13,h-5,cx-5,h-10,Bone); c.Line(cx+4,h-9,cx+13,h-4,Violet); }
                else if(id=="bishop") { c.Line(cx-10,h-6,cx+9,h-10,Gold); c.Line(cx-2,h-11,cx+1,h-5,Bone); }
                else if(id=="matriarch") { c.Ellipse(cx-10,h-8,8,5,Teal); c.Polygon(new[]{cx+2,h-9,cx+9,h-6,cx+3,h-3},Bone); }
                else if(id=="bell") { c.Rect(cx-11,h-7,22,3,Gold); c.Ellipse(cx-3,h-5,6,3,Hex(0x74556e)); }
                else if(id=="iron-executioner") { c.Rect(cx-9,h-8,9,5,LightStone); c.Line(cx+2,h-6,cx+15,h-9,Gold); }
                else { c.Ellipse(cx-9,h-7,8,4,Teal); c.Line(cx+3,h-5,cx+11,h-11,Bone); }
                return c;
            }
            int beat=frame==1?1:frame==3?-1:0;
            if(state==BossAnimationState.Idle)
                Stamp(c,source,0,frame==1?-1:frame==3?1:0,frame==2?.965f:1,frame==2?1.045f:1);
            else if(state==BossAnimationState.Telegraph)
                Stamp(c,source,0,-1-frame%2,.96f,1.02f,(frame-1.5f)*.025f);
            else if(state==BossAnimationState.Attack)
                Stamp(c,source,beat,2+frame%2,frame==1?1.06f:1.02f,.96f,(frame-1.5f)*.055f);
            else
                Stamp(c,source,beat,-1,1.03f,1.02f,beat*.025f);
            if(state==BossAnimationState.Idle)
            {
                if(id=="boss") c.Line(cx-5,4+frame%2,cx-2,2,Teal);
                else if(id=="widow") c.Line(4,6+frame,7,9+frame,Alpha(Bone,150));
                else if(id=="bishop") c.Set(cx+9,cy+6-frame,Gold);
                else if(id=="matriarch") c.Set(9+frame,cy+7,Mint);
                else if(id=="bell") c.Set(cx+frame-2,h-8,Gold);
                else if(id=="iron-executioner") c.Line(6+frame%2,h-19,8,h-16,LightStone);
                else c.Set(cx+frame-2,5,energy);
                return c;
            }
            bool cast=state==BossAnimationState.Telegraph, attack=state==BossAnimationState.Attack;
            if(id=="boss")
            {
                int armY=cast?cy-8:attack?cy+4:cy-4;
                c.Line(11,cy+3,3,armY+beat,Hex(0x648452)); c.Line(3,armY+beat,2,armY-5,Teal);
                c.Line(w-12,cy+3,w-4,armY-beat,Hex(0x648452)); c.Line(w-4,armY-beat,w-2,armY-5,Teal);
                if(attack) { c.Polygon(new[]{cx-9,cy+2,cx+9,cy+2,cx+7,cy+10,cx-6,cy+9},Ink); c.Line(cx-6,cy+4,cx+6,cy+4,Gold); }
                else { c.Ellipse(1,armY-7,5,5,energy); c.Ellipse(w-6,armY-7,5,5,energy); }
            }
            else if(id=="widow")
            {
                for(int i=0;i<3;i++)
                {
                    int y=cy-6+i*7;
                    c.Line(cx-10,y,3,cast?y-9:y+beat*2,Bone); c.Line(cx+10,y,w-4,cast?y-9:y-beat*2,Bone);
                    c.Line(3,cast?y-9:y+beat*2,1,y+(attack?10:-4),Hex(0x9b8ca7));
                    c.Line(w-4,cast?y-9:y-beat*2,w-2,y+(attack?10:-4),Hex(0x9b8ca7));
                }
                if(attack) { c.Polygon(new[]{cx-6,cy+4,cx-2,cy+15,cx,cy+5},Pale); c.Polygon(new[]{cx+2,cy+5,cx+4,cy+15,cx+7,cy+4},Pale); }
            }
            else if(id=="bishop")
            {
                int x=cast?w-9:w-12+frame;
                c.Rect(x,cast?5:cy,3,attack?h-cy-2:cy,Gold); c.Rect(x-4,cast?11:cy+5,11,3,Gold);
                c.Line(8,cy+6,cast?3:cx-9,cast?cy-6:cy+11,Bone); c.Ellipse(1,cast?cy-11:cy+6,7,7,energy);
                if(attack) { c.Line(cx-12,h-4,cx+12,h-4,energy); c.Line(cx,h-9,cx,h-1,Gold); }
            }
            else if(id=="matriarch")
            {
                for(int i=0;i<3;i++)
                {
                    int x=3+i*4;
                    c.Line(cx-9,cy+8,x,cast?cy-8:cy+13+beat,Teal); c.Line(cx+9,cy+8,w-1-x,cast?cy-8:cy+13-beat,Teal);
                    c.Ellipse(x-2,cast?cy-12:cy+11+beat,5,6,energy); c.Ellipse(w-x-4,cast?cy-12:cy+11-beat,5,6,energy);
                }
                if(attack) { c.Ellipse(cx-8,cy+7,16,10,Hex(0x325e53)); c.Ellipse(cx-4,cy+8,8,7,Mint); }
            }
            else if(id=="bell")
            {
                int x=cx+(cast?-5:attack?7:beat*5);
                c.Line(cx,cy+5,x,h-8,Gold); c.Ellipse(x-4,h-11,8,8,energy);
                for(int i=0;i<3;i++)
                {
                    int y=cy-7+i*8;
                    c.Line(3,y+beat,6,y-3+beat,energy); c.Line(w-4,y-beat,w-7,y-3-beat,energy);
                }
                if(attack) { c.Line(4,h-5,w-5,h-5,energy); c.Set(2,h-6,Pale); c.Set(w-3,h-6,Pale); }
            }
            else if(id=="iron-executioner")
            {
                if(attack)
                {
                    c.Line(12,cy+12,w-6,cy+2+frame,Gold); c.Line(12,cy+13,w-6,cy+3+frame,Hex(0x8e6b65));
                    c.Polygon(new[]{w-16,cy+frame,w-3,cy-2+frame,w-1,cy+7+frame,w-12,cy+13+frame},LightStone);
                    c.Line(w-2,cy+4+frame,w-10,cy+11+frame,Pale);
                    c.Line(w-17,cy-5+frame,w-5,cy-4+frame,energy);
                }
                else
                {
                    for(int i=0;i<4;i++) { int x=5+i*4,y=cy-5+i%2*4-beat; c.Ellipse(x,y,5,6,LightStone); c.Ellipse(x+1,y+1,3,4,Ink); }
                    c.Ellipse(3,cy-10-beat,7,8,energy);
                }
            }
            else
            {
                int handY=cast?cy-12:cy+2;
                c.Line(15,cy+3,5,handY+beat,Teal); c.Line(w-17,cy+3,w-6,handY-beat,Teal);
                c.Ellipse(2,handY-3+beat,8,8,Mint); c.Ellipse(w-10,handY-3-beat,8,8,Mint);
                if(attack) { c.Polygon(new[]{cx-7,cy-2,cx+7,cy-2,cx+5,cy+7,cx-5,cy+7},Ink); c.Line(cx,cy+3,cx+12+frame*2,cy+8,Hex(0x88d98a)); c.Ellipse(cx+10+frame*2,cy+5,7,7,energy); }
                else { c.Ellipse(cx-6,3+frame%2,13,13,Teal); c.Ellipse(cx-4,4+frame%2,9,9,energy); c.Set(cx,5,Pale); }
            }
            if(state==BossAnimationState.Enrage)
            {
                // Put the flared silhouette behind the body so rage never hides
                // the guardian's eyes, face or the shape of its attack weapon.
                var charged=new Canvas(w,h);
                for(int side=0;side<2;side++) for(int i=0;i<3;i++)
                {
                    int x=side==0?1+i*3:w-2-i*3, bend=side==0?3:-3;
                    int y=cy-9+i*7, rise=3+(i+frame)%3*2;
                    charged.Polygon(new[]{x,y+9,x+bend,y-rise,x+bend*2,y+7},energy);
                    charged.Line(x,y+7,x+bend,y-rise+2,Pale);
                }
                charged.Line(4+frame,cy+7,2,cy+13,energy); charged.Line(w-5-frame,cy+6,w-2,cy+12,energy);
                Stamp(charged,c); c=charged;
            }
            if(state==BossAnimationState.Attack) NightmareAttackFace(id,c,frame);
            c.Outline(Ink); return c;
        }

        static Canvas ShotCapsule(int kind)
        {
            var c = new Canvas(14,10);
            Color32 rim = kind == 0 ? Hex(0x175469) : kind == 1 ? Hex(0xb46729) : Hex(0xb45d2c);
            Color32 body = kind == 0 ? Mint : kind == 1 ? Gold : Hex(0xffbd67);
            c.Ellipse(1,1,12,8,rim); c.Ellipse(2,2,10,6,body);
            c.Rect(4,2,5,2,Pale); c.Ellipse(8,3,4,4,Hex(0xfff7d9));
            c.Line(3,7,8,7,kind == 0 ? Teal : Orange);
            if(kind==2) { c.Rect(3,3,2,4,Hex(0x825d40)); c.Rect(7,3,1,4,Hex(0xc18b3a)); }
            c.Outline(Ink); return c;
        }

        static Canvas SpectralPetal(bool veil)
        {
            var c = new Canvas(14,14);
            Color32 rim=veil?Hex(0x65418e):Hex(0x3b5b9d), color=veil?Hex(0xc5b5fa):Hex(0x8ed7fa);
            c.Polygon(new[]{1,7,5,2,10,2,13,6,11,10,5,12},rim);
            c.Ellipse(3,3,9,8,color); c.Ellipse(6,3,5,6,Pale);
            c.Line(3,9,8,10,veil?Violet:Teal); c.Set(9,4,Pale);
            c.Outline(Ink); return c;
        }

        static Canvas ShotFist()
        {
            var c = new Canvas(16,14);
            c.Ellipse(2,1,12,12,Hex(0x30667e)); c.Ellipse(3,2,10,10,Mint);
            c.Ellipse(5,2,7,6,Hex(0xd4fff0)); c.Rect(7,3,4,3,Pale);
            c.Rect(1,5,4,5,Gold); c.Rect(2,5,2,2,Pale); c.Line(6,10,11,10,Teal);
            c.Outline(Ink); return c;
        }

        static Canvas CuriousWeapon(int kind)
        {
            var c = new Canvas(36,24);
            if(kind==0)
            {
                // A folded storm umbrella with brass ribs and an ivory handle.
                c.Line(5,18,31,7,Hex(0x7a605c)); c.Line(5,17,31,6,Gold);
                c.Polygon(new[]{4,17,11,7,26,2,32,6,29,10,16,17},Hex(0x324664));
                c.Polygon(new[]{5,16,14,7,27,3,23,11,13,16},Hex(0x3c8b9c));
                c.Polygon(new[]{14,8,28,3,31,6,24,10},Hex(0x71c9d1));
                c.Line(13,8,9,15,Mint); c.Line(24,5,19,12,Mint); c.Set(29,4,Pale);
                c.Rect(29,4,5,3,Gold); c.Rect(32,4,2,2,Ink); c.Rect(6,17,3,4,Bone);
                c.Line(6,20,3,21,Gold); c.Line(3,21,1,19,Gold); c.Rect(1,17,2,3,Bone);
                c.Rect(15,15,3,5,Hex(0x6c5061)); c.Set(16,15,Gold);
            }
            else if(kind==1)
            {
                // A portable hive with glass honey tank and a crooked bee muzzle.
                c.Rect(6,12,8,8,Hex(0x76513f)); c.Line(7,13,11,16,Gold);
                c.Ellipse(6,3,22,15,Hex(0xb97735)); c.Ellipse(7,3,19,12,Gold);
                for(int row=0;row<3;row++) for(int col=0;col<4;col++)
                {
                    int x=9+col*4+row%2, y=5+row*3;
                    c.Polygon(new[]{x,y+1,x+1,y,x+3,y,x+4,y+1,x+3,y+3,x+1,y+3},Hex(0xd99037));
                    c.Set(x+1,y,Pale);
                }
                c.Rect(21,7,11,6,Hex(0x6f5445)); c.Rect(23,7,8,2,Gold);
                c.Ellipse(29,6,5,8,Shade); c.Rect(31,8,3,4,Ink); c.Set(31,7,Mint);
                c.Rect(4,7,4,9,Teal); c.Rect(5,8,2,5,Mint); c.Set(5,8,Pale);
                c.Line(13,3,12,0,Shade); c.Line(18,3,20,0,Shade); c.Set(12,0,Gold);
                c.Ellipse(12,1,6,5,Gold); c.Rect(14,2,2,3,Ink); c.Ellipse(11,0,4,2,Pale);
            }
            else
            {
                // A spring-loaded boxing glove, drawn as a weapon rather than its projectile.
                c.Rect(3,11,7,5,Hex(0x5d6778)); c.Rect(2,13,4,7,Hex(0x715163));
                c.Line(3,14,4,17,Gold); c.Rect(3,10,5,2,Bone);
                for(int x=9;x<19;x+=3) { c.Line(x,9,x+2,15,LightStone); c.Line(x+2,9,x,15,Shade); }
                c.Ellipse(17,3,17,15,Hex(0x9d3154)); c.Ellipse(18,3,14,12,Hex(0xe65b85));
                c.Ellipse(21,4,9,7,Hex(0xffb2b5)); c.Rect(22,4,6,2,Pale);
                c.Ellipse(16,11,8,7,Rust); c.Line(18,12,22,13,Hex(0xffa1a0));
                c.Rect(15,8,5,8,Gold); c.Rect(16,9,3,2,Pale); c.Rect(20,15,9,3,Hex(0x8f375a));
                c.Set(29,10,Hex(0xffd5c8)); c.Set(6,10,Mint);
            }
            c.Outline(Ink); return c;
        }

        static Canvas LanternWisp(int frame)
        {
            var c = new Canvas(30,34); int sway=frame==1?1:frame==3?-1:0;
            c.Polygon(new[]{8,22,23,22,23+sway,29,18,27,17+sway,33,12,29,7+sway,31},Hex(0x537b91));
            c.Polygon(new[]{10,23,20,23,20+sway,28,16,27,13,31,10,28},Hex(0x99c4d3));
            c.Line(13,7,13,3,Gold); c.Line(13,3,18,3,Gold); c.Line(18,3,18,7,Gold);
            c.Rect(6,9,19,15,Hex(0x323d58)); c.Rect(8,10,15,12,Hex(0xc67336));
            c.Rect(10,10,11,10,Gold); c.Ellipse(12,12,7,8,Hex(0xfff1b2));
            c.Rect(11,12,3,3,Ink); c.Rect(18,12,3,3,Ink); c.Set(12,13,Pale); c.Set(19,13,Pale);
            c.Line(14,19,17,19,Hex(0xa86032)); c.Rect(6,8,19,3,Hex(0x5a6779));
            c.Line(7,8,23,8,Bone); c.Rect(5,22,21,3,Hex(0x5a6779)); c.Line(7,22,23,22,Gold);
            c.Rect(6,10,2,13,LightStone); c.Rect(23,10,2,13,Shade);
            c.Line(7,13,2,17+sway,LightStone); c.Line(23,13,28,17-sway,LightStone);
            c.Set(2,18+sway,Mint); c.Set(27,18-sway,Mint);
            if(frame==2) { c.Rect(12,15,8,2,Pale); c.Set(15,5,Orange); }
            c.Outline(Ink); return c;
        }

        static Canvas MushroomBrute(int frame)
        {
            var c = new Canvas(36,34); int stride=frame==1?2:frame==3?-2:0;
            c.Rect(9,26,6,6+stride,Hex(0x574e61)); c.Rect(23,26,6,6-stride,Hex(0x574e61));
            c.Rect(8,30+stride,8,3,Hex(0x9a8790)); c.Rect(22,30-stride,9,3,Hex(0x725f75));
            c.Ellipse(10,14,17,16,Hex(0x92966c)); c.Ellipse(11,15,13,12,Hex(0xc6c293));
            c.Line(10,21,5,27+stride,Hex(0x8b9669)); c.Ellipse(2,25+stride,7,5,Teal);
            c.Line(27,21,31,25-stride,Hex(0x8b9669)); c.Ellipse(29,24-stride,6,5,Teal);
            c.Polygon(new[]{2,15,6,7,12,2,24,2,31,7,35,15,29,18,8,18},Hex(0x6b376f));
            c.Polygon(new[]{4,12,9,5,15,3,25,4,32,12,29,15,8,15},Hex(0xad5c91));
            c.Ellipse(10,5,7,4,Hex(0xe6acb1)); c.Ellipse(22,7,5,4,Gold); c.Set(24,7,Pale);
            c.Rect(4,14,28,3,Hex(0x61395d)); c.Line(9,17,27,17,Hex(0xdbc7b2));
            c.Rect(12,20,4,4,Ink); c.Rect(22,20,4,4,Ink); c.Set(13,21,Mint); c.Set(23,21,Mint);
            c.Rect(16,25,7,2,Hex(0x695f62)); c.Rect(18,25,2,2,Bone);
            c.Ellipse(2,10,4,3,Mint); c.Set(7,4,Hex(0xcbe1a2));
            if(frame==2) { c.Ellipse(16,22,7,7,Ink); c.Ellipse(17,23,5,4,Mint); c.Set(19,22,Pale); }
            c.Outline(Ink); return c;
        }

        static Canvas ClockworkImp(int frame)
        {
            var c=new Canvas(34,32); int stride=frame==1?2:frame==3?-2:0;
            c.Polygon(new[]{8,12,1,8,2,18,8,21},Hex(0x77505e));
            c.Polygon(new[]{25,12,32,8,31,18,25,21},Hex(0x77505e));
            c.Line(2,10,7,15,Hex(0xc58b82)); c.Line(31,10,26,15,Hex(0xc58b82));
            c.Rect(10,23,4,6,Shade); c.Rect(21,23,4,6,Shade);
            c.Line(10,24,14,28+stride,LightStone); c.Line(21,24,25,28-stride,LightStone);
            c.Rect(9,28+stride,7,3,Gold); c.Rect(21,28-stride,7,3,Gold);
            c.Ellipse(8,11,19,16,Hex(0x987039)); c.Ellipse(10,12,15,12,Gold);
            c.Ellipse(13,14,10,10,Hex(0x64575c)); c.Ellipse(15,16,6,6,Teal); c.Rect(17,17,2,4,Mint);
            for(int i=0;i<4;i++) { int x=14+i*3; c.Set(x,13,Pale); c.Set(x,24,Orange); }
            c.Polygon(new[]{10,9,8,2,14,6,19,5,25,1,24,12},Hex(0x68535a));
            c.Ellipse(9,6,17,12,Hex(0xc69754)); c.Ellipse(10,7,14,9,Hex(0xf0c077));
            c.Rect(11,10,4,3,Ink); c.Rect(20,10,4,3,Ink); c.Set(12,10,Pink); c.Set(21,10,Pink);
            c.Line(14,15,21,15,Rust); c.Rect(15,14,2,2,Bone); c.Rect(20,14,2,2,Bone);
            c.Line(8,18,3,24-stride,LightStone); c.Line(26,18,30,23+stride,LightStone);
            c.Ellipse(1,22-stride,5,4,Gold); c.Ellipse(28,21+stride,5,4,Gold);
            if(frame==2) { c.Rect(12,10,3,2,Pale); c.Rect(20,10,3,2,Pale); c.Set(17,15,Orange); }
            c.Outline(Ink); return c;
        }

        static Canvas IronExecutioner(int frame)
        {
            var c=new Canvas(64,64); int stride=frame==1?2:frame==3?-2:0, lift=frame==2?-8:0;
            Color32 steel=Hex(0x85858d), bright=Hex(0xc6c7c4), cloak=Hex(0x583447);
            c.Polygon(new[]{16,34,45,34,51,59,39,56,30,62,20,57,11,59},cloak);
            c.Polygon(new[]{18,37,40,36,43,55,31,53,28,59,18,54},Hex(0x7b4052));
            c.Line(18,40,14,54,Hex(0xaf5d62)); c.Line(39,40,44,54,Hex(0x352e42));
            c.Rect(20,48,8,10+stride,Hex(0x494956)); c.Rect(34,48,8,10-stride,Hex(0x494956));
            c.Rect(17,56+stride,12,5,steel); c.Rect(33,56-stride,12,5,steel);
            c.Line(18,56+stride,27,56+stride,bright); c.Line(34,56-stride,42,56-stride,bright);
            c.Polygon(new[]{17,24,41,24,45,43,15,43},Hex(0x515361));
            c.Polygon(new[]{20,25,38,25,40,39,19,39},steel); c.Rect(22,27,3,10,bright);
            c.Polygon(new[]{26,27,35,27,36,36,30,39,25,35},Hex(0x555560));
            c.Rect(29,29,3,7,Hex(0xae4b46)); c.Rect(27,31,7,2,Orange); c.Set(30,30,Gold);
            c.Line(35,27,38,36,Hex(0x3b3a49)); c.Set(21,27,Pale); c.Set(37,28,Pale);
            c.Set(20,36,Gold); c.Set(38,36,Gold); c.Line(24,35,26,33,Hex(0x555160));
            c.Rect(18,39,26,5,Hex(0x352d3a)); c.Rect(28,39,7,6,Gold); c.Rect(30,40,3,3,Ink);
            c.Ellipse(8,26,15,12,Hex(0x6a6a75)); c.Ellipse(10,26,11,8,steel); c.Line(11,27,18,27,bright);
            c.Ellipse(38,26,15,12,Hex(0x6a6a75)); c.Ellipse(39,26,11,8,steel);
            c.Line(11,35,8,44+stride,Hex(0xa79d98)); c.Ellipse(5,42+stride,8,8,steel);
            // Chains visibly swing independently of the helmet and cloth.
            for(int i=0;i<5;i++) { int x=10+i%2*2+stride/2,y=31+i*4; c.Ellipse(x,y,4,5,bright); c.Ellipse(x+1,y+1,2,3,Ink); }
            c.Polygon(new[]{17,23,16,12,22,3,33,1,43,10,45,24},Hex(0x302d3d));
            c.Polygon(new[]{18,19,21,8,31,4,40,11,42,22},Hex(0x553d49));
            c.Rect(21,13,18,15,Hex(0x686b77)); c.Rect(22,13,14,4,bright); c.Rect(35,15,4,12,Hex(0x535564));
            c.Rect(23,19,6,4,Ink); c.Rect(32,19,6,4,Ink); c.Rect(24,20,4,2,Orange); c.Rect(33,20,4,2,Orange);
            c.Rect(29,23,3,5,Ink); c.Line(23,27,37,27,bright); c.Set(25,26,Ink); c.Set(35,26,Ink);
            c.Line(20,12,23,7,Hex(0x99717b)); c.Set(28,5,Gold);
            // Broad guillotine blade changes height on the cast pose.
            c.Rect(48,13+lift,3,39,Hex(0x8c6258)); c.Line(48,14+lift,48,49,Gold);
            c.Polygon(new[]{48,8+lift,59,8+lift,62,13+lift,58,28+lift,51,31+lift,51,17+lift,46,17+lift},steel);
            c.Polygon(new[]{51,9+lift,58,9+lift,60,13+lift,56,26+lift,52,28+lift},bright);
            c.Line(59,14+lift,55,26+lift,Pale); c.Rect(51,13+lift,4,4,Ink); c.Set(52,13+lift,Orange);
            c.Ellipse(45,34,9,7,steel); c.Rect(47,35,5,2,bright); c.Rect(47,50,6,4,Gold);
            if(frame==2) { c.Rect(24,18,5,2,Pale); c.Rect(33,18,5,2,Pale); c.Line(5,7,8,3,Orange); c.Set(12,1,Gold); }
            c.Outline(Ink); return c;
        }

        static Canvas BogOracle(int frame)
        {
            var c=new Canvas(64,64); int sway=frame==1?2:frame==3?-2:0, reach=frame==2?5:0;
            Color32 moss=Hex(0x547e64), light=Hex(0x96bd84), deep=Hex(0x2d4d4e);
            // Draped moss, gnarled roots and a broad frog silhouette.
            c.Polygon(new[]{13,37,47,37,55+sway,58,44,55,35,62,27,57,16,61,8+sway,56},deep);
            c.Polygon(new[]{18,38,42,37,48,54,35,54,31,59,23,54,14,56},moss);
            c.Line(18,43,15+sway,56,light); c.Line(39,43,46+sway,53,Teal);
            c.Ellipse(13,30,35,23,moss); c.Ellipse(18,33,25,17,light);
            c.Ellipse(21,34,18,13,Hex(0xabc592)); c.Line(24,36,24,45,Hex(0xc5d2a0));
            for(int i=0;i<4;i++) c.Line(26,37+i*3,37,37+i*3,Hex(0x6e9670));
            c.Ellipse(9,45,14,10,moss); c.Ellipse(40,45,14,10,deep);
            c.Polygon(new[]{7,54,17,52,23,55,19,59,13,56,8,59,3,57},light);
            c.Polygon(new[]{40,55,48,52,58,55,61,58,54,58,48,56,43,60},moss);
            c.Line(13,35,5,40-reach,moss); c.Line(5,40-reach,3,34-reach,light);
            c.Line(46,35,56,37-reach,moss); c.Line(56,37-reach,59,30-reach,light);
            c.Ellipse(1,29-reach,7,8,light); c.Ellipse(55,25-reach,7,8,light);
            c.Line(2,31-reach,0,27-reach,Teal); c.Line(6,31-reach,8,27-reach,Teal);
            c.Line(56,28-reach,54,23-reach,Teal); c.Line(60,28-reach,63,24-reach,Teal);
            c.Ellipse(10,17,43,23,deep); c.Ellipse(12,16,39,22,moss); c.Ellipse(16,18,31,16,light);
            c.Ellipse(12,12,14,16,moss); c.Ellipse(36,12,14,16,moss);
            c.Ellipse(15,15,9,10,Gold); c.Ellipse(39,15,9,10,Gold);
            c.Rect(18,16,3,8,Ink); c.Rect(42,16,3,8,Ink); c.Set(17,16,Pale); c.Set(41,16,Pale);
            c.Polygon(new[]{18,28,44,28,42,35,22,35},deep); c.Line(21,29,42,29,Mint);
            c.Rect(24,29,3,3,Bone); c.Rect(36,29,3,3,Bone); c.Set(31,26,Teal);
            // Antler crown and ritual pearl make it distinct from the common slime.
            c.Line(19,17,15,8,Hex(0x716956)); c.Line(15,8,17,2,light); c.Line(15,8,9,5,light);
            c.Line(44,17,47,8,Hex(0x716956)); c.Line(47,8,45,1,light); c.Line(47,8,54,4,light);
            c.Ellipse(25,5+sway,13,13,Hex(0x315c65)); c.Ellipse(27,6+sway,9,9,Mint);
            c.Ellipse(29,7+sway,5,5,Pale); c.Set(30,4+sway,Gold);
            c.Ellipse(12,28,4,3,Teal); c.Ellipse(47,26,4,3,Teal); c.Set(14,20,Pale);
            c.Rect(27,48,9,4,Gold); c.Rect(29,49,5,2,Teal);
            for(int i=0;i<5;i++) { c.Ellipse(20+i*5,35+i%2,3,3,Gold); c.Set(21+i*5,35+i%2,Bone); }
            c.Line(14,38,10,45,Hex(0x25494b)); c.Line(44,39,49,46,Hex(0x25494b));
            c.Ellipse(11,31,4,3,Hex(0xa6bc76)); c.Ellipse(46,32,4,3,Hex(0xa6bc76));
            c.Set(19,28,Hex(0x47785d)); c.Set(45,27,Hex(0x47785d)); c.Set(23,17,Pale);
            if(frame==2) { c.Ellipse(27,29,9,8,Ink); c.Ellipse(29,30,5,5,Mint); c.Set(31,30,Pale); }
            c.Outline(Ink); return c;
        }

        static Canvas ExplosiveBarrel(int frame) => HalloweenBarrel(false, frame);
        static Canvas AcidBarrel(int frame) => HalloweenBarrel(true, frame);

        static Canvas WaterUrn()
        {
            var c=new Canvas(30,34);
            c.Ellipse(1,10,10,16,Hex(0x4e7b9c)); c.Ellipse(3,12,6,10,new Color32());
            c.Ellipse(19,10,10,16,Hex(0x345d81)); c.Ellipse(21,12,6,10,new Color32());
            c.Polygon(new[]{9,7,21,7,24,15,23,28,20,32,10,32,6,27,6,17},Hex(0x315c82));
            c.Ellipse(7,12,16,19,Hex(0x4d9cbb)); c.Ellipse(9,12,10,16,Hex(0x8ed1da));
            c.Rect(9,6,12,6,Hex(0x407b9f)); c.Ellipse(8,3,14,6,Hex(0xb3d7d3)); c.Ellipse(10,4,10,3,Hex(0x274e74));
            c.Rect(9,11,12,2,Gold); c.Line(8,26,21,26,Gold);
            c.Polygon(new[]{15,15,19,22,17,25,13,25,11,22},Teal); c.Line(14,19,13,22,Pale);
            c.Line(10,14,9,20,Pale); c.Set(10,13,Pale); c.Outline(Ink); return c;
        }

        static Canvas ShatteredProp()
        {
            var c=new Canvas(32,16);
            c.Ellipse(1,7,30,8,Alpha(Ink,90));
            c.Polygon(new[]{3,11,5,5,10,7,9,13},Hex(0x805a5d)); c.Line(5,6,8,8,Gold);
            c.Polygon(new[]{12,10,14,2,19,3,19,9},Hex(0x6c919e)); c.Line(15,3,18,4,Bone);
            c.Polygon(new[]{22,12,23,7,29,8,28,13},Hex(0x8c6e67)); c.Set(25,8,Gold);
            c.Rect(10,13,4,2,Stone); c.Rect(20,12,2,2,LightStone); return c;
        }

        static Canvas WetPuddle()
        {
            var c=new Canvas(48,28);
            c.Ellipse(2,7,43,17,Alpha(Hex(0x367a98),115)); c.Ellipse(7,3,25,21,Alpha(Hex(0x4c9caa),135));
            c.Ellipse(19,7,27,17,Alpha(Hex(0x4c9caa),135)); c.Ellipse(9,7,23,14,Alpha(Hex(0x6bbdc0),95));
            c.Line(9,10,17,8,Alpha(Pale,125)); c.Line(7,15,12,16,Alpha(Mint,110));
            c.Line(28,11,38,12,Alpha(Pale,100)); c.Line(28,20,38,19,Alpha(Mint,110));
            c.Set(21,5,Alpha(Pale,120)); c.Set(42,16,Alpha(Mint,120)); return c;
        }

        static Canvas Relic(int kind)
        {
            var c=new Canvas(32,32);
            if(kind==0)
            {
                c.Ellipse(4,2,23,23,Hex(0x916735)); c.Ellipse(6,4,19,19,Gold);
                c.Ellipse(8,6,15,15,Hex(0x35586b)); c.Ellipse(9,7,12,12,Teal);
                c.Ellipse(11,8,9,8,Mint); c.Rect(12,8,4,2,Pale);
                c.Polygon(new[]{20,20,24,19,30,27,27,31,22,26},Hex(0x935966)); c.Line(24,22,28,27,Bone);
                c.Polygon(new[]{11,16,14,11,18,15,17,18,13,19},Gold); c.Set(14,13,Pale);
            }
            else if(kind==1)
            {
                c.Line(8,2,4,8,Gold); c.Line(4,8,7,16,Gold); c.Line(24,2,28,8,Gold); c.Line(28,8,25,16,Gold);
                c.Line(8,2,24,2,Hex(0xa7825b)); c.Polygon(new[]{16,7,27,16,23,28,16,31,8,27,5,16},Hex(0x69547e));
                c.Polygon(new[]{16,9,24,16,21,26,16,29,10,25,8,16},Violet);
                c.Polygon(new[]{16,12,21,17,19,23,13,23,11,17},Mint); c.Rect(14,15,4,6,Pale);
                c.Set(15,10,Gold); c.Line(8,17,10,24,Hex(0xe7acd1));
            }
            else
            {
                c.Polygon(new[]{7,4,18,4,17,17,26,21,28,27,23,30,6,28,4,23,7,19},Hex(0x435b77));
                c.Polygon(new[]{8,5,16,5,15,18,24,22,24,26,8,24,7,21},Teal);
                c.Rect(7,5,10,4,Mint); c.Line(8,5,15,5,Pale);
                for(int y=11;y<19;y+=3) c.Line(9,y,14,y,Gold);
                c.Rect(6,26,20,3,Hex(0x98a6a8)); c.Line(7,26,24,26,Pale);
                c.Polygon(new[]{21,3,17,11,21,11,19,18,27,8,23,8,25,3},Gold);
                c.Set(23,4,Pale);
            }
            c.Outline(Ink); return c;
        }

        static Canvas DeepWall(int theme, bool crown)
        {
            var c=new Canvas(32,32); Color32 trim=ThemeTrim(theme);
            Color32 stone=theme==0?Hex(0x655a48):theme==1?Hex(0x65748a):theme==2?Hex(0x754d55):theme==3?Hex(0x526c52):Hex(0x564266);
            if(crown)
            {
                c.Polygon(new[]{0,8,7,1,31,1,31,22,24,29,0,29},Ink);
                c.Rect(0,8,25,19,stone); c.Polygon(new[]{0,8,7,1,31,1,24,8},trim);
                c.Polygon(new[]{25,8,31,2,31,22,25,28},Hex(0x302e40));
                c.Line(1,8,24,8,Bone); c.Line(7,1,30,1,LightStone); c.Line(24,9,24,26,trim);
                c.Line(0,17,23,17,Hex(0x363544)); c.Rect(2,10,10,2,trim); c.Rect(13,20,9,2,trim);
            }
            else
            {
                c.Rect(0,0,32,32,Ink); c.Rect(1,1,29,29,stone); c.Rect(26,1,4,29,Hex(0x30303e));
                c.Rect(1,1,25,4,trim); c.Line(1,1,24,1,Bone); c.Line(25,5,25,29,LightStone);
                for(int y=8;y<31;y+=10) { c.Line(1,y,25,y,Hex(0x33313f)); c.Line(y%20==8?12:5,y,y%20==8?12:5,y+9,Hex(0x33313f)); }
                c.Rect(3,10,8,2,trim); c.Rect(14,20,9,2,trim); c.Line(1,29,28,29,Ink);
            }
            if(theme==0) { c.Line(3,5,7,17,Teal); c.Line(7,17,3,28,Teal); c.Ellipse(3,12,5,3,Hex(0x8b9b60)); }
            else if(theme==1) { c.Line(22,3,12,13,Hex(0xa6b7c9)); c.Line(22,7,17,7,Hex(0xa6b7c9)); }
            else if(theme==2) { c.Rect(15,10,3,13,Gold); c.Rect(11,13,11,3,Gold); c.Set(15,10,Pale); }
            else if(theme==3) { c.Ellipse(12,10,9,13,Hex(0x8a9c68)); c.Set(15,12,Mint); c.Line(20,20,23,26,Teal); }
            else { c.Polygon(new[]{16,9,21,15,16,22,11,15},Hex(0x30253e)); c.Rect(15,12,3,6,Violet); c.Set(15,12,Pale); }
            return c;
        }

        static Canvas Stapler()
        {
            var c = new Canvas(32,20);
            c.Polygon(new[]{2,14,6,10,28,10,30,14,28,17,5,17},Hex(0x525d70));
            c.Rect(7,12,22,3,Hex(0xb6c5ce)); c.Line(8,12,27,12,Pale);
            c.Polygon(new[]{4,7,7,2,15,2,28,6,29,10,8,9},Hex(0xb94058));
            c.Polygon(new[]{7,3,15,3,25,6,24,8,7,7},Hex(0xf87580));
            c.Line(8,3,15,3,Hex(0xffb6a3)); c.Line(16,4,25,6,Hex(0xffb6a3));
            c.Rect(3,7,5,7,Hex(0x58364f)); c.Ellipse(3,9,5,5,Gold); c.Set(4,10,Pale);
            c.Rect(25,8,5,3,Ink); c.Rect(27,8,2,2,LightStone);
            c.Rect(10,15,11,2,Hex(0x313b51)); c.Rect(8,17,8,2,Rust);
            c.Outline(Ink); return c;
        }

        static Canvas BananaGun()
        {
            var c = new Canvas(32,20);
            c.Polygon(new[]{2,3,6,2,10,9,17,13,24,11,28,6,30,8,29,14,24,18,16,19,9,16,4,10},Hex(0xc18a35));
            c.Polygon(new[]{4,3,7,3,11,10,17,14,24,13,28,9,28,13,23,16,16,17,10,14,6,9},Gold);
            c.Line(6,4,10,12,Hex(0xfff0a0)); c.Line(11,13,17,16,Hex(0xfff0a0)); c.Line(18,16,24,14,Hex(0xfff0a0));
            c.Rect(2,1,5,3,Hex(0x685141)); c.Rect(3,1,3,1,Bone);
            c.Polygon(new[]{27,5,30,5,31,8,28,11,26,9},Hex(0x645052)); c.Rect(28,6,3,3,Ink);
            c.Set(12,13,Hex(0x9b6b3c)); c.Set(17,17,Hex(0x9b6b3c)); c.Set(23,14,Hex(0x9b6b3c));
            c.Ellipse(14,9,3,4,Ink); c.Set(15,10,Pale); c.Rect(20,10,2,2,Ink);
            c.Outline(Ink); return c;
        }

        static Canvas SpinningTop()
        {
            var c = new Canvas(32,24);
            c.Rect(12,1,6,4,Gold); c.Rect(14,0,2,3,Pale);
            c.Ellipse(3,4,24,13,Hex(0x3d4267)); c.Ellipse(4,4,22,10,Hex(0x6d5ab0));
            c.Ellipse(7,4,16,8,Mint); c.Ellipse(10,5,10,6,Teal); c.Ellipse(13,6,4,4,Gold);
            c.Polygon(new[]{4,11,26,11,23,17,18,21,14,23,10,18,6,16},Hex(0x454c81));
            c.Polygon(new[]{6,11,24,11,22,15,16,18,11,16},Violet);
            c.Line(6,12,23,12,Hex(0xc7a9ee)); c.Line(11,16,16,18,Mint);
            c.Rect(23,7,8,4,Hex(0xa9bdc9)); c.Rect(29,6,2,6,Gold); c.Rect(30,8,2,2,Ink);
            c.Rect(13,19,4,4,LightStone); c.Set(15,23,Pale);
            c.Outline(Ink); return c;
        }

        static Canvas StapleShot()
        {
            var c = new Canvas(14,10);
            c.Ellipse(1,2,12,6,Hex(0x39516d)); c.Ellipse(2,3,10,4,Hex(0xaad8ea));
            c.Rect(4,3,6,2,Pale); c.Rect(9,4,3,2,Gold); c.Line(3,7,10,7,Teal);
            c.Outline(Ink); return c;
        }

        static Canvas BananaShot()
        {
            var c = new Canvas(14,12);
            c.Ellipse(1,1,12,10,Hex(0xab5b28)); c.Ellipse(2,2,10,8,Gold);
            c.Ellipse(4,2,7,5,Hex(0xfff3b5)); c.Rect(5,3,5,2,Pale);
            c.Line(3,8,8,9,Orange); c.Outline(Ink); return c;
        }

        static Canvas TopShot(int frame)
        {
            return ShotDisc(frame);
        }

        static Canvas ShopCounter()
        {
            var c = new Canvas(96,32);
            c.Polygon(new[]{1,9,9,2,88,2,95,9,91,13,5,13},Hex(0x986949));
            c.Rect(5,11,87,18,Hex(0x543746)); c.Rect(7,12,82,14,Hex(0x724c49));
            c.Line(8,3,86,3,Hex(0xd6a375)); c.Line(3,9,93,9,Gold); c.Rect(3,10,90,2,Hex(0xc29061));
            for(int x=12;x<87;x+=18) { c.Rect(x,14,2,12,Hex(0x4d3343)); c.Line(x+2,15,x+14,15,Hex(0x9b6652)); }
            c.Rect(7,27,84,3,Hex(0x342b3c)); c.Rect(9,29,6,2,Hex(0x342b3c)); c.Rect(82,29,6,2,Hex(0x342b3c));
            c.Ellipse(40,13,16,13,Gold); c.Ellipse(42,14,12,10,Bone);
            c.Rect(44,17,3,3,Ink); c.Rect(49,17,3,3,Ink); c.Rect(46,21,4,3,Shade);
            c.Rect(15,3,13,4,Shade); c.Line(16,3,26,3,LightStone); c.Rect(69,3,11,5,Hex(0x483546));
            c.Set(73,4,Mint); c.Set(76,4,Gold); c.Outline(Ink); return c;
        }

        static Canvas ShopShelf()
        {
            var c = new Canvas(64,40);
            c.Polygon(new[]{2,8,8,1,56,1,62,8},Hex(0x9d7459));
            c.Rect(4,7,56,29,Hex(0x332e43)); c.Rect(7,9,50,25,Hex(0x443649));
            c.Rect(4,7,4,30,Hex(0xa17c60)); c.Rect(56,7,4,30,Hex(0x77594f));
            c.Rect(7,19,50,3,Hex(0xc09569)); c.Line(8,19,55,19,Bone);
            c.Rect(4,34,56,4,Hex(0x8b6252)); c.Line(5,34,58,34,Gold);
            for(int i=0;i<6;i++) { int x=11+i*7; c.Rect(x,11,4,8,i%2==0?Teal:Rust); c.Rect(x,10,4,2,Gold); c.Set(x+1,12,Pale); }
            c.Rect(12,25,12,6,Hex(0xb9a486)); c.Line(13,26,21,26,Bone); c.Line(13,29,20,29,Hex(0x8c766d));
            c.Ellipse(34,23,10,10,Hex(0x8c7fa9)); c.Ellipse(36,25,6,6,Violet); c.Set(38,25,Pale);
            c.Rect(48,24,5,9,Gold); c.Set(50,25,Bone); c.Outline(Ink); return c;
        }

        static Canvas ShopCase()
        {
            var c = new Canvas(48,32);
            c.Polygon(new[]{2,23,7,18,40,18,46,23,43,30,5,30},Hex(0x71514e));
            c.Rect(5,24,38,6,Hex(0x493345)); c.Line(5,24,42,24,Gold);
            c.Polygon(new[]{6,19,6,7,12,3,37,3,42,7,42,19,37,23,12,23},Alpha(Hex(0x7a9b9a),60));
            c.Line(6,7,12,3,LightStone); c.Line(12,3,37,3,LightStone); c.Line(37,3,42,7,LightStone);
            c.Line(6,7,6,19,Hex(0x5b787d)); c.Line(42,7,42,19,Hex(0x5b787d));
            c.Line(8,8,15,5,Alpha(Pale,170)); c.Line(8,11,21,5,Alpha(Pale,110));
            c.Rect(20,25,9,4,Gold); c.Rect(22,26,5,2,Shade); c.Outline(Ink); return c;
        }

        static Canvas ShopBoard()
        {
            var c = new Canvas(160,112);
            c.Rect(2,4,156,104,Ink); c.Rect(4,2,152,108,Ink);
            c.Rect(5,5,150,102,Hex(0x5f3e47)); c.Rect(8,8,144,96,Hex(0x2b273d));
            c.Rect(6,6,148,2,Hex(0xa47a5b)); c.Rect(6,104,148,2,Hex(0x8e6454));
            c.Rect(6,8,2,96,Hex(0xa47a5b)); c.Rect(152,8,2,96,Hex(0x60464a));
            c.Line(10,10,149,10,Hex(0xd9ac74)); c.Line(10,101,149,101,Hex(0x734f50));
            for(int x=17;x<149;x+=18) { c.Rect(x,3,6,3,Gold); c.Set(x+2,2,Bone); c.Rect(x,106,6,3,Gold); }
            for(int y=17;y<101;y+=17) { c.Rect(3,y,3,6,Gold); c.Rect(154,y,3,6,Gold); }
            foreach(int x in new[]{5,143}) foreach(int y in new[]{5,95})
            {
                c.Rect(x,y,12,12,Hex(0x70505a)); c.Rect(x+2,y+2,8,8,Gold); c.Rect(x+4,y+4,4,4,Hex(0x352b42)); c.Set(x+5,y+5,Bone);
            }
            c.Ellipse(69,2,22,15,Gold); c.Ellipse(72,3,16,12,Bone);
            c.Rect(75,6,4,4,Ink); c.Rect(82,6,4,4,Ink); c.Rect(78,10,5,4,Shade);
            return c;
        }

        static void HeroDepth(Canvas c, int kind, int frame)
        {
            // A cool shadow plane on the right and warm rim on the left gives the
            // same tiny silhouettes volume without changing their physical bounds.
            for (int y = 12; y < 27; y++)
                for (int x = 13; x < 23; x++)
                {
                    int at = (c.Height - 1 - y) * c.Width + x;
                    Color32 pixel = c.Pixels[at];
                    if (pixel.a == 0 || pixel.r + pixel.g + pixel.b < 130 || pixel.r + pixel.g + pixel.b > 670) continue;
                    float shade = x > 17 ? .83f : .93f;
                    c.Pixels[at] = new Color32((byte)(pixel.r * shade), (byte)(pixel.g * shade), (byte)(pixel.b * (shade + .03f)), pixel.a);
                }
            if (kind == 0)
            {
                c.Line(8,15,8,19,Pale); c.Line(15,19,16,23,Hex(0x114c61));
                c.Rect(9,22,2,2,Shade); c.Set(9,22,Gold); c.Set(15,21,Bone);
                c.Line(8,5,10,3,Hex(0x72a9b9)); c.Set(8,14,Gold);
            }
            else if (kind == 1)
            {
                c.Line(7,15,6,20,Hex(0xf0bbdf)); c.Line(15,17,17,22,Hex(0x171d39));
                c.Set(9,21,Gold); c.Set(8,23,Pale); c.Set(14,24,Violet);
                c.Line(8,5,10,4,Pale); c.Rect(11,18,1,2,Bone);
            }
            else if (kind == 2)
            {
                c.Line(8,17,7,21,Hex(0xff9a7d)); c.Line(16,16,18,23,Hex(0x211c32));
                c.Set(8,6,Pale); c.Set(10,7,Pale); c.Line(13,14,15,13,Hex(0x8a6e73));
                c.Rect(9,22,2,1,Bone); c.Set(16,24,Orange);
            }
            else
            {
                c.Line(8,14,7,18,Hex(0xd7d6a0)); c.Rect(15,15,2,5,Hex(0x3d513d));
                c.Line(4,14,4,19,Pale); c.Set(19,13,Pale); c.Rect(9,22,2,2,Teal);
                c.Set(10,8,Pale); c.Set(16,8,Pale); c.Set(8,5,Gold);
            }
            int stride = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            c.Line(7,25+stride,9,25+stride,Hex(0xc3b6aa));
            c.Line(14,25-stride,16,25-stride,Hex(0x655a6d));
        }

        static Canvas Skeleton(int frame)
        {
            var c = new Canvas(28,28); int stride = frame == 1 ? 2 : frame == 3 ? -2 : 0;
            c.Line(11,20,8,25+stride,Hex(0x927e7a)); c.Line(17,20,21,25-stride,Hex(0x927e7a));
            c.Rect(5,25+stride,5,2,Bone); c.Rect(19,25-stride,5,2,LightStone);
            c.Line(10,20,7,24+stride,Pale); c.Line(18,20,21,24-stride,Bone);
            c.Rect(10,12,9,9,Hex(0x665b67)); c.Rect(13,12,3,9,Bone);
            for(int y=13;y<20;y+=3) { c.Line(9,y,19,y,Bone); c.Set(10,y-1,Pale); }
            c.Line(9,14,5,19+stride,Bone); c.Line(19,14,23,18-stride,LightStone);
            c.Ellipse(7,2,15,12,Hex(0x9d8881)); c.Ellipse(8,2,12,11,Bone);
            c.Rect(9,5,4,4,Ink); c.Rect(16,5,4,4,Ink); c.Set(10,6,Orange); c.Set(17,6,Orange);
            c.Rect(13,9,3,2,Ink); c.Line(10,12,18,12,Ink);
            c.Set(11,12,Bone); c.Set(14,12,Bone); c.Set(17,12,Bone);
            int lift = frame==2 ? -4 : 0;
            c.Rect(24,10+lift,2,13,Rust); c.Polygon(new[]{22,8+lift,26,6+lift,27,14+lift,22,15+lift},LightStone);
            c.Line(23,9+lift,26,8+lift,Pale); c.Outline(Ink); return c;
        }

        static Canvas Slime(int frame)
        {
            var c = new Canvas(28,22); int squash = frame==1?2:frame==3?-1:0;
            c.Ellipse(2,6+squash,24,14-squash,Hex(0x274b4c));
            c.Ellipse(3,4+squash,22,14-squash,Hex(0x528563)); c.Ellipse(6,3+squash,16,12-squash,Hex(0x79b77d));
            c.Ellipse(7,5+squash,6,4,Hex(0xb3d69b)); c.Set(8,5+squash,Pale);
            c.Ellipse(7,10+squash,4,5,Ink); c.Ellipse(18,10+squash,4,5,Ink);
            c.Set(8,11+squash,Gold); c.Set(19,11+squash,Gold);
            c.Line(12,16,17,16,Hex(0x294f4c));
            c.Rect(11,12,6,2,Alpha(Bone,180)); c.Set(10,13,Bone); c.Set(17,13,Bone);
            c.Set(6,17,Mint); c.Set(21,17,Teal); c.Ellipse(1,18,7,3,Teal); c.Ellipse(20,18,7,3,Teal);
            if(frame==2) { c.Ellipse(13,7,4,5,Mint); c.Set(15,5,Pale); }
            c.Outline(Ink); return c;
        }

        static Canvas Spider(int frame)
        {
            var c = new Canvas(32,24); int stride=frame==1?2:frame==3?-2:0;
            for(int leg=0;leg<4;leg++)
            {
                int y=8+leg*3, bend=3+leg%2*2;
                c.Line(11,y,bend,y-4+stride,Hex(0xb68eaa)); c.Line(bend,y-4+stride,1,y+3,Violet);
                c.Line(21,y,31-bend,y-4-stride,Hex(0x8e6e97)); c.Line(31-bend,y-4-stride,30,y+3,Hex(0x663b69));
            }
            c.Ellipse(8,1,17,15,Hex(0x472b53)); c.Ellipse(10,2,13,12,Rust);
            c.Ellipse(12,3,8,7,Hex(0xc26780)); c.Rect(15,4,3,3,Gold);
            c.Ellipse(9,12,14,10,Hex(0x38293f));
            foreach(int x in new[]{11,15,19}) { c.Rect(x,14,2,2,Gold); c.Set(x,14,Pale); }
            c.Line(12,18,11,22,Bone); c.Line(20,18,21,22,Bone);
            if(frame==2) { c.Line(11,18,8,22,Pale); c.Line(21,18,24,22,Pale); }
            c.Outline(Ink); return c;
        }

        static Canvas MiniBoss(int frame)
        {
            var c = new Canvas(48,48); int stride=frame==1?2:frame==3?-2:0;
            c.Polygon(new[]{8,42,12,23,34,23,40,43,32,42,24,46,16,43},Hex(0x263a45));
            c.Polygon(new[]{13,26,31,26,35,41,26,40,22,44,16,40},Hex(0x536674));
            c.Rect(15,22,19,13,Hex(0x54624f)); c.Rect(17,23,14,9,Hex(0x94a486));
            c.Line(17,24,17,30,Hex(0xc5cbb0)); c.Rect(29,24,3,9,Hex(0x596d66));
            c.Rect(22,24,4,13,Gold); c.Rect(17,28,15,3,Gold);
            c.Ellipse(14,7,21,20,Hex(0x7b7772)); c.Ellipse(16,8,17,16,Bone);
            c.Rect(17,14,5,5,Ink); c.Rect(27,14,5,5,Ink); c.Set(18,15,Mint); c.Set(28,15,Mint);
            c.Rect(23,19,3,3,Ink); c.Rect(19,23,12,3,Shade);
            for(int x=19;x<31;x+=4)c.Set(x,23,Bone);
            c.Polygon(new[]{14,11,13,3,19,7,24,1,29,7,36,3,35,12},Hex(0x7a665c));
            c.Line(15,11,34,11,Gold); c.Rect(23,7,3,4,Teal); c.Set(24,7,Mint); c.Set(24,5,Mint);
            c.Ellipse(8,23,9,9,Stone); c.Line(8,24,5,35+stride,LightStone);
            c.Ellipse(32,23,9,9,Shade); c.Line(38,27,42,35-stride,Stone);
            c.Line(10,24,14,24,LightStone); c.Set(10,25,Bone); c.Line(34,25,37,25,Stone);
            c.Line(16,35,14,41,Hex(0x8b8a94)); c.Line(28,35,31,40,Hex(0x283140));
            c.Rect(14,40+stride,6,6,Stone); c.Rect(28,40-stride,6,6,Shade);
            int lift=frame==2?-6:0;
            c.Line(5,18+lift,5,44,Hex(0x745454));
            c.Polygon(new[]{5,18+lift,11,15+lift,14,20+lift,11,27+lift,5,26+lift},Hex(0x829b92));
            c.Line(11,16+lift,13,20+lift,Pale); c.Rect(3,31+lift,6,4,Bone);
            c.Set(16,28,Pale); c.Set(33,32,Mint); c.Outline(Ink); return c;
        }

        static Canvas SignatureWeapon(int kind)
        {
            var c = new Canvas(32,20);
            if(kind==0)
            {
                c.Polygon(new[]{1,11,8,8,13,9,12,14,6,14,3,17},Hex(0x81504d));
                c.Rect(9,7,21,6,Stone); c.Rect(11,7,7,4,Teal); c.Rect(17,5,4,3,Gold);
                c.Rect(26,6,5,8,Shade); c.Line(11,7,29,7,Bone); c.Rect(14,12,4,4,Gold);
                c.Rect(3,13,4,2,Gold); c.Set(20,8,Mint);
            }
            else if(kind==1)
            {
                c.Line(3,14,24,7,Hex(0x72525b)); c.Line(3,13,24,6,Gold);
                c.Polygon(new[]{19,6,23,1,29,2,31,7,28,12,22,11},Violet);
                c.Ellipse(22,4,7,5,Ink); c.Ellipse(24,4,3,4,Mint); c.Set(24,4,Pale);
                c.Set(17,9,Pink); c.Set(7,12,Bone);
            }
            else if(kind==2)
            {
                c.Line(2,12,24,8,Bone); c.Line(2,13,24,9,Hex(0xa28b8b));
                c.Ellipse(1,10,6,7,Bone); c.Ellipse(22,5,9,10,Hex(0xa18b8a));
                c.Ellipse(23,5,7,8,Bone); c.Rect(25,7,3,3,Ink); c.Set(26,7,Mint);
                c.Rect(12,8,3,7,Rust); c.Set(15,8,Gold);
            }
            else if(kind==3)
            {
                c.Rect(4,10,9,7,Shade); c.Rect(8,6,17,8,Hex(0x754b47));
                for(int x=11;x<26;x+=4) { c.Rect(x,5,2,10,Gold); c.Set(x,5,Bone); }
                c.Rect(25,4,5,12,Teal); c.Rect(27,6,3,8,Mint); c.Rect(29,8,2,4,Pale);
                c.Line(5,10,8,7,Mint); c.Set(2,9,Pale);
            }
            else if(kind==4)
            {
                c.Rect(6,11,10,5,Hex(0x69455b)); c.Ellipse(13,1,18,18,Violet);
                c.Ellipse(15,3,14,14,Bone); c.Ellipse(17,5,10,10,Teal);
                c.Polygon(new[]{18,5,26,5,21,10,26,13,19,14,23,9},Pale);
                c.Set(22,3,Gold); c.Set(28,10,Pink);
            }
            else if(kind==5)
            {
                c.Rect(4,11,8,6,Rust); c.Ellipse(10,3,19,14,Gold); c.Ellipse(13,5,13,10,Ink);
                c.Rect(16,5,6,10,Mint); c.Rect(18,6,2,7,Pale); c.Rect(11,2,17,2,Gold);
                c.Rect(27,4,4,12,Shade); c.Line(12,7,26,7,Bone); c.Line(12,12,26,12,Bone);
            }
            else
            {
                c.Line(2,13,29,6,Hex(0x8f7778)); c.Line(2,12,29,5,Bone);
                c.Line(3,11,28,4,Pale); c.Ellipse(1,11,5,6,Bone); c.Ellipse(27,3,5,6,Bone);
                for(int x=9;x<25;x+=5)c.Rect(x,9-(x/10),2,2,Ink);
                c.Rect(14,7,2,4,Rust);
            }
            c.Outline(Ink); return c;
        }

        static Canvas ShotDot() => ShotCapsule(1);
        static Canvas ShotBone()
        {
            var c = new Canvas(14,10);
            c.Ellipse(1,1,5,4,Hex(0x856282)); c.Ellipse(1,5,5,4,Hex(0x856282));
            c.Ellipse(8,1,5,4,Hex(0x856282)); c.Ellipse(8,5,5,4,Hex(0x856282));
            c.Rect(4,3,6,4,Bone); c.Ellipse(2,2,4,6,Bone); c.Ellipse(8,2,4,6,Bone);
            c.Line(4,3,10,3,Pale); c.Rect(6,4,5,2,Hex(0xfff5cf));
            c.Line(4,7,10,7,Hex(0xc1a392)); c.Outline(Ink); return c;
        }
        static Canvas ShotShock()
        {
            var c = new Canvas(14,12);
            c.Polygon(new[]{1,6,5,2,9,2,13,6,9,10,5,10},Hex(0x286778));
            c.Polygon(new[]{2,6,6,3,10,3,12,6,8,9,5,8},Mint);
            c.Polygon(new[]{7,2,5,6,8,6,6,10,11,5,8,5,10,2},Pale);
            c.Set(2,6,Hex(0x64b8d6)); c.Outline(Ink); return c;
        }
        static Canvas ShotDisc(int frame)
        {
            var c = new Canvas(14,14); c.Ellipse(1,1,12,12,Hex(0x34476c));
            c.Ellipse(2,2,10,10,Hex(0x91cee6)); c.Ellipse(4,4,6,6,Hex(0x456e9c));
            c.Ellipse(5,5,4,4,Gold); c.Set(6,5,Pale);
            int[][] points={new[]{3,3,10,10},new[]{6,2,7,11},new[]{10,3,3,10},new[]{11,6,2,7}};
            var q=points[frame%4]; c.Rect(q[0],q[1],2,2,Pale); c.Rect(q[2],q[3],2,2,Mint);
            c.Outline(Ink); return c;
        }
        static Canvas ShotRay()
        {
            var c = new Canvas(8,4);
            for(int x=0;x<8;x++)
            {
                byte core=(byte)(x==0||x==7?95:x==1||x==6?190:235);
                c.Set(x,1,Alpha(Hex(0xe7eee0),core)); c.Set(x,2,Alpha(Hex(0xd2e0d9),(byte)(core*3/4)));
                c.Set(x,0,Alpha(Hex(0xc5d8d0),(byte)(core/8))); c.Set(x,3,Alpha(Hex(0xc5d8d0),(byte)(core/8)));
            }
            return c;
        }

        static Canvas ShotHalo()
        {
            var c = new Canvas(20,20);
            for(int y=0;y<20;y++) for(int x=0;x<20;x++)
            {
                double dx=(x-9.5)/9.5,dy=(y-9.5)/9.5,d=dx*dx+dy*dy;
                if(d<1) c.Set(x,y,new Color32(255,249,232,(byte)(Math.Pow(1-d,2.5)*96)));
            }
            return c;
        }

        static Canvas ShotTrail()
        {
            var c = new Canvas(12,4);
            // Local +X is the tip; the low opacity tail stays behind the moving shot.
            byte[] opacity={0,8,16,28,42,58,78,98,122,146,124,60};
            for(int x=0;x<12;x++)
            {
                c.Set(x,1,new Color32(244,241,228,opacity[x]));
                if(x>3) c.Set(x,2,new Color32(235,236,226,(byte)(opacity[x]/2)));
                if(x>7&&x<11) c.Set(x,0,new Color32(235,236,226,(byte)(opacity[x]/9)));
            }
            return c;
        }

        static Canvas ImpactDust()
        {
            var c = new Canvas(8,8); Color32 dust=Hex(0xc6beae);
            c.Rect(1,3,3,3,Alpha(dust,34)); c.Rect(1,4,3,2,Alpha(dust,110));
            c.Rect(4,1,3,3,Alpha(dust,26)); c.Rect(4,2,2,2,Alpha(dust,85));
            c.Rect(5,5,2,2,Alpha(dust,28)); c.Rect(5,6,2,1,Alpha(dust,75)); return c;
        }
        static Canvas SpawnRift(int frame)
        {
            var c = new Canvas(32,16); int lift=frame==1?1:frame==2?3:frame==3?2:0;
            c.Ellipse(2,5,28,10,Alpha(Ink,160)); c.Ellipse(4,5,24,8,Hex(0x34243e));
            c.Ellipse(6,7,20,5,Ink); c.Line(7,9,14,7,Violet); c.Line(14,7,25,10,Mint);
            c.Rect(1,8-lift,4,3,Stone); c.Rect(27,10-lift,3,3,Shade); c.Set(4,7-lift,Bone);
            c.Rect(9,4-lift,3,2,LightStone); c.Set(10,3-lift,Pale); c.Rect(22,5-lift,3,2,Stone);
            c.Set(15,7-lift,Mint); c.Set(18,11-lift,Violet); return c;
        }
        static Canvas Pit()
        {
            var c = new Canvas(48,32);
            c.Polygon(new[]{5,6,15,2,33,2,44,8,46,20,38,29,16,30,3,23,1,13},Hex(0x545160));
            c.Polygon(new[]{7,8,15,5,34,5,41,10,43,19,35,26,15,27,6,21,4,14},Hex(0x242536));
            c.Polygon(new[]{9,10,17,7,32,7,39,11,39,19,32,24,16,24,9,19,7,14},Ink);
            c.Line(5,8,15,4,LightStone); c.Line(15,4,32,4,Hex(0x79737b)); c.Line(34,5,42,10,Hex(0x79737b));
            c.Line(6,15,9,20,Hex(0x33313e)); c.Line(10,22,16,25,Hex(0x3b3547));
            c.Line(15,7,17,11,Hex(0x111322)); c.Line(31,7,30,11,Hex(0x111322));
            c.Set(7,8,Bone); c.Set(36,5,LightStone); c.Set(13,28,Stone); return c;
        }
        static Canvas Fog()
        {
            var c = new Canvas(64,32);
            for(int y=0;y<32;y++) for(int x=0;x<64;x++)
            {
                double dx=(x-31.5)/32,dy=(y-15.5)/16;
                double edge=Math.Max(0,1-dx*dx-dy*dy);
                double curl=.65+.2*Math.Sin(x*.23+y*.19)+.15*Math.Cos(x*.14-y*.35);
                byte alpha=(byte)(Math.Max(0,edge*curl)*75);
                c.Set(x,y,new Color32(156,174,182,alpha));
            }
            return c;
        }

        static Canvas LegacyMedium(int frame)
        {
            var c = new Canvas(24, 28);
            int sway = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            c.Polygon(new[] { 4,25, 7,12, 3,9, 12,1, 21,9, 17,13, 21+sway,25, 15,24, 12,26, 8,24 }, Hex(0x5a365f));
            c.Polygon(new[] { 6,20, 8,12, 16,12, 19+sway,23, 15,22, 12,24, 9,22 }, Violet);
            c.Polygon(new[] { 9,12, 15,12, 17+sway,23, 13,22, 12,24, 9,23 }, Hex(0x302a4b));
            c.Line(6,22,9,16,Pink); c.Line(17,16,19+sway,23,Hex(0x7a5899));
            c.Ellipse(7,6,11,9,Ink); c.Rect(8,8,9,4,Hex(0x4b3a59));
            c.Rect(9,9,2,2,Mint); c.Rect(14,9,2,2,Mint); c.Set(9,9,Pale); c.Set(14,9,Pale);
            c.Rect(10,13,5,2,Bone); c.Rect(11,14,3,1,Hex(0xa88bba));
            c.Polygon(new[] { 5,8, 9,3, 12,2, 16,5, 19,8, 16,7, 12,4, 8,8 }, Hex(0xd1a8d5));
            c.Rect(11,5,3,2,Gold); c.Set(12,5,Pale);
            c.Rect(11,16,3,6,Gold); c.Rect(12,17,1,3,Pale);
            c.Rect(5,16+sway,3,5,Hex(0x865a9b)); c.Rect(5,20+sway,3,2,Bone);
            c.Rect(17,16-sway,3,5,Hex(0x865a9b)); c.Rect(17,20-sway,3,2,Bone);
            c.Ellipse(1,12+sway,4,4,Teal); c.Ellipse(2,12+sway,2,2,Mint);
            c.Ellipse(19,3-sway,4,4,Violet); c.Set(20,4-sway,Pale);
            c.Set(3,8+sway,Mint); c.Set(21,13-sway,Pink);
            if(frame==2) { c.Ellipse(0,11,5,5,Mint); c.Ellipse(19,10,5,5,Violet); c.Rect(11,16,3,3,Pale); c.Set(12,12,Pink); }
            HeroDepth(c, 1, frame); c.Outline(Ink); return c;
        }

        static Canvas LegacyReaper(int frame)
        {
            var c = new Canvas(24,28); int stride = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            c.Polygon(new[] { 4,25, 7,13, 5,8, 10,2, 15,2, 20,8, 18,14, 22+stride,25, 15,24, 12,26, 8,24 }, Hex(0x532c45));
            c.Polygon(new[] { 7,24, 9,13, 16,13, 19+stride,24, 14,23, 12,25 }, Rust);
            c.Polygon(new[] { 9,14, 14,14, 16,24, 12,23, 10,25 }, Hex(0x281f34));
            c.Line(6,21,9,15,Orange); c.Line(17,17,19+stride,23,Hex(0xca6472));
            c.Ellipse(8,6,10,10,Bone); c.Rect(9,7,2,6,Pale); c.Rect(9,9,3,3,Ink); c.Rect(14,9,3,3,Ink);
            c.Set(10,10,Orange); c.Set(15,10,Orange); c.Rect(12,12,2,2,Ink);
            c.Line(10,14,15,14,Ink); c.Set(11,14,Bone); c.Set(14,14,Bone);
            c.Rect(8,17,9,2,Hex(0x9a6c71)); c.Rect(11,17,3,3,Gold); c.Set(12,18,Pale);
            c.Rect(5,17+stride,3,5,Rust); c.Rect(5,21+stride,3,2,Bone);
            c.Rect(18,17-stride,3,5,Rust); c.Rect(18,21-stride,3,2,Bone);
            c.Line(2,5,2,23,Hex(0x8f8495)); c.Polygon(new[] { 2,5, 5,2, 10,2, 7,4, 4,5 }, Bone);
            c.Line(3,5,8,3,Pale); c.Rect(8,25+stride,3,2,Stone); c.Rect(15,25-stride,3,2,Stone);
            if(frame==2) { c.Rect(9,10,3,1,Gold); c.Rect(14,10,3,1,Gold); c.Line(5,4,9,1,Orange); c.Rect(4,19,4,2,Bone); }
            HeroDepth(c, 2, frame); c.Outline(Ink); return c;
        }

        static Canvas LegacyAlchemist(int frame)
        {
            var c = new Canvas(24,28); int stride = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            c.Rect(2,12,5,12,Hex(0x614a43)); c.Rect(3,13,3,8,Gold); c.Rect(3,14,2,5,Teal);
            c.Rect(19,12,3,10,Hex(0x725f46)); c.Rect(19,13,2,6,Gold);
            c.Rect(7,22+stride,4,5,Shade); c.Rect(14,22-stride,4,5,Shade);
            c.Rect(6,25+stride,5,2,Bone); c.Rect(14,25-stride,5,2,Bone);
            c.Polygon(new[] { 7,13, 17,13, 20,23, 15,24, 12,22, 7,24, 5,21 }, Hex(0x617749));
            c.Rect(9,13,6,8,Hex(0x83905d)); c.Rect(10,15,3,5,Bone); c.Rect(11,17,1,2,Gold);
            c.Rect(6,20,13,2,Hex(0x654b47)); c.Rect(11,20,3,2,Gold);
            c.Rect(8,6,10,9,Hex(0xa36d58)); c.Rect(9,7,8,6,Hex(0xe6ba8e));
            c.Rect(7,4,12,4,Hex(0x65784c)); c.Rect(9,3,8,2,Hex(0x91a26b)); c.Rect(7,7,12,3,Hex(0x705943));
            c.Ellipse(7,6,6,6,Gold); c.Ellipse(13,6,6,6,Gold); c.Ellipse(8,7,4,4,Teal); c.Ellipse(14,7,4,4,Teal);
            c.Set(9,7,Pale); c.Set(15,7,Pale); c.Rect(12,12,4,2,Bone);
            c.Rect(5,16+stride,3,5,Hex(0x83905d)); c.Rect(5,20+stride,3,2,Bone);
            c.Rect(18,16-stride,3,5,Hex(0x83905d)); c.Rect(18,20-stride,3,2,Bone);
            c.Ellipse(1,20+stride,5,5,Teal); c.Rect(2,19+stride,2,2,Gold); c.Set(2,22+stride,Mint);
            if(frame==2) { c.Ellipse(0,15,5,5,Mint); c.Set(2,12,Mint); c.Set(0,10,Pale); c.Set(10,8,Pale); c.Set(16,8,Pale); }
            HeroDepth(c, 3, frame); c.Outline(Ink); return c;
        }

        static Canvas NewCreature(int kind, int frame)
        {
            var c = new Canvas(28, 28);
            int step = frame % 2;
            if (kind == 0)
            {
                for (int i = 0; i < 5; i++) c.Ellipse(3+i*4,13+(i+frame)%2,8,9, i%2==0 ? Teal : Mint);
                c.Ellipse(16,9,10,10,Teal); c.Rect(18,11,2,2,Gold); c.Rect(23,11,2,2,Gold);
                for (int i=0;i<5;i++) c.Line(5+i*4,21,3+i*4,24-step, Bone);
            }
            else if (kind == 1)
            {
                c.Rect(8,20,4,6-step,Bone); c.Rect(17,20,4,6+step,Bone);
                c.Rect(8,11,13,11,Stone); c.Rect(12,11,5,12,Bone);
                c.Ellipse(7,2,15,13,Bone); c.Rect(9,6,4,4,Ink); c.Rect(17,6,3,4,Ink);
                c.Set(10,7,Orange); c.Set(18,7,Orange); c.Rect(12,12,6,2,Ink);
                c.Line(4,14,4,25,Gold); c.Rect(2,12,5,3,LightStone);
            }
            else
            {
                c.Polygon(new[] { 5,25, 8,11, 14,3, 20,11, 24,25 }, Rust);
                c.Polygon(new[] { 10,10, 14,6, 18,10, 17,15, 11,15 }, Ink);
                c.Rect(11,11,6,2,Pink); c.Rect(13,17,3,7,Gold);
                c.Line(4,10,4,24,Bone); c.Ellipse(1,6+step,7,6,Gold);
            }
            c.Outline(Ink); return c;
        }

        static Canvas Widow(int frame)
        {
            var c = new Canvas(56,56); int sway = frame == 1 ? 2 : frame == 3 ? -2 : 0;
            // Four pairs of articulated legs frame a shrouded bride and a spectral veil.
            for (int i=0;i<4;i++)
            {
                int y=24+i*6, bend=8+i*2;
                c.Line(19,y,bend,y-7+sway,Bone); c.Line(bend,y-7+sway,2+i,y+5,Hex(0x827da2));
                c.Line(37,y,56-bend,y-7-sway,Bone); c.Line(56-bend,y-7-sway,53-i,y+5,Hex(0x827da2));
                c.Set(bend,y-7+sway,Pale); c.Set(56-bend,y-7-sway,Pale);
            }
            c.Polygon(new[] { 15,23, 18,12, 26,5, 33,7, 39,15, 42+sway,39, 38,47, 34,44, 29,52, 24,47, 18,49, 12+sway,43 }, Hex(0x747da1));
            c.Polygon(new[] { 20,16, 28,8, 35,14, 38+sway,42, 32,39, 29,47, 24,41, 18+sway,45 }, Hex(0xb9c4d9));
            c.Polygon(new[] { 22,25, 34,25, 39,46, 31,44, 27,51, 23,46, 16,49 }, Hex(0x8c85ae));
            c.Line(22,30,18+sway,44,Pale); c.Line(33,30,37+sway,43,Pale); c.Line(26,35,25,46,Hex(0xd6dce8));
            c.Ellipse(20,12,16,18,Hex(0x3b3656)); c.Ellipse(23,13,10,13,Bone);
            c.Rect(23,18,3,4,Ink); c.Rect(30,18,3,4,Ink); c.Set(24,19,Pink); c.Set(31,19,Pink);
            c.Ellipse(26,23,4,5,Ink); c.Set(27,24,Pink);
            c.Polygon(new[] { 17,13, 20,6, 27,2, 35,6, 39,13, 34,12, 28,6, 22,11 }, Bone);
            c.Rect(19,9,18,2,Hex(0xd1a5bd)); c.Ellipse(25,6,6,5,Pink); c.Set(27,7,Pale);
            c.Line(16,16,13+sway,35,Hex(0xe2e5e8)); c.Line(40,16,42+sway,36,Hex(0xe2e5e8));
            c.Rect(25,30,6,4,Hex(0x685273)); c.Rect(27,31,2,2,Gold);
            c.Ellipse(19,42,18,10,Hex(0x423b65)); c.Line(22,45,33,45,Pink);
            if(frame==2) { c.Ellipse(24,21,8,9,Ink); c.Ellipse(26,23,4,5,Pink); c.Line(15,19,10,17,Pale); c.Line(40,19,45,17,Pale); }
            c.Outline(Ink); return c;
        }

        static Canvas Bishop(int frame)
        {
            var c = new Canvas(56,56); int arm=frame==1?-2:frame==3?2:0;
            c.Polygon(new[] { 16,25, 39,25, 47,49, 35,47, 28,53, 20,49, 10,51 }, Hex(0x4f304c));
            c.Polygon(new[] { 21,27, 35,27, 41,48, 33,46, 28,51, 19,48 }, Rust);
            c.Polygon(new[] { 25,27, 31,27, 35,48, 28,51, 23,47 }, Hex(0xbaa28f));
            c.Line(16,31,11,48,Hex(0xa65d66)); c.Line(39,31,45,47,Hex(0x8c4b65));
            c.Rect(22,36,12,3,Gold); c.Rect(27,32,3,14,Gold); c.Set(28,35,Pale);
            c.Polygon(new[] { 17,27, 12,31+arm, 5,29+arm, 6,24+arm, 15,22 }, Hex(0xa45b64));
            c.Polygon(new[] { 38,24, 45,24-arm, 49,30-arm, 43,33-arm, 36,29 }, Hex(0xa45b64));
            c.Rect(4,24+arm,6,5,Bone); c.Rect(44,26-arm,5,5,Bone);
            c.Ellipse(19,15,18,16,Hex(0x8d737c)); c.Ellipse(22,15,12,15,Bone);
            c.Rect(22,20,4,4,Ink); c.Rect(30,20,4,4,Ink); c.Set(23,21,Orange); c.Set(31,21,Orange);
            c.Rect(26,26,5,2,Ink); c.Rect(25,27,2,2,Shade);
            c.Polygon(new[] { 18,18, 19,7, 24,1, 28,5, 32,1, 37,8, 38,18 }, Hex(0x8c445e));
            c.Polygon(new[] { 21,15, 22,8, 25,5, 28,10, 31,5, 34,9, 35,15 }, Gold);
            c.Rect(27,8,3,8,Bone); c.Rect(24,11,9,2,Bone); c.Rect(18,16,20,3,Gold);
            c.Line(48,12,48,48,Hex(0xa79bac)); c.Rect(47,14,3,30,Hex(0x756580));
            c.Ellipse(42,3-arm,13,14,Gold); c.Ellipse(44,5-arm,9,10,Ink);
            c.Rect(47,5-arm,3,10,Orange); c.Rect(44,8-arm,9,3,Orange); c.Set(48,8-arm,Pale);
            c.Line(17,47,15,53,Hex(0x6b5a69)); c.Line(39,47,41,53,Hex(0x6b5a69));
            if(frame==2) { c.Rect(22,20,4,2,Gold); c.Rect(30,20,4,2,Gold); c.Line(44,2,40,6,Orange); c.Line(51,2,54,5,Orange); c.Set(48,1,Pale); }
            c.Outline(Ink); return c;
        }

        static Canvas NewGuardian(bool bell, int frame)
        {
            var c = new Canvas(56,56); int sway=frame==1?2:frame==3?-2:0;
            if (bell)
            {
                c.Line(8,2,13,18,Hex(0x786b8d)); c.Line(46,2,41,18,Hex(0x786b8d));
                for(int i=0;i<4;i++) { c.Rect(6+i,2+i*3,5,2,LightStone); c.Rect(45-i,2+i*3,5,2,LightStone); }
                c.Rect(24,3,8,7,Hex(0x7b5575)); c.Rect(25,4,6,4,Gold);
                c.Polygon(new[] { 6,43, 13,33, 14,18, 21,10, 35,10, 42,18, 43,33, 50,43 }, Hex(0x513c62));
                c.Polygon(new[] { 10,41, 17,32, 18,19, 24,13, 33,13, 38,19, 39,33, 46,41 }, Hex(0xb48568));
                c.Polygon(new[] { 15,39, 21,31, 22,20, 27,16, 32,19, 34,32, 40,39 }, Gold);
                c.Line(18,20,17,33,Hex(0xe8be83)); c.Line(38,22,39,33,Rust);
                c.Rect(8,40,40,6,Hex(0x7c5365)); c.Rect(9,40,38,2,Gold); c.Line(10,45,46,45,Bone);
                for(int i=0;i<6;i++) { c.Rect(12+i*6,41,3,3,Ink); c.Set(13+i*6,42,Violet); }
                c.Polygon(new[] { 19,24, 25,26, 23,30, 18,29 },Ink); c.Polygon(new[] { 38,24, 31,26, 33,30, 39,29 },Ink);
                c.Set(20,27,Pink); c.Set(35,27,Pink);
                c.Polygon(new[] { 25,32, 31,32, 34,38, 22,38 },Ink); c.Line(25,35,30,35,Pink);
                c.Ellipse(22+sway,46,12,8,Hex(0x8f637b)); c.Ellipse(24+sway,46,8,6,Gold); c.Set(26+sway,47,Pale);
                c.Line(7,20,3,36,Violet); c.Line(49,20,53,36,Violet);
                c.Set(4,20+sway,Pink); c.Set(51,14-sway,Pink);
                if(frame==2) { c.Ellipse(23,30,11,9,Ink); c.Line(25,32,30,32,Pink); c.Line(2,10,7,10,Violet); c.Line(49,10,54,10,Violet); c.Set(28,8,Pale); }
            }
            else
            {
                for(int i=0;i<4;i++)
                {
                    int y=24+i*6,offset=frame==i?3:sway;
                    c.Line(18,y,5+i,y-10+offset,Hex(0x517469)); c.Line(5+i,y-10+offset,1+i,y+5,Bone);
                    c.Line(38,y,50-i,y-10-offset,Hex(0x517469)); c.Line(50-i,y-10-offset,54-i,y+5,Bone);
                    c.Set(6+i,y-10+offset,Mint); c.Set(49-i,y-10-offset,Mint);
                }
                c.Ellipse(13,26,31,27,Hex(0x334856)); c.Ellipse(16,27,25,24,Hex(0x637f67));
                c.Ellipse(21,28,15,20,Hex(0x86996d));
                for(int i=0;i<4;i++) { c.Line(19,32+i*4,37,32+i*4,Hex(0x425a58)); c.Set(25+i%2*5,33+i*4,Mint); }
                c.Ellipse(14,10,28,24,Hex(0x386865)); c.Ellipse(17,11,23,21,Teal);
                c.Polygon(new[] { 15,18, 11,9, 15,5, 21,13, 28,6, 35,13, 42,5, 46,9, 41,20 },Hex(0x69916b));
                c.Line(14,8,19,14,Bone); c.Line(42,8,37,14,Bone);
                for(int i=0;i<3;i++) { c.Ellipse(18+i*7,15,6,6,Ink); c.Ellipse(19+i*7,16,4,4,Gold); c.Set(20+i*7,17,Pale); }
                c.Polygon(new[] { 19,25, 24,27, 28,25, 32,27, 37,25, 35,31, 22,31 },Ink);
                c.Polygon(new[] { 21,25, 24,31, 26,25 },Bone); c.Polygon(new[] { 31,25, 34,31, 36,25 },Bone);
                c.Rect(27,28,3,4,Mint); c.Set(28,33,Alpha(Mint,180));
                c.Ellipse(20,39,7,7,Hex(0xb1bb78)); c.Ellipse(31,43,7,7,Hex(0xb1bb78)); c.Set(22,40,Pale); c.Set(33,44,Pale);
            }
            c.Outline(Ink); return c;
        }

        static Canvas NewWeapon(int kind)
        {
            var c = new Canvas(32,18);
            c.Polygon(new[] { 3,8, 11,8, 11,12, 8,12, 6,16, 3,14 },Hex(0x79515a));
            c.Line(4,11,7,13,Hex(0xc18f77)); c.Rect(10,7,9,6,Hex(0x52556c)); c.Line(11,7,18,7,Bone);
            c.Rect(10,12,5,2,Gold); c.Rect(14,9,3,2,Gold);
            if(kind==0)
            {
                c.Rect(16,5,13,4,Hex(0x898b9e)); c.Rect(16,10,13,4,Hex(0x616b80));
                c.Line(17,5,29,5,Pale); c.Line(17,10,29,10,LightStone);
                c.Rect(27,4,4,11,Hex(0x363a51)); c.Rect(28,5,2,3,Gold); c.Rect(28,10,2,3,Gold);
                c.Rect(18,8,7,2,Hex(0x825d55)); c.Rect(12,5,4,3,Gold); c.Set(13,5,Pale);
            }
            else if(kind==1)
            {
                c.Rect(17,7,12,4,Hex(0x9098ad)); c.Line(18,7,29,7,Pale);
                c.Rect(27,6,4,6,Shade); c.Rect(28,7,2,3,Gold);
                c.Ellipse(11,9,9,8,Hex(0x8f5c63)); c.Ellipse(13,10,5,6,Gold); c.Rect(15,11,1,4,Orange);
                c.Rect(16,3,3,4,Gold); c.Rect(17,4,6,2,Hex(0xa1b5bb)); c.Rect(23,4,2,2,Mint);
                c.Rect(8,5,3,3,LightStone); c.Set(9,5,Pale);
            }
            else
            {
                c.Ellipse(10,3,19,12,Hex(0x4d6b78)); c.Ellipse(13,5,14,8,Teal);
                c.Line(14,5,24,5,Mint); c.Rect(19,5,2,9,Gold); c.Rect(11,6,3,7,Gold);
                c.Ellipse(24,3,7,12,Shade); c.Ellipse(25,5,5,8,Mint); c.Ellipse(26,6,4,6,Ink);
                c.Ellipse(15,6,5,5,Mint); c.Set(16,7,Pale); c.Rect(16,1,4,3,Gold);
                c.Set(6,5,Mint); c.Line(5,6,10,7,Teal);
            }
            c.Outline(Ink); return c;
        }

        static Canvas LegacyPlayer(int frame)
        {
            var c = new Canvas(24, 28);
            int stride = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            int cape = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            // Navy witch-hunter hat, ivory shoulder mantle and a turquoise coat.
            c.Polygon(new[] { 6,16, 17,16, 20+cape,25, 14,24, 11,26, 4+cape,24 }, Hex(0x226575));
            c.Polygon(new[] { 6,16, 11,16, 10,24, 6+cape,23 }, Teal);
            c.Line(5+cape,21, 4+cape,24, Mint);
            c.Rect(7,23+stride, 4,3, Hex(0x393e54)); c.Rect(13,23-stride,4,3, Hex(0x393e54));
            c.Rect(6,25+stride,5,2, Hex(0x776680)); c.Rect(13,25-stride,5,2, Hex(0x776680));
            c.Rect(7,25+stride,3,1, Bone); c.Rect(14,25-stride,3,1, Bone);
            c.Rect(8,14,9,8, Teal); c.Rect(8,14,3,8, Mint); c.Rect(15,15,3,8, Hex(0x1e606d));
            c.Polygon(new[] { 7,14, 11,14, 12,17, 9,18, 5,17 }, Bone);
            c.Polygon(new[] { 13,14, 17,14, 20,18, 15,17 }, Pale);
            c.Rect(10,19,6,2, Hex(0x59434f)); c.Rect(12,19,2,2, Gold);
            c.Rect(5,17+stride,3,5, Teal); c.Rect(5,21+stride,3,2, Bone);
            c.Rect(17,17-stride,3,5, Hex(0x2b7780)); c.Rect(17,21-stride,3,2, Bone);
            c.Rect(8,8,9,6, Hex(0xb77561)); c.Rect(9,8,7,5, Hex(0xf3cfa0));
            c.Rect(9,10,2,2, Ink); c.Rect(14,10,2,2, Ink); c.Set(9,10,Pale); c.Set(14,10,Pale);
            c.Rect(11,13,4,2, Orange); c.Rect(14,14,3,3, Gold);
            c.Polygon(new[] { 6,7, 9,1, 15,2, 17,7 }, Hex(0x34516c));
            c.Polygon(new[] { 9,2, 11,2, 9,6, 7,7 }, Teal);
            c.Rect(6,6,12,2, Hex(0x7d5362)); c.Rect(10,6,3,2, Gold); c.Set(11,6,Pale);
            c.Polygon(new[] { 2,8, 6,7, 18,7, 22,9, 20,10, 4,10 }, Hex(0x30465f));
            c.Line(4,8,20,8,Mint); c.Line(3,9,7,9,Teal);
            // Brass buckles, embroidered cuffs and a glowing feather distinguish the hunter.
            c.Rect(9,16,1,2,Gold); c.Rect(15,16,1,2,Gold); c.Set(9,17,Pale);
            c.Line(17,6,20,2,Hex(0xc9d7c2)); c.Set(21,1,Mint); c.Line(18,5,20,4,Teal);
            c.Set(8,22,Bone); c.Set(16,22,Bone); c.Set(10,20,Pale);
            if(frame==2) { c.Line(18,5,21,3,Pale); c.Rect(5,20,3,2,Bone); c.Rect(17,18,3,2,Bone); c.Set(14,13,Pale); }
            HeroDepth(c, 0, frame); c.Outline(Ink);
            return c;
        }

        static Canvas Pumpkin(int frame)
        {
            var c = new Canvas(24,24);
            int step = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            c.Line(8,18,6,21+step,Hex(0x9f4b33)); c.Line(6,21+step,3,21+step,Gold);
            c.Line(16,18,18,21-step,Hex(0x9f4b33)); c.Line(18,21-step,21,21-step,Orange);
            c.Ellipse(2,5,20,16,Rust); c.Ellipse(3,5,18,14,Orange);
            c.Ellipse(5,6,5,12,Hex(0xffaa49)); c.Ellipse(13,6,4,12,Hex(0xe36729));
            c.Line(11,6,10,9,Hex(0xc5592c)); c.Line(17,7,18,10,Rust);
            c.Polygon(new[] { 5,9, 10,11, 5,12 },Ink); c.Polygon(new[] { 18,9,13,11,18,12 },Ink);
            c.Set(6,10,Gold); c.Set(17,10,Gold); c.Polygon(new[]{11,12,13,12,12,14},Ink);
            c.Polygon(new[]{5,14,8,15,10,14,12,16,15,14,18,15,17,18,8,18},Ink);
            c.Line(8,16,16,16,Gold); c.Set(9,15,Gold); c.Set(15,17,Gold);
            c.Rect(10,2,3,4,Hex(0x40764b)); c.Line(12,2,15,1,Mint);
            c.Polygon(new[]{13,3,16,2,18,4,14,5},Hex(0x73a756));
            c.Outline(Ink); return c;
        }

        static Canvas Ghost(int frame)
        {
            var c = new Canvas(24,28);
            int sway = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            c.Ellipse(4,2,16,19,Hex(0xb89bd1)); c.Ellipse(5,2,13,18,Bone);
            c.Ellipse(7,3,9,15,Pale);
            c.Polygon(new[]{4,12,7,13,7,19,4+sway,23,7+sway,24,10+sway,21,13+sway,25,16+sway,21,20+sway,23,19,12},Bone);
            c.Line(5,17,4+sway,22,Hex(0xb89bd1)); c.Line(17,16,19+sway,21,Hex(0xb89bd1));
            c.Polygon(new[]{3,10,5,12,4,17,1,18,2,14},Bone);
            c.Polygon(new[]{20,10,21,14,23,17,20,18,18,13},Hex(0xd0bddb));
            c.Rect(7,8,4,5,Ink); c.Rect(14,8,4,5,Ink);
            c.Rect(8,9,2,2,Pink); c.Rect(15,9,2,2,Pink); c.Set(8,9,Pale); c.Set(15,9,Pale);
            c.Ellipse(10,14,5,6,Ink); c.Rect(11,15,3,2,Rust);
            c.Line(7,5,9,4,Pale); c.Set(5,23,Hex(0x9186c5));
            c.Outline(Ink); return c;
        }

        static Canvas Cultist(int frame)
        {
            var c = new Canvas(24,28);
            int arm = frame == 1 ? -1 : frame == 3 ? 1 : 0;
            c.Polygon(new[]{8,14,16,14,21,25,15,25,12,24,7,26,3,25},Hex(0x784295));
            c.Polygon(new[]{8,15,10,16,8+arm,24,5,24},Violet);
            c.Polygon(new[]{15,15,17,17,19,24,15,24},Hex(0x482c67));
            c.Line(10,17,10,23,Rust); c.Line(14,17,14,23,Pink);
            c.Rect(9,21,7,2,Hex(0x291c44)); c.Rect(11,21,3,2,Gold);
            c.Polygon(new[]{4,17,8,15,9,19,6,22+arm,2,21+arm},Hex(0x9d56ad));
            c.Polygon(new[]{17,15,20,17,22,21-arm,18,22-arm,15,19},Hex(0x9d56ad));
            c.Rect(2,21+arm,4,2,Bone); c.Rect(18,21-arm,4,2,Bone);
            c.Polygon(new[]{5,11,8,4,12,1,17,5,20,12,17,15,7,15},Hex(0x9e58b5));
            c.Polygon(new[]{7,10,9,5,12,3,12,8,9,12},Hex(0xc18dd2));
            c.Polygon(new[]{8,10,16,10,17,14,7,14},Ink);
            c.Rect(8,11,3,2,Pink); c.Rect(14,11,3,2,Pink); c.Set(8,11,Pale); c.Set(14,11,Pale);
            c.Rect(10,15,5,3,Rust); c.Rect(12,16,1,4,Gold);
            c.Line(12,5,13,8,Gold); c.Set(12,7,Pink);
            c.Outline(Ink); return c;
        }

        static Canvas Bat(int frame)
        {
            var c = new Canvas(28,20);
            int tip = frame == 0 ? 2 : frame == 1 ? 7 : frame == 2 ? 12 : 5;
            c.Polygon(new[]{11,8,7,5,1,tip,2,10,5,12,7,15,11,12},Hex(0x9652b0));
            c.Polygon(new[]{17,8,21,5,27,tip,26,10,23,12,21,15,17,12},Hex(0x9652b0));
            c.Line(2,tip+1,10,9,Pink); c.Line(26,tip+1,18,9,Pink);
            c.Line(6,8,7,13,Hex(0x592b76)); c.Line(22,8,21,13,Hex(0x592b76));
            c.Polygon(new[]{10,7,10,2,13,6,15,6,18,2,18,8,19,12,16,16,12,16,9,12},Hex(0x6b3b89));
            c.Ellipse(11,7,7,8,Hex(0xb26bc0)); c.Rect(11,8,3,3,Ink); c.Rect(15,8,3,3,Ink);
            c.Rect(12,9,2,1,Pink); c.Rect(15,9,2,1,Pink);
            c.Rect(13,12,3,2,Bone); c.Set(13,14,Pale); c.Set(15,14,Pale);
            c.Outline(Ink); return c;
        }

        static Color32 ThemeBase(int theme) => theme == 0 ? Hex(0x343e36) : theme == 1 ? Hex(0x394355)
            : theme == 2 ? Hex(0x493a41) : theme == 3 ? Hex(0x344338) : Hex(0x302b48);
        static Color32 ThemeTrim(int theme) => theme == 0 ? Hex(0xa58c5b) : theme == 1 ? Hex(0xb7c9dc)
            : theme == 2 ? Hex(0xd99767) : theme == 3 ? Hex(0x9db36b) : Hex(0xb38bcd);

        static Canvas ThemeFloor(int theme)
        {
            var c = new Canvas(16,16); Color32 ground=ThemeBase(theme), trim=ThemeTrim(theme);
            c.Rect(0,0,16,16,ground);
            for(int y=0;y<16;y++) for(int x=0;x<16;x++)
                if((x*29+y*43+x*y*3+theme*17)%37==0) c.Set(x,y,Alpha(trim,70));
            if(theme==0)
            {
                // Broad timber slats, iron nails, broken roots and fallen leaves.
                c.Rect(0,1,16,3,Hex(0x44423b)); c.Rect(0,6,16,4,Hex(0x45493b)); c.Rect(0,12,16,3,Hex(0x3e4439));
                c.Line(1,1,14,1,Hex(0x665b48)); c.Line(0,6,15,6,Hex(0x5b5d46));
                c.Line(5,10,7,6,Hex(0x26372f)); c.Line(7,6,10,4,Hex(0x26372f));
                c.Set(2,2,Hex(0x929181)); c.Set(13,8,Hex(0x929181));
                c.Polygon(new[] { 11,11, 14,10, 13,13, 10,14 },Hex(0x92704c)); c.Set(12,12,Gold);
            }
            else if(theme==1)
            {
                // Cold marble diamonds and fine veins beneath the widow's silk.
                c.Polygon(new[] { 8,1, 15,8, 8,15, 1,8 },Hex(0x48566a));
                c.Line(8,1,15,8,Hex(0x657188)); c.Line(1,8,8,15,Hex(0x272d43));
                c.Line(0,2,5,5,Hex(0x737f8e)); c.Line(5,5,4,9,Hex(0x586379));
                c.Set(6,4,Hex(0x77839a)); c.Line(11,11,14,15,Hex(0x33394f));
            }
            else if(theme==2)
            {
                // Charred cathedral herringbone masonry, glowing cracks kept sparse.
                c.Rect(1,1,6,10,Hex(0x594443)); c.Rect(9,5,6,10,Hex(0x503e42));
                c.Line(1,1,6,1,Hex(0x79605b)); c.Line(9,5,14,5,Hex(0x76534b));
                c.Line(0,13,5,13,Hex(0x2b2532)); c.Line(5,13,7,15,Hex(0x2b2532));
                c.Set(6,13,Hex(0xae6243)); c.Set(7,14,Hex(0xdf8650)); c.Set(13,2,Hex(0x8a5f4e));
            }
            else if(theme==3)
            {
                // Knotted chitin plates and damp pustules, with a readable dark center.
                c.Ellipse(1,1,13,12,Hex(0x415540)); c.Ellipse(3,2,9,8,Hex(0x4d6046));
                c.Line(0,14,6,10,Hex(0x243b34)); c.Line(6,10,12,13,Hex(0x243b34)); c.Line(12,13,15,9,Hex(0x243b34));
                c.Ellipse(11,3,4,4,Hex(0x728653)); c.Set(12,3,Hex(0xa9b977));
                c.Ellipse(3,13,3,2,Hex(0x6b7d4d)); c.Set(2,4,Hex(0x829269));
            }
            else
            {
                // Black obsidian with broken concentric brass inlays.
                c.Polygon(new[] { 2,2, 12,1, 15,8, 11,14, 3,15, 0,8 },Hex(0x39304f));
                c.Line(3,3,12,3,Hex(0x66516f)); c.Line(12,3,14,8,Hex(0x66516f));
                c.Line(1,10,3,14,Hex(0x181b31)); c.Line(3,14,11,14,Hex(0x181b31));
                c.Line(5,5,10,5,Hex(0x887166)); c.Line(10,5,11,9,Hex(0x887166));
                c.Set(5,10,Hex(0x907c83)); c.Set(12,12,Hex(0xa77db3));
            }
            return c;
        }

        static Canvas ThemeFloor(int theme, int variant)
        {
            if (variant == 0) return ThemeFloor(theme);
            // Most alternates are quieter than the motif tile; mixing them avoids
            // repeating the same diamond, leaf or brass loop across the room.
            var c = new Canvas(16,16);
            Color32 ground = ThemeBase(theme), trim = ThemeTrim(theme);
            c.Rect(0,0,16,16,ground);
            Color32 grain = theme == 0 ? Hex(0x45493b) : theme == 1 ? Hex(0x48566a)
                : theme == 2 ? Hex(0x594443) : theme == 3 ? Hex(0x415540) : Hex(0x39304f);
            Color32 seam = theme == 0 ? Hex(0x26372f) : theme == 1 ? Hex(0x272d43)
                : theme == 2 ? Hex(0x2b2532) : theme == 3 ? Hex(0x243b34) : Hex(0x181b31);
            for(int y=0;y<16;y++) for(int x=0;x<16;x++)
            {
                int sample=(x*31+y*47+variant*71+x*y*5+theme*19)%67;
                if(sample<3) c.Set(x,y,grain);
            }
            if(theme==0)
            {
                if(variant==1) { c.Rect(0,2,16,4,grain); c.Line(0,2,15,2,Hex(0x565746)); c.Line(0,11,15,11,seam); }
                else if(variant==2) { c.Rect(0,6,16,5,grain); c.Line(0,6,15,6,Hex(0x5b5d46)); c.Line(2,6,5,10,seam); c.Line(5,10,3,14,seam); c.Set(13,8,LightStone); }
                else { c.Line(0,4,15,4,seam); c.Line(0,13,15,13,seam); c.Line(10,4,10,13,seam); c.Rect(3,10,3,2,Hex(0x4c6041)); c.Set(4,9,Hex(0x6b7b4d)); }
            }
            else if(theme==1)
            {
                if(variant==1) { c.Rect(0,0,8,16,grain); c.Line(8,0,8,15,seam); c.Line(0,12,7,12,Hex(0x5a667a)); }
                else if(variant==2) { c.Line(2,0,5,5,Hex(0x657188)); c.Line(5,5,4,10,grain); c.Line(4,10,7,15,seam); c.Line(11,0,10,4,seam); }
                else { c.Polygon(new[] { 0,8, 8,0, 16,8, 8,16 },grain); c.Line(0,8,8,0,Hex(0x56647a)); c.Line(8,16,16,8,seam); }
            }
            else if(theme==2)
            {
                if(variant==1) { c.Rect(0,1,16,6,grain); c.Line(0,8,15,8,seam); c.Line(11,0,11,7,seam); c.Line(4,9,4,15,seam); }
                else if(variant==2) { c.Rect(1,0,6,16,grain); c.Line(7,0,7,15,seam); c.Line(8,4,15,4,seam); c.Set(12,10,Hex(0x75544d)); }
                else { c.Line(1,0,4,4,seam); c.Line(4,4,3,8,seam); c.Line(3,8,8,12,seam); c.Line(8,12,7,15,seam); c.Set(4,7,Hex(0xa36246)); c.Rect(10,10,3,2,Hex(0x57474c)); }
            }
            else if(theme==3)
            {
                if(variant==1) { c.Ellipse(-3,2,12,15,grain); c.Line(8,0,6,7,seam); c.Line(6,7,11,12,seam); }
                else if(variant==2) { c.Ellipse(7,-4,13,15,grain); c.Line(0,9,5,5,seam); c.Line(5,5,11,9,seam); c.Ellipse(3,12,4,3,Hex(0x5a6f49)); }
                else { c.Ellipse(0,5,16,7,grain); c.Line(0,12,8,13,seam); c.Line(8,13,15,10,seam); c.Set(11,7,Hex(0x829269)); }
            }
            else
            {
                if(variant==1) { c.Polygon(new[] { 0,0, 10,0, 16,7, 16,16, 6,16, 0,9 },grain); c.Line(0,9,6,16,seam); c.Line(10,0,16,7,Hex(0x4c3b5b)); }
                else if(variant==2) { c.Line(0,6,5,9,seam); c.Line(5,9,8,8,seam); c.Line(8,8,11,14,seam); c.Line(11,14,15,15,seam); c.Set(7,8,Hex(0x7c697e)); }
                else { c.Rect(1,0,5,16,grain); c.Line(6,0,6,15,seam); c.Line(0,11,5,11,Hex(0x66516f)); c.Set(12,3,Hex(0x66516f)); }
            }
            return c;
        }

        static Canvas WallRelief(int theme)
        {
            // Broad top cap, illuminated left face, recessed center and shaded right
            // return make these read as wall projections rather than floor decals.
            var c=new Canvas(32,48); Color32 trim=ThemeTrim(theme), stone=ThemeBase(theme);
            c.Ellipse(1,41,30,6,Alpha(Ink,100));
            c.Polygon(new[] { 3,7, 23,3, 29,7, 29,41, 24,46, 3,43 },Ink);
            c.Polygon(new[] { 3,7, 23,3, 29,7, 9,11 },trim);
            c.Polygon(new[] { 3,8, 9,11, 9,42, 3,43 },LightStone);
            c.Polygon(new[] { 9,11, 24,8, 24,44, 9,42 },stone);
            c.Polygon(new[] { 24,8, 29,7, 29,41, 24,46 },Shade);
            c.Line(3,7,23,3,Bone); c.Line(9,12,9,40,trim);
            c.Rect(4,40,20,4,trim); c.Line(4,40,23,40,Bone);
            if(theme==0)
            {
                c.Line(17,7,14,20,Hex(0x776448)); c.Line(14,20,19,31,Hex(0x776448)); c.Line(19,31,15,42,Hex(0x776448));
                c.Line(14,19,9,16,Hex(0x5d7945)); c.Line(19,29,24,23,Hex(0x5d7945));
                c.Line(14,18,12,24,Hex(0x91a360)); c.Ellipse(11,28,11,10,Rust); c.Ellipse(12,28,9,8,Orange);
                c.Rect(15,26,2,3,Teal); c.Set(14,31,Gold); c.Set(19,31,Gold); c.Line(15,35,18,35,Ink);
            }
            else if(theme==1)
            {
                c.Ellipse(12,13,10,22,Ink); c.Ellipse(13,15,8,18,Hex(0x4a5874));
                c.Line(12,15,19,12,Bone); c.Line(19,12,22,17,Bone); c.Line(11,33,21,33,trim);
                c.Line(26,8,12,30,Hex(0x8c99ae)); c.Line(26,8,14,13,Hex(0x8c99ae)); c.Line(26,8,24,33,Hex(0x8c99ae));
                c.Line(16,17,24,17,Hex(0xa7b4c8)); c.Line(14,22,23,22,Hex(0xa7b4c8)); c.Ellipse(16,27,4,4,Ink);
            }
            else if(theme==2)
            {
                c.Rect(12,13,10,23,Hex(0x342530)); c.Rect(16,12,3,23,Gold); c.Rect(12,18,10,3,Gold);
                c.Rect(17,13,1,19,Bone); c.Rect(12,19,8,1,Bone);
                c.Ellipse(12,29,10,8,Bone); c.Rect(13,31,3,3,Ink); c.Rect(18,31,3,3,Ink); c.Rect(16,35,2,2,Ink);
                c.Line(5,23,7,29,Hex(0x9b7267)); c.Set(26,34,Orange);
            }
            else if(theme==3)
            {
                c.Ellipse(11,13,12,24,Hex(0x597548)); c.Ellipse(13,15,8,18,Hex(0x93a46a));
                c.Line(17,17,14,23,Hex(0x486143)); c.Line(14,23,18,30,Hex(0x486143)); c.Set(15,18,Bone);
                c.Ellipse(20,31,6,8,Teal); c.Set(21,32,Mint); c.Line(12,35,8,43,Teal); c.Line(23,21,27,29,Teal);
            }
            else
            {
                c.Polygon(new[] { 17,13, 23,22, 17,34, 11,25 },Ink);
                c.Polygon(new[] { 17,16, 20,22, 17,29, 14,24 },Violet); c.Line(16,18,16,25,Pale);
                c.Rect(12,35,10,2,trim); c.Rect(14,37,6,2,Hex(0x73567e));
                c.Line(5,15,7,28,Hex(0x94849c)); c.Set(26,18,Pink);
            }
            c.Outline(Ink); return c;
        }

        static Canvas Chandelier(int frame)
        {
            var c=new Canvas(56,64); int flicker=frame==1?-1:frame==3?1:0;
            // A chained, freestanding ossuary candelabrum: five flames sit at
            // staggered heights, above curved iron arms and a skull reliquary.
            c.Ellipse(8,56,42,7,Alpha(Ink,110));
            c.Line(26,1,26,18,Hex(0x62546b)); c.Line(29,1,29,18,Hex(0x62546b));
            for(int y=1;y<18;y+=4) { c.Rect(25,y,6,3,LightStone); c.Rect(27,y+1,2,1,Ink); }
            c.Polygon(new[] { 25,18, 31,18, 33,53, 43,57, 42,61, 14,61, 13,57, 23,53 },Hex(0x3d3046));
            c.Rect(26,20,3,34,Hex(0x91787d)); c.Line(27,21,27,51,Bone);
            c.Polygon(new[] { 16,55, 25,52, 31,52, 40,56, 36,59, 18,59 },Hex(0x8a7169));
            c.Line(17,55,26,53,Gold); c.Line(18,60,37,60,Hex(0x4b3b50));
            // Rear arms are dark; front arms carry a bright bevel and brass cups.
            c.Line(26,35,17,31,Hex(0x635166)); c.Line(17,31,14,30,Hex(0x635166));
            c.Line(30,35,39,31,Hex(0x635166)); c.Line(39,31,42,30,Hex(0x635166));
            c.Line(25,43,10,39,Hex(0x876c78)); c.Line(10,39,6,38,Hex(0x876c78));
            c.Line(31,43,46,39,Hex(0x876c78)); c.Line(46,39,50,38,Hex(0x876c78));
            c.Line(11,38,24,41,Gold); c.Line(32,41,45,38,Gold);
            for(int i=0;i<5;i++)
            {
                int x=i==0?6:i==1?14:i==2?28:i==3?42:50;
                int top=i==2?24:i%2==1?30:38;
                c.Ellipse(x-4,top,9,4,Hex(0xa47b60)); c.Line(x-3,top,x+3,top,Gold);
                c.Rect(x-2,top-10,5,11,Hex(0xb4a18a)); c.Rect(x-2,top-10,2,10,Bone);
                c.Rect(x-1,top-8,1,7,Pale); c.Rect(x+2,top-7,2,3,Hex(0xa18873));
                c.Ellipse(x-3+flicker,top-18,7,10,Orange);
                c.Polygon(new[] { x+flicker,top-21, x+3+flicker,top-14, x,top-10, x-2,top-14 },Gold);
                c.Rect(x,top-15,2,4,Pale); c.Set(x+4-flicker,top-17,Orange);
            }
            c.Ellipse(19,33,18,16,Hex(0x776370)); c.Ellipse(21,33,14,13,Bone);
            c.Rect(23,37,4,4,Ink); c.Rect(29,37,4,4,Ink); c.Set(24,38,Orange); c.Set(30,38,Orange);
            c.Polygon(new[] { 27,41, 30,43, 26,43 },Ink); c.Rect(24,45,8,4,Hex(0xc4ae98));
            c.Line(26,45,26,48,Shade); c.Line(29,45,29,48,Shade); c.Line(32,46,32,48,Shade);
            c.Outline(Ink); return c;
        }

        static Canvas ExitArrow()
        {
            var c=new Canvas(24,24);
            c.Polygon(new[] { 2,7, 12,7, 12,2, 23,12, 12,22, 12,17, 2,17 },Hex(0x173b45));
            c.Polygon(new[] { 3,9, 14,9, 14,5, 21,12, 14,19, 14,15, 3,15 },Gold);
            c.Polygon(new[] { 4,10, 16,10, 15,8, 20,12, 16,16, 16,14, 4,14 },Mint);
            c.Line(4,9,13,9,Pale); c.Line(14,5,20,11,Pale); c.Line(4,15,13,15,Teal);
            c.Outline(Ink); return c;
        }

        static Canvas ThemeWall(int theme)
        {
            var c=new Canvas(16,16); Color32 trim=ThemeTrim(theme);
            c.Rect(0,0,16,16,Ink); c.Rect(0,0,16,3,trim); c.Line(0,0,15,0,Bone);
            if(theme==0)
            {
                c.Rect(0,3,16,11,Hex(0x565348)); c.Line(0,7,15,7,Hex(0x302f30));
                c.Rect(1,4,6,2,Hex(0x77705c)); c.Rect(9,9,6,2,Hex(0x77705c));
                c.Line(3,1,5,8,Hex(0x36563b)); c.Line(5,8,10,15,Hex(0x36563b));
                c.Line(4,3,2,6,Hex(0x638457)); c.Line(6,9,10,7,Hex(0x638457));
                c.Rect(11,2,4,3,Hex(0x69774e)); c.Set(12,2,Hex(0x9ba36e));
            }
            else if(theme==1)
            {
                c.Rect(0,3,16,11,Hex(0x596379)); c.Rect(1,4,14,2,Hex(0x7f8aa1));
                c.Rect(2,7,3,7,Hex(0x8d98ad)); c.Rect(11,7,3,7,Hex(0x8d98ad));
                c.Ellipse(5,5,6,10,Hex(0x242b44)); c.Ellipse(6,7,4,7,Hex(0x394b69));
                c.Line(15,3,8,13,Hex(0xa8b8ca)); c.Line(15,6,11,6,Hex(0xa8b8ca));
                c.Line(14,8,10,8,Hex(0xa8b8ca)); c.Rect(0,14,16,2,Hex(0x3a435a));
            }
            else if(theme==2)
            {
                c.Rect(0,3,16,11,Hex(0x5e444d)); c.Rect(0,4,16,2,Hex(0x906356));
                c.Rect(1,7,5,7,Hex(0x77515c)); c.Rect(10,7,5,7,Hex(0x77515c));
                c.Rect(6,6,4,10,Hex(0x231f32)); c.Rect(7,7,2,6,Orange); c.Set(7,8,Gold);
                c.Rect(0,14,16,2,Hex(0x382b3a)); c.Set(2,12,Hex(0xa16a59));
            }
            else if(theme==3)
            {
                c.Rect(0,3,16,12,Hex(0x4b5746));
                c.Ellipse(0,4,8,10,Hex(0x6d7750)); c.Ellipse(8,3,9,11,Hex(0x5c6e4d));
                c.Line(6,5,8,13,Hex(0x253c36)); c.Line(13,4,10,10,Hex(0x253c36));
                c.Ellipse(2,7,4,5,Hex(0x9ba763)); c.Set(3,7,Bone); c.Set(13,5,Mint);
                c.Rect(0,14,16,2,Hex(0x2e4437)); c.Set(5,14,Hex(0x9dae6e));
            }
            else
            {
                c.Rect(0,3,16,12,Hex(0x493855)); c.Rect(1,4,14,3,Hex(0x725475));
                c.Rect(2,7,3,7,Hex(0x9b7d91)); c.Rect(11,7,3,7,Hex(0x9b7d91));
                c.Polygon(new[] { 8,6, 11,9, 8,13, 5,9 },Ink); c.Rect(7,8,2,3,Violet); c.Set(7,8,Pale);
                c.Line(0,14,15,14,Hex(0x987663)); c.Rect(0,15,16,1,Hex(0x25213a));
            }
            return c;
        }

        static Canvas ThemeDecor(int theme)
        {
            var c=new Canvas(48,48);
            if(theme==0)
            {
                c.Ellipse(5,38,38,7,Hex(0x2c3a30));
                c.Polygon(new[] { 16,42, 22,26, 19,14, 23,4, 26,16, 29,24, 29,35, 38,43, 29,40, 24,44, 20,40 },Hex(0x67534c));
                c.Line(24,8,25,32,Hex(0x9b7e5c)); c.Line(21,23,9,14,Hex(0x67534c)); c.Line(9,14,7,5,Hex(0x67534c));
                c.Line(28,22,38,13,Hex(0x67534c)); c.Line(38,13,42,6,Hex(0x67534c)); c.Line(37,14,32,6,Hex(0x67534c));
                c.Line(11,15,14,6,Hex(0x67534c)); c.Line(19,37,8,41,Hex(0x67534c));
                c.Ellipse(2,29,11,10,Rust); c.Ellipse(3,29,9,8,Orange); c.Rect(5,27,2,3,Teal); c.Set(5,32,Gold); c.Set(9,32,Gold);
                c.Ellipse(33,35,10,8,Hex(0x765e3d)); c.Rect(35,35,5,2,Hex(0x9f8653));
                c.Line(29,29,35,27,Teal); c.Line(21,18,18,11,Teal); c.Set(32,8,Gold); c.Set(8,20,Orange);
            }
            else if(theme==1)
            {
                c.Polygon(new[] { 5,44, 6,16, 13,5, 34,5, 42,16, 43,44 },Hex(0x54617c));
                c.Polygon(new[] { 9,41, 10,18, 16,9, 31,9, 38,18, 39,41 },Hex(0xb3bccc));
                c.Polygon(new[] { 14,41, 14,20, 18,14, 30,14, 34,20, 34,41 },Hex(0x252b45));
                c.Polygon(new[] { 10,15, 17,12, 16,40, 10,43 },Hex(0x8c94b7));
                c.Polygon(new[] { 31,12, 38,15, 38,43, 32,40 },Hex(0x8c94b7));
                c.Line(11,19,12,38,Bone); c.Line(35,18,37,39,Bone);
                for(int i=0;i<5;i++) c.Line(11+i*5,8,24,29,Hex(0x9aa8bf));
                c.Line(11,10,37,10,Bone); c.Line(15,16,33,16,Bone); c.Line(19,22,29,22,Bone);
                c.Ellipse(22,27,5,6,Ink); c.Line(21,29,18,27,Ink); c.Line(27,29,30,27,Ink);
                c.Rect(4,42,40,4,Hex(0x7d859e)); c.Line(5,42,42,42,LightStone);
            }
            else if(theme==2)
            {
                c.Rect(9,28,30,15,Hex(0x5c3c4d)); c.Rect(6,27,36,5,Hex(0x916257));
                c.Rect(11,32,26,10,Hex(0x7d4c55)); c.Rect(12,42,24,3,Hex(0x3c2a39));
                c.Rect(21,7,6,21,Gold); c.Rect(15,12,18,5,Gold); c.Rect(23,8,2,19,Bone);
                c.Rect(17,13,14,2,Bone); c.Rect(21,28,7,13,Hex(0xb18a6a)); c.Set(24,33,Pale);
                c.Rect(7,21,4,7,Bone); c.Ellipse(6,15,6,8,Orange); c.Rect(8,17,2,4,Gold);
                c.Rect(37,21,4,7,Bone); c.Ellipse(36,15,6,8,Orange); c.Rect(38,17,2,4,Gold);
                c.Line(14,36,18,39,Ink); c.Line(31,34,28,39,Ink); c.Set(30,40,Orange);
            }
            else if(theme==3)
            {
                c.Ellipse(3,33,43,12,Hex(0x354f3a));
                for(int i=0;i<4;i++)
                {
                    int x=7+i*8, y=16+i%2*8;
                    c.Ellipse(x,y,13,22,Hex(0x607950)); c.Ellipse(x+2,y+2,9,16,Hex(0x9aa76d));
                    c.Line(x+5,y+5,x+3,y+12,Hex(0x576747)); c.Line(x+3,y+12,x+7,y+16,Hex(0x576747));
                    c.Set(x+4,y+4,Bone); c.Set(x+8,y+13,Mint);
                }
                c.Line(5,34,3,20,Teal); c.Line(41,35,46,22,Teal); c.Line(19,39,11,44,Teal);
                c.Ellipse(13,38,7,4,Hex(0x829958)); c.Ellipse(32,38,9,4,Hex(0x829958));
            }
            else
            {
                c.Rect(7,7,5,35,Hex(0x6f526f)); c.Rect(36,7,5,35,Hex(0x6f526f));
                c.Rect(5,4,38,5,Hex(0x94728a)); c.Line(6,4,41,4,Bone);
                c.Line(24,8,24,16,LightStone); c.Rect(22,15,5,4,Gold);
                c.Polygon(new[] { 14,35, 18,28, 18,21, 22,18, 28,18, 32,21, 32,28, 36,35 },Hex(0x956b75));
                c.Polygon(new[] { 18,33, 22,26, 22,21, 27,21, 29,29, 32,33 },Gold);
                c.Rect(14,34,22,4,Hex(0x5f425b)); c.Line(15,34,34,34,Gold);
                c.Ellipse(22,38,7,5,Gold); c.Rect(6,42,36,4,Hex(0x493751));
                c.Rect(8,12,3,4,Gold); c.Rect(37,12,3,4,Gold); c.Set(9,13,Pink); c.Set(38,13,Pink);
            }
            c.Outline(Ink); return c;
        }

        static Canvas ThemeMap(int theme)
        {
            // A tiny illustrated chamber: irregular architecture, thematic floor and motifs.
            var c=new Canvas(48,32); Color32 ground=ThemeBase(theme),trim=ThemeTrim(theme);
            c.Polygon(new[] { 6,1, 40,1, 46,7, 46,26, 40,31, 6,31, 1,26, 1,7 },Ink);
            c.Polygon(new[] { 7,3, 39,3, 44,8, 44,25, 39,29, 7,29, 3,25, 3,8 },trim);
            c.Polygon(new[] { 8,6, 38,6, 41,9, 41,24, 38,26, 8,26, 6,24, 6,9 },ground);
            c.Line(8,6,38,6,Bone); c.Line(7,26,39,26,Ink);
            // Passages cut through the wall rather than presenting a graph of square nodes.
            c.Rect(21,0,6,7,ground); c.Rect(21,26,6,6,ground); c.Rect(0,13,7,6,ground); c.Rect(41,13,7,6,ground);
            c.Line(21,1,26,1,trim); c.Line(21,30,26,30,trim);
            if(theme==0)
            {
                for(int y=10;y<25;y+=5) c.Line(8,y,39,y,Hex(0x5a5a44));
                c.Line(10,7,13,16,Teal); c.Line(13,16,9,25,Teal);
                c.Ellipse(30,19,7,5,Orange); c.Rect(32,17,2,3,Teal); c.Set(32,21,Gold);
                c.Line(35,7,38,10,Hex(0xa17b4e));
            }
            else if(theme==1)
            {
                c.Polygon(new[] { 24,8, 34,16, 24,24, 14,16 },Hex(0x73809c));
                c.Polygon(new[] { 24,10, 31,16, 24,22, 17,16 },Hex(0x414b67));
                c.Line(8,7,17,19,Bone); c.Line(8,7,24,8,Bone); c.Line(8,7,9,24,Bone);
                c.Line(10,12,16,12,LightStone); c.Line(11,16,14,16,LightStone);
            }
            else if(theme==2)
            {
                c.Rect(13,8,3,17,Hex(0x976954)); c.Rect(32,8,3,17,Hex(0x976954));
                c.Rect(21,10,7,14,Hex(0x78505b)); c.Rect(23,11,3,11,Gold); c.Rect(20,14,9,2,Gold);
                c.Set(10,22,Orange); c.Set(36,10,Orange); c.Set(30,23,Orange);
            }
            else if(theme==3)
            {
                c.Ellipse(9,9,12,13,Hex(0x647d4d)); c.Ellipse(27,12,12,11,Hex(0x647d4d));
                c.Ellipse(12,11,6,8,Hex(0x9dac6e)); c.Ellipse(30,14,6,6,Hex(0x9dac6e));
                c.Line(20,9,22,18,Teal); c.Line(22,18,18,24,Teal); c.Set(14,12,Bone); c.Set(31,15,Mint);
            }
            else
            {
                c.Ellipse(13,8,23,17,Hex(0x755b7e)); c.Ellipse(16,10,17,13,ground);
                c.Polygon(new[] { 19,21, 21,16, 21,12, 27,12, 28,17, 30,21 },Gold);
                c.Rect(19,21,11,2,Hex(0x9b7182)); c.Set(25,24,Pink);
                c.Line(8,8,11,11,Violet); c.Line(37,23,40,25,Violet);
            }
            return c;
        }

        static Canvas Floor(int variant)
        {
            var c = new Canvas(16,16);
            Color32 baseColor = variant == 4 ? Hex(0x3c4244) : variant == 5 ? Hex(0x44404a) : Hex(0x3e3d49);
            c.Rect(0,0,16,16,baseColor);
            // Fine deterministic grain: no bevel on every square, so the floor reads
            // as broad irregular flagstones rather than a screen-wide checkerboard.
            for (int y=0;y<16;y++) for (int x=0;x<16;x++)
            {
                int noise=(x*37+y*61+variant*83+(x*y)*7)%31;
                if (noise==0) c.Set(x,y,Hex(0x4d4b56));
                if (noise==1) c.Set(x,y,Hex(0x363642));
            }
            if (variant==0) { c.Line(0,7,7,7,Hex(0x2e303b)); c.Line(7,7,10,10,Hex(0x2e303b)); c.Line(10,10,15,10,Hex(0x2e303b)); c.Line(1,6,6,6,Hex(0x55515d)); }
            else if (variant==1) { c.Line(9,0,9,5,Hex(0x2c2e38)); c.Line(9,5,6,8,Hex(0x2c2e38)); c.Line(6,8,7,11,Hex(0x2c2e38)); c.Line(10,1,10,4,Hex(0x57505b)); }
            else if (variant==2) { c.Line(0,13,15,13,Hex(0x30313c)); c.Line(1,12,14,12,Hex(0x514d59)); c.Line(4,14,4,15,Hex(0x30313c)); }
            else if (variant==3) { c.Line(0,1,4,1,Hex(0x343540)); c.Line(4,1,6,3,Hex(0x343540)); c.Line(6,3,6,6,Hex(0x343540)); }
            else if (variant==4) { c.Rect(3,9,4,2,Hex(0x475452)); c.Set(4,8,Hex(0x5a6b5c)); c.Rect(9,3,3,1,Hex(0x4c5b54)); c.Set(10,2,Hex(0x64705c)); }
            else { c.Line(2,12,5,10,Hex(0x55505d)); c.Line(5,10,8,11,Hex(0x55505d)); c.Set(12,5,Hex(0x65585e)); }
            return c;
        }

        static Canvas Wall()
        {
            var c=new Canvas(16,16);
            c.Rect(0,0,16,16,Ink); c.Rect(0,0,16,3,Hex(0xa598a5)); c.Line(0,0,15,0,Hex(0xc4b9b7));
            c.Rect(0,3,16,4,Hex(0x726879)); c.Line(0,7,15,7,Hex(0x3c364b));
            c.Rect(0,8,7,5,Hex(0x655b70)); c.Rect(8,8,8,5,Hex(0x6f6376));
            c.Line(1,8,6,8,Hex(0x918396)); c.Line(9,8,15,8,Hex(0x918396));
            c.Rect(0,14,16,2,Hex(0x3a3448)); c.Set(4,10,LightStone); c.Set(13,11,Hex(0x56495f));
            return c;
        }

        static Canvas Pillar()
        {
            var c=new Canvas(24,32);
            c.Rect(4,3,16,27,Hex(0x4c455b)); c.Rect(3,2,18,5,Hex(0x6b6077));
            c.Line(4,2,19,2,Hex(0xbbb0ba)); c.Rect(5,4,14,2,LightStone);
            c.Rect(7,8,10,19,Stone); c.Rect(7,8,3,19,LightStone); c.Rect(15,8,2,19,Hex(0x3a344b));
            c.Line(12,10,12,23,Hex(0x6d6677)); c.Line(16,14,13,17,Ink); c.Line(13,17,15,20,Ink);
            c.Rect(4,27,16,3,LightStone); c.Rect(3,30,18,1,Hex(0x65586f)); c.Rect(8,6,8,1,Gold);
            c.Rect(5,25,3,2,Teal); c.Set(6,24,Mint); c.Outline(Ink); return c;
        }

        static Canvas Grave()
        {
            var c=new Canvas(20,24);
            c.Ellipse(3,1,14,13,LightStone); c.Rect(3,7,14,15,Stone); c.Rect(4,7,3,14,LightStone);
            c.Rect(9,5,2,10,Ink); c.Rect(6,8,8,2,Ink); c.Rect(9,5,1,8,Hex(0xa199a3));
            c.Line(13,15,11,18,Ink); c.Line(11,18,13,20,Ink); c.Rect(1,21,18,2,Hex(0x4a4c52));
            c.Rect(2,21,4,1,Teal); c.Rect(14,20,3,2,Hex(0x5d7764)); c.Outline(Ink); return c;
        }

        static Canvas Candle(int frame)
        {
            var c=new Canvas(16,24); int flicker=frame==1?-1:frame==3?1:0;
            c.Ellipse(2,19,12,4,Hex(0x735065)); c.Rect(4,18,8,2,Gold);
            c.Rect(6,10,5,9,Bone); c.Rect(6,10,2,7,Pale); c.Line(10,12,10,17,Hex(0xaf927b));
            c.Rect(7,9,1,3,Ink); c.Ellipse(5+flicker,3,6,8,Orange);
            c.Polygon(new[]{8+flicker,1,10+flicker,7,8,10,6,7},Gold);
            c.Rect(7,6,2,3,Pale); c.Set(10+flicker,3,Orange); c.Outline(Ink); return c;
        }

        static Canvas Door(bool open, bool boss)
        {
            var c=new Canvas(32,32); Color32 trim=boss?Gold:LightStone;
            c.Ellipse(2,1,28,28,Hex(0x57455f)); c.Rect(2,14,28,17,Hex(0x57455f));
            c.Ellipse(5,3,22,25,trim); c.Rect(5,15,22,16,trim);
            c.Ellipse(8,7,16,22,Ink); c.Rect(8,17,16,15,Ink);
            c.Line(4,16,6,16,Ink); c.Line(4,22,6,22,Ink); c.Line(25,16,28,16,Ink); c.Line(25,22,28,22,Ink);
            c.Line(8,7,11,4,Hex(0xd1bdba)); c.Line(22,5,25,9,Hex(0x998699));
            c.Rect(0,29,32,3,Hex(0x494154)); c.Line(1,29,30,29,LightStone);
            if(open)
            {
                c.Rect(9,20,14,9,Hex(0x153c43)); c.Rect(10,28,12,2,Mint); c.Rect(12,26,8,2,Teal);
                c.Polygon(new[]{15,15,11,20,14,20,14,24,17,24,17,20,20,20},Mint);
                c.Rect(4,13,2,4,Mint); c.Rect(26,13,2,4,Mint); c.Set(4,12,Pale); c.Set(26,12,Pale);
            }
            else
            {
                for(int x=10;x<=22;x+=4) { c.Rect(x,12,2,17,Hex(0x655063)); c.Line(x,12,x,27,Hex(0x9f8094)); }
                c.Rect(8,17,16,3,Rust); c.Line(9,18,23,18,Hex(0xf08680));
                c.Rect(13,16,7,8,boss?Orange:Pink); c.Rect(14,18,5,5,Hex(0x8a314d));
                c.Rect(16,19,1,3,Gold); c.Rect(4,13,2,4,boss?Gold:Pink); c.Rect(26,13,2,4,boss?Gold:Pink);
            }
            if(boss)
            {
                c.Polygon(new[]{10,5,8,0,13,3,16,0,19,3,24,0,22,5},Gold);
                c.Rect(14,6,4,3,Rust); c.Set(14,6,Pale); c.Set(17,6,Pale);
            }
            c.Outline(Ink); return c;
        }

        static Canvas Portal(int frame)
        {
            var c=new Canvas(32,32);
            c.Ellipse(1,1,30,29,Hex(0x43394f)); c.Ellipse(3,2,26,27,LightStone);
            c.Ellipse(5,4,22,23,Teal); c.Ellipse(7,5,18,21,Mint); c.Ellipse(9,7,14,18,Hex(0x205f6b));
            c.Ellipse(10,8,12,16,Ink); c.Rect(0,28,32,3,Stone); c.Line(2,28,29,28,LightStone);
            c.Line(6+frame,10,10+frame,6,Pale); c.Line(22-frame,23,25-frame,18,Pale);
            c.Rect(13+frame,12,2,2,Mint); c.Set(19-frame,20,Gold); c.Set(5,19-frame,Pale);
            return c;
        }

        static Canvas Boss(int frame)
        {
            var c=new Canvas(48,48); int sway=frame==1?1:frame==3?-1:0;
            c.Polygon(new[]{12,30,34,30,42+sway,44,30,42,24,46,18,43,6+sway,44},Hex(0x6d416e));
            c.Line(13,33,8+sway,42,Violet); c.Line(32,33,38+sway,41,Rust);
            c.Line(16,39,11,46,Hex(0x718251)); c.Line(32,39,37,46,Hex(0x718251));
            c.Line(19,40,17+sway,47,Teal); c.Line(29,40,31+sway,47,Teal);
            c.Line(12,28,4,35+sway,Hex(0x718251)); c.Line(4,35+sway,2,29+sway,Hex(0x718251));
            c.Line(35,28,44,35-sway,Hex(0x718251)); c.Line(44,35-sway,46,29-sway,Hex(0x718251));
            c.Ellipse(8,9,32,25,Rust); c.Ellipse(9,9,29,23,Orange);
            c.Ellipse(12,10,8,21,Hex(0xffac48)); c.Line(25,10,23,16,Hex(0xbe5431));
            c.Line(32,11,34,17,Rust); c.Line(19,9,17,12,Gold);
            c.Polygon(new[]{12,17,21,19,19,23,12,22},Ink); c.Polygon(new[]{35,17,26,19,28,23,35,22},Ink);
            c.Rect(14,19,5,2,Gold); c.Rect(29,19,5,2,Gold); c.Set(14,19,Pale); c.Set(33,19,Pale);
            c.Polygon(new[]{22,23,25,23,26,26,21,26},Ink);
            c.Polygon(new[]{14,26,18,27,22,26,25,28,29,26,33,27,32,30,17,30},Ink);
            c.Line(18,28,29,28,Gold); c.Set(20,27,Orange); c.Set(27,29,Orange);
            c.Rect(20,3,6,7,Hex(0x477c54)); c.Line(24,3,28,1,Mint);
            c.Polygon(new[]{10,12,5,4,3,1,10,5,13,11},Hex(0x9f6570));
            c.Polygon(new[]{34,11,38,5,45,1,42,6,38,13},Hex(0x9f6570));
            c.Rect(20,32,7,3,Rust); c.Rect(22,35,3,6,Gold); c.Set(22,35,Pale);
            c.Outline(Ink); return c;
        }

        static Canvas Merchant(int frame)
        {
            var c=new Canvas(28,32); int hand=frame==1?-1:frame==3?1:0;
            c.Ellipse(5,1,18,19,Hex(0x566989)); c.Ellipse(7,2,14,17,Hex(0x8c9fc5));
            c.Ellipse(9,5,10,11,Ink); c.Ellipse(9,6,10,9,Bone); c.Rect(10,9,3,3,Ink); c.Rect(16,9,3,3,Ink);
            c.Set(11,10,Gold); c.Set(17,10,Gold); c.Line(12,14,16,14,Ink);
            c.Polygon(new[]{8,17,20,17,24,26,4,26},Hex(0x576d8b)); c.Rect(12,18,4,4,Gold);
            c.Rect(4,22+hand,4,3,Bone); c.Rect(20,22-hand,4,3,Bone);
            c.Rect(1,26,26,5,Hex(0x7f415a)); c.Line(1,26,26,26,Orange); c.Line(2,27,25,27,Gold);
            c.Rect(12,28,4,3,Gold); c.Rect(13,29,2,2,Ink);
            c.Rect(3,18,2,6,Bone); c.Rect(3,17,2,2,Gold); c.Set(3+hand,15,Orange); c.Outline(Ink); return c;
        }

        static Canvas Weapon()
        {
            var c=new Canvas(28,16);
            c.Polygon(new[] { 3,7, 10,7, 10,11, 7,11, 6,14, 2,12 },Hex(0x86575a));
            c.Line(3,10,6,12,Hex(0xc38c77)); c.Rect(7,6,13,5,Hex(0x67738a));
            c.Ellipse(8,5,8,7,Gold); c.Ellipse(10,6,4,5,Hex(0x9b715c)); c.Set(11,7,Pale);
            c.Rect(16,5,9,4,Hex(0xa2b7c3)); c.Line(16,5,25,5,Pale);
            c.Rect(24,4,3,6,Shade); c.Rect(25,5,1,4,Gold); c.Rect(8,11,5,2,Gold);
            c.Rect(8,3,2,3,Stone); c.Set(9,3,Pale); c.Rect(18,3,3,2,Teal); c.Set(19,3,Mint);
            c.Outline(Ink); return c;
        }
        static Canvas Coin()
        {
            var c=new Canvas(10,12); c.Ellipse(1,1,8,10,Hex(0xba6437)); c.Ellipse(1,1,6,9,Gold);
            c.Line(3,2,3,8,Pale); c.Line(5,3,5,8,Orange); c.Outline(Ink); return c;
        }
        static Canvas Bullet(bool enemy)
        {
            if (!enemy) return ShotDot();
            var c = new Canvas(14,14);
            c.Ellipse(1,1,12,12,Hex(0x602748)); c.Ellipse(2,2,10,10,Hex(0xe04d77));
            c.Ellipse(4,3,7,7,Hex(0xffa7bb)); c.Ellipse(5,4,5,5,Hex(0xfff0dc));
            c.Rect(5,3,3,2,Pale); c.Set(10,8,Hex(0xffced5));
            c.Line(4,11,9,11,Hex(0xaa355a)); c.Outline(Ink); return c;
        }
        static Canvas Muzzle()
        {
            var c=new Canvas(12,12); c.Polygon(new[]{6,0,8,4,11,3,9,6,12,9,8,8,6,12,4,8,0,9,3,6,1,3,4,4},Gold);
            c.Ellipse(4,4,5,5,Pale); return c;
        }
        static Canvas Heart()
        {
            var c=new Canvas(12,12); c.Ellipse(1,1,6,6,Pink); c.Ellipse(6,1,5,6,Pink);
            c.Polygon(new[]{1,5,10,5,6,10},Pink); c.Rect(3,2,2,2,Pale); c.Outline(Ink); return c;
        }
        static Canvas Shadow()
        {
            var c = new Canvas(32,16);
            c.Ellipse(1,2,30,12,Alpha(Ink,24)); c.Ellipse(4,4,24,8,Alpha(Ink,58));
            c.Ellipse(8,5,17,6,Alpha(Ink,92)); c.Ellipse(11,6,11,4,Alpha(Ink,125));
            return c;
        }
        static Canvas Ring(int size,bool enemy)
        {
            var c=new Canvas(size,size/2); Color32 color=enemy?Pink:Mint;
            c.Ellipse(1,1,size-2,size/2-2,Alpha(color,190)); c.Ellipse(3,3,size-6,size/2-6,new Color32(0,0,0,0));
            c.Rect(size/2-1,0,3,2,color); c.Rect(0,size/4-1,2,3,color); c.Rect(size-2,size/4-1,2,3,color);
            return c;
        }
        static Canvas Glow()
        {
            var c=new Canvas(32,32);
            for(int y=0;y<32;y++) for(int x=0;x<32;x++)
            {
                double dx=(x-15.5)/16,dy=(y-15.5)/16,d=Math.Sqrt(dx*dx+dy*dy);
                if(d<1) c.Set(x,y,new Color32(255,255,255,(byte)(Math.Pow(1-d,2)*135)));
            }
            return c;
        }
        static Canvas Rune()
        {
            var c=new Canvas(64,64); Color32 rune=Alpha(Hex(0xb5a6bb),120);
            c.Ellipse(2,2,60,60,rune); c.Ellipse(4,4,56,56,new Color32(0,0,0,0));
            c.Ellipse(10,10,44,44,rune); c.Ellipse(11,11,42,42,new Color32(0,0,0,0));
            c.Line(32,5,48,47,rune); c.Line(48,47,9,21,rune); c.Line(9,21,55,21,rune); c.Line(55,21,16,47,rune); c.Line(16,47,32,5,rune);
            for(int i=0;i<8;i++) { double a=i*Math.PI/4; int x=32+(int)(Math.Cos(a)*27),y=32+(int)(Math.Sin(a)*27); c.Rect(x-1,y-1,3,3,rune); }
            return c;
        }
        static Canvas Rug()
        {
            var c=new Canvas(64,32); c.Rect(0,0,64,32,Ink); c.Rect(1,1,62,30,Hex(0x84475f));
            c.Rect(3,3,58,26,Hex(0xb88a67)); c.Rect(5,5,54,22,Hex(0x653b59));
            for(int x=7;x<59;x+=6) { c.Rect(x,3,2,2,Gold); c.Rect(x,27,2,2,Gold); }
            c.Polygon(new[]{32,7,45,16,32,25,19,16},Hex(0xa75f75)); c.Polygon(new[]{32,11,39,16,32,21,25,16},Gold);
            for(int y=1;y<32;y+=3) { c.Set(0,y,Bone); c.Set(63,y,Bone); } return c;
        }
        static Canvas Slash()
        {
            var c=new Canvas(16,16); c.Polygon(new[]{1,14,6,4,14,1,10,7,7,12},Mint); c.Line(3,12,12,3,Pale); return c;
        }
        static Canvas Dust()
        {
            var c=new Canvas(8,8); c.Rect(1,4,3,2,Alpha(Bone,150)); c.Rect(4,2,2,2,Alpha(Bone,100)); c.Set(6,6,Alpha(Bone,180)); return c;
        }
        static Canvas Spark()
        {
            var c=new Canvas(8,8); c.Line(3,0,3,7,Gold); c.Line(0,3,7,3,Gold); c.Rect(2,2,3,3,Pale); return c;
        }

        public void Dispose()
        {
            if(disposed) return; disposed=true;
            foreach(var sprite in sprites.Values) DestroyOwned(sprite);
            foreach(var texture in textures) DestroyOwned(texture);
            if(material) DestroyOwned(material);
            sprites.Clear(); textures.Clear(); referenceTextures.Clear(); ClearCharacterCatalog(); material=null;
        }
        static void DestroyOwned(UnityEngine.Object value)
        {
            if(Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value);
        }
        sealed class Canvas
        {
            public readonly int Width,Height;
            public readonly Color32[] Pixels;
            public Canvas(int width,int height) { Width=width; Height=height; Pixels=new Color32[width*height]; }
            public void Set(int x,int y,Color32 color)
            {
                if(x>=0&&x<Width&&y>=0&&y<Height) Pixels[(Height-1-y)*Width+x]=color;
            }
            public void Rect(int x,int y,int width,int height,Color32 color)
            {
                for(int py=y;py<y+height;py++) for(int px=x;px<x+width;px++) Set(px,py,color);
            }
            public void Ellipse(int x,int y,int width,int height,Color32 color)
            {
                float rx=width*.5f,ry=height*.5f;
                for(int py=y;py<y+height;py++) for(int px=x;px<x+width;px++)
                {
                    float dx=(px+.5f-x-rx)/rx,dy=(py+.5f-y-ry)/ry;
                    if(dx*dx+dy*dy<=1) Set(px,py,color);
                }
            }
            public void Polygon(int[] points,Color32 color)
            {
                for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
                {
                    bool inside=false; int count=points.Length/2;
                    for(int i=0,j=count-1;i<count;j=i++)
                    {
                        int ax=points[i*2],ay=points[i*2+1],bx=points[j*2],by=points[j*2+1];
                        if((ay>y)!=(by>y) && x<(float)(bx-ax)*(y-ay)/(by-ay)+ax) inside=!inside;
                    }
                    if(inside) Set(x,y,color);
                }
            }
            public void Outline(Color32 color)
            {
                var original=(Color32[])Pixels.Clone();
                for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
                {
                    int at=(Height-1-y)*Width+x;
                    if(original[at].a>0) continue;
                    for(int oy=-1;oy<=1;oy++) for(int ox=-1;ox<=1;ox++)
                    {
                        int nx=x+ox,ny=y+oy;
                        if(nx<0||nx>=Width||ny<0||ny>=Height) continue;
                        Color32 neighbor=original[(Height-1-ny)*Width+nx];
                        // Existing outline pixels never become dilation seeds.
                        // Layered poses can request a contour without growing it.
                        bool alreadyOutline=neighbor.r==color.r&&neighbor.g==color.g&&neighbor.b==color.b&&neighbor.a==color.a;
                        if(neighbor.a>0&&!alreadyOutline) Set(x,y,color);
                    }
                }
            }
            public void Line(int x0,int y0,int x1,int y1,Color32 color)
            {
                int dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,error=dx+dy;
                while(true)
                {
                    Set(x0,y0,color); if(x0==x1&&y0==y1) break;
                    int twice=2*error; if(twice>=dy) { error+=dy; x0+=sx; } if(twice<=dx) { error+=dx; y0+=sy; }
                }
            }
        }
    }
}
