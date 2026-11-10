# CLASSES – Project Tyrant (Lauf 5, Stand M27)

Vier Klassen mit je vier Elite-Spezialisierungen, vollständig aus Daten (`Data/Classes`, `ClassBuilder`). Koeffizienten nach dem M27-Balancing (Faktoren in `ClassBuilder.Balance.cs`, Messung im Abschnitt Balancing). Effekt-Komposition = Auswurf → Einschlag (Schlüssel Form/Palette/Größe, siehe VFX.md); Sound-Familie = Palette der Skill-Klänge (AUDIO.md).

## Übersicht

| Klasse | Rüstung | Mechanik | Ressource | Waffen | Klassenfaktor |
|---|---|---|---|---|---|
| Wächter | Heavy | None |  | Glutklinge, Sturmruf | 1 |
| Wildhüter | Medium | Pet | Tiergefährte | Langbogen, Axt & Fackel, Schwert & Kriegshorn | 1,15 |
| Elementarweber | Light | Attunement | Einstimmung | Elementarstab, Zepter & Fokus, Doppeldolche | 2 |
| Schattenklinge | Medium | Initiative | Initiative | Dolch & Dolch, Schwert & Pistole, Kurzbogen | 1,55 |

## Balancing (M27)

Messung: Duell-Bot im Messmodus (Stufe 30, exotische Waffen der jeweiligen Sets, Gegner mit 5·10⁷ HP, Spieler unverwundbar), 60 s Spielzeit × 3, Bestwert aus 2–3 Durchgängen (Ausreißer nach unten = Sichtlinien-/Reichweitenfehler des Bots). Referenz Wächter 775 DPS, Ziel ±15 % (659–891).

| Klasse/Spezialisierung | Klassenfaktor | Spez.-Faktor | DPS | zum Wächter |
|---|---|---|---|---|
| Wächter | 1,0 | – | 775 | +0 % |
| Wildhüter | 1,15 | – | 752 | -3 % |
| Elementarweber | 2,0 | – | 667 | -14 % |
| Schattenklinge | 1,55 | – | 729 | -6 % |
| Glutrichter | 1,0 | 0,9 | 762 | -2 % |
| Bollwerkmeister | 1,0 | 2,0 | 689 | -11 % |
| Sturmherold | 1,0 | 1,15 | 681 | -12 % |
| Dornenschild | 1,0 | 1,5 | 667 | -14 % |
| Hainwächter | 1,15 | 0,95 | 781 | +1 % |
| Bestienbund | 1,15 | 0,95 | 878 | +13 % |
| Dornenwächter | 1,15 | 0,5 | 856 | +10 % |
| Sturmfalke | 1,15 | 0,65 | 820 | +6 % |
| Sturmrufer | 2,0 | 2,0 | 718 | -7 % |
| Weber | 2,0 | 0,9 | 784 | +1 % |
| Katalysator | 2,0 | 0,85 | 769 | -1 % |
| Glutbinder | 2,0 | 1,0 | 860 | +11 % |
| Wegelagerer | 1,55 | 1,4 | 705 | -9 % |
| Phantomtänzer | 1,55 | 1,45 | 708 | -9 % |
| Schattenhüter | 1,55 | 1,55 | 730 | -6 % |
| Klingenwirbel | 1,55 | 1,0 | 701 | -10 % |

**Ressourcen-Tempi:** Initiative 12, +1 je s (Schattenklinge, Waffenskills 2–5 kosten 3–6); Einstimmungen 10 s Wiederaufladung der verlassenen Einstimmung, 1 s global; Überladen nach 5 s in einer Einstimmung (Sturmrufer); Axt-Rückruf 6 s Fenster, +3 Initiative; Gefährten-Tausch 20 s (Wildhüter).
**Spezialisierungs-Kosten:** ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp) = 30 je Spezialisierung, nur eine aktiv (Platz 3), Wechsel außerhalb des Kampfes.

## Wächter

Schwere Rüstung · Segen, Rufe und Meditationen · Großschwert und Stab.

**Mechanik:** Schwere Rüstung, Segen und Feuer: Glutklinge im Nahkampf, Sturmruf aus der Ferne.

### Waffe: Glutklinge (Greatsword)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| 1 | Glutschnitt | 0,5 s | 0,706 | – | Release/Glut/S → Impact/Glut/S | Glut | Schneller Hieb. Verursacht 1 Stapel Verwundbarkeit. |
| 1.2 | Glutkreuz | 0,5 s | 0,706 | – | Release/Glut/S → Impact/Glut/S | Glut | Zweites Glied der Kette (71 % Koeffizient). |
| 1.3 | Glutfinale | 0,5 s | 0,988 | – | Release/Glut/S → Impact/Glut/S | Glut | Kettenabschluss: +40 % Schaden, trifft bis zu 3 Ziele, 1 Macht. |
| 2 | Aschewirbel | 0,2 s | 0,45 ×4 | 8 s | Release/Glut/M → Impact/Glut/M | Glut | Wirbelangriff (4 Treffer), weicht dabei aus. Wirbel-Finisher. |
| 3 | Flammensprung | 0,2 s | 1,4 | 10 s | Release/Glut/M → Impact/Glut/M | Glut | Springt zum Ziel, setzt es in Brand. Sprung-Finisher. |
| 4 | Glutboden | 0,5 s | 0,5 | 15 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Entzündet den Boden: Feuerfeld (6 s). |
| 5 | Bergbrecher | 0,75 s | 2 | 18 s | Shockwave/Glut/L → Impact/Glut/L | Glut | Wuchtiger Bodenstoß, 5 Verwundbarkeit. Explosions-Finisher. |

### Waffe: Sturmruf (Staff)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| 1 | Funkenbolzen | 0,6 s | 0,618 | – | Release/Luft/S → Impact/Luft/S | Luft | Magisches Geschoss. Projektil-Finisher. |
| 1.2 | Funkenpaar | 0,6 s | 0,618 | – | Release/Luft/S → Impact/Luft/S | Luft | Zweites Glied der Kette (62 % Koeffizient). |
| 1.3 | Funkensturm | 0,6 s | 0,865 | – | Release/Luft/S → Impact/Luft/S | Luft | Kettenabschluss: +40 % Schaden, durchschlägt (bis zu 3 Ziele), 2 Verwundbarkeit. |
| 2 | Kettenblitz | 0,5 s | 0,9 | 6 s | Release/Luft/M → Impact/Luft/M | Luft | Blitz, 3 Verwundbarkeit. Projektil-Finisher. |
| 3 | Gewitterfeld | 0,75 s | 0,4 | 15 s | Sigil/Luft/M → GroundCircle/Luft/M | Luft | Blitzfeld am Zielort (6 s), Schaden pro Sekunde. |
| 4 | Frostlanze | 0,75 s | 1 | 10 s | Release/Frost/M → Impact/Frost/M | Frost | Eisgeschoss, verursacht Kälte. |
| 5 | Donnerschlag | 1 s | 1,8 | 16 s | Sigil/Luft/L → Pillar/Luft/L | Luft | Einschlag am Zielort. Explosions-Finisher. |

### Heilung, Hilfe, Elite

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| Elite | Meteorzorn | 1,5 s | 4 | 75 s | Sigil/Feuer/Elite → GroundCircle/Feuer/Elite | Feuer | Meteor: großer Flächenschaden, Brennen, Feuerfeld und Explosions-Finisher. |
| Elite | Wächteravatar | 0,5 s | 0 | 90 s | Shockwave/Licht/Elite → Aura/Licht/Elite | Licht | Elite: 10 Macht, Schutz, Stabilität, Schnelligkeit und Regeneration für dich und Verbündete. |
| Heal | Quellsegen | 0,75 s | 0 | 25 s | Release/Wasser/M → Aura/Wasser/M | Wasser | Heilt dich stark und erzeugt ein Wasserfeld (5 s). |
| Heal · Shout | Atem der Wacht | 0,5 s | 0 | 20 s | Shockwave/Licht/L → Aura/Licht/L | Licht | Ruf: heilt dich und Verbündete in der Nähe, gewährt Regeneration. |
| Heal · Meditation | Innere Ruhe | 1 s | 0 | 30 s | Release/Frost/M → Aura/Frost/M | Frost | Meditation: starke Heilung, entfernt 2 Zustände, Entschlossenheit. |
| Utility · Shout | Kriegsschrei | 0,25 s | 0 | 25 s | Release/Licht/M → Aura/Licht/M | Licht | 6 Macht (12 s) und Eile (8 s). |
| Utility · Trap | Rauchbombe | 0,25 s | 0 | 20 s | Sigil/Schatten/M → GroundCircle/Schatten/M | Schatten | Rauchfeld + Explosions-Finisher: Tarnung im Feld. |
| Utility · Trap | Frostfessel | 0,5 s | 0,6 | 20 s | Sigil/Frost/M → Pillar/Frost/M | Frost | Kälte und 2 Verwundbarkeit im Bereich. |
| Utility · Shout | Haltet stand! | 0,25 s | 0 | 30 s | Shockwave/Licht/L → Aura/Licht/L | Licht | Ruf: Schutz und Stabilität für dich und Verbündete. |
| Utility · Shout | Zum Angriff! | 0,25 s | 0 | 35 s | Shockwave/Licht/L → Aura/Licht/L | Licht | Ruf: Wut und Schnelligkeit für dich und Verbündete. |
| Utility · Meditation | Klingenfokus | 0 s | 0 | 25 s | Release/Frost/M → Aura/Frost/M | Frost | Meditation: Wut und 5 Macht. |
| Utility · Meditation | Leere Stille | 0 s | 0 | 30 s | Release/Frost/M → Aura/Frost/M | Frost | Meditation: entfernt 3 Zustände, Widerstand. |
| Utility · Meditation | Sturmschritt | 0 s | 0 | 35 s | Release/Frost/M → Aura/Frost/M | Frost | Meditation: Aegis, Elan und Eile. |
| Utility · Trap | Dornengrube | 0,5 s | 0,5 | 25 s | Sigil/Natur/M → Pillar/Natur/M | Natur | Falle: Blutung, Verkrüppelung und Immobilisierung im Bereich. |

### Eigenschaftslinien

**Glutzorn** – Kraft, kritische Treffer und Großschwert.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Glühende Kraft | +100 Kraft. |
| 2 | Neben | Schwelende Wunden | Brennen hält 20 % länger. |
| 3 | Neben | Grausamkeit | +120 Wildheit. |
| 1 | Haupt | Scharfe Schneide | +10 % Schaden gegen verwundbare Ziele. |
| 1 | Haupt | Großschwert-Meisterschaft | Großschwert-Fertigkeiten laden 20 % schneller auf; +5 % Großschwert-Schaden. |
| 1 | Haupt | Funkenschlag | Kritische Treffer setzen das Ziel 2 s in Brand (Abklingzeit 3 s). |
| 2 | Haupt | Blutrausch | +10 % Schaden, solange du Wut hast. |
| 2 | Haupt | Zornesmacht | Kritische Treffer gewähren 1 Macht für 8 s (Abklingzeit 1 s). |
| 2 | Haupt | Aschenhaut | +150 Zähigkeit; Schaden erleiden gewährt 3 s Schutz (Abklingzeit 20 s). |
| 3 | Haupt | Henkersstoß | +15 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Glutentfesselung | Waffenwechsel: 6 s Wut und 3 Macht (Abklingzeit 9 s). |
| 3 | Haupt | Feuersturm | Brennen hält 33 % länger; Elitefertigkeit lädt 20 % schneller auf. |

**Sturmweisheit** – Zustände, Kontrolle, Stab und Fallen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Funkenblut | +100 Zustandsschaden. |
| 2 | Neben | Eisige Hände | Kälte hält 25 % länger. |
| 3 | Neben | Gelehrsamkeit | +120 Sachkenntnis. |
| 1 | Haupt | Stabmeisterschaft | Stab-Fertigkeiten laden 20 % schneller auf; +5 % Stab-Schaden. |
| 1 | Haupt | Splitterfunken | Kritische Treffer verursachen 1 Blutung für 6 s (Abklingzeit 1 s). |
| 1 | Haupt | Frostbiss | +10 % Schaden gegen unterkühlte Ziele. |
| 2 | Haupt | Fallensteller | Fallen laden 20 % schneller auf und verursachen 10 % mehr Schaden. |
| 2 | Haupt | Zersetzung | Erhalte Zustandsschaden in Höhe von 10 % deiner Präzision. |
| 2 | Haupt | Kältewelle | Ausweichen lässt dein Ziel 2 s unterkühlen (Abklingzeit 5 s). |
| 3 | Haupt | Gewittergeist | Besiegte Gegner gewähren 3 s Schnelligkeit (Abklingzeit 10 s). |
| 3 | Haupt | Siechtum | Alle Zustände halten 15 % länger; +5 % Schaden gegen blutende Ziele. |
| 3 | Haupt | Sturmauge | +150 Präzision und +5 % kritische Trefferchance. |

**Wachtbund** – Verteidigung, Segen, Rufe und Meditationen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Eiserner Wille | +100 Vitalität. |
| 2 | Neben | Segenshand | +120 Konzentration. |
| 3 | Neben | Heilende Gabe | +120 Heilkraft. |
| 1 | Haupt | Lautstarke Wacht | Rufe laden 20 % schneller auf. |
| 1 | Haupt | Stille Disziplin | Meditationen laden 20 % schneller auf. |
| 1 | Haupt | Heiliger Schild | Heilfertigkeiten gewähren Aegis (Abklingzeit 15 s). |
| 2 | Haupt | Ausdauernde Segen | Alle Segen halten 20 % länger. |
| 2 | Haupt | Zweiter Atem | Ausweichen heilt dich (Abklingzeit 3 s). |
| 2 | Haupt | Bollwerk | Erhalte Kraft in Höhe von 8 % deiner Zähigkeit; Schaden erleiden gewährt 3 s Stabilität (Abklingzeit 25 s). |
| 3 | Haupt | Ewige Wacht | Heilfertigkeiten laden 33 % schneller auf. |
| 3 | Haupt | Siegesmut | Besiegte Gegner heilen dich (Abklingzeit 2 s); +7 % Schaden mit Schutz. |
| 3 | Haupt | Unerschütterlich | Macht hält 25 % länger; +7 % Schaden mit Stabilität. |

### Spezialisierung: Glutrichter

- **Mechanik:** Urteil – F5 Urteil: markiert das Ziel (2 Brennen); nach 2,5 s explodiert die Marke mit Brennen im Umkreis (20 s Abklingzeit). Ergänzt die Linien aus Lauf 3.
- **Waffe:** Richterbogen · **Skilltyp:** Trap · **Palette/Sound-Motiv:** Richtglut · **Signatur:** glutrichter_GR_Urteil · **Faktor:** 0,9
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Lichtpfeil | 0,6 s | 0,648 | – | Release/Richtglut/S → Impact/Richtglut/S | Richtglut | Grundangriff auf bis zu 30 m; Brennen (1). Projektil-Finisher. |
| W2 | Strahlensalve | 0,5 s | 0,378 ×3 | 8 s | Release/Richtglut/M → Impact/Richtglut/M | Richtglut | Drei schnelle Geschosse; je Treffer Brennen. |
| W3 | Rückzugspfeil | 0,3 s | 0,45 | 12 s | Release/Richtglut/M → Impact/Richtglut/M | Richtglut | Schuss aus der Ausweichbewegung; Eile und Wut für dich. |
| W4 | Bannpfeil | 0,5 s | 0,585 | 18 s | Release/Richtglut/M → Impact/Richtglut/M | Richtglut | Kontrollschuss: Immobilisierung (2 s) und Schwäche. |
| W5 | Lichtregen | 0,75 s | 0,54 ×3 | 25 s | Sigil/Richtglut/L → Pillar/Richtglut/L | Richtglut | Großer Flächen-Skill am Zielort (3 Wellen), Brennen (2). Explosions-Finisher. |
| Typ | Lichtfalle der Gnade | 0,75 s | 0 | 25 s | Release/Richtglut/M → Aura/Richtglut/M | Richtglut | Falle: Heilung; Wut für dich. |
| Typ | Lichtfalle des Banns | 0,25 s | 0,72 | 25 s | Shockwave/Richtglut/M → Impact/Richtglut/M | Richtglut | Falle: wird gelegt und löst bei Kontakt aus – Brennen (3). |
| Typ | Lichtfalle der Glut | 0,25 s | 0,9 | 30 s | Shockwave/Richtglut/M → Impact/Richtglut/M | Richtglut | Falle: wird gelegt und löst bei Kontakt aus – Brennen (4). |
| Typ | Lichtfalle des Kreises | 0,25 s | 1,08 | 35 s | Shockwave/Richtglut/M → Impact/Richtglut/M | Richtglut | Falle: wird gelegt und löst bei Kontakt aus – Brennen (5), Immobilisierung. |
| Typ | Falle des Gerichts | 0,75 s | 1,08 | 90 s | Shockwave/Richtglut/Elite → Impact/Richtglut/Elite | Richtglut | Elite (Falle): Brennen (4) an Gegnern um dich, Wut und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Urteilsspruch | 0 s | 1,26 | – | Sigil/Richtglut/M → Pillar/Richtglut/M | Richtglut | Explosion der Urteilsmarke: Brennen im Umkreis (Explosions-Finisher). |

**Glutrichter** – Gold-Orange und Lichtpfeile – Langbogen, Urteil, Licht-Fallen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Richterblick | +120 Kraft. |
| 2 | Neben | Glutpfeil | Wut halten 20 % länger. +60 Präzision. |
| 3 | Neben | Heiliger Eifer | +120 Wildheit. |
| 1 | Haupt | Bogenkunde | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Fallensteller des Lichts | Falle-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Falle-Fertigkeiten. |
| 1 | Haupt | Schnelles Urteil | Elite-Mechanik (F5) einsetzen: Wut für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Brandmal | +10 % Schaden gegen Ziele mit Brennen. |
| 2 | Haupt | Lichtsplitter | Kritische Treffer: Brennen auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Gerechter Zorn | +7 % Schaden, solange du Wut hast. Wut halten 15 % länger. |
| 3 | Haupt | Vollstreckung | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Lichtschritt | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Ewiges Feuer | Alle Zustände halten 15 % länger. +7 % deiner Kraft als Wildheit. |

### Spezialisierung: Bollwerkmeister

- **Mechanik:** Festung – F5 Festung: 10 s Kuppel – Projektile werden abgewehrt, ein Drittel des Schadens trägt die Kuppel; Schutz für dich und Verbündete (8 m), 30 s Abklingzeit.
- **Waffe:** Schwert & Turmschild · **Skilltyp:** Mantra · **Palette/Sound-Motiv:** Bollwerk · **Signatur:** bollwerkmeister_W5 · **Faktor:** 2
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Schildhieb | 0,5 s | 1,258 | – | Release/Bollwerk/S → Impact/Bollwerk/S | Bollwerk | Grundangriff (Kette aus drei Gliedern), das dritte Glied verursacht Schwäche. |
| W1.2 | Gardehieb | 0,5 s | 1,258 | – | Release/Bollwerk/S → Impact/Bollwerk/S | Bollwerk | Zweites Glied der Kette (63 % Koeffizient). |
| W1.3 | Wallstoß | 0,5 s | 1,761 | – | Release/Bollwerk/L → Impact/Bollwerk/L | Bollwerk | Kettenabschluss: +40 % Schaden, Verkrüppelung (2 s). |
| W2 | Mauerwirbel | 0,3 s | 0,72 ×4 | 8 s | Shockwave/Bollwerk/M → Impact/Bollwerk/M | Bollwerk | Wirbelnder Angriff um dich (4 Treffer), Schwäche. Wirbel-Finisher. |
| W3 | Schildsprung | 0,3 s | 1,9 | 12 s | Release/Bollwerk/L → Impact/Bollwerk/L | Bollwerk | Sprung zum Ziel (9 m). Sprung-Finisher; Schutz für dich. |
| W4 | Schildstoß | 0,5 s | 1,3 | 18 s | Release/Bollwerk/M → Impact/Bollwerk/M | Bollwerk | Wuchtiger Schlag, betäubt (1 s) und unterbricht. |
| W5 | Bollwerk | 0,75 s | 1,2 ×3 | 25 s | Sigil/Bollwerk/L → Pillar/Bollwerk/L | Bollwerk | Großer Flächen-Skill am Zielort (3 Wellen), Schwäche (2). Explosions-Finisher. |
| Typ | Mantra der Linderung | 0,75 s | 0 | 25 s | Release/Bollwerk/M → Aura/Bollwerk/M | Bollwerk | Mantra: Heilung; Schutz für dich. |
| Typ | Mantra des Schilds | 0 s | 0 | 25 s | Shockwave/Bollwerk/L → Aura/Bollwerk/L | Bollwerk | Mantra: aufgeladener Sofort-Skill – Aegis für dich und Verbündete. |
| Typ | Mantra der Wacht | 0 s | 0 | 30 s | Shockwave/Bollwerk/L → Aura/Bollwerk/L | Bollwerk | Mantra: aufgeladener Sofort-Skill – Schutz für dich und Verbündete, heilt. |
| Typ | Mantra der Mauer | 0 s | 0 | 35 s | Shockwave/Bollwerk/L → Aura/Bollwerk/L | Bollwerk | Mantra: aufgeladener Sofort-Skill – Stabilität für dich und Verbündete. |
| Typ | Mantra der Festung | 0,75 s | 2,4 | 90 s | Shockwave/Bollwerk/Elite → Impact/Bollwerk/Elite | Bollwerk | Elite (Mantra): Schwäche (4) an Gegnern um dich, Schutz und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Festungskuppel | 0 s | 0 | – | Shockwave/Bollwerk/L → Aura/Bollwerk/L | Bollwerk | Die Kuppel der Festung (Projektilschutz). |

**Bollwerkmeister** – Blauweiße Schildsegmente – Schwert & Schild, Festung, Mantras.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Fester Stand | +120 Zähigkeit. |
| 2 | Neben | Wallkraft | Schutz halten 20 % länger. +60 Konzentration. |
| 3 | Neben | Schildwacht | +120 Vitalität. |
| 1 | Haupt | Schildkunde | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Mantrenkraft | Mantra-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Mantra-Fertigkeiten. |
| 1 | Haupt | Kuppelbauer | Elite-Mechanik (F5) einsetzen: Schutz für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Entkräften | +10 % Schaden gegen Ziele mit Schwäche. |
| 2 | Haupt | Prallschild | Kritische Treffer: Schwäche auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Unerschütterlich | +7 % Schaden, solange du Schutz hast. Schutz halten 15 % länger. |
| 3 | Haupt | Letzte Bastion | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Rückhalt | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Ewige Mauer | Alle Zustände halten 15 % länger. +7 % deiner Zähigkeit als Vitalität. |

### Spezialisierung: Sturmherold

- **Mechanik:** Sturmzorn – F5 Sturmzorn: 10 s lang schlägt bei jedem gewirkten Skill ein Blitz am Ziel ein (max. 1/s); Schnelligkeit für dich und Verbündete (10 m), 25 s Abklingzeit.
- **Waffe:** Donnerhammer · **Skilltyp:** Symbol · **Palette/Sound-Motiv:** Sturmblau · **Signatur:** sturmherold_SH_Blitz · **Faktor:** 1,15
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Donnerschlag | 0,5 s | 0,723 | – | Release/Sturmblau/S → Impact/Sturmblau/S | Sturmblau | Grundangriff (Kette aus drei Gliedern), das dritte Glied verursacht Verwundbarkeit. |
| W1.2 | Funkenhieb | 0,5 s | 0,723 | – | Release/Sturmblau/S → Impact/Sturmblau/S | Sturmblau | Zweites Glied der Kette (63 % Koeffizient). |
| W1.3 | Wetterschlag | 0,5 s | 1,013 | – | Release/Sturmblau/S → Impact/Sturmblau/S | Sturmblau | Kettenabschluss: +40 % Schaden, Schwäche (3 s). |
| W2 | Blitzwirbel | 0,3 s | 0,414 ×4 | 8 s | Shockwave/Sturmblau/M → Impact/Sturmblau/M | Sturmblau | Wirbelnder Angriff um dich (4 Treffer), Verwundbarkeit. Wirbel-Finisher. |
| W3 | Sturmsprung | 0,3 s | 1,092 | 12 s | Release/Sturmblau/M → Impact/Sturmblau/M | Sturmblau | Sprung zum Ziel (9 m). Sprung-Finisher; Schnelligkeit für dich. |
| W4 | Bannschlag | 0,5 s | 0,747 | 18 s | Release/Sturmblau/M → Impact/Sturmblau/M | Sturmblau | Wuchtiger Schlag, betäubt (1 s) und unterbricht. |
| W5 | Gewitterkreis | 0,75 s | 0,69 ×3 | 25 s | Sigil/Sturmblau/L → GroundCircle/Sturmblau/L | Sturmblau | Großer Flächen-Skill am Zielort (3 Wellen), Verwundbarkeit (2); hinterlässt ein Blitz-Feld. |
| Typ | Symbol der Ruhe | 0,75 s | 0 | 25 s | Release/Sturmblau/M → Aura/Sturmblau/M | Sturmblau | Symbol: Heilung; Schnelligkeit für dich. |
| Typ | Symbol des Funkens | 0,25 s | 0,403 ×4 | 25 s | Sigil/Sturmblau/M → GroundCircle/Sturmblau/M | Sturmblau | Symbol: wanderndes Blitzsymbol am Zielort (4 Treffer, Blitzfeld), 1 Verwundbarkeit. |
| Typ | Symbol des Donners | 0,25 s | 0,46 ×4 | 30 s | Sigil/Sturmblau/M → GroundCircle/Sturmblau/M | Sturmblau | Symbol: wanderndes Blitzsymbol am Zielort (4 Treffer, Blitzfeld), 2 Verwundbarkeit. |
| Typ | Symbol der Eile | 0,25 s | 0,518 ×4 | 35 s | Sigil/Sturmblau/M → GroundCircle/Sturmblau/M | Sturmblau | Symbol: wanderndes Blitzsymbol am Zielort (4 Treffer, Blitzfeld), 3 Verwundbarkeit. |
| Typ | Symbol des Sturmherolds | 0,75 s | 1,38 | 90 s | Shockwave/Sturmblau/Elite → Impact/Sturmblau/Elite | Sturmblau | Elite (Symbol): Verwundbarkeit (4) an Gegnern um dich, Schnelligkeit und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Sturmzorn-Blitz | 0 s | 0,575 | – | Sigil/Sturmblau/M → Pillar/Sturmblau/M | Sturmblau | Blitzeinschlag am Ziel (Sturmzorn), 1 Verwundbarkeit. |

**Sturmherold** – Elektroblaue Blitzsäulen – Hammer, Sturmzorn, Symbole.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Donnerherz | +120 Kraft. |
| 2 | Neben | Herold der Eile | Schnelligkeit halten 20 % länger. +60 Präzision. |
| 3 | Neben | Funkenhaut | +120 Konzentration. |
| 1 | Haupt | Hammerkunde | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Wanderndes Symbol | Symbol-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Symbol-Fertigkeiten. |
| 1 | Haupt | Sturmruf | Elite-Mechanik (F5) einsetzen: Schnelligkeit für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Elektrisiert | +10 % Schaden gegen Ziele mit Verwundbarkeit. |
| 2 | Haupt | Kettenblitz | Kritische Treffer: Verwundbarkeit auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Rasende Eile | +7 % Schaden, solange du Schnelligkeit hast. Schnelligkeit halten 15 % länger. |
| 3 | Haupt | Donnergericht | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Blitzschritt | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Auge des Sturms | Alle Zustände halten 15 % länger. +7 % deiner Kraft als Konzentration. |

### Spezialisierung: Dornenschild

- **Mechanik:** Dornenhaut – F5 Dornenhaut: 8 s Vergeltung – direkte Treffer verletzen den Angreifer, jeden Zeitpunkt (1 s) wird ein Zustand von dir als Brennen auf den nächsten Gegner zurückgeworfen; Entschlossenheit, 30 s Abklingzeit.
- **Waffe:** Dornenzepter & Fackel · **Skilltyp:** Consecration · **Palette/Sound-Motiv:** Smaragd · **Signatur:** dornenschild_DS_Dornenhaut · **Faktor:** 1,5
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Dornenfunke | 0,6 s | 0,952 | – | Release/Smaragd/S → Impact/Smaragd/S | Smaragd | Grundangriff auf bis zu 20 m; Blutung (1). Projektil-Finisher. |
| W1.2 | Dornenfunke · Folgeschuss | 0,6 s | 0,952 | – | Release/Smaragd/S → Impact/Smaragd/S | Smaragd | Zweites Glied der Kette (64 % Koeffizient). |
| W1.3 | Dornenfunke · Abschlussschuss | 0,6 s | 1,334 | – | Release/Smaragd/S → Impact/Smaragd/S | Smaragd | Kettenabschluss: +40 % Schaden, Brennen (3 s). |
| W2 | Rankenfeuer | 0,5 s | 0,63 ×3 | 8 s | Release/Smaragd/M → Impact/Smaragd/M | Smaragd | Drei schnelle Geschosse; je Treffer Blutung. |
| W3 | Lichtsprung | 0,3 s | 0,75 | 12 s | Release/Smaragd/M → Impact/Smaragd/M | Smaragd | Schuss aus der Ausweichbewegung; Eile und Regeneration für dich. |
| W4 | Dornenbann | 0,5 s | 0,975 | 18 s | Release/Smaragd/M → Impact/Smaragd/M | Smaragd | Kontrollschuss: Immobilisierung (2 s) und Schwäche. |
| W5 | Fackelkreis | 0,75 s | 0,9 ×3 | 25 s | Sigil/Smaragd/L → Pillar/Smaragd/L | Smaragd | Großer Flächen-Skill am Zielort (3 Wellen), Blutung (2). Explosions-Finisher. |
| Typ | Weihe der Heilung | 0,75 s | 0 | 25 s | Release/Smaragd/M → Aura/Smaragd/M | Smaragd | Weihe: Heilung; Regeneration für dich. |
| Typ | Weihe des Schutzes | 0,25 s | 0,3 ×3 | 25 s | Sigil/Smaragd/L → Pillar/Smaragd/L | Smaragd | Weihe: Weihekreis am Zielort – Regeneration für Verbündete, Blutung für Gegner. |
| Typ | Weihe der Dornen | 0,25 s | 0,3 ×3 | 30 s | Sigil/Smaragd/L → Pillar/Smaragd/L | Smaragd | Weihe: Weihekreis am Zielort – Schutz für Verbündete, Blutung für Gegner. |
| Typ | Weihe des Willens | 0,25 s | 0,3 ×3 | 35 s | Sigil/Smaragd/L → Pillar/Smaragd/L | Smaragd | Weihe: Weihekreis am Zielort – Entschlossenheit für Verbündete, Blutung für Gegner. |
| Typ | Weihe des Smaragdhains | 0,75 s | 1,8 | 90 s | Shockwave/Smaragd/Elite → Impact/Smaragd/Elite | Smaragd | Elite (Weihe): Blutung (4) an Gegnern um dich, Regeneration und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Dornenhaut | 0 s | 0 | – | Shockwave/Smaragd/M → Aura/Smaragd/M | Smaragd | Die Dornenhaut (Vergeltung, Zustands-Rückwurf). |

**Dornenschild** – Smaragd-Gold und Dornenkreise – Zepter & Fackel, Dornenhaut, Weihen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Smaragdhaut | +120 Zähigkeit. |
| 2 | Neben | Wurzelglaube | Regeneration halten 20 % länger. +60 Heilkraft. |
| 3 | Neben | Dornengebet | +120 Zustandsschaden. |
| 1 | Haupt | Zepterkunde | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Geweihter Boden | Weihe-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Weihe-Fertigkeiten. |
| 1 | Haupt | Vergeltung | Elite-Mechanik (F5) einsetzen: Regeneration für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Spitze Dornen | +10 % Schaden gegen Ziele mit Blutung. |
| 2 | Haupt | Rückwurf | Kritische Treffer: Blutung auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Grüne Gnade | +7 % Schaden, solange du Regeneration hast. Regeneration halten 15 % länger. |
| 3 | Haupt | Letzte Weihe | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Schutzranke | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Dornenkrone | Alle Zustände halten 15 % länger. +7 % deiner Zähigkeit als Zustandsschaden. |

## Wildhüter

Mittlere Rüstung · Tiergefährte · Langbogen, Axt + Fackel, Schwert + Kriegshorn · Fallen, Signale, Wildnis.

**Mechanik:** Tiergefährte: F1 greift dein Ziel an, F2 Familien-Skill, F3 ruft zurück (Passiv), F4 wechselt den Gefährten (2 aktiv, 20 s). Wolf, Bär oder Falke.

### Waffe: Langbogen (Longbow)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| 1 | Weitschuss | 0,6 s | 0,862 | – | Release/Natur/S → Impact/Natur/S | Natur | Pfeil, der auf große Entfernung mehr Schaden verursacht. Projektil-Finisher. |
| 2 | Pfeilhagel | 0,25 s | 0,322 ×6 | 10 s | Release/Natur/M → Impact/Natur/M | Natur | Schnelle Folge von 6 Pfeilen; 1 Verwundbarkeit je Treffer. |
| 3 | Rückzugsschuss | 0,3 s | 0,575 | 15 s | Release/Natur/M → Impact/Natur/M | Natur | Weicht beim Schuss aus und gewährt Eile. |
| 4 | Wuchtpfeil | 0,5 s | 0,69 | 20 s | Release/Natur/M → Impact/Natur/M | Natur | Schwerer Pfeil, der das Ziel betäubt (Unterbrechung). |
| 5 | Dornenregen | 0,5 s | 0,345 ×6 | 24 s | Sigil/Natur/L → Pillar/Natur/L | Natur | Pfeilregen am Zielort (6 Wellen), Blutung. |

### Waffe: Axt & Fackel (AxeTorch)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| 1 | Wirbelaxt | 0,55 s | 0,73 | – | Release/Natur/S → Impact/Natur/S | Natur | Geworfene Axt, die zurückfliegt; 1 Verwundbarkeit. |
| 1.2 | Wirbelaxt · Folgeschuss | 0,55 s | 0,73 | – | Release/Natur/S → Impact/Natur/S | Natur | Zweites Glied der Kette (64 % Koeffizient). |
| 1.3 | Wirbelaxt · Abschlussschuss | 0,55 s | 1,022 | – | Release/Natur/S → Impact/Natur/S | Natur | Kettenabschluss: +40 % Schaden, Wut (4 s). |
| 2 | Doppelwurf | 0,5 s | 0,69 ×2 | 8 s | Release/Natur/M → Impact/Natur/M | Natur | Zwei Äxte, die verkrüppeln. |
| 3 | Klingenwirbel | 0,25 s | 0,483 ×5 | 12 s | Shockwave/Natur/M → Impact/Natur/M | Natur | Wirbelt mit den Äxten (5 Treffer), Wirbel-Finisher. |
| 4 | Fackelstoß | 0,5 s | 1,035 | 15 s | Release/Feuer/M → Impact/Feuer/M | Feuer | Stößt die Fackel ins Ziel: 3 Brennen. |
| 5 | Feuerkreis | 0,5 s | 0,345 | 20 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Entzündet einen Feuerkreis (Feuerfeld, 5 s). |

### Waffe: Schwert & Kriegshorn (SwordWarhorn)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| 1 | Schnitthieb | 0,5 s | 0,69 | – | Release/Natur/S → Impact/Natur/S | Natur | Schneller Schwerthieb. |
| 1.2 | Kreuzhieb | 0,5 s | 0,69 | – | Release/Natur/S → Impact/Natur/S | Natur | Zweites Glied der Kette (60 % Koeffizient). |
| 1.3 | Ausfallhieb | 0,5 s | 0,966 | – | Release/Natur/S → Impact/Natur/S | Natur | Kettenabschluss: +40 % Schaden, Verkrüppelung (2 s). |
| 2 | Hechtsprung | 0,25 s | 1,15 | 8 s | Release/Natur/M → Impact/Natur/M | Natur | Springt zum Ziel. Sprung-Finisher. |
| 3 | Ausweichstoß | 0,4 s | 0,92 | 12 s | Release/Natur/M → Impact/Natur/M | Natur | Rollt zur Seite und kontert; weicht aus. |
| 4 | Ruf der Wildnis | 0,25 s | 0 | 25 s | Shockwave/Natur/L → Aura/Natur/L | Natur | Kriegshorn: Wut und Eile für dich, Verbündete und den Gefährten. |
| 5 | Vogelschwarm | 0,5 s | 0,345 ×5 | 30 s | Release/Natur/M → Impact/Natur/M | Natur | Ein Schwarm Vögel blendet das Ziel (5 Treffer). |

### Heilung, Hilfe, Elite

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| Heal | Heilquelle | 0,75 s | 0 | 25 s | Release/Wasser/M → Aura/Wasser/M | Wasser | Heilt dich und erzeugt ein Wasserfeld (5 s). |
| Heal | Wildatem | 0,5 s | 0 | 20 s | Shockwave/Natur/L → Aura/Natur/L | Natur | Heilt dich, Verbündete und den Gefährten; Regeneration. |
| Heal | Lebenskraut | 1 s | 0 | 30 s | Release/Natur/M → Aura/Natur/M | Natur | Starke Heilung, entfernt 2 Zustände. |
| Utility · Trap | Dornfalle | 0,5 s | 0,575 | 20 s | Shockwave/Natur/M → Impact/Natur/M | Natur | Falle: löst bei Kontakt aus – 4 Blutung, Verkrüppelung. |
| Utility · Trap | Flammenfalle | 0,5 s | 0,69 | 24 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Falle: Feuerfeld und 3 Brennen bei Kontakt. |
| Utility · Trap | Frostfalle | 0,5 s | 0,575 | 28 s | Shockwave/Frost/M → Impact/Frost/M | Frost | Falle: Kälte und Immobilisierung bei Kontakt. |
| Utility · Signal | Sammelruf | 0 s | 0 | 25 s | Shockwave/Natur/L → Aura/Natur/L | Natur | Signal: 5 Macht und Wut für dich, Verbündete und den Gefährten. |
| Utility · Signal | Schutzruf | 0 s | 0 | 30 s | Shockwave/Natur/L → Aura/Natur/L | Natur | Signal: Schutz und Regeneration für alle. |
| Utility · Signal | Jagdruf | 0 s | 0 | 35 s | Shockwave/Natur/L → Aura/Natur/L | Natur | Signal: Schnelligkeit und Eile für alle. |
| Utility · Wilderness | Reinigungskräuter | 0,25 s | 0 | 25 s | Release/Natur/M → Aura/Natur/M | Natur | Wildnis: entfernt 3 Zustände, Widerstand. |
| Utility · Wilderness | Tarnblatt | 0 s | 0 | 30 s | Release/Schatten/M → Aura/Schatten/M | Schatten | Wildnis: 3 s Tarnung. |
| Utility · Wilderness | Quellgrund | 0,5 s | 0 | 30 s | Sigil/Wasser/M → GroundCircle/Wasser/M | Wasser | Wildnis: Heilquelle am Zielort (Wasserfeld, Regeneration). |
| Elite | Rudelruf | 0,5 s | 0 | 90 s | Shockwave/Natur/Elite → Aura/Natur/Elite | Natur | Elite: 10 Macht, Wut und Schnelligkeit für dich, Verbündete und den Gefährten. |
| Elite | Urgewalt | 0,5 s | 0 | 120 s | Release/Natur/Elite → Aura/Natur/Elite | Natur | Elite: Stabilität, Schutz und Regeneration; heilt dich. |

### Mechanik-Skills

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| F | Rudelheulen | 0 s | 0 | 25 s | Shockwave/Natur/L → Aura/Natur/L | Natur | Wolf: Wut für dich und den Gefährten; Feinde in der Nähe werden verkrüppelt. |
| F | Bärenbrüllen | 0 s | 0 | 25 s | Shockwave/Natur/L → Aura/Natur/L | Natur | Bär: verspottet Feinde in der Nähe und gewährt dem Gefährten Schutz. |
| F | Sturzflug | 0 s | 1,15 | 20 s | Release/Natur/M → Impact/Natur/M | Natur | Falke: Sturzflug auf das Ziel, Blindheit; du erhältst Eile. |

### Eigenschaftslinien

**Jagdinstinkt** – Kraft, Präzision und Langbogen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Scharfes Auge | +100 Präzision. |
| 2 | Neben | Jägerkraft | +120 Kraft. |
| 3 | Neben | Tödliche Spur | +120 Wildheit. |
| 1 | Haupt | Langbogen-Meisterschaft | Langbogen-Fertigkeiten laden 20 % schneller auf; +5 % Langbogen-Schaden. |
| 1 | Haupt | Hetzjagd | +10 % Schaden gegen verkrüppelte Ziele. |
| 1 | Haupt | Schnelle Pfeile | Kritische Treffer gewähren 2 s Eile (Abklingzeit 5 s). |
| 2 | Haupt | Raubtierblick | +5 % kritische Trefferchance; +7 % Schaden mit Wut. |
| 2 | Haupt | Axtwerfer | Axt-Fertigkeiten laden 20 % schneller auf; +5 % Axt-Schaden. |
| 2 | Haupt | Zweiter Pfeil | Kritische Treffer verursachen 1 Blutung (Abklingzeit 1 s). |
| 3 | Haupt | Fangschuss | +15 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Jagdfieber | Besiegte Gegner gewähren 10 s Wut und 5 Macht (Abklingzeit 10 s). |
| 3 | Haupt | Klingenmeister | Schwert-Fertigkeiten laden 20 % schneller auf; +7 % Schwert-Schaden. |

**Wildniskunde** – Fallen, Wildnisfertigkeiten und Zustände.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Dornenblut | +100 Zustandsschaden. |
| 2 | Neben | Giftkunde | Blutung hält 20 % länger. |
| 3 | Neben | Wildnisgespür | +120 Sachkenntnis. |
| 1 | Haupt | Fallensteller | Fallen laden 20 % schneller auf, +10 % Fallen-Schaden. |
| 1 | Haupt | Kräuterkunde | Wildnisfertigkeiten laden 20 % schneller auf. |
| 1 | Haupt | Brennende Pfeile | Kritische Treffer setzen 2 s in Brand (Abklingzeit 4 s). |
| 2 | Haupt | Schlangenbiss | +10 % Schaden gegen vergiftete Ziele. |
| 2 | Haupt | Ausweichrolle | Ausweichen verkrüppelt dein Ziel (Abklingzeit 5 s). |
| 2 | Haupt | Zersetzende Dornen | Erhalte Zustandsschaden in Höhe von 10 % deiner Präzision. |
| 3 | Haupt | Wildwuchs | Alle Zustände halten 15 % länger. |
| 3 | Haupt | Tarnung des Jägers | Aus der Tarnung: 3 s Eile und 3 Macht (Abklingzeit 10 s). |
| 3 | Haupt | Feuerhorn | Fackel und Axt: +7 % Schaden; Brennen hält 25 % länger. |

**Tierbund** – Gefährte, Signale, Heilung und Segen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Zäher Pelz | +100 Vitalität. |
| 2 | Neben | Heilende Natur | +120 Heilkraft. |
| 3 | Neben | Rudelführer | +120 Konzentration. |
| 1 | Haupt | Lauter Ruf | Signale laden 20 % schneller auf. |
| 1 | Haupt | Gefährtenwechsel | Gefährten wechseln gewährt 5 Macht und Wut (Abklingzeit 10 s). |
| 1 | Haupt | Quellkraft | Heilfertigkeiten gewähren Regeneration und Schutz (Abklingzeit 15 s). |
| 2 | Haupt | Ausdauernde Segen | Alle Segen halten 20 % länger. |
| 2 | Haupt | Wildes Herz | Schaden erleiden gewährt 3 s Stabilität (Abklingzeit 25 s). |
| 2 | Haupt | Rudeltaktik | +7 % Schaden mit Macht; Macht hält 20 % länger. |
| 3 | Haupt | Ewiger Frühling | Heilfertigkeiten laden 33 % schneller auf. |
| 3 | Haupt | Naturverbunden | Ausweichen heilt dich (Abklingzeit 3 s). |
| 3 | Haupt | Urkraft | Elitefertigkeit lädt 20 % schneller auf; besiegte Gegner heilen dich (Abklingzeit 2 s). |

### Spezialisierung: Hainwächter

- **Mechanik:** Hainform – F5 Hainform: die Leiste füllt sich durch Heilung (und langsam im Kampf); voll → 15 s Avatar mit 5 Heil- und Unterstützungs-Skills (F5 beendet früher).
- **Waffe:** Hainstab · **Skilltyp:** Glyph · **Palette/Sound-Motiv:** Mondlicht · **Signatur:** hainwaechter_HF5 · **Faktor:** 0,95
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Mondstrahl | 0,6 s | 0,694 | – | Release/Mondlicht/S → Impact/Mondlicht/S | Mondlicht | Grundangriff auf bis zu 24 m; Verkrüppelung (1). Projektil-Finisher. Heilt Verbündete nahe dem Ziel leicht. |
| W1.2 | Mondstrahl · Folgeschuss | 0,6 s | 0,694 | – | Release/Mondlicht/S → Impact/Mondlicht/S | Mondlicht | Zweites Glied der Kette (64 % Koeffizient). |
| W1.3 | Mondstrahl · Abschlussschuss | 0,6 s | 0,971 | – | Release/Mondlicht/S → Impact/Mondlicht/S | Mondlicht | Kettenabschluss: +40 % Schaden, durchschlägt (bis zu 3 Ziele), 2 Verwundbarkeit. |
| W2 | Blütenschauer | 0,5 s | 0,459 ×3 | 8 s | Release/Mondlicht/M → Impact/Mondlicht/M | Mondlicht | Drei schnelle Geschosse; je Treffer Verkrüppelung. |
| W3 | Wurzelsprung | 0,3 s | 0,546 | 12 s | Release/Mondlicht/M → Impact/Mondlicht/M | Mondlicht | Schuss aus der Ausweichbewegung; Eile und Regeneration für dich. |
| W4 | Silberwelle | 0,5 s | 0 | 18 s | Shockwave/Mondlicht/L → Aura/Mondlicht/L | Mondlicht | Schützende Welle: Schutz für dich und Verbündete, entfernt einen Zustand. |
| W5 | Lichtung | 0,75 s | 0,328 ×3 | 25 s | Sigil/Mondlicht/L → GroundCircle/Mondlicht/L | Mondlicht | Heilender Kreis am Zielort: heilt Verbündete, Regeneration; schwacher Schaden an Gegnern. |
| Typ | Glyphe des Mondtaus | 0,75 s | 0 | 25 s | Release/Mondlicht/M → Aura/Mondlicht/M | Mondlicht | Glyphe: Heilung; Regeneration für dich. |
| Typ | Glyphe der Wurzeln | 0,25 s | 0,765 | 25 s | Sigil/Mondlicht/M → Pillar/Mondlicht/M | Mondlicht | Glyphe: trifft Gegner am Zielort (Verkrüppelung) und heilt Verbündete dort; in der Hainform zusätzlich Regeneration. |
| Typ | Glyphe des Hains | 0,25 s | 0,983 | 30 s | Sigil/Mondlicht/M → Pillar/Mondlicht/M | Mondlicht | Glyphe: trifft Gegner am Zielort (Verkrüppelung) und heilt Verbündete dort; in der Hainform zusätzlich Regeneration. |
| Typ | Glyphe der Lichtung | 0,25 s | 1,202 | 35 s | Sigil/Mondlicht/M → Pillar/Mondlicht/M | Mondlicht | Glyphe: trifft Gegner am Zielort (Verkrüppelung) und heilt Verbündete dort; in der Hainform zusätzlich Regeneration. |
| Typ | Glyphe des Erwachens | 0,75 s | 1,311 | 90 s | Shockwave/Mondlicht/Elite → Impact/Mondlicht/Elite | Mondlicht | Elite (Glyphe): Verkrüppelung (4) an Gegnern um dich, Regeneration und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Mondsegen | 0,5 s | 0,218 | 3 s | Shockwave/Mondlicht/L → Impact/Mondlicht/L | Mondlicht | Hainform: Heilt Verbündete im Umkreis, Regeneration. |
| Mech | Wurzelnetz | 0,5 s | 0,546 | 6 s | Shockwave/Mondlicht/L → Impact/Mondlicht/L | Mondlicht | Hainform: Ranken immobilisieren Gegner um dich. |
| Mech | Blütenwirbel | 0,5 s | 0,218 | 9 s | Shockwave/Mondlicht/L → Impact/Mondlicht/L | Mondlicht | Hainform: Reinigt 2 Zustände bei Verbündeten, Schutz. |
| Mech | Silberschleier | 0,5 s | 0,218 | 12 s | Shockwave/Mondlicht/L → Impact/Mondlicht/L | Mondlicht | Hainform: Stabilität und Aegis für Verbündete. |
| Mech | Sternenlichtung | 0,5 s | 0,218 | 15 s | Shockwave/Mondlicht/L → Impact/Mondlicht/L | Mondlicht | Hainform: Starker Heilkreis, belebt Verbündete auf (Wasserfeld). |

**Hainwächter** – Heilung, Mondlicht und Wurzeln – Stab, Hainform, Glyphen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Mondtau | +120 Heilkraft. |
| 2 | Neben | Wurzelkraft | Regeneration halten 20 % länger. +60 Vitalität. |
| 3 | Neben | Hainruhe | +120 Konzentration. |
| 1 | Haupt | Stab der Lichtung | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Glyphenkunde | Glyphe-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Glyphe-Fertigkeiten. |
| 1 | Haupt | Erwachter Hain | Elite-Mechanik (F5) einsetzen: Regeneration für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Fesselnde Ranken | +10 % Schaden gegen Ziele mit Verkrüppelung. |
| 2 | Haupt | Silbersplitter | Kritische Treffer: Verkrüppelung auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Ewiger Frühling | +7 % Schaden, solange du Regeneration hast. Regeneration halten 15 % länger. |
| 3 | Haupt | Letztes Licht | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Tanzende Blüten | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Wildwuchs des Hains | Alle Zustände halten 15 % länger. +7 % deiner Heilkraft als Konzentration. |

### Spezialisierung: Bestienbund

- **Mechanik:** Verschmelzen – F5 Verschmelzen: der Gefährte wird Teil von dir – kein aktiver Gefährte, dafür +150 Kraft/Präzision, +100 Wildheit; F1/F2 setzen die Familien-Skills beider Gefährten ein, F3 Raserei. F5 trennt wieder.
- **Waffe:** Dolch & Kriegshorn · **Skilltyp:** Stance · **Palette/Sound-Motiv:** Wildblut · **Signatur:** bestienbund_BB_Raserei · **Faktor:** 0,95
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Fangzahn | 0,5 s | 0,687 | – | Release/Wildblut/S → Impact/Wildblut/S | Wildblut | Grundangriff (Kette aus drei Gliedern), das dritte Glied verursacht Blutung. |
| W1.2 | Krallenriss | 0,5 s | 0,687 | – | Release/Wildblut/S → Impact/Wildblut/S | Wildblut | Zweites Glied der Kette (63 % Koeffizient). |
| W1.3 | Blutrausch | 0,5 s | 0,962 | – | Release/Wildblut/S → Impact/Wildblut/S | Wildblut | Kettenabschluss: +40 % Schaden, 2 Blutung. |
| W2 | Rudelwirbel | 0,3 s | 0,393 ×4 | 8 s | Shockwave/Wildblut/M → Impact/Wildblut/M | Wildblut | Wirbelnder Angriff um dich (4 Treffer), Blutung. Wirbel-Finisher. |
| W3 | Raubsprung | 0,3 s | 1,038 | 12 s | Release/Wildblut/M → Impact/Wildblut/M | Wildblut | Sprung zum Ziel (9 m). Sprung-Finisher; Macht für dich. |
| W4 | Kehlbiss | 0,5 s | 0,71 | 18 s | Release/Wildblut/M → Impact/Wildblut/M | Wildblut | Wuchtiger Schlag, betäubt (1 s) und unterbricht. |
| W5 | Jagdgeheul | 0,75 s | 0,656 ×3 | 25 s | Sigil/Wildblut/L → Pillar/Wildblut/L | Wildblut | Großer Flächen-Skill am Zielort (3 Wellen), Blutung (2). Explosions-Finisher. |
| Typ | Haltung des Lagers | 0,75 s | 0 | 25 s | Release/Wildblut/M → Aura/Wildblut/M | Wildblut | Haltung: Heilung; Macht für dich. |
| Typ | Haltung der Pirsch | 0 s | 0 | 25 s | Release/Wildblut/M → Aura/Wildblut/M | Wildblut | Haltung: lange Stärkung (Macht, Wut), dafür 2 Verwundbarkeit für dich. |
| Typ | Haltung des Rudels | 0 s | 0 | 30 s | Release/Wildblut/M → Aura/Wildblut/M | Wildblut | Haltung: lange Stärkung (Macht, Eile), dafür 2 Verwundbarkeit für dich. |
| Typ | Haltung des Felsens | 0 s | 0 | 35 s | Release/Wildblut/M → Aura/Wildblut/M | Wildblut | Haltung: lange Stärkung (Macht, Widerstand), dafür 2 Verwundbarkeit für dich. |
| Typ | Haltung der Urbestie | 0,75 s | 1,311 | 90 s | Shockwave/Wildblut/Elite → Impact/Wildblut/Elite | Wildblut | Elite (Haltung): Blutung (4) an Gegnern um dich, Macht und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Raserei | 0,25 s | 0 | 20 s | Release/Wildblut/M → Aura/Wildblut/M | Wildblut | Verschmolzen (F3): Schnelligkeit, Wut und 5 Macht für 6 s. |

**Bestienbund** – Wildheit und Blut – Dolch & Kriegshorn, Verschmelzen, Haltungen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Geteiltes Blut | +120 Kraft. |
| 2 | Neben | Rudelinstinkt | Macht halten 20 % länger. +60 Präzision. |
| 3 | Neben | Wilde Klauen | +120 Wildheit. |
| 1 | Haupt | Dolchmeister | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Haltungswechsel | Haltung-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Haltung-Fertigkeiten. |
| 1 | Haupt | Ruf des Bundes | Elite-Mechanik (F5) einsetzen: 5 Macht für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Blutspur | +10 % Schaden gegen Ziele mit Blutung. |
| 2 | Haupt | Klauenhieb | Kritische Treffer: Blutung auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Zorn der Bestie | +7 % Schaden, solange du Macht hast. Macht halten 15 % länger. |
| 3 | Haupt | Witterung | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Pirschschritt | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Urinstinkt | Alle Zustände halten 15 % länger. +7 % deiner Kraft als Wildheit. |

### Spezialisierung: Dornenwächter

- **Mechanik:** Entfesseln – F5 Entfesseln: die Verstärkung wechselt zwischen dir (+10 % Schaden, Skills 1–2 werden zu Dornenhieb und Rankensprung) und dem Gefährten (10 Macht, Wut). Entfesselungs-Rufe lassen den Gefährten Kontrolle einsetzen.
- **Waffe:** Rankenhammer · **Skilltyp:** Unleash · **Palette/Sound-Motiv:** Dorngift · **Signatur:** dornenwaechter_W5 · **Faktor:** 0,55
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Dornenschlag | 0,5 s | 0,398 | – | Release/Dorngift/S → Impact/Dorngift/S | Dorngift | Grundangriff (Kette aus drei Gliedern), das dritte Glied verursacht Vergiftung. |
| W1.2 | Stachelhieb | 0,5 s | 0,398 | – | Release/Dorngift/S → Impact/Dorngift/S | Dorngift | Zweites Glied der Kette (63 % Koeffizient). |
| W1.3 | Giftschlag | 0,5 s | 0,557 | – | Release/Dorngift/S → Impact/Dorngift/S | Dorngift | Kettenabschluss: +40 % Schaden, Schwäche (3 s). |
| W2 | Rankenwirbel | 0,3 s | 0,228 ×4 | 8 s | Shockwave/Dorngift/M → Impact/Dorngift/M | Dorngift | Wirbelnder Angriff um dich (4 Treffer), Vergiftung. Wirbel-Finisher. |
| W3 | Wurzelsturz | 0,3 s | 0,601 | 12 s | Release/Dorngift/M → Impact/Dorngift/M | Dorngift | Sprung zum Ziel (9 m). Sprung-Finisher; Schutz für dich. |
| W4 | Giftwurz | 0,5 s | 0,411 | 18 s | Release/Dorngift/M → Impact/Dorngift/M | Dorngift | Wuchtiger Schlag, betäubt (1 s) und unterbricht. |
| W5 | Dornenfeld | 0,75 s | 0,38 ×3 | 25 s | Sigil/Dorngift/L → Pillar/Dorngift/L | Dorngift | Großer Flächen-Skill am Zielort (3 Wellen), Vergiftung (2). Explosions-Finisher. |
| Typ | Ruf der Wurzeln | 0,75 s | 0 | 25 s | Release/Dorngift/M → Aura/Dorngift/M | Dorngift | Entfesselungs-Ruf: Heilung; Schutz für dich. |
| Typ | Ruf der Dornen | 0,25 s | 0 | 25 s | Shockwave/Dorngift/L → Aura/Dorngift/L | Dorngift | Entfesselungs-Ruf: dein Gefährte setzt Kontrolle ein (Betäubung/Verkrüppelung am Ziel), 3 Macht für euch. |
| Typ | Ruf des Dickichts | 0,25 s | 0 | 30 s | Shockwave/Dorngift/L → Aura/Dorngift/L | Dorngift | Entfesselungs-Ruf: dein Gefährte setzt Kontrolle ein (Betäubung/Verkrüppelung am Ziel), 6 Macht für euch. |
| Typ | Ruf der Ranken | 0,25 s | 0 | 35 s | Shockwave/Dorngift/L → Aura/Dorngift/L | Dorngift | Entfesselungs-Ruf: dein Gefährte setzt Kontrolle ein (Betäubung/Verkrüppelung am Ziel), 9 Macht für euch. |
| Typ | Ruf des Urwalds | 0,75 s | 0,759 | 90 s | Shockwave/Dorngift/Elite → Impact/Dorngift/Elite | Dorngift | Elite (Entfesselungs-Ruf): Vergiftung (4) an Gegnern um dich, Schutz und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Dornenhieb | 0,5 s | 0,696 | – | Release/Dorngift/M → Impact/Dorngift/M | Dorngift | Entfesselt: schwerer Hieb, 2 Gift. |
| Mech | Rankensprung | 0,3 s | 0,759 | 6 s | Release/Dorngift/M → Impact/Dorngift/M | Dorngift | Entfesselt: Sprung zum Ziel, Ranken immobilisieren (1,5 s). |

**Dornenwächter** – Gift und Ranken – Hammer, Entfesseln, Entfesselungs-Rufe.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Dornenhaut | +120 Zähigkeit. |
| 2 | Neben | Giftsaft | Schutz halten 20 % länger. +60 Kraft. |
| 3 | Neben | Wurzelstand | +120 Zustandsschaden. |
| 1 | Haupt | Hammerschmied | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Lauter Ruf | Entfesselungs-Ruf-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Entfesselungs-Ruf-Fertigkeiten. |
| 1 | Haupt | Entfesselte Wut | Elite-Mechanik (F5) einsetzen: Schutz für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Giftdorn | +10 % Schaden gegen Ziele mit Vergiftung. |
| 2 | Haupt | Stachelkrone | Kritische Treffer: Vergiftung auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Schutzborke | +7 % Schaden, solange du Schutz hast. Schutz halten 15 % länger. |
| 3 | Haupt | Würgeranke | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Zähe Wurzel | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Dickicht | Alle Zustände halten 15 % länger. +7 % deiner Zähigkeit als Zustandsschaden. |

### Spezialisierung: Sturmfalke

- **Mechanik:** Markieren – F5 Markieren: das Ziel erhält 5 Verwundbarkeit, der Gefährte greift es an; dein nächster Skill 1 wird zum aufgeladenen Präzisionsschuss (15 s).
- **Waffe:** Falkengewehr · **Skilltyp:** Mark · **Palette/Sound-Motiv:** Himmel · **Signatur:** sturmfalke_SF_Praezision · **Faktor:** 0,65
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Federschuss | 0,6 s | 0,475 | – | Release/Himmel/S → Impact/Himmel/S | Himmel | Grundangriff auf bis zu 30 m; Verwundbarkeit (1). Projektil-Finisher. |
| W1.2 | Federschuss · Folgeschuss | 0,6 s | 0,475 | – | Release/Himmel/S → Impact/Himmel/S | Himmel | Zweites Glied der Kette (64 % Koeffizient). |
| W1.3 | Federschuss · Abschlussschuss | 0,6 s | 0,665 | – | Release/Himmel/S → Impact/Himmel/S | Himmel | Kettenabschluss: +40 % Schaden, 3 Verwundbarkeit. |
| W2 | Salve | 0,5 s | 0,314 ×3 | 8 s | Release/Himmel/M → Impact/Himmel/M | Himmel | Drei schnelle Geschosse; je Treffer Verwundbarkeit. |
| W3 | Sprungschuss | 0,3 s | 0,374 | 12 s | Release/Himmel/M → Impact/Himmel/M | Himmel | Schuss aus der Ausweichbewegung; Eile und Wut für dich. |
| W4 | Fesselschuss | 0,5 s | 0,486 | 18 s | Release/Himmel/M → Impact/Himmel/M | Himmel | Kontrollschuss: Immobilisierung (2 s) und Schwäche. |
| W5 | Sturmhagel | 0,75 s | 0,448 ×3 | 25 s | Sigil/Himmel/L → Pillar/Himmel/L | Himmel | Großer Flächen-Skill am Zielort (3 Wellen), Verwundbarkeit (2). Explosions-Finisher. |
| Typ | Zeichen der Rast | 0,75 s | 0 | 25 s | Release/Himmel/M → Aura/Himmel/M | Himmel | Markierung: Heilung; Wut für dich. |
| Typ | Zeichen der Beute | 0,25 s | 0,224 | 25 s | Sigil/Himmel/M → Pillar/Himmel/M | Himmel | Markierung: markiert den Zielbereich – Verwundbarkeit; Schussfenster für Präzisionsschüsse. |
| Typ | Zeichen der Schwäche | 0,25 s | 0,224 | 30 s | Sigil/Himmel/M → Pillar/Himmel/M | Himmel | Markierung: markiert den Zielbereich – Schwäche; Schussfenster für Präzisionsschüsse. |
| Typ | Zeichen des Blendens | 0,25 s | 0,224 | 35 s | Sigil/Himmel/M → Pillar/Himmel/M | Himmel | Markierung: markiert den Zielbereich – Blindheit; Schussfenster für Präzisionsschüsse. |
| Typ | Zeichen des Sturms | 0,75 s | 0,897 | 90 s | Shockwave/Himmel/Elite → Impact/Himmel/Elite | Himmel | Elite (Markierung): Verwundbarkeit (4) an Gegnern um dich, Wut und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Präzisionsschuss | 1 s | 1,943 | – | Release/Himmel/L → Impact/Himmel/L | Himmel | Aufgeladener Schuss auf das markierte Ziel; durchschlägt Verteidigung (Explosions-Finisher). |
| Mech | Falkenmarke | 0 s | 0,149 | – | Release/Himmel/M → Impact/Himmel/M | Himmel | Markiert das Ziel (Zielstrahl). |

**Sturmfalke** – Federn und Zielstrahl – Gewehr, Markieren, Markierungen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Scharfer Blick | +120 Präzision. |
| 2 | Neben | Höhenwind | Wut halten 20 % länger. +60 Wildheit. |
| 3 | Neben | Ruhige Hand | +120 Kraft. |
| 1 | Haupt | Gewehrmeister | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Spurenleser | Markierung-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Markierung-Fertigkeiten. |
| 1 | Haupt | Falkenauge | Elite-Mechanik (F5) einsetzen: Wut für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Offene Flanke | +10 % Schaden gegen Ziele mit Verwundbarkeit. |
| 2 | Haupt | Federkiel | Kritische Treffer: Verwundbarkeit auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Jagdwut | +7 % Schaden, solange du Wut hast. Wut halten 15 % länger. |
| 3 | Haupt | Fangschuss | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Ausweichflug | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Sturmfeder | Alle Zustände halten 15 % länger. +7 % deiner Präzision als Kraft. |

## Elementarweber

Leichte Rüstung · vier Einstimmungen · Stab, Zepter + Fokus, Dolch + Dolch · Glyphen, Beschwörungen, Arkan.

**Mechanik:** Einstimmungen: F1 Feuer, F2 Wasser, F3 Luft, F4 Erde tauschen die Waffenskills 1–5 (10 s Abklingzeit der verlassenen Einstimmung). Wechsel lässt ein kurzes Elementarfeld zurück.

### Waffe: Elementarstab (Staff)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| Feuer 1 | Glutkugel | 0,6 s | 1,61 | – | Release/Feuer/L → Impact/Feuer/L | Feuer | Grundangriff der Einstimmung. |
| Feuer 2 | Lavaquell | 0,5 s | 2,3 | 6 s | Sigil/Feuer/L → Pillar/Feuer/L | Feuer | Flächenangriff. |
| Feuer 3 | Flammenwall | 0,75 s | 0,69 | 18 s | Sigil/Feuer/M → GroundCircle/Feuer/M | Feuer | Elementarfeld am Zielort (6 s). |
| Feuer 4 | Brandschritt | 0,25 s | 0,92 | 20 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Flammen-Aura: 3 Macht, Brennen um dich. |
| Feuer 5 | Sternenglut | 1 s | 3,68 ×3 | 30 s | Sigil/Feuer/L → Pillar/Feuer/L | Feuer | Starker Elementar-Skill. |
| Wasser 1 | Wasserpfeil | 0,6 s | 1,12 | – | Release/Wasser/M → Impact/Wasser/M | Wasser | Grundangriff der Einstimmung. |
| Wasser 2 | Eisregen | 0,5 s | 1,6 | 6 s | Sigil/Wasser/L → Pillar/Wasser/L | Wasser | Flächenangriff. |
| Wasser 3 | Heilbrunnen | 0,75 s | 0,48 | 18 s | Sigil/Wasser/M → GroundCircle/Wasser/M | Wasser | Elementarfeld am Zielort (6 s). |
| Wasser 4 | Frostkegel | 0,25 s | 0,64 | 20 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Heilt, reinigt einen Zustand, Regeneration. |
| Wasser 5 | Gezeitenregen | 1 s | 1,28 ×3 | 30 s | Sigil/Wasser/L → Pillar/Wasser/L | Wasser | Heilender Regen: heilt Verbündete im Bereich. |
| Luft 1 | Funkenbogen | 0,6 s | 1,54 | – | Release/Luft/L → Impact/Luft/L | Luft | Grundangriff der Einstimmung. |
| Luft 2 | Kugelblitz | 0,5 s | 2,2 ×3 | 6 s | Sigil/Luft/L → Pillar/Luft/L | Luft | Flächenangriff. |
| Luft 3 | Sturmfeld | 0,75 s | 0,66 | 18 s | Sigil/Luft/M → GroundCircle/Luft/M | Luft | Elementarfeld am Zielort (6 s). |
| Luft 4 | Windstoß | 0,25 s | 0,88 | 20 s | Shockwave/Luft/M → Impact/Luft/M | Luft | Windstoß: betäubt, Eile und Wut für dich. |
| Luft 5 | Gewittersturm | 1 s | 3,52 ×3 | 30 s | Sigil/Luft/L → Pillar/Luft/L | Luft | Starker Elementar-Skill. |
| Erde 1 | Steinsplitter | 0,6 s | 1,26 | – | Release/Erde/M → Impact/Erde/M | Erde | Grundangriff der Einstimmung. |
| Erde 2 | Schlammgrube | 0,5 s | 1,8 | 6 s | Sigil/Erde/L → Pillar/Erde/L | Erde | Flächenangriff. |
| Erde 3 | Erdwall | 0,75 s | 0,54 | 18 s | Sigil/Erde/M → Pillar/Erde/M | Erde | Elementarfeld am Zielort (6 s). |
| Erde 4 | Erdfessel | 0,25 s | 0,72 | 20 s | Shockwave/Erde/M → Impact/Erde/M | Erde | Erdfessel: Immobilisierung, Schutz für dich. |
| Erde 5 | Bergbeben | 1 s | 2,88 ×3 | 30 s | Sigil/Erde/L → Pillar/Erde/L | Erde | Starker Elementar-Skill. |

### Waffe: Zepter & Fokus (ScepterFocus)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| Feuer 1 | Glutfaden | 0,6 s | 3,22 | – | Release/Feuer/L → Impact/Feuer/L | Feuer | Grundangriff der Einstimmung. |
| Feuer 2 | Zündfunke | 0,5 s | 4,6 | 7 s | Sigil/Feuer/L → Pillar/Feuer/L | Feuer | Flächenangriff. |
| Feuer 3 | Feuerstrahl | 0,75 s | 1,38 | 18 s | Sigil/Feuer/M → GroundCircle/Feuer/M | Feuer | Elementarfeld am Zielort (6 s). |
| Feuer 4 | Flammenschild | 0,25 s | 0,828 | 20 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Flammen-Aura: 3 Macht, Brennen um dich. |
| Feuer 5 | Feuerring | 1 s | 3,312 ×3 | 30 s | Sigil/Feuer/L → Pillar/Feuer/L | Feuer | Starker Elementar-Skill. |
| Wasser 1 | Wassernadel | 0,6 s | 2,24 | – | Release/Wasser/L → Impact/Wasser/L | Wasser | Grundangriff der Einstimmung. |
| Wasser 2 | Quellschlag | 0,5 s | 3,2 | 7 s | Sigil/Wasser/L → Pillar/Wasser/L | Wasser | Flächenangriff. |
| Wasser 3 | Nebelhauch | 0,75 s | 0,96 | 18 s | Sigil/Wasser/M → GroundCircle/Wasser/M | Wasser | Elementarfeld am Zielort (6 s). |
| Wasser 4 | Wellenschutz | 0,25 s | 0,576 | 20 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Heilt, reinigt einen Zustand, Regeneration. |
| Wasser 5 | Eiswall | 1 s | 1,152 ×3 | 30 s | Sigil/Wasser/L → Pillar/Wasser/L | Wasser | Heilender Regen: heilt Verbündete im Bereich. |
| Luft 1 | Blitznadel | 0,6 s | 3,08 | – | Release/Luft/L → Impact/Luft/L | Luft | Grundangriff der Einstimmung. |
| Luft 2 | Donnerkeil | 0,5 s | 4,4 ×3 | 7 s | Sigil/Luft/L → Pillar/Luft/L | Luft | Flächenangriff. |
| Luft 3 | Funkenregen | 0,75 s | 1,32 | 18 s | Sigil/Luft/M → GroundCircle/Luft/M | Luft | Elementarfeld am Zielort (6 s). |
| Luft 4 | Windmantel | 0,25 s | 0,792 | 20 s | Shockwave/Luft/M → Impact/Luft/M | Luft | Windstoß: betäubt, Eile und Wut für dich. |
| Luft 5 | Sturmauge | 1 s | 3,168 ×3 | 30 s | Sigil/Luft/L → Pillar/Luft/L | Luft | Starker Elementar-Skill. |
| Erde 1 | Kieselwurf | 0,6 s | 2,52 | – | Release/Erde/L → Impact/Erde/L | Erde | Grundangriff der Einstimmung. |
| Erde 2 | Felsspitze | 0,5 s | 3,6 | 7 s | Sigil/Erde/L → Pillar/Erde/L | Erde | Flächenangriff. |
| Erde 3 | Staubwirbel | 0,75 s | 1,08 | 18 s | Sigil/Erde/M → Pillar/Erde/M | Erde | Elementarfeld am Zielort (6 s). |
| Erde 4 | Steinhaut | 0,25 s | 0,648 | 20 s | Shockwave/Erde/M → Impact/Erde/M | Erde | Erdfessel: Immobilisierung, Schutz für dich. |
| Erde 5 | Felsenkäfig | 1 s | 2,592 ×3 | 30 s | Sigil/Erde/L → Pillar/Erde/L | Erde | Starker Elementar-Skill. |

### Waffe: Doppeldolche (DaggerDagger)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| Feuer 1 | Glutschnitt | 0,5 s | 1,61 | – | Release/Feuer/L → Impact/Feuer/L | Feuer | Grundangriff der Einstimmung. |
| Feuer 2 | Flammenfächer | 0,5 s | 2,3 | 8 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Flächenangriff. |
| Feuer 3 | Feuersprung | 0,25 s | 2,07 | 12 s | Release/Feuer/L → Impact/Feuer/L | Feuer | Sprung zum Ziel. Sprung-Finisher. |
| Feuer 4 | Brandwirbel | 0,25 s | 0,92 | 20 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Flammen-Aura: 3 Macht, Brennen um dich. |
| Feuer 5 | Aschemantel | 1 s | 3,68 | 30 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Starker Elementar-Skill. |
| Wasser 1 | Eisschnitt | 0,5 s | 1,12 | – | Release/Wasser/M → Impact/Wasser/M | Wasser | Grundangriff der Einstimmung. |
| Wasser 2 | Frostfächer | 0,5 s | 1,6 | 8 s | Shockwave/Wasser/L → Impact/Wasser/L | Wasser | Flächenangriff. |
| Wasser 3 | Gischtsprung | 0,25 s | 1,44 | 12 s | Release/Wasser/M → Impact/Wasser/M | Wasser | Sprung zum Ziel. Sprung-Finisher. |
| Wasser 4 | Heilwirbel | 0,25 s | 0,64 | 20 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Heilt, reinigt einen Zustand, Regeneration. |
| Wasser 5 | Kälteschleier | 1 s | 1,28 | 30 s | Shockwave/Wasser/L → Impact/Wasser/L | Wasser | Heilender Regen: heilt Verbündete im Bereich. |
| Luft 1 | Blitzschnitt | 0,5 s | 1,54 | – | Release/Luft/L → Impact/Luft/L | Luft | Grundangriff der Einstimmung. |
| Luft 2 | Funkenfächer | 0,5 s | 2,2 ×3 | 8 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Flächenangriff. |
| Luft 3 | Sturmsprung | 0,25 s | 1,98 | 12 s | Release/Luft/L → Impact/Luft/L | Luft | Sprung zum Ziel. Sprung-Finisher. |
| Luft 4 | Wirbelsturm | 0,25 s | 0,88 | 20 s | Shockwave/Luft/M → Impact/Luft/M | Luft | Windstoß: betäubt, Eile und Wut für dich. |
| Luft 5 | Donnerhall | 1 s | 3,52 | 30 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Starker Elementar-Skill. |
| Erde 1 | Steinschnitt | 0,5 s | 1,26 | – | Release/Erde/M → Impact/Erde/M | Erde | Grundangriff der Einstimmung. |
| Erde 2 | Erdfächer | 0,5 s | 1,8 | 8 s | Shockwave/Erde/L → Impact/Erde/L | Erde | Flächenangriff. |
| Erde 3 | Felssprung | 0,25 s | 1,62 | 12 s | Release/Erde/L → Impact/Erde/L | Erde | Sprung zum Ziel. Sprung-Finisher. |
| Erde 4 | Sandwirbel | 0,25 s | 0,72 | 20 s | Shockwave/Erde/M → Impact/Erde/M | Erde | Erdfessel: Immobilisierung, Schutz für dich. |
| Erde 5 | Erdpanzer | 1 s | 2,88 | 30 s | Shockwave/Erde/L → Impact/Erde/L | Erde | Starker Elementar-Skill. |

### Heilung, Hilfe, Elite

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| Heal · Glyph | Glyphe der Erneuerung | 0,75 s | 0 | 25 s | Release/Arkan/M → Aura/Arkan/M | Arkan | Glyphe: Heilung; je Einstimmung Zusatz (Feuer Macht, Wasser Regeneration, Luft Eile, Erde Schutz). |
| Heal | Quellströmung | 0,5 s | 0 | 20 s | Release/Wasser/M → Aura/Wasser/M | Wasser | Heilt und erzeugt ein Wasserfeld. |
| Heal | Zaubertau | 1 s | 0 | 30 s | Release/Wasser/M → Aura/Wasser/M | Wasser | Starke Heilung, entfernt 2 Zustände. |
| Utility · Glyph | Glyphe der Kraft | 0 s | 0 | 25 s | Shockwave/Arkan/L → Aura/Arkan/L | Arkan | Glyphe: je Einstimmung Macht, Regeneration, Wut oder Schutz für dich und Verbündete. |
| Utility · Glyph | Glyphe der Stärke | 0 s | 0 | 30 s | Release/Arkan/M → Aura/Arkan/M | Arkan | Glyphe: Stabilität und Entschlossenheit; je Einstimmung Zusatz. |
| Utility · Glyph | Glyphe des Sturms | 0,75 s | 0,8 ×8 | 40 s | Sigil/Arkan/L → Pillar/Arkan/L | Arkan | Glyphe: Sturm am Zielort (8 Treffer), Element nach Einstimmung. |
| Utility · Conjure | Glutbeil beschwören | 0,5 s | 0 | 40 s | Release/Feuer/M → Aura/Feuer/M | Feuer | Beschwörung: Glutbeil ersetzt die Waffenskills für 20 s. |
| Utility · Conjure | Frostbogen beschwören | 0,5 s | 0 | 40 s | Release/Frost/M → Aura/Frost/M | Frost | Beschwörung: Frostbogen ersetzt die Waffenskills für 20 s. |
| Utility · Conjure | Blitzhammer beschwören | 0,5 s | 0 | 40 s | Release/Luft/M → Aura/Luft/M | Luft | Beschwörung: Blitzhammer ersetzt die Waffenskills für 20 s. |
| Utility · Arcane | Blitzschritt | 0 s | 0,6 | 25 s | Shockwave/Luft/M → Impact/Luft/M | Luft | Arkan: teleportiert dich 12 m nach vorn; Feinde am Ziel werden betäubt. |
| Utility · Arcane | Arkaner Schild | 0 s | 0 | 40 s | Release/Arkan/M → Aura/Arkan/M | Arkan | Arkan: Aegis und Schutz; weicht 2 s aus. |
| Utility · Arcane | Arkane Explosion | 0,25 s | 2,4 | 20 s | Shockwave/Arkan/L → Impact/Arkan/L | Arkan | Arkan: Explosion um dich (Explosions-Finisher, Combo-Ende). |
| Elite | Elementarsturm | 1,5 s | 7 | 90 s | Sigil/Arkan/Elite → Pillar/Arkan/Elite | Arkan | Elite: alle vier Elemente schlagen am Zielort ein – Brennen, Kälte, Verwundbarkeit, Blutung; Explosions-Finisher. |
| Elite | Elementargestalt | 0,5 s | 0 | 120 s | Release/Arkan/Elite → Aura/Arkan/Elite | Arkan | Elite: 15 s Stabilität, Schnelligkeit, Wut und 10 Macht. |

### Mechanik-Skills

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| F | Glutbeilhieb | 0,5 s | 1,4 | – | Release/Feuer/S → Impact/Feuer/S | Feuer | Beschworene Waffe. |
| F | Beilwurf | 0,5 s | 1,9 | 9 s | Release/Feuer/L → Impact/Feuer/L | Feuer | Beschworene Waffe. |
| F | Feuerspur | 0,5 s | 2,4 | 12 s | Release/Feuer/L → Impact/Feuer/L | Feuer | Beschworene Waffe. |
| F | Glutsprung | 0,5 s | 2,9 | 15 s | Release/Feuer/L → Impact/Feuer/L | Feuer | Beschworene Waffe. |
| F | Flammenausbruch | 0,5 s | 3,4 | 18 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Beschworene Waffe. |
| F | Frostpfeil | 0,5 s | 1,4 | – | Release/Frost/S → Impact/Frost/S | Frost | Beschworene Waffe. |
| F | Eissalve | 0,5 s | 1,9 | 9 s | Release/Frost/L → Impact/Frost/L | Frost | Beschworene Waffe. |
| F | Frostfessel | 0,5 s | 2,4 | 12 s | Release/Frost/L → Impact/Frost/L | Frost | Beschworene Waffe. |
| F | Kältepfeil | 0,5 s | 2,9 | 15 s | Release/Frost/L → Impact/Frost/L | Frost | Beschworene Waffe. |
| F | Eissturm | 0,5 s | 3,4 | 18 s | Shockwave/Frost/L → Impact/Frost/L | Frost | Beschworene Waffe. |
| F | Hammerschlag | 0,5 s | 1,4 | – | Release/Luft/S → Impact/Luft/S | Luft | Beschworene Waffe. |
| F | Donnerwurf | 0,5 s | 1,9 | 9 s | Release/Luft/L → Impact/Luft/L | Luft | Beschworene Waffe. |
| F | Blitzwelle | 0,5 s | 2,4 | 12 s | Release/Luft/L → Impact/Luft/L | Luft | Beschworene Waffe. |
| F | Funkenhieb | 0,5 s | 2,9 | 15 s | Release/Luft/L → Impact/Luft/L | Luft | Beschworene Waffe. |
| F | Sturmschlag | 0,5 s | 3,4 | 18 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Beschworene Waffe. |

### Eigenschaftslinien

**Glutlehre** – Feuer, Brennen und Kraft.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Brennende Kraft | +100 Kraft. |
| 2 | Neben | Glimmende Spur | Brennen hält 20 % länger. |
| 3 | Neben | Hitzeschild | +120 Zustandsschaden. |
| 1 | Haupt | Flammenwechsel | Einstimmung wechseln gewährt 3 Macht (Abklingzeit 3 s). |
| 1 | Haupt | Stabkunde | Stab-Fertigkeiten laden 20 % schneller auf; +5 % Stab-Schaden. |
| 1 | Haupt | Funkenschlag | Kritische Treffer setzen 2 s in Brand (Abklingzeit 3 s). |
| 2 | Haupt | Glühender Zorn | +10 % Schaden gegen brennende Ziele. |
| 2 | Haupt | Beschwörerglut | Beschwörungen laden 20 % schneller auf; +10 % Beschwörungs-Schaden. |
| 2 | Haupt | Feuerseele | +7 % Schaden mit Macht. |
| 3 | Haupt | Aschenwind | Brennen hält 33 % länger. |
| 3 | Haupt | Flammenherz | Erhalte Kraft in Höhe von 8 % deines Zustandsschadens. |
| 3 | Haupt | Sternenfall | Elitefertigkeit lädt 20 % schneller auf; +10 % Schaden gegen Ziele unter 50 %. |

**Sturmlehre** – Luft, kritische Treffer und Tempo.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Scharfer Wind | +100 Präzision. |
| 2 | Neben | Blitzschnell | Eile hält 25 % länger. |
| 3 | Neben | Donnergewalt | +120 Wildheit. |
| 1 | Haupt | Sturmwechsel | Einstimmung wechseln gewährt 4 s Wut (Abklingzeit 5 s). |
| 1 | Haupt | Dolchtanz | Dolch-Fertigkeiten laden 20 % schneller auf; +5 % Dolch-Schaden. |
| 1 | Haupt | Statische Ladung | +10 % Schaden gegen verwundbare Ziele. |
| 2 | Haupt | Arkane Schnelle | Arkan-Fertigkeiten laden 20 % schneller auf. |
| 2 | Haupt | Windgeschwind | Ausweichen gewährt 2 s Schnelligkeit (Abklingzeit 10 s). |
| 2 | Haupt | Gewitterauge | +5 % kritische Trefferchance; +7 % Schaden mit Wut. |
| 3 | Haupt | Blitzableiter | Kritische Treffer: 1 Verwundbarkeit (Abklingzeit 0,5 s). |
| 3 | Haupt | Zepterfokus | Zepter und Fokus: Abklingzeit −20 %, +7 % Schaden. |
| 3 | Haupt | Sturmbringer | +15 % Schaden gegen Ziele unter 50 % Lebenspunkten. |

**Stein- und Quelllehre** – Erde, Wasser, Schutz und Heilung.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Steinhaut | +100 Zähigkeit. |
| 2 | Neben | Quellwasser | +120 Heilkraft. |
| 3 | Neben | Erdverbunden | +120 Vitalität. |
| 1 | Haupt | Glyphenkunde | Glyphen laden 20 % schneller auf. |
| 1 | Haupt | Gezeitenwechsel | Einstimmung wechseln heilt dich (Abklingzeit 5 s). |
| 1 | Haupt | Felsenpanzer | Schaden erleiden gewährt 3 s Schutz (Abklingzeit 20 s). |
| 2 | Haupt | Ausdauernde Segen | Alle Segen halten 20 % länger. |
| 2 | Haupt | Erdblut | Kritische Treffer: 1 Blutung (Abklingzeit 1 s). |
| 2 | Haupt | Heilende Ströme | Heilfertigkeiten laden 33 % schneller auf. |
| 3 | Haupt | Bergesruhe | Erhalte Kraft in Höhe von 8 % deiner Zähigkeit. |
| 3 | Haupt | Lebensquell | Ausweichen heilt dich (Abklingzeit 3 s). |
| 3 | Haupt | Unbeugsam | +7 % Schaden mit Schutz; Schutz hält 25 % länger. |

### Spezialisierung: Sturmrufer

- **Mechanik:** Überladen – Überladen: nach 5 s in einer Einstimmung deren Taste erneut drücken (oder F5) – Überlade-Kanal mit Flächenwirkung des Elements; danach lädt diese Einstimmung 5 s länger.
- **Waffe:** Zepter & Kriegshorn · **Skilltyp:** Shout · **Palette/Sound-Motiv:** Gewitter · **Signatur:** sturmrufer_UL_2 · **Faktor:** 2
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1/1 | Glutfaden | 0,6 s | 3,22 | – | Release/Feuer/L → Impact/Feuer/L | Feuer | Grundangriff der Einstimmung. |
| W2/1 | Zündfunke | 0,5 s | 4,6 | 7 s | Sigil/Feuer/L → Pillar/Feuer/L | Feuer | Flächenangriff. |
| W3/1 | Feuerstrahl | 0,75 s | 1,38 | 18 s | Sigil/Feuer/M → GroundCircle/Feuer/M | Feuer | Elementarfeld am Zielort (6 s). |
| W4/1 | Glutfanfare | 0,25 s | 1,84 | 20 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Flammen-Aura: 3 Macht, Brennen um dich. |
| W5/1 | Glutsignal | 0,75 s | 6,9 ×2 | 28 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/2 | Wassernadel | 0,6 s | 2,24 | – | Release/Wasser/L → Impact/Wasser/L | Wasser | Grundangriff der Einstimmung. |
| W2/2 | Quellschlag | 0,5 s | 3,2 | 7 s | Sigil/Wasser/L → Pillar/Wasser/L | Wasser | Flächenangriff. |
| W3/2 | Nebelhauch | 0,75 s | 0,96 | 18 s | Sigil/Wasser/M → GroundCircle/Wasser/M | Wasser | Elementarfeld am Zielort (6 s). |
| W4/2 | Gischtfanfare | 0,25 s | 1,28 | 20 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Heilt, entfernt einen Zustand. |
| W5/2 | Gischtsignal | 0,75 s | 4,8 ×2 | 28 s | Shockwave/Wasser/L → Impact/Wasser/L | Wasser | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/3 | Blitznadel | 0,6 s | 3,08 | – | Release/Luft/L → Impact/Luft/L | Luft | Grundangriff der Einstimmung. |
| W2/3 | Donnerkeil | 0,5 s | 4,4 ×3 | 7 s | Sigil/Luft/L → Pillar/Luft/L | Luft | Flächenangriff. |
| W3/3 | Funkenregen | 0,75 s | 1,32 | 18 s | Sigil/Luft/M → GroundCircle/Luft/M | Luft | Elementarfeld am Zielort (6 s). |
| W4/3 | Donnerfanfare | 0,25 s | 1,76 | 20 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Windstoß: betäubt, Eile für dich. |
| W5/3 | Donnersignal | 0,75 s | 6,6 ×2 | 28 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/4 | Kieselwurf | 0,6 s | 2,52 | – | Release/Erde/L → Impact/Erde/L | Erde | Grundangriff der Einstimmung. |
| W2/4 | Felsspitze | 0,5 s | 3,6 | 7 s | Sigil/Erde/L → Pillar/Erde/L | Erde | Flächenangriff. |
| W3/4 | Staubwirbel | 0,75 s | 1,08 | 18 s | Sigil/Erde/M → Pillar/Erde/M | Erde | Elementarfeld am Zielort (6 s). |
| W4/4 | Felsfanfare | 0,25 s | 1,44 | 20 s | Shockwave/Erde/M → Impact/Erde/M | Erde | Erdfessel: Immobilisierung, Schutz für dich. |
| W5/4 | Felssignal | 0,75 s | 5,4 ×2 | 28 s | Shockwave/Erde/L → Impact/Erde/L | Erde | Starker Elementar-Skill um dich (Explosions-Finisher). |
| Typ | Schrei der Stille | 0,75 s | 0 | 25 s | Release/Gewitter/M → Aura/Gewitter/M | Gewitter | Ruf: Heilung; Eile für dich. |
| Typ | Schrei des Windes | 0,25 s | 0 | 25 s | Shockwave/Gewitter/L → Aura/Gewitter/L | Gewitter | Ruf: Segen für dich und Verbündete, entfernt 1 Zustand/Zustände. |
| Typ | Schrei der Glut | 0,25 s | 0 | 30 s | Shockwave/Gewitter/L → Aura/Gewitter/L | Gewitter | Ruf: Segen für dich und Verbündete, entfernt 2 Zustand/Zustände. |
| Typ | Schrei der Brandung | 0,25 s | 0 | 35 s | Shockwave/Gewitter/L → Aura/Gewitter/L | Gewitter | Ruf: Segen für dich und Verbündete, entfernt 3 Zustand/Zustände. |
| Typ | Schrei des Gewitters | 0,75 s | 4,8 | 90 s | Shockwave/Gewitter/Elite → Impact/Gewitter/Elite | Gewitter | Elite (Ruf): Verwundbarkeit (4) an Gegnern um dich, Eile und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Überladung: Feuer | 3 s | 2 ×6 | – | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Überlade-Kanal (3 s, 6 Treffer um dich): Brennen. |
| Mech | Überladung: Wasser | 3 s | 2 ×6 | – | Shockwave/Wasser/L → Impact/Wasser/L | Wasser | Überlade-Kanal (3 s, 6 Treffer um dich): Kühle; heilt Verbündete. |
| Mech | Überladung: Luft | 3 s | 2 ×6 | – | Shockwave/Luft/L → Impact/Luft/L | Luft | Überlade-Kanal (3 s, 6 Treffer um dich): Verwundbarkeit. |
| Mech | Überladung: Erde | 3 s | 2 ×6 | – | Shockwave/Erde/L → Impact/Erde/L | Erde | Überlade-Kanal (3 s, 6 Treffer um dich): Blutung; Schutz für dich. |

**Sturmrufer** – Gewitter und Donner – Zepter & Kriegshorn, Überladen, Schreie.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Donnerstimme | +120 Kraft. |
| 2 | Neben | Sturmlauf | Eile halten 20 % länger. +60 Zähigkeit. |
| 3 | Neben | Gewitterhaut | +120 Konzentration. |
| 1 | Haupt | Hornbläser | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Laute Schreie | Ruf-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Ruf-Fertigkeiten. |
| 1 | Haupt | Nachhall | Elite-Mechanik (F5) einsetzen: Eile für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Blitzrisse | +10 % Schaden gegen Ziele mit Verwundbarkeit. |
| 2 | Haupt | Funkenschlag | Kritische Treffer: Verwundbarkeit auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Rückenwind | +7 % Schaden, solange du Eile hast. Eile halten 15 % länger. |
| 3 | Haupt | Letzter Donner | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Windschritt | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Hagelschauer | Alle Zustände halten 15 % länger. +7 % deiner Kraft als Konzentration. |

### Spezialisierung: Weber

- **Mechanik:** Doppel-Einstimmung – Doppel-Einstimmung: Hauptelement = gewählte, Nebenelement = vorherige Einstimmung; Skill 3 wird zum Kombi-Skill beider Elemente (6 Kombinationen).
- **Waffe:** Degen & Fokus · **Skilltyp:** Stance · **Palette/Sound-Motiv:** Zwielicht · **Signatur:** weber_KO_2 · **Faktor:** 0,9
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1/1 | Glutdegen | 0,5 s | 1,49 | – | Release/Feuer/S → Impact/Feuer/S | Feuer | Grundangriff der Einstimmung. |
| W2/1 | Glutstich | 0,5 s | 2,07 | 7 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Flächenangriff. |
| W3/1 | Gluttanz | 0,3 s | 1,863 | 14 s | Release/Feuer/L → Impact/Feuer/L | Feuer | Sprung zum Ziel. Sprung-Finisher. |
| W4/1 | Flammenschild | 0,25 s | 0,828 | 20 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Flammen-Aura: 3 Macht, Brennen um dich. |
| W5/1 | Feuerring | 1 s | 3,312 ×3 | 30 s | Sigil/Feuer/L → Pillar/Feuer/L | Feuer | Starker Elementar-Skill. |
| W1/2 | Gischtdegen | 0,5 s | 1,037 | – | Release/Wasser/M → Impact/Wasser/M | Wasser | Grundangriff der Einstimmung. |
| W2/2 | Gischtstich | 0,5 s | 1,44 | 7 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Flächenangriff. |
| W3/2 | Gischttanz | 0,3 s | 1,296 | 14 s | Release/Wasser/M → Impact/Wasser/M | Wasser | Sprung zum Ziel. Sprung-Finisher. |
| W4/2 | Wellenschutz | 0,25 s | 0,576 | 20 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Heilt, reinigt einen Zustand, Regeneration. |
| W5/2 | Eiswall | 1 s | 1,152 ×3 | 30 s | Sigil/Wasser/L → Pillar/Wasser/L | Wasser | Heilender Regen: heilt Verbündete im Bereich. |
| W1/3 | Donnerdegen | 0,5 s | 1,426 | – | Release/Luft/M → Impact/Luft/M | Luft | Grundangriff der Einstimmung. |
| W2/3 | Donnerstich | 0,5 s | 1,98 ×3 | 7 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Flächenangriff. |
| W3/3 | Donnertanz | 0,3 s | 1,782 | 14 s | Release/Luft/L → Impact/Luft/L | Luft | Sprung zum Ziel. Sprung-Finisher. |
| W4/3 | Windmantel | 0,25 s | 0,792 | 20 s | Shockwave/Luft/M → Impact/Luft/M | Luft | Windstoß: betäubt, Eile und Wut für dich. |
| W5/3 | Sturmauge | 1 s | 3,168 ×3 | 30 s | Sigil/Luft/L → Pillar/Luft/L | Luft | Starker Elementar-Skill. |
| W1/4 | Felsdegen | 0,5 s | 1,166 | – | Release/Erde/M → Impact/Erde/M | Erde | Grundangriff der Einstimmung. |
| W2/4 | Felsstich | 0,5 s | 1,62 | 7 s | Shockwave/Erde/L → Impact/Erde/L | Erde | Flächenangriff. |
| W3/4 | Felstanz | 0,3 s | 1,458 | 14 s | Release/Erde/M → Impact/Erde/M | Erde | Sprung zum Ziel. Sprung-Finisher. |
| W4/4 | Steinhaut | 0,25 s | 0,648 | 20 s | Shockwave/Erde/M → Impact/Erde/M | Erde | Erdfessel: Immobilisierung, Schutz für dich. |
| W5/4 | Felsenkäfig | 1 s | 2,592 ×3 | 30 s | Sigil/Erde/L → Pillar/Erde/L | Erde | Starker Elementar-Skill. |
| Typ | Haltung der Fäden | 0,75 s | 0 | 25 s | Release/Zwielicht/M → Aura/Zwielicht/M | Zwielicht | Haltung: Heilung; Wut für dich. |
| Typ | Haltung der Glut | 0 s | 0 | 25 s | Release/Zwielicht/M → Aura/Zwielicht/M | Zwielicht | Haltung: lange Stärkung (Wut, Wut), dafür 2 Verwundbarkeit für dich. |
| Typ | Haltung der Woge | 0 s | 0 | 30 s | Release/Zwielicht/M → Aura/Zwielicht/M | Zwielicht | Haltung: lange Stärkung (Wut, Eile), dafür 2 Verwundbarkeit für dich. |
| Typ | Haltung des Sturms | 0 s | 0 | 35 s | Release/Zwielicht/M → Aura/Zwielicht/M | Zwielicht | Haltung: lange Stärkung (Wut, Widerstand), dafür 2 Verwundbarkeit für dich. |
| Typ | Haltung der Vier | 0,75 s | 2,16 | 90 s | Shockwave/Zwielicht/Elite → Impact/Zwielicht/Elite | Zwielicht | Elite (Haltung): Brennen (4) an Gegnern um dich, Wut und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Dampfstoß | 0,4 s | 1,98 | 10 s | Shockwave/Zwielicht/L → Impact/Zwielicht/L | Zwielicht | Kombi-Skill (Brennen + Kühle): wirkt beide Elemente zugleich um dich. |
| Mech | Funkenfeuer | 0,4 s | 1,98 | 10 s | Shockwave/Zwielicht/L → Impact/Zwielicht/L | Zwielicht | Kombi-Skill (Brennen + Verwundbarkeit): wirkt beide Elemente zugleich um dich. |
| Mech | Lavasprung | 0,4 s | 1,98 | 10 s | Release/Zwielicht/L → Impact/Zwielicht/L | Zwielicht | Kombi-Skill (Brennen + Blutung): wirkt beide Elemente zugleich um dich. |
| Mech | Hagelwind | 0,4 s | 1,98 | 10 s | Shockwave/Zwielicht/L → Impact/Zwielicht/L | Zwielicht | Kombi-Skill (Kühle + Verwundbarkeit): wirkt beide Elemente zugleich um dich. |
| Mech | Schlammflut | 0,4 s | 1,98 | 10 s | Shockwave/Zwielicht/L → Impact/Zwielicht/L | Zwielicht | Kombi-Skill (Kühle + Blutung): wirkt beide Elemente zugleich um dich. |
| Mech | Sandsturm | 0,4 s | 1,98 | 10 s | Shockwave/Zwielicht/L → Impact/Zwielicht/L | Zwielicht | Kombi-Skill (Verwundbarkeit + Blindheit): wirkt beide Elemente zugleich um dich. |

**Weber** – Verwobene Elemente – Schwert & Fokus, Doppel-Einstimmung, Haltungen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Fadenkunde | +120 Kraft. |
| 2 | Neben | Doppelklinge | Wut halten 20 % länger. +60 Wildheit. |
| 3 | Neben | Zwiefache Flamme | +120 Präzision. |
| 1 | Haupt | Degenkunst | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Haltungswechsel | Haltung-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Haltung-Fertigkeiten. |
| 1 | Haupt | Verwobener Strom | Elite-Mechanik (F5) einsetzen: Wut für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Zwei Herzen | +10 % Schaden gegen Ziele mit Brennen. |
| 2 | Haupt | Bänderschnitt | Kritische Treffer: Brennen auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Glimmender Faden | +7 % Schaden, solange du Wut hast. Wut halten 15 % länger. |
| 3 | Haupt | Letzte Masche | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Gewandter Schritt | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Elementares Muster | Alle Zustände halten 15 % länger. +7 % deiner Kraft als Präzision. |

### Spezialisierung: Katalysator

- **Mechanik:** Elementarkern – F5 Elementarkern: Treffer laden Energie (max. 30); eine Sphäre kostet 10 und gibt dir und Verbündeten Segen nach Einstimmung (Feuer Macht, Wasser Regeneration, Luft Wut, Erde Schutz), 10 s Abklingzeit.
- **Waffe:** Jadehammer · **Skilltyp:** Augment · **Palette/Sound-Motiv:** Jade · **Signatur:** katalysator_KE_0 · **Faktor:** 0,85
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1/1 | Gluthammer | 0,5 s | 1,408 | – | Release/Feuer/S → Impact/Feuer/S | Feuer | Grundangriff der Einstimmung. |
| W2/1 | Glutschmiede | 0,5 s | 1,955 | 7 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Flächenangriff. |
| W3/1 | Glutring | 0,3 s | 1,76 | 14 s | Release/Feuer/L → Impact/Feuer/L | Feuer | Sprung zum Ziel. Sprung-Finisher. |
| W4/1 | Glutwelle | 0,25 s | 0,782 | 20 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Flammen-Aura: 3 Macht, Brennen um dich. |
| W5/1 | Glutbeben | 0,75 s | 2,932 ×2 | 28 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/2 | Gischthammer | 0,5 s | 0,979 | – | Release/Wasser/M → Impact/Wasser/M | Wasser | Grundangriff der Einstimmung. |
| W2/2 | Gischtschmiede | 0,5 s | 1,36 | 7 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Flächenangriff. |
| W3/2 | Gischtring | 0,3 s | 1,224 | 14 s | Release/Wasser/M → Impact/Wasser/M | Wasser | Sprung zum Ziel. Sprung-Finisher. |
| W4/2 | Gischtwelle | 0,25 s | 0,544 | 20 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Heilt, entfernt einen Zustand. |
| W5/2 | Gischtbeben | 0,75 s | 2,04 ×2 | 28 s | Shockwave/Wasser/L → Impact/Wasser/L | Wasser | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/3 | Donnerhammer | 0,5 s | 1,346 | – | Release/Luft/M → Impact/Luft/M | Luft | Grundangriff der Einstimmung. |
| W2/3 | Donnerschmiede | 0,5 s | 1,87 ×3 | 7 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Flächenangriff. |
| W3/3 | Donnerring | 0,3 s | 1,683 | 14 s | Release/Luft/L → Impact/Luft/L | Luft | Sprung zum Ziel. Sprung-Finisher. |
| W4/3 | Donnerwelle | 0,25 s | 0,748 | 20 s | Shockwave/Luft/M → Impact/Luft/M | Luft | Windstoß: betäubt, Eile für dich. |
| W5/3 | Donnerbeben | 0,75 s | 2,805 ×2 | 28 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/4 | Felshammer | 0,5 s | 1,102 | – | Release/Erde/M → Impact/Erde/M | Erde | Grundangriff der Einstimmung. |
| W2/4 | Felsschmiede | 0,5 s | 1,53 | 7 s | Shockwave/Erde/L → Impact/Erde/L | Erde | Flächenangriff. |
| W3/4 | Felsring | 0,3 s | 1,377 | 14 s | Release/Erde/M → Impact/Erde/M | Erde | Sprung zum Ziel. Sprung-Finisher. |
| W4/4 | Felswelle | 0,25 s | 0,612 | 20 s | Shockwave/Erde/M → Impact/Erde/M | Erde | Erdfessel: Immobilisierung, Schutz für dich. |
| W5/4 | Felsbeben | 0,75 s | 2,295 ×2 | 28 s | Shockwave/Erde/L → Impact/Erde/L | Erde | Starker Elementar-Skill um dich (Explosions-Finisher). |
| Typ | Verstärkung der Quelle | 0,75 s | 0 | 25 s | Release/Jade/M → Aura/Jade/M | Jade | Verstärkung: Heilung; Macht für dich. |
| Typ | Verstärkung der Kraft | 0 s | 0 | 25 s | Shockwave/Jade/L → Aura/Jade/L | Jade | Verstärkung: Kampfsegen für dich; Explosions-Finisher (Combo-Ende). |
| Typ | Verstärkung des Schilds | 0 s | 0 | 30 s | Shockwave/Jade/L → Aura/Jade/L | Jade | Verstärkung: Kampfsegen für dich; Explosions-Finisher (Combo-Ende). |
| Typ | Verstärkung der Eile | 0 s | 0 | 35 s | Shockwave/Jade/L → Aura/Jade/L | Jade | Verstärkung: Kampfsegen für dich; Explosions-Finisher (Combo-Ende). |
| Typ | Verstärkung der Vier | 0,75 s | 2,04 | 90 s | Shockwave/Jade/Elite → Impact/Jade/Elite | Jade | Elite (Verstärkung): Blutung (4) an Gegnern um dich, Macht und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Glutkern | 0,25 s | 0 | – | Shockwave/Feuer/L → Aura/Feuer/L | Feuer | Elementarkern-Sphäre: Macht für dich und Verbündete im Umkreis (8 m). |
| Mech | Gischtkern | 0,25 s | 0 | – | Shockwave/Wasser/L → Aura/Wasser/L | Wasser | Elementarkern-Sphäre: Regeneration für dich und Verbündete im Umkreis (8 m). |
| Mech | Donnerkern | 0,25 s | 0 | – | Shockwave/Luft/L → Aura/Luft/L | Luft | Elementarkern-Sphäre: Wut für dich und Verbündete im Umkreis (8 m). |
| Mech | Felskern | 0,25 s | 0 | – | Shockwave/Erde/L → Aura/Erde/L | Erde | Elementarkern-Sphäre: Schutz für dich und Verbündete im Umkreis (8 m). |

**Katalysator** – Jade und rotierende Ringe – Hammer, Elementarkern, Verstärkungen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Jadeherz | +120 Kraft. |
| 2 | Neben | Ringkraft | Macht halten 20 % länger. +60 Wildheit. |
| 3 | Neben | Kernhitze | +120 Konzentration. |
| 1 | Haupt | Hammerform | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Verstärkte Segen | Verstärkung-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Verstärkung-Fertigkeiten. |
| 1 | Haupt | Pulsierender Kern | Elite-Mechanik (F5) einsetzen: 5 Macht für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Splitterring | +10 % Schaden gegen Ziele mit Blutung. |
| 2 | Haupt | Zermalmen | Kritische Treffer: Blutung auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Kraftfeld | +7 % Schaden, solange du Macht hast. Macht halten 15 % länger. |
| 3 | Haupt | Brechender Schlag | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Leichter Fuß | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Elementarer Kreislauf | Alle Zustände halten 15 % länger. +7 % deiner Kraft als Konzentration. |

### Spezialisierung: Glutbinder

- **Mechanik:** Erdbruch – F5 Erdbruch: Feuer und Erde verschmelzen – ein Lava-Feld am Ziel (Brennen, Blutung, Feuerfeld, 20 s Abklingzeit). Beschwörungen bleiben 50 % länger.
- **Waffe:** Zepter & Glutdolch · **Skilltyp:** Conjure · **Palette/Sound-Motiv:** Lava · **Signatur:** glutbinder_EB_Lava · **Faktor:** 1
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1/1 | Glutfaden | 0,6 s | 3,22 | – | Release/Feuer/L → Impact/Feuer/L | Feuer | Grundangriff der Einstimmung. |
| W2/1 | Zündfunke | 0,5 s | 4,6 | 7 s | Sigil/Feuer/L → Pillar/Feuer/L | Feuer | Flächenangriff. |
| W3/1 | Feuerstrahl | 0,75 s | 1,38 | 18 s | Sigil/Feuer/M → GroundCircle/Feuer/M | Feuer | Elementarfeld am Zielort (6 s). |
| W4/1 | Glutkralle | 0,25 s | 0,92 | 20 s | Shockwave/Feuer/M → Impact/Feuer/M | Feuer | Flammen-Aura: 3 Macht, Brennen um dich. |
| W5/1 | Glutschlund | 0,75 s | 3,45 ×2 | 28 s | Shockwave/Feuer/L → Impact/Feuer/L | Feuer | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/2 | Wassernadel | 0,6 s | 2,24 | – | Release/Wasser/L → Impact/Wasser/L | Wasser | Grundangriff der Einstimmung. |
| W2/2 | Quellschlag | 0,5 s | 3,2 | 7 s | Sigil/Wasser/L → Pillar/Wasser/L | Wasser | Flächenangriff. |
| W3/2 | Nebelhauch | 0,75 s | 0,96 | 18 s | Sigil/Wasser/M → GroundCircle/Wasser/M | Wasser | Elementarfeld am Zielort (6 s). |
| W4/2 | Gischtkralle | 0,25 s | 0,64 | 20 s | Shockwave/Wasser/M → Impact/Wasser/M | Wasser | Heilt, entfernt einen Zustand. |
| W5/2 | Gischtschlund | 0,75 s | 2,4 ×2 | 28 s | Shockwave/Wasser/L → Impact/Wasser/L | Wasser | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/3 | Blitznadel | 0,6 s | 3,08 | – | Release/Luft/L → Impact/Luft/L | Luft | Grundangriff der Einstimmung. |
| W2/3 | Donnerkeil | 0,5 s | 4,4 ×3 | 7 s | Sigil/Luft/L → Pillar/Luft/L | Luft | Flächenangriff. |
| W3/3 | Funkenregen | 0,75 s | 1,32 | 18 s | Sigil/Luft/M → GroundCircle/Luft/M | Luft | Elementarfeld am Zielort (6 s). |
| W4/3 | Donnerkralle | 0,25 s | 0,88 | 20 s | Shockwave/Luft/M → Impact/Luft/M | Luft | Windstoß: betäubt, Eile für dich. |
| W5/3 | Donnerschlund | 0,75 s | 3,3 ×2 | 28 s | Shockwave/Luft/L → Impact/Luft/L | Luft | Starker Elementar-Skill um dich (Explosions-Finisher). |
| W1/4 | Kieselwurf | 0,6 s | 2,52 | – | Release/Erde/L → Impact/Erde/L | Erde | Grundangriff der Einstimmung. |
| W2/4 | Felsspitze | 0,5 s | 3,6 | 7 s | Sigil/Erde/L → Pillar/Erde/L | Erde | Flächenangriff. |
| W3/4 | Staubwirbel | 0,75 s | 1,08 | 18 s | Sigil/Erde/M → Pillar/Erde/M | Erde | Elementarfeld am Zielort (6 s). |
| W4/4 | Felskralle | 0,25 s | 0,72 | 20 s | Shockwave/Erde/M → Impact/Erde/M | Erde | Erdfessel: Immobilisierung, Schutz für dich. |
| W5/4 | Felsschlund | 0,75 s | 2,7 ×2 | 28 s | Shockwave/Erde/L → Impact/Erde/L | Erde | Starker Elementar-Skill um dich (Explosions-Finisher). |
| Typ | Glutgeist rufen | 0,75 s | 0 | 25 s | Release/Lava/M → Aura/Lava/M | Lava | Beschwörung: Heilung; Macht für dich. |
| Typ | Aschegeist rufen | 0,25 s | 0,5 ×8 | 25 s | Sigil/Lava/M → GroundCircle/Lava/M | Lava | Beschwörung: ruft einen ortsfesten Elementar-Diener (8 s), der Gegner im Umkreis jede Sekunde trifft und brennt (Feuerfeld). |
| Typ | Lavageist rufen | 0,25 s | 0,5 ×10 | 30 s | Sigil/Lava/M → GroundCircle/Lava/M | Lava | Beschwörung: ruft einen ortsfesten Elementar-Diener (10 s), der Gegner im Umkreis jede Sekunde trifft und brennt (Feuerfeld). |
| Typ | Obsidiangeist rufen | 0,25 s | 0,5 ×12 | 35 s | Sigil/Lava/M → GroundCircle/Lava/M | Lava | Beschwörung: ruft einen ortsfesten Elementar-Diener (12 s), der Gegner im Umkreis jede Sekunde trifft und brennt (Feuerfeld). |
| Typ | Vulkangeist rufen | 0,75 s | 2,4 | 90 s | Shockwave/Lava/Elite → Impact/Lava/Elite | Lava | Elite (Beschwörung): Brennen (4) an Gegnern um dich, Macht und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Lavafeld | 0,5 s | 0,7 ×5 | – | Sigil/Lava/L → GroundCircle/Lava/L | Lava | Erdbruch: Lava-Feld am Zielort (5 Treffer) – Brennen und Blutung, Feuerfeld. |

**Glutbinder** – Lava auf Obsidian – Zepter & Dolch, Erdbruch, Beschwörungen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Glutblut | +120 Zustandsschaden. |
| 2 | Neben | Obsidianhaut | Macht halten 20 % länger. +60 Kraft. |
| 3 | Neben | Lavaader | +120 Sachkenntnis. |
| 1 | Haupt | Dolchglut | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Bindekunst | Beschwörung-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Beschwörung-Fertigkeiten. |
| 1 | Haupt | Brodelnder Grund | Elite-Mechanik (F5) einsetzen: 5 Macht für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Glutrisse | +10 % Schaden gegen Ziele mit Brennen. |
| 2 | Haupt | Aschenregen | Kritische Treffer: Brennen auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Schmelzhitze | +7 % Schaden, solange du Macht hast. Macht halten 15 % länger. |
| 3 | Haupt | Vulkanzorn | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Funkenflug | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Glühender Bund | Alle Zustände halten 15 % länger. +7 % deiner Zustandsschaden als Sachkenntnis. |

## Schattenklinge

Mittlere Rüstung · Initiative und Stehlen · Dolch + Dolch, Schwert + Pistole, Kurzbogen · Tricks, Gifte, Täuschungen.

**Mechanik:** Initiative: 12 Punkte, Waffenskills 2–5 kosten Initiative statt Abklingzeit (1 Punkt/s). F1 Stehlen: Schattenschritt zum Ziel und ein gestohlener Skill nach Gegnerart. Tarnung macht Skill 1 zum Tarnungsangriff.

### Waffe: Dolch & Dolch (DaggerDagger)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| 1 | Klingenhieb | 0,45 s | 0,866 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Schneller Dolchhieb. |
| 1.2 | Doppelstich | 0,45 s | 0,866 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Zweites Glied der Kette (56 % Koeffizient). |
| 1.3 | Giftstoß | 0,45 s | 1,213 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Kettenabschluss: +40 % Schaden, 2 Blutung. |
| 2 | Herzstich | 0,5 s | 2,48 | 4 Ini | Release/Schatten/L → Impact/Schatten/L | Schatten | Tiefer Stich; mehr Schaden gegen geschwächte Ziele. |
| 3 | Todeswirbel | 0,25 s | 0,542 ×5 | 4 Ini | Shockwave/Schatten/M → Impact/Schatten/M | Schatten | Doppelwaffen-Skill: wirbelnder Sprung (5 Treffer), weicht aus, Wirbel-Finisher. |
| 4 | Giftstreuer | 0,25 s | 0,62 | 3 Ini | Release/Schatten/M → Impact/Schatten/M | Schatten | Wirft einen Dolch: Gift und Schwäche. |
| 5 | Rauchschleier | 0,25 s | 0 | 6 Ini | Sigil/Schatten/M → GroundCircle/Schatten/M | Schatten | Rauchfeld (4 s) am Zielort, blendet. |
| Tarnung | Hinterhalt | 0,4 s | 3,41 | – | Release/Schatten/L → Impact/Schatten/L | Schatten | Tarnungsangriff: massiver Stich in den Rücken, 5 Verwundbarkeit. |

### Waffe: Schwert & Pistole (SwordPistol)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| 1 | Säbelhieb | 0,5 s | 0,949 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Säbelhieb. |
| 1.2 | Rückhandhieb | 0,5 s | 0,949 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Zweites Glied der Kette (61 % Koeffizient). |
| 1.3 | Fesselhieb | 0,5 s | 1,328 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Kettenabschluss: +40 % Schaden, Verkrüppelung (2 s). |
| 2 | Schattensprung | 0 s | 1,085 | 3 Ini | Release/Schatten/M → Impact/Schatten/M | Schatten | Schattenschritt zum Ziel und Hieb. |
| 3 | Pistolenpeitsche | 0,5 s | 0,465 ×6 | 5 Ini | Release/Schatten/M → Impact/Schatten/M | Schatten | Doppelwaffen-Skill: betäubt, dann 6 schnelle Hiebe. |
| 4 | Kopfschuss | 0 s | 0,775 | 4 Ini | Release/Schatten/M → Impact/Schatten/M | Schatten | Pistolenschuss, unterbricht das Ziel. |
| 5 | Schwarzpulver | 0,25 s | 0,31 | 6 Ini | Sigil/Schatten/M → GroundCircle/Schatten/M | Schatten | Rauchfeld, blendet Feinde im Bereich. |
| Tarnung | Schattenhieb | 0,4 s | 2,79 | – | Release/Schatten/L → Impact/Schatten/L | Schatten | Tarnungsangriff: Hieb aus dem Schatten, Schwäche. |

### Waffe: Kurzbogen (Shortbow)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| 1 | Spaltpfeil | 0,55 s | 0,82 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Pfeil, der auf ein zweites Ziel überspringt. |
| 1.2 | Spaltpfeil · Folgeschuss | 0,55 s | 0,82 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Zweites Glied der Kette (53 % Koeffizient). |
| 1.3 | Spaltpfeil · Abschlussschuss | 0,55 s | 1,148 | – | Release/Schatten/S → Impact/Schatten/S | Schatten | Kettenabschluss: +40 % Schaden, Gift (4 s). |
| 2 | Streupfeil | 0,5 s | 1,395 | 3 Ini | Release/Schatten/M → Impact/Schatten/M | Schatten | Explodierender Pfeil, Explosions-Finisher. |
| 3 | Ausweichsalto | 0,3 s | 0 | 4 Ini | Release/Schatten/M → Aura/Schatten/M | Schatten | Doppelwaffen-Skill: Salto, weicht aus, Eile. |
| 4 | Giftpfeil | 0,4 s | 0,465 | 4 Ini | Release/Schatten/M → Impact/Schatten/M | Schatten | Giftwolke am Ziel (Gift). |
| 5 | Infiltrationspfeil | 0,25 s | 0 | 4 Ini | Sigil/Schatten/M → Pillar/Schatten/M | Schatten | Schattenschritt zum Zielort. |
| Tarnung | Überraschungsschuss | 0,4 s | 2,325 | – | Release/Schatten/L → Impact/Schatten/L | Schatten | Tarnungsangriff: betäubender Schuss. |

### Heilung, Hilfe, Elite

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| Heal | Schattenzuflucht | 0,5 s | 0 | 30 s | Release/Schatten/M → Aura/Schatten/M | Schatten | Heilt dich und gewährt 3 s Tarnung. |
| Heal | Tief durchatmen | 0,5 s | 0 | 25 s | Release/Schatten/M → Aura/Schatten/M | Schatten | Heilt dich, entfernt 2 Zustände, 2 Initiative. |
| Heal | Kräutertrunk | 1 s | 0 | 30 s | Release/Schatten/M → Aura/Schatten/M | Schatten | Starke Heilung und Regeneration. |
| Utility · Trick | Blendstaub | 0 s | 0 | 25 s | Shockwave/Schatten/M → Aura/Schatten/M | Schatten | Trick: blendet Feinde in der Nähe, 3 s Tarnung. |
| Utility · Trick | Hechtrolle | 0,25 s | 0 | 20 s | Release/Schatten/M → Aura/Schatten/M | Schatten | Trick: Rolle mit Ausweichen, entfernt Bewegungs-Zustände, Eile. |
| Utility · Trick | Fallstrick | 0,25 s | 0,465 | 30 s | Sigil/Schatten/M → Pillar/Schatten/M | Schatten | Trick: Fallstrick am Zielort, Verkrüppelung und Immobilisierung. |
| Utility · Venom | Lähmgift | 0 s | 0,31 | 25 s | Release/Schatten/M → Impact/Schatten/M | Schatten | Gift: vergiftete Klinge – Immobilisierung und Gift beim nächsten Treffer. |
| Utility · Venom | Schwächegift | 0 s | 0,31 | 25 s | Release/Schatten/M → Impact/Schatten/M | Schatten | Gift: Schwäche und Verwundbarkeit. |
| Utility · Venom | Spinnengift | 0 s | 0,31 | 30 s | Release/Schatten/M → Impact/Schatten/M | Schatten | Gift: 4 Gift, 1 Qual. |
| Utility · Deception | Schattenschritt | 0 s | 0 | 30 s | Release/Schatten/M → Aura/Schatten/M | Schatten | Täuschung: Schattenschritt 15 m nach vorn, entfernt 2 Zustände. |
| Utility · Deception | Rauchwolke | 0 s | 0 | 35 s | Release/Schatten/M → Aura/Schatten/M | Schatten | Täuschung: 4 s Tarnung. |
| Utility · Deception | Ablenkung | 0 s | 0,31 | 30 s | Release/Schatten/M → Impact/Schatten/M | Schatten | Täuschung: blendet und betäubt das Ziel kurz. |
| Elite | Klingenregen | 0,75 s | 0,93 ×6 | 90 s | Sigil/Schatten/Elite → Pillar/Schatten/Elite | Schatten | Elite: Klingenregen am Zielort (6 Wellen), Blutung. |
| Elite | Schattensturm | 0,5 s | 0 | 120 s | Shockwave/Schatten/Elite → Aura/Schatten/Elite | Schatten | Elite: Tarnung (4 s) für dich und Verbündete, Schnelligkeit. |

### Mechanik-Skills

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| F | Gestohlene Giftwolke | 0 s | 0,62 | – | Sigil/Natur/M → GroundCircle/Natur/M | Natur | Gestohlen (Moor): Giftwolke, Rauchfeld. |
| F | Eissplitter | 0 s | 1,86 | – | Release/Frost/L → Impact/Frost/L | Frost | Gestohlen (Frost): Eissplitter, Kälte. |
| F | Glutstoß | 0 s | 2,17 | – | Shockwave/Glut/L → Impact/Glut/L | Glut | Gestohlen (Glut): Explosion, Brennen. |
| F | Fell der Bestie | 0 s | 0 | – | Release/Natur/M → Aura/Natur/M | Natur | Gestohlen (Tier): 5 Macht und Schutz. |
| F | Felsbrocken | 0 s | 2,325 | – | Release/Erde/L → Impact/Erde/L | Erde | Gestohlen (Plünderer): Felsbrocken, betäubt. |

### Eigenschaftslinien

**Tödliche Künste** – Gift, Zustände und Gifte.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Giftblut | +100 Zustandsschaden. |
| 2 | Neben | Schleichendes Gift | Gift hält 25 % länger. |
| 3 | Neben | Giftmischer | +120 Sachkenntnis. |
| 1 | Haupt | Giftmeister | Gifte laden 20 % schneller auf; +10 % Gift-Schaden. |
| 1 | Haupt | Blutende Klingen | Kritische Treffer: 1 Blutung (Abklingzeit 1 s). |
| 1 | Haupt | Vergiftete Spitzen | +10 % Schaden gegen vergiftete Ziele. |
| 2 | Haupt | Zersetzung | Erhalte Zustandsschaden in Höhe von 10 % deiner Präzision. |
| 2 | Haupt | Hinterhältig | Aus der Tarnung: 3 Gift auf das nächste Ziel (Abklingzeit 8 s). |
| 2 | Haupt | Qualvoll | Qual hält 33 % länger. |
| 3 | Haupt | Siechtum | Alle Zustände halten 15 % länger. |
| 3 | Haupt | Kurzbogen-Kunst | Kurzbogen-Fertigkeiten: +7 % Schaden. |
| 3 | Haupt | Gnadenstoß | +15 % Schaden gegen Ziele unter 50 % Lebenspunkten. |

**Klingenkunst** – Kraft, kritische Treffer und Dolche.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Scharfe Klinge | +100 Kraft. |
| 2 | Neben | Präzise Hand | +120 Präzision. |
| 3 | Neben | Mordlust | +120 Wildheit. |
| 1 | Haupt | Dolchmeister | Dolch-Fertigkeiten: +7 % Schaden. |
| 1 | Haupt | Verletzlichkeit | +10 % Schaden gegen verwundbare Ziele. |
| 1 | Haupt | Schwachstelle | +5 % kritische Trefferchance. |
| 2 | Haupt | Säbeltanz | Schwert und Pistole: +7 % Schaden. |
| 2 | Haupt | Kaltblütig | Kritische Treffer gewähren 1 Macht (Abklingzeit 1 s). |
| 2 | Haupt | Hinterhalt | +10 % Schaden mit Wut. |
| 3 | Haupt | Henkersmeister | +15 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Tricktaschen | Tricks laden 20 % schneller auf. |
| 3 | Haupt | Tödlicher Schwung | Besiegte Gegner gewähren 3 s Schnelligkeit (Abklingzeit 10 s). |

**Schattenkunst** – Tarnung, Heilung und Täuschungen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Schattenhaut | +100 Vitalität. |
| 2 | Neben | Schattenheilung | +120 Heilkraft. |
| 3 | Neben | Leises Wesen | +120 Zähigkeit. |
| 1 | Haupt | Schattenzehrer | Tarnung betreten heilt dich (Abklingzeit 3 s). |
| 1 | Haupt | Täuschungskunst | Täuschungen laden 20 % schneller auf. |
| 1 | Haupt | Zweiter Atem | Ausweichen heilt dich (Abklingzeit 3 s). |
| 2 | Haupt | Schattenschleier | Tarnung betreten gewährt 3 s Schutz (Abklingzeit 10 s). |
| 2 | Haupt | Fluchtweg | Schaden erleiden gewährt 3 s Eile (Abklingzeit 10 s). |
| 2 | Haupt | Reinigende Schatten | Heilfertigkeiten laden 33 % schneller auf. |
| 3 | Haupt | Schattenrückkehr | Besiegte Gegner heilen dich (Abklingzeit 2 s). |
| 3 | Haupt | Verborgene Klinge | Aus der Tarnung: 5 s Wut (Abklingzeit 8 s). |
| 3 | Haupt | Schattenfürst | Elitefertigkeit lädt 20 % schneller auf; alle Segen halten 15 % länger. |

### Spezialisierung: Wegelagerer

- **Mechanik:** Knien – F1 Stehlen markiert das Ziel; Treffer darauf bauen Bosheit auf (max. 5). Bei voller Bosheit wird Skill 5 zum Todesschuss (+15 % Schaden je Bosheit). F5 Knien: unbeweglich, +10 % Schaden, +150 Präzision.
- **Waffe:** Wegelagerer-Gewehr · **Skilltyp:** Cantrip · **Palette/Sound-Motiv:** Pulver · **Signatur:** wegelagerer_WL_Todesschuss · **Faktor:** 1,4
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Pulverschuss | 0,6 s | 1,378 | – | Release/Pulver/S → Impact/Pulver/S | Pulver | Grundangriff auf bis zu 30 m; Verwundbarkeit (1). Projektil-Finisher. |
| W1.2 | Pulverschuss · Folgeschuss | 0,6 s | 1,378 | – | Release/Pulver/S → Impact/Pulver/S | Pulver | Zweites Glied der Kette (64 % Koeffizient). |
| W1.3 | Pulverschuss · Abschlussschuss | 0,6 s | 1,929 | – | Release/Pulver/L → Impact/Pulver/L | Pulver | Kettenabschluss: +40 % Schaden, 3 Verwundbarkeit. |
| W2 | Streuschuss | 0,5 s | 0,911 ×3 | 3 Ini | Release/Pulver/M → Impact/Pulver/M | Pulver | Drei schnelle Geschosse; je Treffer Verwundbarkeit. |
| W3 | Hechtschuss | 0,3 s | 1,085 | 4 Ini | Release/Pulver/M → Impact/Pulver/M | Pulver | Schuss aus der Ausweichbewegung; Eile und Wut für dich. |
| W4 | Kniekehlenschuss | 0,5 s | 1,41 | 5 Ini | Release/Pulver/M → Impact/Pulver/M | Pulver | Kontrollschuss: Immobilisierung (2 s) und Schwäche. |
| W5 | Kugelhagel | 0,75 s | 1,302 ×3 | 6 Ini | Sigil/Pulver/L → Pillar/Pulver/L | Pulver | Großer Flächen-Skill am Zielort (3 Wellen), Verwundbarkeit (2). Explosions-Finisher. |
| Tarnung | Schuss aus dem Dunkel | 0,4 s | 4,34 | – | Release/Pulver/L → Impact/Pulver/L | Pulver | Tarnungsangriff: verlässt die Tarnung mit einem schweren Treffer, 5 Verwundbarkeit. |
| Typ | Kantrip: Atemholen | 0,75 s | 0 | 25 s | Release/Pulver/M → Aura/Pulver/M | Pulver | Kantrip: Heilung; Wut für dich. |
| Typ | Kantrip: Schnellladen | 0 s | 0 | 25 s | Release/Pulver/M → Aura/Pulver/M | Pulver | Kantrip: Sofort-Skill, bricht die Tarnung nicht – Elan. |
| Typ | Kantrip: Abschütteln | 0 s | 0 | 30 s | Release/Pulver/M → Aura/Pulver/M | Pulver | Kantrip: Sofort-Skill, bricht die Tarnung nicht – Widerstand, entfernt 2 Zustände. |
| Typ | Kantrip: Kaltblut | 0 s | 0 | 35 s | Release/Pulver/M → Aura/Pulver/M | Pulver | Kantrip: Sofort-Skill, bricht die Tarnung nicht – Wut. |
| Typ | Kantrip: Letzte Kugel | 0,75 s | 2,604 | 90 s | Shockwave/Pulver/Elite → Impact/Pulver/Elite | Pulver | Elite (Kantrip): Verwundbarkeit (4) an Gegnern um dich, Wut und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Todesschuss | 1,2 s | 6,076 | – | Release/Pulver/L → Impact/Pulver/L | Pulver | Finisher bei voller Bosheit: verbraucht Bosheit und Markierung, +15 % Schaden je Bosheit. |

**Wegelagerer** – Pulverrauch und Laserlinie – Gewehr, Bosheit, Kantrips.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Kaltes Blut | +120 Präzision. |
| 2 | Neben | Pulverdampf | Wut halten 20 % länger. +60 Wildheit. |
| 3 | Neben | Schmale Kimme | +120 Kraft. |
| 1 | Haupt | Gewehrlauf | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Kleine Tricks | Kantrip-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Kantrip-Fertigkeiten. |
| 1 | Haupt | Markiertes Wild | Elite-Mechanik (F5) einsetzen: Wut für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Offene Wunde | +10 % Schaden gegen Ziele mit Verwundbarkeit. |
| 2 | Haupt | Streifschuss | Kritische Treffer: Verwundbarkeit auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Jagdfieber | +7 % Schaden, solange du Wut hast. Wut halten 15 % länger. |
| 3 | Haupt | Gnadenschuss | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Abrollen | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Letzte Patrone | Alle Zustände halten 15 % länger. +7 % deiner Präzision als Kraft. |

### Spezialisierung: Phantomtänzer

- **Mechanik:** Ausweich-Skill – Drei Ausdauer-Balken (150); jedes Ausweichen wird zum Angriff. F5 wählt die Ausweich-Fertigkeit: Staubwirbel (Fläche), Phantomstoß (Einzelziel, betäubt), Sandsprung (Sprung).
- **Waffe:** Kampfstab · **Skilltyp:** Physical · **Palette/Sound-Motiv:** Staubgold · **Signatur:** phantomtaenzer_PT_0 · **Faktor:** 1,45
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Stabschlag | 0,5 s | 1,414 | – | Release/Staubgold/S → Impact/Staubgold/S | Staubgold | Grundangriff (Kette aus drei Gliedern), das dritte Glied verursacht Schwäche. |
| W1.2 | Doppelschlag | 0,5 s | 1,414 | – | Release/Staubgold/S → Impact/Staubgold/S | Staubgold | Zweites Glied der Kette (63 % Koeffizient). |
| W1.3 | Rundumhieb | 0,5 s | 1,979 | – | Release/Staubgold/L → Impact/Staubgold/L | Staubgold | Kettenabschluss: +40 % Schaden, durchschlägt (bis zu 3 Ziele), 2 Verwundbarkeit. |
| W2 | Wirbelstab | 0,3 s | 0,809 ×4 | 3 Ini | Shockwave/Staubgold/M → Impact/Staubgold/M | Staubgold | Wirbelnder Angriff um dich (4 Treffer), Schwäche. Wirbel-Finisher. |
| W3 | Stabsprung | 0,3 s | 2,135 | 4 Ini | Release/Staubgold/L → Impact/Staubgold/L | Staubgold | Sprung zum Ziel (9 m). Sprung-Finisher; Elan für dich. |
| W4 | Kniefeger | 0,5 s | 1,461 | 5 Ini | Release/Staubgold/M → Impact/Staubgold/M | Staubgold | Wuchtiger Schlag, betäubt (1 s) und unterbricht. |
| W5 | Staubsturm | 0,75 s | 1,348 ×3 | 6 Ini | Sigil/Staubgold/L → Pillar/Staubgold/L | Staubgold | Großer Flächen-Skill am Zielort (3 Wellen), Schwäche (2). Explosions-Finisher. |
| Tarnung | Hinterhaltstoß | 0,4 s | 4,495 | – | Release/Staubgold/L → Impact/Staubgold/L | Staubgold | Tarnungsangriff: verlässt die Tarnung mit einem schweren Treffer, 5 Verwundbarkeit. |
| Typ | Zweiter Atem | 0,75 s | 0 | 25 s | Release/Staubgold/M → Aura/Staubgold/M | Staubgold | Körperfertigkeit: Heilung; Elan für dich. |
| Typ | Fußfeger | 0,25 s | 2,472 | 25 s | Release/Staubgold/L → Impact/Staubgold/L | Staubgold | Körperfertigkeit: Tritt betäubt (1 s), Stabilität für dich. |
| Typ | Sprungtritt | 0,25 s | 2,922 | 30 s | Release/Staubgold/L → Impact/Staubgold/L | Staubgold | Körperfertigkeit: Sprungtritt betäubt (1 s), Stabilität für dich. |
| Typ | Ringergriff | 0,25 s | 3,371 | 35 s | Release/Staubgold/L → Impact/Staubgold/L | Staubgold | Körperfertigkeit: Tritt betäubt (1 s), Stabilität für dich. |
| Typ | Phantomsturm | 0,75 s | 2,697 | 90 s | Shockwave/Staubgold/Elite → Impact/Staubgold/Elite | Staubgold | Elite (Körperfertigkeit): Schwäche (4) an Gegnern um dich, Elan und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Staubwirbel | 0 s | 1,798 | – | Shockwave/Staubgold/L → Impact/Staubgold/L | Staubgold | Ausweich-Fertigkeit: Wirbel um dich (Schwäche). |
| Mech | Phantomstoß | 0 s | 2,697 | – | Release/Staubgold/L → Impact/Staubgold/L | Staubgold | Ausweich-Fertigkeit: Stoß aufs Ziel, betäubt (0,5 s). |
| Mech | Sandsprung | 0 s | 1,348 | – | Shockwave/Staubgold/M → Impact/Staubgold/M | Staubgold | Ausweich-Fertigkeit: Landung wirft Sand auf (Blindheit, Explosions-Finisher). |

**Phantomtänzer** – Staubgold und Wirbelspuren – Stab, Phantomtanz, Körperfertigkeiten.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Leichte Füße | +120 Kraft. |
| 2 | Neben | Ausdauernder Atem | Elan halten 20 % länger. +60 Vitalität. |
| 3 | Neben | Staubkleid | +120 Wildheit. |
| 1 | Haupt | Stabkunst | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Körperbeherrschung | Körperfertigkeit-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Körperfertigkeit-Fertigkeiten. |
| 1 | Haupt | Tänzerischer Fluss | Elite-Mechanik (F5) einsetzen: Elan für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Gelenkschlag | +10 % Schaden gegen Ziele mit Schwäche. |
| 2 | Haupt | Wirbelnde Staubwolke | Kritische Treffer: Schwäche auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Hochmut | +7 % Schaden, solange du Elan hast. Elan halten 15 % länger. |
| 3 | Haupt | Letzter Tanz | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Unermüdlich | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Phantomschritt | Alle Zustände halten 15 % länger. +7 % deiner Kraft als Wildheit. |

### Spezialisierung: Schattenhüter

- **Mechanik:** Schattenschleier – F2 oder F5 Schattenschleier: zweite Lebensleiste aus Schattenkraft (ab 50 %), Schaden trifft zuerst den Schleier, die Kraft sinkt mit der Zeit; Skills 1–5 werden zu Schleier-Skills mit Unterstützung für Verbündete. Treffer und Initiative laden Schattenkraft.
- **Waffe:** Zepter & Pistole · **Skilltyp:** Well · **Palette/Sound-Motiv:** Tiefviolett · **Signatur:** schattenhueter_SS2 · **Faktor:** 1,55
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Schattenbolzen | 0,6 s | 1,526 | – | Release/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Grundangriff auf bis zu 22 m; Qual (1). Projektil-Finisher. |
| W1.2 | Schattenbolzen · Folgeschuss | 0,6 s | 1,526 | – | Release/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Zweites Glied der Kette (64 % Koeffizient). |
| W1.3 | Schattenbolzen · Abschlussschuss | 0,6 s | 2,136 | – | Release/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Kettenabschluss: +40 % Schaden, Brennen (3 s). |
| W2 | Rissfeuer | 0,5 s | 1,009 ×3 | 3 Ini | Release/Tiefviolett/M → Impact/Tiefviolett/M | Tiefviolett | Drei schnelle Geschosse; je Treffer Qual. |
| W3 | Schattengang | 0,3 s | 1,201 | 4 Ini | Release/Tiefviolett/M → Impact/Tiefviolett/M | Tiefviolett | Schuss aus der Ausweichbewegung; Eile und Schutz für dich. |
| W4 | Fesselriss | 0,5 s | 1,562 | 5 Ini | Release/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Kontrollschuss: Immobilisierung (2 s) und Schwäche. |
| W5 | Schattenkreis | 0,75 s | 1,442 ×3 | 6 Ini | Sigil/Tiefviolett/L → Pillar/Tiefviolett/L | Tiefviolett | Großer Flächen-Skill am Zielort (3 Wellen), Qual (2). Explosions-Finisher. |
| Tarnung | Schuss aus dem Riss | 0,4 s | 4,805 | – | Release/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Tarnungsangriff: verlässt die Tarnung mit einem schweren Treffer, 5 Verwundbarkeit. |
| Typ | Brunnen der Zuflucht | 0,75 s | 0 | 25 s | Release/Tiefviolett/M → Aura/Tiefviolett/M | Tiefviolett | Brunnen: Heilung; Schutz für dich. |
| Typ | Brunnen der Pein | 0,25 s | 0,721 ×3 | 25 s | Sigil/Tiefviolett/L → Pillar/Tiefviolett/L | Tiefviolett | Brunnen: Schattenbrunnen am Zielort – heilt Verbündete, Qual für Gegner. |
| Typ | Brunnen der Stille | 0,25 s | 0,721 ×3 | 30 s | Sigil/Tiefviolett/L → Pillar/Tiefviolett/L | Tiefviolett | Brunnen: Schattenbrunnen am Zielort – heilt Verbündete, Qual für Gegner. |
| Typ | Brunnen des Portals | 0,25 s | 0,721 ×3 | 35 s | Sigil/Tiefviolett/L → Pillar/Tiefviolett/L | Tiefviolett | Brunnen: Schattenbrunnen am Zielort – heilt Verbündete, Qual für Gegner; Schattenportal: du springst 12 m nach vorn. |
| Typ | Brunnen der Nacht | 0,75 s | 2,883 | 90 s | Shockwave/Tiefviolett/Elite → Impact/Tiefviolett/Elite | Tiefviolett | Elite (Brunnen): Qual (4) an Gegnern um dich, Schutz und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Schleierpfeil | 0,4 s | 1,682 | – | Release/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Schattenschleier: Schaden und Pein am Ziel; heilt den nächsten Verbündeten. |
| Mech | Tentakelgriff | 0,4 s | 1,682 | 6 s | Shockwave/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Schattenschleier: Tentakel ziehen Gegner zusammen (Immobilisierung). |
| Mech | Schattenhülle | 0,4 s | 0 | 8 s | Shockwave/Tiefviolett/L → Aura/Tiefviolett/L | Tiefviolett | Schattenschleier: Schutz und Regeneration für dich und Verbündete. |
| Mech | Risswelle | 0,4 s | 1,682 ×2 | 10 s | Release/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Schattenschleier: Welle aus Schatten, 3 Pein. |
| Mech | Nachtgang | 0,4 s | 1,682 | 12 s | Release/Tiefviolett/L → Impact/Tiefviolett/L | Tiefviolett | Schattenschleier: Schattenschritt nach vorn, Tarnung 2 s. |

**Schattenhüter** – Tentakel-Schatten und Risse – Zepter & Pistole, Schattenschleier, Brunnen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Schattenkraft | +120 Zustandsschaden. |
| 2 | Neben | Nachtsicht | Schutz halten 20 % länger. +60 Vitalität. |
| 3 | Neben | Dunkle Zuflucht | +120 Heilkraft. |
| 1 | Haupt | Zepterkunde | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Brunnenmeister | Brunnen-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Brunnen-Fertigkeiten. |
| 1 | Haupt | Schleierweber | Elite-Mechanik (F5) einsetzen: Schutz für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Peinigende Schatten | +10 % Schaden gegen Ziele mit Qual. |
| 2 | Haupt | Rissbolzen | Kritische Treffer: Qual auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Tiefe Nacht | +7 % Schaden, solange du Schutz hast. Schutz halten 15 % länger. |
| 3 | Haupt | Letzter Schatten | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Schattensprung | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Hüter der Risse | Alle Zustände halten 15 % länger. +7 % deiner Zustandsschaden als Heilkraft. |

### Spezialisierung: Klingenwirbel

- **Mechanik:** Rückruf – Geworfene Äxte kehren zurück: nach einem Axtwurf wird F1 (oder F5) zum Rückruf – Treffer auf dem Rückflug, +3 Initiative. Nach 6 s fällt die Axt von selbst zurück.
- **Waffe:** Wurfaxt & Dolch · **Skilltyp:** Preparation · **Palette/Sound-Motiv:** Stahl · **Signatur:** klingenwirbel_KW_Rueckruf · **Faktor:** 1
- Freischaltung ab Stufe 30, 3 Schritte à 10 Heldenpunkte (Linie, Waffe, Skilltyp)

| Platz | Skill | Aktivierung | Koeffizient | Abklingzeit/Kosten | Effekt-Komposition | Sound-Familie | Wirkung |
|---|---|---|---|---|---|---|---|
| W1 | Kreiselaxt | 0,6 s | 0,984 | – | Release/Stahl/S → Impact/Stahl/S | Stahl | Grundangriff auf bis zu 18 m; Blutung (1). Projektil-Finisher. |
| W1.2 | Kreiselaxt · Folgeschuss | 0,6 s | 0,984 | – | Release/Stahl/S → Impact/Stahl/S | Stahl | Zweites Glied der Kette (64 % Koeffizient). |
| W1.3 | Kreiselaxt · Abschlussschuss | 0,6 s | 1,378 | – | Release/Stahl/S → Impact/Stahl/S | Stahl | Kettenabschluss: +40 % Schaden, Wut (4 s). |
| W2 | Doppelwurf | 0,5 s | 0,651 ×3 | 3 Ini | Release/Stahl/M → Impact/Stahl/M | Stahl | Drei schnelle Geschosse; je Treffer Blutung. |
| W3 | Wirbelsprung | 0,3 s | 0,775 | 4 Ini | Release/Stahl/M → Impact/Stahl/M | Stahl | Schuss aus der Ausweichbewegung; Eile und Eile für dich. |
| W4 | Kniebrecher | 0,5 s | 1,007 | 5 Ini | Release/Stahl/M → Impact/Stahl/M | Stahl | Kontrollschuss: Immobilisierung (2 s) und Schwäche. |
| W5 | Axtsturm | 0,75 s | 0,93 ×3 | 6 Ini | Sigil/Stahl/L → Pillar/Stahl/L | Stahl | Großer Flächen-Skill am Zielort (3 Wellen), Blutung (2). Explosions-Finisher. |
| Tarnung | Wurf aus dem Schatten | 0,4 s | 3,1 | – | Release/Stahl/L → Impact/Stahl/L | Stahl | Tarnungsangriff: verlässt die Tarnung mit einem schweren Treffer, 5 Verwundbarkeit. |
| Typ | Vorbereitung: Verband | 0,75 s | 0 | 25 s | Release/Stahl/M → Aura/Stahl/M | Stahl | Vorbereitung: Heilung; Eile für dich. |
| Typ | Vorbereitung: Stolperdraht | 0,25 s | 1,24 | 25 s | Shockwave/Stahl/M → Impact/Stahl/M | Stahl | Vorbereitung: wird gelegt und löst bei Kontakt aus – Blutung (3). |
| Typ | Vorbereitung: Klingenfalle | 0,25 s | 1,55 | 30 s | Shockwave/Stahl/L → Impact/Stahl/L | Stahl | Vorbereitung: wird gelegt und löst bei Kontakt aus – Blutung (4). |
| Typ | Vorbereitung: Fangnetz | 0,25 s | 1,86 | 35 s | Shockwave/Stahl/L → Impact/Stahl/L | Stahl | Vorbereitung: wird gelegt und löst bei Kontakt aus – Blutung (5), Immobilisierung. |
| Typ | Vorbereitung: Klingenkreis | 0,75 s | 1,86 | 90 s | Shockwave/Stahl/Elite → Impact/Stahl/Elite | Stahl | Elite (Vorbereitung): Blutung (4) an Gegnern um dich, Eile und Stabilität für dich und Verbündete. Explosions-Finisher. |
| Mech | Rückruf | 0 s | 1,395 ×2 | – | Release/Stahl/M → Impact/Stahl/M | Stahl | Die Axt kehrt zurück und trifft auf dem Rückflug (2 Treffer, Blutung); +3 Initiative. |

**Klingenwirbel** – Stahlsilber und Rotations-Trails – Axt & Dolch, Rückruf, Vorbereitungen.

| Rang | Art | Eigenschaft | Wirkung |
|---|---|---|---|
| 1 | Neben | Stahlnerv | +120 Kraft. |
| 2 | Neben | Rotation | Eile halten 20 % länger. +60 Zustandsschaden. |
| 3 | Neben | Klingenauge | +120 Präzision. |
| 1 | Haupt | Axtkunde | Fertigkeiten der Elite-Waffe laden 20 % schneller auf. +5 % Schaden mit der Elite-Waffe. |
| 1 | Haupt | Gute Vorbereitung | Vorbereitung-Fertigkeiten laden 20 % schneller auf. +7 % Schaden mit Vorbereitung-Fertigkeiten. |
| 1 | Haupt | Bumerang | Elite-Mechanik (F5) einsetzen: Eile für dich (6 s, Abklingzeit 8 s). |
| 2 | Haupt | Tiefe Kerbe | +10 % Schaden gegen Ziele mit Blutung. |
| 2 | Haupt | Pfeifende Klinge | Kritische Treffer: Blutung auf das Ziel (4 s, Abklingzeit 2 s). |
| 2 | Haupt | Schwung | +7 % Schaden, solange du Eile hast. Eile halten 15 % länger. |
| 3 | Haupt | Letzter Wurf | +12 % Schaden gegen Ziele unter 50 % Lebenspunkten. |
| 3 | Haupt | Ausweichrolle | Ausweichen: Elan für dich (3 s, Abklingzeit 10 s). Ausweichen: heilt dich (Abklingzeit 10 s). |
| 3 | Haupt | Wirbelsturm aus Stahl | Alle Zustände halten 15 % länger. +7 % deiner Kraft als Präzision. |

