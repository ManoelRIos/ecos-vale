using System;
using Ecos.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ecos.Tests
{
    public sealed class ProjectSetupTests
    {
        string folder;
        string PipelinePath => folder + "/Pipeline.asset";
        string RendererPath => folder + "/Renderer.asset";

        [SetUp]
        public void CreateTemporaryFolder()
        {
            string name = "EcosSetupTests-" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name);
            folder = "Assets/" + name;
        }

        [TearDown]
        public void RemoveTemporaryAssets()
        {
            if (!string.IsNullOrEmpty(folder))
                AssetDatabase.DeleteAsset(folder);
        }

        [Test]
        public void PipelineFactoryIsIdempotentAndPreservesValidSettings()
        {
            var graphics = GraphicsSettings.defaultRenderPipeline;
            var quality = QualitySettings.renderPipeline;
            var pipeline = ProjectSetup.EnsurePipelineAssets(PipelinePath, RendererPath);
            string guid = AssetDatabase.AssetPathToGUID(PipelinePath);
            pipeline.shadowDistance = 37;
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();

            var again = ProjectSetup.EnsurePipelineAssets(PipelinePath, RendererPath);

            Assert.AreSame(pipeline, again);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(PipelinePath));
            Assert.AreEqual(37, again.shadowDistance);
            Assert.AreEqual(1, again.rendererDataList.Length);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath), again.rendererDataList[0]);
            Assert.AreSame(graphics, GraphicsSettings.defaultRenderPipeline);
            Assert.AreSame(quality, QualitySettings.renderPipeline);
        }

        [Test]
        public void MissingRendererIsRepairedWithoutReplacingPipelineAsset()
        {
            var graphics = GraphicsSettings.defaultRenderPipeline;
            var quality = QualitySettings.renderPipeline;
            var invalid = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(invalid, PipelinePath);
            string guid = AssetDatabase.AssetPathToGUID(PipelinePath);

            var repaired = ProjectSetup.EnsurePipelineAssets(PipelinePath, RendererPath);

            Assert.AreSame(invalid, repaired);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(PipelinePath));
            Assert.AreEqual(1, repaired.rendererDataList.Length);
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath), repaired.rendererDataList[0]);
            Assert.AreEqual(90, repaired.shadowDistance);
            Assert.AreSame(repaired, ProjectSetup.EnsurePipelineAssets(PipelinePath, RendererPath));
            Assert.AreSame(graphics, GraphicsSettings.defaultRenderPipeline);
            Assert.AreSame(quality, QualitySettings.renderPipeline);
        }
    }
}
