# WORLD – Kontinent von Project Tyrant (Stand Lauf 4)

Vier Zonen, verbunden über eine Portal-Kette. Kontinentkarte: Weltkarte (M) über die minimale Zoomstufe hinaus herauszoomen.

| Zone | Stufe | Größe | Biom | Szene | Portale |
|---|---|---|---|---|---|
| Grenzmark Ashveil (Startzone, Weiler Sonnfurt) | 1–15 | 200 m | Grasland (Lauf-3-Layout) | `Scenes/Zones/PT_Zone_Sonnfurt.unity` | Nordrand → Nebelmoor |
| Nebelmoor | 15–25 | 1.000 m | Moor: Kanäle, Tümpel, Giftnebel | `PT_Zone_Nebelmoor.unity` | Süd → Startzone · Nord → Frostkamm |
| Frostkamm | 25–30 | 1.100 m | Gebirge: Terrassen, Tafelberge, Aufwinde | `PT_Zone_Frostkamm.unity` | Süd → Nebelmoor · Ost → Glutöde |
| Glutöde | 30 | 1.200 m | Aschewüste: Lavarisse, Lava-Ringe | `PT_Zone_Gluetoede.unity` | West → Frostkamm |

Core-Szene `PT_Core.unity` (Systeme, Spieler, Kamera) bleibt geladen; genau eine Zone ist additiv geladen. Zonenwechsel 0,24–0,4 s.

## Zonenabschluss je Biom-Zone
5 Wegmarken · 5 Sehenswürdigkeiten · 3 Aussichtspunkte · 4 Heldenherausforderungen · 3 Herzen (+ 3 Beherrschungs-Einblicke, zählen nicht).

| Zone | Reittier-Sperren (Flutfüllung) | Stallmeister |
|---|---|---|
| Nebelmoor | Irrlicht-Insel: POI, Heldenherausforderung, Einblick → Skimmer | Ole am Herz „Fährfrau Merle“: Skimmer 4 Gold |
| Frostkamm | Adlerhorst (POI, Einblick), Firnsäule (Vista), Gratschrein (HC) → Springer | Brynja am Herz „Bergführerin Runa“: Springer 6 Gold |
| Glutöde | Verlassene Esse (POI, Einblick), Glutsiegel (HC) → Raptor; Schlackenturm (Vista) → Springer | – |
| Startzone | – | Hedda in Sonnfurt, Herz „Bäuerin Wilma“: Raptor 2 Gold |

## Gegner (neu in Lauf 4)
| Zone | Typen (Verhalten) | Champions / Bosse |
|---|---|---|
| Nebelmoor | Moorschleicher (Hinterhalt), Sumpfspucker (Fernkampf), Moorkoloss (Heranziehen) | Uralter Moorkoloss (Jagd) |
| Frostkamm | Eiswolf (Rudelheulen), Frosthüter (Frontschild), Lawinenwerfer (Wurf + Rückstoß) | Rudelführer Weißzahn (Jagd), **Weltboss Frostschlund** |
| Glutöde | Aschenkriecher (Todes-Explosion), Glutbeschwörer (Diener), Lavagolem (Sturmangriff + Brennaura) | Brutmutter (Jagd), **Meta-Boss Sturmtyrann** |

Umgebungstiere: Reiher, Kröte · Schneehase, Bergziege · Aschekäfer, Salamander.

## Ereignisse
- Nebelmoor (5): Stege retten (Bauen), Jagd auf den Uralten Moorkoloss (Jagd), Irrlichter entzünden (Benutzen), Krötenfurt verteidigen (Verteidigen), Kräuter für Albin (Sammeln).
- Frostkamm (6): Lager sichern (Bauen), Rudeljagd (Jagd), Signalfeuer (Benutzen); **Weltboss-Kette Frostschlund** alle 15 min (Versatz 2 min): Eiskristalle (Töten) → Siegelsteine (Benutzen) → Frostschlund (8 min, 2 Phasen, Trotzleiste 1.500).
- Glutöde (7): Blitzableiter (Bauen), Kriecherplage (Jagd), Kühlrinnen (Benutzen); **Meta „Sturm über der Glutöde“** alle 20 min (Versatz 5 min): Sturmwall (Bauen) → Sturmsiegel (Benutzen) → Sturmbrut (Verteidigen) → Sturmtyrann (3 Phasen, Trotzleiste 1.200).
- Startzone: 10 Ereignisse aus Lauf 3 unverändert.

## Beherrschungs-Umwelt
Giftnebel (Nebelmoor, 3) · Sprungpilze (Nebelmoor 2, Frostkamm 2) · Aufwinde (Frostkamm 3, Glutöde 2) · verborgene Truhen (je Zone 1) · Lavarisse (Glutöde).

## Routen
Spawn Sonnfurt → Portal Nordrand (≈ 130 m) → Nebelmoor Torfsteg → Bohlenweg nach Norden (≈ 840 m) → Frostkamm Firnpforte → Pass nach Osten (≈ 650 m) → Glutöde Aschentor. Kontinent-Reise zu entdeckten Wegmarken: 10 + 50 + 6 × Stufe Kupfer (Kommandant −20/−40 %).

## Belebung (Stand 2026-11-11)
Jede Wildnis-Zone: zusätzliche Gegnerlager bis zur Zieldichte (BALANCE.md „Belebung“), zwei Reisende je Wegmarke und drei Helfer je Herz mit Tagesroutine. Städte Sonnentor und Jadehafen: 40 Bürger zwischen Basar, Plätzen und Herzen (Silbermund hatte schon 44).
