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
        [Test]
        public void MaterialRepairPreservesAssetIdentityAndBaseColor()
        {
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            Assert.IsNotNull(unlit, "The imported URP package must supply its Unlit shader.");
            Assert.IsNotNull(lit, "The imported URP package must supply its Lit shader.");
            string path = folder + "/RepairMaterial.mat";
            var material = new Material(unlit);
            Color expectedColor = new Color(.17f, .41f, .29f, 1);
            material.SetColor("_BaseColor", expectedColor);
            AssetDatabase.CreateAsset(material, path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            var graphics = GraphicsSettings.defaultRenderPipeline;
            var quality = QualitySettings.renderPipeline;

            var repaired = ProjectSetup.EnsureMaterialAsset(path, "Universal Render Pipeline/Lit");

            Assert.AreSame(material, repaired);
            Assert.AreEqual(guid, AssetDatabase.AssetPathToGUID(path));
            Assert.AreSame(lit, repaired.shader);
            Assert.AreEqual(expectedColor, repaired.GetColor("_BaseColor"));
            Assert.IsTrue(repaired.enableInstancing);
            Assert.AreSame(repaired, ProjectSetup.EnsureMaterialAsset(path, "Universal Render Pipeline/Lit"));
            Assert.AreSame(graphics, GraphicsSettings.defaultRenderPipeline);
            Assert.AreSame(quality, QualitySettings.renderPipeline);
        }

        [Test]
        public void ExplicitResourceRefreshPreservesBothAssetGuidsAndQualityValues()
        {
            var pipeline = ProjectSetup.EnsurePipelineAssets(PipelinePath, RendererPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            string pipelineGuid = AssetDatabase.AssetPathToGUID(PipelinePath);
            string rendererGuid = AssetDatabase.AssetPathToGUID(RendererPath);
            pipeline.shadowDistance = 73;
            pipeline.renderScale = .85f;
            var graphics = GraphicsSettings.defaultRenderPipeline;
            var quality = QualitySettings.renderPipeline;

            var refreshed = ProjectSetup.RefreshPipelineResources(PipelinePath, RendererPath);

            Assert.AreSame(pipeline, refreshed);
            Assert.AreSame(renderer, refreshed.rendererDataList[0]);
            Assert.AreEqual(pipelineGuid, AssetDatabase.AssetPathToGUID(PipelinePath));
            Assert.AreEqual(rendererGuid, AssetDatabase.AssetPathToGUID(RendererPath));
            Assert.AreEqual(73, refreshed.shadowDistance);
            Assert.AreEqual(.85f, refreshed.renderScale);
            Assert.AreSame(graphics, GraphicsSettings.defaultRenderPipeline);
            Assert.AreSame(quality, QualitySettings.renderPipeline);
        }
    }
}
