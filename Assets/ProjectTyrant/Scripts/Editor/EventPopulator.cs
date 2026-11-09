using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    public static partial class SceneBuilder
    {
        static partial void AddEventSystemsImpl(GameObject go)
        {
            var d = go.AddComponent<EventDirector>();
            d.GoldLoot = ItemBuilder.Table("Veteran");
        }
    }

    public static partial class ZoneBuilder
    {
        static partial void PopulateEventImpl(ZoneRoot director, ZoneAnchors anchors, Transform npcRoot)
        {
            director.Chain = AssetDatabase.LoadAssetAtPath<EventChain>(PTEditorUtil.DataDir + "/Events/EventChain_Grenzposten.asset");
            director.Events = EventDefinitionBuilder.Load();
            var crates = new List<Combatant>();
            var cratePrefab = DataBuilder.CratePrefab;
            foreach (var spot in anchors.CrateSpots)
            {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, npcRoot);
                c.transform.position = spot.position;
                c.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                crates.Add(c.GetComponent<Combatant>());
            }
            director.Crates = crates.ToArray();
            var guardArch = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(PTEditorUtil.DataDir + "/Enemies/Ally_Dornwacht_Waechter.asset");
            var guards = new List<NpcBrain>();
            for (int i = 0; i < anchors.GuardSpots.Length; i++)
            {
                var g = SpawnNpc(guardArch, anchors.GuardSpots[i].position, 45f + i * 90f);
                guards.Add(g.GetComponent<NpcBrain>());
            }
            director.Guards = guards.ToArray();
            // Sonnfurts Vorräte (Ziel der Meta-Verteidigung)
            var hubCrates = new List<Combatant>();
            Vector2[] hubSpots = { new Vector2(-64, -54), new Vector2(-56, -66), new Vector2(-66, -64) };
            foreach (var hs in hubSpots)
            {
                var c = (GameObject)PrefabUtility.InstantiatePrefab(cratePrefab, npcRoot);
                c.transform.position = G(hs);
                c.name = "Sonnfurt_Vorrat";
                hubCrates.Add(c.GetComponent<Combatant>());
            }
            director.HubCrates = hubCrates.ToArray();
            // Karawanenhändlerin (nur nach erfolgreicher Eskorte, 5 min)
            var caravanDef = AssetDatabase.LoadAssetAtPath<VendorDefinition>(ItemBuilder.VendorDir + "/Vendor_Karawane.asset");
            if (caravanDef == null) { caravanDef = ScriptableObject.CreateInstance<VendorDefinition>(); AssetDatabase.CreateAsset(caravanDef, ItemBuilder.VendorDir + "/Vendor_Karawane.asset"); }
            caravanDef.Id = "karawane"; caravanDef.DisplayName = "Karawanenhändlerin Mara"; caravanDef.Kind = VendorKind.Caravan;
            var db = ItemBuilder.Database;
            caravanDef.Stock = new List<VendorEntry>();
            foreach (var (id, price) in new[] { ("gs_mw_rit", 700), ("st_mw_aas", 700), ("tr_amulet_mw", 650), ("bag_15", 1500), ("food_soup", 60), ("kit_fine", 110) })
            { var it = db != null ? db.Get(id) : null; if (it != null) caravanDef.Stock.Add(new VendorEntry { Item = it, PriceCopper = price, Count = 1 }); }
            EditorUtility.SetDirty(caravanDef);
            director.CaravanVendor = ItemBuilder.Place(npcRoot, caravanDef, new Vector2(-9, -3), 45f, new Color(0.3f, 0.5f, 0.6f));
        }
    }

    public static class EventDataBuilder
    {
        [MenuItem("Project Tyrant/Build Event Data")]
        public static void Build()
        {
            string dir = PTEditorUtil.DataDir + "/Events";
            PTEditorUtil.EnsureFolder(dir);
            string path = dir + "/EventChain_Grenzposten.asset";
            var chain = AssetDatabase.LoadAssetAtPath<EventChain>(path);
            if (chain == null) { chain = ScriptableObject.CreateInstance<EventChain>(); AssetDatabase.CreateAsset(chain, path); }
            chain.ChainName = "Grenzposten Dornwacht";
            chain.FirstStage = "defend";
            chain.EventRadius = 32f;
            chain.AutoStartDelay = 60f;
            chain.RestartCooldown = 60f;
            chain.Stages = EventChain.CreateDefaultStages();
            chain.Trash = DataBuilder.Archetype("Trash_Pluenderer");
            chain.Veteran = DataBuilder.Archetype("Veteran_Brecher");
            chain.Champion = DataBuilder.Archetype("Champion_Grolmok");
            EditorUtility.SetDirty(chain);
            AssetDatabase.SaveAssets();
            EventDefinitionBuilder.Build(chain);
        }

        /// <summary>Lauf 4: Rebuild Everything läuft über <see cref="WorldBuilder.RebuildEverything"/>.</summary>
        public static void RebuildEverything() => WorldBuilder.RebuildEverything();
    }
}
