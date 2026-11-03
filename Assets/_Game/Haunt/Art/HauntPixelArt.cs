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
            bool animated = id == "medium" || id == "larva" || id == "sentinel" || id == "acolyte" || id == "matriarch" || id == "bell" || id == "player" || id == "pumpkin" || id == "ghost" || id == "cultist"
                || id == "bat" || id == "boss" || id == "merchant" || id == "candle" || id == "portal";
            frame = animated ? ((frame % 4) + 4) % 4 : 0;
            string key = id + "/" + frame;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            Canvas pixels;
            switch (id)
            {
                case "player": pixels = Player(frame); break;
                case "medium": pixels = Medium(frame); break;
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

        static Canvas Medium(int frame)
        {
            var c = new Canvas(24, 28);
            int sway = frame == 1 ? 1 : frame == 3 ? -1 : 0;
            c.Polygon(new[] { 5,25, 7,12, 4,9, 12,1, 20,9, 17,13, 20+sway,25, 12,23 }, Violet);
            c.Polygon(new[] { 9,12, 15,12, 18+sway,24, 12,22, 8,24 }, Shade);
            c.Ellipse(8,7,9,8, Ink); c.Rect(9,10,2,2,Mint); c.Rect(14,10,2,2,Mint);
            c.Rect(11,16,3,5,Gold); c.Rect(6,17,3,4,Bone); c.Rect(17,17,3,4,Bone);
            c.Ellipse(3,13+sway,3,3,Mint); c.Ellipse(19,4-sway,3,3,Pink);
            c.Outline(Ink); return c;
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

        static Canvas NewGuardian(bool bell, int frame)
        {
            var c = new Canvas(48, 48);
            int pulse = frame % 2;
            if (bell)
            {
                c.Rect(21,3,6,5,Gold);
                c.Polygon(new[] { 6,37, 12,29, 13,13, 21,7, 28,7, 36,13, 37,29, 43,37 }, Rust);
                c.Polygon(new[] { 12,33, 17,28, 18,15, 24,11, 30,16, 32,30, 37,33 }, Gold);
                c.Rect(8,35,33,5,Bone); c.Ellipse(20,38,8,6+pulse,Gold);
                c.Rect(18,20,5,4,Ink); c.Rect(27,20,5,4,Ink); c.Rect(22,28,6,4,Ink);
                c.Line(6,12,3,30,Violet); c.Line(42,12,45,30,Violet);
            }
            else
            {
                for (int i=0;i<4;i++)
                {
                    int y=14+i*6;
                    c.Line(16,y,3,y-7+pulse,Mint); c.Line(3,y-7+pulse,1,y+4,Teal);
                    c.Line(32,y,44,y-7-pulse,Mint); c.Line(44,y-7-pulse,46,y+4,Teal);
                }
                c.Ellipse(11,15,27,29,Teal); c.Ellipse(16,18,18,20,Violet);
                c.Ellipse(13,5,23,18,Teal);
                for(int i=0;i<3;i++) { c.Ellipse(16+i*6,10,4,4,Gold); c.Rect(17+i*6,11,2,2,Ink); }
                c.Polygon(new[] { 16,19, 20,26, 23,19, 28,26, 32,19 },Bone);
            }
            c.Outline(Ink); return c;
        }

        static Canvas NewWeapon(int kind)
        {
            var c = new Canvas(28, 14);
            c.Rect(2,5,20,5,kind == 2 ? Teal : Stone);
            c.Rect(5,8,5,5,Rust); c.Rect(10,4,7,5,Gold);
            if(kind == 0) { c.Rect(15,3,12,3,Bone); c.Rect(15,8,12,3,LightStone); }
            else if(kind == 1) { c.Rect(13,9,4,4,Gold); c.Rect(19,5,8,3,Bone); }
            else { c.Ellipse(14,2,12,10,Mint); c.Ellipse(19,4,7,6,Ink); }
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
            c.Outline(Ink);
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
            var c=new Canvas(24,12);
            c.Polygon(new[]{2,4,9,4,9,8,6,8,5,10,2,9},Hex(0x9e6757));
            c.Rect(7,3,14,4,Hex(0x9096ad)); c.Rect(10,3,11,1,Hex(0xe0e4dc));
            c.Rect(20,2,3,6,Hex(0x535770)); c.Rect(21,3,2,3,Gold);
            c.Rect(8,7,4,2,Gold); c.Rect(7,1,2,3,Hex(0x5c4b68)); c.Set(8,1,Pale); c.Outline(Ink); return c;
        }
        static Canvas Coin()
        {
            var c=new Canvas(10,12); c.Ellipse(1,1,8,10,Hex(0xba6437)); c.Ellipse(1,1,6,9,Gold);
            c.Line(3,2,3,8,Pale); c.Line(5,3,5,8,Orange); c.Outline(Ink); return c;
        }
        static Canvas Bullet(bool enemy)
        {
            var c=new Canvas(enemy?10:14,enemy?10:6);
            if(enemy) { c.Ellipse(1,1,8,8,Rust); c.Ellipse(2,2,6,6,Pink); c.Ellipse(3,3,4,4,Pale); }
            else { c.Rect(1,2,11,2,Teal); c.Rect(4,1,8,4,Mint); c.Rect(8,2,5,2,Pale); }
            c.Outline(Ink); return c;
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
            var c=new Canvas(32,16); c.Ellipse(1,2,30,12,Alpha(Ink,35)); c.Ellipse(4,4,24,8,Alpha(Ink,85));
            c.Ellipse(8,5,16,6,Alpha(Ink,110)); return c;
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
