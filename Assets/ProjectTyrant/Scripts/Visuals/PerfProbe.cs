using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G1/G7: misst je Kamera-Lesezeichen Bildrate, CPU-/GPU-Zeit, Draw Calls, SetPass, Dreiecke und Partikel.
    /// Ergebnis: Temp/PT_QA/G1/perf_&lt;tag&gt;.csv. Gestartet über /gfx perf (alle Zonen) oder /gfx perf zone.
    /// </summary>
    public static class PerfProbe
    {
        public const int SampleFrames = 90;
        public static bool Running { get; private set; }
        public static string Status = "";

        public struct Sample
        {
            public string Zone, Name; public float AvgFps, MinFps, CpuMs, GpuMs; public long DrawCalls, SetPass, Triangles; public int Particles;
            public string ToCsv() => string.Join(";", Zone, Name, AvgFps.ToString("0.0", Inv), MinFps.ToString("0.0", Inv), CpuMs.ToString("0.00", Inv), GpuMs.ToString("0.00", Inv), DrawCalls, SetPass, Triangles, Particles);
            public const string Header = "zone;lesezeichen;fps_mittel;fps_min;cpu_ms;gpu_ms;draw_calls;setpass;dreiecke;partikel";
        }
        static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

        /// <summary>Kennzahlen aus einer Liste von Bildzeiten (Sekunden): Mittel und schlechtestes 1-%-Bild als FPS.</summary>
        public static void FpsOf(IList<float> dt, out float avg, out float min)
        {
            avg = 0f; min = 0f; if (dt == null || dt.Count == 0) return;
            float sum = 0f; foreach (var d in dt) sum += d;
            avg = sum > 0f ? dt.Count / sum : 0f;
            var sorted = dt.OrderByDescending(d => d).ToList();
            int k = Mathf.Max(1, Mathf.CeilToInt(sorted.Count * 0.01f));
            float worst = 0f; for (int i = 0; i < k; i++) worst += sorted[i];
            worst /= k; min = worst > 0f ? 1f / worst : 0f;
        }

        public static string Start(string tag, bool currentZoneOnly)
        {
            var gm = GameManager.Instance; var lib = GraphicsLibrary.Current;
            if (gm == null || lib == null) return "Keine Grafik-Bibliothek";
            if (Running) return "Messung läuft bereits: " + Status;
            var marks = lib.Bookmarks.Where(b => b != null && b.ZoneId != "klassen" && (!currentZoneOnly || (gm.CurrentZone != null && b.ZoneId == gm.CurrentZone.Id)))
                .OrderBy(b => b.ZoneId == (gm.CurrentZone != null ? gm.CurrentZone.Id : "") ? 0 : 1).ThenBy(b => b.ZoneId).ToList();
            gm.StartCoroutine(Run(marks, string.IsNullOrEmpty(tag) ? "vorher" : tag));
            return marks.Count + " Lesezeichen werden gemessen → Temp/PT_QA/G1/perf_" + tag + ".csv";
        }

        static IEnumerator Run(List<CameraBookmark> marks, string tag)
        {
            Running = true; ZoneGate.QaBypass = true;
            int vsync = QualitySettings.vSyncCount, target = Application.targetFrameRate;
            QualitySettings.vSyncCount = 0; Application.targetFrameRate = -1;   // ungedrosselt messen, sonst deckelt die Bildwiederholrate
            var gm = GameManager.Instance;
            var rows = new List<string> { Sample.Header };
            var draw = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            var setp = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            var tris = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            var timings = new FrameTiming[1];
            string path = "Temp/PT_QA/G1/perf_" + tag + ".csv";
            System.IO.Directory.CreateDirectory("Temp/PT_QA/G1");
            int n = 0;
            foreach (var b in marks)
            {
                n++; Status = n + "/" + marks.Count + " " + b.Key;
                if (gm.CurrentZone == null || gm.CurrentZone.Id != b.ZoneId)
                {
                    gm.ChangeZone(b.ZoneId, null, null);
                    for (int i = 0; i < 900 && (gm.CurrentZone == null || gm.CurrentZone.Id != b.ZoneId); i++) yield return null;
                    yield return new WaitForSecondsRealtime(2f);
                }
                if (gm.CurrentZone == null || gm.CurrentZone.Id != b.ZoneId) { Debug.LogWarning("[ProjectTyrant] Leistungsmessung: Zone " + b.ZoneId + " nicht erreicht – " + b.Key + " übersprungen"); continue; }
                GraphicsAdminCommands.Apply(b);
                for (int i = 0; i < 30; i++) yield return null;   // Einschwingen (Streaming, Culling)
                var dt = new List<float>(SampleFrames);
                double cpu = 0, gpu = 0; long dc = 0, sp = 0, tr = 0; int timed = 0, parts = 0;
                for (int i = 0; i < SampleFrames; i++)
                {
                    yield return null;
                    dt.Add(Time.unscaledDeltaTime);
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0) { cpu += timings[0].cpuFrameTime; gpu += timings[0].gpuFrameTime; timed++; }
                    dc = System.Math.Max(dc, draw.LastValue); sp = System.Math.Max(sp, setp.LastValue); tr = System.Math.Max(tr, tris.LastValue);
                    if (i == SampleFrames / 2) parts = CountParticles();
                }
                FpsOf(dt, out float avg, out float min);
                var s = new Sample { Zone = b.ZoneId, Name = b.Name, AvgFps = avg, MinFps = min, CpuMs = timed > 0 ? (float)(cpu / timed) : 0f, GpuMs = timed > 0 ? (float)(gpu / timed) : 0f, DrawCalls = dc, SetPass = sp, Triangles = tr, Particles = parts };
                rows.Add(s.ToCsv());
                System.IO.File.WriteAllLines(path, rows);   // nach jedem Lesezeichen sichern
            }
            draw.Dispose(); setp.Dispose(); tris.Dispose();
            GraphicsAdminCommands.FreezeCamera(false);
            Status = "fertig: " + (rows.Count - 1) + " Lesezeichen → " + path;
            Debug.Log("[ProjectTyrant] Leistungsmessung " + Status);
            QualitySettings.vSyncCount = vsync; Application.targetFrameRate = target;
            ZoneGate.QaBypass = false; Running = false;
        }

        public static int CountParticles()
        {
            int c = 0;
            foreach (var p in Object.FindObjectsByType<ParticleSystem>()) if (p != null && p.isPlaying) c += p.particleCount;
            return c;
        }
    }
}
