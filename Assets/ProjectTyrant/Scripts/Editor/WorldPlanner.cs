using System;
using System.Collections.Generic;
using ProjectTyrant.Landmarks;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Welt-Lauf W1: Planer (deterministisch). Setzt je Zone Wahrzeichen, Terrain-Operationen, Hürden, Aufstieg, Abkürzung, Geheimnisse,
    /// Lore, Schleifen und Umwelt-Szenen auf freie Flächen (Abstand zu allen bestehenden Inhalten, Wegmarken, Portalen, Story-Orten)
    /// und schreibt das Ergebnis als Daten in <see cref="ZoneDefinition.WorldPlan"/>.
    /// </summary>
    public static partial class WorldPlanner
    {
        public const int PlanVersion = 4;
        public static readonly List<string> LastReport = new List<string>();

        public class Occupancy
        {
            public readonly List<(Vector2 p, float r)> Items = new List<(Vector2, float)>();
            public void Add(Vector2 p, float r) => Items.Add((p, r));
            public bool Free(Vector2 p, float r)
            {
                foreach (var o in Items) if ((o.p - p).sqrMagnitude < (o.r + r) * (o.r + r)) return false;
                return true;
            }
            public float Clearance(Vector2 p) { float m = float.MaxValue; foreach (var o in Items) m = Mathf.Min(m, (o.p - p).magnitude - o.r); return m; }
        }

        public class Context
        {
            public ZoneDefinition Zone; public ZSpec Spec;
            public Func<float, float, float> Ground;   // Gelände ohne Plan
            public float Water; public float Size; public bool Legacy; public bool Lava;
            public Occupancy Occ = new Occupancy();
            public List<Vector3> Markers = new List<Vector3>();
            public List<Vector3> Waypoints = new List<Vector3>();
            public Vector3 Spawn;
        }

        static float Half(Context c) => c.Size * 0.5f;

        /// <summary>Freien Platz suchen (Raster 20 m, deterministisch), Bewertung frei wählbar; null wenn keiner frei ist.</summary>
        static Vector2? Spot(Context c, float r, Func<Vector2, float> score, float margin = 30f)
        {
            if (c.Legacy) margin = Mathf.Min(margin, 6f);   // Startzone: 200 m, dicht bebaut
            float h = Half(c) - r - margin;
            if (h <= 0f) return null;
            Vector2? best = null; float bs = float.MinValue;
            float step = c.Size < 400f ? 8f : 20f;
            for (float x = -h; x <= h; x += step)
                for (float z = -h; z <= h; z += step)
                {
                    var p = new Vector2(x, z);
                    if (!c.Occ.Free(p, r)) continue;
                    float s = score(p);
                    if (s > bs) { bs = s; best = p; }
                }
            return best;
        }

        static Vector3 V3(Context c, Vector2 p, Func<float, float, float> h = null) => new Vector3(p.x, (h ?? c.Ground)(p.x, p.y), p.y);

        public static ZoneWorldPlan Plan(Context c)
        {
            var plan = new ZoneWorldPlan { Version = PlanVersion };
            var s = c.Spec; var z = c.Zone;
            if (s == null) return plan;
            int lvl = z.LevelMax;
            var rnd = new System.Random(z.Seed * 7919 + 13);
            Func<float, float, float> H = (x, zz) => { float h = c.Ground(x, zz); if (!c.Legacy) h = TerrainOps.ApplyAll(plan.Ops, x, zz, h, c.Water); return h; };
            var spawn2 = new Vector2(c.Spawn.x, c.Spawn.z);

            // 1) Aufstieg: Massiv/Gipfel (Legacy: gebaute Felsnadel)
            float rv = c.Legacy ? 18f : Mathf.Clamp(c.Size * (s.Mountain ? 0.17f : 0.14f), 100f, 200f);
            float climbH = c.Legacy ? s.ClimbHeight : Mathf.Clamp(s.ClimbHeight, 150f, 300f);
            Vector2? cp = null;
            if (!c.Legacy)   // Startzone (200 m, dicht bebaut): Aufstieg am Leuchtfeuerturm statt Felsnadel
                for (float r = rv; r >= 70f && cp == null; r -= 15f) { rv = r; cp = Spot(c, r + 15f, p => (p - spawn2).magnitude * 0.6f - p.magnitude * 0.25f); }
            if (cp != null)
            {
                var cc = cp.Value;
                if (!c.Legacy) plan.Ops.Add(new TerrainOpDef { Kind = s.Mountain ? TerrainOpKind.Massif : TerrainOpKind.Peak, Center = cc, Radius = rv, Height = climbH, Yaw = rnd.Next(0, 360) });
                c.Occ.Add(cc, rv + 10f);
                plan.Climbs.Add(BuildClimb(c, plan, H, cc, rv, climbH, lvl));
            }
            else if (!c.Legacy) LastReport.Add(z.Id + ": kein Platz für einen Aufstieg (Massiv) – Aufstieg am Haupt-Wahrzeichen");

            // 2) Signatur-Operation (Krater, Klippenküste, Terrassen, Schlucht, Tafelberg, Delta, Senke)
            if (!c.Legacy)
            {
                float sr = s.Signature == TerrainOpKind.Canyon ? 160f : s.Signature == TerrainOpKind.Delta ? 120f : 85f;
                var sp = Spot(c, sr + 10f, p => -(p.magnitude - c.Size * 0.3f) * (p.magnitude - c.Size * 0.3f) * 0.001f + (p - spawn2).magnitude * 0.1f);
                if (sp != null)
                {
                    var op = new TerrainOpDef { Kind = s.Signature, Center = sp.Value, Radius = sr, Yaw = rnd.Next(0, 180) };
                    switch (s.Signature)
                    {
                        case TerrainOpKind.Crater: op.Height = 35f; op.Param = 4f; break;
                        case TerrainOpKind.CliffCoast: op.Height = 60f; break;
                        case TerrainOpKind.Terraces: op.Height = 40f; op.Param = 4f; break;
                        case TerrainOpKind.Canyon: op.Height = 45f; op.Param = 34f; break;
                        case TerrainOpKind.Mesa: op.Height = c.Ground(sp.Value.x, sp.Value.y) + 45f; op.Param = 4f; op.Radius = 55f; break;
                        case TerrainOpKind.Delta: op.Param = 2.5f; break;
                        case TerrainOpKind.Basin: op.Param = 1.8f; op.Radius = 60f; break;
                        default: op.Height = 30f; break;
                    }
                    plan.Ops.Add(op);
                    c.Occ.Add(sp.Value, op.Radius + 12f);
                    if (s.Signature == TerrainOpKind.Crater && Array.IndexOf(s.Hazards, HazardKind.Lava) >= 0)
                        plan.Hazards.Add(new HazardDef { Id = z.Id + "_kraterlava", Kind = HazardKind.Lava, Center = new Vector3(sp.Value.x, c.Water, sp.Value.y), Size = Vector2.one * sr * 1.3f, Circular = true, Keys = HazardRules.Spec(HazardKind.Lava).Keys });
                    if (s.Signature == TerrainOpKind.Canyon)
                        plan.Hazards.Add(new HazardDef { Id = z.Id + "_schlucht", Kind = HazardKind.Abyss, Center = new Vector3(sp.Value.x, c.Water, sp.Value.y), Size = new Vector2(sr * 2f, op.Param), Yaw = op.Yaw, Keys = HazardRules.Spec(HazardKind.Abyss).Keys });
                }
            }

            // 3) Haupt-Wahrzeichen (Sichtbarkeit entscheidet unter den besten Kandidaten)
            var lmMain = Place(c, plan, H, s.Main, true, z.Id + "_wz_haupt", rnd, null);
            // Kein Platz für ein Massiv (Städte): Aufstieg am Haupt-Wahrzeichen selbst
            if (plan.Climbs.Count == 0 && lmMain != null && lmMain.Stages.Count >= 3) { plan.Climbs.Add(LandmarkClimb(c, plan, lmMain, lvl)); plan.CompactZone = true; }
            if (c.Legacy) plan.CompactZone = true;
            // 4) Neben-Wahrzeichen
            int si = 0;
            foreach (var sec in s.Sec) { Place(c, plan, H, sec, false, z.Id + "_wz_" + (++si), rnd, lmMain); }

            // 5) Hürden (Becken um/neben den Wahrzeichen, Dornen, Verderbnis, Wind, Steilwand, Sandportal)
            Hazards(c, plan, H, rnd, lvl);

            // 6) Geheimnisse, Schleifen, Szenen, Lore
            Secrets(c, plan, H, rnd, lvl);
            return plan;
        }

        // ---------------- Aufstieg ----------------
        static ClimbDef BuildClimb(Context c, ZoneWorldPlan plan, Func<float, float, float> H, Vector2 cc, float rv, float climbH, int lvl)
        {
            var z = c.Zone;
            // Basis am Rand Richtung Spawn, Gipfel im Zentrum
            var dir = (new Vector2(c.Spawn.x, c.Spawn.z) - cc); if (dir.sqrMagnitude < 1f) dir = Vector2.up; dir.Normalize();
            var b2 = cc + dir * (rv + 12f);
            float g0 = c.Ground(b2.x, b2.y);
            Vector3 baseP = new Vector3(b2.x, Mathf.Max(g0, c.Water + 0.5f), b2.y);
            float peakY = c.Legacy ? c.Ground(cc.x, cc.y) + climbH : H(cc.x, cc.y);
            var peak = new Vector3(cc.x, peakY, cc.y);
            var cl = new ClimbDef { Id = z.Id + "_aufstieg", Name = c.Legacy ? "Elmhain-Nadel" : (c.Spec.Mountain ? "Gipfelpfad" : "Hochpfad"), Base = baseP, Peak = peak };
            cl.Stages = ClimbTemplate.Create(baseP, peak, lvl);
            // Stufen auf die tatsächlichen Plateaus legen (Strahl vom Rand zum Zentrum)
            float[] fr = ClimbTemplate.StageFractions;
            for (int i = 0; i < 3; i++)
            {
                float target = Mathf.Lerp(baseP.y, peak.y, fr[i]);
                Vector2 at = cc;
                if (i < 2 && !c.Legacy)
                    for (float t = 1f; t >= 0f; t -= 0.01f) { var q = cc + dir * rv * t; if (H(q.x, q.y) >= target - 0.5f) { at = q; break; } }
                else if (c.Legacy && i < 2) at = cc + dir * (rv * (1f - fr[i] * 0.5f));
                float y = c.Legacy ? Mathf.Lerp(baseP.y, peak.y, fr[i]) : H(at.x, at.y);
                cl.Stages[i].Position = new Vector3(at.x, y, at.y);
                cl.Stages[i].Checkpoints.Clear();
                var prev = i == 0 ? baseP : cl.Stages[i - 1].Position;
                int n = Mathf.Max(1, Mathf.CeilToInt((y - prev.y) / 15f));
                for (int k = 1; k <= n; k++) cl.Stages[i].Checkpoints.Add(Vector3.Lerp(prev, cl.Stages[i].Position, k / (float)n));
            }
            // Fußweg: Serpentine Basis → Stufe 1 → (Grat) Stufe 2
            var s1 = cl.Stages[0].Position; var s2 = cl.Stages[1].Position;
            float r1 = new Vector2(s1.x - cc.x, s1.z - cc.y).magnitude, r2 = new Vector2(s2.x - cc.x, s2.z - cc.y).magnitude;
            if (!c.Legacy)
            {
                var p1 = TerrainOps.Serpentine(cc, rv + 6f, Mathf.Max(r1, 8f), baseP.y, s1.y, 0.16f, Mathf.Max(1, Mathf.CeilToInt((s1.y - baseP.y) / (Mathf.PI * 2f * (rv + r1) * 0.5f * 0.14f))));
                var p2 = TerrainOps.Serpentine(cc, Mathf.Max(r1, 8f), Mathf.Max(r2, 6f), s1.y, s2.y, 0.16f, Mathf.Max(1, Mathf.CeilToInt((s2.y - s1.y) / (Mathf.PI * 2f * (r1 + r2) * 0.5f * 0.14f))));
                RotateToStart(p1, cc, b2); RotateToStart(p2, cc, new Vector2(p1[p1.Count - 1].x, p1[p1.Count - 1].z));
                cl.FootPath.AddRange(p1); cl.FootPath.AddRange(p2);
                cl.Stages[0].Position = p1[p1.Count - 1]; cl.Stages[1].Position = p2[p2.Count - 1];
            }
            // Springer-Abkürzung: Vorsprünge direkt den Hang hinauf (≤ 13 m je Satz)
            // (Basis → Hochplateau = Stufe 2: Springer-Tor mit Grat-Alternative)
            var sStart = baseP; var sEnd = cl.Stages[1].Position;
            int steps = Mathf.Max(2, Mathf.CeilToInt((sEnd.y - sStart.y) / 12f));
            for (int k = 1; k <= steps; k++)
            {
                var q = Vector3.Lerp(sStart, sEnd, k / (float)steps);
                if (!c.Legacy) q.y = Mathf.Max(q.y, H(q.x, q.z));
                cl.SpringerLedges.Add(q + Vector3.up * 0.2f);
            }
            // Langer Kletterpfad zum Gipfel (Fuß-Alternative für Stufe 3): Platten im Abstand 3 m, je ≤ 1 m höher
            var top = peak; var from = cl.Stages[1].Position;
            int plates = Mathf.Clamp(Mathf.CeilToInt((top.y - from.y) / 0.9f), 4, 400);
            for (int k = 1; k <= plates; k++)
            {
                float t = k / (float)plates, ang = t * Mathf.PI * 4f;
                float rr = Mathf.Lerp(Mathf.Max(r2, 10f) + 4f, 6f, t);
                cl.ClimbPath.Add(new Vector3(cc.x + Mathf.Cos(ang) * rr, Mathf.Lerp(from.y, top.y, t), cc.y + Mathf.Sin(ang) * rr));
            }
            // Gipfel-Stufe: Kontrollpunkte entlang des Kletterpfads alle 15 Höhenmeter
            cl.Stages[2].Checkpoints.Clear();
            for (int k = 0; k < cl.ClimbPath.Count; k += Mathf.Max(1, Mathf.RoundToInt(15f / 0.9f))) cl.Stages[2].Checkpoints.Add(cl.ClimbPath[k]);
            cl.Stages[2].Checkpoints.Add(top);
            cl.Stages[2].Position = top;
            // Greif-Startturm an der Basis, Abfahrt zur nächsten Wegmarke
            cl.LaunchTower = baseP + new Vector3(dir.x, 0, dir.y) * 18f;
            cl.DescentTarget = Nearest(c.Waypoints, top, baseP);
            c.Markers.Add(cl.Stages[0].Position);
            // Abkürzung messen: Fußweg (Serpentine bis Stufe 1) gegen Springer-Kette
            if (!c.Legacy)
            {
                float foot = TerrainOps.Length(cl.FootPath);   // Serpentine bis zum Hochplateau
                float mount = Vector3.Distance(sStart, sEnd) * 1.2f;
                plan.Shortcuts.Add(new ShortcutDef { Id = z.Id + "_abk_hang", Name = "Springer-Wand am " + cl.Name, Key = Traversal.Springer, From = sStart, To = sEnd, FootPath = foot, MountPath = mount });
            }
            return cl;
        }

        /// <summary>Aufstieg an einem Bauwerk: Stufen = die ersten drei Anker (zu Fuß, Springer, Gipfel), Springer-Kette außen, Kletterplatten um den Bau.</summary>
        static ClimbDef LandmarkClimb(Context c, ZoneWorldPlan plan, LandmarkDef lm, int lvl)
        {
            var z = c.Zone;
            var fwd = Quaternion.Euler(0, lm.Yaw, 0) * Vector3.forward;
            float half = lm.Footprint * 0.5f;
            var baseP = lm.Position + fwd * (half + 8f);
            var sorted = new List<StageDef>(lm.Stages); sorted.Sort((a, b) => a.Position.y.CompareTo(b.Position.y));
            var st = new[] { sorted[0], sorted[sorted.Count / 2], sorted[sorted.Count - 1] };
            var peak = st[2].Position;
            var cl = new ClimbDef { Id = z.Id + "_aufstieg", Name = "Aufstieg " + lm.Name, Base = baseP, Peak = peak, LandmarkId = lm.Id };
            cl.Stages = ClimbTemplate.Create(baseP, peak, lvl);
            for (int i = 0; i < 3; i++)
            {
                cl.Stages[i].Name = st[i].Name; cl.Stages[i].Position = st[i].Position;
                cl.Stages[i].Checkpoints.Clear();
                var prev = i == 0 ? baseP : cl.Stages[i - 1].Position;
                int n = Mathf.Max(1, Mathf.CeilToInt((st[i].Position.y - prev.y) / 15f));
                for (int k = 1; k <= n; k++) cl.Stages[i].Checkpoints.Add(Vector3.Lerp(prev, st[i].Position, k / (float)n));
            }
            var s2 = cl.Stages[1].Position;
            var outward = new Vector3(s2.x - lm.Position.x, 0, s2.z - lm.Position.z); outward = outward.sqrMagnitude < 1f ? fwd : outward.normalized;
            var sEnd = s2 + outward * 4f;
            int steps = Mathf.Max(2, Mathf.CeilToInt((sEnd.y - baseP.y) / 12f));
            for (int k = 1; k <= steps; k++) cl.SpringerLedges.Add(Vector3.Lerp(baseP, sEnd, k / (float)steps) + Vector3.up * 0.2f);
            int plates = Mathf.Clamp(Mathf.CeilToInt((peak.y - s2.y) / 0.9f), 4, 400);
            for (int k = 1; k <= plates; k++)
            {
                float t = k / (float)plates, ang = t * Mathf.PI * 4f, rr = Mathf.Lerp(half + 6f, 4f, Mathf.Clamp01((t - 0.9f) * 10f));
                cl.ClimbPath.Add(new Vector3(lm.Position.x + Mathf.Cos(ang) * rr, Mathf.Lerp(s2.y, peak.y, t), lm.Position.z + Mathf.Sin(ang) * rr));
            }
            cl.Stages[2].Checkpoints.Clear();
            for (int k = 0; k < cl.ClimbPath.Count; k += Mathf.Max(1, Mathf.RoundToInt(15f / 0.9f))) cl.Stages[2].Checkpoints.Add(cl.ClimbPath[k]);
            cl.Stages[2].Checkpoints.Add(peak);
            cl.LaunchTower = baseP + fwd * 20f;
            cl.DescentTarget = Nearest(c.Waypoints, peak, baseP);
            // Fußweg im Bau: Rampen ≤ 32° bis zur Springer-Stufe (≈ Höhe / sin 30° + Grundriss)
            float foot = (s2.y - baseP.y) / 0.5f + lm.Footprint;
            var sc = new ShortcutDef { Id = z.Id + "_abk_bau", Name = "Springer-Kette am " + lm.Name, Key = Traversal.Springer, From = baseP, To = sEnd, FootPath = foot, MountPath = Vector3.Distance(baseP, sEnd) * 1.2f };
            if (ShortcutModel.SavedSeconds(sc) >= ShortcutModel.MinSaving) plan.Shortcuts.Add(sc);
            else LastReport.Add(z.Id + ": kompakte Zone – Springer-Kette spart nur " + Mathf.RoundToInt(ShortcutModel.SavedSeconds(sc)) + " s, keine 90-s-Abkürzung möglich");
            return cl;
        }

        static void RotateToStart(List<Vector3> pts, Vector2 c, Vector2 start)
        {
            if (pts.Count == 0) return;
            float a0 = Mathf.Atan2(pts[0].z - c.y, pts[0].x - c.x), a1 = Mathf.Atan2(start.y - c.y, start.x - c.x), d = a1 - a0;
            for (int i = 0; i < pts.Count; i++)
            {
                float dx = pts[i].x - c.x, dz = pts[i].z - c.y;
                pts[i] = new Vector3(c.x + dx * Mathf.Cos(d) - dz * Mathf.Sin(d), pts[i].y, c.y + dx * Mathf.Sin(d) + dz * Mathf.Cos(d));
            }
        }

        static Vector3 Nearest(List<Vector3> list, Vector3 to, Vector3 fallback)
        {
            if (list == null || list.Count == 0) return fallback;
            Vector3 best = list[0]; float bd = float.MaxValue;
            foreach (var p in list) { float d = new Vector2(p.x - to.x, p.z - to.z).sqrMagnitude; if (d < bd) { bd = d; best = p; } }
            return best;
        }

        // ---------------- Wahrzeichen ----------------
        static LandmarkDef Place(Context c, ZoneWorldPlan plan, Func<float, float, float> H, Lm spec, bool main, string id, System.Random rnd, LandmarkDef mainDef)
        {
            var layout = LandmarkKit.Build(spec.Kind, spec.Height, id.GetHashCode());
            float fp = layout.Footprint + (spec.Kind == LandmarkKind.Massif ? 30f : 0f);
            var spawn2 = new Vector2(c.Spawn.x, c.Spawn.z);
            Func<Vector2, float> score = p =>
            {
                float sc = -p.magnitude * (main ? 0.5f : 0.1f) + c.Ground(p.x, p.y) * 0.4f;
                if ((p - spawn2).magnitude < 90f) sc -= 400f;
                if (mainDef != null) { float d = (p - new Vector2(mainDef.Position.x, mainDef.Position.z)).magnitude; sc -= d < 200f ? (200f - d) * 4f : 0f; }
                return sc;
            };
            Vector2? at = null;
            for (float m = 15f; m >= 2f && at == null; m -= 6f) at = Spot(c, fp + m, score, 20f);
            if (at == null && !main && spec.Height * 0.8f >= WorldValidator.HeightRange(spec.Kind, false).min)   // Neben-Wahrzeichen: kleiner versuchen
                return Place(c, plan, H, new Lm { Kind = spec.Kind, Name = spec.Name, Height = spec.Height * 0.8f, Prim = spec.Prim, Acc = spec.Acc, Lore = spec.Lore, Top = spec.Top }, main, id, rnd, mainDef);
            if (at == null) { LastReport.Add(c.Zone.Id + ": kein Platz für " + spec.Name + " (Fußabdruck " + Mathf.RoundToInt(fp) + " m)"); return null; }
            // Sichtbarkeit: unter den 6 besten Kandidaten im Umkreis den sichtbarsten wählen (Haupt-Wahrzeichen)
            if (main && !c.Legacy)
            {
                var samples = VisibilityModel.WalkableSamples(H, c.Size, c.Water, 120, c.Zone.Seed);
                Vector2 best = at.Value; float bv = -1f;
                var cands = new List<Vector2> { at.Value };
                float hh = Half(c) - fp - 25f;
                for (int i = 0; i < 24; i++)
                {
                    var q = new Vector2((float)(rnd.NextDouble() * 2 - 1) * hh, (float)(rnd.NextDouble() * 2 - 1) * hh);
                    if (c.Occ.Free(q, fp + 4f) && (q - spawn2).magnitude > 90f) cands.Add(q);
                }
                foreach (var q in cands)
                {
                    float top = c.Ground(q.x, q.y) + layout.Height;
                    float v = VisibilityModel.Fraction(H, samples, new[] { new Vector3(q.x, top, q.y), new Vector3(q.x, c.Ground(q.x, q.y) + layout.Height * 0.66f, q.y) });
                    if (v > bv + 0.02f || (Mathf.Abs(v - bv) <= 0.02f && score(q) > score(best))) { bv = v; best = q; }
                }
                at = best;
            }
            var p2 = at.Value;
            float ground = c.Ground(p2.x, p2.y);
            if (spec.Kind == LandmarkKind.SunkenCity || spec.Kind == LandmarkKind.GlacierPalace) ground = Mathf.Max(ground, c.Water);
            float padH = Mathf.Max(ground, c.Water + 1.2f);
            if (spec.Kind != LandmarkKind.Massif && spec.Kind != LandmarkKind.SunkenCity) plan.Pads.Add(new Vector4(p2.x, p2.y, layout.Footprint * 0.85f, padH));
            if (spec.Kind == LandmarkKind.Massif && !c.Legacy) plan.Ops.Add(new TerrainOpDef { Kind = TerrainOpKind.Mesa, Center = p2, Radius = 26f, Height = padH + spec.Height, Param = 3f });
            if (spec.Kind == LandmarkKind.SunkenCity && !c.Legacy) plan.Ops.Add(new TerrainOpDef { Kind = TerrainOpKind.Basin, Center = p2 + new Vector2(0, 0), Radius = 34f, Param = 2.2f });
            float yaw = Mathf.Atan2(-p2.x, -p2.y) * Mathf.Rad2Deg;   // Eingang zur Zonenmitte
            float baseY = spec.Kind == LandmarkKind.Massif ? padH : spec.Kind == LandmarkKind.SunkenCity ? c.Water : padH;
            var d = new LandmarkDef
            {
                Id = id, Name = spec.Name, Lore = spec.Lore, Kind = spec.Kind, Main = main, Position = new Vector3(p2.x, baseY, p2.y),
                Height = spec.Height, BuildHeight = spec.Height, Footprint = layout.Footprint, Yaw = yaw,
                Primary = spec.Prim, Accent = spec.Acc, Interior = layout.HasInterior,
                LightProfile = LightFor(spec.Kind), SoundProfile = SoundFor(spec.Kind),
            };
            if (spec.Kind == LandmarkKind.SkyIsland || spec.Kind == LandmarkKind.Colossus) d.Height = layout.Height;
            d.Stages = StagesFor(c, d, layout, spec);
            plan.Landmarks.Add(d);
            c.Occ.Add(p2, layout.Footprint + 8f);
            c.Markers.Add(d.Position);
            return d;
        }

        static string LightFor(LandmarkKind k) => k == LandmarkKind.Cathedral ? "Lichtschächte" : k == LandmarkKind.Pyramid ? "Spitzenkristall" : k == LandmarkKind.Lighthouse ? "Laterne" : k == LandmarkKind.Pagoda ? "Laternen" : k == LandmarkKind.GlacierPalace ? "Eisblau" : "Fackeln";
        static string SoundFor(LandmarkKind k)
        {
            switch (k)
            {
                case LandmarkKind.Cathedral: case LandmarkKind.SunkenCity: return "SFX_Amb_Bell_0";
                case LandmarkKind.Pyramid: case LandmarkKind.Citadel: return "SFX_Amb_Torch_0";
                case LandmarkKind.GiantTree: return "SFX_Amb_BirdCall_0";
                case LandmarkKind.Lighthouse: case LandmarkKind.GlacierPalace: return "SFX_Amb_Water_0";
                case LandmarkKind.Pagoda: return "SFX_Amb_Bell_2";
                default: return "SFX_Amb_WindHigh_0";
            }
        }

        /// <summary>Stufen eines Wahrzeichens aus den Ankern des Baukastens (Abschnitt 6, Spalte „Aufstieg und Tore“).</summary>
        static List<StageDef> StagesFor(Context c, LandmarkDef d, LandmarkLayout L, Lm spec)
        {
            var list = new List<StageDef>();
            var rot = Quaternion.Euler(0, d.Yaw, 0);
            void S(string anchor, Traversal keys, RewardKind reward, bool foot = false)
            {
                if (!L.Anchors.TryGetValue(anchor, out var a)) return;
                var g = new GateDef { Keys = keys, FootAlternative = foot };
                g.ReturnGate = keys != Traversal.None && !GateRules.InBand(keys, c.Zone.LevelMax);
                var pos = d.Position + rot * a;
                list.Add(new StageDef { Index = list.Count + 1, Name = anchor, Position = pos, Gate = g, Reward = reward, Checkpoints = new List<Vector3> { pos } });
            }
            var top = spec.Top;
            bool glut = c.Zone.Region == WorldRegion.Glutwueste;
            switch (d.Kind)
            {
                case LandmarkKind.Cathedral:
                    S("Galerie", Traversal.None, RewardKind.Lore);
                    S("Turmgalerie", Traversal.Springer, RewardKind.Vista);
                    S("Turmspitze", top, RewardKind.Insight);
                    S("Krypta", CryptKey(c), RewardKind.Chest);
                    break;
                case LandmarkKind.Pyramid:
                    S("Halbe Höhe", Traversal.None, RewardKind.Lore);
                    S("Flanke", Traversal.Springer, RewardKind.Vista);
                    S("Spitze", Traversal.Griffon, RewardKind.Insight);
                    S("Königskammer", Traversal.None, RewardKind.Lore);
                    if (glut) S("Schatzkammer", Traversal.DuneRunner, RewardKind.Chest);
                    else S("Spitzenkammer", Traversal.Griffon | Traversal.Skyscale, RewardKind.Chest);
                    break;
                case LandmarkKind.Citadel:
                    S("Wehrgang", Traversal.None, RewardKind.Lore);
                    S("Zinnen", Traversal.Springer, RewardKind.Vista);
                    S("Bergfried-Dach", Traversal.Griffon, RewardKind.Chest);
                    S("Halle", Traversal.None, RewardKind.Lore);
                    break;
                case LandmarkKind.Pagoda:
                    S("Ebene 3", Traversal.None, RewardKind.Lore);
                    S("Balkon", Traversal.Springer, RewardKind.Vista);
                    S("Spitze", Traversal.Skyscale | Traversal.Griffon, RewardKind.Insight);
                    break;
                case LandmarkKind.GiantTree:
                    S("Wurzelhalle", Traversal.None, RewardKind.Lore);
                    S("Äste", Traversal.Springer, RewardKind.Vista);
                    S("Kronendorf", Traversal.Springer, RewardKind.Chest);
                    S("Krone", Traversal.Griffon, RewardKind.Insight);
                    break;
                case LandmarkKind.Colossus:
                    S("Brust", Traversal.None, RewardKind.Lore);
                    S("Schulter", Traversal.Springer, RewardKind.Vista);
                    S("Kopf", Traversal.Griffon, RewardKind.Insight);
                    S("Brustkammer", Traversal.None, RewardKind.Chest);
                    break;
                case LandmarkKind.Lighthouse:
                    S("Treppe", Traversal.None, RewardKind.Lore);
                    S("Außengalerie", Traversal.Springer, RewardKind.Chest);
                    S("Laterne", Traversal.None, RewardKind.Vista);   // Wendeltreppe innen bis zur Laterne (Aussicht zu Fuß)
                    break;
                case LandmarkKind.GlacierPalace:
                    S("Halle", Traversal.None, RewardKind.Lore);
                    S("Eisbrücke", Traversal.None, RewardKind.Chest, true);
                    S("Decke", Traversal.Skyscale, RewardKind.Insight);
                    break;
                case LandmarkKind.SunkenCity:
                    S("Halle", Traversal.None, RewardKind.Lore);
                    S("Dächer", Traversal.Raptor | Traversal.Skimmer, RewardKind.Chest);
                    S("Turm", Traversal.Springer, RewardKind.Vista);
                    break;
                case LandmarkKind.SkyIsland:
                    S("Tempel", top, RewardKind.Insight);
                    S("Windorgel", Traversal.Skyscale, RewardKind.Chest);
                    break;
                default:   // Massiv: Felsband (Springer-Vorsprünge) und Gipfel
                    S("Felsband", Traversal.Springer, RewardKind.Vista);
                    S("Gipfel", Traversal.Springer | Traversal.Griffon, RewardKind.Chest);
                    break;
            }
            return list;
        }

        static Traversal CryptKey(Context c)
        {
            foreach (var h in c.Spec.Hazards)
            {
                if (h == HazardKind.ToxicWater) return Traversal.Skimmer;
                if (h == HazardKind.CorruptionField) return Traversal.CorruptionWard;
                if (h == HazardKind.ThornHedge) return Traversal.Raptor | Traversal.Springer;
            }
            return Traversal.Raptor;
        }
    }
}
