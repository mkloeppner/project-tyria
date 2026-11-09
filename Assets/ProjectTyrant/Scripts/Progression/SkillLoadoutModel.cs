using System;
using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>
    /// Fertigkeiten-Pool und Leiste (Heilung, 3 Hilfsfertigkeiten, Elite). Freischalten kostet Heldenpunkte
    /// (Heilung 2, Hilfs-Tier 1/2/3 = 2/4/6, Elite 8 – Werte in <see cref="SkillDefinition.UnlockCost"/>).
    /// </summary>
    public class SkillLoadoutModel
    {
        public const int SlotCount = 5;            // 0 Heilung, 1–3 Hilfe, 4 Elite
        public readonly HashSet<string> Unlocked = new HashSet<string>();
        public readonly SkillDefinition[] Slots = new SkillDefinition[SlotCount];
        public event Action Changed;

        public static SkillSlotType SlotTypeOf(int slot) => slot == 0 ? SkillSlotType.Heal : slot == 4 ? SkillSlotType.Elite : SkillSlotType.Utility;
        public static bool IsSlottable(SkillDefinition s) => s != null && (s.SlotType == SkillSlotType.Heal || s.SlotType == SkillSlotType.Utility || s.SlotType == SkillSlotType.Elite);

        public bool IsUnlocked(SkillDefinition s) => s != null && (s.StartsUnlocked || Unlocked.Contains(s.Id));

        public bool CanUnlock(SkillDefinition s, ProgressionModel p, out string reason)
        {
            reason = null;
            if (!IsSlottable(s)) { reason = "Nicht freischaltbar"; return false; }
            if (IsUnlocked(s)) { reason = "Bereits freigeschaltet"; return false; }
            if (p.AvailableHeroPoints < s.UnlockCost) { reason = "Benötigt " + s.UnlockCost + " Heldenpunkte"; return false; }
            return true;
        }

        public bool Unlock(SkillDefinition s, ProgressionModel p, out string reason)
        {
            if (!CanUnlock(s, p, out reason)) return false;
            p.TrySpendHeroPoints(s.UnlockCost);
            Unlocked.Add(s.Id);
            Changed?.Invoke();
            return true;
        }

        public bool CanSlot(SkillDefinition s, int slot, out string reason)
        {
            reason = null;
            if (slot < 0 || slot >= SlotCount) { reason = "Ungültiger Platz"; return false; }
            if (s == null) { reason = "Keine Fertigkeit"; return false; }
            if (s.SlotType != SlotTypeOf(slot)) { reason = "Passt nicht in diesen Platz"; return false; }
            if (!IsUnlocked(s)) { reason = "Nicht freigeschaltet"; return false; }
            return true;
        }

        /// <summary>Setzt eine Fertigkeit; liegt sie schon in einem anderen Hilfsplatz, werden die Plätze getauscht.</summary>
        public bool Slot(SkillDefinition s, int slot, out string reason)
        {
            if (!CanSlot(s, slot, out reason)) return false;
            for (int i = 0; i < SlotCount; i++)
                if (i != slot && Slots[i] == s) Slots[i] = Slots[slot];
            Slots[slot] = s;
            Changed?.Invoke();
            return true;
        }

        public void ApplyTo(SkillCaster caster)
        {
            if (caster == null) return;
            caster.HealSkill = Slots[0];
            if (caster.Utilities == null || caster.Utilities.Length != 3) caster.Utilities = new SkillDefinition[3];
            caster.Utilities[0] = Slots[1]; caster.Utilities[1] = Slots[2]; caster.Utilities[2] = Slots[3];
            caster.EliteSkill = Slots[4];
        }

        /// <summary>Füllt leere Plätze mit freigeschalteten Standardfertigkeiten (Reihenfolge der Liste).</summary>
        public void FillDefaults(IList<SkillDefinition> pool)
        {
            if (pool == null) return;
            foreach (var s in pool)
            {
                if (!IsSlottable(s) || !IsUnlocked(s)) continue;
                bool already = false; for (int i = 0; i < SlotCount; i++) if (Slots[i] == s) already = true;
                if (already) continue;
                for (int i = 0; i < SlotCount; i++) if (Slots[i] == null && SlotTypeOf(i) == s.SlotType) { Slots[i] = s; break; }
            }
            Changed?.Invoke();
        }

        public int Refund(IList<SkillDefinition> pool, ProgressionModel p)
        {
            int refund = 0;
            if (pool != null) foreach (var s in pool) if (s != null && Unlocked.Contains(s.Id) && !s.StartsUnlocked) refund += s.UnlockCost;
            Unlocked.Clear();
            for (int i = 0; i < SlotCount; i++) if (Slots[i] != null && !Slots[i].StartsUnlocked) Slots[i] = null;
            if (refund > 0) p.RefundHeroPoints(refund);
            FillDefaults(pool);
            return refund;
        }

        public void Clear() { Unlocked.Clear(); Array.Clear(Slots, 0, SlotCount); Changed?.Invoke(); }
    }
}
