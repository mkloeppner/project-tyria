# VFX – Project Tyrant (Lauf 5, Stand M27)

Alle Effekte sind Kompositionen aus dem Baukasten `VfxKitBuilder` (Shuriken-Systeme, Mesh-Effekte, Blitz-Linien, eigene HLSL-Shader) – kein VFX Graph, kein Shader Graph. Schlüssel „Form/Palette/Größe“ (`VfxIds.Compose`), abgelegt unter `Prefabs/VFX/<Palette>/<Form>_<Größe>.prefab`, Katalog `VfxCatalog`. **13 Formen × 27 Paletten × 4 Größen = 1 404 Prefabs.**

## Formen

| Form | Einsatz |
|---|---|
| Charge | Aufladung am Wirker (Aktivierungsbeginn) |
| Release | Auswurf am Wirker/Waffe |
| Projectile | Geschoss mit Schweif (kreist in der Galerie) |
| Beam | Strahl/Blitz-Linie |
| Cone | Kegel vor dem Wirker |
| GroundCircle | Bodenkreis: Ring = Fläche (Rand), Aufsteigen = Effekt |
| Pillar | Säule am Einschlagpunkt (Bodenziel ohne Feld) |
| Shockwave | Druckwelle um den Wirker (Selbst-Skills ≥ 3 m) |
| Aura | Aura/Segen am Ziel |
| Impact | Einschlag am Ziel |
| Trail | Spur/Bewegung (Sprünge, Ansturm) |
| Sigil | Siegel: Siegel = Fläche, Glimmen = Effekt (Bodenziel-Auswurf) |
| Veil | Schleier/Tarnung |

Zuordnung in der Skill-Zeitachse (`SkillTimelineBuilder`): Aufladung = Charge; Auswurf = Shockwave (Selbst, Radius ≥ 3 m) / Sigil (Bodenziel) / Release; Einschlag = GroundCircle (Bodenziel mit Feld) / Pillar (Bodenziel) / Impact (Schaden) / Aura; Nachwirkung = GroundCircle (Feld) oder kleine Aura (Selbst-Segen).

## Paletten

| Palette | Farbe (RGB 0–1) | Verwendung |
|---|---|---|
| Feuer | 1, 0.35, 0.08 | Kern (Wächter Glut, Elementarweber Feuer) |
| Wasser | 0.25, 0.65, 1 | Kern (Elementarweber Wasser) |
| Luft | 0.62, 0.52, 1 | Kern (Elementarweber Luft, Wächter Sturm) |
| Erde | 0.6, 0.45, 0.25 | Kern (Elementarweber Erde) |
| Natur | 0.45, 0.8, 0.3 | Kern (Wildhüter) |
| Schatten | 0.45, 0.4, 0.55 | Kern (Schattenklinge) |
| Licht | 0.98, 0.82, 0.35 | Kern (Wächter) |
| Blut | 0.75, 0.12, 0.15 | Kern (Gegner) |
| Arkan | 0.85, 0.35, 0.9 | Kern (Elementarweber) |
| Frost | 0.55, 0.85, 1 | Kern (Frostkamm) |
| Glut | – | Kern (Glutöde) |
| Mondlicht | 0.8, 0.88, 1 | Hainwächter |
| Wildblut | 0.7, 0.3, 0.15 | Bestienbund |
| Dorngift | 0.5, 0.9, 0.2 | Dornenwächter |
| Himmel | 0.55, 0.82, 1 | Sturmfalke |
| Gewitter | 0.88, 0.9, 1 | Sturmrufer |
| Zwielicht | 0.8, 0.5, 0.75 | Weber |
| Jade | 0.35, 0.9, 0.55 | Katalysator |
| Lava | 1, 0.42, 0.08 | Glutbinder |
| Pulver | 0.85, 0.15, 0.15 | Wegelagerer |
| Staubgold | 0.92, 0.75, 0.4 | Phantomtänzer |
| Tiefviolett | 0.5, 0.2, 0.8 | Schattenhüter |
| Stahl | 0.55, 0.88, 0.95 | Klingenwirbel |
| Richtglut | 1, 0.7, 0.25 | Glutrichter |
| Bollwerk | 0.6, 0.75, 1 | Bollwerkmeister |
| Sturmblau | 0.3, 0.6, 1 | Sturmherold |
| Smaragd | 0.3, 0.85, 0.45 | Dornenschild |

## Größen und Budgets

| Größe | Max. Partikel je Komposition | Einsatz |
|---|---|---|
| S | 80 | Auto-Angriffe, kleine Treffer |
| M | 160 | Waffen-Skills 2–5, Hilfs-Skills |
| L | 320 | große Flächen, Heilung |
| Elite | 640 | Elite-Skills, Signatur-Skills der Spezialisierungen |

Pool (`VfxPool` am GameManager-Objekt): Gesamtbudget 20 000 Partikel (überzählige Effekte werden übersprungen und gezählt), max. 8 geliehene Punktlichter. Effektdichte Voll / Reduziert / Minimal = 1 / 0,5 / 0,2 (Emission und Bursts). Dauer ≤ 3 s außer Feldern und Auren (Test).

## Lesbarkeit

- Intensität (Alpha) nach Besitzer: Spieler 0,75, Verbündete 0,6, Gegner 1,0 – Gegner-Telegrafen bleiben am auffälligsten.
- Fix-Lauf F1: Flächen-Kompositionen tragen `VfxAreaLayer` (Rand = `AreaOutline`, Effekt = `AreaEffect`, 216 Prefabs). Optionen: eigene Skills „Fläche und Effekt“ / „Nur Effekt“; Verbündete Voll / Nur Effekt / Reduziert (Standard) / Aus; Gegner-Telegrafen nur mit Warnung abschaltbar. Combo-Felder behalten einen dünnen pulsierenden Rand.
- Farbenblind-Modus für Telegrafen (Blau–Gelb statt Rot–Orange).

## Shader (`Shaders/FX/`)

| Shader | Zweck |
|---|---|
| PT_FxParticle | Partikel additiv/Alpha mit Soft-Particles (additiv ohne Alpha²-Gewichtung) |
| PT_FxMesh | Mesh-Effekte mit UV-Scroll, Fresnel-Rand, Dissolve-Kante |
| PT_FxDistort | Hitzeflimmern/Verzerrung über die Opaque-Textur |
| PT_Water | Seen und Lava (Wellen, Schaum, Emission) |
| PT_AdditiveTrail | Waffen-Trails an Klingenspitze/Stabkopf |
| PT_Outline | Ziel-Umriss (Inverted Hull) in Rangfarbe |

## Welt-VFX

Wetter-Betten folgen der Kamera (Pollen Sonnfurt, Nebelschwaden Nebelmoor, Schneetreiben Frostkamm, Aschewind mit Funkenflug Glutöde), Glühwürmchen/Blätter/Schmetterlinge, Boid-Vögel, Vertex-Wind im Toon-Shader (`_PT_Wind`), Kaminrauch, Laternen-Licht, Wasser- und Lava-Shader. Zustands-Tints (`HitFlash`): Brennen orange, Kühle blau, Gift grün, Blutung rot.

## Galerie und QA

- Szene `Scenes/QA/PT_VfxGallery.unity` (vom Builder erzeugt): alle 13 Formen × Paletten in Schleife; Trail/Projektil kreisen.
- `VfxGalleryCapture` schreibt Paletten-Captures nach `Temp/PT_QA/` (über `ScreenCapture`, weil Kamera-Renders keine Opaque-Textur haben).
- Signatur-Captures der Spezialisierungen: `Temp/PT_QA/m26_<spez>.png` (Duell-Bot).
- Tests: Komposition → Prefab für alle Schlüssel, Partikelbudget je Größe, 3-s-Regel, Paletten vollständig, jeder Skill hat auflösbare VFX, Flächen-Ebenen je Boden-Komposition.

