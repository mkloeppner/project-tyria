using System.Linq;
using NUnit.Framework;
using ProjectTyrant.Visuals;
using ProjectTyrant.Visuals.ElementSim;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>VFX-Lauf V2: Wasser-Baukasten, Lagen-Prüfung, Form-Animationen, Bibliothek.</summary>
    public class WaterKitV2Tests
    {
        static VfxDefinition Compose(FxSkillType type, bool showpiece, SimElement kit = SimElement.Wasser, SkillDelivery d = SkillDelivery.Projectile)
        {
            var def = ScriptableObject.CreateInstance<VfxDefinition>();
            VfxComposer.Fill(def, new VfxComposer.Input { SkillId = "t", Palette = VfxPalette.Wasser, Delivery = d, Slot = SkillSlotType.Weapon, Radius = 3f, KitElement = kit, Type = type, Showpiece = showpiece });
            return def;
        }

        static readonly FxSkillType[] Types = (FxSkillType[])System.Enum.GetValues(typeof(FxSkillType));

        [Test]
        public void EveryType_PassesG5AndV2Rules([Values(false, true)] bool showpiece)
        {
            foreach (var t in Types)
            {
                var d = Compose(t, showpiece);
                CollectionAssert.IsEmpty(d.Problems(), t + " G5");
                CollectionAssert.IsEmpty(ElementKits.ProblemsV2(d, true, showpiece), t + " V2");
                Assert.GreaterOrEqual(d.LayerCount, showpiece ? 7 : 4, t.ToString());
            }
        }

        [Test]
        public void NoKit_KeepsPureG5Layers()
        {
            var d = Compose(FxSkillType.Projektil, false, SimElement.Keins);
            Assert.IsFalse(d.Phases.SelectMany(p => p.Layers).Any(l => ElementKits.IsElementLayer(l.Kind)));
            var w = Compose(FxSkillType.Projektil, false);
            Assert.AreEqual(5, w.Phases.Count, "Phasen-Struktur bleibt");
            Assert.Greater(w.LayerCount, d.LayerCount);
        }

        [Test]
        public void ElementWithoutKit_AddsNothing()
        {
            var add = ElementKits.Layers(new ElementKits.KitInput { Element = SimElement.Sand, Type = FxSkillType.Flaeche });
            Assert.AreEqual(0, ElementKits.Count(add));
        }

        [Test]
        public void Water_ProjectileFollowsElementPhysics()
        {
            var l = ElementKits.Layers(new ElementKits.KitInput { Element = SimElement.Wasser, Type = FxSkillType.Projektil, Radius = 2f });
            Assert.IsTrue(l[VfxPhase.Aufladen].Any(x => x.Id == "wasser/sammeln"), "Tropfen sammeln sich");
            Assert.IsTrue(l[VfxPhase.Flug].Any(x => x.Kind == VfxLayerKind.Refract), "Linse bricht im Flug");
            Assert.IsTrue(l[VfxPhase.Einschlag].Any(x => x.Id == "wasser/SplashCrown"), "Spritzkrone");
            Assert.IsTrue(l[VfxPhase.Nachwirkung].Any(x => x.Kind == VfxLayerKind.WorldMark && x.Id == "wasser/pfuetze"), "Pfütze");
        }

        [Test]
        public void Water_AreaHasCausticAndWetGround()
        {
            var l = ElementKits.Layers(new ElementKits.KitInput { Element = SimElement.Wasser, Type = FxSkillType.Flaeche, Radius = 4f });
            Assert.IsTrue(l[VfxPhase.Aufladen].Any(x => x.Id == "wasser/nass"));
            Assert.IsTrue(l[VfxPhase.Einschlag].Any(x => x.Id == "wasser/wasser_kaustik_loop"));
            Assert.AreEqual(4f, l[VfxPhase.Einschlag].First(x => x.Id == "wasser/WaveRing").Radius, 1e-4f, "Wellenring auf Flächenradius");
        }

        [Test]
        public void Water_AuraRefractionIsWeak_ShowpieceRefractionIsStrong()
        {
            var aura = ElementKits.Layers(new ElementKits.KitInput { Element = SimElement.Wasser, Type = FxSkillType.Aura });
            Assert.Less(aura[VfxPhase.Aufladen].First(x => x.Kind == VfxLayerKind.Refract).Strength, 0.5f);
            var show = ElementKits.Layers(new ElementKits.KitInput { Element = SimElement.Wasser, Type = FxSkillType.Aura, Showpiece = true });
            Assert.AreEqual(1f, show[VfxPhase.Einschlag].First(x => x.Kind == VfxLayerKind.Refract).Strength, "Auftraggeber: Brechung stark");
        }

        [Test]
        public void Water_ShowpieceLightStaysUnderValidatorCap()
        {
            var show = ElementKits.Layers(new ElementKits.KitInput { Element = SimElement.Wasser, Type = FxSkillType.Projektil, Showpiece = true });
            foreach (var kv in show) foreach (var l in kv.Value) if (l.Kind == VfxLayerKind.Light) Assert.LessOrEqual(l.Strength, 6f);
        }

        [Test]
        public void ProblemsV2_DetectsMissingSimAndWorldContact()
        {
            var d = Compose(FxSkillType.Projektil, false, SimElement.Keins);
            var p = ElementKits.ProblemsV2(d, true, false);
            Assert.IsTrue(p.Any(x => x.Contains("Simulations-Flipbook"))); Assert.IsTrue(p.Any(x => x.Contains("Welt-Kontakt")));
            CollectionAssert.IsEmpty(ElementKits.ProblemsV2(d, false, false), "nicht umgesetzte Elemente werden noch nicht geprüft");
        }

        [Test]
        public void Ids_SplitElementAndContent()
        {
            Assert.AreEqual("wasser", ElementKits.ElementOf("wasser/SplashCrown")); Assert.AreEqual("SplashCrown", ElementKits.ContentOf("wasser/SplashCrown"));
            Assert.AreEqual("", ElementKits.ElementOf("x")); Assert.AreEqual("x", ElementKits.ContentOf("x"));
        }

        [Test]
        public void Anim_FadeBounds()
        {
            Assert.AreEqual(0f, ElementAnim.Fade(0f)); Assert.AreEqual(1f, ElementAnim.Fade(0.3f)); Assert.AreEqual(0f, ElementAnim.Fade(1f), 1e-5f);
            for (float t = 0f; t <= 1f; t += 0.05f) Assert.That(ElementAnim.Fade(t), Is.InRange(0f, 1f));
        }

        [Test]
        public void Anim_CrownRisesThenFalls_RingExpands_PuddleKeepsSize()
        {
            float y0 = ElementAnim.Scale(VfxLayerKind.FxMesh, "SplashCrown", 1f, 0f).y, yPeak = ElementAnim.Scale(VfxLayerKind.FxMesh, "SplashCrown", 1f, 0.35f).y, yEnd = ElementAnim.Scale(VfxLayerKind.FxMesh, "SplashCrown", 1f, 1f).y;
            Assert.Greater(yPeak, y0); Assert.Less(yEnd, yPeak);
            float prev = 0f;
            for (float t = 0f; t <= 1f; t += 0.1f) { float s = ElementAnim.Scale(VfxLayerKind.FxMesh, "WaveRing", 2f, t).x; Assert.GreaterOrEqual(s, prev); prev = s; }
            Assert.AreEqual(2f, ElementAnim.Scale(VfxLayerKind.FxMesh, "WaveRing", 2f, 1f).x, 1e-3f, "Ring endet am Radius");
            Assert.AreEqual(4f, ElementAnim.Scale(VfxLayerKind.WorldMark, "pfuetze", 2f, 0.9f).x, 1e-3f, "Pfütze = Durchmesser, Trocknen im Shader");
        }

        [Test]
        public void Library_MaterialFallbackToGeneric()
        {
            var lib = ScriptableObject.CreateInstance<ElementFxLibrary>();
            var a = new Material(Shader.Find("ProjectTyrant/FX/Refract")); var b = new Material(Shader.Find("ProjectTyrant/FX/Refract"));
            lib.Materials.Add(new ElementFxLibrary.MaterialEntry { Element = "", Key = "Refract", Material = a });
            lib.Materials.Add(new ElementFxLibrary.MaterialEntry { Element = "wasser", Key = "Refract", Material = b });
            Assert.AreSame(b, lib.Mat("wasser", "Refract")); Assert.AreSame(a, lib.Mat("feuer", "Refract")); Assert.IsNull(lib.Mat("feuer", "Mesh"));
            Object.DestroyImmediate(a); Object.DestroyImmediate(b); Object.DestroyImmediate(lib);
        }

        [Test]
        public void ElementMeshShader_Compiles() { var s = Shader.Find("ProjectTyrant/FX/ElementMesh"); Assert.IsNotNull(s); Assert.IsFalse(UnityEditor.ShaderUtil.ShaderHasError(s)); }
    }
}
