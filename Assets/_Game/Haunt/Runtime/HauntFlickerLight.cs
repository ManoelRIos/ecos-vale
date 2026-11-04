using UnityEngine;

namespace Ecos.Haunt
{
    /// <summary>A room-owned additive light. The game ticks it so pausing freezes flicker.</summary>
    public sealed class HauntFlickerLight : MonoBehaviour
    {
        SpriteRenderer sprite;
        Transform source;
        Vector2 anchor;
        Color tint;
        float radius, intensity, variation, phase;
        bool followsSource;
        public void Configure(SpriteRenderer renderer, Transform owner, Vector2 position, Color color,
            float size, float opacity, float flicker, float seed)
        {
            sprite = renderer; source = owner; followsSource = owner != null; anchor = position;
            tint = color; radius = Mathf.Max(.1f, size); intensity = Mathf.Clamp01(opacity);
            variation = Mathf.Clamp(flicker, 0, .4f); phase = seed;
        }
        public bool SourceAlive => !followsSource || source;
        public void Tick(float time)
        {
            if (!sprite || !SourceAlive) return;
            float brightness = 1 + (Mathf.PerlinNoise(phase, time * 2.1f) * 2 - 1) * variation;
            float size = radius * (1 + (Mathf.PerlinNoise(phase + 37, time * 1.3f) * 2 - 1) * variation * .35f);
            sprite.transform.position = followsSource ? source.position : (Vector3)anchor;
            sprite.transform.localScale = Vector3.one * size;
            sprite.color = new Color(tint.r, tint.g, tint.b, intensity * brightness);
        }
    }
}
