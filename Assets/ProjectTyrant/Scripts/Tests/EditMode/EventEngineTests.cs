using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    public class EventEngineTests
    {
        static EventDefinition D(string id, EventObjective o, params EventTrigger[] triggers)
        {
            var d = ScriptableObject.CreateInstance<EventDefinition>();
            d.Id = id; d.Title = id; d.Objective = o; d.Center = Vector3.zero; d.Radius = 20f; d.Duration = 60f; d.TargetCount = 3; d.Cooldown = 30f; d.GoldContribution = 1000f;
            d.Triggers = new List<EventTrigger>(triggers);
            return d;
        }
        static EventTrigger T(EventTriggerType t, float delay = 0f, string param = null, float min = 0f, float max = 0f) => new EventTrigger { Type = t, Delay = delay, Param = param, Min = min, Max = max };
        static readonly Vector3 Far = new Vector3(500, 0, 500);

        [Test]
        public void Scheduler_RunsAtLeastThreeInParallel_AndRespectsLimit()
        {
            var defs = new[] { D("a", EventObjective.Kill, T(EventTriggerType.Timer, 1f)), D("b", EventObjective.Defend, T(EventTriggerType.Timer, 1f)), D("c", EventObjective.Capture, T(EventTriggerType.Timer, 1f)), D("d", EventObjective.Boss, T(EventTriggerType.Timer, 1f)) };
            var s = new EventScheduler(defs) { MaxParallel = 3 };
            s.Tick(0.5f, Far); Assert.AreEqual(0, s.RunningCount, "Timer noch nicht abgelaufen");
            s.Tick(0.6f, Far);
            Assert.AreEqual(3, s.RunningCount, "drei gleichzeitig");
            Assert.AreEqual(EventScheduler.DefState.Idle, s.StateOf("d"), "viertes wartet");
            s.Complete(s.RunnerOf("a"), true);
            s.Tick(0.1f, Far);
            Assert.AreEqual(3, s.RunningCount, "nachgerückt");
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("d"));
        }

        [Test]
        public void Triggers_Proximity_WorldState_Heart_RandomWindow()
        {
            var prox = D("prox", EventObjective.Kill, T(EventTriggerType.Proximity, 2f)); prox.Center = new Vector3(100, 0, 0);
            var world = D("world", EventObjective.Defend, T(EventTriggerType.WorldState, 0f, "fallen"));
            var heart = D("heart", EventObjective.Capture, T(EventTriggerType.Heart, 0f, "h1"));
            var rnd = D("rnd", EventObjective.Boss, T(EventTriggerType.RandomWindow, min: 10f, max: 20f));
            var s = new EventScheduler(new[] { prox, world, heart, rnd }, 42) { MaxParallel = 10 };
            s.Tick(1f, new Vector3(100, 0, 5));
            Assert.AreEqual(EventScheduler.DefState.Idle, s.StateOf("prox"), "Mindest-Leerlauf 2 s");
            s.Tick(1.5f, Far); Assert.AreEqual(EventScheduler.DefState.Idle, s.StateOf("prox"), "Spieler zu weit weg");
            s.Tick(0.1f, new Vector3(100, 0, 5)); Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("prox"), "Annäherung");
            Assert.AreEqual(EventScheduler.DefState.Idle, s.StateOf("world"));
            s.SetFlag("fallen", 0f); s.Tick(0.1f, Far);
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("world"), "Welt-Zustand");
            s.NotifyHeart("h2"); s.Tick(0.1f, Far); Assert.AreEqual(EventScheduler.DefState.Idle, s.StateOf("heart"));
            s.NotifyHeart("h1"); s.Tick(0.1f, Far); Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("heart"), "Herz-Interaktion");
            float t = 0f; while (s.StateOf("rnd") == EventScheduler.DefState.Idle && t < 30f) { s.Tick(0.25f, Far); t += 0.25f; }
            Assert.That(t, Is.InRange(9.5f, 21f), "Zufallsfenster 10–20 s, war " + t);
        }

        [Test]
        public void Chain_BranchesOnOutcome_AndMembersNeverStartAlone()
        {
            var head = D("head", EventObjective.Defend, T(EventTriggerType.Timer, 0f)); head.ChainId = "c"; head.SuccessNext = "win"; head.FailNext = "lose";
            var win = D("win", EventObjective.Kill, T(EventTriggerType.ChainLink)); win.ChainId = "c";
            var lose = D("lose", EventObjective.Capture, T(EventTriggerType.ChainLink)); lose.ChainId = "c";
            var s = new EventScheduler(new[] { head, win, lose });
            for (int i = 0; i < 5; i++) s.Tick(1f, Far);
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("head"));
            Assert.AreEqual(EventScheduler.DefState.Idle, s.StateOf("win"), "Ketten-Glied startet nicht allein");
            s.Complete(s.RunnerOf("head"), false);
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("lose"), "Fehlschlag → Zweig B");
            Assert.AreEqual(EventScheduler.DefState.Idle, s.StateOf("win"));
            s.Complete(s.RunnerOf("lose"), true);
            for (int i = 0; i < 40; i++) s.Tick(1f, Far);
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("head"), "nach Cooldown neu");
            s.Complete(s.RunnerOf("head"), true);
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("win"), "Erfolg → Zweig A");
        }

        [Test]
        public void Cooldown_And_WorldFlags_WithRollback()
        {
            var e = D("e", EventObjective.Defend, T(EventTriggerType.Timer, 0f)); e.Cooldown = 10f; e.SetFlagOnFail = "fallen"; e.FlagDuration = 5f;
            var s = new EventScheduler(new[] { e });
            var changes = new List<string>(); s.FlagChanged += (f, on) => changes.Add(f + (on ? "+" : "-"));
            s.Tick(0.1f, Far);
            s.Complete(s.RunnerOf("e"), false);
            Assert.IsTrue(s.HasFlag("fallen"));
            Assert.AreEqual(EventScheduler.DefState.Cooldown, s.StateOf("e"));
            for (int i = 0; i < 9; i++) s.Tick(1f, Far);
            Assert.AreNotEqual(EventScheduler.DefState.Running, s.StateOf("e"), "Cooldown 10 s");
            Assert.IsFalse(s.HasFlag("fallen"), "Rückbau nach 5 s");
            CollectionAssert.AreEqual(new[] { "fallen+", "fallen-" }, changes);
            for (int i = 0; i < 3; i++) s.Tick(1f, Far);
            Assert.AreEqual(EventScheduler.DefState.Running, s.StateOf("e"));
        }

        [Test]
        public void Contribution_MedalsAndFailureQuarter()
        {
            var r = new EventRunner(D("k", EventObjective.Kill));
            r.Contribution = 100f; Assert.AreEqual(RewardTier.None, r.Medal);
            r.Contribution = 150f; Assert.AreEqual(RewardTier.Bronze, r.Medal);
            r.Contribution = 500f; Assert.AreEqual(RewardTier.Silver, r.Medal);
            r.Contribution = 1000f; Assert.AreEqual(RewardTier.Gold, r.Medal);
            r.Kills = 3; r.Tick(0.1f, default(EventInputs));
            Assert.AreEqual(RunnerState.Succeeded, r.State);
            Assert.AreEqual(1f, r.RewardMultiplier, 1e-4f);
            var f = new EventRunner(D("f", EventObjective.Kill)); f.Contribution = 1000f;
            f.Tick(61f, default(EventInputs));
            Assert.AreEqual(RunnerState.Failed, f.State, "Zeit abgelaufen");
            Assert.AreEqual(0.25f, f.RewardMultiplier, 1e-4f, "Fehlschlag 25 %");
        }

        [Test]
        public void Escort_FailsWhenEscortDies_SucceedsAtEnd()
        {
            var r = new EventRunner(D("esc", EventObjective.Escort));
            r.EscortProgress = 0.6f; r.Tick(1f, default(EventInputs));
            Assert.IsTrue(r.Active);
            r.EscortAlive = false; r.Tick(0.1f, default(EventInputs));
            Assert.AreEqual(RunnerState.Failed, r.State, "Tod der Eskorte = Fehlschlag");
            var ok = new EventRunner(D("esc2", EventObjective.Escort));
            ok.EscortProgress = 1f; ok.Tick(0.1f, default(EventInputs));
            Assert.AreEqual(RunnerState.Succeeded, ok.State);
        }

        [Test]
        public void Capture_OnlyWithoutEnemies_Defend_FailsAtZero()
        {
            var d = D("cap", EventObjective.Capture); d.CaptureTime = 4f;
            var r = new EventRunner(d);
            r.Tick(2f, new EventInputs { PlayerInCapture = true, EnemiesInCapture = 2 });
            Assert.AreEqual(0f, r.Capture, 1e-4f, "Gegner im Kreis blockieren");
            r.Tick(2f, new EventInputs { PlayerInCapture = true, EnemiesInCapture = 0 });
            Assert.AreEqual(0.5f, r.Capture, 1e-4f);
            r.Tick(2f, new EventInputs { PlayerInCapture = false });
            Assert.AreEqual(0.25f, r.Capture, 1e-4f, "ohne Spieler sinkt es langsam");
            r.Tick(4f, new EventInputs { PlayerInCapture = true });
            Assert.AreEqual(RunnerState.Succeeded, r.State);
            var def = new EventRunner(D("def", EventObjective.Defend));
            def.DefendHealth = 0f; def.Tick(0.1f, default(EventInputs));
            Assert.AreEqual(RunnerState.Failed, def.State);
            var hold = new EventRunner(D("hold", EventObjective.Defend));
            hold.Tick(61f, default(EventInputs));
            Assert.AreEqual(RunnerState.Succeeded, hold.State, "Verteidigen: Zeit überstanden");
            Assert.AreEqual(1f, EventDefinition.EventScale(5), "Skalierungs-Hook konstant 1");
        }

        [Test]
        public void Content_Migration_SevenEvents_SixObjectives_TwoChains()
        {
            var defs = new List<EventDefinition>();
            foreach (var g in AssetDatabase.FindAssets("t:EventDefinition", new[] { "Assets/ProjectTyrant/Data/Events" }))
                defs.Add(AssetDatabase.LoadAssetAtPath<EventDefinition>(AssetDatabase.GUIDToAssetPath(g)));
            Assert.GreaterOrEqual(defs.Count, 7);
            var objs = new HashSet<EventObjective>(); var chains = new HashSet<string>(); var triggers = new HashSet<EventTriggerType>();
            foreach (var d in defs) { objs.Add(d.Objective); if (!string.IsNullOrEmpty(d.ChainId)) chains.Add(d.ChainId); foreach (var t in d.Triggers) triggers.Add(t.Type); }
            Assert.AreEqual(6, objs.Count, "alle 6 Zieltypen");
            Assert.GreaterOrEqual(chains.Count, 2);
            Assert.AreEqual(6, triggers.Count, "alle 6 Auslöser-Typen");
            var defend = defs.Find(d => d.Id == "dornwacht_defend");
            Assert.AreEqual("dornwacht_assault", defend.SuccessNext); Assert.AreEqual("dornwacht_recapture", defend.FailNext);
            // Balance B1 (DECISIONS): Die Verteidigung ist ein Stufe-1-Event – ihr Fehlschlag setzt „Dornwacht gefallen“ nicht mehr (kein Hub-Angriff für neue Spieler);
            // der Meta-Zustand entsteht erst, wenn auch „Zurückerobern“ (Stufe 2) scheitert, und „Sonnfurt unter Angriff“ hängt weiter am Flag.
            Assert.IsTrue(string.IsNullOrEmpty(defend.SetFlagOnFail), "Stufe 1: keine große Welt-Folge bei Fehlschlag");
            Assert.AreEqual(Balance.EventTier.Easy, defend.Tier);
            var meta = defs.Find(d => d.Id == "sonnfurt_defense");
            Assert.IsTrue(meta.Triggers.Exists(t => t.Type == EventTriggerType.WorldState && t.Param == EventDirector.FlagDornwachtFallen));
            var recapture = defs.Find(d => d.Id == "dornwacht_recapture");
            Assert.AreEqual(EventDirector.FlagDornwachtFallen, recapture.SetFlagOnFail, "Meta: Zurückerobern scheitert → Dornwacht gefallen");
            Assert.AreEqual(EventDirector.FlagDornwachtFallen, recapture.ClearFlagOnSuccess, "Zurückerobern hebt den Meta-Zustand auf");
        }
    }
}
