namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M25: Schnittstelle der Klassenmechanik zum SkillCaster (Slot-Ersatz, Kosten, Waffenwechsel).</summary>
    public interface IClassMechanic
    {
        /// <summary>Ersatz für Slot 0–4 (Einstimmung, Beschwörung, Tarnungsangriff); null = Standard.</summary>
        SkillDefinition OverrideSlot(int slot, WeaponSet active);
        bool CanCast(SkillDefinition s, out string reason);
        void OnCast(SkillDefinition s);
        bool AllowSwap(out string reason);
    }
}
