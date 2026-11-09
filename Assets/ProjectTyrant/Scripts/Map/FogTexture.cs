using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Überträgt die Nebel-Maske in eine kleine Textur (weich gefiltert), nur bei Änderung.</summary>
    public class FogTexture
    {
        public readonly Texture2D Texture;
        readonly Color32[] _px;
        readonly Color32 _fog;
        int _version = -1;
        FogModel _model;

        public FogTexture(int cells, Color fog)
        {
            Texture = new Texture2D(cells, cells, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, name = "PT_Fog" };
            _px = new Color32[cells * cells];
            _fog = fog;
        }

        /// <summary>Gibt true zurück, wenn neu hochgeladen wurde.</summary>
        public bool Update(FogModel model)
        {
            if (model == null) return false;
            if (model == _model && model.Version == _version) return false;
            _model = model; _version = model.Version;
            int n = model.Cells;
            // 3×3-Mittel für weiche Nebelkanten (Rand außerhalb der Zone zählt als Nebel)
            for (int z = 0; z < n; z++)
                for (int x = 0; x < n; x++)
                {
                    int open = 0;
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (model.IsRevealedCell(x + dx, z + dz)) open++;
                    var c = _fog; c.a = (byte)(_fog.a * (9 - open) / 9);
                    _px[z * n + x] = c;
                }
            Texture.SetPixels32(_px);
            Texture.Apply(false);
            return true;
        }
    }
}
