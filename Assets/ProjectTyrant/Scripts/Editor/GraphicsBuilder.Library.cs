using System.Linq;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    public static partial class GraphicsBuilder
    {
        public const string LibraryPath = Dir + "/PT_GraphicsLibrary.asset";

        /// <summary>Bibliothek für die Laufzeit (am GameManager): Profile, Elemente, Lesezeichen, Texturen.</summary>
        public static GraphicsLibrary Library()
        {
            EnsureFolder(Dir);
            var lib = AssetDatabase.LoadAssetAtPath<GraphicsLibrary>(LibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<GraphicsLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
            lib.Profiles = ZoneBiomes.Select(z => Profile(z.zone)).Where(p => p != null).ToList();
            lib.Elements = System.Enum.GetValues(typeof(FxElement)).Cast<FxElement>().Select(Element).Where(e => e != null).ToList();
            lib.Bookmarks = AssetDatabase.FindAssets("t:CameraBookmark", new[] { BookmarkDir }).Select(g => AssetDatabase.LoadAssetAtPath<CameraBookmark>(AssetDatabase.GUIDToAssetPath(g))).Where(b => b != null).OrderBy(b => b.Key).ToList();
            lib.Noise = Tex("Noise_Simplex"); lib.Clouds = Tex("Clouds"); lib.Stars = Tex("Starfield"); lib.Caustics = Tex("Caustics"); lib.LeafCookie = Tex("Cookie_Leaves");
            lib.Ramp = Tex("Ramp_Toon"); lib.FlowMap = Tex("FlowMap"); lib.Worley = Tex("Noise_Worley");
            BuildAmbientVolumes();
            lib.ShowcaseSkills = ShowcaseSkills(out var showcaseClasses); lib.ShowcaseClasses = showcaseClasses;
            lib.Mounts = AssetDatabase.FindAssets("t:MountDefinition", new[] { Root + "/Data/Mounts" }).Select(g => AssetDatabase.LoadAssetAtPath<MountDefinition>(AssetDatabase.GUIDToAssetPath(g))).Where(m => m != null && m.Prefab != null).OrderBy(m => m.name).ToList();
            EditorUtility.SetDirty(lib);
            return lib;
        }

        /// <summary>Je Klasse: Skills 1–5 des ersten Waffensets, erste Heilung und erste Elite aus dem Pool.</summary>
        public static System.Collections.Generic.List<SkillDefinition> ShowcaseSkills(out System.Collections.Generic.List<string> classes)
        {
            var list = new System.Collections.Generic.List<SkillDefinition>();
            classes = new System.Collections.Generic.List<string>();
            var lib = AssetDatabase.LoadAssetAtPath<ClassLibrary>(Root + "/Data/Classes/ClassLibrary.asset");
            if (lib == null) return list;
            var seen = new System.Collections.Generic.HashSet<SkillDefinition>();
            foreach (var c in lib.Classes)
            {
                if (c == null) continue;
                var set = c.WeaponSets.FirstOrDefault(w => w != null);
                var pick = new System.Collections.Generic.List<SkillDefinition>();
                if (set != null) pick.AddRange(set.Skills.Where(sk => sk != null));
                pick.Add(c.SkillPool.FirstOrDefault(sk => sk != null && sk.SlotType == SkillSlotType.Heal));
                pick.Add(c.SkillPool.FirstOrDefault(sk => sk != null && sk.SlotType == SkillSlotType.Elite));
                foreach (var sk in pick) if (sk != null && seen.Add(sk)) { list.Add(sk); classes.Add(c.Id); }
            }
            return list;
        }
    }
}
