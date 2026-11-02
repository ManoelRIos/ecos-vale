Shader "Ecos/Terrain" {
 Properties { }
 SubShader {
 Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
 Pass {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;half4 color:COLOR;};
 struct V {float4 positionCS:SV_POSITION;half4 color:COLOR;float3 normalWS:TEXCOORD0;float fog:TEXCOORD1;};
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.color=i.color;o.fog=ComputeFogFactor(o.positionCS.z);return o;}
 half4 frag(V i):SV_Target {Light l=GetMainLight();half3 c=i.color.rgb*(SampleSH(normalize(i.normalWS))+.22+saturate(dot(normalize(i.normalWS),l.direction))*l.color);return half4(MixFog(c,i.fog),1);}
 ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
 }
}
