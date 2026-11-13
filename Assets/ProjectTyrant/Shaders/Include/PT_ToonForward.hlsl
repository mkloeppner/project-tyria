// Project Tyrant – gemeinsamer Toon-Forward-Pass (Grafik-Lauf G3) für ToonLit und seine Familie (Terrain, Vegetation, Stoff, Lava, Kristall).
// Der einbindende Shader definiert vor dem Include:
//   CBUFFER mit PT_TOON_PROPS (+ eigene Felder), optional PT_SURFACE_HOOK (void PT_Surface(inout PTSurface s, PTVaryings i))
//   und PT_LIGHT_HOOK (half3 PT_ExtraLight(PTSurface s, Light l, float3 viewDir)).
#ifndef PT_TOON_FORWARD_INCLUDED
#define PT_TOON_FORWARD_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Assets/ProjectTyrant/Shaders/Include/PT_Common.hlsl"

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_AlbedoMap); SAMPLER(sampler_AlbedoMap);

struct PTAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PTVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 color : TEXCOORD2;
    float objectY : TEXCOORD3;
    float2 uv : TEXCOORD4;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct PTSurface
{
    half3 albedo;
    float3 normal;
    half3 emission;
    half shadowTint;   // 1 = Material-Schattenfarbe
    half rim;          // Faktor auf _RimStrength
    float3 positionWS;
};

#if defined(PT_SURFACE_HOOK)
void PT_Surface(inout PTSurface s, PTVaryings i);
#endif
#if defined(PT_LIGHT_HOOK)
half3 PT_ExtraLight(PTSurface s, Light l, float3 viewDir);
#endif

PTVaryings PTToonVert(PTAttributes v)
{
    PTVaryings o = (PTVaryings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    float3 posOS = PT_ApplyWind(v.positionOS.xyz, _Wind, _WindPivot);
    VertexPositionInputs pos = GetVertexPositionInputs(posOS);
    o.positionCS = pos.positionCS;
    o.positionWS = pos.positionWS;
    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
    o.color = v.color;
    o.objectY = v.positionOS.y;
    o.uv = TRANSFORM_TEX(v.uv, _AlbedoMap);
    return o;
}

half3 PT_BaseAlbedo(PTVaryings i, float3 n)
{
    half3 albedo = _BaseColor.rgb;
    if (_UseVertexColor > 0.5) albedo *= lerp(half3(1,1,1), i.color.rgb, i.color.a);
    albedo *= SAMPLE_TEXTURE2D(_AlbedoMap, sampler_AlbedoMap, i.uv).rgb;
    float3 bw = pow(abs(n), 4); bw /= (bw.x + bw.y + bw.z + 1e-4);
    half paint = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.positionWS.zy * _WorldUVScale).r * bw.x
               + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.positionWS.xz * _WorldUVScale).r * bw.y
               + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.positionWS.xy * _WorldUVScale).r * bw.z;
    albedo *= lerp(1.0, 0.75 + paint * 0.5, _PaintStrength);
    float g = saturate(i.objectY * 0.5 + 0.5);
    albedo *= lerp(lerp(half3(1,1,1), _GradientBottom.rgb, _HeightGradient), half3(1,1,1), g);
    return albedo;
}

half4 PTToonFrag(PTVaryings i, bool frontFace)
{
    UNITY_SETUP_INSTANCE_ID(i);
    PTSurface s;
    s.normal = normalize(i.normalWS) * (frontFace ? 1.0 : -1.0);
    s.albedo = PT_BaseAlbedo(i, s.normal);
    s.emission = _EmissionColor.rgb * lerp(1.0, _PT_Night.x, _NightEmission);
    s.shadowTint = 1; s.rim = 1; s.positionWS = i.positionWS;
    #if defined(PT_SURFACE_HOOK)
    PT_Surface(s, i);
    #endif
    #if defined(_DBUFFER)
    ApplyDecalToBaseColor(i.positionCS, s.albedo);
    #endif
    s.albedo = lerp(s.albedo, PT_ApplyWeather(s.albedo, s.normal, i.positionWS), _WeatherMask);

    InputData inputData = (InputData)0;
    inputData.positionWS = i.positionWS;
    inputData.normalWS = s.normal;
    inputData.viewDirectionWS = normalize(GetWorldSpaceViewDir(i.positionWS));
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
    inputData.shadowCoord = TransformWorldToShadowCoord(i.positionWS);
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);

    Light mainLight = GetMainLight(inputData.shadowCoord, i.positionWS, half4(1,1,1,1));
    half lit = PT_ToonRamp(dot(s.normal, mainLight.direction), mainLight.shadowAttenuation, _ShadowThreshold, _ShadowSoftness, _MidTone) * mainLight.distanceAttenuation;
    lit *= ao.directAmbientOcclusion;
    half3 shadowCol = s.albedo * lerp(half3(1,1,1), _ShadowColor.rgb, s.shadowTint) * 0.85;
    half3 col = lerp(shadowCol, s.albedo * mainLight.color, lit);
    col += s.albedo * PT_Trilight(s.normal) * 0.35 * ao.indirectAmbientOcclusion;
    #if defined(PT_LIGHT_HOOK)
    col += PT_ExtraLight(s, mainLight, inputData.viewDirectionWS);
    #endif

    #if defined(_ADDITIONAL_LIGHTS)
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light l = GetAdditionalLight(lightIndex, i.positionWS, half4(1,1,1,1));
        col += s.albedo * l.color * l.distanceAttenuation * l.shadowAttenuation * smoothstep(0.0, 0.3, dot(s.normal, l.direction)) * 0.8;
    LIGHT_LOOP_END
    #endif

    col += _RimColor.rgb * PT_Rim(s.normal, inputData.viewDirectionWS, mainLight.direction, _RimPower) * _RimStrength * s.rim;
    col += s.emission;
    return half4(col, 1);
}

#endif
