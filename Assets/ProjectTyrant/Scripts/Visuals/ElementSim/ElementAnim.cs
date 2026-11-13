using UnityEngine;

namespace ProjectTyrant.Visuals.ElementSim
{
    /// <summary>Form-Animationen der Element-Lagen (rein, testbar): Wasser fällt und weitet sich, Ringe laufen nach außen, Säulen steigen und sinken.</summary>
    public static class ElementAnim
    {
        /// <summary>Ein- und Ausblenden: 8 % hoch, ab 65 % weich aus (Nachklang länger als die Spitze).</summary>
        public static float Fade(float t) => t < 0.08f ? t / 0.08f : t > 0.65f ? Mathf.Clamp01(1f - (t - 0.65f) / 0.35f) : 1f;

        static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        public static Vector3 Scale(VfxLayerKind kind, string content, float radius, float t)
        {
            t = Mathf.Clamp01(t); float r = Mathf.Max(0.05f, radius);
            switch (content)
            {
                case "WaveRing": case "ShockDisc": { float s = r * Mathf.Lerp(0.25f, 1f, EaseOut(t)); return new Vector3(s, Mathf.Min(r * 0.35f, 1.2f) * (1f - t * 0.7f), s); }   // flach: Ring, keine Wand
                case "SplashCrown":
                    {
                        float xz = r * Mathf.Lerp(0.4f, 1.15f, EaseOut(t));
                        float y = r * (t < 0.35f ? Mathf.Lerp(0.15f, 1f, t / 0.35f) : Mathf.Lerp(1f, 0.25f, (t - 0.35f) / 0.65f));   // steigt, fällt zurück
                        return new Vector3(xz, y, xz);
                    }
                case "WaterColumn": { float y = r * (t < 0.4f ? EaseOut(t / 0.4f) : 1f - EaseOut((t - 0.4f) / 0.6f) * 0.9f); return new Vector3(r * 0.6f, Mathf.Max(0.02f, y), r * 0.6f); }
                case "LensSphere": { float s = r * 2f * (t < 0.15f ? EaseOut(t / 0.15f) : 1f); return Vector3.one * s; }
                case "BladeArc": return Vector3.one * r;
            }
            if (kind == VfxLayerKind.WorldMark) { float s = r * 2f * (content == "nass" ? EaseOut(Mathf.Clamp01(t * 3f)) : Mathf.Lerp(0.7f, 1f, EaseOut(Mathf.Clamp01(t * 6f)))); return new Vector3(s, 1f, s); }
            if (kind == VfxLayerKind.Sim) { float s = r * 2f; return new Vector3(s, s, s); }
            return Vector3.one * r;
        }
    }
}
