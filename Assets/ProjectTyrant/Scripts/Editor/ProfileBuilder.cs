using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    public static partial class SceneBuilder
    {
        static partial void ConfigureProfileImpl(PlayerProfile profile)
        {
            profile.Items = AssetDatabase.LoadAssetAtPath<ItemDatabase>(PTEditorUtil.DataDir + "/Items/ItemDatabase.asset");
            var skills = new List<SkillDefinition>();
            foreach (var g in AssetDatabase.FindAssets("t:SkillDefinition", new[] { PTEditorUtil.DataDir + "/Skills" }))
                skills.Add(AssetDatabase.LoadAssetAtPath<SkillDefinition>(AssetDatabase.GUIDToAssetPath(g)));
            profile.AllSkills = skills.ToArray();
            ItemBuilder.ConfigureProfile(profile);
            profile.Specializations = TraitBuilder.Load();
            profile.Classes = ClassBuilder.Library;   // Lauf 5 / M25
        }
    }
}
