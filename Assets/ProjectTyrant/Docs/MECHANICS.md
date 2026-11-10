# MECHANICS – Verfolgung, Flucht, neutrale Gegner (Stand Balance B1)

## Verfolgungsprofile (`PursuitProfile` aus `DangerProfile_0..5`)
Reichweiten in GW2-Einheiten (1 Einheit = 0,0254 m). Tempo relativ zum Spieler-Grundtempo 7 m/s.

| Stufe | Aggro | Leine | Tempo | max. Verfolgung | Sicht weg → Aufgabe | Zögern | Fluchtgnade | Reittier |
|---|---|---|---|---|---|---|---|---|
| 0 Dorfrand | 450 (11 m) | 800 (20 m) | 0,85× | 8 s | 3 s | 1 s | 600 / 5 s | gibt auf ab 400 |
| 1 Umland | 550 (14 m) | 1.100 (28 m) | 0,92× | 12 s | 3 s | 1 s | 600 / 5 s | gibt auf ab 400 |
| 2 Wildnis | 650 (17 m) | 1.400 (36 m) | 1,00× | 18 s | 3 s | – | – | gibt auf ab 400 |
| 3 Grenzland | 750 (19 m) | 1.700 (43 m) | 1,05× | 25 s | 6 s | – | – | gibt auf ab 400 |
| 4 Feindland | 850 (22 m) | 2.100 (53 m) | 1,10× + Eile | 35 s | 6 s | – | – | nur Jäger verfolgen |
| 5 Verderbt | 1.000 (25 m) | 2.500 (64 m) | 1,15× + Eile | 60 s | 6 s | – | – | nur Jäger, Abwurf |

Jäger: Leine/Dauer ×1,5, Tempo ×1,1, Schattenschritt (Stufe 4–5). Neutrale Kreaturen: Dauer ×0,6, Leine ×0,7. Tyrannenfeste (Innerer Hof, Siegelkammern, Thronsaal): keine Leine.

## Aufgabe-Regeln (`PursuitModel.Tick`)
1. Entfernung vom Spawn > Leine.
2. Verfolgungsdauer > Maximum (Schaden des Spielers an diesem Gegner setzt die Uhr zurück).
3. Sichtlinie verloren > 3 s (Stufe 0–2) bzw. 6 s (Stufe 3–5).
4. Fluchtgnade (Stufe 0–1): Spieler hat 5 s keinen Schaden verursacht und ist > 600 Einheiten entfernt.
5. Spieler aufgesessen und > 400 Einheiten entfernt (Stufe 0–3); Stufe 4–5 nur Jäger verfolgen weiter.
6. Wegmarke, Portal, Zonenwechsel, Stadt oder Sicherheitszone (Hub 30 m, Wegmarke 12/25 m).

Bei Aufgabe: GW2-Reset – unverwundbar, Rückkehr zum Spawn, volle Heilung; „Gegner gibt auf“ als Lern-Hinweis einmal in Stufe 0–1. Kampfende für den Spieler nach 5 s ohne Schaden.

## Neutral und feindlich
Gelber Name = neutral: greift erst an, wenn angegriffen (danach rot bis zur Rückkehr). Roter Name = feindlich. Neutrale Tiere fliehen in Stufe 0 unter 30 % HP. Wachen greifen keine gelben Kreaturen an. Verteilung je Stufe 80/60/40/20/10/0 % neutral.

## Rudel, Fesseln, Separation
- Rudel: Stufe 0 keins · 1 nur der nächste Partner (≤ 6 m) · ab 2 die Gruppe im Radius 300 (bis 3/4/6/8 Mitglieder) · 4 Hinterhalte (Lauerer) · 5 Rufer alarmieren das Lager (1,5-s-Ruf, unterbrechbar durch Kontrolle oder 10 % Schaden).
- Fesseln: Stufe 0 keine Kontrolle · 1 Verkrüppelung 10 % · 2 +Kühle 25 % · 3 +Immobilisierung 40 % · 4 +Heranziehen/Benommenheit 60 % · 5 +Reittier-Abwurf 80 %; je Gegner höchstens eine Fessel je 6 s. Schadenszustände (Gift, Brennen) sind keine Fesseln.
- Separation nur unter Gegnern desselben Teams; Gegner kommen dem Spieler nie näher als 0,8 m.

## Hilfe in der Welt (Stufe 0–2)
Wachen-Patrouillen auf den Wegen der Startzone übernehmen die Aggro (Stufe 0–2). Downed-Gnade: Stufe 0 Gegner wechseln auf Wachen oder brechen nach 6 s ab, Stufe 1 wechseln auf Wachen, ab Stufe 4 stampfen Veteranen auf Downed-Spieler (doppelter Schaden). NPC-Wiederbelebung in Stufe 0–1: „Hilfe ist unterwegs“, Wiederbelebung nach 8 s ohne Gegner in 15 m, spätestens 20 s.
