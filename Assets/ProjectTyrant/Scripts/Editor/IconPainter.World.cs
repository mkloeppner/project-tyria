using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Lauf 4: Karten-Symbole für Portal, Beherrschungs-Einblick und Weltboss.</summary>
    public partial class IconPainter
    {
        static partial void PaintLater()
        {
            var outline = new Color(0.05f, 0.05f, 0.07f, 0.9f);
            // Portal: Torbogen mit violettem Schleier
            Save("map_portal", new IconPainter(64)
                .Fill(Union(Box(32, 24, 20, 20, 3), Circle(32, 42, 20)), outline)
                .Fill(Union(Box(32, 23, 16, 17, 2), Circle(32, 40, 16)), new Color(0.55f, 0.4f, 0.95f))
                .Fill(Union(Box(32, 21, 9, 13, 1), Circle(32, 35, 9)), new Color(0.85f, 0.8f, 1f)));
            // Beherrschungs-Einblick: Kristall-Raute in Grün-Türkis
            Save("map_insight", new IconPainter(64)
                .Fill(Polygon(new Vector2(32, 60), new Vector2(10, 32), new Vector2(32, 4), new Vector2(54, 32)), outline)
                .Fill(Polygon(new Vector2(32, 54), new Vector2(15, 32), new Vector2(32, 10), new Vector2(49, 32)), new Color(0.3f, 0.85f, 0.7f))
                .Fill(Polygon(new Vector2(32, 46), new Vector2(23, 32), new Vector2(32, 18)), new Color(0.85f, 1f, 0.95f)));
            // Weltboss: roter Schädel-Kreis mit Hörnern
            Save("map_worldboss", new IconPainter(64)
                .Fill(Union(Circle(32, 30, 22), Polygon(new Vector2(8, 58), new Vector2(14, 34), new Vector2(24, 44)), Polygon(new Vector2(56, 58), new Vector2(50, 34), new Vector2(40, 44))), outline)
                .Fill(Circle(32, 30, 18), new Color(0.85f, 0.18f, 0.12f))
                .Fill(Circle(25, 32, 4.5f), outline).Fill(Circle(39, 32, 4.5f), outline)
                .Fill(Box(32, 19, 6, 2, 1), outline));
            PaintMounts();
            PaintMastery();
            PaintStory();   // Lauf 6
        }

        static void PaintMastery()
        {
            var ink = new Color(0.08f, 0.06f, 0.06f, 1f);
            var sand = new Color(0.95f, 0.85f, 0.55f);
            Save("mastery_glide", MountBg(sand).Fill(Polygon(new Vector2(14, 70), new Vector2(64, 92), new Vector2(114, 70), new Vector2(64, 78)), sand)
                .Fill(Segment(30, 72, 60, 40, 2.5f), ink).Fill(Segment(98, 72, 68, 40, 2.5f), ink).Fill(Circle(64, 36, 7), new Color(0.6f, 0.75f, 0.95f)));
            var green = new Color(0.45f, 0.9f, 0.55f);
            Save("mastery_env", MountBg(green).Fill(Union(Circle(64, 70, 30), Box(64, 40, 10, 22, 4)), new Color(0.75f, 0.35f, 0.85f))
                .Fill(Box(64, 36, 7, 20, 3), new Color(0.92f, 0.88f, 0.78f)).Fill(Circle(52, 76, 5), Color.white).Fill(Circle(76, 66, 4), Color.white));
            var blue = new Color(0.6f, 0.8f, 1f);
            Save("mastery_cmd", MountBg(blue).Fill(Box(64, 44, 4, 34, 2), new Color(0.6f, 0.45f, 0.3f))
                .Fill(Polygon(new Vector2(68, 98), new Vector2(110, 86), new Vector2(68, 70)), blue).Fill(Circle(64, 22, 10), new Color(1f, 0.82f, 0.4f)));
        }

        static IconPainter MountBg(Color c) => new IconPainter(128).Fill(Circle(64, 64, 62), new Color(0.06f, 0.05f, 0.07f, 0.95f)).Fill(Ring(64, 64, 58, 4), c);

        static void PaintMounts()
        {
            var ink = new Color(0.08f, 0.06f, 0.06f, 1f);
            // Raptor: Kopf mit Kamm im Profil
            var orange = new Color(0.9f, 0.55f, 0.25f);
            Save("mount_raptor", MountBg(orange)
                .Fill(Union(Polygon(new Vector2(30, 40), new Vector2(60, 70), new Vector2(98, 74), new Vector2(104, 62), new Vector2(70, 52), new Vector2(46, 30)), Circle(84, 70, 16)), orange)
                .Fill(Polygon(new Vector2(70, 84), new Vector2(78, 100), new Vector2(84, 86), new Vector2(92, 98), new Vector2(94, 84)), new Color(0.35f, 0.55f, 0.95f))
                .Fill(Circle(88, 74, 4), ink).Fill(Segment(84, 62, 104, 64, 2.5f), ink));
            // Springer: Hasenkopf mit langen Ohren
            var tan = new Color(0.78f, 0.66f, 0.52f);
            Save("mount_springer", MountBg(new Color(0.65f, 0.45f, 0.75f))
                .Fill(Union(Circle(64, 52, 26), Box(50, 92, 8, 26, 7), Box(78, 92, 8, 26, 7)), tan)
                .Fill(Box(50, 92, 4, 20, 3), new Color(0.95f, 0.75f, 0.75f)).Fill(Box(78, 92, 4, 20, 3), new Color(0.95f, 0.75f, 0.75f))
                .Fill(Circle(54, 56, 4), ink).Fill(Circle(74, 56, 4), ink).Fill(Circle(64, 44, 4), new Color(0.9f, 0.5f, 0.55f)));
            // Skimmer: Rochen von oben
            var teal = new Color(0.3f, 0.65f, 0.7f);
            Save("mount_skimmer", MountBg(new Color(0.4f, 0.85f, 0.95f))
                .Fill(Polygon(new Vector2(14, 64), new Vector2(64, 96), new Vector2(114, 64), new Vector2(64, 44)), teal)
                .Fill(Segment(64, 46, 64, 14, 3.5f), teal).Fill(Circle(56, 78, 4), ink).Fill(Circle(72, 78, 4), ink)
                .Fill(Circle(44, 64, 4), new Color(0.95f, 0.85f, 0.4f)).Fill(Circle(84, 64, 4), new Color(0.95f, 0.85f, 0.4f)));
        }
    }
}
