// Project Tyrant – stilisierter Toon-Shader mit Rim-Light, kühlen Schatten und Painterly-Variation.
Shader "ProjectTyrant/ToonRim"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _AlbedoMap ("Albedo (UV, z. B. Meshy-Textur)", 2D) = "white" {}
        _BaseMap ("Painterly Map (Graustufen-Pinselstruktur)", 2D) = "white" {}
        _PaintStrength ("Painterly Strength", Range(0,1)) = 0.25
        _WorldUVScale ("World UV Scale (Triplanar)", Float) = 0.15
        _ShadowColor ("Shadow Tint (kühl)", Color) = (0.45,0.5,0.8,1)
        _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = 0.05
        _ShadowSoftness ("Shadow Softness", Range(0.001,1)) = 0.12
        _MidTone ("Mid-Tone Band", Range(0,1)) = 0.55
        _RimColor ("Rim Color", Color) = (1,0.88,0.65,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.0
        _RimStrength ("Rim Strength", Range(0,2)) = 0.6
        _HeightGradient ("Height Gradient Strength", Range(0,1)) = 0.15
        _GradientBottom ("Gradient Bottom Tint", Color) = (0.75,0.7,0.9,1)
        _EmissionColor ("Emission", Color) = (0,0,0,1)
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 1
        _Wind ("Wind (Vertex-Animation, Lauf 5)", Range(0,1)) = 0
        _WindPivot ("Wind-Aufhängung (0 = Fuß unten, >0 = oben hängend, z. B. Banner)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _PaintStrength;
                float _WorldUVScale;
                float4 _ShadowColor;
                float _ShadowThreshold;
                float _ShadowSoftness;
                float _MidTone;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
                float _HeightGradient;
                float4 _GradientBottom;
                float4 _EmissionColor;
                float _UseVertexColor;
                float4 _AlbedoMap_ST;
                float _Wind;
                float _WindPivot;
            CBUFFER_END

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            float4 _PT_Wind;   // global: xy Richtung (normiert), z Stärke (m), w frei
            TEXTURE2D(_AlbedoMap); SAMPLER(sampler_AlbedoMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float objectY : TEXCOORD4;
                float2 uv : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                // Lauf 5 / M23: Wind (Laub, Gras, Banner) – Auslenkung wächst mit der Objekthöhe, Richtung/Stärke global je Biom
                float3 posOS = v.positionOS.xyz;
                if (_Wind > 0.001)
                {
                    float3 wpos = TransformObjectToWorld(posOS);
                    float h = _WindPivot > 0 ? saturate((_WindPivot - posOS.y) * 0.8) : saturate(posOS.y * 0.35);
                    float gust = sin(_Time.y * 1.7 + dot(wpos.xz, float2(0.13, 0.21))) * 0.6 + sin(_Time.y * 3.9 + wpos.x * 0.7) * 0.25;
                    float3 dirW = float3(_PT_Wind.x, 0, _PT_Wind.y) * (_PT_Wind.z * (0.6 + gust)) * _Wind * h;
                    posOS += mul((float3x3)GetWorldToObjectMatrix(), dirW);
                }
                VertexPositionInputs pos = GetVertexPositionInputs(posOS);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color;
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                o.objectY = v.positionOS.y;
                o.uv = TRANSFORM_TEX(v.uv, _AlbedoMap);
                return o;
            }

            half3 ToonShade(Light light, float3 n, half3 albedo)
            {
                float ndl = dot(n, light.direction);
                float atten = light.shadowAttenuation * light.distanceAttenuation;
                float lit = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, ndl);
                float band = smoothstep(_MidTone - 0.05, _MidTone + 0.05, ndl) * 0.15;
                lit = saturate(lit * smoothstep(0.2, 0.6, light.shadowAttenuation) + band * lit) * light.distanceAttenuation;
                half3 shadowCol = albedo * _ShadowColor.rgb;
                return lerp(shadowCol * 0.85, albedo * light.color, lit);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                half3 albedo = _BaseColor.rgb;
                if (_UseVertexColor > 0.5) albedo *= lerp(half3(1,1,1), i.color.rgb, i.color.a);
                albedo *= SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, i.uv).rgb;

                // Painterly: Triplanar-Pinselstruktur in Weltkoordinaten
                float3 bw = pow(abs(n), 4); bw /= (bw.x + bw.y + bw.z + 1e-4);
                float2 uvx = i.positionWS.zy * _WorldUVScale;
                float2 uvy = i.positionWS.xz * _WorldUVScale;
                float2 uvz = i.positionWS.xy * _WorldUVScale;
                half paint = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvx).r * bw.x
                           + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvy).r * bw.y
                           + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvz).r * bw.z;
                albedo *= lerp(1.0, 0.75 + paint * 0.5, _PaintStrength);

                // Höhen-Gradient (Objektraum): unten leicht kühler
                float g = saturate(i.objectY * 0.5 + 0.5);
                albedo *= lerp(lerp(half3(1,1,1), _GradientBottom.rgb, _HeightGradient), half3(1,1,1), g);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half3 col = ToonShade(mainLight, n, albedo);

                // Ambient (SH) für weiche Füllung
                col += albedo * SampleSH(n) * 0.35;

                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0; li < count; li++)
                {
                    Light l = GetAdditionalLight(li, i.positionWS, half4(1,1,1,1));
                    col += albedo * l.color * l.distanceAttenuation * smoothstep(0.0, 0.3, dot(n, l.direction)) * 0.8;
                }
                #endif

                // Rim-Light
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float rim = pow(1.0 - saturate(dot(viewDir, n)), _RimPower);
                float rimMask = saturate(dot(n, mainLight.direction) * 0.5 + 0.6);
                col += _RimColor.rgb * rim * _RimStrength * rimMask;

                col += _EmissionColor.rgb;
                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back

            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _PaintStrength;
                float _WorldUVScale;
                float4 _ShadowColor;
                float _ShadowThreshold;
                float _ShadowSoftness;
                float _MidTone;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
                float _HeightGradient;
                float4 _GradientBottom;
                float4 _EmissionColor;
                float _UseVertexColor;
                float4 _AlbedoMap_ST;
                float _Wind;
                float _WindPivot;
            CBUFFER_END

            float3 _LightDirection;
            float3 _LightPosition;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };

            V vertShadow(A v)
            {
                V o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs;
                return o;
            }
            half4 fragShadow(V i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _PaintStrength;
                float _WorldUVScale;
                float4 _ShadowColor;
                float _ShadowThreshold;
                float _ShadowSoftness;
                float _MidTone;
                float4 _RimColor;
                float _RimPower;
                float _RimStrength;
                float _HeightGradient;
                float4 _GradientBottom;
                float4 _EmissionColor;
                float _UseVertexColor;
                float4 _AlbedoMap_ST;
                float _Wind;
                float _WindPivot;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vertDepth(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half fragDepth(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
