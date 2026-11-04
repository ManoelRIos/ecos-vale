// Export-only API surface: this does not simulate Unity rendering or import.
// Compiled only by SpriteSheetExport.csproj, never by the Unity project.
using System;

namespace UnityEngine
{
    public class Object
    {
        public static implicit operator bool(Object value) => value is not null;
        public static void Destroy(Object value) { }
        public static void DestroyImmediate(Object value) { }
    }
    public class Material : Object
    {
        public string name;
        public Material(Material source) { }
    }
    public static class Resources { public static T Load<T>(string path) where T : class => null; }
    public static class Application { public static bool isPlaying; }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point }
    public enum TextureWrapMode { Clamp }
    public enum SpriteMeshType { FullRect }
    public class Texture2D : Object
    {
        public string name;
        public int width, height, anisoLevel;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        Color32[] pixels;
        public Texture2D(int width, int height, TextureFormat format, bool mipmap)
        { this.width = width; this.height = height; pixels = new Color32[width * height]; }
        public void SetPixels32(Color32[] value)
        {
            if (value.Length != width * height) throw new ArgumentException("Pixel buffer dimensions disagree");
            pixels = (Color32[])value.Clone();
        }
        public Color32[] GetPixels32() => (Color32[])pixels.Clone();
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
    }
    public class Sprite : Object
    {
        public string name;
        public Texture2D texture;
        public Rect rect;
        public Vector2 pivot;
        public float pixelsPerUnit;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit,
            uint extrude, SpriteMeshType meshType)
            => new Sprite { texture = texture, rect = rect,
                pivot = new Vector2(pivot.x * rect.width, pivot.y * rect.height), pixelsPerUnit = pixelsPerUnit };
    }
}
