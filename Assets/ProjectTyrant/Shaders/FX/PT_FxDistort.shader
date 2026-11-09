// Lauf 5 / M23: Hitzeflimmern/Schockwellen-Verzerrung über die Opaque-Texture (URP-Asset: Opaque Texture an).
Shader "ProjectTyrant/FX/Distort"
{
    Properties
    {
        _NoiseTex ("Rauschen", 2D) = "gray" {}
        _Strength ("Stärke", Range(0, 0.1)) = 0.025
        _Speed ("Tempo", Range(0, 4)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial) float4 _NoiseTex_ST; float _Strength; float _Speed; CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 screenPos : TEXCOORD1; half4 color : COLOR; };
            Varyings vert (Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; o.screenPos = ComputeScreenPos(o.positionCS); o.color = v.color; return o; }
            half4 frag (Varyings i) : SV_Target
            {
                float2 n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv + _Time.y * _Speed * float2(0.1, 0.27)).rg * 2 - 1;
                float mask = saturate(1 - length(i.uv * 2 - 1)) * i.color.a;
                float2 suv = i.screenPos.xy / max(i.screenPos.w, 1e-4) + n * _Strength * mask;
                return half4(SampleSceneColor(suv), mask);
            }
            ENDHLSL
        }
    }
}
