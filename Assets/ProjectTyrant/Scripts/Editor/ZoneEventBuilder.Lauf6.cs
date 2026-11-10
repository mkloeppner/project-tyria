using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 6 / M30 + M32: Ereignisse der sechs neuen Zonen (je ≥ 4, je eine Kette). Weltboss-Champions „Der Alte Hain“ (Dämmerwald) und „Himmelswyrm“ (Himmelsspitzen)
    /// mit Zyklus, Sandsturm-Champion im Dünenmeer und das Meta „Belagerung der Feste“ (drei Tore → Hof → Thronsaal, 25-min-Zyklus).
    /// </summary>
    public static partial class ZoneEventBuilder
    {
        static void Lauf6Events(string zone, List<Vector3> crates)
        {
            switch (zone)
            {
                case "silberkueste": Kueste(crates); break;
                case "daemmerwald": Wald(); break;
                case "duenenmeer": Duene(crates); break;
                case "kristalltiefe": Tiefe(); break;
                case "himmelsspitzen": Himmel(); break;
                case "tyrannenfeste": Feste(crates); break;
            }
        }

        static void Kueste(List<Vector3> crates)
        {
            var k1 = E("kueste_netze", "Netze flicken", "Ein Sturm hat die Netze der Hafenfischer zerrissen – flicke sie, bevor die Harpyien die Fische holen.", EventObjective.Build, 120, -60, 30, 240, 4, 30);
            Sites(k1, 4, 10f, "Netz flicken"); k1.Trash = A("Kueste_Harpyie"); k1.Triggers.Add(Prox()); k1.Triggers.Add(Timer(240));
            var k2 = E("kueste_schmuggel", "Schmuggelware abfangen", "Fang die Schmuggler ab, die Kisten über den Grottenpfad tragen.", EventObjective.Kill, -230, -170, 36, 240, 8, 30, "grotte");
            k2.Trash = A("Kueste_Schmuggler"); k2.Triggers.Add(Prox()); k2.Triggers.Add(Timer(300)); k2.SuccessNext = "kueste_koenigin";
            var k3 = E("kueste_koenigin", "Die Schmugglerkönigin", "Die Königin der Grotte zeigt sich – brich ihre Trotzleiste.", EventObjective.Boss, -260, -210, 36, 300, 1, 30, "grotte");
            k3.Boss = A("Kueste_Schmugglerkoenigin"); k3.Trash = A("Kueste_Schmuggler"); k3.Triggers.Add(Link()); k3.Cooldown = 300;
            var k4 = E("kueste_leuchtfeuer", "Leuchtfeuer entzünden", "Entzünde die Leuchtfeuer der Klippen, damit die Schiffe sicher einlaufen.", EventObjective.Use, 270, 170, 30, 210, 4, 30);
            Sites(k4, 4, 11f, "Leuchtfeuer entzünden", 2.5f); k4.Trash = A("Kueste_Harpyie"); k4.Triggers.Add(Prox()); k4.Triggers.Add(Timer(300));
            var k5 = E("kueste_mole", "Die Mole halten", "Halte die Frachtkisten an der Mole gegen anrückende Schmuggler.", EventObjective.Defend, 90, -50, 32, 180, 0, 30);
            k5.Trash = A("Kueste_Schmuggler"); k5.Veteran = A("Kueste_Archivwaechter"); k5.WaveSize = 3; k5.WaveInterval = 20f; k5.Triggers.Add(Prox()); k5.Triggers.Add(Timer(360));
            for (int i = 0; i < 3; i++) crates.Add(V(90 + Mathf.Cos(i * 2.1f) * 5f, -50 + Mathf.Sin(i * 2.1f) * 5f));
        }

        static void Wald()
        {
            var w1 = E("wald_laternen", "Laternen der Lichtung", "Entzünde die Laternen an den Wächtersteinen – das Licht drängt die Verderbnis zurück.", EventObjective.Use, -150, -200, 34, 240, 4, 30, "verderbnis");
            Sites(w1, 4, 12f, "Laterne entzünden", 2.5f); w1.Trash = A("Wald_Dornenschreck"); w1.Triggers.Add(Prox()); w1.Triggers.Add(Timer(240)); w1.SuccessNext = "wald_priester";
            var w2 = E("wald_priester", "Die Schattenpriester unterbrechen", "Besiege die Schattenpriester, bevor ihr Gebet den Grund vergiftet.", EventObjective.Kill, -250, 110, 38, 240, 6, 30, "verderbnis");
            w2.Trash = A("Wald_Dornenschreck"); w2.Veteran = A("Wald_Schattenpriester"); w2.Triggers.Add(Link());
            var w3 = E("wald_keiler", "Verderbte Keiler", "Jage die verderbten Keiler, bevor sie das Dorf überrennen.", EventObjective.Hunt, 140, -200, 40, 300, 8, 30);
            w3.Trash = A("Wald_Wildtier"); w3.Triggers.Add(Prox(25)); w3.Triggers.Add(Timer(360));
            var w4 = E("wald_heilkraut", "Heilkraut für Oda", "Sammle Heilkraut im Unterholz und bring es zu Odas Kessel.", EventObjective.Collect, -40, 200, 34, 240, 6, 30);
            w4.DeliverPoint = V(-50, 212); w4.Trash = A("Wald_Wildtier"); w4.Triggers.Add(Prox()); w4.Triggers.Add(Timer(300));
            // Weltboss-Champion: Der Alte Hain (18-min-Zyklus, Versatz 4 min)
            var w5 = E("wald_alterhain", "Der Alte Hain erwacht", "Der Baumriese im Alten Hain ist erwacht – brich seine Trotzleiste, bevor die Verderbnis ihn ganz frisst.", EventObjective.WorldBoss, 200, 150, 45, 480, 1, 30, "hain");
            w5.Boss = A("Champion_AlterHain"); w5.Trash = A("Wald_Dornenschreck"); w5.Veteran = A("Wald_Schattenpriester"); w5.PhaseThresholds = new[] { 0.5f };
            w5.Triggers.Add(Cycle(1080f, 240f)); w5.MasteryPoints = 1; w5.Cooldown = 60; w5.Experience *= 3; w5.Karma *= 3; w5.Coins *= 4;
            _z.TimerLabel = "Der Alte Hain"; _z.TimerPeriod = 1080f; _z.TimerOffset = 240f; _z.TimerPosition = w5.Center; _z.TimerEventId = "hain";
        }

        static void Duene(List<Vector3> crates)
        {
            var d1 = E("duene_karawane", "Karawane nach Qasir", "Geleite Nadiras Karawane von der Oase bis zu den Ruinen.", EventObjective.Escort, -190, -80, 30, 360, 1, 30);
            d1.Path = new[] { V(-190, -80), V(-60, -20), V(60, 60), V(180, 140), V(230, 180) }; d1.Trash = A("Duene_Raeuber"); d1.WaveSize = 2; d1.WaveInterval = 25f; d1.Triggers.Add(Prox()); d1.Triggers.Add(Timer(420));
            var d2 = E("duene_wuermer", "Wurmplage", "Treib die Sandwürmer aus der Wurmgrube.", EventObjective.Hunt, -60, 300, 40, 300, 8, 30);
            d2.Trash = A("Duene_Sandwurm"); d2.Triggers.Add(Prox(25)); d2.Triggers.Add(Timer(360));
            var d3 = E("duene_saeulen", "Säulen von Qasir stützen", "Stütze die Säulen, bevor die Ruinen einstürzen.", EventObjective.Build, 260, 210, 32, 240, 4, 30, "qasir");
            Sites(d3, 4, 11f, "Säule stützen"); d3.Trash = A("Duene_Raeuber"); d3.Triggers.Add(Prox()); d3.Triggers.Add(Timer(300)); d3.SuccessNext = "duene_oase";
            var d4 = E("duene_oase", "Die Oase verteidigen", "Halte die Wasserschläuche der Oase gegen die Dünenräuber.", EventObjective.Defend, -200, -100, 32, 180, 0, 30, "qasir");
            d4.Trash = A("Duene_Raeuber"); d4.Veteran = A("Duene_Glaselementar"); d4.WaveSize = 3; d4.Triggers.Add(Link());
            for (int i = 0; i < 3; i++) crates.Add(V(-200 + Mathf.Cos(i * 2.1f) * 5f, -100 + Mathf.Sin(i * 2.1f) * 5f));
            // Sandsturm-Champion (Sandsturm-Zyklus 12 min, Sturm 2 min ab Minute 10)
            var d5 = E("duene_sturmwanderer", "Der Sturmwanderer", "Im Sandsturm wandert ein Glasriese über die Ebene – stell ihn, solange der Sturm tobt.", EventObjective.Boss, 300, -260, 40, 240, 1, 30);
            d5.Boss = A("Champion_Sturmwanderer"); d5.Trash = A("Duene_Glaselementar"); d5.Triggers.Add(Cycle(SandstormModel.Period, SandstormModel.StormStart)); d5.Cooldown = 60;
            _z.TimerLabel = "Sandsturm"; _z.TimerPeriod = SandstormModel.Period; _z.TimerOffset = SandstormModel.StormStart; _z.TimerPosition = d5.Center; _z.TimerEventId = null;
        }

        static void Tiefe()
        {
            var t1 = E("tiefe_kristalle", "Leuchtkristalle setzen", "Setze Leuchtkristalle in die Halterungen, damit die Bergleute den Weg finden.", EventObjective.Use, 0, -240, 30, 240, 4, 30, "licht");
            Sites(t1, 4, 10f, "Kristall einsetzen", 2.5f); t1.Trash = A("Tiefe_Kristallkrabbler"); t1.Triggers.Add(Prox()); t1.Triggers.Add(Timer(240)); t1.SuccessNext = "tiefe_kammer";
            var t2 = E("tiefe_kammer", "Das Kristallherz", "Der Wächter des Kristallherzens erwacht – besiege ihn.", EventObjective.Boss, 120, 200, 40, 300, 1, 30, "licht");
            t2.Boss = A("Champion_Kristallherz"); t2.Trash = A("Tiefe_Kristallkrabbler"); t2.Triggers.Add(Link()); t2.Cooldown = 300;
            var t3 = E("tiefe_jagd", "Stille Jagd", "Jage die Tiefenschrecken im Pilzwald – leise.", EventObjective.Hunt, 200, -120, 38, 300, 8, 30);
            t3.Trash = A("Tiefe_Tiefenschreck"); t3.Triggers.Add(Prox(25)); t3.Triggers.Add(Timer(360));
            var t4 = E("tiefe_verschuettet", "Verschüttete Kumpel", "Grab die verschütteten Bergleute in der alten Mine frei.", EventObjective.Build, -260, 230, 32, 240, 4, 30);
            Sites(t4, 4, 10f, "Geröll räumen", 3f); t4.Trash = A("Tiefe_Tiefenschreck"); t4.Triggers.Add(Prox()); t4.Triggers.Add(Timer(300));
        }

        static void Himmel()
        {
            var h1 = E("himmel_eier", "Greifeneier zurückbringen", "Sammle die gestohlenen Greifeneier ein und bring sie ins Nest.", EventObjective.Collect, -200, 90, 36, 240, 5, 30, "piraten");
            h1.DeliverPoint = V(-210, 102); h1.Trash = A("Himmel_Pirat"); h1.Triggers.Add(Prox()); h1.Triggers.Add(Timer(300)); h1.SuccessNext = "himmel_bucht";
            var h2 = E("himmel_bucht", "Angriff auf die Piratenbucht", "Vertreibe die Himmelspiraten aus ihrer Bucht.", EventObjective.Kill, 290, -130, 38, 240, 8, 30, "piraten");
            h2.Trash = A("Himmel_Pirat"); h2.Veteran = A("Himmel_Wolkenschlange"); h2.Triggers.Add(Link());
            var h3 = E("himmel_glocken", "Windglocken stimmen", "Stimme die Windglocken des Tempels, bevor der Sturm sie zerreißt.", EventObjective.Use, 20, 230, 32, 210, 4, 30);
            Sites(h3, 4, 11f, "Glocke stimmen", 2.5f); h3.Trash = A("Himmel_Sturmgreif"); h3.Triggers.Add(Prox()); h3.Triggers.Add(Timer(300));
            // Weltboss-Champion: Himmelswyrm (20-min-Zyklus, Versatz 6 min)
            var h4 = E("himmel_wyrm", "Der Himmelswyrm", "Ein Wyrm aus Sturm und Wolke greift den Tempel an – brich seine Trotzleiste.", EventObjective.WorldBoss, 40, 280, 45, 480, 1, 30, "wyrm");
            h4.Boss = A("Champion_Himmelswyrm"); h4.Trash = A("Himmel_Sturmgreif"); h4.Veteran = A("Himmel_Wolkenschlange"); h4.PhaseThresholds = new[] { 0.66f, 0.33f };
            h4.Triggers.Add(Cycle(1200f, 360f)); h4.MasteryPoints = 1; h4.Cooldown = 60; h4.Experience *= 3; h4.Karma *= 3; h4.Coins *= 4;
            _z.TimerLabel = "Himmelswyrm"; _z.TimerPeriod = 1200f; _z.TimerOffset = 360f; _z.TimerPosition = h4.Center; _z.TimerEventId = "wyrm";
        }

        static void Feste(List<Vector3> crates)
        {
            // Meta „Belagerung der Feste“: drei Tore → Hof → Thronsaal (25-min-Zyklus, Versatz 3 min); Phasen gemäß SiegeModel
            var g1 = E("belagerung_tor1", "Belagerung: Das Eisentor", "Brich mit dem Bündnis durch das Eisentor – besiege die Herolds-Garde.", EventObjective.Kill, -60, -290, 36, 240, 10, 30, "belagerung");
            g1.Trash = A("Feste_Garde"); g1.Triggers.Add(Cycle(1500f, 180f)); g1.SuccessNext = "belagerung_tor2"; g1.MetaId = "belagerung"; g1.MetaPhase = 1; g1.Cooldown = 60;
            var g2 = E("belagerung_tor2", "Belagerung: Das Steintor", "Setze die Rammböcke am Steintor ein.", EventObjective.Use, 0, -300, 36, 240, 4, 30, "belagerung");
            Sites(g2, 4, 12f, "Rammbock ansetzen", 3f); g2.Trash = A("Feste_Siegelbrecher"); g2.Triggers.Add(Link()); g2.SuccessNext = "belagerung_tor3"; g2.MetaId = "belagerung"; g2.MetaPhase = 2; g2.Cooldown = 60;
            var g3 = E("belagerung_tor3", "Belagerung: Das Siegeltor", "Der Verderbte Ritter hält das Siegeltor – besiege ihn.", EventObjective.Boss, 60, -290, 36, 300, 1, 30, "belagerung");
            g3.Boss = A("Feste_Ritter"); g3.Trash = A("Feste_Garde"); g3.Triggers.Add(Link()); g3.SuccessNext = "belagerung_hof"; g3.MetaId = "belagerung"; g3.MetaPhase = 3; g3.Cooldown = 60;
            var g4 = E("belagerung_hof", "Belagerung: Der Hof", "Halte die Banner der Bündnisse im Hof gegen den Gegenangriff.", EventObjective.Defend, 0, 0, 38, 180, 0, 30, "belagerung");
            g4.Trash = A("Feste_Garde"); g4.Veteran = A("Feste_Ritter"); g4.WaveSize = 3; g4.WaveInterval = 18f; g4.Triggers.Add(Link()); g4.SuccessNext = "belagerung_thron"; g4.MetaId = "belagerung"; g4.MetaPhase = 4; g4.Cooldown = 60;
            for (int i = 0; i < 3; i++) crates.Add(V(Mathf.Cos(i * 2.1f) * 6f, Mathf.Sin(i * 2.1f) * 6f));
            var g5 = E("belagerung_thron", "Belagerung: Der Herold im Thronsaal", "Das Finale: Der Herold stellt sich vor dem leeren Thron.", EventObjective.WorldBoss, 0, 250, 45, 420, 1, 30, "belagerung");
            g5.Boss = A("Meta_Herold"); g5.Trash = A("Feste_Garde"); g5.Veteran = A("Feste_Ritter"); g5.PhaseThresholds = new[] { 0.5f };
            g5.Triggers.Add(Link()); g5.MetaId = "belagerung"; g5.MetaPhase = 5; g5.MasteryPoints = 1; g5.Cooldown = 60; g5.Experience *= 3; g5.Karma *= 3; g5.Coins *= 4;
            _z.TimerLabel = "Belagerung der Feste"; _z.TimerPeriod = 1500f; _z.TimerOffset = 180f; _z.TimerPosition = g5.Center; _z.TimerEventId = "belagerung";
            var f6 = E("feste_lager", "Das Lager versorgen", "Bring Vorräte aus den Kammern ins Belagerungslager.", EventObjective.Collect, -40, -360, 34, 240, 6, 30);
            f6.DeliverPoint = V(-30, -350); f6.Trash = A("Feste_Garde"); f6.Triggers.Add(Prox()); f6.Triggers.Add(Timer(300));
        }
    }
}
