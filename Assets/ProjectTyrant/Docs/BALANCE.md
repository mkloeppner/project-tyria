# BALANCE – Gefahrenstufen, Verfolgung, Event-Stufen, Gegner-Kurve (Balance B1)

## Gefahrenstufen (`DangerProfile_0..5`, `Data/Balance`)
| Stufe | Name | Neutral | Aggro | Leine | Tempo | max. Verfolgung | Fessel | Rudel | Respawn | Veteranen |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | Dorfrand | 80 % | 450 | 800 | 0,85 | 8 s | – | – | 120 s | keine |
| 1 | Umland | 60 % | 550 | 1.100 | 0,92 | 12 s | 10 % Verkrüppelung | Partner | 90 s | einzeln an Lagern |
| 2 | Wildnis | 40 % | 650 | 1.400 | 1,00 | 18 s | 25 % +Kühle | Gruppe ≤ 3 | 75 s | Lager, Events |
| 3 | Grenzland | 20 % | 750 | 1.700 | 1,05 | 25 s | 40 % +Immobilisierung | Gruppe ≤ 4 | 60 s | Patrouillen |
| 4 | Feindland | 10 % | 850 | 2.100 | 1,10 + Eile | 35 s | 60 % +Zug | Rudel ≤ 6, Hinterhalt | 45 s | häufig |
| 5 | Verderbt | 0 % | 1.000 | 2.500 | 1,15 + Eile | 60 s | 80 % +Abwurf | Rudel ≤ 8, Rufer | 30 s | Eliten |

Zuordnung je Zone: siehe WORLD.md („Gefahrenstufen“). Validator (`BalanceValidator`, läuft in Rebuild Everything): keine Stufen-Sprünge > 1 zwischen Nachbarn, Stufe 0 um Hub/Ankunft (Startzone), keine Spawns in Sicherheitszonen/Städten, keine Spawns in Stufe 0 näher als 100 m (skaliert) an Wegmarken, keine Veteranen in Stufe 0, Neutral-Quote ±20 %, Event-Stufe passend zur Gefahrenstufe, kein Auto-Start < 8 min in Event-Stufe 0–1, Event-Spawns nicht nahe Hub/Wegmarke.

## Event-Stufen
| Stufe | erlaubt in Gefahrenstufe | Werte |
|---|---|---|
| 0 Einführung | 0 | ≤ 5 Trash gleichzeitig, Spieler-Level − 1, keine Veteranen/Fesseln, Timer ≥ 180 s, Ziele −50 %, Neustart nach 2 min, keine Welt-Folge |
| 1 Leicht | 0–2 | ≤ 8 Trash, Timer ≥ 150 s, Ziele −25 %, Cooldown 3 min |
| 2 Standard | 1–3 | wie bisher |
| 3 Schwer | 3–5 | Veteranen-Gruppen, Fesseln, Zeitdruck |
| 4 Meta/Boss | 4–5 | unverändert (Weltbosse, Metas) |

## Gegner-Kurve (`EnemyCurve`)
Lebenspunkte = erwarteter Spieler-DPS × Ziel-Zeit-bis-Kill; Angriffskoeffizient = Treffer-Anteil × erwartete Spieler-HP × Rüstung / (Waffenstärke × Kraft); Treffer-Anteil = min(Deckel der Stufe, Intervall / (3 × (Zeit-bis-Tod-Ziel + Intervall) × Sicherheitsfaktor)). Rollen: Fernkampf −20 % HP, Heiler −30 % Schaden, Jäger +10 % Tempo. Ausgenommen: Weltbosse, Metas, Story-Bosse, Champions (außer Grolmok, Stufe-2-Finale Level 13).

## Zielwerte, Baseline und Endwerte
(wird in B5 mit den Messungen gefüllt)
