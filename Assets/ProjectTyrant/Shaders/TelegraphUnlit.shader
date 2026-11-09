// Boden-Telegraph / Combo-Felder: transparenter Ring mit Füllfortschritt.
Shader "ProjectTyrant/GroundMarker"
{
    Properties
    {
        _Color ("Color", Color) = (1,0.2,0.1,0.6)
        _Fill ("Fill (0..1)", Range(0,1)) = 0
        _RingWidth ("Ring Width", Range(0.01,0.5)) = 0.08
        _Pulse ("Pulse", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off
        Offset -1, -1
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float _Fill; float _RingWidth; float _Pulse;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float r = length(i.uv - 0.5) * 2.0;
                if (r > 1.0) discard;
                float ring = smoothstep(1.0 - _RingWidth, 1.0 - _RingWidth * 0.5, r) * (1.0 - smoothstep(0.98, 1.0, r));
                float fill = step(r, _Fill) * 0.45;
                float base = 0.18 + 0.12 * sin(_Time.y * 6.0) * _Pulse;
                float a = saturate(ring + fill + base) * _Color.a;
                return half4(_Color.rgb * (1.0 + ring), a);
            }
            ENDHLSL
        }
    }
}
