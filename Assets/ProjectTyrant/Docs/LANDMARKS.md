# LANDMARKS – Wahrzeichen, Vertikalität und Reittier-Hürden (Welt-Lauf W1)

Alles ist datengetrieben: `ZoneDefinition.WorldPlan` (Typ `ZoneWorldPlan`) hält Gelände-Operationen, Wahrzeichen, Hürden, Aufstiege, Geheimnisse, Lore, Schleifen, Abkürzungen, Szenen und Fußwege. Der Plan entsteht deterministisch aus dem Zonen-Steckbrief (`WorldPlanner.Specs`, 30 Zonen) und der Belegung der Szene; **Rebuild Everything** plant neu, wenn die Plan-Version (`WorldPlanner.PlanVersion`, derzeit 4) nicht passt. Menü: *Project Tyrant → Welt W1 → Alle Zonen neu planen / Bericht / Galerie-Szene bauen*.

## Ablauf beim Zonen-Bau
1. `WorldW1Builder.PlanZone` (falls nötig): Belegung aus der zuletzt gebauten Szene (ohne Gelände, Wasser, Vegetation), Wegmarken, Portale, Story-Orte, Wege → Aufstieg (Massiv/Gipfel), Signatur-Operation, Haupt-Wahrzeichen (unter 25 Kandidaten der sichtbarste, 120 Stichproben), Neben-Wahrzeichen (bei Platzmangel bis 80 % kleiner), Hürden, Geheimnisse/Schleifen/Szenen/Lore, Sichtbarkeit mit 500 begehbaren Punkten, Fußwege zu Pflicht-Markern.
2. `BiomeZoneBuilder.Sampler` → `WorldW1Builder.ApplyToSampler`: Operationen, Plätze, Serpentinen, Fußwege fließen ins Gelände (Karte, NavMesh, Erreichbarkeit sehen dasselbe).
3. Nach `WorldStoryBuilder.Populate`, vor NavMesh und Karte: `WorldW1Builder.Populate` baut alles unter `PT_ZoneRoot/PT_W1` und ruft den Validator (`WorldW1Builder.Reports`).

## Gelände-Operationen (`TerrainOps`, 11 Arten)
Massiv (Grate + zwei Plateaus), Gipfel, Tafelberg (absolute Plateauhöhe), Schlucht, Krater (Rand + See), Klippenküste, Senke, Höhle (Hufeisen-Ring mit Dach im Builder), Himmelsinsel (Abrisskrater darunter), Terrassen, Delta. Jede Operation wirkt nur in ihrem Radius; Reihenfolge = Liste.

## Wahrzeichen-Baukasten (`LandmarkKit`, 11 Typen)
Kathedrale, Pyramide, Zitadelle, Pagode, Riesenbaum, Steinriese, Turm, Gletscherpalast, Versunkene Stadt, Himmelsinsel, Massiv. Regeln: Rampen ≤ 32°, Springer-Vorsprünge ≤ 13 m Höhe, begehbarer Innenraum (außer Massiv), benannte Anker für Stufen. Builder: Module je Material zu einem Mesh vereint, Innenraum getrennt (`InteriorCulling`), LOD-Gruppe mit Silhouetten-Impostor ab ≈ 600 m, Glühen oben, Innenlicht, Klang (`PeriodicSound`), Kartensymbol `map_landmark` mit Tooltip (Typ, Tore, erreichte Stufen).

Maßstab (Validator): Haupt-Wahrzeichen 60–200 m (Kathedrale 80–140, Pyramide 60–120, Massiv 150–300), Neben 25–60 m; sichtbar von ≥ 60 % der begehbaren Fläche.

## Hürden (`HazardRules`, 10 Arten)
| Hürde | Wirkung zu Fuß | Rettungsfenster | Schlüssel |
|---|---|---|---|
| Treibsand | Tempo 40 %, Versinken ab 4 s, Tod 8 s (Ausweichen: −1 s) | 8 s | Skimmer, Greif, Himmelsschuppe |
| Giftwasser | Tempo 60 %, Gift 3/s, 6 % HP/s | 16,7 s | Skimmer, Flug |
| Lava | Tempo 70 %, Brennen 5/s, 10 % HP/s, Tod 10 s | 10 s | Lava-Skimmer, Flug |
| Eiswasser | Tempo 50 %, Kälte 10/s, Versinken 4 s, Tod 8 s | 8 s | Skimmer, Flug |
| Abgrund | – (Sturz) | ∞ | Raptor, Greif, Himmelsschuppe |
| Steilwand | blockiert | ∞ | Springer, Himmelsschuppe, Greif |
| Windwand | Tempo 50 %, drückt zurück | ∞ | Greif, Himmelsschuppe |
| Sandportal | – | ∞ | Dünenläufer |
| Dornenhecke | blockiert, Blutung | ∞ | Raptor, Springer, Flug |
| Verderbnis-Feld | Qual, 3 % HP/s (Deckel 10 %) | 33 s | Verderbnis-Widerstand, Flug |
Jede Hürde hat eine Randmarkierung in Warnfarbe (`HazardSurface.EdgeDecal`), Partikel und ein Schild „Benötigt: …“.

## Tore (`GateRules`)
Schlüssel ab Stufe: Raptor 1, Skimmer 15, Springer 25, Lava-Skimmer 35, Greif/Himmelsschuppe 75, übrige 80. Liegt der Schlüssel außerhalb des Zonen-Bands, ist das Tor ein **Rückkehr-Tor** („– komm später wieder“). Pflichtinhalt (Wegmarke, Herz, Sehenswürdigkeit, Späher, Story, Portal, Händler) hat nie ein Tor; Inhalte mit Fuß-Alternative (Grat, Kletterpfad, Treppe) zeigen auf der Karte kein „Benötigt“.

## Aufstiege (`ClimbTemplate`)
Drei Stufen: 1 Rastplateau zu Fuß (Serpentine ≤ 16 % Steigung, Aussicht), 2 Hochplateau Springer mit langem Grat als Fuß-Alternative (Einblick), 3 Gipfel Greif/Himmelsschuppe mit langem Kletterpfad (Gipfel-Aussicht über die ganze Zone + Truhe). Kontrollpunkte alle ≈ 15 Höhenmeter (`ClimbArea`: Sturz setzt an den letzten Kontrollpunkt zurück, Flug zählt nicht). Greif-Startturm mit Aufwind an der Basis. Städte und Startzone: Aufstieg am Haupt-Wahrzeichen (`ClimbDef.LandmarkId`).

## Geheimnisse, Lore, Schleifen, Szenen
Je Zone 7 Truhen (Wahrzeichen-Stufe, Gipfel, Höhle, Verstecke), täglich einmal (`SecretRules.DailyFlag`), ≥ 25 m von jedem Marker entfernt (Stufen-Belohnungen desselben Bauwerks ausgenommen). 5 Entdeckungsschleifen: Auslöser (Rauchsäule, Lichtstrahl, Vogelschwarm, Glocke alle 30 s, Kristallglühen) → Truhe → Lichtsäule zum nächsten Ziel (`NextGoalBeam`). 3 Umwelt-Szenen (Lager, Schlachtfeld, Grab, Karawane, Wrack) je mit Lore-Tafel; Lore-Tafeln zusätzlich an jedem Wahrzeichen und am Aufstieg.

## Fußwege zu Pflicht-Markern
Sehenswürdigkeiten, die ein Reittier verlangten, bekommen einen Weg: Bohlenweg (Insel der Irrlichter), Felsrampe (Adlerhorst), Steinbrücke (Verlassene Esse). Prüfung per Flutfüllung; nur bestandene Wege bleiben im Plan.

## Validator (`WorldValidator`)
Fehler: Pflicht-Marker mit Tor, > 50 % der Aussichtspunkte/Herausforderungen hinter Toren, Tor außerhalb des Bands ohne Rückkehr-Markierung, kein/zu kleines/zu großes/unsichtbares Haupt-Wahrzeichen, Haupt-Wahrzeichen ohne Innenraum, zwei ununterscheidbare Wahrzeichen, Hürde ohne Rettungsfenster, Aufstieg nicht 3-stufig/ohne Kontrollpunkt/falsche Schlüssel, Geheimnis im Marker-Radius, Abkürzung < 90 s, Mindestmengen (1 Aufstieg, 1 Abkürzung, 6 Geheimnisse, 3 Lore, 5 Schleifen, 3 Szenen). Kompakte Zonen (Startzone 200 m, Städte) melden fehlende Abkürzungen als Hinweis.

## QA
- Galerie: `Scenes/QA/PT_LandmarkGallery.unity` – alle 11 Typen, alle 10 Hürden, ein Aufstieg.
- Admin (F9): `Welt W1: /landmark tp (Name | haupt | Nr.)`, `/landmark list`, `/hazard show` (Lichtsäulen über den sechs nächsten Hürden), `/climb stage N`.
- Tests: `WorldW1Tests` (rein), `WorldW1ZoneTests` (gespeicherte Pläne aller 30 Zonen).
