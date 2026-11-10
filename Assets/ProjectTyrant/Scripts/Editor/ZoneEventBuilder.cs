using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 4 / M19: Ereignisse der Biom-Zonen (je ≥ 5) mit neuen Zieltypen Bauen, Benutzen, Jagd in Phasen, Weltboss;
    /// Weltboss-Kette „Frostschlund“ (15-min-Zyklus, 2 Vor-Ereignisse, 8 min Limit) und Meta „Sturm über der Glutöde“ (4 Phasen, 20-min-Zyklus).
    /// </summary>
    public static partial class ZoneEventBuilder
    {
        static System.Func<float, float, float> _h;
        static ZoneDefinition _z;
        static List<EventDefinition> _list;
        static float Scale(int lvl) => (400f + 120f * (lvl - 1)) / (400f + 120f * 7f);
        static EnemyArchetype A(string id) => DataBuilder.Archetype(id);
        static Vector3 V(float x, float z) => new Vector3(x, _h(x, z), z);

        static EventDefinition E(string id, string title, string desc, EventObjective obj, float x, float z, float radius, float duration, int target, int level, string chain = null)
        {
            string dir = DataDir + "/Zones/Events/" + _z.Id; EnsureFolder(DataDir + "/Zones/Events"); EnsureFolder(dir);   // getrennt von Data/Events (Lauf-3-Startzone)
            string path = dir + "/Event_" + id + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<EventDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<EventDefinition>(); AssetDatabase.CreateAsset(d, path); }
            d.Id = id; d.Title = title; d.Description = desc; d.Objective = obj; d.Center = V(x, z); d.Radius = radius; d.Duration = duration; d.TargetCount = target;
            d.ChainId = chain; d.Triggers = new List<EventTrigger>(); d.SuccessNext = d.FailNext = null; d.Cooldown = 240f;
            float s = Scale(level);
            d.Experience = Mathf.RoundToInt(500 * s); d.Karma = Mathf.RoundToInt(150 * s); d.Coins = Mathf.RoundToInt(80 * s); d.GoldContribution = 1500f * s; d.GoldItemRoll = true;
            d.SetFlagOnSuccess = d.SetFlagOnFail = d.ClearFlagOnSuccess = d.ClearFlagOnFail = null; d.FlagDuration = 0f;
            d.WorldHook = _z.Id + "_" + id; d.Path = new Vector3[0]; d.DeliverPoint = d.Center; d.CaptureRadius = 8f; d.CaptureTime = 8f;
            d.WaveSize = 2; d.WaveInterval = 18f; d.Trash = d.Veteran = d.Boss = null;
            d.Sites = new Vector3[0]; d.SiteChannel = 3f; d.SiteLabel = "Reparieren"; d.PhaseThresholds = new float[0]; d.MasteryPoints = 0; d.MetaId = null; d.MetaPhase = 0;
            EditorUtility.SetDirty(d);
            _list.Add(d);
            return d;
        }

        static void Sites(EventDefinition d, int n, float r, string label, float channel = 3f)
        {
            var a = new Vector3[n];
            for (int i = 0; i < n; i++) { float ang = i * Mathf.PI * 2f / n + 0.4f; a[i] = V(d.Center.x + Mathf.Cos(ang) * r, d.Center.z + Mathf.Sin(ang) * r); }
            d.Sites = a; d.SiteLabel = label; d.SiteChannel = channel; d.TargetCount = n;
        }

        static EventTrigger Prox(float delay = 20f) => new EventTrigger { Type = EventTriggerType.Proximity, Delay = delay, ProximityFactor = 1.4f };
        static EventTrigger Timer(float t) => new EventTrigger { Type = EventTriggerType.Timer, Delay = t };
        static EventTrigger Link() => new EventTrigger { Type = EventTriggerType.ChainLink };
        static EventTrigger Cycle(float period, float offset) => new EventTrigger { Type = EventTriggerType.Cycle, Period = period, Delay = offset };

        /// <summary>Baut die Ereignisse einer Zone; liefert Definitionen und Verteidigungs-Positionen (Vorratskisten).</summary>
        public static EventDefinition[] Build(ZoneDefinition z, System.Func<float, float, float> height, out List<Vector3> defendCrates)
        {
            _z = z; _h = height; _list = new List<EventDefinition>();
            defendCrates = new List<Vector3>();
            z.TimerLabel = null; z.TimerPeriod = 0f; z.TimerOffset = 0f; z.TimerEventId = null;
            switch (z.Id)
            {
                case "nebelmoor": Moor(defendCrates); break;
                case "frostkamm": Frost(); break;
                case "gluetoede": Glut(defendCrates); break;
                default: Lauf6Events(z.Id, defendCrates); break;   // Lauf 6: neue Zonen
            }
            EditorUtility.SetDirty(z);
            return _list.ToArray();
        }

        static void Moor(List<Vector3> crates)
        {
            var e1 = E("moor_stege", "Stege vor dem Versinken retten", "Die Bohlenstege der Torfstecher brechen ein – repariere sie, bevor die Moorschleicher kommen.", EventObjective.Build, -60, -270, 34, 240, 5, 16);
            Sites(e1, 5, 11f, "Steg reparieren"); e1.Trash = A("Moor_Schleicher"); e1.Triggers.Add(Prox()); e1.Triggers.Add(Timer(240));
            var e2 = E("moor_kolossjagd", "Jagd auf den Uralten Moorkoloss", "Treibe die Sumpfspucker aus dem Weidengrund, bis ihr Anführer sich zeigt.", EventObjective.Hunt, -60, 230, 40, 360, 8, 23);
            e2.Trash = A("Moor_Spucker"); e2.Boss = A("Champion_Moorkoloss"); e2.Triggers.Add(Prox(30)); e2.Triggers.Add(Timer(420)); e2.Cooldown = 360;
            var e3 = E("moor_irrlichter", "Irrlichter entzünden", "Entzünde die Laternen am Nebelkreuz, damit die Fährleute den Weg finden.", EventObjective.Use, 60, 20, 30, 210, 4, 20);
            Sites(e3, 4, 12f, "Laterne entzünden", 2f); e3.Trash = A("Moor_Spucker"); e3.Triggers.Add(Prox()); e3.Triggers.Add(Timer(300));
            var e4 = E("moor_furt", "Krötenfurt verteidigen", "Halte die Fährkisten der Krötenfurt gegen anrückende Moorschleicher.", EventObjective.Defend, 340, -60, 32, 180, 0, 19);
            e4.Trash = A("Moor_Schleicher"); e4.Veteran = A("Moor_Koloss"); e4.WaveInterval = 22f; e4.WaveSize = 3; e4.Triggers.Add(Prox()); e4.Triggers.Add(Timer(360));
            for (int i = 0; i < 3; i++) crates.Add(V(340 + Mathf.Cos(i * 2.1f) * 5f, -60 + Mathf.Sin(i * 2.1f) * 5f));
            var e5 = E("moor_kraeuter", "Kräuter für Albin", "Sammle Moorkräuter und bring sie zu Albins Kessel.", EventObjective.Collect, -150, 255, 36, 240, 6, 23);
            e5.DeliverPoint = V(-142, 246); e5.Trash = A("Moor_Schleicher"); e5.WaveSize = 2; e5.WaveInterval = 25f; e5.Triggers.Add(Prox()); e5.Triggers.Add(Timer(300));
        }

        static void Frost()
        {
            var f1 = E("frost_lager", "Lager vor dem Sturm sichern", "Verankere die Zelte des Bergsteigerlagers, bevor der Sturm sie fortreißt.", EventObjective.Build, -240, -262, 32, 240, 5, 25);
            Sites(f1, 5, 10f, "Zelt verankern"); f1.Trash = A("Frost_Eiswolf"); f1.Triggers.Add(Prox()); f1.Triggers.Add(Timer(240));
            var f2 = E("frost_rudeljagd", "Rudeljagd am Kammpass", "Jage die Eiswölfe, bis Rudelführer Weißzahn erscheint.", EventObjective.Hunt, 200, 20, 40, 360, 8, 28);
            f2.Trash = A("Frost_Eiswolf"); f2.Boss = A("Champion_Weisszahn"); f2.Triggers.Add(Prox(30)); f2.Triggers.Add(Timer(420)); f2.Cooldown = 360;
            var f3 = E("frost_signalfeuer", "Signalfeuer am Pass", "Entzünde die Signalfeuer, damit die Bergführer den Pass sichern können.", EventObjective.Use, 120, -60, 30, 210, 4, 27);
            Sites(f3, 4, 12f, "Signalfeuer entzünden", 2.5f); f3.Trash = A("Frost_Werfer"); f3.Triggers.Add(Prox()); f3.Triggers.Add(Timer(300));
            // Weltboss-Kette Frostschlund: 15-min-Zyklus, Versatz 2 min
            var w1 = E("wb_kristalle", "Eiskristalle des Schlunds zerschlagen", "Der Frostschlund regt sich – zerschlage die Eiskristalle und ihre Wächter.", EventObjective.Kill, 40, 300, 40, 180, 10, 30, "frostschlund");
            w1.Trash = A("Frost_Eiswolf"); w1.Triggers.Add(Cycle(900f, 120f)); w1.SuccessNext = "wb_siegel"; w1.Cooldown = 60;
            var w2 = E("wb_siegel", "Siegelsteine entzünden", "Entzünde die vier Siegelsteine, um den Frostschlund an die Oberfläche zu zwingen.", EventObjective.Use, 40, 320, 36, 180, 4, 30, "frostschlund");
            Sites(w2, 4, 14f, "Siegelstein entzünden", 3f); w2.Trash = A("Frost_Werfer"); w2.Triggers.Add(Link()); w2.SuccessNext = "wb_frostschlund"; w2.Cooldown = 60;
            var w3 = E("wb_frostschlund", "Weltboss: Frostschlund", "Der Frostschlund ist erwacht. Brich seine Trotzleiste und besiege ihn in 8 Minuten.", EventObjective.WorldBoss, 40, 330, 45, 480, 1, 30, "frostschlund");
            w3.Boss = A("WorldBoss_Frostschlund"); w3.Trash = A("Frost_Eiswolf"); w3.Veteran = A("Frost_Huter"); w3.PhaseThresholds = new[] { 0.5f };
            w3.Triggers.Add(Link()); w3.MasteryPoints = 1; w3.Cooldown = 60; w3.Experience *= 3; w3.Karma *= 3; w3.Coins *= 4;
            _z.TimerLabel = "Frostschlund"; _z.TimerPeriod = 900f; _z.TimerOffset = 120f; _z.TimerPosition = w3.Center; _z.TimerEventId = "frostschlund";
        }

        static void Glut(List<Vector3> crates)
        {
            var g1 = E("glut_blitzableiter", "Blitzableiter errichten", "Hilf Oleg, die Blitzableiter vor dem nächsten Sturm aufzurichten.", EventObjective.Build, 180, 215, 32, 240, 4, 30);
            Sites(g1, 4, 10f, "Blitzableiter aufrichten"); g1.Trash = A("Glut_Kriecher"); g1.Triggers.Add(Prox()); g1.Triggers.Add(Timer(240));
            var g2 = E("glut_kriecherplage", "Kriecherplage im Schlackenfeld", "Vernichte die Aschenkriecher, bis die Brutmutter auftaucht.", EventObjective.Hunt, -300, -200, 40, 360, 10, 30);
            g2.Trash = A("Glut_Kriecher"); g2.Boss = A("Champion_Brutmutter"); g2.Triggers.Add(Prox(30)); g2.Triggers.Add(Timer(420)); g2.Cooldown = 360;
            var g3 = E("glut_kuehlrinnen", "Kühlrinnen öffnen", "Öffne die Kühlrinnen der Glutschmiede, bevor die Esse überkocht.", EventObjective.Use, 30, 20, 30, 210, 4, 30);
            Sites(g3, 4, 11f, "Kühlrinne öffnen", 2.5f); g3.Trash = A("Glut_Kriecher"); g3.Triggers.Add(Prox()); g3.Triggers.Add(Timer(300));
            // Meta „Sturm über der Glutöde“: 4 Phasen, 20-min-Zyklus, Versatz 5 min
            var p1 = E("meta_sturmwall", "Sturm über der Glutöde: Sturmwall errichten", "Der Sturm zieht auf – errichte den Sturmwall am Auge des Sturms.", EventObjective.Build, 250, 330, 40, 240, 5, 30, "sturm");
            Sites(p1, 5, 13f, "Sturmwall bauen", 3f); p1.Trash = A("Glut_Kriecher"); p1.Triggers.Add(Cycle(1200f, 300f)); p1.SuccessNext = "meta_siegel"; p1.MetaId = "sturm"; p1.MetaPhase = 1; p1.Cooldown = 60;
            var p2 = E("meta_siegel", "Sturm über der Glutöde: Sturmsiegel brechen", "Brich die vier Sturmsiegel, damit der Tyrann seine Deckung verliert.", EventObjective.Use, 250, 340, 40, 210, 4, 30, "sturm");
            Sites(p2, 4, 15f, "Sturmsiegel brechen", 3f); p2.Trash = A("Glut_Beschwoerer"); p2.WaveSize = 1; p2.Triggers.Add(Link()); p2.SuccessNext = "meta_brut"; p2.MetaId = "sturm"; p2.MetaPhase = 2; p2.Cooldown = 60;
            var p3 = E("meta_brut", "Sturm über der Glutöde: Sturmbrut zurückschlagen", "Halte die Vorräte der Sturmreiter gegen die Sturmbrut.", EventObjective.Defend, 250, 320, 36, 150, 0, 30, "sturm");
            p3.Trash = A("Glut_Kriecher"); p3.Veteran = A("Glut_Golem"); p3.WaveSize = 3; p3.WaveInterval = 18f; p3.Triggers.Add(Link()); p3.SuccessNext = "meta_tyrann"; p3.MetaId = "sturm"; p3.MetaPhase = 3; p3.Cooldown = 60;
            for (int i = 0; i < 3; i++) crates.Add(V(250 + Mathf.Cos(i * 2.1f) * 5f, 320 + Mathf.Sin(i * 2.1f) * 5f));
            var p4 = E("meta_tyrann", "Sturm über der Glutöde: Der Sturmtyrann", "Das Finale: besiege den Sturmtyrannen, bevor der Sturm die Glutöde verschlingt.", EventObjective.WorldBoss, 250, 345, 45, 420, 1, 30, "sturm");
            p4.Boss = A("Meta_Sturmtyrann"); p4.Trash = A("Glut_Kriecher"); p4.Veteran = A("Glut_Golem"); p4.PhaseThresholds = new[] { 0.66f, 0.33f };
            p4.Triggers.Add(Link()); p4.MetaId = "sturm"; p4.MetaPhase = 4; p4.MasteryPoints = 1; p4.Cooldown = 60; p4.Experience *= 3; p4.Karma *= 3; p4.Coins *= 4;
            _z.TimerLabel = "Sturm über der Glutöde"; _z.TimerPeriod = 1200f; _z.TimerOffset = 300f; _z.TimerPosition = p4.Center; _z.TimerEventId = "sturm";
        }
    }
}
