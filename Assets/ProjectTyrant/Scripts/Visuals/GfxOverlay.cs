using System.Linq;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1: Admin-Overlay /gfx show – FPS, CPU-/GPU-Zeit, Partikel, Draw Calls (Editor), Lichter, VFX, Stufe, Wetter, Tageszeit.</summary>
    public class GfxOverlay : MonoBehaviour
    {
        public static bool Visible => _inst != null && _inst.enabled;
        static GfxOverlay _inst;
        float _next; string _text = ""; float _fps;
        readonly FrameTiming[] _ft = new FrameTiming[1];

        public static void Toggle()
        {
            if (_inst == null) { var go = new GameObject("PT_GfxOverlay"); DontDestroyOnLoad(go); _inst = go.AddComponent<GfxOverlay>(); return; }
            _inst.enabled = !_inst.enabled;
        }

        void Update()
        {
            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(1e-4f, Time.unscaledDeltaTime), 0.1f);
            FrameTimingManager.CaptureFrameTimings();
            if (Time.unscaledTime < _next) return; _next = Time.unscaledTime + 0.5f;
            int particles = 0; foreach (var ps in FindObjectsByType<ParticleSystem>()) particles += ps.particleCount;
            int lights = FindObjectsByType<Light>().Count(l => l.isActiveAndEnabled);
            double cpu = 0, gpu = 0; if (FrameTimingManager.GetLatestTimings(1, _ft) > 0) { cpu = _ft[0].cpuFrameTime; gpu = _ft[0].gpuFrameTime; }
            string stats = "";
#if UNITY_EDITOR
            stats = " · Draw Calls " + UnityEditor.UnityStats.drawCalls + " · SetPass " + UnityEditor.UnityStats.setPassCalls + " · Dreiecke " + (UnityEditor.UnityStats.triangles / 1000) + "k";
#endif
            var d = GraphicsDirector.Instance;
            _text = "FPS " + _fps.ToString("0") + " · CPU " + cpu.ToString("0.0") + " ms · GPU " + gpu.ToString("0.0") + " ms" + stats +
                    "\nPartikel " + particles + " · Lichter " + lights + " · Stufe " + QualityTiers.FromUnityLevel(QualitySettings.GetQualityLevel()) + " (" + QualitySettings.names[QualitySettings.GetQualityLevel()] + ")" +
                    (d != null ? "\nTageszeit " + d.Time.EffectiveHour.ToString("00.0") + " h" + (d.Weather != null ? " · Wetter " + d.Weather.Current + (d.Weather.Locked ? " (gesperrt)" : "") : "") : "") +
                    "\n" + GraphicsSettingsRuntime.Summary();
        }

        void OnGUI()
        {
            var r = new Rect(10, Screen.height - 110, 760, 100);
            GUI.color = new Color(0, 0, 0, 0.6f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 8, r.y + 6, r.width - 16, r.height - 12), _text);
        }
    }
}
