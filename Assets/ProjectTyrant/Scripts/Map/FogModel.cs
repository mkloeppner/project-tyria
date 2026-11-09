using System;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Nebel des Krieges: Bitmaske (Zellen × Zellen) über die Zone, Aufdeckung im Radius.</summary>
    public class FogModel
    {
        public readonly int Cells;
        public readonly float WorldSize;
        readonly bool[] _revealed;
        public int RevealedCount { get; private set; }
        public event Action Changed;
        public int Version { get; private set; }

        public FogModel(int cells = 128, float worldSize = 200f)
        {
            Cells = cells; WorldSize = worldSize; _revealed = new bool[cells * cells];
        }

        public float CellSize => WorldSize / Cells;

        public bool CellOf(Vector3 world, out int cx, out int cz)
        {
            float half = WorldSize * 0.5f;
            cx = Mathf.FloorToInt((world.x + half) / CellSize);
            cz = Mathf.FloorToInt((world.z + half) / CellSize);
            return cx >= 0 && cz >= 0 && cx < Cells && cz < Cells;
        }

        public bool IsRevealed(Vector3 world) => CellOf(world, out int x, out int z) && _revealed[z * Cells + x];
        public bool IsRevealedCell(int x, int z) => x >= 0 && z >= 0 && x < Cells && z < Cells && _revealed[z * Cells + x];
        public float RevealedFraction => RevealedCount / (float)_revealed.Length;

        /// <summary>Deckt alle Zellen im Radius auf. Gibt die Anzahl neu aufgedeckter Zellen zurück.</summary>
        public int Reveal(Vector3 world, float radius)
        {
            float half = WorldSize * 0.5f, cs = CellSize;
            int r = Mathf.CeilToInt(radius / cs);
            CellOf(world, out int cx, out int cz);
            int added = 0;
            for (int z = cz - r; z <= cz + r; z++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || z < 0 || x >= Cells || z >= Cells) continue;
                float wx = -half + (x + 0.5f) * cs, wz = -half + (z + 0.5f) * cs;
                float dx = wx - world.x, dz = wz - world.z;
                if (dx * dx + dz * dz > radius * radius) continue;
                int i = z * Cells + x;
                if (_revealed[i]) continue;
                _revealed[i] = true; added++;
            }
            if (added > 0) { RevealedCount += added; Version++; Changed?.Invoke(); }
            return added;
        }

        public void Clear() { Array.Clear(_revealed, 0, _revealed.Length); RevealedCount = 0; Version++; Changed?.Invoke(); }

        public string Serialize()
        {
            var bytes = new byte[(_revealed.Length + 7) / 8];
            for (int i = 0; i < _revealed.Length; i++) if (_revealed[i]) bytes[i >> 3] |= (byte)(1 << (i & 7));
            return Convert.ToBase64String(bytes);
        }

        public void Deserialize(string data)
        {
            Array.Clear(_revealed, 0, _revealed.Length); RevealedCount = 0;
            if (!string.IsNullOrEmpty(data))
            {
                byte[] bytes;
                try { bytes = Convert.FromBase64String(data); } catch { bytes = new byte[0]; }
                for (int i = 0; i < _revealed.Length && (i >> 3) < bytes.Length; i++)
                    if ((bytes[i >> 3] & (1 << (i & 7))) != 0) { _revealed[i] = true; RevealedCount++; }
            }
            Version++; Changed?.Invoke();
        }
    }
}
