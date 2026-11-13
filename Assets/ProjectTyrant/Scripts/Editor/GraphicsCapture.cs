using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ProjectTyrant.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ProjectTyrant.EditorTools
{
    /// <summary>
    /// Grafik-Lauf G1: Capture-Matrix (1920×1080, je Lesezeichen) mit Post-Processing, Himmels-Maske über Raycasts, Metriken (CSV)
    /// und Lesbarkeit einer Test-Gefahrenfläche in Kampf-Captures. Vorher-Captures werden nie überschrieben.
    /// </summary>
    public static class GraphicsCapture
    {
        public const string Root = "Temp/PT_QA/G1";
        public const int W = 1920, Hgt = 1080, MaskW = 96, MaskH = 54;
        public static readonly List<string> LastLog = new List<string>();
        /// <summary>Nur für Korrekturen am Messverfahren innerhalb von G1 (vor jeder Render-Änderung).</summary>
        public static bool AllowOverwrite;

        public struct Shot { public string Zone, Name; public MetricsResult M; public float DeltaE; public string File; }

        /// <summary>Alle Zonen (oder eine) aufnehmen nach Temp/PT_QA/G1/&lt;tag&gt;/&lt;Zone&gt;/&lt;Name&gt;.png; CSV metrics_&lt;tag&gt;.csv. Vorher: niemals überschreiben.</summary>
        public static List<Shot> CaptureAll(string tag, string onlyZone = null, float? hour = null)
        {
            var shots = new List<Shot>();
            bool protect = tag == "vorher" && !AllowOverwrite;
            // Grafik-Lauf G2: Nachher-Aufnahmen mit Zonenprofil, Tageszeit und Stufe Hoch (Vorher = Bestand ohne neue Beleuchtung)
            _light = tag != "vorher"; _hour = hour ?? 11f;
            int prevLevel = QualitySettings.GetQualityLevel();
            if (_light) QualitySettings.SetQualityLevel(3, true);
            try {
            foreach (var (zone, _, _) in GraphicsBuilder.ZoneBiomes)
            {
                if (onlyZone != null && zone != onlyZone) continue;
                var z = WorldBuilder.Zone(zone); if (z == null) continue;
                string path = GraphicsBuilder.ScenePathOf(z); if (!File.Exists(path)) continue;
                var marks = GraphicsBuilder.Bookmarks(zone); if (marks.Count == 0) continue;
                string dir = Path.Combine(Root, tag, zone);
                if (protect && Directory.Exists(dir) && Directory.GetFiles(dir, "*.png").Length >= marks.Count) { LastLog.Add(zone + ": vorher existiert – nicht überschrieben"); continue; }
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                _profile = _light ? GraphicsBuilder.Profile(zone) : null;
                Directory.CreateDirectory(dir);
                foreach (var b in marks) shots.Add(CaptureOne(b, dir, z));
            }
            } finally { if (_light) QualitySettings.SetQualityLevel(prevLevel, true); _profile = null; }
            WriteCsv(Path.Combine(Root, "metrics_" + tag + ".csv"), shots, onlyZone != null);
            return shots;
        }

        static bool _light; static float _hour = 11f; static ZoneArtProfile _profile;
        /// <summary>Wetter für Nachher-Aufnahmen (Standard: klar).</summary>
        public static WeatherState CaptureWeather = WeatherState.Klar;

        public static Shot CaptureOne(CameraBookmark b, string dir, ZoneDefinition z)
        {
            GameObject danger = null;
            if (b.Kind == BookmarkKind.Kampf) danger = SpawnTestDanger(b);
            var go = new GameObject("PT_G1_CaptureCam");
            var shot = new Shot { Zone = b.ZoneId, Name = b.Name };
            try
            {
                var cam = go.AddComponent<Camera>();
                var data = go.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.None; data.requiresDepthTexture = true; data.requiresColorTexture = true;
                cam.fieldOfView = b.Fov; cam.nearClipPlane = 0.2f; cam.farClipPlane = 3000f; cam.allowHDR = true;
                cam.aspect = W / (float)Hgt;   // G7: feste Bildgeometrie auch nach dem Zurücksetzen der Zieltextur (sonst verschobene ΔE-Abtastung)
                go.transform.position = b.Position; go.transform.rotation = Quaternion.Euler(b.Euler);
                if (_profile != null)
                {
                    go.tag = "MainCamera";
                    SceneLighting.Apply(_profile, _hour, WeatherModel.LookOf(CaptureWeather), AssetDatabase.LoadAssetAtPath<GraphicsLibrary>(GraphicsBuilder.LibraryPath));
                }
                var px = Render(cam, W, Hgt);
                var mask = SkyMask(cam);
                shot.M = VisualMetrics.Measure(px, W, Hgt, mask);
                shot.DeltaE = danger != null ? DangerDeltaE(cam, px, danger.transform.position, 3f) : -1f;
                shot.File = Path.Combine(dir, b.Name + ".png");
                var tex = new Texture2D(W, Hgt, TextureFormat.RGBA32, false); tex.SetPixels32(px); tex.Apply();
                File.WriteAllBytes(shot.File, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            }
            finally { Object.DestroyImmediate(go); if (danger != null) Object.DestroyImmediate(danger); }
            return shot;
        }

        public static Color32[] Render(Camera cam, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1 };
            cam.targetTexture = rt; cam.Render();
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            var px = tex.GetPixels32();
            RenderTexture.active = prev; cam.targetTexture = null; rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
            return px;
        }

        /// <summary>Himmel = Sichtstrahl trifft nichts (Raster 96×54).</summary>
        public static bool[] SkyMask(Camera cam)
        {
            var m = new bool[MaskW * MaskH];
            for (int y = 0; y < MaskH; y++) for (int x = 0; x < MaskW; x++)
            {
                var ray = cam.ViewportPointToRay(new Vector3((x + 0.5f) / MaskW, (y + 0.5f) / MaskH, 0));
                m[y * MaskW + x] = !Physics.Raycast(ray, 2500f, ~0, QueryTriggerInteraction.Ignore);
            }
            return m;
        }

        /// <summary>Test-Gefahrenfläche (wie GroundTelegraph: Bodenmarker, Rot-Orange, Füllung 60 %) vor dem Lager.</summary>
        static GameObject SpawnTestDanger(CameraBookmark b)
        {
            var lib = AssetDatabase.LoadAssetAtPath<VfxLibrary>(PTEditorUtil.DataDir + "/VfxLibrary.asset");
            if (lib == null || lib.GroundMarker == null) return null;
            var fwd = Quaternion.Euler(b.Euler) * Vector3.forward; fwd.y = 0; fwd.Normalize();
            var p = b.Position + fwd * 6.5f;
            if (Physics.Raycast(new Vector3(p.x, 900f, p.z), Vector3.down, out var hit, 1800f, 1, QueryTriggerInteraction.Ignore)) p = hit.point;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "PT_G1_TestDanger"; q.transform.position = p + Vector3.up * 0.06f; q.transform.rotation = Quaternion.Euler(90f, 0, 0); q.transform.localScale = new Vector3(6f, 6f, 1f);
            var r = q.GetComponent<Renderer>(); r.sharedMaterial = lib.GroundMarker;
            var mpb = new MaterialPropertyBlock(); mpb.SetColor("_Color", new Color(1f, 0.35f, 0.1f, 0.85f)); mpb.SetFloat("_Fill", 0.6f); mpb.SetFloat("_Pulse", 0f); r.SetPropertyBlock(mpb);
            return q;
        }

        /// <summary>ΔE zwischen Flächenrand (innen, 0,9–0,98 r) und Boden (1,1–1,3 r) aus dem Bild.</summary>
        public static Color LastInner, LastOuter; public static string LastSamples = "";
        public static float DangerDeltaE(Camera cam, Color32[] px, Vector3 center, float radius)
        {
            var inner = new List<int>(); var outer = new List<int>();
            for (int i = 0; i < 64; i++)
            {
                float a = i / 64f * Mathf.PI * 2f; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                foreach (var f in new[] { 0.975f, 0.99f }) Add(inner, cam.WorldToViewportPoint(center + d * radius * f + Vector3.up * 0.06f));
                foreach (var f in new[] { 1.15f, 1.3f }) Add(outer, cam.WorldToViewportPoint(center + d * radius * f));
            }
            if (inner.Count < 8 || outer.Count < 8) return -1f;
            var ci = Mean(px, inner); var co = Mean(px, outer);
            LastInner = ci; LastOuter = co; LastSamples = inner.Count + "/" + outer.Count;
            return DangerReadability.DeltaE(ci, co);
        }

        static void Add(List<int> l, Vector3 s) { int x = Mathf.RoundToInt(s.x * W), y = Mathf.RoundToInt(s.y * Hgt); if (s.z > 0 && x >= 0 && y >= 0 && x < W && y < Hgt) l.Add(y * W + x); }
        static Color Mean(Color32[] px, List<int> idx) { double r = 0, g = 0, b = 0; foreach (var i in idx) { r += px[i].r; g += px[i].g; b += px[i].b; } int n = idx.Count; return new Color((float)(r / n / 255), (float)(g / n / 255), (float)(b / n / 255)); }

        static void WriteCsv(string path, List<Shot> shots, bool append)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var sb = new StringBuilder();
            if (!append || !File.Exists(path)) sb.AppendLine("zone;lesezeichen;" + MetricsResult.CsvHeader + ";gefahr_deltaE");
            foreach (var s in shots) sb.AppendLine(s.Zone + ";" + s.Name + ";" + s.M.ToCsv() + ";" + s.DeltaE.ToString("0.0").Replace(',', '.'));
            if (append) File.AppendAllText(path, sb.ToString()); else File.WriteAllText(path, sb.ToString());
        }

        /// <summary>Ziel-Helligkeit einer Zone (rein): Vorher-Mittel, begrenzt auf 0,36–0,6 (dunkle Zonen heller, gleißende dunkler).</summary>
        public static float TargetLuminance(float before) => Mathf.Clamp(before, 0.36f, 0.6f);

        /// <summary>Belichtungs-Korrektur in EV (rein, begrenzt ±1,5): log2(Ziel/Ist) mit Dämpfung 0,8.</summary>
        public static float ExposureStep(float target, float measured) => Mathf.Clamp(Mathf.Log(Mathf.Max(target, 0.01f) / Mathf.Max(measured, 0.01f), 2f) * 0.8f, -1.5f, 1.5f);

        /// <summary>
        /// G7: misst je Zone die mittlere Helligkeit (Wegmarke, Wahrzeichen, Horizont; 320×180) und passt `ExposureCalibration` in zwei
        /// Schritten an das Ziel an. Vorher-Werte aus metrics_vorher.csv, sonst 0,5.
        /// </summary>
        [MenuItem("Project Tyrant/Grafik G7/Belichtung einmessen")]
        public static string CalibrateExposure()
        {
            var before = new Dictionary<string, List<float>>();
            string csv = Path.Combine(Root, "metrics_vorher.csv");
            if (File.Exists(csv))
                foreach (var line in File.ReadAllLines(csv).Skip(1))
                {
                    var c = line.Split(';'); if (c.Length < 11) continue;
                    if (c[1] != "Wegmarke" && c[1] != "Wahrzeichen" && c[1] != "Horizont") continue;
                    if (!before.ContainsKey(c[0])) before[c[0]] = new List<float>();
                    before[c[0]].Add(float.Parse(c[10], System.Globalization.CultureInfo.InvariantCulture));
                }
            var lib = AssetDatabase.LoadAssetAtPath<GraphicsLibrary>(GraphicsBuilder.LibraryPath);
            int prevLevel = QualitySettings.GetQualityLevel(); QualitySettings.SetQualityLevel(3, true);
            var log = new List<string>();
            try
            {
                foreach (var (zone, _, _) in GraphicsBuilder.ZoneBiomes)
                {
                    var z = WorldBuilder.Zone(zone); if (z == null) continue;
                    string path = GraphicsBuilder.ScenePathOf(z); if (!File.Exists(path)) continue;
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Single);   // entlädt ungenutzte Assets: Profil und Lesezeichen danach laden
                    lib = AssetDatabase.LoadAssetAtPath<GraphicsLibrary>(GraphicsBuilder.LibraryPath);
                    var p = GraphicsBuilder.Profile(zone); if (p == null) continue;
                    var marks = GraphicsBuilder.Bookmarks(zone).Where(b => b.Kind == BookmarkKind.Wegmarke || b.Kind == BookmarkKind.Wahrzeichen || b.Kind == BookmarkKind.Horizont).ToList();
                    if (marks.Count == 0) continue;
                    float target = TargetLuminance(before.TryGetValue(zone, out var bl) && bl.Count > 0 ? bl.Average() : 0.5f);
                    float lum = 0f;
                    for (int it = 0; it < 3; it++)
                    {
                        p.Volume = GraphicsBuilder.ZoneVolume(p);
                        lum = 0f;
                        foreach (var b in marks)
                        {
                            var go = new GameObject("PT_Calib"); go.tag = "MainCamera";
                            try
                            {
                                var cam = go.AddComponent<Camera>(); var d = go.AddComponent<UniversalAdditionalCameraData>(); d.renderPostProcessing = true; d.stopNaN = true;
                                cam.fieldOfView = b.Fov; cam.farClipPlane = 3000f; cam.aspect = 16f / 9f; go.transform.SetPositionAndRotation(b.Position, Quaternion.Euler(b.Euler));
                                SceneLighting.Apply(p, 11f, WeatherModel.LookOf(WeatherState.Klar), lib);
                                var px = Render(cam, 320, 180); double s = 0; foreach (var c in px) s += (0.2126 * c.r + 0.7152 * c.g + 0.0722 * c.b) / 255.0;
                                lum += (float)(s / px.Length);
                            }
                            finally { Object.DestroyImmediate(go); }
                        }
                        lum /= marks.Count;
                        float step = ExposureStep(target, lum);
                        if (Mathf.Abs(step) < 0.05f) break;
                        p.ExposureCalibration = Mathf.Clamp(p.ExposureCalibration + step, -2f, 2.5f);
                    }
                    p.Volume = GraphicsBuilder.ZoneVolume(p); EditorUtility.SetDirty(p);
                    log.Add(zone + " " + lum.ToString("0.00") + "→" + target.ToString("0.00") + " (" + p.ExposureCalibration.ToString("+0.00;-0.00") + " EV)");
                }
            }
            finally { QualitySettings.SetQualityLevel(prevLevel, true); }
            AssetDatabase.SaveAssets();
            return string.Join(" · ", log);
        }

        [MenuItem("Project Tyrant/Grafik G1/Captures vorher")]
        public static void CaptureBeforeMenu() => Debug.Log("[ProjectTyrant] Captures vorher: " + CaptureAll("vorher").Count);
    }
}
