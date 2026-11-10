using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectTyrant.Anim;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 6: Story-Figuren als geriggte MeshKit-Figuren (Rig, Clips, Kopf für Lippen-Andeutung) je Sprecher: PT_Story_&lt;Id&gt;.
    /// Meshy-Fassungen (M34) werden wie in Lauf 5 erst versucht, bei Rissen bleibt die MeshKit-Figur.
    /// Zusätzlich Kampf-Fassung und Archetyp des Herolds (Boss in Kapitel 4, 8, 10).
    /// </summary>
    public static class StoryFigureBuilder
    {
        public const string Dir = "Assets/ProjectTyrant/Prefabs/Story";

        public static List<GameObject> BuildAll(StoryCast cast)
        {
            PTEditorUtil.EnsureFolder(Dir);
            var list = new List<GameObject>();
            foreach (var sp in cast.Speakers) { if (sp == null || sp.Id == "held" || sp.Look == "world") continue; /* „world“: Späher/Herz-NPCs stehen nur in der Zone */ var p = Build(sp.Id, Look(sp), "PT_Meshy_Story_" + sp.Id); if (p != null) list.Add(p); }
            var hero = Build("held", CharacterRigs.PlayerLook(), null); if (hero != null) list.Add(hero);
            var villager = BuildVillager(); if (villager != null) list.Add(villager);
            BuildHeroldFoe(cast.Get("herold"));
            return list;
        }

        public static RigBuilder.HumanLook Look(SpeakerProfile sp)
        {
            var l = new RigBuilder.HumanLook { Cloth = sp.Cloth, Trim = sp.Trim, Skin = sp.Skin, Hair = sp.Hair, Seed = sp.Id.GetHashCode() };
            switch (sp.Look)
            {
                case "soldier": l.Pauldrons = true; l.Cape = sp.Id == "brennar"; l.Beard = sp.Id == "brennar" || sp.Id == "orsk"; l.Helmet = sp.Id == "haldis"; l.Metal = new Color(0.72f, 0.74f, 0.8f); l.Height = sp.Id == "brennar" ? 1.88f : 1.8f; l.Bulk = 1.1f; break;
                case "scholar": l.Robe = true; l.Hood = sp.Id == "elmar"; l.Height = 1.74f; l.Bulk = 0.92f; break;
                case "agent": l.Hood = true; l.Cape = true; l.Height = 1.76f; l.Bulk = 0.9f; break;
                case "herald": l.Hood = true; l.Robe = true; l.Cape = true; l.GlowEyes = true; l.Eye = new Color(0.7f, 0.75f, 1.6f); l.Height = 1.95f; l.Skin = new Color(0.62f, 0.62f, 0.7f); break;
                case "tyrant": l.Crown = true; l.Horns = true; l.Cracks = true; l.GlowEyes = true; l.Eye = new Color(2f, 0.6f, 0.2f); l.Height = 2.7f; l.Bulk = 1.5f; l.Skin = new Color(0.35f, 0.2f, 0.18f); l.Cape = true; break;
                case "raider": l.Horns = true; l.Beard = true; l.Belly = true; l.Bulk = 1.25f; l.Skin = new Color(0.52f, 0.36f, 0.3f); break;
                default: l.Robe = sp.Id == "alda"; l.Hood = false; l.Height = 1.7f; break;
            }
            if (sp.Id == "mira") { l.Skin = new Color(0.82f, 0.6f, 0.46f); l.Hair = new Color(0.75f, 0.35f, 0.12f); l.Height = 1.68f; l.Bulk = 0.9f; }
            if (sp.Id == "veyra") l.Hair = new Color(0.12f, 0.1f, 0.12f);
            return l;
        }

        static GameObject Build(string id, RigBuilder.HumanLook look, string meshy)
        {
            string path = Dir + "/PT_Story_" + id + ".prefab";
            var root = new GameObject("PT_Story_" + id);
            var r = CharacterRigs.Human(root.transform, "PT_Story_" + id, look, meshy);
            var ca = CharacterRigs.Finish(r, "Sword", true);
            ca.StartClip = "Breath";
            var col = root.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, look.Height * 0.5f, 0); col.height = look.Height; col.radius = 0.35f * look.Bulk;
            var d = CharacterRigs.Drive(root, r.Visual, AnimDriver.Role.Villager); d.LookAtPlayer = true;
            var a = root.AddComponent<StoryActor>(); a.Speaker = id;
            var marker = root.AddComponent<PlaceholderMarker>(); marker.AssetId = "story_" + id; marker.MeshyPrompt = "Story-Figur " + id + " (MeshKit; Meshy-Fassung in M34 versucht)";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject BuildVillager()
        {
            string path = Dir + "/PT_Story_dorfbewohner.prefab";
            var root = new GameObject("PT_Story_dorfbewohner");
            CharacterRigs.Npc(root.transform, new Color(0.55f, 0.45f, 0.3f), "story_dorfbewohner");
            root.AddComponent<StoryActor>().Speaker = "dorfbewohner";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Kampf-Fassung des Herolds: Gegner-Prefab (Combatant, NpcBrain, HitFlash, Treiber) + Archetyp „Enemy_Story_Herold“ (Champion, Trotzleiste).</summary>
        static void BuildHeroldFoe(SpeakerProfile sp)
        {
            if (sp == null) return;
            var template = AssetDatabase.LoadAssetAtPath<EnemyArchetype>("Assets/ProjectTyrant/Data/Enemies/Enemy_Champion_Grolmok.asset");
            if (template == null || template.Prefab == null) return;
            string prefabPath = "Assets/ProjectTyrant/Prefabs/Characters/PT_StoryFoe_Herold.prefab";
            var root = (GameObject)PrefabUtility.InstantiatePrefab(template.Prefab);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "PT_StoryFoe_Herold";
            var oldVis = root.transform.Find("Visual"); if (oldVis != null) Object.DestroyImmediate(oldVis.gameObject);
            var look = Look(sp);
            var r = CharacterRigs.Human(root.transform, "PT_StoryFoe_Herold", look, null);
            var ca = CharacterRigs.Finish(r, "Staff", true);
            var d = CharacterRigs.Drive(root, r.Visual, AnimDriver.Role.Npc); d.NpcAttackClip = "Cast2H";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            string aPath = "Assets/ProjectTyrant/Data/Enemies/Enemy_Story_Herold.asset";
            var a = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(aPath);
            if (a == null) { a = Object.Instantiate(template); AssetDatabase.CreateAsset(a, aPath); }
            else EditorUtility.CopySerialized(template, a);
            a.name = "Enemy_Story_Herold"; a.DisplayName = "Der Herold"; a.Prefab = prefab; a.Scale = 1.15f; a.Defiance = 900f; a.HealthFactor = 22f; a.AttackCoefficient = 5f; a.AttackRange = 12f; a.Palette = VfxPalette.Schatten; a.TelegraphRadius = 4.5f; a.TelegraphInterval = 8f; a.Loot = null;
            EditorUtility.SetDirty(a);
        }
    }
}
