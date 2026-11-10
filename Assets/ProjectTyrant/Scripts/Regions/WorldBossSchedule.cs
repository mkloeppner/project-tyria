using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: Weltboss-Fahrplan (rein, getestet). Zwei-Stunden-Zyklus mit 12 Boss-Fenstern à 10 Minuten (lückenlos, ohne Überlappung)
    /// und einer zweiten Spur für die großen Metas (Zarathis, Drachenende) à 20 Minuten.
    /// </summary>
    public static class WorldBossSchedule
    {
        public const float Cycle = 7200f, Window = 600f, MetaWindow = 1200f;

        public struct Entry
        {
            public string Id, Name, Zone; public int Level; public float Offset, Duration; public bool Meta;
            public Entry(string id, string name, string zone, int level, float offset, float duration, bool meta) { Id = id; Name = name; Zone = zone; Level = level; Offset = offset; Duration = duration; Meta = meta; }
        }

        public static readonly Entry[] Bosses =
        {
            new Entry("moorkoloss", "Moorkoloss", "nebelmoor", 15, 0 * Window, Window, false),
            new Entry("frostschlund", "Frostschlund", "frostkamm", 30, 1 * Window, Window, false),
            new Entry("glutkern", "Glutkern", "gluetoede", 45, 2 * Window, Window, false),
            new Entry("leviathan", "Leviathan", "silberkueste", 50, 3 * Window, Window, false),
            new Entry("alter_hain", "Der Alte Hain", "daemmerwald", 55, 4 * Window, Window, false),
            new Entry("grossmaul", "Großmaul", "duenenmeer", 65, 5 * Window, Window, false),
            new Entry("kristallkoenigin", "Kristallkönigin", "kristalltiefe", 70, 6 * Window, Window, false),
            new Entry("gletscherwaechter", "Gletscherwächter", "weisse_stille", 75, 7 * Window, Window, false),
            new Entry("himmelswyrm", "Himmelswyrm", "himmelsspitzen", 80, 8 * Window, Window, false),
            new Entry("tempelwaechter", "Tempelwächter", "schaedelkueste", 80, 9 * Window, Window, false),
            new Entry("salzkoloss", "Salzkoloss", "spiegel_der_sonne", 80, 10 * Window, Window, false),
            new Entry("nebelschlange", "Nebelschlange", "nebelkueste", 80, 11 * Window, Window, false),
        };

        public static readonly Entry[] Metas =
        {
            new Entry("fall_von_zarathis", "Fall von Zarathis", "zarathis", 80, 1800f, MetaWindow, true),
            new Entry("erwachen_des_meeresdrachen", "Erwachen des Meeresdrachen", "herz_der_flut", 80, 5400f, MetaWindow, true),
        };

        public static IEnumerable<Entry> All() { foreach (var e in Bosses) yield return e; foreach (var e in Metas) yield return e; }

        /// <summary>Aktives Boss-Fenster zur Zeit <paramref name="t"/> (s seit Zyklusbeginn, beliebig groß).</summary>
        public static Entry ActiveBoss(double t)
        {
            float c = (float)(t % Cycle); if (c < 0) c += Cycle;
            return Bosses[(int)(c / Window) % Bosses.Length];
        }

        /// <summary>Sekunden bis zum nächsten Start von <paramref name="id"/>.</summary>
        public static float SecondsUntil(string id, double t)
        {
            foreach (var e in All())
                if (e.Id == id) { float c = (float)(t % Cycle); if (c < 0) c += Cycle; float d = e.Offset - c; return d < 0 ? d + Cycle : d; }
            return -1f;
        }

        public static bool TryGet(string id, out Entry e) { foreach (var x in All()) if (x.Id == id) { e = x; return true; } e = default; return false; }
    }
}
