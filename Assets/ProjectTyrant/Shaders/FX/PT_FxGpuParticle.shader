// Project Tyrant – VFX-Lauf V1: Darstellung der GPU-Partikel (Billboards oder entlang der Geschwindigkeit gestreckt) aus dem Partikel-Puffer.
// Additiv, HDR-Farbe Start → Ende über das Alter; tote Partikel fallen aus dem Bild (außerhalb des Clip-Raums).
Shader "ProjectTyrant/FX/GpuParticle"
{
    Properties
    {
        [HDR] _ColorStart ("Farbe Start", Color) = (2, 1.4, 0.6, 1)
        [HDR] _ColorEnd ("Farbe Ende", Color) = (0.8, 0.2, 0.05, 0)
        _Stretch ("Streckung (0 = Billboard)", Range(0, 0.2)) = 0
        [NoScaleOffset] _MainTex ("Form", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 4.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorStart, _ColorEnd; float _Stretch;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Particle { float3 pos; float age; float3 vel; float life; float size; uint seed; uint flags; float pad; };
            StructuredBuffer<Particle> _Particles;

            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };

            static const float2 Corners[6] = { float2(-1, -1), float2(1, -1), float2(1, 1), float2(-1, -1), float2(1, 1), float2(-1, 1) };

            V Vert(uint vid : SV_VertexID)
            {
                V o; Particle p = _Particles[vid / 6]; float2 c = Corners[vid % 6];
                o.uv = c * 0.5 + 0.5;
                if (p.age >= p.life || p.life <= 0) { o.positionCS = float4(2, 2, 2, 1); o.color = 0; return o; }
                float3 right = UNITY_MATRIX_V[0].xyz, up = UNITY_MATRIX_V[1].xyz;
                if (_Stretch > 0 && dot(p.vel, p.vel) > 1e-4)
                {
                    float3 toCam = normalize(_WorldSpaceCameraPos - p.pos);
                    up = normalize(p.vel); right = normalize(cross(up, toCam));
                    up *= 1 + length(p.vel) * _Stretch * 10;
                }
                float3 ws = p.pos + (right * c.x + up * c.y) * p.size;
                o.positionCS = TransformWorldToHClip(ws);
                float t = saturate(p.age / p.life);
                o.color = lerp(_ColorStart, _ColorEnd, t);
                return o;
            }

            half4 Frag(V i) : SV_Target
            {
                float2 d = i.uv * 2 - 1; half soft = saturate(1 - dot(d, d));
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half a = soft * tex.a * i.color.a;
                return half4(i.color.rgb * tex.rgb * a, 0);
            }
            ENDHLSL
        }
    }
}
