using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 5 / M23: VFX-Baukasten – Komposition → Prefab, Partikelbudget, Paletten-Vollständigkeit, jeder Skill hat VFX, Lesbarkeits-Regeln.</summary>
    public class VfxKitTests
    {
        static VfxCatalog Cat { get { var c = VfxKitBuilder.Catalog; Assert.IsNotNull(c, "VfxCatalog fehlt (Rebuild)"); return c; } }

        [Test]
        public void EveryComposition_HasPrefabWithInstanceAndRenderer()
        {
            var cat = Cat; var missing = new List<string>();
            foreach (VfxPalette p in System.Enum.GetValues(typeof(VfxPalette)))
                foreach (VfxShape s in System.Enum.GetValues(typeof(VfxShape)))
                    foreach (VfxSize z in System.Enum.GetValues(typeof(VfxSize)))
                    {
                        var e = cat.Get(VfxIds.Compose(s, p, z));
                        if (e == null || e.Prefab == null) { missing.Add(VfxIds.Compose(s, p, z)); continue; }
                        if (e.Prefab.GetComponent<VfxInstance>() == null) missing.Add(e.Key + " ohne VfxInstance");
                        if (e.Prefab.GetComponentsInChildren<Renderer>(true).Length == 0) missing.Add(e.Key + " ohne Renderer");
                    }
            Assert.AreEqual(13 * 11 * 4, cat.Entries.Count);
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        [Test]
        public void ParticleBudget_PerComposition_AndDurationRule()
        {
            var over = new List<string>();
            foreach (var e in Cat.Entries)
            {
                VfxIds.TryParse(e.Key, out var shape, out _, out var size);
                if (e.MaxParticles > VfxCatalog.Budget(size)) over.Add(e.Key + " " + e.MaxParticles + " > " + VfxCatalog.Budget(size));
                if (!e.Loop && e.Duration > 3f) over.Add(e.Key + " dauert " + e.Duration + " s");
            }
            Assert.IsEmpty(over, string.Join("\n", over));
            // Globales Budget: 60 gleichzeitige mittlere Effekte passen unter 20.000
            Assert.LessOrEqual(60 * VfxCatalog.Budget(VfxSize.M), VfxPool.ParticleBudget);
        }

        [Test]
        public void Palettes_Complete_AndDistinct()
        {
            var cat = Cat;
            Assert.AreEqual(11, cat.Palettes.Count);
            var seen = new HashSet<VfxPalette>();
            foreach (var p in cat.Palettes) { Assert.IsTrue(seen.Add(p.Id)); Assert.IsFalse(string.IsNullOrEmpty(p.SoundFamily)); Assert.Greater(p.A.maxColorComponent, 0.05f); }
            foreach (VfxPalette p in System.Enum.GetValues(typeof(VfxPalette))) Assert.IsTrue(seen.Contains(p), p.ToString());
            Assert.IsTrue(cat.Palette(VfxPalette.Luft).Lightning, "Luft mit Blitzen");
            Assert.IsTrue(cat.Palette(VfxPalette.Schatten).Alpha, "Schatten als Alpha-Rauch (additiv wäre unsichtbar)");
        }

        [Test]
        public void EverySkill_HasResolvableVfxForEachPhase()
        {
            var cat = Cat; var errors = new List<string>();
            foreach (var g in AssetDatabase.FindAssets("t:SkillDefinition", new[] { PTEditorUtil.DataDir }))
            {
                var s = AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g));
                var t = s.Timeline;
                foreach (var id in new[] { t.VfxCharge, t.VfxRelease, t.VfxProjectile, t.VfxImpact, t.VfxAfter })
                    if (!string.IsNullOrEmpty(id) && cat.Get(id) == null) errors.Add(s.Id + ": " + id);
                if (string.IsNullOrEmpty(t.VfxRelease) || string.IsNullOrEmpty(t.VfxImpact)) errors.Add(s.Id + ": Auswurf/Einschlag fehlt");
            }
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void Readability_PlayerQuieterThanEnemy_AlliesReduced40Percent_DensityScales()
        {
            Assert.Less(SkillFx.TeamIntensity(Team.Player), SkillFx.TeamIntensity(Team.Enemy));
            Assert.AreEqual(0.6f, SkillFx.TeamIntensity(Team.Ally), 1e-4f);
            var old = VfxPool.EffectDensity;
            VfxPool.EffectDensity = VfxPool.Density.Voll; float full = VfxPool.DensityFactor;
            VfxPool.EffectDensity = VfxPool.Density.Reduziert; float red = VfxPool.DensityFactor;
            VfxPool.EffectDensity = VfxPool.Density.Minimal; float min = VfxPool.DensityFactor;
            VfxPool.EffectDensity = old;
            Assert.Greater(full, red); Assert.Greater(red, min); Assert.Greater(min, 0f);
        }

        [Test]
        public void ConditionTints_Priority_AndEffectPalettes()
        {
            var fx = new EffectModel();
            Assert.AreEqual(Color.white, HitFlash.TintFor(fx));
            fx.Apply(EffectType.Bleeding, 1, 5f, null); Assert.AreEqual(HitFlash.TintColors[EffectType.Bleeding], HitFlash.TintFor(fx));
            fx.Apply(EffectType.Burning, 1, 5f, null); Assert.AreEqual(HitFlash.TintColors[EffectType.Burning], HitFlash.TintFor(fx), "Brennen hat Vorrang");
            foreach (var t in WorldFx.BodyConditions) Assert.IsTrue(WorldFx.EffectPalette.ContainsKey(t), t.ToString());
        }

        [Test]
        public void VfxIds_RoundTrip()
        {
            var id = VfxIds.Compose(VfxShape.Shockwave, VfxPalette.Frost, VfxSize.Elite);
            Assert.IsTrue(VfxIds.TryParse(id, out var s, out var p, out var z));
            Assert.AreEqual(VfxShape.Shockwave, s); Assert.AreEqual(VfxPalette.Frost, p); Assert.AreEqual(VfxSize.Elite, z);
            Assert.IsFalse(VfxIds.TryParse("Unsinn", out _, out _, out _));
        }
    
        [Test]
        public void AdditiveShaders_DoNotSquareAlpha_AndRuntimeParticlesAreSoft()
        {
            // Blend SrcAlpha One gewichtet bereits mit Alpha – eine zweite Multiplikation machte die Effekte im Spiel fast unsichtbar (QA M23).
            foreach (var path in new[] { "Assets/ProjectTyrant/Shaders/FX/PT_FxParticle.shader", "Assets/ProjectTyrant/Shaders/FX/PT_FxMesh.shader" })
            {
                var src = System.IO.File.ReadAllText(path);
                StringAssert.DoesNotContain("_Intensity * c.a", src, path);
            }
            var lib = AssetDatabase.LoadAssetAtPath<VfxLibrary>("Assets/ProjectTyrant/Data/VfxLibrary.asset");
            Assert.IsNotNull(lib);
            Assert.IsNotNull(lib.ParticleGlow, "Weiche Laufzeit-Partikel fehlen (sonst quadratische Partikel)");
            Assert.IsNotNull(lib.ParticleDrop);
            Assert.AreEqual("ProjectTyrant/FX/Particle", lib.ParticleGlow.shader.name);
            Assert.IsNotNull(lib.ParticleGlow.GetTexture("_MainTex"));
        }

        [Test]
        public void AuraDome_IsFlatColored_SoItStaysVisible()
        {
            foreach (VfxPalette p in System.Enum.GetValues(typeof(VfxPalette)))
            {
                var e = Cat.Get(VfxIds.Compose(VfxShape.Aura, p, VfxSize.M)); Assert.IsNotNull(e, p.ToString());
                var dome = e.Prefab.transform.Find("Dome"); Assert.IsNotNull(dome, p + ": Dome fehlt");
                var m = dome.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.AreEqual(Vector2.zero, m.GetTextureScale("_MainTex"), p + ": Dome-Textur muss flach (Skalierung 0) sein");
                var sc = m.GetVector("_Scroll"); Assert.AreEqual(0f, sc.x, 1e-4f); Assert.AreEqual(0f, sc.y, 1e-4f);
            }
        }
    }
}
