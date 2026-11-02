using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ecos.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string Resource = "Assets/_Game/Resources/";
        static ProjectSetup()
        {
            EditorApplication.delayCall += Ensure;
        }

        static void Ensure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                return;
            if (!AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Resource + "ValleyURP.asset"))
                CreateDefaults();
        }

        [MenuItem("Ecos/Criar assets e atualizar cena")]
        public static void CreateDefaults()
        {
            Directory.CreateDirectory(Resource);
            Directory.CreateDirectory("Assets/_Game/Scenes");
            var w = AssetDatabase.LoadAssetAtPath<WorldSettings>(Resource + "WorldSettings.asset");
            if (!w)
            {
                w = WorldSettings.Defaults();
                for (int i = 0; i < 6; i++)
                {
                    var biome = w.biomes[i];
                    AssetDatabase.CreateAsset(biome, $"Assets/_Game/Biomes/Biome{i}.asset");
                }

                AssetDatabase.CreateAsset(w, Resource + "WorldSettings.asset");
            }

            EnsurePipeline();
            EnsureMaterial("TerrainMaterial", "Ecos/Terrain");
            EnsureMaterial("LitMaterial", "Universal Render Pipeline/Lit");
            EnsureMaterial("WeatherMaterial", "Universal Render Pipeline/Particles/Unlit");
            string[] ids =
            {
                "wood",
                "stone",
                "crystal",
                "herb",
                "artifact",
                "key",
                "quest"
            };
            string[] names =
            {
                "Madeira",
                "Pedra",
                "Cristal de eco",
                "Erva",
                "Artefato do Vale",
                "Chave",
                "Fragmento de missão"
            };
            for (int i = 0; i < ids.Length; i++)
            {
                string path = Resource + "Item_" + ids[i] + ".asset";
                if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path))
                    continue;
                var item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.id = ids[i];
                item.displayName = names[i];
                item.description = i == 2 ? "Restaura um santuário antigo." : "Objeto encontrado no vale.";
                item.category = i < 3 ? ItemCategory.Resource : i == 3 ? ItemCategory.Herb : i == 4 ? ItemCategory.Artifact : i == 5 ? ItemCategory.Key : ItemCategory.Quest;
                AssetDatabase.CreateAsset(item, path);
            }

            // Keep authored reference prefabs; create missing examples through engine serialization.
            if (!AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Traveler.prefab"))
            {
                var palette = new Palette();
                var player = LowPolyFactory.Player(palette);
                PersistMaterials(player);
                player.AddComponent<CharacterController>();
                PrefabUtility.SaveAsPrefabAsset(player, "Assets/_Game/Prefabs/Traveler.prefab");
                UnityEngine.Object.DestroyImmediate(player);
                var shrine = LowPolyFactory.Shrine(palette, 0);
                PersistMaterials(shrine);
                PrefabUtility.SaveAsPrefabAsset(shrine, "Assets/_Game/Prefabs/Shrine.prefab");
                UnityEngine.Object.DestroyImmediate(shrine); /* Persisted materials now belong to AssetDatabase. */
            }

            if (!File.Exists("Assets/_Game/Scenes/Valley.unity"))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("Ecos do Vale");
                root.AddComponent<GameBootstrap>().configuration = w;
                EditorSceneManager.SaveScene(scene, "Assets/_Game/Scenes/Valley.unity");
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Game/Scenes/Valley.unity", true)
            };
            PlayerSettings.companyName = "Ecos do Vale";
            PlayerSettings.productName = "Ecos do Vale";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void PersistMaterials(GameObject g)
        {
            foreach (var filter in g.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == LowPolyMesh.Octahedron && !AssetDatabase.Contains(filter.sharedMesh))
                    AssetDatabase.CreateAsset(filter.sharedMesh, "Assets/_Game/Art/Octahedron.asset");
            }

            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                var m = r.sharedMaterial;
                if (AssetDatabase.Contains(m))
                    continue;
                string path = AssetDatabase.GenerateUniqueAssetPath("Assets/_Game/Art/LowPoly.mat");
                AssetDatabase.CreateAsset(m, path);
            }
        }

        static void EnsureMaterial(string name, string shader)
        {
            string path = Resource + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path))
                return;
            var s = Shader.Find(shader);
            if (!s)
                throw new InvalidOperationException("Shader ausente: " + shader);
            var m = new Material(s)
            {
                enableInstancing = true
            };
            AssetDatabase.CreateAsset(m, path);
        }

        static void EnsurePipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Resource + "ValleyURP.asset");
            if (!pipeline)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Resource + "ValleyRenderer.asset");
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, Resource + "ValleyURP.asset");
                var serialized = new SerializedObject(pipeline);
                var renderers = serialized.FindProperty("m_RendererDataList");
                renderers.arraySize = 1;
                renderers.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                pipeline.renderScale = 1;
                pipeline.shadowDistance = 90;
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }

        [MenuItem("Ecos/Selecionar configurações do mundo")]
        public static void SelectSettings()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<WorldSettings>(Resource + "WorldSettings.asset");
        }

        [MenuItem("Ecos/Abrir cena principal")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Valley.unity");
        }

        public static void BuildWindows()
        {
            CreateDefaults();
            string output = Environment.GetEnvironmentVariable("ECOS_BUILD_OUTPUT");
            if (string.IsNullOrEmpty(output))
                output = "Builds/Windows/EcosDoVale.exe";
            var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Build Windows falhou: " + result.summary.result);
        }
    }
}
