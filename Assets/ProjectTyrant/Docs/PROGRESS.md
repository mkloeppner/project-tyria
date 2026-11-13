# PROGRESS – Project Tyrant

| Milestone | Status | Abschluss (Europe/Berlin) | Verifikation |
|---|---|---|---|
| M0 Setup | ✅ | 2026-11-09 02:12 | Packages, URP-Asset, Ordner, 3 asmdefs, Sandbox-Szene; kein Git-Repo vorhanden |
| M1 Player/Kamera/Dodge | ✅ | 02:22 | 0 Compile-Errors · Play Mode 20 s ohne Exceptions · Dodge 6,5 m + i-Frames live geprüft · DodgeTests 3/3 |
| M2 Zone/Visuals/Meshy-Queue | ✅ | 02:35 | Shader ohne Fehler · Zone gebaut (60k-Vertex-Terrain, 4 POIs, ~170 Bäume) · ZoneLayoutTests · Meshy Tier 1 gestartet |
| M3 Skills/Combos/HUD | ✅ | 02:42 | Combo-/Buff-/SkillCaster-Tests grün · live: Feuer+Explosion → 3 Macht, Weapon Swap, HUD ohne Fehler |
| M4 Gegner/KI/Downed | ✅ | 02:42 | DownedTests 6/6, NpcBrain/Telegraph-Test · live: Downed → Kill → Rally (4.500 HP) |
| M5 Event-Kette | ✅ | 02:48 | EventChainTests · live: Trigger-Zone → Defend → Fail → Recapture (Banner/Wachen/Besatzer) → Champion → Gold-Belohnung → Cooldown |
| M6 Assets/Polish | ✅ (Meshy teilweise) | 02:50 | Meshy-Spieler normalisiert & verbaut · Hit-Flash, Partikel, 5 prozedurale Sounds, OOC-Regen |
| M7 Build-Check/Doku | ✅ | 02:52 | Windows64-Build erfolgreich (0 Errors, 1 Warning, 131 MB, nach `Temp/PT_BuildCheck`) · Doku vollständig |

**Tests gesamt:** 24/24 EditMode grün (Lauf 2: +GroundTargetAimTests).

## Lauf 2 – Nächste Schritte (2026-11-09 07:46–08:1x)
| Schritt | Status | Verifikation |
|---|---|---|
| NavMesh-Pfadfindung | ✅ | 2.299 NavMesh-Dreiecke, Pfad Hub→Lager komplett; live: Wellen laufen durchs Tor zu den Kisten |
| Ground-Target-Vorschau | ✅ | Test + live: Zielkreis erscheint, Bestätigen wirkt Meteorzorn |
| Champion-Balancing | ✅ | live: untätiger Spieler ~15 s |
| Prozedurale Gliederanimation | ✅ | Gegner/Wachen-Prefabs |
| Meshy Tier 1+2 (6 Modelle) | ✅ | Bounds geprüft (aufrecht, 1,8 m / 16 m / 1,5 m / 1,6 m) |
| Editor-Neustart (Input-Backend) | ✅ | 08:03, Keyboard + Mouse im New Input System |
| Build-Check | ✅ | Windows64: 0 Errors, 1 Warning, 231 MB (Meshy-Texturen) |

## Offene Punkte
- Visueller QA-Durchgang in der Szene (Kamera-Abstände, Farben, Meshy-Ausrichtung).

## Lauf 3 – GW2-Vollausbau (Start 2026-11-09 14:31)
| Milestone | Status | Abschluss | Verifikation (Tests · Live · Profiler · Regression) |
|---|---|---|---|
| M8 Fundament | ✅ | 15:58 | 42/42 EditMode (neu: AttributeSet 3, Schadensformel/Krit/Blind/Fall 5, EffectModel 5, Progression 2, SaveGame 3) · live: Level 10 per Debug (HP 3.668, Kraft 147 → 207 mit 10 Macht, Krit 25 % mit Wut), Macht-Slot mit Stapel „10“ + Ring, Sprung auf 1-m-Stufe (Y 1,08 → 2,08), Speichern/Laden stellt Position (−60; 2,08; −6) und Level 12 wieder her, Fenster/Esc/Dialog · Profiler: CPU 8,2 ms (Main 1,6 ms), GPU 1,1 ms · Regression: Dodge/Combo/Downed/Telegraph/Ground-Target (Tests), NavMesh unverändert |
| M9 Items & Inventar | ✅ | 15:32 ¹ | 57/57 EditMode (neu 15: Inventar Stapeln/Voll/Teilen+Verschieben/Tasche mit Inhalt/Aufräumen, Wallet-Umrechnung, Equip-Aggregation, Level-Sperre+Slot, Seelenbindung+Waffenstärke, Loot-Determinismus, Gewichte 75/25 ±3 % + Stufenfilter, Garantiert, Händler Kauf/Verkauf/Rückkauf, Plunder verkaufen, Bergen) · 76 Items, 6 Beutetabellen, 2 Händler · live: Trash Lvl 5 getötet → Beutesack → F: Lederstück + 16 k ins Inventar; Wegwacht-Helm bei Stufe 3 abgelehnt („Benötigt Stufe 4“), bei Stufe 5 angelegt (Kraft/Präzision/Rüstung steigen, alter Helm ins Inventar); Schmiedeklinge: Waffenstärke 49,5 → 75; Krämer: Kauf 5 Tränke (24 k), Verkauf Rekrutenklinge (+28 k), Rückkauf (−28 k); Speichern/Laden stellt Wallet, angelegte Tasche (28 Plätze), Stapel, Kit-Ladungen und alle 8 Ausrüstungsslots identisch wieder her · 2 Captures (Temp/PT_QA/M9_*) → Layout-Fixes Heldenfenster, Händlerliste · Profiler: CPU 8,3 ms (Main 1,5 ms), GPU 1,1 ms · Regression: alle 42 Alt-Tests grün, Play-Start ohne Warnung |
| M10 Skillsystem | ✅ | 15:44 | 66/66 EditMode (neu 9: Freischalten/Budget/Rückerstattung, Leiste Typprüfung + Tausch, Speichern Leiste/Eigenschaften, Ränge der Reihe nach + Auto-Wahl, aktive Eigenschaften → Attribute ohne Doppelung, HP-Budget 60 % auf Stufe 15, Attribut flach/Umwandlung/Krit, Abklingzeit/Dauer je Kategorie, bedingter Schaden + Auslöser mit interner Abklingzeit + Kill-Heilung) · 14 Fertigkeiten, 3 Spezialisierungen, 36 Eigenschaften · live: Stufe 15 → 60 HP; Klingenfokus freigeschaltet (−2) und in Hilfe 1 gelegt (HUD zeigt „Fokus“); Glutzorn Rang 1 (+20,8 Kraft) + Funkenschlag: Krit setzt Trash nach 4 Treffern in Brand; Wachtbund „Lautstarke Wacht“: Kriegsschrei 25 → 20 s im Tooltip; nach Treffer gesperrt; Leiste/Eigenschaften/ausgegebene Punkte überleben Play-Neustart · 2 Captures (Temp/PT_QA/M10_*) · Profiler: CPU 5,9 ms (Main 1,4 ms), GPU 1,0 ms · Regression: alle 57 Alt-Tests grün (1 angepasst, s. DECISIONS) |
| M11 Karte & Minimap | ✅ | 15:57 | 74/74 EditMode (neu 8: Welt↔UV + bekannte Orte, Minimap Nord/Kamera-Rotation + Umkehrung, Rand-Klemmung + 5 Stufen, Weltkarten-Zoom um Zeiger + Ansichtsgrenze, Nebel 60 m + Persistenz über Speicherstand, Nebel-Textur nur bei Änderung, Zonenabschluss-Zählung, Reisekosten) · Karte 2048² gebacken (Rebuild) · live: Minimap-Pivot = Spieler-UV (0,14/0,22), Zoom 0 ↔ 4 erreichbar (Mausrad + Tasten), Rotation umschaltbar; 12 Marker (Wegmarke, 5 POI, 2 Händler, 4 Wachen grün); Weltkarte zeigt Sonnfurt unten links, Dornwacht Mitte, Elmhain unten rechts (Capture M11_worldmap); Event-Tracker an der Minimap (Capture M11_minimap); Schnellreise Dornwacht → Sonnfurt: −78 k, 2,5 m neben der Wegmarke nach Ladeblende; Abnahme-Check: 4 Fenster auf/zu, Minimap-Zoom, Speichern/Laden ✓ · Profiler mit offener Weltkarte: CPU 8,5 ms (Main 1,3 ms), GPU 1,25 ms · Regression: alle 66 Alt-Tests grün |
| M12 Zonenabschluss | ✅ | 16:15 | 82/82 EditMode (neu 8: Treppenstufen ≤ 1 m aus Builder-Daten, Sprung-Herausforderung nur springend erreichbar, Standorte trocken + sanfter Zugang, Mindestinhalt + eindeutige Ids, Umkämpft-Regel, Herz-Fortschritt einmalig, Zähler/100 %/Truhe einmalig, Speicherstand) · 20 Zielpunkte (5 WP, 5 POI, 3 Vista, 4 HC, 3 Herzen) + 3 Karma-Händler · live: Treppe zu Fuß erklommen (Y 1,3 → 5,78), Kamerafahrt 5,5 s mit HUD aus → Vista erledigt (+250 EP); Kommunion durch Schaden unterbrochen, danach 5 s → +10 HP; Kampf-Herausforderer (Veteran L10) besiegt → +10 HP; Relikt: 3 Gegner, Blutung ×2 + Gift, besiegt → +10 HP; Herz Gerstenbrunn: 7 Aufgaben + 4 Kills + 2 Aufgaben → 100 %, +250 Karma, Karma-Händler aktiv; Dornwacht-Wegmarke während der Kette umkämpft; Tod → Weltkarte „Besiegt“, Dornwacht abgelehnt, Sonnfurt → 5.046/5.046 HP, 2,5 m neben dem Monument; 100 % → Truhe genau einmal (Exotisch + 2 Selten + Material + Tasche) · Profiler: CPU 8,8 ms (Main 1,5 ms), GPU 0,7 ms · Regression: alle 74 Alt-Tests grün, Fenster/Minimap/Speichern ✓ |
| M13 Dynamische Events | ✅ | 16:27 | 90/90 EditMode (neu 8: ≥ 3 parallel + Limit, Auslöser Annäherung/Welt-Zustand/Herz/Zufallsfenster, Ketten-Zweige + Glieder starten nie allein, Cooldown + Flag-Rückbau, Medaillen + Fehlschlag 25 %, Eskorte scheitert bei Tod, Einnehmen nur ohne Gegner + Verteidigen, Migration 10 Events/6 Ziele/6 Auslöser/2 Ketten; alte EventChain-Tests unverändert grün) · live: Karawane Sonnfurt → Dornwacht eskortiert (2 Überfälle, Wagen 81 %, Gold) → Karawanenhändlerin aktiv; Verteidigung durch zerstörte Kisten gescheitert → „Dornwacht gefallen“ → Rückeroberung (Zweig B) + „Sonnfurt unter Angriff“ parallel, Wegmarke umkämpft, Kerla weg, Wachen tot; Kreis erst nach 9 Besatzern gefüllt → Erfolg → Flag gelöscht, Wegmarke frei, Champion-Stufe startet; Sammeln 6/6 abgeliefert → Erfolg; 3 Events parallel (Champion, Elmhain, Alpha) · Capture M13_events_map (Kreise + Icons, Elmhain-Wegmarke umkämpft) · Profiler mit Weltkarte + Events: 120 fps, Main 6,4 ms · Regression: alle 82 Alt-Tests grün |
| M14 Polish, Meshy, Build, Doku | ✅ | 16:43 | 90/90 EditMode (0,2 s) · Meshy 3/6 (kumuliert 12/18): Monument, Stele, Schrein eingebaut, Ausrichtung per Capture geprüft · 6 neue AudioSynth-Sounds · Win64-Build-Check: Erfolg, 0 Errors, 1 Warning, 295 MB (`Temp/PT_BuildCheck`) · 5-min-Play-Mode (325 s): alle Fenster, Minimap-Zoom/Rotation, Kampf + Beutesack, Schnellreise, 3 parallele Events, Speichern/Laden – 0 Errors, 0 Warnings, 120 fps · Docs: README (Steuerung/Fenster/Speicherstand/Debug/2-Minuten-Tour), GW2_ABGLEICH, PLACEHOLDERS, ASSET_QUEUE, DECISIONS, KNOWN_ISSUES |

¹ Uhrzeit ab M9 aus der Systemuhr (MEZ). Die M8-Angabe „15:58“ war geschätzt und ist zu spät; tatsächlicher M8-Abschluss lag vor 15:00.

## Lauf 4 – Offene Welt, Reittiere, Beherrschungen (Start 2026-11-09 17:43)
Lauf-3-Speicherstand gesichert: `Temp/PT_QA/save_lauf3.json` (8.257 Bytes). Blockierende Lauf-3-Lücken laut KNOWN_ISSUES: keine (Speicherstand, Weltkarte, Ereignis-Engine, Sprung, Interaktion vorhanden).

| Milestone | Status | Abschluss | Verifikation (Tests · Live · FPS je Zone · Ladezeit · Rebuild-Dauer) |
|---|---|---|---|
| M15 Welt-Fundament | ✅ | 18:10 | 100/100 EditMode (neu 10: Cap 30 = 1000 Primär, Herabstufung Max+1 nur nach unten, EP-Faktor/Item-Skalierung, Portale bidirektional + Validierung, Save-Migration V1 → Startzone (Nebel, Position, Truhe), V2-Rundreise mit Nebel je Zone, Chunk-Zerlegung lückenlos/ohne Überlappung, Reise-Aufschlag, Biom-Gelände deterministisch) · Rebuild Everything 37,7 s (Startzone 19,2 s, je Biom-Zone ≈ 4 s, Core 0,4 s) · Szenen: PT_Core + 4 Zonen, alte PT_Zone.unity entfernt · live: Lauf-3-Stand (Stufe 18) lädt in Sonnfurt (eff. 16) an gespeicherter Position → Portal Nordrand → Nebelmoor 0,31 s → zurück 0,24 s: Inventar 14 Stapel + 8 Ausrüstung identisch, Stufe/Kupfer/21 Abschlüsse gleich; Frostkamm per Debug, Speichern, Play-Neustart → Start direkt in Frostkamm an (30/7/−400); Sonnfurt-Regression: 28 NPCs, 20 Zonenobjekte, 10 Ereignisse, Scheduler aktiv · 120 fps in allen 4 Zonen (Editor) · Wechsel 0,24–0,37 s · 2 Captures (m15_frostkamm, m15_gluetoede) |
| M15a | Hinweis | | Speicherbedarf je Zone wird im Build (M20) gemessen; im Editor misst der Profiler den gesamten Editor (1,5 GB). |
| M16 Drei Zonen | ✅ | 18:41 | 107/107 EditMode (neu 7: Mindestinhalt je Zone, eindeutige Ids über alle 4 Zonen + innerhalb vor dem Randgebirge, Reittier-Sperre je Zone, Flutfüllung Tafelberg→Springer / Wasser→Skimmer / Lava→Raptor / Hang zu Fuß, Gelände-Merkmale + Wege) · je Zone 5 WP, 5 POI, 3 Vista, 4 HC, 3 Herzen, 3 Einblicke, 2 Tierarten (21/17/20 Tiere), 3 neue Gegnertypen in 12 Lagern (26/30/27 NPCs) · Erreichbarkeit: 69 Marker + 5 Portale, 0 Abweichungen (Sperren: 3 Skimmer, 4 Springer, 3 Raptor) · Rebuild Everything 85,8 s · live: Moorschleicher verborgen → bei 6 m Ausbruch + Festhalten + Gift (3 Gegner L16); Frosthüter L29 frontal 200 / Rücken 1.000 Schaden; Glutbeschwörer ruft 4 Kriecher, Kriecher-Tod → Explosion; Gebiet „Torfstecher-Siedlung“ (15–16) · 3 Vista-Aufnahmen (m16_vista_*) · 119–120 fps in allen Zonen (Editor) · 0 Errors |
| M17 Reittiere | ✅ | 19:05 | 114/114 EditMode (neu 7: Aufsitzen 1 s/Boden/Besitz, Schaden bricht Aufsitzen ab + Reittier-HP + Abwurf-Sperre, Raptor-Sprung ≥ Raptor-Lücke + Springer 3–15 m, Skimmer Wasser + Absitz-Angriff + Ausweichen, X antippen/halten, Erwerb Herz+Gold → Freischaltung, Speicherstand) · Rebuild 89,0 s · live: Raptor beim Stallmeister gekauft (Herz erfüllt, −2 Gold), 1 s Aufsitzen, Tempo 11,5, Reittier-HP 6.000, Sprung 12,4 m (Hauswand), Fertigkeiten gesperrt; Skimmer schwebt bei Spiegel 0,9 über 3,1 m tiefem Wasser (12,5 m/s); ohne Skimmer zurück ans Ufer; Lava: −1.463 HP + 8 Brennen, zurück auf sicheren Boden (Fehler „sichere Position aus alter Zone“ gefunden und behoben) · Capture m17_raptor · 120 fps · 0 Errors |
| M18 Beherrschungen | ✅ | 19:10 | 119/119 EditMode (neu 5: EP in gewählte Linie + Punkte-Bedarf, Punkte aus Rängen + Abschluss leert Auswahl, alle Wirkungen je Rang, Speicherstand, Reiserabatt) · 4 Linien / 14 Ränge / 24 Punkte · Umwelt: 3 Giftnebel, 4 Sprungpilze, 5 Aufwinde, 3 verborgene Truhen · live: alle Linien gemeistert → Reisekosten −40 %, Gleiter öffnet in der Luft (Fehler „Gleiten endet sofort nach Teleport“ behoben), Glutöde 2 Aufwinde + 1 Truhe vorhanden; Fehler „Kosten nach dem Laden leer“ behoben · 0 Errors |
| M19 Events, Weltboss, Meta | ✅ | 19:15 | 126/126 EditMode (neu 7: Bauen/Benutzen, Jagd 2 Phasen, Weltboss-Phasen + 8-min-Limit, Trotzleiste Block/Bruch/Auffüllen, Weltuhr-Zyklen, Zyklus-Auslöser startet Kette, je Zone ≥ 5 Ereignisse + Meta 4 Phasen) · 18 neue Ereignisse (5/6/7) · Rebuild 84,6 s · live: Frostschlund L30, 261.818 HP, Trotzleiste 1.500 → 4 Betäubungen 300 → Phase 2 (45 %) mit 5 Dienern und Leiste 1.875 → gebrochen: betäubt, 1.000 → 1.500 Schaden; Tod → Erfolg + 1 Beherrschungspunkt einmalig; „Lager sichern“ 5/5 Zelte → Erfolg; Rudeljagd 8 Kills → Weißzahn L27 mit Trotzleiste; Meta Phase 1 → Phase 2 startet automatisch; Kontinent: „Frostschlund in 1:02“, „Sturm über der Glutöde in 4:02“/„aktiv“ · 120 fps · 0 Errors |
| M20 Polish, Meshy, Build, Doku | ✅ | 19:41 | 126/126 EditMode (0,3 s) · Meshy 6/8 (kumuliert 18/26), alle eingebaut, Ausrichtung per Capture m20_meshy_lineup geprüft · Rebuild Everything 73,1 s · Win64-Build `Temp/PT_BuildCheck`: Erfolg, 0 Errors, 2 Warnings, 552 MB, 19,5 s · Play-Mode 813 s (13,5 min) über alle 4 Zonen, beritten mit voller Geschwindigkeit, Weltkarte offen, Ereignisse/Weltboss/Meta laufend: 0 Errors/Warnings; FPS je Zone (Durchschnitt): Startzone 121, Nebelmoor 121 (min. 63), Frostkamm 121, Glutöde 121 – Einzel-Ausreißer in Startzone/Frostkamm/Glutöde fallen genau auf MCP-Befehle (CodeDom-Kompilierung im Hauptthread), nicht auf Spielcode; Zonenwechsel 0,18–0,38 s; Editor-Speicher 1,76–1,99 GB gesamt · Docs: README, WORLD (neu), GW2_ABGLEICH, DECISIONS, PLACEHOLDERS, ASSET_QUEUE, KNOWN_ISSUES 16–21 · Speicherstand auf echten Lauf-3-Stand zurückgesetzt |

## Lauf 5 – Klassen, Animation, VFX, Sound & Musik (Start 2026-11-09 19:45)
Blockierende Lücken aus Lauf 3/4: keine (Skill-Datenmodell mit Koeffizienten, Speicherstand v2, Reittiere vorhanden). Abweichungen vom erwarteten Stand: Weltboss-Timer je Sitzung (KNOWN_ISSUES 20), Wetter ohne Partikel (19).

| Milestone | Status | Abschluss | Verifikation (Tests · Live · FPS · Partikel · Stimmen · Rebuild) |
|---|---|---|---|
| M21 Animations-Fundament | ✅ | 20:42 | 138/138 EditMode (neu 12: Reiter sitzt auf Sattelhöhe, Skelette 22/18 Knochen + Hierarchie, Gewichte Summe 1 + Glättung normiert, MeshKit-Humanoid/Raptor rissfrei über alle Clips, 192 Clips Länge/Frames/Schleifennaht/kein NaN, Katalog vollständig (8 Waffenstile, 6 Körperbauten, 3 Reittiere), Marker Treffer/Effekt/Ende, Fußkontakt am Fuß-Minimum + Boden erreicht, Telegraph ≥ 0,6 s, Fortbewegungsmischung Summe 1, Fertigkeit→Clip, Determinismus) · Rebuild Everything 92 s · Meshy-Skinning 8/8 mit Rissen (3 Ansätze) → MeshKit-Rigs + Heldenhain-Statuen · live: Gehen/Kampf (Großschwert-Wirbel, Gegner holt sichtbar aus), Waffe auf dem Rücken außerhalb des Kampfes, Raptor-Ritt in Sitzpose, 6 Dorfbewohner mit Routinen, 0 Errors · Animationsbudget: 31 Figuren ≤ 30 m / 70 gesamt → Ø 1,16 ms je Frame (vorher 1,90 ms ohne LOD), 121 fps · 6 Captures + 10 Kontaktbögen (8 Frames) |
| M22 Skill-Timeline und Kampfgefühl | ✅ | 21:20 | 146/146 EditMode (neu 8: Zeitachse aller 32 Skills datengetrieben (Treffer in Aktivierung, Clip × Faktor = Aktivierung + Nachwirkung, 3 Spuren, Treffer-Marker = Wirkzeit), Auto-Ketten (3 Glieder, steigend, gleiches Mittel, Zusatzeffekt, eigener Clip/Sound), Ketten-Reset 2 s, Warteschlange/Sofort/Shift/Ausweichen, Unterbrechung + 5-s-Sperre (Modell + echter SkillCaster), Hit-Stop 40–80 ms + Impuls-Klassen, Ableitung, Treffer-Richtung) · Rebuild 93 s · live: Kette Glut I→II→III im 0,5-s-Takt (Clips Auto1/2/3, SFX Swing1/2/3, unterschiedliche Tonhöhe), Bergbrecher mit Aktivierungsbalken, Betäubung bricht ab → Splitter + 5 s gesperrt (4,86 s nach 0,14 s), Ziel-Umriss + Rangkreis, Gegner-Aktivierungsbalken, 23 Hit-Stops in 8 s Kettenkampf, 0 Errors · 120 fps, Animation Ø 0,49 ms · Captures: m22_chain_a, m22_heavy_windup, Serie m22_series_heavy_1/2 + m22_series_interrupt |
| M23 VFX-Baukasten und Welt-VFX | ✅ | 21:02 | 155/155 EditMode (neu 9: Komposition → Prefab (572), Partikelbudget + 3-s-Regel, Paletten vollständig, jeder Skill hat auflösbare VFX, Lesbarkeit/Dichte, Zustands-Tint-Priorität, Schlüssel-Rundlauf, additive Shader ohne Alpha², Aura sichtbar) · Rebuild 117 s (VFX 13 s) · live: 10 Gegner (Werfer, Spucker, Beschwörer, Hüter, Golem) in Aggro um den Spieler, 8 gleichzeitig im Angriff → 106 fps (Mittel über 12 s), 1 222 Partikel live (Spitze Pool 1 029), 21 Instanzen, 7/8 Lichter; Zonen-Ambient Sonnfurt 370 / Nebelmoor 180 / Frostkamm 900 / Glutöde 660 Partikel bei 117–121 fps; Am-Boden-Vignette; 0 Errors · Stimmen –(Audio M24) · Captures: m23_palette_<11 Paletten>, m23_life_<4 Zonen>, m23_live_10casters, m23_downed_vignette, 8-Bild-Serien je VFX-Familie m23_series_<13 Formen> (Palette Arkan) · Laternen-Funken in ZoneBuilder ergänzt (wirkt ab nächstem Rebuild) |
| M24 Audio | ✅ | 22:48 | 170/170 EditMode (neu 15: Spitze/RMS/DC/Dauer je Kategorie für alle 562 WAV, Spektral-Schwerpunkt (UI 1–4 kHz, Bass-Stems < 300 Hz), Loop-Nähte < 0,01, Stems synchron 60–90 s in ganzen Takten, Komponist Tonart-Treue + Tempo, Hauptthema je Zone, Mixer-Hierarchie/Snapshots, Ducking/Lautstärken, Stimmen-Prioritäten + Budget 32, Round-Robin, Musik-Zustände 3 s/8 s, Vollständigkeit (Skills, UI, Ereignisse, Banner, Untergründe, Effekte, Seltenheit, Fraktionen, Zonen, Stinger), Optionen + Menü-Zustandsmaschine, Klangkarte, DSP deterministisch) · Rebuild 148 s (Audio 15 s, erstmals 100 s inkl. Import) · live (Glutöde): Erkundung Musik −16 dBFS → 10 Gegner → Kampf nach 3 s (Musik −14, Stimme −9…−12) → Sieg-Stinger (Musik −12 inkl. Stinger, Ducking) → 8 s nach Kampfende Erkundung; Esc → Menü-Snapshot (Musik −23, Umgebung Tiefpass 900 Hz); Optionen Musik 80 → 40 % = −12 dB sofort; Stimmen Spitze 27/32, 0 verworfen; 0 Errors · 106 fps (M23-Messung, Audio-Last vernachlässigbar) · Captures: m24_combat_audio, m24_gamemenu, m24_options |
| M25 Drei Klassen und Charakterauswahl | ✅ | 00:43 | 182/182 EditMode (neu 12: Einstimmungen, Initiative, Heimlichkeit/Enthüllt, Gefährte Tausch/Tod/Wiederbelebung, Stehlen Reichweite/Fraktion, Vollständigkeit je Klasse wie Wächter (3/9/2, 3 Linien × 3+9), jede Fertigkeit jeder Klasse mit Zeitachse/Animation/VFX/SFX/Tooltip, Vorlagen-Raster Elementarweber, Initiative statt Abklingzeit, Account-Migration/Erstellen/Löschen/Trennung, Waffen-Klassenbindung, ungültige Einstimmung) · Rebuild 107 s (Klassen 1,4 s, Charakterauswahl 0,7 s) · live: Migration Lauf-4-Stand → Wächter „Aeryn“ Stufe 19; erstellt Wildhüter/Elementarweber/Schattenklinge → Spielen → Sonnfurt Stufe 1 mit Langbogen + Wolf; Esc → Charakterauswahl → Elementarweber: Einstimmung Feuer → Wasser; Klassen-Duell je 180 s gegen Veteran Brecher (Stufe 20), Wächter 396 DPS/4 Kills, Wildhüter 435/4, Elementarweber 223/1, Schattenklinge 125/0 – 0 Exceptions, Balancing in M27 · 121 fps (Sonnfurt) · Partikel/Stimmen unverändert ggü. M24 · Captures: m25_charselect, m25_lyra_start, m25_switch_ivo, m25_duel_waechter, m25_wildhueter, m25_elementarweber, m25_schattenklinge |
| M26 Elite-Spezialisierungen | ✅ | 01:25 | 192/192 EditMode (neu 10: Vollständigkeit aller 16 Spezialisierungen (Waffe 5/20 Skills, Tarnungsangriff + Initiative, Skilltyp 1/3/1 mit Stufe 1–3, Linie 3+9 mit Wirkung und Text, Palette eindeutig, Sound-Motiv, Aura, Signatur, Rekrutenwaffe, Waffe erst ab Schritt 2), jede der 273 Elite-Fertigkeiten mit Zeitachse/Clip/VFX/SFX/Tooltip, nur eine aktiv + dritter Platz + Stufe 30 + 3 × 10 HP + Erstattung, Speicher-Rundlauf/fehlende Linie/alter Stand, Hainform, Bosheit, Überladen, Doppel-Einstimmung (6 Kombis), Schattenschleier, Energie + Axt-Rückruf; VFX-Zählungen fortgeschrieben) · Rebuild 173 s (VFX 22 s, Klassen 14 s, Audio 43 s) · live: je Spezialisierung 60 s Spielprobe gegen Veteran Brecher (Stufe 30, exotische Waffen, ×3), F5 alle 7 s, Signatur-Skill mit Capture; 0 Exceptions in 20 Läufen; DPS Kern: Wächter 1077, Wildhüter 976, Elementarweber 493, Schattenklinge 805; Elite: Hainwächter 1008, Bestienbund 1116, Dornenwächter 1357, Sturmfalke 1142, Sturmrufer 327, Weber 671, Katalysator 677, Glutbinder 649, Wegelagerer 669, Phantomtänzer 607, Schattenhüter 434, Klingenwirbel 776, Glutrichter 1162, Bollwerkmeister 804, Sturmherold 1224, Dornenschild 794 (Balancing → M27) · 120 fps (Frostkamm, Duell) · Partikel/Stimmen im Rahmen von M23/M24 · Captures: m26_<16 Spezialisierungen> |
| M27 Polish, Meshy, Build, Doku | ✅ | 04:05 | 224/224 EditMode (neu 3: Ladebild je Zone mit Inhalt, Balancing-Tabellen vollständig + Wächter = Referenz, Skalierung nur Schadenswerte) · Balancing: 20 Klassen/Spezialisierungen im Duell-Bot-Messmodus, Bestwert je Eintrag ±15 % zum Wächter (775 DPS): Spanne 667 (Elementarweber, Dornenschild) bis 878 (Bestienbund) · Meshy 6 geliefert (+6 durch Play-Mode-Reload verloren), 3 Waffen eingebaut, 3 Helden → Statuen · Ladebilder 4 Zonen · 10 min Play Mode (Frostkamm per Portal-Wechsel, Raptor 46 m, Weltboss Frostschlund gestartet und gekämpft, 4 Klassen + 6 Spezialisierungen im Duell, zurück nach Sonnfurt): 0 Exceptions · Rebuild Everything 126,1 s · Win64 `Temp/PT_BuildCheck`: Erfolg, 17,3 s, 730 MB, 0 Fehler, 2 Warnungen · FPS im unfokussierten Editor gedrosselt (nicht aussagekräftig), Partikel-/Stimmenbudgets unverändert (20 000 / 32) |

## Fix-Lauf F1 – Wegmarken, Zoom, Drehen, Bodenziel, Flächen, Ketten (Start 2026-11-10 01:03, zwischen M26 und M27)
Vorgezogen, weil Fix 1 einen Soft-Lock behebt. EditMode vorher 192/192, nachher 221/221 (29 neu).

| Fix | Befund | Änderung | Verifikation | Abschluss |
|---|---|---|---|---|
| 1 Wegmarken-Sicherheit | Besiegt in Nebelmoor ohne freigeschaltete Wegmarke → Karte ohne Wiederbelebungs-Option (Soft-Lock); 6 Eingänge ohne Wegmarke in Reichweite | `ZoneDefinition.Arrivals`, `WaypointSafety` (Eingänge, Abdeckung ≤ 40 m, Auto-Freischaltung, Besiegt-Optionen nie leer + Notfall-Wegmarke, Migration), `ArrivalWaypointBuilder` (6 neue Ankunfts-Wegmarken), Debug „Spieler besiegen“ / „Wegmarken vergessen“ | 5 Tests · Live: neuer Charakter → Sonnfurt, Portal → Ankunft Nebelmoor auto-frei, Wegmarken vergessen → „Notfall-Wegmarke: Torfsteg“ (`f1_besiegt_*.png`) | vor 02:00 |
| 2 Mausrad-Zoom | Scroll war gebunden, das Input System liefert aber ±1 je Raste; × 0,0015 ergab praktisch keinen Zoom | `CameraZoomModel` (Rasten, 1,5–12 m / beritten 2,5–16 m, 0,75 m je Raste, SmoothDamp 0,15 s), Rig setzt `Orbital.Radius`, Abstand im Account gespeichert, 3 Optionen | 3 Tests · Live: 1,50 / 6 / 12,00 m gemessen, Raptor 16 m und Zoom-in auf 2,5 m (`f1_zoom_*.png`) | 02:05 |
| 3 A/D drehen | A/D strafte (M1-Entscheidung), GW2-Standard ist Drehen | `MovementModel` (Drehen 180 °/s; RMB/Action-Cam/Reittier → seitwärts), Kamera folgt (`FollowYaw`), Lernhinweis, Option „A/D-Tasten“; DECISIONS M1 ersetzt | 3 Tests · Live: 831° in 4,62 s = 180 °/s ohne Positionsänderung (360° in 2 s); mit RMB 14,6 m seitwärts, 0° Drehung durch A/D (`f1_turn_*.png`) | 02:08 |
| 4 Bodenziel-Modi | nur Normal + Shift-Sofort | `GroundTargetModel` (4 Modi, Halte-Schwelle 150 ms, Einrasten aus, Reichweiten-Begrenzung an), `SkillCaster` Vorschau/Loslassen/Abbruch, Reichweitenring | 6 Tests · Live mit Klingenregen: Modus 1 halten → Vorschau sichtbar, Loslassen → gewirkt; Modus 2 sofort gewirkt (`f1_bodenziel_*.png`) | 02:10 |
| 5 Flächen-Optionen | Flächen eigener/verbündeter Skills immer voll | `VfxAreaLayer` (Rand/Effekt je Komposition, 216 Prefabs), `AreaDisplay` je Besitzer, Combo-Felder dünner Rand, Gegner-Telegraphen mit Warn-Tooltip | 3 Tests · Live: „Fläche und Effekt“ Rand 3/3 + Effekt 3/3, „Nur Effekt“ Rand 0 + Effekt 1/1 (`f1_flaeche_und_effekt_*.png`, `f1_nur_effekt_*.png`) | 02:12 |
| 6 Auto-Ketten | Ketten nur bei 10 Waffen (Glutklinge, Sturmruf, Schwert, Dolche, Schattenpistole, 5 Nahkampf-Elites); Projektilwaffen ohne Kette | `NormalizeChains` für alle Waffen außer Langbogen/Elementarweber: Glied 1 = 2, Glied 3 × 1,4 + Waffenbonus, Mittelwert unverändert; Abbruch bei Pause 2 s/Wechsel/Ausweichen/Unterbrechung/Zielverlust; Abzeichen, Tooltip, „Kettenabschluss“ | 5 Tests + M22-Test angepasst · Live: Großschwert 0,706 / 0,706 / 0,988 (Mittel 0,800), nach 2,5 s Pause wieder Glied 1; Dolche 0,559 / 0,559 / 0,783 (`f1_kette_*.png`) | 02:14 |
| Optionen | Optionen ohne Erklärung, kein Zurücksetzen | `OptionCatalog` (Tooltip je Zeile), „Standard“ je Reiter, Tasten-Reiter, Account-Speicherung | 3 Tests | 02:00 || QA-Fund Weltboden | Dauertest: Duell-Bot stellte in Sonnfurt neben der Karte auf, Spieler fiel bis y = −37.131 | `WaypointSafety.WorldFloorY` −150 + `PlayerProfile.RescueFromVoid`, Duell-Bot nur Plätze mit Boden (`ZoneAnchors.TryGroundAt`) | 1 Test · Live: Teleport auf (900, −10, 900) → nach < 18 s an der Wegmarke Sonnfurt (`f1_weltboden_rueckholung.png`) | 02:35 || Abschluss F1 | – | – | 221/221 EditMode · Dauertest 676 s Play Mode (8 Klassen/Spezialisierungen im Duell-Bot, Sonnfurt): 0 Exceptions, 0 Warnungen · Rebuild Everything 119,7 s · Win64-Build `Temp/PT_BuildCheck`: Erfolg, 35,5 s, 655 MB, 0 Fehler, 4 Warnungen | 02:41 |

Hinweis: Die Duell-DPS des Dauertests (Wächter 571, Sturmfalke 630, Elementarweber 293) sind mit M26 nicht vergleichbar (Zeitfaktor 1 statt 3, Sonnfurt statt Biom-Zone, Niederlagen mit Wiederaufstehen); M27 misst für das Balancing neu.

## Lauf 6 – Heldengeschichte, Späher, Herzen, sechs neue Zonen (Start 2026-11-10 04:20)
Blockierende Lücken aus Lauf 3–5 laut Auftrag: Dialog-fähige NPCs (bisher nur Benachrichtigungs-Zeilen), Zonen aus Daten (vorhanden: `BiomeZoneBuilder`), Audio-Mixer (vorhanden: `MixerModel`), Animations-Events (vorhanden: Marker im `AnimClipSet`) → in M28 nur das Dialog-System neu. Windows-Sprachsynthese verfügbar: „Microsoft Hedda Desktop“ (de-DE, weiblich), David/Zira (en-US) → alle Figuren auf Hedda, Männerstimmen über Tonhöhen-/Formant-Nachbearbeitung.

| Milestone | Status | Abschluss | Verifikation (Tests · Live · FPS · Ladezeiten · Kapitel-Dauer) |
|---|---|---|---|
| M28 Story- und Dialog-Fundament | erledigt | 2026-11-10 05:50 | 244/244 EditMode (+23: 9 Modell-, 11 Daten-/Stimm-Tests, 3 Bestand). Story-DB aus CSV: 4 Kapitel, 132 Zeilen, 22 Szenen (6 mit Wahl), 27 Hinweise, 4 Briefe, 7 Bündel, 17 Figuren. Sprache: 125 Zeilen SAPI (Hedda) + Nachbearbeitung, 44 Held-B-Varianten, Rebuild Story+Voice 19–35 s. Tonhöhen gemessen: Männer 124–151 Hz, Frauen 188–209 Hz (vorher 153–174 Hz, Granular-Verschiebung wirkungslos → WSOLA). Live: Gesprächsszene, Cinematic (Letterbox, Verdeckungs-Schwenk), Sprechblase, Wahl + Überspringen mit nachgereichter Wahl, Instanz betreten/scheitern/Neustart/verlassen, Lern-Gegner für „Ausweichen“, 0 Fehler/Warnungen. Rebuild Everything inkl. Story läuft durch. |
| M29 Bestehende Zonen und Kapitel 1–4 | in Arbeit | | 248/248 EditMode (+4 Welt-Tests). `world.csv` + `WorldStoryBuilder` (läuft in jedem Zonen-Build): 15 Späher (3/4/4/4), je Zone ein viertes Herz mit neuen Wegen (Tiere füttern, Fallen entschärfen, Bündel-Ziele Eimer/Sandsack/Harpune, Emote), 12 bestehende Herz-NPCs sprechen (Sprechblase + Stimme), 16 Dankesbriefe, 28 neue Sprecher; 178 Zeilen (171 vertont). Sonnfurt-Zonenabschluss-Liste war leer (Kontinentkarte 0 %) → jetzt 21 Ziele. Instanz-Requisiten (Brand, Rauch, Barrikade, Zelt, Käfig, Kisten) für alle 7 Instanzen aus K1–4. Live: Späher → Blase + Karte, Eimer → Glutnest gelöscht (+20 %), Ziege füttern (+12 %), /jubeln (+15 %), Rebuild Everything 140–152 s. **05:00 Editor eingefroren** beim Verlassen einer Instanz (Reaktivieren ausgeblendeter Verbündeter mit Rig/Navmesh-Agent; davor TransformStreamHandle-Fehler) – Ausblenden auf Gegner zurückgenommen, Editor-Neustart nötig (kein Neustart durch mich möglich). |
| M30 Neue Zonen I | in Arbeit (ungeprüft) | | Während des Editor-Hängers (ab 05:00) außerhalb des Editors geschrieben, noch nicht kompiliert: 6 Biome, 6 Zonen-Definitionen mit Grenzportalen und Portalplatz (9 Portalpaare), Inhalte je Zone (5/4 Wegmarken, 5 POI, 3 Aussichtspunkte, 4 Heldenherausforderungen, 3 Einblicke, 3+1 Herzen, 3–4 Späher), 25 Gegner-Archetypen, 29 Ereignisse (Alter Hain, Himmelswyrm, Sandsturm-Champion, Meta „Belagerung der Feste“ in 5 Phasen), Silbermund (≈ 36 Häuser, 44 Bewohner, Bündnis-Händler, Grabmal, Figurenplatz), Verderbnis, Sandsturm, Dunkelheit, schwebende Inseln, Festungstore, 3 Sprungrätsel, Greif als Reittier mit Beherrschungslinie (2 Ränge), Zonen-Sperre der Tyrannenfeste, Musik und Klangbetten für 6 Zonen; Kapitel 5–10 (232 Zeilen) und 113 Welt-Zeilen, 30 Briefe. Tests geschrieben: Lauf6WorldTests (8), StoryPathTests (27 Pfade + 2). |
| M31 Kapitel 5–8 | offen | | |
| M32 Neue Zonen II | offen | | |
| M33 Kapitel 9–10 und Finale | offen | | |
| M34 Polish, Meshy, Sprache, Build, Doku | offen | | |



## Balance B1 – Sanfter Einstieg, Gefahrenstufen, Verfolgung und Flucht (Start 2026-11-10 07:52)
Ausgangslage: Lauf 6 M30–M34 nur teilweise geprüft (Rebuild Everything vor B1 durchgelaufen, 321/329 EditMode; 6 offene Lauf-6-Tests werden in B1 mit grün gezogen). Kein TestPilot aus einem Abnahme-Lauf → minimaler Autopilot `BalanceBot` mit beiden Profilen. Kein Admin-Menü (Lauf 7 nicht gelaufen) → Befehle im Debug-Panel (F9). Startzone ist 200 m groß → Entfernungsregeln des Auftrags (300/700/100 m für 1.000-m-Zonen) skaliert (DECISIONS).

| Milestone | Status | Abschluss | Verifikation (Tests · Telemetrie · Captures · Rebuild) |
|---|---|---|---|
| B1 Telemetrie und Baseline | erledigt | 2026-11-10 16:30 | Telemetrie/Heatmap/Bots/Zielwert-Auswertung; Baseline 36 Sessions (2 Profile × 6 Stufen × 3 Seeds × 30 min, alte Szenen, Messschalter „vorher“), Trainingspuppe je Stufe, Heatmaps `Temp/PT_QA/balance/heat_*_vorher.png`; Hypothesen 1–8 mit Befund |
| B2 Gefahrenstufen und Verfolgung | Abnahme läuft | | Rebuild Everything 299 s, Validator 0 Verstöße, 342/342 EditMode (+55 B1-Tests). Live: Flucht Stufe 0 15/15 (Aufgabe nach 1–3 s), 30 gelbe/24 rote Gegner in der Startzone, 0 Rig-Fehler; Nachher-Bots laufen |
| B3 Startzone und Event-Stufen | Abnahme läuft | | 54 Spawns Startzone (Stufe 0/1/2: 6/26/22), 30 neutral, 4 Patrouillen; Event-Stufen für alle 57 Events; Dornwacht Stufe 1 ohne Auto-Start; Trainingspuppe Stufe 0 46 s / Stufe 1 38 s / Stufe 2 28 s |
| B4 Mittel- und Spätspiel | Abnahme läuft | | Kurve geeicht (1,85); Trainingspuppe Stufe 3 17 s / 4 14,5 s / 5 12,3 s (Korridor), Veteran 22/15/9,6 s; 89 Rollen (Jäger/Rufer/Lauerer); Flucht Stufe 4–5 zunächst 20/20 → Fessel auf Treffer + Ausholen im Laufen |
| B5 Messung, Feinabstimmung, Doku | **abgebrochen** (Auftraggeber, 18:39) | 2026-11-10 18:40 | Nachher-Bot-Messung nach zwei Bot-/Modellfehlern neu gestartet und auf Wunsch abgebrochen (Vorrang Lauf 10/11). Offen: Nachher-Bots, Zielwert-Tabelle, Heatmaps nachher, Feinabstimmung Stufe 4–5, Regressionen Weltbosse/Story-Bosse, Win64-Build, BALANCE.md-Zahlen |

**12:10 zweiter Hänger** in derselben Messreihe (Neustart 09:46 im Safe Mode wegen CS0619 `GetInstanceID` in NpcBrain – behoben; danach CS0136 im BalanceBuilder – behoben; 333/342 EditMode, Rest braucht Rebuild). Vermutete Ursache beider Hänger: SetActive-Umschalten geriggter Gegner in `BalanceProbe` (KNOWN_ISSUES 61) – entfernt. **08:30 Editor reagiert nicht mehr** (MCP-Bridge getrennt, keine Neukompilierung seit 08:20) während der Baseline-Messreihe im Play Mode (Trainingspuppen-Messung in der Kristalltiefe, Zeitfaktor 4). Kein Neustart durch mich; Arbeit an Builder/Validator/UI läuft außerhalb des Editors weiter, Messung wird nach Rückkehr fortgesetzt.

### Balance B1 – Hypothesen (Befund vor jeder Änderung)
| # | Hypothese | Befund | Messung/Code | Folge |
|---|---|---|---|---|
| 1 | Auto-Start am Start: Dornwacht-Kette nach 60 s, Wellen im Startgebiet | **bestätigt** | `EventDataBuilder`: `AutoStartDelay = 60`, Timer-Auslöser; Bot-Probelauf (Stufe 0, Seed 1): nach 26 s „Dornwacht verteidigen“ gescheitert → „Dornwacht gefallen“ → „Sonnfurt unter Angriff“ im Hub, Bot 2× besiegt in 2 min | Timer entfernt, Stufe 1, kein Hub-Angriff bei Fehlschlag (B3) |
| 2 | Keine neutralen Gegner | **bestätigt** | Alle Kampf-NPCs `Team.Enemy` mit Aggro-Radius 10–18 m; Umgebungstiere ohne Kampf; Startzone nur Plünderer/Brecher | Neutral-Flag je Spawn, Wildeber/Rothirsch, Quote je Stufe (B2) |
| 3 | Verfolgung ohne/zu lange Leine, zu schnell, Separation drückt in den Spieler, kein Sicht-Vergessen | **teilweise** | Leine 40–45 m vom Spawn, keine Zeitgrenze, kein Sichtlinien-Vergessen (nur Tarnung) – bestätigt; Tempo: Plünderer 4,5 m/s < Spieler 7 m/s – verworfen, Wölfe 6,2 m/s; Separation schob von allen Kämpfern weg, auch vom Spieler – kein Mindestabstand | PursuitModel mit 6 Regeln, Tempo nach Stufe, Separation nur unter Gegnern + 0,8 m (B2) |
| 4 | Rudel-Aggro überall ab Stufe 0 | **verworfen (Startzone)** | Kein Rudel-Mechanismus außer Heulen (Eiswölfe, Frostkamm); Gruppen ziehen nur über den gemeinsamen Aggro-Radius (14 m) | Rudel nach Stufe (0 keine · 1 Partner · 2+ Gruppe 300) (B2) |
| 5 | Schaden zu hoch auf Level 1–5 (Zeit-bis-Tod 3 Trash < 15 s) | **bestätigt** | Trainingspuppe: Level 2 gegen 3 Plünderer **10,2 s** (Ziel ≥ 30), Level 4 **6,4 s** (Ziel ≥ 25), Veteran L4 12,8 s (≥ 25); Downed→Tod im Bot-Lauf in 0,2 s | Gegner-Kurve nach Stufe (B2/B4) |
| 6 | Respawn zu kurz, Dichte zu hoch um Sonnfurt; Events spawnen in Hörweite des Hubs | **teilweise** | Respawn: verworfen – Welt-Gegner kehrten nie zurück (Respawn fehlte ganz); Events: bestätigt – Karawane startet am Hub (−54/−50), „Sonnfurt unter Angriff“ im Hub, Sammeln in Gerstenbrunn (Herz) | Respawn nach Stufe, Event-Spawns ≥ 20 m (skaliert 100 m) von Hub/Wegmarken (B2/B3) |
| 7 | Kampfende zu spät, Aufsitzen wird unterbrochen, Wegmarke im Kampf gesperrt | **teilweise** | Kampfende nach 6 s statt 5 s – bestätigt; Aufsitzen im Kampf war **erlaubt** und Schaden unterbrach es nicht (GW2-Regel verletzt, aber kein Fluchthindernis); Wegmarken-Reise im Kampf erlaubt – verworfen | Kampfende 5 s, Aufsitzen nur außerhalb des Kampfes, Schaden unterbricht (B2) |
| 8 | Wachen helfen nur in Events, niemand belebt wieder | **bestätigt** | Wachen nur als Dornwacht-Event-Aktoren (`ZoneRoot.Guards`); keine NPC-Wiederbelebung | Patrouillen auf den Wegen, Aggro-Übernahme, NPC-Wiederbelebung in Stufe 0–1 (B3) |

### Balance B1 – Baseline (vorher, Trainingspuppe, Code-Stand vor jeder Änderung)
| Stufe | Zone, Level | Zeit-bis-Tod 3 Trash | Zeit-bis-Tod Veteran | Zeit-bis-Kill Trash | Zeit-bis-Kill Veteran |
|---|---|---|---|---|---|
| 0 | Sonnfurt, L2 | 10,2 s | – | 3,7 s | – |
| 1 | Grenzmark, L4 | 6,4 s | 12,8 s | 4,9 s | 21,6 s |
| 2 | Dornwacht, L7 | 21,3 s | 10,4 s | 6,0 s | 29,2 s |
| 3 | Nebelmoor, L20 | 4,3 s | 6,5 s | 11,5 s | 44,6 s |
| 4 | Glutöde, L30 | 19,3 s | 30,3 s | – (Messung ungültig: Ziel außer Reichweite, 180 s) | – (ungültig) |
| 5 | Kristalltiefe, L30 | 27,2 s | 29,6 s | 21,4 s | 62,3 s |

Befund Baseline-Kurve: Stufe 0–3 deutlich zu tödlich (Zeit-bis-Tod 4–10 s statt 16–30 s), Stufe 5 dagegen **zu leicht** (27 s gegen 3 Trash, Korridor ≥ 9 s, praktisch 9–13 s) und zu zäh (Trash 21 s statt 7–9 s). Die Kurve muss den Einstieg entschärfen und das Spätspiel gefährlicher, aber schneller machen.


## Lauf 10 – Greif komplett, Himmelsschuppe, Reittier-Auswahl, Admin-Menü (Start 2026-11-10 18:40)
Befund vor Beginn: Greif aus Lauf 6 als Gleit-/Sturzmodell (`GriffonFlight`, 2 Beherrschungsränge, Sink über `MaxFallSpeed`), Radialmenü und `SelectedMount` vorhanden, Admin-Funktionen nur als lose „Welt:“/„Story:“-Befehle im Debug-Panel (F9), Story-„Schritt abschließen“ ohne Reise/Auto-Wahl/Kennzeichnung, keine Flugregeln, kein Icon für den Greif.

| Meilenstein | Status | Ende | Nachweis |
|---|---|---|---|
| M54 Admin + Auswahl | fertig | 18:36 | `AdminCommands` (Reittiere/Beherrschungen/alles mit Kennzeichnung, /flight, Story weiter/Kapitel/Sprung/Reset/Instanz-Ziel, Szenen-/Auto-Wahl-/Reise-Schalter), `StoryDirector.Admin` über `FinishStep`; `MountSelectionModel` + `MountPrefsDto` auf Account-Ebene, X-Logik Standard/Zuletzt, Radial-Regel, Direkttasten, HUD-Symbol, Reittier-Tab 3 × 2 mit Einstellungen |
| M55 FlightMotor + Regeln | fertig | 18:36 | `FlightMotor` kinematisch, `PlayerController.FlightVelocity`, `FlightRules` (Decken 150/400/80, 5 Verbote), Aufwinde für beide Flieger |
| M56 Greif komplett | fertig | 18:36 | Auftrieb 3/5, Gleiten 1,6× 6:1, Sturz 4×/4,6×, Abfangen 60/85 %, Strömungsabriss < 0,6×, Sturzangriff nach Tempo, 4 Ränge |
| M57 Himmelsschuppe | fertig | 18:36 | `MountKind.Skyscale`, Daten/Modell/Icon, Ausdauer 100/150, Wandhaften, Feuerstoß, Linie mit 4 Rängen, Sammlung 3 Schritte (`SkyscaleCollection`, `Lauf10Builder`) |
| M58 Validator + Doku | fertig | 18:41 | `FlightReach` (Zwei-Wege-Regel, Builder-Bericht), MOUNTS.md, ADMIN.md; Rebuild Everything 353 s (Himmelsspitzen: Ei auf 99 m, Greif über Aufwind bis 129 m erreichbar, Himmelsschuppe mit 2 Rasten), 407/407 EditMode (+64 Lauf-10-Tests), 0 eigene Warnungen. Live (Play Mode, Sonnfurt): Admin-Freischaltung 5 Reittiere, Greif gleitet 11,2 m/s bei 1,87 m/s Sinken (6 : 1), Luftwechsel Himmelsschuppe → Greif bei 39 m ohne Höhenverlust |


## Lauf 11 – Große Erweiterung: Level 80, 20 Zonen, Glutwüste, Dracheninseln (Start 2026-11-10 18:42)
Befund vor Beginn (Abweichung vom erwarteten Ist-Stand, Details KNOWN_ISSUES 67): Läufe 7–9 fehlen im Projekt – keine sechs Hauptstädte (nur Silbermund), keine Völker, kein Weltboss-Fahrplan (Zonen-Timer je Zone), kein Abnahme-/Autopilot-System außer den B1-Bots, Ausrüstung bis Exotisch (Aufgestiegen/Legendär nur als Enum). Level-Kurven waren linear bis 30 (AttributeSet, Waffenstärke, Gegner-HP, XP).

| Meilenstein | Status | Ende | Nachweis |
|---|---|---|---|
| M59 Level-80-Fundament | fertig (Telemetrie offen, KI 68) | 2026-11-10 20:15 | `LevelCurves` (Attribute 37→1.000 mit Stützstellen, Fraktion für HP/Waffenstärke/Gegner, XP 1–30 unverändert und ab 30 konstant 3.880, Heldenpunkte 4/Stufe bis 30 + 1 bis 80, Beute-Seltenheit nach Level), Cap 80, Re-Leveling (`LevelBands`, Layouts und Ereignisse ins neue Band), Story-Gates 1/5/12/20/30/40/50/60/70/80, Elite ab 80 (Bestandsschutz), Speicherstand v5 mit Sicherungskopie, Gefahrenstufe 6 „Drachenland“ (inkl. Zug aus der Luft), Fahrplan 12 Bosse + 2 Metas, Ausrüstung 40–80 (+ Aufgestiegen 80), Admin `/level`, Fahrplan; 452/453 EditMode (Rest: Datenkorrektur, behoben) |
| M60/M61 Kernland I+II | Welt fertig (Telemetrie/Captures offen) | 2026-11-10 20:15 | 8 Zonen aus `RegionZones` (je 5 Wegmarken, 5 POIs, 3 Aussichtspunkte, 4 Herausforderungen, 3 Einblicke, 4 Herzen, 10 Lager, 4 Tiergruppen, 3 Späher, Sprungrätsel, 6 Ereignisse mit Kette), 9 neue Gegnertypen, Weltbosse Gletscherwächter/Tempelwächter, Metas Vulkanausbruch/Tempel |
| M62 Glutwüste Welt | Welt fertig (Telemetrie/Captures offen) | 2026-11-10 20:15 | 6 Zonen, Fraktionen Sandgeister/Flammenkult/Knochenkonstrukte, Salzkoloss, Metas Flammentempel und Fall von Zarathis, Portal Tyrannenfeste + Schiff Silbermund |
| M64 Dracheninseln Welt | Welt fertig (Telemetrie/Captures offen) | 2026-11-10 20:15 | 6 Zonen, Jade-Konstrukte/Geisterwesen/Abtrünnige/Jade-Piraten, Nebelschlange, Meta Erwachen des Meeresdrachen, Schiffe Silbermund/Sonnentor |
| M63/M65 Story Glutwüste + Dracheninseln | Modell-Durchlauf grün, Live-Stichprobe | 2026-11-10 20:40 | 26 Kapitel · 90 Schritte · 982 Zeilen · 260 Szenen · 66 Briefe · 37 Hinweise · 0 Warnungen; 1.243 Sprachdateien. Jedes Kapitel 11–26 hat ≥ 3 Wahl-Stellen (eine mit Flag) und 3–5 Schritte (neu: Epilog-Schritte K18_S03 Gedenkstein, K19_S03 Schiff nach Osten, K25_S03 Vor dem Erwachen). `StoryPathTests` auf 26 Kapitel erweitert: alle 27 Kombinationen bis Finale, Varianten K17/K23 je nach Wahl, keine fremde Variante. Live: Sprung K25 S3 → Lin Sao steht an der Zielstelle, Abschluss führt nach K26/Herz der Flut, 0 Exceptions. Fehler gefunden + behoben: `DebugSetChapter` setzte den Schritt nicht (Kommentar hatte die Anweisung verschluckt) |
| Zwischenbericht 1–3 | im Chat gesendet (zusammengefasst) | 2026-11-10 20:25 | Zonen 30 (20 neu), Tests 493/493, Konsole 0/0 |
| Live-Stichprobe Welt | Play Mode, Level 80 | 2026-11-10 20:28 | Weite von Ahrn 123 FPS / 28 Kämpfer / Ladezeit 0,5 s · Sonnentor 127 / 4 / 0,4 s · Zarathis 121 / 28 / 0,6 s · Jadehafen 119 / 4 / 0,4 s (3 Aufzüge) · Herz der Flut 120 / 28 / 0,4 s; Spieler jeweils am Boden, 0 Exceptions (Editor-Messung, `1/smoothDeltaTime`) |


## Welt-Lauf W1 – Wahrzeichen, Vertikalität und Reittier-Hürden (Start 2026-11-11 01:20)
Befund vor Beginn (Inventur, alle 30 Zonen): keine Wahrzeichen aus einem Baukasten (nur Szenen-Bauten der Läufe 3–11: Weiler, Ruinen, Aussichtstreppen, Silbermund-Portalring), keine Gelände-Operationen (Biom-Rauschen + Merkmale „Tafelberg/Insel/Lavaring“), Hürden nur als Biom-Wasser bzw. Lava, keine Geheim-Truhen, keine Lore-Tafeln, keine Entdeckungsschleifen, keine Umwelt-Szenen, Sprungrätsel ohne Kontrollpunkte. Drei Pflicht-Sehenswürdigkeiten verlangten ein Reittier (Insel der Irrlichter: Skimmer, Adlerhorst: Springer, Verlassene Esse: Raptor). `NpcRescue` lag in `WorldHelp.cs` → fehlendes Skript am Spieler (Ursache der alten Warnung).

**Nachher (Plan, vom Validator geprüft; Spalten: Haupt-Wahrzeichen · Neben-Wahrzeichen · Gelände-Operationen · Hürden · Aufstieg · Geheimnisse · Lore · Schleifen · Szenen · beste Abkürzung · Tore/Rückkehr-Tore · Sichtbarkeit · neue Fußwege):**

| Zone | Stufe | Haupt-Wahrzeichen | Neben | Ops | Hürden | Aufstieg | Geh. | Lore | Schl. | Sz. | Abk. | Tore | Sicht | Fußweg |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Grenzmark Ashveil | 1–15 | Leuchtfeuerturm Sonnfurt (Turm, 60 m) | 0 | 0 | Dornenhecke, Abgrund | 56 m (Bau) | 7 | 5 | 5 | 3 | – (kompakt) | 8/5 | 100 % | 0 |
| Nebelmoor | 15–25 | Versunkene Kathedrale (Kathedrale, 90 m) | 1 | 7 | Giftwasser, Giftwasser | 149 m | 7 | 6 | 5 | 3 | 128 s | 15/4 | 97 % | 1 |
| Frostkamm | 25–35 | Eiszitadelle (Zitadelle, 120 m) | 1 | 5 | Eiswasser, Windwand, Steilwand | 206 m | 7 | 6 | 5 | 3 | 138 s | 11/5 | 93 % | 1 |
| Glutöde | 35–45 | Obsidianpyramide (Pyramide, 100 m) | 1 | 5 | Lava, Windwand | 156 m | 7 | 6 | 5 | 3 | 129 s | 11/6 | 96 % | 1 |
| Silberküste | 40–50 | Leuchtturm Silbermund (Turm, 70 m) | 1 | 3 | Abgrund, Steilwand | 148 m | 7 | 6 | 5 | 3 | 147 s | 11/3 | 93 % | 0 |
| Dämmerwald | 50–60 | Dämmerkathedrale (Kathedrale, 140 m) | 1 | 3 | Dornenhecke, Verderbnis-Feld, Dornenhecke | 159 m | 7 | 6 | 5 | 3 | 107 s | 16/4 | 97 % | 0 |
| Dünenmeer | 60–70 | Pyramide des Sandkönigs (Pyramide, 90 m) | 2 | 6 | Treibsand, Windwand | 158 m | 7 | 7 | 5 | 3 | 162 s | 17/8 | 97 % | 0 |
| Kristalltiefe | 65–75 | Kristallkathedrale (Kathedrale, 90 m) | 1 | 3 | Abgrund, Steilwand | 169 m | 7 | 6 | 5 | 3 | 97 s | 10/0 | 97 % | 0 |
| Himmelsspitzen | 75–80 | Wolkenthron (Himmelsinsel, 240 m) | 1 | 3 | Windwand, Abgrund, Steilwand | 293 m | 7 | 6 | 5 | 3 | 257 s | 11/0 | 94 % | 0 |
| Tyrannenfeste | 80 | Thronturm (Zitadelle, 150 m) | 1 | 5 | Verderbnis-Feld, Lava | 159 m | 7 | 6 | 5 | 3 | 146 s | 11/0 | 98 % | 0 |
| Weite von Ahrn | 30–40 | Liegender Steinriese (Steinriese, 100 m) | 1 | 3 | Abgrund, Steilwand | 148 m | 7 | 6 | 5 | 3 | 131 s | 10/5 | 97 % | 0 |
| Nebelkamm | 30–40 | Nebelkathedrale (Kathedrale, 85 m) | 1 | 6 | Giftwasser, Steilwand | 168 m | 7 | 6 | 5 | 3 | 131 s | 13/3 | 94 % | 0 |
| Sturmbucht | 40–50 | Klippenkathedrale über dem Meer (Kathedrale, 110 m) | 1 | 4 | Abgrund, Steilwand | 158 m | 7 | 6 | 5 | 3 | 145 s | 12/3 | 97 % | 0 |
| Aschenkessel | 50–60 | Feuertempel im Krater (Pyramide, 70 m) | 1 | 5 | Lava, Lava, Windwand | 156 m | 7 | 6 | 5 | 3 | 125 s | 14/7 | 89 % | 0 |
| Grüner Schlund | 55–65 | Weltenbaum (Riesenbaum, 200 m) | 1 | 3 | Dornenhecke, Abgrund | 154 m | 7 | 6 | 5 | 3 | 128 s | 14/4 | 98 % | 0 |
| Schilfmündung | 60–70 | Delta-Tempel auf Pfählen (Pagode, 70 m) | 1 | 6 | Giftwasser, Giftwasser | 148 m | 7 | 6 | 5 | 3 | 108 s | 13/4 | 100 % | 0 |
| Weiße Stille | 70–80 | Eispyramide (Pyramide, 120 m) | 2 | 5 | Eiswasser, Windwand, Steilwand | 190 m | 7 | 7 | 5 | 3 | 175 s | 14/0 | 89 % | 0 |
| Schädelküste | 75–80 | Versunkene Kathedrale der alten Hauptstadt (Kathedrale, 100 m) | 1 | 6 | Giftwasser, Verderbnis-Feld, Abgrund | 148 m | 7 | 6 | 5 | 3 | 145 s | 14/0 | 99 % | 0 |
| Sonnentor (Stadt) | 80 | Oasenpalast (Zitadelle, 70 m) | 1 | 1 | – | 71 m (Bau) | 7 | 6 | 5 | 3 | – (kompakt) | 8/0 | 100 % | 0 |
| Große Leere | 80 | Große Pyramide von Zarathis (Pyramide, 120 m) | 1 | 5 | Treibsand, Windwand, Sandportal | 159 m | 7 | 6 | 5 | 3 | 142 s | 13/0 | 96 % | 0 |
| Klingenschlucht | 80 | Felsbrücken-Kathedrale (Kathedrale, 100 m) | 1 | 2 | Abgrund, Windwand, Sandportal | 169 m | 7 | 6 | 5 | 3 | 132 s | 13/0 | 96 % | 0 |
| Spiegel der Sonne | 80 | Spiegeltempel (Pagode, 70 m) | 1 | 6 | Treibsand | 148 m | 7 | 6 | 5 | 3 | 125 s | 10/0 | 96 % | 0 |
| Glutkessel | 80 | Flammentempel (Pyramide, 80 m) | 1 | 5 | Lava, Lava | 156 m | 7 | 6 | 5 | 3 | 143 s | 12/0 | 93 % | 0 |
| Zarathis | 80 | Thronpyramide (Pyramide, 110 m) | 1 | 7 | Treibsand, Lava, Sandportal | 145 m | 7 | 6 | 5 | 3 | 141 s | 15/0 | 92 % | 0 |
| Jadehafen (Stadt) | 80 | Hafenpagode (Pagode, 90 m) | 1 | 1 | – | 90 m (Bau) | 7 | 6 | 5 | 3 | – (kompakt) | 8/0 | 100 % | 0 |
| Grüne Flut | 80 | Jadekathedrale (Kathedrale, 120 m) | 1 | 6 | Giftwasser | 148 m | 7 | 6 | 5 | 3 | 142 s | 12/0 | 98 % | 0 |
| Flüsterhain | 80 | Große Pagode (Pagode, 110 m) | 1 | 3 | Abgrund, Steilwand | 178 m | 7 | 6 | 5 | 3 | 155 s | 11/0 | 99 % | 0 |
| Sonnenstufen | 80 | Kaiserpalast (Zitadelle, 100 m) | 1 | 3 | Abgrund | 149 m | 7 | 6 | 5 | 3 | 143 s | 9/0 | 98 % | 0 |
| Nebelküste | 80 | Drachenknochen-Bogen (Steinriese, 160 m) | 1 | 3 | Abgrund, Windwand | 148 m | 7 | 6 | 5 | 3 | 145 s | 9/0 | 94 % | 0 |
| Herz der Flut | 80 | Drachenarena (Himmelsinsel, 110 m) | 1 | 3 | Abgrund | 148 m | 7 | 6 | 5 | 3 | 162 s | 9/0 | 89 % | 0 |

| Meilenstein | Status | Ende | Nachweis |
|---|---|---|---|
| W1 Werkzeuge | fertig | 2026-11-11 02:15 | Laufzeit `Scripts/World/Landmarks/` (TerrainOps 11 Arten, HazardRules/Exposure 10 Hürden mit Rettungsfenster ≥ 4 s, GateRules nach Band, LandmarkKit 11 Typen mit Rampen ≤ 32°/Vorsprüngen ≤ 13 m/Innenraum/Ankern, ClimbTemplate + CheckpointTracker, Visibility/Shortcut/InteriorCulling/SecretRules, WorldValidator, Komponenten je eigene Datei), Editor `WorldPlanner` (30 Zonen-Steckbriefe), `WorldW1Builder` (Bau, LOD/Impostor, Innenraum-Culling, Stufen-Belohnungen, Hürden mit Randmarkierung + Schild, Aufstieg mit Startturm/Aufwind, Truhen, Lore, Schleifen-Auslöser, Szenen, Fußwege), Haken in BiomeZoneBuilder/ZoneBuilder, Menü „Welt W1“, Galerie `PT_LandmarkGallery`, Admin `/landmark tp|list`, `/hazard show`, `/climb stage N`, Karten-Höhenlinien (jede 5. kräftiger). Konsole 0/0. Alle 30 Pläne validieren (Probe-Bau Nebelmoor: Validator OK, 7 Truhen, 8 Lore-Tafeln) |
| W2–W5 Zonen (bestehend, Städte, Kernland, Erweiterungen) | fertig | 2026-11-11 02:39 | Rebuild Everything (≈ 19 min, 30 Zonen + Core/Galerien) mit W1-Bau; „Gebaute Zonen prüfen“ auf den gespeicherten Szenen: **30/30 ohne Fehler**, je Zone 7 Truhen, 6–12 Lore-Tafeln, 1 Aufstieg, 1–3 Wahrzeichen, 0 fehlende Skripte. Fußwege: Insel der Irrlichter (Bohlenweg), Adlerhorst (Felsrampe), Verlassene Esse (Steinbrücke) – Flutfüllung „zu Fuß erreichbar“. 178 Aufnahmen unter `Temp/PT_QA/W1/<Szene>/` (Wahrzeichen vom Spawn, nah, Luftbild ohne Nebel, Aufstieg, Gipfelblick, Hürde) + Galerie + Live |
| W6 Erfolge, Leistung, Doku | fertig (Meshy offen) | 2026-11-11 02:50 | 4 Erfolge (Wahrzeichen-Sammler 30, Gipfelstürmer 10, Schatzsucher 50, Chronist 60) mit Haken in Wahrzeichen/Aufstieg/Truhe/Tafel. Live (Play Mode, Startzone, Level 80): `/landmark tp haupt` → vor dem Leuchtfeuerturm, Entdeckung + Erfolg 1/30, `/climb stage 3` → Laterne (59 m) mit Aussichts-Prompt, 120 FPS, 0 Exceptions. Wahrzeichen je Material vereint + Impostor-LOD. **766/766 EditMode** (+253 W1), Konsole 0 Fehler / 0 Warnungen (alte „missing script“-Warnung behoben). Doku: LANDMARKS (neu), ADMIN, MOUNTS, MECHANICS, BALANCE, GW2_ABGLEICH, EXPANSIONS, PLACEHOLDERS, README, DECISIONS, KNOWN_ISSUES 76–80. Meshy (Budget 6) nicht genutzt – KNOWN_ISSUES 79 |
| Fix nach W1: Start auf dem Leuchtturm-Dach | fertig | 2026-11-11 08:40 | Ursache: die Live-Prüfung (`/climb stage 3`) hatte die Spielerposition auf der Dachkappe des Leuchtfeuerturms (63 m) gespeichert, und der Admin-Teleport suchte den Boden von oben (Dach statt Laternen-Galerie). Behoben: Spieler auf die Sonnfurt-Wegmarke zurückgesetzt und gespeichert; Admin-Teleporte setzen exakt; Laternen-Boden der Türme hat eine Luke über dem Treppen-Ende (vorher endete die Wendelrampe unter dem Boden). 8 Zonen mit Türmen neu gebaut, 30/30 Zonen ohne Fehler, 767/767 EditMode (+1 Test „Treppe öffnet sich in die Laterne“). Live: WASD vom Start 16 m in 2 s, `/climb stage 3` landet auf dem Laternen-Boden |


## Belebung aller Zonen (Spieler-Wunsch, Start 2026-11-11 11:40)
Befund: Regions-Zonen 24–27 Gegner auf 1,1–1,3 km² (≈ 20/km², 10 Lager à 3/3/1), Städte Sonnentor/Jadehafen ohne Bürger, Wildnis ohne Bewohner außer Herz-NPCs. Der Dichte-Faktor der Gefahrenstufen (`DangerSettings.Density`) war ungenutzt.

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Belebung Gegner + Bewohner | fertig | 2026-11-11 12:15 | `PopulationModel` (Zieldichte 50/km² × Dichte-Faktor der Stufe, Lagerabstand ≥ 2 × Aggro + 8 m, Gruppengröße/Veteranen nach Stufe) + `LivelinessBuilder` (Lager im Balance-Pass, Bewohner im Zonen-Bau). Rebuild Everything: +537 Gegner in 216 Lagern über 25 Wildnis-Zonen (z. B. Weiße Stille 24 → 71, Große Leere 24 → 65, Weite von Ahrn 24 → 58), Insel-/Wasserzonen wenig (Dichte gilt je trockener Fläche); +469 Bewohner in 28 Zonen (2 je Wegmarke, 3 je Herz; davon Sonnentor 35 und Jadehafen 19 Bürger). Balance-Validator 0 Verstöße in allen 30 Zonen, W1-Validator 30/30. Live: Weiße Stille 71 Gegner + 22 Bewohner bei 120 FPS, Sonnentor 35 Bürger bei 121 FPS, 0 Exceptions. 773/773 EditMode (+6 `LivelinessTests`), Konsole 0/0 |
| Kontrollpunkte ohne Auto-Zurücksetzen (Spieler-Wunsch) | fertig | 2026-11-11 20:40 | Absturz setzt nicht mehr zurück; nur Niederlage auf dem Pfad → letzter Kontrollpunkt; 1,5 s am Boden abseits des Pfads (> 18 m von Serpentine/Vorsprüngen/Platten) → Verfolgung verworfen, Niederlage → Wegmarken-Wahl. 776/776 EditMode (+3). Rebuild Everything: 30 Aufstiege mit Pfadpunkten (z. B. Frostkamm 137), Balance 0 Verstöße. Live (Frostkamm): Niederlage auf Stufe 2 → Wiederbelebung am Kontrollpunkt 10, ohne Karte; abseits des Pfads → Kontrollpunkt verworfen, Niederlage → Wegmarken-Wahl (5 Optionen), 0 Exceptions |


## Grafik-Lauf G1 – Bestand und Werkzeuge (Start 2026-11-11 21:00)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Datenmodelle + reine Modelle | fertig | 2026-11-11 | Namespace `ProjectTyrant.Visuals`: `ZoneArtProfile` (30), `ElementPalette` (10), `CameraBookmark` (184 = 6 × 30 + 4 Klassen), `TextureRecipe` (22), `GraphicsLibrary` am GameManager; reine Modelle `VisualMetrics`, `DangerReadability` (CIE76-ΔE), `TextureForge`, `TimeOfDayModel` (48 min), `WeatherModel` (6–14 min, 30 s Blende, 30 % Partikel in Arenen), `EmissiveSchedule`, `PerformanceGovernor`, `QualityTiers`, `Photosensitive` |
| TextureForge | fertig | 2026-11-11 | 22 Rezepte → `Generated/Textures` (17 MB), Determinismus-Prüfung 0 Abweichungen, Hashes in den Rezepten |
| Admin-Gerüst | fertig | 2026-11-11 | /cam bookmark·release·capture all, /gfx show·preset·feature·stress·perf, /tod, /weather, /vfx play·loop·slow·capture; `GfxOverlay`, `GraphicsDirector` (Teil des GameManager-Objekts, kein neues Singleton) |
| Vorher-Captures | fertig | 2026-11-11 | 180 Zonenbilder (`vorher/<Zone>/<Lesezeichen>.png`) + `metrics_vorher.csv`; 56 Skill-Bilder (28 Skills × 2 Phasen, 7 je Klasse), 7 Reittiere, 9 Champions/Veteranen als Boss-Stellvertreter; Kontaktbögen `sheets/` |
| Leistung vorher | fertig | 2026-11-11 | `perf_vorher.csv` (180 Lesezeichen, ungedrosselt, Editor, RTX 5080): FPS-Mittel min 166 / Median 336; 1-%-Tief < 45 nur an 2 Lesezeichen (Silberküste Wahrzeichen 6,9 – Nachlade-Ruckler, Sonnfurt Wegmarke 36); Draw Calls Median 414 / max 2004; SetPass max 110; Partikel max 1217. Gedrosselte Messung `perf_vorher_vsync.csv` |
| Technischer Bestand | fertig | 2026-11-11 | `bestand_vorher.md`: 1 URP-Asset für alle 6 Stufen (Forward, 2 Kaskaden/110 m, keine Renderer-Features), 9 eigene Shader, 1150 Materialien, davon 24 auf Unity-Shadern (22 URP/Lit, 2 Particles/Unlit) |
| ART_BIBLE | fertig | 2026-11-11 | Stilregeln, Biomtabelle, 3 Befunde je Zone, Querschnittsbefunde Q1–Q5; Gefahr-ΔE vorher: nur 2 von 30 Zonen ≥ 25 |
| Tests | fertig | 2026-11-11 | 889/889 EditMode (+113 inkl. TestCases: `GraphicsG1Tests`, `GraphicsG1DataTests`, `GraphicsG1PerfTests`), Konsole 0 eigene Fehler/Warnungen |

## Grafik-Lauf G2 – Rendering-Basis (Start 2026-11-11 22:05)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| URP je Stufe | fertig | 2026-11-11 22:40 | `PT_URP_Niedrig/Mittel/Hoch/Ultra` + Renderer, alle Forward+; Schatten 1/2/4/4 Kaskaden bis 40/80/150/200 m (weich ab Mittel), SSAO ab Mittel (Tiefe+Normalen), Decals (DBuffer, Albedo), Renderskala 0,75/0,85/1/1; Unity-Stufen 0–1/2/3–4/5 zugeordnet, Standard = Hoch |
| Render-Graph-Passes | fertig | 2026-11-11 22:40 | `PTRenderFeature`: Kontur, Nebel (nach Himmel), Lichtschaft, Verzerrung (Hitze + Schockwellen, Dispersion), Unterwasser, Schaden – je Schalter in /gfx feature und Bedingung (z. B. Unterwasser nur unter der Wasserlinie) |
| Licht/Himmel/Tageszeit | fertig | 2026-11-11 22:40 | `SceneLighting` (Sonne/Mond, Trilight, Höhennebel, Himmel 2.0 `PT/Sky`, ACES-Volume je Zone, Zeit-Volume für Belichtung, Zonen-Cubemap als Echtzeit-Sonde nur mit Himmel); `GraphicsDirector` je Bild, 48-min-Zyklus; Sandsturm und Dunkelheit (Kristalltiefe) steuern den Nebel weiter |
| ToonLit 2.0 + Migration | fertig | 2026-11-11 22:40 | `PT_Common.hlsl`, `PT/ToonLit` (Forward+, SSAO, Decals, Cookies, Wind in allen Passes, DepthNormals); `MaterialMigrator`: 861 ToonRim, 22 URP/Lit, 2 URP-Partikel, 30 Himmel umgestellt, 22 Meshy-FBX ohne eingebettete Materialien; Validator 0 Restmaterialien |
| Telegrafen tiefensicher | fertig | 2026-11-11 22:40 | GroundMarker: ZTest Always mit Verdeckungsprüfung, deckender Ring ohne Nebel; Gefahr-ΔE Kampf (vorher → G2-Test): Sonnfurt 4→22, Sturmbucht 1→27, Weite 11→26, Dämmerwald 4→26, Kristalltiefe 12→30, Glutöde 20→26 |
| Messung | fertig | 2026-11-11 22:40 | Sonnfurt im Spiel (Hoch): 334–470 FPS; Draw Calls Horizont 3488 (> 2500, 4 Kaskaden) → Leistungs-Pass G7; 908/908 EditMode (+19 `GraphicsG2Tests`), Konsole 0/0 |

## Grafik-Lauf G3 – Shader-Bibliothek 2.0 (Start 2026-11-11 22:42)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Toon-Familie | fertig | 2026-11-11 23:00 | `PT_ToonForward.hlsl` (gemeinsamer Forward-Pass mit Haken für Oberfläche und Zusatzlicht) + Vorlage: ToonLit, Terrain (Hang-Fels, Großflächen-Variation), Vegetation (Durchscheinen, Wind, beidseitig), Cloth, Lava (fließende Kruste, HDR-Glut), Crystal (Facetten, Atem, Fresnel), Hazard (Muster je Art, Warnstreifen am Rand); alle mit DepthOnly/ShadowCaster/DepthNormals und Wind |
| Transparent/UI | fertig | 2026-11-11 23:00 | Water2 (Brechung ab Mittel, Kaustik am Grund, Schaum, Cubemap-Fresnel, Nebel im Shader), Glass, UI/Glow (uGUI-Maske, lichtempfindlich gedeckelt), Sky, PostFx, GroundMarker tiefensicher |
| Migration | fertig | 2026-11-11 23:00 | Familie nach Materialname: 818 ToonLit, 15 Hazard, 10 Vegetation, 6 Crystal, 6 Glass, 5 Cloth, 1 Terrain; 21 Wasser → Water2, 5 Lavaseen → Lava; Validator: 0 Materialien auf alten/Unity-Shadern |
| Validator | fertig | 2026-11-11 23:00 | `ShaderLibraryValidator`: CBUFFER, Pflicht-Passes, ≤ 8 multi_compile je Pass, SRP-Batcher 13/13, Varianten 1428 (≤ 4000; Toon-Familie je 200 nach Wegfall von `_ADDITIONAL_LIGHTS` – Forward+ setzt es selbst – und DBuffer nur Albedo) |
| Tests | fertig | 2026-11-11 23:00 | 925/925 EditMode (+17 `GraphicsG3Tests`), Konsole 0/0; Gefahr-ΔE Kampf G3-Test: Glutöde 27, Nebelmoor 24, Silberküste 27, Kristalltiefe 30, Dämmerwald 30 |

## Grafik-Lauf G4 – Wetter und lebendige Welt (Start 2026-11-11 23:01)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Wetter | fertig | 2026-11-12 00:10 | 10 Zustände je Zonenprofil, Wechsel 6–14 min, 30 s Überblendung (Partikel-Betten Regen/Jaderegen/Schnee/Schneesturm/Sandsturm/Asche/Niesel, Licht, Nebel, Wolken, Nässe/Schnee in den Shadern), Blitze bei Gewitter (lichtempfindlich gedeckelt), Deckel 30 % in Sprungrätseln und bei Champions; Nebelmoor-Gewitter im Spiel: 1609 Partikel, 120 FPS (vSync) |
| Umgebung | fertig | 2026-11-12 00:10 | 150 Umgebungs-Volumen (≥ 5 je Zone, aus Lesezeichen-Blickpunkten und Biom-Effekten), nur in Kameranähe aktiv, Glühwürmchen nachts / Pollen tags, Dämpfung auf 40 % im 15-m-Kampfradius, Leistungs-Regler (45/55 FPS, Hysterese) |
| Licht | fertig | 2026-11-12 00:10 | Laternen/Fenster/Fackeln mit `_NightEmission` (Migrator nach Name), Blätter-Cookie der Sonne in Wald-Zonen bei direkter Sonne (in G7 wieder abgeschaltet, KNOWN_ISSUES 91) |
| Tests | fertig | 2026-11-12 00:10 | 936/936 EditMode (+11 `GraphicsG4Tests`, u. a. Gameplay-Neutralität), Konsole 0/0 |

## Grafik-Lauf G5 – VFX 2.0 (Start 2026-11-12 00:12)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Datenmodell | fertig | 2026-11-12 | `VfxDefinition` (5 Phasen Vorlauf/Aufladung/Auswurf/Einschlag/Nachwirkung, Ebenen Composite/Licht/Druckwelle/Spur/Kamerastoß, Stufen Standard/Finisher/Elite mit ≥ 3/6/5 Ebenen), `VfxComposer` aus Zeitachse + Palette |
| Wiedergabe | fertig | 2026-11-12 | `VfxLayerPlayer` (verzögerte Warteschlange, 12 gepoolte Lichter, Lichtkurven), `VfxPlayer.PlayV2`, `SkillFxDirector` spielt je Phase; Druckwellen über den einen Verzerrungspuffer |
| Daten | fertig | 2026-11-12 | `VfxDefinitionBuilder.BuildAll` (Teil von Rebuild Everything): 466 Skills (317 Standard, 143 Finisher, 6 Elite), Validator 0 Verstöße – kein Skill ohne Effekt |
| Zustands-Auren | fertig | 2026-11-12 | `EffectAuraFx` (Zustände/Segen, dominanter Effekt je Ziel) |
| Tests | fertig | 2026-11-12 | +15 `GraphicsG5Tests` (inkl. TestCases), Konsole 0/0; Showcase nachher unter `Temp/PT_QA/G1/nachher_g5` |

## Grafik-Lauf G6 – Signaturen (Start 2026-11-12 00:40)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Kampf | fertig | 2026-11-12 | Trotzbalken-Bruch (Druckwelle, Licht, Kamerastoß lichtempfindlich 0), Auflösen beim Tod |
| Rang | fertig | 2026-11-12 | `RankAuraFx`: Veteran/Champion/Boss mit Ring, Funken, Licht in Rangfarbe |
| Reise | fertig | 2026-11-12 | Wegmarken-Sprung Abreise/Ankunft in Biomfarbe; Reittier Rufen/Absitzen/Spur/Landung in Reittierfarbe |
| Gekürzt | – | – | Volks-Signaturen, Zwischensequenz-Profile, UI-VFX (Kürzungsreihenfolge des Auftrags), siehe KNOWN_ISSUES |
| Tests | fertig | 2026-11-12 | +8 `GraphicsG6Tests` (inkl. TestCases) |

## Grafik-Lauf G7 – Stufen, Leistung, Nachher, Doku (Start 2026-11-12 01:00)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Stufen + Optionen | fertig | 2026-11-12 | 4 Stufen, Auto-Erkennung beim Erststart, 8 neue Einträge im Grafik-Reiter (u. a. Lichtempfindlich, Leistungs-Regler), `PTShaderStripper` |
| Post-Processing-Befund | fertig | 2026-11-12 | `postProcessData` war seit Lauf 1 leer → kein Post lief; zugewiesen, Belichtung je Zone kalibriert (`CalibrateExposure`, `belichtung.txt`), Himmel-Ausgleich |
| Fehlerbilder | fertig | 2026-11-12 | SSAO-Flecken = Blätter-Cookie (aus), Niedrig ohne Tiefe (copyDepth AfterOpaques), große Regentropfen (`maxParticleSize`), Zeit-Volume überschrieb Zonen-Belichtung |
| Telegrafen | fertig | 2026-11-12 | Overlay-Pass nach Post; Ringfarbe nach geschätzter Bildschirmhelligkeit des Bodens (`_PT_ExposureMul` + ACES-Näherung): hell → Dunkelrot, dunkel → Gelborange |
| Nachher-Bilder | fertig | 2026-11-12 | 180 Lesezeichen × Tag/Nacht/Regen (`nachher`, `nachher_nacht`, `nachher_regen`); verbessert 163/180 = 90,6 % |
| Gefahr-ΔE | teilweise | 2026-11-12 | ≥ 25 in Tag 23/30, Nacht 27/30, Regen 24/30 Kampf-Lesezeichen (Ausreißer KNOWN_ISSUES 86) |
| Leistung Hoch | fertig | 2026-11-12 | `perf_nachher.csv` 180 Lesezeichen ungedrosselt: FPS min 151 / Median 413, Draw Calls Median 811 / max 2518 (1 Lesezeichen über 2500) |
| Leistung Niedrig | fertig | 2026-11-12 | `perf_nachher_niedrig.csv` (Werte PERFORMANCE.md) |
| Doku | fertig | 2026-11-12 | GRAPHICS.md, PERFORMANCE.md neu; VFX, ADMIN, ART_BIBLE, DECISIONS, KNOWN_ISSUES ergänzt |
| Tests | fertig | 2026-11-12 | 960/960 EditMode (+184 im Grafik-Lauf inkl. TestCases, Basis 776), Konsole 0 eigene Fehler/Warnungen |

## VFX-Lauf V1 – Element-Simulationen, Milestone V1 Bestand und Werkzeuge (Start 2026-11-12 01:05)

Grafik-Lauf G1–G7 war beim Start abgeschlossen (Docs GRAPHICS, PERFORMANCE, VFX, ART_BIBLE vorhanden); nicht vorhanden waren TextureForge-Rezepte als eigener Baukasten, FxMeshKit, MotionMetrics und ClassFxStyle – FxMeshKit und MotionMetrics in V1 nachgeholt.

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Fluid-Löser | fertig | 2026-11-12 01:15 | `FluidSolver2D` (Stable Fluids, Auftrieb, Schwerkraft, Wirbelstärke, Hindernisse, 4 Emitter-Formen), deterministisch |
| Flipbook-Baker | fertig | 2026-11-12 01:30 | 4 Atlanten je Rezept, Burst/Loop, `SimRecipe` mit Parameter-Hash; 7 Rezepte, 22,8 MB; zweiter Lauf „0 gebacken, 7 unverändert“; Hash zweier Läufe identisch für alle 7 |
| Generatoren | fertig | 2026-11-12 01:15 | SDF (7 Formen), Kaustik (nahtlos), Blitz (Verzweigung, Zittern), FxMeshKit 2.0 (13 Formen), MotionMetrics |
| GPU-Partikel | fertig | 2026-11-12 01:35 | Compute `PT_GpuParticles` (Emit/Update, Tiefen-Kollision), `GpuParticleSystem` mit Shuriken-Fallback, Budgets 100 000 / 200 000 |
| Verzerrungs-Puffer | fertig | 2026-11-12 01:35 | `PTDistortionAccumulatePass` + Blit in PT_PostFx Pass 3, Shader `FX/Refract`; Bild `Temp/PT_QA/V1/werkzeuge/verzerrung20.png` |
| Flipbook-Shader | fertig | 2026-11-12 01:35 | `FX/Flipbook` (Bewegungsvektoren, Normalen-Licht, Emissiv-Kern, Soft-Particles) |
| Messung | fertig | 2026-11-12 01:40 | 100 000 GPU-Partikel + 20 Verzerrer im Spiel (Hoch, Editor): Bildzeit 2,97 → 3,04 ms |
| Inventar + Schaustücke | fertig | 2026-11-12 01:17 | 466 Skills, Wasser 10,7 %, Feuer 20,8 %, Brechung 13,1 %, 164 Schaustücke (`inventar.csv`) |
| Vorher-Aufnahmen | fertig | 2026-11-12 01:32 | 28 Skills × 5 Phasen + 8-Bild-Streifen = 364 Bilder; Bewegung vorher Differenz 0,0103, Kohärenz 0,13 |
| Befunde | fertig | 2026-11-12 02:40 | VFX.md „Befund V1“ |
| Docs | fertig | 2026-11-12 02:40 | SIMULATION.md, ELEMENTS.md neu |
| Tests | fertig | 2026-11-12 01:37 | 1036/1036 EditMode (+76 V1: `ElementSimV1Tests`, `ElementAssignV1Tests`, `GpuDistortV1Tests`), Konsole 0/0 |
| Offen in V1 | – | – | ClassFxStyle je Klasse, Galerie-Element-Bänke, Admin `/vfx layer|step|sim bake|gpu`; Milestones V2–V7 nicht begonnen (KNOWN_ISSUES 94–99) |

## VFX-Lauf V2 – Wasser, erster Durchgang (Start 2026-11-12 08:15)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Antworten Auftraggeber | fertig | 2026-11-12 08:20 | Kombo vertiefen (V6), Brechung stark (Material `Brechung_Stark` 24 px / Dispersion 0,6), keine Nutzungsdaten – DECISIONS |
| Lagen-Arten | fertig | 2026-11-12 09:00 | VFX 2.0 um Sim, FxMesh, Refract, WorldMark, Gpu erweitert; Wirt `ElementFxHost` (gepoolt, ≤ 64 Instanzen, ≤ 16 GPU-Effekte) |
| Wasser-Baukasten | fertig | 2026-11-12 09:00 | `ElementKits` je Typ (Projektil, Fläche/Heilung, Aura, Nahkampf/Sprung) + Schaustück-Zusatz; neue Flipbooks Spritzer seitlich und Kaustik-Loop; Materialien Wasser-Körper, Pfütze, Nässe, Brechung; GPU-Vorlagen sammeln, Tropfen, Gischt |
| Welt-Kontakt | teilweise | 2026-11-12 09:00 | Pfütze (trocknet aus), Nässe beim Aufladen, Ringe auf Wasser, Dampf auf Lava (nach Kollider/Material-Name); Vegetation biegt sich noch nicht |
| Migration | fertig | 2026-11-12 09:05 | 50 Wasser-Skills (20 Schaustücke) mit Element-Lagen, 418 neue Lagen; Validator 0 Verstöße (G5 + V2-Prüfung) |
| Nachher | fertig | 2026-11-12 09:30 | `Temp/PT_QA/V1/nachher_v2`, Vergleich `sheets/v2_wasser_vergleich.jpg`; Bewegung Heilquelle 0,0041 → 0,0063 (Kohärenz 0,05 → 0,15), Quellsegen 0,0056 → 0,0066 (Kohärenz 0,27 → 0,13) |
| Tests | fertig | 2026-11-12 09:30 | 1050/1050 EditMode (+14 `WaterKitV2Tests`), Konsole 0/0 |

## Fix: Löcher in Gegnern und Reittieren (Spieler-Meldung, Start 2026-11-12 13:55)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Ursache | fertig | 2026-11-12 14:05 | `SkinMesh` (MeshKit-Charaktere): (1) `Basis` spiegelte die Seitenachse ohne die Vorderachse → Röhren/Kegel mit Seitenachse nach links (linke Gliedmaßen, Hörner, Schwanz) nach innen; (2) alle Kästen (Hände, Füße, Zehen, Nase, Schnallen) nach innen; (3) Röhren-Deckel nach innen. Back-Face-Culling verwarf diese Flächen. Befund vorher: 1143 von 1579 Meshes, 9296 nach innen zeigende Teile (ohne Kästen) |
| Korrektur | fertig | 2026-11-12 14:10 | Basis dreht Vorder- mit Seitenachse, Kasten- und Deckel-Reihenfolge umgekehrt, `AddMesh` dreht bei gespiegelter Übernahme; +8 Tests `CharacterMeshFixTests` (alle Richtungen, Kegel, Kasten gedreht, Ellipsoid, Reparatur-Werkzeug) |
| Rebuild Everything | fertig | 2026-11-12 14:35 | 1073 s ohne Fehler; danach 991 geskinnte Meshes geprüft: nach innen zeigende Röhren/Kegel/Ellipsoide 0 in allen neu gebauten Meshes (übrige Treffer: gewölbte doppelseitige Umhänge = Fehlalarm der Prüfung, und alte, nirgends mehr verwendete Villager-/Test-Meshes vom 09./10.11. (nicht gelöscht)) |
| Nachweis | fertig | 2026-11-12 14:45 | `Temp/PT_QA/fix_holes/mounts_nah.jpg` (Raptor, Springer beidseitig geschlossen, Füße und Kamm aus Kästen jetzt sichtbar), `ingame.jpg`; 1058/1058 EditMode, Konsole 0/0 |

## Fix: Greif treibt beim Richtungswechsel in die alte Richtung (Spieler-Meldung, 2026-11-12 15:35)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Ursache | fertig | 2026-11-12 15:40 | Flugrichtung drehte mit 1,8 rad/s zur Wunschrichtung, das volle Tempo hing an der alten Richtung: Kehrtwende ≈ 1,75 s, bei 21 m/s ≈ 12 m weiter in die alte Richtung |
| Korrektur | fertig | 2026-11-12 15:45 | `GriffonSteer`: Richtung dreht mit 10 rad/s (180° in 0,32 s), Geschwindigkeit immer exakt in Flugrichtung mit unverändertem Betrag, genaue Kehrtwende dreht waagrecht; Körper dreht mit 720°/s nach. Himmelsschuppe unverändert |
| Tests | fertig | 2026-11-12 15:50 | 1063/1063 EditMode (+5 `GriffonSteerTests`: Kehrtwende ≤ 0,34 s, Betrag bleibt, kein Rest in alter Richtung, Weg in alte Richtung < 2,5 m), Konsole 0/0 |

## Fix: Greif springt beim Loslassen von V auf waagrecht (Spieler-Meldung, 2026-11-12 15:48)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Ursache | fertig | 2026-11-12 15:55 | Loslassen setzte Sinktempo in einem Bild auf 0 und das ganze Tempo waagrecht – die Bahn knickte ab, die Neigung sprang hinterher; zusätzlich las erneutes Abtauchen das Tempo nach dem Zustandswechsel (1 × Lauf → Neigungssprung) |
| Korrektur | fertig | 2026-11-12 16:00 | Abfangbogen im Flugmodell (`PullAngle`, 240 → 110 °/s, mit S 260 °/s in den 50°-Aufschwung); aus 80° bei 21 m/s: 0,48 s, 5,0 m Höhe, die als Energie zurückkommen (Höchstmarke unverändert); erneutes V setzt am aktuellen Winkel an |
| Tests | fertig | 2026-11-12 16:05 | 1069/1069 EditMode (+6 `GriffonPullOutTests`; `Griffon_ReleaseKeepsSpeed_NoAutoLift` auf die neue Vorgabe umgestellt: Tempo als Betrag, Sinken gemessen nach dem Bogen, Bogen ≤ 8 m), Konsole 0/0 |

## Fix: Himmelsschuppe läuft schneller als sie fliegt (Spieler-Meldung, 2026-11-12 16:30)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Ursache | fertig | 2026-11-12 16:35 | Flug-Reisetempo fest 7 × 1,4 = 9,8 m/s; Laufen = Reittier-Tempo 9,1 m/s × Bewegungs-Effekte (Eile, Tempo-Boni) × Reittier-Beherrschung → mit Effekt am Boden schneller |
| Korrektur | fertig | 2026-11-12 16:40 | `SkyscaleSteer.Cruise`: Flugtempo = Lauftempo (gleiche Formel); erschöpftes Sinken und C-Sinken 60 %, Wandhaften 0; Tempo-Anzeige zeigt das echte Flugtempo |
| Tests | fertig | 2026-11-12 16:45 | 1075/1075 EditMode (+6 `SkyscaleCruiseTests`), Konsole 0/0 |

## Änderung: Himmelsschuppe mit festem Tempo (Spieler-Wunsch, 2026-11-12 19:13)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Umsetzung | fertig | 2026-11-12 19:20 | `SkyscaleSteer.FixedSpeed` 9,1 m/s in allen Flugzuständen (vorher Sinken 60 %), ohne Tempo-Effekte und ohne Beherrschungs-Faktor; am Boden `PlayerController.FixedSpeed` (überschreibt Eile, Verlangsamung, Hürden, Rückwärts, Wasser), beim Absitzen zurückgesetzt; Wandhaften 0, Sprint unverändert |
| Tests | fertig | 2026-11-12 19:20 | `SkyscaleCruiseTests` auf festes Tempo umgestellt (+1); Kompilieren 0/0 |

## Fix: Leere Anzeige beim Start + Hasen sitzen und hoppeln (Spieler-Meldung, 2026-11-12 19:27)

| Schritt | Status | Ende | Nachweis |
|---|---|---|---|
| Leere Anzeige | fertig | 2026-11-12 19:30 | Im Editor war eine leere, unbenannte Szene offen (vom abgebrochenen Testlauf um 19:20 zurückgelassen); PT_Core + Startzone wieder geöffnet, nach dem nächsten Testlauf weiter offen |
| Hasen-Gangart | fertig | 2026-11-12 19:50 | Neue Gangart `Hop` (ClipLibrary): Sitz mit aufgerichtetem Oberkörper (Winkel je Profil so, dass Vorder- und Hinterpfoten aufsetzen: Springer 23,5°, Schneehase 13°), gefaltete Hinterläufe; Fortbewegung als kleine Sprünge (Abstoßen, gestreckter Flug, Landung vorn bei 70 %, hinten bei 85 %), tiefster Fuß nie im Boden; Aufladen geduckt. Springer-Reittier und neues Profil „Hare“ für den Schneehasen (lange Ohren, kräftige Hinterläufe) |
| Bau | fertig | 2026-11-12 19:50 | Clips neu gebacken, Schneehase-Prefab neu; Bilder `Temp/PT_QA/hase/sheet.jpg` |
| Tests | fertig | 2026-11-12 19:55 | 1087/1087 EditMode (+10 `HareHopTests`), Konsole 0/0 |
| Absturz Charakterauswahl | Code fertig, Kompilieren offen | 2026-11-12 21:10 | Absturzbericht `CrashReport_1eeed754…`: Zugriffsverletzung in `mecanim::animation::DestroyAnimationSet` ← `Animator::Deactivate` ← `GameObject.SetActive` in `CharacterSelectController.Refresh` (aufgerufen von `CloseCreate`, Knopf „Abbrechen“/„Erstellen“ im Erstellungsdialog). Ursache: die Vorschaufiguren (geriggt, mit Animator) wurden per SetActive ein-/ausgeblendet – Verstoß gegen „nie SetActive auf geriggten Figuren“. Neu: `ShowFigure` schaltet nur die Renderer (`ActorVisibility.SetRenderersVisible`), die Figur bleibt aktiv (einmaliges Einschalten, falls gespeichert inaktiv). Neue Tests `CharacterSelectFigureTests` (2). Editor war beim Fix nicht erreichbar (abgestürzt) → Kompilieren/Testlauf beim nächsten Start |
