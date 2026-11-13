using System;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Grafik-Lauf G1: die zehn Effekt-Elemente (Identität aus Farbe, Form, Bewegung, Nachwirkung).</summary>
    public enum FxElement { Feuer, Eis, Blitz, Natur, Schatten, Licht, Jade, Sand, Chaos, Arkan }

    public enum ElementMotion { Steigend, Fallend, Wirbelnd, Zuckend, Stroemend, Wachsend, Saugend, Strahlend, Traege, Schillernd }

    public enum WeatherState { Klar, Bewoelkt, Regen, Gewitter, Nieselnebel, Schnee, Schneesturm, Sandsturm, Aschefall, Jaderegen }

    public enum BookmarkKind { Wegmarke, Wahrzeichen, Kampf, Innenraum, WasserHuerde, Horizont, SkillShowcase }

    public enum QualityTier { Niedrig = 0, Mittel = 1, Hoch = 2, Ultra = 3 }

    public enum AmbientFxKind { Gluehwuermchen, Pollen, Schmetterlinge, Blaetter, Sporen, Schneeflocken, Glut, Asche, Sand, Jadestaub, Lichtstaub, Rauch, Voegel, Tropfen, Kristallpuls, Windstreifen }

    public enum TextureKind { Perlin, Simplex, Worley, Voronoi, Ridged, DomainWarp, Caustics, FlowMap, Ramp, Flipbook, CookieLeaves, CookieGrid, Starfield, Clouds }

    public enum FlipbookShape { Explosion, Rauch, Feuer, Blitz, Spritzer, Schneeflocke, Blatt, Funke, Glyphe }

    /// <summary>Zuordnung der 27 Lauf-5-Paletten auf die zehn Elemente (rein).</summary>
    public static class ElementMap
    {
        public static FxElement Of(VfxPalette p)
        {
            switch (p)
            {
                case VfxPalette.Feuer: case VfxPalette.Glut: case VfxPalette.Lava: case VfxPalette.Richtglut: case VfxPalette.Pulver: return FxElement.Feuer;
                case VfxPalette.Frost: case VfxPalette.Wasser: case VfxPalette.Mondlicht: case VfxPalette.Bollwerk: return FxElement.Eis;
                case VfxPalette.Luft: case VfxPalette.Gewitter: case VfxPalette.Sturmblau: case VfxPalette.Himmel: return FxElement.Blitz;
                case VfxPalette.Natur: case VfxPalette.Smaragd: case VfxPalette.Dorngift: case VfxPalette.Wildblut: return FxElement.Natur;
                case VfxPalette.Schatten: case VfxPalette.Tiefviolett: return FxElement.Schatten;
                case VfxPalette.Licht: case VfxPalette.Stahl: return FxElement.Licht;
                case VfxPalette.Jade: return FxElement.Jade;
                case VfxPalette.Erde: case VfxPalette.Staubgold: return FxElement.Sand;
                case VfxPalette.Blut: case VfxPalette.Zwielicht: return FxElement.Chaos;
                case VfxPalette.Arkan: return FxElement.Arkan;
                default: return FxElement.Licht;
            }
        }
    }

    /// <summary>Gefahren-Sprache (Regel 5): diese Farbtöne gehören Flächen und Telegrafen, nie einer Zonen-Bodenfarbe.</summary>
    public static class DangerLanguage
    {
        public static readonly Color Danger = new Color(1f, 0.32f, 0.12f);      // Rot-Orange
        public static readonly Color DangerEdge = new Color(1f, 0.9f, 0.75f);
        public static readonly Color Heal = new Color(0.45f, 1f, 0.6f);         // Grün-Weiß
        public static readonly Color Ally = new Color(0.3f, 0.6f, 1f);          // Blau
        public static readonly Color Telegraph = Color.white;

        /// <summary>Liegt eine Farbe zu nah an der Gefahren- oder Heil-Sprache (ΔE &lt; 20)?</summary>
        public static bool Conflicts(Color c) => DangerReadability.DeltaE(c, Danger) < 20f || DangerReadability.DeltaE(c, Heal) < 20f;
    }
}
