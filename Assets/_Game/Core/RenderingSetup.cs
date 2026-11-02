using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ecos
{
    /// <summary>The game's URP selection must also be correct when entering Play early.</summary>
    public static class RenderingSetup
    {
        public static bool TryApply(out string error)
        {
            var pipeline = Resources.Load<UniversalRenderPipelineAsset>("ValleyURP");
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
