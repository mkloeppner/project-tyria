using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Zonenabschluss-Zählung (x = erledigt/entdeckt, y = gesamt) aus Markern oder Zahlen.</summary>
    public struct ZoneCompletion
    {
        public Vector2Int Waypoints, Pois, Vistas, Hearts, Heroes;

        public int Done => Waypoints.x + Pois.x + Vistas.x + Hearts.x + Heroes.x;
        public int Total => Waypoints.y + Pois.y + Vistas.y + Hearts.y + Heroes.y;
        public float Percent => Total == 0 ? 0f : Done / (float)Total;
        public bool Complete => Total > 0 && Done >= Total;

        public static ZoneCompletion Count(List<MapMarkerInfo> markers)
        {
            var c = new ZoneCompletion();
            foreach (var m in markers)
            {
                switch (m.Kind)
                {
                    case MapMarkerKind.Waypoint: c.Waypoints += new Vector2Int(m.State != MarkerState.Undiscovered ? 1 : 0, 1); break;
                    case MapMarkerKind.PointOfInterest: c.Pois += new Vector2Int(m.State == MarkerState.Done ? 1 : 0, 1); break;
                    case MapMarkerKind.Vista: c.Vistas += new Vector2Int(m.State == MarkerState.Done ? 1 : 0, 1); break;
                    case MapMarkerKind.Heart: c.Hearts += new Vector2Int(m.State == MarkerState.Done ? 1 : 0, 1); break;
                    case MapMarkerKind.HeroChallenge: c.Heroes += new Vector2Int(m.State == MarkerState.Done ? 1 : 0, 1); break;
                }
            }
            return c;
        }
    }
}
