using System.Collections.Generic;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        static readonly Color DungeonAmbientTint = new Color(.64f, .61f, .84f, 1);
        readonly List<HauntFlickerLight> flickerLights = new List<HauntFlickerLight>();
        Texture2D radialLightTexture, vignetteTexture;
        Sprite radialLightSprite;

        Sprite RadialLightSprite
        {
            get
            {
                if (radialLightSprite) return radialLightSprite;
                const int size = 64;
                radialLightTexture = new Texture2D(size, size) { name = "Haunt / smooth radial light", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float dx = (x - 31.5f) / 32, dy = (y - 31.5f) / 32;
                    float falloff = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(falloff * falloff * 185));
                }
                radialLightTexture.SetPixels32(pixels); radialLightTexture.Apply(false, true);
                radialLightSprite = Sprite.Create(radialLightTexture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
                return radialLightSprite;
            }
        }

        HauntFlickerLight AttachFlickerLight(Transform owner, Color color, float radius, float intensity,
            float flicker = .12f, int order = 1100)
        {
            return CreateRoomLight(owner, owner ? (Vector2)owner.position : Vector2.zero, color, radius, intensity, flicker, order);
        }

        HauntFlickerLight CreateRoomLight(Transform owner, Vector2 position, Color color, float radius,
            float intensity, float flicker, int order)
        {
            if (!roomRoot || flickerLights.Count >= 96) return null;
            var glow = MakeSprite("glow", position, roomRoot.transform, order);
            glow.sprite = RadialLightSprite; glow.sharedMaterial = GlowMaterial;
            var light = glow.gameObject.AddComponent<HauntFlickerLight>();
            light.Configure(glow, owner, position, color, radius, intensity, flicker, flickerLights.Count * 7.13f + 5);
            flickerLights.Add(light); light.Tick(simulationTime);
            return light;
        }

        void TickRoomLighting(float dt)
        {
            for (int i = flickerLights.Count - 1; i >= 0; i--)
            {
                var light = flickerLights[i];
                if (!light || !light.SourceAlive)
                { if (light) Destroy(light.gameObject); flickerLights.RemoveAt(i); continue; }
                light.Tick(simulationTime);
            }
        }

        void ClearRoomLighting()
        {
            foreach (var light in flickerLights) if (light) Destroy(light.gameObject);
            flickerLights.Clear();
        }

        void AttachEnemyLighting(Enemy enemy)
        {
            if (enemy.Kind == 0 || enemy.Kind == 12 || enemy.Kind == 15 || enemy.Kind == 18 || enemy.Kind == 4 && BossVariant == 0)
                AttachFlickerLight(enemy.Sprite.transform, new Color(1, .46f, .14f), enemy.Kind == 4 ? 4 : 2.7f, .22f);
        }

        void TintDungeonRoom()
        {
            if (InShop || State == null) return;
            foreach (var sprite in roomRoot.GetComponentsInChildren<SpriteRenderer>())
                if (sprite.sharedMaterial == art.Material) sprite.color *= DungeonAmbientTint;
        }

        void DrawDungeonVignette()
        {
            if (!Playing || InShop) return;
            if (!vignetteTexture)
            {
                const int size = 128;
                vignetteTexture = new Texture2D(size, size) { name = "Haunt / night vignette", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float dx = (x - 63.5f) / 64, dy = (y - 63.5f) / 64;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.SmoothStep(0, .52f, Mathf.Clamp01((distance - .4f) / 1.0f));
                    pixels[y * size + x] = new Color32(8, 3, 24, (byte)(alpha * 255));
                }
                vignetteTexture.SetPixels32(pixels); vignetteTexture.Apply(false, true);
            }
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            GUI.matrix = Matrix4x4.identity; GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture);
            GUI.matrix = matrix; GUI.color = color;
        }

        void DisposeLighting()
        {
            ClearRoomLighting();
            if (radialLightSprite) Destroy(radialLightSprite);
            if (radialLightTexture) Destroy(radialLightTexture);
            if (vignetteTexture) Destroy(vignetteTexture);
        }
    }
}
