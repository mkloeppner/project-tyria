// Project Tyrant – Vollbild-Passes des PTRenderFeature (Grafik-Lauf G2), je Pass ein Effekt mit eigenem Aus-Schalter:
// 0 Nebel (Höhennebel + Horizont), 1 Kontur (Tiefe/Normalen), 2 Lichtschaft (radial zur Sonne), 3 Verzerrung (Hitzeflimmern + Schockwellen),
// 4 Unterwasser, 5 Schaden (Vignette + Treffer-Puls). Globale Werte setzt SceneLighting/GraphicsDirector.
// Vollbild-Shader brauchen keine DepthOnly-/ShadowCaster-Passes (sie zeichnen keine Geometrie).
Shader "ProjectTyrant/Fullscreen/PostFx"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _PT_Unused;
        CBUFFER_END

        float4 _PT_OutlineColor;   // rgb, a Stärke
        float4 _PT_OutlineParams;  // x Dicke (px bei 1080p), y Ausblend-Start (m), z Ausblend-Ende (m)
        float4 _PT_SunScreen;      // xy Sonne im Bild (UV), z Sichtbarkeit 0..1, w Intensität
        float4 _PT_Heat;           // x Stärke (px bei 1080p, ≤ 6), y Skala, z Tempo
        float4 _PT_Shock[8];       // xy UV, z Radius (UV-Höhe), w Stärke 0..1
        float _PT_ShockCount;
        float4 _PT_Underwater;     // x an, y Dichte, z Wackeln, w Kaustik
        float4 _PT_WaterShallow, _PT_WaterDeep;
        float4 _PT_Damage;         // rgb Farbe, a Vignette 0..1
        float _PT_DamageFlash;
        float _PT_DistortScale;    // 1, im Lichtempfindlich-Modus 0,5
        TEXTURE2D(_PT_DistortBuffer); float _PT_HasDistortBuffer;   // V1: gesammelte Brechung (rg Verschiebung UV, b Dispersion, a Deckung)
        TEXTURE2D(_PT_Noise); SAMPLER(sampler_PT_Noise);

        float RawDepth(float2 uv) { return SampleSceneDepth(uv); }
        bool IsSky(float raw)
        {
            #if UNITY_REVERSED_Z
                return raw <= 1e-6;
            #else
                return raw >= 0.999999;
            #endif
        }
        float3 WorldPos(float2 uv, float raw)
        {
            #if UNITY_REVERSED_Z
                float d = raw;
            #else
                float d = lerp(UNITY_NEAR_CLIP_VALUE, 1, raw);
            #endif
            return ComputeWorldSpacePosition(uv, d, UNITY_MATRIX_I_VP);
        }
        half4 Src(float2 uv) { return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv); }
        float2 PxToUv(float px) { return px / 1080.0 * float2(_ScreenParams.y / _ScreenParams.x, 1.0); }
        ENDHLSL

        Pass
        {
            Name "PT_Fog"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 col = Src(uv);
                if (_PT_FogColor.a < 0.5) return col;
                float raw = RawDepth(uv);
                float3 wp = WorldPos(uv, raw);
                if (IsSky(raw))
                {
                    float3 dir = normalize(wp - _WorldSpaceCameraPos);
                    half horizon = 1.0 - saturate(dir.y * 4.0);
                    half sun = pow(saturate(dot(dir, _PT_SunDir.xyz)), 8.0) * _PT_FogParams.w;
                    half3 fc = lerp(_PT_FogColor.rgb, _PT_FogSunColor.rgb, sun);
                    col.rgb = lerp(col.rgb, fc, horizon * horizon * _PT_FogSunColor.a * 0.85);
                    return col;
                }
                col.rgb = PT_ApplyFog(col.rgb, wp);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "PT_Outline"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 col = Src(uv);
                float raw = RawDepth(uv);
                if (IsSky(raw) || _PT_OutlineColor.a <= 0) return col;
                float d0 = LinearEyeDepth(raw, _ZBufferParams);
                float2 o = PxToUv(_PT_OutlineParams.x);
                float dl = LinearEyeDepth(RawDepth(uv - float2(o.x, 0)), _ZBufferParams);
                float dr = LinearEyeDepth(RawDepth(uv + float2(o.x, 0)), _ZBufferParams);
                float du = LinearEyeDepth(RawDepth(uv + float2(0, o.y)), _ZBufferParams);
                float dd = LinearEyeDepth(RawDepth(uv - float2(0, o.y)), _ZBufferParams);
                // Nur Silhouetten (nähere Mitte gegen fernere Nachbarn) – keine Linien auf schrägen Flächen
                float edge = max(max(dl, dr), max(du, dd)) - d0;
                float e = smoothstep(0.06, 0.16, edge / max(d0, 0.5));
                float fade = 1.0 - smoothstep(_PT_OutlineParams.y, _PT_OutlineParams.z, d0);
                col.rgb = lerp(col.rgb, _PT_OutlineColor.rgb, e * fade * _PT_OutlineColor.a);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "PT_Godrays"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 col = Src(uv);
                if (_PT_SunScreen.z <= 0.001) return col;
                const int N = 16;
                float2 delta = (uv - _PT_SunScreen.xy) * (0.9 / N);
                float2 p = uv; half acc = 0; half decay = 1;
                [unroll] for (int k = 0; k < N; k++)
                {
                    p -= delta;
                    float raw = RawDepth(p);
                    half sky = IsSky(raw) ? 1.0 : 0.0;
                    half3 c = Src(p).rgb;
                    acc += sky * saturate(Luminance(c) - 0.6) * decay;
                    decay *= 0.92;
                }
                float fall = 1.0 - saturate(length((uv - _PT_SunScreen.xy) * float2(_ScreenParams.x / _ScreenParams.y, 1)) * 0.9);
                col.rgb += _PT_SunColor.rgb * acc * (_PT_SunScreen.w * _PT_SunScreen.z / N) * fall;
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "PT_Distortion"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float2 off = 0;
                if (_PT_Heat.x > 0)
                {
                    float raw = RawDepth(uv);
                    float d = IsSky(raw) ? 400.0 : LinearEyeDepth(raw, _ZBufferParams);
                    float w = saturate((d - 6.0) / 40.0);
                    float2 nuv = uv * _PT_Heat.y + float2(0, -_Time.y * _PT_Heat.z);
                    float2 n = SAMPLE_TEXTURE2D(_PT_Noise, sampler_PT_Noise, nuv).rg - 0.5;
                    off += n * 2.0 * PxToUv(_PT_Heat.x) * w;
                }
                float aspect = _ScreenParams.x / _ScreenParams.y;
                int count = (int)_PT_ShockCount;
                for (int k = 0; k < 8; k++)
                {
                    if (k >= count) break;
                    float4 s = _PT_Shock[k];
                    float2 v = (uv - s.xy) * float2(aspect, 1);
                    float r = length(v);
                    float ring = exp(-pow((r - s.z) / max(s.z * 0.18, 0.004), 2.0)) * s.w;
                    off += (v / max(r, 1e-4)) * float2(1.0 / aspect, 1) * ring * PxToUv(24.0).y;
                }
                off *= _PT_DistortScale;
                float disp = 0.12;
                if (_PT_HasDistortBuffer > 0.5)
                {
                    float4 buf = SAMPLE_TEXTURE2D_X(_PT_DistortBuffer, sampler_LinearClamp, uv);   // Quellen sind schon gedeckelt/halbiert
                    off += buf.xy; disp += buf.z;
                }
                float maxShift = PxToUv(24.0).y;   // Regel 8: ≤ 24 px Verschiebung bei 1080p
                float len = length(off * float2(_ScreenParams.x / _ScreenParams.y, 1));
                if (len > maxShift) off *= maxShift / len;
                // Dispersion: Rot weiter, Blau weniger verschoben, Farbtrennung ≤ 6 px
                float2 sep = off * disp;
                float slen = length(sep * float2(_ScreenParams.x / _ScreenParams.y, 1)), maxSep = PxToUv(6.0).y * 0.5;
                if (slen > maxSep) sep *= maxSep / slen;
                half r = Src(uv + off + sep).r;
                half2 gb = Src(uv + off).gb;
                half b = Src(uv + off - sep).b;
                return half4(r, gb.x, b, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "PT_Underwater"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float2 wob = float2(sin(uv.y * 40.0 + _Time.y * 2.1), cos(uv.x * 34.0 + _Time.y * 1.7)) * PxToUv(3.0 * _PT_Underwater.z);
                float2 suv = uv + wob * _PT_DistortScale;
                half4 col = Src(suv);
                float raw = RawDepth(suv);
                float d = IsSky(raw) ? 200.0 : LinearEyeDepth(raw, _ZBufferParams);
                half f = 1.0 - exp(-d * _PT_Underwater.y);
                half3 tint = col.rgb * _PT_WaterShallow.rgb * 1.2;
                half caustic = SAMPLE_TEXTURE2D(_PT_Noise, sampler_PT_Noise, uv * 3.0 + _Time.y * 0.05).r;
                tint += _PT_WaterShallow.rgb * saturate(caustic - 0.55) * _PT_Underwater.w * (1.0 - f);
                col.rgb = lerp(tint, _PT_WaterDeep.rgb, saturate(f));
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "PT_Damage"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 col = Src(uv);
                float2 v = (uv - 0.5) * float2(_ScreenParams.x / _ScreenParams.y, 1);
                half vig = smoothstep(0.45, 1.05, length(v) * 1.25);
                half pulse = 0.85 + 0.15 * sin(_Time.y * 5.0);
                col.rgb = lerp(col.rgb, _PT_Damage.rgb, vig * saturate(_PT_Damage.a * pulse + _PT_DamageFlash));
                return col;
            }
            ENDHLSL
        }
    }
}
