using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 11: Ereignisse der 20 neuen Zonen aus <see cref="RegionZones"/> – je Zone sechs Ereignisse mit Kette (Angriff → Anführer),
    /// Verteidigung, Sammeln, Signalfeuer und Geleit; Weltbosse (Gletscherwächter, Tempelwächter, Salzkoloss, Nebelschlange) mit Fahrplan-Fenster,
    /// Metas mit Phasen (Vulkanausbruch, Tempel der Schädelküste, Belagerung des Flammentempels, Fall von Zarathis, Erwachen des Meeresdrachen).
    /// </summary>
    public static partial class ZoneEventBuilder
    {
        static bool Lauf11Events(string zone, List<Vector3> crates)
        {
            var s = RegionZones.Get(zone); if (s == null) return false;
            var l = RegionZones.Layout(zone);
            int lv = s.LevelMax;
            string a0 = s.Archetypes[0], a1 = s.Archetypes[1 % s.Archetypes.Length], a2 = s.Archetypes[2 % s.Archetypes.Length];
            string champ = "Champion_" + zone;
            Vector2 H(int i) => l.Hearts[i % l.Hearts.Length].Center;
            Vector2 W(int i) => l.Waypoints[i % l.Waypoints.Length].Pos;
            string chain = zone + "_angriff";

            var e1 = E(zone + "_angriff", s.Enemy + " zurückschlagen", "Die " + s.Enemy + " rücken auf " + s.Places[3] + " vor – schlage sie zurück.", EventObjective.Kill, H(1).x + 30f, H(1).y + 20f, 36, 240, 8, lv, chain);
            e1.Trash = A(a0); e1.Veteran = A(a2); e1.Triggers.Add(Prox()); e1.Triggers.Add(Timer(s.Region == WorldRegion.Kernland ? 480 : 420)); e1.SuccessNext = zone + "_anfuehrer";
            var e2 = E(zone + "_anfuehrer", "Anführer der " + s.Enemy, "Ihr Anführer zeigt sich – brich seine Trotzleiste.", EventObjective.Boss, H(1).x + 50f, H(1).y + 40f, 36, 300, 1, lv, chain);
            e2.Boss = A(champ); e2.Trash = A(a0); e2.Triggers.Add(Link()); e2.Cooldown = 300;
            var e3 = E(zone + "_verteidigung", s.Places[0] + " verteidigen", "Halte " + s.Places[0] + " gegen die " + s.Enemy + ".", EventObjective.Defend, W(1).x + 20f, W(1).y + 20f, 32, 180, 0, lv);
            e3.Trash = A(a1); e3.Veteran = A(a2); e3.WaveSize = 3; e3.WaveInterval = 20f; e3.Triggers.Add(Prox()); e3.Triggers.Add(Timer(480));
            for (int i = 0; i < 3; i++) crates.Add(V(e3.Center.x + Mathf.Cos(i * 2.1f) * 5f, e3.Center.z + Mathf.Sin(i * 2.1f) * 5f));
            var e4 = E(zone + "_vorraete", "Vorräte für " + s.Ally, "Sammle Vorräte und bring sie zu " + s.Ally + ".", EventObjective.Collect, H(2).x - 30f, H(2).y + 10f, 34, 240, 6, lv);
            e4.DeliverPoint = V(H(2).x - 20f, H(2).y + 18f); e4.Trash = A(a1); e4.Triggers.Add(Prox()); e4.Triggers.Add(Timer(420));
            var e5 = E(zone + "_signalfeuer", s.Places[4] + ": Signalfeuer", "Entzünde die Signalfeuer von " + s.Places[4] + ", bevor die Nacht hereinbricht.", EventObjective.Use, W(3).x - 25f, W(3).y + 15f, 30, 210, 4, lv);
            Sites(e5, 4, 11f, "Feuer entzünden", 2.5f); e5.Trash = A(a1); e5.Triggers.Add(Prox()); e5.Triggers.Add(Timer(480));
            var e6 = E(zone + "_geleit", "Geleit nach " + s.Places[5], "Begleite die Träger von " + s.Ally + " sicher nach " + s.Places[5] + ".", EventObjective.Escort, W(0).x + 10f, W(0).y + 30f, 30, 360, 1, lv);
            e6.Path = new[] { V(W(0).x + 10f, W(0).y + 30f), V(W(1).x, W(1).y - 20f), V(W(2).x, W(2).y - 20f) };
            e6.Trash = A(a0); e6.WaveSize = 2; e6.WaveInterval = 25f; e6.Triggers.Add(Prox()); e6.Triggers.Add(Timer(540));

            switch (zone)
            {
                case "weisse_stille": BossFromSchedule("gletscherwaechter", "Champion_Gletscherwaechter", a1, H(3) + new Vector2(60f, 60f), lv); break;
                case "schaedelkueste": BossFromSchedule("tempelwaechter", "Champion_Tempelwaechter", a0, H(3) + new Vector2(50f, 70f), lv); Meta(zone, "tempel", "Tempel der Schädelküste", new[] { "Die Tore des versunkenen Tempels", "Der verderbte Brunnen", "Die Tempelkammer" }, a0, a2, "Champion_Tempelwaechter", H(0), lv); break;
                case "spiegel_der_sonne": BossFromSchedule("salzkoloss", "Champion_Salzkoloss", a1, H(3) + new Vector2(60f, 60f), lv); break;
                case "nebelkueste": BossFromSchedule("nebelschlange", "Champion_Nebelschlange", a2, H(3) + new Vector2(60f, 60f), lv); break;
                case "aschenkessel": Meta(zone, "ausbruch", "Vulkanausbruch", new[] { "Lavaströme umleiten", "Die Magmabrut zurückdrängen", "Das Herz des Vulkans" }, a0, a2, "Champion_Vulkanherz", H(0), lv); break;
                case "glutkessel": Meta(zone, "flammentempel", "Belagerung des Flammentempels", new[] { "Der Kult am Tempeltor", "Die Opferplätze", "Der Hohepriester der Flamme" }, a0, a1, "Champion_Hohepriester", H(0), lv); break;
                case "zarathis": Meta(zone, "zarathis", "Fall von Zarathis", new[] { "Die Knochenmauer durchbrechen", "Thron- und Händlerviertel", "Tempel- und Hafenviertel", "Der Flammenkönig" }, a1, a0, "Champion_Flammenkoenig", H(0), lv, "fall_von_zarathis"); break;
                case "herz_der_flut": Meta(zone, "drachenende", "Erwachen des Meeresdrachen", new[] { "Die vier Plattformen", "Boot-Anfahrt durch den Sturm", "Der Meeresdrache – Flutwellen", "Der Meeresdrache – Jade-Kanonen" }, a0, a1, "Champion_Meeresdrache", H(0), lv, "erwachen_des_meeresdrachen"); break;
            }
            return true;
        }

        static void BossFromSchedule(string scheduleId, string champId, string trash, Vector2 at, int lv)
        {
            WorldBossSchedule.TryGet(scheduleId, out var e);
            var w = E(_z.Id + "_" + scheduleId, e.Name, e.Name + " erscheint – brich die Trotzleiste und weiche den Mustern aus.", EventObjective.WorldBoss, at.x, at.y, 45, 480, 1, lv, scheduleId);
            w.Boss = A(champId); w.Trash = A(trash); w.PhaseThresholds = new[] { 0.5f };
            w.Triggers.Add(Cycle(WorldBossSchedule.Cycle, e.Offset)); w.MasteryPoints = 1; w.Cooldown = 60; w.Experience *= 3; w.Karma *= 3; w.Coins *= 4;
            _z.TimerLabel = e.Name; _z.TimerPeriod = WorldBossSchedule.Cycle; _z.TimerOffset = e.Offset; _z.TimerPosition = w.Center; _z.TimerEventId = scheduleId;
        }

        /// <summary>Meta-Ereignis: Phasen als Kette, letzte Phase mit Champion; optional im Fahrplan (Meta-Spur).</summary>
        static void Meta(string zone, string id, string title, string[] phases, string trash, string vet, string champ, Vector2 at, int lv, string scheduleId = null)
        {
            string chain = zone + "_" + id;
            float period = 1800f, offset = 300f;
            if (scheduleId != null && WorldBossSchedule.TryGet(scheduleId, out var se)) { period = WorldBossSchedule.Cycle; offset = se.Offset; }
            EventDefinition prev = null;
            for (int i = 0; i < phases.Length; i++)
            {
                bool last = i == phases.Length - 1;
                var p = at + new Vector2(Mathf.Cos(i * 1.7f) * 60f, Mathf.Sin(i * 1.7f) * 60f + i * 30f);
                var e = E(chain + "_p" + (i + 1), title + ": " + phases[i], phases[i] + " – Phase " + (i + 1) + " von " + phases.Length + ".", last ? EventObjective.WorldBoss : i % 2 == 0 ? EventObjective.Kill : EventObjective.Defend, p.x, p.y, last ? 48 : 40, last ? 600 : 300, last ? 1 : 10, lv, chain);
                e.Trash = A(trash); e.Veteran = A(vet); e.MetaId = chain; e.MetaPhase = i + 1;
                if (last) { e.Boss = A(champ); e.PhaseThresholds = new[] { 0.66f, 0.33f }; e.MasteryPoints = 2; e.Experience *= 4; e.Karma *= 4; e.Coins *= 5; }
                if (i == 0) e.Triggers.Add(Cycle(period, offset)); else e.Triggers.Add(Link());
                if (prev != null) prev.SuccessNext = e.Id;
                e.Cooldown = 60; prev = e;
            }
            _z.TimerLabel = title; _z.TimerPeriod = period; _z.TimerOffset = offset; _z.TimerPosition = new Vector3(at.x, 0f, at.y); _z.TimerEventId = chain;
        }
    }
}
