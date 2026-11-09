using ProjectTyrant.Procedural;
using UnityEngine;
using static ProjectTyrant.EditorTools.PTEditorUtil;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Setzt die statischen Karten-Marker in der Szene (Wegmarke, Sehenswürdigkeiten). Zonenabschluss-Objekte folgen in M12.</summary>
    public static class MapMarkerBuilder
    {
        public static MapMarker Add(GameObject go, MapMarkerKind kind, string label, string desc = null, float radius = 0f)
        {
            var m = go.GetComponent<MapMarker>(); if (m == null) m = go.AddComponent<MapMarker>();
            m.Kind = kind; m.Label = label; m.Description = desc; m.Radius = radius;
            return m;
        }

        public static void PlaceMarkers(ZoneAnchors anchors, Transform npcRoot)
        {
            // Wegmarken, Sehenswürdigkeiten, Aussichtspunkte, Heldenherausforderungen und Herzen sind Zonen-Objekte (eigene Marker).
            ZoneContentBuilder.Build(anchors, npcRoot);
        }
    }
}
