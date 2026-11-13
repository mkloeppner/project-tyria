# Elemente – VFX-Lauf V1

## 1. Zuordnung

`SkillInventoryV1` (Teil von Rebuild Everything) setzt an jedem Skill `SimElementV1`, `SimSecondaryV1` und `Showpiece`; Inventar `Temp/PT_QA/V1/inventar.csv`
(Id, Klasse, Elite-Spezialisierung, Element, Sekundär, Typ, Dauer, Radius, Lagen, Phasen, Schaustück, Nutzung).

Reihenfolge der Hauptelement-Regel (`SimElements.Primary`): Kombo-Feld (Feuer, Wasser, Blitz, Rauch → Schatten) → Wort im Skillnamen (Feuer, Wasser) → Palette über die G1-Zuordnung, wobei Eis und Licht zur **Brechung** werden und die Wasser-Palette zu **Wasser** (G1 führte Wasser unter Eis). Arkan mit Linse/Spiegel/Kristall im Namen → Brechung.

| Element | Anteil (466 Skills) | Ziel |
|---|---|---|
| Wasser | 10,7 % | ≥ 10 % |
| Feuer | 20,8 % | ≥ 10 % |
| Brechung | 13,1 % | ≥ 12 % |

Sekundär-Beteiligungen: 0 nötig (alle Leuchttürme über Ziel). Die Regel ist vorhanden (`SimElements.Secondaries`): Kandidaten mit passendem Wort, dann Nachbar-Elemente, nie Umbenennung.

Typen (`SimElements.TypeOf`): Projektil, Strahl, Fläche, Nahkampf, Sprung, Wirbel, Aura, Beschwörung, Heilung, Blockade, Kette, Finisher. Blockade und Kette werden aus den Skill-Daten nicht erkannt (kein Feld dafür) und fallen auf Nahkampf/Projektil zurück.

## 2. Schaustücke

164 markiert: alle Elite-Skills und Finisher (Auftrag), je Klasse drei weitere (verschiedene Typen zuerst, dann Dauer, Radius, Leuchtturm-Element; Nutzungsdaten liegen nicht vor) und je Elite-Spezialisierung einer. Schaustücke bekommen ≥ 7 Lagen, alle anderen ≥ 4 (`SimElements.MinLayersV1`); die G1-Mindestwerte bleiben Untergrenze.

## 3. Gestaltungsregeln (Kurzfassung des Auftrags, gelten für jede Lage)

1. Das Element bestimmt die Physik (Wasser fällt, Feuer steigt, Licht bricht sofort). 2. Vorbereitung 100–300 ms (Instant 60 ms), Spitze 2–4 Bilder, Nachklang länger; Finisher Hit-Stop ≤ 60 ms. 3. Drei Geschwindigkeiten. 4. Kern (HDR ≥ 2), Körper unter der Bloom-Schwelle, Hof in Sekundärfarbe. 5. Bewegungsvektor-Überblendung, ≥ 24 FPS, Loop-Naht ≤ 2 %. 6. Welt-Kontakt hinterlässt etwas. 7. Licht mit Kurve an jeder Spitze. 8. Brechung am Rand stark, ≤ 24 px, Dispersion ≤ 6 px, Hitze ≤ 6 px. 9. Lesbarkeit bleibt Chef, Nachwirkung ≤ 40 % HDR-Fläche. 10. Klang synchron zur Spitze (`SimRecipe.PeakTime`). 11. Formen vor Partikeln. 12. Klassen bleiben erkennbar (`ClassFxStyle`, noch offen).

## 4. Stand

V1 (Werkzeuge) umgesetzt, V2–V7 (Element-Inhalte) offen – siehe PROGRESS und KNOWN_ISSUES.

## 5. Wasser (V2, erster Durchgang)

| Typ | Aufladen | Auswurf | Flug | Einschlag | Nachwirkung |
|---|---|---|---|---|---|
| Projektil / Strahl / Kette | Tropfen sammeln sich (GPU, Anziehung) | Spritzer-Flipbook, kleiner Ring | Linsen-Brechung | Spritzkrone, Spritzer, Wellenring | Pfütze (12 s), Tropfen |
| Fläche / Heilung | Boden wird nass | Wassersäule | Gischt-Loop | Wellenring auf Radius, Kaustik, Gischt-Nebel | Pfütze (16 s) |
| Aura / Blockade / Beschwörung | Wasserhaut (schwache Brechung) | Gischt | – | Ring | Tropfen, Pfütze (8 s) |
| Nahkampf / Sprung / Wirbel / Finisher | Gischt an der Waffe | Klingenbogen bzw. Säule (Sprung) | Bogen-Brechung | Spritzkrone, Ring | Pfütze (10 s) |

Schaustücke zusätzlich: starke Brechung (volle 24 px), Glitzer-Licht, Gischt-Nebel, Kaustik, seitlicher Spritzer. Auf Niedrig werden Brechungs-Lagen als halbtransparente Wasser-Meshes gezeigt, GPU-Partikel laufen über Shuriken.
