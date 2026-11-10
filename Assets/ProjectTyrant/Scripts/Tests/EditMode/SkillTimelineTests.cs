using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Anim;
using ProjectTyrant.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 5 / M22: Zeitachsen aller Skills (datengetrieben), Auto-Ketten, Warteschlange, Unterbrechung, Hit-Stop.</summary>
    public class SkillTimelineTests
    {
        static List<SkillDefinition> AllSkills()
        {
            var l = new List<SkillDefinition>();
            foreach (var g in AssetDatabase.FindAssets("t:SkillDefinition", new[] { PTEditorUtil.DataDir }))
            {
                var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g));
                if (s != null) l.Add(s);
            }
            return l;
        }

        [Test]
        public void EverySkill_HasConsistentTimeline_WithAllThreeTracks()
        {
            var set = ClipBuilder.HumanoidSet;
            Assert.IsNotNull(set, "Clip-Satz fehlt (Rebuild)");
            var errors = new List<string>();
            var skills = AllSkills();
            Assert.Greater(skills.Count, 25);
            foreach (var s in skills)
            {
                var tl = s.Timeline;
                if (tl == null || !tl.IsBuilt) { errors.Add(s.Id + ": keine Zeitachse"); continue; }
                var e = set.Get(tl.Clip);
                if (e == null) { errors.Add(s.Id + ": Clip " + tl.Clip + " fehlt"); continue; }
                if (!tl.Validate(e.Length, out var err)) errors.Add(s.Id + ": " + err);
                // Treffer-Marker des Clips fällt auf den ersten Treffer (außer bei begrenztem Zeitfaktor und Schleifen-Clips)
                float mark = Mathf.Max(e.MarkerTime(Marker.Hit), e.MarkerTime(Marker.Projectile));
                float first = -1f; foreach (var m in e.Markers) if ((m.Type == Marker.Hit || m.Type == Marker.Projectile) && (first < 0f || m.Time < first)) first = m.Time;
                bool clamped = tl.AnimScale <= 0.301f || tl.AnimScale >= 2.999f || Mathf.Approximately(tl.AnimScale, 0.35f) || tl.Instant;
                if (!e.Loop && !tl.Channel && first > 0f && !clamped && Mathf.Abs(first * tl.AnimScale - tl.FirstHit) > 0.02f)
                    errors.Add(s.Id + ": Treffer im Clip bei " + (first * tl.AnimScale).ToString("0.00") + " s, Wirkung bei " + tl.FirstHit.ToString("0.00") + " s");
            }
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void GuardianAutoChains_HaveThreeLinks_RisingCoefficient_SameAverage_ThirdHasExtra()
        {
            foreach (var id in new[] { "GS1_Glutschnitt", "ST1_Funkenbolzen" })
            {
                var b = AssetDatabase.LoadAssetAtPath<SkillDefinition>(DataBuilder.SkillDir + "/Skill_" + id + ".asset");
                Assert.IsNotNull(b, id);
                Assert.AreEqual(2, b.Chain.Length, id);
                var links = new[] { b, b.Chain[0], b.Chain[1] };
                // Fix-Lauf F1 (Auftrag): Glied 1 und 2 Basis-Koeffizient, Glied 3 höher (+30–50 %) – vorher steigend 1 < 2 < 3; Mittelwert-Prüfung unverändert
                Assert.AreEqual(links[0].Coefficient, links[1].Coefficient, 1e-4f); Assert.Less(links[1].Coefficient, links[2].Coefficient);
                Assert.That(links[2].Coefficient / links[0].Coefficient, Is.InRange(1.3f, 1.5f));
                float avg = (links[0].Coefficient + links[1].Coefficient + links[2].Coefficient) / 3f;
                Assert.AreEqual(id.StartsWith("GS") ? 0.8f : 0.7f, avg, 0.005f, "Mittel = bisheriger Koeffizient (gleiche Zahlen)");
                // Fix-Lauf F1: Zusatzeffekt je Waffe kann am Ziel ODER am Wirker liegen (Großschwert: Macht + 3 Ziele) – geprüft wird die Summe beider Listen
                Assert.Greater(links[2].TargetBuffs.Count + links[2].SelfBuffs.Count, links[0].TargetBuffs.Count + links[0].SelfBuffs.Count, "Glied 3 mit Zusatzeffekt");
                Assert.IsTrue(links[2].ChainBonus, "Glied 3 als Kettenabschluss markiert");
                var clips = new HashSet<string>(); foreach (var l in links) clips.Add(l.Timeline.Clip);
                Assert.AreEqual(3, clips.Count, "jede Stufe eigene Animation");
                var sfx = new HashSet<string>(); foreach (var l in links) sfx.Add(l.Timeline.SfxRelease);
                Assert.AreEqual(3, sfx.Count, "jede Stufe eigener Sound");
                foreach (var l in links) Assert.AreEqual(l.CastTime, l.Timeline.Total, 0.02f, "Takt bleibt: Aktivierung + Nachwirkung = bisherige Aktivierungszeit");
            }
        }

        [Test]
        public void AutoChain_AdvancesAndResetsAfterTwoSeconds()
        {
            var c = new AutoChainModel();
            Assert.AreEqual(0, c.Current(0f));
            c.Completed(0.5f); Assert.AreEqual(1, c.Current(0.6f));
            c.Completed(1.0f); Assert.AreEqual(2, c.Current(1.2f));
            c.Completed(1.5f); Assert.AreEqual(0, c.Current(1.6f), "nach Glied 3 wieder Glied 1");
            c.Completed(2.0f); Assert.AreEqual(1, c.Current(3.9f));
            Assert.AreEqual(0, c.Current(4.01f), "> 2 s Pause → zurück");
        }

        [Test]
        public void Queue_QueuesDuringActivation_InstantRunsAlongside_ForceAndDodge()
        {
            var q = new SkillQueueModel<string>();
            Assert.AreEqual(SkillQueueModel<string>.Decision.StartNow, q.Press("A", false, false, 0f));
            q.Begin(0f, 0.75f, 0.25f);
            Assert.AreEqual(SkillQueueModel<string>.Decision.Queued, q.Press("B", false, false, 0.3f));
            Assert.AreEqual(SkillQueueModel<string>.Decision.Queued, q.Press("C", false, false, 0.4f));
            Assert.AreEqual("C", q.Queued, "zuletzt gedrückter gewinnt");
            Assert.AreEqual(SkillQueueModel<string>.Decision.StartAlongside, q.Press("Instant", true, false, 0.5f));
            Assert.AreEqual("C", q.Queued, "Sofort-Skill lässt die Warteschlange stehen");
            Assert.IsNull(q.PopReady(0.9f), "Nachwirkung läuft noch");
            Assert.AreEqual("C", q.PopReady(1.0f), "startet nach der Nachwirkung");
            q.Begin(2f, 1f, 0.2f); q.Press("D", false, false, 2.1f);
            q.Cancel(2.2f); Assert.IsNull(q.Queued, "Ausweichen leert"); Assert.IsFalse(q.IsBusy(2.21f));
            q.Begin(3f, 1f, 0.2f);
            Assert.AreEqual(SkillQueueModel<string>.Decision.ForceStart, q.Press("E", false, true, 3.1f), "Shift erzwingt");
        }

        [Test]
        public void Interrupt_ControlBreaksCast_AndLocksSkillFiveSeconds()
        {
            Assert.IsTrue(InterruptRule.Interrupts(EffectType.Stun));
            Assert.IsTrue(InterruptRule.Interrupts(EffectType.Fear));
            Assert.IsFalse(InterruptRule.Interrupts(EffectType.Cripple));
            Assert.AreEqual(5f, InterruptRule.LockedCooldown(0f));
            Assert.AreEqual(12f, InterruptRule.LockedCooldown(12f), "längere Abklingzeit bleibt");

            var go = new GameObject("CasterTest");
            try
            {
                var c = go.AddComponent<Combatant>(); c.EnsureInitialized();
                var caster = go.AddComponent<SkillCaster>(); caster.Init();
                var s = ScriptableObject.CreateInstance<SkillDefinition>();
                s.Delivery = SkillDelivery.Self; s.CastTime = 1f; s.Cooldown = 3f; s.Coefficient = 0f;
                s.Timeline = SkillTimeline.Derive(1f, 1, 0.25f, false, false, SkillMoveRule.Free, 0f, "Cast2H", 0.9f, 0.55f, 0.55f);
                Assert.IsTrue(caster.TryCast(s, true));
                Assert.IsTrue(caster.IsCasting);
                SkillDefinition interrupted = null; caster.Interrupted += x => interrupted = x;
                c.Effects.Apply(new EffectApplication(EffectType.Stun, 1, 1f), null);
                caster.Tick(0.1f);
                Assert.IsFalse(caster.IsCasting, "Betäubung bricht ab");
                Assert.AreSame(s, interrupted);
                Assert.AreEqual(5f, caster.GetCooldownRemaining(s), 0.01f, "5 s gesperrt");
                Object.DestroyImmediate(s);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void HitStop_OnlyHeavyOrCrit_Within40To80ms_ImpulseScalesWithClass()
        {
            Assert.AreEqual(0f, HitStopModel.Duration(100f, 10000f, false), "leichter Treffer: kein Hit-Stop");
            float c = HitStopModel.Duration(100f, 10000f, true);
            Assert.That(c, Is.InRange(0.04f, 0.08f));
            Assert.That(HitStopModel.Duration(1500f, 10000f, false), Is.InRange(0.04f, 0.08f));
            Assert.That(HitStopModel.Duration(9000f, 10000f, true), Is.InRange(0.04f, 0.08f));
            Assert.Greater(HitStopModel.Duration(3000f, 10000f, false), HitStopModel.Duration(1100f, 10000f, false));
            Assert.AreEqual(0f, HitStopModel.Impulse(HitStopModel.DamageClass.Light, false));
            Assert.Greater(HitStopModel.Impulse(HitStopModel.DamageClass.Huge, false), HitStopModel.Impulse(HitStopModel.DamageClass.Heavy, false));
        }

        [Test]
        public void Timeline_Derive_ClipTimesFactorEqualsActivationPlusAftercast()
        {
            var t = SkillTimeline.Derive(0.75f, 1, 0.25f, false, false, SkillMoveRule.Rooted, 0f, "Greatsword_Heavy", 1.38f, 0.78f, 0.78f);
            t.VfxRelease = "x"; t.VfxImpact = "x"; t.SfxRelease = "x"; t.SfxImpact = "x";
            Assert.IsTrue(t.Validate(1.38f, out var e), e);
            Assert.AreEqual(0.75f, t.FirstHit, 1e-4f);
            Assert.AreEqual(0.75f / 0.78f, t.AnimScale, 1e-4f);
            var multi = SkillTimeline.Derive(0.2f, 4, 0.22f, false, false, SkillMoveRule.Free, 0f, "Greatsword_Whirl", 1f, 0.3f, 0.8f);
            Assert.AreEqual(0.2f + 3 * 0.22f, multi.Activation, 1e-4f);
            Assert.AreEqual(4, multi.Hits.Count); Assert.IsTrue(multi.Channel);
            var inst = SkillTimeline.Derive(0f, 1, 0.25f, false, false, SkillMoveRule.Free, 0f, "Cheer", 1.2f, 0.6f, 0.6f);
            Assert.IsTrue(inst.Instant); Assert.AreEqual(0f, inst.Activation);
        }

        [Test]
        public void DirectionalHitReaction_PicksSideOfAttacker()
        {
            var go = new GameObject("HitDir");
            try
            {
                var t = go.transform;
                Assert.AreEqual("HitLight", AnimDriver.HitClipFor(t, new Vector3(0, 0, 3)));
                Assert.AreEqual("HitLightB", AnimDriver.HitClipFor(t, new Vector3(0, 0, -3)));
                Assert.AreEqual("HitLightL", AnimDriver.HitClipFor(t, new Vector3(-3, 0, 0.5f)));
                Assert.AreEqual("HitLightR", AnimDriver.HitClipFor(t, new Vector3(3, 0, 0.5f)));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
