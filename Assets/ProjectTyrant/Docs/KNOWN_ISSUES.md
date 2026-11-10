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
| 30 | Rüstungsgewicht (leicht/mittel/schwer) ist je Klasse hinterlegt, beschränkt aber noch keine Rüstungsteile – Rüstung ist klassenübergreifend. | offen (M27) |
| 31 | Klassen-Silhouetten unterscheiden sich nur über Farben und Waffen, nicht über Rüstungsformen. | offen (M27, Meshy) |
| 32 | Klassen-Duell (ohne Spezialisierungen): Schattenklinge 125 DPS und Elementarweber 223 DPS liegen deutlich unter Wächter (396) und Wildhüter (435); Ziel ±15 %. | offen → M27 Balancing |
| 33 | Ein Debug-Klassentest vor M25-Abschluss hatte Klasse/Waffen des migrierten Wächters überschrieben; aus `save.json` wiederhergestellt, Debug-Wechsel speichern seitdem nicht. | behoben (M25) |
| 34 | Klassen-/Spezialisierungs-Schaden streut stark (Duell, gleiches Gear: Sturmrufer 327 bis Dornenwächter 1357 DPS; Wildhüter-Schaden kommt großteils vom Gefährten). Ziel ±15 % zum Wächter. | offen → M27 Balancing |
| 35 | Zauber-Posen der Spezialisierungen nutzen die vorhandenen Clips (Cast1H/Cast2H/CastChannel/Schuss je Waffenstil) – keine eigenen Clips je Spezialisierung. | bewusst (Lauf 5) |
| 36 | Glutbinder-„Elementar-Diener“ sind ortsfeste Felder mit Treffer-Takt, keine beweglichen beschworenen Begleiter. | bewusst (Lauf 5) |
| 37 | Haupt- und Nebenhand sind keine getrennten Ausrüstungsplätze: Elite-Waffen kommen als ganze Sets (z. B. Zepter & Kriegshorn); beim Elementarweber übernehmen sie die Zepter-Plätze 1–3. | bewusst (Lauf 5, ggf. Lauf 8) |
| 38 | Duell-Bot: in Einzelläufen entstand ein Gegner an unerreichbarer Stelle (DPS 0, kein Schaden erlitten); Wiederholung lieferte normale Werte. Stellungskorrektur über Sichtlinie ist eingebaut, ein Gegner-Neuplatzieren noch nicht. | offen (QA-Werkzeug) |
