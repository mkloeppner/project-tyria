// Project Tyrant – Gefahren-Oberfläche (Lava-, Eis-, Giftwasser, Treibsand, Verderbnis): fließendes Muster je Art, Rand mit wandernden Warnstreifen; Lesbarkeit vor Schönheit.
// Erzeugt aus der Toon-Familien-Vorlage (Grafik-Lauf G3, gemeinsamer Forward-Pass in PT_ToonForward.hlsl). Eigenschaften der Familie wie ToonLit.
Shader "ProjectTyrant/Hazard"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color", Color) = (1,1,1,1)
        [MainTexture] _AlbedoMap ("Albedo (UV)", 2D) = "white" {}
        _BaseMap ("Painterly Map", 2D) = "white" {}
        _PaintStrength ("Painterly Strength", Range(0,1)) = 0.25
        _WorldUVScale ("World UV Scale (Triplanar)", Float) = 0.15
        _ShadowColor ("Shadow Tint", Color) = (0.45,0.5,0.8,1)
        _ShadowThreshold ("Shadow Threshold", Range(-1,1)) = 0.05
        _ShadowSoftness ("Shadow Softness", Range(0.001,1)) = 0.12
        _MidTone ("Mid-Tone Band", Range(0,1)) = 0.55
        _RimColor ("Rim Color", Color) = (1,0.88,0.65,1)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.0
        _RimStrength ("Rim Strength", Range(0,2)) = 0.6
        _HeightGradient ("Height Gradient Strength", Range(0,1)) = 0.15
        _GradientBottom ("Gradient Bottom Tint", Color) = (0.75,0.7,0.9,1)
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)
        _NightEmission ("Emission nur nachts (Laternen, Fenster)", Range(0,1)) = 0
        [Toggle] _UseVertexColor ("Use Vertex Color", Float) = 1
        _Wind ("Wind", Range(0,1)) = 0
        _WindPivot ("Wind Pivot (0 = Fuß, >0 = hängend)", Float) = 0
        _WeatherMask ("Wetter (Nässe, Schnee)", Range(0,1)) = 1
        _Kind ("Art (0 Lava, 1 Eis, 2 Gift, 3 Treibsand, 4 Verderbnis)", Float) = 0
        _Edge ("Randstreifen", Range(0,1)) = 0
        _PatternTex ("Muster (Worley)", 2D) = "gray" {}
        _PatternScale ("Muster-Maßstab", Float) = 0.15
        _FlowSpeed ("Fluss-Tempo", Float) = 0.04
        _StripeColor ("Streifen", Color) = (0.08,0.06,0.05,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Back

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"
        CBUFFER_START(UnityPerMaterial)
            PT_TOON_PROPS
            float _Kind; float _Edge; float _PatternScale; float _FlowSpeed; float4 _StripeColor; float4 _PatternTex_ST;
        CBUFFER_END
        TEXTURE2D(_PatternTex); SAMPLER(sampler_PatternTex);
        ENDHLSL

        Pass
        {
            Name "ForwardToon"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex PTToonVert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_instancing
            #define PT_SURFACE_HOOK
            #include "Assets/ProjectTyrant/Shaders/Include/PT_ToonForward.hlsl"
            void PT_Surface(inout PTSurface s, PTVaryings i)
            {
                float2 uv = i.positionWS.xz * _PatternScale;
                float t = _Time.y * _FlowSpeed;
                half a = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, uv + float2(t, t * 0.6)).r;
                half b = SAMPLE_TEXTURE2D(_PatternTex, sampler_PatternTex, uv * 2.1 - float2(t * 0.7, -t)).r;
                half w = a * 0.6 + b * 0.4;
                half k = _Kind;
                // Lava/Verderbnis: helle Adern; Eis: Risse; Gift: Blasen; Treibsand: Wirbel
                half veins = smoothstep(0.42, 0.3, w);
                half bubbles = smoothstep(0.75, 0.85, b) * step(1.5, k) * step(k, 2.5);
                half swirl = sin(length(uv) * 6.0 - _Time.y * 0.8 + w * 4.0) * 0.5 + 0.5;
                half3 c = s.albedo;
                c *= 0.75 + 0.5 * w;
                if (k > 2.5 && k < 3.5) c *= 0.85 + 0.3 * swirl;
                s.albedo = c;
                s.emission *= 0.6 + 0.8 * veins + bubbles * 1.5;
                if (k < 0.5 || k > 3.5) s.emission += s.albedo * veins * 0.6;
                if (_Edge > 0.5)
                {
                    half stripe = step(0.5, frac((i.positionWS.x + i.positionWS.z) * 0.6 + _Time.y * 0.4));
                    s.albedo = lerp(s.albedo, _StripeColor.rgb, stripe * 0.65);
                    s.emission *= 1.0 - stripe * 0.6;
                }
                s.shadowTint = 0.6;
            }

            half4 frag(PTVaryings i, bool ff : SV_IsFrontFace) : SV_Target { return PTToonFrag(i, ff); }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"
            float3 _LightDirection;
            float3 _LightPosition;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vertShadow(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v);
                float3 posWS = TransformObjectToWorld(PT_ApplyWind(v.positionOS.xyz, _Wind, _WindPivot));
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
                o.positionCS = cs; return o;
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
            #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vertDepth(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformObjectToHClip(PT_ApplyWind(v.positionOS.xyz, _Wind, _WindPivot)); return o; }
            half fragDepth(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vertDN
            #pragma fragment fragDN
            #pragma multi_compile_instancing
            #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };
            V vertDN(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformObjectToHClip(PT_ApplyWind(v.positionOS.xyz, _Wind, _WindPivot)); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 fragDN(V i, bool ff : SV_IsFrontFace) : SV_Target { return half4(NormalizeNormalPerPixel(i.normalWS * (ff ? 1.0 : -1.0)), 0); }
            ENDHLSL
        }
    }
    FallBack Off
}
