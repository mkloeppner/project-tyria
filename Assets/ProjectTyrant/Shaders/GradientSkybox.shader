// Project Tyrant – Gradient-Skybox (Horizont warm, Zenit kühl, weiche Sonnenscheibe).
Shader "ProjectTyrant/GradientSkybox"
{
    Properties
    {
        _TopColor ("Zenith", Color) = (0.30,0.52,0.85,1)
        _HorizonColor ("Horizon", Color) = (0.98,0.80,0.58,1)
        _BottomColor ("Ground", Color) = (0.45,0.42,0.50,1)
        _HorizonSharpness ("Horizon Sharpness", Range(0.5,8)) = 2.2
        _SunColor ("Sun Color", Color) = (1,0.9,0.7,1)
        _SunSize ("Sun Size", Range(0.0,0.2)) = 0.04
        _SunGlow ("Sun Glow", Range(0,4)) = 1.2
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float4 _TopColor, _HorizonColor, _BottomColor, _SunColor;
            float _HorizonSharpness, _SunSize, _SunGlow;
            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            V vert(A v) { V o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.dir = v.positionOS.xyz; return o; }
            half4 frag(V i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = d.y;
                float t = pow(saturate(up), 1.0 / _HorizonSharpness);
                float3 sky = lerp(_HorizonColor.rgb, _TopColor.rgb, t);
                float b = pow(saturate(-up), 0.5);
                sky = lerp(sky, _BottomColor.rgb, b);
                Light l = GetMainLight();
                float sd = dot(d, l.direction);
                float sun = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.6, sd);
                float glow = pow(saturate(sd), 32.0) * _SunGlow * 0.35;
                sky += _SunColor.rgb * (sun * 2.0 + glow);
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
}
