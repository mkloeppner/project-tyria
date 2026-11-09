using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M26: Elite-Spezialisierungen am Profil – Freischaltung (Stufe 30, 3 × 10 Heldenpunkte), Aktivieren nur außerhalb des Kampfes
    /// (belegt Platz 3), Fertigkeiten-Pool mit Skilltyp (ab Schritt 3), Waffe (ab Schritt 2), Mechanik-Variante, Farbakzent + Aura beim Wechsel.
    /// </summary>
    public partial class PlayerProfile
    {
        SkillDefinition[] _corePool = new SkillDefinition[0];
        SpecializationDefinition[] _coreSpecs = new SpecializationDefinition[0];

        /// <summary>Aktive Elite-Spezialisierung des Charakters (oder null).</summary>
        public EliteSpecDefinition ActiveElite => CurrentClass != null && State.Traits.ActiveElite != null ? CurrentClass.Elite(State.Traits.ActiveElite) : null;
        public int ActiveEliteSteps => State.Traits.ActiveEliteSteps;

        /// <summary>Kern-Pool + Skilltyp der aktiven Spezialisierung (ab Schritt 3).</summary>
        void RebuildSkillPool()
        {
            var pool = new List<SkillDefinition>(_corePool ?? new SkillDefinition[0]);
            var e = ActiveElite;
            if (e != null && ActiveEliteSteps >= EliteSpecDefinition.StepSkills) foreach (var s in e.TypeSkills) if (s != null) pool.Add(s);
            AllSkills = pool.ToArray();
        }

        /// <summary>Elite-Spezialisierung aktivieren (id) oder ablegen (null). Nur außerhalb des Kampfes.</summary>
        public bool ActivateElite(string id)
        {
            if (!CheckLock()) return false;
            var before = State.Traits.ActiveElite;
            if (!Report(State.Traits.SetActiveElite(id, out var why), why, null)) return false;
            if (before == State.Traits.ActiveElite) return true;
            RefreshEliteData();
            var e = ActiveElite;
            GameEventBus.Notify(e != null ? "Elite-Spezialisierung: " + e.DisplayName : "Elite-Spezialisierung abgelegt", e != null ? e.Accent : new Color(0.8f, 0.8f, 0.8f));
            ApplyEliteToPlayer(true);
            RequestSave("Elite-Spezialisierung");
            return true;
        }

        /// <summary>Schritt freischalten (1 Linie, 2 Waffe, 3 Skilltyp). Der erste Schritt aktiviert die Spezialisierung nicht automatisch.</summary>
        public bool UnlockEliteStep(string id)
        {
            if (!CheckLock()) return false;
            var l = State.Traits.Get(id); var spec = l != null ? l.Spec as EliteSpecDefinition : null;
            bool ok = State.Traits.UnlockTier(l, State.Progression, out var r);
            if (ok && spec != null && spec.Weapon != null && l.TiersUnlocked == EliteSpecDefinition.StepWeapon) GrantEliteWeapon(spec);
            if (ok && State.Traits.ActiveElite == id) RefreshEliteData();
            string[] step = { "", "Linie", "Waffe", "Skilltyp" };
            return Report(ok, r, ok && spec != null ? spec.DisplayName + ": " + step[Mathf.Clamp(l.TiersUnlocked, 0, 3)] + " freigeschaltet" : null);
        }

        /// <summary>Rekrutenwaffe der Spezialisierung ins Inventar (wie GW2 die Waffe mit dem Schritt nutzbar wird).</summary>
        void GrantEliteWeapon(EliteSpecDefinition spec)
        {
            var d = Items != null ? Items.Get(ItemIds.Recruit(spec.Weapon.Kind)) : null;
            if (d != null && State.Inventory.TryAdd(d, 1) == 0) GameEventBus.Notify("Erhalten: " + d.DisplayName, new Color(0.7f, 0.9f, 1f));
        }

        /// <summary>Pool, Leiste, Waffen und Mechanik nach Aktivieren/Ablegen/Freischalten neu anwenden.</summary>
        void RefreshEliteData()
        {
            RebuildSkillPool();
            var pool = new HashSet<SkillDefinition>(AllSkills);
            for (int i = 0; i < SkillLoadoutModel.SlotCount; i++) if (State.Loadout.Slots[i] != null && !pool.Contains(State.Loadout.Slots[i])) State.Loadout.Slots[i] = null;
            State.Loadout.FillDefaults(AllSkills);
            // Elite-Waffe einer anderen (oder keiner) Spezialisierung ablegen
            for (int set = 0; set < 2; set++)
            {
                var slot = set == 0 ? EquipSlot.Weapon1 : EquipSlot.Weapon2; var k = State.Equipment.WeaponKindOf(set);
                if (k != WeaponKind.None && CurrentClass != null && !CurrentClass.CanWield(k, ActiveElite, ActiveEliteSteps)) Unequip(slot);
            }
            OnEquipmentChanged();
            ApplySkills();
        }

        void ApplyEliteToPlayer(bool fx)
        {
            if (Player == null) return;
            var e = ActiveElite;
            var host = Player.GetComponent<ClassMechanicHost>(); if (host != null) host.SetElite(e, ActiveEliteSteps);
            var vis = Player.GetComponent<PlayerVisuals>(); if (vis != null) vis.SetAccent(e != null ? e.Accent : (Color?)null);
            var slot = ActiveCharacter; if (slot != null) slot.EliteSpec = e != null ? e.DisplayName : "";
            if (!fx) return;
            var pal = e != null ? e.Palette : CurrentClass != null ? CurrentClass.Palette : VfxPalette.Licht;
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Aura, pal, VfxSize.L), Player.transform.position, Quaternion.identity, Player.transform, 0.9f);
            SkillFx.Vfx(VfxIds.Compose(VfxShape.Shockwave, pal, VfxSize.M), Player.transform.position, Quaternion.identity, null, 0.9f);
            GameAudio.Play(VfxIds.Sfx(pal, "Buff"), Player.transform.position);
        }

        /// <summary>Debug/QA: Spezialisierung sofort vollständig freischalten und aktivieren (Stufe 30, Punkte werden gutgeschrieben).</summary>
        public string DebugActivateElite(string id)
        {
            var c = CurrentClass; var e = c != null ? c.Elite(id) : null;
            if (e == null) { var ids = new List<string>(); if (c != null) foreach (var x in c.EliteSpecs) if (x != null) ids.Add(x.Id); return string.IsNullOrEmpty(id) ? "Ids: " + string.Join(", ", ids) : "Unbekannt: " + id + " (Ids: " + string.Join(", ", ids) + ")"; }
            if (State.Progression.Level < EliteSpecDefinition.RequiredLevel) State.Progression.SetLevel(EliteSpecDefinition.RequiredLevel);
            var l = State.Traits.Get(id);
            while (l != null && l.TiersUnlocked < EliteSpecDefinition.Steps)
            {
                if (State.Progression.AvailableHeroPoints < EliteSpecDefinition.StepCost) State.Progression.HeroPoints += EliteSpecDefinition.StepCost;
                if (!State.Traits.UnlockTier(l, State.Progression, out var r)) return r;
            }
            for (int t = 1; t <= 3; t++) if (l.Selected[t - 1] < 0) State.Traits.UnlockMajor(l, t, 0, State.Progression, out _);
            State.Traits.SetActiveElite(id, out _);
            RefreshEliteData();
            // Elite-Waffe in Set 2 anlegen
            var d = Items != null && e.Weapon != null ? Items.Get(ItemIds.Recruit(e.Weapon.Kind)) : null;
            if (d != null) { State.Equipment.SetDirect(EquipSlot.Weapon2, new ItemInstance(d, 1, false)); OnEquipmentChanged(); }
            var caster = Player.GetComponent<SkillCaster>(); if (caster != null && caster.ActiveSetIndex != 1) caster.TrySwapWeapons();
            ApplyEliteToPlayer(true);
            RequestSave("Elite (Debug)");
            return "Elite: " + e.DisplayName;
        }
    }
}
