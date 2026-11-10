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
| Sturzflug (V halten) | bis 4 × Lauf | bis 4,6 × (Sturzflug-Meister) |
| Abfangen (V loslassen) | 60 % der verlorenen Höhe zurück, Tempo bleibt zu 70 % | 85 % (Sturzflug-Meister) |
| Strömungsabriss | unter 0,6 × Lauf: sackt ab, bis wieder 0,72 × | – |
| Aufwinde | wirkungslos | heben und füllen den Auftrieb (Aufwindflug) |
| Luftakrobatik | – | 1 Auftrieb-Ladung je 4 s Gleiten |
Lenkung: der Greif folgt der Kamera (A/D ± 70°), 1,8 rad/s.

### Himmelsschuppe
| Wert | Rang 0 | mit Beherrschung |
|---|---|---|
| Flugausdauer | 100 | 150 (Flugkondition) |
| Steigen (Leertaste halten) | 20/s, 5 m/s | – |
| Schweben (loslassen) | 5/s, Höhe bleibt | 3/s (Flugkondition) |
| Vorwärtsflug (W) | kostenlos, 1,4 × Lauf, 8 : 1 | – |
| Sturz (V halten) | 3 × Lauf senkrecht | – |
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
