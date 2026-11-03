using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    /// <summary>Original pixel art and four-pose animation. Runtime textures belong to this instance.</summary>
    public sealed class HauntPixelArt : IDisposable
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
            bool animated = id == "skeleton" || id == "slime" || id == "spider" || id == "miniboss"
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
                case "boss-fill0": pixels = BossFill(0); break;
                case "boss-fill1": pixels = BossFill(1); break;
                case "boss-fill2": pixels = BossFill(2); break;
                case "boss-fill3": pixels = BossFill(3); break;
                case "boss-fill4": pixels = BossFill(4); break;
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
            var texture = new Texture2D(pixels.Width, pixels.Height, TextureFormat.RGBA32, false)
            {
                name = "Haunt / " + key, filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
            };
            texture.SetPixels32(pixels.Pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, pixels.Width, pixels.Height),
                new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
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

        // The five frames share a transparent HP window [24,216) x [8,16).
        // UI can fill that region without changing geometry for each guardian.
        static Canvas BossBar(int theme)
        {
            var c = new Canvas(240,24);
            Color32 trim = theme == 0 ? Hex(0x9c743d) : theme == 1 ? Hex(0x9eabc8)
                : theme == 2 ? Gold : theme == 3 ? Hex(0x7da26a) : Hex(0xae8ac1);
            Color32 dark = theme == 0 ? Hex(0x3a3729) : theme == 1 ? Hex(0x38344d)
                : theme == 2 ? Hex(0x583342) : theme == 3 ? Hex(0x2e4945) : Hex(0x352d4b);
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
            else
                for(int x=28;x<212;x+=18) { c.Polygon(new[]{x,3,x+4,0,x+8,3},trim); c.Set(x+4,22,Violet); }
            return c;
        }

        static Canvas BossFill(int theme)
        {
            var c = new Canvas(16,8);
            Color32 baseColor = theme == 0 ? Hex(0xe5762e) : theme == 1 ? Hex(0x9d8dc5)
                : theme == 2 ? Hex(0xd65d69) : theme == 3 ? Hex(0x6bb563) : Hex(0xa863b5);
            Color32 light = theme == 0 ? Gold : theme == 1 ? Hex(0xe0e4f1)
                : theme == 2 ? Hex(0xffa395) : theme == 3 ? Mint : Hex(0xe2adf0);
            c.Rect(0,0,16,8,baseColor); c.Rect(0,0,16,1,light); c.Rect(0,6,16,2,Alpha(Ink,80));
            c.Line(1,2,5,2,light); c.Line(9,3,13,3,light); c.Set(7,4,Alpha(Ink,90)); c.Set(14,5,light);
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
            var c = new Canvas(6,6);
            c.Rect(1,2,4,2,Hex(0x7b8790)); c.Rect(2,2,3,1,Hex(0xe9e9df));
            c.Set(4,3,Hex(0xb5bec0)); c.Set(0,2,Alpha(Hex(0xb5bec0),75));
            return c;
        }

        static Canvas BananaShot()
        {
            var c = new Canvas(6,6);
            c.Ellipse(1,1,4,4,Hex(0xb6a46b)); c.Ellipse(2,1,3,3,Hex(0xe7d8a0));
            c.Rect(3,2,2,1,Hex(0xfff0cf)); c.Set(1,3,Hex(0x837b61));
            return c;
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

        static Canvas ShotDot()
        {
            var c = new Canvas(6,6);
            c.Ellipse(1,1,4,4,Hex(0x8c9b96)); c.Ellipse(2,1,3,3,Hex(0xd6e1d5));
            c.Rect(3,2,2,1,Hex(0xfff6df)); c.Set(1,3,Hex(0x64716e)); return c;
        }
        static Canvas ShotBone()
        {
            var c = new Canvas(8,4); c.Rect(1,1,6,2,Hex(0xaaa594));
            c.Rect(2,1,5,1,Hex(0xeee4cc)); c.Set(1,0,Hex(0xd3cbb7));
            c.Set(6,0,Hex(0xe3dac2)); c.Set(6,2,Hex(0xc5bca7)); return c;
        }
        static Canvas ShotShock()
        {
            var c = new Canvas(8,6); c.Rect(1,2,6,2,Hex(0x819e9e));
            c.Rect(3,1,3,1,Alpha(Hex(0xa7c6c3),170)); c.Rect(2,4,3,1,Alpha(Hex(0xa7c6c3),120));
            c.Line(2,3,4,2,Hex(0xe4efdf)); c.Line(4,2,6,2,Hex(0xf8f5df)); return c;
        }
        static Canvas ShotDisc(int frame)
        {
            var c = new Canvas(8,8); c.Ellipse(1,1,6,6,Hex(0x718589));
            c.Ellipse(2,2,4,4,Hex(0xb8c7c3)); c.Ellipse(3,3,2,2,Hex(0xeee8d5));
            int[][] points={new[]{2,2,5,5},new[]{3,1,4,6},new[]{5,2,2,5},new[]{6,3,1,4}};
            var p=points[frame%4]; c.Set(p[0],p[1],Hex(0xdce3d5));
            c.Set(p[2],p[3],Hex(0x889b9a)); return c;
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
            var c = new Canvas(16,16);
            for(int y=0;y<16;y++) for(int x=0;x<16;x++)
            {
                double dx=(x-7.5)/7.5,dy=(y-7.5)/7.5,d=dx*dx+dy*dy;
                if(d<1) c.Set(x,y,new Color32(255,247,230,(byte)(Math.Pow(1-d,3)*62)));
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

        static Canvas Medium(int frame)
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

        static Canvas Reaper(int frame)
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

        static Canvas Alchemist(int frame)
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

        static Canvas Player(int frame)
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
            var c = new Canvas(8,8);
            c.Ellipse(1,1,6,6,Hex(0x9c465c)); c.Ellipse(2,2,4,4,Hex(0xe58b9e));
            c.Rect(3,2,2,3,Hex(0xffebd7)); c.Set(5,3,Hex(0xffebd7)); return c;
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
            sprites.Clear(); textures.Clear(); material=null;
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
                        if(nx>=0&&nx<Width&&ny>=0&&ny<Height && original[(Height-1-ny)*Width+nx].a>0) Set(x,y,color);
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
