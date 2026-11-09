using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 4 / M19: neue Zieltypen, Trotzleiste, Weltuhr-Zyklen, Bossphasen, Ereignis-Bestand je Zone.</summary>
    public class WorldEventTests
    {
        static EventDefinition Def(EventObjective o, int target = 4, float duration = 100f, float[] phases = null)
        {
            var d = ScriptableObject.CreateInstance<EventDefinition>();
            d.Id = "t"; d.Objective = o; d.TargetCount = target; d.Duration = duration; d.PhaseThresholds = phases ?? new float[0];
            return d;
        }

        [Test]
        public void BuildAndUse_SucceedAtTargetCount_FailOnTimeout()
        {
            foreach (var o in new[] { EventObjective.Build, EventObjective.Use })
            {
                var r = new EventRunner(Def(o));
                r.Built = 2; r.Tick(1f, default(EventInputs));
                Assert.AreEqual(0.5f, r.Progress, 1e-4f);
                r.Built = 4;
                Assert.IsTrue(r.Tick(1f, default(EventInputs)));
                Assert.AreEqual(RunnerState.Succeeded, r.State);
                var f = new EventRunner(Def(o));
                Assert.IsTrue(f.Tick(101f, default(EventInputs)));
                Assert.AreEqual(RunnerState.Failed, f.State);
            }
        }

        [Test]
        public void Hunt_TwoPhases_LeaderAfterKills()
        {
            var r = new EventRunner(Def(EventObjective.Hunt, 8));
            r.Kills = 4; r.Tick(1f, default(EventInputs));
            Assert.AreEqual(0, r.Phase); Assert.AreEqual(0.25f, r.Progress, 1e-4f);
            r.Kills = 8; r.Tick(1f, default(EventInputs));
            Assert.AreEqual(1, r.Phase); Assert.IsTrue(r.PhaseChanged);
            r.BossHealth = 0.5f;
            Assert.AreEqual(0.75f, r.Progress, 1e-4f);
            r.BossDefeated = true;
            Assert.IsTrue(r.Tick(1f, default(EventInputs)));
            Assert.AreEqual(RunnerState.Succeeded, r.State);
        }

        [Test]
        public void WorldBoss_PhasesByHealth_FailsAtTimeLimit()
        {
            var r = new EventRunner(Def(EventObjective.WorldBoss, 1, 480f, new[] { 0.66f, 0.33f }));
            r.BossHealth = 0.8f; r.Tick(1f, default(EventInputs)); Assert.AreEqual(0, r.Phase);
            r.BossHealth = 0.5f; r.Tick(1f, default(EventInputs)); Assert.AreEqual(1, r.Phase);
            r.BossHealth = 0.2f; r.Tick(1f, default(EventInputs)); Assert.AreEqual(2, r.Phase);
            Assert.IsTrue(r.Tick(480f, default(EventInputs)));
            Assert.AreEqual(RunnerState.Failed, r.State, "8-min-Limit");
        }

        [Test]
        public void Defiance_BlocksControl_BreaksAndRefills()
        {
            var m = new DefianceModel(300f);
            Assert.IsTrue(m.Hit(EffectType.Stun, 1f, out bool b1)); Assert.IsFalse(b1);
            Assert.AreEqual(100f, m.Value, 1e-3f, "Betäubung 1 s = 200");
            Assert.IsFalse(m.Hit(EffectType.Might, 5f, out _), "Segen beeinflussen die Leiste nicht");
            Assert.IsTrue(m.Hit(EffectType.Immobilize, 2f, out bool b2));
            Assert.IsTrue(b2); Assert.IsTrue(m.Broken);
            Assert.AreEqual(1.5f, m.DamageTakenMultiplier);
            m.Tick(m.BreakDuration + 0.01f);
            Assert.IsFalse(m.Broken); Assert.IsTrue(m.Refilling);
            Assert.IsTrue(m.Hit(EffectType.Stun, 1f, out _), "während des Auffüllens ist Kontrolle blockiert");
            m.Tick(m.RefillTime + 0.01f);
            Assert.AreEqual(300f, m.Value, 1e-3f);
            Assert.AreEqual(1, m.Breaks);
        }

        [Test]
        public void WorldClock_CyclesStartOnce_NextStartFormats()
        {
            Assert.AreEqual(120f, WorldClock.NextStart(900f, 120f, 0f));
            Assert.AreEqual(1020f, WorldClock.NextStart(900f, 120f, 500f));
            Assert.IsTrue(WorldClock.CycleStartedSince(900f, 120f, 119f, 121f));
            Assert.IsFalse(WorldClock.CycleStartedSince(900f, 120f, 121f, 500f));
            Assert.IsTrue(WorldClock.CycleStartedSince(900f, 120f, 1000f, 1020.5f));
            Assert.AreEqual("12:30", WorldClock.Format(750f));
        }

        [Test]
        public void Scheduler_CycleTriggerStartsChain()
        {
            WorldClock.Seconds = 0f;
            var a = Def(EventObjective.Kill, 1, 100f); a.Id = "a"; a.ChainId = "c"; a.Triggers.Add(new EventTrigger { Type = EventTriggerType.Cycle, Period = 900f, Delay = 10f }); a.SuccessNext = "b";
            var b = Def(EventObjective.Kill, 1, 100f); b.Id = "b"; b.ChainId = "c"; b.Triggers.Add(new EventTrigger { Type = EventTriggerType.ChainLink });
            var s = new EventScheduler(new[] { a, b });
            WorldClock.Seconds = 5f; s.Tick(5f, Vector3.zero);
            Assert.AreEqual(EventScheduler.DefState.Idle, s.StateOf("a"));
            WorldClock.Seconds = 11f; s.Tick(6f, Vector3.zero);
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("a"));
            s.RunnerOf("a").Kills = 1;
            s.TickRunners(1f, null);
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("b"), "Kette läuft weiter");
            WorldClock.Seconds = 0f;
        }

        [Test]
        public void EachBiomeZone_HasAtLeastFiveEvents_WithNewObjectives()
        {
            foreach (var zone in new[] { "nebelmoor", "frostkamm", "gluetoede" })
            {
                var guids = UnityEditor.AssetDatabase.FindAssets("t:EventDefinition", new[] { "Assets/ProjectTyrant/Data/Zones/Events/" + zone });
                Assert.GreaterOrEqual(guids.Length, 5, zone);
                bool build = false, use = false, hunt = false;
                foreach (var g in guids)
                {
                    var d = UnityEditor.AssetDatabase.LoadAssetAtPath<EventDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
                    build |= d.Objective == EventObjective.Build; use |= d.Objective == EventObjective.Use; hunt |= d.Objective == EventObjective.Hunt;
                }
                Assert.IsTrue(build && use && hunt, zone + ": Bauen, Benutzen, Jagd");
            }
            var wb = UnityEditor.AssetDatabase.LoadAssetAtPath<EventDefinition>("Assets/ProjectTyrant/Data/Zones/Events/frostkamm/Event_wb_frostschlund.asset");
            Assert.AreEqual(480f, wb.Duration); Assert.AreEqual(EventObjective.WorldBoss, wb.Objective);
            int metaPhases = 0;
            foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:EventDefinition", new[] { "Assets/ProjectTyrant/Data/Zones/Events/gluetoede" }))
            { var d = UnityEditor.AssetDatabase.LoadAssetAtPath<EventDefinition>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)); if (d.MetaId == "sturm") metaPhases++; }
            Assert.AreEqual(4, metaPhases, "Meta in 4 Phasen");
        }
    }
}
