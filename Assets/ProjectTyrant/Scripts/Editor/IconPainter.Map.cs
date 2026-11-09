using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Karten-Marker-Symbole (GW2-artig: Wegmarke blau, Herz golden, Event orange …).</summary>
    public partial class IconPainter
    {
        static partial void PaintMore()
        {
            PaintMap();
            PaintLater();
        }

        static partial void PaintLater();

        static void PaintMap()
        {
            var outline = new Color(0.05f, 0.05f, 0.07f, 0.9f);
            // Wegmarke: Raute mit hellem Kern
            Save("map_waypoint", new IconPainter(64).Fill(Polygon(RegularPolygon(32, 32, 28, 4, 90f)), outline).Fill(Polygon(RegularPolygon(32, 32, 24, 4, 90f)), new Color(0.25f, 0.55f, 1f)).Fill(Polygon(RegularPolygon(32, 32, 11, 4, 90f)), new Color(0.85f, 0.95f, 1f)));
            Save("map_waypoint_contested", new IconPainter(64).Fill(Polygon(RegularPolygon(32, 32, 28, 4, 90f)), outline).Fill(Polygon(RegularPolygon(32, 32, 24, 4, 90f)), new Color(0.85f, 0.2f, 0.15f))
                .Fill(Segment(20, 20, 44, 44, 4), Color.white).Fill(Segment(44, 20, 20, 44, 4), Color.white));
            // POI: Quadrat mit Punkt
            Save("map_poi", new IconPainter(64).Fill(Box(32, 32, 22, 22, 5), outline).Fill(Box(32, 32, 18, 18, 4), new Color(0.95f, 0.8f, 0.35f)).Fill(Circle(32, 32, 6), outline));
            // Aussichtspunkt: Dreieck
            Save("map_vista", new IconPainter(64).Fill(Polygon(new Vector2(32, 58), new Vector2(6, 10), new Vector2(58, 10)), outline).Fill(Polygon(new Vector2(32, 51), new Vector2(12, 14), new Vector2(52, 14)), new Color(0.95f, 0.85f, 0.45f)).Fill(Circle(32, 25, 5), outline));
            // Heldenherausforderung: Pfeil nach oben im Sechseck
            Save("map_hero", new IconPainter(64).Fill(Polygon(RegularPolygon(32, 32, 28, 6, 90f)), outline).Fill(Polygon(RegularPolygon(32, 32, 24, 6, 90f)), new Color(0.35f, 0.75f, 1f)).Fill(Polygon(new Vector2(32, 50), new Vector2(18, 34), new Vector2(27, 34), new Vector2(27, 16), new Vector2(37, 16), new Vector2(37, 34), new Vector2(46, 34)), Color.white));
            // Herz
            Save("map_heart", new IconPainter(64).Fill(Union(Circle(22, 38, 14), Circle(42, 38, 14), Polygon(new Vector2(9, 33), new Vector2(55, 33), new Vector2(32, 6))), outline)
                .Fill(Union(Circle(22, 38, 10.5f), Circle(42, 38, 10.5f), Polygon(new Vector2(12.5f, 34), new Vector2(51.5f, 34), new Vector2(32, 11))), new Color(1f, 0.78f, 0.25f)));
            // Händler: Beutel
            Save("map_vendor", new IconPainter(64).Fill(Circle(32, 26, 20), outline).Fill(Circle(32, 26, 16), new Color(0.75f, 0.55f, 0.3f)).Fill(Box(32, 47, 8, 7, 2), new Color(0.75f, 0.55f, 0.3f)).Fill(Circle(32, 24, 6), new Color(1f, 0.85f, 0.35f)));
            // Event (generisch) und Zieltypen
            Color ev = new Color(1f, 0.6f, 0.15f);
            Save("map_event", EventBase(ev).Fill(Circle(32, 32, 7), Color.white));
            Save("map_ev_kill", EventBase(ev).Fill(Segment(20, 20, 44, 44, 4), Color.white).Fill(Segment(44, 20, 20, 44, 4), Color.white));
            Save("map_ev_defend", EventBase(ev).Fill(Polygon(new Vector2(20, 44), new Vector2(44, 44), new Vector2(44, 30), new Vector2(32, 16), new Vector2(20, 30)), Color.white));
            Save("map_ev_escort", EventBase(ev).Fill(Circle(25, 24, 5), Color.white).Fill(Circle(39, 40, 5), Color.white).Fill(Circle(23, 36, 3), Color.white).Fill(Circle(41, 28, 3), Color.white));
            Save("map_ev_collect", EventBase(ev).Fill(Box(32, 30, 12, 10, 2), Color.white).Fill(Box(32, 30, 13, 1.5f, 0), ev));
            Save("map_ev_capture", EventBase(ev).Fill(Segment(24, 16, 24, 48, 3), Color.white).Fill(Polygon(new Vector2(25, 48), new Vector2(44, 42), new Vector2(25, 36)), Color.white));
            Save("map_boss", EventBase(new Color(0.85f, 0.2f, 0.15f)).Fill(Circle(32, 34, 11), Color.white).Fill(Box(32, 21, 7, 5, 2), Color.white).Fill(Circle(27, 35, 3), outline).Fill(Circle(37, 35, 3), outline));
            Save("map_ally", new IconPainter(32).Fill(Circle(16, 16, 14), outline).Fill(Circle(16, 16, 11), new Color(0.35f, 0.95f, 0.4f)));
            Save("map_target", new IconPainter(32).Fill(Ring(16, 16, 11, 4), new Color(1f, 0.25f, 0.2f)).Fill(Circle(16, 16, 4), new Color(1f, 0.25f, 0.2f)));
            Save("map_personal", new IconPainter(64).Fill(Union(Circle(32, 40, 16), Polygon(new Vector2(18, 34), new Vector2(46, 34), new Vector2(32, 4))), outline).Fill(Union(Circle(32, 40, 12), Polygon(new Vector2(22, 36), new Vector2(42, 36), new Vector2(32, 10))), new Color(0.75f, 0.45f, 1f)));
            // Spieler: weißer Pfeil (zeigt nach oben)
            Save("map_player", new IconPainter(64).Fill(Polygon(new Vector2(32, 60), new Vector2(8, 6), new Vector2(32, 18), new Vector2(56, 6)), outline).Fill(Polygon(new Vector2(32, 53), new Vector2(14, 11), new Vector2(32, 22), new Vector2(50, 11)), Color.white));
        }

        static IconPainter EventBase(Color c) => new IconPainter(64).Fill(Circle(32, 32, 29), new Color(0.05f, 0.05f, 0.07f, 0.9f)).Fill(Circle(32, 32, 25), c);

        static Vector2[] Star(float cx, float cy, float r, float inner, int points)
        {
            var pts = new Vector2[points * 2];
            for (int i = 0; i < points * 2; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / points;
                float rr = i % 2 == 0 ? r : inner;
                pts[i] = new Vector2(cx + Mathf.Cos(a) * rr, cy + Mathf.Sin(a) * rr);
            }
            return pts;
        }
    }
}
