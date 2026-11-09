# GW2-Abgleich (Lauf 3 + Lauf 4)

Feature → GW2-Verhalten → Umsetzung im Prototyp → Abweichung und Grund.

## A/B – Attribute, Stufe, Segen, Zustände
| Feature | GW2 | Prototyp | Abweichung / Grund |
|---|---|---|---|
| Primärattribute | 37 → 1000 über Stufe 1–80 | 37 → 1000 über Stufe 1–30 (Lauf 4) | Level-Cap 30 statt 80; Kurven umskaliert |
| Schadensformel | WS × Kraft × Koeffizient / Rüstung | identisch, WS 55 → 1000 | – |
| Krit / Wildheit | 5 % + Präz.-Anteil; 150 % + Wildheit/15 | identisch (stufenskaliert) | – |
| Lebenspunkte | Klassenbasis + Vitalität × 10 | 1.300 → 9.212 + Vit × 10 | eine Klasse |
| Zustandsdauer/Segendauer | Sachkenntnis/Konzentration ÷ 15 % | identisch | – |
| Segen | 12, Stapelregeln, Dauer-Stapel | 12 inkl. Schnelligkeit/Tatendrang | – |
| Zustände | 14 + Kontrolle | 14; Furcht/Spott/Verlangsamung nur Anzeige | KNOWN_ISSUES 10 |
| EP-Kurve | steigend, Ereignisse geben viel | 400 + 120 × (S−1); Zone ≈ Stufe 15 | kompakte Zone |
| Sprung/Fallschaden | ~1,2 m, Schaden ab Höhe | 1,2 m, ab 8 m, tödlich ab 20 m | – |

## C – Skillsystem
| Feature | GW2 | Prototyp | Abweichung / Grund |
|---|---|---|---|
| Leiste | 5 Waffe, Heil, 3 Hilfe, Elite | identisch, Waffen aus angelegter Waffe | – |
| Freischalten | Heldenpunkte, Tiers | Heil 2, Hilfe 2/4/6, Elite 8 | – |
| Heldenpunkte | Stufe + Herausforderungen | 4 je Stufe + 10 je Herausforderung | Budget ~60 % auf Stufe 15 (DECISIONS M10) |
| Spezialisierungen | 3 Slots aus mehreren Linien, je Rang 1 aus 3 | 3 Linien, alle aktiv, Ränge einzeln freischalten | nur 3 Linien vorhanden |
| Eigenschaften | Attribute, Auslöser mit ICD, Abklingzeit | 11 Wirkungstypen datengetrieben | – |
| Sperre im Kampf | ja | ja (6 s nach Schaden, Wirken, am Boden) | – |
| Tooltips | berechnete Fakten | Schaden/Heilung/CD live | – |

## D – Items, Inventar, Händler
| Feature | GW2 | Prototyp | Abweichung / Grund |
|---|---|---|---|
| Seltenheiten | 8 inkl. Plunder | 8 (Legendär/Aufgestiegen ungenutzt) | Zone Stufe 1–15 |
| Präfixe | viele, Haupt/Neben | 7 (Berserker … Himmlisch) | – |
| Inventar | Rucksack 20 + 4 Taschen | identisch, Taschenregeln | – |
| Bindung | accountgebunden/seelengebunden bei Anlegen | identisch | – |
| Beute | Beutesack, Bereichs-Beute | F sammelt alle Säcke in 5 m | – |
| Bergen | Bergungskit mit Ladungen | identisch, Plunder nicht bergbar | – |
| Händler | Kaufen/Verkaufen/Rückkauf, Karma | identisch, Karma-Händler an Herzen | – |
| Handelsposten/Bank | ja | nein | außerhalb des Umfangs |

## E – Zonenabschluss
| Feature | GW2 | Prototyp | Abweichung / Grund |
|---|---|---|---|
| Wegmarken | Annähern, Reisekosten, umkämpft | 10 m, Kosten nach Distanz × Stufe, umkämpft im Event-Radius | – |
| Tod | Wegmarke wählen | Weltkarte „Besiegt“ → freie Wegmarke | kostenlos |
| POIs | Annähern | 5 Gebiete | – |
| Aussichtspunkte | Klettern + Kamerafahrt | Treppen ≤ 0,45 m, Orbit 5,5 s | kein Kletter-Puzzle |
| Heldenherausforderungen | Kommunion, Kampf, Gegenstand | 3 Typen, 10 Punkte (HoT-Modell) | – |
| Herzen | mehrere Beitragswege, Karma-Händler | Kills + 3 Aufgabenarten, Händler danach | – |
| Zonenabschluss | Belohnung bei 100 % | Truhe (Exotisch, 2 Selten, Material) | – |

## F – Karte
| Feature | GW2 | Prototyp | Abweichung / Grund |
|---|---|---|---|
| Minimap | unten rechts, Rotation, Zoom | identisch, 5 Stufen | – |
| Weltkarte | gemalte Karte, Pan/Zoom, Tooltips | gebackene 2048²-Karte | – |
| Nebel | Aufdeckung beim Erkunden | 128², 60 m, gespeichert | – |
| Tracker | rechts an der Minimap | oberhalb der Minimap | Spezifikation sagt „unter“; kein Platz (DECISIONS M11) |
| Persönliche Wegmarke | Rechtsklick | nicht umgesetzt | optional |

## G – Dynamische Events
| Feature | GW2 | Prototyp | Abweichung / Grund |
|---|---|---|---|
| Parallel/Ketten | viele, verzweigte Ketten | Scheduler ≥ 3, 2 Ketten | – |
| Zieltypen | Töten, Verteidigen, Eskorte, Sammeln, Einnehmen, Boss | alle 6 | – |
| Auslöser | Zeit, Nähe, Kette, Weltzustand, NPC | alle 6 inkl. Zufallsfenster/Herz | – |
| Meta | Zustände beeinflussen Zone | „Dornwacht gefallen“ → Sonnfurt-Angriff, Wegmarke, Händler | – |
| Belohnung | Gold/Silber/Bronze | identisch, Fehlschlag 25 %, Gold-Item | – |
| Skalierung | nach Spielerzahl | Hook EventScale = 1 | Einzelspieler |


## Lauf 4 – Offene Welt, Reittiere, Beherrschungen
| Feature | GW2 | Prototyp | Abweichung / Grund |
|---|---|---|---|
| Karten | einzeln geladene Karten, Ladebildschirm | Core-Szene + 1 additive Zonen-Szene, Ladebildschirm mit Tipp, 0,24–0,4 s | – |
| Portale | Kartenränder | Portal-Bögen an Zonenrändern, bidirektional validiert | – |
| Kontinentkarte | Herauszoomen aus der Zonenkarte | identisch (Zonen-Kacheln, Abschluss %, Timer, Wegmarken-Reise) | – |
| Herabstufung | Gebietsmax. + 1, Ausrüstung skaliert | identisch, EP-Faktor 0,5 ab 8 Stufen | Stufe 30 statt 80 |
| Reise in andere Karten | Aufschlag | 10 + 50 + 6 × Stufe Kupfer | – |
| Reittiere | Raptor/Springer/Skimmer (PoF), Reittier-HP, Absitz-Angriff | 3 Reittiere, 1 s Aufsitzen, Reittier-HP, Absitz-Angriff auf 1, X tippen/halten (Radialmenü) | Kein Schwimmen; Raptor-Sprung nicht aufladbar |
| Reittier-Erwerb | Beherrschungen + Story | Herz der Zone + Gold beim Stallmeister | Story fehlt (Lauf 6) |
| Gelände-Sperren | Reittier-Fähigkeiten schalten Wege frei | Flutfüllung je Marker → „Benötigt: Springer“ im Tooltip | Gleiten nie Pflicht |
| Beherrschungen | EP ab Stufe 80, Punkte aus Einblicken/Erfolgen/Story | EP ab Stufe 30, Punkte aus Einblicken, Zonenabschluss, Weltboss/Meta, Erfolgen | 4 kompakte Linien |
| Gleiten | ab Beherrschung, Aufwinde | identisch | – |
| Umwelt-Beherrschungen | Pilze, Gift, Lava je Karte | Giftnebel, Sprungpilze, Lavarisse, verborgene Truhen | – |
| Trotzleiste | Kontrolle bricht Leiste, Gegner wehrlos | identisch, +50 % Schaden 5 s | – |
| Weltboss | feste Zeitpläne, Vor-Events | 15-min-Zyklus über Sitzungs-Weltuhr, 2 Vor-Ereignisse, 8 min Limit | Uhr nicht server-weit |
| Meta-Ereignis | mehrere Phasen, kartenweit | 4 Phasen, 20-min-Zyklus | auf einen Kartenbereich beschränkt |
| Herzen | ≥ 3 je Karte | 3 je Biom-Zone | Lauf 6 erweitert |
