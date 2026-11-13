using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G4: Laternen und Fenster leuchten in der Dämmerung (rein).</summary>
    public static class EmissiveSchedule
    {
        public const float OnHour = 18.5f, OffHour = 6.5f, Ramp = 0.75f;
        /// <summary>Leuchtstärke 0–1: an ab 18:30, aus ab 6:30, je 45 min weicher Übergang.</summary>
        public static float Level(float hour)
        {
            hour = Mathf.Repeat(hour, 24f);
            if (hour >= OnHour) return Mathf.Clamp01((hour - OnHour) / Ramp);
            if (hour < OffHour - Ramp) return 1f;
            if (hour < OffHour) return Mathf.Clamp01((OffHour - hour) / Ramp);
            return 0f;
        }
    }

    /// <summary>Grafik-Lauf G7: automatische Degradation mit Hysterese (rein). 3 s unter 45 FPS → eine Stufe runter (nur Umgebung + Wetter), 10 s über 55 FPS → wieder hoch.</summary>
    public class PerformanceGovernor
    {
        public const float LowFps = 45f, HighFps = 55f, DownAfter = 3f, UpAfter = 10f;
        public const int MaxSteps = 2;
        public int Steps { get; private set; }
        float _low, _high;
        /// <summary>Faktor für Umgebungs-VFX und Wetter-Partikel (1, 0,6, 0,3). Gefahrenflächen und Telegrafen sind nie betroffen.</summary>
        public float AmbientFactor => Steps == 0 ? 1f : Steps == 1 ? 0.6f : 0.3f;

        /// <summary>Rückgabe: −1 Stufe runter, +1 Stufe hoch, 0 unverändert.</summary>
        public int Tick(float fps, float dt)
        {
            if (fps < LowFps) { _low += dt; _high = 0f; } else if (fps > HighFps) { _high += dt; _low = 0f; } else { _low = 0f; _high = 0f; }
            if (_low >= DownAfter && Steps < MaxSteps) { Steps++; _low = 0f; return -1; }
            if (_high >= UpAfter && Steps > 0) { Steps--; _high = 0f; return 1; }
            return 0;
        }
    }

    /// <summary>Einstellungen je Qualitätsstufe (rein, Tabelle aus dem Lastenheft Abschnitt 9).</summary>
    public struct TierSettings
    {
        public int Cascades; public float ShadowDistance; public bool SoftShadows, AdditionalShadows, Ssao, Bloom, Grading, Distortion, Refraction, Foam, HighCaustics;
        public int VfxMaxLayer; public float AmbientFx, WeatherParticles, RenderScale; public int ParticleCap;
    }

    public static class QualityTiers
    {
        public static TierSettings Of(QualityTier t)
        {
            switch (t)
            {
                case QualityTier.Niedrig: return new TierSettings { Cascades = 1, ShadowDistance = 40f, VfxMaxLayer = 1, AmbientFx = 0.25f, WeatherParticles = 0.3f, RenderScale = 0.75f, ParticleCap = 4000 };
                case QualityTier.Mittel: return new TierSettings { Cascades = 2, ShadowDistance = 80f, SoftShadows = true, Ssao = true, Bloom = true, Grading = true, Refraction = true, VfxMaxLayer = 2, AmbientFx = 0.6f, WeatherParticles = 0.6f, RenderScale = 0.85f, ParticleCap = 10000 };
                case QualityTier.Ultra: return new TierSettings { Cascades = 4, ShadowDistance = 200f, SoftShadows = true, AdditionalShadows = true, Ssao = true, Bloom = true, Grading = true, Distortion = true, Refraction = true, Foam = true, HighCaustics = true, VfxMaxLayer = 99, AmbientFx = 1.3f, WeatherParticles = 1f, RenderScale = 1f, ParticleCap = 30000 };
                default: return new TierSettings { Cascades = 4, ShadowDistance = 150f, SoftShadows = true, Ssao = true, Bloom = true, Grading = true, Distortion = true, Refraction = true, Foam = true, VfxMaxLayer = 99, AmbientFx = 1f, WeatherParticles = 1f, RenderScale = 1f, ParticleCap = 20000 };
            }
        }

        /// <summary>Stufe → Unity-Qualitätsstufe (Niedrig 1, Mittel 2, Hoch 3, Ultra 5).</summary>
        public static int UnityLevelOf(QualityTier t) => t == QualityTier.Niedrig ? 1 : t == QualityTier.Mittel ? 2 : t == QualityTier.Ultra ? 5 : 3;

        /// <summary>Unity-Qualitätsstufe (Index in QualitySettings) → Stufe.</summary>
        public static QualityTier FromUnityLevel(int level) => level <= 1 ? QualityTier.Niedrig : level == 2 ? QualityTier.Mittel : level <= 4 ? QualityTier.Hoch : QualityTier.Ultra;

        /// <summary>Vorschlag beim ersten Start aus GPU-Speicher (MB) und Benchmark-Frame (ms).</summary>
        public static QualityTier Suggest(int gpuMemoryMb, float benchmarkMs)
        {
            if (gpuMemoryMb < 2000 || benchmarkMs > 25f) return QualityTier.Niedrig;
            if (gpuMemoryMb < 4000 || benchmarkMs > 14f) return QualityTier.Mittel;
            if (gpuMemoryMb >= 10000 && benchmarkMs < 6f) return QualityTier.Ultra;
            return QualityTier.Hoch;
        }
    }

    /// <summary>Barrierefreiheit „Lichtempfindlich“ (rein): Blitze und Bloom-Spitzen gedeckelt, Kamera-Wackeln aus.</summary>
    public static class Photosensitive
    {
        public static bool Enabled;
        public const float FlashCap = 0.15f, BloomCap = 0.35f;
        public static float ClampFlash(float intensity) => Enabled ? Mathf.Min(intensity, FlashCap) : intensity;
        public static float ClampBloom(float intensity) => Enabled ? Mathf.Min(intensity, BloomCap) : intensity;
        public static float Shake(float amplitude) => Enabled ? 0f : amplitude;
    }
}
