using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Reiner Spielerfortschritt (ohne Unity-Objekte): Level/XP, Geldbörse, Nebel, Zonenabschluss, Position.
    /// Weitere Teile (Inventar, Ausrüstung, Skills, Eigenschaften) ergänzen sich über partielle Klassen.
    /// </summary>
    public partial class ProfileState
    {
        public ProgressionModel Progression = new ProgressionModel();
        public WalletModel Wallet = new WalletModel();
        public const string StartZoneId = "sonnfurt";
        public string CurrentZone = StartZoneId;
        readonly Dictionary<string, FogModel> _fogs = new Dictionary<string, FogModel>();
        readonly Dictionary<string, float> _zoneSizes = new Dictionary<string, float>();
        /// <summary>Nebel der aktuellen Zone (je Zone eigener Nebel).</summary>
        public FogModel Fog => FogFor(CurrentZone);
        public void RegisterZoneSize(string zone, float size) { if (!string.IsNullOrEmpty(zone)) _zoneSizes[zone] = size; }
        public FogModel FogFor(string zone)
        {
            if (string.IsNullOrEmpty(zone)) zone = StartZoneId;
            if (!_fogs.TryGetValue(zone, out var f))
            {
                float size = _zoneSizes.TryGetValue(zone, out var s) ? s : Procedural.ZoneLayout.Size;
                f = new FogModel(128, size); _fogs[zone] = f;
            }
            return f;
        }
        public IEnumerable<string> FogZones => _fogs.Keys;
        public readonly HashSet<string> Completed = new HashSet<string>();
        public readonly Dictionary<string, float> Hearts = new Dictionary<string, float>();
        public bool ZoneChestGranted;
        public bool HasPosition;
        public Vector3 Position;
        public float RotationY;
        public readonly List<string> Flags = new List<string>();
        // Lauf 5 / M25: Charakter
        public string ClassId = "waechter";
        public string CharacterName = "Aeryn Glutwacht";
        public string PrimaryHex = "#C8A040", SecondaryHex = "#3050A0";
        public int ActiveAttunement;
        public readonly List<string> PetPair = new List<string>();

        public SaveGameDto ToDto()
        {
            var d = new SaveGameDto
            {
                Level = Progression.Level, Xp = Progression.Xp, HeroPoints = Progression.HeroPoints, HeroPointsSpent = Progression.HeroPointsSpent,
                Version = SaveGameDto.CurrentVersion, Copper = Wallet.Copper, Karma = Wallet.Karma,
                HasPosition = HasPosition, PosX = Position.x, PosY = Position.y, PosZ = Position.z, RotY = RotationY,
                Fog = FogFor(StartZoneId).Serialize(), ZoneChestGranted = ZoneChestGranted, CurrentZone = CurrentZone,
                ClassId = ClassId, CharacterName = CharacterName, EliteSpec = Traits.ActiveElite ?? "", PrimaryHex = PrimaryHex, SecondaryHex = SecondaryHex, ActiveAttunement = ActiveAttunement,
            };
            d.PetPair.AddRange(PetPair);
            foreach (var kv in _fogs) d.ZoneFogs.Add(new ZoneFogDto { Zone = kv.Key, Size = kv.Value.WorldSize, Data = kv.Value.Serialize() });
            d.Completed.AddRange(Completed);
            foreach (var kv in Hearts) d.Hearts.Add(new HeartDto { Id = kv.Key, Progress = kv.Value });
            d.Flags.AddRange(Flags);
            WriteItems(d); WriteSkills(d); WriteWorld(d);
            return d;
        }

        public void LoadDto(SaveGameDto d, ItemDatabase items, IList<SkillDefinition> skills)
        {
            if (d == null) return;
            Progression.Level = Mathf.Clamp(d.Level, 1, AttributeSet.MaxLevel);
            Progression.Xp = Mathf.Max(0, d.Xp);
            Progression.HeroPoints = d.HeroPoints; Progression.HeroPointsSpent = d.HeroPointsSpent;
            // Migration (M10): ältere Stände bekamen 1 statt 4 Punkte je Stufe.
            if (Progression.HeroPoints < ProgressionModel.HeroPointsForLevel(Progression.Level)) Progression.HeroPoints = ProgressionModel.HeroPointsForLevel(Progression.Level);
            Wallet.Copper = d.Copper; Wallet.Karma = d.Karma;
            HasPosition = d.HasPosition; Position = new Vector3(d.PosX, d.PosY, d.PosZ); RotationY = d.RotY;
            // Zonen (Lauf 4). Version 1 (Lauf 3): ein Nebel = Startzone, Zone = Startzone.
            _fogs.Clear();
            CurrentZone = string.IsNullOrEmpty(d.CurrentZone) ? StartZoneId : d.CurrentZone;
            if (d.Version < 2 || d.ZoneFogs == null || d.ZoneFogs.Count == 0) FogFor(StartZoneId).Deserialize(d.Fog);
            else foreach (var z in d.ZoneFogs) { if (string.IsNullOrEmpty(z.Zone)) continue; RegisterZoneSize(z.Zone, z.Size); FogFor(z.Zone).Deserialize(z.Data); }
            Completed.Clear(); if (d.Completed != null) foreach (var c in d.Completed) Completed.Add(c);
            if (d.Version < 2 && d.ZoneChestGranted) Completed.Add("zonechest:" + StartZoneId);   // Migration Lauf 3
            Hearts.Clear(); if (d.Hearts != null) foreach (var h in d.Hearts) Hearts[h.Id] = h.Progress;
            ZoneChestGranted = d.ZoneChestGranted;
            Flags.Clear(); if (d.Flags != null) Flags.AddRange(d.Flags);
            ClassId = string.IsNullOrEmpty(d.ClassId) ? "waechter" : d.ClassId;   // Lauf 4 → Wächter
            CharacterName = string.IsNullOrEmpty(d.CharacterName) ? "Aeryn Glutwacht" : d.CharacterName;
            if (!string.IsNullOrEmpty(d.PrimaryHex)) PrimaryHex = d.PrimaryHex; if (!string.IsNullOrEmpty(d.SecondaryHex)) SecondaryHex = d.SecondaryHex;
            ActiveAttunement = d.ActiveAttunement; PetPair.Clear(); if (d.PetPair != null) PetPair.AddRange(d.PetPair);
            ReadItems(d, items); ReadSkills(d, skills); ReadWorld(d);
            Traits.RestoreActiveElite(d.EliteSpec);
        }

        partial void WriteWorld(SaveGameDto d);
        partial void ReadWorld(SaveGameDto d);
        partial void WriteItems(SaveGameDto d);
        partial void ReadItems(SaveGameDto d, ItemDatabase items);
        partial void WriteSkills(SaveGameDto d);
        partial void ReadSkills(SaveGameDto d, IList<SkillDefinition> skills);
    }
}
