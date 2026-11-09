// Lauf 5 / M23: Mesh-Effekte (Ringe, Kuppeln, Säulen, Siegel): Scroll-Textur, Fresnel-Rand und Dissolve mit Randglühen in einem Shader.
// _Dissolve 0 = voll sichtbar, 1 = aufgelöst; _Fresnel > 0 betont die Kanten (Aura/Kuppel); _Scroll bewegt die Textur (Strahlen, Säulen).
Shader "ProjectTyrant/FX/Mesh"
{
    Properties
    {
        _MainTex ("Textur", 2D) = "white" {}
        _NoiseTex ("Rauschen (Dissolve)", 2D) = "gray" {}
        _Color ("Farbe", Color) = (1,1,1,1)
        _EdgeColor ("Randglühen", Color) = (1,0.8,0.4,1)
        _Intensity ("Intensität", Range(0, 8)) = 1.8
        _Scroll ("Scroll (UV/s)", Vector) = (0, 0.6, 0, 0)
        _Fresnel ("Fresnel", Range(0, 8)) = 0
        _Dissolve ("Dissolve", Range(0, 1)) = 0
        _EdgeWidth ("Randbreite", Range(0, 0.3)) = 0.08
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
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _NoiseTex_ST; half4 _Color; half4 _EdgeColor; float _Intensity; float4 _Scroll; float _Fresnel; float _Dissolve; float _EdgeWidth;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; float3 normalWS : TEXCOORD1; float3 viewWS : TEXCOORD2; };
            Varyings vert (Attributes v)
            {
                Varyings o; VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.uv = TRANSFORM_TEX(v.uv, _MainTex); o.color = v.color;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS); o.viewWS = GetWorldSpaceViewDir(p.positionWS);
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                float2 uv = i.uv + _Scroll.xy * _Time.y;
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv * 1.7 + _Scroll.zw * _Time.y).r;
                half4 c = t * _Color * i.color;
                if (_Fresnel > 0)
                {
                    float f = pow(1.0 - saturate(abs(dot(normalize(i.normalWS), normalize(i.viewWS)))), _Fresnel);
                    c.a *= f;
                }
                float d = n - _Dissolve;
                clip(d);
                half edge = 1.0 - saturate(d / max(_EdgeWidth, 1e-3));
                c.rgb = lerp(c.rgb, _EdgeColor.rgb * 2.0, edge * step(0.001, _Dissolve));
                c.rgb *= _Intensity;   // Blend SrcAlpha One gewichtet bereits mit Alpha
                return c;
            }
            ENDHLSL
        }
    }
}
