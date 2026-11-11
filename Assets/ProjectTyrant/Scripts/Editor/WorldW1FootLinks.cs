using System.Collections.Generic;
using ProjectTyrant.Landmarks;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Welt-Lauf W1: Pflichtinhalt zu Fuß. Sehenswürdigkeiten und Wegmarken, die bisher ein Reittier verlangten (Insel, Tafelberg, Lavaring),
    /// bekommen einen Fußweg (Damm, Wendelrampe, Brücke). Das Reittier bleibt die schnelle Abkürzung.
    /// </summary>
    public static partial class WorldW1Builder
    {
        const float LinkHalfWidth = 2.2f, LinkSlope = 0.24f;

        static void FootLinks(ZoneDefinition z, ZoneWorldPlan plan)
        {
            plan.FootLinks.Clear();
            var l = Procedural.BiomeZoneContent.For(z.Id); if (l == null) return;
            var gated = new List<Procedural.BzSpot>();
            foreach (var s in l.Waypoints) if (s.Expect != Traversal.None) gated.Add(s);
            foreach (var s in l.Pois) if (s.Expect != Traversal.None) gated.Add(s);
            if (gated.Count == 0) return;
            var t = BiomeZoneBuilder.Sampler(z);   // mit Plan (Operationen, Plätze, Wege)
            float wl = t.WaterLevel;
            var spawn = new Vector2(z.DefaultSpawn.x, z.DefaultSpawn.z);
            foreach (var s in gated)
            {
                var dir = spawn - s.Pos; if (dir.sqrMagnitude < 1f) dir = Vector2.up; dir.Normalize();
                float gr = Mathf.Max(8f, s.GateRadius);
                var link = new FootLinkDef { Id = s.Id, Name = s.Name };
                Vector2 start = s.Pos + dir * (gr + 10f);
                // Ufer/Rand suchen: erster trockener, flacher Punkt außerhalb des Hindernisses
                for (float d = gr + 6f; d < gr + 160f; d += 4f)
                {
                    var q = s.Pos + dir * d; float h = t.Height(q.x, q.y);
                    if (h > wl + 0.4f && Mathf.Abs(t.Height(q.x + 2f, q.y) - h) < 1.2f && Mathf.Abs(t.Height(q.x, q.y + 2f) - h) < 1.2f) { start = q; break; }
                }
                float hs = Mathf.Max(t.Height(start.x, start.y), wl + 0.8f), ht = Mathf.Max(t.Height(s.Pos.x, s.Pos.y), wl + 0.8f);
                float dist = Vector2.Distance(start, s.Pos);
                if (s.Gate == Procedural.SpotGate.Mesa && Mathf.Abs(ht - hs) / Mathf.Max(1f, dist) > LinkSlope)
                {
                    // Wendelrampe außen am Tafelberg hinauf, endet am Rand des Plateaus
                    link.Kind = "Felsrampe";
                    float r0 = gr + 5f, r1 = gr + 1.5f;
                    var sp = TerrainOps.Serpentine(s.Pos, r0, r1, hs, ht, LinkSlope * 0.9f, Mathf.Max(1, Mathf.CeilToInt((ht - hs) / LinkSlope / (Mathf.PI * 2f * r0))));
                    float a0 = Mathf.Atan2(sp[0].z - s.Pos.y, sp[0].x - s.Pos.x), a1 = Mathf.Atan2(start.y - s.Pos.y, start.x - s.Pos.x), da = a1 - a0;
                    link.Points.Add(new Vector3(start.x, hs, start.y));
                    foreach (var p in sp)
                    {
                        float dx = p.x - s.Pos.x, dz = p.z - s.Pos.y;
                        link.Points.Add(new Vector3(s.Pos.x + dx * Mathf.Cos(da) - dz * Mathf.Sin(da), p.y, s.Pos.y + dx * Mathf.Sin(da) + dz * Mathf.Cos(da)));
                    }
                    link.Points.Add(new Vector3(s.Pos.x, ht, s.Pos.y));
                }
                else
                {
                    link.Kind = s.Gate == Procedural.SpotGate.LavaRing ? "Steinbrücke" : "Bohlenweg";
                    link.Points.Add(new Vector3(start.x, hs, start.y));
                    link.Points.Add(new Vector3(s.Pos.x, ht, s.Pos.y));
                }
                plan.FootLinks.Add(link);
            }
            // Prüfen: jetzt zu Fuß erreichbar?
            var t2 = BiomeZoneBuilder.Sampler(z);
            bool lava = z.Biome != null && z.Biome.Ambient == AmbientKind.Embers;
            var grid = ReachabilityGrid.FromSampler(t2, 4f, lava);
            var cache = new Dictionary<Traversal, bool[]>();
            foreach (var s in gated)
            {
                bool ok = ReachabilityModel.Required(grid, spawn, s.Pos, t2.WaterLevel, out var req, 6f, cache);
                LastReport.Add(z.Id + ": Fußweg zu " + s.Name + " – " + (ok && req == Traversal.None ? "zu Fuß erreichbar" : "NICHT zu Fuß (" + (ok ? ReachabilityModel.Label(req) : "unerreichbar") + ")"));
                if (!(ok && req == Traversal.None)) plan.FootLinks.RemoveAll(f => f.Id == s.Id);
            }
        }

        static void ApplyFootLinks(ZoneWorldPlan plan, BiomeTerrain t)
        {
            foreach (var f in plan.FootLinks)
                for (int i = 1; i < f.Points.Count; i++)
                    t.Road(new Vector2(f.Points[i - 1].x, f.Points[i - 1].z), new Vector2(f.Points[i].x, f.Points[i].z), f.Points[i - 1].y, f.Points[i].y, LinkHalfWidth);
        }

        static void BuildFootLink(FootLinkDef f)
        {
            if (f.Points.Count < 2) return;
            var root = Child(_root, "FootLink_" + f.Id, f.Points[0]).transform;
            var wood = Mat("W1_Plank", new Color(0.48f, 0.34f, 0.22f), 0.35f);
            var stone = Mat("W1_BridgeStone", new Color(0.46f, 0.42f, 0.4f), 0.3f);
            bool planks = f.Kind == "Bohlenweg";
            for (int i = 1; i < f.Points.Count; i++)
            {
                var a = f.Points[i - 1]; var b = f.Points[i];
                float len = new Vector2(b.x - a.x, b.z - a.z).magnitude; int n = Mathf.Max(1, Mathf.FloorToInt(len / (planks ? 1.2f : 3f)));
                float yaw = Mathf.Atan2(b.x - a.x, b.z - a.z) * Mathf.Rad2Deg;
                for (int k = 0; k <= n; k++)
                {
                    var p = Vector3.Lerp(a, b, k / (float)n); p.y = Ground(p.x, p.z) + 0.06f;
                    if (f.Kind == "Felsrampe") { if (k % 3 == 0) Part(_root, PrimitiveType.Cube, p + Vector3.up * 0.4f, new Vector3(0.35f, 0.8f, 0.35f), stone, "RampPost", new Vector3(0, yaw, 0)); continue; }
                    Part(_root, PrimitiveType.Cube, p, planks ? new Vector3(LinkHalfWidth * 1.6f, 0.12f, 0.9f) : new Vector3(LinkHalfWidth * 2f, 0.3f, 2.8f), planks ? wood : stone, planks ? "Plank" : "BridgeStone", new Vector3(0, yaw, 0));
                }
            }
            var s0 = f.Points[0]; s0.y = Ground(s0.x, s0.z);
            Sign(_root, s0 + new Vector3(2.5f, 0, 2.5f), f.Kind, "zu Fuß zu " + f.Name, new Color(0.9f, 0.85f, 0.6f));
        }
    }
}
