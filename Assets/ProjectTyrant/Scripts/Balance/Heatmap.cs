using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Heatmaps (Tode, Fluchtversuche) auf der Karten-Textur einer Zone; optional mit Stufen-Overlay.</summary>
    public static class Heatmap
    {
        public static readonly Color[] DangerColors =
        {
            new Color(0.3f, 0.85f, 0.35f), new Color(0.65f, 0.9f, 0.3f), new Color(1f, 0.85f, 0.25f),
            new Color(1f, 0.55f, 0.2f), new Color(0.95f, 0.25f, 0.2f), new Color(0.6f, 0.15f, 0.65f),
        };

        /// <summary>Welt-Position → Pixel (Zone zentriert um 0/0, Kantenlänge size).</summary>
        public static Vector2Int ToPixel(Vector2 world, float size, int w, int h)
            => new Vector2Int(Mathf.RoundToInt((world.x / size + 0.5f) * (w - 1)), Mathf.RoundToInt((world.y / size + 0.5f) * (h - 1)));

        public static Texture2D Render(Texture2D map, float zoneSize, IList<Vector2> points, Color hot, ZoneDangerMap danger = null, int res = 512)
        {
            map = Readable(map);
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var px = new Color[res * res];
            for (int y = 0; y < res; y++) for (int x = 0; x < res; x++)
            {
                Color c = map != null && map.isReadable ? map.GetPixelBilinear(x / (float)(res - 1), y / (float)(res - 1)) : new Color(0.2f, 0.22f, 0.2f);
                c = Color.Lerp(c, Color.gray, 0.35f);
                if (danger != null)
                {
                    var wp = new Vector2((x / (float)(res - 1) - 0.5f) * zoneSize, (y / (float)(res - 1) - 0.5f) * zoneSize);
                    var dc = danger.IsCity(wp) ? Color.white : DangerColors[Mathf.Clamp(danger.At(wp), 0, 5)];
                    c = Color.Lerp(c, dc, 0.22f);
                }
                px[y * res + x] = c;
            }
            // Dichte: Gauß-Kleckse je Punkt
            float rad = Mathf.Max(4f, res / 40f);
            var heat = new float[res * res];
            foreach (var p in points)
            {
                var c = ToPixel(p, zoneSize, res, res);
                int r = Mathf.CeilToInt(rad * 2.5f);
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    int x = c.x + dx, y = c.y + dy;
                    if (x < 0 || y < 0 || x >= res || y >= res) continue;
                    heat[y * res + x] += Mathf.Exp(-(dx * dx + dy * dy) / (2f * rad * rad));
                }
            }
            float max = 0.0001f; foreach (var v in heat) max = Mathf.Max(max, v);
            for (int i = 0; i < px.Length; i++)
            {
                float t = Mathf.Clamp01(heat[i] / max);
                if (t > 0.02f) px[i] = Color.Lerp(px[i], hot, Mathf.Sqrt(t) * 0.9f);
            }
            // Punkte als kleine Kreuze
            foreach (var p in points)
            {
                var c = ToPixel(p, zoneSize, res, res);
                for (int k = -3; k <= 3; k++) { Set(px, res, c.x + k, c.y, Color.black); Set(px, res, c.x, c.y + k, Color.black); }
            }
            tex.SetPixels(px); tex.Apply();
            return tex;
        }

        /// <summary>Lesbare Kopie (Karten-Texturen sind meist nicht lesbar).</summary>
        public static Texture2D Readable(Texture2D src)
        {
            if (src == null || src.isReadable) return src;
            var rt = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            var prev = RenderTexture.active; RenderTexture.active = rt;
            var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0); t.Apply();
            RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
            return t;
        }

        static void Set(Color[] px, int res, int x, int y, Color c) { if (x >= 0 && y >= 0 && x < res && y < res) px[y * res + x] = c; }

        public static string Save(Texture2D t, string name)
        {
            string dir = "Temp/PT_QA/balance";
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, name + ".png");
            File.WriteAllBytes(path, t.EncodeToPNG());
            return path;
        }

        /// <summary>Punkte einer Art aus Sessions einer Zone sammeln.</summary>
        public static List<Vector2> Points(IEnumerable<TelemetrySession> sessions, string zone, TelemetryKind kind)
        {
            var l = new List<Vector2>();
            foreach (var s in sessions) if (s != null && s.Zone == zone) foreach (var r in s.Records) if (r.Kind == kind) l.Add(new Vector2(r.X, r.Z));
            return l;
        }
    }
}
