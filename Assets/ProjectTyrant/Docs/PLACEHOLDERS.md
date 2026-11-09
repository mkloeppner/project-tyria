# PLACEHOLDERS – prozedurale Assets

Jedes Objekt mit `PlaceholderMarker`-Komponente ist hier gelistet. Austausch: Meshy-Prompt generieren → `MeshyIntegrator.GetOrBuild()` → im jeweiligen Builder statt der Primitive einsetzen.

| Asset | Wo | Erzeugt durch | Vorgesehener Meshy-Prompt |
|---|---|---|---|
| ~~Spielercharakter~~ (ersetzt) | PT_Player/Visual | Meshy ✅ (Fallback: CharacterBuilder) | stylized painterly fantasy hero, armored human warden with blue and gold tabard, short cape, game-ready low poly, hand-painted texture, A-pose, full body |
| ~~Champion „Grolmok“~~ (Meshy ✅) | Prefabs/Characters/PT_Enemy_Champion | CharacterBuilder (Primitive) | stylized painterly fantasy brute warlord, huge horned ash-skinned ogre chieftain with bone crown, glowing orange eyes, spiked shoulder armor, red war sash, holding a giant bone club, game-ready low poly, hand-painted texture, full body |
| ~~Hub-Wahrzeichen Leuchtfeuerturm~~ (Meshy ✅) | Szene: POI_Hub_WeilerSonnfurt | Primitive + MeshKit-Dach | stylized painterly fantasy village beacon tower, tall stone lighthouse-like watchtower with wooden balconies, red tiled conical roof and a glowing golden brazier on top, game-ready low poly, hand-painted texture |
| ~~Trash „Aschkrallen-Plünderer“~~ (Meshy ✅) | Prefabs/Characters/PT_Enemy_Trash | CharacterBuilder | stylized painterly fantasy horned goblin-like raider with ash grey hide, red sash and bone club, game-ready low poly, hand-painted texture, full body |
| ~~Veteran „Aschkrallen-Brecher“~~ (Meshy ✅) | Prefabs/Characters/PT_Enemy_Veteran | CharacterBuilder | stylized painterly fantasy hulking horned raider brute with dark iron pauldrons, red war sash and heavy bone club, game-ready low poly, hand-painted texture, full body |
| Dornwacht-Wächter | Prefabs/Characters/PT_Ally_Guard | CharacterBuilder | stylized painterly fantasy frontier guard in blue tabard with spear and round shield, game-ready low poly, hand-painted texture, full body |
| ~~Waffe Glutklinge~~ (Meshy ✅) | PT_Player/Visual/WeaponSocket/Greatsword | Primitive | stylized painterly fantasy greatsword with ember-glowing blade, low poly, hand-painted |
| ~~Waffe Sturmruf~~ (Meshy ✅) | PT_Player/Visual/WeaponSocket/Staff | Primitive | stylized painterly fantasy wooden staff with glowing violet storm orb, low poly, hand-painted |
| Vorratskisten | Prefabs/Characters/PT_SupplyCrate | Primitive | stylized painterly stack of wooden supply crates and grain sacks, low poly, hand-painted |
| Häuser (House_0..5) | Szene: POI_Hub_WeilerSonnfurt | Primitive + MeshKit-Satteldach | stylized painterly fantasy timber-frame cottage, cream plaster walls, dark wooden beams, red gable roof, low poly, hand-painted |
| Palisade / Wachturm | Szene: POI_Grenzposten_Dornwacht | Primitive + MeshKit | stylized painterly wooden frontier palisade wall segment and watchtower, low poly, hand-painted |
| Ruinen (Säulen, Bogen, Schutt) | Szene: POI_Ruinen_Elmhain | Primitive | stylized painterly ancient elven ruin pillars and broken arch, mossy pale stone, low poly, hand-painted |
| Env_Tree_0..3 | Prefabs/Environment | MeshKit (C#) | stylized painterly low poly broadleaf tree with chunky rounded canopy, hand-painted |
| Env_Pine_0..1 | Prefabs/Environment | MeshKit (C#) | stylized painterly low poly pine tree, layered cone canopy, hand-painted |
| Env_Rock_0..3 | Prefabs/Environment | MeshKit (C#) | stylized painterly low poly boulder, faceted, hand-painted |
| Env_Tent_Ally | Prefabs/Environment | MeshKit (C#) | stylized painterly canvas army tent, cream fabric with blue trim, low poly, hand-painted |
| Env_Tent_Raider | Prefabs/Environment | MeshKit (C#) | stylized painterly crude raider hide tent with bones and red war paint, low poly, hand-painted |

## Lauf 3 (prozedural, `PlaceholderMarker` gesetzt)
| Asset | Ort | Umsetzung | Meshy-Prompt |
|---|---|---|---|
| Waypoint_Monument (Tier 1) | ZoneContent/Waypoint_* | Primitive + Kristall + Licht | stylized painterly fantasy waypoint monument, small stone plinth with a floating glowing blue crystal, game-ready low poly, hand-painted texture |
| Vista_Stele (Tier 1) | ZoneContent/Vista_*/Stele | Primitive + Leuchtkugel | stylized painterly fantasy viewpoint stele, slender carved stone pillar topped with a glowing golden orb, game-ready low poly, hand-painted texture |
| HeroChallenge_Shrine (Tier 1) | ZoneContent/Challenge_*/Shrine | Primitive + Kristall/Relikt/Banner | stylized painterly fantasy hero challenge shrine, small hexagonal stone altar with a floating light-blue crystal, game-ready low poly, hand-painted texture |
| Heart_NPC (Tier 2) | ZoneContent/Heart_* | Kapsel + Kugel + Namensschild | stylized painterly fantasy villager quest giver, friendly farmer in golden tunic, game-ready low poly, hand-painted texture, full body |
| Händler-NPCs (Tier 2) | NPCs/Vendor_* | Kapsel + Stand | stylized painterly fantasy market vendor with wooden stall, game-ready low poly, hand-painted texture |
| Karawanenwagen (Tier 2) | Laufzeit (EventDirector) | Würfel + Plane | stylized painterly fantasy merchant caravan wagon with canvas cover, game-ready low poly, hand-painted texture |
| Item-/Karten-/Eigenschafts-Icons | UI/Icons | IconPainter (SDF) bzw. Glyphen | – (prozedural gewollt) |


### Lauf 4
| Platzhalter | Ort | Umsetzung | Meshy-Prompt |
|---|---|---|---|
| Portal_Arch (Tier 2) – **Meshy ersetzt (M20)** | Zonen-Szenen / Environment/Portal_* | Meshy-Bogen + Schleier + Licht + Namensschild (Primitive als Rückfall) | stylized painterly fantasy zone portal, tall stone archway with glowing blue magical veil, game-ready low poly, hand-painted texture |
| Mastery_Insight (Tier 1) – **Meshy ersetzt (M20)** | PT_ZoneRoot/ZoneContent/Insight_* | Meshy-Kristall + Licht (Primitive als Rückfall) | stylized painterly fantasy floating teal crystal on a small stone base, game-ready low poly, hand-painted texture |
| Landmark_<POI> (Tier 2, 15×) | PT_ZoneRoot/ZoneContent/Landmark_* | 4 Primitive-Varianten (Hütten, schiefer Turm, Säulenkreis, Felsbogen), je Biom getönt | stylized painterly fantasy landmark: <POI-Name>, game-ready low poly, hand-painted texture |
| PT_Enemy_Moor_Schleicher / _Spucker / _Koloss | Prefabs/Characters | CreatureBuilder Humanoid/Vierbeiner + ProceduralLimbAnimator | siehe Prompt im PlaceholderMarker (Moor-Lauerer, Giftkröte, Torfkoloss) |
| PT_Enemy_Frost_Eiswolf / _Huter / _Werfer | Prefabs/Characters | CreatureBuilder Vierbeiner/Humanoid mit Schild | siehe PlaceholderMarker (Eiswolf, Frostwächter mit Eisschild, Schneeyeti) |
| PT_Enemy_Glut_Kriecher / _Beschwoerer / _Golem | Prefabs/Characters | CreatureBuilder Käfer/Humanoid mit Stab/Glutrissen | siehe PlaceholderMarker (Aschekäfer, Glutbeschwörer, Lavagolem) |
| PT_Critter_Reiher / Kroete / Schneehase / Bergziege / Aschekaefer / Salamander | Prefabs/Critters | CreatureBuilder Vogel/Vierbeiner/Käfer | stylized painterly fantasy <Tier> ambient animal, game-ready low poly, hand-painted texture |
| PT_Mount_Raptor / Springer / Skimmer – **Meshy ersetzt (M20)** | Prefabs/Mounts | Meshy-Modell statisch + Bobber; prozedurale Fassung (CreatureBuilder) als Rückfall | siehe ASSET_QUEUE 17–19 |
| PT_WorldBoss_Frostschlund – **Meshy ersetzt (M20)** | Prefabs/Characters | Meshy-Modell statisch | ASSET_QUEUE 20 |
| PT_Meta_Sturmtyrann | Prefabs/Characters | CreatureBuilder Humanoid mit Glutrissen | stylized painterly fantasy storm titan made of dark basalt with crackling blue lightning veins |
| Stallmeister (Tier 2, 3×) | Startzone Hub, Nebelmoor, Frostkamm | Kapsel + Kugel + Namensschild + Reittier-Ausstellung | stylized painterly fantasy stable master npc in leather apron |
| PoisonMist / BounceMushroom / HiddenChest / Updraft | PT_ZoneRoot/Mastery | Partikel bzw. Primitive | siehe PlaceholderMarker |

### Lauf 5 / M21 – Animation
| Objekt | Ort | Platzhalter | Ziel |
|---|---|---|---|
| Spieler, Wachen, Plünderer (3 Ränge), Biom-Humanoide, Weltboss Frostschlund | Prefabs/Characters, PT_Core | **geriggte MeshKit-Figuren** (SkinnedMeshRenderer, 22 Knochen); Meshy-Modelle nicht rissfrei skinnbar (DECISIONS M21) | geriggte Meshy-/Hand-Modelle mit sauberer Topologie |
| Reittiere Raptor/Springer/Skimmer, Tiere (6 Körperbauten) | Prefabs/Mounts, Prefabs/Critters | geriggte MeshKit-Vierbeiner (18 Knochen) | geriggte Modelle |
| Heldenhain-Statuen (8 Meshy-Modelle) | Sonnfurt, Hub-Süd | Meshy-Modelle auf Steinsockeln (Weiterverwendung nach Skinning-Fehlschlag) | – (bewusst Statuen) |
| Dorfbewohner, Herz-NPCs, Händler, Stallmeister, verletzte Wachen | alle Zonen | geriggte MeshKit-Dorfbewohner (Robe/Haar/Haut per Seed) – ersetzen Kapsel + Kugel | Tier-2-Modelle |

### Lauf 5 / M23 – VFX
| Objekt | Ort | Platzhalter | Ziel |
|---|---|---|---|
| FX-Texturen (13: Glow, Spark, Smoke, Ring, Sigil, Crack, Noise, Leaf, Flake, Drop, Shard, Ember, Beam) | Procedural/Textures/FX | prozedural gezeichnete 64/128-px-PNGs (`FxTextureKit`) | gemalte Effekt-Texturen / Flipbooks |
| Vögel | alle Zonen | 5-Vertex-V-Mesh mit skaliertem Flügelschlag | animierte Vogelmodelle |
| Laternen-Flamme | Sonnfurt-Hub | Funken-Partikel über Würfel-Lampe | Laternenmodell mit Flammen-Flipbook |

### Lauf 5 / M24 – Audio
| Objekt | Ort | Platzhalter | Ziel |
|---|---|---|---|
| Alle Spiel-, UI- und Umgebungsklänge (215 Ids, 562 WAV) | Procedural/Audio | prozedural (Synth/SoundKit), Analysewerte in Data/Audio/SoundReport.txt | aufgenommene/gemischte Sound-Bibliothek |
| Musik (10 Stem-Sätze, 28 Stinger) | Procedural/Audio/Music, /Stinger | generativer Komponist (Synth-Instrumente) | komponierte/eingespielte Musik |

### Lauf 5 / M25 – Klassen
| Objekt | Ort | Platzhalter | Ziel |
|---|---|---|---|
| Gefährten Wolf/Bär/Falke | Data/Classes/wildhueter, Prefabs | Tier-Rigs aus dem CreatureBuilder, umgefärbt | eigene Gefährten-Modelle |
| Waffenmodelle Langbogen, Axt+Fackel, Schwert+Kriegshorn, Stab, Zepter+Fokus, Dolche, Schwert+Pistole, Kurzbogen | CharacterBuilder (Primitive) | prozedurale Primitive mit Vertex-Farben | modellierte Waffen (Meshy M27) |
| Klassen-Rüstungen | PlayerVisuals | Wächter-Rig mit umgefärbten Vertex-Farben | eigene Rüstungs-Silhouetten je Gewichtsklasse |
| Kulisse der Charakterauswahl | Scenes/PT_CharacterSelect | Primitive (Wiese, Steinplatz, Bäume, Felsen) | gestaltete Kulisse |
