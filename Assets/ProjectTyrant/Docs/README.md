# Project Tyrant – GW2-artiger Prototyp (Lauf 4: Offene Welt, Reittiere, Beherrschungen)

**Start:** `Assets/ProjectTyrant/Scenes/PT_Core.unity` öffnen → Play. Die Core-Szene lädt die Zone aus dem Speicherstand (sonst die Startzone) additiv. Zonen-Szenen liegen unter `Scenes/Zones/`; ist eine schon geöffnet, wird sie übernommen.
**Neu bauen:** Menü **Project Tyrant → Rebuild Everything** (≈ 85 s) – Icons, Daten, Gegner/Tiere, Reittiere, Beherrschungen, Zonen-Definitionen, 4 Zonen-Szenen (Gelände, Vegetation, Inhalte, Ereignisse, NavMesh, Karte), Core-Szene, Build-Einstellungen. Einzelne Zone: **Project Tyrant → Rebuild Zone → <Name>**; **Build Core Scene**.
**Tests:** Test Runner → EditMode → `ProjectTyrant.Tests` (126 Tests, ~3 s).
**Build-Check:** Windows64 nach `Temp/PT_BuildCheck/ProjectTyrant.exe` (außerhalb von Assets, siehe PROGRESS).

## Steuerung
| Taste | Aktion |
|---|---|
| WASD / Q E | Laufen / Strafen |
| Leertaste | Springen (1,2 m, Fallschaden ab 8 m) · in der Luft: Gleiter (ab „Gleiten 1“) · beritten: Reittier-Fähigkeit (Raptor-Sprung, Springer halten = laden, Skimmer-Hüpfer) |
| X | antippen: auf-/absitzen (1 s) · halten: Reittier-Radialmenü (Maus wählt, loslassen sitzt auf) |
| V oder Doppeltipp Richtung | Ausweichrolle (i-Frames, Ausdauer) |
| Rechtsklick halten | Kamera drehen · Mausrad = Zoom (über der Minimap: Minimap-Zoom) |
| C | Action-Kamera an/aus |
| Tab / Linksklick / Esc | Ziel wechseln / anklicken / Esc: oberstes Fenster schließen, sonst Zielkreis abbrechen, sonst Ziel löschen |
| 1–5 · 6 · 7–9 · 0 | Waffen-Skills · Heilung · Hilfe · Elite · beritten: 1 = Absitz-Angriff, V = Reittier-Ausweichen |
| ^ (~) oder Y | Waffenwechsel |
| F | Interagieren: Aufheben, Kommunizieren, Sprechen, Reisen, Reparieren (Prompt unten mittig, Kanäle mit Balken) |
| I | Inventar · H Held (Ausrüstung · Fertigkeiten · Eigenschaften · Fortschritt · Reittiere · Beherrschungen) · M Weltkarte (weiter herauszoomen = Kontinent) |
| + / − | Minimap-Zoom (5 Stufen) |
| Alt + Ziehen | Stapel teilen · Shift (gehalten) = Item-Vergleich · Rechtsklick = Kontextmenü · Doppelklick = benutzen/anlegen |
| F1–F4 | Klassenmechanik: Wildhüter Gefährte (Angriff, Familien-Skill, passiv, Tausch) · Elementarweber Einstimmung Feuer/Wasser/Luft/Erde · Schattenklinge F1 Stehlen / gestohlenen Skill einsetzen |
| F5 | Elite-Mechanik der aktiven Spezialisierung (Hainform, Verschmelzen, Entfesseln, Markieren, Überladen, Elementarkern, Erdbruch, Knien, Ausweich-Skill wählen, Schattenschleier, Rückruf, Urteil, Festung, Sturmzorn, Dornenhaut) |
| Esc | Spielmenü (Fortsetzen, Optionen, Charakterauswahl, Beenden) |
| F9 | Debug-Panel (Editor/Development Build) · F10 Hilfe |

## Charakterauswahl
- Startszene `PT_CharacterSelect`: ↑/↓ (←/→) wählt einen der 5 Plätze, **Spielen** lädt den Charakter, **Erstellen** (Klasse, Name, Farben, Signatur-Vorschau), **Löschen** (Name eintippen). Geldbörse, Reittiere, Beherrschungen und Optionen gelten für den ganzen Account.
- Klassen: **Wächter** (schwer, Großschwert/Stab), **Wildhüter** (mittel, Gefährte; Langbogen, Axt+Fackel, Schwert+Kriegshorn), **Elementarweber** (leicht, Einstimmungen; Stab, Zepter+Fokus, Dolch+Dolch), **Schattenklinge** (mittel, 12 Initiative, Stehlen, Heimlichkeit; Dolch+Dolch, Schwert+Pistole, Kurzbogen).

## Elite-Spezialisierungen
- Ab Stufe 30: Held (H) → Eigenschaften → Spalte 3, Reiter der Spezialisierung → 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp) → „Aktivieren“ (nur außerhalb des Kampfes; belegt Platz 3). Nur eine gleichzeitig.
- Wildhüter: Hainwächter (Stab), Bestienbund (Dolch & Horn), Dornenwächter (Hammer), Sturmfalke (Gewehr) · Elementarweber: Sturmrufer (Zepter & Horn), Weber (Degen & Fokus), Katalysator (Hammer), Glutbinder (Zepter & Dolch) · Schattenklinge: Wegelagerer (Gewehr), Phantomtänzer (Stab), Schattenhüter (Zepter & Pistole), Klingenwirbel (Axt & Dolch) · Wächter: Glutrichter (Langbogen), Bollwerkmeister (Schwert & Schild), Sturmherold (Hammer), Dornenschild (Zepter & Fackel).
- Schnelltest: F9 → „Elite aktivieren (Id)“, z. B. `hainwaechter` (setzt Stufe 30, schaltet frei, legt die Waffe an).

## Fenster
- **Inventar (I):** Rucksack 20 + 4 Taschenplätze, Ziehen/Tauschen/Zusammenführen, Aufräumen, Plunder verkaufen, Geldbörse (g/s/k) und Karma.
- **Held (H):** Ausrüstung (14 Slots + Attribute), Fertigkeiten (Leiste + Pool, Freischalten mit Heldenpunkten), Eigenschaften (3 Spezialisierungen × 3 Ränge), Fortschritt. Im Kampf gesperrt.
- **Weltkarte (M):** Pan/Zoom, Marker-Tooltips, Legende, Zonenabschluss, Klick auf Wegmarke = Schnellreise. Nach „Besiegt“: Wegmarke wählen.
- **Händler:** Kaufen · Verkaufen (auch Ziehen) · Rückkauf. Karma-Händler nach erfüllten Herzen; Karawanenhändlerin nach der Eskorte.
- **HUD:** Leiste mit HP-Kugel (Stufe), EP-Leiste, Segen/Zustände, Minimap unten rechts (Kompass, Rotation, Zoom), Tracker (Ereignisse + Herzen), Belohnungs-Pop-up.

## In 2 Minuten ausprobieren
1. **Items:** Plünderer töten → Beutesack → F → I → Item doppelklicken (anlegen). Händler Hella/Ottfried im Hub.
2. **Skills:** H → Fertigkeiten: Platz anklicken, Fertigkeit freischalten/legen · Eigenschaften: Rang freischalten, große Eigenschaft wählen (F9 → „+HP“).
3. **Karte:** M → Wegmarke anklicken → reisen. Minimap: Mausrad, „Nord/Kamera“.
4. **Zonenabschluss:** Wegmarken entdecken (10 m), Aussichtspunkt-Treppen hochsteigen → F, Schreine (5 s Kommunion), Herz-NPCs (Aufgaben + Kills), 100 % → Truhe (F9 → „Zone: alles abschließen“).
5. **Ereignisse:** Dornwacht betreten (Kette), Karawane in Sonnfurt, Vorräte in Gerstenbrunn, Elmhain (oder mit Archivar Bertold sprechen), Alpha alle 8–12 min. F9 → „Event starten (Id)“: `dornwacht_defend`, `caravan`, `supplies`, `elmhain_capture`, `alpha`, `sonnfurt_defense`.

## Offene Welt (Lauf 4)
- **Zonen:** Grenzmark Ashveil (1–15) → Nebelmoor (15–25) → Frostkamm (25–30) → Glutöde (30), Portale an den Zonenrändern, Kontinent auf der Weltkarte. Details: WORLD.md.
- **Reittiere:** Raptor (Stallmeisterin Hedda, Sonnfurt, 2 Gold, Herz „Bäuerin Wilma“), Skimmer (Nebelmoor, 4 Gold, Herz „Fährfrau Merle“), Springer (Frostkamm, 6 Gold, Herz „Bergführerin Runa“).
- **Beherrschungen (H → Beherrschungen):** ab Stufe 30 fließen EP in die gewählte Linie; Punkte aus Einblicken, Zonenabschluss, Weltboss/Meta, Erfolgen.
- **Weltboss Frostschlund** alle 15 min, **Meta „Sturm über der Glutöde“** alle 20 min; Timer auf der Kontinentkarte.

## Speicherstand
JSON unter `Application.persistentDataPath/ProjectTyrant/save.json` (DTOs, atomar). Autosave alle 60 s, bei Stufenaufstieg, Zonenabschluss, Reise und Fensteränderungen; Laden beim Szenenstart. Version 2 (Lauf-3-Stände werden migriert). Enthält aktuelle Zone, Nebel je Zone, Reittiere/Auswahl, Beherrschungen (EP, Punkte, Ränge, Auswahl), Stufe/EP, Heldenpunkte, Fertigkeiten/Eigenschaften, Leiste, Inventar/Taschen/Ausrüstung/Nahrung, Geldbörse/Karma, Zonenabschluss, Herzen, Truhe, Nebel, Position.

## Debug-Panel (F9)
Stufe setzen, +EP, +Heldenpunkte, +Gold, +Karma, Gott-Modus, Effekt-Tests, Speichern/Laden/Neues Spiel/Löschen, Item geben (Name/Id), Champion-Beutel, Build zurückerstatten, Zone abschließen/Stand, Event starten/Status. Lauf 4: Zone wechseln (Id), nächste Zone, Level-Cap setzen, Reittier freischalten, Beherrschungspunkte/-EP, alle Beherrschungen, letzte Ladezeit. Lauf 5: Klasse wechseln (Id, Testmodus ohne Speichern), alle Skills freischalten, Klasse: Status, Duell (alle Klassen je 180 s gegen Veteranen, Bericht im Log), Elite aktivieren (Id) / ablegen.

## Architektur
- **Daten (ScriptableObjects, `Data/`):** Skills, Effekte, Items + Datenbank, Beutetabellen, Händler, Spezialisierungen/Eigenschaften, Ereignisse, Zonen-Objekte, Gegner-Archetypen, Combo-Tabelle.
- **Reine Modelle (getestet):** `AttributeSet`/`DamageFormula`, `EffectModel`, `ProgressionModel`, `InventoryModel`/`EquipmentModel`/`LootRoller`/`VendorModel`/`WalletModel`, `SkillLoadoutModel`/`TraitModel`/Modifikatoren, `MapProjection`/`FogModel`, `ZoneRules`/`ZoneContent`, `EventScheduler`/`EventRunner`.
- **Singletons:** `GameManager` (+ Zonenwechsel), `InputManager`, `EventDirector` (Host der Ereignis-Engine, bindet je Zone), `UIController`, `PlayerProfile` (Fortschritt, Inventar, Reittiere, Beherrschungen, Speichern). Zonen-Objekt `ZoneRoot`, Spieler-Komponenten `MountController`, `MasteryAbilities`. Kommunikation über `GameEventBus`.
- **Lauf-4-Modelle (getestet):** `WorldModel`, `BiomeTerrain`, `ReachabilityModel`, `MountMotor`/`MountShop`/`MountInput`, `MasteryModel`/`MasteryEffects`, `DefianceModel`, `WorldClock`/`EventRules`.
- **UI:** uGUI + TMP komplett im Code (`UiWindow`, `WindowManager`, Tooltips, Dialoge), Icons prozedural (`IconPainter`).
- **Editor-Builder:** `DataBuilder`, `ItemBuilder`, `TraitBuilder`, `EventDefinitionBuilder`, `ZoneBuilder`, `ZoneContentBuilder`, `MapBaker`, `IconPainter`, `AudioSynth`, `MeshyIntegrator`; Lauf 4: `WorldBuilder`, `BiomeZoneBuilder` (+ Content), `WorldEnemyBuilder`, `CreatureBuilder`, `MountBuilder`, `MasteryBuilder`, `ZoneEventBuilder`.

Weitere Doku: WORLD · DECISIONS · PROGRESS · KNOWN_ISSUES · GW2_ABGLEICH · ASSET_QUEUE · PLACEHOLDERS.
