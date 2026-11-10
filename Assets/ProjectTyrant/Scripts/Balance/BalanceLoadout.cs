using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>
    /// Balance B1: „erwartete Ausrüstung“ für Messungen (Trainingspuppe, Bots): Waffenstärke, Kraft, Rüstung und Vitalität werden auf den
    /// erwarteten Spieler-Stand des Levels gesetzt (Herzen und Loot bis dahin, <see cref="ExpectedPlayer"/>) – unabhängig von der echten Ausrüstung
    /// des Mess-Charakters. Nur während Messungen aktiv, Spielstand wird nicht gespeichert.
    /// </summary>
    public static class BalanceLoadout
    {
        public static bool Enabled = true;
        public static bool Active { get; private set; }
        static FlatAttributeSource _src; static Combatant _p; static int _level;

        public static float WeaponStrength(int level) => DamageFormula.WeaponStrength(level);

        public static void Apply(Combatant p, int level)
        {
            Remove();
            if (!Enabled || p == null) return;
            _p = p; _level = level;
            _src = new FlatAttributeSource();
            p.Stats.AddSource(_src);
            var e = EnemyCurve.Player;
            _src.Defense = e.Armor(level) - p.Stats.Armor;
            _src.Values[(int)Attr.Power] = e.Power(level) - p.Stats.Get(Attr.Power);
            _src.Values[(int)Attr.Vitality] = AttributeSet.PrimaryBase(level) * (1f + e.GearVitality) - p.Stats.Get(Attr.Vitality);
            p.Stats.MarkDirty();
            Active = true;
            p.WeaponStrengthOverride = WeaponStrength(level);
            p.SetHealth(p.MaxHealth);
        }

        public static void Remove()
        {
            if (_p != null && _src != null) { _p.Stats.RemoveSource(_src); _p.Stats.MarkDirty(); }
            _src = null; _p = null; Active = false;
        }

        /// <summary>Erwartete Waffenstärke, solange aktiv (PlayerProfile fragt beim Waffenwechsel nach).</summary>
        public static float Override(float ws) => Active ? WeaponStrength(_level) : ws;
    }
}
