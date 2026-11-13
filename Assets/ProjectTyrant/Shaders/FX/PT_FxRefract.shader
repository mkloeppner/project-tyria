// Project Tyrant – VFX-Lauf V1: Brechungs-Quelle für den gemeinsamen Verzerrungs-Puffer (DistortionAccumulator).
// Zeichnet nichts Sichtbares: Pass "PTDistort" schreibt additiv Normalen-Verschiebung (rg, UV), Dispersion (b) und Deckung (a)
// in _PT_DistortBuffer; ein einziger Blit (PT_PostFx Pass 3) wendet den Puffer mit RGB-Trennung an.
// Regel 8: stark am Rand, schwach in der Mitte (Kantenfaktor 1 − |N·V|), Tiefen-Test gegen Opaque von Hand (Brechung hinter Wänden verworfen).
Shader "ProjectTyrant/FX/Refract"
{
    Properties
    {
        _Strength ("Verschiebung (px bei 1080p, ≤ 24)", Range(0, 24)) = 12
        _Dispersion ("Dispersion (0–1, ≤ 6 px)", Range(0, 1)) = 0.3
        _EdgePower ("Kantenbetonung", Range(0.5, 6)) = 2
        _Center ("Mitte (Anteil 0–1)", Range(0, 1)) = 0.15
        _NoiseScale ("Wellen-Skala", Float) = 3
        _NoiseSpeed ("Wellen-Tempo", Float) = 0.4
        _NoiseAmount ("Wellen-Anteil", Range(0, 1)) = 0
        [NoScaleOffset] _NoiseTex ("Rauschen", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "PTDistort"
            Tags { "LightMode"="PTDistort" }
            Blend One One
            ZWrite Off ZTest Always Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength, _Dispersion, _EdgePower, _Center, _NoiseScale, _NoiseSpeed, _NoiseAmount;
            CBUFFER_END
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            float _PT_DistortScale;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 normalVS : TEXCOORD0; float3 viewVS : TEXCOORD1; float2 uv : TEXCOORD2; float eye : TEXCOORD3; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };

            V Vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                float3 vs = TransformWorldToView(ws);
                o.positionCS = TransformWorldToHClip(ws);
                o.normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(v.normalOS), true);
                o.viewVS = normalize(-vs); o.eye = -vs.z; o.uv = v.uv; o.color = v.color;
                return o;
            }

            float4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float2 suv = i.positionCS.xy / _ScaledScreenParams.xy;
                float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                if (i.eye > scene + 0.05) discard;   // hinter einer Wand: keine Brechung
                float3 n = normalize(i.normalVS);
                if (_NoiseAmount > 0)
                {
                    float2 w = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv * _NoiseScale + _Time.y * _NoiseSpeed).rg - 0.5;
                    n = normalize(n + float3(w * 2.0 * _NoiseAmount, 0));
                }
                float edge = pow(saturate(1.0 - abs(dot(n, normalize(i.viewVS)))), _EdgePower);
                float k = lerp(_Center, 1.0, edge) * i.color.a;
                float2 px = n.xy * _Strength * k * _PT_DistortScale;   // Pixel bei 1080p
                float2 off = px / 1080.0 * float2(_ScreenParams.y / _ScreenParams.x, 1.0);
                return float4(off, _Dispersion * k, k);
            }
            ENDHLSL
        }
    }
}
