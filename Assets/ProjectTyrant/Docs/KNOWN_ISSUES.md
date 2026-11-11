# KNOWN ISSUES – Project Tyrant

1. ~~Input-Backend erst nach Editor-Neustart aktiv~~ – Editor am 2026-11-09 08:03 neu gestartet, New Input System liefert Keyboard/Mouse. Active Input Handling = "Both"; Unity übernimmt das beim nächsten Start. Bis dahin liefert der `InputManager` automatisch Legacy-Input mit identischer Belegung. → Unity einmal neu starten.
2. ~~Meshy-Jobs gehen bei Domain-Reload verloren~~ (gelöst durch Kompilier-Pause; Regel beachten) (Status meldet dauerhaft „Unity is reloading“). Workaround: während laufender Generierung nicht kompilieren. Meshy wurde in diesem Lauf deshalb gesperrt.
3. **Meshy-Modelle sind statisch (kein Rig).** ~~Ausrichtung ungeprüft~~ – Lauf 3 M8: per Capture geprüft, Front stimmt; Großschwert-Flip automatisch korrigiert. Gilt für Spieler, Gegner und Champion. Bewegung wird prozedural animiert (Wippen, Roll-Rotation, Liegen im Downed State). Ausrichtung (Vorderseite) des Modells wurde nicht visuell geprüft (keine Screenshots erlaubt) – ggf. `Visual/MeshyBody` um 180° drehen.
4. ~~Gegner ohne NavMesh~~ → NavMesh-Pfade. Rest-Risiko: Separation kann Gegner kurzzeitig vom Pfad schieben.
5. ~~Kein visueller QA-Durchgang~~ – Lauf 3 erlaubt 2 Captures je Milestone (Temp/PT_QA). Hinweis: `manage_camera screenshot` rendert bei unfokussiertem Game-View über die Kamera, Overlay-UI erscheint dann nicht im Bild → UI wird per Code verifiziert. Farben, Lesbarkeit und Kameraabstände sind per Code verifiziert, nicht per Bild. Feintuning erfolgt am besten in der Szene.
6. ~~Ground-Target ohne Vorschau~~ → Zielkreis vorhanden.
7. **Event-Kette:** Wellen-Gegner, die beim Stufenwechsel noch leben, zählen in Stufe 2b als Besatzer mit (gewollt: Posten ist „überrannt“).
8. **`com.unity.ai.navigation` installiert, aber ungenutzt** (Runtime-Assembly wird unter 6000.6.7f1 nicht kompiliert/geladen). Kann entfernt werden; NavMesh läuft über die Kern-API.
9. ~~Waffen-Ausrichtung der Meshy-Waffen~~ (Lauf 3: automatische Korrektur über Querschnittsprofil) wurde nur rechnerisch (längste Achse → Y) bestimmt; Klinge könnte nach unten zeigen → `WeaponSocket/*/MeshyModel` um 180° drehen.

## Lauf 3
10. **Furcht, Spott, Verlangsamung** werden nur angezeigt (Dauer/Stapel), haben aber noch keine Wirkung.
11. ~~Unity-Warnung „The referenced script (Unknown) on this Behaviour is missing!“** erscheint einmal beim Start des Play Mode; in der Szene findet sich kein Objekt mit fehlendem Skript (vermutlich ein Editor-internes Objekt). Ohne Auswirkung.~~ – M9: Ursache war `InteractionSystem` in einer Sammeldatei; ausgelagert, Warnung weg.
12. **Item-Attribute auf niedriger Stufe sind klein** (z. B. Fein-Helm Stufe 4: +2 Kraft), weil sie wie in GW2 mit der Primärbasis (37 → 1000) skalieren. Spürbar sind vor allem Waffenstärke und Verteidigung; ab Stufe ~10 werden Präfixe deutlich.
13. **Unity-Warnung „Mismatched state in DefaultScenario, was Stopping but expected Idle“** erscheint gelegentlich direkt nach dem Beenden des Play Mode über das MCP-Werkzeug. Stammt aus Unitys Szenario-/Play-Mode-Verwaltung, nicht aus Projektcode; ohne Auswirkung.
14. **Aschkrallen-Alpha** nutzt das Veteranen-Archetyp (Name „Aschkrallen-Alpha“, wandert zwischen 4 Punkten); ein eigenes Modell/Archetyp wäre ein Meshy-/Balancing-Kandidat.
15. **„Wachen-Dialog“ als Meta-Folge** ist nicht als eigener Text umgesetzt; sichtbare Folgen sind Banner, Wachen (tot/zurück), Händlerin, Wegmarke und Besatzer.

| 16 | Waffenwechsel „Y“: Das Input System bindet die physische Taste nach US-Belegung (auf deutscher Tastatur = Z-Taste), der Legacy-Fallback das Zeichen Y. ^ funktioniert in beiden. | offen (Lauf 4) |
| 17 | ~~Reiter sitzt in Standpose auf dem Reittier.~~ M21: Auf-/Reit-Clips (Mount, Ride). | erledigt (Lauf 5) |
| 18 | ~~Meshy-Reittiere und Frostschlund sind statisch.~~ M21: Skinning mit 3 Ansätzen gescheitert (Risse) → geriggte MeshKit-Fassungen, Meshy-Modelle als Statuen. | erledigt (Lauf 5) |
| 19 | Biom-Wetter (`BiomeProfile.Weather`: Pollen, Nebelschwaden, Schnee, Aschewind) und Umgebungsgeräusche je Biom sind als Daten vorhanden, aber noch ohne Partikel/Sound. | offen – Welt-VFX/Audio (Lauf 5) |
| 20 | Weltuhr läuft je Sitzung; Ereignisse laufen nur in der geladenen Zone weiter (Zyklusbeginn während der Abwesenheit verfällt bis zum nächsten Zyklus). | bewusst (Lauf 4) |
| 21 | Speicherbedarf je Zone nur im Editor gemessen (Editor gesamt 1,76–1,99 GB, Zuwachs je Zone 70–90 MB); im Build nicht gemessen (Build läuft nicht in der Linux-Shell). | offen |
| 22 | Meshy-Modelle (8) lassen sich nicht rissfrei auf das prozedurale Skelett skinnen (3 Ansätze, Messwerte in `Data/Animation/MeshySkinReport.txt`); Figuren nutzen geriggte MeshKit-Fassungen, Meshy-Modelle stehen als Statuen. Abhilfe wäre ein Meshy-Modell mit Rig/sauberer Topologie. | Blocker-Regel angewandt (Lauf 5) |
| 23 | Humanoid-Skelett hat 22 statt der genannten 20 Knochen (Aufzählung der Aufgabe ergibt 22). | bewusst (Lauf 5) |
| 24 | Tiere ohne Fuß-IK (nur Humanoide); Vierbeiner-Beine folgen nur den Clips. | offen |
| 25 | Effektdichte-Regler wirkt (VfxPool, PlayerPrefs „PT_EffectDensity“), ist aber erst mit dem Optionsmenü in M24 im Spiel einstellbar. | behoben (M24, Optionen › Grafik) |
| 26 | Keine Lichtstrahlen (God Rays) und keine Decals für Brandspuren – Welt-Lebendigkeit über Partikel, Wind und Wasser. | bewusst (Lauf 5) |
| 27 | Kein Unity-AudioMixer-Asset: Mixer-Gruppen/Snapshots sind ein eigenes Modell (MixerModel) – Unity-Mixer-Effekte (Hall-Send, Kompressor) stehen deshalb nicht zur Verfügung; Hall ist in die Clips gerendert, Höhlen/Ruinen nutzen noch keine AudioReverbZone. | bewusst (Lauf 5) |
| 28 | Steuerung in den Optionen nur als Übersicht, keine Umbelegung (laut Auftrag optional). | bewusst (Lauf 5) |
| 29 | Optionen liegen bis M25 in PlayerPrefs („PT_Options“), nicht im Account-Speicherstand. | behoben (M25, account.json) |
| 30 | Rüstungsgewicht (leicht/mittel/schwer) ist je Klasse hinterlegt, beschränkt aber noch keine Rüstungsteile – Rüstung ist klassenübergreifend. | offen → Lauf 8 (Rüstungssystem) |
| 31 | Klassen-Silhouetten unterscheiden sich nur über Farben und Waffen, nicht über Rüstungsformen. | offen (M27, Meshy) |
| 32 | Klassen-Duell (ohne Spezialisierungen): Schattenklinge 125 DPS und Elementarweber 223 DPS liegen deutlich unter Wächter (396) und Wildhüter (435); Ziel ±15 %. | erledigt (M27 Balancing, siehe 47–50) |
| 33 | Ein Debug-Klassentest vor M25-Abschluss hatte Klasse/Waffen des migrierten Wächters überschrieben; aus `save.json` wiederhergestellt, Debug-Wechsel speichern seitdem nicht. | behoben (M25) |
| 34 | Klassen-/Spezialisierungs-Schaden streut stark (Duell, gleiches Gear: Sturmrufer 327 bis Dornenwächter 1357 DPS; Wildhüter-Schaden kommt großteils vom Gefährten). Ziel ±15 % zum Wächter. | erledigt (M27 Balancing, siehe 47–50) |
| 35 | Zauber-Posen der Spezialisierungen nutzen die vorhandenen Clips (Cast1H/Cast2H/CastChannel/Schuss je Waffenstil) – keine eigenen Clips je Spezialisierung. | bewusst (Lauf 5) |
| 36 | Glutbinder-„Elementar-Diener“ sind ortsfeste Felder mit Treffer-Takt, keine beweglichen beschworenen Begleiter. | bewusst (Lauf 5) |
| 37 | Haupt- und Nebenhand sind keine getrennten Ausrüstungsplätze: Elite-Waffen kommen als ganze Sets (z. B. Zepter & Kriegshorn); beim Elementarweber übernehmen sie die Zepter-Plätze 1–3. | bewusst (Lauf 5, ggf. Lauf 8) |
| 38 | Duell-Bot: in Einzelläufen entstand ein Gegner an unerreichbarer Stelle (DPS 0, kein Schaden erlitten); Wiederholung lieferte normale Werte. Stellungskorrektur über Sichtlinie ist eingebaut, ein Gegner-Neuplatzieren noch nicht. | offen (QA-Werkzeug) |

## Fix-Lauf F1
| 39 | ~~Soft-Lock: nach dem Besiegtwerden in einer Zone ohne freigeschaltete Wegmarke bot die Karte keine Wiederbelebung an.~~ Ankunfts-Wegmarke je Eingang (≤ 40 m, Auto-Freischaltung), Builder-Prüfung, Notfall-Wegmarke, Migration alter Spielstände. | erledigt (F1) |
| 40 | ~~Mausrad-Zoom ohne spürbare Wirkung~~ (Scroll war gebunden, wurde aber als ±1 je Raste normiert und mit 0,0015 multipliziert). Neuer Zoom 1,5–12 m / beritten 2,5–16 m. | erledigt (F1) |
| 41 | Ego-Perspektive bei minimalem Zoom („FirstPersonAtMinZoom“) ist als Option vorbereitet, aber nicht umgesetzt (Kopf-/Waffen-Clipping der Figur, eigene Kamera-Nähe fehlt). | offen |
| 42 | Drehen mit A/D nutzt den Lauf-/Leerlauf-Clip; kein eigener Dreh-Clip (Füße gleiten beim Drehen im Stand). | offen (Animation) |
| 43 | Elementarweber-Auto-Angriffe bleiben einzelne Angriffe (Einstimmungs-Skills wechseln ohnehin); Langbogen bewusst ohne Kette. | bewusst (F1) |
| 44 | Ankunfts-Wegmarken gibt es für die 4 Zonen; Städte (Lauf 7) brauchen dieselbe Prüfung – der Builder-Test deckt neue Zonen automatisch ab. | offen → Lauf 7 |
| 45 | Live-Prüfungen in F1 liefen über QA-Hilfen ohne Fenster-Fokus (`PlayerController.QaMove/QaRmb`, `QaDelayedCapture`, `QaChainProbe`); echte Tastatur-Ereignisse kommen bei unfokussiertem Editor nicht an. | bewusst (QA) |
| 46 | ~~Kein Weltboden: wer neben die Karte gerät (im F1-Dauertest: Duell-Bot stellte in Sonnfurt neben der Karte auf), fiel endlos (y = −37.000).~~ Weltboden y = −150 holt zur ersten Besiegt-Option zurück; Duell-Bot wählt nur Plätze mit Boden. | erledigt (F1) |

## M27
| 47 | Balancing-Messung streut stark: gleiche Konfiguration 329 vs. 722 DPS (Wildhüter) bzw. 85 vs. 775 (Wächter), weil der Duell-Bot gelegentlich ohne Sichtlinie/Reichweite steht; ausgewertet wird das Maximum aus 2 Durchgängen. | bewusst (Messmethode) |
| 48 | Elementarweber (Kern) streut nach Faktor 2,0 im Bot zwischen 0 und 667 DPS (Bestwert −14 % zum Wächter 775): der Bot wirkt nur ~0,6–0,9 Skills/s (Bodenziel- und Kanal-Skills vor dem Auto-Angriff); der Auto-Angriff allein liegt mit 1,27–2,68 Koeffizient/s auf Wächter-Niveau (1,41/s). Faktor bewusst nicht über 2,0 (Test-Grenze), Spieler-Rotation erreicht mehr. | offen → Bot-Rotation je Klasse |
| 49 | Sturmrufer streut 474–718 DPS (Bestwert −7 %): Überladen-Kanal alle 7 s im Bot blockiert die Waffenskills. | beobachten |
| 50 | Dornenwächter nach Faktor 0,5: 855–856 DPS (+10 %); der Gefährte trägt ~40 % und wird nur mit dem Klassenfaktor skaliert – eine weitere Senkung bräuchte einen Gefährten-Faktor je Spezialisierung. | erledigt (M27) |
| 51 | Meshy-Helden (3) wie alle bisherigen 8 Meshy-Figuren nicht rissfrei skinnbar → Statuen; Klassen-Silhouetten weiter nur über Farben und Waffen (KNOWN 31). Rüstungsformen kommen mit Lauf 8. | bewusst |
| 52 | Ladebilder zeigen den Zonennebel stark (Nebelmoor/Glutöde) – gewollt als Biom-Stimmung, aber weniger „Key-Art“ als gemalte Bilder. | bewusst |
| 53 | Herz „Sonnfurt sichern“ (Mira) und Späher sind in M28 nur als Modell/Objekt-Typ vorhanden; die Inhalte für alle bestehenden Zonen folgen in M29 (Schritt K01_S02 ist bis dahin nicht abschließbar). | erledigt (M29) |
| 54 | Story-Instanzen spielen am Ort in der offenen Welt; Häuser, Dorfbewohner und Requisiten der Zone bleiben stehen – „Feuer auf dem Hof“ zeigt noch keinen brennenden Hof. Brand-/Zerstörungs-Requisiten je Instanz kommen mit M29. | erledigt (M29, `StoryProps`) |
| 55 | Gesprächsszene: Figuren stehen frontal mit 50° Drehung, die Kamera ist fest (keine Schnitte zwischen den Sprechern wie im Vorbild). | beobachten |
| 56 | Gesichter der Story-Figuren wirken hell (Hauttöne aus Standard-Werten, Bühnenlicht); Mira nachjustiert, andere Figuren folgen mit den Meshy-/Farb-Durchgängen in M34. | beobachten |
| 57 | QA-Screenshots von `QaDelayedCapture` lagen fest unter `Temp/PT_QA/fixlauf_f1`; jetzt `Folder` (Standard „lauf6“). Ältere Lauf-6-Aufnahmen (m28_*) liegen noch im F1-Ordner. | erledigt |
| 58 | (Ursache siehe Nr. 61: SetActive auf geriggten Figuren) Editor fror am 2026-11-10 05:00 beim Verlassen einer Story-Instanz ein (nach dem Ausblenden auch verbündeter Kämpfer: Karawane, Wachen, Vorräte; vorher TransformStreamHandle-/IndexOutOfRange-Fehler aus Animation Rigging beim Reaktivieren). Ausblenden wieder auf Gegner beschränkt; offene Ereignis-Figuren bleiben in der Instanz sichtbar. Prüfen nach Neustart. | offen |
| 59 | Sprechblasen liegen über offenen Fenstern (Weltkarte); Späher fehlen in der Legende der Weltkarte. | beobachten |
| 60 | Herz-Panel der offenen Welt bleibt in Story-Instanzen sichtbar, wenn ein Herz am selben Ort liegt (Gerstenbrunn/„Feuer auf dem Hof“). | beobachten |
| 61 | Editor-Abstürze am 10.11. (08:30, 12:10, 12:37): **Ursache gefunden** im Editor.log – Burst-Ausnahmen in `RigSyncSceneToStreamJob` („TransformStreamHandle is invalid“, „Index 1 out of range“) direkt nach dem Laden von Nebelmoor, danach „Overflow in memory allocator“ → Absturz. Auslöser: Moorschleicher (Hinterhalt) blendeten ihr geriggtes Visual per `SetActive` aus/ein; ebenso Elmhain-Gegner (Event „befreit“) und Story-Instanzen (Nr. 58). Alle drei Stellen nutzen jetzt `ActorVisibility` (Renderer/Collider/Gehirn statt SetActive); `FindNearestHostile` überspringt nicht anvisierbare Figuren. | behoben (live prüfen) |
| 62 | Balance B1 auf Wunsch abgebrochen (18:39): Nachher-Messung mit Bots fehlt, Zielwerte (Tode, Fluchterfolg, Event-Erfolg je Stufe) unbelegt; Trainingspuppe erfüllt die Zeit-bis-Tod-Ziele aller Stufen, Veteran Stufe 5 9,6 s (Ziel ≥ 10 s). Flucht in Stufe 4–5 vermutlich noch zu leicht (Probe 20/20 vor Fessel-Änderung). | offen |
| 63 | Baseline-Messung (vorher) war vom Bot-Fehler „Ausweichrolle blockiert Bewegung“ betroffen: in vielen Sessions kämpfte der Bot nur gegen Gegner, die zu ihm kamen. Vorher-Zahlen nur grob vergleichbar. | offen |
| 64 | Startzone: Stufe 0 nur 6 Spawns (Ziel-Quote 80/20 erfüllt), Dichte-Regel des Validators nur über die Quote geprüft, nicht über Spawns je Fläche. | beobachten |

65. **Lauf 10 – nicht live per Tastatur geprüft**: Sturzflug (V), Flügelschlag (Leertaste), Wandhaften und Direkttasten sind über Modell-Tests (64) und Code-Aufrufe im Play Mode geprüft, nicht mit echter Tastatureingabe (keine Computer-Bedienung erlaubt). Ebenso Admin-„Story weiter“ mit echten Szenen nur über den Code-Pfad (FinishStep) verifiziert.
66. **Warnung „The referenced script (Unknown) on this Behaviour is missing!“** beim Betreten des Play Mode (Szenen-Backup). Seit der B1-Messreihe, vor Lauf 10; in geladenen Szenen und Prefabs kein Objekt mit fehlendem Skript gefunden (Suche über alle GameObjects). Vermutlich ein Objekt im Szenen-Backup der B1-Bots; offen.

67. **Lauf 11 – Ist-Stand ohne Läufe 7–9**: keine sechs Hauptstädte, keine Völker, kein Autopilot/Abnahme-Matrizen außer den B1-Bots, kein Weltboss-Fahrplan vor Lauf 11. Nachgebaut: Fahrplan-Modell (12 Bosse + 2 Metas) mit Zonen-Timern der neuen Weltbosse; bestehende Bosse behalten ihre Zonen-Timer.
68. **Lauf 11 – Telemetrie je neuer Zone nicht gemessen** (B1-Bots × 20 Zonen × 2 Profile × 30 min = 20 h). Gefahren-/Event-Stufen und Validatoren sind grün, Zielwert-Korridore ungeprüft.
69. **Lauf 11 – Inhaltsumfang gekürzt**: Story 171 Dialogzeilen + 16 Tagebuch-Zeilen + 120 Späher-Zeilen statt ≈ 650; 16 Briefe statt 40; Hubs ohne eigene Stadtarchitektur (Biom-Gelände + Dienste-NPCs, keine Vertikalebenen außer Aufzug-Plattformen), keine Luftspiegelungs-Rätsel als Mechanik (Story nutzt „Lichtsteine“), keine Festival-Events, keine Geisterwege als Sichtbarkeitsmechanik.
70. **Lauf 11 – Meshy-Lauf (14 Modelle), Win64-Build, Performance-Messung je Zone, Ladebildschirme der neuen Zonen (Builder-Aufnahme läuft automatisch), regionale Musik-Themen** offen.
71. **Lauf 11 – Jadegeist-Geräte nur Aufzug**: Brücken, Kanonen, Truhen nicht als Weltobjekte gebaut; Jade-Kanonen nur als Story-Ziel (Interact).
72. **Lauf 11 – Bounty-Eigenschaften vereinfacht**: Spiegelbilder/Orbkranz beschwören die Begleiter des Archetyps, Trotzleisten-Fenster ist nur ein Hinweis; Lösbarkeit über Druck-Budget statt Lauf-9-Simulator (nicht vorhanden).
73. **Lauf 11 – Verwaiste Sprachdateien** `K18_S02E_Q1/C1_*`, `K25_S02_Q2/C2_*` (Wahl-Stellen verschoben); `device_bash` darf nicht löschen, die Dateien werden nicht referenziert und stören nicht.
74. **Lauf 11 – Admin-Abschluss von Gesprächs-Schritten** spielt (wie in Lauf 10) nur die Endszene; Wahl-Flags der Einstiegsszene entstehen dabei nicht. Im echten Gespräch werden sie gesetzt (Modell-Test `StoryPathTests`).
75. **Greif-Beherrschung „Flügelkraft“ (+2 Auftrieb) ohne Wirkung**, seit Flügelschläge die Ausweich-Leiste (fest 3 Balken) nutzen. Vorschlag: +1 Balken oder schnellere Regeneration auf dem Greif – Entscheidung des Auftraggebers.
