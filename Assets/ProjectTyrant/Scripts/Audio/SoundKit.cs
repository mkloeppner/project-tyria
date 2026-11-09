using System;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M24: Klang-Bausteine für die Rezepte (Whoosh, Aufprall, Glocke, Knistern …) auf Basis von <see cref="Synth"/>.</summary>
    public static class SoundKit
    {
        /// <summary>Rausch-Whoosh: Bandpass gleitet von f0 nach f1.</summary>
        public static float[] Whoosh(float len, float f0, float f1, float q, float amp, Rng r, float attack = 0.3f)
        {
            var b = Synth.Buffer(len);
            Synth.AddPink(b, 0, len, amp, new Adsr(len * attack, len * (1f - attack) * 0.6f, 0f, 0.01f), r);
            Synth.Filter(b, Synth.FilterType.BandPass, t => f0 * (float)Math.Pow(f1 / f0, Math.Min(1f, t / len)), q);
            return b;
        }

        /// <summary>Tiefer Aufprall: Sinus mit Tonhöhen-Abfall plus kurzer Rausch-Anschlag.</summary>
        public static float[] Thump(float len, float f0, float f1, float amp, float noise, Rng r)
        {
            var b = Synth.Buffer(len);
            Synth.AddTone(b, 0, len, Synth.Wave.Sine, t => f1 + (f0 - f1) * (float)Math.Exp(-t * 25f), amp, Adsr.Perc(0.001f, len * 0.4f));
            if (noise > 0f)
            {
                var n = Synth.Buffer(Math.Min(len, 0.15f));
                Synth.AddNoise(n, 0, n.Length / (float)Synth.Rate, noise, Adsr.Perc(0.0005f, 0.03f), r); Synth.Filter(n, Synth.FilterType.LowPass, 2500f);
                Synth.Mix(b, n);
            }
            return b;
        }

        /// <summary>Kurzer Knack (Hochpass-Rauschen).</summary>
        public static float[] Crack(float len, float hp, float amp, Rng r)
        {
            var b = Synth.Buffer(len); Synth.AddNoise(b, 0, len, amp, Adsr.Perc(0.0005f, len * 0.25f), r); Synth.Filter(b, Synth.FilterType.HighPass, hp, 0.8f); Synth.Filter(b, Synth.FilterType.LowPass, Math.Max(hp * 3f, 5000f), 0.7f); return b;
        }

        /// <summary>FM-Glocke(n): mehrere Frequenzen nacheinander (Abstand step).</summary>
        public static float[] Chime(float len, float[] freqs, float step, float ratio, float index, float amp)
        {
            var b = Synth.Buffer(len);
            for (int i = 0; i < freqs.Length; i++) Synth.AddFm(b, (int)(i * step * Synth.Rate), len - i * step, freqs[i], ratio, index, 4f, amp, Adsr.Perc(0.002f, Math.Max(0.05f, (len - i * step) * 0.45f)));
            return b;
        }

        /// <summary>Knistern: zufällige kurze Impulse (Feuer, Glut, Funken).</summary>
        public static void Crackle(float[] b, float density, float amp, Rng r, float hp = 1500f)
        {
            var c = new float[b.Length];
            for (int i = 0; i < b.Length; i++) if (r.Value() < density / Synth.Rate) { int len = r.Range(30, 220); float a = amp * r.Range(0.3f, 1f); for (int k = 0; k < len && i + k < c.Length; k++) c[i + k] += r.Bipolar() * a * (1f - k / (float)len); }
            Synth.Filter(c, Synth.FilterType.HighPass, hp, 0.7f); Synth.Filter(c, Synth.FilterType.LowPass, Math.Max(hp * 2.5f, 4500f), 0.7f); Synth.Mix(b, c);
        }

        /// <summary>Tropfen/Blubbern: kurze Sinus-Gleiter nach oben.</summary>
        public static void Bubbles(float[] b, int count, float fLow, float fHigh, float amp, Rng r)
        {
            for (int i = 0; i < count; i++)
            {
                int s = r.Range(0, Math.Max(1, b.Length - 4000)); float f = r.Range(fLow, fHigh); float len = r.Range(0.03f, 0.08f);
                Synth.AddTone(b, s, len, Synth.Wave.Sine, t => f * (1f + t * 18f), amp * r.Range(0.5f, 1f), Adsr.Perc(0.002f, len * 0.5f));
            }
        }

        /// <summary>Kurzer UI-Klick mit Ton im Bereich 1–4 kHz.</summary>
        public static float[] Tick(float len, float f, float amp, Rng r, float noise = 0.3f)
        {
            var b = Synth.Buffer(len);
            Synth.AddTone(b, 0, len, Synth.Wave.Sine, t => f, amp, Adsr.Perc(0.0008f, len * 0.3f));
            if (noise > 0f) { var n = Synth.Buffer(len); Synth.AddNoise(n, 0, len, amp * noise, Adsr.Perc(0.0005f, len * 0.12f), r); Synth.Filter(n, Synth.FilterType.BandPass, f * 1.2f, 1.5f); Synth.Mix(b, n); }
            return b;
        }

        public static float[] Concat(params float[][] parts)
        {
            int n = 0; foreach (var p in parts) n += p.Length; var o = new float[n]; int k = 0;
            foreach (var p in parts) { Array.Copy(p, 0, o, k, p.Length); k += p.Length; }
            return o;
        }

        public static float[] Scale(float[] b, float g) { for (int i = 0; i < b.Length; i++) b[i] *= g; return b; }
    }
}
