# Erweiterungen (Stand Lauf 11)

Drei Regionen auf der Kontinentkarte (Reiter oben rechts, mit Regionen-Abschluss „abgeschlossene Zonen / alle“). Alle 20 neuen Zonen entstehen aus kompakten Spezifikationen (`RegionZones`): Name, Level-Band, Biom, Gefahrenstufen, Fraktionen, Orte, Herzen. Daraus erzeugt ein deterministischer Generator den vollständigen Zonenabschluss.

## Zonen

| Zone | Region | Level | Biom | Größe | Gefahr Eingang → Kern | Kurzbeschreibung |
|---|---|---|---|---|---|---|
| Weite von Ahrn (`weite_von_ahrn`) | Kernland | 30–40 | steppe | 1200 m | 3 → 3 | Offene Steppe, Nomadenzelte und wandernde Herden bis zum Horizont. |
| Nebelkamm (`nebelkamm`) | Kernland | 30–40 | hochmoor | 1100 m | 3 → 3 | Ein Hochplateau mit Mooren, Steinkreisen und Geisterlichtern bei Nacht. |
| Sturmbucht (`sturmbucht`) | Kernland | 40–50 | klippen | 1000 m | 3 → 4 | Klippen, Fischerdörfer und Piratenbuchten im ewigen Wind. |
| Aschenkessel (`aschenkessel`) | Kernland | 50–60 | vulkan | 1100 m | 4 → 4 | Lavafelder, Obsidianklippen und Schwefelseen unter einem rauchenden Gipfel. |
| Grüner Schlund (`gruener_schlund`) | Kernland | 55–65 | dschungel | 1100 m | 4 → 4 | Dichter Dschungel mit einer zweiten Welt in den Baumkronen. |
| Schilfmündung (`schilfmuendung`) | Kernland | 60–70 | delta | 1100 m | 4 → 4 | Ein Flussdelta voller Inseln, Mangroven und Flößer. |
| Weiße Stille (`weisse_stille`) | Kernland | 70–80 | eis | 1200 m | 4 → 5 | Gletscher, Eishöhlen und eine Polarnacht ohne Ende. |
| Schädelküste (`schaedelkueste`) | Kernland | 75–80 | ruinen | 1100 m | 4 → 6 | Eine versunkene Ruinenstadt, in die die Verderbnis sickert. |
| Sonnentor (`sonnentor`) | Glutwüste | 80–80 | oase | 800 m | 0 → 2 | Die Oasenstadt mit Basaren, Karawanserei, Rennbahn und Hafen. |
| Große Leere (`grosse_leere`) | Glutwüste | 80–80 | duenen | 1300 m | 4 → 4 | Endlose Dünen, Sandstürme und versunkene Riesen-Skelette. |
| Klingenschlucht (`klingenschlucht`) | Glutwüste | 80–80 | schlucht | 1100 m | 4 → 5 | Canyons, Hochplateaus und Felsbrücken über tiefen Schluchten. |
| Spiegel der Sonne (`spiegel_der_sonne`) | Glutwüste | 80–80 | salz | 1200 m | 4 → 5 | Salzflächen, Luftspiegelungen und Ruinenstädte am Horizont. |
| Glutkessel (`glutkessel`) | Glutwüste | 80–80 | glutkessel | 1100 m | 5 → 5 | Lavaschluchten und der Tempel des Flammenkönigs. |
| Zarathis (`zarathis`) | Glutwüste | 80–80 | zarathis | 1300 m | 5 → 6 | Die Verlorene Stadt hinter einer Knochenmauer. |
| Jadehafen (`jadehafen`) | Dracheninseln | 80–80 | jadestadt | 800 m | 0 → 2 | Die Vertikalstadt in drei Ebenen mit Aufzügen und Jade-Technik. |
| Grüne Flut (`gruene_flut`) | Dracheninseln | 80–80 | jademeer | 1300 m | 4 → 4 | Ein erstarrtes Jademeer mit Inseln und Schiffswracks. |
| Flüsterhain (`fluesterhain`) | Dracheninseln | 80–80 | bambus | 1100 m | 4 → 5 | Bambus- und Riesenbaumwald mit Klöstern und Geisterwegen. |
| Sonnenstufen (`sonnenstufen`) | Dracheninseln | 80–80 | terrassen | 1200 m | 4 → 5 | Reisterrassen, Dörfer und Festungen der Kaiserin. |
| Nebelküste (`nebelkueste`) | Dracheninseln | 80–80 | nebelkueste | 1100 m | 5 → 5 | Zerklüftete Küste im Nebel, übersät mit Drachenknochen. |
| Herz der Flut (`herz_der_flut`) | Dracheninseln | 80–80 | drachenende | 1200 m | 5 → 6 | Eine Meeresarena aus Jade-Plattformen im Sturm. |

Je Zone: 5 Wegmarken, 5 Sehenswürdigkeiten, 3 Aussichtspunkte, 4 Heldenherausforderungen, 3 Einblicke, 4 Herzen (Karma-Händler mit Ausrüstung auf Zonen-Level), 10 Gegnerlager (Hubs: keine), 4 Tiergruppen, 3 Späher, 1 Sprungrätsel, 6 Ereignisse mit Kette (Angriff → Anführer, Verteidigung, Sammeln, Signalfeuer, Geleit), Ankunfts-Wegmarken an jedem Portal.

## Kernland-Erweiterung (8 Zonen, Level 30–80)
Zwei Zonen je Band als Alternative zu den neu eingestuften Bestandszonen. Portale: Weite von Ahrn ↔ Frostkamm, Nebelkamm ↔ Glutöde, Sturmbucht ↔ Silberküste, Aschenkessel/Grüner Schlund ↔ Dämmerwald, Schilfmündung ↔ Dünenmeer, Weiße Stille ↔ Himmelsspitzen, Schädelküste ↔ Tyrannenfeste; außerdem Paare (Ahrn–Nebelkamm, Sturmbucht–Aschenkessel, Schlund–Schilfmündung, Weiße Stille–Schädelküste).
Neue Fraktionen: Steppennomaden, Steppenbullen, Steinkreisgeister, Buchtpiraten, Magmabrut, Dschungelpirscher, Mangrovenschrecken, Frostriesen (Veteran), Ertrunkene. Weltbosse: Gletscherwächter (Weiße Stille, Stufe 75), Tempelwächter (Schädelküste). Metas: Vulkanausbruch (Aschenkessel, 3 Phasen), Tempel der Schädelküste (3 Phasen).

## Glutwüste (6 Zonen, Level 80)
Hub Sonnentor (Gefahrenstufe 0–2, Kopfgeldtafel, Quartiermeisterin, Stallmeister des Dünenläufers, Rennbahn, Hafen). Zugang: Portal im Vorland der Tyrannenfeste und Schiff von Silbermund.
- **Dünenläufer**: Blinkschritt 15 m (2 Ladungen, 3 mit Beherrschung), Ladungen laden am Boden alle 1,5 s; Sandportale (golden) nur mit Dünenläufer, Radius 2,5 m (Portalmeister 4 m); Sandgleiten = Blink in der Luft. Erwerb: Geschichte Kapitel 14 oder Herz in Sonnentor + 10 Gold.
- **Kopfgeldjagd**: Tafel in jeder Wüstenzone, 4 Ziele (3 Veteranen, 1 legendärer Champion), Spürsinn-Kreis 60 m, 10 Minuten, 2/3 instabile Eigenschaften aus 10 (Lösbarkeit: Druck ≤ 7, höchstens eine Kontrolle, verbotene Paare), tägliche Sperre, 2/5 Kopfgeld-Marken.
- **Reittier-Rennen**: „Lauf durch die Leere“ (Große Leere) und „Ring von Zarathis“, 6 Checkpoints, Boost-Pads (+50 %, 3 s), Bestzeit je Reittier, Geisterfahrer-Aufzeichnung, Goldzeit 110 s, Abkürzung per Sandportal nur mit Dünenläufer.
- Fraktionen: Sandgeister (Lauerer, Blindheit), Flammenkult (Fernkampf, Brennen), Flammenpriester (Veteran, Beschwörer, Trotzleiste), Knochenkonstrukte (Veteran, Schild). Weltboss Salzkoloss. Metas Belagerung des Flammentempels (3 Phasen), Fall von Zarathis (4 Phasen, Fahrplan 0:30 im 2-h-Zyklus).
- Umwelt: Sandsturm-Zyklus in Großer Leere, Salzsee, Zarathis; Gefahrenstufen 4–6 (Zarathis-Kern: 6 „Drachenland“).

## Dracheninseln (6 Zonen, Level 80)
Hub Jadehafen (Gefahrenstufe 0–2, drei Aufzüge – zwei frei, einer mit Jadebatterien –, Quartiermeister, Werkstatt, Anglerverband, Bootswerft). Zugang: Schiffe von Silbermund und Sonnentor.
- **Jadegeist**: ab Kapitel 20; Linie mit 4 Rängen (Rettungsprotokoll alle 10 min mit 50 % HP, Gleitverstärkung +50 %, Jadebatterien für Jade-Geräte, Jadeband 20 m Beute-Magnet).
- **Angeln**: 3 Angelstellen je Angel-Zone (8 Zonen), Wurf 5–25 m, Biss nach 2–8 s, Spannungsspiel (grüner Bereich 20–80 %), Seltenheit nach Rute/Köder/Boot, je Zone 9 Fische (Sammlung, Erfolg).
- **Boot**: Reittier „Boot“ (Radialmenü/Direkttaste), nur auf Wasser ≥ 1,2 m, Trägheit, Anker (Leertaste), 2 Mitfahrer; Beherrschung Rudermeister (+20 %) und Jade-Schub (V).
- Fraktionen: Jade-Konstrukte (Veteran, Trotzleiste), Geisterwesen (Lauerer, Furcht), Kaiserliche Abtrünnige (Fernkampf), Jade-Piraten. Weltboss Nebelschlange. Meta Erwachen des Meeresdrachen (4 Phasen, Fahrplan 1:30).

## Belohnungsökonomie
Kopfgeld-Marken (Kopfgeld, Rennen) bei Quartiermeisterin Halima in Sonnentor, Drachen-Marken bei Quartiermeister Wen in Jadehafen: Exotische Ausrüstung 80, Skins, Jade-Kerne, Gabe der Wüste / Gabe der Jade (Alternativen in der Legendär-Pipeline, je 250 Marken). Ausrüstung je Band 40/50/60/70/80 (Meisterwerk, Selten, Exotisch) plus Aufgestiegen 80 (Rüstung, Schmuck).

## Erfolge
Erfolgs-Kategorien Erkundung (Regionen-Abschlüsse mit Truhe und Titel), Story, Kopfgeld, Rennen, Angeln, Weltbosse, Sammlungen (Fischsammlungen je Zone); Punkte, Titel, Beherrschungspunkte.

## Geschichte
- **Zwischenspiel „Asche des Siegels“** (Kapitel 11–12): Verderbnis an der Schädelküste, Tovin findet den Orden der Glut, Mira schickt die Expedition nach Sonnentor.
- **„Thron der Glut“** (13–19): Nadira, Kopfgeld, Dünenläufer, Ilyas und die Luftspiegelungen, erste Konfrontation mit Zarath, Wahl Sturm/List an der Knochenmauer (eigene Instanz, Ilyas’ Schicksal in Kapitel 18), Thronkampf in Zarathis.
- **„Jadeherz“** (20–26): Lin Sao und der Jadegeist, Kaiserin Yuen, Boot und Angeln, Schwester Mei und der Vertrag, Wahl Kaiserin/Daro, Kael und die Nebelschlange, Entscheidung in der Kernkammer (Drache frei/Stadt gesichert) mit Epilog-Varianten.

## Spielzeit-Schätzung
Kernland 30–80: je Band 2–3 h (Simulation: eine Zone allein trägt ihr Band). Glutwüste und Dracheninseln: je Zone 60–90 min Zonenabschluss, Geschichte je Erweiterung ≈ 2 h, Zwischenspiel ≈ 30 min.
