# Grafik – Project Tyrant (Grafik-Lauf G1–G7)

Stand 2026-11-12. Ergänzt ART_BIBLE (Stilregeln, Befunde), VFX (Baukasten, VFX 2.0) und PERFORMANCE (Budgets, Messungen).
Alles handgeschriebenes HLSL, Render Graph, kein Shader Graph, kein VFX Graph, keine Lightmaps/gebackenen Sonden.

## 1. Pipeline

| Teil | Umsetzung |
|---|---|
| Renderer | URP 17.6 Forward+, je Qualitätsstufe ein Paar `PT_URP_<Stufe>` / `PT_Renderer_<Stufe>` (Builder `GraphicsBuilder.BuildRendering`) |
| Tiefe | Depth Priming aus, `copyDepthMode = AfterOpaques` (Nebel/Kontur brauchen Tiefe auch ohne SSAO) |
| Post | `postProcessData` zugewiesen (war seit Lauf 1 leer → Volumes, Bloom, Tonemapping liefen nie). ACES, Farb-Anpassung, Weißabgleich, Split-Toning, Bloom, Vignette, Schatten/Mitten/Lichter; Bewegungsunschärfe aus |
| Renderer-Features | SSAO (DepthNormals, 0,9 / 0,35 m), Screen-Space-Decals, `PTRenderFeature` |
| `PTRenderFeature` | Vollbild-Passes aus `PT_PostFx.shader`: 0 Nebel, 1 Kontur (55–140 m Ausblendung), 2 Lichtstrahlen (16 Proben), 3 Verzerrung (Hitze + 8 Druckwellen, ein Puffer je Bild), 4 Unterwasser, 5 Schadensvignette; danach Overlay-Pass „PT Telegrafen“ (LightMode `PTOverlay`, nach dem Post-Processing) |
| Licht | Sonne/Mond aus `SceneLighting.Compute(Profil, Stunde, Wetter)`, Trilight-Umgebung als Globals (`_PT_AmbSky/Equator/Ground`), Echtzeit-Reflexionssonde nur mit Himmel (Zonenwechsel + alle 2 Spielstunden) |
| Belichtung | Zonen-Volume (Priorität 50) + Tageszeit-Volume (60, Zone + Tageszeit-Belichtung, nächtlicher Farbfilter). `AcesExposureOffset(Boden) = clamp(1 − 0,95 · L, −0,2, 1)` plus `ExposureCalibration` je Zone (Editor: `GraphicsCapture.CalibrateExposure`, Ziel-Luminanz, 3 Iterationen) |

## 2. Shader-Bibliothek (`Shaders/`)

Gemeinsam: `Include/PT_Common.hlsl` (Globals Wind/Sonne/Trilight/Nebel/Wetter/Nacht, Funktionen `PT_ApplyWind`, `PT_ToonRamp`, `PT_Trilight`, `PT_Rim`, `PT_FogAmount`, `PT_ApplyWeather`) und `Include/PT_ToonForward.hlsl` (Vertex/Fragment der Toon-Familie mit Haken `PT_SURFACE_HOOK`/`PT_LIGHT_HOOK`).

| Shader | Zweck |
|---|---|
| ToonLit | Standard für Figuren, Bauten, Requisiten |
| Terrain | Fels an Hängen, Variation 0,08, Bemalung 0,15 |
| Vegetation | Durchscheinen, beidseitig, Wind |
| Cloth | Stoff mit Wind |
| Lava | Kruste/Risse (Worley), HDR nur in Rissen |
| Crystal | Facetten-Normalen, Glühen |
| Hazard | Gefahrenflächen, Art + Randstreifen |
| Water2 / Glass | Wasser 2.0 mit Nebel im Shader, Glas |
| Sky | Verlauf, Sonne/Halo, Mond, 2 Wolkenschichten, Sterne, Polarlicht, Asche/Sand |
| GroundMarker (TelegraphUnlit) | Telegrafen nach dem Post-Processing, tiefensicher, Ringfarbe passt sich der geschätzten Bildschirmhelligkeit des Bodens an |
| UI/Glow, FX/Particle | UI-Leuchten, Partikel |

Jeder Material-Shader: `CBUFFER_START(UnityPerMaterial)`, Passes ForwardToon, ShadowCaster, DepthOnly, DepthNormals; ≤ 8 multi_compile je Pass. `ShaderLibraryValidator` prüft das; `MaterialMigrator.MigrateAll` (Teil von Rebuild Everything) setzt Familie und Standardwerte nach Materialname. Ausgemustert und vom Validator verboten: ToonRim, GradientSkybox, Water. `PTShaderStripper` entfernt im Build Varianten abgeschalteter Stufen-Features.

## 3. Qualitätsstufen

| Stufe | Schatten | SSAO | MSAA | Render-Skala | Lichtstrahlen / Verzerrung | VFX-Ebenen | Umgebung / Wetter-Partikel | Partikel-Deckel |
|---|---|---|---|---|---|---|---|---|
| Niedrig | 1 Kaskade, 40 m, 1024 | aus | 1× | 0,75 | aus / aus | 1 | 25 % / 30 % | 4 000 |
| Mittel | 2 Kaskaden, 80 m, 2048, weich | halbe Aufl. | 2× | 0,85 | an / an | 2 | 60 % / 60 % | 10 000 |
| Hoch (Standard) | 4 Kaskaden, 150 m, 2048, weich | halbe Aufl. | 4× | 1,0 | an / an | alle | 100 % / 100 % | 20 000 |
| Ultra | 4 Kaskaden, 200 m, 4096, weich hoch, Zusatzlicht-Schatten | volle Aufl. | 4× | 1,0 | an / an | alle | 130 % / 100 % | 30 000 |

Nebel, Kontur, Unterwasser und Schadensvignette laufen in allen Stufen (Lesbarkeit). Unity-Qualitätsstufen: Niedrig 1, Mittel 2, Hoch 3, Ultra 5.

Erststart: automatische Erkennung (`QualityTiers.Suggest`), Leistungs-Regler (45/55 FPS mit Hysterese) optional. Grafik-Reiter der Optionen: Stufe, Nebel, Kontur, Lichtstrahlen, Verzerrung, Wetter, Umgebung, Regler, **Lichtempfindlich** (Blitze ≤ 0,15, Bloom ≤ 0,35, kein Kamerawackeln, Verzerrung halb, UI-Pulsieren 30 %).

## 4. Wetter und Umgebung

10 Zustände (Klar, Bewölkt, Regen, Gewitter, Nieselnebel, Schnee, Schneesturm, Sandsturm, Aschefall, Jaderegen) je Zonenprofil gewichtet, Wechsel 6–14 min, 30 s Überblendung, wirkt auf Licht, Nebel, Wolken, Nässe/Schnee in Shadern und Partikelbetten. Deckel 30 % in Sprungrätseln und bei Champions. 150 Umgebungs-Volumen (≥ 4 je Zone), aktiv in 110 m, im 15-m-Kampfradius auf 40 % gedämpft.

## 5. Lesbarkeit vor Schönheit

* Gefahrenflächen (Telegrafen) liegen nach Nebel und Post-Processing → nie vernebelt, nie getonemappt.
* ΔE (CIE76) Ring gegen Boden, Ziel ≥ 25 (Farbenblind 20). Messung: `GraphicsCapture` (Kampf-Lesezeichen, Testgefahr r = 3 m). Werte und Ausreißer: PERFORMANCE.md §3, KNOWN_ISSUES.
* Boss- und Ranganzeigen (`RankAuraFx`), Zustände (`EffectAuraFx`) haben feste Elementfarben, die nicht mit Gefahr/Heilung kollidieren (`ElementPalette.Conflicts`).

## 6. Werkzeuge

| Werkzeug | Aufruf |
|---|---|
| Vorher/Nachher-Bilder je Lesezeichen | Editor `GraphicsCapture.CaptureAll(tag, zone, stunde)`, Wetter über `CaptureWeather`; Ausgabe `Temp/PT_QA/G1/<tag>/`, Kennzahlen `metrics_<tag>.csv` |
| Skill-/Boss-/Reittier-Showcase | `/vfx capture (Tag)` |
| Leistung je Lesezeichen | `/gfx perf (all|zone) [Tag]` → `perf_<tag>.csv` |
| Stufe, Features, Stresstest | `/gfx preset`, `/gfx feature`, `/gfx stress n` |
| Tageszeit, Wetter | `/tod`, `/weather` |
