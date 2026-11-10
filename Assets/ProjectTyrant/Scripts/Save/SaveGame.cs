using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectTyrant
{
    [Serializable] public class ItemStackDto { public int Bag; public int Slot; public string Id; public int Count; public bool Bound; public int Charges; }
    [Serializable] public class EquipDto { public int Slot; public string Id; public bool Bound; }
    [Serializable] public class FoodDto { public string Id; public float Remaining; }
    [Serializable] public class TraitLineDto { public string Line; public int Tiers; public int[] Majors = new int[3]; public List<int> Unlocked = new List<int>(); }
    [Serializable] public class HeartDto { public string Id; public float Progress; }

    [Serializable] public class MasteryTrackDto { public string Line; public int Rank; public long Xp; }
    [Serializable] public class ZoneFogDto { public string Zone; public float Size; public string Data; }

    /// <summary>Speicherstand eines Charakters (JSON über JsonUtility). Version 3 (Lauf 5: Klasse, Account-Trennung); 2 = Lauf 4; 1 = Lauf 3 (werden migriert).</summary>
    [Serializable]
    public class SaveGameDto
    {
        public const int CurrentVersion = 4;   // Lauf 6: Story-Zustand je Charakter
        public int Version;
        // Lauf 5 / M25: Charakter-Ebene (Account-Felder liegen in account.json; Version 2 = Lauf 4, wird als Charakter 1 migriert)
        public string ClassId;
        public string CharacterName;
        public string EliteSpec;                                                                     // Lauf 5 / M26: aktive Elite-Spezialisierung (Id) – fehlt in älteren Ständen
        public string PrimaryHex, SecondaryHex;
        public int ActiveAttunement;
        public List<string> PetPair = new List<string>();                                         // 0/1 = Lauf 3 (Feld fehlte), ToDto schreibt CurrentVersion
        public string CurrentZone;
        public StoryState Story;                                                                      // Lauf 6: Persönliche Geschichte (fehlt vor Version 4 → Migration)
        public List<ZoneFogDto> ZoneFogs = new List<ZoneFogDto>();
        // Lauf 4: Reittiere + Beherrschungen
        public List<string> Mounts = new List<string>();
        public string SelectedMount;
        public long MasteryXp;
        public int MasteryPointsEarned;
        public List<MasteryTrackDto> Masteries = new List<MasteryTrackDto>();
        public string SelectedMastery;
        public string SavedAt;
        public int Level = 1;
        public int Xp;
        public int HeroPoints;
        public int HeroPointsSpent;
        public long Copper;
        public long Karma;
        public float PosX, PosY, PosZ, RotY;
        public bool HasPosition;
        public List<string> UnlockedSkills = new List<string>();
        public List<string> SlotSkills = new List<string>();       // Heil, Utility 1–3, Elite
        public List<TraitLineDto> Traits = new List<TraitLineDto>();
        public List<string> Bags = new List<string>();             // 4 Taschenplätze (Id oder "")
        public List<ItemStackDto> Inventory = new List<ItemStackDto>();
        public List<EquipDto> Equipment = new List<EquipDto>();
        public FoodDto Food;
        public bool StarterKitGranted;
        public List<string> Completed = new List<string>();        // Wegmarken, POIs, Aussichtspunkte, Herausforderungen, Herzen
        public List<HeartDto> Hearts = new List<HeartDto>();
        public bool ZoneChestGranted;
        public string Fog;                                          // Base64-Bitmaske
        public List<string> Flags = new List<string>();
    }

    public static class SaveSystem
    {
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "ProjectTyrant", "save.json");
        /// <summary>Für Tests/Debug umleitbar.</summary>
        public static string PathOverride;
        public static string CurrentPath => string.IsNullOrEmpty(PathOverride) ? DefaultPath : PathOverride;

        public static string ToJson(SaveGameDto dto) => JsonUtility.ToJson(dto, true);
        public static SaveGameDto FromJson(string json) => string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveGameDto>(json);

        public static void Write(SaveGameDto dto, string path = null)
        {
            path = path ?? CurrentPath;
            dto.SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, ToJson(dto));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public static SaveGameDto Read(string path = null)
        {
            path = path ?? CurrentPath;
            if (!File.Exists(path)) return null;
            try { return FromJson(File.ReadAllText(path)); }
            catch (Exception e) { Debug.LogWarning("[ProjectTyrant] Speicherstand unlesbar: " + e.Message); return null; }
        }

        public static bool Exists(string path = null) => File.Exists(path ?? CurrentPath);

        public static void Delete(string path = null)
        {
            path = path ?? CurrentPath;
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
