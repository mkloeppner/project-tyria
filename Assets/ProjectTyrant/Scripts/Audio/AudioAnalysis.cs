using System;
using System.IO;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M24: Hörkontrolle per Analyse – Spitze, RMS, DC-Offset, Loop-Naht, Spektral-Schwerpunkt.</summary>
    [Serializable]
    public struct ClipStats
    {
        public float PeakDb, RmsDb, Dc, LoopSeam, CentroidHz, Seconds;
        public override string ToString() => string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0:0.00} s · Spitze {1:0.0} dBFS · RMS {2:0.0} dBFS · DC {3:0.0000} · Naht {4:0.0000} · Schwerpunkt {5:0} Hz", Seconds, PeakDb, RmsDb, Dc, LoopSeam, CentroidHz);
    }

    public static class AudioAnalysis
    {
        public static ClipStats Analyze(float[] s, int rate = Synth.Rate)
        {
            var st = new ClipStats { Seconds = s.Length / (float)rate };
            double sum = 0, sq = 0; float peak = 0f;
            foreach (var v in s) { sum += v; sq += v * v; peak = Math.Max(peak, Math.Abs(v)); }
            int n = Math.Max(1, s.Length);
            st.Dc = (float)(sum / n);
            st.RmsDb = Synth.LinToDb((float)Math.Sqrt(sq / n));
            st.PeakDb = Synth.LinToDb(peak);
            st.LoopSeam = LoopSeam(s);
            st.CentroidHz = SpectralCentroid(s, rate);
            return st;
        }

        /// <summary>Phasengleichheit der Enden: Sprung zwischen letztem und erstem Sample plus Steigungs-Abweichung (normiert auf Vollaussteuerung).</summary>
        public static float LoopSeam(float[] s)
        {
            if (s.Length < 4) return 1f;
            float jump = Math.Abs(s[0] - s[s.Length - 1]);
            float slopeEnd = s[s.Length - 1] - s[s.Length - 2], slopeStart = s[1] - s[0];
            return jump + Math.Abs(slopeEnd - slopeStart) * 0.5f;
        }

        /// <summary>Spektral-Schwerpunkt: Mittel über bis zu 16 Fenster à 2048 Samples (Hann-Fenster, Radix-2-FFT), energiegewichtet.</summary>
        public static float SpectralCentroid(float[] s, int rate = Synth.Rate)
        {
            const int N = 2048;
            int windows = s.Length < N ? 1 : Math.Min(16, s.Length / N);
            var re = new double[N]; var im = new double[N]; double num = 0, den = 0;
            for (int w = 0; w < windows; w++)
            {
                int start = s.Length < N ? 0 : (int)((long)(s.Length - N) * w / Math.Max(1, windows - 1));
                for (int i = 0; i < N; i++)
                {
                    int j = start + i; double v = j < s.Length ? s[j] : 0.0;
                    re[i] = v * (0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (N - 1))); im[i] = 0;
                }
                Fft(re, im);
                for (int k = 1; k < N / 2; k++)
                {
                    double mag = Math.Sqrt(re[k] * re[k] + im[k] * im[k]), f = k * (double)rate / N;
                    num += mag * f; den += mag;
                }
            }
            return den > 1e-9 ? (float)(num / den) : 0f;
        }

        /// <summary>Iterative Radix-2-FFT in place (Länge = Zweierpotenz).</summary>
        public static void Fft(double[] re, double[] im)
        {
            int n = re.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1; for (; (j & bit) != 0; bit >>= 1) j ^= bit; j ^= bit;
                if (i < j) { double t = re[i]; re[i] = re[j]; re[j] = t; t = im[i]; im[i] = im[j]; im[j] = t; }
            }
            for (int len = 2; len <= n; len <<= 1)
            {
                double ang = -2 * Math.PI / len, wr = Math.Cos(ang), wi = Math.Sin(ang);
                for (int i = 0; i < n; i += len)
                {
                    double cr = 1, ci = 0;
                    for (int k = 0; k < len / 2; k++)
                    {
                        int a = i + k, b = a + len / 2;
                        double tr = re[b] * cr - im[b] * ci, ti = re[b] * ci + im[b] * cr;
                        re[b] = re[a] - tr; im[b] = im[a] - ti; re[a] += tr; im[a] += ti;
                        double ncr = cr * wr - ci * wi; ci = cr * wi + ci * wr; cr = ncr;
                    }
                }
            }
        }
    }

    /// <summary>WAV lesen/schreiben (PCM 16 Bit, Mono, 44,1 kHz) – rein, damit Tests die erzeugten Dateien analysieren können.</summary>
    public static class WavIO
    {
        public static void Write(string path, float[] s, int rate = Synth.Rate)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int n = s.Length;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + n * 2); w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(n * 2);
                var bytes = new byte[n * 2];
                for (int i = 0; i < n; i++)
                {
                    int v = (int)Math.Round(Synth.Clamp(s[i], -1f, 1f) * 32767f);
                    bytes[i * 2] = (byte)(v & 0xFF); bytes[i * 2 + 1] = (byte)((v >> 8) & 0xFF);
                }
                w.Write(bytes);
            }
        }

        public static float[] Read(string path, out int rate)
        {
            var b = File.ReadAllBytes(path); rate = BitConverter.ToInt32(b, 24);
            int channels = BitConverter.ToInt16(b, 22), pos = 12, dataPos = -1, dataLen = 0;
            while (pos + 8 <= b.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(b, pos, 4); int len = BitConverter.ToInt32(b, pos + 4);
                if (id == "data") { dataPos = pos + 8; dataLen = Math.Min(len, b.Length - dataPos); break; }
                pos += 8 + len;
            }
            if (dataPos < 0) return new float[0];
            int n = dataLen / 2 / Math.Max(1, channels); var s = new float[n];
            for (int i = 0; i < n; i++) s[i] = BitConverter.ToInt16(b, dataPos + i * 2 * channels) / 32768f;
            return s;
        }
    }
}
