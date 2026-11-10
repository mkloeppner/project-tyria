using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectTyrant.EditorTools;

namespace ProjectTyrant.Tests
{
    /// <summary>M27: Balancing-Tabellen vollständig, Wächter als Referenz unverändert, Skalierung trifft nur Schadenswerte.</summary>
    public class BalanceTests
    {
        [Test]
        public void FactorTables_CoverAllClassesAndSpecs_GuardianIsReference()
        {
            var lib = ClassBuilder.Library; Assert.IsNotNull(lib);
            foreach (var c in lib.Classes)
            {
                Assert.IsTrue(ClassBuilder.ClassDamage.ContainsKey(c.Id), "Klassenfaktor fehlt: " + c.Id);
                Assert.That(ClassBuilder.ClassFactor(c.Id), Is.InRange(0.5f, 2f), c.Id);
                foreach (var e in c.EliteSpecs)
                {
                    Assert.IsTrue(ClassBuilder.EliteDamage.ContainsKey(e.Id), "Spezialisierungsfaktor fehlt: " + e.Id);
                    Assert.That(ClassBuilder.EliteFactor(e.Id), Is.InRange(0.5f, 2f), e.Id);
                }
            }
            Assert.AreEqual(1f, ClassBuilder.ClassFactor("waechter"), "Wächter = Referenz (gleiche Zahlen wie vorher)");
        }

        [Test]
        public void ScaleDamage_TouchesCoefficientFieldTickAndDamagingConditionsOnly()
        {
            var s = ScriptableObject.CreateInstance<SkillDefinition>();
            s.Coefficient = 1f; s.FieldTickCoefficient = 0.2f; s.Cooldown = 10f; s.CastTime = 0.5f;
            s.TargetBuffs = new List<EffectApplication> { new EffectApplication(EffectType.Bleeding, 2, 4f), new EffectApplication(EffectType.Cripple, 1, 2f), new EffectApplication(EffectType.Burning, 1, 3f) };
            s.SelfBuffs = new List<EffectApplication> { new EffectApplication(EffectType.Might, 1, 8f) };
            ClassBuilder.ScaleDamage(s, 1.25f);
            Assert.AreEqual(1.25f, s.Coefficient, 1e-3f); Assert.AreEqual(0.25f, s.FieldTickCoefficient, 1e-3f);
            Assert.AreEqual(5f, s.TargetBuffs[0].Duration, 1e-3f, "Blutung skaliert"); Assert.AreEqual(2, s.TargetBuffs[0].Stacks, "Stapel bleiben");
            Assert.AreEqual(2f, s.TargetBuffs[1].Duration, 1e-3f, "Verkrüppelung (Kontrolle) bleibt");
            Assert.AreEqual(3.75f, s.TargetBuffs[2].Duration, 1e-3f, "Brennen skaliert");
            Assert.AreEqual(8f, s.SelfBuffs[0].Duration, 1e-3f, "Segen bleiben"); Assert.AreEqual(10f, s.Cooldown); Assert.AreEqual(0.5f, s.CastTime);
            Object.DestroyImmediate(s);
        }
    }
}
