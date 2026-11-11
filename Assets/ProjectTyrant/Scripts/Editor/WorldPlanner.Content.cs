using System;
using System.Collections.Generic;
using ProjectTyrant.Landmarks;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    public static partial class WorldPlanner
    {
        static readonly string[] LoopKinds = { "Rauchsäule", "Lichtstrahl", "Vogelschwarm", "Glocke", "Glühen" };

        // ---------------- Hürden ----------------
        static void Hazards(Context c, ZoneWorldPlan plan, Func<float, float, float> H, System.Random rnd, int lvl)
        {
            var z = c.Zone; var main = plan.Main;
            var mainP = main != null ? new Vector2(main.Position.x, main.Position.z) : Vector2.zero;
            int n = 0;
            foreach (var kind in c.Spec.Hazards)
            {
                n++;
                string id = z.Id + "_h" + n;
                var spec = HazardRules.Spec(kind);
                switch (kind)
                {
                    case HazardKind.Quicksand: case HazardKind.ToxicWater: case HazardKind.IceWater: case HazardKind.Lava:
                    {
                        if (c.Legacy) break;
                        float r = 46f;
                        var p = Spot(c, r + 12f, q => -Mathf.Abs((q - mainP).magnitude - (main != null ? main.Footprint + r + 30f : 200f)) - q.magnitude * 0.05f);
                        if (p == null) { LastReport.Add(z.Id + ": kein Platz für " + spec.Name); break; }
                        plan.Ops.Add(new TerrainOpDef { Kind = TerrainOpKind.Basin, Center = p.Value, Radius = r, Param = kind == HazardKind.Quicksand ? 1.2f : 2f });
                        plan.Ops.Add(new TerrainOpDef { Kind = TerrainOpKind.Mesa, Center = p.Value, Radius = 6f, Height = c.Water + 1.6f, Param = 2.5f });
                        c.Occ.Add(p.Value, r + 14f);
                        plan.Hazards.Add(new HazardDef { Id = id, Kind = kind, Center = new Vector3(p.Value.x, c.Water, p.Value.y), Size = Vector2.one * r * 2f, Circular = true, Keys = spec.Keys });
                        AddSecret(c, plan, id + "_insel", "Insel im " + spec.Name, new Vector3(p.Value.x, c.Water + 1.6f, p.Value.y), Gate(spec.Keys, lvl), "Etwas glitzert in der Mitte – " + spec.Name + " schützt es.");
                        break;
                    }
                    case HazardKind.ThornHedge:
                    {
                        var p = Spot(c, 16f, q => -Mathf.Abs((q - mainP).magnitude - 120f));
                        if (p == null) break;
                        c.Occ.Add(p.Value, 16f);
                        plan.Hazards.Add(new HazardDef { Id = id, Kind = kind, Center = V3(c, p.Value, H), Size = Vector2.one * 20f, Circular = true, Keys = spec.Keys });
                        AddSecret(c, plan, id + "_dornen", "Hinter den Dornen", V3(c, p.Value, H), Gate(Traversal.Raptor | Traversal.Springer, lvl), "Rote Blüten verbergen eine Kiste.");
                        break;
                    }
                    case HazardKind.CorruptionField:
                    {
                        var crypt = main != null ? main.Stages.Find(s => s.Name == "Krypta") : null;
                        Vector3 at;
                        if (crypt != null) at = crypt.Position;
                        else { var p = Spot(c, 18f, q => -Mathf.Abs((q - mainP).magnitude - 160f)); if (p == null) break; c.Occ.Add(p.Value, 18f); at = V3(c, p.Value, H); AddSecret(c, plan, id + "_riss", "Im Verderbnis-Riss", at, Gate(spec.Keys, lvl), "Violettes Licht pulsiert über einer Kiste."); }
                        plan.Hazards.Add(new HazardDef { Id = id, Kind = kind, Center = at, Size = Vector2.one * 26f, Circular = true, Keys = spec.Keys });
                        break;
                    }
                    case HazardKind.WindWall:
                    {
                        if (plan.Climbs.Count == 0) break;
                        var cl = plan.Climbs[0]; var s3 = cl.Stages[2].Position; var s2 = cl.Stages[1].Position;
                        var mid = Vector3.Lerp(s2, s3, 0.6f);
                        var dir = new Vector2(mid.x - cl.Peak.x, mid.z - cl.Peak.z); float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
                        plan.Hazards.Add(new HazardDef { Id = id, Kind = kind, Center = mid, Size = new Vector2(40f, 14f), Yaw = yaw, Keys = spec.Keys });
                        break;
                    }
                    case HazardKind.SteepWall:
                    {
                        if (plan.Climbs.Count == 0) break;
                        var cl = plan.Climbs[0]; var a = cl.Stages[0].Position; var b = cl.Stages[1].Position;
                        var mid = Vector3.Lerp(a, b, 0.5f); var dir = new Vector2(b.x - a.x, b.z - a.z);
                        plan.Hazards.Add(new HazardDef { Id = id, Kind = kind, Center = mid, Size = new Vector2(30f, Mathf.Max(10f, dir.magnitude)), Yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg, Keys = spec.Keys });
                        break;
                    }
                    case HazardKind.Abyss:
                    {
                        if (plan.Hazards.Exists(h => h.Kind == HazardKind.Abyss) || plan.Climbs.Count == 0) break;
                        var cl = plan.Climbs[0];
                        var mid = cl.ClimbPath.Count > 0 ? cl.ClimbPath[cl.ClimbPath.Count / 2] : cl.Stages[2].Position;
                        plan.Hazards.Add(new HazardDef { Id = id, Kind = kind, Center = mid, Size = Vector2.one * 40f, Circular = true, Keys = spec.Keys });
                        break;
                    }
                    case HazardKind.SandPortal:
                    {
                        var p = Spot(c, 10f, q => -Mathf.Abs((q - mainP).magnitude - (main != null ? main.Footprint + 40f : 100f)));
                        if (p == null) break;
                        c.Occ.Add(p.Value, 10f);
                        plan.Hazards.Add(new HazardDef { Id = id, Kind = kind, Center = V3(c, p.Value, H), Size = Vector2.one * 6f, Circular = true, Keys = Traversal.DuneRunner });
                        break;
                    }
                }
            }
        }

        static GateDef Gate(Traversal keys, int lvl) => new GateDef { Keys = keys, ReturnGate = keys != Traversal.None && !GateRules.InBand(keys, lvl) };

        static void AddSecret(Context c, ZoneWorldPlan plan, string id, string name, Vector3 pos, GateDef gate, string hint)
        {
            plan.Secrets.Add(new SecretDef { Id = id, Name = name, Position = pos, Gate = gate, Hint = hint });
        }

        // ---------------- Geheimnisse, Schleifen, Szenen, Lore ----------------
        static void Secrets(Context c, ZoneWorldPlan plan, Func<float, float, float> H, System.Random rnd, int lvl)
        {
            var z = c.Zone; var s = c.Spec;
            // Truhen der Wahrzeichen- und Aufstiegsstufen sind Geheimnisse (außerhalb aller Marker)
            foreach (var l in plan.Landmarks)
                foreach (var st in l.Stages)
                    if (st.Reward == RewardKind.Chest) AddSecret(c, plan, l.Id + "_" + st.Index, l.Name + ": " + st.Name, st.Position, st.Gate, "Späher erzählen von einer Kiste " + (st.Name == "Krypta" ? "in der Krypta" : "oben am " + l.Name) + ".");
            foreach (var cl in plan.Climbs)
            {
                var top = cl.Stages[2];
                AddSecret(c, plan, cl.Id + "_gipfel", cl.Name + ": Gipfeltruhe", top.Position + Vector3.up * 0.4f, top.Gate, "Auf dem Gipfel liegt angeblich der Proviant des ersten Bezwingers.");
            }
            // Höhle (Terrain-Operation „Höhle“) und freie Verstecke bis mindestens 6
            int guard = 0;
            while (plan.Secrets.Count < WorldValidator.MinSecrets + 1 && guard++ < 12)
            {
                bool cave = !c.Legacy && !plan.Ops.Exists(o => o.Kind == TerrainOpKind.Cave);
                var p = Spot(c, cave ? 22f : 12f, q => (float)(Mathf.Sin(q.x * 0.013f + guard) + Mathf.Cos(q.y * 0.011f - guard)) - q.magnitude * 0.0005f);
                if (p == null) break;
                c.Occ.Add(p.Value, cave ? 22f : 12f);
                if (cave)
                {
                    plan.Ops.Add(new TerrainOpDef { Kind = TerrainOpKind.Cave, Center = p.Value, Radius = 18f, Height = 9f, Yaw = rnd.Next(0, 360) });
                    AddSecret(c, plan, z.Id + "_hoehle", "Verborgene Höhle", new Vector3(p.Value.x, c.Ground(p.Value.x, p.Value.y), p.Value.y), Gate(Traversal.None, lvl), "Hinter dem Hügel führt ein Gang in den Fels.");
                }
                else AddSecret(c, plan, z.Id + "_versteck" + guard, "Versteck", V3(c, p.Value, H), Gate(Traversal.None, lvl), "Ein Vogelschwarm kreist über einer Stelle ohne Weg.");
            }
            // Entdeckungsschleifen: die ersten fünf Geheimnisse, Belohnung zeigt das nächste Ziel
            var targets = plan.Secrets;
            for (int i = 0; i < Mathf.Min(5, targets.Count); i++)
            {
                var t = targets[i];
                var next = i + 1 < Mathf.Min(5, targets.Count) ? targets[i + 1].Position : (plan.Main != null ? plan.Main.Position + Vector3.up * plan.Main.Height : t.Position);
                plan.Loops.Add(new LoopDef { Id = z.Id + "_schleife" + (i + 1), TriggerKind = LoopKinds[i % LoopKinds.Length], Target = t.Id, TriggerPos = t.Position, TargetPos = t.Position, NextGoal = next });
            }
            // Umwelt-Szenen (drei) mit Lore-Tafel
            string mainName = plan.Main != null ? plan.Main.Name : z.DisplayName;
            for (int i = 0; i < 3; i++)
            {
                string kind = s.Scenes[i % s.Scenes.Length];
                float sr = c.Legacy ? 7f : 16f;   // Startzone: kleine Szenen zwischen den Weilern
                var p = Spot(c, sr, q => (float)Math.Sin(q.x * 0.021f + i * 1.7f) + (float)Math.Cos(q.y * 0.017f - i) - q.magnitude * 0.0006f);
                if (p == null) continue;
                c.Occ.Add(p.Value, sr);
                var pos = V3(c, p.Value, H);
                string loreId = z.Id + "_lore_szene" + (i + 1);
                plan.Scenes.Add(new SceneDef { Id = z.Id + "_szene" + (i + 1), Kind = kind, Position = pos, LoreId = loreId });
                plan.Lore.Add(new LoreDef { Id = loreId, Title = SceneTitle(kind), Text = SceneLore(kind, s.Figure, mainName), Figure = s.Figure, Position = pos + new Vector3(3f, 0, 2f) });
            }
            // Lore-Tafeln an den Wahrzeichen und am Aufstieg
            foreach (var l in plan.Landmarks)
            {
                var layoutEntrance = l.Position + Quaternion.Euler(0, l.Yaw, 0) * new Vector3(4f, 0f, -l.Footprint * 0.5f);
                plan.Lore.Add(new LoreDef { Id = l.Id + "_lore", Title = l.Name, Text = l.Lore, Figure = s.Figure, Position = new Vector3(layoutEntrance.x, c.Ground(layoutEntrance.x, layoutEntrance.z), layoutEntrance.z) });
            }
            foreach (var cl in plan.Climbs)
                plan.Lore.Add(new LoreDef { Id = cl.Id + "_lore", Title = cl.Name, Text = "Wer den " + cl.Name + " bis zum Gipfel geht, sieht die ganze Zone – und das nächste Ziel dahinter. Die Ungeduldigen nehmen die Wand.", Figure = s.Figure, Position = cl.Stages[0].Position + new Vector3(2.5f, 0, 2.5f) });
        }

        static string SceneTitle(string kind) => kind == "Lager" ? "Verlassenes Lager" : kind == "Schlachtfeld" ? "Altes Schlachtfeld" : kind == "Grab" ? "Grab eines Wanderers" : kind == "Karawane" ? "Gestrandete Karawane" : "Wrack am Ufer";

        static string SceneLore(string kind, string figure, string main)
        {
            switch (kind)
            {
                case "Lager": return "Die Asche ist kalt, die Zelte sind zerrissen. Auf einem Brett steht für " + figure + ": „Wir sind zum " + main + " gegangen. Folge dem Rauch.“";
                case "Schlachtfeld": return "Banner beider Seiten liegen nebeneinander im Gras. Niemand hat die Toten gezählt – nur die Klingen, die " + figure + " später fand.";
                case "Grab": return "Ein Wanderer ruht hier, den Blick zum " + main + ". Auf dem Stein: „Ich wollte nur sehen, wie es von oben aussieht.“";
                case "Karawane": return "Die Karawane blieb im Sand stecken. Die Kisten sind leer – bis auf eine, und die zeigt mit ihren Spuren zum " + main + ".";
                default: return "Das Wrack liegt mit dem Kiel nach oben. Im Logbuch steht nur ein Satz: „Kurs auf " + main + ".“ " + figure + " kannte den Kapitän.";
            }
        }
    }
}
