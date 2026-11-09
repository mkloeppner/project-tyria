using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M25: Account/Charakter-Trennung und Klasse des aktiven Charakters.</summary>
    public partial class PlayerProfile
    {
        [Header("Lauf 5: Klassen")]
        public ClassLibrary Classes;
        public AccountDto Account { get; private set; }
        public int ActiveSlot => Account != null ? Account.Selected : -1;
        public ClassDefinition CurrentClass { get; private set; }
        SkillDefinition[] _guardianSkills; SpecializationDefinition[] _guardianSpecs;

        /// <summary>Account laden (oder aus Lauf 4 migrieren); ohne Charakter wird ein Wächter in Platz 1 angelegt (Schnellstart im Editor).</summary>
        void EnsureAccount()
        {
            if (Account != null) return;
            Account = AccountStore.LoadOrMigrate();
            if (Account.Characters.Count == 0)
            {
                int slot = AccountModel.Create(Account, "Aeryn Glutwacht", "waechter", "#C8A040", "#3050A0", out _);
                Account.Selected = slot; AccountStore.Save(Account);
            }
            if (Account.Selected < 0 || AccountModel.Get(Account, Account.Selected) == null) Account.Selected = Account.Characters[0].Slot;
            if (!string.IsNullOrEmpty(Account.Options)) { try { var o = GameOptions.FromJson(Account.Options); o.Save(); } catch { } }
        }

        public CharacterSlotDto ActiveCharacter => Account != null ? AccountModel.Get(Account, Account.Selected) : null;

        /// <summary>Klassen-Daten anwenden: Fertigkeiten-Pool, Linien, Waffensets, Mechanik, Waffenbindung, Optik.</summary>
        void ApplyClassData(string classId)
        {
            if (_guardianSkills == null) { _guardianSkills = AllSkills; _guardianSpecs = Specializations; }
            var c = Classes != null ? Classes.Get(classId) : null;
            CurrentClass = c;
            if (c == null || c.Id == "waechter") { _corePool = _guardianSkills; _coreSpecs = _guardianSpecs; }
            else { _corePool = c.SkillPool.ToArray(); _coreSpecs = c.Specializations.ToArray(); }
            var specs = new List<SpecializationDefinition>(_coreSpecs); if (c != null) foreach (var e in c.EliteSpecs) if (e != null) specs.Add(e);
            Specializations = specs.ToArray();
            RebuildSkillPool();
            EquipmentModel.WeaponAllowed = k => CurrentClass == null || CurrentClass.CanWield(k, ActiveElite, State.Traits.ActiveEliteSteps);
            if (Player == null) return;
            var host = Player.GetComponent<ClassMechanicHost>();
            if (host != null)
            {
                if (State.PetPair.Count == 2 && System.Enum.TryParse<PetFamily>(State.PetPair[0], out var a) && System.Enum.TryParse<PetFamily>(State.PetPair[1], out var b)) host.Pet.SetPair(a, b);
                host.SetClass(c);
            }
            var caster = Player.GetComponent<SkillCaster>(); if (caster != null) caster.WeaponSwapCooldown = 10f;
            var vis = Player.GetComponent<PlayerVisuals>(); if (vis != null) vis.ApplyLook(c, State.PrimaryHex, State.SecondaryHex);
            ApplyEliteToPlayer(false);
            Player.DisplayName = State.CharacterName;
        }

        /// <summary>Debug/QA: Klasse des aktiven Charakters wechseln (Waffen = Rekrutenwaffen der Klasse, Fertigkeiten-Leiste neu).</summary>
        public string DebugSwitchClass(string classId)
        {
            var c = Classes != null ? Classes.Get(classId) : null;
            if (c == null || c.Id != classId) return "Unbekannte Klasse: " + classId;
            // Debug-Klassentest: der Spielstand des Charakters bleibt unangetastet, bis seine eigene Klasse wieder aktiv ist.
            var own = ActiveCharacter != null ? ActiveCharacter.ClassId : classId;
            if (classId == own && SaveSuspended) { SaveSuspended = false; Load(); return "Klasse: " + c.DisplayName + " (eigener Spielstand neu geladen)"; }
            SaveSuspended = classId != own;
            State.ClassId = classId;
            State.PrimaryHex = "#" + ColorUtility.ToHtmlStringRGB(c.Color); State.SecondaryHex = "#" + ColorUtility.ToHtmlStringRGB(c.SecondaryColor);
            State.Loadout.Clear();
            ApplyClassData(classId);
            for (int i = 0; i < 2; i++)
            {
                var d = Items != null ? Items.Get(ItemIds.Recruit(c.StartWeapons[i])) : null;
                if (d != null) State.Equipment.SetDirect(i == 0 ? EquipSlot.Weapon1 : EquipSlot.Weapon2, new ItemInstance(d, 1, false));
            }
            InitRuntimeExtra();
            ApplyToPlayer(false);
            if (SaveSuspended) return "Klasse: " + c.DisplayName + " (Testmodus – Speichern ausgesetzt; zurück zu '" + own + "' setzt es fort)";
            RequestSave("Klassenwechsel");
            return "Klasse: " + c.DisplayName;
        }

        public string DebugUnlockAll()
        {
            int n = 0; foreach (var s in AllSkills) if (s != null && SkillLoadoutModel.IsSlottable(s) && !State.Loadout.IsUnlocked(s)) { State.Loadout.Unlocked.Add(s.Id); n++; }
            State.Loadout.FillDefaults(AllSkills); ApplySkills();
            return n + " Fertigkeiten freigeschaltet";
        }

        void SaveAccount(SaveGameDto dto)
        {
            if (Account == null) return;
            AccountModel.ExtractAccount(dto, Account);
            var slot = ActiveCharacter;
            if (slot != null) { slot.Name = State.CharacterName; slot.ClassId = State.ClassId; slot.Level = State.Progression.Level; slot.Zone = State.CurrentZone; slot.LastPlayed = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"); slot.PrimaryHex = State.PrimaryHex; slot.SecondaryHex = State.SecondaryHex; slot.EliteSpec = ActiveElite != null ? ActiveElite.DisplayName : ""; }
            var o = GameOptions.Load(); Account.Options = o.ToJson();
            AccountStore.Save(Account);
        }
    }

    /// <summary>Lauf 5 / M25: Item-Ids der Rekrutenwaffen je Waffentyp (Startausrüstung jeder Klasse).</summary>
    public static class ItemIds
    {
        public static string Prefix(WeaponKind k)
        {
            switch (k)
            {
                case WeaponKind.Greatsword: return "gs"; case WeaponKind.Staff: return "st"; case WeaponKind.Longbow: return "lb";
                case WeaponKind.AxeTorch: return "at"; case WeaponKind.SwordWarhorn: return "sh"; case WeaponKind.ScepterFocus: return "sf";
                case WeaponKind.DaggerDagger: return "dd"; case WeaponKind.SwordPistol: return "sp"; case WeaponKind.Shortbow: return "sb";
                // Lauf 5 / M26: Waffen der Elite-Spezialisierungen
                case WeaponKind.Hammer: return "hm"; case WeaponKind.Rifle: return "rf"; case WeaponKind.DaggerWarhorn: return "dw";
                case WeaponKind.ScepterWarhorn: return "sw"; case WeaponKind.SwordFocus: return "so"; case WeaponKind.ScepterDagger: return "sd";
                case WeaponKind.ScepterPistol: return "sq"; case WeaponKind.AxeDagger: return "ad"; case WeaponKind.SwordShield: return "ss";
                case WeaponKind.ScepterTorch: return "sx";
                default: return "x";
            }
        }
        public static string Recruit(WeaponKind k) => Prefix(k) + "_recruit";
    }
}
