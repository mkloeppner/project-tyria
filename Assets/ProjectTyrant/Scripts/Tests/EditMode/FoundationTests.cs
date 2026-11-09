using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    public class AttributeSetTests
    {
        [Test]
        public void PrimaryBase_GoesFrom37To1000()
        {
            Assert.AreEqual(37f, AttributeSet.PrimaryBase(1), 0.01f);
            Assert.AreEqual(1000f, AttributeSet.PrimaryBase(80), 0.01f);
            var s = new AttributeSet { Level = 1 };
            Assert.AreEqual(37f, s.Get(Attr.Power), 0.01f);
            Assert.AreEqual(37f, s.Get(Attr.Vitality), 0.01f);
            Assert.AreEqual(0f, s.Get(Attr.Ferocity), 0.01f, "Sekundärattribute starten bei 0");
            Assert.AreEqual(0f, s.Get(Attr.ConditionDamage), 0.01f);
        }

        [Test]
        public void Derived_CritChance_CritDamage_Health_Armor()
        {
            var s = new AttributeSet { Level = 80 };
            Assert.AreEqual(0.05f, s.CritChance, 0.0001f, "5 % beim Grundwert");
            Assert.AreEqual(1.5f, s.CritDamage, 0.0001f);
            var src = new FlatAttributeSource();
            src.Values[(int)Attr.Precision] = 210f; src.Values[(int)Attr.Ferocity] = 150f; src.Values[(int)Attr.Vitality] = 100f; src.Defense = 500f;
            s.AddSource(src);
            Assert.AreEqual(0.15f, s.CritChance, 0.0001f, "+210 Präzision = +10 % auf Level 80");
            Assert.AreEqual(1.6f, s.CritDamage, 0.0001f, "Wildheit/15 %");
            Assert.AreEqual(AttributeSet.ClassBaseHealthL80 + 1100f * 10f, s.MaxHealth, 0.1f);
            Assert.AreEqual(1000f + 500f, s.Armor, 0.01f);
        }

        [Test]
        public void Sources_AggregateFlatAndPercent_AndRemove()
        {
            var s = new AttributeSet { Level = 80 };
            var flat = new FlatAttributeSource(); flat.Values[(int)Attr.Power] = 200f;
            var pct = new PercentSource();
            s.AddSource(flat); s.AddSource(pct);
            Assert.AreEqual((1000f + 200f) * 1.1f, s.Get(Attr.Power), 0.01f);
            s.RemoveSource(flat);
            Assert.AreEqual(1100f, s.Get(Attr.Power), 0.01f);
        }

        class PercentSource : IAttributeSource { public void Contribute(AttributeAccumulator acc, int level) => acc.AddPercent(Attr.Power, 0.1f); }
    }

    public class DamageFormulaTests
    {
        [Test]
        public void Direct_IsWeaponStrengthTimesPowerTimesCoefficientOverArmor()
        {
            Assert.AreEqual(1000f * 2000f * 1.5f / 2500f, DamageFormula.Direct(1000f, 2000f, 1.5f, 2500f), 0.01f);
            Assert.AreEqual(55f, DamageFormula.WeaponStrength(1), 0.01f);
            Assert.AreEqual(1000f, DamageFormula.WeaponStrength(80), 0.01f);
        }

        [Test]
        public void Crit_MultipliesWithCritDamage_AndWeaknessGlances()
        {
            var s = new AttributeSet { Level = 80 };
            var src = new FlatAttributeSource(); src.Values[(int)Attr.Precision] = 5000f; src.Values[(int)Attr.Ferocity] = 750f;
            s.AddSource(src);
            var r = DamageFormula.Compute(s, 1f, 1000f, new System.Random(1));
            Assert.IsTrue(r.Crit, "100 % Krit-Chance");
            Assert.AreEqual(1000f * 1000f / 1000f * 2.0f, r.Amount, 0.01f, "150 % + 50 % Wildheit");
            int glances = 0;
            var rng = new System.Random(5);
            for (int i = 0; i < 200; i++) if (DamageFormula.Compute(new AttributeSet { Level = 80 }, 1f, 1000f, rng, -1f, 1f, true).Glancing) glances++;
            Assert.That(glances, Is.InRange(70, 130), "Schwäche: ~50 % Streiftreffer");
        }

        [Test]
        public void TargetSide_Protection_Aegis_Vulnerability()
        {
            var go = new GameObject("T");
            try
            {
                var c = go.AddComponent<Combatant>(); c.EnsureInitialized(); c.SetMaxHealth(100000f, true);
                c.Effects.Apply(EffectType.Protection, 1, 5f);
                Assert.AreEqual(670f, c.TakeDamage(new DamageInfo { Amount = 1000f }), 0.5f, "Schutz −33 %");
                c.Effects.Apply(EffectType.Aegis, 1, 5f);
                Assert.AreEqual(0f, c.TakeDamage(new DamageInfo { Amount = 1000f }), "Aegis blockt");
                Assert.IsFalse(c.Effects.Has(EffectType.Aegis), "Aegis verbraucht");
                c.Effects.Remove(EffectType.Protection);
                c.Effects.Apply(EffectType.Vulnerability, 10, 5f);
                Assert.AreEqual(1100f, c.TakeDamage(new DamageInfo { Amount = 1000f }), 0.5f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Blind_MakesNextAttackMiss()
        {
            var a = new GameObject("A"); var b = new GameObject("B");
            try
            {
                var src = a.AddComponent<Combatant>(); src.EnsureInitialized(); src.Team = Team.Player;
                var tgt = b.AddComponent<Combatant>(); tgt.EnsureInitialized(); tgt.SetMaxHealth(10000f, true);
                src.Effects.Apply(EffectType.Blind, 1, 5f);
                Assert.AreEqual(0f, DamageCalculator.DealDirect(src, tgt, 1f));
                Assert.IsFalse(src.Effects.Has(EffectType.Blind));
                Assert.Greater(DamageCalculator.DealDirect(src, tgt, 1f), 0f);
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
        }

        [Test]
        public void FallDamage_StartsAboveEightMeters()
        {
            Assert.AreEqual(0f, PlayerController.FallDamageFraction(7.9f, 8f, 12f));
            Assert.AreEqual(0.5f, PlayerController.FallDamageFraction(14f, 8f, 12f), 0.001f);
            Assert.AreEqual(1f, PlayerController.FallDamageFraction(25f, 8f, 12f));
        }
    }

    public class EffectModelTests
    {
        [Test]
        public void Intensity_StacksToMax_DurationStacksAddUp()
        {
            var e = new EffectModel();
            e.Apply(EffectType.Bleeding, 30, 5f);
            Assert.AreEqual(25, e.GetStacks(EffectType.Bleeding));
            e.Apply(EffectType.Swiftness, 1, 5f);
            e.Apply(EffectType.Swiftness, 1, 5f);
            Assert.AreEqual(1, e.GetStacks(EffectType.Swiftness), "Dauer-Stapelung: ein Eintrag");
            Assert.AreEqual(10f, e.GetRemaining(EffectType.Swiftness), 0.001f);
        }

        [Test]
        public void Effects_Expire_AndCleanseRemovesConditions()
        {
            var e = new EffectModel();
            e.Apply(EffectType.Might, 3, 2f);
            e.Apply(EffectType.Burning, 2, 10f);
            e.Apply(EffectType.Poison, 1, 10f);
            e.Tick(2.1f, null);
            Assert.AreEqual(0, e.GetStacks(EffectType.Might));
            Assert.AreEqual(2, e.ConditionCount());
            Assert.AreEqual(1, e.Cleanse(1));
            Assert.AreEqual(1, e.ConditionCount());
        }

        [Test]
        public void Duration_ScalesWithConcentrationAndExpertise()
        {
            var go = new GameObject("Src");
            try
            {
                var src = go.AddComponent<Combatant>(); src.EnsureInitialized(); src.Level = 80;
                var f = new FlatAttributeSource(); f.Values[(int)Attr.Concentration] = 750f; f.Values[(int)Attr.Expertise] = 300f;
                src.Stats.AddSource(f);
                var e = new EffectModel();
                e.Apply(EffectType.Fury, 1, 10f, src);
                Assert.AreEqual(15f, e.GetRemaining(EffectType.Fury), 0.01f, "+50 % Segensdauer");
                e.Apply(EffectType.Cripple, 1, 10f, src);
                Assert.AreEqual(12f, e.GetRemaining(EffectType.Cripple), 0.01f, "+20 % Zustandsdauer");
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Stability_BlocksControl_FuryRaisesCrit_RegenerationHeals()
        {
            var go = new GameObject("C");
            try
            {
                var c = go.AddComponent<Combatant>(); c.EnsureInitialized(); c.Level = 80; c.SetMaxHealth(10000f, true);
                c.Effects.Apply(EffectType.Stability, 1, 5f);
                c.Effects.Apply(EffectType.Stun, 1, 2f);
                Assert.IsFalse(c.Effects.Has(EffectType.Stun));
                float crit = c.Stats.CritChance;
                c.Effects.Apply(EffectType.Fury, 1, 5f);
                Assert.AreEqual(crit + 0.2f, c.Stats.CritChance, 0.0001f);
                c.SetHealth(5000f);
                c.Effects.Apply(EffectType.Regeneration, 1, 5f);
                c.Effects.Tick(1.0f, c);
                Assert.Greater(c.Health, 5000f);
                c.Effects.Apply(EffectType.Chill, 1, 5f);
                Assert.AreEqual(EffectRules.ChillMoveFactor, c.MoveSpeedMultiplier, 0.001f, "Kühle −66 %");
                c.Effects.Remove(EffectType.Chill);
                c.Effects.Apply(EffectType.Immobilize, 1, 2f);
                Assert.AreEqual(0f, c.MoveSpeedMultiplier);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Catalog_HasTwelveBoonsAndFourteenConditions()
        {
            int boons = 0, conds = 0;
            foreach (var i in EffectCatalog.All) { if (i.Cat == EffectCategory.Boon) boons++; if (i.Cat == EffectCategory.Condition) conds++; }
            Assert.AreEqual(12, boons); Assert.AreEqual(14, conds);
        }
    }

    public class ProgressionTests
    {
        [Test]
        public void XpCurve_LevelUpGrantsHeroPoint()
        {
            var p = new ProgressionModel();
            int ups = 0; p.LeveledUp += _ => ups++;
            Assert.AreEqual(400, ProgressionModel.XpToNext(1));
            Assert.AreEqual(520, ProgressionModel.XpToNext(2));
            p.AddXp(400 + 520 + 10);
            Assert.AreEqual(3, p.Level); Assert.AreEqual(10, p.Xp); Assert.AreEqual(2, ups);
            // Spezifikation M10: 4 Heldenpunkte je Stufe (inkl. Stufe 1) → Stufe 3 = 12
            Assert.AreEqual(3 * ProgressionModel.HeroPointsPerLevel, p.HeroPoints);
            Assert.IsTrue(p.TrySpendHeroPoints(12)); Assert.IsFalse(p.TrySpendHeroPoints(1));
        }

        [Test]
        public void KillXp_DependsOnLevelAndRank()
        {
            int trash = ProgressionModel.KillXp(5, 5, EnemyTier.Trash);
            Assert.Greater(ProgressionModel.KillXp(5, 5, EnemyTier.Veteran), trash);
            Assert.Greater(ProgressionModel.KillXp(5, 5, EnemyTier.Champion), ProgressionModel.KillXp(5, 5, EnemyTier.Veteran));
            Assert.Less(ProgressionModel.KillXp(15, 2, EnemyTier.Trash), ProgressionModel.KillXp(2, 2, EnemyTier.Trash), "graue Gegner geben wenig");
        }
    }

    public class SaveGameTests
    {
        [Test]
        public void RoundTrip_PreservesProgressWalletPositionFogAndCompletion()
        {
            var s = new ProfileState();
            s.Progression.SetLevel(7); s.Progression.Xp = 123; s.Progression.HeroPointsSpent = 2;
            s.Wallet.AddCopper(WalletModel.FromGold(1, 23, 45)); s.Wallet.AddKarma(678);
            s.HasPosition = true; s.Position = new Vector3(12.5f, 3f, -40f); s.RotationY = 90f;
            s.Fog.Reveal(new Vector3(-60, 0, -60), 30f);
            s.Completed.Add("wp_sonnfurt"); s.Hearts["heart_sonnfurt"] = 0.4f;

            string json = SaveSystem.ToJson(s.ToDto());
            var loaded = new ProfileState();
            loaded.LoadDto(SaveSystem.FromJson(json), null, null);

            Assert.AreEqual(7, loaded.Progression.Level);
            Assert.AreEqual(123, loaded.Progression.Xp);
            Assert.AreEqual(s.Progression.HeroPoints, loaded.Progression.HeroPoints);
            Assert.AreEqual(2, loaded.Progression.HeroPointsSpent);
            Assert.AreEqual(12345, loaded.Wallet.Copper);
            Assert.AreEqual(678, loaded.Wallet.Karma);
            Assert.IsTrue(loaded.HasPosition);
            Assert.AreEqual(s.Position, loaded.Position);
            Assert.AreEqual(s.Fog.RevealedCount, loaded.Fog.RevealedCount);
            Assert.IsTrue(loaded.Fog.IsRevealed(new Vector3(-60, 0, -60)));
            Assert.IsTrue(loaded.Completed.Contains("wp_sonnfurt"));
            Assert.AreEqual(0.4f, loaded.Hearts["heart_sonnfurt"], 0.0001f);
        }

        [Test]
        public void File_WriteRead_Delete()
        {
            string path = Path.Combine(Path.GetTempPath(), "pt_save_test_" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var dto = new SaveGameDto { Level = 5, Copper = 999 };
                SaveSystem.Write(dto, path);
                Assert.IsTrue(SaveSystem.Exists(path));
                var r = SaveSystem.Read(path);
                Assert.AreEqual(5, r.Level); Assert.AreEqual(999, r.Copper);
                SaveSystem.Delete(path);
                Assert.IsFalse(SaveSystem.Exists(path));
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        [Test]
        public void Wallet_ConvertsGoldSilverCopper()
        {
            Assert.AreEqual(10000, WalletModel.FromGold(1));
            Assert.AreEqual("1 g 23 s 45 k", WalletModel.Format(12345));
            Assert.AreEqual("5 s 0 k", WalletModel.Format(500));
            var w = new WalletModel();
            w.AddCopper(100);
            Assert.IsFalse(w.TrySpendCopper(101));
            Assert.IsTrue(w.TrySpendCopper(100));
            Assert.AreEqual(0, w.Copper);
        }
    }
}
