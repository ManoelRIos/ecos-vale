using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ecos
{
    /// <summary>Local visual aid; rewards and interactions still pass through authority.</summary>
    public sealed class ExplorationScanner : MonoBehaviour
    {
        const float Range = 42, Duration = 5, Cooldown = 14;
        readonly List<LineRenderer> beams = new List<LineRenderer>();
        readonly List<Interactable> sources = new List<Interactable>();
        readonly List<Interactable> candidates = new List<Interactable>();
        SaveData state; Landmarks sites; Transform player; Material material;
        LineRenderer ring; float activeUntil, readyAt;
        public float CooldownRemaining => Mathf.Max(0, readyAt - Time.time);
        public float DurationRemaining => Mathf.Max(0, activeUntil - Time.time);

        public void Initialize(SaveData s, Landmarks landmarks, Transform actor, Palette palette)
        {
            state = s; sites = landmarks; player = actor;
            material = palette.Get(new Color(.12f, .57f, .82f), true);
            ring = CreateLine("Pulso da lente", 64, .07f); ring.loop = true; ring.gameObject.SetActive(false);
        }

        LineRenderer CreateLine(string title, int points, float width)
        {
            var g = new GameObject(title); g.transform.SetParent(transform);
            var line = g.AddComponent<LineRenderer>(); line.positionCount = points;
            line.useWorldSpace = true; line.widthMultiplier = width; line.generateLightingData = true;
            line.sharedMaterial = material; line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            return line;
        }

        bool Pending(Interactable target)
        {
            if (!target || !target.gameObject.activeSelf) return false;
            switch (target.EffectiveKind)
            {
                case InteractionKind.Pickup: return !state.collected.Contains(target.id);
                case InteractionKind.Shrine: return !state.restored.Contains(target.id);
                case InteractionKind.Camp: return !state.campBuilt;
                case InteractionKind.Workbench: return !state.lensCrafted;
                case InteractionKind.Chest: return !state.openedCaches.Contains(target.id);
                default: return !state.readClues.Contains(target.id);
            }
        }

        public bool Pulse(out string message)
        {
            if (!state.lensCrafted) { message = "Prepare a lente na bancada do acampamento para revelar ecos."; return false; }
            if (CooldownRemaining > 0) { message = "A lente recarrega em " + Mathf.CeilToInt(CooldownRemaining) + " s."; return false; }
            readyAt = Time.time + Cooldown; activeUntil = Time.time + Duration;
            candidates.Clear(); sources.Clear();
            foreach (var target in sites.registry.Values)
                if (Pending(target) && (target.transform.position - player.position).sqrMagnitude <= Range * Range) candidates.Add(target);
            candidates.Sort((a, b) => (a.transform.position - player.position).sqrMagnitude.CompareTo((b.transform.position - player.position).sqrMagnitude));
            for (int i = 0; i < beams.Count; i++) beams[i].gameObject.SetActive(false);
            for (int i = 0; i < Mathf.Min(18, candidates.Count); i++)
            {
                if (i == beams.Count) beams.Add(CreateLine("Eco revelado", 2, .055f));
                sources.Add(candidates[i]); beams[i].gameObject.SetActive(true);
            }
            for (int i = 0; i < 64; i++)
            {
                float a = i * Mathf.PI * 2 / 64, x = player.position.x + Mathf.Cos(a) * Range, z = player.position.z + Mathf.Sin(a) * Range;
                ring.SetPosition(i, new Vector3(x, WorldMath.Height(x, z, state.seed) + .2f, z));
            }
            ring.gameObject.SetActive(true);
            message = sources.Count + " ecos próximos revelados por 5 segundos.";
            return true;
        }

        void Update()
        {
            bool active = DurationRemaining > 0;
            if (ring) ring.gameObject.SetActive(active);
            for (int i = 0; i < sources.Count; i++)
            {
                bool visible = active && Pending(sources[i]); beams[i].gameObject.SetActive(visible);
                if (!visible) continue;
                Vector3 start = sources[i].transform.position + Vector3.up * .6f;
                beams[i].SetPosition(0, start); beams[i].SetPosition(1, start + Vector3.up * 3.2f);
            }
        }
    }
}
