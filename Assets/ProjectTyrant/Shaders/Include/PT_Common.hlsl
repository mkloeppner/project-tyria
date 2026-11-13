// Project Tyrant – gemeinsame Shader-Bausteine (Grafik-Lauf G2/G3): Wind, Toon-Licht, Rim, Trilight-Umgebung, Höhennebel, Wetter.
// Globale Werte setzt SceneLighting (C#) je Bild. Keine Materialeigenschaften hier – jeder Shader deklariert sein eigenes CBUFFER(UnityPerMaterial).
#ifndef PT_COMMON_INCLUDED
#define PT_COMMON_INCLUDED

// Gemeinsame Materialfelder der Toon-Familie (in das CBUFFER(UnityPerMaterial) jedes Shaders einsetzen)
#define PT_TOON_PROPS float4 _BaseColor; float4 _BaseMap_ST; float _PaintStrength; float _WorldUVScale; float4 _ShadowColor; \
    float _ShadowThreshold; float _ShadowSoftness; float _MidTone; float4 _RimColor; float _RimPower; float _RimStrength; float _HeightGradient; \
    float4 _GradientBottom; float4 _EmissionColor; float _NightEmission; float _UseVertexColor; float4 _AlbedoMap_ST; float _Wind; float _WindPivot; float _WeatherMask;

// ---------------- Globale Werte ----------------
float4 _PT_Wind;          // xy Richtung (normiert), z Stärke (m), w Böen-Tempo
float4 _PT_SunDir;        // xyz zur Sonne, w Sonnenhöhe 0..1
float4 _PT_SunColor;      // rgb Lichtfarbe, a Intensität
float4 _PT_AmbSky;        // Trilight-Umgebung (linear)
float4 _PT_AmbEquator;
float4 _PT_AmbGround;     // a = Unterlicht (Lava)
float4 _PT_FogColor;      // rgb, a = an (1) / aus (0)
float4 _PT_FogSunColor;   // rgb Streulicht, a = maximale Deckung
float4 _PT_FogParams;     // x Dichte, y Höhenbasis (Welt-Y), z Höhen-Abfall, w Sonnenstreuung
float4 _PT_Weather;       // x Nässe, y Schnee, z Staub, w Blitz (0..1, bereits lichtempfindlich gedeckelt)
float4 _PT_Night;         // x Emissions-Stufe (Laternen), y Nacht 0..1, z Sternen-Alpha, w frei

// ---------------- Wind ----------------
// Auslenkung im Objektraum; Höhe 0 bleibt stehen. pivot > 0: hängend (Banner).
float3 PT_ApplyWind(float3 posOS, float wind, float pivot)
{
    if (wind <= 0.001) return posOS;
    float3 wpos = TransformObjectToWorld(posOS);
    float h = pivot > 0 ? saturate((pivot - posOS.y) * 0.8) : saturate(posOS.y * 0.35);
    float t = _Time.y * max(_PT_Wind.w, 1.0);
    float gust = sin(t * 1.7 + dot(wpos.xz, float2(0.13, 0.21))) * 0.6 + sin(t * 3.9 + wpos.x * 0.7) * 0.25;
    float3 dirW = float3(_PT_Wind.x, 0, _PT_Wind.y) * (_PT_Wind.z * (0.6 + gust)) * wind * h;
    return posOS + mul((float3x3)GetWorldToObjectMatrix(), dirW);
}

// ---------------- Licht ----------------
// Toon-Stufe: harte Hell-Dunkel-Kante, schmale Mittelbande, farbige Schatten.
half PT_ToonRamp(float ndl, float shadowAtten, float threshold, float softness, float midTone)
{
    float lit = smoothstep(threshold - softness, threshold + softness, ndl);
    float band = smoothstep(midTone - 0.05, midTone + 0.05, ndl) * 0.15;
    return saturate(lit * smoothstep(0.2, 0.6, shadowAtten) + band * lit);
}

half3 PT_Trilight(float3 n)
{
    float up = n.y;
    half3 a = lerp(_PT_AmbEquator.rgb, _PT_AmbSky.rgb, saturate(up));
    a = lerp(a, _PT_AmbGround.rgb, saturate(-up));
    // Unterlicht (Lava, Glut): von unten warm, auch auf Seitenflächen
    a += _PT_AmbGround.rgb * _PT_AmbGround.a * saturate(-up * 0.7 + 0.3);
    return a;
}

half PT_Rim(float3 n, float3 viewDir, float3 lightDir, float power)
{
    float rim = pow(1.0 - saturate(dot(viewDir, n)), power);
    return rim * saturate(dot(n, lightDir) * 0.5 + 0.6);
}

// ---------------- Nebel ----------------
// Exponentieller Höhennebel (analytisches Integral entlang des Sichtstrahls) mit Sonnenstreuung und Deckelung.
half PT_FogAmount(float3 posWS, float3 camPos, out half sun)
{
    float3 v = posWS - camPos;
    float d = max(length(v), 1e-3);
    float fall = max(_PT_FogParams.z, 1e-4);
    float y0 = camPos.y - _PT_FogParams.y;
    float dy = v.y;
    float k = fall * dy;
    float heightTerm = exp(-fall * y0) * (abs(k) > 1e-3 ? (1.0 - exp(-k)) / k : 1.0);
    float f = 1.0 - exp(-_PT_FogParams.x * min(heightTerm, 8.0) * d);
    sun = pow(saturate(dot(v / d, _PT_SunDir.xyz)), 8.0) * _PT_FogParams.w * saturate(_PT_SunDir.w * 4.0);
    return saturate(f) * _PT_FogSunColor.a;
}

half3 PT_ApplyFog(half3 col, float3 posWS)
{
    if (_PT_FogColor.a < 0.5) return col;
    half sun;
    half f = PT_FogAmount(posWS, _WorldSpaceCameraPos, sun);
    half3 fc = lerp(_PT_FogColor.rgb, _PT_FogSunColor.rgb, sun);
    return lerp(col, fc, f);
}

// ---------------- Wetter ----------------
// Nässe dunkelt und glättet nach oben zeigende Flächen, Schnee legt sich auf Oberseiten.
half3 PT_ApplyWeather(half3 albedo, float3 n, float3 posWS)
{
    float up = saturate(n.y);
    albedo *= lerp(1.0, 0.72, _PT_Weather.x * up);
    float snowMask = smoothstep(0.55, 0.85, up) * _PT_Weather.y;
    albedo = lerp(albedo, half3(0.92, 0.95, 1.0), snowMask);
    albedo = lerp(albedo, albedo * half3(1.05, 0.95, 0.8), _PT_Weather.z * 0.4);
    return albedo;
}

#endif
