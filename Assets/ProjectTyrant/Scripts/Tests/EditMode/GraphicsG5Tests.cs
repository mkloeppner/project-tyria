using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using ProjectTyrant.Visuals;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Grafik-Lauf G5: VFX 2.0 – Zusammensetzung, Phasen, Mindest-Lagen, Validator, Licht-Kurve, Wirkungs-Auren.</summary>
    public class GraphicsG5Tests
    {
        static VfxDefinition Make(SkillDelivery del, SkillSlotType slot, bool finisher, float radius = 2f)
        {
            var d = ScriptableObject.CreateInstance<VfxDefinition>();
            VfxComposer.Fill(d, new VfxComposer.Input { SkillId = "t", Palette = VfxPalette.Feuer, Delivery = del, Slot = slot, Finisher = finisher, Radius = radius });
            return d;
        }

        [TestCase(SkillDelivery.Melee)] [TestCase(SkillDelivery.Projectile)] [TestCase(SkillDelivery.GroundTarget)] [TestCase(SkillDelivery.Self)] [TestCase(SkillDelivery.Leap)]
        public void Compose_AllFivePhasesFilled(SkillDelivery del)
        {
            var d = Make(del, SkillSlotType.Weapon, false);
            Assert.IsEmpty(d.Problems(), string.Join(", ", d.Problems()));
            foreach (VfxPhase p in System.Enum.GetValues(typeof(VfxPhase))) Assert.Greater(d.Get(p).Layers.Count, 0, p.ToString());
            Object.DestroyImmediate(d);
        }

        [Test] public void Compose_EliteHasAtLeastFive() { var d = Make(SkillDelivery.GroundTarget, SkillSlotType.Elite, false, 6f); Assert.AreEqual(VfxTier.Elite, d.Tier); Assert.GreaterOrEqual(d.LayerCount, 5); Object.DestroyImmediate(d); }
        [Test] public void Compose_FinisherHasAtLeastSix() { var d = Make(SkillDelivery.Melee, SkillSlotType.Weapon, true); Assert.AreEqual(VfxTier.Finisher, d.Tier); Assert.GreaterOrEqual(d.LayerCount, 6); Object.DestroyImmediate(d); }

        [Test]
        public void Compose_KeepsLegacyTimelineIds()
        {
            var d = ScriptableObject.CreateInstance<VfxDefinition>();
            VfxComposer.Fill(d, new VfxComposer.Input { Palette = VfxPalette.Frost, Delivery = SkillDelivery.Projectile, Charge = "Charge/Frost/S", Impact = "Impact/Frost/M" });
            Assert.AreEqual("Charge/Frost/S", d.Get(VfxPhase.Aufladen).Layers[0].Id);
            Assert.AreEqual("Impact/Frost/M", d.Get(VfxPhase.Einschlag).Layers[0].Id);
            Assert.AreEqual(FxElement.Eis, d.Element);
            Object.DestroyImmediate(d);
        }

        [Test]
        public void Compose_MeleeHasWeaponTrail_FinisherHasCameraKick()
        {
            var d = Make(SkillDelivery.Melee, SkillSlotType.Weapon, true);
            Assert.IsTrue(d.Get(VfxPhase.Auswurf).Layers.Any(l => l.Kind == VfxLayerKind.Trail));
            Assert.IsTrue(d.Get(VfxPhase.Einschlag).Layers.Any(l => l.Kind == VfxLayerKind.CameraKick));
            Object.DestroyImmediate(d);
        }

        [Test]
        public void Compose_LightsNeverInDangerColor()
        {
            // Paletten nahe der Gefahren-Farbe werden für Licht aufgehellt (Gefahr bleibt dem Telegrafen vorbehalten)
            foreach (VfxPalette p in System.Enum.GetValues(typeof(VfxPalette)))
            {
                var orig = SkillFx.PaletteColor(p); var c = VfxComposer.SkillFxColor(p);
                if (DangerLanguage.Conflicts(orig)) Assert.Greater(c.grayscale, orig.grayscale, p.ToString());
                else Assert.AreEqual(orig, c, p.ToString());
            }
        }

        [Test]
        public void Problems_EmptyDefinitionFails()
        {
            var d = ScriptableObject.CreateInstance<VfxDefinition>();
            Assert.GreaterOrEqual(d.Problems().Count, 5);
            Object.DestroyImmediate(d);
        }

        [Test]
        public void AllSkills_HaveValidVfx2()
        {
            var bad = VfxDefinitionBuilder.Validate();
            Assert.IsEmpty(bad, string.Join("\n", bad.Take(10)));
            Assert.Greater(VfxDefinitionBuilder.AllSkills().Count(), 400);
        }

        [Test]
        public void LightCurve_RisesFastThenDecays()
        {
            Assert.AreEqual(0f, VfxLayerPlayer.LightCurve(0f), 1e-5f);
            Assert.AreEqual(1f, VfxLayerPlayer.LightCurve(0.1f), 1e-5f);
            Assert.Less(VfxLayerPlayer.LightCurve(0.6f), VfxLayerPlayer.LightCurve(0.3f));
            Assert.AreEqual(0f, VfxLayerPlayer.LightCurve(1f), 1e-5f);
        }

        [Test]
        public void Aura_ConditionBeatsBoon()
        {
            var active = new List<EffectType> { EffectType.Might, EffectType.Poison };
            Assert.AreEqual(EffectType.Poison, EffectAuraFx.Dominant(active, t => t == EffectType.Might ? 25 : 1));
        }

        [Test]
        public void Aura_AllDamagingConditionsVisible()
        {
            foreach (var t in new[] { EffectType.Burning, EffectType.Bleeding, EffectType.Poison, EffectType.Torment, EffectType.Confusion, EffectType.Chill, EffectType.Fear, EffectType.Blind })
                Assert.IsNotNull(EffectAuraFx.LookOf(t), t.ToString());
        }
    }
}
