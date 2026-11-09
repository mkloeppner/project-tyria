using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>
    /// Eigenschafts-Fortschritt je Spezialisierung: Ränge freischalten (5 HP je Rang, der Reihe nach),
    /// große Eigenschaften freischalten (3 HP) und je Rang eine davon wählen. Kleine Eigenschaften wirken automatisch.
    /// </summary>
    public class TraitModel
    {
        public const int TierCost = 5;
        public const int MajorCost = 3;
        public const int Tiers = 3;

        public class Line
        {
            public string Id;
            public SpecializationDefinition Spec;
            public int TiersUnlocked;
            public readonly HashSet<int> UnlockedMajors = new HashSet<int>();   // Index 0–8
            public readonly int[] Selected = { -1, -1, -1 };                      // Wahl 0–2 je Rang
        }

        readonly List<Line> _lines = new List<Line>();

        // ------------------------------------------------------------ Lauf 5 / M26: Elite-Spezialisierungen
        /// <summary>Aktive Elite-Linie (belegt den dritten Platz) oder null.</summary>
        public string ActiveElite { get; private set; }
        public static bool IsElite(Line l) => l != null && l.Spec is EliteSpecDefinition;
        public EliteSpecDefinition ActiveEliteSpec { get { var l = Get(ActiveElite); return l != null ? l.Spec as EliteSpecDefinition : null; } }
        /// <summary>Freigeschaltete Elite-Schritte der aktiven Spezialisierung (1 Linie, 2 Waffe, 3 Skilltyp).</summary>
        public int ActiveEliteSteps { get { var l = Get(ActiveElite); return l != null ? l.TiersUnlocked : 0; } }

        /// <summary>Die drei Plätze: Kernlinien 1 und 2 immer, Platz 3 = dritte Kernlinie oder die aktive Elite-Linie.</summary>
        public bool IsLineActive(Line l)
        {
            if (l == null) return false;
            if (IsElite(l)) return l.Id == ActiveElite;
            if (ActiveElite == null) return true;
            int core = 0; foreach (var x in _lines) { if (IsElite(x)) continue; if (x == l) return core < 2; core++; }
            return false;
        }

        public bool SetActiveElite(string id, out string reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(id)) { if (ActiveElite != null) { ActiveElite = null; Changed?.Invoke(); } return true; }
            var l = Get(id);
            if (!IsElite(l)) { reason = "Keine Elite-Spezialisierung"; return false; }
            if (l.TiersUnlocked < 1) { reason = "Erst freischalten (Stufe " + EliteSpecDefinition.RequiredLevel + ", " + EliteSpecDefinition.StepCost + " Heldenpunkte)"; return false; }
            if (ActiveElite == id) return true;
            ActiveElite = id; Changed?.Invoke(); return true;
        }

        /// <summary>Laden: ungeprüft setzen (Linien werden danach eingelesen).</summary>
        public void RestoreActiveElite(string id) { ActiveElite = string.IsNullOrEmpty(id) ? null : id; }
        public IReadOnlyList<Line> Lines => _lines;
        public event Action Changed;

        public void SetSpecializations(IEnumerable<SpecializationDefinition> specs)
        {
            var old = new Dictionary<string, Line>();
            foreach (var l in _lines) if (l.Id != null) old[l.Id] = l;
            _lines.Clear();
            foreach (var s in specs)
            {
                if (s == null) continue;
                if (old.TryGetValue(s.Id, out var keep)) { keep.Spec = s; _lines.Add(keep); }
                else _lines.Add(new Line { Id = s.Id, Spec = s });
            }
            if (ActiveElite != null) { var a = Get(ActiveElite); if (!IsElite(a) || a.TiersUnlocked < 1) ActiveElite = null; }   // Klasse gewechselt / Linie fehlt
            Changed?.Invoke();
        }

        public Line Get(string specId) { foreach (var l in _lines) if (l.Id == specId) return l; return null; }

        /// <summary>Für Laden: Linie per Id holen oder (noch ohne Definition) anlegen.</summary>
        public Line GetOrCreate(string specId)
        {
            var l = Get(specId);
            if (l == null) { l = new Line { Id = specId }; _lines.Add(l); }
            return l;
        }

        public static int TotalCostPerLine => Tiers * TierCost + 9 * MajorCost;

        public static int CostOf(Line l) => IsElite(l) ? EliteSpecDefinition.StepCost : TierCost;

        public bool CanUnlockTier(Line l, ProgressionModel p, out string reason)
        {
            reason = null;
            if (l == null) { reason = "Unbekannt"; return false; }
            if (l.TiersUnlocked >= Tiers) { reason = IsElite(l) ? "Vollständig freigeschaltet" : "Alle Ränge freigeschaltet"; return false; }
            if (IsElite(l) && p.Level < EliteSpecDefinition.RequiredLevel) { reason = "Elite-Spezialisierungen ab Stufe " + EliteSpecDefinition.RequiredLevel; return false; }
            if (p.AvailableHeroPoints < CostOf(l)) { reason = "Benötigt " + CostOf(l) + " Heldenpunkte"; return false; }
            return true;
        }

        public bool UnlockTier(Line l, ProgressionModel p, out string reason)
        {
            if (!CanUnlockTier(l, p, out reason)) return false;
            p.TrySpendHeroPoints(CostOf(l));
            l.TiersUnlocked++;
            Changed?.Invoke();
            return true;
        }

        public bool CanUnlockMajor(Line l, int tier, int choice, ProgressionModel p, out string reason)
        {
            reason = null;
            int idx = (tier - 1) * 3 + choice;
            if (l == null || tier < 1 || tier > Tiers || choice < 0 || choice > 2) { reason = "Ungültig"; return false; }
            if (l.UnlockedMajors.Contains(idx)) { reason = "Bereits freigeschaltet"; return false; }
            if (l.TiersUnlocked < tier) { reason = "Erst Rang " + tier + " freischalten"; return false; }
            if (!IsElite(l) && p.AvailableHeroPoints < MajorCost) { reason = "Benötigt " + MajorCost + " Heldenpunkte"; return false; }
            return true;
        }

        /// <summary>Schaltet eine große Eigenschaft frei; ist im Rang noch nichts gewählt, wird sie direkt gewählt.</summary>
        public bool UnlockMajor(Line l, int tier, int choice, ProgressionModel p, out string reason)
        {
            if (!CanUnlockMajor(l, tier, choice, p, out reason)) return false;
            if (!IsElite(l)) p.TrySpendHeroPoints(MajorCost);   // Elite-Linien: Haupteigenschaften sind mit dem Schritt bezahlt
            l.UnlockedMajors.Add((tier - 1) * 3 + choice);
            if (l.Selected[tier - 1] < 0) l.Selected[tier - 1] = choice;
            Changed?.Invoke();
            return true;
        }

        public bool Select(Line l, int tier, int choice, out string reason)
        {
            reason = null;
            if (l == null || tier < 1 || tier > Tiers || choice < 0 || choice > 2) { reason = "Ungültig"; return false; }
            if (!l.UnlockedMajors.Contains((tier - 1) * 3 + choice)) { reason = "Nicht freigeschaltet"; return false; }
            l.Selected[tier - 1] = choice;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Alle aktiven Eigenschaften (kleine der freigeschalteten Ränge + gewählte große).</summary>
        public IEnumerable<TraitDefinition> ActiveTraits()
        {
            foreach (var l in _lines)
            {
                if (l.Spec == null || !IsLineActive(l)) continue;
                for (int t = 1; t <= l.TiersUnlocked; t++)
                {
                    var minor = l.Spec.Minors.Length >= t ? l.Spec.Minors[t - 1] : null;
                    if (minor != null) yield return minor;
                    int sel = l.Selected[t - 1];
                    if (sel >= 0) { var m = l.Spec.Major(t, sel); if (m != null) yield return m; }
                }
            }
        }

        /// <summary>Ersetzt alle Eigenschafts-Modifikatoren im Set (Quelle „trait:“).</summary>
        public void ApplyTo(CombatModifierSet set)
        {
            if (set == null) return;
            set.RemoveSource("traits");
            foreach (var t in ActiveTraits())
                foreach (var spec in t.Modifiers) set.Add(TraitModifierFactory.Create(spec, "traits"));
        }

        /// <summary>Gibt alle ausgegebenen Punkte zurück (Debug/„Zurücksetzen“).</summary>
        public int Refund(ProgressionModel p)
        {
            int refund = 0;
            foreach (var l in _lines)
            {
                refund += IsElite(l) ? l.TiersUnlocked * EliteSpecDefinition.StepCost : l.TiersUnlocked * TierCost + l.UnlockedMajors.Count * MajorCost;
                l.TiersUnlocked = 0; l.UnlockedMajors.Clear(); l.Selected[0] = l.Selected[1] = l.Selected[2] = -1;
            }
            ActiveElite = null;
            if (refund > 0) p.RefundHeroPoints(refund);
            Changed?.Invoke();
            return refund;
        }

        public void Clear() { ActiveElite = null; foreach (var l in _lines) { l.TiersUnlocked = 0; l.UnlockedMajors.Clear(); l.Selected[0] = l.Selected[1] = l.Selected[2] = -1; } Changed?.Invoke(); }
        public void NotifyChanged() => Changed?.Invoke();
    }
}
