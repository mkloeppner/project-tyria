using System;
using System.Collections.Generic;
using ProjectTyrant.Landmarks;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Welt-Lauf W1: plant (falls nötig) und baut Wahrzeichen, Hürden, Aufstiege, Geheimnisse, Lore, Schleifen und Szenen einer Zone
    /// aus <see cref="ZoneDefinition.WorldPlan"/>. Wird vom Zonen-Builder vor NavMesh und Karte aufgerufen; Terrain-Operationen,
    /// Plätze und Fußwege fließen über <see cref="ApplyToSampler"/> in den Höhen-Sampler.
    /// </summary>
    public static partial class WorldW1Builder
    {
        public const string RootName = "PT_W1";
        public static readonly List<string> LastReport = new List<string>();
        public static readonly Dictionary<string, WorldReport> Reports = new Dictionary<string, WorldReport>();

        // ---------------- Planung ----------------
        public static bool NeedsPlan(ZoneDefinition z) => z.WorldPlan == null || z.WorldPlan.Version != WorldPlanner.PlanVersion || z.WorldPlan.IsEmpty;

        /// <summary>Plan neu berechnen (deterministisch) und in der Zonen-Definition speichern.</summary>
        public static ZoneWorldPlan PlanZone(ZoneDefinition z)
        {
            WorldPlanner.Specs.TryGetValue(z.Id, out var spec);
            z.WorldPlan = new ZoneWorldPlan();   // Sampler ohne Plan
            var c = new WorldPlanner.Context { Zone = z, Spec = spec, Legacy = z.LegacyLayout, Size = z.LegacyLayout ? Procedural.ZoneLayout.Size : z.Size };
            BiomeTerrain bt = null;
            if (z.LegacyLayout) { c.Ground = Procedural.ZoneLayout.Height; c.Water = Procedural.ZoneLayout.WaterLevel; }
            else { bt = BiomeZoneBuilder.Sampler(z); c.Ground = bt.Height; c.Water = bt.WaterLevel; c.Lava = z.Biome != null && z.Biome.Ambient == AmbientKind.Embers; }
            c.Spawn = z.DefaultSpawn;
            // Belegung: Inhalte der letzten gebauten Szene (ohne W1), Layout-Daten, Wegmarken, Portale, Spawn, Story-Orte
            Occupy(c, z);
            if (bt != null && bt.RoadCount > 0)
                for (float x = -c.Size * 0.5f; x < c.Size * 0.5f; x += 24f)
                    for (float zz = -c.Size * 0.5f; zz < c.Size * 0.5f; zz += 24f)
                        if (bt.RoadDistance(x, zz) < 6f) c.Occ.Add(new Vector2(x, zz), 10f);
            var plan = WorldPlanner.Plan(c);
            // Sichtbarkeit des Haupt-Wahrzeichens (500 begehbare Punkte)
            var main = plan.Main;
            if (main != null)
            {
                Func<float, float, float> H = (x, zz) => { float h = c.Ground(x, zz); return c.Legacy ? h : TerrainOps.ApplyAll(plan.Ops, x, zz, h, c.Water); };
                var samples = VisibilityModel.WalkableSamples(H, c.Size, c.Water, 500, z.Seed + 1);
                plan.MainVisibility = VisibilityModel.Fraction(H, samples, new[] { main.Position + Vector3.up * main.Height, main.Position + Vector3.up * main.Height * 0.66f });
            }
            z.WorldPlan = plan;
            if (!z.LegacyLayout) FootLinks(z, plan);   // Pflichtinhalt zu Fuß
            EditorUtility.SetDirty(z);
            LastReport.Add(z.Id + ": Plan – " + plan.Landmarks.Count + " Wahrzeichen, " + plan.Ops.Count + " Gelände-Operationen, " + plan.Hazards.Count + " Hürden, " + plan.Secrets.Count + " Geheimnisse, Sichtbarkeit " + Mathf.RoundToInt(plan.MainVisibility * 100f) + " %");
            return plan;
        }

        static void Occupy(WorldPlanner.Context c, ZoneDefinition z)
        {
            c.Occ.Add(new Vector2(z.DefaultSpawn.x, z.DefaultSpawn.z), 40f);
            foreach (var p in z.Portals) c.Occ.Add(new Vector2(p.Position.x, p.Position.z), 30f);
            foreach (var w in z.Waypoints) { c.Occ.Add(new Vector2(w.Position.x, w.Position.z), 25f); c.Waypoints.Add(w.Position); }
            foreach (var a in z.Arrivals) c.Occ.Add(new Vector2(a.Position.x, a.Position.z), 20f);
            var l = z.LegacyLayout ? null : Procedural.BiomeZoneContent.For(z.Id);
            if (l != null)
            {
                foreach (var s in l.AllSpots()) { c.Occ.Add(s.Pos, 28f); c.Markers.Add(new Vector3(s.Pos.x, 0, s.Pos.y)); }
                foreach (var h in l.Hearts) { c.Occ.Add(h.Center, 45f); c.Markers.Add(new Vector3(h.Center.x, 0, h.Center.y)); }
                foreach (var k in l.Camps) c.Occ.Add(k.Pos, 30f);
            }
            var db = StoryBuilder.Load();
            if (db != null)
                foreach (var ch in db.Chapters) foreach (var st in ch.Steps) if (st.Zone == z.Id) c.Occ.Add(new Vector2(st.Position.x, st.Position.z), z.LegacyLayout ? 18f : 35f);
            // Bestehende Szene (letzter Bau): alle Objekte außer Gelände, Wasser, Vegetation und W1
            string path = z.LegacyLayout ? ZoneBuilder.ZonePath : BiomeZoneBuilder.ScenePath(z);
            if (!System.IO.File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r.transform.IsChildOf(root.transform) && Skip(r.transform)) continue;
                        var b = r.bounds; if (b.size.x > 160f || b.size.z > 160f) continue;
                        if (z.LegacyLayout && Mathf.Max(b.extents.x, b.extents.z) < 2f) continue;   // Startzone: Kleinkram (Zäune, Kisten) blockiert nicht
                        c.Occ.Add(new Vector2(b.center.x, b.center.z), Mathf.Max(b.extents.x, b.extents.z) + (z.LegacyLayout ? 1f : 4f));
                    }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        static bool Skip(Transform t)
        {
            for (var p = t; p != null; p = p.parent)
            {
                string n = p.name;
                if (n == RootName || n == "Terrain" || n.StartsWith("Water") || n.StartsWith("Vegetation") || n.StartsWith("Chunk_") || n.StartsWith("PT_Ambient") || n.StartsWith("Sky")) return true;
            }
            return false;
        }

        /// <summary>Terrain-Operationen, Plätze und Fußwege des Plans in den Sampler (vom Zonen-Builder aufgerufen).</summary>
        public static void ApplyToSampler(ZoneDefinition z, BiomeTerrain t)
        {
            var plan = z.WorldPlan; if (plan == null) return;
            t.Ops = plan.Ops;
            foreach (var cl in plan.Climbs)
                for (int i = 1; i < cl.FootPath.Count; i++)
                    t.Road(new Vector2(cl.FootPath[i - 1].x, cl.FootPath[i - 1].z), new Vector2(cl.FootPath[i].x, cl.FootPath[i].z), cl.FootPath[i - 1].y, cl.FootPath[i].y, 2.5f);
            foreach (var p in plan.Pads) t.Flatten(new Vector2(p.x, p.y), p.z, p.w);
            ApplyFootLinks(plan, t);
        }

        // ---------------- Bau ----------------
        static Func<float, float, float> _h;
        static ZoneDefinition _z;
        static Transform _root;
        static readonly List<(ZoneObjectDefinition def, Vector3 pos)> _defs = new List<(ZoneObjectDefinition, Vector3)>();

        public static void Populate(ZoneDefinition z, Transform zoneRoot, Func<float, float, float> height)
        {
            var plan = z.WorldPlan;
            if (plan == null || plan.IsEmpty) return;
            _z = z; _h = height; _defs.Clear();
            _root = Child(zoneRoot, RootName, Vector3.zero).transform;
            foreach (var l in plan.Landmarks) BuildLandmark(l);
            foreach (var h in plan.Hazards) BuildHazard(h, plan);
            foreach (var cl in plan.Climbs) BuildClimb(cl, plan);
            foreach (var s in plan.Secrets) BuildSecret(s, plan);
            foreach (var l in plan.Lore) BuildLore(l);
            foreach (var lp in plan.Loops) BuildLoopTrigger(lp);
            foreach (var sc in plan.Scenes) BuildScene(sc);
            foreach (var f in plan.FootLinks) BuildFootLink(f);
            Validate(z, zoneRoot);
        }

        public static float Ground(float x, float z) => _h != null ? _h(x, z) : 0f;

        static Material RoleMat(LandmarkDef l, ModuleRole r)
        {
            string id = _z.Id + "_" + l.Id;
            switch (r)
            {
                case ModuleRole.Accent: return Mat("W1_" + id + "_Accent", l.Accent, 0.2f);
                case ModuleRole.Glow: return Mat("W1_" + id + "_Glow", l.Accent, 0.1f, l.Accent * 2.2f);
                case ModuleRole.Glass: return Mat("W1_" + id + "_Glass", Color.Lerp(l.Primary, Color.white, 0.25f), 0.1f, l.Primary * 0.35f);
                case ModuleRole.Rock: return Mat("W1_" + id + "_Rock", Color.Lerp(l.Primary, new Color(0.45f, 0.42f, 0.4f), 0.3f), 0.35f);
                case ModuleRole.Foliage: return Mat("W1_" + id + "_Leaf", l.Accent, 0.3f);
                case ModuleRole.Floor: case ModuleRole.Ramp: case ModuleRole.Ledge: return Mat("W1_" + id + "_Walk", Color.Lerp(l.Primary, new Color(0.5f, 0.45f, 0.38f), 0.45f), 0.3f);
                case ModuleRole.Detail: return Mat("W1_" + id + "_Detail", Color.Lerp(l.Primary, Color.black, 0.2f), 0.25f);
                default: return Mat("W1_" + id + "_Main", l.Primary, 0.25f);
            }
        }

        static PrimitiveType Prim(ModuleShape s) => s == ModuleShape.Cylinder ? PrimitiveType.Cylinder : s == ModuleShape.Sphere ? PrimitiveType.Sphere : PrimitiveType.Cube;

        static void BuildLandmark(LandmarkDef l)
        {
            var layout = LandmarkKit.Build(l.Kind, l.BuildHeight > 0f ? l.BuildHeight : l.Height, l.Id.GetHashCode());
            var root = Child(_root, "Landmark_" + l.Id, l.Position).transform;
            root.rotation = Quaternion.Euler(0, l.Yaw, 0);
            var parts = Child(root, "Parts", Vector3.zero).transform;
            var interiorRenderers = new List<Renderer>();
            var byMat = new Dictionary<Material, List<CombineInstance>>();
            var byMatInterior = new Dictionary<Material, List<CombineInstance>>();
            var silhouette = new List<CombineInstance>();
            foreach (var m in layout.Modules)
            {
                var go = Part(parts, Prim(m.Shape), m.Pos, m.Scale, RoleMat(l, m.Role), m.Name, m.Euler, m.Solid);
                var mf = go.GetComponent<MeshFilter>(); var mr = go.GetComponent<MeshRenderer>();
                var ci = new CombineInstance { mesh = mf.sharedMesh, transform = root.worldToLocalMatrix * go.transform.localToWorldMatrix };
                var dict = m.Interior ? byMatInterior : byMat;
                if (!dict.TryGetValue(mr.sharedMaterial, out var list)) dict[mr.sharedMaterial] = list = new List<CombineInstance>();
                list.Add(ci);
                if (m.Silhouette || m.Role == ModuleRole.Structure || m.Role == ModuleRole.Rock || m.Role == ModuleRole.Foliage) silhouette.Add(ci);
                UnityEngine.Object.DestroyImmediate(mr); UnityEngine.Object.DestroyImmediate(mf);
                go.isStatic = true;
            }
            // Kombinierte Meshes je Material (Draw-Calls), Innenraum getrennt (Portal-Culling)
            var lod0 = new List<Renderer>();
            int k = 0;
            foreach (var kv in byMat) lod0.Add(Combined(root, "Mesh_" + (k++), kv.Key, kv.Value, l.Id));
            foreach (var kv in byMatInterior) { var r = Combined(root, "Interior_" + (k++), kv.Key, kv.Value, l.Id); lod0.Add(r); interiorRenderers.Add(r); }
            // Impostor ab ≈ 600 m: nur die Silhouette in einem Mesh
            var imp = Combined(root, "Impostor", RoleMat(l, ModuleRole.Structure), silhouette, l.Id + "_imp");
            var lodg = root.gameObject.AddComponent<LODGroup>();
            float top = Mathf.Max(20f, layout.MaxTop);
            float t0 = Mathf.Clamp(top / (600f * 1.15f), 0.02f, 0.5f);
            lodg.SetLODs(new[] { new LOD(t0, lod0.ToArray()), new LOD(0.004f, new Renderer[] { imp }) });
            lodg.RecalculateBounds();
            // Licht (Glühen oben, Innenlicht, Lichtschacht) und Klang
            var lights = new List<Light>();
            var glow = new GameObject("TopLight"); glow.transform.SetParent(root, false); glow.transform.localPosition = new Vector3(0, top * 0.9f, 0);
            var gl = glow.AddComponent<Light>(); gl.type = LightType.Point; gl.color = l.Accent; gl.range = Mathf.Clamp(top * 0.5f, 20f, 60f); gl.intensity = 2.5f; gl.shadows = LightShadows.None;
            if (layout.HasInterior)
            {
                var b = layout.Interior;
                var il = new GameObject("InteriorLight"); il.transform.SetParent(root, false); il.transform.localPosition = b.center + Vector3.up * b.extents.y * 0.5f;
                var li = il.AddComponent<Light>(); li.type = l.Kind == LandmarkKind.Cathedral ? LightType.Spot : LightType.Point; li.color = Color.Lerp(l.Accent, Color.white, 0.5f);
                li.range = Mathf.Max(b.size.x, b.size.z) * 0.9f; li.intensity = 3f; li.spotAngle = 50f; li.shadows = LightShadows.None;
                if (l.Kind == LandmarkKind.Cathedral) il.transform.localRotation = Quaternion.Euler(70f, 0, 0);
                lights.Add(li);
                var cull = root.gameObject.AddComponent<InteriorCulling>();
                cull.LocalBounds = new Bounds(b.center, b.size + Vector3.one * 2f);
                cull.InteriorOnly = interiorRenderers.ToArray(); cull.InteriorLights = lights.ToArray();
            }
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ProcDir + "/Audio/Amb/" + l.SoundProfile + ".wav");
            if (clip != null)
            {
                var snd = new GameObject("Sound"); snd.transform.SetParent(root, false); snd.transform.localPosition = new Vector3(0, top * 0.7f, 0);
                var a = snd.AddComponent<AudioSource>(); a.clip = clip; a.spatialBlend = 1f; a.minDistance = 30f; a.maxDistance = 450f; a.rolloffMode = AudioRolloffMode.Linear; a.volume = 0.6f; a.playOnAwake = false;
                snd.AddComponent<PeriodicSound>().Interval = l.Kind == LandmarkKind.Cathedral || l.Kind == LandmarkKind.SunkenCity ? 60f : 25f;
            }
            // Karten-Icon, Stufen, Belohnungen
            var mk = root.gameObject.AddComponent<LandmarkMarker>();
            mk.Id = l.Id; mk.Name = l.Name; mk.Main = l.Main; mk.Height = l.Height; mk.TypeName = TypeName(l.Kind); mk.Radius = Mathf.Max(30f, l.Footprint);
            var gates = new List<string>();
            foreach (var s in l.Stages)
            {
                mk.StagePositions.Add(s.Position); mk.StageNames.Add(s.Name);
                if (s.Gate.Keys != Traversal.None) gates.Add(s.Name + ": " + GateRules.Hint(s.Gate));
                StageReward(l.Id + "_" + s.Index, l.Name + " – " + s.Name, s, l);
            }
            mk.GateText = string.Join("\n", gates);
            AddPlaceholder(root.gameObject, "W1_" + l.Id + (l.Main ? " (Tier 1, Hero-Fassade)" : " (Tier 2)"), "stylized painterly fantasy " + TypeName(l.Kind).ToLower() + ": " + l.Name + ", monumental, game-ready, hand-painted texture");
        }

        static Renderer Combined(Transform parent, string name, Material mat, List<CombineInstance> list, string meshId)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.isStatic = true;
            var mesh = new Mesh { name = "W1_" + meshId + "_" + name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(list.ToArray(), true, true);
            mesh.RecalculateBounds();
            string dir = ProcDir + "/Meshes/W1"; EnsureFolder(dir);
            string path = dir + "/" + mesh.name.Replace(' ', '_') + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null) { EditorUtility.CopySerialized(mesh, old); mesh = old; } else AssetDatabase.CreateAsset(mesh, path);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            return r;
        }

        public static string TypeName(LandmarkKind k)
        {
            switch (k)
            {
                case LandmarkKind.Cathedral: return "Kathedrale";
                case LandmarkKind.Pyramid: return "Pyramide";
                case LandmarkKind.Citadel: return "Zitadelle";
                case LandmarkKind.Pagoda: return "Pagode";
                case LandmarkKind.GiantTree: return "Riesenbaum";
                case LandmarkKind.Colossus: return "Steinriese";
                case LandmarkKind.Lighthouse: return "Turm";
                case LandmarkKind.GlacierPalace: return "Palast";
                case LandmarkKind.SunkenCity: return "Versunkene Stadt";
                case LandmarkKind.SkyIsland: return "Himmelsinsel";
                default: return "Landform";
            }
        }
    }
}
