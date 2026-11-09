using UnityEngine;

namespace ProjectTyrant
{
    public enum WeatherKind { None, Pollen, MistWisps, Snow, AshWind }
    public enum AmbientKind { Meadow, Swamp, Wind, Embers }

    /// <summary>Biom-Profil: Gelände-Rauschen, Wasser, Farben, Nebel, Himmel, Post-Processing, Vegetation, Wetter, Ambient.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Biome Profile")]
    public class BiomeProfile : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [Header("Gelände")]
        public float Amplitude = 12f;
        public float Frequency = 0.004f;
        public int Octaves = 4;
        [Tooltip("Terrassen-Stufe in m (0 = aus) – Plateaus")] public float Terrace;
        [Tooltip("Tiefe geriffelter Schluchten (Lavarisse, Kanäle)")] public float CanyonDepth;
        public float CanyonFrequency = 0.006f;
        public float BaseHeight = 2f;
        public float WaterLevel = -100f;
        public float EdgeMountainHeight = 30f;
        [Header("Farben")]
        public Color GroundLow = new Color(0.35f, 0.55f, 0.25f);
        public Color GroundMid = new Color(0.45f, 0.62f, 0.3f);
        public Color GroundHigh = new Color(0.6f, 0.6f, 0.55f);
        public Color Cliff = new Color(0.45f, 0.43f, 0.42f);
        public Color Shore = new Color(0.6f, 0.55f, 0.4f);
        public Color Water = new Color(0.25f, 0.45f, 0.55f, 0.85f);
        [Header("Atmosphäre")]
        public Color FogColor = new Color(0.75f, 0.82f, 0.9f);
        public float FogDensity = 0.004f;
        public Color SkyTop = new Color(0.35f, 0.55f, 0.9f), SkyHorizon = new Color(0.8f, 0.85f, 0.9f), SkyBottom = new Color(0.6f, 0.65f, 0.6f);
        public Color SunColor = new Color(1f, 0.95f, 0.85f);
        public float SunIntensity = 1.2f;
        public float Saturation = 10f;
        public Color SplitShadows = new Color(0.45f, 0.5f, 0.6f), SplitHighlights = new Color(0.65f, 0.6f, 0.5f);
        [Header("Vegetation & Wetter")]
        [Range(0, 1)] public float TreeDensity = 0.5f;
        [Range(0, 1)] public float RockDensity = 0.3f;
        public Color TreeColor = new Color(0.3f, 0.55f, 0.25f);
        public Color TrunkColor = new Color(0.4f, 0.28f, 0.18f);
        public bool Conifers;
        public bool DeadTrees;
        public WeatherKind Weather;
        public AmbientKind Ambient;
    }
}
