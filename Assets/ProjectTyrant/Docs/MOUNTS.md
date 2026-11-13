# Reittiere (Stand Lauf 10)

## Übersicht
| Reittier | Freischaltung | Boden | Fähigkeit (Leertaste) | Absitz-Angriff (1) |
|---|---|---|---|---|
| Raptor | Stallmeister Sonnfurt (Herz + 2 Gold) | 11,5 m/s | weiter Satz ≈ 15 m | Schwanzfeger: Verkrüppelung |
| Skimmer | Stallmeister Nebelmoor (Herz + 4 Gold) | 10 m/s, Wasser 12,5 m/s | Hüpfer, schwebt über Wasser | Wellenschlag: 5 Verwundbarkeit |
| Springer | Stallmeisterin Frostkamm (Herz + 6 Gold) | 9,5 m/s | Ladesprung 3–15 m | Stampfer: 1,5 s Betäubung |
| Greif | Persönliche Geschichte, Kapitel 9 | 10,5 m/s | Abheben/Flügelschlag, Sturzflug | Sturzangriff: Schaden × (0,6 + 0,9 × Sturz-Tempo), Betäubung, Verwundbarkeit |
| Himmelsschuppe | Sammlung „Himmelsschuppe“ (Himmelsspitzen) | 9,1 m/s | senkrecht steigen, schweben, Wandhaften | Feuerstoß: Fläche 6 m, 3 Brennen, Trotz 150 (0,5 s Betäubung) |

## Bedienung
- **X antippen** (< 0,35 s): auf-/absitzen – Standard-Reittier oder (Einstellung) das zuletzt genutzte.
- **X halten** (≥ 0,35 s; Mindestwert der Regel: 250 ms): Radialmenü, Loslassen über einem Symbol sitzt auf. Ob das Radial den Standard setzt, ist einstellbar.
- **Direkttasten** je Reittier im Reittier-Tab (H → Reittiere): F6, F7, F8, N, J, K, L, U, O, P (reservierte Tasten wie X, V, Leertaste, 1–0, F9 sind gesperrt, Doppelbelegung wird abgelehnt).
- **Direktwechsel aufgesessen**: am Boden immer; in der Luft nur Greif ↔ Himmelsschuppe mit „Vertrauensband“ (Höhe und Tempo bleiben).
- HUD: Symbol des X-Reittiers links neben der Fertigkeitenleiste (hell = aufgesessen); über der Reittier-HP-Leiste Auftrieb bzw. Flugausdauer, Tempo als Vielfaches des Lauftempos und Höhe über Boden.
- Alle Einstellungen liegen auf **Account-Ebene** (`AccountDto.MountPrefs`), gelten also für alle Charaktere.

## Flug (FlightMotor)
Kinematisch und deterministisch (fester Zeitschritt in den Tests, keine Rigidbody-Physik). Der `PlayerController` erhält Fluggeschwindigkeit (`FlightVelocity`) und Steigrate, die Schwerkraft ist im Flug aus. Lauftempo 7 m/s.

### Greif
| Wert | Rang 0 | mit Beherrschung |
|---|---|---|
| Auftrieb-Ladungen | 3 (am Boden 1 je 0,5 s) | 5 (Flügelkraft) |
| Gleiten | 1,6 × Lauf, Gleitverhältnis 6 : 1 (aus 100 m ≈ 600 m, ≈ 54 s) | – |
| W / S | Nase runter (bis 2 × Lauf) / Nase hoch (bis 1,2 × Lauf) | – |
| Sturzflug (V halten) | kippt in ≈ 0,7 s fließend auf steile 80° (Greif neigt sich mit), die Vorwärtsfahrt läuft dabei weich aus (5 m/s²) statt abrupt zu enden, beschleunigt mit 12 m/s² bis 3 × Lauf; Leertaste im Sturz (frühestens alle 1 s, kostet 1 Ausweich-Balken): 4 × bzw. 5 × Lauf | Beschleunigung 16 m/s² (Sturzflug-Meister) |
| V loslassen | abfangen, kein automatisches Hochziehen, danach leichtes Sinken (1 m/s); Höchstmarke = Höhe beim Loslassen + Rückgewinn wird gemerkt, S steigt nie darüber; das Sturz-Tempo bleibt als Sturz-Energie (= gestürzte Höhe), Verlust 5 %/s | 2 %/s (Sturzflug-Meister) |
| S mit Sturz-Energie | steiler Aufschwung (50°), verbraucht Energie 1 m je Meter; Rückgewinn nach erreichtem Sturz-Tempo: 3 × = 90 % (knapp unter der Ausgangshöhe), 4 × = 100 % (genau), 5 × = 110 % (etwas darüber); das Tempo sinkt dabei auf Gleittempo | – |
| HUD | Tempoleiste über der Fertigkeitenleiste (0–5 × Lauf, Marken 3×/4×/5×, Farbe je Stufe) mit „zurück: … m“ im Sturz bzw. „Reserve: … m“ im Energieflug | – |
| Strömungsabriss | unter 0,6 × Lauf: sackt ab, bis wieder 0,72 × | – |
| Aufwinde | wirkungslos | heben und füllen den Auftrieb (Aufwindflug) |
| Luftakrobatik | – | 1 Auftrieb-Ladung je 4 s Gleiten |
Loslassen von V: weicher Abfangbogen statt Sprung auf waagrecht – der Sturzwinkel läuft mit 240°/s (steil) bis 110°/s (kurz vor waagrecht) zurück, aus 80° in ≈ 0,5 s; das Tempo bleibt, die ≈ 5 m Bogen-Höhe kommen als Sturz-Energie zurück. S während des Bogens geht mit 260°/s direkt in den 50°-Aufschwung über; erneut V taucht vom aktuellen Winkel aus wieder ab (Spieler-Wunsch 2026-11-12).

Lenkung: der Greif folgt der Kamera (A/D ± 70°) mit 10 rad/s (Kehrtwende ≈ 0,3 s); das ganze Tempo wird beim Richtungswechsel sofort mit umgelenkt, es treibt nichts in die alte Richtung weiter (`GriffonSteer`, Spieler-Wunsch 2026-11-12). Der Körper dreht mit bis zu 720°/s nach.

### Himmelsschuppe
| Wert | Rang 0 | mit Beherrschung |
|---|---|---|
| Flugausdauer | 100 | 150 (Flugkondition) |
| Steigen (Leertaste halten) | 20/s, 5 m/s | – |
| Schweben (loslassen) | 5/s, Höhe bleibt | 3/s (Flugkondition) |
| Vorwärtsflug (W, frei in alle Richtungen) | festes Tempo 9,1 m/s – am Boden wie in der Luft, in jedem Zustand (Schweben, Steigen, Sinken, erschöpft), ohne Tempo-Effekte (Eile, Verlangsamung, Hürden, Beherrschung); nur am Fels haftend 0 und der Sprint (V) als eigene Fähigkeit (seit 2026-11-12) | – |
| Sprint (V drücken, nur in der Luft) | 4,5 × Lauf waagrecht für 0,3 s (≈ 9,5 m), kostet 1 Ausweich-Balken + 6 Flugkraft, 0,9 s Abklingzeit; danach bleibt ~1,8 × Lauf als Schwung | – |
| Boden | +25/s | – |
| Wandhaften | – | an Wänden ≥ 60°: +15/s, höchstens 20 s (Wandhaften) |
| Aufwinde | wirkungslos | füllen die Ausdauer (Aufwind-Segeln) |
| Vertrauensband | – | +5/s im Flug, Luftwechsel Greif ↔ Himmelsschuppe |
Leer: sanftes Absinken mit 1,5 m/s, kein Absturz.

### Flugregeln (FlightRules)
- Höhendecke **über Boden**: 150 m, Himmelsspitzen 400 m, Städte/Hubs 80 m; darüber sanfter Druck nach unten.
- Flugverbot (sanftes Absinken mit 4 m/s, kein Abheben) – Reihenfolge des angezeigten Grundes: Zwischensequenz → Story-Instanz → Tyrannenfeste bis zum Ende der Geschichte → unter einem Dach (Strahl nach oben 10 m) → laufender Champion-/Bosskampf im Umkreis von 40 m.
- Prüfung alle 0,25 s am Spieler.

### Erreichbarkeit (FlightReach)
Zwei-Wege-Regel für erhöhte Ziele: Greif über Startpunkt bzw. Aufwind-Oberkante minus Gleitverlust (Strecke / 6), Himmelsschuppe über Steighöhe je Tank (Ausdauer / Kosten × 5 m/s = 25 m, mit Flugkondition 37,5 m) × (Rasten + 1). Der Builder meldet das Ergebnis für das Himmelsschuppen-Ei.

## Himmelsschuppen-Sammlung (Himmelsspitzen)
1. **Ei**: im Horst auf dem höchsten Gipfel (Builder sucht das Raster-Maximum); zwei Aufwinde am Fuß tragen den Greif hinauf.
2. **Ausbrüten**: fünf Glutsteine auf einem 120-m-Ring um die Brutpflegerin (erst nach dem Ei sichtbar), dann zu ihr bringen.
3. **Sattel**: bei Brutpflegerin Ilva für 3 Gold → Himmelsschuppe freigeschaltet.
Fortschritt in `ProfileState.Completed` (`sky_egg`, `sky_stone_0–4`, `sky_hatch`, `sky_saddle`).

## Beherrschungen
| Linie | Rang 1 | Rang 2 | Rang 3 | Rang 4 |
|---|---|---|---|---|
| Greif | Flügelkraft (6 000 EP, 1 P) | Aufwindflug (12 000, 1) | Sturzflug-Meister (18 000, 2) | Luftakrobatik (24 000, 2) |
| Himmelsschuppe | Wandhaften (8 000, 1) | Flugkondition (14 000, 2) | Aufwind-Segeln (18 000, 2) | Vertrauensband (26 000, 3) |

## Schwung (nach Lauf 11)
- Aufsitzen, Abheben (Leertaste) und Abgehen von Kanten übernehmen das aktuelle Bodentempo und die Laufrichtung; die Himmelsschuppe steigt mit dem Tempo weiter, statt in der Luft stehen zu bleiben. Der Greif startet mit mindestens Gleittempo oder schneller, wenn er schneller lief.
- Luftwechsel Greif ↔ Himmelsschuppe übernimmt Tempo und Höhe wie bisher.
- **C halten (Himmelsschuppe in der Luft): Sinken** mit 6 m/s, lädt dabei 20 Flugausdauer/s (auch aus 0); W hält etwas Vorwärtsfahrt. Solange die Himmelsschuppe fliegt, schaltet C nicht die Action-Kamera um.
- **Aufsitzen in der Luft**: Greif und Himmelsschuppe lassen sich im Sprung, Fall oder beim Gleiten sofort rufen (X, Radial oder Direkttaste) – ohne Aufsitz-Kanal, Schwung bleibt, der Fall wird abgefangen (kein Fallschaden). Andere Reittiere: Hinweis „In der Luft nur Greif oder Himmelsschuppe“. Flugverbote und die Kampfregel gelten weiter.

## Greif: Flügelschläge über die Ausweich-Leiste (nach Lauf 11)
- Auf dem Greif hat die Ausweich-Leiste 3 Balken (sonst 2); Regeneration unverändert (10/s).
- Jeder Flügelschlag in der Luft (Leertaste) und jeder Flügelschlag im Sturz kostet 1 Balken; Abheben vom Boden ist frei. Aufwinde und Luftakrobatik füllen Balken auf.
- Die frühere Anzeige „Auftrieb x/y“ entfällt; die Tempoleiste bleibt.

## Himmelsschuppe: Flugkraft (nach Lauf 11)
- Eigene Leiste (hellblau) direkt über der Ausweich-Leiste; ersetzt die Textanzeige „Flugausdauer“.
- Leertaste halten = steigen (25/s → 20 m je Tank, 30 m mit Flugkondition). Bewegung in eine Richtung (W, seitlich, Treiben) kostet 8/s (5/s mit Flugkondition), der Sprint zusätzlich 6.
- Leer = erschöpft: Leertaste oder Bewegung lässt sie sinken (1,5 m/s), bis wieder 30 % geladen sind (Boden, Wand, C-Sinken, Aufwind; Vertrauensband nur im Stillstand). Gedacht: steigen – auf Klippe/Wand rasten – weiter steigen.
- Stillstehen in der Luft kostet nichts, die Himmelsschuppe sinkt dabei leicht (0,5 m/s). Leer: Stillstehen geht weiter, Bewegung oder Steigen lässt sie sinken.
- Am Boden lädt die Flugkraft (25/s), außerdem C-Sinken (20/s), Wandhaften, Aufwinde und Vertrauensband.
- **Freie Richtung in der Luft:** W/S/A/D (und Q/E) bewegen die Himmelsschuppe relativ zur Kamera vor, zurück und seitlich – sie schaut in Kamerarichtung. Richtungswechsel und Abbremsen sind schnell (28 m/s²: volle Umkehr < 0,8 s, Stopp ≈ 0,35 s); weiter in die gleiche Richtung bleibt mitgebrachter Schwung (Aufsitzen, Abheben, Sprint) erhalten und klingt nur mit 3 m/s² ab. Der Sprint (V) geht in die gedrückte Richtung, sonst nach vorn.

## Welt-Lauf W1 – Reittiere als Schlüssel
Hürden und Tore lesen `Traversal`-Flags (Raptor, Springer, Skimmer, Greif, Himmelsschuppe, Dünenläufer, Boot, Jade-Bot, Verderbnis-Widerstand, Lava-Skimmer). Verfügbar ab Stufe: Raptor 1, Skimmer 15, Springer 25, Lava-Skimmer 35, Greif/Himmelsschuppe 75, übrige 80 (`GateRules.AvailableFromLevel`). Aufgesessen schützt das Reittier vor seiner Hürde (`HazardRules.Protected`): Skimmer über Gift-/Eiswasser und Treibsand, Lava-Skimmer über Lava, Flug über alles außer Sandportalen. Aufstiege: Stufe 2 mit dem Springer (Vorsprünge ≤ 13 m), Stufe 3 mit Greif (Startturm + Aufwind) oder Himmelsschuppe. Siehe LANDMARKS.md.
