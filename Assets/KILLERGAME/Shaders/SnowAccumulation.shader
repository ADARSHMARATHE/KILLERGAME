Shader "KillerGame/SnowAccumulation"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.35, 0.28, 0.22, 1)
        _SnowColor ("Snow Color", Color) = (0.90, 0.94, 0.98, 1)
        _SnowThreshold ("Snow Threshold", Range(-1, 1)) = 0.35
        _SnowBlend ("Snow Blend Sharpness", Range(0.01, 0.5)) = 0.12
        _FresnelPower ("Rim Intensity", Range(0, 3)) = 1.2
        _Warmth ("Warm Light Boost", Range(0, 2)) = 0.35
        _EmissionColor ("Window Glow Color", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SnowColor;
                float  _SnowThreshold;
                float  _SnowBlend;
                float  _FresnelPower;
                float  _Warmth;
                float4 _EmissionColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS   = GetWorldSpaceViewDir(posInputs.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS  = normalize(IN.normalWS);
                float3 viewDir   = normalize(IN.viewDirWS);

                float snowFactor = saturate((normalWS.y - _SnowThreshold) / max(_SnowBlend, 0.001));
                float3 albedo = lerp(_BaseColor.rgb, _SnowColor.rgb, snowFactor);

                Light mainLight = GetMainLight();
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 ambient = unity_AmbientSky.rgb * 0.85;
                float3 warm = float3(1.0, 0.75, 0.45) * _Warmth * NdotL;
                float3 diffuse = mainLight.color * NdotL * 0.95;
                float3 lighting = ambient + diffuse + warm;

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower);
                float3 rimColor = float3(0.55, 0.72, 0.95) * fresnel * 0.45;

                float windowGlow = saturate(1.0 - abs(normalWS.y)) * 0.15;
                float3 windows = _EmissionColor.rgb * windowGlow;

                float3 finalColor = albedo * lighting + rimColor + _EmissionColor.rgb + windows;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
