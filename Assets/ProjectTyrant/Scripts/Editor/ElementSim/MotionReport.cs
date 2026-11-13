using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProjectTyrant.Visuals.ElementSim;
using UnityEngine;

namespace ProjectTyrant.EditorTools
{
    /// <summary>VFX-Lauf V1: Bewegungs-Metrik je Bildstreifen (Temp/PT_QA/V1/&lt;tag&gt;) → motion_&lt;tag&gt;.csv.</summary>
    public static class MotionReport
    {
        public const int W = 160, H = 90;

        public static float[] Luminance(string png)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(File.ReadAllBytes(png));
            var lum = new float[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    var c = tex.GetPixelBilinear((x + 0.5f) / W, (y + 0.5f) / H);
                    lum[x + y * W] = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                }
            Object.DestroyImmediate(tex);
            return lum;
        }

        public static string Build(string tag)
        {
            string root = SkillStripCapture.Root(tag); if (!Directory.Exists(root)) return "kein Ordner " + root;
            var sb = new StringBuilder("klasse;skill;frame_diff;kohaerenz;tote_bilder\n"); int n = 0; float sumD = 0f, sumC = 0f;
            foreach (var dir in Directory.GetDirectories(root).OrderBy(d => d))
            {
                var strips = Directory.GetFiles(dir, "*_strip_0.png").OrderBy(f => f);
                foreach (var first in strips)
                {
                    string skill = Path.GetFileName(first).Replace("_strip_0.png", "");
                    var frames = new List<float[]>();
                    for (int f = 0; f < SkillStripCapture.StripFrames; f++) { string p = Path.Combine(dir, skill + "_strip_" + f + ".png"); if (File.Exists(p)) frames.Add(Luminance(p)); }
                    var m = MotionMetrics.Measure(frames, W, H);
                    sb.Append(Path.GetFileName(dir)).Append(';').Append(skill).Append(';').Append(m.MeanFrameDiff.ToString("0.0000", CultureInfo.InvariantCulture)).Append(';')
                      .Append(m.Coherence.ToString("0.000", CultureInfo.InvariantCulture)).Append(';').Append(m.DeadShare.ToString("0.00", CultureInfo.InvariantCulture)).Append('\n');
                    n++; sumD += m.MeanFrameDiff; sumC += m.Coherence;
                }
            }
            File.WriteAllText("Temp/PT_QA/V1/motion_" + tag + ".csv", sb.ToString());
            return n + " Streifen, mittlere Bild-Differenz " + (n > 0 ? sumD / n : 0f).ToString("0.0000") + ", Kohärenz " + (n > 0 ? sumC / n : 0f).ToString("0.00");
        }
    }
}
