using UnityEngine;

namespace ProjectTyrant.Procedural
{
    /// <summary>Lauf 6 / M30 + M32: Inhalte der sechs neuen Zonen (≥ 4 Wegmarken, ≥ 4 Sehenswürdigkeiten, 3 Aussichtspunkte, 4 Heldenherausforderungen, 3 Einblicke, 3 Herzen; das vierte Herz kommt aus world.csv).</summary>
    public static partial class BiomeZoneContent
    {
        static BzHeart H(string id, string name, string npc, string task, float cx, float cz, int lvl, (ZoneContent.TaskKind, int, string)[] tasks, string vendor) =>
            new BzHeart { Id = id, Name = name, Npc = npc, Task = task, Center = new Vector2(cx, cz), NpcPos = new Vector2(cx - 10, cz + 12), Level = lvl, VendorId = vendor, VendorName = npc + " (Karma)",
                Tasks = tasks, Stock = new[] { ("ar_glut_chest", 2400, 1), ("tr_acc_glut", 2400, 1), ("potion_big", 50, 5), ("food_soup", 60, 5) } };

        public static readonly BzLayout Silberkueste = new BzLayout
        {
            ZoneId = "silberkueste",
            Archetypes = new[] { "Kueste_Schmuggler", "Kueste_Harpyie", "Kueste_Archivwaechter" },
            Waypoints = new[]
            {
                S("wp_kueste_silbermund", "Wegmarke Silbermund", 0, 22, 30, "Am Portalplatz der Hafenstadt."),
                S("wp_kueste_hafen", "Wegmarke Hafenmole", 90, -50, 30, "Zwischen Kränen und Fischkisten."),
                S("wp_kueste_leuchtturm", "Wegmarke Leuchtturmklippe", 260, 160, 30, "Unter dem alten Leuchtturm."),
                S("wp_kueste_grotte", "Wegmarke Grottenpfad", -230, -170, 30, "Oberhalb der Schmugglergrotten."),
                S("wp_kueste_waldtor", "Wegmarke Waldtor", -30, 360, 30, "Am Weg in den Dämmerwald."),
            },
            Pois = new[]
            {
                S("poi_kueste_portalplatz", "Portalplatz", 0, 40, 30, "Neun Portale im Kreis – in jede Zone des Kontinents."),
                S("poi_kueste_buendnis", "Bündnis-Viertel", -60, 80, 30, "Wacht, Archiv und Schleier unter einem Dach. Fast."),
                S("poi_kueste_markt", "Markt der hundert Segel", -40, 10, 30, "Stände, Gewürze, Lärm."),
                S("poi_kueste_grotte", "Schmugglergrotte", -260, -210, 30, "Nasse Höhlen unter der Steilküste."),
                S("poi_kueste_leuchtturm", "Alter Leuchtturm", 280, 190, 30, "Sein Licht ist seit Jahren aus."),
            },
            Vistas = new[]
            {
                S("vista_kueste_mole", "Molenkopf", 140, -90, 30),
                S("vista_kueste_steilkueste", "Steilküsten-Balkon", -320, 40, 30),
                S("vista_kueste_inselfels", "Inselfels", 330, -260, 30, "Auf einer Felsinsel – nur mit dem Skimmer erreichbar.", SpotGate.Island, 26f),
            },
            Challenges = new[]
            {
                C("hc_kueste_brandung", "Brandungsstein", 200, 40, 30, Com, "Kommuniziere mit dem Stein, an dem die Wellen brechen."),
                C("hc_kueste_koenigin", "Herausforderung der Schmuggler", -180, -260, 30, Fight, "Rufe einen Archivwächter herbei und besiege ihn."),
                C("hc_kueste_wrack", "Verfluchtes Wrack", 320, 60, 30, Relic, "Berühre das Wrackholz und überstehe die Harpyien."),
                C("hc_kueste_insel", "Muschelschrein", 350, -240, 30, Com, "Auf dem Inselfels – nur mit dem Skimmer erreichbar.", SpotGate.Island),
            },
            Insights = new[]
            {
                S("insight_kueste_muschel", "Beherrschungs-Einblick: Singende Muschel", 120, 260, 30, "Sie flüstert vom Meer."),
                S("insight_kueste_anker", "Beherrschungs-Einblick: Alter Anker", -120, -320, 30, "Ein Anker so groß wie ein Haus."),
                S("insight_kueste_insel", "Beherrschungs-Einblick: Gezeitenstein", 312, -275, 30, "Auf dem Inselfels.", SpotGate.Island),
            },
            Hearts = new[]
            {
                H("heart_kueste_hafen", "Hilf Hafenmeister Gorran", "Hafenmeister Gorran", "Belade die Schiffe, flicke Netze und versorge verletzte Hafenarbeiter.", 90, -30, 30,
                    new[] { (Pick, 4, "Frachtkiste"), (Fix, 3, "Netz"), (Band, 2, "Hafenarbeiter") }, "karma_gorran"),
                H("heart_kueste_markt", "Unterstütze Marktfrau Ysolde", "Marktfrau Ysolde", "Sammle verlorene Waren ein, repariere die Stände und vertreibe Hafenratten.", -40, 10, 30,
                    new[] { (Pick, 4, "Ware"), (Fix, 3, "Marktstand") }, "karma_ysolde"),
                H("heart_kueste_leuchtturm", "Hilf Wärter Bastian", "Wärter Bastian", "Repariere die Linsen, sammle Treibholz für das Feuer und halte die Harpyien fern.", 270, 170, 30,
                    new[] { (Fix, 3, "Linse"), (Pick, 4, "Treibholz") }, "karma_bastian"),
            },
            Camps = new[]
            {
                K("Kueste_Schmuggler", -250, -190, 4), K("Kueste_Schmuggler", -300, -120, 3), K("Kueste_Harpyie", -340, 60, 3), K("Kueste_Harpyie", 300, 120, 2),
                K("Kueste_Archivwaechter", -200, -280, 1), K("Kueste_Schmuggler", 200, -200, 3), K("Kueste_Harpyie", 360, 280, 2), K("Kueste_Archivwaechter", 180, 300, 1),
                K("Kueste_Schmuggler", -150, 250, 3), K("Kueste_Harpyie", 50, 330, 2),
            },
            Mushrooms = new[] { new Vector2(-300, 20), new Vector2(240, 140) },
            Chests = new[] { ("chest_kueste_leuchtturm", new Vector2(292, 198)) },
            Critters = new[] { A("Reiher", 150, -120, 4), A("Reiher", -280, 100, 3), A("Kroete", -100, 300, 3) },
        };

        public static readonly BzLayout Daemmerwald = new BzLayout
        {
            ZoneId = "daemmerwald",
            Archetypes = new[] { "Wald_Wildtier", "Wald_Dornenschreck", "Wald_Schattenpriester" },
            Waypoints = new[]
            {
                S("wp_wald_ankunft", "Wegmarke Waldsaum", 10, -400, 30, "Ankunft von der Silberküste."),
                S("wp_wald_lichtung", "Wegmarke Lichtung", -160, -180, 30, "Am Dorf auf der Lichtung."),
                S("wp_wald_hain", "Wegmarke Alter Hain", 230, 120, 30, "Wo die ältesten Bäume stehen."),
                S("wp_wald_stein", "Wegmarke Wächterstein", 20, 280, 30, "Am alten Wächterstein."),
            },
            Pois = new[]
            {
                S("poi_wald_lichtung", "Dorf auf der Lichtung", -150, -200, 30, "Hütten im Kreis um ein Laternenfeuer."),
                S("poi_wald_hain", "Der Alte Hain", 200, 150, 30, "Bäume so dick wie Türme."),
                S("poi_wald_stein", "Wächterstein", 0, 300, 30, "Ein Stein voller Zeichen aus der Zeit vor den Karten."),
                S("poi_wald_sumpf", "Violetter Grund", -260, 120, 30, "Hier ist die Verderbnis am dichtesten."),
                S("poi_wald_ruine", "Zerfallene Kapelle", 300, -220, 30, "Moos frisst die Mauern."),
            },
            Vistas = new[]
            {
                S("vista_wald_wipfel", "Wipfelpfad", -60, -60, 30),
                S("vista_wald_kapelle", "Kapellenturm", 320, -250, 30),
                S("vista_wald_tafel", "Moosfelsen", -320, 300, 30, "Auf einem Tafelberg.", SpotGate.Mesa, 16f),
            },
            Challenges = new[]
            {
                C("hc_wald_quelle", "Klare Quelle", 120, -300, 30, Com, "Kommuniziere mit der letzten klaren Quelle."),
                C("hc_wald_priester", "Herausforderung des Priesters", -260, 40, 30, Fight, "Rufe einen Schattenpriester herbei und besiege ihn."),
                C("hc_wald_dornen", "Dornenaltar", 260, 280, 30, Relic, "Berühre den Altar und überstehe die Dornenschrecken."),
                C("hc_wald_tafel", "Mooskrone", -300, 320, 30, Com, "Auf dem Moosfelsen – nur mit dem Springer erreichbar.", SpotGate.Mesa),
            },
            Insights = new[]
            {
                S("insight_wald_laterne", "Beherrschungs-Einblick: Ewige Laterne", -80, -250, 30, "Sie brennt seit Jahrhunderten."),
                S("insight_wald_wurzel", "Beherrschungs-Einblick: Violette Wurzel", -230, 160, 30, "Die Verderbnis pulsiert darin."),
                S("insight_wald_tafel", "Beherrschungs-Einblick: Moossiegel", -312, 290, 30, "Auf dem Moosfelsen.", SpotGate.Mesa),
            },
            Hearts = new[]
            {
                H("heart_wald_fenn", "Hilf Ältestem Fenn", "Ältester Fenn", "Entzünde die Laternen, sammle Lichtmoos und versorge Verletzte.", -140, -230, 30,
                    new[] { (Fix, 3, "Laterne"), (Pick, 4, "Lichtmoos"), (Band, 2, "Verletzter") }, "karma_fenn"),
                H("heart_wald_hira", "Unterstütze Jägerin Hira", "Jägerin Hira", "Sammle Pfeilholz und repariere die Hochsitze.", 160, -80, 30,
                    new[] { (Pick, 4, "Pfeilholz"), (Fix, 3, "Hochsitz") }, "karma_hira"),
                H("heart_wald_oda", "Hilf Kräuterfrau Oda", "Kräuterfrau Oda", "Sammle Heilkräuter und verbinde verwundete Waldläufer.", -40, 200, 30,
                    new[] { (Pick, 4, "Heilkraut"), (Band, 3, "Waldläufer") }, "karma_oda"),
            },
            Camps = new[]
            {
                K("Wald_Wildtier", -60, -320, 3), K("Wald_Dornenschreck", -260, 100, 4), K("Wald_Schattenpriester", -300, 160, 1), K("Wald_Wildtier", 120, -200, 3),
                K("Wald_Dornenschreck", 240, 260, 3), K("Wald_Schattenpriester", 280, 200, 1), K("Wald_Wildtier", -120, 120, 3), K("Wald_Dornenschreck", 60, 360, 3),
                K("Wald_Wildtier", 340, -60, 3), K("Wald_Dornenschreck", -340, -200, 3),
            },
            Mists = new[] { (new Vector2(-260, 120), 22f), (new Vector2(220, 240), 18f) },
            Chests = new[] { ("chest_wald_tafel", new Vector2(-328, 308)) },
            Critters = new[] { A("Kroete", -100, -100, 4), A("Kroete", 200, 0, 4), A("Reiher", 120, -300, 2) },
        };

        public static readonly BzLayout Duenenmeer = new BzLayout
        {
            ZoneId = "duenenmeer",
            Archetypes = new[] { "Duene_Raeuber", "Duene_Sandwurm", "Duene_Glaselementar" },
            Waypoints = new[]
            {
                S("wp_duene_ankunft", "Wegmarke Glutpass", -440, -20, 30, "Ankunft aus der Glutöde."),
                S("wp_duene_oase", "Wegmarke Oase Sahim", -190, -80, 30, "Palmen und Zelte am Wasser."),
                S("wp_duene_qasir", "Wegmarke Qasir", 230, 180, 30, "Vor den Ruinen von Qasir."),
                S("wp_duene_schlucht", "Wegmarke Drei Schluchten", 20, -60, 30, "Am Rand der ersten Schlucht."),
                S("wp_duene_glas", "Wegmarke Glasebene", 300, -280, 30, "Wo Blitze den Sand zu Glas schmelzen."),
            },
            Pois = new[]
            {
                S("poi_duene_oase", "Oase Sahim", -210, -110, 30, "Karawanenzelte im Schatten."),
                S("poi_duene_qasir", "Ruinen von Qasir", 260, 210, 30, "Säulen ragen aus dem Sand."),
                S("poi_duene_schlucht", "Drei Schluchten", 60, -20, 30, "Ein Raptor springt darüber."),
                S("poi_duene_glas", "Glasebene", 320, -250, 30, "Funkelnd und scharf."),
                S("poi_duene_wurm", "Wurmgrube", -60, 300, 30, "Der Sand bewegt sich hier."),
            },
            Vistas = new[]
            {
                S("vista_duene_saeule", "Säule von Qasir", 300, 240, 30),
                S("vista_duene_duenenkamm", "Dünenkamm", -300, 200, 30),
                S("vista_duene_tafel", "Wüstentafel", 120, 360, 30, "Auf einem Tafelberg.", SpotGate.Mesa, 16f),
            },
            Challenges = new[]
            {
                C("hc_duene_brunnen", "Alter Brunnen", -260, -200, 30, Com, "Kommuniziere mit dem alten Brunnen der Oase."),
                C("hc_duene_elementar", "Herausforderung des Glases", 260, -180, 30, Fight, "Rufe ein Glas-Elementar herbei und besiege es."),
                C("hc_duene_grab", "Grab des Sultans", 180, 300, 30, Relic, "Berühre den Sarkophag und überstehe die Räuber."),
                C("hc_duene_tafel", "Windschrein", 140, 380, 30, Com, "Auf der Wüstentafel – nur mit dem Springer erreichbar.", SpotGate.Mesa),
            },
            Insights = new[]
            {
                S("insight_duene_glas", "Beherrschungs-Einblick: Blitzglas", 340, -300, 30, "Ein Blitz, zu Glas erstarrt."),
                S("insight_duene_knochen", "Beherrschungs-Einblick: Wurmknochen", -80, 320, 30, "Ein Rippenbogen, groß wie ein Tor."),
                S("insight_duene_tafel", "Beherrschungs-Einblick: Sternkarte", 112, 352, 30, "Auf der Wüstentafel.", SpotGate.Mesa),
            },
            Hearts = new[]
            {
                H("heart_duene_nadira", "Hilf Karawanenführerin Nadira", "Karawanenführerin Nadira", "Sammle verstreute Waren, repariere Zelte und versorge erschöpfte Träger.", -200, -60, 30,
                    new[] { (Pick, 4, "Ware"), (Fix, 3, "Zelt"), (Band, 2, "Träger") }, "karma_nadira"),
                H("heart_duene_qasir", "Unterstütze Archäologin Imra", "Archäologin Imra", "Grab Scherben aus und stütze die einsturzgefährdeten Säulen.", 220, 150, 30,
                    new[] { (Pick, 4, "Scherbe"), (Fix, 3, "Säule") }, "karma_imra"),
                H("heart_duene_glas", "Hilf Glasbläser Faris", "Glasbläser Faris", "Sammle Blitzglas und verbinde verbrannte Sammler.", 290, -230, 30,
                    new[] { (Pick, 4, "Blitzglas"), (Band, 3, "Sammler") }, "karma_faris"),
            },
            Camps = new[]
            {
                K("Duene_Raeuber", -300, -40, 4), K("Duene_Sandwurm", -60, 280, 2), K("Duene_Glaselementar", 330, -300, 1), K("Duene_Raeuber", 200, 260, 4),
                K("Duene_Sandwurm", 100, 80, 2), K("Duene_Glaselementar", 260, 120, 1), K("Duene_Raeuber", -120, -260, 3), K("Duene_Sandwurm", -320, 260, 2),
                K("Duene_Raeuber", 380, 20, 3), K("Duene_Glaselementar", -20, -340, 1),
            },
            Chests = new[] { ("chest_duene_tafel", new Vector2(128, 368)) },
            Critters = new[] { A("Salamander", -150, 100, 4), A("Salamander", 250, -50, 4), A("Aschekaefer", 0, 200, 4) },
        };

        public static readonly BzLayout Kristalltiefe = new BzLayout
        {
            ZoneId = "kristalltiefe",
            Archetypes = new[] { "Tiefe_Kristallkrabbler", "Tiefe_Tiefenschreck", "Tiefe_BlinderJaeger" },
            Waypoints = new[]
            {
                S("wp_tiefe_eingang", "Wegmarke Höhlenmund", 10, -360, 30, "Am Eingang unter den Hügeln."),
                S("wp_tiefe_see", "Wegmarke Schwarzer See", -150, -40, 30, "Am Ufer des unterirdischen Sees."),
                S("wp_tiefe_pilze", "Wegmarke Pilzwald", 200, -120, 30, "Zwischen Pilzen so hoch wie Bäume."),
                S("wp_tiefe_herz", "Wegmarke Kristallherz", 120, 200, 30, "Vor der großen Kristallkammer."),
            },
            Pois = new[]
            {
                S("poi_tiefe_see", "Schwarzer See", -180, -10, 30, "Kein Licht dringt bis zum Grund."),
                S("poi_tiefe_pilze", "Pilzwald", 230, -150, 30, "Hüpfpilze federn jeden Schritt."),
                S("poi_tiefe_bruecke", "Kristallbrücke", 0, 80, 30, "Eine Brücke aus gewachsenem Kristall."),
                S("poi_tiefe_kammer", "Kammer der Stimme", 150, 150, 30, "Hier spricht etwas, das schlafen sollte."),
                S("poi_tiefe_mine", "Verlassene Zwergenmine", -280, 250, 30, "Loren, Gleise, Stille."),
            },
            Vistas = new[]
            {
                S("vista_tiefe_kuppel", "Kristallkuppel", 260, 60, 30),
                S("vista_tiefe_mine", "Förderturm", -300, 280, 30),
                S("vista_tiefe_insel", "Seeinsel", -200, 40, 30, "Auf einer Insel im See – nur mit dem Skimmer erreichbar.", SpotGate.Island, 24f),
            },
            Challenges = new[]
            {
                C("hc_tiefe_echo", "Echostein", 60, -240, 30, Com, "Kommuniziere mit dem Stein, der jedes Wort zurückgibt."),
                C("hc_tiefe_jaeger", "Herausforderung des Jägers", 280, 240, 30, Fight, "Rufe einen Blinden Jäger herbei – bewege dich leise."),
                C("hc_tiefe_lore", "Verfluchte Lore", -260, 200, 30, Relic, "Berühre die Lore und überstehe die Tiefenschrecken."),
                C("hc_tiefe_insel", "Seekristall", -215, 55, 30, Com, "Auf der Seeinsel – nur mit dem Skimmer erreichbar.", SpotGate.Island),
            },
            Insights = new[]
            {
                S("insight_tiefe_lichtpilz", "Beherrschungs-Einblick: Lichtpilz", 180, -200, 30, "Er leuchtet, wenn man ihn ansieht."),
                S("insight_tiefe_ader", "Beherrschungs-Einblick: Kristallader", 40, 300, 30, "Eine Ader, die wie ein Herz schlägt."),
                S("insight_tiefe_insel", "Beherrschungs-Einblick: Versunkene Laterne", -190, 30, 30, "Auf der Seeinsel.", SpotGate.Island),
            },
            Hearts = new[]
            {
                H("heart_tiefe_brom", "Hilf Bergmann Brom", "Bergmann Brom", "Stütze die Stollen, sammle Leuchtkristalle und rette verschüttete Kumpel.", -240, 220, 30,
                    new[] { (Fix, 3, "Stütze"), (Pick, 4, "Leuchtkristall"), (Band, 2, "Kumpel") }, "karma_brom"),
                H("heart_tiefe_lys", "Unterstütze Forscherin Lys", "Forscherin Lys", "Sammle Pilzproben und repariere die Messgeräte.", 210, -130, 30,
                    new[] { (Pick, 4, "Pilzprobe"), (Fix, 3, "Messgerät") }, "karma_lys"),
                H("heart_tiefe_fischer", "Hilf Höhlenfischer Tamo", "Höhlenfischer Tamo", "Birg Reusen aus dem See und verbinde gebissene Fischer.", -140, -60, 30,
                    new[] { (Pick, 4, "Reuse"), (Band, 3, "Fischer") }, "karma_tamo"),
            },
            Camps = new[]
            {
                K("Tiefe_Kristallkrabbler", 40, -280, 3), K("Tiefe_Tiefenschreck", -100, 100, 3), K("Tiefe_BlinderJaeger", 260, 220, 1), K("Tiefe_Kristallkrabbler", 200, 40, 4),
                K("Tiefe_Tiefenschreck", -280, 160, 2), K("Tiefe_BlinderJaeger", -60, 260, 1), K("Tiefe_Kristallkrabbler", 300, -260, 3), K("Tiefe_Tiefenschreck", 120, -60, 2),
                K("Tiefe_Kristallkrabbler", -320, -200, 3),
            },
            Mushrooms = new[] { new Vector2(220, -160), new Vector2(250, -110), new Vector2(190, -90), new Vector2(-20, 60) },
            Chests = new[] { ("chest_tiefe_insel", new Vector2(-208, 48)) },
            Critters = new[] { A("Aschekaefer", 100, -100, 4), A("Kroete", -150, -20, 3), A("Aschekaefer", -200, 260, 3) },
        };

        public static readonly BzLayout Himmelsspitzen = new BzLayout
        {
            ZoneId = "himmelsspitzen",
            Archetypes = new[] { "Himmel_Sturmgreif", "Himmel_Pirat", "Himmel_Wolkenschlange" },
            Waypoints = new[]
            {
                S("wp_himmel_ankunft", "Wegmarke Wolkentor", 20, -400, 30, "Ankunft aus dem Frostkamm."),
                S("wp_himmel_nest", "Wegmarke Greifennester", -200, 80, 30, "Unter den Nestern der Greifen."),
                S("wp_himmel_tempel", "Wegmarke Himmelstempel", 10, 230, 30, "Vor dem Tempel der Himmelswächter."),
                S("wp_himmel_pirat", "Wegmarke Piratenbucht", 280, -120, 30, "Wo die Luftschiffe ankern."),
            },
            Pois = new[]
            {
                S("poi_himmel_tempel", "Himmelswächter-Tempel", 0, 260, 30, "Säulen aus weißem Stein über den Wolken."),
                S("poi_himmel_nest", "Greifennester", -220, 110, 30, "Riesige Nester an den Klippen."),
                S("poi_himmel_bucht", "Piratenbucht", 300, -150, 30, "Luftschiffe mit geflickten Segeln."),
                S("poi_himmel_windbruecke", "Windbrücke", 100, 0, 30, "Ein Steg, den der Wind trägt."),
                S("poi_himmel_gipfel", "Höchster Gipfel", -300, 340, 30, "Darüber nur noch Himmel."),
            },
            Vistas = new[]
            {
                S("vista_himmel_tempel", "Tempelspitze", 40, 300, 30),
                S("vista_himmel_bucht", "Mastkorb", 340, -200, 30),
                S("vista_himmel_gipfel", "Wolkenthron", -320, 360, 30, "Auf einem Tafelberg.", SpotGate.Mesa, 16f),
            },
            Challenges = new[]
            {
                C("hc_himmel_wind", "Windharfe", -100, -250, 30, Com, "Kommuniziere mit der singenden Windharfe."),
                C("hc_himmel_pirat", "Herausforderung der Piraten", 260, -60, 30, Fight, "Rufe eine Wolkenschlange herbei und besiege sie."),
                C("hc_himmel_wrack", "Luftschiffwrack", 180, 200, 30, Relic, "Berühre das Wrack und überstehe die Piraten."),
                C("hc_himmel_gipfel", "Sternenschrein", -290, 380, 30, Com, "Auf dem Wolkenthron – nur mit dem Springer erreichbar.", SpotGate.Mesa),
            },
            Insights = new[]
            {
                S("insight_himmel_feder", "Beherrschungs-Einblick: Greifenfeder", -180, 140, 30, "Eine Feder, länger als ein Arm."),
                S("insight_himmel_wolke", "Beherrschungs-Einblick: Gefrorene Wolke", 160, -280, 30, "Eine Wolke, die stehen geblieben ist."),
                S("insight_himmel_gipfel", "Beherrschungs-Einblick: Sternenstaub", -312, 352, 30, "Auf dem Wolkenthron.", SpotGate.Mesa),
            },
            Hearts = new[]
            {
                H("heart_himmel_sela", "Hilf Himmelswächterin Sela", "Himmelswächterin Sela", "Repariere die Windglocken, sammle Federn und versorge verletzte Wächter.", 20, 200, 30,
                    new[] { (Fix, 3, "Windglocke"), (Pick, 4, "Feder"), (Band, 2, "Wächter") }, "karma_sela"),
                H("heart_himmel_nest", "Unterstütze Greifenpflegerin Aya", "Greifenpflegerin Aya", "Sammle Nistmaterial und flicke die Nester.", -200, 60, 30,
                    new[] { (Pick, 4, "Nistmaterial"), (Fix, 3, "Nest") }, "karma_aya"),
                H("heart_himmel_bucht", "Hilf Kapitänin Rhea", "Kapitänin Rhea", "Flicke Segel und verbinde Matrosen, die von Piraten getroffen wurden.", 240, -160, 30,
                    new[] { (Fix, 3, "Segel"), (Band, 3, "Matrose") }, "karma_rhea"),
            },
            Camps = new[]
            {
                K("Himmel_Sturmgreif", -100, -200, 2), K("Himmel_Pirat", 300, -100, 4), K("Himmel_Wolkenschlange", 200, 160, 1), K("Himmel_Sturmgreif", -260, 200, 3),
                K("Himmel_Pirat", 160, -240, 3), K("Himmel_Wolkenschlange", -280, 300, 1), K("Himmel_Sturmgreif", 80, 120, 2), K("Himmel_Pirat", 340, 60, 3),
            },
            Updrafts = new[] { new Vector2(0, -320), new Vector2(-150, -40), new Vector2(120, 80), new Vector2(-260, 260), new Vector2(200, -200) },
            Chests = new[] { ("chest_himmel_gipfel", new Vector2(-328, 368)) },
            Critters = new[] { A("Bergziege", -150, -150, 3), A("Schneehase", 100, 250, 4), A("Bergziege", 250, 50, 3) },
        };

        public static readonly BzLayout Tyrannenfeste = new BzLayout
        {
            ZoneId = "tyrannenfeste",
            Archetypes = new[] { "Feste_Garde", "Feste_Siegelbrecher", "Feste_Ritter" },
            Waypoints = new[]
            {
                S("wp_feste_lager", "Wegmarke Belagerungslager", 10, -380, 30, "Das Lager der Bündnisse vor den Toren."),
                S("wp_feste_tore", "Wegmarke Torvorfeld", 0, -260, 30, "Vor den drei Toren."),
                S("wp_feste_hof", "Wegmarke Innerer Hof", 0, 20, 30, "Im Hof der Feste."),
                S("wp_feste_thron", "Wegmarke Thronstiege", 0, 200, 30, "Am Aufgang zum Thronsaal."),
            },
            Pois = new[]
            {
                S("poi_feste_lager", "Belagerungslager", -40, -360, 30, "Zelte der Wacht, des Archivs und des Schleiers."),
                S("poi_feste_tore", "Die drei Tore", 0, -300, 30, "Eisen, Stein, Siegel."),
                S("poi_feste_hof", "Innerer Hof", 0, 0, 30, "Hier fiel der letzte Wächter vor tausend Jahren."),
                S("poi_feste_kammern", "Siegelkammern", -200, 120, 30, "Glühende Runen im Fels."),
                S("poi_feste_thron", "Thronsaal", 0, 260, 30, "Der Thron ist leer. Noch."),
            },
            Vistas = new[]
            {
                S("vista_feste_wehrgang", "Wehrgang", 120, -220, 30),
                S("vista_feste_turm", "Schwarzer Turm", -180, 260, 30),
                S("vista_feste_lava", "Lavafels", 260, 120, 30, "Von Lava umschlossen – nur mit dem Raptor erreichbar.", SpotGate.LavaRing, 18f),
            },
            Challenges = new[]
            {
                C("hc_feste_siegel", "Erstes Siegel", -220, -120, 30, Com, "Kommuniziere mit dem ersten Siegel."),
                C("hc_feste_ritter", "Herausforderung des Ritters", 200, -60, 30, Fight, "Rufe einen Verderbten Ritter herbei und besiege ihn."),
                C("hc_feste_thronwache", "Thronwächter-Relikt", 160, 220, 30, Relic, "Berühre das Relikt und überstehe die Garde."),
                C("hc_feste_lava", "Glutsiegel", 280, 140, 30, Com, "Im Lava-Ring – nur mit dem Raptor erreichbar.", SpotGate.LavaRing),
            },
            Insights = new[]
            {
                S("insight_feste_rune", "Beherrschungs-Einblick: Gebrochene Rune", -240, 160, 30, "Sie flackert wie ein Herzschlag."),
                S("insight_feste_banner", "Beherrschungs-Einblick: Altes Banner", 80, -320, 30, "Das Banner der ersten Wächter."),
                S("insight_feste_lava", "Beherrschungs-Einblick: Erstarrte Krone", 252, 112, 30, "Auf dem Lavafels.", SpotGate.LavaRing),
            },
            Hearts = new[]
            {
                H("heart_feste_lager", "Hilf Feldscherin Marit", "Feldscherin Marit", "Verbinde Verwundete, sammle Verbandszeug und flicke die Zelte des Lagers.", -60, -340, 30,
                    new[] { (Band, 3, "Verwundeter"), (Pick, 4, "Verbandszeug"), (Fix, 2, "Zelt") }, "karma_marit"),
                H("heart_feste_tore", "Unterstütze Rammmeister Ulf", "Rammmeister Ulf", "Repariere die Rammböcke und schaffe Munition heran.", 60, -240, 30,
                    new[] { (Fix, 3, "Rammbock"), (Pick, 4, "Munition") }, "karma_ulf"),
                H("heart_feste_kammern", "Hilf Siegelkundigem Edric", "Siegelkundiger Edric", "Sammle Siegelsplitter und verbinde verwundete Gelehrte.", -200, 80, 30,
                    new[] { (Pick, 4, "Siegelsplitter"), (Band, 3, "Gelehrter") }, "karma_edric"),
            },
            Camps = new[]
            {
                K("Feste_Garde", -60, -260, 4), K("Feste_Siegelbrecher", 60, -280, 2), K("Feste_Ritter", 0, -200, 1), K("Feste_Garde", -150, -20, 3),
                K("Feste_Siegelbrecher", 150, 40, 2), K("Feste_Ritter", 0, 100, 1), K("Feste_Garde", -200, 220, 3), K("Feste_Garde", 200, 220, 3),
                K("Feste_Siegelbrecher", -260, 60, 2), K("Feste_Ritter", 120, 180, 1),
            },
            Chests = new[] { ("chest_feste_lava", new Vector2(272, 128)) },
            Critters = new[] { A("Aschekaefer", -100, -300, 4), A("Salamander", 200, -150, 3) },
        };
    }
}
