using System.Collections.Generic;
using ProjectTyrant.Landmarks;
using TMPro;
using UnityEditor;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    public static partial class WorldW1Builder
    {
        static string DefDir => DataDir + "/Zone/" + _z.Id;

        static ZoneObjectDefinition Def(string id, MapMarkerKind kind, string name, string desc, Vector3 pos, int xp, Traversal requires)
        {
            EnsureFolder(DefDir);
            string path = DefDir + "/Zone_" + id + ".asset";
            var d = AssetDatabase.LoadAssetAtPath<ZoneObjectDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<ZoneObjectDefinition>(); AssetDatabase.CreateAsset(d, path); }
            d.Id = id; d.Kind = kind; d.DisplayName = name; d.Description = desc; d.Position = pos; d.Radius = 3f;
            d.Experience = xp; d.Karma = 0; d.Copper = 0; d.HeroPoints = 0; d.HeartTask = null; d.KillValue = d.TaskValue = 0f;
            d.Requires = PrimaryKey(requires);
            EditorUtility.SetDirty(d);
            _defs.Add((d, pos));
            return d;
        }

        /// <summary>Für die Karten-Anzeige „Benötigt“ (UND-Logik) den frühesten Schlüssel eines Alternativ-Tors verwenden.</summary>
        static Traversal PrimaryKey(Traversal keys)
        {
            if (keys == Traversal.None) return keys;
            Traversal best = Traversal.None; int bl = int.MaxValue;
            foreach (var k in GateRules.Singles(keys)) { int l = GateRules.AvailableFromLevel(k); if (l < bl) { bl = l; best = k; } }
            return best;
        }

        /// <summary>Karten-„Benötigt“: mit Fuß-Alternative (Grat, Kletterpfad, Treppe) keiner.</summary>
        static Traversal Needs(GateDef g) => g == null || g.FootAlternative ? Traversal.None : g.Keys;

        static int Xp(float baseXp) => Mathf.RoundToInt(baseXp * LevelCurves.RewardScale(_z.LevelMax));

        static void StageReward(string id, string name, StageDef s, LandmarkDef l)
        {
            var at = s.Position;
            if (s.Gate.Keys != Traversal.None) Sign(_root, at + new Vector3(2.5f, 0, -2.5f), s.Name, GateRules.Hint(s.Gate), new Color(0.95f, 0.8f, 0.45f));
            switch (s.Reward)
            {
                case RewardKind.Vista:
                {
                    var def = Def("w1_" + id, MapMarkerKind.Vista, name, "Aussicht von " + l.Name + ".", at, Xp(XpRewards.Vista), Needs(s.Gate));
                    var go = Stele(at, new Color(1f, 0.8f, 0.35f));
                    var v = go.AddComponent<Vista>(); v.Definition = def; v.PromptText = "Aussicht betrachten"; v.Range = 3f; v.OrbitRadius = 30f; v.OrbitHeight = 14f;
                    break;
                }
                case RewardKind.Insight:
                {
                    var def = Def("w1_" + id, MapMarkerKind.Insight, name, "Ein Einblick, der nur von hier oben zu sehen ist.", at, Xp(XpRewards.Insight), Needs(s.Gate));
                    var go = Stele(at, new Color(0.45f, 0.85f, 1f));
                    var ins = go.AddComponent<MasteryInsight>(); ins.Definition = def; ins.PromptText = "Untersuchen"; ins.Range = 3f; ins.ChannelTime = 2f;
                    break;
                }
                case RewardKind.Lore:
                {
                    var t = Tablet(at, "Inschrift: " + s.Name);
                    var lm = t.AddComponent<LoreMarker>(); lm.Id = id; lm.Title = l.Name + " – " + s.Name; lm.Text = l.Lore; lm.PromptText = "Tafel lesen"; lm.Range = 3f;
                    break;
                }
            }
        }

        static GameObject Stele(Vector3 at, Color glow)
        {
            var stone = Mat("W1_Stele", new Color(0.58f, 0.55f, 0.5f));
            var g = Mat("W1_SteleGlow_" + ColorUtility.ToHtmlStringRGB(glow), glow, 0.1f, glow * 1.8f);
            var root = Child(_root, "Stele", at).transform;
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.9f, 0), new Vector3(0.35f, 1.8f, 0.35f), stone, "Pillar", null, true);
            Part(root, PrimitiveType.Sphere, new Vector3(0, 1.95f, 0), new Vector3(0.45f, 0.45f, 0.45f), g, "Orb").AddComponent<Bobber>();
            return root.gameObject;
        }

        static GameObject Tablet(Vector3 at, string title)
        {
            var stone = Mat("W1_Tablet", new Color(0.62f, 0.58f, 0.5f));
            var root = Child(_root, "Lore_" + title, at).transform;
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.7f, 0), new Vector3(1.2f, 1.4f, 0.25f), stone, "Tablet", new Vector3(-12f, 0, 0), true);
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.1f, 0.1f), new Vector3(1.6f, 0.2f, 0.8f), stone, "Base", null, true);
            return root.gameObject;
        }

        /// <summary>Warnschild mit Weltschrift (Hürden-Rand, Tore).</summary>
        static void Sign(Transform parent, Vector3 at, string title, string text, Color c)
        {
            var wood = Mat("W1_SignWood", new Color(0.42f, 0.3f, 0.2f));
            var root = Child(parent, "Sign_" + title, at).transform;
            Part(root, PrimitiveType.Cube, new Vector3(0, 1.1f, 0), new Vector3(0.15f, 2.2f, 0.15f), wood, "Post", null, true);
            Part(root, PrimitiveType.Cube, new Vector3(0, 2.1f, 0), new Vector3(2.2f, 0.9f, 0.08f), wood, "Board");
            var tgo = new GameObject("Text"); tgo.transform.SetParent(root, false); tgo.transform.localPosition = new Vector3(0, 2.1f, -0.06f);
            var tmp = tgo.AddComponent<TextMeshPro>(); tmp.text = "<b>" + title + "</b>\n" + text; tmp.fontSize = 2.2f; tmp.alignment = TextAlignmentOptions.Center; tmp.color = c;
            tmp.rectTransform.sizeDelta = new Vector2(2.1f, 0.85f); tmp.enableAutoSizing = true; tmp.fontSizeMin = 0.8f; tmp.fontSizeMax = 2.2f;
            var back = new GameObject("TextBack"); back.transform.SetParent(root, false); back.transform.localPosition = new Vector3(0, 2.1f, 0.06f); back.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            var tb = back.AddComponent<TextMeshPro>(); tb.text = tmp.text; tb.fontSize = 2.2f; tb.alignment = TextAlignmentOptions.Center; tb.color = c;
            tb.rectTransform.sizeDelta = new Vector2(2.1f, 0.85f); tb.enableAutoSizing = true; tb.fontSizeMin = 0.8f; tb.fontSizeMax = 2.2f;
        }

        static Material _particleMat;
        static ParticleSystem Particles(Transform parent, Vector3 local, Color c, float rate, float speed, float size, float radius, float life = 3f, bool upward = true)
        {
            if (_particleMat == null)
            {
                string path = MatDir + "/W1_Particle.mat";
                _particleMat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (_particleMat == null) { var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit"); _particleMat = new Material(sh != null ? sh : Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(_particleMat, path); }
            }
            var go = new GameObject("Particles"); go.transform.SetParent(parent, false); go.transform.localPosition = local;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main; main.startColor = c; main.startSpeed = speed; main.startSize = size; main.startLifetime = life; main.maxParticles = 300; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = rate;
            var sh2 = ps.shape; sh2.shapeType = ParticleSystemShapeType.Circle; sh2.radius = radius; sh2.rotation = new Vector3(upward ? -90f : 0f, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var gr = new Gradient(); gr.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.8f, 0.2f), new GradientAlphaKey(0f, 1f) });
            col.color = gr;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = _particleMat;
            return ps;
        }

        // ---------------- Hürden ----------------
        static void BuildHazard(HazardDef h, ZoneWorldPlan plan)
        {
            var spec = HazardRules.Spec(h.Kind);
            var root = Child(_root, "Hazard_" + h.Id, h.Center).transform;
            root.rotation = Quaternion.Euler(0, h.Yaw, 0);
            var edge = Mat("W1_HazardEdge_" + h.Kind, spec.EdgeColor, 0.15f, spec.EdgeColor * 0.8f);
            bool surface = h.Kind == HazardKind.Quicksand || h.Kind == HazardKind.ToxicWater || h.Kind == HazardKind.Lava || h.Kind == HazardKind.IceWater || h.Kind == HazardKind.CorruptionField;
            if (surface)
            {
                var face = Mat("W1_Hazard_" + h.Kind, spec.EdgeColor * 0.75f, 0.3f, h.Kind == HazardKind.Lava ? spec.EdgeColor * 1.6f : h.Kind == HazardKind.ToxicWater ? spec.EdgeColor * 0.5f : h.Kind == HazardKind.CorruptionField ? spec.EdgeColor * 0.7f : (Color?)null);
                float y = h.Kind == HazardKind.CorruptionField ? 0.12f : 0.06f;
                if (h.Circular) Part(root, PrimitiveType.Cylinder, new Vector3(0, y, 0), new Vector3(h.Size.x, 0.02f, h.Size.x), face, "Surface");
                else Part(root, PrimitiveType.Cube, new Vector3(0, y, 0), new Vector3(h.Size.x, 0.04f, h.Size.y), face, "Surface");
            }
            // Rand-Markierung (Decal-Ersatz): Ring/Rahmen in der Warnfarbe
            if (h.Circular)
            {
                float r = h.Size.x * 0.5f; int n = Mathf.Clamp(Mathf.RoundToInt(r * 0.8f), 12, 48);
                for (int i = 0; i < n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f; var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                    float gy = Ground(h.Center.x + p.x, h.Center.z + p.z) - h.Center.y;
                    Part(root, PrimitiveType.Cube, new Vector3(p.x, Mathf.Max(gy, 0f) + 0.1f, p.z), new Vector3(r * 2f * Mathf.PI / n * 0.7f, 0.15f, 0.9f), edge, "Edge", new Vector3(0, -a * Mathf.Rad2Deg + 90f, 0));
                }
            }
            else
            {
                float hx = h.Size.x * 0.5f, hz = h.Size.y * 0.5f;
                Part(root, PrimitiveType.Cube, new Vector3(0, 0.1f, -hz), new Vector3(h.Size.x, 0.15f, 0.8f), edge, "Edge");
                Part(root, PrimitiveType.Cube, new Vector3(0, 0.1f, hz), new Vector3(h.Size.x, 0.15f, 0.8f), edge, "Edge");
                Part(root, PrimitiveType.Cube, new Vector3(-hx, 0.1f, 0), new Vector3(0.8f, 0.15f, h.Size.y), edge, "Edge");
                Part(root, PrimitiveType.Cube, new Vector3(hx, 0.1f, 0), new Vector3(0.8f, 0.15f, h.Size.y), edge, "Edge");
            }
            // Partikel je Art
            float pr = h.Circular ? h.Size.x * 0.45f : Mathf.Min(h.Size.x, h.Size.y) * 0.45f;
            switch (h.Kind)
            {
                case HazardKind.Quicksand: Particles(root, new Vector3(0, 0.3f, 0), new Color(0.85f, 0.72f, 0.45f), 30, 0.6f, 1.2f, pr, 4f); break;
                case HazardKind.ToxicWater: Particles(root, new Vector3(0, 0.2f, 0), new Color(0.55f, 1f, 0.4f), 25, 0.8f, 0.5f, pr, 2.5f); break;
                case HazardKind.Lava: Particles(root, new Vector3(0, 0.3f, 0), new Color(1f, 0.5f, 0.15f), 40, 2.5f, 0.3f, pr, 2f); Particles(root, new Vector3(0, 1f, 0), new Color(0.3f, 0.28f, 0.28f), 10, 1.5f, 3f, pr * 0.8f, 6f); break;
                case HazardKind.IceWater: Particles(root, new Vector3(0, 0.4f, 0), new Color(0.85f, 0.95f, 1f), 15, 0.4f, 1.5f, pr, 4f); break;
                case HazardKind.WindWall: Particles(root, new Vector3(0, 3f, 0), new Color(0.95f, 0.98f, 1f), 60, 9f, 0.25f, pr, 1.5f, false); break;
                case HazardKind.CorruptionField: Particles(root, new Vector3(0, 0.3f, 0), new Color(0.7f, 0.35f, 1f), 30, 1.2f, 0.6f, pr, 3f); break;
                case HazardKind.Abyss: Particles(root, new Vector3(0, 2f, 0), new Color(0.9f, 0.9f, 0.95f), 8, 2f, 0.3f, pr, 3f, false); break;
            }
            // Dornenhecke: undurchdringlicher Ring (Raptor-/Springer-Sprung darüber)
            if (h.Kind == HazardKind.ThornHedge)
            {
                var thorn = Mat("W1_Thorn", new Color(0.25f, 0.18f, 0.15f), 0.4f);
                var bloom = Mat("W1_ThornBloom", new Color(0.85f, 0.15f, 0.2f), 0.2f, new Color(0.6f, 0.05f, 0.1f));
                float r = h.Size.x * 0.5f - 1f; int n = 20;
                for (int i = 0; i < n; i++)
                {
                    float a = i / (float)n * Mathf.PI * 2f; var p = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                    float gy = Ground(h.Center.x + p.x, h.Center.z + p.z) - h.Center.y;
                    Part(root, PrimitiveType.Cube, new Vector3(p.x, gy + 1.6f, p.z), new Vector3(r * 2f * Mathf.PI / n * 1.15f, 3.2f, 1.8f), thorn, "Thorn", new Vector3(0, -a * Mathf.Rad2Deg + 90f, 0), true);
                    if (i % 2 == 0) Part(root, PrimitiveType.Sphere, new Vector3(p.x, gy + 3.3f, p.z), Vector3.one * 0.6f, bloom, "Bloom");
                }
            }
            // Sandportal: Paar (draußen → Schatzkammer der Pyramide)
            if (h.Kind == HazardKind.SandPortal)
            {
                var main = plan.Main; StageDef inner = main != null ? main.Stages.Find(s => s.Name == "Schatzkammer") : null;
                var a = Child(root, "SandPortal_A", Vector3.zero).AddComponent<SandPortal>();
                var b = Child(_root, "SandPortal_B_" + h.Id, inner != null ? inner.Position + Vector3.up * 0.2f : h.Center + new Vector3(30f, 0, 0)).AddComponent<SandPortal>();
                a.Partner = b; b.Partner = a; a.Label = "Sandportal zur Schatzkammer"; b.Label = "Sandportal ins Freie";
                Particles(a.transform, Vector3.up, new Color(1f, 0.8f, 0.45f), 40, 2f, 0.6f, 2.5f);
            }
            if (surface || h.Kind == HazardKind.WindWall)
            {
                var hs = root.gameObject.AddComponent<HazardSurface>();
                hs.Id = h.Id; hs.Kind = h.Kind; hs.Size = h.Size; hs.Circular = h.Circular; hs.EdgeDecal = true;
                hs.Ceiling = h.Kind == HazardKind.WindWall ? 14f : 3.5f;
            }
            // Warnschild am Rand (Richtung Zonenmitte)
            var toC = new Vector3(-h.Center.x, 0, -h.Center.z).normalized; if (toC.sqrMagnitude < 0.1f) toC = Vector3.forward;
            float rr = h.Circular ? h.Size.x * 0.5f + 2.5f : Mathf.Max(h.Size.x, h.Size.y) * 0.5f + 2.5f;
            var sp = h.Center + toC * rr; sp.y = Ground(sp.x, sp.z);
            Sign(_root, sp, spec.Name, HazardRules.Requirement(h.Kind), spec.EdgeColor);
        }

        // ---------------- Aufstiege ----------------
        static void BuildClimb(ClimbDef cl, ZoneWorldPlan plan)
        {
            var root = Child(_root, "Climb_" + cl.Id, cl.Peak).transform;
            var rock = Mat("W1_ClimbRock_" + _z.Id, new Color(0.5f, 0.47f, 0.43f), 0.35f);
            var cpMat = Mat("W1_Checkpoint", new Color(0.55f, 0.9f, 1f), 0.1f, new Color(0.4f, 0.85f, 1f) * 1.5f);
            if (_z.LegacyLayout && string.IsNullOrEmpty(cl.LandmarkId)) RockSpire(root, cl, rock);
            // Springer-Abkürzung: Felsvorsprünge
            foreach (var p in cl.SpringerLedges) Part(_root, PrimitiveType.Cube, p + Vector3.down * 0.4f, new Vector3(5f, 0.8f, 5f), rock, "SpringerLedge", new Vector3(0, p.x * 7f % 90f, 0), true);
            if (cl.SpringerLedges.Count > 0) Sign(_root, cl.Base + new Vector3(3f, 0, 3f), "Springer-Wand", "Abkürzung zum " + cl.Stages[0].Name + "\n(zu Fuß: Serpentine)", new Color(0.9f, 0.8f, 0.5f));
            // Langer Kletterpfad zum Gipfel (Fuß-Alternative), Kontrollpunkt-Steine
            for (int i = 0; i < cl.ClimbPath.Count; i++) Part(_root, PrimitiveType.Cube, cl.ClimbPath[i] + Vector3.down * 0.25f, new Vector3(2.6f, 0.5f, 2.6f), rock, "ClimbPlate", null, true);
            var allCp = new List<Vector3>();
            foreach (var s in cl.Stages) foreach (var cp in s.Checkpoints) { allCp.Add(cp); Part(_root, PrimitiveType.Cylinder, cp + Vector3.up * 0.25f, new Vector3(0.6f, 0.25f, 0.6f), cpMat, "Checkpoint"); }
            var area = root.gameObject.AddComponent<ClimbArea>();
            area.Id = cl.Id; area.Name = cl.Name; area.Checkpoints = allCp; area.Radius = Mathf.Max(80f, new Vector2(cl.Base.x - cl.Peak.x, cl.Base.z - cl.Peak.z).magnitude + 40f);
            foreach (var s in cl.Stages) area.Stages.Add(s.Position);
            // Greif-Startturm mit Aufwind an der Basis
            var towerMat = Mat("W1_LaunchTower", new Color(0.62f, 0.56f, 0.48f), 0.3f);
            var tb = cl.LaunchTower; tb.y = Ground(tb.x, tb.z);
            var tower = Child(_root, "LaunchTower_" + cl.Id, tb).transform;
            Part(tower, PrimitiveType.Cylinder, new Vector3(0, 9f, 0), new Vector3(7f, 9f, 7f), towerMat, "Tower", null, true);
            for (int i = 0; i < 12; i++) { float a0 = i * 0.55f, a1 = (i + 1) * 0.55f; float y0 = i * 1.5f, y1 = (i + 1) * 1.5f; RampPart(tower, new Vector3(Mathf.Cos(a0) * 5.2f, y0 + 0.2f, Mathf.Sin(a0) * 5.2f), new Vector3(Mathf.Cos(a1) * 5.2f, y1, Mathf.Sin(a1) * 5.2f), 2f, towerMat); }
            var up = Child(_root, "LaunchUpdraft_" + cl.Id, tb + new Vector3(6f, 18f, 0)).AddComponent<Updraft>(); up.Height = 70f; up.Radius = 5f; up.Strength = 12f;
            Sign(_root, tb + new Vector3(-5f, 0, -5f), "Greif-Startturm", "Aufwind zum Gipfel – " + GateRules.Hint(cl.Stages[2].Gate), new Color(0.75f, 0.9f, 1f));
            if (!string.IsNullOrEmpty(cl.LandmarkId)) return;   // Aufstieg am Bauwerk: Belohnungen kommen von den Stufen des Wahrzeichens
            // Stufen-Belohnungen: 1 Aussicht (zu Fuß), 2 Einblick (Springer/Grat), 3 Gipfel-Aussicht über die Zone (Truhe = Geheimnis)
            var s1 = cl.Stages[0]; var s2 = cl.Stages[1]; var s3 = cl.Stages[2];
            var v1 = Def("w1_" + cl.Id + "_1", MapMarkerKind.Vista, cl.Name + " – " + s1.Name, "Blick hinauf zum Gipfel.", s1.Position, Xp(XpRewards.Vista), Traversal.None);
            Stele(s1.Position + new Vector3(-2f, 0, 0), new Color(1f, 0.8f, 0.35f)).AddComponent<Vista>().Definition = v1;
            var i2 = Def("w1_" + cl.Id + "_2", MapMarkerKind.Insight, cl.Name + " – " + s2.Name, "Ein Einblick auf dem Hochplateau.", s2.Position, Xp(XpRewards.Insight), Needs(s2.Gate));
            var ins = Stele(s2.Position, new Color(0.45f, 0.85f, 1f)).AddComponent<MasteryInsight>(); ins.Definition = i2; ins.ChannelTime = 2f; ins.PromptText = "Untersuchen";
            Sign(_root, s2.Position + new Vector3(3f, 0, 0), s2.Name, GateRules.Hint(s2.Gate) + "\noder der lange Grat zu Fuß", new Color(0.95f, 0.8f, 0.45f));
            var v3 = Def("w1_" + cl.Id + "_3", MapMarkerKind.Vista, cl.Name + " – Gipfel", "Die ganze Zone liegt dir zu Füßen.", s3.Position, Xp(XpRewards.Vista * 2f), Needs(s3.Gate));
            var gv = Stele(s3.Position + new Vector3(2.5f, 0, 0), new Color(1f, 0.8f, 0.35f)).AddComponent<Vista>(); gv.Definition = v3; gv.OrbitRadius = 90f; gv.OrbitHeight = 40f; gv.Duration = 7f;
        }

        static void RampPart(Transform parent, Vector3 a, Vector3 b, float width, Material m)
        {
            var d = b - a; float hor = new Vector2(d.x, d.z).magnitude;
            Part(parent, PrimitiveType.Cube, (a + b) * 0.5f + Vector3.down * 0.25f, new Vector3(width, 0.5f, d.magnitude), m, "Ramp", new Vector3(-Mathf.Atan2(d.y, hor) * Mathf.Rad2Deg, Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 0f), true);
        }

        /// <summary>Startzone (Legacy-Gelände ohne Terrain-Operationen): gebaute Felsnadel mit Wendelrampe bis Stufe 2.</summary>
        static void RockSpire(Transform root, ClimbDef cl, Material rock)
        {
            float g = Ground(cl.Peak.x, cl.Peak.z), top = cl.Peak.y, h = top - g;
            int seg = 6;
            for (int i = 0; i < seg; i++) { float rr = Mathf.Lerp(16f, 5f, i / (float)seg); float y0 = g + i * h / seg; Part(_root, PrimitiveType.Cylinder, new Vector3(cl.Peak.x, y0 + h / seg * 0.5f, cl.Peak.z), new Vector3(rr * 2f, h / seg * 0.5f, rr * 2f), rock, "Spire", null, true); }
            float y = g + 0.2f, s2y = cl.Stages[1].Position.y; int k = 0;
            while (y < s2y && k < 80)
            {
                float a0 = k * 0.4f, a1 = (k + 1) * 0.4f, rr = Mathf.Lerp(18.5f, 12f, (y - g) / Mathf.Max(1f, s2y - g));
                float y1 = Mathf.Min(s2y, y + 1.6f);
                RampPart(_root, new Vector3(cl.Peak.x + Mathf.Cos(a0) * rr, y, cl.Peak.z + Mathf.Sin(a0) * rr), new Vector3(cl.Peak.x + Mathf.Cos(a1) * rr, y1, cl.Peak.z + Mathf.Sin(a1) * rr), 2.6f, rock);
                y = y1; k++;
            }
        }

        // ---------------- Geheimnisse, Lore, Schleifen, Szenen ----------------
        static void BuildSecret(SecretDef s, ZoneWorldPlan plan)
        {
            var wood = Mat("W1_ChestWood", new Color(0.45f, 0.3f, 0.18f)); var gold = Mat("W1_ChestGold", new Color(1f, 0.8f, 0.35f), 0.1f, new Color(1f, 0.7f, 0.2f) * 0.8f);
            var at = s.Position;
            if (at.y < Ground(at.x, at.z) - 0.5f || Mathf.Abs(at.y) < 0.001f) at.y = Ground(at.x, at.z);
            var root = Child(_root, "Secret_" + s.Id, at).transform;
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.4f, 0), new Vector3(1.2f, 0.8f, 0.8f), wood, "Chest", null, true);
            Part(root, PrimitiveType.Cube, new Vector3(0, 0.85f, 0), new Vector3(1.25f, 0.15f, 0.85f), gold, "Lid");
            var ch = root.gameObject.AddComponent<SecretChest>(); ch.Id = s.Id; ch.Title = s.Name; ch.PromptText = "Truhe öffnen"; ch.Range = 2.5f;
            var loop = plan.Loops.Find(l => l.Target == s.Id);
            if (loop != null) { ch.NextGoal = loop.NextGoal; ch.NextGoalName = NameNear(plan, loop.NextGoal); }
            if (s.Id.EndsWith("_hoehle"))
            {
                var rock = Mat("W1_CaveRock", new Color(0.38f, 0.35f, 0.33f), 0.4f);
                var op = plan.Ops.Find(o => o.Kind == TerrainOpKind.Cave);
                float roofY = (op != null ? op.Height : 8f) + 0.5f;
                Part(root, PrimitiveType.Cube, new Vector3(0, roofY, 0), new Vector3(16f, 1.5f, 16f), rock, "CaveRoof", op != null ? new Vector3(0, op.Yaw, 0) : (Vector3?)null, true);
                Particles(root, new Vector3(0, roofY + 1f, 0), new Color(1f, 0.95f, 0.8f), 3, 0.2f, 2f, 1.5f, 6f);   // Lichtschacht-Staub
            }
            if (s.Gate.Keys != Traversal.None && !s.Id.Contains("_wz_")) Sign(_root, at + new Vector3(3f, 0, 3f), s.Name, GateRules.Hint(s.Gate), new Color(0.95f, 0.8f, 0.45f));
        }

        static string NameNear(ZoneWorldPlan plan, Vector3 p)
        {
            foreach (var s in plan.Secrets) if ((s.Position - p).sqrMagnitude < 4f) return s.Name;
            var m = plan.Main; return m != null ? m.Name : "";
        }

        static void BuildLore(LoreDef l)
        {
            var at = l.Position; at.y = Ground(at.x, at.z);
            var t = Tablet(at, l.Title);
            var lm = t.AddComponent<LoreMarker>(); lm.Id = l.Id; lm.Title = l.Title; lm.Text = l.Text; lm.Figure = l.Figure; lm.PromptText = "Tafel lesen"; lm.Range = 3f;
        }

        static void BuildLoopTrigger(LoopDef lp)
        {
            var root = Child(_root, "Loop_" + lp.Id + "_" + lp.TriggerKind, lp.TriggerPos).transform;
            switch (lp.TriggerKind)
            {
                case "Rauchsäule": Particles(root, Vector3.up * 1f, new Color(0.45f, 0.43f, 0.42f), 12, 3f, 4f, 1.5f, 9f); break;
                case "Lichtstrahl":
                {
                    var beam = Mat("W1_LightBeam", new Color(1f, 0.95f, 0.7f), 0.05f, new Color(1f, 0.9f, 0.6f) * 2.5f);
                    Part(root, PrimitiveType.Cylinder, Vector3.up * 30f, new Vector3(0.8f, 30f, 0.8f), beam, "Beam");
                    break;
                }
                case "Vogelschwarm":
                {
                    var bird = Mat("W1_Bird", new Color(0.12f, 0.12f, 0.14f));
                    for (int i = 0; i < 7; i++) { var b = Part(root, PrimitiveType.Sphere, new Vector3(0, 25f + i * 0.8f, 0), new Vector3(0.9f, 0.25f, 0.5f), bird, "Bird_" + i); var c = b.AddComponent<Circler>(); c.Radius = 9f + i; c.Speed = 0.5f + i * 0.05f; }
                    break;
                }
                case "Glocke":
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProcDir + "/Audio/Amb/SFX_Amb_Bell_1.wav");
                    var go = Child(root, "Bell", Vector3.up * 3f); var a = go.AddComponent<AudioSource>(); a.clip = clip; a.spatialBlend = 1f; a.minDistance = 20f; a.maxDistance = 300f; a.playOnAwake = false;
                    go.AddComponent<PeriodicSound>().Interval = 30f;
                    var bronze = Mat("W1_Bell", new Color(0.7f, 0.5f, 0.25f), 0.15f);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 2.2f, 0), new Vector3(0.2f, 4.4f, 0.2f), Mat("W1_SignWood", new Color(0.42f, 0.3f, 0.2f)), "BellPost", null, true);
                    Part(root, PrimitiveType.Sphere, new Vector3(0.5f, 3.6f, 0), new Vector3(0.7f, 0.8f, 0.7f), bronze, "Bell");
                    break;
                }
                default:
                {
                    var crystal = Mat("W1_GlowCrystal", new Color(0.5f, 0.9f, 1f), 0.05f, new Color(0.4f, 0.85f, 1f) * 2.5f);
                    for (int i = 0; i < 5; i++) Part(root, PrimitiveType.Cube, new Vector3(Mathf.Cos(i * 1.3f) * 2.5f, 0.8f, Mathf.Sin(i * 1.3f) * 2.5f), new Vector3(0.5f, 1.6f + i * 0.3f, 0.5f), crystal, "Crystal", new Vector3(10f * i, i * 40f, 12f));
                    var lgo = Child(root, "Glow", Vector3.up * 2f); var lt = lgo.AddComponent<Light>(); lt.color = new Color(0.4f, 0.85f, 1f); lt.range = 14f; lt.intensity = 2f; lt.shadows = LightShadows.None;
                    break;
                }
            }
        }

        static void BuildScene(SceneDef sc)
        {
            var at = sc.Position; at.y = Ground(at.x, at.z);
            var root = Child(_root, "Scene_" + sc.Id + "_" + sc.Kind, at).transform;
            var wood = Mat("W1_SceneWood", new Color(0.4f, 0.28f, 0.18f)); var cloth = Mat("W1_SceneCloth", new Color(0.62f, 0.2f, 0.18f)); var cloth2 = Mat("W1_SceneCloth2", new Color(0.2f, 0.32f, 0.6f));
            var stone = Mat("W1_Tablet", new Color(0.62f, 0.58f, 0.5f));
            var r = new System.Random(sc.Id.GetHashCode());
            float R() => (float)r.NextDouble();
            switch (sc.Kind)
            {
                case "Karawane":
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.8f, 0), new Vector3(2.4f, 1.2f, 4.5f), wood, "Cart", new Vector3(0, 30f, 12f), true);
                    for (int i = 0; i < 7; i++) Part(root, PrimitiveType.Cube, new Vector3(R() * 10f - 5f, 0.4f, R() * 10f - 5f), Vector3.one * (0.6f + R() * 0.4f), wood, "Crate", new Vector3(0, R() * 90f, R() * 20f), true);
                    break;
                case "Schlachtfeld":
                    for (int i = 0; i < 6; i++) { var p = new Vector3(R() * 14f - 7f, 0, R() * 14f - 7f); Part(root, PrimitiveType.Cube, p + Vector3.up * 1.3f, new Vector3(0.1f, 2.6f, 0.1f), wood, "Pole", new Vector3(R() * 30f - 15f, 0, R() * 30f - 15f)); Part(root, PrimitiveType.Cube, p + new Vector3(0.45f, 2.2f, 0), new Vector3(0.8f, 1f, 0.04f), i % 2 == 0 ? cloth : cloth2, "Banner"); }
                    for (int i = 0; i < 8; i++) Part(root, PrimitiveType.Cube, new Vector3(R() * 12f - 6f, 0.05f, R() * 12f - 6f), new Vector3(0.12f, 0.06f, 1.2f), Mat("W1_Blade", new Color(0.6f, 0.62f, 0.66f), 0.1f), "Blade", new Vector3(0, R() * 180f, 0));
                    break;
                case "Grab":
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.7f, 0), new Vector3(0.9f, 1.4f, 0.25f), stone, "Gravestone", null, true);
                    Part(root, PrimitiveType.Cube, new Vector3(0, 0.15f, 1.2f), new Vector3(1.1f, 0.3f, 2f), Mat("W1_Earth", new Color(0.36f, 0.28f, 0.2f)), "Mound");
                    for (int i = 0; i < 5; i++) Part(root, PrimitiveType.Sphere, new Vector3(-4f - i * 2.2f, 0.02f, i * 1.6f), new Vector3(0.4f, 0.04f, 0.7f), Mat("W1_Footprint", new Color(0.25f, 0.2f, 0.15f)), "Footprint", new Vector3(0, 30f, 0));
                    break;
                case "Wrack":
                    Part(root, PrimitiveType.Cube, new Vector3(0, 1.2f, 0), new Vector3(4f, 2.4f, 12f), wood, "Hull", new Vector3(0, 20f, 160f), true);
                    Part(root, PrimitiveType.Cylinder, new Vector3(3f, 2f, 2f), new Vector3(0.4f, 4f, 0.4f), wood, "Mast", new Vector3(70f, 0, 20f), true);
                    break;
                default:   // Lager
                    Part(root, PrimitiveType.Cylinder, new Vector3(0, 0.1f, 0), new Vector3(1.6f, 0.1f, 1.6f), Mat("W1_Ash", new Color(0.2f, 0.19f, 0.18f)), "Firepit");
                    for (int i = 0; i < 3; i++) { float a = i * 2.1f; Part(root, PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 5f, 1f, Mathf.Sin(a) * 5f), new Vector3(2.6f, 2f, 2.6f), cloth, "TornTent", new Vector3(0, a * 57f, 15f), true); }
                    break;
            }
        }
        // ---------------- Prüfung ----------------
        /// <summary>Validator auf einer gebauten, geöffneten Zonen-Szene (Marker aus der Szene).</summary>
        public static WorldReport ValidateBuilt(ZoneDefinition z, Transform zoneRoot) { Validate(z, zoneRoot); return Reports[z.Id]; }

        static void Validate(ZoneDefinition z, Transform zoneRoot)
        {
            var markers = new List<MarkerCheck>();
            foreach (var zo in zoneRoot.GetComponentsInChildren<ZoneObject>(true))
                if (zo.Definition != null) markers.Add(new MarkerCheck { Id = zo.Definition.Id, Kind = zo.Definition.Kind, Position = zo.Definition.Position, Requires = zo.Definition.Requires, OwnStage = zo.Definition.Id.StartsWith("w1_") });
            var rep = WorldValidator.Validate(z.Id, z.LevelMax, z.WorldPlan, markers, z.WorldPlan.MainVisibility);
            Reports[z.Id] = rep;
            LastReport.Add(z.Id + ": Validator " + rep);
            foreach (var e in rep.Errors) LastReport.Add("  FEHLER " + e);
            foreach (var w in rep.Warnings) LastReport.Add("  Hinweis " + w);
        }
    }
}
