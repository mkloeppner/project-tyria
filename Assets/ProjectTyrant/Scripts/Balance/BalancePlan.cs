using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Mess-Zellen (Gefahrenstufe × Zone × Startpunkt × Spieler-Level) – Baseline und Nachher nutzen dieselben Zellen und Seeds.</summary>
    public static class BalancePlan
    {
        public struct Cell { public int Danger; public string Zone; public Vector3 Start; public int Level; public string Label; }

        public static readonly Cell[] Cells =
        {
            new Cell { Danger = 0, Zone = "sonnfurt", Start = new Vector3(-54, 1, -51), Level = 2, Label = "Sonnfurt (Dorfrand)" },
            new Cell { Danger = 1, Zone = "sonnfurt", Start = new Vector3(-25, 1, -30), Level = 4, Label = "Grenzmark Umland" },
            new Cell { Danger = 2, Zone = "sonnfurt", Start = new Vector3(-15, 1, -3), Level = 7, Label = "Dornwacht/Elmhain" },
            new Cell { Danger = 3, Zone = "nebelmoor", Start = new Vector3(0, 3, 0), Level = 20, Label = "Nebelmoor Kern" },
            new Cell { Danger = 4, Zone = "gluetoede", Start = new Vector3(-470, 2, 0), Level = 30, Label = "Glutöde" },
            new Cell { Danger = 5, Zone = "kristalltiefe", Start = new Vector3(0, 2, -380), Level = 30, Label = "Kristalltiefe" },
        };

        public static readonly int[] Seeds = { 11, 23, 37 };
        public const float SessionSeconds = 1800f;

        public static Cell? Find(int danger) { foreach (var c in Cells) if (c.Danger == danger) return c; return null; }

        /// <summary>Plan für ein Profil über die gewählten Stufen (alle Seeds).</summary>
        public static List<(BotProfileKind, Cell, int)> Build(BotProfileKind kind, IEnumerable<int> dangers)
        {
            var l = new List<(BotProfileKind, Cell, int)>();
            foreach (int d in dangers) { var c = Find(d); if (c == null) continue; foreach (int s in Seeds) l.Add((kind, c.Value, s)); }
            return l;
        }
    }
}
