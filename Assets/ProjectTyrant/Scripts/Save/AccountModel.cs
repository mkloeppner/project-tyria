using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M25: Charakterplatz (Zusammenfassung für die Charakterauswahl).</summary>
    [Serializable]
    public class CharacterSlotDto
    {
        public int Slot; public string Name; public string ClassId; public int Level = 1; public string Zone = ProfileState.StartZoneId;
        public string Created; public string PrimaryHex = "#C8A040", SecondaryHex = "#3050A0"; public string LastPlayed;
        public string EliteSpec = "";   // Lauf 5 / M26: Anzeigename der aktiven Elite-Spezialisierung (Charakterauswahl)
    }

    /// <summary>
    /// Lauf 5 / M25: Account-Ebene (wie GW2): Geldbörse, Karma, Reittiere, Beherrschungen, Optionen und die bis zu 5 Charakterplätze.
    /// Charakter-Ebene bleibt das bisherige <see cref="SaveGameDto"/> (Klasse, Level, Skills, Linien, Inventar, Ausrüstung, Zonen, Position).
    /// </summary>
    [Serializable]
    public class AccountDto
    {
        public const int CurrentVersion = 3;
        public int Version = CurrentVersion;
        public long Copper, Karma;
        public List<string> Mounts = new List<string>();
        public string SelectedMount;
        public long MasteryXp; public int MasteryPointsEarned;
        public List<MasteryTrackDto> Masteries = new List<MasteryTrackDto>();
        public string SelectedMastery;
        public string Options;
        public int Selected = -1;
        public List<CharacterSlotDto> Characters = new List<CharacterSlotDto>();
        public string SavedAt;
    }

    public static class AccountModel
    {
        public const int MaxCharacters = 5;

        /// <summary>Lauf-4-Speicherstand → Account + Charakter 1 (Klasse Wächter).</summary>
        public static AccountDto MigrateLegacy(SaveGameDto legacy)
        {
            var acc = new AccountDto();
            if (legacy == null) return acc;
            ExtractAccount(legacy, acc);
            if (string.IsNullOrEmpty(legacy.ClassId)) legacy.ClassId = "waechter";
            if (string.IsNullOrEmpty(legacy.CharacterName)) legacy.CharacterName = "Aeryn Glutwacht";
            acc.Characters.Add(new CharacterSlotDto { Slot = 0, Name = legacy.CharacterName, ClassId = legacy.ClassId, Level = Math.Max(1, legacy.Level), Zone = string.IsNullOrEmpty(legacy.CurrentZone) ? ProfileState.StartZoneId : legacy.CurrentZone, Created = legacy.SavedAt ?? "" });
            acc.Selected = 0;
            return acc;
        }

        /// <summary>Account-Felder aus einem vollständigen Stand übernehmen (beim Speichern).</summary>
        public static void ExtractAccount(SaveGameDto d, AccountDto acc)
        {
            acc.Copper = d.Copper; acc.Karma = d.Karma;
            acc.Mounts = new List<string>(d.Mounts ?? new List<string>()); acc.SelectedMount = d.SelectedMount;
            acc.MasteryXp = d.MasteryXp; acc.MasteryPointsEarned = d.MasteryPointsEarned;
            acc.Masteries = new List<MasteryTrackDto>(d.Masteries ?? new List<MasteryTrackDto>()); acc.SelectedMastery = d.SelectedMastery;
        }

        /// <summary>Account-Felder in den Charakterstand schreiben (beim Laden) – Account gewinnt.</summary>
        public static void ApplyAccount(AccountDto acc, SaveGameDto d)
        {
            d.Copper = acc.Copper; d.Karma = acc.Karma;
            d.Mounts = new List<string>(acc.Mounts ?? new List<string>()); d.SelectedMount = acc.SelectedMount;
            d.MasteryXp = acc.MasteryXp; d.MasteryPointsEarned = acc.MasteryPointsEarned;
            d.Masteries = new List<MasteryTrackDto>(acc.Masteries ?? new List<MasteryTrackDto>()); d.SelectedMastery = acc.SelectedMastery;
        }

        public static CharacterSlotDto Get(AccountDto acc, int slot) { foreach (var c in acc.Characters) if (c.Slot == slot) return c; return null; }

        public static int FreeSlot(AccountDto acc)
        {
            for (int i = 0; i < MaxCharacters; i++) if (Get(acc, i) == null) return i;
            return -1;
        }

        public static bool ValidName(AccountDto acc, string name, out string reason)
        {
            reason = null; name = (name ?? "").Trim();
            if (name.Length < 3 || name.Length > 24) { reason = "Name: 3–24 Zeichen"; return false; }
            foreach (var c in acc.Characters) if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) { reason = "Name bereits vergeben"; return false; }
            foreach (char ch in name) if (!(char.IsLetter(ch) || ch == ' ' || ch == '-' || ch == '\'')) { reason = "Nur Buchstaben, Leerzeichen, - und '"; return false; }
            return true;
        }

        /// <summary>Neuen Charakter anlegen (Startzone immer Sonnfurt); liefert den Platz oder −1.</summary>
        public static int Create(AccountDto acc, string name, string classId, string primaryHex, string secondaryHex, out string reason)
        {
            reason = null;
            if (!ValidName(acc, name, out reason)) return -1;
            int slot = FreeSlot(acc); if (slot < 0) { reason = "Alle 5 Charakterplätze belegt"; return -1; }
            acc.Characters.Add(new CharacterSlotDto { Slot = slot, Name = name.Trim(), ClassId = classId, Level = 1, Zone = ProfileState.StartZoneId, Created = DateTime.Now.ToString("yyyy-MM-dd HH:mm"), PrimaryHex = primaryHex, SecondaryHex = secondaryHex });
            acc.Characters.Sort((a, b) => a.Slot.CompareTo(b.Slot));
            return slot;
        }

        /// <summary>Löschen nur mit Namenseingabe (wie GW2).</summary>
        public static bool Delete(AccountDto acc, int slot, string confirmName, out string reason)
        {
            reason = null; var c = Get(acc, slot);
            if (c == null) { reason = "Kein Charakter"; return false; }
            if (!string.Equals((confirmName ?? "").Trim(), c.Name, StringComparison.Ordinal)) { reason = "Name stimmt nicht überein"; return false; }
            acc.Characters.Remove(c);
            if (acc.Selected == slot) acc.Selected = acc.Characters.Count > 0 ? acc.Characters[0].Slot : -1;
            return true;
        }

        public static SaveGameDto NewCharacter(CharacterSlotDto slot)
            => new SaveGameDto { Version = SaveGameDto.CurrentVersion, ClassId = slot.ClassId, CharacterName = slot.Name, PrimaryHex = slot.PrimaryHex, SecondaryHex = slot.SecondaryHex, CurrentZone = ProfileState.StartZoneId, Level = 1 };
    }

    public static partial class AccountStore
    {
        public static string Dir => Path.GetDirectoryName(SaveSystem.CurrentPath);
        public static string AccountPath => Path.Combine(Dir, "account.json");
        public static string CharacterPath(int slot) => Path.Combine(Dir, "char_" + slot + ".json");

        public static AccountDto LoadOrMigrate()
        {
            if (File.Exists(AccountPath))
            {
                try { var a = JsonUtility.FromJson<AccountDto>(File.ReadAllText(AccountPath)); if (a != null) return a; }
                catch (Exception e) { Debug.LogWarning("[ProjectTyrant] Account unlesbar: " + e.Message); }
            }
            var legacy = SaveSystem.Read();   // Lauf 4: save.json
            var acc = AccountModel.MigrateLegacy(legacy);
            if (legacy != null) { SaveSystem.Write(legacy, CharacterPath(0)); Debug.Log("[ProjectTyrant] Speicherstand aus Lauf 4 als Charakter 1 migriert."); }
            Save(acc);
            return acc;
        }

        public static void Save(AccountDto acc)
        {
            acc.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Directory.CreateDirectory(Dir);
            string tmp = AccountPath + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(acc, true));
            if (File.Exists(AccountPath)) File.Delete(AccountPath);
            File.Move(tmp, AccountPath);
        }

        public static void DeleteCharacterFile(int slot) { var p = CharacterPath(slot); if (File.Exists(p)) File.Delete(p); }
    }
}
