// Lauf 5 / M22: additiver Waffen-Trail (TrailRenderer). Farbe aus Vertexfarbe × _Color, weicher Rand über die Trail-Breite (UV.y).
Shader "ProjectTyrant/FX/AdditiveTrail"
{
    Properties
    {
        _Color ("Farbe", Color) = (1,0.6,0.25,1)
        _Intensity ("Intensität", Range(0, 4)) = 1.6
        _Softness ("Randweichheit", Range(0.01, 1)) = 0.45
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; float _Intensity; float _Softness;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings vert (Attributes v) { Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; o.color = v.color; return o; }
            half4 frag (Varyings i) : SV_Target
            {
                float edge = 1.0 - abs(i.uv.y * 2.0 - 1.0);
                float a = smoothstep(0.0, _Softness, edge) * (1.0 - i.uv.x);
                half4 c = _Color * i.color;
                return half4(c.rgb * _Intensity, a * c.a);
            }
            ENDHLSL
        }
    }
}
