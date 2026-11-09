using System.Collections.Generic;
using ProjectTyrant.Procedural;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Lauf 4 / M16: Inhalte der Biom-Zonen aus <see cref="BiomeZoneContent"/> – Gelände-Sperren (Tafelberg/Insel/Lava-Ring),
    /// Wegenetz (Prim-Baum, umgeht Sperren), eingeebnete Plätze, Zonenabschluss-Objekte, Einblicke, Gegnerlager, Tiere,
    /// Gebiete und die Erreichbarkeits-Prüfung je Marker (Ergebnis in <see cref="ZoneObjectDefinition.Requires"/>).
    /// </summary>
    public static partial class BiomeZoneBuilder
    {
        struct Gate { public SpotGate Kind; public Vector2 C; public float R, Outer, Top; }
        static readonly List<Gate> _gates = new List<Gate>();
        static readonly List<(Vector2 c, float r)> _keepFree = new List<(Vector2, float)>();
        public static readonly List<string> LastReachReport = new List<string>();
        public static int LastReachErrors;

        static Gate? GateAt(Vector2 p)
        {
            foreach (var g in _gates) if (Vector2.Distance(g.C, p) <= g.R + 1f) return g;
            return null;
        }

        static float SpotFlatRadius(BzLayout l, BzSpot s)
        {
            foreach (var w in l.Waypoints) if (w == s) return 9f;
            foreach (var w in l.Pois) if (w == s) return 16f;
            foreach (var w in l.Vistas) if (w == s) return 11f;
            foreach (var w in l.Challenges) if (w == s) return 9f;
            return 5f;
        }

        static partial void ExtraFlats(ZoneDefinition z, BiomeTerrain t)
        {
            _gates.Clear(); _keepFree.Clear();
            ExtraFreeCheck = null;
            var l = BiomeZoneContent.For(z.Id);
            if (l == null) return;
            float water = t.WaterLevel;
            // 1) Sperren
            foreach (var s in l.AllSpots())
            {
                if (s.Gate == SpotGate.None || GateAt(s.Pos).HasValue) continue;
                var g = new Gate { Kind = s.Gate, C = s.Pos, R = s.GateRadius };
                switch (s.Gate)
                {
                    case SpotGate.Mesa:
                        g.Top = RingMax(t, s.Pos, g.R + 3f, g.R + 16f) + 12.5f; g.Outer = g.R + 4f;   // über dem höchsten Umland → nur Springer
                        t.Mesa(s.Pos, g.R, g.Top);
                        break;
                    case SpotGate.Island:
                        g.Top = water + 1.6f; g.Outer = g.R + 30f;
                        t.Pool(s.Pos, g.R + 24f, 3.2f);
                        break;
                    case SpotGate.LavaRing:
                        g.Top = Mathf.Max(FlatHeight(t, s.Pos.x, s.Pos.y), water + 1.5f); g.Outer = g.R + 10f;
                        t.Ring(s.Pos, g.R, g.R + 8f, 2.5f);
                        break;
                }
                _gates.Add(g);
            }
            // 2) Wegenetz (Prim): Spawn, Portale und alle frei erreichbaren Inhalte
            var nodes = new List<Vector2> { new Vector2(z.DefaultSpawn.x, z.DefaultSpawn.z) };
            foreach (var p in z.Portals) nodes.Add(new Vector2(p.Position.x, p.Position.z));
            foreach (var s in l.AllSpots()) if (s.Gate == SpotGate.None) nodes.Add(s.Pos);
            foreach (var h in l.Hearts) nodes.Add(h.Center);
            // Ufer-/Rand-Knoten der Sperren, damit man bis an Insel/Lava/Tafelberg herankommt
            foreach (var g in _gates) nodes.Add(g.C + (Vector2.zero - g.C).normalized * (g.Outer + 6f));
            var heights = new float[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) heights[i] = FlatHeight(t, nodes[i].x, nodes[i].y);
            var inTree = new bool[nodes.Count]; inTree[0] = true;
            for (int added = 1; added < nodes.Count; added++)
            {
                int bi = -1, bj = -1; float best = float.MaxValue;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (!inTree[i]) continue;
                    for (int j = 0; j < nodes.Count; j++)
                    {
                        if (inTree[j]) continue;
                        float d = Vector2.Distance(nodes[i], nodes[j]);
                        if (d >= best || CrossesGate(nodes[i], nodes[j])) continue;
                        best = d; bi = i; bj = j;
                    }
                }
                if (bj < 0) break;
                inTree[bj] = true;
                t.Road(nodes[bi], nodes[bj], heights[bi], heights[bj]);
            }
            // 3) Plätze
            foreach (var s in l.AllSpots())
            {
                var g = GateAt(s.Pos);
                if (g.HasValue && g.Value.Kind == SpotGate.Mesa) { _keepFree.Add((s.Pos, 7f)); continue; }   // Tafelberg ist oben flach
                float r = SpotFlatRadius(l, s);
                float h = g.HasValue ? g.Value.Top : FlatHeight(t, s.Pos.x, s.Pos.y);
                t.Flatten(s.Pos, r, h);
                _keepFree.Add((s.Pos, r + 3f));
            }
            foreach (var g in _gates) if (g.Kind == SpotGate.Island) t.Flatten(g.C, g.R + 6f, g.Top);
            foreach (var hz in l.Hearts) { t.Flatten(hz.NpcPos, 6f, FlatHeight(t, hz.NpcPos.x, hz.NpcPos.y)); _keepFree.Add((hz.NpcPos, 8f)); _keepFree.Add((hz.Center, 14f)); }
            ExtraFreeCheck = (zz, x, y) =>
            {
                var p = new Vector2(x, y);
                foreach (var k in _keepFree) if ((p - k.c).sqrMagnitude < k.r * k.r) return false;
                return t.RoadDistance(x, y) > 2.5f;
            };
        }

        static float RingMax(BiomeTerrain t, Vector2 c, float r0, float r1)
        {
            float m = float.MinValue;
            for (int i = 0; i < 24; i++)
            {
                float a = i * Mathf.PI / 12f;
                for (float r = r0; r <= r1; r += 4f) m = Mathf.Max(m, t.RawHeight(c.x + Mathf.Cos(a) * r, c.y + Mathf.Sin(a) * r));
            }
            return m;
        }

        static bool CrossesGate(Vector2 a, Vector2 b)
        {
            foreach (var g in _gates)
            {
                Vector2 ab = b - a; float tt = Mathf.Clamp01(Vector2.Dot(g.C - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                if (Vector2.Distance(a + ab * tt, g.C) < g.Outer + 5f) return true;
            }
            return false;
        }

        // ---------------- Inhalte ----------------
        static string DefDir(ZoneDefinition z) => DataDir + "/Zone/" + z.Id;

        static ZoneObjectDefinition Def(ZoneDefinition z, string id, MapMarkerKind kind, string name, string desc, Vector3 pos, float radius, int xp, int karma = 0, int copper = 0, int hp = 0)
        {
            string path = DefDir(z) + "/Zone_" + id + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<ZoneObjectDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<ZoneObjectDefinition>(); AssetDatabase.CreateAsset(d, path); }
            d.Id = id; d.Kind = kind; d.DisplayName = name; d.Description = desc; d.Position = pos; d.Radius = radius;
            d.Experience = xp; d.Karma = karma; d.Copper = copper; d.HeroPoints = hp; d.HeartTask = null; d.KillValue = d.TaskValue = 0f; d.Requires = Traversal.None;
            EditorUtility.SetDirty(d);
            return d;
        }

        /// <summary>Belohnungen wachsen mit der Gebietsstufe wie die EP-Kurve (Stufe 8 = Faktor 1).</summary>
        static float RewardScale(int level) => (400f + 120f * (level - 1)) / (400f + 120f * 7f);

        static partial void PopulateZone(ZoneDefinition z, BiomeTerrain t, Transform env, ZoneRoot root)
        {
            var l = BiomeZoneContent.For(z.Id);
            if (l == null) return;
            EnsureFolder(DataDir + "/Zone"); EnsureFolder(DefDir(z));
            ZoneContent.HeightOverride = t.Height;
            try
            {
                var content = Child(root.transform, "ZoneContent", Vector3.zero).transform;
                var npcRoot = Child(root.transform, "NPCs", Vector3.zero).transform;
                var defs = new List<(ZoneObjectDefinition def, BzSpot spot)>();
                Vector3 G(Vector2 p, float y = 0f) => new Vector3(p.x, t.Height(p.x, p.y) + y, p.y);

                // Wegmarken
                var crystal = Mat("WaypointCrystal", new Color(0.45f, 0.75f, 1f), 0.1f, new Color(0.3f, 0.6f, 1f) * 2.2f);
                var stone = Mat("WaypointStone", new Color(0.62f, 0.62f, 0.66f));
                z.Waypoints = new List<ZoneWaypointInfo>();
                Transform first = null; float firstD = float.MaxValue;
                foreach (var w in l.Waypoints)
                {
                    var def = Def(z, w.Id, MapMarkerKind.Waypoint, w.Name, w.Desc, G(w.Pos), ZoneContent.WaypointDiscoverRadius, Mathf.RoundToInt(120 * RewardScale(w.Level)));
                    var go = Child(content, "Waypoint_" + w.Id, G(w.Pos));
                    ZoneContentBuilder.BuildMonument(go.transform, stone, crystal);
                    var wp = go.AddComponent<Waypoint>(); wp.Definition = def; wp.PromptText = "Reisen"; wp.Range = 3.5f;
                    z.Waypoints.Add(new ZoneWaypointInfo { Id = w.Id, Name = w.Name, Position = G(w.Pos) });
                    float d = Vector2.Distance(w.Pos, new Vector2(z.DefaultSpawn.x, z.DefaultSpawn.z));
                    if (d < firstD) { firstD = d; first = go.transform; }
                    defs.Add((def, w));
                }
                if (first != null) root.Waypoint = first;

                // Sehenswürdigkeiten (+ Landmarke) und Gebiete
                z.Regions = new List<ZoneRegionInfo>();
                int pi = 0;
                foreach (var p in l.Pois)
                {
                    var def = Def(z, p.Id, MapMarkerKind.PointOfInterest, p.Name, p.Desc, G(p.Pos, 1f), ZoneContent.PoiRadius, Mathf.RoundToInt(150 * RewardScale(p.Level)));
                    var go = Child(content, "POI_" + p.Id, G(p.Pos, 1f));
                    go.AddComponent<PointOfInterest>().Definition = def;
                    Landmark(content, z, t, p, pi++);
                    z.Regions.Add(new ZoneRegionInfo { Name = p.Name, Center = p.Pos, Radius = 110f, LevelMin = Mathf.Clamp(p.Level - 1, z.LevelMin, z.LevelMax), LevelMax = Mathf.Clamp(p.Level + 1, z.LevelMin, z.LevelMax) });
                    defs.Add((def, p));
                }

                // Aussichtspunkte
                var vstone = Mat("VistaStone", new Color(0.58f, 0.55f, 0.5f));
                var gold = Mat("VistaGold", new Color(1f, 0.8f, 0.35f), 0.1f, new Color(1f, 0.7f, 0.2f) * 1.6f);
                foreach (var v in l.Vistas)
                {
                    var toC = (-v.Pos).normalized; if (toC.sqrMagnitude < 0.1f) toC = Vector2.up;
                    var spot = new ZoneContent.VistaSpot { Id = v.Id, Name = v.Name, Pos = v.Pos, StairDir = toC, Height = 9f };
                    var top = ZoneContent.PlatformTop(spot);
                    var def = Def(z, v.Id, MapMarkerKind.Vista, v.Name, "Erklimme die Treppe und genieße die Aussicht.", top, 3f, Mathf.RoundToInt(250 * RewardScale(v.Level)));
                    var vr = Child(content, "Vista_" + v.Id, Vector3.zero).transform;
                    float ground = t.Height(v.Pos.x, v.Pos.y);
                    float h = spot.Height + 1.5f;
                    Part(vr, PrimitiveType.Cube, new Vector3(v.Pos.x, top.y - h * 0.5f, v.Pos.y), new Vector3(ZoneContent.PlatformHalf * 2f, h, ZoneContent.PlatformHalf * 2f), vstone, "Platform", null, true);
                    float yaw = Mathf.Atan2(toC.x, toC.y) * Mathf.Rad2Deg;
                    foreach (var s in ZoneContent.StairSteps(spot))
                    {
                        float g0 = Mathf.Min(t.Height(s.x, s.z), ground) - 0.6f;
                        float sh = s.y - g0;
                        Part(vr, PrimitiveType.Cube, new Vector3(s.x, g0 + sh * 0.5f, s.z), new Vector3(2.0f, sh, ZoneContent.StairTread + 0.02f), vstone, "Step", new Vector3(0, yaw, 0), true);
                    }
                    var stele = Child(vr, "Stele", top).transform;
                    Part(stele, PrimitiveType.Cube, new Vector3(0, 0.9f, 0), new Vector3(0.35f, 1.8f, 0.35f), vstone, "Pillar", null, true);
                    Part(stele, PrimitiveType.Sphere, new Vector3(0, 1.95f, 0), new Vector3(0.45f, 0.45f, 0.45f), gold, "Orb").AddComponent<Bobber>();
                    AddPlaceholder(stele.gameObject, "Vista_Stele (Tier 1)", "stylized painterly fantasy viewpoint stele, slender carved stone pillar topped with a glowing golden orb, game-ready low poly, hand-painted texture");
                    ZoneContentBuilder.UseMeshy(stele, "PT_Meshy_VistaStele", 2.4f, Vector3.zero);
                    var vista = stele.gameObject.AddComponent<Vista>();
                    vista.Definition = def; vista.PromptText = "Aussicht betrachten"; vista.Range = 3f;
                    defs.Add((def, v));
                }

                // Heldenherausforderungen
                var sstone = Mat("ShrineStone", new Color(0.5f, 0.52f, 0.56f));
                var blue = Mat("ShrineGlow", new Color(0.45f, 0.8f, 1f), 0.1f, new Color(0.3f, 0.7f, 1f) * 1.8f);
                var curse = Mat("RelicCurse", new Color(0.35f, 0.15f, 0.45f), 0.1f, new Color(0.6f, 0.2f, 1f) * 1.6f);
                var red = Mat("ChallengeBanner", new Color(0.75f, 0.15f, 0.12f));
                var trash = DataBuilder.Archetype(l.Archetypes[0]);
                var vet = DataBuilder.Archetype(l.Archetypes[2]);
                if (vet == null || vet.Tier != EnemyTier.Veteran) vet = DataBuilder.Archetype(l.Archetypes[1]);
                foreach (var c in l.Challenges)
                {
                    var top = G(c.Pos);
                    var def = Def(z, c.Id, MapMarkerKind.HeroChallenge, c.Name, c.Desc, top, 3f, Mathf.RoundToInt(200 * RewardScale(c.Level)), 0, 0, ZoneContent.ChallengeHeroPoints);
                    var shrine = Child(content, "Challenge_" + c.Id, top).transform;
                    switch (c.Kind)
                    {
                        case ZoneContent.ChallengeKind.Relic:
                            Part(shrine, PrimitiveType.Cube, new Vector3(0, 0.5f, 0), new Vector3(0.9f, 1f, 0.9f), curse, "Relic", new Vector3(0, 30, 15), true); break;
                        case ZoneContent.ChallengeKind.Combat:
                            Part(shrine, PrimitiveType.Cylinder, new Vector3(0, 1.5f, 0), new Vector3(0.12f, 1.5f, 0.12f), sstone, "Pole", null, true);
                            Part(shrine, PrimitiveType.Cube, new Vector3(0.45f, 2.4f, 0), new Vector3(0.8f, 0.9f, 0.04f), red, "Banner"); break;
                        default:
                            Part(shrine, PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(1.1f, 0.3f, 1.1f), sstone, "Base", null, true);
                            Part(shrine, PrimitiveType.Cube, new Vector3(0, 1.3f, 0), new Vector3(0.4f, 0.9f, 0.4f), blue, "Crystal", new Vector3(0, 45, 0)).AddComponent<Bobber>(); break;
                    }
                    AddPlaceholder(shrine.gameObject, "HeroChallenge_Shrine (Tier 1)", "stylized painterly fantasy hero challenge shrine, small hexagonal stone altar with a floating light-blue crystal, game-ready low poly, hand-painted texture");
                    if (c.Kind == ZoneContent.ChallengeKind.Communion) ZoneContentBuilder.UseMeshy(shrine, "PT_Meshy_ChallengeShrine", 1.8f, Vector3.zero);
                    var hc = shrine.gameObject.AddComponent<HeroChallenge>();
                    hc.Definition = def; hc.Kind = c.Kind; hc.Range = 3f;
                    hc.ChannelTime = c.Kind == ZoneContent.ChallengeKind.Communion ? 5f : 0f;
                    hc.PromptText = c.Kind == ZoneContent.ChallengeKind.Communion ? "Kommunizieren" : "Interagieren";
                    hc.Challenger = vet; hc.Minion = trash;
                    defs.Add((def, c));
                }

                // Beherrschungs-Einblicke
                var insightMat = Mat("InsightCrystal", new Color(0.3f, 0.9f, 0.7f), 0.1f, new Color(0.2f, 1f, 0.7f) * 2f);
                foreach (var s in l.Insights)
                {
                    var pos = G(s.Pos);
                    var def = Def(z, s.Id, MapMarkerKind.Insight, s.Name, s.Desc, pos, 3f, Mathf.RoundToInt(100 * RewardScale(s.Level)));
                    var go = Child(content, "Insight_" + s.Id, pos).transform;
                    Part(go, PrimitiveType.Cylinder, new Vector3(0, 0.2f, 0), new Vector3(1.4f, 0.2f, 1.4f), sstone, "Base", null, true);
                    var cr = Part(go, PrimitiveType.Cube, new Vector3(0, 1.4f, 0), new Vector3(0.45f, 1.1f, 0.45f), insightMat, "Crystal", new Vector3(0, 45, 20));
                    cr.AddComponent<Bobber>();
                    var light = new GameObject("Glow").AddComponent<Light>(); light.transform.SetParent(go, false); light.transform.localPosition = new Vector3(0, 1.6f, 0);
                    light.type = LightType.Point; light.color = new Color(0.3f, 1f, 0.75f); light.range = 6f; light.intensity = 1.8f; light.shadows = LightShadows.None;
                    AddPlaceholder(go.gameObject, "Mastery_Insight (Tier 1)", "stylized painterly fantasy floating teal crystal on a small stone base, game-ready low poly, hand-painted texture");
                    ZoneContentBuilder.UseMeshy(go, "PT_Meshy_InsightCrystal", 2.2f, Vector3.zero);
                    var mi = go.gameObject.AddComponent<MasteryInsight>();
                    mi.Definition = def; mi.PromptText = "Untersuchen"; mi.Range = 3f; mi.ChannelTime = 2f;
                    defs.Add((def, s));
                }

                // Herzen
                BuildHearts(z, t, l, content, npcRoot, defs);

                // Gegnerlager & Tiere
                foreach (var camp in l.Camps)
                {
                    var a = DataBuilder.Archetype(camp.Archetype);
                    if (a == null || a.Prefab == null) continue;
                    for (int i = 0; i < camp.Count; i++)
                    {
                        float ang = i * Mathf.PI * 2f / Mathf.Max(1, camp.Count) + camp.Pos.x * 0.01f;
                        var p = camp.Pos + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (camp.Count > 1 ? 4f : 0f);
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(a.Prefab, npcRoot);
                        go.transform.position = G(p, 0.05f);
                        go.transform.rotation = Quaternion.Euler(0, ang * Mathf.Rad2Deg, 0);
                        go.GetComponent<NpcBrain>().Archetype = a;
                    }
                }
                var critters = Child(root.transform, "Critters", Vector3.zero).transform;
                foreach (var cr in l.Critters)
                {
                    var prefab = WorldEnemyBuilder.Critter(cr.Kind);
                    if (prefab == null) continue;
                    for (int i = 0; i < cr.Count; i++)
                    {
                        var off = new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f)) * (3f + i * 2.5f);
                        var p = cr.Pos + off;
                        if (t.Height(p.x, p.y) < t.WaterLevel + 0.2f) p = cr.Pos;
                        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, critters);
                        go.transform.position = G(p);
                        go.transform.rotation = Quaternion.Euler(0, i * 77f, 0);
                    }
                }

                MasteryBuilder.PlaceEnvironment(l, root.transform, (p2, y) => G(p2, y));

                // Ereignisse (M19) + Vorratskisten für Verteidigungs-Ereignisse
                root.Events = ZoneEventBuilder.Build(z, t.Height, out var cratePos);
                var crates = new List<Combatant>();
                foreach (var cp in cratePos)
                {
                    var c = (GameObject)PrefabUtility.InstantiatePrefab(DataBuilder.CratePrefab, npcRoot);
                    c.transform.position = cp; c.name = "Ereignis_Vorrat";
                    crates.Add(c.GetComponent<Combatant>());
                }
                root.Crates = crates.ToArray();

                // Zonenabschluss-Ids + Erreichbarkeit
                z.CompletionIds = new List<string>();
                foreach (var d in defs) if (d.def.Kind != MapMarkerKind.Insight) z.CompletionIds.Add(d.def.Id);
                ValidateReachability(z, t, defs);
            }
            finally { ZoneContent.HeightOverride = null; }
        }

        static void BuildHearts(ZoneDefinition z, BiomeTerrain t, BzLayout l, Transform content, Transform npcRoot, List<(ZoneObjectDefinition, BzSpot)> defs)
        {
            Vector3 G(Vector2 p, float y = 0f) => new Vector3(p.x, t.Height(p.x, p.y) + y, p.y);
            var robe = Mat("HeartNpcRobe", new Color(0.85f, 0.65f, 0.25f));
            var skin = Mat("NpcSkin", new Color(0.9f, 0.72f, 0.58f));
            var crate = Mat("HeartCrate", new Color(0.55f, 0.38f, 0.2f));
            var fence = Mat("HeartFence", new Color(0.45f, 0.32f, 0.2f));
            var hurt = Mat("InjuredGuard", new Color(0.35f, 0.45f, 0.7f));
            var db = ItemBuilder.Database;
            foreach (var h in l.Hearts)
            {
                float sc = RewardScale(h.Level);
                var def = Def(z, h.Id, MapMarkerKind.Heart, h.Name, h.Task, G(h.Center), h.Radius, Mathf.RoundToInt(400 * sc), Mathf.RoundToInt(250 * sc), Mathf.RoundToInt(120 * sc));
                def.HeartTask = h.Task; def.KillValue = h.KillValue; def.TaskValue = h.TaskValue;
                var npc = Child(content, "Heart_" + h.Id, G(h.NpcPos)).transform;
                CharacterRigs.Npc(npc, robe.GetColor("_BaseColor"), h.Id);
                ZoneContentBuilder.Nameplate(npc, h.Npc, "Herz: " + h.Name, new Color(1f, 0.8f, 0.3f));
                AddPlaceholder(npc.gameObject, "Heart_NPC (Tier 2)", "stylized painterly fantasy villager quest giver, game-ready low poly, hand-painted texture, full body");
                var heart = npc.gameObject.AddComponent<Heart>();
                heart.Definition = def; heart.NpcName = h.Npc; heart.PromptText = "Sprechen"; heart.Range = 3f;
                int ti = 0, total = 0; foreach (var tk in h.Tasks) total += tk.count;
                foreach (var tk in h.Tasks)
                    for (int i = 0; i < tk.count; i++, ti++)
                    {
                        float ang = (ti + 0.5f) * Mathf.PI * 2f / total;
                        var p = h.Center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (h.Radius * (0.45f + 0.25f * (ti % 2)));
                        var tgo = Child(content, "HeartTask_" + h.Id + "_" + tk.kind + i, G(p)).transform;
                        var vis = Child(tgo, "Visual", Vector3.zero).transform;
                        switch (tk.kind)
                        {
                            case ZoneContent.TaskKind.Pickup: Part(vis, PrimitiveType.Cube, new Vector3(0, 0.35f, 0), new Vector3(0.7f, 0.7f, 0.7f), crate, "Crate", new Vector3(0, 20, 0)); break;
                            case ZoneContent.TaskKind.Repair:
                                Part(vis, PrimitiveType.Cube, new Vector3(-0.9f, 0.6f, 0), new Vector3(0.15f, 1.2f, 0.15f), fence, "Post");
                                Part(vis, PrimitiveType.Cube, new Vector3(0.9f, 0.6f, 0), new Vector3(0.15f, 1.2f, 0.15f), fence, "Post");
                                Part(vis, PrimitiveType.Cube, new Vector3(0.1f, 0.45f, 0), new Vector3(1.9f, 0.12f, 0.08f), fence, "BrokenPlank", new Vector3(0, 0, 18)); break;
                            case ZoneContent.TaskKind.Bandage: CharacterRigs.Injured(vis, "injured_" + h.Id + "_" + i); break;
                        }
                        var task = tgo.gameObject.AddComponent<HeartTask>();
                        task.Heart = heart; task.Kind = tk.kind; task.Range = 2.6f;
                        task.ChannelTime = tk.kind == ZoneContent.TaskKind.Pickup ? 0f : tk.kind == ZoneContent.TaskKind.Repair ? 2f : 2.5f;
                        task.PromptText = tk.label;
                    }
                var vdef = AssetDatabase.LoadAssetAtPath<VendorDefinition>(ItemBuilder.VendorDir + "/Vendor_" + h.VendorId + ".asset");
                if (vdef == null) { vdef = ScriptableObject.CreateInstance<VendorDefinition>(); AssetDatabase.CreateAsset(vdef, ItemBuilder.VendorDir + "/Vendor_" + h.VendorId + ".asset"); }
                vdef.Id = h.VendorId; vdef.DisplayName = h.VendorName; vdef.Kind = VendorKind.Karma;
                vdef.Stock = new List<VendorEntry>();
                foreach (var st in h.Stock) { var it = db != null ? db.Get(st.item) : null; if (it != null) vdef.Stock.Add(ZoneContentBuilder.K(it, st.karma, st.count)); }
                EditorUtility.SetDirty(vdef);
                var vp = h.NpcPos + new Vector2(2.5f, 1.5f);
                var vn = ItemBuilder.Place(content, vdef, G(vp), 180f, new Color(0.55f, 0.45f, 0.2f));
                if (vn != null) vn.RequiredCompletion = h.Id;
                foreach (var md in MountBuilder.LoadAll())
                    if (md != null && md.RequiredHeart == h.Id && md.VendorZone == z.Id)
                        MountBuilder.PlaceStallmeister(content, md.Kind, G(h.NpcPos + new Vector2(-4f, 3f)), 200f, md.Kind == MountKind.Skimmer ? "Stallmeister Ole" : "Stallmeisterin Brynja");
                defs.Add((def, new BzSpot { Id = h.Id, Name = h.Name, Pos = h.Center, Level = h.Level }));
            }
        }

        /// <summary>Landmarke einer Sehenswürdigkeit (Primitive, je Biom getönt; Variante nach Index).</summary>
        static void Landmark(Transform parent, ZoneDefinition z, BiomeTerrain t, BzSpot p, int idx)
        {
            var b = z.Biome;
            var stone = Mat("Landmark_" + z.Id, Color.Lerp(b.Cliff, b.GroundHigh, 0.35f));
            var wood = Mat("LandmarkWood_" + z.Id, Color.Lerp(b.TrunkColor, new Color(0.45f, 0.32f, 0.2f), 0.5f));
            var accent = Mat("LandmarkAccent_" + z.Id, b.Ambient == AmbientKind.Embers ? new Color(1f, 0.45f, 0.15f) : b.Ambient == AmbientKind.Wind ? new Color(0.6f, 0.8f, 1f) : new Color(0.6f, 0.85f, 0.4f), 0.1f,
                b.Ambient == AmbientKind.Embers ? new Color(1f, 0.4f, 0.1f) * 1.5f : (Color?)null);
            var root = Child(parent, "Landmark_" + p.Id, new Vector3(p.Pos.x, t.Height(p.Pos.x, p.Pos.y), p.Pos.y)).transform;
            switch (idx % 4)
            {
                case 0:   // Hütten auf Pfählen / Zelte
                    for (int i = 0; i < 3; i++)
                    {
                        var c = new Vector3(Mathf.Cos(i * 2.1f) * 8f, 0, Mathf.Sin(i * 2.1f) * 8f);
                        Part(root, PrimitiveType.Cube, c + new Vector3(0, 1.6f, 0), new Vector3(3.2f, 2.2f, 3.2f), wood, "Hut", new Vector3(0, i * 40f, 0), true);
                        Part(root, PrimitiveType.Cube, c + new Vector3(0, 3.1f, 0), new Vector3(3.8f, 0.6f, 3.8f), accent, "Roof", new Vector3(0, i * 40f + 45f, 0));
                    }
                    break;
                case 1:   // schiefer Turm
                    Part(root, PrimitiveType.Cylinder, new Vector3(0, 4f, 0), new Vector3(4f, 4f, 4f), stone, "Tower", new Vector3(8, 0, 6), true);
                    Part(root, PrimitiveType.Cylinder, new Vector3(0.6f, 8.4f, 0.5f), new Vector3(4.6f, 0.4f, 4.6f), accent, "Crown", new Vector3(8, 0, 6));
                    for (int i = 0; i < 5; i++) Part(root, PrimitiveType.Cube, new Vector3(Mathf.Cos(i) * 7f, 0.5f, Mathf.Sin(i) * 7f), new Vector3(1.4f, 1f, 1.1f), stone, "Rubble", new Vector3(0, i * 30, 10), true);
                    break;
                case 2:   // Säulenkreis
                    for (int i = 0; i < 7; i++)
                    {
                        float a = i * Mathf.PI * 2f / 7f;
                        Part(root, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 9f, 2.5f, Mathf.Sin(a) * 9f), new Vector3(1f, 2.5f, 1f), stone, "Pillar", null, true);
                    }
                    Part(root, PrimitiveType.Sphere, new Vector3(0, 1.5f, 0), new Vector3(1.6f, 1.6f, 1.6f), accent, "Core").AddComponent<Bobber>();
                    break;
                default:  // Steg/Esse/Felsbogen
                    Part(root, PrimitiveType.Cube, new Vector3(-4f, 3f, 0), new Vector3(1.6f, 6f, 1.6f), stone, "ArchL", null, true);
                    Part(root, PrimitiveType.Cube, new Vector3(4f, 3f, 0), new Vector3(1.6f, 6f, 1.6f), stone, "ArchR", null, true);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 6.4f, 0), new Vector3(10f, 1.2f, 2f), stone, "ArchTop", null, true);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.3f, 6f), new Vector3(3f, 0.6f, 6f), wood, "Platform", null, true);
                    break;
            }
            AddPlaceholder(root.gameObject, "Landmark_" + p.Id + " (Tier 2)", "stylized painterly fantasy landmark: " + p.Name + ", game-ready low poly, hand-painted texture");
        }

        /// <summary>Flutfüllung je Reittier-Kombination; schreibt <see cref="ZoneObjectDefinition.Requires"/> und protokolliert Abweichungen zur erwarteten Sperre.</summary>
        static void ValidateReachability(ZoneDefinition z, BiomeTerrain t, List<(ZoneObjectDefinition def, BzSpot spot)> defs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool lava = z.Biome.Ambient == AmbientKind.Embers;
            var grid = ReachabilityGrid.FromSampler(t, 4f, lava);
            var cache = new Dictionary<Traversal, bool[]>();
            var start = new Vector2(z.DefaultSpawn.x, z.DefaultSpawn.z);
            int errors = 0;
            foreach (var d in defs)
            {
                bool ok = ReachabilityModel.Required(grid, start, d.spot.Pos, t.WaterLevel, out var req, 6f, cache);
                d.def.Requires = req;
                EditorUtility.SetDirty(d.def);
                var expect = d.spot.Expect;
                string line = z.Id + " · " + d.def.Id + ": " + (ok ? (req == Traversal.None ? "zu Fuß" : ReachabilityModel.Label(req)) : "UNERREICHBAR") + (ok && req == expect ? "" : "  (erwartet: " + (expect == Traversal.None ? "zu Fuß" : ReachabilityModel.Label(expect)) + ")");
                LastReachReport.Add(line);
                if (!ok || req != expect) { errors++; Debug.LogWarning("[ProjectTyrant] Erreichbarkeit " + line); }
            }
            foreach (var p in z.Portals)
            {
                bool ok = ReachabilityModel.Required(grid, start, new Vector2(p.Position.x, p.Position.z), t.WaterLevel, out var req, 6f, cache);
                if (!ok || req != Traversal.None) { errors++; Debug.LogWarning("[ProjectTyrant] Erreichbarkeit " + z.Id + " · Portal " + p.Id + ": " + (ok ? ReachabilityModel.Label(req) : "UNERREICHBAR")); }
            }
            LastReachErrors += errors;
            Debug.Log("[ProjectTyrant] Erreichbarkeit " + z.DisplayName + ": " + defs.Count + " Marker + " + z.Portals.Count + " Portale geprüft, " + errors + " Abweichungen (" + sw.ElapsedMilliseconds + " ms).");
        }
    }
}
