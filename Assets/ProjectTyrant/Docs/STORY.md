# Persönliche Geschichte – „Chronik des Tyrannen“ (Lauf 6)

## Prämisse
Ein Niemand aus der Grenzmark Ashveil wird beim Überfall der Aschkrallen auf Sonnfurt zum Helden. Hinter den Plünderern steht der Herold, ein ehemaliger Wächter der Tyrannenfeste, der die vier Siegel brechen will, die Vareth – einen Herrscher aus der Zeit vor den Karten – unter seiner Feste schlafen lassen. Mentor Brennar fällt am Pass, drei Bündnisse werben um den Helden, der Schlüssel zur Siegelkammer liegt geteilt in Tiefe und Wolken. Am Ende wird Vareth versiegelt, nicht getötet.

Alle Texte stehen in `Data/Story/Source/*.csv` (Id-Schema `K04_S02_L07`, ≤ 25 Wörter je Zeile), Export aller Zeilen nach `Docs/TEXT_TABLE.csv`, Kapitel-Lesefassung nach `Temp/PT_QA/story_K##.md`. Kapitel-Struktur: `Scripts/Editor/StoryBuilder.Chapters.cs` (K1–4) und `StoryBuilder.Chapters2.cs` (K5–10).

## Figuren (Bogen · Stimmprofil SAPI „Hedda“ + Nachbearbeitung)
| Figur | Rolle und Bogen | Stimme (Tonhöhe · Tempo · Hall · Formant) |
|---|---|---|
| Held | Spricht in der ersten Person, Varianten nach Wesenszug (Charme/Würde/Wildheit); Stimme A (hell) oder B (tief) aus der Charaktererstellung | 1,0 · 1,0 · 0,10 · 0 / B: 0,72 · −0,4 |
| Mira Sonnfurt | Freundin aus der Kindheit, erste Herz-NPC; von der Zweiflerin zur Organisatorin, schreibt die meisten Briefe, Epilog mit ihr | 1,08 · 1,12 · 0,08 · +0,3 |
| Hauptmann Brennar Feld | Mentor; lehrt Kampf und Pflicht, fällt in K04 am Pass | 0,62 · 0,88 · 0,14 · −0,6 |
| Kommandantin Haldis | Bündnis Wacht; lernt zu delegieren | 0,86 · 1,0 · 0,12 · −0,2 |
| Gelehrter Tovin Aschgrau | Bündnis Archiv; entdeckt die Geschichte des Tyrannen | 0,78 · 1,25 · 0,06 · +0,1 |
| Veyra | Bündnis Schleier; eigene Rechnung mit dem Herold | 0,92 · 0,82 · 0,16 · −0,1 |
| Der Herold | Gegenspieler, Boss in K04, K08, K10; glaubt an Ordnung durch den Tyrannen, bereut am Ende | 0,7 · 0,9 · 0,35 · −0,4 |
| Vareth, der Tyrann | Spricht nur in K09 (Vision) und K10; zwei Stimmen übereinander | 0,5 · 0,75 · 0,5 · −0,9 · doppelt |
| Nebenfiguren | Grolmok, Alda, Orsk, Elmar, Merle, Runa, Sela, Gorran, Fenn, Nadira, Ilan, Kerrin + 39 Späher/Herz-NPCs | je eigenes Profil in `cast.csv` |

## Biografie und Bündnis
- **Herkunft** (Hof / Posten / Ruinen) → eigene Start-Instanz in K01 (`K01_S01H/P/R`) mit eigenen Zeilen.
- **Wesenszug** → Zeilen-Varianten `@Charme/@Wuerde/@Wildheit` und Färbung der Antworten (♥ ♦ ▲).
- **Leitstern** (Verlorener / Rätsel / Schuld) → Nebenstrang am Wächterstein (K07) mit Ilan bzw. den Wächterzeichen bzw. Kerrin, Aufgriff im Finale (K10_S03E).
- **Bündnis** (Wahl in K05_S04: Wacht / Archiv / Schleier) → Gefährte in K07–K10, eigene Instanz in K06 (Sturm auf die Grotte / Einbruch im Archiv / Doppelagent), Zeilen-Varianten in K08–K10, Bündnis-Händler in Silbermund.

## Kapitel
| # | Titel | Zone · Mindeststufe | Schritte | Pflicht-Mechaniken |
|---|---|---|---|---|
| 1 | Rauch über Sonnfurt | Startzone · 1 | Start-Instanz (3 Varianten) · Miras Herz · Lager in der Rabenschanze | Ausweichen (Lern-Gegner), Fertigkeiten, Heilen, Waffenwechsel, Interagieren, Herz, Wegmarke |
| 2 | Die Wacht von Dornwacht | Startzone · 5 | Einladung · Palisade (Ereignis) · Grolmoks Lager · Ein Name | Ereignisse, Am Boden/Wiederbeleben (Mira), Kombo, Ausrüstung |
| 3 | Flüstern im Moor | Nebelmoor · 12 | Ins Moor (Späher) · Merles Fähre (Herz + Skimmer) · Der Gelehrte (Eigenschaften) · Das Fischerdorf | Skimmer, Eigenschaften, Zustände reinigen, Späher |
| 4 | Der Pass der Stürme | Frostkamm · 20 | Firnpforte (Gleiten, Springer) · Lagebesprechung (Weltboss-Vorereignis) · Der Pass (Trotzleiste, Brennars Tod) · Die erste Zeile | Gleiten, Weltboss-Zyklus, Trotzleiste |
| 5 | Asche und Eid | Glutöde · 30 | Drei Werber · Sturm über der Glutöde (Meta) · Die Aschwacht · Der Eid (Bündnis-Wahl) | Beherrschungen, Meta-Phasen, Elite-Spezialisierung |
| 6 | Der Hafen der hundert Segel | Silberküste · 30 | Silbermund · Bündnis-Instanz (W/A/S) · Rat der Bündnisse | Portalplatz, Unterbrechen (Archiv), Trotzleiste (Wacht) |
| 7 | Wurzeln der Finsternis | Dämmerwald · 30 | Violettes Licht (Ereignis) · Die Lichtung (Eskorte) · Der Wächterstein (Leitstern, Laterne) | Verderbnis, Eskorte, Unterbrechen, Bündel |
| 8 | Das Lied der Dünen | Dünenmeer · 30 | Drei Schluchten (Raptor) · Ruinen von Qasir (Herold 2) · Am Feuer | Raptor, Mehrphasen-Boss, Gefährte |
| 9 | Unter Kristall, über Wolken | Kristalltiefe + Himmelsspitzen · 30 | Stimme in der Tiefe (Laterne, Vision) · Über den Wolken (Aufwind) · Greifeneier · Der Schlüssel (Greif) | Dunkelheit, Aufwinde, Greif |
| 10 | Der Tyrann erwacht | Tyrannenfeste · 30 | Drei Tore · Der Herold · Die Versiegelung · Erste Zeile (Epilog in Silbermund) | alles zusammen |

## Szenen (Form)
Gesprächsszene (zwei Figuren vor gemalter Kulisse), In-World-Cinematic (Letterbox, Verdeckungs-Schwenk) und Sprechblase. Cinematics: K01_S01, K01_S03E, K02_S03/E, K03_S04E, K04_S03/E (Brennars Tod), K05_S03E, K07_S03, K08_S02/E, K09_S01, K10_S02/E, K10_S03/E. Alle Szenen mit Untertiteln; Wahl-Stellen je Kapitel ≥ 3 (eine mit Flag), auch beim Überspringen gestellt.

## Flag-Register (Auswahl)
`k01_eintopf/pflicht/jagd` · `k02_frueh` · `kennt_herold` · `k03_kinder_zuerst/tovin_licht/spur` · `kennt_siegel` · `brennar_gefallen` (Grabmal in Silbermund) · `k04_schwur/rache/erinnerung` · `k05_laut/leise` · `pakt_wacht/archiv/schleier` · `k06_grotte/buch/agent` · `k06_wald_zuerst` · `k07_leitstern`, `k07_leitstern_treu/ehrlich` · `k08_tiefe` · `herold_2` · `schluessel_tiefe` · `greif_vertrauen` (Greif freigeschaltet) · `schluessel_ganz`, `tyrannenfeste_offen` · `herold_gefallen` · `vareth_versiegelt` · `k10_angebot/kampf` · `epilog_frage/halten/essen` · `story_abgeschlossen` · `jp_<id>` (Sprungrätsel).

## Statistik
Wird in M34 aus dem Builder-Bericht ergänzt (Zeilen, Sprachminuten, Instanzen, Kapitel-Dauern).
