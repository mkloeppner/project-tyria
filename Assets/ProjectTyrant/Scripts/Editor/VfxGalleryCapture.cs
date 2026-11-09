using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 5 / M23: QA-Hilfe für Paletten-Captures in PT_VfxGallery (nur Play Mode).
    /// Blendet alle anderen Paletten aus, ordnet die 13 Formen im 5×3-Raster vor der Kamera an und startet sie gleichzeitig.</summary>
    public static class VfxGalleryCapture
    {
        public static int Prepare(string palette, float timeScale = 0.4f)
        {
            GameObject row = null;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.parent == null && t.name.StartsWith("Palette_")) { bool on = t.name == "Palette_" + palette; t.gameObject.SetActive(on); if (on) row = t.gameObject; }
            if (row == null) return 0;
            var loopers = row.GetComponentsInChildren<VfxGalleryLooper>();
            for (int i = 0; i < loopers.Length; i++)
            {
                var l = loopers[i]; int c = i % 5, r = i / 5;
                l.transform.position = new Vector3(c * 6f, l.transform.position.y, 100f + (2 - r) * 6.5f);
                l.Offset = 0f;
            }
            foreach (var ps in row.GetComponentsInChildren<ParticleSystem>()) ps.Clear(true);
            foreach (var l in loopers) { l.Instance.transform.position = l.transform.position; l.Instance.Play(null, l.transform.position, l.transform.rotation, null, 1f, 1f); }
            Time.timeScale = timeScale;
            var cam = Camera.main; cam.transform.position = new Vector3(12f, 13f, 91f); cam.transform.rotation = Quaternion.Euler(42f, 0f, 0f); cam.fieldOfView = 62f;
            return loopers.Length;
        }

        /// <summary>Simulation anhalten und (optional) die echte Game-View aufnehmen – Kamera-Renders hätten keine Opaque-Textur für die Verzerrung.</summary>
        public static int Freeze(string capture = null)
        {
            Time.timeScale = 0f; int n = 0;
            if (!string.IsNullOrEmpty(capture)) ScreenCapture.CaptureScreenshot("Temp/PT_QA/" + capture + ".png");
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>()) n += ps.particleCount;
            return n;
        }

        // ---- 8-Bild-Serien je VFX-Familie (Form): EditorApplication.update tritt Bild für Bild weiter, ScreenCapture nimmt die Game-View auf.
        static VfxGalleryLooper[] _series; static int _shape, _frame; static double _start; static float _dur; static string _palette;
        public static bool SeriesRunning => _series != null;

        public static int StartSeries(string palette)
        {
            Time.timeScale = 1f; _palette = palette;
            if (Prepare(palette, 1f) == 0) return 0;
            var row = GameObject.Find("Palette_" + palette);
            _series = row.GetComponentsInChildren<VfxGalleryLooper>(); _shape = -1; NextShape();
            UnityEditor.EditorApplication.update -= Step; UnityEditor.EditorApplication.update += Step;
            return _series.Length;
        }

        static void NextShape()
        {
            _shape++; _frame = 0;
            if (_shape >= _series.Length) { UnityEditor.EditorApplication.update -= Step; _series = null; return; }
            for (int i = 0; i < _series.Length; i++) { _series[i].gameObject.SetActive(i == _shape); _series[i].enabled = false; }
            var l = _series[_shape];
            foreach (var ps in l.GetComponentsInChildren<ParticleSystem>()) ps.Clear(true);
            l.Instance.transform.position = l.transform.position;
            l.Instance.Play(null, l.transform.position, l.transform.rotation, null, 1f, 1f);
            _dur = Mathf.Clamp(l.Period - 0.8f, 0.5f, 2.8f); _start = UnityEditor.EditorApplication.timeSinceStartup;
            var cam = Camera.main; cam.transform.position = l.transform.position + new Vector3(0f, 4.5f, -7.5f); cam.transform.rotation = Quaternion.Euler(28f, 0f, 0f); cam.fieldOfView = 55f;
        }

        static void Step()
        {
            if (_series == null || !Application.isPlaying) { UnityEditor.EditorApplication.update -= Step; _series = null; return; }
            double t = UnityEditor.EditorApplication.timeSinceStartup - _start;
            var l = _series[_shape];
            if (l.Orbit > 0f) { float a = (float)t * 2.4f; l.Instance.transform.position = l.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * l.Orbit; }
            if (_frame >= 8) { if (t > _dur + 0.4) NextShape(); return; }   // ScreenCapture schreibt erst am Ende eines späteren Spielbilds: kurz warten, dann wechseln
            if (t < (_frame + 0.5) * _dur / 8.0) return;
            ScreenCapture.CaptureScreenshot("Temp/PT_QA/_series/m23_series_" + l.name + "_" + _frame + ".png");
            _frame++;
        }
    }
}
