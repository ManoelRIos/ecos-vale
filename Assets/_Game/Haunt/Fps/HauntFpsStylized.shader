// Stylized lighting for the first-person dungeon: texel relief from a normal map (no mesh
// tangents needed), soft banded diffuse, small glints, a rim light and ground-contact shade.
// HauntFpsLit stays as the plain fallback; HauntFpsGame.LitTemplate picks this shader only
// when it compiled without errors.
Shader "Ecos/HauntFpsStylized"
{
    Properties
    {
        [MainColor] _BaseColor ("Pixel tint", Color) = (1,1,1,1)
        [MainTexture] _BaseMap ("Pixel texture", 2D) = "white" {}
        [HDR] _EmissionColor ("Spectral emission", Color) = (0,0,0,1)
        [NoScaleOffset] _ReliefMap ("Relief normal map", 2D) = "bump" {}
        _ReliefScale ("Relief strength", Range(0,2)) = 0
        _Gloss ("Gloss", Range(0,1)) = 0
        _GlintColor ("Glint tint", Color) = (1,1,1,1)
        _RimColor ("Rim light", Color) = (0,0,0,1)
        _RimPower ("Rim sharpness", Range(0.5,8)) = 3
        _Banding ("Light banding", Range(0,1)) = 0.35
        _ContactShade ("Ground contact shade", Range(0,1)) = 0
        _ContactHeight ("Ground contact height", Float) = 1.2
        [PerRendererData] _HitFlash ("Hit flash", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Back
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_ReliefMap);
        SAMPLER(sampler_ReliefMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half4 _GlintColor;
            half4 _RimColor;
            half _ReliefScale;
            half _Gloss;
            half _RimPower;
            half _Banding;
            half _ContactShade;
            half _ContactHeight;
            half _HitFlash;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "StylizedForward"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PixelVertex
            #pragma fragment PixelFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct PixelAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct PixelVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                half3 vertexLighting : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            PixelVaryings PixelVertex(PixelAttributes input)
            {
                PixelVaryings output = (PixelVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                output.vertexLighting = VertexLighting(positions.positionWS, output.normalWS);
                output.shadowCoord = GetShadowCoord(positions);
                return output;
            }

            // Tangent frame from screen-space derivatives, so plain box meshes need no tangents.
            // The handedness term keeps it correct whichever way the platform flips ddy.
            half3 ReliefNormal(half3 faceNormal, float3 positionWS, float2 uv)
            {
                half3 relief = SAMPLE_TEXTURE2D(_ReliefMap, sampler_ReliefMap, uv).xyz * 2.0h - 1.0h;
                relief.xy *= _ReliefScale;
                float3 n = float3(faceNormal);
                float3 dp1 = ddx(positionWS);
                float3 dp2 = ddy(positionWS);
                float2 duv1 = ddx(uv);
                float2 duv2 = ddy(uv);
                float3 dp2perp = cross(dp2, n);
                float3 dp1perp = cross(n, dp1);
                float3 tangent = dp2perp * duv1.x + dp1perp * duv2.x;
                float3 bitangent = dp2perp * duv1.y + dp1perp * duv2.y;
                float handed = dot(dp1, dp2perp) < 0.0 ? -1.0 : 1.0;
                float longest = max(max(dot(tangent, tangent), dot(bitangent, bitangent)), 1e-12);
                float frame = handed * rsqrt(longest);
                float3 bent = tangent * (frame * relief.x) + bitangent * (frame * relief.y) + n * max(relief.z, 0.05);
                return half3(normalize(bent));
            }

            // Diffuse blends smooth shading with four soft bands; the glint is a small
            // Blinn-Phong highlight that only glossy materials (iron, gold, polished stone) show.
            void StylizedLight(Light light, half3 normalWS, half3 viewWS, inout half3 diffuse, inout half3 glint)
            {
                half ndl = saturate(dot(normalWS, light.direction));
                half stepped = floor(ndl * 4.0h + 0.5h) * 0.25h;
                half shade = lerp(ndl, stepped, _Banding);
                half3 radiance = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                diffuse += radiance * shade;
                float3 halfway = SafeNormalize(float3(light.direction) + float3(viewWS));
                half ndh = saturate(dot(float3(normalWS), halfway));
                half highlight = pow(max(ndh, 0.0001h), 8.0h + _Gloss * 56.0h);
                glint += radiance * (highlight * _Gloss * saturate(ndl * 4.0h));
            }

            half4 PixelFragment(PixelVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half3 faceNormal = NormalizeNormalPerPixel(input.normalWS);
                half3 normalWS = ReliefNormal(faceNormal, input.positionWS, input.uv);
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                // InputData is also the contract consumed by URP's Forward+
                // LIGHT_LOOP_BEGIN macro for clustered screen/world positions.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    inputData.shadowCoord = input.shadowCoord;
                #else
                    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                #endif

                half3 diffuse = max(SampleSH(normalWS), half3(.055h, .045h, .075h));
                half3 glint = half3(0.0h, 0.0h, 0.0h);
                Light mainLight = GetMainLight(inputData.shadowCoord);
                StylizedLight(mainLight, normalWS, viewWS, diffuse, glint);

                #if defined(_ADDITIONAL_LIGHTS) || USE_FORWARD_PLUS
                    // Forward+ keeps non-main directional lights outside the
                    // clustered punctual loop. Both loops are required there.
                    #if USE_FORWARD_PLUS
                        UNITY_LOOP for (uint lightIndex = 0u; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
                        {
                            Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                            StylizedLight(light, normalWS, viewWS, diffuse, glint);
                        }
                    #endif
                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                        StylizedLight(light, normalWS, viewWS, diffuse, glint);
                    LIGHT_LOOP_END
                #elif defined(_ADDITIONAL_LIGHTS_VERTEX)
                    diffuse += input.vertexLighting;
                #endif

                // Things standing on the floor darken toward their base, a cheap contact shadow.
                half contact = saturate(half(input.positionWS.y) / max(_ContactHeight, 0.01h));
                half grounding = lerp(1.0h, 0.45h + 0.55h * contact, _ContactShade);
                half rim = pow(max(1.0h - saturate(dot(normalWS, viewWS)), 0.0001h), _RimPower);

                half3 color = (albedo * diffuse + glint * _GlintColor.rgb) * grounding
                    + _RimColor.rgb * rim + max(_EmissionColor.rgb, half3(0,0,0)) * .65h;
                color = lerp(color, half3(1.5h,1.5h,1.5h), saturate(_HitFlash));
                return half4(MixFog(color, input.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVertex
            #pragma fragment ShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct ShadowAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct ShadowVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ShadowVaryings ShadowVertex(ShadowAttributes input)
            {
                ShadowVaryings output = (ShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE * output.positionCS.w);
                #endif
                return output;
            }

            half4 ShadowFragment(ShadowVaryings input) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings DepthVertex(DepthAttributes input)
            {
                DepthVaryings output = (DepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half DepthFragment(DepthVaryings input) : SV_Target { return input.positionCS.z; }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
