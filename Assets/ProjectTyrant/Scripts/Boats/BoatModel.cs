using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: Boot (rein, getestet). Rufen nur auf Wasser (Wassertiefe ≥ 1,2 m), Steuerung mit Trägheit (Beschleunigung, Widerstand, Wenderate),
    /// Anker (Leertaste) stoppt das Boot, bis zu 2 Mitfahrer (Gefährte/Truppe), Angeln vom Boot +1 Seltenheitsstufe.
    /// Beherrschung (2 Stufen): 1 Tempo (+20 %), 2 Jade-Schub (5 s +60 %, 20 s Abklingzeit).
    /// </summary>
    public class BoatModel
    {
        public const float MinDepth = 1.2f, MaxSpeed = 14f, Accel = 4f, Drag = 0.6f, TurnRate = 70f, BoostFactor = 1.6f, BoostTime = 5f, BoostCooldown = 20f;
        public const int MaxPassengers = 2;
        public int Rank;
        public bool Summoned, Anchored;
        public float Speed, Heading, BoostLeft, BoostCd;
        public int Passengers;

        public static bool CanSummon(float waterDepth) => waterDepth >= MinDepth;

        public bool Summon(float waterDepth) { if (!CanSummon(waterDepth)) return false; Summoned = true; Speed = 0f; Anchored = false; return true; }
        public void Dismiss() { Summoned = false; Passengers = 0; Speed = 0f; }
        public bool Board() { if (!Summoned || Passengers >= MaxPassengers) return false; Passengers++; return true; }
        public void ToggleAnchor() { if (Summoned) Anchored = !Anchored; }

        public float Top => MaxSpeed * (Rank >= 1 ? 1.2f : 1f) * (BoostLeft > 0f ? BoostFactor : 1f);

        public bool Boost() { if (Rank < 2 || BoostCd > 0f || !Summoned) return false; BoostLeft = BoostTime; BoostCd = BoostCooldown; return true; }

        /// <summary>throttle −1…1, steer −1…1; liefert die Fahrtrichtung (Grad) und das Tempo.</summary>
        public void Step(float dt, float throttle, float steer)
        {
            if (!Summoned) return;
            if (BoostLeft > 0f) BoostLeft -= dt; if (BoostCd > 0f) BoostCd -= dt;
            if (Anchored) { Speed = Mathf.MoveTowards(Speed, 0f, Accel * 3f * dt); return; }
            Speed += throttle * Accel * dt;
            Speed -= Speed * Drag * dt;
            Speed = Mathf.Clamp(Speed, -Top * 0.3f, Top);
            Heading = Mathf.Repeat(Heading + steer * TurnRate * dt * Mathf.Clamp01(Mathf.Abs(Speed) / 3f + 0.2f), 360f);
        }
    }

    /// <summary>
    /// Lauf 11: Jadegeist (rein, getestet) – Beherrschungslinie mit 4 Stufen: 1 Rettungsprotokoll (belebt einmal je 10 min aus dem Downed-Zustand mit 50 % HP),
    /// 2 Gleitverstärkung (Gleiter-Ausdauer +50 %), 3 Jadebatterien (aktiviert Jade-Geräte: Aufzüge, Brücken, Kanonen, Truhen), 4 Jadeband (Beute im Umkreis 20 m, markiert Angelstellen/Einblicke).
    /// Kerne (Items) steigern die Werte; Stationen laden ihn auf.
    /// </summary>
    public class JadeBotModel
    {
        public const float RescueCooldown = 600f, RescueHealth = 0.5f, LootRadius = 20f;
        public int Rank, Cores;
        public float RescueCd;
        public float Charge = 1f;

        public float GlideEnduranceFactor => Rank >= 2 ? 1.5f + 0.05f * Cores : 1f;
        public bool CanPowerDevices => Rank >= 3 && Charge > 0.1f;
        public bool CollectsLoot => Rank >= 4;
        public float LootRange => CollectsLoot ? LootRadius + Cores : 0f;

        /// <summary>Spieler am Boden: Rettungsprotokoll auslösen? Rückgabe = wiederhergestellter HP-Anteil (0 = nein).</summary>
        public float TryRescue()
        {
            if (Rank < 1 || RescueCd > 0f) return 0f;
            RescueCd = RescueCooldown; return RescueHealth;
        }

        public void Tick(float dt) { if (RescueCd > 0f) RescueCd = Mathf.Max(0f, RescueCd - dt); }

        /// <summary>Jade-Gerät benutzen (verbraucht Ladung); Station lädt voll auf.</summary>
        public bool UseDevice(float cost = 0.25f) { if (!CanPowerDevices || Charge < cost) return false; Charge -= cost; return true; }
        public void Recharge() { Charge = 1f; }
    }

    /// <summary>Lauf 11: Aufzug (Vertikalstadt und Jade-Geräte). Stadt-Aufzüge sind frei, Welt-Aufzüge brauchen Jadebatterien.</summary>
    public class ElevatorModel
    {
        public readonly float[] Levels; public int Current, Target; public float Height; public bool NeedsJade;
        public const float Speed = 4f;
        public ElevatorModel(float[] levels, bool needsJade) { Levels = levels; NeedsJade = needsJade; Height = levels[0]; }

        public bool Call(int level, JadeBotModel bot)
        {
            if (level < 0 || level >= Levels.Length) return false;
            if (NeedsJade && (bot == null || !bot.UseDevice())) return false;
            Target = level; return true;
        }

        public bool Tick(float dt)
        {
            Height = Mathf.MoveTowards(Height, Levels[Target], Speed * dt);
            if (Mathf.Approximately(Height, Levels[Target])) { Current = Target; return true; }
            return false;
        }
    }
}
