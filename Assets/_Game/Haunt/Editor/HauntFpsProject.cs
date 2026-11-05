using System;
using System.Linq;
using Ecos.Haunt.Fps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Ecos.Haunt.Editor
{
    /// <summary>Separate entry for the first-person experiment; the 2D scene remains available.</summary>
    public static class HauntFpsProject
    {
        public const string ScenePath = "Assets/_Game/Scenes/HauntFps.unity";
        public const string PipelinePath = "Assets/_Game/Resources/FpsPipeline.asset";
        public const string RendererPath = "Assets/_Game/Resources/FpsRenderer.asset";
        public const string MaterialPath = "Assets/_Game/Resources/FpsPalette.mat";

        [MenuItem("Ecos/Abrir protótipo FPS", false, -10)]
        public static void Open()
        {
            if (!HauntProject.CanOpen())
            {
                Debug.LogWarning("Ecos: pare o Play Mode e aguarde compilação/importação antes de abrir o protótipo FPS.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Ecos/Abrir protótipo FPS", true)]
        public static bool CanOpen() => HauntProject.CanOpen();

        public static UniversalRenderPipelineAsset EnsurePipeline() => EnsurePipelineAssets(PipelinePath, RendererPath);

        // Isolated paths let repair checks exercise the same configuration without
        // replacing the user's global Graphics/Quality settings or renderer assets.
        public static UniversalRenderPipelineAsset EnsurePipelineAssets(string pipelinePath, string rendererPath)
        {
            var pipeline = HauntSetup.EnsurePipelineAssets(pipelinePath, rendererPath);
            bool dirty = pipeline.shadowDistance != 30 || pipeline.msaaSampleCount != 1 || pipeline.renderScale != 1;
            pipeline.shadowDistance = 30;
            pipeline.msaaSampleCount = 1;
            pipeline.renderScale = 1;
            if (!pipeline.supportsMainLightShadows)
            {
                // URP 17 exposes only a public getter; its setter is internal to
                // the package. Editor tools must modify the serialized asset.
                using (var serialized = new SerializedObject(pipeline))
                {
                    var shadows = serialized.FindProperty("m_MainLightShadowsSupported");
                    if (shadows == null)
                        throw new InvalidOperationException("Configuração de sombras URP não encontrada. Use a versão de pacotes do projeto.");
                    shadows.boolValue = true;
                    var anyShadows = serialized.FindProperty("m_AnyShadowsSupported");
                    if (anyShadows != null) anyShadows.boolValue = true;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                dirty = true;
            }
            if (dirty) EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        /// <summary>Checks imported resources and scene wiring; does not enter Play Mode.</summary>
        public static void SmokeCheck()
        {
            if (!CanOpen()) throw new InvalidOperationException("Pare o Play Mode antes do smoke check FPS.");
            HauntSetup.CreateDefaults();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                throw new InvalidOperationException("A cena do protótipo FPS não foi importada.");
            var entry = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            if (entry == null || entry.path != ScenePath)
                throw new InvalidOperationException("HauntFps deve ser a primeira cena habilitada no build.");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (!pipeline || pipeline.rendererDataList.Length == 0 || !(pipeline.rendererDataList[0] is UniversalRendererData))
                throw new InvalidOperationException("O FPS exige um renderer URP 3D válido.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material || !material.shader || material.shader.name != "Ecos/HauntFpsLit")
                throw new InvalidOperationException("O material e shader de iluminação do FPS não foram importados.");
            if (ShaderUtil.ShaderHasError(material.shader))
                throw new InvalidOperationException("O shader FPS possui erros de compilação. Confira o Console do Unity.");
            var previous = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !existing.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : existing;
            try
            {
                var games = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HauntFpsGame>(true)).ToArray();
                if (games.Length != 1 || !games[0].enabled)
                    throw new InvalidOperationException("A cena FPS deve conter exatamente um HauntFpsGame habilitado.");
                Debug.Log("Haunt FPS import smoke: separate scene, 3D renderer and first build entry PASS. Play Mode, physics, rendering and controller remain separate checks.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
