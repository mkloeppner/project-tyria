using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Belebung: Ziel-Gegnerdichte je Gefahrenstufe (rein, getestet). Nutzt den bisher ungenutzten Dichte-Faktor der Gefahrenstufe,
    /// die maximale Gruppengröße und die Veteranen-Regel. Lager halten Abstand ≥ 2 × Aggro-Reichweite + 8 m, damit nie zwei Lager zugleich anspringen.
    /// </summary>
    public static class PopulationModel
    {
        /// <summary>Feindliche Spawns je km² bei Dichte-Faktor 1 (vorher ≈ 20/km² in allen Zonen).</summary>
        public const float BaseSpawnsPerKm2 = 50f;
        /// <summary>Abstand zu Wegmarken, Herzen, Sehenswürdigkeiten und Bauwerken (m).</summary>
        public const float ContentClearance = 22f;
        /// <summary>Höchstens so viele Spawns je Stufe hinzufügen (Leistung).</summary>
        public const int MaxAddPerZone = 90;

        public static int TargetSpawns(float areaM2, DangerSettings s)
        {
            if (s == null || s.Level <= 0) return 0;   // Stufe 0 (Startzone) bleibt wie abgestimmt
            return Mathf.RoundToInt(areaM2 / 1e6f * BaseSpawnsPerKm2 * Mathf.Max(0f, s.Density));
        }

        /// <summary>Mindestabstand zwischen zwei Lagern (m).</summary>
        public static float CampSpacing(DangerSettings s) => 2f * Units.M(s.AggroRange) + 8f;

        /// <summary>Lagergröße: Stufe 1 Paare (Partner), sonst Wechsel 3 / max / 2 / 1, gedeckelt durch die Gruppengröße der Stufe.</summary>
        public static int CampSize(DangerSettings s, int index)
        {
            int max = Mathf.Max(1, s.MaxGroupSize);
            if (s.Level <= 0) return 1;
            if (s.Pack == PackMode.Partner) return Mathf.Min(max, index % 3 == 2 ? 1 : 2);
            int[] pattern = { 3, Mathf.Min(max, 4), 2, 1 };
            return Mathf.Clamp(pattern[index % pattern.Length], 1, max);
        }

        /// <summary>Jedes fünfte Lager mit einem Veteranen (nur wenn die Stufe Veteranen erlaubt), nie mehr als einer je Lager.</summary>
        public static bool VeteranCamp(DangerSettings s, int index) => s.Level >= 1 && s.VeteranRule >= 1 && index % 5 == 4;

        /// <summary>Wie viele Spawns fehlen (nie negativ, gedeckelt).</summary>
        public static int Missing(int have, int target) => Mathf.Clamp(target - have, 0, MaxAddPerZone);
    }
}
