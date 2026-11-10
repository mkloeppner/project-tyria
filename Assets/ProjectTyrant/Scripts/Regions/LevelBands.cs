using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: Re-Leveling der zehn Bestandszonen auf die Level-80-Kurve (reine Daten, getestet). Inhalte (Herzen, Sehenswürdigkeiten,
    /// Herausforderungen, Einblicke) werden vom alten Band ins neue übertragen: breite alte Bänder linear, einstufige alte Bänder (30–30)
    /// nach der Entfernung vom Zoneneingang. Die Startzone und das Nebelmoor bleiben unverändert.
    /// </summary>
    public static class LevelBands
    {
        public struct Band { public string Zone; public int OldMin, OldMax, NewMin, NewMax; public Band(string z, int om, int oM, int nm, int nM) { Zone = z; OldMin = om; OldMax = oM; NewMin = nm; NewMax = nM; } }

        public static readonly Band[] Existing =
        {
            new Band("sonnfurt", 1, 15, 1, 15), new Band("nebelmoor", 15, 25, 15, 25), new Band("frostkamm", 25, 30, 25, 35),
            new Band("gluetoede", 30, 30, 35, 45), new Band("silberkueste", 30, 30, 40, 50), new Band("daemmerwald", 30, 30, 50, 60),
            new Band("duenenmeer", 30, 30, 60, 70), new Band("kristalltiefe", 30, 30, 65, 75), new Band("himmelsspitzen", 30, 30, 75, 80),
            new Band("tyrannenfeste", 30, 30, 80, 80),
        };

        /// <summary>Weltboss-Level nach dem Re-Leveling (Archetyp-Id → Level).</summary>
        public static readonly Dictionary<string, int> WorldBossLevels = new Dictionary<string, int>
        {
            { "Moorkoloss", 15 }, { "Frostschlund", 30 }, { "Glutkern", 45 }, { "AlterHain", 55 }, { "Grossmaul", 65 }, { "Kristallkoenigin", 70 }, { "Himmelswyrm", 80 }, { "Leviathan", 50 },
        };

        /// <summary>Story-Kapitel-Gates 1–10.</summary>
        public static readonly int[] ChapterGates = { 1, 5, 12, 20, 30, 40, 50, 60, 70, 80 };

        public static bool TryGet(string zone, out Band b)
        {
            foreach (var x in Existing) if (x.Zone == zone) { b = x; return true; }
            b = default; return false;
        }

        /// <summary>Neues Level eines Inhalts. <paramref name="distance01"/>: 0 = am Eingang, 1 = am weitesten entfernt (nur für einstufige alte Bänder).</summary>
        public static int Map(string zone, int oldLevel, float distance01)
        {
            if (!TryGet(zone, out var b)) return oldLevel;
            if (b.OldMax > b.OldMin)
            {
                float t = Mathf.Clamp01((oldLevel - b.OldMin) / (float)(b.OldMax - b.OldMin));
                return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(b.NewMin, b.NewMax, t)), b.NewMin, b.NewMax);
            }
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(b.NewMin, b.NewMax, Mathf.Clamp01(distance01))), b.NewMin, b.NewMax);
        }

        /// <summary>Höchster Level-Sprung zwischen zwei Gebieten derselben Zone (Regel: ≤ 10).</summary>
        public static int MaxJump(IList<int> levelsAlongPath)
        {
            int m = 0; for (int i = 1; i < levelsAlongPath.Count; i++) m = Mathf.Max(m, Mathf.Abs(levelsAlongPath[i] - levelsAlongPath[i - 1])); return m;
        }
    }
}
