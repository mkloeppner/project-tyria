using UnityEngine;

namespace ProjectTyrant
{
    public partial class PlayerProfile
    {
        [Header("Fertigkeiten & Eigenschaften")]
        public SpecializationDefinition[] Specializations = new SpecializationDefinition[0];

        SkillLoadoutModel _hookedLoadout;
        TraitModel _hookedTraits;
        static readonly Color Warn = new Color(1f, 0.6f, 0.4f);

        public SkillLoadoutModel Loadout => State.Loadout;
        public TraitModel Traits => State.Traits;

        void InitSkills()
        {
            if (_hookedLoadout != null) _hookedLoadout.Changed -= OnSkillsChanged;
            if (_hookedTraits != null) _hookedTraits.Changed -= OnSkillsChanged;
            State.Traits.SetSpecializations(Specializations);
            State.Loadout.FillDefaults(AllSkills);
            _hookedLoadout = State.Loadout; _hookedTraits = State.Traits;
            _hookedLoadout.Changed += OnSkillsChanged; _hookedTraits.Changed += OnSkillsChanged;
            ApplySkills();
        }

        void OnSkillsChanged()
        {
            ApplySkills();
            GameEventBus.RaiseProfileChanged();
            RequestSave("Fertigkeiten");
        }

        void ApplySkills()
        {
            if (Player == null) return;
            State.Loadout.ApplyTo(Player.GetComponent<SkillCaster>());
            State.Traits.ApplyTo(Player.Modifiers);
            Player.Stats.MarkDirty();
        }

        /// <summary>Im Kampf (oder beim Wirken) sind Fertigkeiten und Eigenschaften gesperrt.</summary>
        public bool BuildLocked
        {
            get
            {
                if (Player == null) return false;
                var regen = Player.GetComponent<OutOfCombatRegen>();
                var caster = Player.GetComponent<SkillCaster>();
                return (regen != null && regen.InCombat) || (caster != null && caster.IsCasting) || Player.IsDowned;
            }
        }

        bool CheckLock()
        {
            if (!BuildLocked) return true;
            GameEventBus.Notify("Im Kampf nicht änderbar", Warn);
            return false;
        }

        bool Report(bool ok, string reason, string success)
        {
            if (ok) { if (success != null) GameEventBus.Notify(success, new Color(0.6f, 0.9f, 1f)); }
            else if (reason != null) GameEventBus.Notify(reason, Warn);
            return ok;
        }

        public bool UnlockSkill(SkillDefinition s) => CheckLock() && Report(State.Loadout.Unlock(s, State.Progression, out var r), r, "Freigeschaltet: " + (s != null ? s.DisplayName : ""));
        public bool SlotSkill(SkillDefinition s, int slot) => CheckLock() && Report(State.Loadout.Slot(s, slot, out var r), r, null);

        public bool UnlockTraitTier(string specId)
        {
            if (!CheckLock()) return false;
            var l = State.Traits.Get(specId);
            return Report(State.Traits.UnlockTier(l, State.Progression, out var r), r, l != null && l.Spec != null ? l.Spec.DisplayName + ": Rang " + l.TiersUnlocked + " freigeschaltet" : null);
        }

        public bool UnlockMajor(string specId, int tier, int choice)
        {
            if (!CheckLock()) return false;
            var l = State.Traits.Get(specId);
            bool ok = State.Traits.UnlockMajor(l, tier, choice, State.Progression, out var r);
            return Report(ok, r, ok ? "Eigenschaft freigeschaltet: " + l.Spec.Major(tier, choice).DisplayName : null);
        }

        public bool SelectMajor(string specId, int tier, int choice)
        {
            if (!CheckLock()) return false;
            return Report(State.Traits.Select(State.Traits.Get(specId), tier, choice, out var r), r, null);
        }

        /// <summary>Debug: alle Freischaltungen zurückerstatten.</summary>
        public int RespecAll()
        {
            int n = State.Traits.Refund(State.Progression) + State.Loadout.Refund(AllSkills, State.Progression);
            return n;
        }
    }
}
