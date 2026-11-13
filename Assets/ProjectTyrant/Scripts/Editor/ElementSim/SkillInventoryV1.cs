using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjectTyrant.Visuals;
using ProjectTyrant.Visuals.ElementSim;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>VFX-Lauf V1: Skill-Inventar, Element-Zuordnung (Haupt + Sekundär), Schaustück-Markierung. Teil von Rebuild Everything.</summary>
    public static class SkillInventoryV1
    {
        public const string CsvPath = "Temp/PT_QA/V1/inventar.csv";
        public static string LastSummary = "";

        public static SkillFacts Facts(SkillDefinition s, string classId, string elite)
        {
            float dur = s.CastTime + Mathf.Max(0, s.Hits - 1) * s.HitInterval + (s.Field != ComboFieldType.None ? s.FieldDuration : 0f);
            return new SkillFacts
            {
                Id = s.Id, Name = s.DisplayName, ClassId = classId, EliteSpec = elite, Palette = s.Palette, Field = s.Field, Delivery = s.Delivery, Slot = s.SlotType,
                Finisher = s.Finisher != ComboFinisherType.None, Whirl = s.Finisher == ComboFinisherType.Whirl, Heals = s.HealBase > 0f || s.HealCoefficient > 0f,
                Summons = s.Special == SkillSpecial.Conjure || s.Special == SkillSpecial.PetCommand, Blocks = false, Chain = false,
                Duration = dur, Radius = s.Delivery == SkillDelivery.Melee ? 0f : s.Radius, Usage = 0f
            };
        }

        /// <summary>Alle Skills mit Klasse und Elite-Spezialisierung (erste Fundstelle gewinnt); übrige Skills (Gegner, Reittiere) ohne Klasse.</summary>
        public static List<(SkillDefinition def, SkillFacts facts)> Collect()
        {
            var list = new List<(SkillDefinition, SkillFacts)>(); var seen = new HashSet<SkillDefinition>();
            void Add(SkillDefinition s, string c, string e) { if (s != null && !string.IsNullOrEmpty(s.Id) && seen.Add(s)) list.Add((s, Facts(s, c, e))); }
            var lib = AssetDatabase.LoadAssetAtPath<ClassLibrary>(Root + "/Data/Classes/ClassLibrary.asset");
            if (lib != null)
                foreach (var c in lib.Classes.Where(c => c != null))
                {
                    foreach (var es in c.EliteSpecs.Where(e => e != null))
                    {
                        if (es.Weapon != null) foreach (var s in es.Weapon.Skills) Add(s, c.Id, es.name);
                        foreach (var s in es.TypeSkills) Add(s, c.Id, es.name);
                        foreach (var s in es.MechanicSkills) Add(s, c.Id, es.name);
                    }
                    foreach (var w in c.WeaponSets.Where(w => w != null)) { foreach (var s in w.Skills) Add(s, c.Id, ""); foreach (var s in w.Attunement) Add(s, c.Id, ""); Add(w.StealthSkill, c.Id, ""); }
                    foreach (var s in c.SkillPool) Add(s, c.Id, "");
                    foreach (var s in c.ExtraSkills) Add(s, c.Id, "");
                }
            foreach (var s in VfxDefinitionBuilder.AllSkills().OrderBy(s => s.Id)) Add(s, "", "");
            return list;
        }

        [MenuItem("Project Tyrant/VFX V1/Skill-Inventar und Schaustücke")]
        public static void Menu() => Debug.Log("[ProjectTyrant] V1 Inventar: " + Build());

        public static string Build()
        {
            var all = Collect(); var facts = all.Select(a => a.facts).ToList();
            var primary = facts.ToDictionary(f => f.Id, SimElements.Primary);
            var sec = SimElements.Secondaries(facts, primary);
            var show = SimElements.Showpieces(facts, primary);
            var sb = new StringBuilder("id;klasse;elite;element;sekundaer;typ;dauer;radius;lagen;phasen;schaustueck;nutzung\n");
            foreach (var (def, f) in all)
            {
                var e = primary[f.Id]; var s2 = sec.TryGetValue(f.Id, out var x) ? x : SimElement.Keins; bool sp = show.Contains(f.Id);
                if (def.SimElementV1 != e || def.SimSecondaryV1 != s2 || def.Showpiece != sp) { def.SimElementV1 = e; def.SimSecondaryV1 = s2; def.Showpiece = sp; EditorUtility.SetDirty(def); }
                int layers = def.Vfx2 != null ? def.Vfx2.LayerCount : 0, phases = def.Vfx2 != null ? def.Vfx2.Phases.Count(p => p.Layers.Count > 0) : 0;
                sb.Append(string.Join(";", f.Id, f.ClassId, f.EliteSpec, e, s2, SimElements.TypeOf(f), f.Duration.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    f.Radius.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture), layers, phases, sp ? 1 : 0, "0")).Append('\n');
            }
            Directory.CreateDirectory(Path.GetDirectoryName(CsvPath)); File.WriteAllText(CsvPath, sb.ToString());
            AssetDatabase.SaveAssets();
            int n = facts.Count;
            string Pct(SimElement el) => (100f * SimElements.Share(facts, primary, sec, el) / Mathf.Max(1, n)).ToString("0.0") + " %";
            LastSummary = n + " Skills, Schaustücke " + show.Count + ", Wasser " + Pct(SimElement.Wasser) + ", Feuer " + Pct(SimElement.Feuer) + ", Brechung " + Pct(SimElement.Brechung) + ", Sekundär " + sec.Count;
            return LastSummary;
        }
    }
}
