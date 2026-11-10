using System.Collections.Generic;
using ProjectTyrant.Procedural;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 11: die drei Regionen der Kontinentkarte.</summary>
    public enum WorldRegion { Kernland, Glutwueste, Dracheninseln }

    /// <summary>
    /// Lauf 11: Kompakte Zonen-Beschreibung (reine Daten). Aus ihr erzeugt <see cref="RegionZones.Layout"/> deterministisch den vollständigen
    /// Zonen-Inhalt (Wegmarken, Sehenswürdigkeiten, Aussichtspunkte, Heldenherausforderungen, Einblicke, Herzen, Lager, Tiere) und der Builder
    /// Ereignisse, Späher, Sprungrätsel, Gefahrenkarte und Portale.
    /// </summary>
    public class RegionZoneSpec
    {
        public string Id, Name, Description, Biome, Tip, Signature;
        public WorldRegion Region;
        public int LevelMin, LevelMax, Seed;
        public float Size;
        public int DangerEntry, DangerCore;
        public bool Hub;
        public string[] Archetypes, Critters, Places;
        /// <summary>„Npc|Aufgabe|Etikett1|Etikett2|Herzname“.</summary>
        public string[] Hearts;
        public string Enemy, Ally;
        public Rect MapRect; public Color MapColor;
        public string WorldBoss, Meta;   // Ereignis-Ids (Builder), optional
    }

    public static class RegionZones
    {
        static RegionZoneSpec Z(string id, string name, WorldRegion r, int lmin, int lmax, string biome, float size, int entry, int core, string desc, string[] arch, string[] critters, string[] places, string[] hearts, string enemy, string ally, Rect rect, string color, string tip, string signature)
        {
            ColorUtility.TryParseHtmlString(color, out var c);
            return new RegionZoneSpec { Id = id, Name = name, Region = r, LevelMin = lmin, LevelMax = lmax, Biome = biome, Size = size, DangerEntry = entry, DangerCore = core, Description = desc,
                Archetypes = arch, Critters = critters, Places = places, Hearts = hearts, Enemy = enemy, Ally = ally, MapRect = rect, MapColor = c, Tip = tip, Signature = signature, Hub = entry == 0, Seed = 40000 + (id.GetHashCode() & 0x7fff) };
        }

        const WorldRegion K = WorldRegion.Kernland, G = WorldRegion.Glutwueste, D = WorldRegion.Dracheninseln;

        public static readonly RegionZoneSpec[] All =
        {
            // ---------------- Kernland-Erweiterung (8) ----------------
            Z("weite_von_ahrn", "Weite von Ahrn", K, 30, 40, "steppe", 1200f, 3, 3, "Offene Steppe, Nomadenzelte und wandernde Herden bis zum Horizont.",
                new[] { "Kern_Nomade", "Kern_Steppenbulle", "Duene_Raeuber" }, new[] { "Steppenbison", "Bergziege" },
                new[] { "Lager der Ahrn", "Hirtenfurt", "Windsäulen", "Grasmeer", "Ahnenhügel", "Herdenpfad", "Wolkenrast", "Steppentor" },
                new[] { "Hirtin Saya|Treibe versprengte Tiere zusammen und flicke die Gatter.|Kalb|Gatter|Hilf Hirtin Saya", "Sattler Orun|Sammle Felle und verbinde verletzte Reiter.|Fell|Reiter|Unterstütze Sattler Orun",
                        "Brunnenwächterin Tala|Repariere Brunnen und vertreibe Wühler.|Brunnen|Eimer|Hilf Brunnenwächterin Tala", "Späher Kemal|Sammle Spuren und richte Signalmasten auf.|Spur|Mast|Unterstütze Späher Kemal" },
                "Steppenräuber", "Nomaden der Ahrn", new Rect(0.0f, 0.29f, 0.05f, 0.05f), "#c8c070", "Wandernde Herden ziehen über die Weite – folge ihnen zu den Wasserstellen.", "Herden-Events, Reittier-Weiten"),
            Z("nebelkamm", "Nebelkamm", K, 30, 40, "hochmoor", 1100f, 3, 3, "Ein Hochplateau mit Mooren, Steinkreisen und Geisterlichtern bei Nacht.",
                new[] { "Kern_Steingeist", "Moor_Schleicher", "Moor_Spucker" }, new[] { "Moorfuchs", "Reiher" },
                new[] { "Steinkreis von Ulm", "Torfdorf", "Grauer Kamm", "Irrlichtsenke", "Alte Warte", "Moorbrücke", "Nebelsee", "Hügelgrab" },
                new[] { "Torfstecher Ivo|Steche Torf und stütze die Bohlenwege.|Torf|Bohlenweg|Hilf Torfstecher Ivo", "Kräuterfrau Wenna|Sammle Moorkraut und verbinde Verirrte.|Moorkraut|Verirrter|Unterstütze Kräuterfrau Wenna",
                        "Steinmetz Bruno|Richte umgestürzte Steine auf und vertreibe Geister.|Stein|Rune|Hilf Steinmetz Bruno", "Laternenmagd Elis|Entzünde Laternen und sammle Irrlichtstaub.|Laterne|Staub|Unterstütze Laternenmagd Elis" },
                "Steingeister", "Torfdorf", new Rect(0.28f, 0.0f, 0.3f, 0.05f), "#7a8a72", "Nachts erwachen die Geister der Steinkreise.", "Geister-Events bei Nacht, Einblicke auf Steinkreisen"),
            Z("sturmbucht", "Sturmbucht", K, 40, 50, "klippen", 1000f, 3, 4, "Klippen, Fischerdörfer und Piratenbuchten im ewigen Wind.",
                new[] { "Kern_Pirat", "Kueste_Harpyie", "Kueste_Schmuggler" }, new[] { "Reiher", "Kroete" },
                new[] { "Hafen Weißwasser", "Wrackbucht", "Möwenklippe", "Piratennest", "Leuchtturm Ost", "Salzpfanne", "Schmugglergrotte", "Sturmkap" },
                new[] { "Hafenmeisterin Rhea|Sichere Taue und repariere Stege.|Tau|Steg|Hilf Hafenmeisterin Rhea", "Fischer Olav|Flicke Netze und versorge Verletzte.|Netz|Fischer|Unterstütze Fischer Olav",
                        "Bergungstaucherin Mara|Berge Wrackgut und kennzeichne Riffe.|Wrackgut|Boje|Hilf Bergungstaucherin Mara", "Leuchtturmwärter Gerd|Trage Öl und repariere Linsen.|Öl|Linse|Unterstütze Leuchtturmwärter Gerd" },
                "Piraten der Sturmbucht", "Hafen Weißwasser", new Rect(0.0f, 0.58f, 0.04f, 0.14f), "#7fb0c8", "Die Piraten belagern regelmäßig den Hafen.", "Schiffswracks, Belagerung des Hafens"),
            Z("aschenkessel", "Aschenkessel", K, 50, 60, "vulkan", 1100f, 3, 4, "Lavafelder, Obsidianklippen und Schwefelseen unter einem rauchenden Gipfel.",
                new[] { "Kern_Magmabrut", "Glut_Kriecher", "Glut_Golem" }, new[] { "Salamander", "Aschekaefer" },
                new[] { "Obsidianwerk", "Schwefelsee", "Rauchschlot", "Glutpass", "Basaltterrassen", "Kraterrand", "Aschenhof", "Schlackenhalde" },
                new[] { "Schmiedin Hulda|Sammle Obsidian und kühle die Essen.|Obsidian|Esse|Hilf Schmiedin Hulda", "Alchemist Borek|Schöpfe Schwefel und versorge Vergiftete.|Schwefel|Vergifteter|Unterstütze Alchemist Borek",
                        "Hüterin Asta|Errichte Hitzeschilde und lösche Brände.|Schild|Brand|Hilf Hüterin Asta", "Bergmann Thoren|Sichere Stollen und berge Erz.|Stütze|Erz|Unterstütze Bergmann Thoren" },
                "Magmabrut", "Obsidianwerk", new Rect(0.62f, 0.26f, 0.04f, 0.08f), "#b05a3a", "Wenn der Gipfel grollt, beginnt der Vulkanausbruch.", "Glutschritt-Beherrschung, Vulkanausbruch-Meta"),
            Z("gruener_schlund", "Grüner Schlund", K, 55, 65, "dschungel", 1100f, 3, 4, "Dichter Dschungel mit einer zweiten Welt in den Baumkronen.",
                new[] { "Kern_Dschungelpirscher", "Wald_Wildtier", "Wald_Dornenschreck" }, new[] { "Kroete", "Reiher" },
                new[] { "Kronendorf", "Lianenbrücke", "Wurzeltempel", "Pilzhain", "Schlundfall", "Blätterdach", "Rankenpfad", "Smaragdteich" },
                new[] { "Rankenläuferin Ysa|Spanne Lianen und rette Abgestürzte.|Liane|Gestürzter|Hilf Rankenläuferin Ysa", "Pilzsammler Noam|Ernte Hüpfpilze und verbinde Gestochene.|Pilz|Gestochener|Unterstütze Pilzsammler Noam",
                        "Tempelhüterin Aka|Lege Wurzeln frei und entzünde Opferschalen.|Wurzel|Schale|Hilf Tempelhüterin Aka", "Kronenbauer Pim|Repariere Hängebrücken und sammle Harz.|Brücke|Harz|Unterstütze Kronenbauer Pim" },
                "Dschungelpirscher", "Kronendorf", new Rect(0.04f, 0.88f, 0.26f, 0.1f), "#3f8a3a", "Greif und Himmelsschuppe erreichen das Kronendorf am schnellsten.", "Vertikalität, Kronen-Dorf"),
            Z("schilfmuendung", "Schilfmündung", K, 60, 70, "delta", 1100f, 3, 4, "Ein Flussdelta voller Inseln, Mangroven und Flößer.",
                new[] { "Kern_Mangrovenschreck", "Moor_Spucker", "Kern_Pirat" }, new[] { "Reiher", "Kroete" },
                new[] { "Flößerhafen", "Mangrovengürtel", "Schilfinseln", "Rohrdommelbank", "Alte Schleuse", "Deltaturm", "Salzwiesen", "Krebsbucht" },
                new[] { "Flößerin Jule|Binde Flöße und rette Treibende.|Floß|Treibender|Hilf Flößerin Jule", "Schleusenwart Eik|Repariere die Schleuse und räume Treibgut.|Schleuse|Treibgut|Unterstütze Schleusenwart Eik",
                        "Reiherfreundin Lis|Säubere Nester und verbinde Vögel.|Nest|Vogel|Hilf Reiherfreundin Lis", "Krebsfischer Hal|Stelle Reusen und sammle Krebse.|Reuse|Krebs|Unterstütze Krebsfischer Hal" },
                "Mangrovenschrecken", "Flößerhafen", new Rect(0.62f, 0.96f, 0.16f, 0.04f), "#6a9a7a", "Der Skimmer gleitet über die Kanäle, Flöße verbinden die Inseln.", "Boot-Vorbereitung (Flöße), Skimmer-Pfade"),
            Z("weisse_stille", "Weiße Stille", K, 70, 80, "eis", 1200f, 3, 5, "Gletscher, Eishöhlen und eine Polarnacht ohne Ende.",
                new[] { "Frost_Eiswolf", "Frost_Huter", "Kern_Frostriese" }, new[] { "Schneehase", "Bergziege" },
                new[] { "Heiligtum der Urskai", "Gletscherzunge", "Eishöhle", "Polarlichtgrat", "Frostgrab", "Schneefeldlager", "Kristallbruch", "Stiller See" },
                new[] { "Eisfischerin Sif|Bohre Eislöcher und rette Erfrierende.|Eisloch|Erfrierender|Hilf Eisfischerin Sif", "Urskai-Hüter Bran|Entzünde Feuerschalen und vertreibe Wölfe.|Feuerschale|Fährte|Unterstütze Urskai-Hüter Bran",
                        "Kartografin Edda|Setze Markierungen und berge Ausrüstung.|Markierung|Ausrüstung|Hilf Kartografin Edda", "Schmied Grim|Sammle Eiserz und repariere Schlitten.|Eiserz|Schlitten|Unterstütze Schmied Grim" },
                "Frostriesen", "Heiligtum der Urskai", new Rect(0.82f, 0.0f, 0.16f, 0.04f), "#e8f0f8", "Der Schneesturm kehrt in Zyklen zurück – Feuerschalen wärmen.", "Schneesturm-Zyklus, Weltboss Gletscherwächter"),
            Z("schaedelkueste", "Schädelküste", K, 75, 80, "ruinen", 1100f, 3, 6, "Eine versunkene Ruinenstadt, in die die Verderbnis sickert.",
                new[] { "Kern_Untoter", "Feste_Garde", "Wald_Schattenpriester" }, new[] { "Aschekaefer", "Kroete" },
                new[] { "Versunkener Tempel", "Schädelhafen", "Knochenstrand", "Ertrunkene Gasse", "Verderbter Brunnen", "Grabstraße", "Gezeitentor", "Riff der Toten" },
                new[] { "Exorzistin Vala|Reinige Altäre und verbinde Gezeichnete.|Altar|Gezeichneter|Hilf Exorzistin Vala", "Totengräber Ulf|Bette Gebeine und verriegle Gräber.|Gebein|Grab|Unterstütze Totengräber Ulf",
                        "Taucher Nils|Berge Reliquien und sichere Stege.|Reliquie|Steg|Hilf Taucher Nils", "Priesterin Oda|Entzünde Schutzfeuer und sammle Salz.|Schutzfeuer|Salz|Unterstütze Priesterin Oda" },
                "Untote der Schädelküste", "Schädelhafen", new Rect(0.84f, 0.26f, 0.12f, 0.04f), "#5a4a6a", "Verderbnis-Widerstand schützt im Inneren der Ruinen.", "Verderbnis-Widerstand, Tempel-Meta, Weltboss Tempelwächter"),

            // ---------------- Glutwüste (6) ----------------
            Z("sonnentor", "Sonnentor", G, 80, 80, "oase", 800f, 0, 2, "Die Oasenstadt mit Basaren, Karawanserei, Rennbahn und Hafen.",
                new[] { "Wueste_Sandgeist", "Duene_Sandwurm", "Wueste_Flammenkultist" }, new[] { "Wuestenspringmaus", "Sandechse" },
                new[] { "Großer Basar", "Karawanserei", "Rennbahn", "Oasenhafen", "Kopfgeldhalle", "Palmenhain", "Sonnenturm", "Stallhof" },
                new[] { "Basarmeisterin Leyla|Trage Waren und repariere Stände.|Ware|Stand|Hilf Basarmeisterin Leyla", "Stallmeister Amir|Tränke Dünenläufer und flicke Sättel.|Tränke|Sattel|Unterstütze Stallmeister Amir",
                        "Hafenschreiber Idris|Zähle Ladung und sichere Taue.|Ladung|Tau|Hilf Hafenschreiber Idris", "Gärtnerin Suha|Wässere Palmen und verbinde Arbeiter.|Palme|Arbeiter|Unterstütze Gärtnerin Suha" },
                "Sandgeister", "Sonnentor", new Rect(0.38f, 0.12f, 0.22f, 0.2f), "#e8c890", "Kopfgeldtafel, Rennleitung und Stallmeister – alles am Großen Basar.", "Hub, Kopfgeldtafel, Stallmeister Dünenläufer"),
            Z("grosse_leere", "Große Leere", G, 80, 80, "duenen", 1300f, 4, 4, "Endlose Dünen, Sandstürme und versunkene Riesen-Skelette.",
                new[] { "Wueste_Sandgeist", "Duene_Sandwurm", "Wueste_Flammenkultist" }, new[] { "Wuestenspringmaus", "Sandechse" },
                new[] { "Riesenrippe", "Dünenlager", "Sturmauge", "Wanderdüne", "Glasgrab", "Nomadenbrunnen", "Schädelsenke", "Leere Straße" },
                new[] { "Karawanenführerin Nadira|Sammle verlorene Waren und verbinde Träger.|Ware|Träger|Hilf Karawanenführerin Nadira", "Knochenleser Sabir|Grabe Riesenknochen aus und stütze Gruben.|Knochen|Grube|Unterstütze Knochenleser Sabir",
                        "Brunnenbauerin Huda|Grabe Brunnen frei und vertreibe Sandgeister.|Brunnen|Siegel|Hilf Brunnenbauerin Huda", "Dünenläufer-Zähmerin Rima|Fange Läufer ein und flicke Zaumzeug.|Läufer|Zaum|Unterstütze Zähmerin Rima" },
                "Sandgeister", "Nomaden der Leere", new Rect(0.06f, 0.36f, 0.34f, 0.3f), "#e4c070", "Im Sandsturm sieht man kaum – Dünenläufer finden den Weg.", "Raptor- und Dünenläufer-Weiten, Rennstrecke 1, Kopfgelder"),
            Z("klingenschlucht", "Klingenschlucht", G, 80, 80, "schlucht", 1100f, 4, 5, "Canyons, Hochplateaus und Felsbrücken über tiefen Schluchten.",
                new[] { "Wueste_Flammenkultist", "Wueste_Knochenkonstrukt", "Himmel_Sturmgreif" }, new[] { "Sandechse", "Bergziege" },
                new[] { "Felsbrücke", "Klingenkamm", "Hochplateau Ras", "Schattenschlucht", "Adlerhorst", "Rote Wand", "Echokessel", "Kultistenlager" },
                new[] { "Brückenbauer Yusuf|Spanne Seile und repariere Planken.|Seil|Planke|Hilf Brückenbauer Yusuf", "Kletterin Zara|Setze Haken und rette Abgestürzte.|Haken|Gestürzter|Unterstütze Kletterin Zara",
                        "Hirte Malik|Treibe Ziegen zusammen und wehre Greifen ab.|Ziege|Greif|Hilf Hirte Malik", "Späherin Noor|Markiere Pfade und sammle Kultzeichen.|Pfad|Zeichen|Unterstütze Späherin Noor" },
                "Flammenkult", "Hochplateau Ras", new Rect(0.42f, 0.38f, 0.26f, 0.26f), "#c07040", "Sandportale verbinden die Plateaus – nur mit dem Dünenläufer.", "Springer- und Greif-Gelände, Sandportale"),
            Z("spiegel_der_sonne", "Spiegel der Sonne", G, 80, 80, "salz", 1200f, 4, 5, "Salzflächen, Luftspiegelungen und Ruinenstädte am Horizont.",
                new[] { "Wueste_Sandgeist", "Duene_Glaselementar", "Wueste_Knochenkonstrukt" }, new[] { "Sandechse", "Reiher" },
                new[] { "Salzspiegel", "Archiv des Ordens", "Trugstadt", "Kristallkruste", "Salzsiederei", "Sonnenuhr", "Gläserner Steg", "Weiße Kuppel" },
                new[] { "Salzsiederin Farah|Siede Salz und repariere Pfannen.|Salz|Pfanne|Hilf Salzsiederin Farah", "Archivar Ilyas|Sammle Schriftrollen und entziffere Tafeln.|Schriftrolle|Tafel|Unterstütze Archivar Ilyas",
                        "Glasschleiferin Dalia|Schleife Linsen und vertreibe Elementare.|Linse|Splitter|Hilf Glasschleiferin Dalia", "Führer Tarek|Setze Wegsteine und rette Verirrte.|Wegstein|Verirrter|Unterstütze Führer Tarek" },
                "Glaselementare", "Salzsiederei", new Rect(0.68f, 0.1f, 0.28f, 0.26f), "#f0f0e0", "Luftspiegelungen zeigen Wege, die nur aus bestimmten Winkeln sichtbar sind.", "Skimmer-Gleiten, Luftspiegelungen, Weltboss Salzkoloss"),
            Z("glutkessel", "Glutkessel", G, 80, 80, "glutkessel", 1100f, 4, 5, "Lavaschluchten und der Tempel des Flammenkönigs.",
                new[] { "Wueste_Flammenkultist", "Wueste_Flammenpriester", "Glut_Golem" }, new[] { "Salamander", "Aschekaefer" },
                new[] { "Flammentempel", "Lavaschlucht", "Kultbasar", "Aschentreppe", "Glutbrücke", "Opferplatz", "Schwarze Säulen", "Feuerquelle" },
                new[] { "Widerständlerin Samira|Sabotiere Kultaltäre und befreie Gefangene.|Altar|Gefangener|Hilf Widerständlerin Samira", "Wasserträger Jamal|Trage Wasser und lösche Brände.|Wasser|Brand|Unterstütze Wasserträger Jamal",
                        "Heilerin Nur|Verbinde Verbrannte und sammle Kühlmoos.|Verbrannter|Moos|Hilf Heilerin Nur", "Schmuggler Kadir|Verstecke Vorräte und stiehl Kultbanner.|Vorrat|Banner|Unterstütze Schmuggler Kadir" },
                "Flammenkult", "Widerstand", new Rect(0.44f, 0.68f, 0.24f, 0.28f), "#c04020", "Der Kult belagert den Flammentempel in drei Phasen.", "Story-Kapitel 4–6, Meta Belagerung des Flammentempels"),
            Z("zarathis", "Zarathis", G, 80, 80, "zarathis", 1300f, 4, 6, "Die Verlorene Stadt hinter einer Knochenmauer.",
                new[] { "Wueste_Flammenpriester", "Wueste_Knochenkonstrukt", "Wueste_Sandgeist" }, new[] { "Sandechse", "Aschekaefer" },
                new[] { "Knochenmauer", "Thronviertel", "Händlerviertel", "Tempelviertel", "Hafenviertel", "Palast des Zarath", "Gefallene Kuppel", "Renntor" },
                new[] { "Hauptfrau Rana|Errichte Barrikaden und versorge Verwundete.|Barrikade|Verwundeter|Hilf Hauptfrau Rana", "Kundschafter Omid|Öffne Tore und markiere Fallen.|Tor|Falle|Unterstütze Kundschafter Omid",
                        "Priesterin Layla|Reinige Schreine und sammle Relikte.|Schrein|Relikt|Hilf Priesterin Layla", "Sappeur Bahram|Lege Sprengladungen und berge Werkzeug.|Ladung|Werkzeug|Unterstütze Sappeur Bahram" },
                "Armee des Flammenkönigs", "Expedition", new Rect(0.06f, 0.70f, 0.34f, 0.28f), "#7a3a2a", "Höchste Gefahrenstufe: Rufer holen Verstärkung, Flieger werden vom Himmel geholt.", "Finale-Meta Fall von Zarathis, Rennstrecke 2"),

            // ---------------- Dracheninseln (6) ----------------
            Z("jadehafen", "Jadehafen", D, 80, 80, "jadestadt", 800f, 0, 2, "Die Vertikalstadt in drei Ebenen mit Aufzügen und Jade-Technik.",
                new[] { "Jade_Pirat", "Jade_Konstrukt", "Jade_Abtruenniger" }, new[] { "Jadekranich", "Bambusmarder" },
                new[] { "Hafenebene", "Mittelstadt", "Oberstadt", "Jadegeist-Werkstatt", "Anglerverband", "Bootswerft", "Kaiserlicher Garten", "Laternenmarkt" },
                new[] { "Werkstattgehilfe Bo|Sortiere Jade-Kerne und repariere Aufzüge.|Kern|Aufzug|Hilf Werkstattgehilfe Bo", "Anglerin Mei-Lan|Sortiere Fang und flicke Netze.|Fang|Netz|Unterstütze Anglerin Mei-Lan",
                        "Laternenmacher Ren|Hänge Laternen auf und repariere Leitungen.|Laterne|Leitung|Hilf Laternenmacher Ren", "Werftmeisterin Kiri|Trage Planken und kalfatere Boote.|Planke|Boot|Unterstütze Werftmeisterin Kiri" },
                "Jade-Piraten", "Jadehafen", new Rect(0.40f, 0.40f, 0.2f, 0.2f), "#5ac8a0", "Aufzüge verbinden Hafen, Mittelstadt und Oberstadt.", "Hub, Jadegeist-Werkstatt, Anglerverband, Bootswerft"),
            Z("gruene_flut", "Grüne Flut", D, 80, 80, "jademeer", 1300f, 4, 4, "Ein erstarrtes Jademeer mit Inseln und Schiffswracks.",
                new[] { "Jade_Pirat", "Jade_Konstrukt", "Kueste_Harpyie" }, new[] { "Jadekranich", "Reiher" },
                new[] { "Jade-Mine", "Wrackfeld", "Kranichinsel", "Rissfeld", "Laternenriff", "Fischerdorf Wan", "Grüner Spiegel", "Strahlender Riss" },
                new[] { "Minenvorarbeiter Gao|Berge Jade und stütze Stollen.|Jade|Stollen|Hilf Vorarbeiter Gao", "Fischerin Ana|Flicke Netze und rette Gestrandete.|Netz|Gestrandeter|Unterstütze Fischerin Ana",
                        "Leuchtfeuerwärter Hu|Entzünde Laternen und kennzeichne Risse.|Laterne|Riss|Hilf Leuchtfeuerwärter Hu", "Taucherin Yun|Berge Wrackgut und sammle Perlen.|Wrackgut|Perle|Unterstütze Taucherin Yun" },
                "Jade-Piraten", "Fischerdorf Wan", new Rect(0.04f, 0.04f, 0.34f, 0.3f), "#40a080", "Mit dem Boot erreichst du die Angelstellen am besten.", "Boot-Zone, Angelstellen, Jade-Minen-Events"),
            Z("fluesterhain", "Flüsterhain", D, 80, 80, "bambus", 1100f, 4, 5, "Bambus- und Riesenbaumwald mit Klöstern und Geisterwegen.",
                new[] { "Jade_Geisterwesen", "Wald_Dornenschreck", "Jade_Abtruenniger" }, new[] { "Bambusmarder", "Jadekranich" },
                new[] { "Kloster der Stille", "Geisterweg", "Bambusmeer", "Mondteich", "Glockenhang", "Uralter Baum", "Schreinpfad", "Nebeltor" },
                new[] { "Schwester Mei|Läute Glocken und reinige Schreine.|Glocke|Schrein|Hilf Schwester Mei", "Novize Tam|Sammle Bambus und verbinde Pilger.|Bambus|Pilger|Unterstütze Novize Tam",
                        "Gärtner Lo|Pflege Teiche und vertreibe Geister.|Teich|Geist|Hilf Gärtner Lo", "Holzschnitzerin Fen|Schnitze Talismane und repariere Stege.|Talisman|Steg|Unterstütze Holzschnitzerin Fen" },
                "Geisterwesen", "Kloster der Stille", new Rect(0.62f, 0.04f, 0.34f, 0.3f), "#3a7a5a", "Geisterwege sind nur nachts oder mit dem Jadegeist sichtbar.", "Kloster-Herzen, Geister-Jagd, Sprungrätsel im Wald"),
            Z("sonnenstufen", "Sonnenstufen", D, 80, 80, "terrassen", 1200f, 4, 5, "Reisterrassen, Dörfer und Festungen der Kaiserin.",
                new[] { "Jade_Abtruenniger", "Jade_Konstrukt", "Jade_Pirat" }, new[] { "Jadekranich", "Bambusmarder" },
                new[] { "Kaiserliche Festung", "Terrassendorf", "Festwiese", "Wasserrad", "Pagode", "Reisspeicher", "Kirschhang", "Wachturm" },
                new[] { "Reisbäuerin Lian|Pflanze Reis und repariere Dämme.|Setzling|Damm|Hilf Reisbäuerin Lian", "Müller Dao|Repariere Wasserräder und trage Säcke.|Rad|Sack|Unterstütze Müller Dao",
                        "Festmeisterin Pei|Hänge Banner auf und entzünde Laternen.|Banner|Laterne|Hilf Festmeisterin Pei", "Hauptmann Daro|Sabotiere Patrouillen und befreie Bauern.|Patrouille|Bauer|Unterstütze Hauptmann Daro" },
                "Kaiserliche Abtrünnige", "Terrassendorf", new Rect(0.04f, 0.66f, 0.34f, 0.3f), "#90c060", "Kaiserliche Patrouillen kontrollieren die Wege.", "Kaiserliche Patrouillen, Festival-Events, Angeln in Reisfeldern"),
            Z("nebelkueste", "Nebelküste", D, 80, 80, "nebelkueste", 1100f, 4, 5, "Zerklüftete Küste im Nebel, übersät mit Drachenknochen.",
                new[] { "Jade_Pirat", "Jade_Geisterwesen", "Himmel_Wolkenschlange" }, new[] { "Jadekranich", "Kroete" },
                new[] { "Drachenrippe", "Nebelhafen", "Piratenfeste", "Knochenbucht", "Schwarzer Fels", "Schlangenhöhle", "Grauer Strand", "Sturmwarte" },
                new[] { "Knochenschnitzer Wei|Sammle Drachenknochen und stütze Hütten.|Knochen|Hütte|Hilf Knochenschnitzer Wei", "Lotsin Hana|Setze Bojen und rette Schiffbrüchige.|Boje|Schiffbrüchiger|Unterstütze Lotsin Hana",
                        "Kundschafter Jin|Kartiere Höhlen und stiehl Piratenbeute.|Höhle|Beute|Hilf Kundschafter Jin", "Heiler Shu|Verbinde Verletzte und sammle Nebelkraut.|Verletzter|Kraut|Unterstütze Heiler Shu" },
                "Piraten der Nebelküste", "Nebelhafen", new Rect(0.62f, 0.66f, 0.34f, 0.3f), "#708090", "Im Nebel lauern Piraten – und eine gewaltige Schlange.", "Story-Kapitel 4–6, Weltboss Nebelschlange"),
            Z("herz_der_flut", "Herz der Flut", D, 80, 80, "drachenende", 1200f, 4, 6, "Eine Meeresarena aus Jade-Plattformen im Sturm.",
                new[] { "Jade_Konstrukt", "Jade_Geisterwesen", "Jade_Abtruenniger" }, new[] { "Jadekranich", "Reiher" },
                new[] { "Plattform Nord", "Plattform Ost", "Plattform Süd", "Plattform West", "Sturmauge", "Kanonenring", "Drachenmaul", "Letzter Steg" },
                new[] { "Kanonierin Yi|Lade Jade-Kanonen und repariere Lafetten.|Ladung|Lafette|Hilf Kanonierin Yi", "Matrose Kun|Sichere Taue und rette Über-Bord-Gegangene.|Tau|Matrose|Unterstütze Matrose Kun",
                        "Jade-Technikerin Lu|Lade Kerne und flicke Leitungen.|Kern|Leitung|Hilf Technikerin Lu", "Wachoffizier Tao|Errichte Barrikaden und vertreibe Konstrukte.|Barrikade|Konstrukt|Unterstütze Wachoffizier Tao" },
                "Erwachende Flut", "Kaiserliche Flotte", new Rect(0.40f, 0.70f, 0.2f, 0.28f), "#2a6a7a", "Vier Plattformen, eine Boot-Anfahrt, ein erwachender Drache.", "Finale-Meta Erwachen des Meeresdrachen"),
        };

        static readonly Dictionary<string, BzLayout> _layouts = new Dictionary<string, BzLayout>();

        public static RegionZoneSpec Get(string id) { foreach (var s in All) if (s.Id == id) return s; return null; }
        public static IEnumerable<RegionZoneSpec> InRegion(WorldRegion r) { foreach (var s in All) if (s.Region == r) yield return s; }

        /// <summary>Zoneneingang (Spawn) einer Regions-Zone: Süden der Zone.</summary>
        public static Vector3 Spawn(RegionZoneSpec s) => new Vector3(0f, 0f, -s.Size * 0.38f);

        /// <summary>Vollständiger Zonen-Inhalt aus der Spezifikation (deterministisch über den Seed, einmal je Zone erzeugt).</summary>
        public static BzLayout Layout(string id)
        {
            if (_layouts.TryGetValue(id, out var cached)) return cached;
            var s = Get(id); if (s == null) return null;
            var rng = new System.Random(s.Seed);
            float half = s.Size * 0.4f;
            var used = new List<Vector2>();
            Vector2 spawn = new Vector2(0f, -s.Size * 0.38f);
            used.Add(spawn);
            Vector2 Pick(float minDist, float t01)
            {
                // t01: gewünschte Tiefe in die Zone (0 = Eingang, 1 = hinterer Rand) – Level steigen von Süd nach Nord
                for (int tries = 0; tries < 400; tries++)
                {
                    float z = Mathf.Lerp(-half, half, Mathf.Clamp01(t01 + (float)(rng.NextDouble() - 0.5) * 0.35f));
                    float x = (float)(rng.NextDouble() * 2 - 1) * half;
                    var p = new Vector2(x, z);
                    bool ok = true; foreach (var u in used) if (Vector2.Distance(u, p) < minDist) { ok = false; break; }
                    if (!ok) continue;
                    used.Add(p); return p;
                }
                var f = new Vector2((float)(rng.NextDouble() * 2 - 1) * half, (float)(rng.NextDouble() * 2 - 1) * half); used.Add(f); return f;
            }
            int Lv(Vector2 p) => Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(s.LevelMin, s.LevelMax, Mathf.InverseLerp(-half, half, p.y))), s.LevelMin, s.LevelMax);
            string P(int i) => s.Places[i % s.Places.Length];
            string Slug(string t) => t.ToLowerInvariant().Replace(" ", "_").Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss").Replace("-", "_").Replace(".", "");

            var l = new BzLayout { ZoneId = s.Id, Archetypes = s.Archetypes };
            var wps = new List<BzSpot>();
            var first = new Vector2(spawn.x + 12f, spawn.y + 18f); used.Add(first);
            wps.Add(new BzSpot { Id = "wp_" + s.Id + "_ankunft", Name = "Wegmarke " + s.Name, Pos = first, Level = s.LevelMin, Desc = "Ankunft in " + s.Name + "." });
            for (int i = 0; i < 4; i++) { var p = Pick(110f, (i + 1) / 5f); wps.Add(new BzSpot { Id = "wp_" + s.Id + "_" + Slug(P(i)), Name = "Wegmarke " + P(i), Pos = p, Level = Lv(p), Desc = P(i) + "." }); }
            l.Waypoints = wps.ToArray();
            var pois = new List<BzSpot>();
            for (int i = 0; i < 5; i++) { var p = Pick(80f, i / 4f); pois.Add(new BzSpot { Id = "poi_" + s.Id + "_" + Slug(P(i + 3)), Name = P(i + 3), Pos = p, Level = Lv(p), Desc = s.Description }); }
            l.Pois = pois.ToArray();
            var vis = new List<BzSpot>();
            for (int i = 0; i < 3; i++) { var p = Pick(90f, (i + 0.5f) / 3f); vis.Add(new BzSpot { Id = "vista_" + s.Id + "_" + i, Name = "Aussicht " + P(i + 1), Pos = p, Level = Lv(p) }); }
            l.Vistas = vis.ToArray();
            var ch = new List<BzSpot>();
            var kinds = new[] { ZoneContent.ChallengeKind.Communion, ZoneContent.ChallengeKind.Combat, ZoneContent.ChallengeKind.Relic, ZoneContent.ChallengeKind.Communion };
            string[] chText = { "Kommuniziere mit dem Geist des Ortes.", "Rufe einen Wächter herbei und besiege ihn.", "Berühre das Relikt und überstehe den Angriff.", "Lausche dem Wind an der alten Stätte." };
            for (int i = 0; i < 4; i++) { var p = Pick(80f, (i + 0.5f) / 4f); ch.Add(new BzSpot { Id = "hc_" + s.Id + "_" + i, Name = "Herausforderung: " + P(i + 5), Pos = p, Level = Lv(p), Kind = kinds[i], Desc = chText[i] }); }
            l.Challenges = ch.ToArray();
            var ins = new List<BzSpot>();
            for (int i = 0; i < 3; i++) { var p = Pick(70f, (i + 0.3f) / 3f); ins.Add(new BzSpot { Id = "insight_" + s.Id + "_" + i, Name = "Beherrschungs-Einblick: " + P(i + 2), Pos = p, Level = Lv(p), Desc = "Ein Ort voller Erinnerungen." }); }
            l.Insights = ins.ToArray();
            int tier = Mathf.Clamp(Mathf.RoundToInt(s.LevelMax / 10f) * 10, 40, 80);
            var hs = new List<BzHeart>();
            for (int i = 0; i < s.Hearts.Length; i++)
            {
                var f = s.Hearts[i].Split('|');
                var c = Pick(120f, (i + 0.4f) / s.Hearts.Length);
                string karma = "karma_" + s.Id + "_" + i;
                hs.Add(new BzHeart { Id = "heart_" + s.Id + "_" + i, Name = f[4], Npc = f[0], Task = f[1], Center = c, NpcPos = c + new Vector2(-10f, 12f), Level = Lv(c),
                    Tasks = new[] { (ZoneContent.TaskKind.Pickup, 4, f[2]), (i % 2 == 0 ? ZoneContent.TaskKind.Repair : ZoneContent.TaskKind.Bandage, 3, f[3]) },
                    VendorId = karma, VendorName = f[0] + " (Karma)", Stock = new[] { ("ar_l" + tier + "_chest", 2400, 1), ("tr_amulet_l" + tier, 2400, 1), ("potion_big", 50, 5), ("food_soup", 60, 5) } });
            }
            l.Hearts = hs.ToArray();
            var camps = new List<BzCamp>();
            if (!s.Hub)
                for (int i = 0; i < 10; i++)
                {
                    var p = Pick(60f, 0.15f + i * 0.085f);
                    string a = s.Archetypes[i % s.Archetypes.Length];
                    camps.Add(new BzCamp { Archetype = a, Pos = p, Count = i % 3 == 2 ? 1 : 3 });
                }
            l.Camps = camps.ToArray();
            var cr = new List<BzCritter>();
            for (int i = 0; i < 4; i++) cr.Add(new BzCritter { Kind = s.Critters[i % s.Critters.Length], Pos = Pick(40f, i / 3f), Count = 4 });
            l.Critters = cr.ToArray();
            l.Updrafts = new[] { Pick(50f, 0.5f), Pick(50f, 0.8f) };
            l.Chests = new (string, Vector2)[0];
            _layouts[id] = l;
            return l;
        }

        /// <summary>Höchste Gefahrenstufe am Eingang je Region (Kernland 3, Erweiterungen 4).</summary>
        public static int MaxEntryDanger(WorldRegion r) => r == WorldRegion.Kernland ? 3 : 4;

        public static string RegionName(WorldRegion r) => r == WorldRegion.Kernland ? "Kernland" : r == WorldRegion.Glutwueste ? "Glutwüste" : "Dracheninseln";
    }
}
