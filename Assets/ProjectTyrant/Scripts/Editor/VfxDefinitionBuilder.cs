using System.Collections.Generic;
using System.Linq;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Grafik-Lauf G5: VFX 2.0 für jeden Skill (Rebuild Everything) und Validator (fünf Phasen, Mindest-Lagen, gültige Kompositionen).</summary>
    public static class VfxDefinitionBuilder
    {
        public const string Dir = GraphicsBuilder.Dir + "/Vfx";
        public static readonly List<string> Report = new List<string>();

        public static IEnumerable<SkillDefinition> AllSkills() =>
            AssetDatabase.FindAssets("t:SkillDefinition", new[] { Root }).Select(g => AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(s => s != null);

        [MenuItem("Project Tyrant/Grafik G5/VFX 2.0 für alle Skills")]
        public static int BuildAll()
        {
            Report.Clear(); EnsureFolder(GraphicsBuilder.Dir); EnsureFolder(Dir);
            int n = 0; var tiers = new Dictionary<VfxTier, int>();
            foreach (var s in AllSkills())
            {
                string path = Dir + "/VFX_" + s.name + ".asset";
                var d = AssetDatabase.LoadAssetAtPath<VfxDefinition>(path);
                if (d == null) { d = ScriptableObject.CreateInstance<VfxDefinition>(); AssetDatabase.CreateAsset(d, path); }
                var tl = s.Timeline ?? new SkillTimeline();
                var facts = SkillInventoryV1.Facts(s, "", "");
                var prim = ProjectTyrant.Visuals.ElementSim.SimElements.Primary(facts);
                var kit = prim == ProjectTyrant.Visuals.ElementSim.SimElement.Wasser || s.SimSecondaryV1 == ProjectTyrant.Visuals.ElementSim.SimElement.Wasser ? ProjectTyrant.Visuals.ElementSim.SimElement.Wasser : ProjectTyrant.Visuals.ElementSim.SimElement.Keins;   // V2: Wasser; V3+ folgen
                VfxComposer.Fill(d, new VfxComposer.Input
                {
                    SkillId = string.IsNullOrEmpty(s.Id) ? s.name : s.Id, Palette = s.Palette, Delivery = s.Delivery, Slot = s.SlotType, Finisher = s.Finisher != ComboFinisherType.None,
                    Radius = s.Radius, Charge = tl.VfxCharge, Release = tl.VfxRelease, Projectile = tl.VfxProjectile, Impact = tl.VfxImpact, After = tl.VfxAfter,
                    KitElement = kit, Type = ProjectTyrant.Visuals.ElementSim.SimElements.TypeOf(facts), Showpiece = s.Showpiece
                });
                EditorUtility.SetDirty(d);
                if (s.Vfx2 != d) { s.Vfx2 = d; EditorUtility.SetDirty(s); }
                tiers[d.Tier] = tiers.TryGetValue(d.Tier, out int c) ? c + 1 : 1; n++;
            }
            AssetDatabase.SaveAssets();
            Report.Add(n + " Skills mit VFX 2.0 (" + string.Join(", ", tiers.Select(kv => kv.Key + " " + kv.Value)) + ")");
            var bad = Validate();
            Report.Add(bad.Count == 0 ? "Validator: 0 Verstöße" : "Validator: " + bad.Count + " Verstöße");
            Debug.Log("[ProjectTyrant] " + string.Join(" · ", Report));
            return n;
        }

        /// <summary>Verstöße „Skill: Problem“; leer = bestanden. Kein Skill ohne Effekte, fünf Phasen, Mindest-Lagen.</summary>
        public static List<string> Validate()
        {
            var bad = new List<string>();
            foreach (var s in AllSkills())
            {
                if (s.Vfx2 == null) { bad.Add(s.name + ": keine VFX-2.0-Definition"); continue; }
                foreach (var p in s.Vfx2.Problems()) bad.Add(s.name + ": " + p);
                foreach (var p in ProjectTyrant.Visuals.ElementSim.ElementKits.ProblemsV2(s.Vfx2, s.SimElementV1 == ProjectTyrant.Visuals.ElementSim.SimElement.Wasser || s.SimSecondaryV1 == ProjectTyrant.Visuals.ElementSim.SimElement.Wasser, s.Showpiece)) bad.Add(s.name + ": " + p);
            }
            return bad;
        }
    }
}
