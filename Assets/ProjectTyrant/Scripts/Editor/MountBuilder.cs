using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 4 / M17: drei Reittiere (Daten + prozedurale Modelle) und die Stallmeister in Sonnfurt, Nebelmoor, Frostkamm.</summary>
    public static class MountBuilder
    {
        public const string Dir = DataDir + "/Mounts";
        const string PrefabDirMounts = PrefabDir + "/Mounts";
        const long Gold = 10000;

        public static MountDefinition Load(MountKind k) => AssetDatabase.LoadAssetAtPath<MountDefinition>(Dir + "/Mount_" + k + ".asset");
        public static MountDefinition[] LoadAll() => new[] { Load(MountKind.Raptor), Load(MountKind.Springer), Load(MountKind.Skimmer) };

        public static void BuildAll()
        {
            EnsureFolder(Dir); EnsureFolder(PrefabDirMounts);
            Def(MountKind.Raptor, "Raptor", "Schneller Läufer mit weitem Satz – überspringt Lavarisse und Schluchten.", "Weiter Sprung nach vorn (≈ 15 m)", "Schwanzfeger: Schaden + Verkrüppelung",
                2 * Gold, "heart_gerstenbrunn", "sonnfurt", "mount_raptor", 1.55f,
                p => CreatureBuilder.Raptor(p, "MRaptor", new CreatureBuilder.Look { Body = new Color(0.78f, 0.45f, 0.22f), Belly = new Color(0.95f, 0.85f, 0.62f), Accent = new Color(0.35f, 0.55f, 0.9f), Eye = new Color(1.6f, 1.2f, 0.2f) }, true),
                "stylized painterly fantasy saddled raptor mount, orange scales with blue crest, game-ready low poly, hand-painted texture");
            Def(MountKind.Skimmer, "Skimmer", "Schwebender Rochen – gleitet schnell über tiefes Wasser.", "Hüpfer; schwebt über Wasser (Tempo 12,5 m/s)", "Wellenschlag: Schaden + 5 Verwundbarkeit",
                4 * Gold, "heart_moor_merle", "nebelmoor", "mount_skimmer", 1.05f,
                p => CreatureBuilder.Ray(p, "MSkim", new CreatureBuilder.Look { Body = new Color(0.25f, 0.55f, 0.6f), Belly = new Color(0.85f, 0.92f, 0.9f), Accent = new Color(0.95f, 0.85f, 0.4f), Eye = new Color(0.4f, 1.4f, 1.6f), Glow = 0.6f }, true),
                "stylized painterly fantasy hovering manta ray mount with saddle, teal skin with golden spots, game-ready low poly, hand-painted texture");
            Def(MountKind.Springer, "Springer", "Kräftiger Hüpfer – lädt Sprünge bis 15 m Höhe auf.", "Halten: Sprung aufladen (3–15 m), loslassen: springen", "Stampfer: Schaden + 1,5 s Betäubung",
                6 * Gold, "heart_frost_runa", "frostkamm", "mount_springer", 1.7f,
                p => CreatureBuilder.Springer(p, "MSpring", new CreatureBuilder.Look { Body = new Color(0.72f, 0.62f, 0.5f), Belly = new Color(0.95f, 0.92f, 0.85f), Accent = new Color(0.55f, 0.3f, 0.6f), Eye = new Color(0.15f, 0.1f, 0.1f), Scale = 1.15f }, true),
                "stylized painterly fantasy giant rabbit mount with powerful hind legs and saddle, game-ready low poly, hand-painted texture");
            AssetDatabase.SaveAssets();
        }

        static void Def(MountKind k, string name, string desc, string ability, string engage, long price, string heart, string zone, string icon, float seat, System.Func<Transform, GameObject> visual, string prompt)
        {
            string path = Dir + "/Mount_" + k + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<MountDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<MountDefinition>(); AssetDatabase.CreateAsset(d, path); }
            d.Kind = k; d.DisplayName = name; d.Description = desc; d.AbilityText = ability; d.EngageText = engage;
            d.Stats = MountStats.Default(k); d.PriceCopper = price; d.RequiredHeart = heart; d.VendorZone = zone; d.Icon = icon; d.SeatHeight = seat;
            var go = new GameObject("PT_Mount_" + k);
            var v = visual(go.transform);
            AddPlaceholder(v, "PT_Mount_" + k, prompt);
            // Lauf 5: Meshy-Modell auf das Vierbeiner-Skelett skinnen; bei Rissen bleibt die geriggte MeshKit-Fassung (Modell → Statue)
            float mh = k == MountKind.Raptor ? 2.6f : k == MountKind.Springer ? 2.4f : 1.3f;
            string prof = k == MountKind.Raptor ? "Raptor" : k == MountKind.Springer ? "Springer" : "Ray";
            var skinned = CharacterRigs.TryMeshyBeast(go.transform, "PT_Meshy_Mount" + k, mh, prof, true);
            if (skinned != null) { Object.DestroyImmediate(v); v = skinned; }
            CharacterRigs.Drive(go, v, Anim.AnimDriver.Role.Mount);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            SetLayerRecursive(go, CharacterLayer);
            d.Prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDirMounts + "/PT_Mount_" + k + ".prefab");
            Object.DestroyImmediate(go);
            EditorUtility.SetDirty(d);
        }

        /// <summary>Stallmeister mit Reittier-Modell daneben.</summary>
        public static MountVendor PlaceStallmeister(Transform parent, MountKind k, Vector3 pos, float yaw, string npcName)
        {
            var d = Load(k);
            if (d == null) return null;
            var go = Child(parent, "Stallmeister_" + k, pos);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            CharacterRigs.Npc(go.transform, new Color(0.45f, 0.32f, 0.2f), "stall_" + k, true);
            ZoneContentBuilder.Nameplate(go.transform, npcName, "Stallmeister · " + d.DisplayName, new Color(1f, 0.85f, 0.45f));
            if (d.Prefab != null)
            {
                var show = (GameObject)PrefabUtility.InstantiatePrefab(d.Prefab, go.transform);
                show.transform.localPosition = new Vector3(2.8f, 0f, 0.5f); show.transform.localRotation = Quaternion.Euler(0, -30, 0);
                var drv = show.GetComponent<Anim.AnimDriver>(); if (drv != null) drv.Mode = Anim.AnimDriver.Role.Critter;   // Schau-Tier: steht/idlet
            }
            AddPlaceholder(go, "Stallmeister (Tier 2)", "stylized painterly fantasy stable master npc in leather apron, game-ready low poly, hand-painted texture, full body");
            var mv = go.AddComponent<MountVendor>();
            mv.Mount = d; mv.NpcName = npcName; mv.PromptText = "Sprechen"; mv.Range = 3.2f;
            return mv;
        }
    }
}
