using System.Collections.Generic;
using ProjectTyrant.Procedural;
using TMPro;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Zonenabschluss: Definitionen (Data/Zone) + Weltobjekte (Wegmarken, POIs, Aussichtspunkte, Heldenherausforderungen, Herzen, Karma-Händler).</summary>
    public static class ZoneContentBuilder
    {
        public const string Dir = DataDir + "/Zone";
        static Transform _root;

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); }
            return a;
        }

        static ZoneObjectDefinition Def(string id, MapMarkerKind kind, string name, string desc, Vector3 pos, float radius, int xp, int karma = 0, int copper = 0, int hp = 0)
        {
            var d = Asset<ZoneObjectDefinition>(Dir + "/Zone_" + id + ".asset");
            d.Id = id; d.Kind = kind; d.DisplayName = name; d.Description = desc; d.Position = pos; d.Radius = radius;
            d.Experience = xp; d.Karma = karma; d.Copper = copper; d.HeroPoints = hp; d.HeartTask = null; d.KillValue = d.TaskValue = 0f;
            EditorUtility.SetDirty(d);
            return d;
        }

        static Vector3 G(Vector2 p, float y = 0f) => ZoneBuilder.G(p, y);

        /// <summary>Ersetzt die Primitive-Komposition (außer Lichtern und Interaktions-Komponenten) durch das Meshy-Modell, falls vorhanden.</summary>
        public static bool UseMeshy(Transform parent, string model, float height, Vector3 offset)
        {
            var prefab = MeshyIntegrator.GetOrBuild(model, height, false, 0f, true);
            if (prefab == null) return false;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var ch = parent.GetChild(i);
                if (ch.GetComponentInChildren<Light>() == null && ch.name != "Nameplate") Object.DestroyImmediate(ch.gameObject);
            }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.name = "Meshy_" + model; inst.transform.localPosition = offset;
            var ph = parent.GetComponent<PlaceholderMarker>(); if (ph != null) ph.ReplacedByMeshy = true;
            return true;
        }

        public static void Build(ZoneAnchors anchors, Transform npcRoot)
        {
            EnsureFolder(Dir);
            _root = new GameObject("ZoneContent").transform;
            BuildWaypoints(anchors);
            BuildPois();
            BuildVistas();
            BuildChallenges();
            BuildHearts(npcRoot);
            AssetDatabase.SaveAssets();
        }

        // ---------------- Wegmarken ----------------
        static void BuildWaypoints(ZoneAnchors anchors)
        {
            var crystal = Mat("WaypointCrystal", new Color(0.45f, 0.75f, 1f), 0.1f, new Color(0.3f, 0.6f, 1f) * 2.2f);
            var stone = Mat("WaypointStone", new Color(0.62f, 0.62f, 0.66f));
            foreach (var w in ZoneContent.Waypoints)
            {
                var def = Def(w.Id, MapMarkerKind.Waypoint, w.Name, w.Desc, G(w.Pos), ZoneContent.WaypointDiscoverRadius, 120);
                GameObject go;
                if (w.Id == "wp_sonnfurt" && anchors != null && anchors.Waypoint != null)
                {
                    go = anchors.Waypoint.gameObject;
                    var old = go.GetComponent<MapMarker>(); if (old != null) Object.DestroyImmediate(old);
                }
                else
                {
                    go = Child(_root, "Waypoint_" + w.Id, G(w.Pos));
                }
                if (go.transform.Find("Monument") == null) BuildMonument(go.transform, stone, crystal);
                var wp = go.GetComponent<Waypoint>(); if (wp == null) wp = go.AddComponent<Waypoint>();
                wp.Definition = def; wp.PromptText = "Reisen"; wp.Range = 3.5f;
            }
        }

        public static void BuildMonument(Transform parent, Material stone, Material crystal)
        {
            var m = Child(parent, "Monument", Vector3.zero).transform;
            Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(2.4f, 0.15f, 2.4f), stone, "Base", null, true);
            Part(m, PrimitiveType.Cylinder, new Vector3(0, 0.55f, 0), new Vector3(1.2f, 0.25f, 1.2f), stone, "Plinth", null, true);
            var c = Part(m, PrimitiveType.Cube, new Vector3(0, 2.0f, 0), new Vector3(0.55f, 1.4f, 0.55f), crystal, "Crystal", new Vector3(0, 45, 0));
            c.AddComponent<Bobber>();
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(m, false); l.transform.localPosition = new Vector3(0, 2.2f, 0);
            l.type = LightType.Point; l.color = new Color(0.45f, 0.7f, 1f); l.range = 7f; l.intensity = 2.2f; l.shadows = LightShadows.None;
            AddPlaceholder(m.gameObject, "Waypoint_Monument (Tier 1)", "stylized painterly fantasy waypoint monument, small stone plinth with a floating glowing blue crystal, game-ready low poly, hand-painted texture");
            UseMeshy(m, "PT_Meshy_WaypointMonument", 2.8f, Vector3.zero);
        }

        // ---------------- Sehenswürdigkeiten ----------------
        static void BuildPois()
        {
            foreach (var p in ZoneContent.Pois)
            {
                var def = Def(p.Id, MapMarkerKind.PointOfInterest, p.Name, p.Desc, G(p.Pos, 1f), ZoneContent.PoiRadius, 150);
                var go = Child(_root, "POI_" + p.Id, G(p.Pos, 1f));
                go.AddComponent<PointOfInterest>().Definition = def;
            }
        }

        // ---------------- Aussichtspunkte ----------------
        static void BuildVistas()
        {
            var stone = Mat("VistaStone", new Color(0.58f, 0.55f, 0.5f));
            var gold = Mat("VistaGold", new Color(1f, 0.8f, 0.35f), 0.1f, new Color(1f, 0.7f, 0.2f) * 1.6f);
            foreach (var v in ZoneContent.Vistas)
            {
                var top = ZoneContent.PlatformTop(v);
                var def = Def(v.Id, MapMarkerKind.Vista, v.Name, "Erklimme die Treppe und genieße die Aussicht.", top, 3f, 250);
                var root = Child(_root, "Vista_" + v.Id, Vector3.zero).transform;
                float ground = ZoneLayout.Height(v.Pos.x, v.Pos.y);
                float h = v.Height + 1.5f;   // Plattform reicht unter den Boden (Hang)
                Part(root, PrimitiveType.Cube, new Vector3(v.Pos.x, top.y - h * 0.5f, v.Pos.y), new Vector3(ZoneContent.PlatformHalf * 2f, h, ZoneContent.PlatformHalf * 2f), stone, "Platform", null, true);
                var dir = v.StairDir.normalized;
                float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                foreach (var s in ZoneContent.StairSteps(v))
                {
                    float g = Mathf.Min(ZoneLayout.Height(s.x, s.z), ground) - 0.6f;
                    float sh = s.y - g;
                    Part(root, PrimitiveType.Cube, new Vector3(s.x, g + sh * 0.5f, s.z), new Vector3(2.0f, sh, ZoneContent.StairTread + 0.02f), stone, "Step", new Vector3(0, yaw, 0), true);
                }
                var stele = Child(root, "Stele", top).transform;
                Part(stele, PrimitiveType.Cube, new Vector3(0, 0.9f, 0), new Vector3(0.35f, 1.8f, 0.35f), stone, "Pillar", null, true);
                Part(stele, PrimitiveType.Sphere, new Vector3(0, 1.95f, 0), new Vector3(0.45f, 0.45f, 0.45f), gold, "Orb").AddComponent<Bobber>();
                AddPlaceholder(stele.gameObject, "Vista_Stele (Tier 1)", "stylized painterly fantasy viewpoint stele, slender carved stone pillar topped with a glowing golden orb, game-ready low poly, hand-painted texture");
                UseMeshy(stele, "PT_Meshy_VistaStele", 2.4f, Vector3.zero);
                var vista = stele.gameObject.AddComponent<Vista>();
                vista.Definition = def; vista.PromptText = "Aussicht betrachten"; vista.Range = 3f;
            }
        }

        // ---------------- Heldenherausforderungen ----------------
        static void BuildChallenges()
        {
            var stone = Mat("ShrineStone", new Color(0.5f, 0.52f, 0.56f));
            var blue = Mat("ShrineGlow", new Color(0.45f, 0.8f, 1f), 0.1f, new Color(0.3f, 0.7f, 1f) * 1.8f);
            var curse = Mat("RelicCurse", new Color(0.35f, 0.15f, 0.45f), 0.1f, new Color(0.6f, 0.2f, 1f) * 1.6f);
            var red = Mat("ChallengeBanner", new Color(0.75f, 0.15f, 0.12f));
            var vet = DataBuilder.Archetype("Veteran_Brecher");
            var trash = DataBuilder.Archetype("Trash_Pluenderer");
            foreach (var c in ZoneContent.Challenges)
            {
                float g = ZoneLayout.Height(c.Pos.x, c.Pos.y);
                var top = new Vector3(c.Pos.x, g + c.PillarHeight, c.Pos.y);
                var def = Def(c.Id, MapMarkerKind.HeroChallenge, c.Name, c.Desc, top, 3f, 200, 0, 0, ZoneContent.ChallengeHeroPoints);
                var root = Child(_root, "Challenge_" + c.Id, Vector3.zero).transform;
                if (c.PillarHeight > 0f)
                {
                    Part(root, PrimitiveType.Cylinder, new Vector3(c.Pos.x, g + c.PillarHeight * 0.5f - 0.5f, c.Pos.y), new Vector3(3.2f, c.PillarHeight * 0.5f + 0.5f, 3.2f), stone, "Pillar", null, true);
                    foreach (var s in ZoneContent.PillarSteps(c))
                    {
                        float sg = ZoneLayout.Height(s.x, s.z) - 0.5f;
                        Part(root, PrimitiveType.Cylinder, new Vector3(s.x, (s.y + sg) * 0.5f, s.z), new Vector3(1.6f, (s.y - sg) * 0.5f, 1.6f), stone, "RockStep", null, true);
                    }
                }
                var shrine = Child(root, "Shrine", top).transform;
                switch (c.Kind)
                {
                    case ZoneContent.ChallengeKind.Relic:
                        Part(shrine, PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(0.9f, 1f, 0.9f), curse, "Relic", new Vector3(0, 30, 15), true);
                        break;
                    case ZoneContent.ChallengeKind.Combat:
                        Part(shrine, PrimitiveType.Cylinder, new Vector3(0, 1.5f, 0), new Vector3(0.12f, 1.5f, 0.12f), stone, "Pole", null, true);
                        Part(shrine, PrimitiveType.Cube, new Vector3(0.45f, 2.4f, 0), new Vector3(0.8f, 0.9f, 0.04f), red, "Banner");
                        break;
                    default:
                        Part(shrine, PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(1.1f, 0.3f, 1.1f), stone, "Base", null, true);
                        Part(shrine, PrimitiveType.Cube, new Vector3(0, 1.3f, 0), new Vector3(0.4f, 0.9f, 0.4f), blue, "Crystal", new Vector3(0, 45, 0)).AddComponent<Bobber>();
                        break;
                }
                AddPlaceholder(shrine.gameObject, "HeroChallenge_Shrine (Tier 1)", "stylized painterly fantasy hero challenge shrine, small hexagonal stone altar with a floating light-blue crystal, game-ready low poly, hand-painted texture");
                if (c.Kind == ZoneContent.ChallengeKind.Communion) UseMeshy(shrine, "PT_Meshy_ChallengeShrine", 1.8f, Vector3.zero);
                var hc = shrine.gameObject.AddComponent<HeroChallenge>();
                hc.Definition = def; hc.Kind = c.Kind; hc.Range = 3f;
                hc.ChannelTime = c.Kind == ZoneContent.ChallengeKind.Communion ? 5f : 0f;
                hc.PromptText = c.Kind == ZoneContent.ChallengeKind.Communion ? "Kommunizieren" : "Interagieren";
                hc.Challenger = vet; hc.Minion = trash;
            }
        }

        // ---------------- Herzen ----------------
        static void BuildHearts(Transform npcRoot)
        {
            var db = ItemBuilder.Database;
            ItemDefinition I(string id) => db != null ? db.Get(id) : null;
            var vendorStock = new Dictionary<string, (string name, List<VendorEntry> stock)>
            {
                { "karma_wilma", ("Bäuerin Wilma (Karma)", new List<VendorEntry> { K(I("food_stew"), 30, 5), K(I("food_pie"), 30, 5), K(I("ar_fine_head"), 60), K(I("ar_fine_shoulders"), 60), K(I("ar_fine_chest"), 90), K(I("ar_fine_hands"), 60), K(I("ar_fine_legs"), 80), K(I("ar_fine_feet"), 60), K(I("ar_mw_chest"), 420), K(I("ar_mw_legs"), 380), K(I("bag_10"), 300) }) },   // Balance B1: vollständiges Edel-Set im ersten Herzen
                { "karma_bertold", ("Archivar Bertold (Karma)", new List<VendorEntry> { K(I("st_rare_aas"), 1800), K(I("tr_ring_rare_aas"), 1400), K(I("ar_rare_head"), 900), K(I("food_soup"), 60, 5) }) },
                { "karma_kerla", ("Hauptfrau Kerla (Karma)", new List<VendorEntry> { K(I("gs_rare_bers"), 1800), K(I("tr_acc_rare"), 1400), K(I("ar_rare_chest"), 1200), K(I("potion_big"), 50, 5) }) },
            };
            var robe = Mat("HeartNpcRobe", new Color(0.85f, 0.65f, 0.25f));
            var skin = Mat("NpcSkin", new Color(0.9f, 0.72f, 0.58f));
            var crate = Mat("HeartCrate", new Color(0.55f, 0.38f, 0.2f));
            var fence = Mat("HeartFence", new Color(0.45f, 0.32f, 0.2f));
            var guardMat = Mat("InjuredGuard", new Color(0.35f, 0.45f, 0.7f));
            var trash = DataBuilder.Archetype("Trash_Pluenderer");
            foreach (var h in ZoneContent.Hearts)
            {
                var def = Def(h.Id, MapMarkerKind.Heart, h.Name, h.Task, G(h.Center), h.Radius, 400, 250, 120);
                def.HeartTask = h.Task; def.KillValue = h.KillValue; def.TaskValue = h.TaskValue;
                var npc = Child(_root, "Heart_" + h.Id, G(h.NpcPos)).transform;
                CharacterRigs.Npc(npc, robe.GetColor("_BaseColor"), h.Id);
                Nameplate(npc, h.Npc, "Herz: " + h.Name, new Color(1f, 0.8f, 0.3f));
                AddPlaceholder(npc.gameObject, "Heart_NPC (Tier 2)", "stylized painterly fantasy villager quest giver, friendly farmer in golden tunic, game-ready low poly, hand-painted texture, full body");
                var heart = npc.gameObject.AddComponent<Heart>();
                heart.Definition = def; heart.NpcName = h.Npc; heart.PromptText = "Sprechen"; heart.Range = 3f;

                foreach (var t in h.Tasks)
                {
                    var tgo = Child(_root, "HeartTask_" + h.Id + "_" + t.Kind, G(t.Pos)).transform;
                    var vis = Child(tgo, "Visual", Vector3.zero).transform;
                    switch (t.Kind)
                    {
                        case ZoneContent.TaskKind.Pickup:
                            Part(vis, PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(0.7f, 0.7f, 0.7f), crate, "Crate", new Vector3(0, 20, 0));
                            break;
                        case ZoneContent.TaskKind.Repair:
                            Part(vis, PrimitiveType.Cube, new Vector3(-0.9f, 0.6f, 0), new Vector3(0.15f, 1.2f, 0.15f), fence, "Post");
                            Part(vis, PrimitiveType.Cube, new Vector3(0.9f, 0.6f, 0), new Vector3(0.15f, 1.2f, 0.15f), fence, "Post");
                            Part(vis, PrimitiveType.Cube, new Vector3(0.1f, 0.45f, 0), new Vector3(1.9f, 0.12f, 0.08f), fence, "BrokenPlank", new Vector3(0, 0, 18));
                            break;
                        case ZoneContent.TaskKind.Bandage:
                            CharacterRigs.Injured(vis, "injured_" + h.Id);
                            break;
                    }
                    var task = tgo.gameObject.AddComponent<HeartTask>();
                    task.Heart = heart; task.Kind = t.Kind; task.Range = 2.6f;
                    task.ChannelTime = t.Kind == ZoneContent.TaskKind.Pickup ? 0f : t.Kind == ZoneContent.TaskKind.Repair ? 2f : 2.5f;
                    task.PromptText = t.Kind.ToString();
                }
                if (trash != null && trash.Prefab != null && npcRoot != null)
                    foreach (var sp in h.ExtraSpawns)
                    {
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(trash.Prefab, npcRoot);
                        go.transform.position = G(sp);
                        go.GetComponent<NpcBrain>().Archetype = trash;
                    }

                var vs = vendorStock[h.VendorId];
                var vdef = Asset<VendorDefinition>(ItemBuilder.VendorDir + "/Vendor_" + h.VendorId + ".asset");
                vdef.Id = h.VendorId; vdef.DisplayName = vs.name; vdef.Kind = VendorKind.Karma; vdef.Stock = vs.stock.FindAll(e => e.Item != null);
                EditorUtility.SetDirty(vdef);
                var vn = ItemBuilder.Place(_root, vdef, h.NpcPos + new Vector2(2.5f, 1.5f), 180f, new Color(0.55f, 0.45f, 0.2f));
                if (vn != null) vn.RequiredCompletion = h.Id;
            }
        }

        public static VendorEntry K(ItemDefinition d, int karma, int count = 1) => new VendorEntry { Item = d, PriceKarma = karma, PriceCopper = 0, Count = count };

        public static void Nameplate(Transform parent, string name, string sub, Color c)
        {
            var plate = new GameObject("Nameplate");
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = new Vector3(0, 2.7f, 0);
            var tmp = plate.AddComponent<TextMeshPro>();
            tmp.text = "<b>" + name + "</b>\n<size=70%>" + sub + "</size>";
            tmp.fontSize = 1.8f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = c;
            tmp.rectTransform.sizeDelta = new Vector2(6, 1.5f);
            plate.AddComponent<Billboard>();
        }
    }
}
