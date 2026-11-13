# Leistung – Project Tyrant (Grafik-Lauf G7)

Messung: `/gfx perf all <Tag>` im Play Mode des Editors, 180 Kamera-Lesezeichen in 30 Zonen, ungedrosselt (vSync 0, kein Zielwert), je Lesezeichen Mittel und 1-%-Tief der FPS, CPU-Zeit, Draw Calls, SetPass, Dreiecke, Partikel. Rohdaten: `Temp/PT_QA/G1/perf_vorher.csv` (vor dem Grafik-Lauf, Post-Processing lief damals nicht), `perf_nachher.csv` (Hoch), `perf_nachher_niedrig.csv` (Niedrig). GPU-Zeiten im Editor sind nicht belastbar (KNOWN_ISSUES 81, 93).

## 1. Budgets (Stufe Hoch)

| Budget | Ziel | Ergebnis | Status |
|---|---|---|---|
| FPS | ≥ 60 | min 151, Median 413 (180/180 Lesezeichen ≥ 60) | erfüllt |
| Draw Calls | ≤ 2 500 | Median 810, 95 % 1 355, max 2 518 (Tyrannenfeste Horizont) | 179/180 |
| Shader-Varianten | ≤ 4 000 | 756 (Editor-Zählung, vor Build-Stripping) | erfüllt |
| Gefahr-ΔE | ≥ 25 Tag/Nacht/Wetter | Tag 23/30, Nacht 27/30, Regen 24/30 | teilweise (§3) |
| Nachher-Bilder verbessert | ≥ 90 % | 163/180 = 90,6 % | erfüllt |
| Neue Tests | ≥ 60 | +184 (inkl. TestCases), 960/960 grün | erfüllt |

## 2. Vergleich

| Lauf | FPS min | FPS Median | 1-%-Tief Median | Draw Calls Median / max | CPU ms Median | Dreiecke Median |
|---|---|---|---|---|---|---|
| vorher (Lauf vor G1) | 166 | 331 | 168 | 414 / 2 004 | 3,05 | 2,9 Mio. |
| nachher Hoch | 151 | 413 | 224 | 810 / 2 518 | 2,42 | 5,9 Mio. |
| nachher Niedrig | 91 | 443 | 241 | 392 / 866 | 2,25 | 2,6 Mio. |

Mehr Draw Calls und Dreiecke auf Hoch kommen aus 4 Schattenkaskaden bis 150 m, DepthNormals für SSAO und den Umgebungs-Volumen. Die CPU-Zeit sank trotzdem (SRP-Batcher-fähige Toon-Familie, Material-Migration auf 9 Shader-Familien). Sonnfurt Horizont: G2 3 488 → 1 810 Draw Calls.

Einziger Ausreißer im 1-%-Tief: Silberküste Wahrzeichen 6,6 FPS (vorher 6,9) beim ersten Lesezeichen nach dem Zonen-Laden (Shader-Aufwärmen, KNOWN_ISSUES 88).

## 3. Lesbarkeit der Gefahrenflächen (ΔE CIE76, Ring gegen Boden)

| Kampf-Lesezeichen | ≥ 25 | unter 25 |
|---|---|---|
| Tag (11 Uhr) | 23/30 | Glutöde 19, Aschenkessel 14, Dünenmeer 23, Kristalltiefe 21, Himmelsspitzen 9*, Tyrannenfeste 22, Zarathis 20 |
| Nacht (23 Uhr) | 27/30 | Kristalltiefe 19, Himmelsspitzen 12*, Zarathis 25 (gerundet, knapp darunter) |
| Regen (11 Uhr) | 24/30 | Glutöde 21, Glutkessel 17, Dünenmeer 21, Große Leere 24, Himmelsspitzen 9*, Zarathis 21 |

\* Messartefakt: der Testring liegt an einer Felskante. Vorher-Werte aus G1 sind wegen des damaligen Seitenverhältnis-Fehlers nicht vergleichbar (DECISIONS G7).

## 4. Stellschrauben

* Stufe, einzelne Features und Leistungs-Regler im Grafik-Reiter; Regler senkt bei < 45 FPS Umgebung/Wetter/VFX-Ebenen und hebt bei > 55 FPS wieder an.
* Schattenweite und Kaskaden je Stufe in `TierSettings.For` (Builder überträgt sie in die URP-Assets).
* `PTShaderStripper` entfernt im Build Varianten, deren Feature in keiner Stufe aktiv ist.
