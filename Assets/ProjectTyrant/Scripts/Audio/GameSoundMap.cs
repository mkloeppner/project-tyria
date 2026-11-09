using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M24: Zuordnung Spielereignis → Klang (+ optional Stinger). Rein, damit ein Test jedes Ereignis der Liste prüfen kann.</summary>
    public static class GameSoundMap
    {
        public struct Cue { public string Sfx; public StingerKind? Stinger; public Cue(string s, StingerKind? st = null) { Sfx = s; Stinger = st; } }

        /// <summary>Banner-Titel (Präfix) → Klang. Reihenfolge = Priorität.</summary>
        public static readonly KeyValuePair<string, Cue>[] Banners =
        {
            new KeyValuePair<string, Cue>("Ereignis erfolgreich", new Cue("Event/Success", StingerKind.Victory)),
            new KeyValuePair<string, Cue>("Ereignis fehlgeschlagen", new Cue("Event/Fail", StingerKind.Defeat)),
            new KeyValuePair<string, Cue>("Stufe", new Cue("Prog/LevelUp", StingerKind.LevelUp)),
            new KeyValuePair<string, Cue>("Wegmarke entdeckt", new Cue("World/Waypoint")),
            new KeyValuePair<string, Cue>("Sehenswürdigkeit entdeckt", new Cue("World/POI", StingerKind.Discovery)),
            new KeyValuePair<string, Cue>("Aussichtspunkt erreicht", new Cue("World/Vista", StingerKind.Discovery)),
            new KeyValuePair<string, Cue>("Heldenherausforderung", new Cue("World/HeroPoint")),
            new KeyValuePair<string, Cue>("Herz erfüllt", new Cue("Event/Medal")),
            new KeyValuePair<string, Cue>("Beherrschungs-Einblick", new Cue("World/POI")),
            new KeyValuePair<string, Cue>("Beherrschung erlernt", new Cue("Prog/Mastery", StingerKind.Mastery)),
            new KeyValuePair<string, Cue>("Zone abgeschlossen", new Cue("World/ZoneComplete", StingerKind.Victory)),
            new KeyValuePair<string, Cue>("Trotzleiste gebrochen", new Cue("Combat/DefianceBreak")),
            new KeyValuePair<string, Cue>("Verborgene Truhe", new Cue("World/Chest")),
            new KeyValuePair<string, Cue>("Erfolg", new Cue("Event/Medal")),
            new KeyValuePair<string, Cue>("Reittier freigeschaltet", new Cue("Mount/Call")),
            new KeyValuePair<string, Cue>("Meta-Ereignis", new Cue("Event/Horn")),
            new KeyValuePair<string, Cue>("Der Anführer", new Cue("Boss/Roar")),
            new KeyValuePair<string, Cue>("Die Dornwacht ist gefallen", new Cue("Event/Fail", StingerKind.Defeat)),
            new KeyValuePair<string, Cue>("Die Dornwacht ist gehalten", new Cue("Event/Success", StingerKind.Victory)),
        };

        /// <summary>Benachrichtigungs-Text (Präfix) → Klang.</summary>
        public static readonly KeyValuePair<string, Cue>[] Notifications =
        {
            new KeyValuePair<string, Cue>("Gereist", new Cue("World/Portal", StingerKind.Travel)),
            new KeyValuePair<string, Cue>("Angelegt", new Cue("Equip/Metal")),
            new KeyValuePair<string, Cue>("Gekauft", new Cue("UI/Buy")),
            new KeyValuePair<string, Cue>("Verkauft", new Cue("UI/Sell")),
            new KeyValuePair<string, Cue>("Geborgen", new Cue("Loot/Salvage")),
            new KeyValuePair<string, Cue>("Besiegt", new Cue("Event/Fail", StingerKind.Death)),
            new KeyValuePair<string, Cue>("Ereignis", new Cue("Event/Horn")),
            new KeyValuePair<string, Cue>("Event", new Cue("Event/Horn")),
        };

        public static Cue? ForBanner(string title)
        {
            if (string.IsNullOrEmpty(title)) return null;
            foreach (var kv in Banners) if (title.StartsWith(kv.Key)) return kv.Value;
            if (title.Contains("Phase") || title.Contains("unter Angriff") || title.Contains("Weltboss")) return new Cue("Event/Horn");
            return new Cue("World/POI");
        }

        public static Cue? ForNotification(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return null;
            foreach (var kv in Notifications) if (msg.StartsWith(kv.Key)) return kv.Value;
            return null;
        }

        /// <summary>Feste Spielereignisse (Bus/Systeme) → Klang. Der Vollständigkeitstest prüft, dass alle existieren.</summary>
        public static readonly string[] FixedEvents =
        {
            "Hit/Flesh", "Hit/Armor", "Hit/Stone", "Hit/Wood", "Hit/Elemental", "Hit/Crit", "Hit/Block", "Move/Dodge", "Combat/Interrupted", "Combat/ComboFinish",
            "Move/Jump", "Move/Land", "Move/GliderOpen", "Move/GliderClose", "Mount/Call", "Mount/Engage", "Mount/Dismount", "Mount/Ability",
            "Enemy/Telegraph", "Boss/Roar", "Loot/Coins", "Loot/Salvage", "Equip/Metal", "Equip/Leather", "Equip/Cloth",
            "World/Waypoint", "World/POI", "World/Vista", "World/ZoneComplete", "World/Portal", "World/Chest", "World/HeroPoint",
            "Event/Horn", "Event/Success", "Event/Fail", "Event/Medal", "Prog/LevelUp", "Prog/Mastery", "Combat/DefianceBreak",
        };

        /// <summary>Treffer-Material nach Ziel (Palette des Archetyps, Kreatur oder Humanoid).</summary>
        public static string HitMaterial(VfxPalette? palette, bool creature, bool player)
        {
            if (player) return "Hit/Armor";
            if (palette.HasValue)
                switch (palette.Value)
                {
                    case VfxPalette.Frost: case VfxPalette.Glut: case VfxPalette.Luft: case VfxPalette.Arkan: return "Hit/Elemental";
                    case VfxPalette.Erde: return "Hit/Stone";
                    case VfxPalette.Natur: return creature ? "Hit/Flesh" : "Hit/Wood";
                }
            return creature ? "Hit/Flesh" : "Hit/Armor";
        }

        /// <summary>Gegner-Fraktion für Laute.</summary>
        public static string Faction(VfxPalette palette, bool creature)
        {
            switch (palette)
            {
                case VfxPalette.Natur: return "Moor";
                case VfxPalette.Frost: return "Frost";
                case VfxPalette.Glut: case VfxPalette.Luft: return "Glut";
                default: return creature ? "Beast" : "Bandit";
            }
        }

        /// <summary>Untergrund aus Kollider-Namen (Holz, Stein) und Zone (Standard-Boden).</summary>
        public static string Surface(string colliderName, string zoneId, bool belowWater)
        {
            if (belowWater) return "Water";
            string n = (colliderName ?? "").ToLowerInvariant();
            if (n.Contains("bridge") || n.Contains("steg") || n.Contains("wood") || n.Contains("plank") || n.Contains("dock") || n.Contains("holz") || n.Contains("crate") || n.Contains("stall")) return "Wood";
            if (n.Contains("rock") || n.Contains("stone") || n.Contains("ruin") || n.Contains("wall") || n.Contains("road") || n.Contains("plaza") || n.Contains("cliff") || n.Contains("stein") || n.Contains("fels") || n.Contains("pedestal")) return "Stone";
            switch (zoneId)
            {
                case "nebelmoor": return "Earth";
                case "frostkamm": return "Snow";
                case "gluetoede": return "Ash";
                default: return "Grass";
            }
        }
    }
}
