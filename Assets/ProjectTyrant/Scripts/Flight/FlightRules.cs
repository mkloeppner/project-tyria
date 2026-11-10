using UnityEngine;

namespace ProjectTyrant.Flight
{
    /// <summary>Lauf 10: Ort, an dem geflogen werden soll (vom <see cref="MountController"/> aus der Welt gelesen).</summary>
    public struct FlightContext
    {
        public string Zone;
        public bool InCity, InStoryInstance, InCutscene, StoryFinished, UnderRoof, NearBossFight;
    }

    /// <summary>Lauf 10: Ergebnis der Flugregeln: erlaubt? Höhendecke über Boden, Grund für das Verbot.</summary>
    public struct FlightVerdict
    {
        public bool Allowed; public float Ceiling; public string Reason;
        public static FlightVerdict Ok(float ceiling) => new FlightVerdict { Allowed = true, Ceiling = ceiling };
        public static FlightVerdict No(string reason, float ceiling) => new FlightVerdict { Allowed = false, Ceiling = ceiling, Reason = reason };
    }

    /// <summary>Lauf 10: Admin-Schalter für den Flug (Debug-Panel „/flight …“). Nur Editor/Dev-Build.</summary>
    public static class FlightDebug
    {
        public static bool Infinite, CeilingOff, RulesOff, Show;
        public static void Reset() { Infinite = CeilingOff = RulesOff = Show = false; }
    }

    /// <summary>
    /// Lauf 10: Flugregeln je Zone (rein, getestet). Höhendecke 150 m über Boden, 400 m in den Himmelsspitzen, 80 m in Städten;
    /// Flugverbot in Story-Instanzen, Zwischensequenzen, in der Tyrannenfeste bis zum Ende der Geschichte, unter Dächern
    /// und im Umkreis von 40 m um laufende Champion-/Bosskämpfe. Reihenfolge = Priorität des angezeigten Grundes.
    /// </summary>
    public static class FlightRules
    {
        public const float DefaultCeiling = 150f, PeaksCeiling = 400f, CityCeiling = 80f, BossRadius = 40f;
        public const string PeaksZone = "himmelsspitzen", FortressZone = "tyrannenfeste";

        public static float CeilingFor(string zone, bool inCity) => inCity ? CityCeiling : zone == PeaksZone ? PeaksCeiling : DefaultCeiling;

        public static FlightVerdict Evaluate(FlightContext c, bool rulesOff = false, bool ceilingOff = false)
        {
            float ceiling = ceilingOff ? 100000f : CeilingFor(c.Zone, c.InCity);
            if (rulesOff) return FlightVerdict.Ok(ceiling);
            if (c.InCutscene) return FlightVerdict.No("Während einer Zwischensequenz wird nicht geflogen", ceiling);
            if (c.InStoryInstance) return FlightVerdict.No("In Story-Instanzen ist Fliegen nicht möglich", ceiling);
            if (c.Zone == FortressZone && !c.StoryFinished) return FlightVerdict.No("Über der Tyrannenfeste liegt ein Bann – erst nach dem Ende der Geschichte", ceiling);
            if (c.UnderRoof) return FlightVerdict.No("Unter einem Dach kann nicht geflogen werden", ceiling);
            if (c.NearBossFight) return FlightVerdict.No("Ein Bosskampf tobt – die Luft ist zu unruhig", ceiling);
            return FlightVerdict.Ok(ceiling);
        }

        /// <summary>Flugreittier?</summary>
        public static bool IsFlyer(MountKind k) => k == MountKind.Griffon || k == MountKind.Skyscale;
    }

    /// <summary>
    /// Lauf 10: Erreichbarkeit erhöhter Ziele auf zwei Wegen (rein, getestet; Builder-Bericht). Greif: Start auf Höhe <c>start</c>,
    /// Aufwind hebt bis <c>updraftTop</c>, Gleiten 6:1 verliert <c>dist/6</c>. Himmelsschuppe: steigt mit voller Ausdauer um
    /// Ausdauer/Kosten × Steigtempo; jede Landefläche (Rast) oder Wand (Wandhaften) füllt nach – <c>rests</c> = Zahl der Zwischenhalte.
    /// </summary>
    public static class FlightReach
    {
        public static float SkyscaleClimbPerTank(FlightParams p) => p.ClimbCost > 0f ? p.Endurance / p.ClimbCost * p.ClimbSpeed : 0f;

        public static bool GriffonReaches(float start, float updraftTop, float target, float horizontalDist, FlightParams p)
        {
            float top = Mathf.Max(start, updraftTop);
            return top - horizontalDist / Mathf.Max(1f, p.GlideRatio) >= target;
        }

        public static bool SkyscaleReaches(float start, float target, int rests, FlightParams p)
        {
            return start + SkyscaleClimbPerTank(p) * (rests + 1) >= target;
        }

        /// <summary>Zwei-Wege-Regel: ein Ziel ist gültig platziert, wenn beide Flugreittiere es erreichen.</summary>
        public static bool BothPaths(float start, float updraftTop, float target, float dist, int rests, int griffonRank, int skyscaleRank)
            => GriffonReaches(start, updraftTop, target, dist, FlightParams.Griffon(griffonRank)) && SkyscaleReaches(start, target, rests, FlightParams.Skyscale(skyscaleRank));
    }
}
