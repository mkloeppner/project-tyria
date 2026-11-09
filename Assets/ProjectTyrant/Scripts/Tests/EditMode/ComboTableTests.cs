using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    public class ComboTableTests
    {
        const string TablePath = "Assets/ProjectTyrant/Data/Combos/ComboTable.asset";

        static ComboTable LoadTable()
        {
            var t = AssetDatabase.LoadAssetAtPath<ComboTable>(TablePath);
            Assert.IsNotNull(t, "ComboTable-Asset fehlt: " + TablePath);
            return t;
        }

        [Test]
        public void Table_HasAtLeastSixCombos_AndNoDuplicatePairs()
        {
            var t = LoadTable();
            Assert.GreaterOrEqual(t.Entries.Count, 6);
            for (int i = 0; i < t.Entries.Count; i++)
            for (int j = i + 1; j < t.Entries.Count; j++)
                Assert.IsFalse(t.Entries[i].Field == t.Entries[j].Field && t.Entries[i].Finisher == t.Entries[j].Finisher,
                    "Doppeltes Paar: " + t.Entries[i].Field + "+" + t.Entries[i].Finisher);
        }

        [Test]
        public void Table_ReturnsExpectedResult_ForEveryDefinedPair()
        {
            var t = LoadTable();
            var expected = ComboTable.CreateDefaultEntries();
            foreach (var e in expected)
            {
                Assert.IsTrue(t.TryGet(e.Field, e.Finisher, out var got), "Paar fehlt: " + e.Field + " + " + e.Finisher);
                Assert.AreEqual(e.ResultName, got.ResultName);
                Assert.AreEqual(e.ResultType, got.ResultType);
                Assert.AreEqual(e.Buff.Type, got.Buff.Type);
                Assert.AreEqual(e.Buff.Stacks, got.Buff.Stacks);
                Assert.AreEqual(e.Amount, got.Amount, 0.001f);
            }
        }

        [Test]
        public void Table_KeyExamples()
        {
            var t = LoadTable();
            Assert.IsTrue(t.TryGet(ComboFieldType.Fire, ComboFinisherType.Blast, out var fireBlast));
            Assert.AreEqual(ComboResultType.AreaBuffAllies, fireBlast.ResultType);
            Assert.AreEqual(EffectType.Might, fireBlast.Buff.Type);
            Assert.IsTrue(t.TryGet(ComboFieldType.Water, ComboFinisherType.Leap, out var waterLeap));
            Assert.AreEqual(ComboResultType.AreaHealAllies, waterLeap.ResultType);
            Assert.Greater(waterLeap.Amount, 0f);
            Assert.IsFalse(t.TryGet(ComboFieldType.None, ComboFinisherType.Blast, out _));
            Assert.IsFalse(t.TryGet(ComboFieldType.Smoke, ComboFinisherType.Whirl, out _), "Rauch+Wirbel ist bewusst nicht definiert");
        }

        [Test]
        public void Resolver_FireFieldPlusBlast_GrantsMightToCaster()
        {
            var t = LoadTable();
            var casterGo = new GameObject("Caster");
            var fieldGo = new GameObject("Field");
            try
            {
                var caster = casterGo.AddComponent<Combatant>();
                caster.Team = Team.Player;
                caster.SetMaxHealth(1000, true);
                Combatant.All.Add(caster);
                var field = fieldGo.AddComponent<ComboFieldZone>();
                field.Field = ComboFieldType.Fire; field.Radius = 4f; field.Owner = caster;
                ComboFieldZone.Active.Add(field);

                Assert.IsTrue(ComboResolver.TryFinisher(t, ComboFinisherType.Blast, Vector3.zero, caster, out var entry));
                Assert.AreEqual("Flammende Macht", entry.ResultName);
                Assert.AreEqual(3, caster.Effects.GetStacks(EffectType.Might));
            }
            finally
            {
                Combatant.All.Clear(); ComboFieldZone.Active.Clear();
                Object.DestroyImmediate(casterGo); Object.DestroyImmediate(fieldGo);
            }
        }
    }

    public class BuffTests
    {
        [Test]
        public void Might_AddsPower_And_Vulnerability_ScalesIncomingDamage()
        {
            // Spezifikation Lauf 3: Macht = +30 Kraft je Stapel (level-skaliert), Verwundbarkeit = +1 % je Stapel.
            var go = new GameObject("Vuln");
            try
            {
                var c = go.AddComponent<Combatant>(); c.EnsureInitialized(); c.Level = 80;
                float before = c.Stats.Get(Attr.Power);
                c.Effects.Apply(EffectType.Might, 10, 10f);
                Assert.AreEqual(before + 300f, c.Stats.Get(Attr.Power), 0.01f);
                c.SetMaxHealth(10000f, true);
                c.Effects.Apply(EffectType.Vulnerability, 25, 10f);
                float dealt = c.TakeDamage(new DamageInfo { Amount = 1000f });
                Assert.AreEqual(1250f, dealt, 0.01f);
            }
            finally { Object.DestroyImmediate(go); }
        }

                [Test]
        public void Stacks_AreCapped_AndExpire()
        {
            var b = new EffectModel();
            b.Apply(EffectType.Might, 40, 5f);
            Assert.AreEqual(25, b.GetStacks(EffectType.Might));
            b.Tick(5.1f, null);
            Assert.AreEqual(0, b.GetStacks(EffectType.Might));
        }

        [Test]
        public void Burning_DealsDamageOverTime()
        {
            var go = new GameObject("Burn");
            try
            {
                var c = go.AddComponent<Combatant>(); c.SetMaxHealth(1000f, true);
                c.Effects.Apply(EffectType.Burning, 2, 3f);
                c.Effects.Tick(1.0f, c);
                // Spezifikation Lauf 3: Tick = Basis(Level) + 0,155 × Zustandsschaden je Stapel
                Assert.AreEqual(1000f - 2 * DamageFormula.ConditionTick(EffectType.Burning, 0f, 1), c.Health, 0.01f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Chill_SlowsMovement()
        {
            var go = new GameObject("Chill");
            try
            {
                var c = go.AddComponent<Combatant>();
                c.Effects.Apply(EffectType.Chill, 1, 3f);
                Assert.Less(c.MoveSpeedMultiplier, 1f);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }

    public class SkillCasterTests
    {
        [Test]
        public void WeaponSwap_SwapsSlots_AndStartsTenSecondCooldown()
        {
            var go = new GameObject("Caster");
            try
            {
                go.AddComponent<Combatant>().Team = Team.Player;
                var caster = go.AddComponent<SkillCaster>();
                var a = ScriptableObject.CreateInstance<WeaponSet>(); var b = ScriptableObject.CreateInstance<WeaponSet>();
                a.Skills = new SkillDefinition[5]; b.Skills = new SkillDefinition[5];
                for (int i = 0; i < 5; i++) { a.Skills[i] = ScriptableObject.CreateInstance<SkillDefinition>(); b.Skills[i] = ScriptableObject.CreateInstance<SkillDefinition>(); }
                caster.WeaponSets = new[] { a, b };
                caster.Init();
                Assert.AreSame(a.Skills[0], caster.GetSkillInSlot(0));
                Assert.IsTrue(caster.TrySwapWeapons());
                Assert.AreSame(b.Skills[0], caster.GetSkillInSlot(0));
                Assert.AreEqual(10f, caster.SwapCooldownRemaining, 0.001f);
                Assert.IsFalse(caster.TrySwapWeapons(), "Swap während Cooldown muss scheitern");
                caster.Tick(10.01f);
                Assert.IsTrue(caster.TrySwapWeapons());
                Assert.AreSame(a.Skills[4], caster.GetSkillInSlot(4));
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Cooldown_StartsAfterCast_AndTicksDown()
        {
            var go = new GameObject("Caster");
            try
            {
                go.AddComponent<Combatant>().Team = Team.Player;
                var caster = go.AddComponent<SkillCaster>();
                var s = ScriptableObject.CreateInstance<SkillDefinition>();
                s.Delivery = SkillDelivery.Self; s.CastTime = 0f; s.Cooldown = 5f; s.Coefficient = 0f;
                caster.HealSkill = s;
                caster.Init();
                Assert.IsTrue(caster.TryUseSlot(5, true));
                Assert.AreEqual(5f, caster.GetCooldownRemaining(s), 0.001f);
                Assert.IsFalse(caster.TryUseSlot(5, true));
                caster.Tick(5.1f);
                Assert.AreEqual(0f, caster.GetCooldownRemaining(s), 0.001f);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}

namespace ProjectTyrant.Tests
{
    public class GroundTargetAimTests
    {
        [Test]
        public void GroundTargetSkill_EntersAimMode_AndCastsOnConfirm()
        {
            var go = new GameObject("Aimer");
            try
            {
                go.AddComponent<Combatant>().Team = Team.Player;
                var caster = go.AddComponent<SkillCaster>();
                var s = ScriptableObject.CreateInstance<SkillDefinition>();
                s.Delivery = SkillDelivery.GroundTarget; s.CastTime = 0f; s.Cooldown = 8f; s.Range = 10f; s.Coefficient = 0f;
                caster.EliteSkill = s;
                caster.Init();
                Assert.IsTrue(caster.TryUseSlot(9));
                Assert.IsTrue(caster.IsAiming, "Ohne Fast-Cast wird zuerst gezielt");
                Assert.AreEqual(0f, caster.GetCooldownRemaining(s), "Zielen startet noch keinen Cooldown");
                caster.CancelAim();
                Assert.IsFalse(caster.IsAiming);
                Assert.IsTrue(caster.TryUseSlot(9));
                Assert.IsTrue(caster.ConfirmAim());
                Assert.IsFalse(caster.IsAiming);
                Assert.AreEqual(8f, caster.GetCooldownRemaining(s), 0.001f, "Bestätigen wirkt den Skill");
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
