using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectTyrant.Anim;
using ProjectTyrant.Procedural;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 6 / M29: Späher, zusätzliche Herzen (Tiere füttern, Fallen entschärfen, Bündel-Ziele, Emote) und Herz-Sprecher je Zone aus
    /// <c>Data/Story/Source/world.csv</c>. Läuft am Ende jedes Zonen-Builds (vor Navmesh/Speichern), damit „Rebuild Zone“ alles reproduziert.
    /// </summary>
    public static class WorldStoryBuilder
    {
        public const string Source = "Assets/ProjectTyrant/Data/Story/Source/world.csv";

        public class Row { public string Zone, Type, Id, Name, Npc; public float X, Z, Radius; public Dictionary<string, string> Extra = new Dictionary<string, string>(); }

        public static List<Row> Read()
        {
            var list = new List<Row>();
            if (!File.Exists(Source)) return list;
            var lines = File.ReadAllLines(Source);
            for (int i = 1; i < lines.Length; i++)
            {
                var l = lines[i]; if (string.IsNullOrWhiteSpace(l) || l.StartsWith("#")) continue;
                var c = l.Split(';'); if (c.Length < 8) { Debug.LogWarning("[ProjectTyrant] world.csv Zeile " + (i + 1) + ": zu wenige Spalten"); continue; }
                var r = new Row { Zone = c[0], Type = c[1], Id = c[2], Name = c[3], X = F(c[4]), Z = F(c[5]), Radius = F(c[6]), Npc = c[7] };
                if (c.Length > 8) foreach (var kv in c[8].Split('|')) { var p = kv.Split(new[] { '=' }, 2); if (p[0].Trim().Length > 0) r.Extra[p[0].Trim()] = p.Length > 1 ? p[1].Trim() : ""; }
                list.Add(r);
            }
            return list;
        }

        static float F(string s) => float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0f;

        /// <summary>Inhalte einer Zone in die offene Zonen-Szene setzen.</summary>
        public static void Populate(ZoneDefinition z, Transform zoneRoot, Func<float, float, float> height)
        {
            if (z == null || zoneRoot == null) return;
            var root = new GameObject("PT_StoryWorld").transform; root.SetParent(zoneRoot, false);
            Vector3 G(float x, float zz) => new Vector3(x, height(x, zz), zz);
            var cast = AssetDatabase.LoadAssetAtPath<StoryCast>(StoryBuilder.Root + "/StoryCast.asset");
            string Display(string speaker, string fallback) { var sp = cast != null ? cast.Get(speaker) : null; return sp != null ? sp.DisplayName : fallback; }
            int scouts = 0, hearts = 0;
            foreach (var r in Read())
            {
                if (r.Zone != z.Id) continue;
                switch (r.Type)
                {
                    case "Scout": BuildScout(root, r, G(r.X, r.Z), Display(r.Npc, r.Name)); scouts++; break;
                    case "Heart": BuildHeart(z, root, r, G, Display(r.Npc, r.Name)); hearts++; break;
                    case "HeartNpc":
                        var go = GameObject.Find("Heart_" + r.Id);
                        if (go != null) GetOrAdd<StoryActor>(go).Speaker = r.Npc; else Debug.LogWarning("[ProjectTyrant] world.csv: Herz-NPC Heart_" + r.Id + " nicht gefunden");
                        break;
                }
            }
            var rs = RegionZones.Get(z.Id);   // Lauf 11: je Regions-Zone drei Späher an den Wegmarken
            if (rs != null)
            {
                var lay = RegionZones.Layout(z.Id);
                for (int i = 1; i <= 3 && i < lay.Waypoints.Length; i++)
                {
                    var w = lay.Waypoints[i];
                    var r = new Row { Zone = z.Id, Type = "Scout", Id = "scout_" + z.Id + "_" + i, Name = "Späher von " + rs.Places[i], Npc = "scout", X = w.Pos.x + 8f, Z = w.Pos.y - 6f, Radius = 160f };
                    BuildScout(root, r, G(r.X, r.Z), r.Name); scouts++;
                }
            }
            Debug.Log("[ProjectTyrant] Story-Welt " + z.Id + ": " + scouts + " Späher, " + hearts + " neue Herzen");
        }

        static void BuildScout(Transform root, Row r, Vector3 pos, string name)
        {
            var npc = Child(root, "Scout_" + r.Id, pos).transform;
            npc.rotation = Quaternion.Euler(0f, Mathf.Abs(r.Id.GetHashCode()) % 360, 0f);
            CharacterRigs.Npc(npc, new Color(0.32f, 0.5f, 0.68f), "scout_" + r.Id);   // eigener Mesh-Name (Dorfbewohner heißen „Sonnfurt_n“)
            var brass = Mat("ScoutSpyglass", new Color(0.78f, 0.62f, 0.3f), 0.15f);
            var glass = Part(npc, PrimitiveType.Cylinder, new Vector3(0.32f, 1.05f, 0.18f), new Vector3(0.07f, 0.22f, 0.07f), brass, "Spyglass", new Vector3(70f, 0f, 0f));
            AddPlaceholder(glass, "Scout_Spyglass", "brass spyglass, stylized hand-painted, game-ready low poly");
            ZoneContentBuilder.Nameplate(npc, name, "Späher", new Color(0.7f, 0.9f, 1f));
            var s = npc.gameObject.AddComponent<Scout>();
            s.Id = r.Id; s.DisplayName = name; s.Scene = "SP_" + r.Id; s.SectorCenter = new Vector3(r.Extra.TryGetValue("cx", out var cx) ? F(cx) : r.X, pos.y, r.Extra.TryGetValue("cz", out var cz) ? F(cz) : r.Z);
            s.SectorRadius = r.Radius > 0f ? r.Radius : 120f; s.PromptText = "Späher ansprechen"; s.Range = 3f;
            npc.gameObject.AddComponent<StoryActor>().Speaker = r.Npc;
        }

        static void BuildHeart(ZoneDefinition z, Transform root, Row r, Func<float, float, Vector3> G, string npcName)
        {
            int level = r.Extra.TryGetValue("level", out var lv) ? Mathf.RoundToInt(F(lv)) : z.LevelMin;
            float sc = LevelCurves.RewardScale(level);   // Lauf 11
            string dir = DataDir + "/Zone/" + z.Id; EnsureFolder(dir);
            string path = dir + "/Zone_" + r.Id + ".asset";
            var def = AssetDatabase.LoadAssetAtPath<ZoneObjectDefinition>(path);
            if (def == null) { def = ScriptableObject.CreateInstance<ZoneObjectDefinition>(); AssetDatabase.CreateAsset(def, path); }
            var center = G(r.X, r.Z);
            def.Id = r.Id; def.Kind = MapMarkerKind.Heart; def.DisplayName = r.Name; def.Description = r.Extra.TryGetValue("task", out var task) ? task : r.Name;
            def.Position = center; def.Radius = r.Radius > 0f ? r.Radius : 25f; def.Experience = Mathf.RoundToInt(XpRewards.Heart * sc); def.Karma = Mathf.RoundToInt(250 * sc); def.Copper = Mathf.RoundToInt(120 * sc); def.HeroPoints = 0;
            def.HeartTask = def.Description; def.KillValue = r.Extra.TryGetValue("kill", out var kv) ? F(kv) : 0.04f; def.TaskValue = r.Extra.TryGetValue("value", out var tv) ? F(tv) : 0.1f; def.Requires = Traversal.None;
            EditorUtility.SetDirty(def);
            if (z.CompletionIds == null) z.CompletionIds = new List<string>();
            if (!z.CompletionIds.Contains(def.Id)) { z.CompletionIds.Add(def.Id); EditorUtility.SetDirty(z); }

            var np = r.Extra.TryGetValue("npc", out var npcAt) ? npcAt.Split(',') : null;
            var npcPos = np != null && np.Length == 2 ? G(r.X + F(np[0]), r.Z + F(np[1])) : G(r.X + 3f, r.Z + 3f);
            var npc = Child(root, "Heart_" + r.Id, npcPos).transform;
            CharacterRigs.Npc(npc, new Color(0.85f, 0.65f, 0.25f), r.Id);
            ZoneContentBuilder.Nameplate(npc, npcName, "Herz: " + r.Name, new Color(1f, 0.8f, 0.3f));
            AddPlaceholder(npc.gameObject, "Heart_NPC (Tier 2)", "stylized painterly fantasy villager quest giver, game-ready low poly, hand-painted texture, full body");
            var heart = npc.gameObject.AddComponent<Heart>();
            heart.Definition = def; heart.NpcName = npcName; heart.PromptText = "Sprechen"; heart.Range = 3f;
            if (r.Extra.TryGetValue("emote", out var em) && Enum.TryParse(em, out Emote e)) heart.EmoteWanted = e;
            var summary = new List<string>();
            npc.gameObject.AddComponent<StoryActor>().Speaker = r.Npc;

            // Aufgaben: tasks=Animals:4,Disarm:3 · bundle=eimer:3
            var tasks = new List<(HeartWay way, int n)>();
            if (r.Extra.TryGetValue("tasks", out var ts)) foreach (var t in ts.Split(',')) { var p = t.Split(':'); if (p.Length == 2 && Enum.TryParse(p[0], out HeartWay w)) tasks.Add((w, Mathf.RoundToInt(F(p[1])))); }
            int total = 0; foreach (var t in tasks) total += t.n;
            string bundle = null; int bundleTargets = 0;
            if (r.Extra.TryGetValue("bundle", out var bs)) { var p = bs.Split(':'); bundle = p[0]; bundleTargets = p.Length > 1 ? Mathf.RoundToInt(F(p[1])) : 3; total += bundleTargets; }
            int k = 0;
            Vector3 Spot(int i) { float ang = (i + 0.5f) * Mathf.PI * 2f / Mathf.Max(1, total) + 0.4f; float rad = def.Radius * (0.4f + 0.25f * (i % 2)); return G(r.X + Mathf.Cos(ang) * rad, r.Z + Mathf.Sin(ang) * rad); }
            foreach (var t in tasks)
            {
                summary.Add(t.way.ToString());
                for (int i = 0; i < t.n; i++, k++)
                {
                    var tgo = Child(root, "HeartTask_" + r.Id + "_" + t.way + i, Spot(k)).transform;
                    var vis = Child(tgo, "Visual", Vector3.zero).transform;
                    var ht = tgo.gameObject.AddComponent<HeartTask>();
                    ht.Heart = heart; ht.Way = t.way; ht.Range = 2.6f; ht.RespawnTime = 30f;
                    switch (t.way)
                    {
                        case HeartWay.Animals:
                            Animal(vis, r.Extra.TryGetValue("animal", out var an) ? an : "Bergziege");
                            ht.Kind = ZoneContent.TaskKind.Repair; ht.ChannelTime = 1.5f; ht.Label = r.Extra.TryGetValue("animalLabel", out var al) ? al : "Füttern"; break;
                        case HeartWay.Disarm:
                            Trap(vis); ht.Kind = ZoneContent.TaskKind.Repair; ht.ChannelTime = 2f; ht.Label = r.Extra.TryGetValue("trapLabel", out var tl) ? tl : "Falle entschärfen"; break;
                        case HeartWay.Revive:
                            CharacterRigs.Injured(vis, "injured_" + r.Id + "_" + i); ht.Kind = ZoneContent.TaskKind.Bandage; ht.ChannelTime = 2.5f; ht.Label = "Verbinden"; break;
                        default:
                            Part(vis, PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(0.7f, 0.7f, 0.7f), Mat("HeartCrate", new Color(0.55f, 0.38f, 0.2f)), "Crate", new Vector3(0, 20, 0));
                            ht.Kind = ZoneContent.TaskKind.Pickup; ht.ChannelTime = 0f; ht.Label = "Aufheben"; break;
                    }
                }
            }
            if (bundle != null)
            {
                summary.Add("Bundle");
                var src = Child(root, "Bundle_" + r.Id + "_" + bundle, G(npcPos.x - 2f, npcPos.z + 1.5f)).transform;
                BundleVisual(Child(src, "Visual", Vector3.zero).transform, bundle);
                var bo = src.gameObject.AddComponent<BundleObject>(); bo.BundleId = bundle; bo.Respawn = 10f;
                string label = r.Extra.TryGetValue("targetLabel", out var tlb) ? tlb : "Bündel";
                for (int i = 0; i < bundleTargets; i++, k++)
                {
                    var tg = Child(root, "BundleTarget_" + r.Id + "_" + i, Spot(k)).transform;
                    var bt = tg.gameObject.AddComponent<BundleTarget>();
                    bt.BundleId = bundle; bt.Heart = heart; bt.Label = label; bt.Value = def.TaskValue * 1.2f;
                    TargetVisual(tg, bundle, out bt.Open, out bt.Done);
                }
            }
            if (heart.EmoteWanted != Emote.None) summary.Add("Emote");
            if (def.KillValue > 0f) summary.Add("Kill");
            heart.TaskSummary = string.Join(",", summary);
            var ways = new List<HeartWay>(); foreach (var s in summary) if (Enum.TryParse(s, out HeartWay w2) && !ways.Contains(w2)) ways.Add(w2);
            ways.Add(HeartWay.Combo);   // Kombos zählen überall
            heart.Ways = ways.ToArray();
        }

        static void Animal(Transform vis, string kind)
        {
            var prefab = WorldEnemyBuilder.Critter(kind);
            if (prefab != null)
            {
                var a = (GameObject)PrefabUtility.InstantiatePrefab(prefab, vis);
                PrefabUtility.UnpackPrefabInstance(a, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var c in a.GetComponentsInChildren<AmbientCritter>(true)) UnityEngine.Object.DestroyImmediate(c);   // bleibt stehen, bis es gefüttert ist
                a.transform.localPosition = Vector3.zero; a.transform.localScale = Vector3.one * 1.3f;
            }
            Part(vis, PrimitiveType.Cylinder, new Vector3(0.7f, 0.12f, 0f), new Vector3(0.45f, 0.12f, 0.45f), Mat("FeedTrough", new Color(0.5f, 0.36f, 0.2f)), "Trough");
        }

        static void Trap(Transform vis)
        {
            var iron = Mat("TrapIron", new Color(0.28f, 0.27f, 0.26f), 0.1f);
            Part(vis, PrimitiveType.Cylinder, new Vector3(0, 0.04f, 0), new Vector3(0.8f, 0.03f, 0.8f), iron, "Plate");
            for (int i = 0; i < 6; i++) { float a = i * 60f; Part(vis, PrimitiveType.Cube, Quaternion.Euler(0, a, 0) * new Vector3(0.32f, 0.12f, 0f), new Vector3(0.05f, 0.22f, 0.05f), iron, "Tooth", new Vector3(0, a, 20f)); }
            Part(vis, PrimitiveType.Sphere, new Vector3(0, 0.1f, 0), new Vector3(0.12f, 0.12f, 0.12f), Mat("TrapTrigger", new Color(0.8f, 0.2f, 0.15f), 0.1f, new Color(1f, 0.2f, 0.1f) * 0.6f), "Trigger");
        }

        static void BundleVisual(Transform vis, string id)
        {
            switch (id)
            {
                case "eimer": Part(vis, PrimitiveType.Cylinder, new Vector3(0, 0.25f, 0), new Vector3(0.4f, 0.25f, 0.4f), Mat("BundleBucket", new Color(0.5f, 0.38f, 0.25f)), "Bucket"); break;
                case "sandsack": Part(vis, PrimitiveType.Cube, new Vector3(0, 0.2f, 0), new Vector3(0.6f, 0.35f, 0.4f), Mat("BundleSand", new Color(0.78f, 0.68f, 0.45f)), "Sack"); break;
                case "harpune": Part(vis, PrimitiveType.Cylinder, new Vector3(0, 0.7f, 0), new Vector3(0.05f, 0.7f, 0.05f), Mat("BundleSpear", new Color(0.55f, 0.58f, 0.62f), 0.1f), "Spear", new Vector3(0, 0, 12f)); break;
                default: Part(vis, PrimitiveType.Cube, new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.5f), Mat("BundleCrate", new Color(0.6f, 0.5f, 0.35f)), "Crate"); break;
            }
        }

        static void TargetVisual(Transform tg, string bundle, out GameObject open, out GameObject done)
        {
            open = Child(tg, "Open", Vector3.zero); done = Child(tg, "Done", Vector3.zero);
            switch (bundle)
            {
                case "eimer":   // Glutnest → nasser Aschefleck
                    Part(open.transform, PrimitiveType.Cylinder, new Vector3(0, 0.05f, 0), new Vector3(1.2f, 0.05f, 1.2f), Mat("Ember", new Color(0.9f, 0.35f, 0.1f), 0.1f, new Color(1f, 0.35f, 0.05f) * 2f), "Embers");
                    var l = new GameObject("Glow"); l.transform.SetParent(open.transform, false); l.transform.localPosition = Vector3.up * 0.6f; var lt = l.AddComponent<Light>(); lt.type = LightType.Point; lt.range = 4f; lt.intensity = 1.6f; lt.color = new Color(1f, 0.5f, 0.15f); l.AddComponent<FlickerLight>();
                    Part(done.transform, PrimitiveType.Cylinder, new Vector3(0, 0.03f, 0), new Vector3(1.2f, 0.03f, 1.2f), Mat("WetAsh", new Color(0.22f, 0.22f, 0.24f)), "WetAsh"); break;
                case "sandsack":   // Rutschhang → Sandsackwall
                    Part(open.transform, PrimitiveType.Cube, new Vector3(0, 0.08f, 0), new Vector3(2f, 0.15f, 0.8f), Mat("LooseSnow", new Color(0.88f, 0.92f, 0.98f)), "Slide", new Vector3(8f, 0, 0));
                    for (int i = 0; i < 3; i++) Part(done.transform, PrimitiveType.Cube, new Vector3(-0.6f + i * 0.6f, 0.2f, 0), new Vector3(0.55f, 0.35f, 0.4f), Mat("BundleSand", new Color(0.78f, 0.68f, 0.45f)), "Sack" + i); break;
                case "harpune":   // Treibgut-Sperre → aufgebrochen
                    for (int i = 0; i < 4; i++) Part(open.transform, PrimitiveType.Cylinder, new Vector3(-0.6f + i * 0.4f, 0.15f, 0), new Vector3(0.12f, 0.6f, 0.12f), Mat("Driftwood", new Color(0.42f, 0.36f, 0.28f)), "Log" + i, new Vector3(0, 0, 90f + i * 9f));
                    Part(done.transform, PrimitiveType.Cylinder, new Vector3(0.5f, 0.08f, 0.6f), new Vector3(0.12f, 0.5f, 0.12f), Mat("Driftwood", new Color(0.42f, 0.36f, 0.28f)), "Log", new Vector3(0, 40f, 90f)); break;
                case "laterne":   // dunkler Pfahl → Laterne brennt
                    Part(open.transform, PrimitiveType.Cylinder, new Vector3(0, 1f, 0), new Vector3(0.25f, 1f, 0.25f), Mat("WardStone", new Color(0.55f, 0.55f, 0.5f)), "Post");
                    Part(done.transform, PrimitiveType.Cylinder, new Vector3(0, 1f, 0), new Vector3(0.25f, 1f, 0.25f), Mat("WardStone", new Color(0.55f, 0.55f, 0.5f)), "Post");
                    Part(done.transform, PrimitiveType.Sphere, new Vector3(0, 2.2f, 0), Vector3.one * 0.45f, Mat("WardFlame", new Color(1f, 0.9f, 0.6f), 0.1f, new Color(1f, 0.85f, 0.5f) * 2.5f), "Flame");
                    var ll = new GameObject("Light").AddComponent<Light>(); ll.transform.SetParent(done.transform, false); ll.transform.localPosition = Vector3.up * 2.2f; ll.type = LightType.Point; ll.range = 8f; ll.intensity = 1.6f; ll.color = new Color(1f, 0.85f, 0.55f); ll.shadows = LightShadows.None;
                    break;
                default:
                    Part(open.transform, PrimitiveType.Sphere, new Vector3(0, 0.4f, 0), Vector3.one * 0.6f, Mat("BundleTargetOpen", new Color(0.8f, 0.5f, 0.3f)), "Target");
                    break;
            }
            AddPlaceholder(tg.gameObject, "BundleTarget_" + bundle, "stylized hand-painted world prop for a quest interaction, game-ready low poly");
        }
    }
}
