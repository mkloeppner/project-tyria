using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    static class TestSkills
    {
        public static SkillDefinition Make(string id, SkillSlotType t, int cost, UtilityCategory cat = UtilityCategory.None, bool start = false, int tier = 1)
        {
            var s = ScriptableObject.CreateInstance<SkillDefinition>();
            s.Id = id; s.DisplayName = id; s.SlotType = t; s.UnlockCost = cost; s.Category = cat; s.StartsUnlocked = start; s.Tier = tier;
            return s;
        }

        public static TraitDefinition Trait(string id, int tier, bool major, params ModifierSpec[] mods)
        {
            var t = ScriptableObject.CreateInstance<TraitDefinition>();
            t.Id = id; t.DisplayName = id; t.Tier = tier; t.Major = major; t.Modifiers = new List<ModifierSpec>(mods);
            return t;
        }

        public static SpecializationDefinition Spec(string id)
        {
            var s = ScriptableObject.CreateInstance<SpecializationDefinition>();
            s.Id = id;
            for (int i = 0; i < 3; i++) s.Minors[i] = Trait(id + "_m" + i, i + 1, false, new ModifierSpec { Kind = ModifierKind.AttributeFlat, Attr = Attr.Power, Value = 100 });
            for (int i = 0; i < 9; i++) s.Majors[i] = Trait(id + "_M" + i, i / 3 + 1, true, new ModifierSpec { Kind = ModifierKind.AttributeFlat, Attr = Attr.Precision, Value = 10 * (i + 1) });
            return s;
        }
    }

    public class SkillLoadoutTests
    {
        [Test]
        public void Unlock_CostsHeroPoints_AndRespectsBudget()
        {
            var p = new ProgressionModel { HeroPoints = 6 };
            var m = new SkillLoadoutModel();
            var u = TestSkills.Make("u", SkillSlotType.Utility, 4, UtilityCategory.Shout, false, 2);
            var e = TestSkills.Make("e", SkillSlotType.Elite, 8);
            Assert.IsFalse(m.IsUnlocked(u));
            Assert.IsTrue(m.Unlock(u, p, out _));
            Assert.AreEqual(2, p.AvailableHeroPoints);
            Assert.IsFalse(m.Unlock(u, p, out string again)); Assert.IsNotNull(again);
            Assert.IsFalse(m.Unlock(e, p, out string poor)); StringAssert.Contains("8", poor);
            Assert.AreEqual(4, m.Refund(new[] { u, e }, p));
            Assert.AreEqual(6, p.AvailableHeroPoints);
        }

        [Test]
        public void Slot_ChecksTypeAndUnlock_AndSwapsUtilities()
        {
            var m = new SkillLoadoutModel();
            var heal = TestSkills.Make("h", SkillSlotType.Heal, 0, start: true);
            var u1 = TestSkills.Make("u1", SkillSlotType.Utility, 0, UtilityCategory.Shout, true);
            var u2 = TestSkills.Make("u2", SkillSlotType.Utility, 0, UtilityCategory.Trap, true);
            var locked = TestSkills.Make("u3", SkillSlotType.Utility, 2, UtilityCategory.Trap);
            Assert.IsFalse(m.Slot(heal, 1, out _), "Heilung passt nicht in Hilfsplatz");
            Assert.IsTrue(m.Slot(heal, 0, out _));
            Assert.IsFalse(m.Slot(locked, 1, out string r)); Assert.IsNotNull(r);
            m.FillDefaults(new[] { heal, u1, u2 });
            Assert.AreSame(u1, m.Slots[1]); Assert.AreSame(u2, m.Slots[2]); Assert.IsNull(m.Slots[3]);
            Assert.IsTrue(m.Slot(u1, 2, out _));
            Assert.AreSame(u2, m.Slots[1], "Plätze werden getauscht, keine Duplikate");
            Assert.AreSame(u1, m.Slots[2]);
        }

        [Test]
        public void Save_RoundTripsUnlocksSlotsAndTraits()
        {
            var heal = TestSkills.Make("h", SkillSlotType.Heal, 0, start: true);
            var u = TestSkills.Make("u", SkillSlotType.Utility, 2, UtilityCategory.Shout);
            var spec = TestSkills.Spec("glut");
            var s = new ProfileState();
            s.Progression.HeroPoints = 40;
            s.Traits.SetSpecializations(new[] { spec });
            s.Loadout.Unlock(u, s.Progression, out _);
            s.Loadout.Slot(heal, 0, out _); s.Loadout.Slot(u, 3, out _);
            var line = s.Traits.Get("glut");
            s.Traits.UnlockTier(line, s.Progression, out _); s.Traits.UnlockTier(line, s.Progression, out _);
            s.Traits.UnlockMajor(line, 1, 2, s.Progression, out _); s.Traits.UnlockMajor(line, 2, 0, s.Progression, out _); s.Traits.UnlockMajor(line, 2, 1, s.Progression, out _);
            s.Traits.Select(line, 2, 1, out _);
            var json = JsonUtility.ToJson(s.ToDto());
            var t = new ProfileState();
            t.Traits.SetSpecializations(new[] { spec });
            t.LoadDto(JsonUtility.FromJson<SaveGameDto>(json), null, new[] { heal, u });
            Assert.IsTrue(t.Loadout.IsUnlocked(u));
            Assert.AreSame(u, t.Loadout.Slots[3]);
            var l2 = t.Traits.Get("glut");
            Assert.AreSame(spec, l2.Spec, "Linie bleibt mit Definition verbunden");
            Assert.AreEqual(2, l2.TiersUnlocked);
            Assert.AreEqual(3, l2.UnlockedMajors.Count);
            Assert.AreEqual(2, l2.Selected[0]); Assert.AreEqual(1, l2.Selected[1]); Assert.AreEqual(-1, l2.Selected[2]);
            Assert.AreEqual(s.Progression.HeroPointsSpent, t.Progression.HeroPointsSpent);
        }
    }

    public class TraitModelTests
    {
        [Test]
        public void Tiers_UnlockInOrder_MajorsNeedTier_AndAutoSelect()
        {
            var p = new ProgressionModel { HeroPoints = 100 };
            var m = new TraitModel();
            m.SetSpecializations(new[] { TestSkills.Spec("a") });
            var l = m.Get("a");
            Assert.IsFalse(m.UnlockMajor(l, 1, 0, p, out string need)); StringAssert.Contains("Rang 1", need);
            Assert.IsTrue(m.UnlockTier(l, p, out _));
            Assert.AreEqual(TraitModel.TierCost, p.HeroPointsSpent);
            Assert.IsTrue(m.UnlockMajor(l, 1, 1, p, out _));
            Assert.AreEqual(1, l.Selected[0], "Erste freigeschaltete große Eigenschaft wird gewählt");
            Assert.IsTrue(m.UnlockMajor(l, 1, 2, p, out _));
            Assert.AreEqual(1, l.Selected[0], "Weitere ändern die Wahl nicht");
            Assert.IsFalse(m.Select(l, 1, 0, out _), "Nicht freigeschaltet → nicht wählbar");
            Assert.IsTrue(m.Select(l, 1, 2, out _));
            Assert.AreEqual(TraitModel.TierCost + 2 * TraitModel.MajorCost, p.HeroPointsSpent);
            Assert.AreEqual(42, TraitModel.TotalCostPerLine);
        }

        [Test]
        public void ActiveTraits_ApplyModifiers_ToAttributes()
        {
            var p = new ProgressionModel { HeroPoints = 100 };
            var m = new TraitModel();
            m.SetSpecializations(new[] { TestSkills.Spec("a") });
            var l = m.Get("a");
            m.UnlockTier(l, p, out _); m.UnlockTier(l, p, out _);
            m.UnlockMajor(l, 2, 1, p, out _);                // Major-Index 4 → +50 Präzision
            int count = 0; foreach (var _ in m.ActiveTraits()) count++;
            Assert.AreEqual(3, count, "2 kleine + 1 große");
            var set = new CombatModifierSet();
            var stats = new AttributeSet { Level = 80 };
            stats.AddSource(set);
            float pow = stats.Get(Attr.Power), prec = stats.Get(Attr.Precision);
            m.ApplyTo(set); stats.MarkDirty();
            Assert.AreEqual(pow + 200f, stats.Get(Attr.Power), 0.01f);
            Assert.AreEqual(prec + 50f, stats.Get(Attr.Precision), 0.01f);
            m.ApplyTo(set); stats.MarkDirty();
            Assert.AreEqual(pow + 200f, stats.Get(Attr.Power), 0.01f, "Erneutes Anwenden ersetzt statt zu stapeln");
            Assert.AreEqual(TraitModel.TierCost * 2 + TraitModel.MajorCost, m.Refund(p));
            Assert.AreEqual(0, p.HeroPointsSpent);
        }

        [Test]
        public void HeroPointBudget_AboutSixtyPercentAtLevel15()
        {
            // Pool laut Spezifikation: Heil 2×2, Hilfe 2×(2+4+6)−… (siehe DECISIONS), Elite 8, 3 Linien × 42
            int skills = 40, traits = 3 * TraitModel.TotalCostPerLine;
            int atL15 = ProgressionModel.HeroPointsForLevel(15) + 4 * 10;   // 4 Heldenherausforderungen à 10
            float share = atL15 / (float)(skills + traits);
            Assert.That(share, Is.InRange(0.55f, 0.65f));
        }
    }

    public class ModifierTests
    {
        static ModifierSpec S(ModifierKind k) => new ModifierSpec { Kind = k };

        [Test]
        public void AttributeFlat_ScalesWithLevel_AndConversionUsesFinalValue()
        {
            var set = new CombatModifierSet();
            var stats = new AttributeSet { Level = 1 };
            stats.AddSource(set);
            var flat = S(ModifierKind.AttributeFlat); flat.Attr = Attr.Ferocity; flat.Value = 1000;
            set.Add(TraitModifierFactory.Create(flat, "t"));
            stats.MarkDirty();
            Assert.AreEqual(37f, stats.Get(Attr.Ferocity), 0.01f, "Stufe 1: 3,7 %");
            stats.Level = 80;
            Assert.AreEqual(1000f, stats.Get(Attr.Ferocity), 0.01f);
            var conv = S(ModifierKind.AttributeConversion); conv.Attr = Attr.ConditionDamage; conv.Attr2 = Attr.Precision; conv.Value = 0.1f;
            set.Add(TraitModifierFactory.Create(conv, "t"));
            stats.MarkDirty();
            Assert.AreEqual(100f, stats.Get(Attr.ConditionDamage), 0.01f, "10 % von 1000 Präzision");
            var crit = S(ModifierKind.CritChance); crit.Value = 0.05f;
            float before = stats.CritChance;
            set.Add(TraitModifierFactory.Create(crit, "t")); stats.MarkDirty();
            Assert.AreEqual(before + 0.05f, stats.CritChance, 0.0001f);
        }

        [Test]
        public void Recharge_And_Duration_ByCategory()
        {
            var set = new CombatModifierSet();
            var shout = TestSkills.Make("s", SkillSlotType.Utility, 0, UtilityCategory.Shout);
            var trap = TestSkills.Make("t", SkillSlotType.Utility, 0, UtilityCategory.Trap);
            var heal = TestSkills.Make("h", SkillSlotType.Heal, 0);
            var gs = TestSkills.Make("g", SkillSlotType.Weapon, 0); gs.Weapon = WeaponKind.Greatsword;
            var cd = S(ModifierKind.CategoryRecharge); cd.Category = UtilityCategory.Shout; cd.Value = 0.2f;
            var hcd = S(ModifierKind.CategoryRecharge); hcd.MatchSlotType = true; hcd.SlotType = SkillSlotType.Heal; hcd.Value = 0.33f;
            var wdmg = S(ModifierKind.CategoryDamage); wdmg.Weapon = WeaponKind.Greatsword; wdmg.Value = 0.05f;
            set.Add(TraitModifierFactory.Create(cd, "t")); set.Add(TraitModifierFactory.Create(hcd, "t")); set.Add(TraitModifierFactory.Create(wdmg, "t"));
            Assert.AreEqual(0.8f, set.RechargeMultiplier(shout), 0.0001f);
            Assert.AreEqual(1f, set.RechargeMultiplier(trap), 0.0001f);
            Assert.AreEqual(0.67f, set.RechargeMultiplier(heal), 0.0001f);
            Assert.AreEqual(1.05f, set.OutgoingMultiplier(null, null, gs), 0.0001f);
            Assert.AreEqual(1f, set.OutgoingMultiplier(null, null, trap), 0.0001f);
            var boons = S(ModifierKind.EffectDuration); boons.Scope = DurationScope.AllBoons; boons.Value = 0.2f;
            var burn = S(ModifierKind.EffectDuration); burn.Effect = EffectType.Burning; burn.Value = 0.33f;
            set.Add(TraitModifierFactory.Create(boons, "t")); set.Add(TraitModifierFactory.Create(burn, "t"));
            Assert.AreEqual(0.2f, set.DurationBonus(EffectType.Might), 0.0001f);
            Assert.AreEqual(0.33f, set.DurationBonus(EffectType.Burning), 0.0001f);
            Assert.AreEqual(0f, set.DurationBonus(EffectType.Bleeding), 0.0001f);
        }

        [Test]
        public void Conditional_Damage_And_Triggers_WithInternalCooldown()
        {
            var a = new GameObject("A"); var b = new GameObject("B");
            var clock = TriggerModifier.Clock;
            try
            {
                var self = a.AddComponent<Combatant>(); self.EnsureInitialized(); self.SetMaxHealth(10000f, true);
                var tgt = b.AddComponent<Combatant>(); tgt.EnsureInitialized(); tgt.SetMaxHealth(10000f, true);
                var vs = S(ModifierKind.DamageVsCondition); vs.Effect = EffectType.Vulnerability; vs.Value = 0.1f;
                var low = S(ModifierKind.DamageVsLowHealth); low.Value = 0.15f; low.Value2 = 0.5f;
                var fury = S(ModifierKind.DamageWhileBoon); fury.Effect = EffectType.Fury; fury.Value = 0.1f;
                foreach (var spec in new[] { vs, low, fury }) self.Modifiers.Add(TraitModifierFactory.Create(spec, "t"));
                Assert.AreEqual(1f, self.Modifiers.OutgoingMultiplier(self, tgt, null), 0.0001f);
                tgt.Effects.Apply(EffectType.Vulnerability, 1, 5f);
                self.Effects.Apply(EffectType.Fury, 1, 5f);
                tgt.TakeDamage(new DamageInfo { Amount = 6000f });
                Assert.AreEqual(1.35f, self.Modifiers.OutgoingMultiplier(self, tgt, null), 0.0001f);

                float now = 0f; TriggerModifier.Clock = () => now;
                var proc = S(ModifierKind.TriggerApplyEffect); proc.Trigger = TriggerType.Crit; proc.Target = ModifierTarget.Self; proc.Effect = EffectType.Might; proc.Stacks = 1; proc.Duration = 8f; proc.InternalCooldown = 1f;
                self.Modifiers.Add(TraitModifierFactory.Create(proc, "t"));
                var ctx = new TriggerContext { Type = TriggerType.Crit, Self = self, Target = tgt };
                self.Modifiers.Trigger(ctx); self.Modifiers.Trigger(ctx);
                Assert.AreEqual(1, self.Effects.GetStacks(EffectType.Might), "Interne Abklingzeit");
                now = 1.01f; self.Modifiers.Trigger(ctx);
                Assert.AreEqual(2, self.Effects.GetStacks(EffectType.Might));
                self.Modifiers.Trigger(new TriggerContext { Type = TriggerType.Dodge, Self = self });
                Assert.AreEqual(2, self.Effects.GetStacks(EffectType.Might), "Falscher Auslöser");
                var heal = S(ModifierKind.TriggerHeal); heal.Trigger = TriggerType.Kill; heal.Value = 1500f; heal.Value2 = 0.4f;
                self.Modifiers.Add(TraitModifierFactory.Create(heal, "t"));
                self.TakeDamage(new DamageInfo { Amount = 3000f });
                float hp = self.Health;
                tgt.Die(self);
                Assert.Greater(self.Health, hp, "Kill-Auslöser heilt (über Combatant.Die)");
            }
            finally { TriggerModifier.Clock = clock; Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }
    }
}
