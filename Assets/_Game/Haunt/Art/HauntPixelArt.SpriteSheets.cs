using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntPixelArt
    {
        readonly bool useReferenceSheets;
        readonly Dictionary<string, Texture2D> referenceTextures = new Dictionary<string, Texture2D>();

        /// <param name="useReferenceSheets">False is reserved for baking the authored
        /// C# pixels into readable textures; ordinary play uses the shipped sheets.</param>
        public HauntPixelArt(bool useReferenceSheets = true)
        {
            this.useReferenceSheets = useReferenceSheets;
        }

        static int ReferenceBossIndex(string id) => id == "boss" ? 0 : id == "widow" ? 1
            : id == "bishop" ? 2 : id == "matriarch" ? 3 : id == "bell" ? 4
            : id == "iron-executioner" ? 5 : id == "bog-oracle" ? 6 : -1;

        Sprite TryLoadSheetSprite(string key, string sheet, int column, int row, int cellSize, float pivotY)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (!useReferenceSheets) return null;
            if (!referenceTextures.TryGetValue(sheet, out Texture2D texture))
            {
                texture = Resources.Load<Texture2D>("ReferenceSheets/" + sheet);
                referenceTextures.Add(sheet, texture);
            }
            int rows = cellSize == 24 ? 27 : cellSize == 48 ? 5 : 4;
            if (!texture || texture.width != cellSize * 4 || texture.height != cellSize * rows
                || column < 0 || column >= 4 || row < 0 || row >= rows) return null;
            var sprite = Sprite.Create(texture,
                new Rect(column * cellSize, texture.height - (row + 1) * cellSize, cellSize, cellSize),
                new Vector2(.5f, pivotY), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "Haunt / " + key;
            // Resource textures belong to Unity. Only the sliced Sprite belongs to
            // this library and is released by the normal sprite-cache teardown.
            sprites.Add(key, sprite);
            return sprite;
        }
    }
}
