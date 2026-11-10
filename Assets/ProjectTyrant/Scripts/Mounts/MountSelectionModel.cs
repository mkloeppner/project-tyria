using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Lauf 10: gespeicherte Reittier-Einstellungen (Account-Ebene).</summary>
    [Serializable]
    public class MountPrefsDto
    {
        public string LastUsed;
        public bool UseLast;
        public bool RadialSetsDefault = true;
        public List<string> Keys = new List<string>();

        public MountPrefsDto Clone() => new MountPrefsDto { LastUsed = LastUsed, UseLast = UseLast, RadialSetsDefault = RadialSetsDefault, Keys = new List<string>(Keys ?? new List<string>()) };
    }

    /// <summary>Lauf 10: Reittier-Auswahl auf Account-Ebene (rein): Besitz, Standard, Direkt-Tasten, Zuletzt-Modus, Radial-Regel.</summary>
    [Serializable]
    public class MountSelectionModel
    {
        public List<MountKind> Owned = new List<MountKind>();
        public List<MountKind> AdminUnlocked = new List<MountKind>();
        public MountKind Default = MountKind.None, LastUsed = MountKind.None;
        public bool UseLastInsteadOfDefault;
        public bool RadialSetsDefault = true;
        public List<string> KeyBindings = new List<string>();   // "Raptor=Alpha1"

        public const float RadialHoldSeconds = 0.25f;
        public static readonly string[] ReservedKeys = { "Y", "F1", "F2", "F3", "F4", "F5", "B", "I", "H", "M", "X", "V", "Space", "Tab", "Escape", "Backquote", "F9", "F10" };

        public bool Owns(MountKind k) => k != MountKind.None && Owned.Contains(k);

        public void Grant(MountKind k, bool admin = false)
        {
            if (k == MountKind.None) return;
            if (!Owned.Contains(k)) Owned.Add(k);
            if (admin && !AdminUnlocked.Contains(k)) AdminUnlocked.Add(k);
            if (Default == MountKind.None) Default = k;
        }

        /// <summary>X tippen: welches Reittier? None = keins gewählt/besessen (Hinweis).</summary>
        public MountKind ForTap()
        {
            var k = UseLastInsteadOfDefault && Owns(LastUsed) ? LastUsed : Default;
            return Owns(k) ? k : MountKind.None;
        }

        public bool SetDefault(MountKind k) { if (!Owns(k)) return false; Default = k; return true; }

        /// <summary>Radialmenü losgelassen auf <paramref name="k"/>: gibt das aufzusitzende Reittier zurück.</summary>
        public MountKind RadialPick(MountKind k)
        {
            if (!Owns(k)) return MountKind.None;
            if (RadialSetsDefault) Default = k;
            return k;
        }

        public void Used(MountKind k) { if (Owns(k)) LastUsed = k; }

        /// <summary>Direktwechsel aufgesessen: am Boden immer, in der Luft nur Greif ↔ Himmelsschuppe mit Vertrauensband.</summary>
        public static bool CanSwitch(MountKind from, MountKind to, bool airborne, bool trustBond)
        {
            if (from == to || to == MountKind.None) return false;
            if (!airborne) return true;
            bool flyers = (from == MountKind.Griffon && to == MountKind.Skyscale) || (from == MountKind.Skyscale && to == MountKind.Griffon);
            return flyers && trustBond;
        }

        public bool Bind(MountKind k, string key, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(key)) { KeyBindings.RemoveAll(b => b.StartsWith(k + "=")); return true; }
            if (Array.IndexOf(ReservedKeys, key) >= 0) { error = "Taste ist bereits belegt: " + key; return false; }
            foreach (var b in KeyBindings) if (b.EndsWith("=" + key) && !b.StartsWith(k + "=")) { error = "Taste nutzt schon " + b.Split('=')[0]; return false; }
            KeyBindings.RemoveAll(b => b.StartsWith(k + "="));
            KeyBindings.Add(k + "=" + key);
            return true;
        }

        public MountKind ForKey(string key)
        {
            foreach (var b in KeyBindings) { var p = b.Split('='); if (p.Length == 2 && p[1] == key && Enum.TryParse(p[0], out MountKind k) && Owns(k)) return k; }
            return MountKind.None;
        }
    }
}
