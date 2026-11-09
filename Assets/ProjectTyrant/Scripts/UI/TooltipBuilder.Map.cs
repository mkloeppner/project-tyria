using UnityEngine;

namespace ProjectTyrant
{
    public static partial class TooltipBuilder
    {
        public static string StateName(MarkerState s)
        {
            switch (s)
            {
                case MarkerState.Done: return "Erledigt";
                case MarkerState.Undiscovered: return "Unentdeckt";
                case MarkerState.Contested: return "Umkämpft";
                case MarkerState.Inactive: return "Inaktiv";
            }
            return "Offen";
        }

        public static TooltipContent MapMarker(MapMarkerInfo m, string description)
        {
            var c = new TooltipContent { Title = string.IsNullOrEmpty(m.Label) ? MapMarkerRegistry.KindName(m.Kind) : m.Label, TitleColor = UiFactory.AccentColor };
            c.Subtitle = MapMarkerRegistry.KindName(m.Kind) + (m.Kind == MapMarkerKind.Event || m.Kind == MapMarkerKind.Ally || m.Kind == MapMarkerKind.EventBoss ? "" : " · " + StateName(m.State));
            if (!string.IsNullOrEmpty(description)) c.Add(description, Gray);
            var prof = PlayerProfile.Instance;
            if (m.Kind == MapMarkerKind.Waypoint && prof != null && prof.Player != null)
            {
                if (m.State == MarkerState.Contested) { c.Footer = "Umkämpft – Reisen nicht möglich"; c.FooterColor = new Color(1f, 0.45f, 0.35f); }
                else if (m.State == MarkerState.Undiscovered) { c.Footer = "Noch nicht entdeckt"; c.FooterColor = Gray; }
                else
                {
                    c.Add("Reisekosten: " + WalletModel.Format(prof.ZoneTravelCost(prof.Player.transform.position, m.Position, false)), new Color(0.9f, 0.85f, 0.6f));
                    c.Footer = "Klicken: Schnellreise"; c.FooterColor = new Color(0.6f, 0.9f, 1f);
                }
            }
            return c;
        }
    }
}
