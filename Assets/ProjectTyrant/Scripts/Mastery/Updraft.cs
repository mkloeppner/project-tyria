using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Aufwind-Säule: hebt gleitende Spieler an (Beherrschung „Gleiten 2“). Sichtbar als aufsteigende Partikel.</summary>
    public class Updraft : MonoBehaviour, IMapMarkerProvider
    {
        public float Radius = 4.5f, Height = 45f, Strength = 9f;
        void OnEnable() => MapMarkerRegistry.Register(this);
        void OnDisable() => MapMarkerRegistry.Unregister(this);

        void Update()
        {
            var mc = MountController.Local;   // Lauf 6: Aufwinde heben auch den Greif
            if (mc != null && ProjectTyrant.Flight.FlightRules.IsFlyer(mc.Motor.Mounted))   // Lauf 10: Greif und Himmelsschuppe
            {
                var q = mc.transform.position - transform.position;
                if (q.y >= -2f && q.y <= Height) { q.y = 0f; if (q.sqrMagnitude <= Radius * Radius) mc.ApplyUpdraft(Strength); }
                return;
            }
            var a = MasteryAbilities.Local;
            if (a == null || !a.Gliding) return;
            var p = a.transform.position - transform.position;
            if (p.y < -2f || p.y > Height) return;
            p.y = 0f;
            if (p.sqrMagnitude <= Radius * Radius) a.ApplyUpdraft(Strength);
        }

        public void CollectMarkers(System.Collections.Generic.List<MapMarkerInfo> into) { }
        public TooltipContent MarkerTooltip(MapMarkerInfo info) => null;
    }
}
