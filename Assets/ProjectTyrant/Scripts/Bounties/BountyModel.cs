using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Lauf 11: instabile Eigenschaften eines Kopfgeld-Ziels (Pool ≥ 10).</summary>
    public enum BountyModifier { Orbkranz, FesselFelder, Spiegelbilder, Zorn, HeilungBeiBewegung, TrotzFenster, Schnelligkeit, Blindheitsnebel, KettenBlitz, ReittierAbwurf }

    public enum BountyState { Available, Searching, Spawned, Completed, Failed, Locked }

    /// <summary>
    /// Lauf 11: Kopfgeldjagd (rein, getestet). Annahme an der Tafel → Suche (Spürsinn-Kreis auf der Karte) → Ziel erscheint mit 2–3 instabilen
    /// Eigenschaften (Champion 3, Veteran 2) → Sieg in 10 Minuten → Marken, Beute, Erfolge; tägliche Sperre je Ziel.
    /// Lösbarkeit: Druck-Summe der Eigenschaften ≤ Budget, keine verbotenen Paare, höchstens eine Kontroll-Eigenschaft (CC-Budget).
    /// </summary>
    public class BountyModel
    {
        public const float TimeLimit = 600f, SearchRadius = 60f;
        public const int VeteranTokens = 2, ChampionTokens = 5;
        public static readonly Dictionary<BountyModifier, int> Pressure = new Dictionary<BountyModifier, int>
        {
            { BountyModifier.Orbkranz, 3 }, { BountyModifier.FesselFelder, 3 }, { BountyModifier.Spiegelbilder, 2 }, { BountyModifier.Zorn, 2 }, { BountyModifier.HeilungBeiBewegung, 2 },
            { BountyModifier.TrotzFenster, 1 }, { BountyModifier.Schnelligkeit, 1 }, { BountyModifier.Blindheitsnebel, 2 }, { BountyModifier.KettenBlitz, 2 }, { BountyModifier.ReittierAbwurf, 1 },
        };
        public static readonly BountyModifier[] Control = { BountyModifier.FesselFelder, BountyModifier.ReittierAbwurf };
        public static readonly (BountyModifier, BountyModifier)[] Forbidden = { (BountyModifier.Orbkranz, BountyModifier.FesselFelder), (BountyModifier.HeilungBeiBewegung, BountyModifier.Schnelligkeit), (BountyModifier.Zorn, BountyModifier.Spiegelbilder) };
        public const int PressureBudget = 7;

        public string Id; public bool Champion;
        public BountyState State = BountyState.Available;
        public float TimeLeft;
        public readonly List<BountyModifier> Modifiers = new List<BountyModifier>();
        public string LockedDay;

        public BountyModel(string id, bool champion) { Id = id; Champion = champion; }

        public static bool Solvable(IList<BountyModifier> mods)
        {
            int p = 0, cc = 0;
            foreach (var m in mods) { p += Pressure[m]; if (Array.IndexOf(Control, m) >= 0) cc++; }
            if (p > PressureBudget || cc > 1) return false;
            foreach (var (a, b) in Forbidden) if (mods.Contains(a) && mods.Contains(b)) return false;
            return true;
        }

        /// <summary>Zieht 2 (Veteran) bzw. 3 (Champion) verschiedene Eigenschaften, bis die Kombination lösbar ist (deterministisch über den Seed).</summary>
        public static List<BountyModifier> Roll(int seed, bool champion)
        {
            var rng = new Random(seed); var all = (BountyModifier[])Enum.GetValues(typeof(BountyModifier));
            for (int tries = 0; tries < 200; tries++)
            {
                var l = new List<BountyModifier>();
                while (l.Count < (champion ? 3 : 2)) { var m = all[rng.Next(all.Length)]; if (!l.Contains(m)) l.Add(m); }
                if (Solvable(l)) return l;
            }
            return new List<BountyModifier> { BountyModifier.TrotzFenster, BountyModifier.Schnelligkeit };
        }

        public bool Accept(string today, int seed)
        {
            if (State == BountyState.Locked && LockedDay == today) return false;
            if (State == BountyState.Searching || State == BountyState.Spawned) return false;
            State = BountyState.Searching; TimeLeft = TimeLimit; Modifiers.Clear(); Modifiers.AddRange(Roll(seed, Champion)); return true;
        }

        /// <summary>Spieler betritt den Spürsinn-Kreis → Ziel erscheint.</summary>
        public bool Reach(float distanceToSearchCenter)
        {
            if (State != BountyState.Searching || distanceToSearchCenter > SearchRadius) return false;
            State = BountyState.Spawned; return true;
        }

        public void Tick(float dt)
        {
            if (State != BountyState.Searching && State != BountyState.Spawned) return;
            TimeLeft -= dt; if (TimeLeft <= 0f) { TimeLeft = 0f; State = BountyState.Failed; }
        }

        /// <summary>Ziel besiegt: Marken; Ziel ist bis zum nächsten Tag gesperrt.</summary>
        public int Defeat(string today)
        {
            if (State != BountyState.Spawned) return 0;
            State = BountyState.Locked; LockedDay = today;
            return Champion ? ChampionTokens : VeteranTokens;
        }

        public void NewDay(string today) { if (State == BountyState.Locked && LockedDay != today) State = BountyState.Available; if (State == BountyState.Failed || State == BountyState.Completed) State = BountyState.Available; }

        public static string ModifierName(BountyModifier m)
        {
            switch (m)
            {
                case BountyModifier.Orbkranz: return "Orbkranz (bei 50 %)"; case BountyModifier.FesselFelder: return "Fessel-Felder"; case BountyModifier.Spiegelbilder: return "Spiegelbilder";
                case BountyModifier.Zorn: return "Zorn bei Schaden"; case BountyModifier.HeilungBeiBewegung: return "Heilung bei Bewegung"; case BountyModifier.TrotzFenster: return "Trotzleisten-Fenster (20 s)";
                case BountyModifier.Schnelligkeit: return "Schnelligkeit"; case BountyModifier.Blindheitsnebel: return "Blindheitsnebel"; case BountyModifier.KettenBlitz: return "Ketten-Blitz";
                default: return "Reittier-Abwurf";
            }
        }
    }
}
