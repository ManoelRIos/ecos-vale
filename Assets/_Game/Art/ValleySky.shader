Shader "Ecos/ValleySky"
{
    Properties
    {
        _ZenithColor ("Zenith", Color) = (0.12, 0.25, 0.4, 1)
        _HorizonColor ("Horizon", Color) = (0.6, 0.55, 0.49, 1)
        _GroundColor ("Below horizon", Color) = (0.12, 0.17, 0.18, 1)
        _SunColor ("Sun", Color) = (1, 0.75, 0.38, 1)
        _SunDirection ("Sun direction", Vector) = (0, 1, 0, 0)
        _MoonDirection ("Moon direction", Vector) = (0, -1, 0, 0)
        _Daylight ("Daylight", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _SunColor;
                float4 _SunDirection;
                float4 _MoonDirection;
                half _Daylight;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                half elevation = saturate(direction.y);
                half3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(elevation, 0.55));
                color = lerp(color, _GroundColor.rgb, saturate(-direction.y * 5));
                half sunDot = saturate(dot(direction, normalize(_SunDirection.xyz)));
                half moonDot = saturate(dot(direction, normalize(_MoonDirection.xyz)));
                color += _SunColor.rgb * (pow(sunDot, 28) * 0.25 + smoothstep(0.9984, 0.9994, sunDot) * 1.8) * _Daylight;
                color += half3(0.46, 0.59, 0.9) * (pow(moonDot, 60) * 0.10 + smoothstep(0.9987, 0.9995, moonDot) * 0.8) * (1 - _Daylight);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
