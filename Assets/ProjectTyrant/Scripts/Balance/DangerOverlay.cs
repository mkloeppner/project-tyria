using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Balance B1: Stufen-Overlay (/danger show): Minimap und Weltkarte tönen die Gefahrenstufen ein; die Textur wird je Zone einmal erzeugt.
    /// </summary>
    public static class DangerOverlay
    {
        public static bool Visible { get; private set; }
        static Texture2D _tex; static string _for;
        public static void Toggle() { Visible = !Visible; }

        /// <summary>Halbtransparente Stufen-Textur der aktuellen Zone (128²).</summary>
        public static Texture2D Texture
        {
            get
            {
                var z = ZoneRoot.Current; var m = BalanceWorld.Map;
                if (z == null || m == null) return null;
                if (_tex != null && _for == z.Id) return _tex;
                const int res = 128;
                _tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                var px = new Color[res * res];
                for (int y = 0; y < res; y++) for (int x = 0; x < res; x++)
                {
                    var wp = new Vector2((x / (float)(res - 1) - 0.5f) * m.Size, (y / (float)(res - 1) - 0.5f) * m.Size);
                    var c = m.IsCity(wp) ? new Color(1f, 1f, 1f, 0.15f) : Heatmap.DangerColors[Mathf.Clamp(m.At(wp), 0, 5)];
                    c.a = m.InSafezone(wp) ? 0.15f : 0.38f;
                    px[y * res + x] = c;
                }
                _tex.SetPixels(px); _tex.Apply(); _for = z.Id;
                return _tex;
            }
        }
    }
}
