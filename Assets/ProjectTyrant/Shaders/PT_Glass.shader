// Project Tyrant – Glas (Grafik-Lauf G3): getönt, Fresnel-Reflexion der Zonen-Cubemap, leichte Brechung (Opaque-Textur ab Mittel),
// Glanzlicht und Kantenleuchten; Höhennebel im Shader (transparent). Tiefen-Passes für SSAO/Schatten-Empfang der Umgebung.
Shader "ProjectTyrant/Glass"
{
    Properties
    {
        [MainColor] _BaseColor ("Tönung", Color) = (0.7,0.85,1,0.35)
        [HDR] _EmissionColor ("Kantenleuchten", Color) = (0,0,0,1)
        _Refraction ("Brechung", Range(0,0.1)) = 0.02
        _Reflection ("Reflexion", Range(0,1)) = 0.7
        _FresnelPower ("Fresnel", Range(0.5,8)) = 3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor; float4 _EmissionColor; float _Refraction; float _Reflection; float _FresnelPower;
        CBUFFER_END
        ENDHLSL
        Pass
        {
            Name "Glass"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"
            float _PT_HasOpaque;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o); o.positionWS = TransformObjectToWorld(v.positionOS.xyz); o.positionCS = TransformWorldToHClip(o.positionWS); o.normalWS = TransformObjectToWorldNormal(v.normalOS); return o; }
            half4 frag(V i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float2 suv = GetNormalizedScreenSpaceUV(i.positionCS);
                half fres = pow(1 - saturate(dot(n, v)), _FresnelPower);
                half3 behind = _PT_HasOpaque > 0.5 ? SampleSceneColor(suv + n.xy * _Refraction) : _BaseColor.rgb;
                half3 refl = GlossyEnvironmentReflection(reflect(-v, n), i.positionWS, 0.08, 1.0, suv);
                Light L = GetMainLight();
                half spec = pow(saturate(dot(n, normalize(L.direction + v))), 90) * 2.0;
                half3 col = lerp(behind * _BaseColor.rgb, refl, saturate(fres * _Reflection + 0.1)) + L.color * spec + _EmissionColor.rgb * fres;
                half a = _PT_HasOpaque > 0.5 ? 1.0 : saturate(_BaseColor.a + fres * 0.5);
                col = PT_ApplyFog(col, i.positionWS);
                return half4(col, a);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex vd
            #pragma fragment fd
            #pragma multi_compile_instancing
            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vd(A v) { V o; UNITY_SETUP_INSTANCE_ID(v); o.positionCS = TransformObjectToHClip(v.positionOS.xyz); return o; }
            half fd(V i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma vertex vs
            #pragma fragment fs
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };
            V vs(A v)
            {
                V o; UNITY_SETUP_INSTANCE_ID(v);
                float3 p = TransformObjectToWorld(v.positionOS.xyz);
                float4 cs = TransformWorldToHClip(ApplyShadowBias(p, TransformObjectToWorldNormal(v.normalOS), _LightDirection));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = cs; return o;
            }
            half4 fs(V i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
