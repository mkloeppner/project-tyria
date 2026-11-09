using System;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M24 – AudioSynth 2.0 (reines C#): Oszillatoren, Rauschen, ADSR, Filter mit Resonanz, FM, Karplus-Strong,
    /// Rausch-Wavetable (Chor-Pad), Effekte (Schroeder-Hall, Delay, Verzerrung, Chorus) und Lautheits-Normalisierung.
    /// Alles deterministisch über <see cref="Rng"/> mit Seed. Puffer sind Mono-float[] bei <see cref="Rate"/>.
    /// </summary>
    public static class Synth
    {
        public const int Rate = 44100;
        public const float TwoPi = (float)(Math.PI * 2.0);

        public static float[] Buffer(float seconds) => new float[Math.Max(1, (int)Math.Round(seconds * Rate))];
        public static float Seconds(float[] b) => b.Length / (float)Rate;

        // ------------------------------------------------------------------ Oszillatoren (Phase 0..1)
        public enum Wave { Sine, Saw, Square, Triangle }
        public static float Osc(Wave w, float phase)
        {
            phase -= (float)Math.Floor(phase);
            switch (w)
            {
                case Wave.Saw: return 2f * phase - 1f;
                case Wave.Square: return phase < 0.5f ? 1f : -1f;
                case Wave.Triangle: return 1f - 4f * Math.Abs(phase - 0.5f);
                default: return (float)Math.Sin(phase * TwoPi);
            }
        }

        /// <summary>Ton mit Frequenzhüllkurve (Tonhöhen-Hüllkurve) in einen Puffer addieren.</summary>
        public static void AddTone(float[] buf, int start, float seconds, Wave w, Func<float, float> freq, float amp, Adsr env, float detuneCents = 0f)
        {
            int n = (int)(seconds * Rate); double ph = 0; float det = (float)Math.Pow(2, detuneCents / 1200.0);
            for (int i = 0; i < n && start + i < buf.Length; i++)
            {
                if (start + i < 0) continue;
                float t = i / (float)Rate;
                ph += freq(t) * det / Rate;
                buf[start + i] += Osc(w, (float)ph) * amp * env.At(t, seconds);
            }
        }

        /// <summary>FM: Träger mit Modulator (Verhältnis, Index mit Abklingen) – Glocken, Metall, E-Piano.</summary>
        public static void AddFm(float[] buf, int start, float seconds, float carrier, float ratio, float index, float indexDecay, float amp, Adsr env)
        {
            int n = (int)(seconds * Rate); double pc = 0, pm = 0;
            for (int i = 0; i < n && start + i < buf.Length; i++)
            {
                if (start + i < 0) continue;
                float t = i / (float)Rate;
                pm += carrier * ratio / Rate;
                float mod = (float)Math.Sin(pm * TwoPi) * index * (float)Math.Exp(-t * indexDecay);
                pc += carrier / Rate;
                buf[start + i] += (float)Math.Sin(pc * TwoPi + mod) * amp * env.At(t, seconds);
            }
        }

        /// <summary>Karplus-Strong: gezupfte Saite (Harfe, Laute, Pizzicato).</summary>
        public static void AddPluck(float[] buf, int start, float seconds, float freq, float amp, float damping, Rng rng)
        {
            int period = Math.Max(2, (int)(Rate / Math.Max(20f, freq)));
            var line = new float[period];
            for (int i = 0; i < period; i++) line[i] = rng.Bipolar();
            int n = (int)(seconds * Rate), idx = 0; float last = 0f;
            for (int i = 0; i < n && start + i < buf.Length; i++)
            {
                float cur = line[idx];
                int nxt = (idx + 1) % period;
                float avg = (cur + line[nxt]) * 0.5f * damping;
                line[idx] = avg; idx = nxt;
                if (start + i >= 0) buf[start + i] += cur * amp * Math.Min(1f, i / 30f);
                last = cur;
            }
        }

        /// <summary>Weißes Rauschen.</summary>
        public static void AddNoise(float[] buf, int start, float seconds, float amp, Adsr env, Rng rng)
        {
            int n = (int)(seconds * Rate);
            for (int i = 0; i < n && start + i < buf.Length; i++) if (start + i >= 0) buf[start + i] += rng.Bipolar() * amp * env.At(i / (float)Rate, seconds);
        }

        /// <summary>Rosa Rauschen (Paul-Kellet-Filter).</summary>
        public static void AddPink(float[] buf, int start, float seconds, float amp, Adsr env, Rng rng)
        {
            int n = (int)(seconds * Rate); float b0 = 0, b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, b6 = 0;
            for (int i = 0; i < n && start + i < buf.Length; i++)
            {
                float w = rng.Bipolar();
                b0 = 0.99886f * b0 + w * 0.0555179f; b1 = 0.99332f * b1 + w * 0.0750759f; b2 = 0.96900f * b2 + w * 0.1538520f;
                b3 = 0.86650f * b3 + w * 0.3104856f; b4 = 0.55000f * b4 + w * 0.5329522f; b5 = -0.7616f * b5 - w * 0.0168980f;
                float p = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + w * 0.5362f) * 0.11f; b6 = w * 0.115926f;
                if (start + i >= 0) buf[start + i] += p * amp * env.At(i / (float)Rate, seconds);
            }
        }

        /// <summary>Rausch-Wavetable: eine Periode gefiltertes Rauschen als Welle – mehrere verstimmte Stimmen ergeben ein Chor-Pad.</summary>
        public static float[] NoiseWavetable(Rng rng, int size = 2048, int harmonics = 24)
        {
            var t = new float[size];
            for (int h = 1; h <= harmonics; h++)
            {
                float a = rng.Range(0.3f, 1f) / (h * 0.9f); float ph = rng.Value();
                for (int i = 0; i < size; i++) t[i] += a * (float)Math.Sin((i / (float)size * h + ph) * TwoPi);
            }
            float max = 1e-6f; foreach (var v in t) max = Math.Max(max, Math.Abs(v));
            for (int i = 0; i < size; i++) t[i] /= max;
            return t;
        }

        public static void AddWavetable(float[] buf, int start, float seconds, float[] table, float freq, float amp, Adsr env, float detuneCents = 0f)
        {
            int n = (int)(seconds * Rate); double ph = 0; float f = freq * (float)Math.Pow(2, detuneCents / 1200.0);
            for (int i = 0; i < n && start + i < buf.Length; i++)
            {
                ph += f / Rate; double p = ph - Math.Floor(ph);
                float x = (float)(p * table.Length); int a = (int)x % table.Length, b = (a + 1) % table.Length; float fr = x - (int)x;
                if (start + i >= 0) buf[start + i] += (table[a] + (table[b] - table[a]) * fr) * amp * env.At(i / (float)Rate, seconds);
            }
        }

        // ------------------------------------------------------------------ Filter
        public enum FilterType { LowPass, HighPass, BandPass }

        /// <summary>Biquad (RBJ) mit Resonanz Q; Grenzfrequenz optional als Funktion der Zeit (Filter-Sweep).</summary>
        public static void Filter(float[] buf, FilterType type, Func<float, float> cutoff, float q = 0.707f)
        {
            float x1 = 0, x2 = 0, y1 = 0, y2 = 0; float b0 = 0, b1 = 0, b2 = 0, a1 = 0, a2 = 0; float lastFc = -1f;
            for (int i = 0; i < buf.Length; i++)
            {
                float fc = Clamp(cutoff(i / (float)Rate), 20f, Rate * 0.45f);
                if ((i & 31) == 0 || lastFc < 0f)
                {
                    if (Math.Abs(fc - lastFc) > 0.5f) Coefs(type, fc, q, out b0, out b1, out b2, out a1, out a2);
                    lastFc = fc;
                }
                float x = buf[i];
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                buf[i] = y;
            }
        }
        public static void Filter(float[] buf, FilterType type, float cutoff, float q = 0.707f) => Filter(buf, type, t => cutoff, q);

        static void Coefs(FilterType type, float fc, float q, out float b0, out float b1, out float b2, out float a1, out float a2)
        {
            double w0 = 2 * Math.PI * fc / Rate, cos = Math.Cos(w0), alpha = Math.Sin(w0) / (2 * Math.Max(0.1, q));
            double B0, B1, B2, A0 = 1 + alpha, A1 = -2 * cos, A2 = 1 - alpha;
            switch (type)
            {
                case FilterType.HighPass: B0 = (1 + cos) / 2; B1 = -(1 + cos); B2 = (1 + cos) / 2; break;
                case FilterType.BandPass: B0 = alpha; B1 = 0; B2 = -alpha; break;
                default: B0 = (1 - cos) / 2; B1 = 1 - cos; B2 = (1 - cos) / 2; break;
            }
            b0 = (float)(B0 / A0); b1 = (float)(B1 / A0); b2 = (float)(B2 / A0); a1 = (float)(A1 / A0); a2 = (float)(A2 / A0);
        }

        // ------------------------------------------------------------------ Effekte
        /// <summary>Schroeder-Hall: 4 parallele Kammfilter + 2 Allpässe; mix 0..1.</summary>
        public static void Reverb(float[] buf, float roomSize = 0.8f, float damp = 0.3f, float mix = 0.25f)
        {
            int[] combs = { 1557, 1617, 1491, 1422 }; int[] aps = { 225, 556 };
            var cb = new float[4][]; var ci = new int[4]; var cf = new float[4];
            for (int c = 0; c < 4; c++) cb[c] = new float[combs[c]];
            var ab = new float[2][]; var ai = new int[2];
            for (int a = 0; a < 2; a++) ab[a] = new float[aps[a]];
            float fb = 0.7f + 0.28f * Clamp01(roomSize);
            for (int i = 0; i < buf.Length; i++)
            {
                float x = buf[i] * 0.25f, s = 0f;
                for (int c = 0; c < 4; c++)
                {
                    float y = cb[c][ci[c]];
                    cf[c] = y * (1f - damp) + cf[c] * damp;
                    cb[c][ci[c]] = x + cf[c] * fb;
                    ci[c] = (ci[c] + 1) % cb[c].Length; s += y;
                }
                for (int a = 0; a < 2; a++)
                {
                    float bv = ab[a][ai[a]]; float y = -s + bv; ab[a][ai[a]] = s + bv * 0.5f; ai[a] = (ai[a] + 1) % ab[a].Length; s = y;
                }
                buf[i] = buf[i] * (1f - mix) + s * mix;
            }
        }

        public static void Delay(float[] buf, float seconds, float feedback, float mix)
        {
            int d = Math.Max(1, (int)(seconds * Rate)); var line = new float[d]; int idx = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                float y = line[idx]; line[idx] = buf[i] + y * feedback; idx = (idx + 1) % d;
                buf[i] += y * mix;
            }
        }

        public static void Distort(float[] buf, float drive)
        {
            float norm = (float)Math.Tanh(drive);
            for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(buf[i] * drive) / norm;
        }

        public static void Chorus(float[] buf, float depthMs = 6f, float rateHz = 0.8f, float mix = 0.4f)
        {
            int max = (int)((depthMs * 2f + 10f) * Rate / 1000f) + 2; var line = new float[max]; int w = 0;
            for (int i = 0; i < buf.Length; i++)
            {
                line[w] = buf[i];
                float delay = (10f + depthMs * (1f + (float)Math.Sin(i / (float)Rate * rateHz * TwoPi))) * Rate / 1000f;
                float rp = w - delay; while (rp < 0) rp += max;
                int a = (int)rp % max, b = (a + 1) % max; float fr = rp - (int)rp;
                float y = line[a] + (line[b] - line[a]) * fr;
                buf[i] = buf[i] * (1f - mix * 0.5f) + y * mix;
                w = (w + 1) % max;
            }
        }

        /// <summary>Puffer B mit Pegel in A mischen (ab Index).</summary>
        public static void Mix(float[] dst, float[] src, int start = 0, float gain = 1f)
        {
            for (int i = 0; i < src.Length && start + i < dst.Length; i++) if (start + i >= 0) dst[start + i] += src[i] * gain;
        }

        /// <summary>Gleichanteil entfernen (Hochpass 1. Ordnung bei ~10 Hz).</summary>
        public static void RemoveDc(float[] buf)
        {
            double mean = 0; foreach (var v in buf) mean += v; mean /= Math.Max(1, buf.Length);
            for (int i = 0; i < buf.Length; i++) buf[i] -= (float)mean;
        }

        /// <summary>Ein-/Ausblende in ms gegen Knackser.</summary>
        public static void Fade(float[] buf, float inMs, float outMs)
        {
            int a = Math.Min(buf.Length, (int)(inMs * Rate / 1000f)), b = Math.Min(buf.Length, (int)(outMs * Rate / 1000f));
            for (int i = 0; i < a; i++) buf[i] *= i / (float)a;
            for (int i = 0; i < b; i++) buf[buf.Length - 1 - i] *= i / (float)b;
        }

        /// <summary>Nahtlose Schleife: das Ende wird über <paramref name="seconds"/> in den Anfang überblendet und abgeschnitten.</summary>
        public static float[] MakeLoop(float[] buf, float seconds)
        {
            int x = Math.Min(buf.Length / 3, (int)(seconds * Rate)); int n = buf.Length - x;
            var o = new float[n];
            Array.Copy(buf, o, n);
            for (int i = 0; i < x; i++)
            {
                float k = i / (float)x;                       // Anfang blendet aus dem überhängenden Ende ein
                o[i] = buf[n + i] * (1f - k) + buf[i] * k;
            }
            return o;
        }

        /// <summary>Lautheits-Normalisierung: Ziel-RMS (dBFS), danach Spitze auf höchstens <paramref name="peakDb"/> begrenzen (weiche Kennlinie, kein Clipping).</summary>
        public static void Normalize(float[] buf, float targetRmsDb, float peakDb = -1f)
        {
            RemoveDc(buf);
            double sum = 0; foreach (var v in buf) sum += v * v;
            float rms = (float)Math.Sqrt(sum / Math.Max(1, buf.Length));
            if (rms < 1e-7f) return;
            float g = DbToLin(targetRmsDb) / rms;
            float lim = DbToLin(peakDb);
            float knee = lim * 0.8f;
            for (int i = 0; i < buf.Length; i++)
            {
                float v = buf[i] * g, a = Math.Abs(v);
                if (a > knee) { float over = (a - knee) / (lim - knee); a = knee + (lim - knee) * (float)Math.Tanh(over); v = Math.Sign(v) * a; }
                buf[i] = v;
            }
            // Gleichanteil nach der weichen Kennlinie erneut entfernen, Spitze sicher unter dem Limit halten
            RemoveDc(buf);
            float peak = 0f; foreach (var v in buf) peak = Math.Max(peak, Math.Abs(v));
            if (peak > lim) { float k = lim / peak * 0.999f; for (int i = 0; i < buf.Length; i++) buf[i] *= k; }
        }

        /// <summary>Schleifen-Naht versiegeln: um den Übergang (Ende → Anfang) wird das Signal über ±W Samples weich tiefpass-gefiltert
        /// (gleitendes Mittel, Gewicht zur Naht hin 1), damit die Enden phasengleich anschließen. Wirkt nur ~1 ms – unhörbar.</summary>
        public static void SealLoop(float[] buf, int halfWindow = 96, int avg = 24)
        {
            int n = buf.Length; if (n < halfWindow * 4) return;
            var src = (float[])buf.Clone();
            for (int i = -halfWindow; i < halfWindow; i++)
            {
                int idx = ((i % n) + n) % n;
                double sum = 0; for (int k = -avg; k <= avg; k++) sum += src[((idx + k) % n + n) % n];
                float sm = (float)(sum / (2 * avg + 1));
                float w = 1f - Math.Abs(i + 0.5f) / halfWindow; w = w * w * (3f - 2f * w);
                buf[idx] = src[idx] * (1f - w) + sm * w;
            }
        }

        public static float DbToLin(float db) => (float)Math.Pow(10.0, db / 20.0);
        public static float LinToDb(float lin) => 20f * (float)Math.Log10(Math.Max(1e-9, lin));
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float MidiToHz(float midi) => 440f * (float)Math.Pow(2.0, (midi - 69f) / 12.0);
    }

    /// <summary>ADSR-Hüllkurve (Sekunden, Sustain-Pegel 0..1). Release beginnt bei <c>length - R</c>.</summary>
    public struct Adsr
    {
        public float A, D, S, R;
        public Adsr(float a, float d, float s, float r) { A = a; D = d; S = s; R = r; }
        public static Adsr Perc(float attack, float decay) => new Adsr(attack, decay, 0f, 0.005f);
        public static Adsr Pad(float a, float r) => new Adsr(a, 0.2f, 0.85f, r);
        public float At(float t, float length)
        {
            float v;
            if (t < A) v = A <= 0f ? 1f : t / A;
            else if (t < A + D) v = 1f - (1f - S) * ((t - A) / Math.Max(1e-5f, D));
            else v = S;
            if (S <= 0f && t >= A) v = (float)Math.Exp(-(t - A) / Math.Max(1e-4f, D) * 2.3f);   // Perkussiv: exponentielles Abklingen
            float rs = length - R;
            if (t > rs && R > 0f) v *= Math.Max(0f, 1f - (t - rs) / R);
            return v;
        }
    }

    /// <summary>Deterministischer Zufall (xorshift) – gleiche Seeds, gleiche Klänge auf jedem Rechner.</summary>
    public class Rng
    {
        uint _s;
        public Rng(int seed) { _s = (uint)seed * 2654435761u + 0x9E3779B9u; if (_s == 0) _s = 1; Next(); }
        public uint Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return _s; }
        public float Value() => (Next() & 0xFFFFFF) / 16777216f;
        public float Bipolar() => Value() * 2f - 1f;
        public float Range(float a, float b) => a + (b - a) * Value();
        public int Range(int a, int b) => a + (int)(Next() % (uint)Math.Max(1, b - a));
        public static int Hash(string s) { unchecked { int h = 23; foreach (char c in s) h = h * 31 + c; return h; } }
    }
}
