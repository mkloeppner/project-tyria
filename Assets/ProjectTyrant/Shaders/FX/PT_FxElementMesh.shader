// Project Tyrant – VFX-Lauf V2: Element-Meshes und Welt-Spuren.
// _Mode 0 = Körper (Spritzkrone, Wellenring, Säule, Klingenbogen): Körperfarbe, Hof, Schaum oben (UV.y), Fresnel, Ausblenden über _Age.
// _Mode 1 = Pfütze: dunkle, nasse Fläche mit Himmels-Spiegelung am Rand, trocknet von außen nach innen (_Age).
// _Mode 2 = Nässe: dunkelt den Boden kurz ab (Aufladen von Wasser-Flächen).
// Vormultipliziertes Alpha, keine Tiefe schreiben; Gefahren-Telegrafen liegen im Overlay-Pass darüber (Lesbarkeit).
Shader "ProjectTyrant/FX/ElementMesh"
{
    Properties
    {
        [HDR] _Body ("Körper", Color) = (0.25, 0.62, 0.95, 0.55)
        [HDR] _Halo ("Hof", Color) = (0.35, 0.9, 0.85, 1)
        [HDR] _Foam ("Schaum/Kern", Color) = (2.2, 2.6, 3, 1)
        _FoamStart ("Schaum ab UV.y", Range(0, 1)) = 0.7
        _FresnelPower ("Fresnel", Range(0.5, 8)) = 3
        _Mode ("Modus (0 Körper, 1 Pfütze, 2 Nässe)", Float) = 0
        _Age ("Alter 0–1", Range(0, 1)) = 0
        _Tint ("Tönung", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _NoiseTex ("Rauschen", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Body, _Halo, _Foam, _Tint;
                float _FoamStart, _FresnelPower, _Mode, _Age;
            CBUFFER_END
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 normalWS : TEXCOORD1; float3 viewWS : TEXCOORD2; float4 color : COLOR; float3 posWS : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };

            V Vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws); o.posWS = ws;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.viewWS = normalize(_WorldSpaceCameraPos - ws);
                o.uv = v.uv; o.color = v.color;
                return o;
            }

            half4 Frag(V i, bool front : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS) * (front ? 1 : -1);
                float fres = pow(saturate(1 - abs(dot(n, i.viewWS))), _FresnelPower);
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, i.uv * 3 + _Time.y * 0.3).r;
                if (_Mode > 1.5)   // Nässe
                {
                    float r = length(i.uv - 0.5) * 2;
                    float a = saturate(1 - r) * 0.45 * (1 - _Age) * _Tint.a;
                    return half4(0, 0, 0, a);
                }
                if (_Mode > 0.5)   // Pfütze: trocknet von außen nach innen
                {
                    float r = length(i.uv - 0.5) * 2 + (noise - 0.5) * 0.25;
                    float edge = lerp(1.0, 0.25, saturate(_Age));
                    float mask = saturate((edge - r) / 0.12);
                    float3 sky = _PT_AmbSky.rgb * 1.2 + _PT_SunColor.rgb * 0.15;
                    float3 col = lerp(_Body.rgb * 0.25, sky, saturate(fres * 1.5 + 0.25));
                    float a = mask * 0.6 * _Tint.a;
                    return half4(col * a, a);
                }
                // Körper: Hof unten, Körper Mitte, Schaum/Kern oben; Fresnel heller
                float foam = smoothstep(_FoamStart, 1.0, i.uv.y + (noise - 0.5) * 0.2);
                float3 col = lerp(_Halo.rgb, _Body.rgb, saturate(i.uv.y * 1.5));
                col = lerp(col, _Foam.rgb, foam) + fres * _Halo.rgb * 0.8;
                col *= lerp(0.7, 1.15, saturate(dot(n, _PT_SunDir.xyz) * 0.5 + 0.5));
                float fade = saturate(1 - _Age) * saturate(1 - _Age) * (_Age < 0.06 ? _Age / 0.06 : 1);
                float a = saturate(_Body.a + foam * 0.4 + fres * 0.3) * fade * i.color.a * _Tint.a;
                return half4(col * a, a);
            }
            ENDHLSL
        }
    }
}
