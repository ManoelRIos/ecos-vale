using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ecos.Haunt.Fps
{
    /// <summary>The picture itself: post-processing (bloom, tone mapping, grading, vignette),
    /// fog that follows the camera through the 2D/first-person switch, and the small things
    /// that float in the air — embers rising from every flame and dust drifting in the light.
    /// The first-person view gets the full treatment; the overhead pixel view a lighter one.</summary>
    public sealed class HauntFpsLook : MonoBehaviour, IDisposable
    {
        /// <summary>Set false to render without post-processing.</summary>
        public static bool PostEffects = true;
        /// <summary>Set false to remove embers and dust.</summary>
        public static bool Motes = true;
        public const float FirstPersonFog = .024f, OverheadFog = .014f;
        const int EmberCount = 56, DustCount = 44;
        const float EmberReach = 17, DustReach = 6.5f, OverheadViewDistance = 26;

        struct Mote
        {
            public Vector3 Position, Velocity;
            public float Age, Life, Size, Phase;
        }

        readonly Mote[] embers = new Mote[EmberCount], dust = new Mote[DustCount];
        readonly List<Vector3> nearFlames = new List<Vector3>();
        Volume eyeVolume, overheadVolume;
        VolumeProfile eyeProfile, overheadProfile;
        Mesh emberMesh, dustMesh;
        Material emberMaterial, dustMaterial;
        MeshRenderer emberRenderer, dustRenderer;
        Vector3[] emberVertices, dustVertices;
        HauntFpsWorld styledWorld;
        uint seed = 0x9E3779B9u;
        float clock, emberDebt;
        bool initialized, disposed;

        /// <summary>True when the renderer can run post-processing and it is switched on.</summary>
        public bool PostEffectsActive { get; private set; }
        public int LiveEmbers { get; private set; }

        public void Initialize()
        {
            if (initialized || disposed) return;
            initialized = true;
            PostEffectsActive = PostEffects && RendererSupportsPostProcessing();
            if (PostEffectsActive)
            {
                eyeProfile = BuildProfile(false); overheadProfile = BuildProfile(true);
                eyeVolume = AddVolume("Imagem / primeira pessoa", eyeProfile, 10);
                overheadVolume = AddVolume("Imagem / vista 2D", overheadProfile, 11);
                overheadVolume.weight = 0;
            }
            emberMesh = MoteMesh("Brasas", EmberCount, out emberVertices);
            dustMesh = MoteMesh("Poeira", DustCount, out dustVertices);
            emberRenderer = MoteRenderer("Brasas", emberMesh, out emberMaterial);
            dustRenderer = MoteRenderer("Poeira", dustMesh, out dustMaterial);
            for (int i = 0; i < EmberCount; i++) embers[i].Life = 0;
            for (int i = 0; i < DustCount; i++) dust[i].Life = 0;
        }

        /// <summary>URP only runs post-processing when the renderer asset carries its
        /// post-process resources; the editor setup assigns them.</summary>
        static bool RendererSupportsPostProcessing()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!pipeline) return false;
            var renderers = pipeline.rendererDataList;
            for (int i = 0; i < renderers.Length; i++)
            {
                var data = renderers[i] as UniversalRendererData;
                if (data && data.postProcessData) return true;
            }
            return false;
        }

        Volume AddVolume(string label, VolumeProfile profile, float priority)
        {
            var node = new GameObject(label); node.transform.SetParent(transform, false); node.layer = 0;
            var volume = node.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = priority; volume.sharedProfile = profile; volume.weight = 1;
            return volume;
        }

        static VolumeProfile BuildProfile(bool overhead)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = overhead ? "Imagem 2D" : "Imagem primeira pessoa";
            // Fire, bullets and spectral glows bleed light; the pixel view keeps it tighter.
            var bloom = profile.Add<Bloom>();
            bloom.threshold.Override(overhead ? 1.05f : .92f);
            bloom.intensity.Override(overhead ? .4f : .85f);
            bloom.scatter.Override(overhead ? .5f : .68f);
            bloom.tint.Override(new Color(1, .93f, .84f));
            bloom.highQualityFiltering.Override(!overhead);
            // Bright torchlight rolls off instead of clipping to flat white.
            profile.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            var grade = profile.Add<ColorAdjustments>();
            grade.postExposure.Override(overhead ? .12f : .2f);
            grade.contrast.Override(overhead ? 6 : 14);
            grade.saturation.Override(overhead ? 6 : 12);
            // Cool shadows against warm firelight.
            var toning = profile.Add<SplitToning>();
            toning.shadows.Override(new Color(.42f, .45f, .62f));
            toning.highlights.Override(new Color(.62f, .55f, .44f));
            toning.balance.Override(overhead ? 0 : -12);
            var vignette = profile.Add<Vignette>();
            vignette.color.Override(new Color(.04f, .02f, .07f));
            vignette.intensity.Override(overhead ? .2f : .36f);
            vignette.smoothness.Override(.5f);
            return profile;
        }

        // ------------------------------------------------------------------ per frame

        /// <param name="overheadAmount">0 = eye level, 1 = overhead, along the camera path.</param>
        /// <param name="viewDistance">Distance from the camera to what it frames (0 at eye level).</param>
        public void Tick(float dt, HauntFpsWorld world, Camera view, Camera overlay, float overheadAmount, float viewDistance, bool overheadArt)
        {
            if (!initialized || disposed || !view) return;
            overheadAmount = Mathf.Clamp01(overheadAmount);
            clock += Mathf.Clamp(dt, 0, .1f);
            if (PostEffectsActive)
            {
                // Later volumes blend over earlier ones, so the overhead look fades in by weight.
                if (overheadVolume) overheadVolume.weight = overheadAmount;
                // In a camera stack the effects must run once, on the last camera drawn.
                bool overlayDraws = overlay && overlay.isActiveAndEnabled;
                SetPostProcessing(view, !overlayDraws);
                if (overlay) SetPostProcessing(overlay, overlayDraws);
            }
            if (world)
            {
                world.SetFirstPersonDressing(overheadAmount <= .01f);
                // Enough to bloom while the flame keeps its orange instead of clipping to white.
                world.SetFlameGlow(PostEffectsActive ? 1.8f : 1);
                if (RenderSettings.fog)
                {
                    // The dolly path carries the camera far away; thin the fog with distance so
                    // the room never disappears into it and the last frame matches the 2D view.
                    float density = Mathf.Lerp(FirstPersonFog, OverheadFog, overheadAmount);
                    if (viewDistance > OverheadViewDistance) density *= OverheadViewDistance / viewDistance;
                    RenderSettings.fogDensity = density;
                }
                if (styledWorld != world) { styledWorld = world; Recolor(world); }
            }
            TickMotes(dt, world, view, overheadAmount, overheadArt);
        }

        static void SetPostProcessing(Camera camera, bool enabled)
        {
            var data = camera.GetUniversalAdditionalCameraData();
            if (data && data.renderPostProcessing != enabled) data.renderPostProcessing = enabled;
        }

        void Recolor(HauntFpsWorld world)
        {
            Color torch = world.TorchColor;
            float glow = PostEffectsActive ? 3.2f : 1.5f;
            if (emberMaterial) emberMaterial.SetColor("_EmissionColor", Color.Lerp(torch, new Color(1, .75f, .4f), .35f) * glow);
            if (dustMaterial) dustMaterial.SetColor("_EmissionColor", Color.Lerp(torch, Color.white, .5f) * .34f);
            for (int i = 0; i < EmberCount; i++) embers[i].Life = 0;
        }

        // ------------------------------------------------------------------ embers and dust

        float Random01()
        {
            seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
            return (seed & 0xffffff) / (float)0x1000000;
        }

        void TickMotes(float dt, HauntFpsWorld world, Camera view, float overheadAmount, bool overheadArt)
        {
            bool show = Motes && world;
            if (emberRenderer) emberRenderer.enabled = show;
            // Dust is finer than a pixel of the overhead view, where it would only shimmer.
            if (dustRenderer) dustRenderer.enabled = show && !overheadArt;
            if (!show) return;
            dt = Mathf.Clamp(dt, 0, .1f);
            Transform eye = view.transform;
            Vector3 focus = eye.position + eye.forward * Mathf.Lerp(0, OverheadViewDistance, overheadAmount);

            nearFlames.Clear();
            var flames = world.FlamePoints;
            for (int i = 0; i < flames.Count; i++)
                if ((flames[i] - focus).sqrMagnitude < EmberReach * EmberReach) nearFlames.Add(flames[i]);
            emberDebt = Mathf.Min(8, emberDebt + dt * Mathf.Min(26, nearFlames.Count * 3.2f));
            int live = 0;
            for (int i = 0; i < EmberCount; i++)
            {
                ref Mote ember = ref embers[i];
                if (ember.Age >= ember.Life)
                {
                    if (emberDebt < 1 || nearFlames.Count == 0) { ember.Life = 0; continue; }
                    emberDebt -= 1;
                    Vector3 source = nearFlames[Mathf.Min(nearFlames.Count - 1, (int)(Random01() * nearFlames.Count))];
                    ember.Position = source + new Vector3(Random01() - .5f, Random01() * .1f, Random01() - .5f) * .16f;
                    ember.Velocity = new Vector3((Random01() - .5f) * .35f, .55f + Random01() * .7f, (Random01() - .5f) * .35f);
                    ember.Age = 0; ember.Life = 1.1f + Random01() * 1.5f; ember.Size = .018f + Random01() * .02f; ember.Phase = Random01() * 20;
                }
                ember.Age += dt;
                // Hot air wanders as it rises and the ember slows down.
                ember.Velocity.x += (Mathf.PerlinNoise(ember.Phase, clock * 1.7f) - .5f) * dt * 1.6f;
                ember.Velocity.z += (Mathf.PerlinNoise(clock * 1.7f, ember.Phase) - .5f) * dt * 1.6f;
                ember.Velocity.y = Mathf.Max(.12f, ember.Velocity.y - dt * .25f);
                ember.Position += ember.Velocity * dt;
                live++;
            }
            LiveEmbers = live;

            for (int i = 0; i < DustCount; i++)
            {
                ref Mote mote = ref dust[i];
                Vector3 offset = mote.Position - eye.position;
                if (mote.Age >= mote.Life || Mathf.Abs(offset.x) > DustReach || Mathf.Abs(offset.z) > DustReach)
                {
                    mote.Position = new Vector3(eye.position.x + (Random01() * 2 - 1) * DustReach, .25f + Random01() * 3.1f,
                        eye.position.z + (Random01() * 2 - 1) * DustReach);
                    mote.Age = 0; mote.Life = 5 + Random01() * 6; mote.Size = .008f + Random01() * .009f; mote.Phase = Random01() * 40;
                }
                mote.Age += dt;
                mote.Velocity = new Vector3(Mathf.PerlinNoise(mote.Phase, clock * .21f) - .5f, (Mathf.PerlinNoise(mote.Phase + 9, clock * .17f) - .5f) * .5f,
                    Mathf.PerlinNoise(clock * .21f, mote.Phase) - .5f) * .22f;
                mote.Position += mote.Velocity * dt;
            }

            // Embers grow to a whole pixel of the overhead view so they read as sparks there.
            float emberScale = Mathf.Lerp(1, 2.2f, overheadAmount);
            WriteQuads(emberMesh, emberVertices, embers, eye, emberScale, true);
            if (dustRenderer.enabled) WriteQuads(dustMesh, dustVertices, dust, eye, 1, false);
        }

        void WriteQuads(Mesh mesh, Vector3[] vertices, Mote[] motes, Transform eye, float scale, bool flicker)
        {
            Vector3 right = eye.right, up = eye.up;
            for (int i = 0; i < motes.Length; i++)
            {
                Mote mote = motes[i];
                float size = 0;
                if (mote.Life > 0 && mote.Age < mote.Life)
                {
                    // No transparency: a mote swells in and shrinks away instead of fading.
                    float t = mote.Age / mote.Life;
                    float envelope = Mathf.Clamp01(t * 6) * Mathf.Clamp01((1 - t) * (flicker ? 1.6f : 4));
                    if (flicker) envelope *= .7f + .3f * Mathf.PerlinNoise(mote.Phase, clock * 9);
                    size = mote.Size * scale * envelope;
                }
                Vector3 r = right * size, u = up * size;
                int v = i * 4;
                vertices[v] = mote.Position - r - u; vertices[v + 1] = mote.Position - r + u;
                vertices[v + 2] = mote.Position + r + u; vertices[v + 3] = mote.Position + r - u;
            }
            mesh.vertices = vertices;
        }

        static Mesh MoteMesh(string label, int count, out Vector3[] vertices)
        {
            vertices = new Vector3[count * 4];
            var triangles = new int[count * 6];
            var normals = new Vector3[count * 4];
            for (int i = 0; i < count; i++)
            {
                int v = i * 4, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
                normals[v] = normals[v + 1] = normals[v + 2] = normals[v + 3] = Vector3.up;
            }
            var mesh = new Mesh { name = "Imagem / " + label };
            mesh.MarkDynamic();
            mesh.vertices = vertices; mesh.normals = normals; mesh.uv = new Vector2[count * 4]; mesh.triangles = triangles;
            // Motes roam the whole floor; never cull them by a stale box.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4000);
            return mesh;
        }

        MeshRenderer MoteRenderer(string label, Mesh mesh, out Material material)
        {
            var node = new GameObject("Imagem / " + label); node.transform.SetParent(transform, false);
            node.transform.position = Vector3.zero; node.transform.rotation = Quaternion.identity;
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = node.AddComponent<MeshRenderer>();
            // Pure emission: black surface colour, so motes glow the same in light and shadow.
            material = new Material(HauntFpsGame.LitTemplate()) { name = "Imagem / " + label };
            material.SetColor("_BaseColor", Color.black);
            material.SetColor("_EmissionColor", new Color(1, .6f, .3f));
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }

        static void DestroyOwned(UnityEngine.Object value)
        {
            if (!value) return;
            if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }

        public void Dispose()
        {
            if (disposed) return; disposed = true;
            foreach (var profile in new[] { eyeProfile, overheadProfile })
            {
                if (!profile) continue;
                foreach (var component in profile.components) DestroyOwned(component);
                DestroyOwned(profile);
            }
            if (eyeVolume) DestroyOwned(eyeVolume.gameObject);
            if (overheadVolume) DestroyOwned(overheadVolume.gameObject);
            if (emberRenderer) DestroyOwned(emberRenderer.gameObject);
            if (dustRenderer) DestroyOwned(dustRenderer.gameObject);
            DestroyOwned(emberMesh); DestroyOwned(dustMesh); DestroyOwned(emberMaterial); DestroyOwned(dustMaterial);
            eyeProfile = overheadProfile = null; styledWorld = null;
        }

        void OnDestroy() => Dispose();
    }
}
