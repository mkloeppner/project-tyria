using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>
    /// Grafik-Lauf G1: Lesbarkeits-Klassifikator (rein). Farbabstand ΔE (CIE76 in Lab) zwischen Flächenrand und umgebendem Boden,
    /// Mindestwert 25 (Farbfehlsichtigkeits-Paletten 20); Luminanz-Sprung je Frame für den Lichtempfindlich-Modus.
    /// </summary>
    public static class DangerReadability
    {
        public const float MinDeltaE = 25f, MinDeltaEColorblind = 20f;
        /// <summary>Höchster mittlerer Luminanz-Sprung zwischen zwei Frames im Lichtempfindlich-Modus (0–1).</summary>
        public const float MaxFlashJump = 0.1f;

        public static Vector3 ToLab(Color c)
        {
            float R = Lin(c.r), G = Lin(c.g), B = Lin(c.b);
            float X = (R * 0.4124f + G * 0.3576f + B * 0.1805f) / 0.95047f;
            float Y = (R * 0.2126f + G * 0.7152f + B * 0.0722f);
            float Z = (R * 0.0193f + G * 0.1192f + B * 0.9505f) / 1.08883f;
            float fx = F(X), fy = F(Y), fz = F(Z);
            return new Vector3(116f * fy - 16f, 500f * (fx - fy), 200f * (fy - fz));
        }

        static float Lin(float v) { v = Mathf.Clamp01(v); return v <= 0.04045f ? v / 12.92f : Mathf.Pow((v + 0.055f) / 1.055f, 2.4f); }
        static float F(float t) => t > 0.008856f ? Mathf.Pow(t, 1f / 3f) : 7.787f * t + 16f / 116f;

        public static float DeltaE(Color a, Color b) => (ToLab(a) - ToLab(b)).magnitude;

        public static bool Readable(Color edge, Color ground, bool colorblind = false) => DeltaE(edge, ground) >= (colorblind ? MinDeltaEColorblind : MinDeltaE);

        /// <summary>Mittlere Farbe eines Bildbereichs (Pixel innerhalb Maske).</summary>
        public static Color Mean(Color32[] px, System.Func<int, bool> include)
        {
            double r = 0, g = 0, b = 0; int n = 0;
            for (int i = 0; i < px.Length; i++) if (include(i)) { r += px[i].r; g += px[i].g; b += px[i].b; n++; }
            return n == 0 ? Color.black : new Color((float)(r / n / 255.0), (float)(g / n / 255.0), (float)(b / n / 255.0));
        }

        /// <summary>Größter Sprung der mittleren Luminanz in einer Frame-Serie (0–1).</summary>
        public static float MaxLuminanceJump(float[] frameLuma)
        {
            float m = 0f; if (frameLuma == null) return 0f;
            for (int i = 1; i < frameLuma.Length; i++) m = Mathf.Max(m, Mathf.Abs(frameLuma[i] - frameLuma[i - 1]));
            return m;
        }
    }
}
