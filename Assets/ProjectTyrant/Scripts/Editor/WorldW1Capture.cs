using System.IO;
using ProjectTyrant.Landmarks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Welt-Lauf W1: Aufnahmen je Zone nach Temp/PT_QA/W1/&lt;Zone&gt;/ (Wahrzeichen vom Spawn, Luftbild, Aufstieg von unten, Hürde). Rendert im Editor, ohne Play Mode.</summary>
    public static class WorldW1Capture
    {
        public const string Dir = "Temp/PT_QA/W1";

        [MenuItem("Project Tyrant/Welt W1/Aufnahmen aller Zonen")]
        public static void CaptureAllMenu() => CaptureAll();

        public static string CaptureAll(string only = null)
        {
            int n = 0; var sb = new System.Text.StringBuilder();
            foreach (var id in WorldPlanner.Specs.Keys)
            {
                if (only != null && only != id) continue;
                var z = WorldBuilder.Zone(id); if (z == null || z.WorldPlan == null) continue;
                string path = z.LegacyLayout ? ZoneBuilder.ZonePath : BiomeZoneBuilder.ScenePath(z);
                if (!File.Exists(path)) continue;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                n += CaptureZone(z, sb);
            }
            return n + " Aufnahmen\n" + sb;
        }

        [MenuItem("Project Tyrant/Welt W1/Gebaute Zonen prüfen")]
        public static void ValidateAllMenu() => Debug.Log("[ProjectTyrant] Welt W1 – Prüfung:\n" + ValidateAll());

        /// <summary>Öffnet jede gebaute Zone und prüft sie mit den Markern der Szene; zählt W1-Objekte.</summary>
        public static string ValidateAll()
        {
            var sb = new System.Text.StringBuilder(); int ok = 0, n = 0;
            foreach (var id in WorldPlanner.Specs.Keys)
            {
                var z = WorldBuilder.Zone(id); if (z == null || z.WorldPlan == null) continue;
                string path = z.LegacyLayout ? ZoneBuilder.ZonePath : BiomeZoneBuilder.ScenePath(z);
                if (!File.Exists(path)) continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Transform root = null; foreach (var g in scene.GetRootGameObjects()) if (g.name == "PT_ZoneRoot") root = g.transform;
                if (root == null) { sb.Append(id + ": kein PT_ZoneRoot\n"); continue; }
                var w1 = root.Find(WorldW1Builder.RootName);
                var r = WorldW1Builder.ValidateBuilt(z, root); n++; if (r.Ok) ok++;
                int missing = 0; foreach (var t in root.GetComponentsInChildren<Transform>(true)) missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                sb.Append(id + ": " + r + (w1 == null ? " · KEIN PT_W1" : " · Truhen " + w1.GetComponentsInChildren<SecretChest>(true).Length + ", Lore " + w1.GetComponentsInChildren<LoreMarker>(true).Length + ", Hürden " + w1.GetComponentsInChildren<HazardSurface>(true).Length + ", Wahrzeichen " + w1.GetComponentsInChildren<LandmarkMarker>(true).Length + ", Aufstiege " + w1.GetComponentsInChildren<ClimbArea>(true).Length) + (missing > 0 ? " · FEHLENDE SKRIPTE " + missing : ""));
                foreach (var e in r.Errors) sb.Append("\n  FEHLER " + e);
                sb.Append("\n");
            }
            return ok + "/" + n + " Zonen ohne Fehler\n" + sb;
        }

        static int CaptureZone(ZoneDefinition z, System.Text.StringBuilder log)
        {
            string dir = Path.Combine(Dir, z.SceneName); Directory.CreateDirectory(dir);
            var plan = z.WorldPlan; int n = 0;
            var main = plan.Main;
            var spawn = z.DefaultSpawn + Vector3.up * 1.8f;
            if (main != null)
            {
                var top = main.Position + Vector3.up * main.Height * 0.6f;
                n += Shot(dir, "01_wahrzeichen_vom_spawn", spawn, top, 60f);
                var off = (spawn - main.Position); off.y = 0f; off = off.sqrMagnitude < 1f ? Vector3.back : off.normalized;
                n += Shot(dir, "02_wahrzeichen_nah", main.Position + off * (main.Footprint + 60f) + Vector3.up * 12f, top, 60f);
            }
            float s = z.LegacyLayout ? 200f : z.Size;
            n += Shot(dir, "03_luftbild", new Vector3(0, s * 0.55f, -s * 0.62f), Vector3.zero, 55f, true);
            if (plan.Climbs.Count > 0)
            {
                var c = plan.Climbs[0]; var away = c.Base - c.Peak; away.y = 0f; away = away.sqrMagnitude < 1f ? Vector3.back : away.normalized;
                var side = Vector3.Cross(Vector3.up, away).normalized;
                n += Shot(dir, "04_aufstieg", c.Base + away * 70f + side * 45f + Vector3.up * 20f, Vector3.Lerp(c.Base, c.Peak, 0.5f), 65f);
                n += Shot(dir, "05_gipfelblick", c.Peak + Vector3.up * 3f + away * 4f, c.Peak + away * 300f + Vector3.down * 80f, 70f);
            }
            if (plan.Hazards.Count > 0)
            {
                var h = plan.Hazards[0]; float r = Mathf.Max(h.Size.x, h.Size.y);
                n += Shot(dir, "06_huerde_" + h.Kind, h.Center + new Vector3(r * 0.8f, r * 0.6f + 8f, -r * 0.8f), h.Center, 60f);
            }
            log.Append(z.Id + ": " + n + "\n");
            return n;
        }

        static int Shot(string dir, string name, Vector3 from, Vector3 at, float fov, bool noFog = false)
        {
            var go = new GameObject("PT_W1_CaptureCam");
            bool fog = RenderSettings.fog; if (noFog) RenderSettings.fog = false;
            try
            {
                var cam = go.AddComponent<Camera>();
                cam.fieldOfView = fov; cam.nearClipPlane = 0.3f; cam.farClipPlane = 4000f;
                go.transform.position = from; go.transform.rotation = Quaternion.LookRotation((at - from).sqrMagnitude < 0.01f ? Vector3.forward : at - from);
                var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt; cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
                RenderTexture.active = null; cam.targetTexture = null;
                Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
                return 1;
            }
            finally { Object.DestroyImmediate(go); RenderSettings.fog = fog; }
        }
    }
}
