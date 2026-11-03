using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Ecos.Haunt.Editor;

namespace Ecos.Haunt.Tests
{
    public sealed class HauntRenderingTests
    {
        string folder, pipelinePath, rendererPath;

        [SetUp]
        public void CreateIsolatedFolder()
        {
            folder = "Assets/HauntRenderingTests-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
            pipelinePath = folder + "/Pipeline.asset";
            rendererPath = folder + "/Renderer.asset";
        }

        [TearDown]
        public void RemoveIsolatedFolder()
        {
            if (!string.IsNullOrEmpty(folder)) AssetDatabase.DeleteAsset(folder);
        }

        [Test]
        public void PreparingAndRepairingAssetsKeepsReferencesAndGlobalSettings()
        {
            var graphics = GraphicsSettings.defaultRenderPipeline;
            var quality = QualitySettings.renderPipeline;
            int level = QualitySettings.GetQualityLevel();
            var pipeline = HauntSetup.EnsurePipelineAssets(pipelinePath, rendererPath);
            string pipelineGuid = AssetDatabase.AssetPathToGUID(pipelinePath);
            string rendererGuid = AssetDatabase.AssetPathToGUID(rendererPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            Assert.IsNotNull(renderer);
            Assert.AreSame(renderer, pipeline.rendererDataList[0]);
            Assert.AreSame(pipeline, HauntSetup.EnsurePipelineAssets(pipelinePath, rendererPath));

            // Simulate a broken renderer selection left by an interrupted import.
            using (var serialized = new SerializedObject(pipeline))
            {
                var selected = serialized.FindProperty("m_DefaultRendererIndex");
                Assert.IsNotNull(selected);
                selected.intValue = 999;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.AreSame(pipeline, HauntSetup.EnsurePipelineAssets(pipelinePath, rendererPath));
            using (var serialized = new SerializedObject(pipeline))
                Assert.AreEqual(0, serialized.FindProperty("m_DefaultRendererIndex").intValue);
            Assert.AreSame(renderer, pipeline.rendererDataList[0]);
            Assert.AreEqual(pipelineGuid, AssetDatabase.AssetPathToGUID(pipelinePath));
            Assert.AreEqual(rendererGuid, AssetDatabase.AssetPathToGUID(rendererPath));
            Assert.AreSame(graphics, GraphicsSettings.defaultRenderPipeline);
            Assert.AreSame(quality, QualitySettings.renderPipeline);
            Assert.AreEqual(level, QualitySettings.GetQualityLevel());
        }

        [Test]
        public void ExplicitResourceRepairKeepsGuidsAndPixelRenderingDefaults()
        {
            var pipeline = HauntSetup.EnsurePipelineAssets(pipelinePath, rendererPath);
            string pipelineGuid = AssetDatabase.AssetPathToGUID(pipelinePath);
            string rendererGuid = AssetDatabase.AssetPathToGUID(rendererPath);
            pipeline.renderScale = .5f;
            pipeline.msaaSampleCount = 4;
            pipeline.shadowDistance = 50;
            var repaired = HauntSetup.RefreshPipelineResources(pipelinePath, rendererPath);
            Assert.AreSame(pipeline, repaired);
            Assert.AreEqual(pipelineGuid, AssetDatabase.AssetPathToGUID(pipelinePath));
            Assert.AreEqual(rendererGuid, AssetDatabase.AssetPathToGUID(rendererPath));
            Assert.AreEqual(1, repaired.renderScale);
            Assert.AreEqual(1, repaired.msaaSampleCount);
            Assert.AreEqual(0, repaired.shadowDistance);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath), repaired.rendererDataList[0]);
        }
    }
}
