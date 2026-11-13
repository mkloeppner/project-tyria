// Project Tyrant – Wasser 2.0 (Grafik-Lauf G3): Tiefenfarbe flach → tief, Brechung des Grundes (Opaque-Textur, ab Mittel), Kaustiken am Grund,
// Uferschaum mit Rauschen, Wellen-Normalen aus zwei Rausch-Lagen, Fresnel-Reflexion der Zonen-Cubemap, Sonnenglanz, Höhennebel im Shader.
// Eigenschaften von ProjectTyrant/Water bleiben erhalten (Migration tauscht nur den Shader).
Shader "ProjectTyrant/Water2"
{
    Properties
    {
        _ShallowColor ("Flach", Color) = (0.35,0.75,0.85,0.75)
        _DeepColor ("Tief", Color) = (0.08,0.3,0.5,0.95)
        _SkyColor ("Himmel (Ersatz-Reflexion)", Color) = (0.75,0.88,1,1)
        _FoamColor ("Schaum", Color) = (1,1,1,1)
        _DepthRange ("Tiefen-Bereich (m)", Float) = 2.5
        _FoamWidth ("Schaumbreite (m)", Float) = 0.45
        _WaveAmp ("Wellenhöhe", Float) = 0.06
        _WaveScale ("Wellenmaßstab", Float) = 0.35
        [HDR] _Emission ("Eigenleuchten (Lava)", Color) = (0,0,0,0)
        _NoiseTex ("Wellen-Rauschen", 2D) = "gray" {}
        _CausticTex ("Kaustik", 2D) = "black" {}
        _Refraction ("Brechung", Range(0,0.1)) = 0.03
        _CausticStrength ("Kaustik-Stärke", Range(0,2)) = 0.8
        _Reflection ("Reflexion", Range(0,1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Water"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _SkyColor, _FoamColor, _Emission;
                float _DepthRange, _FoamWidth, _WaveAmp, _WaveScale, _Refraction, _CausticStrength, _Reflection;
                float4 _NoiseTex_ST, _CausticTex_ST;
            CBUFFER_END
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            TEXTURE2D(_CausticTex); SAMPLER(sampler_CausticTex);
            float _PT_HasOpaque;   // 1 ab Mittel (Opaque-Textur vorhanden)

            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float eyeDepth : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float Wave(float2 p, float t) { return sin(p.x * 1.3 + t * 1.1) * 0.5 + sin(p.y * 1.7 - t * 0.9) * 0.35 + sin((p.x + p.y) * 2.9 + t * 1.7) * 0.15; }

            Varyings vert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                w.y += Wave(w.xz * _WaveScale, _Time.y) * _WaveAmp;
                o.positionWS = w; o.positionCS = TransformWorldToHClip(w); o.eyeDepth = -TransformWorldToView(w).z;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float t = _Time.y;
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                // Normale: zwei gegenläufige Rausch-Lagen + Vertex-Welle
                float2 p = i.positionWS.xz;
                half2 n1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p * 0.045 + float2(t * 0.012, t * 0.007)).rg - 0.5;
                half2 n2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p * 0.11 - float2(t * 0.017, -t * 0.011)).rg - 0.5;
                float e = 0.15; float2 q = p * _WaveScale;
                float hx = Wave(q + float2(e, 0), t) - Wave(q - float2(e, 0), t);
                float hz = Wave(q + float2(0, e), t) - Wave(q - float2(0, e), t);
                float3 n = normalize(float3(-hx * _WaveAmp * 6 + (n1.x + n2.x) * 0.35, 1, -hz * _WaveAmp * 6 + (n1.y + n2.y) * 0.35));

                float sceneRaw = SampleSceneDepth(suv);
                float scene = LinearEyeDepth(sceneRaw, _ZBufferParams);
                float depth = max(0, scene - i.eyeDepth);
                float d = saturate(depth / _DepthRange);

                // Brechung: Grund versetzt, nur wenn der versetzte Punkt auch unter Wasser liegt
                float2 ruv = suv + n.xz * _Refraction * saturate(depth * 0.5);
                float rScene = LinearEyeDepth(SampleSceneDepth(ruv), _ZBufferParams);
                if (rScene < i.eyeDepth) ruv = suv;
                half3 ground = _PT_HasOpaque > 0.5 ? SampleSceneColor(ruv) : _ShallowColor.rgb;

                // Kaustik auf dem Grund (Weltposition des Grundes aus der Tiefe)
                float3 viewDirW = -normalize(GetWorldSpaceViewDir(i.positionWS));
                float3 bottom = i.positionWS + viewDirW * depth;
                half c1 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, bottom.xz * 0.12 + t * 0.03).r;
                half c2 = SAMPLE_TEXTURE2D(_CausticTex, sampler_CausticTex, bottom.xz * 0.09 - t * 0.025).r;
                half caustic = min(c1, c2) * _CausticStrength * (1.0 - d) * saturate(_PT_SunDir.w * 3.0);
                ground += _PT_SunColor.rgb * caustic;

                half3 body = lerp(ground * _ShallowColor.rgb * 1.15, _DeepColor.rgb, d);
                half alpha = lerp(_ShallowColor.a, _DeepColor.a, d);
                if (_PT_HasOpaque > 0.5) { body = lerp(ground * _ShallowColor.rgb, _DeepColor.rgb, saturate(d * _DeepColor.a)); alpha = 1; }

                // Reflexion: Zonen-Cubemap (Echtzeit-Sonde), Fresnel
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                half fres = pow(1 - saturate(dot(n, v)), 4);
                half3 refl = GlossyEnvironmentReflection(reflect(-v, n), i.positionWS, 0.05, 1.0, suv);
                refl = lerp(_SkyColor.rgb, refl, 0.75);
                half3 col = lerp(body, refl, fres * _Reflection);

                Light L = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                float3 h = normalize(L.direction + v);
                col += L.color * pow(saturate(dot(n, h)), 160) * 3.0 * L.shadowAttenuation;

                half foamNoise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p * 0.35 + t * 0.05).r;
                half foam = saturate(1 - depth / _FoamWidth) * smoothstep(0.3, 0.6, foamNoise + (1 - depth / _FoamWidth) * 0.4);
                col = lerp(col, _FoamColor.rgb * (0.6 + 0.4 * L.color), foam);
                alpha = saturate(alpha + foam * 0.5);
                col *= lerp(0.65, 1.0, saturate(L.direction.y + 0.3));
                col += _Emission.rgb;
                col = PT_ApplyFog(col, i.positionWS);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
