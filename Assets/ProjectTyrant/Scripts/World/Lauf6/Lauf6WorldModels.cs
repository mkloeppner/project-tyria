using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6 / M30: Sandsturm-Zyklus im Dünenmeer (rein, getestet): 12 min Periode, 30 s Vorwarnung, 2 min Sturm. Im Sturm sinken Sicht und Tempo, Späher-Zelte schützen.</summary>
    public static class SandstormModel
    {
        public enum Phase { Ruhig, Warnung, Sturm }
        public const float Period = 720f, StormStart = 600f, StormDuration = 120f, Warning = 30f;
        public static float Local(float t) { float m = t % Period; return m < 0f ? m + Period : m; }
        public static Phase At(float t)
        {
            float m = Local(t);
            if (m >= StormStart && m < StormStart + StormDuration) return Phase.Sturm;
            if (m >= StormStart - Warning && m < StormStart) return Phase.Warnung;
            return Phase.Ruhig;
        }
        /// <summary>Sekunden bis zum nächsten Sturmbeginn (0 im Sturm).</summary>
        public static float UntilStorm(float t) { float m = Local(t); return At(t) == Phase.Sturm ? 0f : (m < StormStart ? StormStart - m : Period - m + StormStart); }
        /// <summary>Stärke 0..1 mit weichem An- und Abschwellen (je 10 s).</summary>
        public static float Intensity(float t)
        {
            float m = Local(t) - StormStart;
            if (m < 0f || m > StormDuration) return 0f;
            return Mathf.Clamp01(Mathf.Min(m, StormDuration - m) / 10f);
        }
        public static float SpeedFactor(float intensity, bool sheltered) => sheltered ? 1f : Mathf.Lerp(1f, 0.6f, intensity);
        public static float FogFactor(float intensity) => Mathf.Lerp(1f, 6f, intensity);
    }

    /// <summary>Lauf 6 / M30: Verderbnis im Dämmerwald (rein, getestet): Flecken wachsen, solange die Verderbnis-Ereignisse nicht bestanden sind, und weichen nach Erfolg zurück.</summary>
    public class CorruptionModel
    {
        public float BaseRadius = 14f, MaxRadius = 30f, MinRadius = 6f, GrowPerMinute = 2f, ShrinkOnSuccess = 10f;
        public float Radius { get; private set; }
        public CorruptionModel(float baseRadius = 14f) { BaseRadius = baseRadius; Radius = baseRadius; }
        public void Tick(float dt) => Radius = Mathf.Min(MaxRadius, Radius + GrowPerMinute * dt / 60f);
        public void EventSucceeded() => Radius = Mathf.Max(MinRadius, Radius - ShrinkOnSuccess);
        public void EventFailed() => Radius = Mathf.Min(MaxRadius, Radius + ShrinkOnSuccess * 0.5f);
        /// <summary>Qual-Stapel je Sekunde für einen Spieler in Abstand <paramref name="distance"/> (0 = geschützt oder außerhalb).</summary>
        public int TormentPerSecond(float distance, bool protectedByLight, bool mastery) => protectedByLight || mastery || distance > Radius ? 0 : distance < Radius * 0.5f ? 2 : 1;
    }

    /// <summary>Lauf 6 / M32: Dunkelheit in der Kristalltiefe (rein, getestet): Lichtradius aus Laterne und Kristallen; außerhalb des Lichts Schwäche und eingeschränkte Sicht.</summary>
    public static class DarknessModel
    {
        public const float LanternRadius = 14f, CrystalRadius = 9f, BaseVision = 7f;
        public static bool InLight(bool lantern, float nearestCrystal) => lantern || nearestCrystal <= CrystalRadius;
        public static float Vision(bool lantern, float nearestCrystal) => lantern ? LanternRadius * 2.5f : nearestCrystal <= CrystalRadius ? CrystalRadius * 3f : BaseVision;
        /// <summary>Nebeldichte für eine Sichtweite (exp²: 2 % Rest bei <paramref name="vision"/> m).</summary>
        public static float FogDensity(float vision) => Mathf.Sqrt(3.9f) / Mathf.Max(1f, vision);
    }

    /// <summary>Lauf 6 / M32: Meta „Belagerung der Feste“ (rein, getestet): Tor 1 → Tor 2 → Tor 3 → Hof → Thronsaal; Scheitern wirft auf Tor 1 zurück, der Thronsaal öffnet erst nach drei Toren.</summary>
    public class SiegeModel
    {
        public static readonly string[] Phases = { "belagerung_tor1", "belagerung_tor2", "belagerung_tor3", "belagerung_hof", "belagerung_thron" };
        public int Phase { get; private set; }
        public bool Completed { get; private set; }
        public int GatesDown => Mathf.Min(Phase, 3);
        public bool ThroneOpen => Phase >= 4;
        public string Current => Completed ? null : Phases[Phase];
        public void Succeed(string id) { if (Completed || id != Current) return; Phase++; if (Phase >= Phases.Length) { Completed = true; Phase = Phases.Length - 1; } }
        public void Fail(string id) { if (id != Current) return; Phase = 0; Completed = false; }
        public void Reset() { Phase = 0; Completed = false; }
    }

    /// <summary>Lauf 6 / M32: Greif-Flug (rein, getestet): Gleitflug mit geringem Sinken, Sturzflug beschleunigt, Abfangen wandelt Tempo in Höhe.</summary>
    public class GriffonFlight
    {
        public float BaseSpeed = 14f, MaxBoost = 16f, GlideSink = 2.4f, EnduranceSink = 1.6f, DiveSink = 18f, DiveAccel = 10f, BoostDecay = 3f, ClimbFactor = 0.7f, TakeoffHeight = 9f;
        public bool Endurance;   // Beherrschung „Greif 2“
        public bool DiveUnlocked = true;   // Beherrschung „Greif 1“
        public float Boost { get; private set; }
        public bool Diving { get; private set; }
        public float Speed => BaseSpeed + Boost;
        public float Sink => Diving ? DiveSink : Endurance ? EnduranceSink : GlideSink;

        /// <summary>Ein Flug-Takt. <paramref name="climb"/> &gt; 0 beim Abfangen (vertikale Geschwindigkeit nach oben).</summary>
        public void Tick(float dt, bool airborne, bool dive, out float climb)
        {
            climb = 0f;
            if (!airborne) { Boost = 0f; Diving = false; return; }
            bool wasDiving = Diving;
            Diving = dive && DiveUnlocked;
            if (Diving) Boost = Mathf.Min(MaxBoost, Boost + DiveAccel * dt);
            else
            {
                if (wasDiving && Boost > 1f) { climb = Boost * ClimbFactor; Boost *= 0.5f; }
                Boost = Mathf.Max(0f, Boost - BoostDecay * dt);
            }
        }
        public void Land() { Boost = 0f; Diving = false; }
    }
}

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Zonen-Freischaltung – alle Zonen frei begehbar, nur die Tyrannenfeste öffnet sich mit Kapitel 10 (Flag „tyrannenfeste_offen“).</summary>
    public static class ZoneGate
    {
        public const string FortressZone = "tyrannenfeste", FortressFlag = "tyrannenfeste_offen";
        public static bool Allowed(string zoneId, StoryState s, out string reason)
        {
            reason = null;
            if (zoneId != FortressZone) return true;
            if (s != null && (s.HasFlag(FortressFlag) || s.Chapter >= 10 || s.Finished)) return true;
            reason = "Die Tyrannenfeste ist versiegelt – sie öffnet sich mit Kapitel 10 der Persönlichen Geschichte.";
            return false;
        }
    }
}
