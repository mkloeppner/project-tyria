using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1: Laufzeit-Verweise auf Kunstprofile, Element-Paletten, Lesezeichen und erzeugte Texturen (vom Builder gefüllt, am GameManager).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Graphics/Graphics Library")]
    public class GraphicsLibrary : ScriptableObject
    {
        public List<ZoneArtProfile> Profiles = new List<ZoneArtProfile>();
        public List<ElementPalette> Elements = new List<ElementPalette>();
        public List<CameraBookmark> Bookmarks = new List<CameraBookmark>();
        public Texture2D Noise, Clouds, Stars, Caustics, LeafCookie, Ramp, FlowMap, Worley;
        public Material Sky;
        [Tooltip("G1: repräsentative Skills je Klasse für Vorher/Nachher-Aufnahmen (Waffe 1–5, Heilung, Elite)")] public List<SkillDefinition> ShowcaseSkills = new List<SkillDefinition>();
        [Tooltip("G1: Klassen-Id je Eintrag in ShowcaseSkills")] public List<string> ShowcaseClasses = new List<string>();
        [Tooltip("G1: Reittiere für Vorher/Nachher-Aufnahmen")] public List<MountDefinition> Mounts = new List<MountDefinition>();
        [Tooltip("VFX-Lauf V2+: Element-Baukästen (Flipbooks, Materialien, GPU-Vorlagen)")] public ElementSim.ElementFxLibrary ElementFx;
        public static GraphicsLibrary Current => GameManager.Instance != null ? GameManager.Instance.GraphicsLib : null;

        public ZoneArtProfile Profile(string zoneId) { foreach (var p in Profiles) if (p != null && p.ZoneId == zoneId) return p; return null; }
        public ElementPalette Element(FxElement e) { foreach (var p in Elements) if (p != null && p.Element == e) return p; return null; }
        public CameraBookmark Bookmark(string key)
        {
            foreach (var b in Bookmarks) if (b != null && (b.Key == key || b.Key.ToLowerInvariant() == key.ToLowerInvariant())) return b;
            return null;
        }
    }
}
