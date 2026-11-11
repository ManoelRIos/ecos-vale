using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ecos.Haunt.Editor
{
    /// <summary>Creates separate renderer resources for the roguelike and first-person prototype.</summary>
    [InitializeOnLoad]
    public static class HauntSetup
    {
        public const string PipelinePath = "Assets/_Game/Resources/HauntURP.asset";
        public const string RendererPath = "Assets/_Game/Resources/HauntRenderer.asset";
        const string MaterialPath = "Assets/_Game/Resources/HauntSprites.mat";
        const string SpriteShader = "Haunt/Pixel Sprites";
        static bool creatingDefaults, playPreparationQueued;

        static HauntSetup()
        {
            EditorApplication.delayCall += Ensure;
            EditorApplication.playModeStateChanged += PrepareBeforePlay;
        }

        static void Ensure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || creatingDefaults) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall -= Ensure;
                EditorApplication.delayCall += Ensure;
                return;
            }
            if (NeedsSetup()) CreateDefaults();
        }

        static bool NeedsSetup()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (!UsesRenderer(pipeline, renderer) || GraphicsSettings.defaultRenderPipeline != pipeline)
                return true;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) != pipeline) return true;
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material || material.shader != Shader.Find(SpriteShader)) return true;
            var fpsPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(HauntFpsProject.PipelinePath);
            var fpsRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(HauntFpsProject.RendererPath);
            if (!UsesRenderer(fpsPipeline, fpsRenderer) || fpsPipeline.shadowDistance != 30
                || !fpsPipeline.supportsMainLightShadows || HauntFpsProject.PictureNeedsSetup(fpsPipeline, fpsRenderer)) return true;
            var scenes = EditorBuildSettings.scenes;
            return scenes.Length != 2 || !scenes[0].enabled || scenes[0].path != HauntFpsProject.ScenePath
                || !scenes[1].enabled || scenes[1].path != HauntProject.ScenePath;
        }

        static void PrepareBeforePlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode || playPreparationQueued || !NeedsSetup()) return;
            // Asset writes happen only after the cancelled Play transition returns to Edit Mode.
            EditorApplication.isPlaying = false;
            playPreparationQueued = true;
            EditorApplication.delayCall += FinishPreparingPlay;
        }

        static void FinishPreparingPlay()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += FinishPreparingPlay;
                return;
            }
            try
            {
                CreateDefaults();
                if (NeedsSetup()) throw new InvalidOperationException("Meia-Noite: preparação incompleta. Confira o primeiro erro do Console.");
                playPreparationQueued = false;
                EditorApplication.isPlaying = true;
            }
            catch (Exception error)
            {
                playPreparationQueued = false;
                Debug.LogException(error);
            }
        }

        [MenuItem("Ecos/Preparar projeto")]
        public static void CreateDefaults()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Pare o Play Mode antes de preparar os recursos do projeto.");
            if (creatingDefaults) return;
            creatingDefaults = true;
            try
            {
                if (!File.Exists(HauntProject.ScenePath))
                    throw new IOException("A cena do roguelike está ausente: " + HauntProject.ScenePath);
                if (!File.Exists(HauntFpsProject.ScenePath))
                    throw new IOException("A cena do protótipo FPS está ausente: " + HauntFpsProject.ScenePath);
                Directory.CreateDirectory("Assets/_Game/Resources");
                AssetDatabase.Refresh();
                var pipeline = EnsurePipelineAssets(PipelinePath, RendererPath);
                ApplyPipeline(pipeline);
                HauntFpsProject.EnsurePipeline();
                EnsureSpriteMaterial();
                EditorBuildSettings.scenes = new[] {
                    new EditorBuildSettingsScene(HauntFpsProject.ScenePath, true),
                    new EditorBuildSettingsScene(HauntProject.ScenePath, true)
                };
                PlayerSettings.companyName = "Ecos da Meia-Noite";
                PlayerSettings.productName = "Ecos da Meia-Noite";
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;
                PlayerSettings.runInBackground = true;
                AssetDatabase.SaveAssets();
            }
            finally { creatingDefaults = false; }
        }

        [MenuItem("Ecos/Preparar projeto", true)]
        [MenuItem("Ecos/Reparar renderização URP", true)]
        public static bool CanPrepare()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
        }

        static void EnsureSpriteMaterial()
        {
            var shader = Shader.Find(SpriteShader);
            if (!shader) throw new InvalidOperationException("Shader de sprites ausente: " + SpriteShader + ". Confira o primeiro erro de importação no Console.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material)
            {
                AssetDatabase.CreateAsset(new Material(shader), MaterialPath);
                return;
            }
            if (material.shader == shader) return;
            material.shader = shader;
            EditorUtility.SetDirty(material);
        }

        static bool UsesRenderer(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer)
        {
            if (!pipeline || !renderer) return false;
            using (var serialized = new SerializedObject(pipeline))
            {
                var selected = serialized.FindProperty("m_DefaultRendererIndex");
                if (selected == null) return false;
                int index = selected.intValue;
                var renderers = pipeline.rendererDataList;
                return index >= 0 && index < renderers.Length && renderers[index] == renderer;
            }
        }

        // This operation leaves global Graphics/Quality settings untouched so it can
        // also repair an isolated asset pair without changing the active project.
        public static UniversalRenderPipelineAsset EnsurePipelineAssets(string pipelinePath, string rendererPath)
        {
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (!renderer)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (!pipeline)
            {
                pipeline = CreatePipeline(renderer);
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            else if (!UsesRenderer(pipeline, renderer))
            {
                var repaired = CreatePipeline(renderer);
                string name = pipeline.name;
                try
                {
                    EditorUtility.CopySerialized(repaired, pipeline);
                    pipeline.name = name;
                    EditorUtility.SetDirty(pipeline);
                }
                finally { UnityEngine.Object.DestroyImmediate(repaired); }
            }
            return pipeline;
        }

        static UniversalRenderPipelineAsset CreatePipeline(UniversalRendererData renderer)
        {
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.renderScale = 1;
            pipeline.msaaSampleCount = 1;
            pipeline.shadowDistance = 0;
            return pipeline;
        }

        static void ApplyPipeline(UniversalRenderPipelineAsset pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int previous = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    if (QualitySettings.GetRenderPipelineAssetAt(i) == pipeline) continue;
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
            }
            finally { QualitySettings.SetQualityLevel(previous, false); }
        }

        [MenuItem("Ecos/Reparar renderização URP")]
        public static void RepairRendering()
        {
            if (!CanPrepare())
            {
                Debug.LogWarning("Ecos: pare o Play Mode e aguarde compilação/importação antes de reparar a renderização.");
                return;
            }
            CreateDefaults();
            var pipeline = RefreshPipelineResources(PipelinePath, RendererPath);
            ApplyPipeline(pipeline);
            EnsureSpriteMaterial();
            AssetDatabase.SaveAssets();
            Debug.Log("Meia-Noite: renderer e material de sprites preparados. Teste a cena Haunt em Play.");
        }

        // An explicit repair resets generated renderer resources but retains both GUIDs.
        public static UniversalRenderPipelineAsset RefreshPipelineResources(string pipelinePath, string rendererPath)
        {
            var pipeline = EnsurePipelineAssets(pipelinePath, rendererPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            var freshRenderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var freshPipeline = CreatePipeline(renderer);
            string rendererName = renderer.name, pipelineName = pipeline.name;
            try
            {
                EditorUtility.CopySerialized(freshRenderer, renderer);
                renderer.name = rendererName;
                EditorUtility.SetDirty(renderer);
                EditorUtility.CopySerialized(freshPipeline, pipeline);
                pipeline.name = pipelineName;
                EditorUtility.SetDirty(pipeline);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(freshPipeline);
                UnityEngine.Object.DestroyImmediate(freshRenderer);
            }
            return pipeline;
        }

        public static void BuildWindows()
        {
            CreateDefaults();
            string output = System.Environment.GetEnvironmentVariable("HAUNT_BUILD_OUTPUT");
            if (string.IsNullOrEmpty(output)) output = "Builds/Windows/EcosDaMeiaNoite.exe";
            var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Build Windows falhou: " + result.summary.result);
        }
    }
}
