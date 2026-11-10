using System.Linq;
using NUnit.Framework;
using ProjectTyrant.Balance;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Balance B1: Hilfe in der Welt, Startausrüstung, Dornwacht-Werte, Event-Stufen und Profile als Daten (nach Rebuild Everything).</summary>
    public class BalanceB1DataTests
    {
        const string Data = "Assets/ProjectTyrant/Data";

        [Test] public void NpcRevive_OnlyStage01()
        {
            var m = new NpcReviveModel();
            Assert.IsFalse(m.Begin(DangerTable.Default(2), true));
            Assert.IsFalse(m.Begin(DangerTable.Default(0), false));
            Assert.IsTrue(m.Begin(DangerTable.Default(1), true));
        }
        [Test] public void NpcRevive_Within20s_OnlyWithoutHostiles()
        {
            var m = new NpcReviveModel(); m.Begin(DangerTable.Default(0), true);
            float t = 0f; bool done = false;
            for (; t < 30f && !done; t += 0.1f) done = m.Tick(0.1f, t < 5f);   // 5 s Gegner in der Nähe
            Assert.IsTrue(done); Assert.LessOrEqual(t, NpcReviveModel.MaxWait + 0.2f);
            var h = new NpcReviveModel(); h.Begin(DangerTable.Default(0), true);
            for (int i = 0; i < 300; i++) Assert.IsFalse(h.Tick(0.1f, true));
        }
        [Test] public void Threat_GuardsTakeAggro()
        {
            Assert.IsTrue(ThreatModel.SwitchToAttacker(0, true, true, DangerTable.Default(0).GuardsHelp));
            Assert.IsTrue(ThreatModel.SwitchToAttacker(2, true, true, DangerTable.Default(2).GuardsHelp));
            Assert.IsFalse(ThreatModel.SwitchToAttacker(3, true, true, DangerTable.Default(3).GuardsHelp));
            Assert.IsFalse(ThreatModel.SwitchToAttacker(1, false, true, true));
            Assert.Less(ThreatModel.GuardPriority(true, 20f), ThreatModel.GuardPriority(false, 5f));
        }
        [Test] public void Downed_GraceByStage()
        {
            Assert.AreEqual(0, DangerTable.Default(0).DownedMode); Assert.AreEqual(1, DangerTable.Default(1).DownedMode);
            Assert.AreEqual(2, DangerTable.Default(2).DownedMode); Assert.AreEqual(3, DangerTable.Default(4).DownedMode);
        }
        [Test] public void CombatEnd_After5s() { Assert.AreEqual(5f, DangerTable.CombatEndDelay); var g = new GameObject("t"); g.AddComponent<Combatant>(); var r = g.AddComponent<OutOfCombatRegen>(); Assert.AreEqual(5f, r.Delay); Object.DestroyImmediate(g); }
        [Test] public void StarterKit_TwoDodges_WeaponAndArmor()
        {
            var d = new DodgeModel(); d.Endurance.Fill();
            Assert.GreaterOrEqual(d.Endurance.Charges, 2);
            var items = AssetDatabase.FindAssets("t:ItemDefinition", new[] { Data }).Select(g => AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(i => i != null).ToList();
            Assert.IsTrue(items.Any(i => i.Id == "gs_recruit"), "Rekrutenwaffe");
            Assert.GreaterOrEqual(items.Count(i => i.Id != null && i.Id.StartsWith("ar_recruit_")), 3, "≥ 3 Rüstungsteile");
            Assert.AreEqual(6, items.Count(i => i.Id != null && i.Id.StartsWith("ar_fine_")), "Edel-Set bis Level 5");
        }
        [Test] public void Profiles_AssetsMatchTable()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<BalanceConfig>(Data + "/Balance/BalanceConfig.asset");
            Assert.IsNotNull(cfg, "BalanceConfig fehlt (Rebuild Everything)");
            for (int i = 0; i <= 5; i++) { Assert.IsNotNull(cfg.Levels[i]); Assert.AreEqual(DangerTable.Default(i).Leash, cfg.Levels[i].Settings.Leash); }
        }
        [Test] public void Archetypes_BossesKeepValues()
        {
            foreach (var id in new[] { "WorldBoss_Frostschlund", "Meta_Sturmtyrann", "Story_Vareth" })
            {
                var a = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(Data + "/Enemies/Enemy_" + id + ".asset");
                Assert.IsNotNull(a, id); Assert.IsTrue(a.CurveExempt, id); Assert.IsTrue(a.PursuitExempt, id);
            }
            var boar = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(Data + "/Enemies/Enemy_Wild_Eber.asset");
            Assert.IsNotNull(boar); Assert.IsTrue(boar.NeutralCreature);
        }
        [Test] public void Dornwacht_Retuned()
        {
            var e = AssetDatabase.LoadAssetAtPath<EventDefinition>(Data + "/Events/Event_dornwacht_defend.asset");
            Assert.IsNotNull(e);
            Assert.AreEqual(EventTier.Easy, e.Tier); Assert.IsTrue(e.TierSet);
            Assert.IsFalse(e.Triggers.Exists(t => t.Type == EventTriggerType.Timer && t.Delay < EventTierRules.MinAutoTimer), "kein 60-s-Auto-Start");
            Assert.GreaterOrEqual(e.Duration, 150f); Assert.AreEqual(3, e.WaveSize);
            Assert.IsTrue(string.IsNullOrEmpty(e.SetFlagOnFail), "keine große Welt-Folge in Stufe 1");
            Assert.LessOrEqual(Mathf.FloorToInt((e.Duration - 10f) / e.WaveInterval), 2, "2 Wellen");
            var g = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(Data + "/Enemies/Enemy_Champion_Grolmok.asset");
            Assert.AreEqual(13, g.FixedLevel); Assert.GreaterOrEqual(g.TelegraphDelay, 1f); Assert.IsFalse(g.CurveExempt);
            Assert.GreaterOrEqual(EnemyCurve.SimTtd(13, EnemyTier.Champion, DangerTable.Default(2), g.AttackInterval, 1), 20f, "Zeit-bis-Tod untätig ≥ 20 s");
        }
        [Test] public void Events_AllTiered_NoEarlyAutoStart()
        {
            var all = AssetDatabase.FindAssets("t:EventDefinition", new[] { Data }).Select(g => AssetDatabase.LoadAssetAtPath<EventDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(e => e != null).ToList();
            Assert.Greater(all.Count, 20);
            foreach (var e in all)
            {
                if (!e.TierSet) continue;   // Events ohne Zone (Story-Instanz) werden nicht eingestuft
                if (e.Tier <= EventTier.Easy) Assert.IsFalse(e.Triggers.Exists(t => t.Type == EventTriggerType.Timer && t.Delay < EventTierRules.MinAutoTimer), e.Id);
                if (e.Objective == EventObjective.WorldBoss) Assert.AreEqual(EventTier.Meta, e.Tier, e.Id);
            }
            Assert.Greater(all.Count(e => e.TierSet), 20);
        }
        [Test] public void Zones_DangerMapsBaked_StartZoneStage0AtHub()
        {
            var z = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(Data + "/Zones/Zone_sonnfurt.asset");
            Assert.IsNotNull(z); Assert.Greater(z.DangerMap.Anchors.Count, 0, "Karte gebacken");
            Assert.AreEqual(0, z.DangerMap.At(new Vector2(-60, -60)));
            foreach (var id in ZoneDangerTable.Specs.Keys)
            {
                var d = AssetDatabase.LoadAssetAtPath<ZoneDefinition>(Data + "/Zones/Zone_" + id + ".asset");
                Assert.IsNotNull(d, id); Assert.Greater(d.DangerMap.Areas.Count, 0, id);
                Assert.IsEmpty(BalanceValidator.Validate(d.DangerMap, new SpawnInfo[0], new EventInfo[0]), id);
            }
        }
        [Test] public void FirstFifteenMinutes_NoDeath_ThreeSeeds()
        {
            foreach (int seed in new[] { 11, 23, 37 })
            {
                var r = FirstFifteen.Run(seed);
                Assert.IsFalse(r.Died, "Seed " + seed + ": " + string.Join(" | ", r.Log));
                Assert.That(r.Seconds, Is.InRange(600f, 1000f), "ca. 15 Minuten");
            }
        }

        [Test] public void Telegraph_FairnessMinimaPerStage()
        {
            Assert.GreaterOrEqual(EnemyCurve.Windup(0.6f, DangerTable.Default(1)), 1.2f);
            Assert.GreaterOrEqual(EnemyCurve.Windup(0.6f, DangerTable.Default(3)), 1.0f);
            Assert.GreaterOrEqual(EnemyCurve.Windup(0.6f, DangerTable.Default(5)), 0.75f);
        }
    }
}
