// Lauf 5 / M23: Wasser mit Wellen (Vertex), Ufer-Schaum (Tiefen-Differenz), Fresnel-Reflexions-Andeutung (Himmelsfarbe) und Glitzern.
Shader "ProjectTyrant/Water"
{
    Properties
    {
        _ShallowColor ("Flach", Color) = (0.35,0.75,0.85,0.75)
        _DeepColor ("Tief", Color) = (0.08,0.3,0.5,0.95)
        _SkyColor ("Himmel (Reflexion)", Color) = (0.75,0.88,1,1)
        _FoamColor ("Schaum", Color) = (1,1,1,1)
        _DepthRange ("Tiefen-Bereich (m)", Float) = 2.5
        _FoamWidth ("Schaumbreite (m)", Float) = 0.45
        _WaveAmp ("Wellenhöhe", Float) = 0.06
        _WaveScale ("Wellenmaßstab", Float) = 0.35
        _Emission ("Eigenleuchten (Lava)", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _SkyColor, _FoamColor, _Emission; float _DepthRange, _FoamWidth, _WaveAmp, _WaveScale;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float4 screenPos : TEXCOORD1; float eyeDepth : TEXCOORD2; float fog : TEXCOORD3; };
            float Wave(float2 p, float t) { return sin(p.x * 1.3 + t * 1.1) * 0.5 + sin(p.y * 1.7 - t * 0.9) * 0.35 + sin((p.x + p.y) * 2.9 + t * 1.7) * 0.15; }
            Varyings vert (Attributes v)
            {
                Varyings o;
                float3 w = TransformObjectToWorld(v.positionOS.xyz);
                w.y += Wave(w.xz * _WaveScale, _Time.y) * _WaveAmp;
                o.positionWS = w; o.positionCS = TransformWorldToHClip(w);
                o.screenPos = ComputeScreenPos(o.positionCS); o.eyeDepth = -TransformWorldToView(w).z; o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                float2 suv = i.screenPos.xy / max(i.screenPos.w, 1e-4);
                float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                float depth = max(0, scene - i.eyeDepth);
                float d = saturate(depth / _DepthRange);
                half4 col = lerp(_ShallowColor, _DeepColor, d);
                // Normale aus Wellen-Ableitung
                float e = 0.15, t = _Time.y; float2 p = i.positionWS.xz * _WaveScale;
                float hx = Wave(p + float2(e, 0), t) - Wave(p - float2(e, 0), t);
                float hz = Wave(p + float2(0, e), t) - Wave(p - float2(0, e), t);
                float3 n = normalize(float3(-hx * _WaveAmp * 6, 1, -hz * _WaveAmp * 6));
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float fres = pow(1 - saturate(dot(n, v)), 4);
                col.rgb = lerp(col.rgb, _SkyColor.rgb, fres * 0.6);
                Light L = GetMainLight();
                float3 h = normalize(L.direction + v);
                float spec = pow(saturate(dot(n, h)), 120) * 2.5;
                col.rgb += L.color * spec;
                float foamNoise = 0.5 + 0.5 * sin(i.positionWS.x * 3.1 + t * 2.0) * sin(i.positionWS.z * 2.7 - t * 1.6);
                float foam = saturate(1 - depth / _FoamWidth) * (0.6 + 0.4 * foamNoise);
                col.rgb = lerp(col.rgb, _FoamColor.rgb, foam);
                col.a = saturate(col.a + foam * 0.5);
                col.rgb *= lerp(0.7, 1.0, saturate(L.direction.y + 0.3));
                col.rgb += _Emission.rgb;
                col.rgb = MixFog(col.rgb, i.fog);
                return col;
            }
            ENDHLSL
        }
    }
}
