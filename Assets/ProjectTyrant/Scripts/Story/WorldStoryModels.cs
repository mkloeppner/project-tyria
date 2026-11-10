using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Herz-Aufgabentypen (≥ 8 wie gefordert).</summary>
    public enum HeartWay { Kill, Collect, Use, Animals, Revive, Bundle, Escort, Disarm, Combo, Emote }

    /// <summary>Herz-Logik: Beitrag je Weg, kein Weg allein über 70 % (GW2: mehrere Wege sichtbar), Abschluss bei 100 %.</summary>
    public static class HeartModel
    {
        public const float MaxShare = 0.7f;
        public static string WayKey(string heart, HeartWay w) => heart + "|" + w;

        /// <summary>Beitrag buchen; Rückgabe = neuer Gesamtfortschritt (Summe der je Weg auf 70 % gekappten Anteile, max. 1).</summary>
        public static float Add(Dictionary<string, float> hearts, string heart, HeartWay way, float amount)
        {
            string k = WayKey(heart, way);
            hearts.TryGetValue(k, out float v); hearts[k] = Mathf.Min(MaxShare, v + Mathf.Max(0f, amount));
            float total = Total(hearts, heart);
            hearts[heart] = total;
            return total;
        }

        public static float Total(Dictionary<string, float> hearts, string heart)
        {
            float t = 0f;
            foreach (HeartWay w in Enum.GetValues(typeof(HeartWay))) if (hearts.TryGetValue(WayKey(heart, w), out float v)) t += Mathf.Min(MaxShare, v);
            return Mathf.Min(1f, t);
        }

        /// <summary>Herz-EP: ein Herz ≈ ein Drittel eines Levels im Stufenbereich der Zone (GW2-Leveltempo).</summary>
        public static int Experience(int zoneLevel) => Mathf.RoundToInt(ProgressionModel.XpToNext(Mathf.Clamp(zoneLevel, 1, AttributeSet.MaxLevel - 1)) / 3f);
    }

    /// <summary>Späher: Sektor (Kreis) aufdecken, Herzen im Sektor markieren.</summary>
    public static class ScoutModel
    {
        public static bool InSector(Vector3 sectorCenter, float radius, Vector3 p) { var d = p - sectorCenter; d.y = 0f; return d.sqrMagnitude <= radius * radius; }

        public static List<string> HeartsInSector(Vector3 center, float radius, IEnumerable<KeyValuePair<string, Vector3>> hearts)
        {
            var list = new List<string>();
            foreach (var h in hearts) if (InSector(center, radius, h.Value)) list.Add(h.Key);
            return list;
        }

        /// <summary>Spricht der Spieler zum ersten Mal mit diesem Späher? (zählt im Tagebuch, nicht zum Zonenabschluss)</summary>
        public static bool Speak(StoryState s, string scoutId)
        {
            if (s == null || string.IsNullOrEmpty(scoutId) || s.Scouts.Contains(scoutId)) return false;
            s.Scouts.Add(scoutId); return true;
        }
    }

    /// <summary>Bündel (aufgehobene Weltobjekte): ersetzen Skills 1–5 durch bis zu 3 Bündel-Skills, Skill 5 = Fallen lassen; Ausweichen ja, Waffenwechsel nein.</summary>
    public class BundleModel
    {
        public const int DropSlot = 4;
        public string Current { get; private set; }
        public SkillDefinition[] Skills { get; private set; } = new SkillDefinition[0];
        public SkillDefinition Drop { get; private set; }
        public bool Active => !string.IsNullOrEmpty(Current);
        public bool CanSwapWeapons => !Active;
        public bool CanDodge => true;
        public event Action Changed;

        public bool Pick(string id, SkillDefinition[] skills, SkillDefinition drop)
        {
            if (string.IsNullOrEmpty(id) || skills == null || skills.Length == 0 || skills.Length > 3) return false;
            Current = id; Skills = skills; Drop = drop; Changed?.Invoke(); return true;
        }
        public string Release() { var c = Current; Current = null; Skills = new SkillDefinition[0]; Drop = null; Changed?.Invoke(); return c; }

        /// <summary>Skill im Waffenplatz (0–4) mit Bündel; null = Platz leer. Ohne Bündel: kein Ersatz (Aufrufer nutzt die Waffe).</summary>
        public SkillDefinition SlotSkill(int slot, out bool overridden)
        {
            overridden = Active && slot >= 0 && slot <= DropSlot;
            if (!overridden) return null;
            if (slot == DropSlot) return Drop;
            return slot < Skills.Length ? Skills[slot] : null;
        }
    }

    /// <summary>Emotes (Chat „/winken“ …) mit Clip-Namen aus dem ClipBuilder.</summary>
    public enum Emote { None, Winken, Tanzen, Jubeln, Verbeugen, Sitzen }

    public static class EmoteModel
    {
        public static Emote Parse(string chat)
        {
            if (string.IsNullOrEmpty(chat)) return Emote.None;
            string s = chat.Trim().ToLowerInvariant();
            if (!s.StartsWith("/")) return Emote.None;
            s = s.Substring(1).Split(' ')[0];
            switch (s)
            {
                case "winken": case "wave": return Emote.Winken;
                case "tanzen": case "dance": return Emote.Tanzen;
                case "jubeln": case "cheer": return Emote.Jubeln;
                case "verbeugen": case "bow": return Emote.Verbeugen;
                case "sitzen": case "sit": return Emote.Sitzen;
            }
            return Emote.None;
        }
        public static string Clip(Emote e) => e == Emote.None ? null : "Emote_" + e;
        public static bool Loops(Emote e) => e == Emote.Tanzen || e == Emote.Sitzen;
    }
}
