using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Procedural
{
    /// <summary>Gelände-Sperre eines Inhalts (vom Builder ins Gelände geformt): Tafelberg (Springer), Insel (Skimmer), Lava-Ring (Raptor).</summary>
    public enum SpotGate { None, Mesa, Island, LavaRing }

    public class BzSpot
    {
        public string Id, Name, Desc;
        public Vector2 Pos;
        public int Level;
        public SpotGate Gate;
        public float GateRadius = 16f;
        public ZoneContent.ChallengeKind Kind;
        /// <summary>Erwartete Reittier-Anforderung (wird vom Builder gegen die Flutfüllung geprüft).</summary>
        public Traversal Expect => Gate == SpotGate.Mesa ? Traversal.Springer : Gate == SpotGate.Island ? Traversal.Skimmer : Gate == SpotGate.LavaRing ? Traversal.Raptor : Traversal.None;
    }

    public class BzHeart
    {
        public string Id, Name, Npc, Task, VendorId, VendorName;
        public Vector2 Center, NpcPos;
        public float Radius = 30f, KillValue = 0.06f, TaskValue = 0.1f;
        public int Level;
        public (ZoneContent.TaskKind kind, int count, string label)[] Tasks;
        public (string item, int karma, int count)[] Stock;
    }

    public class BzCamp { public string Archetype; public Vector2 Pos; public int Count; }
    public class BzCritter { public string Kind; public Vector2 Pos; public int Count; }

    public class BzLayout
    {
        public string ZoneId;
        public BzSpot[] Waypoints, Pois, Vistas, Challenges, Insights;
        public BzHeart[] Hearts;
        public BzCamp[] Camps;
        public BzCritter[] Critters;
        public string[] Archetypes;
        // Beherrschungs-Umwelt (M18)
        public (Vector2 pos, float radius)[] Mists = new (Vector2, float)[0];
        public Vector2[] Mushrooms = new Vector2[0];
        public Vector2[] Updrafts = new Vector2[0];
        public (string id, Vector2 pos)[] Chests = new (string, Vector2)[0];

        public IEnumerable<BzSpot> AllSpots()
        {
            foreach (var a in new[] { Waypoints, Pois, Vistas, Challenges, Insights }) foreach (var s in a) yield return s;
        }
    }

    /// <summary>
    /// Inhalte der drei Biom-Zonen (reine Daten, vom Builder und den Tests gelesen): je ≥ 4 Wegmarken, ≥ 4 Sehenswürdigkeiten,
    /// ≥ 3 Aussichtspunkte, ≥ 4 Heldenherausforderungen, ≥ 3 Herzen, ≥ 3 Beherrschungs-Einblicke, ≥ 2 Tierarten, ≥ 3 neue Gegnertypen.
    /// </summary>
    public static partial class BiomeZoneContent
    {
        static BzSpot S(string id, string name, float x, float z, int lvl, string desc = "", SpotGate gate = SpotGate.None, float gateR = 16f) =>
            new BzSpot { Id = id, Name = name, Pos = new Vector2(x, z), Level = lvl, Desc = desc, Gate = gate, GateRadius = gateR };
        static BzSpot C(string id, string name, float x, float z, int lvl, ZoneContent.ChallengeKind k, string desc, SpotGate gate = SpotGate.None) =>
            new BzSpot { Id = id, Name = name, Pos = new Vector2(x, z), Level = lvl, Kind = k, Desc = desc, Gate = gate };
        static BzCamp K(string a, float x, float z, int n) => new BzCamp { Archetype = a, Pos = new Vector2(x, z), Count = n };
        static BzCritter A(string k, float x, float z, int n) => new BzCritter { Kind = k, Pos = new Vector2(x, z), Count = n };

        const ZoneContent.ChallengeKind Com = ZoneContent.ChallengeKind.Communion, Fight = ZoneContent.ChallengeKind.Combat, Relic = ZoneContent.ChallengeKind.Relic;
        const ZoneContent.TaskKind Pick = ZoneContent.TaskKind.Pickup, Fix = ZoneContent.TaskKind.Repair, Band = ZoneContent.TaskKind.Bandage;

        public static readonly BzLayout Nebelmoor = new BzLayout
        {
            ZoneId = "nebelmoor",
            Archetypes = new[] { "Moor_Schleicher", "Moor_Spucker", "Moor_Koloss" },
            Waypoints = new[]
            {
                S("wp_moor_torfsteg", "Wegmarke Torfsteg", 14, -352, 15, "Ankunft am Portal aus Sonnfurt."),
                S("wp_moor_schilf", "Wegmarke Schilfwacht", -250, -150, 17, "Zwischen Schilfgürteln am Westkanal."),
                S("wp_moor_nebelkreuz", "Wegmarke Nebelkreuz", 60, 20, 20, "Kreuzung der alten Bohlenwege."),
                S("wp_moor_kroetenfurt", "Wegmarke Krötenfurt", 300, -150, 19, "An der Furt der Fährleute."),
                S("wp_moor_weidenhoehe", "Wegmarke Weidenhöhe", -120, 300, 23, "Auf der trockenen Weidenhöhe im Norden."),
            },
            Pois = new[]
            {
                S("poi_moor_torfstecher", "Torfstecher-Siedlung", -70, -300, 15, "Hütten auf Pfählen, Rauch aus Torffeuern."),
                S("poi_moor_wachturm", "Versunkener Wachturm", -300, 40, 18, "Ein schiefer Turm, halb im Moor versunken."),
                S("poi_moor_irrlichter", "Insel der Irrlichter", 190, 170, 21, "Nur über tiefes Wasser erreichbar.", SpotGate.Island, 34f),
                S("poi_moor_kroetenfurt", "Krötenfurt", 340, -60, 19, "Fährsteg über den Ostkanal."),
                S("poi_moor_weiden", "Hängende Weiden", -60, 360, 24, "Uralte Weiden, deren Äste bis ins Wasser reichen."),
            },
            Vistas = new[]
            {
                S("vista_moor_pfahlturm", "Pfahlturm am Torfsteg", 130, -250, 16),
                S("vista_moor_weidenkrone", "Weidenkrone", -330, 220, 22),
                S("vista_moor_nebelspitze", "Nebelspitze", 290, 300, 24),
            },
            Challenges = new[]
            {
                C("hc_moor_quelle", "Moorquelle", -180, -280, 16, Com, "Kommuniziere 5 s mit der sprudelnden Moorquelle."),
                C("hc_moor_herausforderer", "Herausforderung des Koloss", 220, -260, 18, Fight, "Rufe einen Moorkoloss herbei und besiege ihn."),
                C("hc_moor_moorleiche", "Verfluchte Moorleiche", -20, 170, 21, Relic, "Berühre das Moorgrab und überstehe den Fluch."),
                C("hc_moor_irrlicht", "Irrlicht-Schrein", 208, 186, 22, Com, "Auf der Irrlicht-Insel – nur mit dem Skimmer erreichbar.", SpotGate.Island),
            },
            Insights = new[]
            {
                S("insight_moor_sumpfgas", "Beherrschungs-Einblick: Sumpfgas", -200, -60, 18, "Studiere die aufsteigenden Gasblasen."),
                S("insight_moor_insel", "Beherrschungs-Einblick: Versunkener Altar", 172, 154, 21, "Auf der Irrlicht-Insel.", SpotGate.Island),
                S("insight_moor_weidenwurzel", "Beherrschungs-Einblick: Weidenwurzel", 380, 250, 24, "Die Wurzeln flüstern von alten Pfaden."),
            },
            Hearts = new[]
            {
                new BzHeart { Id = "heart_moor_ilse", Name = "Hilf Torfstecherin Ilse", Npc = "Torfstecherin Ilse", Task = "Sammle Sumpfkraut, repariere die Bohlenstege und vertreibe Moorschleicher.",
                    Center = new Vector2(-40, -250), NpcPos = new Vector2(-52, -236), Level = 16, VendorId = "karma_ilse", VendorName = "Torfstecherin Ilse (Karma)",
                    Tasks = new[] { (Pick, 4, "Sumpfkraut"), (Fix, 3, "Bohlensteg") }, Stock = new[] { ("gs_moor_mw", 900, 1), ("st_moor_mw", 900, 1), ("ar_moor_chest", 800, 1), ("food_soup", 60, 5) } },
                new BzHeart { Id = "heart_moor_merle", Name = "Unterstütze Fährfrau Merle", Npc = "Fährfrau Merle", Task = "Bandagiere verletzte Fischer, birg Reusen und halte Spucker fern.",
                    Center = new Vector2(300, -95), NpcPos = new Vector2(288, -76), Level = 19, VendorId = "karma_merle", VendorName = "Fährfrau Merle (Karma)",
                    Tasks = new[] { (Band, 3, "Fischer"), (Pick, 3, "Reuse") }, Stock = new[] { ("gs_moor_rare", 1600, 1), ("st_moor_rare", 1600, 1), ("tr_ring_moor", 1400, 1), ("ar_moor_legs", 800, 1) } },
                new BzHeart { Id = "heart_moor_albin", Name = "Hilf Kräuterkundigem Albin", Npc = "Kräuterkundiger Albin", Task = "Flicke die Weidenzäune und sammle Moorbeeren.",
                    Center = new Vector2(-150, 255), NpcPos = new Vector2(-138, 240), Level = 23, VendorId = "karma_albin", VendorName = "Kräuterkundiger Albin (Karma)",
                    Tasks = new[] { (Fix, 3, "Weidenzaun"), (Pick, 4, "Moorbeeren") }, Stock = new[] { ("ar_moor_head", 800, 1), ("ar_moor_feet", 800, 1), ("food_stew", 40, 5), ("potion_big", 50, 5) } },
            },
            Camps = new[]
            {
                K("Moor_Schleicher", -140, -330, 3), K("Moor_Schleicher", 90, -180, 3), K("Moor_Spucker", -260, -60, 2), K("Moor_Schleicher", -320, 120, 3),
                K("Moor_Spucker", 250, -200, 2), K("Moor_Koloss", 380, -20, 1), K("Moor_Spucker", 20, 100, 3), K("Moor_Koloss", -60, 230, 1),
                K("Moor_Schleicher", 150, 300, 3), K("Moor_Spucker", -240, 340, 2), K("Moor_Koloss", 330, 380, 1), K("Moor_Schleicher", 220, 60, 2),
            },
            Mists = new[] { (new Vector2(-200, -60), 18f), (new Vector2(120, 60), 15f), (new Vector2(-280, 320), 16f) },
            Mushrooms = new[] { new Vector2(-90, -300), new Vector2(40, 40) },
            Chests = new[] { ("chest_moor_weide", new Vector2(-330, 238)) },
            Critters = new[] { A("Reiher", -100, -150, 4), A("Reiher", 230, 0, 3), A("Kroete", 0, -300, 5), A("Kroete", -200, 200, 5), A("Kroete", 330, -120, 4) },
        };

        public static readonly BzLayout Frostkamm = new BzLayout
        {
            ZoneId = "frostkamm",
            Archetypes = new[] { "Frost_Eiswolf", "Frost_Huter", "Frost_Werfer" },
            Waypoints = new[]
            {
                S("wp_frost_ankunft", "Wegmarke Firnpforte", 24, -410, 25, "Ankunft aus dem Nebelmoor."),
                S("wp_frost_lager", "Wegmarke Bergsteigerlager", -260, -220, 25, "Am Lager der Bergführer."),
                S("wp_frost_pass", "Wegmarke Kammpass", 120, -60, 27, "Auf dem windigen Pass."),
                S("wp_frost_mine", "Wegmarke Kristallmine", 380, -260, 26, "Am Eingang der Kristallmine."),
                S("wp_frost_tempel", "Wegmarke Eiswind", -180, 240, 29, "Unterhalb des Eiswindtempels."),
            },
            Pois = new[]
            {
                S("poi_frost_lager", "Bergsteigerlager", -240, -270, 25, "Zelte, Seile und ein wärmendes Feuer."),
                S("poi_frost_horst", "Adlerhorst", 230, 120, 28, "Ein Tafelberg – nur mit dem Springer erreichbar.", SpotGate.Mesa, 20f),
                S("poi_frost_mine", "Kristallmine", 360, -200, 26, "Blau schimmernde Stollen im Fels."),
                S("poi_frost_tempel", "Eiswindtempel", -260, 160, 29, "Säulen, um die der Wind singt."),
                S("poi_frost_schlund", "Schlund des Frostes", 40, 330, 30, "Hier erwacht der Frostschlund."),
            },
            Vistas = new[]
            {
                S("vista_frost_balkon", "Gletscherbalkon", -100, -330, 25),
                S("vista_frost_firnsaeule", "Firnsäule", 300, 300, 29, "Auf einem Tafelberg.", SpotGate.Mesa, 14f),
                S("vista_frost_lawinenwacht", "Lawinenwacht", -380, -20, 27),
            },
            Challenges = new[]
            {
                C("hc_frost_gletscher", "Gletscherherz", 100, -300, 25, Com, "Kommuniziere mit dem Herz des Gletschers."),
                C("hc_frost_herausforderer", "Herausforderung des Hüters", -60, 60, 27, Fight, "Rufe einen Frosthüter herbei – greife ihn von der Seite an."),
                C("hc_frost_relikt", "Gefrorenes Relikt", 300, -60, 27, Relic, "Berühre das Relikt und überstehe die Eiswölfe."),
                C("hc_frost_grat", "Gratschrein", -340, 320, 29, Com, "Auf einem Tafelberg – nur mit dem Springer erreichbar.", SpotGate.Mesa),
            },
            Insights = new[]
            {
                S("insight_frost_eisblume", "Beherrschungs-Einblick: Eisblume", 0, -200, 26, "Eine Blume, die nie taut."),
                S("insight_frost_horst", "Beherrschungs-Einblick: Adlerfeder", 238, 112, 28, "Auf dem Adlerhorst.", SpotGate.Mesa),
                S("insight_frost_spalte", "Beherrschungs-Einblick: Gletscherspalte", -420, -300, 26, "Tief unten glimmt altes Eis."),
            },
            Hearts = new[]
            {
                new BzHeart { Id = "heart_frost_runa", Name = "Hilf Bergführerin Runa", Npc = "Bergführerin Runa", Task = "Sammle Brennholz, versorge erfrorene Bergsteiger und halte Eiswölfe fern.",
                    Center = new Vector2(-215, -200), NpcPos = new Vector2(-226, -186), Level = 25, VendorId = "karma_runa", VendorName = "Bergführerin Runa (Karma)",
                    Tasks = new[] { (Pick, 4, "Brennholz"), (Band, 3, "Bergsteiger") }, Stock = new[] { ("gs_frost_rare", 1800, 1), ("st_frost_rare", 1800, 1), ("tr_back_frost", 1500, 1), ("ar_frost_chest", 1300, 1) } },
                new BzHeart { Id = "heart_frost_bodo", Name = "Unterstütze Steiger Bodo", Npc = "Steiger Bodo", Task = "Stütze die Stollen ab und birg Kristallerz.",
                    Center = new Vector2(330, -160), NpcPos = new Vector2(318, -146), Level = 26, VendorId = "karma_bodo", VendorName = "Steiger Bodo (Karma)",
                    Tasks = new[] { (Fix, 3, "Stützbalken"), (Pick, 4, "Kristallerz") }, Stock = new[] { ("ar_frost_head", 1300, 1), ("ar_frost_shoulders", 1300, 1), ("ar_frost_legs", 1300, 1), ("potion_big", 50, 5) } },
                new BzHeart { Id = "heart_frost_saga", Name = "Hilf Hüterin Saga", Npc = "Hüterin Saga", Task = "Stimme die Windglocken neu und sammle Eisblumen.",
                    Center = new Vector2(-200, 185), NpcPos = new Vector2(-188, 170), Level = 29, VendorId = "karma_saga", VendorName = "Hüterin Saga (Karma)",
                    Tasks = new[] { (Fix, 3, "Windglocke"), (Pick, 3, "Eisblume") }, Stock = new[] { ("ar_frost_hands", 1300, 1), ("ar_frost_feet", 1300, 1), ("food_soup", 60, 5), ("food_pie", 40, 5) } },
            },
            Camps = new[]
            {
                K("Frost_Eiswolf", -120, -380, 3), K("Frost_Werfer", 180, -250, 2), K("Frost_Eiswolf", -330, -120, 4), K("Frost_Huter", 60, -140, 1),
                K("Frost_Werfer", 420, -100, 2), K("Frost_Eiswolf", 200, 20, 3), K("Frost_Huter", -380, 80, 1), K("Frost_Werfer", -100, 180, 2),
                K("Frost_Eiswolf", 120, 220, 4), K("Frost_Huter", 380, 200, 1), K("Frost_Werfer", -300, 260, 2), K("Frost_Eiswolf", -40, 420, 3),
            },
            Updrafts = new[] { new Vector2(100, -250), new Vector2(-300, -40), new Vector2(150, 280) },
            Mushrooms = new[] { new Vector2(-80, -360), new Vector2(330, -120) },
            Chests = new[] { ("chest_frost_horst", new Vector2(222, 128)) },
            Critters = new[] { A("Schneehase", -60, -280, 4), A("Schneehase", 260, -120, 4), A("Bergziege", -300, 0, 3), A("Bergziege", 160, 160, 3), A("Schneehase", -120, 300, 3) },
        };

        public static readonly BzLayout Gluetoede = new BzLayout
        {
            ZoneId = "gluetoede",
            Archetypes = new[] { "Glut_Kriecher", "Glut_Beschwoerer", "Glut_Golem" },
            Waypoints = new[]
            {
                S("wp_glut_ankunft", "Wegmarke Aschentor", -436, 30, 30, "Ankunft aus dem Frostkamm."),
                S("wp_glut_feld", "Wegmarke Schlackenfeld", -200, -200, 30, "Inmitten erkalteter Lavaströme."),
                S("wp_glut_schmiede", "Wegmarke Glutschmiede", 0, 60, 30, "An der Schmiede der Sturmreiter."),
                S("wp_glut_krater", "Wegmarke Kraterrand", 250, -150, 30, "Am Rand des großen Kraters."),
                S("wp_glut_sturm", "Wegmarke Sturmauge", 220, 260, 30, "Vor dem Auge des Sturms."),
            },
            Pois = new[]
            {
                S("poi_glut_lager", "Lager der Sturmreiter", -380, -80, 30, "Letzter Außenposten vor dem Sturm."),
                S("poi_glut_esse", "Verlassene Esse", -60, -280, 30, "Von Lava umschlossen – nur mit dem Raptor erreichbar.", SpotGate.LavaRing, 18f),
                S("poi_glut_obsidian", "Obsidianfeld", 40, 170, 30, "Schwarzes Glas, scharf wie Klingen."),
                S("poi_glut_krater", "Glutkrater", 310, -220, 30, "Der Boden bebt unter der Hitze."),
                S("poi_glut_auge", "Auge des Sturms", 250, 340, 30, "Hier tobt das Meta-Ereignis."),
            },
            Vistas = new[]
            {
                S("vista_glut_aschenkamm", "Aschenkamm", -300, 180, 30),
                S("vista_glut_schlackenturm", "Schlackenturm", 120, -50, 30, "Auf einem Tafelberg.", SpotGate.Mesa, 14f),
                S("vista_glut_brandwacht", "Brandwacht", 420, 40, 30),
            },
            Challenges = new[]
            {
                C("hc_glut_aschequelle", "Aschequelle", -250, -330, 30, Com, "Kommuniziere mit der glimmenden Quelle."),
                C("hc_glut_herausforderer", "Herausforderung des Golems", 150, 70, 30, Fight, "Rufe einen Lavagolem herbei und besiege ihn."),
                C("hc_glut_glutsiegel", "Glutsiegel", 380, -380, 30, Relic, "Im Lava-Ring – nur mit dem Raptor erreichbar.", SpotGate.LavaRing),
                C("hc_glut_sturmstein", "Sturmstein", -120, 330, 30, Com, "Kommuniziere mit dem Sturmstein."),
            },
            Insights = new[]
            {
                S("insight_glut_obsidian", "Beherrschungs-Einblick: Glasader", -150, 40, 30, "Feine Adern aus Obsidian."),
                S("insight_glut_esse", "Beherrschungs-Einblick: Erloschene Glut", -52, -272, 30, "In der verlassenen Esse.", SpotGate.LavaRing),
                S("insight_glut_brandnarbe", "Beherrschungs-Einblick: Brandnarbe", 450, -300, 30, "Der Fels trägt die Spuren alter Stürme."),
            },
            Hearts = new[]
            {
                new BzHeart { Id = "heart_glut_kaya", Name = "Hilf Späherin Kaya", Npc = "Späherin Kaya", Task = "Sammle Glutkristalle und versorge verletzte Späher.",
                    Center = new Vector2(-330, -40), NpcPos = new Vector2(-342, -26), Level = 30, VendorId = "karma_kaya", VendorName = "Späherin Kaya (Karma)",
                    Tasks = new[] { (Pick, 4, "Glutkristall"), (Band, 3, "Späher") }, Stock = new[] { ("gs_glut_exo", 3000, 1), ("st_glut_exo", 3000, 1), ("ar_glut_chest", 2400, 1), ("tr_acc_glut", 2400, 1) } },
                new BzHeart { Id = "heart_glut_torvald", Name = "Unterstütze Schmied Torvald", Npc = "Schmied Torvald", Task = "Repariere die Kühlrinnen und birg Glutlerz.",
                    Center = new Vector2(30, 20), NpcPos = new Vector2(18, 34), Level = 30, VendorId = "karma_torvald", VendorName = "Schmied Torvald (Karma)",
                    Tasks = new[] { (Fix, 3, "Kühlrinne"), (Pick, 3, "Glutlerz") }, Stock = new[] { ("ar_glut_head", 2400, 1), ("ar_glut_shoulders", 2400, 1), ("ar_glut_legs", 2400, 1), ("ar_glut_hands", 2400, 1) } },
                new BzHeart { Id = "heart_glut_oleg", Name = "Hilf Sturmwächter Oleg", Npc = "Sturmwächter Oleg", Task = "Richte die Blitzableiter auf und versorge Verwundete.",
                    Center = new Vector2(180, 215), NpcPos = new Vector2(168, 200), Level = 30, VendorId = "karma_oleg", VendorName = "Sturmwächter Oleg (Karma)",
                    Tasks = new[] { (Fix, 3, "Blitzableiter"), (Band, 3, "Verwundete") }, Stock = new[] { ("ar_glut_feet", 2400, 1), ("tr_amulet_glut", 2600, 1), ("potion_big", 50, 5), ("food_soup", 60, 5) } },
            },
            Camps = new[]
            {
                K("Glut_Kriecher", -300, -200, 4), K("Glut_Beschwoerer", -150, -120, 1), K("Glut_Kriecher", -100, 120, 3), K("Glut_Golem", 80, -160, 1),
                K("Glut_Kriecher", 200, -60, 4), K("Glut_Beschwoerer", 380, -120, 1), K("Glut_Golem", -220, 280, 1), K("Glut_Kriecher", 60, 280, 3),
                K("Glut_Beschwoerer", 300, 160, 1), K("Glut_Golem", 440, -220, 1), K("Glut_Kriecher", -380, 120, 3), K("Glut_Kriecher", 150, -330, 3),
            },
            Updrafts = new[] { new Vector2(-250, 150), new Vector2(330, 100) },
            Chests = new[] { ("chest_glut_esse", new Vector2(-66, -288)) },
            Critters = new[] { A("Aschekaefer", -350, 40, 5), A("Aschekaefer", 100, 100, 5), A("Salamander", -180, -260, 3), A("Salamander", 280, -80, 3), A("Aschekaefer", 200, 300, 4) },
        };

        public static BzLayout[] All => new[] { Nebelmoor, Frostkamm, Gluetoede, Silberkueste, Daemmerwald, Duenenmeer, Kristalltiefe, Himmelsspitzen, Tyrannenfeste };   // Lauf 6: Eigenschaft (Teil-Klassen-Initialisierung)
        public static BzLayout For(string zoneId) { foreach (var l in All) if (l.ZoneId == zoneId) return l; return null; }
    }
}
