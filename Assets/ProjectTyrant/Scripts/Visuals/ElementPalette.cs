using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1: Identität eines Elements – Farben (Kern HDR für Bloom), Partikelform, Bewegung, Trail, Einschlag, Nachwirkung, Klang.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Graphics/Element Palette")]
    public class ElementPalette : ScriptableObject
    {
        public FxElement Element;
        public Color Primary = Color.white, Secondary = Color.white;
        [ColorUsage(false, true)] public Color Core = Color.white;
        public FlipbookShape Particle;
        public ElementMotion Motion;
        [Tooltip("Trail: Breite, Lebensdauer, Zittern")] public float TrailWidth = 0.3f, TrailTime = 0.25f, TrailJitter;
        [Tooltip("Einschlag: Schockwelle, Splitter, Funken")] public bool ImpactShockwave = true, ImpactShards, ImpactSparks = true;
        [Tooltip("Nachwirkung als Overlay im ToonLit (brennend, vereist, elektrisiert, vergiftet, verderbt)")] public string AfterOverlay = "";
        public string SoundFamily = "";
        public string Description = "";
    }
}
