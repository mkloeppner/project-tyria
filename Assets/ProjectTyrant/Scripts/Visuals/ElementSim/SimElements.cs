using System.Collections.Generic;
using System.Linq;

namespace ProjectTyrant.Visuals.ElementSim
{
    /// <summary>
    /// Elemente des VFX-Laufs V1. Die drei Leuchttürme sind Wasser, Feuer und Brechung (Licht, Eis, Kristall, Arkan-Linsen);
    /// die G1-Zuordnung (`FxElement`, 10 Elemente ohne Wasser) bleibt unverändert und wird hier verfeinert.
    /// </summary>
    public enum SimElement { Wasser, Feuer, Brechung, Blitz, Natur, Schatten, Jade, Sand, Chaos, Arkan, Keins }

    public enum FxSkillType { Projektil, Strahl, Flaeche, Nahkampf, Sprung, Wirbel, Aura, Beschwoerung, Heilung, Blockade, Kette, Finisher }

    /// <summary>Reine Daten eines Skills für Zuordnung und Schaustück-Auswahl (aus SkillDefinition im Editor befüllt).</summary>
    public struct SkillFacts
    {
        public string Id, Name, ClassId, EliteSpec;
        public VfxPalette Palette;
        public ComboFieldType Field;
        public SkillDelivery Delivery;
        public SkillSlotType Slot;
        public bool Finisher, Heals, Blocks, Chain, Summons, Whirl;
        public float Duration, Radius;
        /// <summary>Nutzung aus der Balance-Telemetrie (0, wenn keine vorliegt).</summary>
        public float Usage;
    }

    public static class SimElements
    {
        public static readonly SimElement[] Lighthouses = { SimElement.Wasser, SimElement.Feuer, SimElement.Brechung };

        static readonly string[] WaterWords = { "wasser", "flut", "welle", "quell", "strom", "gischt", "regen", "tide", "brandung", "nass", "tropf", "fluss", "see" };
        static readonly string[] RefractWords = { "eis", "frost", "kristall", "prism", "linse", "spiegel", "licht", "glanz", "strahl", "sonne", "mond", "glas", "splitter" };
        static readonly string[] FireWords = { "feuer", "flamm", "brand", "glut", "lava", "asche", "lohe", "fackel", "inferno", "funk", "verbrenn", "hitze" };

        static bool Has(string name, string[] words) { var n = (name ?? "").ToLowerInvariant(); return words.Any(w => n.Contains(w)); }

        /// <summary>Hauptelement: Kombo-Feld zuerst (eindeutige Mechanik), dann Name, dann Palette über die G1-Zuordnung.</summary>
        public static SimElement Primary(SkillFacts s)
        {
            if (s.Field == ComboFieldType.Fire) return SimElement.Feuer;
            if (s.Field == ComboFieldType.Water) return SimElement.Wasser;
            if (s.Field == ComboFieldType.Lightning) return SimElement.Blitz;
            if (s.Field == ComboFieldType.Smoke) return SimElement.Schatten;
            if (Has(s.Name, FireWords)) return SimElement.Feuer;
            if (Has(s.Name, WaterWords) || s.Palette == VfxPalette.Wasser) return SimElement.Wasser;
            switch (ElementMap.Of(s.Palette))
            {
                case FxElement.Feuer: return SimElement.Feuer;
                case FxElement.Eis: case FxElement.Licht: return SimElement.Brechung;
                case FxElement.Blitz: return SimElement.Blitz;
                case FxElement.Natur: return SimElement.Natur;
                case FxElement.Schatten: return SimElement.Schatten;
                case FxElement.Jade: return SimElement.Jade;
                case FxElement.Sand: return SimElement.Sand;
                case FxElement.Chaos: return SimElement.Chaos;
                case FxElement.Arkan: return Has(s.Name, RefractWords) ? SimElement.Brechung : SimElement.Arkan;
                default: return SimElement.Brechung;
            }
        }

        public static FxSkillType TypeOf(SkillFacts s)
        {
            if (s.Finisher && s.Slot == SkillSlotType.Elite) return FxSkillType.Finisher;
            if (s.Summons) return FxSkillType.Beschwoerung;
            if (s.Blocks) return FxSkillType.Blockade;
            if (s.Whirl) return FxSkillType.Wirbel;
            if (s.Chain) return FxSkillType.Kette;
            if (s.Heals && s.Delivery == SkillDelivery.Self) return FxSkillType.Heilung;
            switch (s.Delivery)
            {
                case SkillDelivery.Projectile: return s.Radius <= 0.6f && s.Duration > 0.8f ? FxSkillType.Strahl : FxSkillType.Projektil;
                case SkillDelivery.GroundTarget: return FxSkillType.Flaeche;
                case SkillDelivery.Leap: return FxSkillType.Sprung;
                case SkillDelivery.Self: return s.Radius >= 3f ? FxSkillType.Flaeche : FxSkillType.Aura;
                default: return FxSkillType.Nahkampf;
            }
        }

        /// <summary>
        /// Sekundär-Beteiligung, bis jeder Leuchtturm seinen Anteil erreicht (Wasser, Feuer ≥ 10 %, Brechung ≥ 12 %).
        /// Kandidaten: Skills mit passendem Wort im Namen, sonst passender Palette-Nachbar (Eis → Brechung, Glut → Feuer, Natur → Wasser).
        /// Nie Umbenennung, nie Wechsel des Hauptelements.
        /// </summary>
        public static Dictionary<string, SimElement> Secondaries(IList<SkillFacts> skills, Dictionary<string, SimElement> primary)
        {
            var sec = new Dictionary<string, SimElement>();
            var targets = new Dictionary<SimElement, float> { { SimElement.Wasser, 0.10f }, { SimElement.Feuer, 0.10f }, { SimElement.Brechung, 0.12f } };
            foreach (var t in targets)
            {
                int need = (int)System.Math.Ceiling(t.Value * skills.Count) - Share(skills, primary, sec, t.Key);
                if (need <= 0) continue;
                var words = t.Key == SimElement.Wasser ? WaterWords : t.Key == SimElement.Feuer ? FireWords : RefractWords;
                var cands = skills.Where(s => primary[s.Id] != t.Key && !sec.ContainsKey(s.Id))
                    .OrderByDescending(s => Has(s.Name, words) ? 2 : Neighbour(primary[s.Id], t.Key) ? 1 : 0).ThenByDescending(s => s.Usage).ThenBy(s => s.Id).ToList();
                foreach (var c in cands.Take(need)) sec[c.Id] = t.Key;
            }
            return sec;
        }

        static bool Neighbour(SimElement from, SimElement to) =>
            (to == SimElement.Brechung && (from == SimElement.Arkan || from == SimElement.Jade || from == SimElement.Blitz)) ||
            (to == SimElement.Wasser && (from == SimElement.Natur || from == SimElement.Jade)) ||
            (to == SimElement.Feuer && (from == SimElement.Chaos || from == SimElement.Sand));

        public static int Share(IList<SkillFacts> skills, Dictionary<string, SimElement> primary, Dictionary<string, SimElement> sec, SimElement e) =>
            skills.Count(s => primary[s.Id] == e || (sec.TryGetValue(s.Id, out var x) && x == e));

        /// <summary>
        /// Schaustücke: je Klasse drei (möglichst verschiedene Typen, häufig genutzt, lange sichtbar, klares Element),
        /// je Elite-Spezialisierung einer, dazu alle Elite-Skills und alle Finisher.
        /// </summary>
        public static HashSet<string> Showpieces(IList<SkillFacts> skills, Dictionary<string, SimElement> primary)
        {
            var set = new HashSet<string>();
            foreach (var s in skills) if (s.Slot == SkillSlotType.Elite || s.Finisher) set.Add(s.Id);
            float Score(SkillFacts s) => s.Usage * 2f + s.Duration + s.Radius * 0.3f + (System.Array.IndexOf(Lighthouses, primary[s.Id]) >= 0 ? 1f : 0f);
            foreach (var g in skills.Where(s => !string.IsNullOrEmpty(s.ClassId) && string.IsNullOrEmpty(s.EliteSpec)).GroupBy(s => s.ClassId))
            {
                var types = new HashSet<FxSkillType>(); int picked = 0;
                foreach (var s in g.OrderByDescending(Score).ThenBy(s => s.Id))
                    if (picked < 3 && types.Add(TypeOf(s)) && primary[s.Id] != SimElement.Keins) { set.Add(s.Id); picked++; }
                foreach (var s in g.OrderByDescending(Score).ThenBy(s => s.Id)) if (picked < 3 && set.Add(s.Id)) picked++;
            }
            foreach (var g in skills.Where(s => !string.IsNullOrEmpty(s.EliteSpec)).GroupBy(s => s.EliteSpec))
            {
                var best = g.OrderByDescending(Score).ThenBy(s => s.Id).First(); set.Add(best.Id);
            }
            return set;
        }

        /// <summary>Mindest-Lagen V1: Schaustück 7, sonst 4 (G1-Mindestwerte 3/5/6 bleiben als Untergrenze).</summary>
        public static int MinLayersV1(bool showpiece) => showpiece ? 7 : 4;
    }
}
