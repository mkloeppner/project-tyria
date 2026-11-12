# Admin-Menü (Stand Lauf 10)

Öffnen: **F9** (Debug-Panel, nur im Editor und in Development-Builds). Oben das Parameterfeld, darunter die Befehle; die Admin-Befehle beginnen mit „Admin:“.

## Freischalten
| Befehl | Parameter | Wirkung |
|---|---|---|
| Admin: Reittier freischalten | Name oder Anzeigename (`Skyscale`, `Himmelsschuppe`, `Greif` …) oder `alle` | schaltet frei, Kennzeichnung `mount:<Name>` |
| Admin: Beherrschung meistern | Linie (`Greif`, `Himmelsschuppe`, `Gleiten` …) oder `alle` | ergänzt fehlende Punkte, trainiert alle Ränge, Kennzeichnung `mastery:<Linie>`; laufender Flug übernimmt die neuen Werte |
| Admin: alles freischalten | – | beides für alles |
| Admin: Kennzeichnungen anzeigen | – | Liste aller Admin-Freischaltungen dieses Accounts |
Admin-Freischaltungen sind im Reittier-Tab mit **[Admin]** markiert und werden mit dem Account gespeichert (`AccountDto.AdminMarks`).

## Flug-Debug
`Admin: /flight` mit Parameter:
- `infinite` – Auftrieb und Flugausdauer bleiben voll
- `ceiling off` – keine Höhendecke
- `rules off` – keine Flugverbote
- `show` – Flugzustand, Decke und Verbotsgrund im HUD
- `reset` – alles aus
Jeder Aufruf schaltet um und meldet den Gesamtzustand.

## Geschichte
Alle Story-Befehle laufen über den **echten Abschluss-Ablauf** des Story-Direktors: Endszene, Belohnung, Flags, Briefe, Kapitel-Banner, Verlassen der Instanz; danach Reise zur Zone des nächsten Schritts, wo dessen Startszene wie im Spiel läuft.
| Befehl | Parameter | Wirkung |
|---|---|---|
| Admin: Story weiter | Anzahl Schritte (leer = 1) | schließt Schritte nacheinander ab |
| Admin: Kapitel abschließen | – | bis zum Kapitelende |
| Admin: zu Kapitel springen | `K S` (Schritt ab 1) | setzt Kapitel/Schritt, verlässt Instanzen, reist hin |
| Admin: Schritt zurücksetzen | – | Zähler auf 0, Startszene gilt wieder als ungesehen, Präsenz neu |
| Admin: Instanz-Ziel erfüllen | – | erfüllt das offene Ziel über die normale Meldung (Kontrollpunkte, Folgeziel, Abschluss) |
| Admin: Story-Szenen an/aus | – | Szenen beim Weiter abspielen (Standard) oder überspringen; Wahl-Stellen werden immer gestellt |
| Admin: Story-Auto-Wahl an/aus | – | Wahl-Stellen im Admin-Ablauf mit der ersten Antwort (Standard an) |
| Admin: Story-Reisen an/aus | – | nach dem Weiter zur Zone des nächsten Schritts reisen (Standard an) |
Jeder per Admin abgeschlossene Schritt wird gekennzeichnet (`story:K03_S02`, Sprünge `story:jump:…`, Ziele `story:obj:…`). Die Schalter wirken nur während eines Admin-Ablaufs (`StoryAdmin.Active`), normale Szenen bleiben unberührt.

## Ältere Befehle
„Welt: …“ (Zonen, Level-Cap, Reittiere ohne Kennzeichnung, Beherrschungs-EP), „Story: …“ (Kapitel setzen, Szene abspielen, Flags), „Balance: …“ (Gefahren-Overlay, Messungen) bleiben unverändert.

## Welt-Lauf W1 (Wahrzeichen)
| Befehl | Parameter | Wirkung |
|---|---|---|
| Welt W1: /landmark tp | Name (Teil), `haupt` oder Nummer aus der Liste | Teleport vor das Wahrzeichen |
| Welt W1: /landmark list | – | Wahrzeichen der Zone mit erreichten Stufen |
| Welt W1: /hazard show | – | Lichtsäulen über den sechs nächsten Hürden, Liste mit Entfernung und Schlüssel |
| Welt W1: /climb stage N | 1–3 (leer = 3) | Teleport auf die Stufe des nächsten Aufstiegs |

## Grafik-Lauf G1–G7
| Befehl | Parameter | Wirkung |
|---|---|---|
| Grafik: /cam bookmark | Zone/Name | Kamera auf ein Lesezeichen setzen |
| Grafik: /cam release | – | Kamera wieder dem Spieler geben |
| Grafik: /cam capture all | – | Bilder aller Lesezeichen der Zone |
| Grafik: /gfx show | – | aktuelle Stufe, Features, Wetter, Belichtung |
| Grafik: /gfx preset | Niedrig, Mittel, Hoch, Ultra | Qualitätsstufe wechseln |
| Grafik: /gfx feature | Name on/off (fog, outline, godrays, distortion, weather, ambient …) | einzelnes Feature schalten |
| Grafik: /gfx stress | n (Standard 20) | n Effekte gleichzeitig zum Belastungstest |
| Grafik: /gfx perf | all oder zone, optional Tag | Leistung je Lesezeichen ungedrosselt → `Temp/PT_QA/G1/perf_<Tag>.csv` |
| Grafik: /tod | 0–24, pause, speed x | Tageszeit |
| Grafik: /weather | Zustand oder lock | Wetter setzen/festhalten |
| Grafik: /vfx play | SkillId | Skill-Effekt am Spieler abspielen |
| Grafik: /vfx capture | Tag | Showcase: 28 Skills (7 je Klasse), Champions je Biom, Reittiere |
| Grafik: /vfx loop, /vfx slow | –, Faktor | Effekt-Schleife, Zeitlupe |
