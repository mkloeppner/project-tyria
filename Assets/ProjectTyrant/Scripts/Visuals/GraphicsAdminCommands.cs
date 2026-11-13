using System.Collections;
using System.Linq;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G1: Admin-Befehle /cam, /gfx, /tod, /weather, /vfx im Debug-Panel (F9). Tageszeit und Wetter steuern den GraphicsDirector (G2/G4),
    /// solange es ihn nicht gibt, melden die Befehle das.
    /// </summary>
    public static class GraphicsAdminCommands
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register()
        {
            DebugCommands.Extra.RemoveAll(e => e.label.StartsWith("Grafik"));
            DebugCommands.Extra.Add(("Grafik: /cam bookmark (Zone/Name)", a => CamBookmark(a)));
            DebugCommands.Extra.Add(("Grafik: /cam release", a => CamRelease()));
            DebugCommands.Extra.Add(("Grafik: /cam capture all", a => CamCaptureAll()));
            DebugCommands.Extra.Add(("Grafik: /gfx show", a => GfxShow()));
            DebugCommands.Extra.Add(("Grafik: /gfx preset (Niedrig|Mittel|Hoch|Ultra)", a => GfxPreset(a)));
            DebugCommands.Extra.Add(("Grafik: /gfx feature (Name on|off)", a => GfxFeature(a)));
            DebugCommands.Extra.Add(("Grafik: /gfx stress (n)", a => GfxStress(a)));
            DebugCommands.Extra.Add(("Grafik: /gfx perf (all|zone) [Tag]", a => GfxPerf(a)));
            DebugCommands.Extra.Add(("Grafik: /tod (0–24 | pause | speed x)", a => Tod(a)));
            DebugCommands.Extra.Add(("Grafik: /weather (Zustand | lock)", a => Weather(a)));
            DebugCommands.Extra.Add(("Grafik: /vfx play (SkillId)", a => VfxPlay(a)));
            DebugCommands.Extra.Add(("Grafik: /vfx capture (Tag) – Skills, Champions, Reittiere", a => ShowcaseCapture.Start(string.IsNullOrEmpty(Strip(a, "/vfx", "capture")) ? "vorher" : Strip(a, "/vfx", "capture"))));
            DebugCommands.Extra.Add(("VFX V1: /vfx strip (Tag) – 5 Phasen + 8-Bild-Streifen je Schaustück", a => ElementSim.SkillStripCapture.Start(Strip(a, "/vfx", "strip"))));
            DebugCommands.Extra.Add(("Grafik: /vfx loop", a => { VfxLoop = !VfxLoop; return "VFX-Schleife " + (VfxLoop ? "an" : "aus"); }));
            DebugCommands.Extra.Add(("Grafik: /vfx slow (Faktor)", a => { float f = Parse(a, 0.25f); Time.timeScale = Mathf.Clamp(f, 0.05f, 1f); return "Zeitlupe " + Time.timeScale; }));
        }

        public static bool VfxLoop;
        static float Parse(string a, float def) { float.TryParse((a ?? "").Trim().Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f); return f == 0f ? def : f; }
        static string Strip(string a, params string[] words) { a = (a ?? "").Trim(); foreach (var w in words) if (a.StartsWith(w, System.StringComparison.OrdinalIgnoreCase)) a = a.Substring(w.Length).Trim(); return a; }

        // ---------------- Kamera ----------------
        static Behaviour[] _frozen;
        public static void FreezeCamera(bool freeze)
        {
            var cam = Camera.main; if (cam == null) return;
            if (freeze)
            {
                if (_frozen != null) return;
                _frozen = cam.GetComponentsInParent<Behaviour>(true).Concat(cam.GetComponents<Behaviour>()).Where(b => b != null && b != cam && b.enabled && (b.GetType().Name.Contains("Camera") || b.GetType().Name.Contains("Cinemachine") || b.GetType().Name.Contains("Rig"))).Distinct().ToArray();
                foreach (var b in _frozen) b.enabled = false;
            }
            else if (_frozen != null) { foreach (var b in _frozen) if (b != null) b.enabled = true; _frozen = null; }
        }

        public static string CamBookmark(string arg)
        {
            arg = Strip(arg, "/cam", "bookmark");
            var lib = GraphicsLibrary.Current; if (lib == null) return "Keine Grafik-Bibliothek";
            var b = lib.Bookmark(arg);
            if (b == null) return "Lesezeichen „" + arg + "“ unbekannt (" + lib.Bookmarks.Count + " vorhanden, z. B. " + (lib.Bookmarks.Count > 0 ? lib.Bookmarks[0].Key : "–") + ")";
            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentZone != null && b.ZoneId != gm.CurrentZone.Id && b.ZoneId != "klassen") { gm.StartCoroutine(TravelThenApply(b)); return "Reise nach " + b.ZoneId + " – dann Lesezeichen " + b.Name; }
            Apply(b); return "Kamera: " + b.Key;
        }

        static IEnumerator TravelThenApply(CameraBookmark b)
        {
            var gm = GameManager.Instance; gm.ChangeZone(b.ZoneId, null, null);
            for (int i = 0; i < 600 && (gm.CurrentZone == null || gm.CurrentZone.Id != b.ZoneId); i++) yield return null;
            yield return new WaitForSeconds(1f);
            Apply(b);
        }

        public static void Apply(CameraBookmark b)
        {
            var cam = Camera.main; if (cam == null) return;
            FreezeCamera(true);
            cam.transform.SetPositionAndRotation(b.Position, Quaternion.Euler(b.Euler)); cam.fieldOfView = b.Fov;
        }

        public static string CamRelease() { FreezeCamera(false); return "Kamera folgt wieder dem Spieler"; }

        public static string CamCaptureAll()
        {
            var gm = GameManager.Instance; var lib = GraphicsLibrary.Current; if (gm == null || lib == null || gm.CurrentZone == null) return "–";
            var marks = lib.Bookmarks.Where(b => b != null && b.ZoneId == gm.CurrentZone.Id).ToList();
            gm.StartCoroutine(CaptureRoutine(marks, "spiel"));
            return marks.Count + " Lesezeichen werden aufgenommen → Temp/PT_QA/G1/spiel/" + gm.CurrentZone.Id;
        }

        static IEnumerator CaptureRoutine(System.Collections.Generic.List<CameraBookmark> marks, string tag)
        {
            var canvases = Object.FindObjectsByType<Canvas>().Where(c => c.enabled && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            foreach (var c in canvases) c.enabled = false;
            foreach (var b in marks)
            {
                Apply(b);
                for (int i = 0; i < 5; i++) yield return null;
                string dir = "Temp/PT_QA/G1/" + tag + "/" + b.ZoneId; System.IO.Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(dir + "/" + b.Name + ".png");
                yield return null; yield return null;
            }
            foreach (var c in canvases) if (c != null) c.enabled = true;
            FreezeCamera(false);
        }

        // ---------------- Grafik ----------------
        public static string GfxShow() { GfxOverlay.Toggle(); return "Grafik-Overlay " + (GfxOverlay.Visible ? "an" : "aus"); }

        public static string GfxPreset(string a)
        {
            a = Strip(a, "/gfx", "preset").ToLowerInvariant();
            QualityTier t = a.StartsWith("n") ? QualityTier.Niedrig : a.StartsWith("m") ? QualityTier.Mittel : a.StartsWith("u") ? QualityTier.Ultra : QualityTier.Hoch;
            int level = t == QualityTier.Niedrig ? 1 : t == QualityTier.Mittel ? 2 : t == QualityTier.Ultra ? 5 : 3;
            QualitySettings.SetQualityLevel(level, true);
            GraphicsSettingsRuntime.ApplyTier(t);
            return "Qualitätsstufe " + t + " (Unity-Stufe " + QualitySettings.names[level] + ")";
        }

        public static string GfxFeature(string a)
        {
            var p = Strip(a, "/gfx", "feature").Split(' ');
            if (p.Length < 2) return "Format: Name on|off – " + string.Join(", ", GraphicsSettingsRuntime.FeatureNames);
            bool on = p[1].ToLowerInvariant() == "on";
            return GraphicsSettingsRuntime.SetFeature(p[0], on) ? p[0] + (on ? " an" : " aus") : "Unbekannt: " + p[0] + " – " + string.Join(", ", GraphicsSettingsRuntime.FeatureNames);
        }

        public static string GfxPerf(string a)
        {
            var p = Strip(a, "/gfx", "perf").Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (PerfProbe.Running) return "Messung: " + PerfProbe.Status;
            bool zone = p.Length > 0 && p[0].ToLowerInvariant() == "zone";
            string tag = p.Length > 1 ? p[1] : (p.Length == 1 && p[0] != "all" && p[0] != "zone" ? p[0] : "vorher");
            return PerfProbe.Start(tag, zone);
        }

        public static string GfxStress(string a) => GraphicsStress.Spawn(Mathf.RoundToInt(Parse(Strip(a, "/gfx", "stress"), 20f)));

        public static string Tod(string a)
        {
            a = Strip(a, "/tod");
            var d = GraphicsDirector.Instance; if (d == null) return "Tageszeit-System nicht aktiv";
            if (a.StartsWith("pause")) { d.Time.Paused = !d.Time.Paused; return "Tageszeit " + (d.Time.Paused ? "angehalten" : "läuft"); }
            if (a.StartsWith("speed")) { d.Time.Speed = Parse(Strip(a, "speed"), 1f); return "Tageszeit-Tempo ×" + d.Time.Speed; }
            float h = Parse(a, -1f); if (h < 0f) return "Uhrzeit " + d.Time.EffectiveHour.ToString("0.0") + " h";
            d.Time.Set(h); d.ApplyNow(); return "Uhrzeit " + h.ToString("0.0") + " h";
        }

        public static string Weather(string a)
        {
            a = Strip(a, "/weather");
            var d = GraphicsDirector.Instance; if (d == null || d.Weather == null) return "Wetter-System nicht aktiv";
            if (a.StartsWith("lock")) { d.Weather.Locked = !d.Weather.Locked; return "Wetter " + (d.Weather.Locked ? "gesperrt" : "frei") + " (" + d.Weather.Current + ")"; }
            foreach (WeatherState s in System.Enum.GetValues(typeof(WeatherState)))
                if (s.ToString().ToLowerInvariant().StartsWith(a.ToLowerInvariant()) && a.Length > 1) { d.Weather.SetState(s, false); return "Wetter → " + s; }
            return "Wetter " + d.Weather.Current + " · Zustände der Zone: " + string.Join(", ", d.Weather.States);
        }

        public static string VfxPlay(string a) => VfxPlayer.PlaySkillById(Strip(a, "/vfx", "play"));
    }
}
