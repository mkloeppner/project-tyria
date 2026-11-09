using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>Item-Symbole je Typ (prozedural, SDF). Seltenheit wird über den Slot-Rahmen gezeigt.</summary>
    public partial class IconPainter
    {
        static partial void PaintExtra()
        {
            PaintItems();
            PaintMore();
        }

        static partial void PaintMore();

        static IconPainter ItemBg(Color tint)
        {
            var p = new IconPainter(64);
            p.FillGradient(Box(32, 32, 30, 30, 7), Color.Lerp(new Color(0.22f, 0.2f, 0.25f), tint, 0.35f), new Color(0.07f, 0.06f, 0.08f));
            return p;
        }

        static void PaintItems()
        {
            var steel = new Color(0.82f, 0.85f, 0.9f); var gold = new Color(0.95f, 0.75f, 0.3f); var wood = new Color(0.55f, 0.36f, 0.2f);
            var leather = new Color(0.6f, 0.4f, 0.25f); var red = new Color(0.85f, 0.25f, 0.2f);
            // Großschwert: diagonale Klinge mit Parierstange
            Save("item_greatsword", ItemBg(steel).Fill(Polygon(new Vector2(14, 14), new Vector2(18, 10), new Vector2(54, 46), new Vector2(56, 56), new Vector2(46, 54)), steel)
                .Fill(Segment(10, 24, 24, 10, 4.5f), gold).Fill(Segment(8, 8, 15, 15, 4f), wood).Fill(Circle(7, 7, 3.5f), gold));
            // Stab: Schaft + leuchtender Kristall
            Save("item_staff", ItemBg(new Color(0.3f, 0.5f, 1f)).Fill(Segment(12, 8, 46, 48, 4f), wood)
                .Fill(Polygon(RegularPolygon(49, 51, 8, 4, 45f)), new Color(0.4f, 0.8f, 1f)).Fill(Circle(49, 51, 3f), Color.white).Fill(Ring(49, 51, 11, 1.5f), new Color(1f, 0.6f, 0.2f, 0.8f)));
            // Rüstung
            Save("item_armor_head", ItemBg(steel).Fill(Union(Circle(32, 30, 17), Box(32, 20, 17, 6, 2)), steel).Fill(Box(32, 30, 12, 2.5f, 1), new Color(0.15f, 0.15f, 0.2f)).Fill(Box(32, 47, 2.5f, 6, 1), red));
            Save("item_armor_shoulders", ItemBg(steel).Fill(Circle(18, 34, 12), steel).Fill(Circle(46, 34, 12), steel).Fill(Box(32, 30, 8, 4, 1), leather));
            Save("item_armor_chest", ItemBg(steel).Fill(Polygon(new Vector2(14, 50), new Vector2(50, 50), new Vector2(46, 12), new Vector2(18, 12)), steel).Fill(Box(32, 32, 2, 16, 1), gold).Fill(Circle(32, 50, 7), new Color(0.15f, 0.12f, 0.15f)));
            Save("item_armor_hands", ItemBg(leather).Fill(Box(30, 26, 12, 14, 5), leather).Fill(Box(26, 44, 3, 7, 2), leather).Fill(Box(32, 44, 3, 8, 2), leather).Fill(Box(38, 42, 3, 6, 2), leather).Fill(Box(30, 14, 13, 3, 1), steel));
            Save("item_armor_legs", ItemBg(steel).Fill(Box(32, 46, 14, 4, 1), leather).Fill(Box(24, 26, 6, 18, 2), steel).Fill(Box(40, 26, 6, 18, 2), steel));
            Save("item_armor_feet", ItemBg(leather).Fill(Union(Box(26, 34, 7, 16, 3), Box(34, 14, 15, 5, 3)), leather).Fill(Box(34, 10, 16, 2, 1), steel));
            // Schmuck
            Save("item_trinket_back", ItemBg(red).Fill(Polygon(new Vector2(18, 52), new Vector2(46, 52), new Vector2(52, 10), new Vector2(12, 10)), red).Fill(Box(32, 52, 16, 3, 1), gold));
            Save("item_trinket_amulet", ItemBg(gold).Fill(Ring(32, 40, 16, 2.5f), gold).Fill(Polygon(RegularPolygon(32, 22, 10, 6)), new Color(0.9f, 0.3f, 0.35f)).Fill(Circle(32, 22, 4), new Color(1f, 0.7f, 0.7f)));
            Save("item_trinket_ring", ItemBg(gold).Fill(Ring(32, 28, 14, 5), gold).Fill(Polygon(RegularPolygon(32, 44, 8, 4, 45f)), new Color(0.4f, 0.9f, 0.6f)));
            Save("item_trinket_accessory", ItemBg(gold).Fill(Ring(32, 38, 10, 3), gold).Fill(Segment(32, 28, 32, 16, 2.5f), gold).Fill(Polygon(new Vector2(32, 6), new Vector2(39, 16), new Vector2(32, 22), new Vector2(25, 16)), new Color(0.5f, 0.6f, 1f)));
            // Verbrauch
            Save("item_potion", ItemBg(red).Fill(Circle(32, 24, 15), new Color(0.85f, 0.85f, 0.95f, 0.6f)).Fill(Circle(32, 22, 12), red).Fill(Box(32, 44, 5, 8, 1), new Color(0.85f, 0.85f, 0.95f, 0.7f)).Fill(Box(32, 54, 6, 3, 1), wood));
            Save("item_food", ItemBg(gold).Fill(Box(32, 22, 22, 9, 8), new Color(0.75f, 0.55f, 0.35f)).Fill(Circle(32, 32, 16), new Color(0.9f, 0.6f, 0.25f)).Fill(Circle(26, 36, 3), new Color(0.6f, 0.15f, 0.2f)).Fill(Circle(37, 34, 3), new Color(0.6f, 0.15f, 0.2f)));
            Save("item_container", ItemBg(gold).Fill(Box(32, 26, 22, 14, 3), wood).Fill(Box(32, 44, 22, 6, 4), new Color(0.65f, 0.45f, 0.25f)).Fill(Box(32, 34, 23, 2, 0), gold).Fill(Box(32, 32, 4, 5, 1), gold));
            Save("item_material", ItemBg(steel).Fill(Polygon(RegularPolygon(26, 28, 13, 5, 10f)), new Color(0.55f, 0.55f, 0.6f)).Fill(Polygon(RegularPolygon(42, 22, 9, 6, 0f)), new Color(0.75f, 0.6f, 0.45f)).Fill(Circle(40, 42, 6), new Color(0.8f, 0.85f, 1f)));
            Save("item_junk", ItemBg(new Color(0.4f, 0.4f, 0.4f)).Fill(Polygon(new Vector2(20, 14), new Vector2(46, 18), new Vector2(42, 48), new Vector2(24, 44)), new Color(0.5f, 0.47f, 0.42f)).Fill(Segment(26, 22, 38, 40, 2f), new Color(0.3f, 0.28f, 0.25f)));
            Save("item_bag", ItemBg(leather).Fill(Circle(32, 26, 18), leather).Fill(Box(32, 44, 9, 6, 2), leather).Fill(Box(32, 40, 11, 2, 1), gold).Glyphs("B", 32, 24, 16, new Color(1, 1, 1, 0.6f)));
            Save("item_kit", ItemBg(steel).Fill(Box(32, 22, 20, 10, 3), red).Fill(Segment(20, 34, 44, 52, 4), steel).Fill(Segment(44, 34, 20, 52, 4), wood));
            var sil = new IconPainter(64);
            sil.Fill(Union(Circle(32, 56, 6), Box(32, 38, 10, 11, 4), Box(18, 38, 3, 10, 2), Box(46, 38, 3, 10, 2), Box(27, 14, 4, 13, 2), Box(37, 14, 4, 13, 2)), Color.white);
            Save("ui_hero_silhouette", sil);
            Save("item_unknown", ItemBg(Color.gray).Glyphs("?", 32, 32, 30, Color.white));
        }
    }
}
