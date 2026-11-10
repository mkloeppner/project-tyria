using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 6: Karten-Symbole Persönliche Geschichte (grüner Stern) und Späher (Fernrohr).</summary>
    public partial class IconPainter
    {
        static void PaintStory()
        {
            var outline = new Color(0.04f, 0.06f, 0.04f, 0.95f);
            var pts = new Vector2[10]; var inner = new Vector2[10];
            for (int i = 0; i < 10; i++) { float a = Mathf.PI / 2f + i * Mathf.PI / 5f; float r = i % 2 == 0 ? 29f : 12f; pts[i] = new Vector2(32 + Mathf.Cos(a) * r, 32 + Mathf.Sin(a) * r); inner[i] = new Vector2(32 + Mathf.Cos(a) * (r - 5f), 32 + Mathf.Sin(a) * (r - 5f)); }
            Save("map_story", new IconPainter(64).Fill(Polygon(pts), outline).Fill(Polygon(inner), new Color(0.35f, 0.95f, 0.45f)).Fill(Circle(32, 32, 5), new Color(0.85f, 1f, 0.85f)));
            Save("map_scout", new IconPainter(64)
                .Fill(Circle(32, 32, 26), outline)
                .Fill(Circle(32, 32, 22), new Color(0.35f, 0.6f, 0.85f))
                .Fill(Segment(16, 42, 46, 22, 6f), new Color(0.9f, 0.8f, 0.5f))
                .Fill(Circle(46, 22, 5f), new Color(0.75f, 0.9f, 1f))
                .Fill(Segment(28, 34, 22, 14, 2.5f), outline).Fill(Segment(28, 34, 36, 14, 2.5f), outline));
        }
    }
}
