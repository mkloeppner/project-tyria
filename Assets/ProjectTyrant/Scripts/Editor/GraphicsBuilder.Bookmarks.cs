using System.Collections.Generic;
using System.Linq;
using ProjectTyrant.Visuals;
using ProjectTyrant.Landmarks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    public static partial class GraphicsBuilder
    {
        public static string ScenePathOf(ZoneDefinition z) => z.LegacyLayout ? ZoneBuilder.ZonePath : BiomeZoneBuilder.ScenePath(z);

        static float GroundAt(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, 900f, p.z), Vector3.down, out var h, 1800f, 1, QueryTriggerInteraction.Ignore)) return h.point.y;
            return p.y;
        }

        static CameraBookmark Save(string zone, string name, BookmarkKind kind, Vector3 pos, Vector3 lookAt, float fov, string cls = "")
        {
            EnsureFolder(BookmarkDir); EnsureFolder(BookmarkDir + "/" + zone);
            string path = BookmarkDir + "/" + zone + "/Cam_" + name + ".asset";
            var b = AssetDatabase.LoadAssetAtPath<CameraBookmark>(path);
            if (b == null) { b = ScriptableObject.CreateInstance<CameraBookmark>(); AssetDatabase.CreateAsset(b, path); }
            for (int i = 0; i < 40 && Physics.CheckSphere(pos, 0.5f, 1, QueryTriggerInteraction.Ignore); i++) pos += Vector3.up * 1f;   // nie in Geometrie
            b.ZoneId = zone; b.Name = name; b.Kind = kind; b.Position = pos; b.Fov = fov; b.ClassId = cls;
            var d = lookAt - pos; b.Euler = d.sqrMagnitude < 0.01f ? Vector3.zero : Quaternion.LookRotation(d).eulerAngles;
            EditorUtility.SetDirty(b);
            return b;
        }

        public static List<CameraBookmark> Bookmarks(string zone)
        {
            var l = new List<CameraBookmark>();
            foreach (var g in AssetDatabase.FindAssets("t:CameraBookmark", new[] { BookmarkDir + "/" + zone }))
            { var b = AssetDatabase.LoadAssetAtPath<CameraBookmark>(AssetDatabase.GUIDToAssetPath(g)); if (b != null) l.Add(b); }
            l.Sort((a, b) => a.Kind.CompareTo(b.Kind));
            return l;
        }

        [MenuItem("Project Tyrant/Grafik G1/Kamera-Lesezeichen setzen")]
        public static void BuildBookmarksMenu() => Debug.Log("[ProjectTyrant] Lesezeichen: " + BuildBookmarks());

        /// <summary>Öffnet jede Zone und setzt sechs Lesezeichen (Wegmarke, Wahrzeichen, Kampf, Innenraum, Wasser/Hürde, Horizont) + Skill-Showcase je Klasse.</summary>
        public static int BuildBookmarks()
        {
            int n = 0;
            var ids = ZoneBiomes.Select(z => z.zone).ToList();
            foreach (var id in ids)
            {
                var z = WorldBuilder.Zone(id); if (z == null) continue;
                string path = ScenePathOf(z); if (!System.IO.File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                z = WorldBuilder.Zone(id);
                float size = z.LegacyLayout ? Procedural.ZoneLayout.Size : z.Size;
                var plan = z.WorldPlan;
                Vector3 G(Vector3 p, float up) => new Vector3(p.x, GroundAt(p) + up, p.z);
                // 1 Wegmarke → Zonenzentrum
                var wp = z.Waypoints != null && z.Waypoints.Count > 0 ? z.Waypoints[0].Position : z.DefaultSpawn;
                var wpPos = G(wp, 2.2f);
                Save(id, "Wegmarke", BookmarkKind.Wegmarke, wpPos, G(Vector3.zero, 2f) + Vector3.up * 4f, 60f); n++;
                // 2 Wahrzeichen (Fernsicht)
                var main = plan != null ? plan.Main : null;
                if (main != null)
                {
                    var dir = new Vector3(z.DefaultSpawn.x - main.Position.x, 0, z.DefaultSpawn.z - main.Position.z); if (dir.sqrMagnitude < 1f) dir = Vector3.back; dir.Normalize();
                    float dist = Mathf.Min(size * 0.35f, Mathf.Max(main.Height * 2.4f, main.Footprint * 1.6f));
                    var p = G(main.Position + dir * dist, 3f);
                    Save(id, "Wahrzeichen", BookmarkKind.Wahrzeichen, p, main.Position + Vector3.up * main.Height * 0.45f, 55f); n++;
                }
                // 3 Kampf: Spielerschulter auf ein Lager mit ≥ 3 Gegnern
                var brains = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<NpcBrain>(true)).Where(b => b.Archetype != null && b.Archetype.Team == Team.Enemy).ToList();
                var group = brains.GroupBy(b => string.IsNullOrEmpty(b.SpawnGroup) ? b.name : b.SpawnGroup).Where(g => g.Count() >= 3).OrderBy(g => Vector3.Distance(g.First().transform.position, z.DefaultSpawn)).FirstOrDefault();
                if (group == null) group = brains.GroupBy(b => b.SpawnGroup).OrderByDescending(g => g.Count()).FirstOrDefault();
                if (group != null)
                {
                    var c = new Vector3(group.Average(b => b.transform.position.x), group.Average(b => b.transform.position.y), group.Average(b => b.transform.position.z));
                    var back0 = new Vector3(z.DefaultSpawn.x - c.x, 0, z.DefaultSpawn.z - c.z); if (back0.sqrMagnitude < 1f) back0 = Vector3.back; back0.Normalize();
                    float wl = z.Biome != null ? z.Biome.WaterLevel : -100f;
                    Vector3 best = Vector3.zero; bool ok = false;
                    for (int k = 0; k < 16 && !ok; k++)
                    {
                        var back = Quaternion.Euler(0, (k % 2 == 0 ? 1 : -1) * (k / 2) * 45f * 0.5f, 0) * back0;
                        var right = Vector3.Cross(Vector3.up, back);
                        var cp = G(c + back * 9f + right * 1.6f, 2.6f);
                        if (Physics.CheckSphere(cp, 0.6f, 1, QueryTriggerInteraction.Ignore)) continue;
                        var target = c + Vector3.up * 1.1f;
                        if (Physics.Linecast(cp, target, out var hit, 1, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<NpcBrain>() == null && hit.distance < Vector3.Distance(cp, target) - 2f) continue;
                        var fwd = (target - cp); fwd.y = 0; fwd.Normalize();
                        var dp = cp + fwd * 6.5f; float gy = GroundAt(dp);
                        if (gy < wl + 0.3f) continue;
                        if (Physics.Linecast(cp, new Vector3(dp.x, gy + 0.1f, dp.z), out var h2, 1, QueryTriggerInteraction.Ignore) && h2.collider.GetComponentInParent<NpcBrain>() == null && h2.distance < Vector3.Distance(cp, dp) - 1.5f) continue;
                        best = cp; ok = true;
                    }
                    if (!ok) best = G(c + back0 * 9f, 2.6f);
                    Save(id, "Kampf", BookmarkKind.Kampf, best, c + Vector3.up * 1.1f, 60f); n++;
                }
                else
                {
                    // Stadt ohne Gegner: Platz an der Wegmarke, Blick auf trockenen Boden (Test-Gefahrenfläche liegt dort)
                    Vector3 best = wpPos, look = G(Vector3.zero, 1f); float wl = z.Biome != null ? z.Biome.WaterLevel : -100f;
                    for (int k = 0; k < 16; k++)
                    {
                        var dir = Quaternion.Euler(0, k * 22.5f, 0) * Vector3.forward; var dp = wpPos + dir * 6.5f; float gy = GroundAt(dp);
                        if (gy < wl + 0.3f || Physics.Linecast(wpPos, new Vector3(dp.x, gy + 0.1f, dp.z), 1, QueryTriggerInteraction.Ignore)) continue;
                        look = new Vector3(dp.x, gy + 0.5f, dp.z) + dir * 4f; break;
                    }
                    Save(id, "Kampf", BookmarkKind.Kampf, best, look, 60f); n++;
                }
                // 4 Innenraum: Wahrzeichen-Innenraum
                var ic = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<InteriorCulling>(true)).OrderByDescending(x => x.LocalBounds.size.sqrMagnitude).FirstOrDefault();
                if (ic != null)
                {
                    var center = ic.transform.TransformPoint(ic.LocalBounds.center);
                    var ext = ic.LocalBounds.extents;
                    var from = ic.transform.TransformPoint(ic.LocalBounds.center + new Vector3(0, -ext.y * 0.55f, -ext.z * 0.7f));
                    Save(id, "Innenraum", BookmarkKind.Innenraum, from, center + Vector3.up * ext.y * 0.1f, 70f); n++;
                }
                else { Save(id, "Innenraum", BookmarkKind.Innenraum, wpPos, wpPos + Vector3.forward * 10f, 70f); n++; }
                // 5 Wasser oder Hürde
                var hz = plan != null ? plan.Hazards.FirstOrDefault(h => h.Kind == HazardKind.Quicksand || h.Kind == HazardKind.ToxicWater || h.Kind == HazardKind.Lava || h.Kind == HazardKind.IceWater || h.Kind == HazardKind.CorruptionField) : null;
                if (hz != null)
                {
                    float r = Mathf.Max(hz.Size.x, hz.Size.y);
                    Save(id, "WasserHuerde", BookmarkKind.WasserHuerde, G(hz.Center + new Vector3(r * 0.9f, 0, -r * 0.9f), r * 0.35f + 4f), hz.Center, 60f); n++;
                }
                else
                {
                    float wl = z.Biome != null ? z.Biome.WaterLevel : -100f; Vector3 shore = Vector3.zero; bool found = false;
                    for (float a = 0; a < 6.28f && !found; a += 0.3f) for (float d = 40f; d < size * 0.45f && !found; d += 20f)
                    { var q = new Vector3(Mathf.Cos(a) * d, 0, Mathf.Sin(a) * d); float gh = GroundAt(q); if (wl > -50f && gh < wl + 0.1f) { shore = q; found = true; } }
                    var at = found ? shore : (plan != null && plan.Hazards.Count > 0 ? plan.Hazards[0].Center : Vector3.zero);
                    var dirS = (new Vector3(z.DefaultSpawn.x, 0, z.DefaultSpawn.z) - new Vector3(at.x, 0, at.z)).normalized;
                    Save(id, "WasserHuerde", BookmarkKind.WasserHuerde, G(at + dirS * 22f, 5f), new Vector3(at.x, (wl > -50f ? wl : GroundAt(at)), at.z), 60f); n++;
                }
                // 6 Horizont: tiefer Blick in die Ferne (weiteste Richtung)
                var sp = new Vector3(z.DefaultSpawn.x, 0, z.DefaultSpawn.z);
                var far = (-sp).sqrMagnitude < 1f ? Vector3.forward : (-sp).normalized;
                var hpos = G(sp + far * 8f, 1.4f);
                Save(id, "Horizont", BookmarkKind.Horizont, hpos, hpos + far * 400f + Vector3.up * 12f, 62f); n++;
            }
            // Skill-Showcase je Klasse (Startzone, Platz vor der Wegmarke)
            var sz = WorldBuilder.Zone("sonnfurt");
            if (sz != null)
            {
                string[] cls = { "waechter", "wildhueter", "schattenklinge", "elementarweber" };
                var c0 = sz.DefaultSpawn;
                for (int i = 0; i < cls.Length; i++)
                {
                    var target = c0 + new Vector3(6f + i * 0.5f, 1.1f, 6f);
                    Save("klassen", "Showcase_" + cls[i], BookmarkKind.SkillShowcase, c0 + new Vector3(-1.5f + i * 0.2f, 2.6f, -2.5f), target, 60f, cls[i]); n++;
                }
            }
            AssetDatabase.SaveAssets();
            LastReport.Add(n + " Lesezeichen");
            return n;
        }
    }
}
