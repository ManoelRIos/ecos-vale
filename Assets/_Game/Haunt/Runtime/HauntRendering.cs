using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ecos.Haunt
{
    /// <summary>Select the roguelike's renderer when entering Play or starting a build.</summary>
    public static class HauntRendering
    {
        public static bool TryApply(out string error)
        {
            var pipeline = Resources.Load<UniversalRenderPipelineAsset>("HauntURP");
            if (!pipeline || pipeline.rendererDataList.Length == 0 || !pipeline.rendererDataList[0])
            {
                error = "Renderização não preparada. Pare o Play e use Ecos > Reparar renderização URP.";
                return false;
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            error = null;
            return true;
        }
    }
}
