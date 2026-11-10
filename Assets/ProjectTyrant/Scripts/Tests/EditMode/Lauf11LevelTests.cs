using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectTyrant.Procedural;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 11 / M59: Level-80-Kurven, Re-Leveling, Gates, Heldenpunkte, XP-Simulation, Migration aller historischen Speicherstände.</summary>
    public class Lauf11LevelTests
    {
        [Test] public void Curve_PrimaryAtAnchorLevels()
        {
            Assert.AreEqual(37f, LevelCurves.Primary(1), 0.01f);
            Assert.AreEqual(200f, LevelCurves.Primary(20), 0.01f);
            Assert.AreEqual(300f, LevelCurves.Primary(30), 0.01f);
            Assert.AreEqual(500f, LevelCurves.Primary(50), 0.01f);
            Assert.AreEqual(1000f, LevelCurves.Primary(80), 0.01f);
        }

        [Test] public void Curve_SlowTo20ThenSteeper()
        {
            float early = (LevelCurves.Primary(20) - LevelCurves.Primary(1)) / 19f, late = (LevelCurves.Primary(80) - LevelCurves.Primary(60)) / 20f;
            Assert.Less(early, late);
            for (int l = 2; l <= 80; l++) Assert.Greater(LevelCurves.Primary(l), LevelCurves.Primary(l - 1), "streng steigend bei " + l);
        }

        [Test] public void Curve_FractionEndpoints()
        {
            Assert.AreEqual(0f, LevelCurves.Fraction(1), 1e-5f); Assert.AreEqual(1f, LevelCurves.Fraction(80), 1e-5f);
        }

        [Test] public void BuildCalc_Level80EqualsOldCap30()
        {
            // Lauf 10: Stufe 30 = 1.000 Attribute, Waffenstärke 1.000, Basis-HP 9.212 – jetzt Stufe 80
            Assert.AreEqual(1000f, AttributeSet.PrimaryBase(80), 0.01f);
            Assert.AreEqual(1000f, DamageFormula.WeaponStrength(80), 0.01f);
            var a = new AttributeSet { Level = 80 };
            Assert.AreEqual(AttributeSet.ClassBaseHealthL80 + a.Get(Attr.Vitality) * 10f, a.MaxHealth, 0.5f);
            Assert.AreEqual(55f, DamageFormula.WeaponStrength(1), 0.01f);
        }

        [Test] public void Xp_1To30Unchanged_ConstantFrom30()
        {
            Assert.AreEqual(400, ProgressionModel.XpToNext(1)); Assert.AreEqual(400 + 120 * 28, ProgressionModel.XpToNext(29));
            Assert.AreEqual(3880, ProgressionModel.XpToNext(30)); Assert.AreEqual(3880, ProgressionModel.XpToNext(79));
            Assert.AreEqual(int.MaxValue, ProgressionModel.XpToNext(80));
        }

        [Test] public void Xp_Level30CharacterLevelsOn()
        {
            var p = new ProgressionModel(); p.SetLevel(30);
            Assert.AreEqual(1, p.AddXp(3880)); Assert.AreEqual(31, p.Level);
        }

        [Test] public void HeroPoints_4UpTo30_Then1()
        {
            Assert.AreEqual(120, ProgressionModel.HeroPointsForLevel(30));
            Assert.AreEqual(170, ProgressionModel.HeroPointsForLevel(80));
            var p = new ProgressionModel(); p.SetLevel(80);
            Assert.AreEqual(170, p.HeroPoints);
        }

        [Test] public void HeroPoints_EconomyAt80CoversCoreAndElite()
        {
            // 170 aus Stufen + Heldenherausforderungen: 10 Zonen × 4 × 10 (Bestand) – Elite 30 je Spezialisierung
            int available = ProgressionModel.HeroPointsForLevel(80) + 10 * 4 * ZoneContent.ChallengeHeroPoints;
            Assert.GreaterOrEqual(available, 2 * 3 * EliteSpecDefinition.StepCost + 120, "zwei Elite-Spezialisierungen plus Kernlinien");
        }

        [Test] public void Gates_Chapters()
        {
            CollectionAssert.AreEqual(new[] { 1, 5, 12, 20, 30, 40, 50, 60, 70, 80 }, LevelBands.ChapterGates);
            Assert.AreEqual(80, EliteSpecDefinition.RequiredLevel); Assert.AreEqual(80, AttributeSet.MaxLevel);
        }

        [Test] public void Relevel_TableAsSpecified()
        {
            var want = new Dictionary<string, (int, int)> { { "sonnfurt", (1, 15) }, { "nebelmoor", (15, 25) }, { "frostkamm", (25, 35) }, { "gluetoede", (35, 45) }, { "silberkueste", (40, 50) },
                { "daemmerwald", (50, 60) }, { "duenenmeer", (60, 70) }, { "kristalltiefe", (65, 75) }, { "himmelsspitzen", (75, 80) }, { "tyrannenfeste", (80, 80) } };
            foreach (var kv in want) { Assert.IsTrue(LevelBands.TryGet(kv.Key, out var b), kv.Key); Assert.AreEqual(kv.Value.Item1, b.NewMin, kv.Key); Assert.AreEqual(kv.Value.Item2, b.NewMax, kv.Key); }
        }

        [Test] public void Relevel_MapLinearAndByDistance()
        {
            Assert.AreEqual(25, LevelBands.Map("frostkamm", 25, 0f)); Assert.AreEqual(35, LevelBands.Map("frostkamm", 30, 0f)); Assert.AreEqual(29, LevelBands.Map("frostkamm", 27, 0f), "27 → 29 (linear 25–30 → 25–35)");
            Assert.AreEqual(35, LevelBands.Map("gluetoede", 30, 0f)); Assert.AreEqual(45, LevelBands.Map("gluetoede", 30, 1f)); Assert.AreEqual(40, LevelBands.Map("gluetoede", 30, 0.5f));
            Assert.AreEqual(12, LevelBands.Map("sonnfurt", 12, 0.3f), "Startzone unverändert");
            Assert.AreEqual(7, LevelBands.Map("unbekannt", 7, 1f));
        }

        [Test] public void Relevel_LayoutsInsideBandAndNoBigJumps()
        {
            foreach (var id in new[] { "nebelmoor", "frostkamm", "gluetoede", "silberkueste", "daemmerwald", "duenenmeer", "kristalltiefe", "himmelsspitzen", "tyrannenfeste" })
            {
                var l = BiomeZoneContent.For(id); if (l == null) continue;
                LevelBands.TryGet(id, out var b);
                var lv = new List<int>();
                foreach (var s in l.AllSpots()) { Assert.That(s.Level, Is.InRange(b.NewMin, b.NewMax), id + " " + s.Id); lv.Add(s.Level); }
                foreach (var h in l.Hearts) Assert.That(h.Level, Is.InRange(b.NewMin, b.NewMax), id + " " + h.Id);
                lv.Sort(); Assert.LessOrEqual(LevelBands.MaxJump(lv), 10, id);
                var again = BiomeZoneContent.For(id); Assert.AreSame(l, again);
                foreach (var s in again.AllSpots()) Assert.That(s.Level, Is.InRange(b.NewMin, b.NewMax), "einmalig umgerechnet");
            }
        }

        [Test] public void Downleveling_AllBands()
        {
            foreach (var b in LevelBands.Existing)
            {
                Assert.AreEqual(Mathf.Min(80, b.NewMax + 1), WorldModel.EffectiveLevel(80, b.NewMax), b.Zone);
                Assert.AreEqual(b.NewMin, WorldModel.EffectiveLevel(b.NewMin, b.NewMax), "darunter keine Änderung");
            }
            Assert.AreEqual(0.5f, WorldModel.XpFactor(80, 16));
        }

        [Test] public void LootRarity_GW2Distribution()
        {
            Assert.AreEqual(ItemRarity.Fine, LevelCurves.LootRarityCap(10)); Assert.AreEqual(ItemRarity.Masterwork, LevelCurves.LootRarityCap(30));
            Assert.AreEqual(ItemRarity.Rare, LevelCurves.LootRarityCap(50)); Assert.AreEqual(ItemRarity.Exotic, LevelCurves.LootRarityCap(60)); Assert.AreEqual(ItemRarity.Exotic, LevelCurves.LootRarityCap(80));
        }

        [Test] public void Items_ValuesFollowNewCurve()
        {
            var d = ScriptableObject.CreateInstance<ItemDefinition>(); d.Type = ItemType.Armor; d.Armor = ArmorPiece.Chest; d.Rarity = ItemRarity.Exotic;
            d.ItemLevel = 80; float b80 = ItemStats.Budget(d); d.ItemLevel = 30; float b30 = ItemStats.Budget(d);
            Assert.AreEqual(0.3f, b30 / b80, 1e-3f, "Item-Level bleibt, Werte nach der 80er-Kurve");
        }

        [Test] public void XpSimulation_EachExistingZoneCarriesItsBand()
        {
            foreach (var b in LevelBands.Existing)
            {
                if (b.NewMax <= b.NewMin || b.Zone == "sonnfurt") continue;
                var l = BiomeZoneContent.For(b.Zone); if (l == null) continue;
                System.Func<BzSpot[], int[]> lv = a => System.Array.ConvertAll(a, s => s.Level);
                long xp = XpRewards.ZoneXp(lv(l.Waypoints), lv(l.Pois), lv(l.Vistas), lv(l.Challenges), lv(l.Insights), System.Array.ConvertAll(l.Hearts, h => h.Level), (b.NewMin + b.NewMax) / 2, 5, 2, b.NewMax);
                Assert.GreaterOrEqual(xp, XpRewards.BandXp(b.NewMin, b.NewMax), b.Zone + " trägt sein Band allein (Herzen, Events ×2, Erkundung)");
            }
        }

        [Test] public void XpSimulation_BandOf10Takes2To3Hours()
        {
            // Annahme: 18.000 EP/h auf Level 30+ (Herzen, Events, Kills, Erkundung) – Band 10 Stufen = 38.800 EP
            long band = XpRewards.BandXp(40, 50);
            Assert.AreEqual(38800, band);
            float hours = band / 18000f;
            Assert.That(hours, Is.InRange(2f, 3f));
        }

        [Test] public void Rewards_ScaleWithContentLevel()
        {
            Assert.AreEqual(1f, LevelCurves.RewardScale(8), 1e-4f);
            Assert.Greater(LevelCurves.RewardScale(60), LevelCurves.RewardScale(10), "Herz auf 60 gibt mehr als auf 10");
            Assert.AreEqual(LevelCurves.RewardScale(40), LevelCurves.RewardScale(70), 1e-4f, "ab 30 gleich");
        }

        // ---------------- Migration ----------------
        static readonly string[] Fixtures =
        {
            // Version 0/1 (Lauf 3): ein Nebel, Zonen-Truhe, keine Klasse
            "{\"Version\":1,\"Level\":14,\"Xp\":120,\"HeroPoints\":14,\"Copper\":5000,\"Completed\":[\"wp_sonnfurt\"],\"ZoneChestGranted\":true}",
            // Version 2 (Lauf 4): Zonen, Reittiere, Beherrschungen
            "{\"Version\":2,\"Level\":30,\"Xp\":0,\"HeroPoints\":120,\"CurrentZone\":\"gluetoede\",\"Mounts\":[\"Raptor\"],\"SelectedMount\":\"Raptor\",\"MasteryXp\":5000,\"MasteryPointsEarned\":4,\"Masteries\":[{\"Line\":\"Gleiten\",\"Rank\":2,\"Xp\":300}],\"Completed\":[\"zonechest:nebelmoor\"]}",
            // Version 3 (Lauf 5): Klasse, Elite
            "{\"Version\":3,\"Level\":30,\"ClassId\":\"elementar\",\"CharacterName\":\"Test\",\"EliteSpec\":\"katalysator\",\"HeroPoints\":150,\"HeroPointsSpent\":100,\"CurrentZone\":\"frostkamm\"}",
            // Version 4 (Lauf 6–10): Story
            "{\"Version\":4,\"Level\":30,\"ClassId\":\"waechter\",\"CurrentZone\":\"duenenmeer\",\"Story\":{\"Chapter\":9,\"Step\":2},\"Mounts\":[\"Raptor\",\"Griffon\",\"Skyscale\"],\"MountPrefs\":{\"UseLast\":true,\"RadialSetsDefault\":false,\"Keys\":[\"Raptor=F6\"]}}",
        };

        static ProfileState Load(string json) { var st = new ProfileState(); st.LoadDto(JsonUtility.FromJson<SaveGameDto>(json), null, new List<SkillDefinition>()); return st; }

        [Test] public void Migration_AllHistoricalSaves_KeepLevelAndProgress()
        {
            int[] levels = { 14, 30, 30, 30 };
            for (int i = 0; i < Fixtures.Length; i++)
            {
                var src = JsonUtility.FromJson<SaveGameDto>(Fixtures[i]);
                Assert.IsTrue(SaveSystem.NeedsLevel80Migration(src), "Fixture " + i);
                var st = Load(Fixtures[i]);
                Assert.AreEqual(levels[i], st.Progression.Level, "Level bleibt (Fixture " + i + ")");
                Assert.AreEqual(src.Version, st.MigratedFromVersion);
                Assert.GreaterOrEqual(st.Progression.HeroPoints, ProgressionModel.HeroPointsForLevel(levels[i]));
                var back = st.ToDto();
                Assert.AreEqual(SaveGameDto.CurrentVersion, back.Version);
                Assert.IsFalse(SaveSystem.NeedsLevel80Migration(back));
                Assert.AreEqual(levels[i], Load(JsonUtility.ToJson(back)).Progression.Level, "zweiter Ladevorgang stabil");
            }
        }

        [Test] public void Migration_V1_ZoneChestAndCompletionKept()
        {
            var st = Load(Fixtures[0]);
            Assert.IsTrue(st.Completed.Contains("wp_sonnfurt")); Assert.IsTrue(st.Completed.Contains("zonechest:" + ProfileState.StartZoneId));
        }

        [Test] public void Migration_V2_MasteryKeptAndFrozenBelow80()
        {
            var st = Load(Fixtures[1]);
            Assert.AreEqual(2, st.Mastery.Rank(MasteryLine.Gleiten), "Beherrschung bleibt (Bestandsschutz)");
            Assert.IsTrue(st.Mounts.Contains("Raptor")); Assert.AreEqual("gluetoede", st.CurrentZone);
            Assert.Less(st.Progression.Level, AttributeSet.MaxLevel, "Beherrschungs-EP ruhen bis 80");
        }

        [Test] public void Migration_V3_ActiveEliteKept()
        {
            var dto = JsonUtility.FromJson<SaveGameDto>(Fixtures[2]);
            Assert.AreEqual("katalysator", dto.EliteSpec);
            var st = Load(Fixtures[2]);
            Assert.AreEqual(100, st.Progression.HeroPointsSpent, "ausgegebene Heldenpunkte bleiben");
            Assert.AreEqual("elementar", st.ClassId);
        }

        [Test] public void Migration_V4_StoryKeptGatesWait()
        {
            var st = Load(Fixtures[3]);
            Assert.AreEqual(9, st.Story.Chapter); Assert.AreEqual(2, st.Story.Step);
            Assert.Less(st.Progression.Level, LevelBands.ChapterGates[8], "Kapitel 9 wartet jetzt auf Stufe 70");
            Assert.IsTrue(st.MountPrefs.UseLast); Assert.AreEqual(3, st.Mounts.Count);
        }

        [Test] public void Migration_BackupWrittenOnceAndNeverOverwritten()
        {
            string dir = Path.Combine(Path.GetTempPath(), "pt_l11_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "character_0.json");
                File.WriteAllText(path, Fixtures[3]);
                string bak = SaveSystem.BackupBeforeMigration(path, 4);
                Assert.IsTrue(File.Exists(bak)); Assert.AreEqual(Fixtures[3], File.ReadAllText(bak));
                File.WriteAllText(path, "{}");
                Assert.AreEqual(bak, SaveSystem.BackupBeforeMigration(path, 4));
                Assert.AreEqual(Fixtures[3], File.ReadAllText(bak), "Sicherung bleibt unverändert");
                Assert.IsNull(SaveSystem.BackupBeforeMigration(Path.Combine(dir, "fehlt.json"), 4));
            }
            finally { foreach (var f in Directory.GetFiles(dir)) File.Delete(f); Directory.Delete(dir); }
        }

        [Test] public void Migration_ItemsKeepItemLevel()
        {
            // Speicherstände halten nur Item-Ids; das Item-Level steht in der Definition und bleibt – Werte rechnet ItemStats nach der neuen Kurve
            var d = ScriptableObject.CreateInstance<ItemDefinition>(); d.ItemLevel = 30; d.Rarity = ItemRarity.Exotic; d.Type = ItemType.Trinket; d.Trinket = TrinketKind.Amulet;
            Assert.AreEqual(30, d.ItemLevel);
            Assert.AreEqual(ItemStats.Budget(d), 0.12f * 300f * ItemStats.RarityMultiplier(ItemRarity.Exotic), 0.01f);
        }

        [Test] public void Migration_CurrentVersionIs5()
        {
            Assert.AreEqual(5, SaveGameDto.CurrentVersion); Assert.AreEqual(5, SaveGameDto.Level80Version);
            Assert.AreEqual(0, Load(JsonUtility.ToJson(new ProfileState().ToDto())).MigratedFromVersion);
        }
    }
}
