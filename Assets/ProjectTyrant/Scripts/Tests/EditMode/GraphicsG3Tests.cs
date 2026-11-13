using System.Linq;
using NUnit.Framework;
using ProjectTyrant.EditorTools;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Grafik-Lauf G3: Shader-Bibliothek 2.0, Familien-Zuordnung, Migration.</summary>
    public class GraphicsG3Tests
    {
        [TestCase("M_Terrain", PTShaders.Terrain)]
        [TestCase("M_Vegetation_Wind", PTShaders.Vegetation)]
        [TestCase("M_W1_nebelmoor_nebelmoor_wz_1_leaf", PTShaders.Vegetation)]
        [TestCase("M_BannerCloth", PTShaders.Cloth)]
        [TestCase("M_W1_Hazard_Lava", PTShaders.Hazard)]
        [TestCase("M_W1_HazardEdge_ToxicWater", PTShaders.Hazard)]
        [TestCase("M_Orb_Lava", PTShaders.ToonLit)]
        [TestCase("M_WaypointCrystal", PTShaders.Crystal)]
        [TestCase("M_W1_frostkamm_frostkamm_wz_1_glass", PTShaders.Glass)]
        [TestCase("M_ScoutSpyglass", PTShaders.ToonLit)]
        [TestCase("M_Character_Skin", PTShaders.ToonLit)]
        public void FamilyShaderFor_MapsByName(string material, string expected) => Assert.AreEqual(expected, MaterialMigrator.FamilyShaderFor(material));

        [Test]
        public void Library_AllShadersPassValidator()
        {
            var all = ShaderLibraryValidator.ValidateAll();
            var bad = all.Where(r => r.Problems.Count > 0).Select(r => r.Shader + ": " + string.Join(", ", r.Problems)).ToList();
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void Library_VariantSumWithinBudget()
        {
            var all = ShaderLibraryValidator.ValidateAll();
            if (all.All(r => r.Variants < 0)) Assert.Inconclusive("Varianten-API nicht verfügbar");
            long sum = all.Where(r => r.Variants > 0).Sum(r => r.Variants);
            Assert.LessOrEqual(sum, ShaderLibraryValidator.MaxVariants, "nach Stripping-Regeln (G7) gezählt: Editor-Zahl ist die obere Schranke");
        }

        [Test]
        public void Library_SrpBatcherCompatible()
        {
            foreach (var name in PTShaders.Library)
            {
                int code = ShaderLibraryValidator.SrpBatcherCode(Shader.Find(name));
                if (code < 0) Assert.Inconclusive("SRP-Batcher-API nicht verfügbar");
                Assert.AreEqual(0, code, name);
            }
        }

        [Test]
        public void Migration_LavaLakesUseLavaShader()
        {
            foreach (var z in new[] { "gluetoede", "aschenkessel", "glutkessel" })
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(PTEditorUtil.MatDir + "/M_FX_Water_" + z + ".mat");
                if (m == null) continue;
                Assert.AreEqual(PTShaders.Lava, m.shader.name, z);
                Assert.Greater(m.GetColor("_GlowColor").maxColorComponent, 1f, z + ": HDR-Glut für Bloom");
            }
        }

        [Test]
        public void Migration_FamilyMaterialsHaveNoiseTextures()
        {
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { PTEditorUtil.Root }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                if (m == null || m.shader == null) continue;
                if (m.shader.name == PTShaders.Lava) Assert.IsNotNull(m.GetTexture("_CrackTex"), m.name);
                if (m.shader.name == PTShaders.Hazard) Assert.IsNotNull(m.GetTexture("_PatternTex"), m.name);
                if (m.shader.name == PTShaders.Water2) Assert.IsNotNull(m.GetTexture("_CausticTex"), m.name);
                if (m.shader.name == PTShaders.Terrain) Assert.IsNotNull(m.GetTexture("_VariationTex"), m.name);
            }
        }

        [Test]
        public void Validator_FlagsMissingCbuffer()
        {
            var r = ShaderLibraryValidator.Validate("Hidden/Does/Not/Exist");
            Assert.Contains("Shader fehlt", r.Problems);
        }
    }
}
