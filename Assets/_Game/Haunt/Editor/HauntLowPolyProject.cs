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
    /// <summary>Entry for the new low-poly game. The two prior scenes remain accessible.</summary>
    public static class HauntLowPolyProject
    {
        public const string ScenePath = "Assets/_Game/Scenes/HauntLowPoly.unity";
        public const string MaterialPath = "Assets/_Game/Resources/LowPolyPalette.mat";

        [MenuItem("Ecos/Abrir jogo low poly", false, -20)]
        public static void Open()
        {
            if (!CanOpen())
            {
                Debug.LogWarning("Ecos: pare o Play Mode e aguarde compilação/importação antes de abrir o jogo low poly.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Ecos/Abrir jogo low poly", true)]
        public static bool CanOpen() => HauntProject.CanOpen();

        /// <summary>Imported-resource checks only; this does not enter Play Mode.</summary>
        public static void SmokeCheck()
        {
            if (!CanOpen()) throw new InvalidOperationException("Pare o Play Mode antes do smoke check low poly.");
            HauntSetup.CreateDefaults();
            if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath))
                throw new InvalidOperationException("A cena low poly não foi importada.");
            var entry = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            if (entry == null || entry.path != ScenePath)
                throw new InvalidOperationException("HauntLowPoly deve iniciar o build.");
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(HauntFpsProject.PipelinePath);
            if (!pipeline || pipeline.rendererDataList.Length == 0 || !(pipeline.rendererDataList[0] is UniversalRendererData)
                || !pipeline.supportsMainLightShadows || pipeline.shadowDistance != 30)
                throw new InvalidOperationException("O jogo low poly exige renderer URP 3D com sombras.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (!material || !material.shader || material.shader.name != "Ecos/HauntLowPolyToon")
                throw new InvalidOperationException("Material ou shader low poly não foi importado.");
            if (ShaderUtil.ShaderHasError(material.shader))
                throw new InvalidOperationException("O shader low poly possui erros de compilação. Confira o Console.");

            var previous = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !existing.isLoaded;
            var scene = opened ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : existing;
            try
            {
                var games = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<HauntFpsGame>(true)).ToArray();
                if (games.Length != 1 || !games[0].enabled || !games[0].LowPolyMode)
                    throw new InvalidOperationException("A cena low poly deve ter um único HauntFpsGame habilitado com LowPolyMode ativo.");
                if (scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<HauntGame>(true).Length) != 0)
                    throw new InvalidOperationException("A cena low poly não deve iniciar o runtime 2D antigo.");
                Debug.Log("Haunt low-poly import smoke: scene, profile, material and 3D renderer PASS. Rendering, physics, camera collision, controller and builds require separate validation.");
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
    }
}
