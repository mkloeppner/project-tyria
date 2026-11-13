using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1: Rezept einer prozeduralen Textur (deterministisch über Seed, von TextureForge erzeugt).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Graphics/Texture Recipe")]
    public class TextureRecipe : ScriptableObject
    {
        public string Id;
        public TextureKind Kind;
        public int Size = 256;
        public int Seed = 1;
        public float Scale = 8f;
        public int Octaves = 4;
        [Tooltip("Flipbook: Kacheln je Seite (4–8)")] public int Tiles = 4;
        public FlipbookShape Shape;
        public Gradient Ramp;
        public bool Srgb = true, Mipmaps = true, Repeat = true;
        [Tooltip("Hash der letzten Erzeugung (Determinismus-Prüfung)")] public string Hash = "";
    }
}
