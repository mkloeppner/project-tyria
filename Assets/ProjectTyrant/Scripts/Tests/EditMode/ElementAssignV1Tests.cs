using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectTyrant.Visuals.ElementSim;

namespace ProjectTyrant.Tests
{
    /// <summary>VFX-Lauf V1: Element-Zuordnung, Leuchtturm-Anteile, Schaustück-Auswahl.</summary>
    public class ElementAssignV1Tests
    {
        static SkillFacts F(string id, string name, VfxPalette p, string cls = "waechter", SkillDelivery d = SkillDelivery.Melee, SkillSlotType slot = SkillSlotType.Weapon, ComboFieldType field = ComboFieldType.None, bool fin = false, string elite = "", float dur = 0.5f, float r = 2f) =>
            new SkillFacts { Id = id, Name = name, Palette = p, ClassId = cls, Delivery = d, Slot = slot, Field = field, Finisher = fin, EliteSpec = elite, Duration = dur, Radius = r };

        [Test] public void Primary_ComboFieldWins() => Assert.AreEqual(SimElement.Feuer, SimElements.Primary(F("a", "Hieb", VfxPalette.Wasser, field: ComboFieldType.Fire)));
        [Test] public void Primary_NameBeforePalette() => Assert.AreEqual(SimElement.Wasser, SimElements.Primary(F("a", "Flutwelle", VfxPalette.Licht)));
        [Test] public void Primary_WaterPaletteIsWater_NotIce() => Assert.AreEqual(SimElement.Wasser, SimElements.Primary(F("a", "Schlag", VfxPalette.Wasser)));

        [TestCase(VfxPalette.Frost, SimElement.Brechung)] [TestCase(VfxPalette.Licht, SimElement.Brechung)] [TestCase(VfxPalette.Lava, SimElement.Feuer)]
        [TestCase(VfxPalette.Gewitter, SimElement.Blitz)] [TestCase(VfxPalette.Jade, SimElement.Jade)] [TestCase(VfxPalette.Arkan, SimElement.Arkan)]
        public void Primary_PaletteFallback(VfxPalette p, SimElement e) => Assert.AreEqual(e, SimElements.Primary(F("a", "Hieb", p)));

        [Test] public void Primary_ArcaneLensIsRefraction() => Assert.AreEqual(SimElement.Brechung, SimElements.Primary(F("a", "Arkane Linse", VfxPalette.Arkan)));

        [TestCase(SkillDelivery.Projectile, FxSkillType.Projektil)] [TestCase(SkillDelivery.GroundTarget, FxSkillType.Flaeche)] [TestCase(SkillDelivery.Leap, FxSkillType.Sprung)] [TestCase(SkillDelivery.Melee, FxSkillType.Nahkampf)]
        public void TypeOf_FromDelivery(SkillDelivery d, FxSkillType t) => Assert.AreEqual(t, SimElements.TypeOf(F("a", "x", VfxPalette.Licht, d: d)));

        [Test]
        public void Secondaries_RaiseLighthousesToTargetShare_WithoutChangingPrimary()
        {
            var skills = Enumerable.Range(0, 50).Select(i => F("s" + i, i % 10 == 0 ? "Glutschlag" : "Hieb " + i, VfxPalette.Natur)).ToList();
            var prim = skills.ToDictionary(s => s.Id, SimElements.Primary);
            var sec = SimElements.Secondaries(skills, prim);
            Assert.GreaterOrEqual(SimElements.Share(skills, prim, sec, SimElement.Wasser), 5);
            Assert.GreaterOrEqual(SimElements.Share(skills, prim, sec, SimElement.Feuer), 5);
            Assert.GreaterOrEqual(SimElements.Share(skills, prim, sec, SimElement.Brechung), 6);
            foreach (var kv in sec) Assert.AreNotEqual(prim[kv.Key], kv.Value);
            Assert.AreEqual(prim, skills.ToDictionary(s => s.Id, SimElements.Primary), "Hauptelement darf sich nicht ändern");
        }

        [Test]
        public void Showpieces_ThreePerClass_OnePerElite_AllElitesAndFinishers()
        {
            var skills = new List<SkillFacts>
            {
                F("w1", "Feuerball", VfxPalette.Feuer, d: SkillDelivery.Projectile, dur: 1f), F("w2", "Flutfeld", VfxPalette.Wasser, d: SkillDelivery.GroundTarget, dur: 4f),
                F("w3", "Sprung", VfxPalette.Licht, d: SkillDelivery.Leap), F("w4", "Hieb", VfxPalette.Licht), F("w5", "Hieb 2", VfxPalette.Licht, fin: true),
                F("w6", "Avatar", VfxPalette.Licht, slot: SkillSlotType.Elite),
                F("e1", "Elite-Hieb", VfxPalette.Licht, elite: "spec_a"), F("e2", "Elite-Feld", VfxPalette.Licht, elite: "spec_a", d: SkillDelivery.GroundTarget, dur: 5f),
            };
            var prim = skills.ToDictionary(s => s.Id, SimElements.Primary);
            var sp = SimElements.Showpieces(skills, prim);
            Assert.IsTrue(sp.Contains("w5") && sp.Contains("w6"), "Elite und Finisher immer");
            Assert.IsTrue(sp.Contains("e2"), "bester Skill der Elite-Spezialisierung");
            Assert.AreEqual(3, skills.Count(s => s.EliteSpec == "" && s.Slot != SkillSlotType.Elite && !s.Finisher && sp.Contains(s.Id)), "drei Klassen-Schaustücke");
            Assert.IsTrue(sp.Contains("w1") && sp.Contains("w2") && sp.Contains("w3"), "verschiedene Typen vor gleichartigen");
        }

        [Test] public void MinLayers_V1() { Assert.AreEqual(7, SimElements.MinLayersV1(true)); Assert.AreEqual(4, SimElements.MinLayersV1(false)); }
    }
}
