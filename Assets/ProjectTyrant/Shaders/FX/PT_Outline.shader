// Lauf 5 / M22: Umriss für das aktuelle Ziel (Inverted Hull, URP, HLSL). Wird als zusätzliches Material an die Renderer des Ziels gehängt;
// bei geskinnten Figuren liegt der Körper im letzten Untermesh, damit der Umriss den Körper trifft.
Shader "ProjectTyrant/FX/Outline"
{
    Properties
    {
        _OutlineColor ("Umriss-Farbe", Color) = (1,1,1,1)
        _Width ("Breite (m)", Range(0, 0.1)) = 0.025
        _Pulse ("Pulsieren", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _OutlineColor;
                float _Width;
                float _Pulse;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert (Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                float3 n = normalize(TransformObjectToWorldNormal(v.normalOS));
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float w = _Width * (1.0 + _Pulse * 0.5 * sin(_Time.y * 5.0));
                o.positionCS = TransformWorldToHClip(p + n * w);
                return o;
            }
            half4 frag (Varyings i) : SV_Target { return _OutlineColor; }
            ENDHLSL
        }
    }
}
