using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering;
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
        static bool playPreparationQueued;
        const string LitShaderName = "Universal Render Pipeline/Lit";
        const string WeatherShaderName = "Universal Render Pipeline/Particles/Unlit";
        const string TerrainShaderName = "Ecos/Terrain";
        const string SkyShaderName = "Ecos/ValleySky";

        static ProjectSetup()
        {
            EditorApplication.delayCall += Ensure;
            EditorApplication.playModeStateChanged += PrepareBeforePlay;
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
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Resource + "ValleyRenderer.asset");
            if (!HasDefaultRenderer(pipeline) || !renderer || !UsesRenderer(pipeline, renderer))
                return true;
            if (GraphicsSettings.defaultRenderPipeline != pipeline)
                return true;
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) != pipeline) return true;
            if (!MaterialUsesShader(Resource + "TerrainMaterial.mat", TerrainShaderName)
                || !MaterialUsesShader(Resource + "LitMaterial.mat", LitShaderName)
                || !MaterialUsesShader(Resource + "GlowMaterial.mat", LitShaderName)
                || !MaterialUsesShader(Resource + "WeatherMaterial.mat", WeatherShaderName)
                || !MaterialUsesShader(Resource + "ValleySkyMaterial.mat", SkyShaderName))
                return true;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Game/Art" }))
                if (!MaterialUsesShader(AssetDatabase.GUIDToAssetPath(guid), LitShaderName)) return true;
            foreach (var id in ItemIds)
                if (!AssetDatabase.LoadAssetAtPath<ItemDefinition>(Resource + "Item_" + id + ".asset")) return true;
            return !AssetDatabase.LoadAssetAtPath<GameObject>(TravelerPath)
                || !AssetDatabase.LoadAssetAtPath<GameObject>(ShrinePath)
                || !File.Exists(ScenePath);
        }

        static void PrepareBeforePlay(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode || playPreparationQueued)
                return;
            if (!NeedsSetup())
                return;
            // Save/import only after the cancelled transition has returned to Edit Mode.
            EditorApplication.isPlaying = false;
            playPreparationQueued = true;
            EditorApplication.delayCall += FinishPreparingPlay;
        }

        static void FinishPreparingPlay()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += FinishPreparingPlay;
                return;
            }
            try
            {
                CreateDefaults();
                if (NeedsSetup())
                    throw new InvalidOperationException("Ecos: preparação incompleta. Use Ecos > Diagnosticar renderização e confira o primeiro erro do Console.");
                playPreparationQueued = false;
                EditorApplication.isPlaying = true;
            }
            catch (Exception error)
            {
                playPreparationQueued = false;
                Debug.LogException(error);
            }
        }

        [MenuItem("Ecos/Criar assets e atualizar cena")]
        public static void CreateDefaults()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Pare o Play Mode antes de preparar os assets do projeto.");
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
                EnsureProjectMaterials();
                EnsureItems();
                EnsurePrefabs();
                EnsureScene(world);
                EnsureBuildScene();
                PlayerSettings.companyName = "Ecos do Vale";
                PlayerSettings.productName = "Ecos da Meia-Noite";
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
            // Keep the roguelike as the build entry; retain the valley as an optional legacy scene.
            const string hauntScene = "Assets/_Game/Scenes/Haunt.unity";
            if (!File.Exists(hauntScene))
                throw new IOException("A cena do roguelike está ausente: " + hauntScene);
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(hauntScene, true),
                new EditorBuildSettingsScene(ScenePath, false)
            };
            foreach (var scene in EditorBuildSettings.scenes)
                if (scene.path != hauntScene && scene.path != ScenePath) scenes.Add(scene);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void PersistMaterials(GameObject go)
        {
            // Native primitives already have an engine asset path. Factory meshes need a
            // real asset before the prefab can safely reference them after a domain reload.
            var persisted = new Dictionary<Mesh, Mesh>();
            foreach (var filter in go.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = filter.sharedMesh;
                if (!LowPolyMesh.IsGenerated(mesh) || AssetDatabase.Contains(mesh) || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(mesh)))
                    continue;
                if (!persisted.TryGetValue(mesh, out var saved))
                {
                    string path = "Assets/_Game/Art/" + SafeAssetName(mesh.name) + ".asset";
                    saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (!saved)
                    {
                        saved = mesh;
                        AssetDatabase.CreateAsset(saved, path);
                    }
                    persisted.Add(mesh, saved);
                }
                filter.sharedMesh = saved;
                foreach (var collider in go.GetComponentsInChildren<MeshCollider>())
                    if (collider.sharedMesh == mesh) collider.sharedMesh = saved;
            }
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var material = renderer.sharedMaterial;
                if (material && !AssetDatabase.Contains(material))
                    AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath("Assets/_Game/Art/LowPoly.mat"));
            }
        }

        static string SafeAssetName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return string.IsNullOrEmpty(value) ? "LowPolyMesh" : value;
        }

        [MenuItem("Ecos/Criar assets e atualizar cena", true)]
        [MenuItem("Ecos/Reparar renderização URP", true)]
        [MenuItem("Ecos/Abrir cena principal", true)]
        public static bool CanRunEditModeCommands()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
        }

        static bool MaterialUsesShader(string path, string shaderName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var expected = Shader.Find(shaderName);
            return material && expected && material.shader == expected;
        }

        // Material repair is deliberately isolated from global pipeline/quality settings.
        public static Material EnsureMaterialAsset(string path, string shaderName)
        {
            var expected = Shader.Find(shaderName);
            if (!expected)
                throw new InvalidOperationException("Shader ausente: " + shaderName + ". Confira o primeiro erro de importação no Console e o caminho curto do projeto.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(expected) { enableInstancing = true };
                AssetDatabase.CreateAsset(material, path);
                return material;
            }
            bool changed = false;
            if (material.shader != expected)
            {
                bool hasBase = material.HasProperty("_BaseColor");
                bool hasColor = material.HasProperty("_Color");
                Color original = hasBase ? material.GetColor("_BaseColor") : hasColor ? material.GetColor("_Color") : Color.white;
                material.shader = expected;
                if (hasBase || hasColor)
                {
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", original);
                    else if (material.HasProperty("_Color")) material.SetColor("_Color", original);
                }
                changed = true;
            }
            if (!material.enableInstancing)
            {
                material.enableInstancing = true;
                changed = true;
            }
            if (changed) EditorUtility.SetDirty(material);
            return material;
        }

        static void EnsureProjectMaterials()
        {
            EnsureMaterialAsset(Resource + "TerrainMaterial.mat", TerrainShaderName);
            EnsureMaterialAsset(Resource + "LitMaterial.mat", LitShaderName);
            var glow = EnsureMaterialAsset(Resource + "GlowMaterial.mat", LitShaderName);
            if (!glow.IsKeywordEnabled("_EMISSION"))
            {
                glow.EnableKeyword("_EMISSION");
                glow.SetColor("_EmissionColor", Color.white);
                EditorUtility.SetDirty(glow);
            }
            EnsureMaterialAsset(Resource + "WeatherMaterial.mat", WeatherShaderName);
            EnsureMaterialAsset(Resource + "ValleySkyMaterial.mat", SkyShaderName);
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Game/Art" }))
                EnsureMaterialAsset(AssetDatabase.GUIDToAssetPath(guid), LitShaderName);
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
                return index >= 0 && index < renderers.Length && renderers[index] is UniversalRendererData;
            }
        }

        static bool UsesRenderer(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer)
        {
            if (!HasDefaultRenderer(pipeline)) return false;
            using (var serialized = new SerializedObject(pipeline))
            {
                var selected = serialized.FindProperty("m_DefaultRendererIndex");
                return selected != null && pipeline.rendererDataList[selected.intValue] == renderer;
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
            else if (!HasDefaultRenderer(pipeline) || !UsesRenderer(pipeline, renderer))
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
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int previous = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0; i < QualitySettings.names.Length; i++)
                {
                    if (QualitySettings.GetRenderPipelineAssetAt(i) == pipeline)
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

        [MenuItem("Ecos/Reparar renderização URP")]
        public static void RepairRendering()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Pare o Play Mode antes de reparar a renderização.");
                return;
            }
            CreateDefaults();
            RefreshPipelineResources(Resource + "ValleyURP.asset", Resource + "ValleyRenderer.asset");
            EnsurePipeline();
            EnsureProjectMaterials();
            AssetDatabase.SaveAssets();
            Debug.Log("Ecos: recursos do pipeline e shaders dos materiais reparados. Confira os diagnósticos abaixo antes de testar Play.");
            DiagnoseRendering();
        }

        // Explicit repair for generated project resources; GUIDs remain unchanged.
        public static UniversalRenderPipelineAsset RefreshPipelineResources(string pipelinePath, string rendererPath)
        {
            var pipeline = EnsurePipelineAssets(pipelinePath, rendererPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            var freshRenderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            var freshPipeline = UniversalRenderPipelineAsset.Create(renderer);
            float renderScale = pipeline.renderScale;
            float shadowDistance = pipeline.shadowDistance;
            int samples = pipeline.msaaSampleCount;
            string rendererName = renderer.name;
            string pipelineName = pipeline.name;
            try
            {
                EditorUtility.CopySerialized(freshRenderer, renderer);
                renderer.name = rendererName;
                EditorUtility.SetDirty(renderer);
                EditorUtility.CopySerialized(freshPipeline, pipeline);
                pipeline.name = pipelineName;
                pipeline.renderScale = renderScale;
                pipeline.shadowDistance = shadowDistance;
                pipeline.msaaSampleCount = samples;
                EditorUtility.SetDirty(pipeline);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(freshPipeline);
                UnityEngine.Object.DestroyImmediate(freshRenderer);
            }
            return pipeline;
        }

        [MenuItem("Ecos/Diagnosticar renderização")]
        public static void DiagnoseRendering()
        {
            Debug.Log("Ecos renderização: ativo=" + PipelineDescription(GraphicsSettings.currentRenderPipeline)
                + "; Graphics=" + PipelineDescription(GraphicsSettings.defaultRenderPipeline)
                + "; qualidade=" + QualitySettings.names[QualitySettings.GetQualityLevel()]
                + " -> " + PipelineDescription(QualitySettings.renderPipeline));
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!pipeline)
                Debug.LogError("Ecos: o pipeline ativo não é URP. Pare Play e use Ecos > Reparar renderização URP.");
            else if (!HasDefaultRenderer(pipeline))
                Debug.LogError("Ecos: o URP ativo não possui renderer válido. Use Ecos > Reparar renderização URP.");
            foreach (string shaderName in new[] { TerrainShaderName, LitShaderName, WeatherShaderName, SkyShaderName })
                DiagnoseShader(shaderName);
        }

        static string PipelineDescription(RenderPipelineAsset pipeline)
        {
            return pipeline ? pipeline.name + " (" + pipeline.GetType().Name + ")" : "Built-in / nenhum";
        }

        static void DiagnoseShader(string name)
        {
            var shader = Shader.Find(name);
            if (!shader)
            {
                Debug.LogError("Ecos: shader não encontrado: " + name + ". Confira importação dos pacotes e o primeiro erro do Console.");
                return;
            }
            Debug.Log("Ecos shader: " + name + "; isSupported=" + shader.isSupported
                + "; arquivo=" + AssetDatabase.GetAssetPath(shader));
            // Supported status can lag a pipeline switch; report it, never hide the error
            // by swapping to a different shader or disabling shader validation.
            foreach (var message in ShaderUtil.GetShaderMessages(shader))
            {
                string text = "Ecos shader " + name + ": " + message.message
                    + " (" + message.file + ":" + message.line + ")";
                if (message.severity == ShaderCompilerMessageSeverity.Error) Debug.LogError(text);
                else if (message.severity == ShaderCompilerMessageSeverity.Warning) Debug.LogWarning(text);
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
            // Menu validation protects clicks; this also protects direct calls and transitions.
            if (!CanRunEditModeCommands())
            {
                Debug.LogWarning("Ecos: pare o Play Mode e aguarde o fim da compilação/importação antes de abrir a cena principal.");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static void BuildWindows()
        {
            CreateDefaults();
            string output = System.Environment.GetEnvironmentVariable("ECOS_BUILD_OUTPUT");
            if (string.IsNullOrEmpty(output))
                output = "Builds/Windows/EcosDaMeiaNoite.exe";
            var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, output, BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Build Windows falhou: " + result.summary.result);
        }
    }
}
