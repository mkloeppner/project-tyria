// Project Tyrant – Himmel 2.0 (Grafik-Lauf G2): Zenit-Horizont-Boden-Verlauf je Zone, Sonne mit Hof, Mond, zwei Wolkenschichten,
// Sterne mit Funkeln, Polarlicht, Aschehimmel, Sandschleier. Tageszeit über globale Werte (_PT_SkyTint, _PT_Night, _PT_SunDir).
Shader "ProjectTyrant/Sky"
{
    Properties
    {
        _Zenith ("Zenit", Color) = (0.25,0.5,0.9,1)
        _Horizon ("Horizont", Color) = (0.8,0.85,0.9,1)
        _Ground ("Boden", Color) = (0.5,0.5,0.5,1)
        _CloudColor ("Wolken", Color) = (1,1,1,1)
        _CloudCover ("Bedeckung", Range(0,1)) = 0.4
        _CloudTex ("Wolken-Textur", 2D) = "white" {}
        _StarTex ("Sterne", 2D) = "black" {}
        _Stars ("Sterne an", Float) = 1
        _Aurora ("Polarlicht", Float) = 0
        _Ash ("Aschehimmel", Float) = 0
        _Sand ("Sandschleier", Float) = 0
        _SunSize ("Sonnengröße", Range(0.001,0.1)) = 0.025
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Zenith, _Horizon, _Ground, _CloudColor;
                float _CloudCover, _Stars, _Aurora, _Ash, _Sand, _SunSize;
                float4 _CloudTex_ST, _StarTex_ST;
            CBUFFER_END
            TEXTURE2D(_CloudTex); SAMPLER(sampler_CloudTex);
            TEXTURE2D(_StarTex); SAMPLER(sampler_StarTex);
            float4 _PT_SkyTint;     // rgb Tönung, a Helligkeit
            float4 _PT_SkyWeather;  // x Bedeckungs-Zuschlag, y Blitz-Aufhellung

            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.dir = v.positionOS.xyz; return o; }

            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float y = d.y;
                half3 sky = y >= 0 ? lerp(_Horizon.rgb, _Zenith.rgb, pow(saturate(y), 0.55)) : lerp(_Horizon.rgb, _Ground.rgb, saturate(-y * 3.0));
                sky *= _PT_SkyTint.rgb * _PT_SkyTint.a;

                // Sonne und Hof
                float3 sd = normalize(_PT_SunDir.xyz);
                float sdot = dot(d, sd);
                half day = saturate(sd.y * 5.0 + 0.3);
                half disk = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.6, sdot);
                half halo = pow(saturate(sdot), 64.0) * 0.6 + pow(saturate(sdot), 6.0) * 0.18;
                sky += _PT_SunColor.rgb * (disk * 6.0 + halo) * day * (1.0 - _Ash * 0.6);
                // Mond gegenüber der Sonne
                half moon = smoothstep(0.9992, 0.9996, dot(d, -sd)) * _PT_Night.y;
                sky += half3(0.75, 0.8, 1.0) * moon * 1.5;

                // Sterne (über dem Horizont, funkelnd)
                if (_Stars > 0.5 && y > 0)
                {
                    float2 suv = d.xz / (y + 0.35) * 2.0;
                    half s = SAMPLE_TEXTURE2D(_StarTex, sampler_StarTex, suv).r;
                    half tw = 0.7 + 0.3 * sin(_Time.y * 3.0 + s * 40.0);
                    sky += s * tw * _PT_Night.z * saturate(y * 3.0) * (1.0 - _CloudCover);
                }
                // Polarlicht: wehende Bänder im Nordhimmel
                if (_Aurora > 0.5 && y > 0.05)
                {
                    float2 auv = float2(atan2(d.x, d.z) * 1.5, y * 2.0);
                    half n = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, auv * float2(1, 0.3) + float2(_Time.y * 0.01, 0)).r;
                    half band = smoothstep(0.45, 0.7, n) * smoothstep(0.05, 0.3, y) * (1.0 - smoothstep(0.5, 0.9, y));
                    sky += half3(0.2, 1.0, 0.6) * band * _PT_Night.y * 0.8;
                }

                // Wolken: zwei Schichten, horizontnah dichter
                if (y > -0.02)
                {
                    float cover = saturate(_CloudCover + _PT_SkyWeather.x);
                    float2 cuv = d.xz / (y + 0.12);
                    float2 wind = float2(_PT_Wind.x, _PT_Wind.y) * _Time.y * 0.004;
                    half c1 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cuv * 0.18 + wind).r;
                    half c2 = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cuv * 0.43 - wind * 1.7).r;
                    half c = saturate((c1 * 0.65 + c2 * 0.35) - (1.0 - cover) * 0.75) * 2.2;
                    c = saturate(c) * saturate(y * 6.0 + 0.2);
                    half lit = 0.65 + 0.35 * saturate(sdot * 0.5 + 0.5);
                    half3 cc = _CloudColor.rgb * _PT_SkyTint.rgb * _PT_SkyTint.a * lit + _PT_SunColor.rgb * pow(saturate(sdot), 12.0) * 0.4 * day;
                    sky = lerp(sky, cc, c);
                }
                // Asche / Sand: Schleier zum Horizont
                sky = lerp(sky, _Horizon.rgb * 0.55 * _PT_SkyTint.a, _Ash * 0.45 * (1.0 - saturate(y * 2.0)));
                sky = lerp(sky, _Horizon.rgb * _PT_SkyTint.a, _Sand * 0.35 * (1.0 - saturate(y * 3.0)));
                sky += _PT_SkyWeather.y;
                return half4(max(sky, 0), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
