// Boden-Telegraph / Combo-Felder: Ring mit Füllfortschritt.
// Grafik-Lauf G2/G7 (Lesbarkeit vor Schönheit): nach dem Post-Processing gezeichnet (PTOverlayPass), tiefensicher – an Hängen wird die Fläche nicht mehr vom Gelände verschluckt
// (ZTest Always, verdeckt nur bei echter Verdeckung > 1,5 m bzw. 8 % der Distanz), Ring deckend und HDR-hell, unbeeinflusst von Nebel und Licht.
Shader "ProjectTyrant/GroundMarker"
{
    Properties
    {
        _Color ("Color", Color) = (1,0.2,0.1,0.6)
        _Fill ("Fill (0..1)", Range(0,1)) = 0
        _RingWidth ("Ring Width", Range(0.01,0.5)) = 0.08
        _Pulse ("Pulse", Range(0,1)) = 0.3
        _RingBoost ("Ring HDR-Faktor", Range(1,4)) = 1.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent+50" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off ZTest Always
        Pass
        {
            Tags { "LightMode"="PTOverlay" }   // G7: nach dem Post-Processing (PTOverlayPass)
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            float _PT_HasOpaque; float _PT_ExposureMul;

            CBUFFER_START(UnityPerMaterial)
                float4 _Color; float _Fill; float _RingWidth; float _Pulse; float _RingBoost;
            CBUFFER_END
            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float eye : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws); o.uv = v.uv;
                o.eye = -TransformWorldToView(ws).z;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float r = length(i.uv - 0.5) * 2.0;
                if (r > 1.0) discard;
                // echte Verdeckung (Wand, Kuppe) gegenüber leichtem Einsinken am Hang
                float2 suv = i.positionCS.xy / _ScaledScreenParams.xy;
                float scene = LinearEyeDepth(SampleSceneDepth(suv), _ZBufferParams);
                if (i.eye - scene > max(1.5, i.eye * 0.08)) discard;
                float ring = smoothstep(1.0 - _RingWidth, 1.0 - _RingWidth * 0.6, r);
                float fill = step(r, _Fill) * 0.45;
                float base = 0.18 + 0.12 * sin(_Time.y * 6.0) * _Pulse;
                float a = saturate(max(ring, (fill + base) * _Color.a));
                // nach dem Post-Processing: Farben kommen unverändert an (kein Bloom, kein Tonemapping) – Ring deckend in Gefahrenfarbe
                // auf sehr dunklem Boden (Verderbnis, Höhlen, Nacht) wird der Ring heller und gelblicher, bleibt aber Gefahrenfarbe
                // Opaque-Textur ist linear vor Belichtung und Tonemapping: grob auf Bildschirmhelligkeit abbilden (Belichtung + ACES-Näherung)
                half rawLum = _PT_HasOpaque > 0.5 ? dot(SampleSceneColor(suv), half3(0.2126, 0.7152, 0.0722)) : 0.05;
                half x = rawLum * max(_PT_ExposureMul, 1.0) * 0.6;
                half groundLum = saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14));
                half darkGround = 1.0 - smoothstep(0.05, 0.16, groundLum);
                half brightGround = smoothstep(0.14, 0.3, groundLum);
                half3 ringCol = lerp(_Color.rgb, saturate(_Color.rgb * 1.1 + half3(0.0, 0.28, 0.1)), darkGround);
                ringCol = lerp(ringCol, _Color.rgb * half3(0.55, 0.12, 0.2), brightGround);   // auf hellem, warmem Boden tiefes Dunkelrot
                half3 col = lerp(_Color.rgb * 0.9, ringCol, ring);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
