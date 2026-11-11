using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>
    /// Welt-Lauf W1: Reittier-Tore (rein, getestet). Ein Tor verlangt nur Schlüssel, die im Level-Band der Zone regulär verfügbar sind –
    /// sonst ist es ein Rückkehr-Tor („Benötigt: Greif – komm später wieder“). Pflichtinhalte haben nie ein Tor.
    /// </summary>
    public static class GateRules
    {
        /// <summary>Ab welcher Stufe ein Schlüssel regulär verfügbar ist (Stand Lauf 11: Bänder 1–80).</summary>
        public static int AvailableFromLevel(Traversal key)
        {
            switch (key)
            {
                case Traversal.None: return 1;
                case Traversal.Raptor: return 1;          // Startzone
                case Traversal.Skimmer: return 15;        // Nebelmoor (15–25)
                case Traversal.Springer: return 25;       // Frostkamm
                case Traversal.LavaSkimmer: return 35;    // Glutöde (Umwelt 3)
                case Traversal.Griffon: return 75;        // Himmelsspitzen / Kapitel 9
                case Traversal.Skyscale: return 75;       // Sammlung in den Himmelsspitzen
                case Traversal.CorruptionWard: return 80; // Kapitel 11
                case Traversal.DuneRunner: return 80;     // Glutwüste
                case Traversal.Boat: return 80;           // Dracheninseln
                case Traversal.JadeBot: return 80;
                default:
                    int max = 1;
                    foreach (var k in Singles(key)) max = Mathf.Max(max, AvailableFromLevel(k));
                    return max;
            }
        }

        public static IEnumerable<Traversal> Singles(Traversal keys)
        {
            for (int b = 1; b != 0 && b <= (int)Traversal.All; b <<= 1) if (((int)keys & b) != 0) yield return (Traversal)b;
        }

        /// <summary>Im Band der Zone verfügbar? Bei Alternativ-Schlüsseln (z. B. Skimmer ODER Flug) genügt der früheste.</summary>
        public static bool InBand(Traversal anyOf, int zoneLevelMax)
        {
            if (anyOf == Traversal.None) return true;
            foreach (var k in Singles(anyOf)) if (AvailableFromLevel(k) <= zoneLevelMax) return true;
            return false;
        }

        /// <summary>Gültiges Tor: Schlüssel im Band oder als Rückkehr-Tor markiert.</summary>
        public static bool Valid(GateDef g, int zoneLevelMax) => g == null || g.Keys == Traversal.None || InBand(g.Keys, zoneLevelMax) || g.ReturnGate;

        /// <summary>Erfüllt der Spieler das Tor (mindestens einer der Schlüssel)?</summary>
        public static bool Satisfied(GateDef g, Traversal owned) => g == null || g.Keys == Traversal.None || (g.Keys & owned) != 0;

        public static string Hint(GateDef g)
        {
            if (g == null || g.Keys == Traversal.None) return "";
            string s = "Benötigt: " + ReachabilityModel.Label(g.Keys).Replace(" + ", " oder ");
            return g.ReturnGate ? s + " – komm später wieder" : s;
        }

        /// <summary>Pflichtinhalt (Wegmarke, Herz, Späher, POI, Story, Portal, Händler) darf kein Tor haben.</summary>
        public static bool IsMandatory(MapMarkerKind k) =>
            k == MapMarkerKind.Waypoint || k == MapMarkerKind.Heart || k == MapMarkerKind.PointOfInterest || k == MapMarkerKind.Scout || k == MapMarkerKind.Story || k == MapMarkerKind.Portal || k == MapMarkerKind.Vendor;
    }
}
