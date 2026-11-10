using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Balance;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 11 / M59: Gefahrenstufe 6, Weltboss-Fahrplan mit 12 Bossen.</summary>
    public class Lauf11FoundationTests
    {
        [Test] public void Danger6_DragonlandValues()
        {
            Assert.AreEqual(6, DangerTable.MaxLevel); Assert.AreEqual("Drachenland", DangerTable.Names[6]);
            var s = DangerTable.Default(6);
            Assert.AreEqual(0f, s.NeutralShare); Assert.AreEqual(1100f, s.AggroRange); Assert.AreEqual(1.2f, s.PursuitSpeed, 1e-4f);
            Assert.AreEqual(0.9f, s.ControlChance, 1e-4f); Assert.AreEqual(10, s.MaxGroupSize); Assert.AreEqual(25f, s.Respawn);
            Assert.IsTrue(DangerTable.Allows(s, ControlKinds.Dismount)); Assert.IsTrue(DangerTable.Allows(s, ControlKinds.AirPull));
            Assert.IsTrue(s.Callers); Assert.AreEqual(PackMode.Caller, s.Pack);
        }

        [Test] public void Danger6_HarderThan5_NoAirPullBelow6()
        {
            var five = DangerTable.Default(5); var six = DangerTable.Default(6);
            Assert.Greater(six.AggroRange, five.AggroRange); Assert.Greater(six.PursuitSpeed, five.PursuitSpeed); Assert.Less(six.Respawn, five.Respawn);
            Assert.Less(six.TtdThreeTrash, five.TtdThreeTrash);
            for (int d = 0; d <= 5; d++) Assert.IsFalse(DangerTable.Allows(DangerTable.Default(d), ControlKinds.AirPull), "Stufe " + d);
            Assert.AreEqual(6, DangerTable.Default(9).Level, "geklemmt");
        }

        [Test] public void Schedule_12BossesNoOverlapNoGap()
        {
            Assert.AreEqual(12, WorldBossSchedule.Bosses.Length);
            var b = new List<WorldBossSchedule.Entry>(WorldBossSchedule.Bosses); b.Sort((x, y) => x.Offset.CompareTo(y.Offset));
            float t = 0f;
            foreach (var e in b) { Assert.AreEqual(t, e.Offset, 0.01f, e.Id + " schließt lückenlos an"); Assert.AreEqual(600f, e.Duration); t += e.Duration; }
            Assert.AreEqual(WorldBossSchedule.Cycle, t, 0.01f);
        }

        [Test] public void Schedule_48HoursEveryMinuteHasABoss()
        {
            var seen = new HashSet<string>();
            for (double s = 0; s < 48 * 3600; s += 60) { var e = WorldBossSchedule.ActiveBoss(s); Assert.IsNotNull(e.Id); seen.Add(e.Id); }
            Assert.AreEqual(12, seen.Count);
        }

        [Test] public void Schedule_NewBossesAndMetas()
        {
            foreach (var id in new[] { "gletscherwaechter", "salzkoloss", "nebelschlange", "tempelwaechter", "fall_von_zarathis", "erwachen_des_meeresdrachen" })
                Assert.IsTrue(WorldBossSchedule.TryGet(id, out _), id);
            foreach (var m in WorldBossSchedule.Metas) { Assert.IsTrue(m.Meta); Assert.AreEqual(1200f, m.Duration); }
            Assert.AreNotEqual(WorldBossSchedule.Metas[0].Offset, WorldBossSchedule.Metas[1].Offset);
        }

        [Test] public void Schedule_LevelsAsSpecified()
        {
            var want = new Dictionary<string, int> { { "moorkoloss", 15 }, { "frostschlund", 30 }, { "glutkern", 45 }, { "alter_hain", 55 }, { "grossmaul", 65 }, { "kristallkoenigin", 70 }, { "himmelswyrm", 80 }, { "leviathan", 50 }, { "gletscherwaechter", 75 } };
            foreach (var kv in want) { Assert.IsTrue(WorldBossSchedule.TryGet(kv.Key, out var e)); Assert.AreEqual(kv.Value, e.Level, kv.Key); }
        }

        [Test] public void Schedule_SecondsUntilWraps()
        {
            Assert.AreEqual(600f, WorldBossSchedule.SecondsUntil("frostschlund", 0), 0.01f);
            Assert.AreEqual(7200f - 60f, WorldBossSchedule.SecondsUntil("frostschlund", 660), 0.01f);
            Assert.AreEqual(-1f, WorldBossSchedule.SecondsUntil("gibtsnicht", 0));
        }
    }
}
