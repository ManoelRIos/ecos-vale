using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Ecos.Haunt
{
    /// <summary>Original 5x7 bitmap lettering. No dynamic/system font is used.</summary>
    public sealed class HauntPixelText : IDisposable
    {
        const int GlyphWidth = 5, GlyphHeight = 7, Advance = 6, LineHeight = 9;
        readonly Dictionary<char, Texture2D> textures = new Dictionary<char, Texture2D>();
        bool disposed;

        // Slash-separated rows, top to bottom; 1 is an opaque white pixel.
        static readonly Dictionary<char, string> Glyphs = new Dictionary<char, string>
        {
            { 'A', "01110/10001/10001/11111/10001/10001/10001" },
            { 'B', "11110/10001/10001/11110/10001/10001/11110" },
            { 'C', "01111/10000/10000/10000/10000/10000/01111" },
            { 'D', "11110/10001/10001/10001/10001/10001/11110" },
            { 'E', "11111/10000/10000/11110/10000/10000/11111" },
            { 'F', "11111/10000/10000/11110/10000/10000/10000" },
            { 'G', "01111/10000/10000/10111/10001/10001/01111" },
            { 'H', "10001/10001/10001/11111/10001/10001/10001" },
            { 'I', "11111/00100/00100/00100/00100/00100/11111" },
            { 'J', "00111/00010/00010/00010/10010/10010/01100" },
            { 'K', "10001/10010/10100/11000/10100/10010/10001" },
            { 'L', "10000/10000/10000/10000/10000/10000/11111" },
            { 'M', "10001/11011/10101/10101/10001/10001/10001" },
            { 'N', "10001/11001/10101/10011/10001/10001/10001" },
            { 'O', "01110/10001/10001/10001/10001/10001/01110" },
            { 'P', "11110/10001/10001/11110/10000/10000/10000" },
            { 'Q', "01110/10001/10001/10001/10101/10010/01101" },
            { 'R', "11110/10001/10001/11110/10100/10010/10001" },
            { 'S', "01111/10000/10000/01110/00001/00001/11110" },
            { 'T', "11111/00100/00100/00100/00100/00100/00100" },
            { 'U', "10001/10001/10001/10001/10001/10001/01110" },
            { 'V', "10001/10001/10001/10001/10001/01010/00100" },
            { 'W', "10001/10001/10001/10101/10101/10101/01010" },
            { 'X', "10001/10001/01010/00100/01010/10001/10001" },
            { 'Y', "10001/10001/01010/00100/00100/00100/00100" },
            { 'Z', "11111/00001/00010/00100/01000/10000/11111" },
            { '0', "01110/10001/10011/10101/11001/10001/01110" },
            { '1', "00100/01100/00100/00100/00100/00100/01110" },
            { '2', "01110/10001/00001/00010/00100/01000/11111" },
            { '3', "11110/00001/00001/01110/00001/00001/11110" },
            { '4', "00010/00110/01010/10010/11111/00010/00010" },
            { '5', "11111/10000/10000/11110/00001/00001/11110" },
            { '6', "01110/10000/10000/11110/10001/10001/01110" },
            { '7', "11111/00001/00010/00100/01000/01000/01000" },
            { '8', "01110/10001/10001/01110/10001/10001/01110" },
            { '9', "01110/10001/10001/01111/00001/00001/01110" },
            { '?', "01110/10001/00001/00010/00100/00000/00100" },
            { '!', "00100/00100/00100/00100/00100/00000/00100" },
            { '.', "00000/00000/00000/00000/00000/00110/00110" },
            { ',', "00000/00000/00000/00000/00110/00100/01000" },
            { ':', "00000/00110/00110/00000/00110/00110/00000" },
            { ';', "00000/00110/00110/00000/00110/00100/01000" },
            { '-', "00000/00000/00000/11111/00000/00000/00000" },
            { '_', "00000/00000/00000/00000/00000/00000/11111" },
            { '+', "00000/00100/00100/11111/00100/00100/00000" },
            { '=', "00000/00000/11111/00000/11111/00000/00000" },
            { '/', "00001/00001/00010/00100/01000/10000/10000" },
            { '\\', "10000/10000/01000/00100/00010/00001/00001" },
            { '(', "00010/00100/01000/01000/01000/00100/00010" },
            { ')', "01000/00100/00010/00010/00010/00100/01000" },
            { '[', "01110/01000/01000/01000/01000/01000/01110" },
            { ']', "01110/00010/00010/00010/00010/00010/01110" },
            { '<', "00010/00100/01000/10000/01000/00100/00010" },
            { '>', "01000/00100/00010/00001/00010/00100/01000" },
            { '^', "00100/01010/10001/00000/00000/00000/00000" },
            { '%', "11001/11010/00100/01000/10110/00110/00000" },
            { '*', "00000/10101/01110/11111/01110/10101/00000" },
            { '|', "00100/00100/00100/00100/00100/00100/00100" },
            { '\'', "00100/00100/01000/00000/00000/00000/00000" },
            { '"', "01010/01010/10100/00000/00000/00000/00000" },
            { '#', "01010/01010/11111/01010/11111/01010/01010" },
            { '$', "00100/01111/10100/01110/00101/11110/00100" },
            { '&', "01100/10010/10100/01000/10101/10010/01101" },
            { '@', "01110/10001/10111/10101/10111/10000/01110" },
            { '~', "00000/00000/01001/10110/00000/00000/00000" }
        };

        /// <summary>Draws clipped, integer-scaled glyphs in the current IMGUI coordinate space.</summary>
        public void Draw(Rect rect, string text, int pixelSize, Color color,
            TextAnchor alignment = TextAnchor.UpperLeft, bool wrap = true)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelText));
            if (string.IsNullOrEmpty(text) || rect.width <= 0 || rect.height <= 0) return;
            pixelSize = Mathf.Clamp(pixelSize, 1, 8);
            int columns = Math.Max(1, Mathf.FloorToInt((rect.width + pixelSize) / (Advance * pixelSize)));
            List<string> lines = Layout(Normalize(text), columns, wrap);
            float blockHeight = (lines.Count * LineHeight - (LineHeight - GlyphHeight)) * pixelSize;
            int horizontal = (int)alignment % 3, vertical = (int)alignment / 3;
            float top = vertical == 1 ? (rect.height - blockHeight) * .5f : vertical == 2 ? rect.height - blockHeight : 0;
            Color previousColor = GUI.color;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.BeginGroup(rect);
            try
            {
                GUI.color = color;
                for (int row = 0; row < lines.Count; row++)
                {
                    string line = lines[row];
                    float width = Math.Max(0, line.Length * Advance - 1) * pixelSize;
                    float left = horizontal == 1 ? (rect.width - width) * .5f : horizontal == 2 ? rect.width - width : 0;
                    float y = Mathf.Round(top + row * LineHeight * pixelSize);
                    for (int column = 0; column < line.Length; column++)
                    {
                        char glyph = line[column];
                        if (glyph == ' ') continue;
                        GUI.DrawTexture(new Rect(Mathf.Round(left + column * Advance * pixelSize), y,
                            GlyphWidth * pixelSize, GlyphHeight * pixelSize), Texture(glyph), ScaleMode.StretchToFill, true);
                    }
                }
            }
            finally
            {
                GUI.EndGroup();
                GUI.matrix = previousMatrix;
                GUI.color = previousColor;
            }
        }

        static string Normalize(string text)
        {
            var normalized = new StringBuilder(text.Length);
            foreach (char character in text.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
                char c = char.ToUpperInvariant(character);
                switch (c)
                {
                    case '\r': continue;
                    case '\t': normalized.Append("    "); continue;
                    case '→': c = '>'; break;
                    case '←': c = '<'; break;
                    case '↑': c = '^'; break;
                    case '↓': c = 'V'; break;
                    case '×': c = 'X'; break;
                    case '•': case '·': c = '*'; break;
                    case '–': case '—': c = '-'; break;
                    case '‘': case '’': c = '\''; break;
                    case '“': case '”': c = '"'; break;
                    case '…': normalized.Append("..."); continue;
                    case '\u00a0': c = ' '; break;
                }
                normalized.Append(c == ' ' || c == '\n' || Glyphs.ContainsKey(c) ? c : '?');
            }
            return normalized.ToString();
        }

        static List<string> Layout(string text, int columns, bool wrap)
        {
            var lines = new List<string>();
            foreach (string paragraph in text.Split('\n'))
            {
                if (!wrap || paragraph.Length <= columns) { lines.Add(paragraph); continue; }
                int start = 0;
                while (start < paragraph.Length)
                {
                    int length = Math.Min(columns, paragraph.Length - start);
                    if (start + length < paragraph.Length)
                    {
                        int space = paragraph.LastIndexOf(' ', start + length, length + 1);
                        if (space > start) length = space - start;
                    }
                    lines.Add(paragraph.Substring(start, length));
                    start += length;
                    while (start < paragraph.Length && paragraph[start] == ' ') start++;
                }
            }
            return lines;
        }

        Texture2D Texture(char glyph)
        {
            if (!Glyphs.ContainsKey(glyph)) glyph = '?';
            if (textures.TryGetValue(glyph, out Texture2D cached)) return cached;
            string[] rows = Glyphs[glyph].Split('/');
            var pixels = new Color32[GlyphWidth * GlyphHeight];
            for (int y = 0; y < GlyphHeight; y++)
                for (int x = 0; x < GlyphWidth; x++)
                    if (rows[y][x] == '1') pixels[(GlyphHeight - y - 1) * GlyphWidth + x] = new Color32(255, 255, 255, 255);
            var texture = new Texture2D(GlyphWidth, GlyphHeight, TextureFormat.RGBA32, false)
            {
                name = "Haunt / bitmap letter " + glyph,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            textures.Add(glyph, texture);
            return texture;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (Texture2D texture in textures.Values)
                if (texture)
                {
                    if (Application.isPlaying) UnityEngine.Object.Destroy(texture);
                    else UnityEngine.Object.DestroyImmediate(texture);
                }
            textures.Clear();
        }
    }
}
