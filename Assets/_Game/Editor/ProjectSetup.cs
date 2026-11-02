using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Ecos.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string Resource = "Assets/_Game/Resources/";
        const string ScenePath = "Assets/_Game/Scenes/Valley.unity";
        const string TravelerPath = "Assets/_Game/Prefabs/Traveler.prefab";
        const string ShrinePath = "Assets/_Game/Prefabs/Shrine.prefab";
        static readonly string[] ItemIds = { "wood", "stone", "crystal", "herb", "artifact", "key", "quest" };
        static bool creatingDefaults;

        static ProjectSetup()
        {
            EditorApplication.delayCall += Ensure;
        }

        static void Ensure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || creatingDefaults)
                return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall -= Ensure;
                EditorApplication.delayCall += Ensure;
                return;
            }

            if (NeedsSetup())
                CreateDefaults();
        }

        static bool NeedsSetup()
        {
            var world = AssetDatabase.LoadAssetAtPath<WorldSettings>(Resource + "WorldSettings.asset");
            if (!world || world.biomes == null || world.biomes.Length != 6)
                return true;
            foreach (var biome in world.biomes)
                if (!biome) return true;
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Resource + "ValleyURP.asset");
            if (!HasDefaultRenderer(pipeline) || !AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Resource + "ValleyRenderer.asset"))
                return true;
            if (!GraphicsSettings.defaultRenderPipeline)
                return true;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (!QualitySettings.GetRenderPipelineAssetAt(i)) return true;
            foreach (var name in new[] { "TerrainMaterial", "LitMaterial", "WeatherMaterial" })
                if (!AssetDatabase.LoadAssetAtPath<Material>(Resource + name + ".mat")) return true;
            foreach (var id in ItemIds)
                if (!AssetDatabase.LoadAssetAtPath<ItemDefinition>(Resource + "Item_" + id + ".asset")) return true;
            return !AssetDatabase.LoadAssetAtPath<GameObject>(TravelerPath)
                || !AssetDatabase.LoadAssetAtPath<GameObject>(ShrinePath)
                || !File.Exists(ScenePath);
        }

        [MenuItem("Ecos/Criar assets e atualizar cena")]
        public static void CreateDefaults()
        {
            if (creatingDefaults)
                return;
            creatingDefaults = true;
            try
            {
                Directory.CreateDirectory(Resource);
                Directory.CreateDirectory("Assets/_Game/Biomes");
                Directory.CreateDirectory("Assets/_Game/Prefabs");
                Directory.CreateDirectory("Assets/_Game/Scenes");
                AssetDatabase.Refresh();
                var world = EnsureWorldSettings();
                EnsurePipeline();
                EnsureMaterial("TerrainMaterial", "Ecos/Terrain");
                EnsureMaterial("LitMaterial", "Universal Render Pipeline/Lit");
                EnsureMaterial("WeatherMaterial", "Universal Render Pipeline/Particles/Unlit");
                EnsureItems();
                EnsurePrefabs();
                EnsureScene(world);
                EnsureBuildScene();
                PlayerSettings.companyName = "Ecos do Vale";
                PlayerSettings.productName = "Ecos do Vale";
                PlayerSettings.defaultScreenWidth = 1920;
                PlayerSettings.defaultScreenHeight = 1080;
                PlayerSettings.runInBackground = true;
                AssetDatabase.SaveAssets();
            }
            finally
            {
                creatingDefaults = false;
            }
        }

        static WorldSettings EnsureWorldSettings()
        {
            var world = AssetDatabase.LoadAssetAtPath<WorldSettings>(Resource + "WorldSettings.asset");
            var defaults = WorldSettings.Defaults();
            var templates = (BiomeDefinition[])defaults.biomes.Clone();
            bool created = !world;
            if (created)
                world = defaults;
            if (world.biomes == null || world.biomes.Length != 6)
                Array.Resize(ref world.biomes, 6);
            for (int i = 0; i < 6; i++)
            {
                var biome = world.biomes[i];
                if (!biome || !AssetDatabase.Contains(biome))
                {
                    string path = $"Assets/_Game/Biomes/Biome{i}.asset";
                    var existing = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(path);
                    if (existing)
                        world.biomes[i] = existing;
                    else
                    {
                        world.biomes[i] = templates[i];
                        AssetDatabase.CreateAsset(world.biomes[i], path);
                    }
                }
            }

            if (created)
                AssetDatabase.CreateAsset(world, Resource + "WorldSettings.asset");
            EditorUtility.SetDirty(world);
            // Default templates not adopted as assets are temporary editor objects.
            foreach (var biome in templates)
                if (biome && !AssetDatabase.Contains(biome)) UnityEngine.Object.DestroyImmediate(biome);
            if (!created)
                UnityEngine.Object.DestroyImmediate(defaults);
            return world;
        }

        static void EnsureItems()
        {
            string[] names = { "Madeira", "Pedra", "Cristal de eco", "Erva", "Artefato do Vale", "Chave", "Fragmento de missão" };
            for (int i = 0; i < ItemIds.Length; i++)
            {
                string path = Resource + "Item_" + ItemIds[i] + ".asset";
                if (AssetDatabase.LoadAssetAtPath<ItemDefinition>(path))
                    continue;
                var item = ScriptableObject.CreateInstance<ItemDefinition>();
                item.id = ItemIds[i];
                item.displayName = names[i];
                item.description = i == 2 ? "Restaura um santuário antigo." : "Objeto encontrado no vale.";
                item.category = i < 3 ? ItemCategory.Resource : i == 3 ? ItemCategory.Herb : i == 4 ? ItemCategory.Artifact : i == 5 ? ItemCategory.Key : ItemCategory.Quest;
                AssetDatabase.CreateAsset(item, path);
            }
        }

        static void EnsurePrefabs()
        {
            bool needsTraveler = !AssetDatabase.LoadAssetAtPath<GameObject>(TravelerPath);
            bool needsShrine = !AssetDatabase.LoadAssetAtPath<GameObject>(ShrinePath);
            if (!needsTraveler && !needsShrine)
                return;
            var previous = SceneManager.GetActiveScene();
            var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var palette = new Palette();
            try
            {
                SceneManager.SetActiveScene(temporary);
                if (needsTraveler)
                {
                    var player = LowPolyFactory.Player(palette);
                    PersistMaterials(player);
                    player.AddComponent<CharacterController>();
                    PrefabUtility.SaveAsPrefabAsset(player, TravelerPath);
                    UnityEngine.Object.DestroyImmediate(player);
                }
                if (needsShrine)
                {
                    var shrine = LowPolyFactory.Shrine(palette, 0);
                    PersistMaterials(shrine);
                    PrefabUtility.SaveAsPrefabAsset(shrine, ShrinePath);
                    UnityEngine.Object.DestroyImmediate(shrine);
                }
            }
            finally
            {
                // Persisted materials belong to the AssetDatabase; only the terrain clone is temporary.
                if (palette.terrain && !AssetDatabase.Contains(palette.terrain))
                    UnityEngine.Object.DestroyImmediate(palette.terrain);
                EditorSceneManager.CloseScene(temporary, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
        }

        static void EnsureScene(WorldSettings world)
        {
            if (File.Exists(ScenePath))
                return;
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var root = new GameObject("Ecos do Vale");
                root.AddComponent<GameBootstrap>().configuration = world;
                if (!EditorSceneManager.SaveScene(scene, ScenePath))
                    throw new IOException("Não foi possível salvar a cena inicial.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded)
                    SceneManager.SetActiveScene(previous);
            }
        }

        static void EnsureBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var scene in scenes)
                if (scene.path == ScenePath) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void PersistMaterials(GameObject go)
        {
            const string meshPath = "Assets/_Game/Art/Octahedron.asset";
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != LowPolyMesh.Octahedron || AssetDatabase.Contains(filter.sharedMesh))
                    continue;
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing)
                    filter.sharedMesh = existing;
                else
                    AssetDatabase.CreateAsset(filter.sharedMesh, meshPath);
            }
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var material = renderer.sharedMaterial;
                if (!AssetDatabase.Contains(material))
                    AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath("Assets/_Game/Art/LowPoly.mat"));
            }
        }

        static void EnsureMaterial(string name, string shader)
        {
            string path = Resource + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path))
                return;
            var source = Shader.Find(shader);
            if (!source)
                throw new InvalidOperationException("Shader ausente: " + shader);
            AssetDatabase.CreateAsset(new Material(source) { enableInstancing = true }, path);
        }

        static bool HasDefaultRenderer(UniversalRenderPipelineAsset pipeline)
        {
            if (!pipeline)
                return false;
            // URP exposes rendererDataList but not the selected index. Read the latter without
            // mutating private renderer arrays or constructing renderers during asset import.
            using (var serialized = new SerializedObject(pipeline))
            {
                var selected = serialized.FindProperty("m_DefaultRendererIndex");
                if (selected == null)
                    return false;
                var renderers = pipeline.rendererDataList;
                int index = selected.intValue;
                return index >= 0 && index < renderers.Length && renderers[index];
            }
        }

        // Isolated asset operation: no graphics/quality assignment, suitable for editor tests.
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
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.renderScale = 1;
                pipeline.shadowDistance = 90;
                AssetDatabase.CreateAsset(pipeline, pipelinePath);
            }
            else if (!HasDefaultRenderer(pipeline))
            {
                var repaired = UniversalRenderPipelineAsset.Create(renderer);
                repaired.renderScale = 1;
                repaired.shadowDistance = 90;
                string name = pipeline.name;
                try
                {
                    EditorUtility.CopySerialized(repaired, pipeline);
                    pipeline.name = name;
                    EditorUtility.SetDirty(pipeline);
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(repaired);
                }
            }
            return pipeline;
        }

        static void EnsurePipeline()
        {
            var pipeline = EnsurePipelineAssets(Resource + "ValleyURP.asset", Resource + "ValleyRenderer.asset");
            if (!GraphicsSettings.defaultRenderPipeline)
                GraphicsSettings.defaultRenderPipeline = pipeline;
            int previous = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    if (QualitySettings.GetRenderPipelineAssetAt(i))
                        continue;
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = pipeline;
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(previous, false);
            }
        }

        [MenuItem("Ecos/Selecionar configurações do mundo")]
        public static void SelectSettings()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<WorldSettings>(Resource + "WorldSettings.asset");
        }

        [MenuItem("Ecos/Abrir cena principal")]
        public static void Open()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static void BuildWindows()
        {
            CreateDefaults();
            string output = System.Environment.GetEnvironmentVariable("ECOS_BUILD_OUTPUT");
            if (string.IsNullOrEmpty(output))
                output = "Builds/Windows/EcosDoVale.exe";
            var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Build Windows falhou: " + result.summary.result);
        }
    }
}
