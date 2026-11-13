# Art Bible – Project Tyrant

Stand: Grafik-Lauf G1 (2026-11-11). Grundlage: 180 Vorher-Captures (`Temp/PT_QA/G1/vorher/<Zone>/<Lesezeichen>.png`), `metrics_vorher.csv`, Kontaktbögen `Temp/PT_QA/G1/sheets/sheet_vorher_0..5.jpg`.
Datenquelle aller Werte unten: `ZoneArtProfile`-Assets unter `Data/Graphics/ZoneArt/` (erzeugt von `GraphicsBuilder`). Diese Datei beschreibt die Regeln, die Assets sind die Wahrheit.

## 1. Stilregeln

1. **Lesbarkeit vor Schönheit.** Schadensfelder, Trotzbalken, Hürden-Decals und Fertigkeits-Icons gewinnen jeden Konflikt mit Atmosphäre. Gefahrenfelder müssen gegen den Boden ΔE ≥ 25 (CIE76) erreichen, im Farbenblind-Modus ≥ 20, bei Tag und Nacht. Nebel, Wetter und Post-Processing dürfen ein Schadensfeld nie unter diese Schwelle drücken.
2. **Reservierte Farben.** Gefahr = Orange-Rot (1.00, 0.32, 0.12), Heilung = Grün, Verbündete = Blau, Telegrafie = Orange-Gelb. Keine Zonenpalette darf einen Hauptton mit ΔE < 20 zu Gefahr oder Heilung führen (`DangerLanguage.Conflicts`, geprüft von `Profile_CompleteAndSafe`).
3. **Low-Poly bleibt Low-Poly.** Facetten sind Stil, nicht Fehler. Tiefe entsteht durch Licht (Toon-Ramp, Rim, Unterlicht), Nebelstaffelung und Konturen – nicht durch Normal Maps oder Unterteilung.
4. **Drei Ebenen pro Bild.** Vordergrund kräftig gesättigt mit Kontur, Mittelgrund leicht entsättigt, Hintergrund vom Höhennebel in die Horizontfarbe gezogen. Das Wahrzeichen bleibt in der Silhouette erkennbar (Nebel nie mehr als 60 % Deckung bis zur Wahrzeichen-Distanz).
5. **Licht hat Richtung.** Jede Zone hat einen Sonnenazimut und eine Mittagshöhe; Schatten sind farbig (Schattenfarbe des Profils), nie grau. Ambient als Trilight (Himmel/Horizont/Boden).
6. **Himmel erzählt den Biom-Charakter.** Zenit-Horizont-Verlauf, Wolkenbedeckung, Sterne/Polarlicht/Asche/Sandschleier je Profil. Kein einfarbiger Himmel.
7. **Wasser ist ein Material, keine Fläche.** Flach → tief über Tiefe, Kaustiken in Ufernähe, Schaumrand, Fresnel-Himmel.
8. **Weiß und Schwarz sind teuer.** Geclippter Anteil (Luminanz ≥ 0,98) ≤ 2 %, Schwarzanteil ≤ 5 % außer in Höhlen und Nacht. Schnee wird über Schattenfarbe und Exposure geführt, nicht über Helligkeit.
9. **Effekte sprechen die Elementsprache.** Jedes der 10 Elemente (`ElementPalette`) hat Primär-, Sekundär- und HDR-Kernfarbe, eine Partikelform und eine Bewegung. Ein Feuer-Skill sieht in jeder Klasse wie Feuer aus.
10. **Barrierefreiheit ist Teil des Stils.** Lichtempfindlich-Modus (Blitzsprung ≤ 0,1 Luminanz/Frame, Bloom ≤ 0,35), Farbenblind-Paletten, Bewegungsunschärfe aus.

Kennzahlen (Ziel nach G7 gegenüber vorher): RMS-Kontrast ↑, Kantendichte ↑, geclippter Anteil ↓, Farbcluster ≥ 8 im Freien, Gefahr-ΔE ≥ 25.

## 2. Biomtabelle

Zonen erben das Biom und verschieben den Farbton (Spalte „Zonen“, Verschiebung in Grad).

| Biom | Boden | Vegetation/Fels | Akzent | Licht | Schatten | Sonne Az/Höhe/Stärke | Nebel Dichte/Höhe | Sättigung Ziel | Wetter | Umgebung | Zonen |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Wiese | #6f9a46 | #3f7a34 | #e9c46a | #fff0d4 | #5a6aa0 | 140° / 58° / 1,35 | 0,0028 / 10 m | 0,45 | Klar, Bewölkt, Regen, Gewitter | Pollen, Schmetterlinge, Blätter, Vögel, Glühwürmchen | sonnfurt 0, weite_von_ahrn +22, gruener_schlund −24 |
| Moor | #4d5a34 | #5b4632 | #d8d05a | #ffd9a0 | #4a3f6e | 220° / 28° / 1,10 | 0,0070 / 6 m | 0,34 | Nieselnebel, Bewölkt, Regen, Klar | Glühwürmchen, Sporen, Tropfen, Lichtstaub, Vögel | nebelmoor 0, daemmerwald +24, nebelkamm −22, schilfmuendung +40 |
| Frost | #e8eef4 | #6f7a88 | #f0a8c0 | #e8f2ff | #6878b8 | 160° / 32° / 1,45 | 0,0035 / 14 m | 0,22 | Klar, Schnee, Schneesturm, Bewölkt | Schneeflocken, Windstreifen, Lichtstaub, Vögel | frostkamm 0, weisse_stille +22 |
| Glut | #2a2224 | #5a3226 | #e8c840 | #ffb070 | #3a2848 | 250° / 40° / 1,05, Unterlicht 0,45 | 0,0050 / 5 m | 0,48 | Aschefall, Klar, Bewölkt | Glut, Asche, Rauch, Windstreifen | gluetoede 0, aschenkessel +20, glutkessel −20 |
| Küste | #e2cf9a | #3e8a5a | #e86a5a | #fff6e0 | #4a7aa8 | 120° / 68° / 1,45 | 0,0025 / 6 m | 0,50 | Klar, Bewölkt, Regen, Gewitter | Vögel, Windstreifen, Tropfen, Schmetterlinge | silberkueste 0, sturmbucht +22, nebelkueste −24 |
| Wüste | #e0b06a | #b8603a | #3ac0c0 | #fff0c8 | #7a5aa8 | 100° / 72° / 1,55 | 0,0022 / 4 m | 0,50 | Klar, Sandsturm, Bewölkt | Sand, Windstreifen, Vögel, Lichtstaub | duenenmeer 0, grosse_leere +20, klingenschlucht −20, spiegel_der_sonne +36, sonnentor −36 |
| Kristall | #2a2440 | #5a3a8a | #40e0f0 | #b8a0ff | #1a1a48 | 200° / 50° / 0,70 (unterirdisch) | 0,0090 / 4 m | 0,55 | Klar, Nieselnebel | Kristallpuls, Lichtstaub, Tropfen, Sporen | kristalltiefe 0 |
| Himmel | #d8dce4 | #7a8aa0 | #e8c060 | #ffffff | #7a7ac8 | 150° / 62° / 1,60 | 0,0018 / 20 m | 0,30 | Klar, Bewölkt, Schnee | Windstreifen, Vögel, Lichtstaub, Schneeflocken | himmelsspitzen 0 |
| Jade | #3a6a52 | #2a5a3a | #e8c048 | #e8ffe8 | #2a3a5a | 130° / 50° / 1,25 | 0,0042 / 8 m | 0,46 | Jaderegen, Klar, Bewölkt, Regen | Jadestaub, Tropfen, Glühwürmchen, Blätter | jadehafen 0, gruene_flut +22, fluesterhain −22, sonnenstufen +38, herz_der_flut −38 |
| Verderbnis | #3a3040 | #5a4a6a | #a0e040 | #d8c0ff | #2a1a3a | 230° / 38° / 1,00 | 0,0060 / 4 m | 0,38 | Aschefall, Gewitter, Bewölkt | Sporen, Asche, Rauch, Lichtstaub | tyrannenfeste 0, schaedelkueste +24, zarathis −24 |

Die Tabelle nennt nur die prägenden Werte. Alle Felder (Himmel Zenit/Horizont/Boden, Wolken, Post, Wasser flach/tief, Kontur) siehe `ZoneArtProfile`.

## 3. Befund (vorher)

Messwerte je Zone = Mittel über die 6 Lesezeichen (RMS-Kontrast · Sättigung · Kantendichte · Cluster) und Gefahr-ΔE am Kampf-Lesezeichen. Querschnittsbefunde, die fast alle Zonen betreffen:

- **Q1 Flaches Licht.** Nur Toon-Ramp mit einer Lichtrichtung, keine Umgebungsverdeckung, 2 Kaskaden bis 110 m: Facetten im Mittelgrund verschwimmen, Kantendichte median 0,006.
- **Q2 Gefahrenfelder versinken.** Die flache Quad-Markierung wird an Hängen vom Gelände geschnitten (sturmbucht ΔE 1,3) und färbt sich durch den Nebel um; 25 von 30 Zonen liegen unter ΔE 25.
- **Q3 Innenräume fehlen bildlich.** Das Innenraum-Lesezeichen zeigt in 16 Zonen eine Wand, eine Fläche oder ein leeres Treppenhaus mit Kantendichte < 0,005 – kein Licht, keine Requisiten, kein Raumgefühl.
- **Q4 Einheitshimmel.** Himmel ist ein zweistufiger Verlauf ohne Wolken, Sonne nur als Scheibe in 4 Zonen sichtbar.
- **Q5 Wasser ist eine Fläche.** Einheitliches Blaugrau ohne Tiefe, Kaustik oder Schaumkante (silberkueste, duenenmeer, jadehafen).

| Zone | Messwerte | Befund 1 | Befund 2 | Befund 3 |
|---|---|---|---|---|
| sonnfurt | 0,18 · 0,33 · 0,036 · 13 · ΔE 3,7 | Grün übersättigt und ohne Wertstaffelung: Wiese, Bäume und Hecken haben denselben Grünwert | Kampf-Lesezeichen von Ruinen verdeckt, Gefahrenfeld auf hellem Steinboden kaum sichtbar (ΔE 3,7) | Hintergrundgebirge als einfarbig lila Wand ohne Luftperspektive |
| weite_von_ahrn | 0,15 · 0,33 · 0,008 · 10,5 · ΔE 11,1 | Sandgelber Boden und blassgelbes Gefahrenfeld verschmelzen (ΔE 11) | Horizont ohne Struktur – gelbe Ebene trifft hellblauen Himmel ohne Dunst, Wahrzeichen wirkt winzig | Innenraum: Gegenlicht-Sonnenscheibe überstrahlt, Fassade ohne Schattierung |
| gruener_schlund | 0,10 · 0,36 · 0,005 · 10,5 · ΔE 7,3 | Kampfaufnahme halb verdeckt, Gefahrenfeld auf dunklem Gras dunkelt ab (ΔE 7) | Wahrzeichen im Milchnebel nur als Schemen – Nebel zu dicht für Wiese | Innenraum grau-in-grau, Gang ohne Lichtquelle |
| nebelmoor | 0,08 · 0,28 · 0,004 · 9,5 · ΔE 15,7 | Nebel deckt alles über 60 m gleichmäßig, Wahrzeichen nur Silhouette | Gras und Hügel einheitlich olivgrün, keine Moorfarben (Torf, Wasserglanz) | Wasserhürde: Heilfeld grellgrün ohne Kontur, wirkt wie UI-Platzhalter |
| daemmerwald | 0,04 · 0,30 · 0,004 · 6,7 · ΔE 3,8 | Extrem geringer Kontrast (0,04): violetter Nebel macht Landschaft zu einem Fleck | Gefahrenfeld ΔE 3,8 – vom violetten Licht entfärbt, Kampf nicht lesbar | Wasserhürde-Plattform reines Magenta (Platzhaltermaterial) |
| nebelkamm | 0,09 · 0,22 · 0,006 · 11,3 · ΔE 17,0 | Graugrüne Palette ohne Akzent, Sättigung 0,22 | Seen ohne Spiegelung, wirken wie graue Löcher | Felswände der Wasserhürde mit flachem Gelbgrün ohne Schattenfarbe |
| schilfmuendung | 0,09 · 0,31 · 0,008 · 11,8 · ΔE 17,8 | Wasser milchig türkisgrau, Gefahrenfeld darauf blass | Innenraum: senfgelbe Fläche ohne erkennbare Geometrie | Schilf und Ufervegetation fehlen optisch – Wiese geht hart ins Wasser über |
| frostkamm | 0,13 · 0,18 · 0,007 · 10,7 · ΔE 18,1 | 7,4 % geclippt – Schnee frisst Formen, Innenraum fast reinweiß (Luminanz 0,83) | Sättigung 0,18: Himmel und Schnee gleich blassblau, keine Rosa-/Violett-Schatten | Turm-Wahrzeichen im Dunst zu flach, kein Polarlicht/kein Sonnenstand |
| weisse_stille | 0,15 · 0,39 · 0,003 · 11,2 · ΔE 23,9 | Kantendichte 0,003 – Eisebene ohne Detail, Flächen wirken gestrichen | Himmel oben fast nachtblau, bricht hart zur weißen Ebene (keine Horizontstreuung) | Wasserhürde-Eis und Himmel identisch blau, keine Tiefe |
| gluetoede | 0,10 · 0,52 · 0,005 · 8,7 · ΔE 20,4 | Lava ist flaches Orange ohne Glühen, Krustenbewegung oder Hitzeflimmern | Innenraum nahezu schwarz (Luminanz 0,10) | Sonne als riesige Scheibe ohne Bloom-Abstufung blendet Wahrzeichen weg |
| aschenkessel | 0,04 · 0,52 · 0,003 · 7,0 · ΔE 19,2 | Kontrast 0,04 – braun-oranger Einheitsdunst, Gelände ohne Formen | Innenraum ist eine braune Fläche (Kantendichte 0) | Keine Asche- oder Glutpartikel – Biom wirkt warm, aber nicht gefährlich |
| glutkessel | 0,05 · 0,51 · 0,002 · 7,5 · ΔE 16,8 | Kantendichte 0,002, schwächste Formlesbarkeit aller Zonen | Pyramiden-Wahrzeichen und Hang gleiche Farbe, keine Kontur | Innenraum schwarz-braun, Vulkankrater ohne Lavaglut |
| silberkueste | 0,19 · 0,34 · 0,020 · 13,5 · ΔE 13,9 | Gefahrenfeld liegt teils im Wasser und wird abgeschnitten | Wasser einheitlich blau ohne Strand-Schaum, harte Uferkante | Innenraum: kahle graue Dreiecksflächen, Raum nicht erkennbar |
| sturmbucht | 0,13 · 0,34 · 0,008 · 11,0 · ΔE 1,3 | Gefahrenfeld am Hang vom Gelände verschluckt (ΔE 1,3 – schlechtester Wert) | Grasgelb übersteuert, Himmel/Wiese ohne Sturmstimmung (keine Wolken) | Innenraum-Treppenhaus ohne Licht, hellgrau ohne Akzente |
| nebelkueste | 0,09 · 0,18 · 0,005 · 7,7 · ΔE 24,8 | Sättigung 0,18 – grauer Schleier über Meer und Land | Wahrzeichen versinkt völlig im Nebel | Innenraum einfarbig lila Fläche |
| duenenmeer | 0,13 · 0,26 · 0,008 · 9,0 · ΔE 11,7 | Sand fast weißgelb, Dünen ohne Schatten-Seite | Wahrzeichen-Aufnahme zeigt nur einen braunen Hang (Kamera zu nah am Gelände) | Wasser hart abgegrenzt, kein Oasen-Übergang |
| grosse_leere | 0,15 · 0,25 · 0,007 · 11,2 · ΔE 12,6 | Gefahrenfeld auf hellem Sand nur ΔE 12,6 | Innenraum zeigt eine weiße Wüstenebene – Lesezeichen außerhalb | Himmel ohne Hitzeschleier, Horizont messerscharf |
| klingenschlucht | 0,14 · 0,28 · 0,010 · 11,0 · ΔE 9,5 | Schluchtwände rosa-orange ohne Felsstruktur | Gefahrenfeld halbiert (Fächer), auf hellem Sand ΔE 9,5 | Kein Licht-Schatten-Wechsel in der Schlucht – die Schlucht wirkt nicht tief |
| spiegel_der_sonne | 0,12 · 0,25 · 0,009 · 8,7 · ΔE 18,3 | 35,4 % geclippt – Salzebene reinweiß, schwerster Clipping-Fall | Innenraum senfgelbe Fläche | Pagode im Dunst ohne Schatten, wirkt aufgeklebt |
| sonnentor | 0,16 · 0,28 · 0,008 · 10,2 · ΔE 3,7 | Gefahrenfeld auf hellem Sand fast unsichtbar (ΔE 3,7) | Wasserhürde: Sand überstrahlt, Struktur fehlt | Innenraum-Fassade grau ohne Wärme, Tor wirkt nicht nach Sonne |
| kristalltiefe | 0,04 · 0,44 · 0,003 · 3,5 · ΔE 11,8 | Luminanz 0,11 und nur 3,5 Farbcluster – Höhle praktisch schwarz | Kristalle leuchten nicht (nur Punkte), keine Lichtwirkung auf Umgebung | Innenraum hellblau (Fremdmaterial), stilistisch Bruch zur dunklen Höhle |
| himmelsspitzen | 0,15 · 0,31 · 0,006 · 9,8 · ΔE 4,9 | Hellgelb-grüne Felsen überstrahlen, Gefahrenfeld unlesbar (ΔE 4,9) | Innenraum blau-violette Fläche | Himmel ohne Wolkenmeer – die Höhe wird nicht spürbar |
| jadehafen | 0,15 · 0,38 · 0,012 · 12,0 · ΔE 5,1 | Kampf-Lesezeichen über Wasser/Brücke, Gefahrenfeld ΔE 5,1 | Wahrzeichen-Aufnahme zeigt Nebelwand – Pagode nicht sichtbar | Jadegrün der Gebäude ohne Glanz, Wasser ohne Lichtreflexe |
| gruene_flut | 0,12 · 0,41 · 0,008 · 13,0 · ΔE 18,0 | Türkis überall (Wasser, Kristall, Boden) – keine Farbtrennung | Innenraum-Treppenhaus türkis monochrom | Sandbänke hell, harte Kante zum Wasser |
| fluesterhain | 0,09 · 0,28 · 0,003 · 9,0 · ΔE 17,6 | Gefahrenfeld auf grünem Wasser verwaschen | Innenraum olivgraue Fläche | Hain wirkt kahl – keine Baumkronen, keine Lichtflecken |
| sonnenstufen | 0,13 · 0,37 · 0,008 · 11,5 · ΔE 14,2 | Gelbgrüne Wiese übersättigt, Gefahrenfeld darauf blass | Innenraum: reines Rot ohne Schattierung, wirkt wie Platzhalter | Wasser stumpf graublau |
| herz_der_flut | 0,09 · 0,32 · 0,006 · 11,0 · ΔE 18,3 | Hellmint-Wasser und Boden identisch – Grenze nicht lesbar | Innenraum senffarbene Fläche | 2,5 % geclippt, Hügel ohne Schattierung |
| tyrannenfeste | 0,14 · 0,62 · 0,006 · 8,7 · ΔE 23,1 | Sättigung 0,62 – Rot/Orange zu laut, Gefahr verliert ihre Sonderstellung | Wasserhürde-Plattform magenta (Platzhalter) | Festung nur dunkle Silhouette, keine Fenster/Feuer |
| schaedelkueste | 0,05 · 0,26 · 0,003 · 5,7 · ΔE 25,0 | Kontrast 0,05 – violettgrauer Einheitsdunst | Wahrzeichen nur Schemen | Wasser und Land gleich dunkel |
| zarathis | 0,06 · 0,50 · 0,004 · 7,3 · ΔE 20,1 | Dunkelrot-Brei mit Kontrast 0,06 | Gefahrenfeld als dünner Ring flach gegen den Hang gesehen | Innenraum: helle rosa Ebene ohne Raum (Lesezeichen außerhalb) |

## 4. Konsequenzen für G2–G7

- Gefahrenfelder als projizierte Decals mit Kontur und additivem Kernring, nebelfrei (`Fog` auf Gefahr-Layer aus), ΔE-Prüfung bei Tag und Nacht.
- SSAO, 4 Kaskaden bis 150 m und farbige Schatten gegen Q1.
- Höhennebel pro Zone mit Sonnenstreuung, Wahrzeichen-Distanz als Obergrenze der Deckung.
- Schnee- und Salzflächen: Exposure −, Schattenfarbe kräftig, Weiß-Clipping-Ziel ≤ 2 %.
- Glutzonen: Lava-Shader mit Emission/Kruste, Unterlicht, Asche-Partikel.
- Magenta/Rot-Platzhaltermaterialien über `MaterialMigrator` ersetzen.
- Innenraum-Lesezeichen in G7 neu prüfen; wo die Zone keinen echten Innenraum hat, gilt die Aufnahme als „Nah-Architektur“ (siehe KNOWN_ISSUES).

## 5. Nachher (G7)

180 Lesezeichen, 11 Uhr, Hoch, mit Post-Processing und kalibrierter Belichtung. Verbessert = Kontrast, Kanten, Cluster oder Clipping besser bzw. Luminanz näher an der Mitte, ohne mehr als 1 Punkt zusätzliches Clipping: **163/180 (90,6 %)**.

| Kennzahl (Mittel) | vorher | nachher |
|---|---|---|
| RMS-Kontrast | 0,110 | 0,172 |
| Sättigung | 0,344 | 0,395 |
| Geclippt | 0,020 | 0,017 |
| Kantendichte | 0,0075 | 0,016 |
| Farb-Cluster | 9,7 | 12,6 |
| Luminanz | 0,56 | 0,49 |

Nicht verbessert (17): Kristalltiefe (5 Lesezeichen, Höhlenzone), Nebelkamm (3), Innenräume Dämmerwald/Glutöde/Glutkessel, Kampf Dämmerwald/Nebelmoor/Schädelküste/Kristalltiefe, Wasser Flüsterhain/Zarathis, Horizont Tyrannenfeste. Bilder: `Temp/PT_QA/G1/nachher`, `nachher_nacht`, `nachher_regen`; Vergleichsbögen `sheets/`.
