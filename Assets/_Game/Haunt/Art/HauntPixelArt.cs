using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    /// <summary>Original, point-filtered pixel sprites. Every texture belongs to this instance.</summary>
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

        public Sprite Get(string id)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (sprites.TryGetValue(id, out var cached)) return cached;
            Canvas pixels;
            switch (id)
            {
                case "player": pixels = Player(); break;
                case "pumpkin": pixels = Pumpkin(); break;
                case "ghost": pixels = Ghost(); break;
                case "cultist": pixels = Cultist(); break;
                case "bat": pixels = Bat(); break;
                case "boss": pixels = Boss(); break;
                case "floor0": pixels = Floor(0); break;
                case "floor1": pixels = Floor(1); break;
                case "floor2": pixels = Floor(2); break;
                case "wall": pixels = Wall(); break;
                case "pillar": pixels = Pillar(); break;
                case "candle": pixels = Candle(); break;
                case "grave": pixels = Grave(); break;
                case "door": pixels = Door(); break;
                case "portal": pixels = Portal(); break;
                case "merchant": pixels = Merchant(); break;
                case "coin": pixels = Coin(); break;
                case "bullet-player": pixels = Bullet(false); break;
                case "bullet-enemy": pixels = Bullet(true); break;
                case "muzzle": pixels = Muzzle(); break;
                case "heart": pixels = Heart(); break;
                default: throw new ArgumentException("Sprite desconhecido: " + id, nameof(id));
            }
            var texture = new Texture2D(pixels.Width, pixels.Height, TextureFormat.RGBA32, false)
            {
                name = "Haunt / " + id,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };
            texture.SetPixels32(pixels.Pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, pixels.Width, pixels.Height),
                new Vector2(.5f, .5f), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Haunt / " + id;
            textures.Add(texture);
            sprites.Add(id, sprite);
            return sprite;
        }

        // Ink, wine shadow, violet stone, desaturated teal, bone and candle-orange.
        static readonly Color32 Ink = Hex(0x11101e), Shade = Hex(0x282238),
            Stone = Hex(0x41354e), LightStone = Hex(0x63536b),
            Teal = Hex(0x387b78), Mint = Hex(0x88d5b1), Pale = Hex(0xdcf1c4),
            Rust = Hex(0x92394d), Orange = Hex(0xe47735), Gold = Hex(0xffc96a),
            Bone = Hex(0xdfd0b4), Violet = Hex(0x916299), Pink = Hex(0xf57a9b);

        static Color32 Hex(uint rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        static readonly Dictionary<char, Color32> Palette = new Dictionary<char, Color32>
        {
            ['#'] = Ink, ['s'] = Shade, ['v'] = Stone, ['l'] = LightStone,
            ['t'] = Teal, ['m'] = Mint, ['p'] = Pale, ['r'] = Rust,
            ['o'] = Orange, ['g'] = Gold, ['b'] = Bone, ['u'] = Violet, ['k'] = Pink,
            ['w'] = Hex(0xffffff), ['d'] = Hex(0x633442), ['n'] = Hex(0x45465c)
        };

        static Canvas Pattern(params string[] rows)
        {
            int width = 0;
            foreach (var row in rows) width = Math.Max(width, row.Length);
            var c = new Canvas(width, rows.Length);
            for (int y = 0; y < rows.Length; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    if (Palette.TryGetValue(rows[y][x], out var color)) c.Set(x, y, color);
            return c;
        }

        // A masked gravekeeper, copper hat, mint goggles and a plum traveling coat.
        static Canvas Player() => Pattern(
            "................", "......###.......", ".....#oro#......", "....#ogoro#.....",
            "....#ooooo#.....", "..###rrrrr###...", ".#ooooogooooo#..", "..###sss###.....",
            "....#m#p#m#.....", "....#bbbb#......", ".....#rr#.......", "...##roor##.....",
            "..#uu#oo#uu#....", "..#bu#oo#ub#....", "..###uuuu###....", "...#uuuuuu#.....",
            "...###ss###.....", "....#s##s#......", "...#ll##ll#.....", "...#######......");

        static Canvas Pumpkin() => Pattern(
            ".......##.......", "......#mt#......", "......#t#.......", "....###t###.....",
            "...#oroooor#....", "..#ogoooooor#...", ".#ogoooorooor#..", ".#oo##ooo##or#..",
            ".#o#g#oo#g#or#..", ".#oooo##oooor#..", ".#or##oo##oor#..", "..#o#gggg#or#...",
            "..#or####ror#...", "...#orroorr#....", "....#######.....", "....##...##.....");

        static Canvas Ghost() => Pattern(
            "................", ".....#####......", "...##mmmmm##....", "..#mpppppmmm#...",
            "..#pppppppmm#...", ".#pppppppppmm#..", ".#pp##ppp##mm#..", ".#pm#s#p#s#mm#..",
            ".#pm###p###mm#..", ".#pmpppppppmm#..", ".#pmpp###ppmm#..", ".#mppp#s#pmmm#..",
            ".#mppp###pmmm#..", ".#mpppppppmmm#..", "..#mmppppmmm#...", "..#mmmppmmmm#...",
            "..#m#mmmm#mm#...", "..##.#mm#.##....", "......##........", "................");

        static Canvas Cultist() => Pattern(
            ".......#........", "......#u#.......", ".....#uuu#......", "....#uvuuu#.....",
            "...#uvvuuuu#....", "..#uvvvuuuuu#...", "..#u#######u#...", "..#v#k###k#v#...",
            "...#s#####s#....", "....##ss###.....", "....#rorr#......", "...#u#go#u#.....",
            "..#uu#or#uu#....", "..#bu#or#ub#....", "..#uu#or#uu#....", "..#uu#rr#uu#....",
            ".#uuu#rr#uuu#...", ".#vuuuvvuuuv#...", ".############...", "...##....##.....");

        static Canvas Bat() => Pattern(
            "#..................#", "##......#..#......##", "#v#....#t##t#....#v#",
            "#uv#...#mmmm#...#vu#", "#uuv###m#kk#mt##vuu#", "#uuuuuvm#pp#mtvuuuu#",
            "#vvvuuuvmmmmvvuuuvv#", "##..#uuuvttvuuu#..##", ".....##uvvvvu##.....",
            ".......#tttt#.......", ".......##..##.......", "....................");

        static Canvas Floor(int variant)
        {
            var c = new Canvas(16, 16);
            c.Rect(0, 0, 16, 16, Hex(0x242331));
            c.Rect(1, 1, 14, 14, variant == 0 ? Hex(0x302b3b) : variant == 1 ? Hex(0x2e2b3a) : Hex(0x2b2b38));
            c.Line(1, 1, 14, 1, Hex(0x3b3346));
            c.Line(1, 1, 1, 13, Hex(0x373144));
            c.Line(2, 14, 14, 14, Hex(0x1d1e2a));
            c.Line(14, 2, 14, 14, Hex(0x20212d));
            if (variant == 0)
            {
                c.Line(10, 1, 9, 4, Shade); c.Line(9, 4, 12, 6, Shade);
                c.Set(4, 9, Hex(0x3d3446)); c.Set(5, 9, Hex(0x3d3446));
                c.Set(7, 12, Hex(0x242632));
            }
            else if (variant == 1)
            {
                c.Line(2, 11, 5, 11, Hex(0x394442)); c.Set(4, 10, Hex(0x3b4a43));
                c.Set(6, 3, Hex(0x3e3548)); c.Set(11, 8, Hex(0x272635));
            }
            else
            {
                c.Line(6, 14, 7, 11, Hex(0x20212d)); c.Line(7, 11, 5, 8, Hex(0x20212d));
                c.Set(10, 4, Hex(0x3d3446)); c.Set(11, 4, Hex(0x3d3446));
                c.Set(9, 11, Hex(0x424039));
            }
            return c;
        }

        static Canvas Wall()
        {
            var c = new Canvas(16, 16);
            c.Rect(0, 0, 16, 16, Ink);
            c.Rect(0, 0, 16, 2, Hex(0x756174));
            c.Rect(0, 2, 16, 4, LightStone); c.Line(0, 5, 15, 5, Stone);
            c.Rect(0, 7, 7, 4, Stone); c.Rect(8, 7, 8, 4, Hex(0x493b53));
            c.Line(1, 7, 6, 7, Hex(0x5b4962)); c.Line(9, 7, 15, 7, Hex(0x5b4962));
            c.Rect(0, 12, 12, 3, Hex(0x332c40)); c.Rect(13, 12, 3, 3, Hex(0x393046));
            c.Set(4, 8, Hex(0x6b556c)); c.Set(11, 13, Hex(0x484052));
            c.Line(0, 15, 15, 15, Shade);
            return c;
        }

        static Canvas Pillar()
        {
            var c = new Canvas(16, 24);
            c.Rect(2, 2, 12, 21, Ink); c.Rect(1, 1, 14, 5, Ink);
            c.Rect(2, 1, 12, 2, LightStone); c.Rect(3, 3, 10, 2, Stone);
            c.Rect(4, 6, 8, 14, Stone); c.Rect(4, 6, 2, 14, LightStone);
            c.Rect(10, 6, 2, 14, Shade); c.Line(7, 6, 7, 16, Hex(0x51425d));
            c.Line(10, 9, 7, 12, Ink); c.Line(7, 12, 9, 15, Ink);
            c.Rect(2, 20, 12, 2, LightStone); c.Rect(1, 22, 14, 2, Shade);
            c.Set(3, 19, Teal); c.Set(4, 19, Teal); c.Set(12, 21, Teal);
            return c;
        }

        static Canvas Candle() => Pattern(
            "......o.........", ".....ogo........", ".....gpg........", "......g.........",
            ".....#r#........", ".....#b#........", ".....#bb#.......", ".....#b#........",
            ".....#b#........", "....##r##.......", "...#roror#......", "...#######......");

        static Canvas Grave() => Pattern(
            "................", ".....######.....", "....#lllllv#....", "...#llvvvvvv#...",
            "...#lvvv#vvv#...", "...#lvv###vv#...", "...#lvvv#vvv#...", "...#lvvv#vvv#...",
            "...#lvvvvvvv#...", "...#lvvsssvv#...", "...#lvvvvvvv#...", "...#lvvvvvvv#...",
            "...#vvssvvvv#...", "..##vvvvvvsv##..", ".#ttvvssttssvv#.", ".##############.");

        static Canvas Door()
        {
            var c = new Canvas(16, 24);
            c.Rect(1, 6, 14, 18, Ink); c.Ellipse(1, 0, 14, 15, Ink);
            c.Rect(2, 6, 12, 17, Stone); c.Ellipse(2, 1, 12, 14, LightStone);
            c.Ellipse(4, 4, 8, 12, Ink); c.Rect(4, 10, 8, 14, Ink);
            c.Rect(5, 10, 6, 13, Shade); c.Ellipse(5, 5, 6, 11, Shade);
            c.Line(7, 7, 7, 21, Hex(0x3d2e3e)); c.Line(9, 7, 9, 21, Hex(0x3d2e3e));
            c.Rect(4, 13, 8, 2, Rust); c.Rect(4, 19, 8, 2, Rust);
            c.Set(9, 16, Gold); c.Set(6, 2, Bone); c.Set(7, 2, Bone);
            c.Set(9, 2, Bone); c.Set(10, 2, Bone); c.Rect(7, 3, 3, 3, Stone);
            return c;
        }

        static Canvas Portal()
        {
            var c = new Canvas(32, 32);
            c.Ellipse(0, 1, 32, 30, Ink); c.Ellipse(2, 2, 28, 27, Stone);
            c.Ellipse(4, 4, 24, 23, Teal); c.Ellipse(6, 5, 20, 21, Mint);
            c.Ellipse(8, 7, 16, 18, Hex(0x315751)); c.Ellipse(9, 8, 14, 16, Shade);
            c.Ellipse(11, 10, 10, 12, Ink);
            c.Rect(0, 27, 32, 4, Ink); c.Rect(2, 27, 28, 2, LightStone);
            c.Rect(4, 29, 24, 2, Stone);
            c.Line(5, 8, 8, 5, Pale); c.Line(23, 23, 25, 19, Pale);
            c.Set(6, 18, Pale); c.Set(24, 11, Pale); c.Set(13, 3, Gold);
            c.Rect(15, 13, 2, 2, Teal); c.Set(18, 19, Mint); c.Set(12, 17, Mint);
            c.Set(4, 29, Teal); c.Set(27, 29, Teal);
            return c;
        }

        static Canvas Boss()
        {
            var c = new Canvas(32, 32);
            // Antler crown around a cracked pumpkin skull; thorn cape and roots.
            c.Line(6, 2, 8, 9, Ink); c.Line(6, 2, 2, 0, Ink); c.Line(6, 5, 2, 4, Ink);
            c.Line(25, 2, 23, 9, Ink); c.Line(25, 2, 29, 0, Ink); c.Line(25, 5, 29, 3, Ink);
            c.Line(6, 2, 8, 7, Rust); c.Line(25, 2, 23, 7, Rust);
            c.Rect(14, 0, 4, 6, Ink); c.Rect(15, 1, 2, 5, Teal); c.Set(17, 1, Mint);
            c.Ellipse(5, 5, 22, 18, Ink); c.Ellipse(6, 6, 20, 16, Rust);
            c.Ellipse(7, 6, 17, 15, Orange); c.Ellipse(8, 6, 6, 14, Hex(0xf49b42));
            c.Line(16, 6, 16, 8, Rust); c.Line(16, 8, 19, 10, Rust);
            c.Line(19, 10, 18, 12, Rust); c.Line(12, 6, 10, 9, Gold);
            c.Rect(8, 11, 6, 4, Ink); c.Rect(19, 11, 5, 4, Ink);
            c.Rect(9, 12, 4, 2, Gold); c.Rect(20, 12, 3, 2, Gold);
            c.Set(8, 10, Rust); c.Set(23, 10, Rust);
            c.Rect(15, 14, 3, 2, Ink); c.Rect(11, 17, 11, 3, Ink);
            c.Rect(12, 18, 9, 1, Gold); c.Set(13, 17, Orange); c.Set(18, 19, Orange);
            c.Rect(8, 21, 17, 8, Ink); c.Rect(9, 22, 15, 7, Stone);
            c.Line(15, 22, 13, 28, Violet); c.Line(18, 22, 20, 28, Violet);
            c.Rect(14, 22, 5, 2, Rust); c.Rect(16, 24, 2, 4, Gold);
            c.Line(9, 23, 3, 27, Ink); c.Line(22, 23, 29, 27, Ink);
            c.Line(8, 23, 3, 26, Teal); c.Line(23, 23, 28, 26, Teal);
            c.Line(2, 26, 2, 23, Teal); c.Line(29, 26, 30, 23, Teal);
            c.Line(10, 28, 5, 31, Ink); c.Line(21, 28, 27, 31, Ink);
            c.Line(10, 29, 7, 30, Teal); c.Line(22, 29, 25, 30, Teal);
            c.Line(13, 28, 12, 31, Ink); c.Line(19, 28, 20, 31, Ink);
            return c;
        }

        static Canvas Merchant()
        {
            var c = new Canvas(24, 32);
            // The candle merchant, a gentle skull suspended in an oversized hood.
            c.Ellipse(5, 0, 14, 18, Ink); c.Ellipse(6, 1, 12, 17, Teal);
            c.Ellipse(8, 4, 8, 12, Shade); c.Ellipse(8, 5, 8, 9, Bone);
            c.Rect(9, 9, 2, 2, Ink); c.Rect(13, 9, 2, 2, Ink);
            c.Set(12, 11, Rust); c.Line(10, 13, 14, 13, Ink);
            c.Rect(9, 14, 6, 2, Shade); c.Rect(7, 15, 10, 9, Ink);
            c.Rect(8, 15, 8, 10, Teal); c.Rect(9, 16, 6, 9, Hex(0x285656));
            c.Rect(10, 16, 3, 2, Gold); c.Line(10, 19, 10, 22, Rust);
            c.Line(7, 17, 4, 22, Ink); c.Line(16, 17, 19, 22, Ink);
            c.Line(7, 18, 4, 21, Teal); c.Line(16, 18, 19, 21, Teal);
            c.Rect(3, 21, 3, 3, Bone); c.Rect(18, 21, 3, 3, Bone);
            c.Rect(1, 24, 22, 7, Ink); c.Rect(2, 24, 20, 2, Orange);
            c.Rect(3, 26, 18, 4, Rust); c.Rect(10, 26, 4, 3, Gold);
            c.Rect(11, 27, 2, 2, Shade); c.Set(4, 27, Orange); c.Set(19, 27, Orange);
            c.Rect(3, 31, 3, 1, Ink); c.Rect(18, 31, 3, 1, Ink);
            c.Rect(3, 19, 1, 4, Bone); c.Set(3, 18, Gold); c.Set(3, 17, Orange);
            return c;
        }

        static Canvas Coin() => Pattern(
            "..####..", ".#goog#.", "#goggoo#", "#go#goo#", "#go#goo#", "#googor#", ".#oror#.", "..####..");
        static Canvas Bullet(bool enemy) => enemy ? Pattern(
            "..####..", ".#rkkk#.", "#rkkpkk#", "#kkpppk#", "#kkpppk#", "#rkkkkr#", ".#rrrr#.", "..####..") : Pattern(
            "...##...", "..#gg#..", ".#gppg#.", ".#gppg#.", "..#gg#..", "...##...");
        static Canvas Muzzle() => Pattern(
            "...g....", "..ogo...", "gogpgog.", ".gpppg..", "ogpppgo.", "..gpg...", "..ogo...", "...g....");
        static Canvas Heart() => Pattern(
            ".###.###..", "#kkk#kkk#.", "#kpkkkkk#.", "#kkkkkkk#.", ".#kkkkk#..", "..#kkk#...", "...#k#....", "....#.....");

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var sprite in sprites.Values) DestroyOwned(sprite);
            foreach (var texture in textures) DestroyOwned(texture);
            if (material) DestroyOwned(material);
            sprites.Clear(); textures.Clear(); material = null;
        }

        static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        sealed class Canvas
        {
            public readonly int Width, Height;
            public readonly Color32[] Pixels;
            public Canvas(int width, int height) { Width = width; Height = height; Pixels = new Color32[width * height]; }
            // Drawing coordinates use a top-left origin; Unity texture coordinates use bottom-left.
            public void Set(int x, int y, Color32 color)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height) Pixels[(Height - 1 - y) * Width + x] = color;
            }
            public void Rect(int x, int y, int width, int height, Color32 color)
            {
                for (int py = y; py < y + height; py++) for (int px = x; px < x + width; px++) Set(px, py, color);
            }
            public void Ellipse(int x, int y, int width, int height, Color32 color)
            {
                float rx = width * .5f, ry = height * .5f;
                for (int py = y; py < y + height; py++)
                    for (int px = x; px < x + width; px++)
                    {
                        float dx = (px + .5f - x - rx) / rx, dy = (py + .5f - y - ry) / ry;
                        if (dx * dx + dy * dy <= 1) Set(px, py, color);
                    }
            }
            public void Line(int x0, int y0, int x1, int y1, Color32 color)
            {
                int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
                int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, error = dx + dy;
                while (true)
                {
                    Set(x0, y0, color);
                    if (x0 == x1 && y0 == y1) break;
                    int twice = 2 * error;
                    if (twice >= dy) { error += dy; x0 += sx; }
                    if (twice <= dx) { error += dx; y0 += sy; }
                }
            }
        }
    }
}
