using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G1: Kunstprofil einer Zone – Palette (5 Hauptfarben + Licht/Schatten), Licht, Himmel, Nebel, Post-Processing, Wetter,
    /// Umgebungs-Effekte. Gemeinsame Quelle für Shader, Nebel-Pass, Volume und Umgebungs-VFX.
    /// </summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Graphics/Zone Art Profile")]
    public class ZoneArtProfile : ScriptableObject
    {
        public string ZoneId, Biome;
        [Header("Palette")]
        public Color Ground = Color.gray, VegetationRock = Color.gray, Sky = Color.cyan, Accent = Color.yellow, Hazard = Color.magenta;
        public Color LightColor = Color.white, ShadowColor = new Color(0.45f, 0.5f, 0.75f);
        [Header("Licht")]
        [Tooltip("Sonnen-Azimut (Grad)")] public float SunAzimuth = 135f;
        [Tooltip("Sonnenhöhe zu Mittag (Grad)")] public float SunNoonElevation = 55f;
        public float SunIntensity = 1.3f;
        public Color AmbientSky = Color.gray, AmbientEquator = Color.gray, AmbientGround = Color.gray;
        [Tooltip("Licht von unten (Lava): Anteil am Boden-Ambient")] public float UnderLight;
        [Header("Himmel")]
        public Color SkyZenith = new Color(0.3f, 0.5f, 0.9f), SkyHorizon = new Color(0.8f, 0.85f, 0.9f), SkyGround = new Color(0.5f, 0.5f, 0.5f);
        public Color CloudColor = Color.white;
        [Range(0, 1)] public float CloudCover = 0.4f;
        public bool Stars = true, Aurora, AshSky, SandVeil, Underground;
        [Header("Nebel")]
        public Color FogColor = new Color(0.75f, 0.8f, 0.9f);
        [Tooltip("Distanz-Dichte (exp)")] public float FogDensity = 0.004f;
        [Tooltip("Höhennebel: Basis (m über Wasser/Boden-Niveau) und Abfall")] public float FogHeight = 8f, FogHeightFalloff = 0.08f;
        [Range(0, 1)] public float SunScatter = 0.5f;
        [Header("Post-Processing")]
        public float Exposure = 0.1f, Contrast = 12f, Saturation = 10f;
        [Tooltip("G7: eingemessene Belichtungs-Korrektur (EV) aus den Captures; vom Biom-Builder nicht überschrieben")] public float ExposureCalibration;
        [Tooltip("Ziel-Sättigung der Captures (HSV, 0–1)")] public float TargetSaturation = 0.42f;
        public float WhiteBalance = 0f;
        public Color SplitShadows = new Color(0.45f, 0.5f, 0.65f), SplitHighlights = new Color(0.65f, 0.6f, 0.5f);
        public float BloomThreshold = 1.1f, BloomIntensity = 0.5f, Vignette = 0.22f;
        [Header("Welt")]
        public Color OutlineColor = new Color(0.12f, 0.1f, 0.16f);
        public float WindStrength = 0.6f;
        public Color WaterShallow = new Color(0.3f, 0.7f, 0.75f), WaterDeep = new Color(0.08f, 0.22f, 0.38f);
        public List<WeatherState> Weathers = new List<WeatherState> { WeatherState.Klar, WeatherState.Bewoelkt };
        public List<AmbientFxKind> AmbientFx = new List<AmbientFxKind>();

        [System.Serializable] public struct AmbientVolume { public Vector3 Center; public float Radius; public AmbientFxKind Kind; }
        [Header("Umgebungs-Volumen G4 (vom Builder, ≥ 4 je Zone)")]
        public List<AmbientVolume> AmbientVolumes = new List<AmbientVolume>();

        [Header("Grafik G2 (vom Builder)")]
        public Material SkyMaterial;
        public UnityEngine.Rendering.VolumeProfile Volume;

        public Color[] MainPalette => new[] { Ground, VegetationRock, Sky, Accent, Hazard };
    }
}
