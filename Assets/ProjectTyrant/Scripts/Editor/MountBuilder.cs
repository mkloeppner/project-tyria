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
        public static MountDefinition[] LoadAll() => new[] { Load(MountKind.Raptor), Load(MountKind.Springer), Load(MountKind.Skimmer), Load(MountKind.Griffon), Load(MountKind.Skyscale), Load(MountKind.DuneRunner), Load(MountKind.Skiff) };

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
            Def(MountKind.Griffon, "Greif", "Der Greif der Himmelswächter – gleitet weit und stürzt sich aus großer Höhe herab. Nur über die Persönliche Geschichte (Kapitel 9).", "Abheben (Leertaste), in der Luft halten: Sturzflug, loslassen: Abfangen", "Sturzangriff: Schaden, Betäubung, Verwundbarkeit",
                0, "", "himmelsspitzen", "mount_griffon", 1.6f, p => GriffonVisual(p), "stylized painterly fantasy griffin mount with saddle, eagle head, lion body, white and gold feathers, game-ready low poly, hand-painted texture");
            Def(MountKind.Skyscale, "Himmelsschuppe", "Junger Himmelsdrache – steigt senkrecht auf, schwebt und haftet an Felswänden. Sammlung „Himmelsschuppe“ in den Himmelsspitzen.", "Leertaste halten: steigen; loslassen: schweben; Dodge-Taste in der Luft: Sturz", "Feuerstoß: Flächenschaden, 3 Brennen, hoher Trotzschaden",
                0, "", "himmelsspitzen", "mount_skyscale", 1.5f, p => SkyscaleVisual(p), "stylized painterly fantasy young sky dragon mount with saddle, blue scales, golden horns, membrane wings, game-ready low poly, hand-painted texture");
            // Lauf 11: Dünenläufer (Herz in Sonnentor + 10 Gold; die Geschichte schenkt ihn in Kapitel 14) und Boot (Werft in Jadehafen, Herz + 5 Gold)
            Def(MountKind.DuneRunner, "Dünenläufer", "Schakalartiger Sprinter der Glutwüste – Blinkschritt durch den Sand, nutzt Sandportale.", "Blinkschritt 15 m (2–3 Ladungen, laden am Boden)", "Sandsturm: Fläche, Blindheit, Trotzschaden 200",
                10 * Gold, "heart_sonnentor_1", "sonnentor", "mount_dunerunner", 1.45f,
                p => CreatureBuilder.Raptor(p, "MDune", new CreatureBuilder.Look { Body = new Color(0.88f, 0.7f, 0.42f), Belly = new Color(0.96f, 0.9f, 0.75f), Accent = new Color(0.95f, 0.55f, 0.2f), Eye = new Color(1.8f, 1.4f, 0.4f), Glow = 0.4f, Scale = 0.95f }, true),
                "stylized painterly fantasy sleek desert jackal mount with sand particles and saddle, game-ready low poly, hand-painted texture");
            Def(MountKind.Skiff, "Boot", "Ein wendiges Jade-Boot der Werft von Jadehafen – nur auf Wasser.", "Anker (Leertaste), Jade-Schub (V, Beherrschung)", "–",
                5 * Gold, "heart_jadehafen_3", "jadehafen", "mount_skiff", 0.9f, p => SkiffVisual(p),
                "stylized painterly fantasy small jade wooden skiff boat with sail, game-ready low poly, hand-painted texture");
            AssetDatabase.SaveAssets();
        }

        /// <summary>Lauf 11: Boot aus Primitiven (Rumpf, Mast, Segel).</summary>
        static GameObject SkiffVisual(Transform p)
        {
            var root = new GameObject("MSkiff"); root.transform.SetParent(p, false);
            var wood = Mat("SkiffWood", new Color(0.55f, 0.38f, 0.22f)); var jadeM = Mat("SkiffJade", new Color(0.35f, 0.8f, 0.6f), 0.15f, new Color(0.2f, 0.8f, 0.5f) * 0.5f);
            Part(root.transform, PrimitiveType.Cube, new Vector3(0, 0.3f, 0), new Vector3(1.6f, 0.5f, 4f), wood, "Rumpf");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0, 0.6f, 1.8f), new Vector3(1.2f, 0.3f, 0.6f), jadeM, "Bug");
            Part(root.transform, PrimitiveType.Cylinder, new Vector3(0, 1.8f, 0.3f), new Vector3(0.12f, 1.5f, 0.12f), wood, "Mast");
            Part(root.transform, PrimitiveType.Cube, new Vector3(0, 2f, 0.1f), new Vector3(0.05f, 1.8f, 1.4f), Mat("SkiffSail", new Color(0.95f, 0.92f, 0.85f)), "Segel");
            return root;
        }

        /// <summary>Lauf 10: Himmelsschuppe aus dem Raptor-Rig mit Schuppenfarben, Hautflügeln und Hörnern (Platzhalter).</summary>
        static GameObject SkyscaleVisual(Transform p)
        {
            var v = CreatureBuilder.Raptor(p, "MSkyscale", new CreatureBuilder.Look { Body = new Color(0.32f, 0.5f, 0.82f), Belly = new Color(0.85f, 0.88f, 0.95f), Accent = new Color(0.95f, 0.8f, 0.35f), Eye = new Color(1.6f, 0.7f, 0.2f), Glow = 0.3f }, true);
            var membrane = Mat("SkyscaleWing", new Color(0.45f, 0.62f, 0.95f));
            foreach (var side in new[] { -1f, 1f })
            {
                var w = Part(v.transform, PrimitiveType.Cube, new Vector3(side * 1.0f, 1.8f, -0.1f), new Vector3(1.7f, 0.05f, 1.1f), membrane, side < 0 ? "WingL" : "WingR", new Vector3(0, 0, side * -26f));
                AddPlaceholder(w, "Skyscale_Wing", "– Platzhalter-Hautflügel");
            }
            return v;
        }

        /// <summary>Lauf 6: Greif aus dem Raptor-Rig (Läufer-Skelett) mit Federkleid-Farben und zwei Flügeln.</summary>
        static GameObject GriffonVisual(Transform p)
        {
            var v = CreatureBuilder.Raptor(p, "MGriffon", new CreatureBuilder.Look { Body = new Color(0.82f, 0.68f, 0.42f), Belly = new Color(0.98f, 0.96f, 0.9f), Accent = new Color(0.95f, 0.8f, 0.3f), Eye = new Color(1.6f, 1.2f, 0.2f) }, true);
            var feather = Mat("GriffonWing", new Color(0.96f, 0.94f, 0.88f));
            foreach (var side in new[] { -1f, 1f })
            {
                var w = Part(v.transform, PrimitiveType.Cube, new Vector3(side * 1.1f, 1.7f, 0.1f), new Vector3(1.9f, 0.06f, 0.9f), feather, side < 0 ? "WingL" : "WingR", new Vector3(0, 0, side * -18f));
                AddPlaceholder(w, "Griffon_Wing", "– Platzhalter-Flügel");
            }
            return v;
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
            float mh = k == MountKind.Raptor || k == MountKind.Griffon || k == MountKind.Skyscale || k == MountKind.DuneRunner ? 2.6f : k == MountKind.Springer ? 2.4f : 1.3f;
            string prof = k == MountKind.Raptor || k == MountKind.Griffon || k == MountKind.Skyscale || k == MountKind.DuneRunner ? "Raptor" : k == MountKind.Springer ? "Springer" : "Ray";
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
