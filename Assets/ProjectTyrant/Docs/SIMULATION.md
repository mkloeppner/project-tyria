# Simulation – Element-Baukasten (VFX-Lauf V1)

Alle Simulationen laufen im Editor; zur Laufzeit gibt es nur GPU-Partikel mit einfachen Kräften (nie einen Fluid-Löser).
Code: `Scripts/Visuals/ElementSim/` (reines C#, EditMode-getestet), Editor: `Scripts/Editor/ElementSim/`.

## 1. Fluid-Löser (`FluidSolver2D`)

Stable Fluids (Stam 1999) auf N × N Zellen (8–512, Rezepte 128):

| Schritt | Verfahren |
|---|---|
| Kräfte | Auftrieb `Buoyancy × Temperatur`, Gewicht `Weight × Dichte`, Schwerkraft `Gravity × min(Dichte, 1)` (Wasser), Dämpfung |
| Wirbelstärke | Vorticity Confinement (`Vorticity`) für Flammenzungen und Rauchlocken |
| Diffusion / Projektion | Gauss-Seidel, `Iterations` (Standard 20) |
| Advektion | semi-Lagrange, bilinear |
| Skalare | Dichte und Temperatur, Abkühlung `Cooling`, Ausblenden `DensityFade` |
| Hindernisse | Masken-Zellen ohne Geschwindigkeit und Dichte (Boden für Spritzer, `FloorHeight`) |
| Emitter | Punkt, Linie, Ring, Maske (z. B. aus `Sdf.Mask`); Start/Ende als Anteil der Dauer, Flackern `Jitter` aus dem Seed |

Determinismus: eigener Zufall `SimRng` (xorshift32), feste Schrittweite `Dt`, keine Zeitabhängigkeit → zwei Läufe erzeugen bitgleiche Felder und denselben Ergebnis-Hash.

## 2. Flipbook-Baker (`FlipbookBaker`, `ElementSimBuilder`)

| Ausgabe | Inhalt | Format |
|---|---|---|
| `<id>_farbe.png` | Rampe aus Temperatur (Feuer) oder Dichte, Alpha = Dichte × `AlphaScale` | sRGB, ≤ 2048², 8 × 8 Kacheln |
| `<id>_normalen.png` | Gradient der Dichte → Normalen (beleuchteter Rauch, Wasser-Relief) | linear |
| `<id>_bewegung.png` | Geschwindigkeit (rg, 0,5 = 0), halbe Kachelauflösung | linear |
| `<id>_emissiv.png` | Kern-Maske über `CoreThreshold` | linear |

* **Burst:** Start aus Ruhe, Emitter nach Zeitfenster, die letzten 20 % blenden auf leer.
* **Loop:** Simulation läuft Bilder + 25 % weiter, die ersten 25 % werden mit dem Überhang überblendet; Naht = ein gewöhnlicher Bildwechsel (Test), Abnahme ≤ 2 %.
* **Inkrementell:** `SimRecipe.ParamHash` (FNV-1a über alle Parameter, Rampe und Baker-Version); Rebuild Everything backt nur Rezepte mit geändertem Hash oder fehlenden Dateien.
* **Prüfungen beim Backen:** Atlas ≤ 2048, ≤ 8 × 8 Kacheln, Wiedergabe ≥ 24 FPS, mindestens ein Emitter, Loop-Naht ≤ 2 % (Hinweis im Bericht).
* **Spitzenzeit:** Bild mit der höchsten Energie (`PeakTime`) – für Klang-Transiente und Licht-Spitze (Regel 10).

Rezepte (`Generated/Flipbooks/`): v1_test_burst, v1_test_loop, feuer_flammen_loop, feuer_explosion_burst, rauch_loop, wasser_spritzer_burst, wasser_gischt_loop. Texturbudget aktuell 22,8 MB (Budget 256 MB).

## 3. Generatoren

| Werkzeug | Zweck |
|---|---|
| `Sdf` | Kreis, Ring, Kegel, Stern, Klinge (Sichel), Glyphe, Tropfen; Union, Subtraktion, weiche Vereinigung, weiche Kante, Rausch-Verzerrung; `Sdf.Mask` als Emitter-Maske |
| `CausticGenerator` | gewellte Oberfläche aus 5 periodischen Wellen, Brechung auf den Boden (Vorwärts-Splatten), nahtlos in Raum und Zeit |
| `BoltGenerator` | Mittelpunkt-Verschiebung mit Verzweigungen, Ast-Dicke je Ebene kleiner, Zittern je Frame (`frame`) |
| `FxMeshKit` | Spritzkrone, Wellenring, Wassersäule, Flammenzunge, Feuerwand, Schockwellen-Scheibe, Linsenkugel, Prisma, Lichtkegel, Ranke, Splitter (Voronoi 8–24), Klingenbogen, Blitzast; Low Poly, 1 m, Animation über UV im Shader |
| `MotionMetrics` | mittlere Bild-Differenz, Richtungs-Kohärenz (Block-Matching 8 × 8, ±3 px), Anteil toter Bilder |

## 4. GPU-Partikel (`GpuParticleSystem`, `Shaders/Compute/PT_GpuParticles.compute`)

Puffer mit 48 Byte je Partikel (Position, Alter, Geschwindigkeit, Lebensdauer, Größe, Seed, Zustand). Kernels `Emit` (Punkt, Kugel, Kegel, Kreisrand, Linie) und `Update` (Schwerkraft, Wind aus `_PT_Wind`, Luftwiderstand, Wirbel um die Achse, Anziehung/Abstoßung, Wirbel-Rauschen, Kollision gegen den Tiefenpuffer: Tropfen bleiben liegen, Funken springen). Darstellung `ProjectTyrant/FX/GpuParticle` (Billboard oder gestreckt) über `Graphics.RenderPrimitives`.

Budget gesamt: Ultra 200 000, Hoch 100 000. Mittel, Niedrig oder ohne Compute: Shuriken-Fallback mit 10 % (Niedrig), 25 % (Mittel), 35 % (Hoch ohne Compute) der Emission – gleiche Farben, Lebensdauer, Form, Kollision. CPU-Referenz `GpuParticleModel` rechnet dieselben Kräfte (Tests).

## 5. Verzerrungs-Puffer (`DistortionAccumulator`)

Ein RGBA16F-Puffer je Bild (rg Verschiebung in UV, b Dispersion, a Deckung). Pass „PT Verzerrung sammeln“ (nach Opaque) zeichnet alle Materialien mit LightMode `PTDistort` (Shader `ProjectTyrant/FX/Refract`) additiv; Tiefen-Test gegen die Opaque-Tiefe im Shader. Ein Blit (PT_PostFx Pass 3, vor den transparenten Partikeln) wendet Puffer, Hitzeflimmern und Druckwellen gemeinsam an: Verschiebung ≤ 24 px, Farbtrennung ≤ 6 px (1080p), Lichtempfindlich halbiert. Quellen melden sich über `DistortionSource` an; ohne Quelle entfällt der Pass.

## 6. Flipbook-Shader (`ProjectTyrant/FX/Flipbook`)

Bewegungsvektor-Überblendung zweier Bilder, Normalen-Beleuchtung (Hauptlicht + Himmel), Emissiv-Kern mit HDR ≥ 2 (Körper bleibt unter der Bloom-Schwelle), Soft-Particles, vormultipliziertes Alpha (additiv über `_Additive`), Zeit aus Partikel-Alter (Shuriken-Stream AgePercent in TEXCOORD0.z) oder Laufzeit.
