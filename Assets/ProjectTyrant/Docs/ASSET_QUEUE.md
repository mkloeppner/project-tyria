# ASSET QUEUE – Meshy

**Budget-Ermittlung:** `generate_model` meldet Provider `meshy` als konfiguriert, liefert aber weder Kontostand noch Credit-Verbrauch (kein Balance-Endpunkt über die Bridge). → Konservatives Budget: **max. 12 Generierungen**.
Modell `meshy-6`, Format FBX, Textur an.

**Lauf 1 – Status: Meshy GESPERRT** (Regel 5 – Timeout): Jobs #2 und #3 lieferten über >10 Min. nur noch „Unity is reloading; please retry“ (Job-Tracking des Tools hat die Domain-Reloads nicht überlebt). Kein Re-Roll.

**Verbrauch:** 3 Generierungen gestartet (1 geliefert, 2 ohne Ergebnis – ob Meshy dafür Credits berechnet hat, ist über das Tool nicht feststellbar).

| # | Tier | Asset | Prompt | Status | Gen. | Ergebnis |
|---|---|---|---|---|---|---|
| 1 | 1 | Spielercharakter | stylized painterly fantasy hero, armored human warden with blue and gold tabard, short cape, leather boots, game-ready low poly, hand-painted texture, A-pose, full body, no weapon | ✅ geliefert | 1 | `Prefabs/Meshy/PT_Meshy_PlayerHero.fbx` → normalisiert `PT_Meshy_PlayerHero_Normalized.prefab`, im Spieler verbaut |
| 2 | 1 | Champion Grolmok | stylized painterly fantasy brute warlord, huge horned ash-skinned ogre chieftain with bone crown, glowing orange eyes, spiked shoulder armor, red war sash, holding a giant bone club, game-ready low poly, hand-painted texture, full body | ❌ Timeout | 1 | prozedural (Primitive-Komposition) |
| 3 | 1 | Hub-Wahrzeichen | stylized painterly fantasy village beacon tower, tall stone lighthouse-like watchtower with wooden balconies, red tiled conical roof and a glowing golden brazier on top, game-ready low poly, hand-painted texture | ❌ Timeout | 1 | prozedural (Leuchtfeuerturm) |
| 4 | 2 | Trash-Gegner | siehe PLACEHOLDERS.md | ⛔ gesperrt | 0 | prozedural |
| 5 | 2 | Veteran-Gegner | siehe PLACEHOLDERS.md | ⛔ gesperrt | 0 | prozedural |
| 6 | 2 | Waffenset Glutklinge | stylized painterly fantasy greatsword with ember-glowing blade, low poly, hand-painted | ⛔ gesperrt | 0 | prozedural |
| 7 | 2 | Waffenset Sturmruf | stylized painterly fantasy wooden staff with glowing violet storm orb, low poly, hand-painted | ⛔ gesperrt | 0 | prozedural |

**Nächster Lauf:** Jobs einzeln starten und bis zum Abschluss pollen, **ohne** währenddessen Skripte zu kompilieren (Domain-Reload bricht das Tracking ab). Danach `MeshyIntegrator.GetOrBuild(name, höhe)` nutzen.

## Lauf 2 (2026-11-09 07:5x) – Sperre aufgehoben, Ursache behoben (kein Kompilieren während der Jobs)
Verbrauch kumuliert: **9 von 12** Generierungen (Reserve 3).

| # | Tier | Asset | Status | Gen. | Ergebnis |
|---|---|---|---|---|---|
| 8 | 1 | Champion Grolmok | ✅ | 1 | `PT_Meshy_ChampionWarlord` → `PT_Enemy_Champion` |
| 9 | 1 | Hub-Wahrzeichen | ✅ | 1 | `PT_Meshy_HubBeacon` (16 m, BoxCollider) → POI_Hub |
| 10 | 2 | Trash-Gegner | ✅ | 1 | `PT_Meshy_TrashRaider` → `PT_Enemy_Trash` |
| 11 | 2 | Veteran | ✅ | 1 | `PT_Meshy_VeteranBrute` → `PT_Enemy_Veteran` |
| 12 | 2 | Greatsword Glutklinge | ✅ | 1 | `PT_Meshy_Greatsword` (1,5 m, Griff-Pivot) → Spieler & Wachen |
| 13 | 2 | Stab Sturmruf | ✅ | 1 | `PT_Meshy_StormStaff` (1,6 m) → Spieler |

Alle Modelle: Höhe normalisiert (Charaktere 1,8 m lokal; Veteran/Champion werden über den Archetyp-Scale 1,35/2,0 skaliert), Pivot auf Bodenebene, Toon-Material mit Meshy-Albedo, Prefab unter `Prefabs/Meshy/*_Normalized.prefab`.

## Lauf 3 (2026-11-09 16:3x, M14, Kompilier-Pause eingehalten)
Budget Lauf 3: 6 · verbraucht: **3** · kumuliert: **12 von 18**. Alle 3 Jobs parallel gestartet, ~4 min bis Abschluss, kein Timeout, kein Re-Roll.

| # | Tier | Asset | Status | Gen. | Ergebnis |
|---|---|---|---|---|---|
| 14 | 1 | Wegmarken-Monument | ✅ | 1 | `PT_Meshy_WaypointMonument` (2,8 m, Collider) → 5 Wegmarken, Lichtschein bleibt |
| 15 | 1 | Aussichtspunkt-Stele | ✅ | 1 | `PT_Meshy_VistaStele` (2,4 m) → 3 Aussichtspunkte |
| 16 | 1 | Heldenherausforderungs-Schrein | ✅ | 1 | `PT_Meshy_ChallengeShrine` (1,8 m) → 2 Kommunions-Schreine (Kampf-Banner/Relikt bleiben prozedural) |

Ausrichtung per Capture geprüft (`Temp/PT_QA/M14_meshy_waypoint.png`): aufrecht, Sockel unten. Reserve 3 bewusst nicht genutzt (Tier 2: Händler-/Herz-NPC, Karawanenwagen – siehe PLACEHOLDERS).

## Lauf 4 (Budget 8, kumuliert max. 26) – genutzt 6, kumuliert 18
Alle Jobs in M20 parallel gestartet, ohne Kompilieren gepollt (7–9 min), meshy-6, FBX, Textur an, keine Wiederholungen.

| # | Tier | Asset | Status | Versuche | Einbau |
|---|---|---|---|---|---|
| 17 | 1 | Raptor-Reittier | ✅ | 1 | `PT_Meshy_MountRaptor` (2,6 m) → `PT_Mount_Raptor` + Stallmeister-Ausstellung; Wippen statt Beinschwung |
| 18 | 1 | Springer-Reittier | ✅ | 1 | `PT_Meshy_MountSpringer` (2,4 m, Modell um −90° gedreht, sitzende Pose) → `PT_Mount_Springer` |
| 19 | 1 | Skimmer-Reittier | ✅ | 1 | `PT_Meshy_MountSkimmer` (1,3 m, +0,35 m Schwebehöhe) → `PT_Mount_Skimmer` |
| 20 | 1 | Weltboss Frostschlund | ✅ | 1 | `PT_Meshy_Frostschlund` (1,9 m lokal × Archetyp-Scale 4,5) → `PT_WorldBoss_Frostschlund` |
| 21 | 2 | Portal-Bogen | ✅ | 1 | `PT_Meshy_PortalArch` (7,2 m, Collider) → 5 Portale, Lichtschleier und Namensschild bleiben |
| 22 | 2 | Beherrschungs-Einblick | ✅ | 1 | `PT_Meshy_InsightCrystal` (2,2 m) → 9 Einblicke, Licht bleibt |

Nicht genutzt (2): Sturmtyrann und Gegnertypen bleiben prozedural – ohne Rig verlieren sie die Bein-/Arm-Animation des ProceduralLimbAnimator, die bei Kämpfern wichtiger ist als bei Bossen/Reittieren (Rig: Lauf 5).

## Lauf 5 – generate_audio (M24)
Budget 8 Clips (nur Musik-Grundthemen) · **verbraucht 0**. Begründung: Der generative Komponist liefert je Zone, Kampf, Boss und Menü fünf synchrone, nahtlose Stems mit dem Hauptthema; ein generiertes Thema wäre nur als zusätzlicher Melodie-Stem einbindbar gewesen und hätte Tonart-Treue der Stinger und die Stem-Synchronität (Test) nicht garantiert. Alles prozedural über AudioSynth 2.0 (DECISIONS M24).

## Lauf 5 – Meshy (M27, Budget 6, kumuliert max. 32)
Erster Start (6 Jobs, 02:5x) ging verloren: der Eintritt in den Play Mode (Domain-Reload) während der Generierung machte die Jobs unabfragbar („Unity is reloading“ bei jedem Status-Aufruf, kein Ergebnis). Ob Meshy dafür Guthaben abgebucht hat, ist aus Unity nicht sichtbar – vorsorglich als **6 verbraucht** gezählt. Zweiter Start ohne Play Mode/Kompilieren: alle 6 in ~6 min geliefert. **Verbrauch Lauf 5: 12 (6 verloren + 6 geliefert), kumuliert 30 von 32.**

| # | Tier | Asset | Status | Versuche | Einbau |
|---|---|---|---|---|---|
| 23 | 1 | Held Wildhüter (A-Pose) | ✅ | 2 | Skinning 3 Ansätze mit Rissen (max. Streckung 92,5, 706 674 Risskanten) → Statue im Heldenhain; Figur bleibt geriggte MeshKit-Fassung |
| 24 | 1 | Held Elementarweber (A-Pose) | ✅ | 2 | Skinning 3 Ansätze mit Rissen (85,9 / 156 125) → Statue |
| 25 | 1 | Held Schattenklinge (A-Pose) | ✅ | 2 | Skinning 3 Ansätze mit Rissen (99,5 / 129 902) → Statue |
| 26 | 2 | Langbogen (Wildhüter) | ✅ | 2 | `PT_Meshy_Longbow` (1,5 m, Griff in der Mitte) ersetzt den Primitive-Bogen |
| 27 | 2 | Elementarstab | ✅ | 2 | `PT_Meshy_ElementStaff` (1,6 m, Kristall oben) → neue Waffenoptik `ElementStaff` des Elementarwebers |
| 28 | 2 | Schattendolch | ✅ | 2 | `PT_Meshy_ShadowDagger` (0,45 m) → beide Dolche (Schattenklinge, Elementarweber) |

Regel ergänzt: Während Meshy-Jobs laufen, weder kompilieren **noch den Play Mode betreten** (beides lädt die Domain neu).
