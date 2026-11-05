using System;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntPixelArt
    {
        // The old silhouette remains the construction guide. The new grid adds
        // material edges, mechanics and surface details at twice the resolution.
        static bool HasDetailedEquipment(string id) => id == "weapon" || id == "scattergun" || id == "repeater"
            || id == "cannon" || id == "stapler" || id == "banana-gun" || id == "spinning-top"
            || id == "hunter-carbine" || id == "veil-wand" || id == "bone-caster" || id == "arc-coil"
            || id == "mirror-disc" || id == "ray-lantern" || id == "bone-flute" || id == "umbrella-gun"
            || id == "hive-launcher" || id == "boxing-glove" || id == "water-urn" || id == "ammo-cache"
            || id == "giant-rounds" || id == "character-relic" || id == "quick-boots" || id == "gravestone"
            || id == "grave" || id == "fence" || id == "ossuary-candle" || id == "merchant"
            || id == "decor-theme0" || id == "decor-theme1" || id == "decor-theme2" || id == "decor-theme3" || id == "decor-theme4";

        static Canvas RefineEquipment(string id, Canvas source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var c = new Canvas(source.Width * 2, source.Height * 2);
            for (int y = 0; y < source.Height; y++) for (int x = 0; x < source.Width; x++)
            {
                Color32 p = EquipmentRead(source, x, y);
                if (p.a > 0) c.Rect(x * 2, y * 2, 2, 2, p);
            }
            EquipmentMaterialEdges(c);
            switch (id)
            {
                case "weapon": EquipmentRevolver(c); break;
                case "scattergun": EquipmentScattergun(c); break;
                case "repeater": EquipmentRepeater(c); break;
                case "cannon": EquipmentCannon(c); break;
                case "stapler": EquipmentStapler(c); break;
                case "banana-gun": EquipmentBanana(c); break;
                case "spinning-top": EquipmentTop(c); break;
                case "hunter-carbine": EquipmentCarbine(c); break;
                case "veil-wand": EquipmentWand(c); break;
                case "bone-caster": EquipmentBoneCaster(c); break;
                case "arc-coil": EquipmentCoil(c); break;
                case "mirror-disc": EquipmentMirror(c); break;
                case "ray-lantern": EquipmentLantern(c); break;
                case "bone-flute": EquipmentFlute(c); break;
                case "umbrella-gun": EquipmentUmbrella(c); break;
                case "hive-launcher": EquipmentHive(c); break;
                case "boxing-glove": EquipmentGlove(c); break;
                case "water-urn": EquipmentUrn(c); break;
                case "ammo-cache": EquipmentAmmunition(c); break;
                case "giant-rounds": EquipmentMagnifier(c); break;
                case "character-relic": EquipmentPendant(c); break;
                case "quick-boots": EquipmentBoot(c); break;
                case "gravestone": case "grave": EquipmentGrave(c); break;
                case "fence": EquipmentFence(c); break;
                case "ossuary-candle": EquipmentOssuary(c); break;
                case "merchant": EquipmentMerchant(c); break;
                default: EquipmentDecoration(c, id); break;
            }
            return c;
        }

        static Color32 EquipmentRead(Canvas c, int x, int y)
            => x < 0 || y < 0 || x >= c.Width || y >= c.Height ? new Color32() : c.Pixels[(c.Height - 1 - y) * c.Width + x];
        static bool EquipmentSame(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        static Color32 EquipmentTone(Color32 p, float gain)
            => new Color32((byte)Math.Min(255, p.r * gain), (byte)Math.Min(255, p.g * gain), (byte)Math.Min(255, p.b * gain), p.a);

        static void EquipmentMaterialEdges(Canvas c)
        {
            var original = (Color32[])c.Pixels.Clone();
            for (int y = 1; y < c.Height - 1; y++) for (int x = 1; x < c.Width - 1; x++)
            {
                int at = (c.Height - 1 - y) * c.Width + x;
                Color32 p = original[at], above = original[at + c.Width], below = original[at - c.Width];
                if (p.a < 220 || p.r + p.g + p.b < 135) continue;
                // A single-pixel bevel follows material boundaries. Flat surfaces
                // stay broad and quiet instead of receiving checkerboard noise.
                if (!EquipmentSame(p, above) && above.a > 0) c.Set(x, y, EquipmentTone(p, 1.12f));
                else if (!EquipmentSame(p, below) && below.a > 0) c.Set(x, y, EquipmentTone(p, .85f));
            }
        }

        static void EquipmentScrew(Canvas c, int x, int y, Color32 metal)
        {
            c.Rect(x - 1, y - 1, 3, 3, metal); c.Set(x - 1, y - 1, Pale); c.Set(x, y, Ink); c.Set(x + 1, y + 1, Shade);
        }
        static void EquipmentGrip(Canvas c, int x, int y, int rows)
        {
            for (int i = 0; i < rows; i++) { c.Line(x, y + i * 3, x + 5, y + i * 3 + 1, Hex(0x3d343c)); c.Set(x, y + i * 3, Hex(0xc28d63)); }
        }
        static void EquipmentRing(Canvas c, int x, int y, int w, int h, Color32 bright, Color32 dark)
        {
            c.Ellipse(x, y, w, h, dark); c.Ellipse(x + 1, y, w - 2, h - 1, bright); c.Ellipse(x + 2, y + 2, w - 4, h - 4, dark);
        }
        static void EquipmentGlass(Canvas c, int x, int y, int w, int h, Color32 liquid)
        {
            c.Rect(x, y, w, h, Hex(0x233e4b)); c.Rect(x + 1, y + 2, w - 2, h - 3, liquid);
            c.Line(x + 1, y + 1, x + w - 2, y + 1, Pale); c.Line(x + 1, y + 2, x + 1, y + h - 3, Hex(0xb7e1d2));
            if (w > 5) { c.Set(x + w - 3, y + h - 4, Pale); c.Set(x + 3, y + 3, Hex(0x417868)); }
        }

        static void EquipmentRevolver(Canvas c)
        {
            c.Line(33, 11, 48, 11, Hex(0xe0e6d6)); c.Line(33, 15, 47, 15, Hex(0x657483));
            c.Rect(35, 12, 11, 1, Hex(0x9babb6)); c.Rect(49, 11, 2, 7, Hex(0x232d3d)); c.Set(50, 12, Gold);
            EquipmentRing(c, 17, 11, 16, 12, Hex(0xe4bb73), Hex(0x71534c));
            for (int i = 0; i < 5; i++) { double a = i * Math.PI * 2 / 5; int x = 25 + (int)(Math.Cos(a) * 4), y = 17 + (int)(Math.Sin(a) * 3); c.Ellipse(x - 1, y - 1, 2, 2, Ink); c.Set(x, y - 1, Hex(0xd7bb8e)); }
            c.Ellipse(23, 15, 3, 3, Gold); c.Set(23, 15, Pale); EquipmentScrew(c, 15, 19, LightStone);
            EquipmentGrip(c, 7, 19, 3); c.Line(20, 24, 24, 23, Hex(0xf4d08d)); c.Set(22, 24, Ink);
            c.Rect(38, 7, 3, 1, Mint); c.Rect(17, 7, 2, 2, Hex(0xb7bcbb));
        }
        static void EquipmentScattergun(Canvas c)
        {
            c.Line(34, 10, 57, 10, Hex(0xd7d9d3)); c.Line(34, 21, 57, 21, Hex(0x8b9bab));
            for (int x = 38; x < 53; x += 4) { c.Rect(x, 12, 2, 1, Shade); c.Rect(x, 23, 2, 1, Hex(0x394858)); }
            c.Rect(58, 11, 2, 5, Ink); c.Rect(58, 22, 2, 5, Ink); c.Line(57, 10, 57, 28, Gold);
            c.Line(36, 17, 49, 17, Hex(0xc38a58)); c.Line(36, 18, 48, 18, Hex(0x593e39));
            EquipmentScrew(c, 25, 17, Gold); EquipmentScrew(c, 31, 18, LightStone); EquipmentGrip(c, 8, 22, 3);
            c.Line(23, 24, 28, 26, Gold); c.Line(28, 26, 30, 23, Hex(0xb18b61));
        }
        static void EquipmentRepeater(Canvas c)
        {
            c.Line(35, 14, 57, 14, Pale); c.Rect(37, 15, 14, 1, Hex(0x9baab3));
            for (int x = 41; x < 56; x += 4) c.Rect(x, 18, 2, 1, Shade);
            EquipmentRing(c, 25, 21, 13, 11, Gold, Hex(0x6d4548)); c.Line(29, 23, 28, 28, Hex(0xf9dc92));
            c.Line(33, 23, 34, 28, Hex(0x9c6b49)); c.Rect(30, 22, 1, 7, Hex(0xcc8a3f));
            EquipmentGlass(c, 37, 9, 8, 4, Mint); EquipmentScrew(c, 24, 16, LightStone); EquipmentGrip(c, 8, 22, 3);
            c.Line(21, 26, 23, 28, Gold); c.Line(23, 28, 25, 25, Gold);
        }
        static void EquipmentCannon(Canvas c)
        {
            EquipmentRing(c, 49, 7, 12, 23, Hex(0x637887), Hex(0x192e3c));
            c.Ellipse(52, 12, 6, 13, Mint); c.Ellipse(54, 14, 4, 9, Hex(0x143b45)); c.Line(53, 13, 53, 19, Pale);
            c.Line(29, 11, 45, 11, Hex(0xb7efdb)); c.Line(28, 24, 45, 24, Hex(0x367e88));
            EquipmentGlass(c, 31, 13, 9, 9, Hex(0x52d4ae));
            for (int y = 11; y < 27; y += 5) { EquipmentScrew(c, 24, y, Gold); EquipmentScrew(c, 41, y, Gold); }
            c.Line(34, 4, 39, 4, Pale); c.Rect(35, 5, 2, 2, Hex(0x967753)); EquipmentGrip(c, 8, 22, 3);
        }
        static void EquipmentStapler(Canvas c)
        {
            c.Line(16, 7, 30, 7, Hex(0xffceae)); c.Line(32, 9, 48, 13, Hex(0xffa8a3));
            c.Line(18, 16, 45, 18, Hex(0x8d284d)); c.Rect(17, 11, 13, 1, Hex(0xf59292));
            EquipmentScrew(c, 11, 22, Gold); c.Rect(50, 18, 7, 1, Hex(0xe9e6ce)); c.Rect(55, 19, 3, 3, Shade);
            c.Line(17, 25, 54, 25, Pale); c.Line(18, 29, 53, 29, Hex(0x5d6e7a));
            for (int x = 22; x < 47; x += 4) { c.Rect(x, 26, 1, 2, Hex(0x667f8b)); c.Set(x + 1, 26, Hex(0xe2e6d8)); }
            c.Rect(23, 31, 13, 1, Ink); c.Line(20, 34, 28, 34, Hex(0xce5d6c));
        }
        static void EquipmentBanana(Canvas c)
        {
            c.Line(12, 8, 16, 17, Hex(0xfff3b8)); c.Line(18, 21, 25, 27, Hex(0xffe894));
            c.Line(29, 30, 37, 32, Hex(0xfff3b8)); c.Line(40, 31, 49, 27, Hex(0xffe49a));
            c.Line(13, 16, 20, 26, Hex(0xd19a37)); c.Line(22, 28, 34, 35, Hex(0xab762d));
            c.Line(37, 35, 47, 32, Hex(0xb27b2f)); c.Line(49, 31, 54, 25, Hex(0x9b6830));
            foreach (int x in new[] { 24, 35, 46 }) { int y = x == 24 ? 27 : x == 35 ? 33 : 29; c.Rect(x, y, 2, 1, Hex(0x795439)); c.Set(x - 1, y - 1, Hex(0xffd16a)); }
            c.Rect(57, 13, 3, 3, Ink); c.Line(55, 11, 59, 11, LightStone); c.Line(7, 4, 12, 4, Hex(0xa69a65));
            c.Set(29, 21, Pale); c.Set(29, 24, Hex(0xf4cb54));
        }
        static void EquipmentTop(Canvas c)
        {
            EquipmentRing(c, 13, 8, 37, 19, Hex(0x84e2d2), Hex(0x2a6477));
            c.Ellipse(22, 11, 20, 10, Hex(0x306c7b)); c.Ellipse(27, 12, 9, 7, Gold); c.Ellipse(29, 13, 5, 5, Hex(0x7c5b5f));
            c.Line(11, 27, 26, 32, Hex(0xc5a4ec)); c.Line(27, 33, 36, 34, Hex(0x8b77c5));
            for (int x = 14; x < 47; x += 8) { c.Polygon(new[] { x, 27, x + 4, 28, x + 2, 33 }, Hex(0x403766)); c.Set(x, 26, Mint); }
            EquipmentScrew(c, 24, 17, Gold); EquipmentScrew(c, 42, 17, Gold);
            c.Rect(59, 15, 2, 6, Ink); c.Line(48, 14, 57, 14, Pale); c.Line(29, 39, 31, 43, Hex(0xc4d3d5));
        }
        static void EquipmentCarbine(Canvas c)
        {
            c.Line(24, 14, 56, 14, Hex(0xd8dfd7)); c.Line(23, 23, 51, 23, Hex(0x343c4d));
            EquipmentGlass(c, 24, 16, 11, 5, Hex(0x63baac));
            for (int x = 39; x < 52; x += 4) c.Rect(x, 17, 2, 1, Hex(0x333d4d));
            c.Rect(56, 15, 2, 9, Ink); c.Line(54, 13, 54, 26, Hex(0x72848b));
            EquipmentScrew(c, 38, 19, Gold); EquipmentScrew(c, 20, 21, LightStone);
            EquipmentGrip(c, 9, 22, 2); c.Line(18, 27, 24, 27, Hex(0xe1be83)); c.Rect(35, 11, 3, 1, Pale);
            c.Line(5, 27, 12, 28, Hex(0x9b6653));
        }
        static void EquipmentWand(Canvas c)
        {
            c.Line(8, 26, 40, 15, Hex(0xe2c48c)); c.Line(10, 28, 38, 18, Hex(0x604353));
            for (int x = 14; x < 35; x += 6) { int y = 25 - x / 3; c.Line(x, y, x + 2, y + 3, Hex(0xb18ba0)); c.Set(x, y, Gold); }
            c.Polygon(new[] { 45, 8, 50, 4, 56, 7, 59, 14, 54, 21, 47, 20, 43, 14 }, Hex(0xb884d5));
            c.Polygon(new[] { 49, 7, 54, 8, 56, 13, 52, 18, 48, 15 }, Hex(0x4e3e79));
            c.Polygon(new[] { 51, 8, 54, 10, 53, 15, 50, 17, 49, 12 }, Mint); c.Line(51, 9, 50, 13, Pale);
            c.Line(45, 10, 43, 15, Gold); c.Line(56, 18, 59, 16, Gold); c.Set(47, 7, Pale); c.Set(56, 21, Pink);
        }
        static void EquipmentBoneCaster(Canvas c)
        {
            c.Line(10, 25, 42, 18, Hex(0xd9ccaf)); c.Line(12, 26, 42, 20, Hex(0x8b7c81));
            c.Line(11, 23, 18, 22, Pale); c.Line(23, 21, 29, 20, Pale);
            c.Ellipse(48, 11, 12, 12, Hex(0xf0e1c1)); c.Ellipse(51, 15, 3, 4, Ink); c.Ellipse(56, 14, 3, 4, Ink);
            c.Rect(54, 20, 2, 2, Hex(0x695e62)); c.Line(50, 24, 57, 24, Hex(0x8e7f7c));
            for (int x = 50; x < 58; x += 2) c.Line(x, 23, x, 25, Pale);
            c.Line(50, 12, 53, 11, Pale); c.Set(52, 16, Mint); c.Line(27, 19, 28, 28, Hex(0x5e2d3d)); c.Set(30, 18, Gold);
        }
        static void EquipmentCoil(Canvas c)
        {
            c.Rect(19, 15, 30, 9, Hex(0x4a3e47));
            for (int x = 22; x < 50; x += 8)
            {
                c.Rect(x, 11, 3, 17, Hex(0xc78f4b)); c.Line(x, 11, x, 26, Hex(0xffdb95)); c.Line(x + 2, 13, x + 2, 28, Hex(0x79543e));
                c.Set(x + 1, 11, Pale); c.Line(x + 3, 21, x + 5, 17, Hex(0x9d744e));
            }
            EquipmentGlass(c, 53, 11, 7, 17, Hex(0x64dfb2)); c.Line(56, 13, 55, 18, Pale); c.Line(55, 18, 58, 21, Pale);
            EquipmentGrip(c, 11, 23, 2); EquipmentScrew(c, 18, 22, LightStone);
        }
        static void EquipmentMirror(Canvas c)
        {
            EquipmentRing(c, 27, 3, 34, 34, Hex(0xd9d8c4), Hex(0x705592));
            c.Ellipse(33, 9, 22, 22, Hex(0x326274));
            c.Polygon(new[] { 36, 11, 49, 10, 43, 18, 52, 22, 39, 29, 44, 20, 35, 17 }, Hex(0xb6ece0));
            c.Line(37, 11, 41, 11, Pale); c.Line(46, 13, 38, 21, Hex(0x68a5ae)); c.Line(45, 23, 48, 26, Pale);
            for (int i = 0; i < 6; i++) { double a = i * Math.PI / 3; int x = 44 + (int)(Math.Cos(a) * 15), y = 20 + (int)(Math.Sin(a) * 15); c.Set(x, y, Gold); }
            EquipmentGrip(c, 14, 25, 2); c.Rect(43, 4, 2, 2, Pale);
        }
        static void EquipmentLantern(Canvas c)
        {
            c.Line(25, 5, 51, 5, Hex(0xffe7a6)); c.Line(24, 8, 24, 29, Hex(0xd2a75d));
            c.Line(48, 10, 48, 28, Hex(0xa27348)); c.Rect(31, 10, 13, 20, Hex(0x24494e));
            c.Polygon(new[] { 35, 12, 40, 11, 42, 23, 39, 27, 34, 25 }, Mint);
            c.Line(35, 13, 35, 23, Pale); c.Line(39, 13, 40, 24, Hex(0x48aa92)); c.Set(40, 15, Pale);
            c.Line(25, 15, 50, 15, Hex(0xe4c786)); c.Line(25, 25, 50, 25, Hex(0xae854d));
            EquipmentScrew(c, 25, 15, Gold); EquipmentScrew(c, 49, 25, Gold); c.Rect(56, 13, 2, 11, Ink); EquipmentGrip(c, 11, 26, 2);
        }
        static void EquipmentFlute(Canvas c)
        {
            c.Line(7, 23, 57, 10, Hex(0xf3e7cb)); c.Line(8, 26, 56, 14, Hex(0x9c8c8c));
            for (int x = 18; x < 49; x += 9)
            {
                int y = 22 - x / 4; c.Ellipse(x, y, 4, 3, Hex(0x544653)); c.Set(x + 1, y, Hex(0xbea9a0));
                c.Line(x + 5, y - 3, x + 6, y + 3, Hex(0xbfb09e));
            }
            c.Line(11, 23, 13, 27, Hex(0x80576b)); c.Line(53, 10, 55, 14, Hex(0xc5b49d));
            c.Line(29, 16, 31, 20, Gold); c.Rect(29, 19, 3, 3, Hex(0x7a364c)); c.Set(29, 19, Pale);
        }
        static void EquipmentUmbrella(Canvas c)
        {
            c.Line(12, 31, 28, 15, Hex(0x94dcdb)); c.Line(28, 15, 52, 7, Hex(0x94dcdb));
            c.Line(25, 30, 37, 16, Hex(0x22677b)); c.Line(38, 24, 49, 13, Hex(0x22677b));
            c.Line(26, 15, 24, 22, Hex(0xd1ece2)); c.Line(45, 10, 41, 17, Hex(0xb0e2de));
            c.Line(13, 34, 58, 15, Hex(0xf6d7a0)); c.Line(14, 36, 57, 17, Hex(0x7d5f53));
            c.Rect(61, 9, 4, 3, Hex(0x6c4c45)); c.Set(61, 9, Pale); EquipmentScrew(c, 37, 23, Gold);
            c.Line(13, 36, 13, 41, Bone); c.Line(12, 41, 7, 43, Hex(0xd8b87c)); c.Line(6, 42, 4, 39, Pale);
        }
        static void EquipmentHive(Canvas c)
        {
            for (int row = 0; row < 4; row++) for (int col = 0; col < 5; col++)
            {
                int x = 18 + col * 6 + row % 2 * 3, y = 9 + row * 4;
                c.Polygon(new[] { x, y + 1, x + 2, y, x + 4, y + 1, x + 4, y + 3, x + 2, y + 4, x, y + 3 }, Hex(0xbb7a31));
                c.Line(x + 1, y + 1, x + 3, y + 1, Hex(0xf6d880)); c.Set(x + 1, y + 2, Hex(0x765638));
            }
            EquipmentGlass(c, 9, 16, 4, 13, Hex(0x7ce5bf)); c.Line(46, 15, 61, 15, Hex(0xe1b369));
            c.Rect(62, 18, 3, 5, Ink); EquipmentScrew(c, 47, 21, LightStone); EquipmentGrip(c, 15, 29, 2);
            c.Line(29, 3, 31, 7, Ink); c.Set(27, 3, Pale); c.Set(30, 4, Hex(0xf6ce7e));
        }
        static void EquipmentGlove(Canvas c)
        {
            c.Line(45, 9, 55, 9, Hex(0xffe3d3)); c.Line(42, 12, 40, 17, Hex(0xffb4b0));
            c.Line(55, 12, 60, 16, Hex(0xf67e99)); c.Line(60, 16, 60, 23, Hex(0xa93761));
            c.Line(46, 29, 58, 29, Hex(0x8d3458)); c.Line(46, 30, 55, 32, Hex(0xe67b8c));
            for (int x = 44; x < 58; x += 3) { c.Line(x, 29, x + 1, 31, Hex(0xffa4b3)); c.Set(x, 30, Hex(0x6e3154)); }
            for (int x = 21; x < 36; x += 5) { c.Line(x, 20, x + 2, 26, Hex(0xe3dfd5)); c.Set(x, 19, Pale); }
            c.Line(32, 18, 32, 27, Hex(0xe7c16b)); EquipmentScrew(c, 34, 23, Gold); EquipmentGrip(c, 8, 28, 2);
        }

        static void EquipmentUrn(Canvas c)
        {
            c.Ellipse(19, 7, 22, 9, Hex(0xa6c8c6)); c.Ellipse(22, 9, 16, 5, Hex(0x284b64));
            c.Line(23, 9, 34, 9, Hex(0xe0e4ce)); c.Line(23, 12, 34, 12, Hex(0x51889c));
            c.Line(17, 28, 16, 43, Hex(0xc8e2d9)); c.Line(18, 29, 18, 39, Hex(0x98cbd1));
            c.Line(43, 31, 45, 40, Hex(0x2c5d7c)); c.Line(45, 40, 43, 51, Hex(0x264e68));
            c.Line(19, 24, 41, 24, Hex(0xf1d49b)); c.Line(17, 52, 42, 52, Hex(0xdfbb75));
            for (int x = 22; x < 40; x += 5) { c.Line(x, 25, x + 2, 27, Hex(0x64755e)); c.Set(x + 1, 26, Gold); }
            c.Polygon(new[] { 30, 32, 36, 43, 34, 48, 28, 49, 23, 45 }, Hex(0x277c91));
            c.Line(28, 36, 26, 43, Hex(0xc7e9df)); c.Set(28, 35, Pale); c.Set(32, 45, Mint);
            c.Line(21, 58, 36, 59, Hex(0x81b7bc)); c.Line(19, 61, 38, 61, Hex(0x254459));
        }
        static void EquipmentAmmunition(Canvas c)
        {
            c.Line(20, 7, 45, 7, Hex(0xdbd5af)); c.Line(12, 15, 35, 15, Hex(0x969d83));
            c.Line(11, 19, 11, 38, Hex(0xa2ad8a)); c.Line(36, 19, 36, 41, Hex(0x3b514c));
            EquipmentScrew(c, 12, 23, LightStone); EquipmentScrew(c, 34, 39, LightStone);
            c.Rect(19, 25, 11, 11, Hex(0xe0dcc1)); c.Ellipse(19, 25, 12, 8, Hex(0xf0e6c9));
            c.Ellipse(21, 28, 3, 3, Ink); c.Ellipse(26, 28, 3, 3, Ink); c.Rect(24, 31, 2, 2, Hex(0x5b5b5b));
            for (int x = 21; x < 30; x += 2) c.Line(x, 34, x, 36, Hex(0x6f6f68));
            c.Line(43, 22, 49, 17, Hex(0x6b775f)); c.Line(43, 25, 49, 20, Hex(0x222c37));
            c.Rect(16, 10, 4, 1, Hex(0x96784f)); c.Rect(27, 8, 5, 1, Pale); c.Set(18, 11, Pale);
        }
        static void EquipmentMagnifier(Canvas c)
        {
            EquipmentRing(c, 9, 5, 43, 43, Hex(0xedc777), Hex(0x79543a));
            c.Ellipse(17, 13, 27, 27, Hex(0x306978)); c.Polygon(new[] { 21, 17, 32, 14, 39, 17, 23, 31, 20, 29 }, Hex(0x8de3d2));
            c.Line(22, 16, 30, 14, Pale); c.Line(37, 21, 24, 34, Hex(0x4e9298));
            for (int i = 0; i < 8; i++) { double a = i * Math.PI / 4; int x = 31 + (int)(Math.Cos(a) * 19), y = 26 + (int)(Math.Sin(a) * 19); c.Set(x, y, Pale); }
            c.Polygon(new[] { 24, 32, 28, 23, 36, 30, 34, 36, 27, 38 }, Gold); c.Set(28, 26, Pale);
            c.Line(48, 44, 56, 54, Hex(0xe8bb8e)); c.Line(46, 47, 54, 56, Hex(0x5c374d));
        }
        static void EquipmentPendant(Canvas c)
        {
            for (int x = 17; x < 47; x += 4) { c.Ellipse(x, 4, 3, 3, Hex(0xf5d78e)); c.Set(x + 1, 5, Hex(0x7f684c)); }
            c.Line(13, 17, 16, 24, Hex(0xffd68f)); c.Line(50, 18, 47, 25, Hex(0xb28e5d));
            c.Polygon(new[] { 32, 20, 45, 32, 39, 52, 32, 57, 21, 49, 17, 32 }, Hex(0xa06ac0));
            c.Polygon(new[] { 32, 24, 40, 34, 36, 46, 26, 46, 22, 34 }, Hex(0x67c8bb));
            c.Polygon(new[] { 29, 28, 35, 30, 36, 40, 30, 44, 27, 35 }, Hex(0xddf4d8));
            c.Line(31, 23, 28, 30, Pale); c.Line(38, 35, 35, 46, Hex(0x338b8f));
            c.Line(21, 33, 25, 46, Hex(0xe7acd1)); c.Line(37, 51, 32, 55, Gold); c.Set(30, 25, Pale);
        }
        static void EquipmentBoot(Canvas c)
        {
            c.Line(17, 10, 30, 10, Hex(0xd0e9d0)); c.Line(17, 12, 29, 12, Hex(0x4d9290));
            for (int y = 23; y < 36; y += 5) { c.Line(18, y, 27, y + 2, Gold); c.Line(18, y + 2, 27, y, Hex(0xead3a0)); c.Set(17, y, Ink); c.Set(28, y + 1, Ink); }
            c.Line(18, 18, 17, 37, Hex(0x258b8d)); c.Line(30, 18, 29, 35, Hex(0x1f4b61));
            c.Line(17, 43, 30, 45, Hex(0x8ad7c7)); c.Line(32, 46, 44, 48, Hex(0x66abac));
            c.Line(15, 52, 49, 52, Pale); c.Line(14, 56, 48, 56, Hex(0x465b69));
            for (int x = 16; x < 48; x += 4) c.Rect(x, 55, 2, 2, Hex(0x263c4b));
            c.Set(45, 9, Pale); c.Line(39, 21, 42, 19, Hex(0xffe7aa));
        }
        static void EquipmentGrave(Canvas c)
        {
            int w = c.Width, h = c.Height;
            c.Line(14, 15, 13, h - 21, Hex(0xc0c7bf)); c.Line(w - 14, 18, w - 13, h - 19, Hex(0x3c475a));
            c.Line(17, h - 22, w - 17, h - 22, Hex(0x4b5667));
            for (int x = 20; x < w - 19; x += 4) { c.Rect(x, h - 29, 2, 1, Hex(0xb2b7ac)); c.Rect(x + 1, h - 27, 1, 1, Hex(0x3d4b60)); }
            c.Line(w - 16, h - 29, w - 22, h - 21, Hex(0x333f51)); c.Line(w - 22, h - 21, w - 18, h - 16, Hex(0x333f51));
            c.Line(10, h - 12, w - 9, h - 12, Hex(0x99aaa4)); c.Set(13, h - 8, Hex(0x889c69)); c.Set(16, h - 10, Hex(0xb4bf87));
        }
        static void EquipmentFence(Canvas c)
        {
            for (int x = 10; x < c.Width - 5; x += 18)
            {
                c.Line(x + 1, 23, x + 1, 56, Hex(0xb8c0bd)); c.Line(x + 4, 23, x + 4, 58, Hex(0x404556));
                EquipmentScrew(c, x + 2, 35, Hex(0xb5aa8c)); EquipmentScrew(c, x + 2, 56, Hex(0x887360));
                c.Set(x + 1, 12, Pale); c.Line(x, 29, x + 3, 31, Hex(0x8d5e49));
            }
            c.Line(7, 34, 88, 34, Hex(0x9894a0)); c.Line(7, 57, 88, 57, Hex(0x676775));
            c.Line(46, 27, 47, 29, Hex(0x8e817a)); c.Line(50, 34, 50, 37, Hex(0x8e817a));
        }
        static void EquipmentOssuary(Canvas c)
        {
            c.Line(27, 32, 27, 53, Hex(0xf2e6cd)); c.Line(35, 38, 35, 52, Hex(0x9c8277));
            c.Line(29, 36, 30, 45, Hex(0xb4a08e)); c.Line(30, 45, 28, 48, Hex(0xb4a08e));
            c.Ellipse(22, 42, 4, 6, Hex(0xbdaa94)); c.Ellipse(37, 37, 4, 7, Hex(0xd5c2a4));
            for (int x = 9; x < 49; x += 16)
            {
                int y = x == 25 ? 54 : 56;
                c.Line(x + 3, y - 3, x + 6, y - 4, Pale); c.Rect(x + 4, y, 2, 3, Ink); c.Rect(x + 11, y, 2, 3, Ink);
                for (int t = 4; t < 12; t += 3) c.Line(x + t, y + 8, x + t, y + 10, Hex(0x87796e));
            }
            c.Line(13, 68, 50, 68, Hex(0xc9b7aa)); c.Line(13, 72, 50, 72, Hex(0x65566e));
        }
        static void EquipmentMerchant(Canvas c)
        {
            c.Line(17, 9, 22, 6, Hex(0xc4c9d2)); c.Line(19, 13, 18, 23, Hex(0xa8afbe)); c.Line(39, 13, 39, 25, Hex(0x56627c));
            c.Rect(22, 19, 3, 3, Hex(0x3c2f36)); c.Rect(33, 19, 3, 3, Hex(0x3c2f36));
            c.Set(23, 20, Gold); c.Set(34, 20, Gold); c.Set(23, 19, Pale); c.Set(34, 19, Pale);
            c.Line(25, 28, 31, 28, Hex(0x705c59)); c.Set(27, 25, Hex(0xbda784)); c.Line(20, 25, 22, 27, Hex(0xd8c9ae));
            c.Line(18, 35, 23, 45, Hex(0x93a1b2)); c.Line(38, 36, 42, 47, Hex(0x35435e));
            EquipmentScrew(c, 27, 39, Gold); c.Line(8, 53, 48, 53, Hex(0xf5bd87));
            for (int x = 9; x < 49; x += 8) { c.Line(x, 57, x + 5, 59, Hex(0x713b57)); c.Set(x, 57, Hex(0xb66f7c)); }
            c.Rect(26, 58, 4, 3, Ink); c.Set(27, 58, Gold);
        }
        static void EquipmentDecoration(Canvas c, string id)
        {
            if (id == "decor-theme0")
            {
                c.Line(47, 18, 49, 38, Hex(0xc29a6a)); c.Line(49, 38, 47, 61, Hex(0x473d3d));
                c.Line(43, 45, 46, 53, Hex(0x967452)); c.Line(46, 53, 43, 65, Hex(0x967452));
                c.Ellipse(8, 62, 13, 12, Hex(0xdd762c)); c.Line(12, 64, 12, 72, Gold); c.Set(10, 67, Ink); c.Set(18, 67, Ink);
                c.Line(43, 75, 56, 75, Hex(0xd7b88a)); c.Line(43, 79, 54, 79, Hex(0x75533c));
                c.Line(19, 28, 22, 23, Hex(0xb19067)); c.Line(65, 27, 70, 21, Hex(0x8e7359));
            }
            else if (id == "decor-theme1")
            {
                c.Line(29, 8, 67, 8, Hex(0xd9ded6)); c.Line(18, 31, 18, 76, Hex(0xd3d9d7));
                c.Line(77, 31, 78, 77, Hex(0x46536c)); c.Line(24, 39, 27, 47, Hex(0x717c96));
                c.Line(27, 47, 24, 55, Hex(0x717c96)); c.Line(68, 56, 72, 63, Hex(0x5d6886));
                c.Line(30, 33, 63, 33, Hex(0xc5cbd4)); c.Line(38, 45, 55, 45, Hex(0xa5afc3));
                c.Set(46, 58, Pale); c.Set(51, 58, Pale); c.Line(17, 86, 81, 86, Hex(0xaaaebb));
            }
            else if (id == "decor-theme2")
            {
                c.Line(24, 48, 71, 48, Hex(0xe1b991)); c.Line(23, 65, 23, 80, Hex(0xbd8a70));
                c.Line(74, 65, 74, 80, Hex(0x4b3242)); EquipmentScrew(c, 48, 68, Gold);
                for (int x = 27; x < 71; x += 10) { c.Line(x, 70, x + 4, 76, Hex(0x67444d)); c.Set(x, 69, Hex(0xb47760)); }
                c.Line(44, 16, 44, 49, Hex(0xffefbd)); c.Line(33, 27, 58, 27, Hex(0xffefbd));
                c.Line(17, 43, 17, 51, Hex(0xd2b994)); c.Line(77, 43, 77, 51, Hex(0xc4ae8e));
            }
            else if (id == "decor-theme3")
            {
                for (int i = 0; i < 4; i++)
                {
                    int x = 14 + i * 16, y = 32 + i % 2 * 16;
                    c.Line(x + 7, y + 7, x + 5, y + 18, Hex(0xb8c197)); c.Line(x + 5, y + 18, x + 10, y + 28, Hex(0x60754d));
                    c.Ellipse(x + 10, y + 12, 4, 6, Hex(0x607a4e)); c.Set(x + 11, y + 12, Hex(0xd3d6ac));
                    c.Set(x + 8, y + 26, Hex(0xc9d6a4)); c.Line(x + 17, y + 21, x + 14, y + 28, Hex(0x334f41));
                }
                c.Line(16, 81, 33, 83, Hex(0x98ac78));
            }
            else if (id == "decor-theme4")
            {
                c.Line(13, 10, 81, 10, Hex(0xcbb2aa)); c.Line(17, 25, 17, 73, Hex(0x987788));
                c.Line(79, 25, 79, 74, Hex(0x3d3049)); c.Line(29, 69, 66, 69, Hex(0xd6ad85));
                c.Line(40, 44, 42, 59, Hex(0xb28b7a)); c.Line(60, 45, 60, 61, Hex(0x624d5c));
                c.Line(42, 62, 56, 62, Hex(0x7b5968)); c.Line(48, 18, 48, 31, Hex(0xc5b9af));
                EquipmentScrew(c, 49, 33, Gold); c.Line(22, 80, 73, 80, Hex(0xa18b98));
            }
        }
    }
}
