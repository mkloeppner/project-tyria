using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1/G7: Schalter der eigenen Render-Features und Anwendung einer Qualitätsstufe zur Laufzeit.</summary>
    public static class GraphicsSettingsRuntime
    {
        static readonly Dictionary<string, bool> _features = new Dictionary<string, bool>
        {
            ["Nebel"] = true, ["Outline"] = true, ["Lichtschaft"] = true, ["Verzerrung"] = true, ["Unterwasser"] = true, ["Schaden"] = true, ["SSAO"] = true, ["Wetter"] = true, ["Umgebung"] = true,
        };
        public static IEnumerable<string> FeatureNames => _features.Keys;
        public static QualityTier Tier { get; private set; } = QualityTier.Hoch;
        public static float AmbientDensity = 1f, WeatherDensity = 1f;
        /// <summary>G7: automatische Leistungsanpassung (Optionen › Grafik).</summary>
        public static bool AutoGovernor = true;
        public static System.Action Changed;

        public static bool Feature(string name) => !_features.TryGetValue(name, out bool on) || on;

        public static bool SetFeature(string name, bool on)
        {
            var key = _features.Keys.FirstOrDefault(k => k.ToLowerInvariant() == (name ?? "").ToLowerInvariant());
            if (key == null) return false;
            _features[key] = on; Changed?.Invoke(); return true;
        }

        public static void ApplyTier(QualityTier t)
        {
            Tier = t; var s = QualityTiers.Of(t);
            AmbientDensity = s.AmbientFx; WeatherDensity = s.WeatherParticles;
            _features["SSAO"] = s.Ssao; _features["Verzerrung"] = s.Distortion;
            Changed?.Invoke();
        }

        public static string Summary() => "Features: " + string.Join(" ", _features.Select(kv => kv.Key + (kv.Value ? "+" : "−")));
    }
}
