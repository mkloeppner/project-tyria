namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Bot-Profil (rein, deterministisch mit Seed).</summary>
    public struct BotSettings
    {
        public BotProfileKind Kind;
        /// <summary>Genutzte Waffen-Skills (Slots 0..n-1).</summary>
        public int WeaponSkills;
        public bool UseUtilities;
        /// <summary>Heilskill unter diesem HP-Anteil.</summary>
        public float HealBelow;
        /// <summary>Jedem n-ten Telegraph ausweichen (1 = jedem).</summary>
        public int DodgeEvery;
        public float Reaction;
        public float FleeBelow;
        /// <summary>Flucht: geradeaus vom nächsten Gegner weg (Anfänger) oder zur nächsten Wegmarke (Erfahren).</summary>
        public bool FleeToSafety;
        public bool UseMount, ReadsDanger, BreakBars;
        /// <summary>Meidet Gebiete mit Level ≥ eigenes + n (0 = nie).</summary>
        public int AvoidLevelAbove;

        public static BotSettings For(BotProfileKind k) => k == BotProfileKind.Beginner
            ? new BotSettings { Kind = k, WeaponSkills = 3, UseUtilities = false, HealBelow = 0.30f, DodgeEvery = 2, Reaction = 0.6f, FleeBelow = 0.25f, FleeToSafety = false, UseMount = false, ReadsDanger = false, BreakBars = false, AvoidLevelAbove = 0 }
            : new BotSettings { Kind = k, WeaponSkills = 5, UseUtilities = true, HealBelow = 0.50f, DodgeEvery = 1, Reaction = 0.25f, FleeBelow = 0.20f, FleeToSafety = true, UseMount = true, ReadsDanger = true, BreakBars = true, AvoidLevelAbove = 2 };

        /// <summary>Weicht der Bot diesem Telegraph aus (Zähler 1-basiert)?</summary>
        public bool DodgesTelegraph(int index) => DodgeEvery <= 1 || index % DodgeEvery == 0;
    }
}
