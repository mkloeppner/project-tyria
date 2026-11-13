// Project Tyrant – VFX-Lauf V1: Simulations-Flipbook mit Bewegungsvektor-Überblendung (Regel 5: kein sichtbares Bildspringen),
// Normalen-Beleuchtung durch das Hauptlicht (beleuchteter Rauch, Wasser-Relief), Emissiv-Kern (HDR ≥ 2, nur der Kern erreicht Bloom),
// Soft-Particles. Vormultipliziertes Alpha: Rauch/Wasser deckend, Feuer über _Additive (Alpha → 0) additiv.
// Zeit: Partikel-Alter aus dem Shuriken-Vertex-Stream (TEXCOORD0.z, AgePercent) oder Laufzeit (_StartTime) für Meshes.
Shader "ProjectTyrant/FX/Flipbook"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Farbe (Atlas)", 2D) = "white" {}
        [NoScaleOffset] _NormalTex ("Normalen (Atlas)", 2D) = "bump" {}
        [NoScaleOffset] _MotionTex ("Bewegung (Atlas, halbe Auflösung)", 2D) = "gray" {}
        [NoScaleOffset] _EmissiveTex ("Emissiv-Kern (Atlas)", 2D) = "black" {}
        _Columns ("Spalten", Float) = 8
        _Rows ("Zeilen", Float) = 8
        _Fps ("Wiedergabe (Bilder/s, ≥ 24)", Float) = 48
        _Loop ("Schleife", Float) = 1
        _UseAge ("Partikel-Alter nutzen", Float) = 0
        _StartTime ("Startzeit (Mesh)", Float) = 0
        _MotionStrength ("Bewegungsvektor-Stärke", Range(0, 0.1)) = 0.02
        [HDR] _Tint ("Körperfarbe", Color) = (1, 1, 1, 1)
        _CoreIntensity ("Kern-HDR", Range(0, 8)) = 2.5
        _Lighting ("Licht-Anteil", Range(0, 1)) = 0.6
        _Additive ("Additiv (Feuer)", Range(0, 1)) = 0
        _SoftDistance ("Soft-Particles (m)", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Columns, _Rows, _Fps, _Loop, _UseAge, _StartTime, _MotionStrength, _CoreIntensity, _Lighting, _Additive, _SoftDistance;
                float4 _Tint;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex); TEXTURE2D(_MotionTex); TEXTURE2D(_EmissiveTex);
            float _PT_HasOpaque;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float4 tangentOS : TANGENT; float4 color : COLOR; float4 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 uvAge : TEXCOORD0; float4 color : COLOR; float3 normalWS : TEXCOORD1; float3 tangentWS : TEXCOORD2; float3 bitangentWS : TEXCOORD3; float eye : TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID };

            V Vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws); o.eye = -TransformWorldToView(ws).z;
                o.uvAge = float3(v.uv.xy, v.uv.z); o.color = v.color;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * (v.tangentOS.w >= 0 ? 1 : -1);
                return o;
            }

            float2 TileUv(float2 uv, float frame)
            {
                float col = fmod(frame, _Columns), row = floor(frame / _Columns);
                return (uv + float2(col, _Rows - 1 - row)) / float2(_Columns, _Rows);
            }

            half4 Frag(V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float frames = _Columns * _Rows;
                float pos = _UseAge > 0.5 ? saturate(i.uvAge.z) * (frames - 1) : (_Time.y - _StartTime) * _Fps;
                pos = _Loop > 0.5 ? fmod(max(pos, 0), frames) : clamp(pos, 0, frames - 1);
                float f0 = floor(pos), f1 = _Loop > 0.5 ? fmod(f0 + 1, frames) : min(f0 + 1, frames - 1), t = pos - f0;
                float2 uv = saturate(i.uvAge.xy);
                // Bewegungsvektoren: Bild A vorwärts, Bild B rückwärts verschoben, dann überblenden
                float2 mv0 = SAMPLE_TEXTURE2D(_MotionTex, sampler_MainTex, TileUv(uv, f0)).rg * 2 - 1;
                float2 mv1 = SAMPLE_TEXTURE2D(_MotionTex, sampler_MainTex, TileUv(uv, f1)).rg * 2 - 1;
                float2 uv0 = TileUv(saturate(uv - mv0 * t * _MotionStrength), f0);
                float2 uv1 = TileUv(saturate(uv + mv1 * (1 - t) * _MotionStrength), f1);
                half4 c = lerp(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv0), SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv1), t);
                half3 nT = lerp(SAMPLE_TEXTURE2D(_NormalTex, sampler_MainTex, uv0).xyz, SAMPLE_TEXTURE2D(_NormalTex, sampler_MainTex, uv1).xyz, t) * 2 - 1;
                half core = lerp(SAMPLE_TEXTURE2D(_EmissiveTex, sampler_MainTex, uv0).r, SAMPLE_TEXTURE2D(_EmissiveTex, sampler_MainTex, uv1).r, t);
                float3 n = normalize(i.tangentWS * nT.x + i.bitangentWS * nT.y + i.normalWS * max(nT.z, 0.2));
                half wrap = saturate(dot(n, _PT_SunDir.xyz) * 0.5 + 0.5);
                // Sonnen- und Himmelsfarbe tragen ihre HDR-Intensität; hier nur der Farbton, sonst überstrahlt der Körper (Regel 4)
                half3 sunC = _PT_SunColor.rgb / max(1.0, max(_PT_SunColor.r, max(_PT_SunColor.g, _PT_SunColor.b)));
                half3 skyC = _PT_AmbSky.rgb / max(1.0, max(_PT_AmbSky.r, max(_PT_AmbSky.g, _PT_AmbSky.b)));
                half3 lit = lerp(1, sunC * (0.45 + 0.55 * wrap) + skyC * 0.25, _Lighting);
                float2 e = abs(uv * 2 - 1);
                half border = saturate((1 - max(e.x, e.y)) * 8);   // keine Rechteck-Kanten, falls die Simulation den Kachelrand erreicht
                half a = c.a * i.color.a * _Tint.a * border;
                if (_PT_HasOpaque > -1)   // Soft-Particles gegen die Szenentiefe
                {
                    float2 suv = i.positionCS.xy / _ScaledScreenParams.xy;
                    float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                    a *= saturate((scene - i.eye) / max(_SoftDistance, 0.01));
                }
                half3 body = c.rgb * _Tint.rgb * i.color.rgb * lit;
                half3 rgb = body * a + core * _CoreIntensity * a * _Tint.rgb;   // Kern über der Bloom-Schwelle, Körper darunter
                return half4(rgb, a * (1 - _Additive));
            }
            ENDHLSL
        }
    }
}
