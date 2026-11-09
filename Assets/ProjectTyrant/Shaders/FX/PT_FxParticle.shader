// Lauf 5 / M23: Partikel-Shader des VFX-Baukastens. Additiv (Standard) oder Alpha (Rauch/Schatten) mit Soft-Particles
// (Tiefen-Abgleich gegen _CameraDepthTexture) und Kamera-Nah-Ausblendung. Farbe = Textur × Vertexfarbe × _Color × _Intensity.
Shader "ProjectTyrant/FX/Particle"
{
    Properties
    {
        _MainTex ("Textur", 2D) = "white" {}
        _Color ("Farbe", Color) = (1,1,1,1)
        _Intensity ("Intensität", Range(0, 8)) = 1.5
        _SoftFade ("Soft-Particles (m)", Range(0.01, 3)) = 0.6
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; half4 _Color; float _Intensity; float _SoftFade; float _SrcBlend; float _DstBlend;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float4 screenPos : TEXCOORD1; float eyeDepth : TEXCOORD2; };
            Varyings vert (Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.uv = TRANSFORM_TEX(v.uv, _MainTex); o.color = v.color;
                o.screenPos = ComputeScreenPos(p.positionCS); o.eyeDepth = -p.positionVS.z;
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 c = t * i.color * _Color;
                float2 uv = i.screenPos.xy / max(i.screenPos.w, 1e-4);
                float scene = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float soft = saturate((scene - i.eyeDepth) / _SoftFade);
                float nearFade = saturate((i.eyeDepth - 0.3) / 0.8);
                c.a *= soft * nearFade;
                bool additive = _DstBlend > 0.5 && _DstBlend < 1.5;
                c.rgb *= additive ? _Intensity : 1.0;   // Blend SrcAlpha One gewichtet bereits mit Alpha (kein Alpha²)
                return c;
            }
            ENDHLSL
        }
    }
}
