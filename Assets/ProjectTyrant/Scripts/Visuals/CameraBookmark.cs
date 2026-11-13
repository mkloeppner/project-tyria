using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1: feste Kamera-Position je Zone (Vorher/Nachher-Vergleich, Performance-Messung).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Graphics/Camera Bookmark")]
    public class CameraBookmark : ScriptableObject
    {
        public string ZoneId, Name;
        public BookmarkKind Kind;
        public Vector3 Position;
        public Vector3 Euler;
        public float Fov = 60f;
        [Tooltip("Klasse (nur Skill-Showcase)")] public string ClassId = "";
        public string Key => ZoneId + "/" + Name;
    }
}
