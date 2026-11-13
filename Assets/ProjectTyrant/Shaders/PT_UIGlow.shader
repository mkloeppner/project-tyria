// Project Tyrant – UI-Glühen (Grafik-Lauf G3/G6): uGUI-kompatibel (Stencil/Maske, Vertexfarbe), additiver Schimmer-Streifen und Puls
// für Fertigkeits-Icons (bereit, Kombo, Abklingzeit fertig). Lichtempfindlich: Puls über _PT_UIPulse gedeckelt.
Shader "ProjectTyrant/UI/Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tönung", Color) = (1,1,1,1)
        [HDR] _GlowColor ("Glühen", Color) = (1,0.85,0.4,1)
        _Shine ("Schimmer", Range(0,1)) = 0.5
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float4 _GlowColor; float _Shine; float4 _MainTex_ST;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _PT_UIPulse;   // 1 normal, 0,3 lichtempfindlich
            struct A { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            V vert(A v) { V o; o.pos = TransformObjectToHClip(v.vertex.xyz); o.color = v.color * _Color; o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o; }
            half4 frag(V i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color;
                float band = saturate(1 - abs(frac(i.uv.x + i.uv.y * 0.5 - _Time.y * 0.35) - 0.5) * 8.0);
                float pulse = 0.5 + 0.5 * sin(_Time.y * 4.0);
                float amp = max(_PT_UIPulse, 0.3);
                c.rgb += _GlowColor.rgb * (band * _Shine + pulse * 0.15 * amp) * c.a;
                return c;
            }
            ENDHLSL
        }
    }
}
