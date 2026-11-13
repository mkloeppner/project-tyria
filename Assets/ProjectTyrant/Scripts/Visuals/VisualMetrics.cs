using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Visuals
{
    /// <summary>Ergebnis einer Bild-Messung.</summary>
    public struct MetricsResult
    {
        public float RmsContrast, MeanSaturation, ClippedShare, BlackShare, WhiteShare, EdgeDensity, SkyShare, MeanLuminance;
        public int Clusters;
        public int[] Histogram;
        public static string CsvHeader => "rms_kontrast;saettigung;geclippt;schwarz;weiss;cluster;kantendichte;himmel;luminanz";
        public string ToCsv() => string.Join(";", new[] { RmsContrast.ToString("0.0000"), MeanSaturation.ToString("0.0000"), ClippedShare.ToString("0.0000"), BlackShare.ToString("0.0000"), WhiteShare.ToString("0.0000"), Clusters.ToString(), EdgeDensity.ToString("0.0000"), SkyShare.ToString("0.0000"), MeanLuminance.ToString("0.0000") }).Replace(',', '.');
    }

    /// <summary>
    /// Grafik-Lauf G1: Bild-Metriken (rein, EditMode-testbar): RMS-Kontrast der Luminanz, mittlere HSV-Sättigung, Luminanz-Histogramm mit
    /// geclippten Pixeln (0/255), Farbcluster (k-Means k = 16, Anteil ≥ 1 %), Kantendichte (Sobel), Himmelsanteil (Maske).
    /// </summary>
    public static class VisualMetrics
    {
        public const int K = 16;
        public const float ClusterMinShare = 0.01f, EdgeThreshold = 0.1f;

        public static float Luma(Color32 c) => (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;

        public static MetricsResult Measure(Color32[] px, int w, int h, bool[] skyMask = null, int maxSamples = 40000)
        {
            var r = new MetricsResult { Histogram = new int[256] };
            int n = px.Length; if (n == 0 || w * h != n) return r;
            double sum = 0, sum2 = 0, sat = 0; int black = 0, white = 0;
            for (int i = 0; i < n; i++)
            {
                var c = px[i];
                float l = Luma(c); sum += l; sum2 += l * l;
                int li = Mathf.Clamp(Mathf.RoundToInt(l * 255f), 0, 255); r.Histogram[li]++;
                if (c.r <= 1 && c.g <= 1 && c.b <= 1) black++;
                if (c.r >= 254 && c.g >= 254 && c.b >= 254) white++;
                byte mx = Math.Max(c.r, Math.Max(c.g, c.b)), mn = Math.Min(c.r, Math.Min(c.g, c.b));
                sat += mx == 0 ? 0 : (mx - mn) / (double)mx;
            }
            double mean = sum / n;
            r.MeanLuminance = (float)mean;
            r.RmsContrast = (float)Math.Sqrt(Math.Max(0, sum2 / n - mean * mean));
            r.MeanSaturation = (float)(sat / n);
            r.BlackShare = black / (float)n; r.WhiteShare = white / (float)n; r.ClippedShare = (black + white) / (float)n;
            r.EdgeDensity = EdgeDensity(px, w, h);
            r.Clusters = Clusters(px, maxSamples);
            if (skyMask != null && skyMask.Length > 0) { int s = 0; foreach (var b in skyMask) if (b) s++; r.SkyShare = s / (float)skyMask.Length; }
            return r;
        }

        /// <summary>Anteil der Pixel mit Sobel-Gradient der Luminanz über der Schwelle.</summary>
        public static float EdgeDensity(Color32[] px, int w, int h)
        {
            if (w < 3 || h < 3) return 0f;
            int edges = 0, total = 0;
            int step = Math.Max(1, (w * h) / 250000);
            for (int y = 1; y < h - 1; y += step)
                for (int x = 1; x < w - 1; x += step)
                {
                    float L(int dx, int dy) => Luma(px[(y + dy) * w + x + dx]);
                    float gx = -L(-1, -1) - 2 * L(-1, 0) - L(-1, 1) + L(1, -1) + 2 * L(1, 0) + L(1, 1);
                    float gy = -L(-1, -1) - 2 * L(0, -1) - L(1, -1) + L(-1, 1) + 2 * L(0, 1) + L(1, 1);
                    if (Mathf.Sqrt(gx * gx + gy * gy) > EdgeThreshold * 4f) edges++;
                    total++;
                }
            return total == 0 ? 0f : edges / (float)total;
        }

        /// <summary>k-Means (k = 16, deterministische Startwerte entlang der Luminanz, 8 Iterationen) – Anzahl Cluster mit Anteil ≥ 1 %.</summary>
        public static int Clusters(Color32[] px, int maxSamples = 40000)
        {
            int n = px.Length; if (n == 0) return 0;
            int step = Math.Max(1, n / maxSamples);
            var s = new List<Vector3>();
            for (int i = 0; i < n; i += step) s.Add(new Vector3(px[i].r, px[i].g, px[i].b));
            var sorted = new List<Vector3>(s); sorted.Sort((a, b) => (a.x + a.y + a.z).CompareTo(b.x + b.y + b.z));
            var cent = new Vector3[K];
            for (int k = 0; k < K; k++) cent[k] = sorted[Math.Min(sorted.Count - 1, (int)((k + 0.5f) / K * sorted.Count))];
            var assign = new int[s.Count];
            for (int it = 0; it < 8; it++)
            {
                var acc = new Vector3[K]; var cnt = new int[K];
                for (int i = 0; i < s.Count; i++)
                {
                    int best = 0; float bd = float.MaxValue;
                    for (int k = 0; k < K; k++) { float d = (s[i] - cent[k]).sqrMagnitude; if (d < bd) { bd = d; best = k; } }
                    assign[i] = best; acc[best] += s[i]; cnt[best]++;
                }
                for (int k = 0; k < K; k++) if (cnt[k] > 0) cent[k] = acc[k] / cnt[k];
            }
            var final = new int[K]; foreach (var a in assign) final[a]++;
            // nahe beieinanderliegende Zentren (< 12 RGB) zählen als ein Cluster
            var counted = new List<int>();
            for (int k = 0; k < K; k++)
            {
                if (final[k] < ClusterMinShare * s.Count) continue;
                bool dup = false; foreach (var o in counted) if ((cent[o] - cent[k]).magnitude < 12f) { dup = true; final[o] += final[k]; break; }
                if (!dup) counted.Add(k);
            }
            return counted.Count;
        }
    }
}
