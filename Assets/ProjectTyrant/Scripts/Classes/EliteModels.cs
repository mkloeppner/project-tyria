using System;

namespace ProjectTyrant
{
    // Lauf 5 / M26: reine Modelle der Elite-Mechaniken (EditMode-getestet). Zeiten in Sekunden.

    /// <summary>Hainwächter – F5 Hainform: Leiste füllt sich durch Heilung (und langsam im Kampf), 15 s Avatar mit 5 Heil-/Support-Skills.</summary>
    public class HainformModel
    {
        public const float Max = 100f, Duration = 15f, Recharge = 10f, HealToGauge = 40f, CombatFill = 1.5f;
        public float Gauge { get; private set; }
        public bool Active { get; private set; }
        public float Remaining { get; private set; }
        public float Cooldown { get; private set; }
        public bool CanEnter => !Active && Cooldown <= 0f && Gauge >= Max;
        /// <summary>Heilung um einen vollen Lebensbalken füllt 40 % der Leiste.</summary>
        public void AddHealing(float amount, float maxHealth) { if (!Active && maxHealth > 0f && amount > 0f) Gauge = Math.Min(Max, Gauge + amount / maxHealth * HealToGauge); }
        public bool Enter() { if (!CanEnter) return false; Active = true; Remaining = Duration; return true; }
        public void Exit() { if (!Active) return; Active = false; Remaining = 0f; Gauge = 0f; Cooldown = Recharge; }
        public void Tick(float dt, bool inCombat)
        {
            if (Active) { Remaining -= dt; Gauge = Math.Max(0f, Max * Remaining / Duration); if (Remaining <= 0f) Exit(); return; }
            Cooldown = Math.Max(0f, Cooldown - dt);
            if (inCombat) Gauge = Math.Min(Max, Gauge + CombatFill * dt);
        }
    }

    /// <summary>Wegelagerer – Stehlen markiert das Ziel; Treffer auf das markierte Ziel bauen Bosheit auf (max. 5); der Finisher verbraucht sie.</summary>
    public class MaliceModel
    {
        public const int Max = 5; public const float HitInterval = 0.5f, PerMalice = 0.15f, KneelRange = 1.5f;
        public int Malice { get; private set; }
        public int MarkedId { get; private set; } = -1;
        public bool Kneeling { get; private set; }
        float _next;
        public void Mark(int targetId) { MarkedId = targetId; Malice = 0; _next = 0f; }
        public void ClearMark() { MarkedId = -1; Malice = 0; }
        public bool OnHit(int targetId, float time)
        {
            if (targetId != MarkedId || MarkedId < 0 || time < _next || Malice >= Max) return false;
            Malice++; _next = time + HitInterval; return true;
        }
        /// <summary>Finisher: Schadensfaktor 1 + 15 % je Bosheit; verbraucht Bosheit und Markierung.</summary>
        public float Finish() { float k = 1f + PerMalice * Malice; Malice = 0; MarkedId = -1; return k; }
        public void ToggleKneel() => Kneeling = !Kneeling;
        public float RangeFactor => Kneeling ? KneelRange : 1f;
    }

    /// <summary>Sturmrufer – Überladen: nach 5 s in einer Einstimmung erneut dieselbe Taste (oder F5) → Überlade-Kanal; die Einstimmung lädt danach 5 s länger.</summary>
    public class OverloadModel
    {
        public const float Hold = 5f, ExtraRecharge = 5f;
        public float TimeInAttunement { get; private set; }
        public bool Used { get; private set; }
        public bool Ready => !Used && TimeInAttunement >= Hold;
        public void OnSwap() { TimeInAttunement = 0f; Used = false; }
        public void Tick(float dt) => TimeInAttunement += dt;
        public bool Use() { if (!Ready) return false; Used = true; return true; }
        /// <summary>Zusätzliche Wiederaufladung der verlassenen Einstimmung, wenn darin überladen wurde.</summary>
        public float PenaltyOnLeave => Used ? ExtraRecharge : 0f;
    }

    /// <summary>Weber – Doppel-Einstimmung: Hauptelement = gewählte, Nebenelement = vorherige Einstimmung; Skill 3 wird Kombi-Skill beider Elemente.</summary>
    public class DualAttunementModel
    {
        public Element Primary { get; private set; } = Element.Fire;
        public Element Secondary { get; private set; } = Element.Fire;
        public void Swap(Element to) { if (to == Primary || to == Element.None) return; Secondary = Primary; Primary = to; }
        public bool IsDual => Primary != Secondary;
        /// <summary>6 Kombinationen (ungeordnet): 0 Feuer+Wasser, 1 Feuer+Luft, 2 Feuer+Erde, 3 Wasser+Luft, 4 Wasser+Erde, 5 Luft+Erde; −1 = kein Paar.</summary>
        public static int ComboIndex(Element a, Element b)
        {
            if (a == b || a == Element.None || b == Element.None) return -1;
            int x = Math.Min((int)a, (int)b), y = Math.Max((int)a, (int)b);
            if (x == 0) return y - 1;          // 0,1,2
            if (x == 1) return y + 1;          // 3,4
            return 5;
        }
        public int Combo => ComboIndex(Primary, Secondary);
    }

    /// <summary>Schattenhüter – Schattenschleier: zweite Lebensleiste aus Schattenkraft; Schaden trifft zuerst den Schleier, die Kraft sinkt mit der Zeit.</summary>
    public class ShroudModel
    {
        public const float Max = 100f, EnterMin = 50f, Drain = 5f, HealthShare = 0.6f, Recharge = 5f;
        public float Force { get; private set; }
        public bool Active { get; private set; }
        public float ShroudHealth { get; private set; }
        public float ShroudMax { get; private set; }
        public float Cooldown { get; private set; }
        public bool CanEnter => !Active && Cooldown <= 0f && Force >= EnterMin;
        public void Gain(float v) { if (!Active) Force = Math.Min(Max, Force + v); }
        public bool Enter(float playerMaxHealth)
        {
            if (!CanEnter) return false;
            Active = true; ShroudMax = playerMaxHealth * HealthShare; ShroudHealth = ShroudMax * Force / Max; return true;
        }
        public void Exit() { if (!Active) return; Active = false; Force = Max * (ShroudMax > 0f ? ShroudHealth / ShroudMax : 0f); ShroudHealth = 0f; Cooldown = Recharge; }
        /// <summary>Schaden zuerst auf den Schleier; Rückgabe = durchgehender Rest.</summary>
        public float Absorb(float damage)
        {
            if (!Active || damage <= 0f) return damage;
            float taken = Math.Min(ShroudHealth, damage); ShroudHealth -= taken;
            if (ShroudHealth <= 0f) Exit();
            return damage - taken;
        }
        public void Tick(float dt)
        {
            if (Active) { ShroudHealth -= ShroudMax * Drain / Max * dt; if (ShroudHealth <= 0f) { ShroudHealth = 0f; Exit(); } }
            else Cooldown = Math.Max(0f, Cooldown - dt);
        }
    }

    /// <summary>Katalysator – Elementarkern: Energie (max. 30) aus Treffern, eine Sphäre kostet 10 und gibt Verbündeten Segen nach Einstimmung.</summary>
    public class EnergyModel
    {
        public const float Max = 30f, PerHit = 1f, HitInterval = 0.25f, SphereCost = 10f, SphereRecharge = 10f;
        public float Energy { get; private set; }
        public float Cooldown { get; private set; }
        float _next;
        public void OnHit(float time) { if (time < _next) return; Energy = Math.Min(Max, Energy + PerHit); _next = time + HitInterval; }
        public bool CanSphere => Energy >= SphereCost && Cooldown <= 0f;
        public bool Sphere() { if (!CanSphere) return false; Energy -= SphereCost; Cooldown = SphereRecharge; return true; }
        public void Tick(float dt) => Cooldown = Math.Max(0f, Cooldown - dt);
    }

    /// <summary>Klingenwirbel – geworfene Axt kehrt per F1 zurück (Treffer auf dem Rückflug), +3 Initiative; nach 6 s fällt sie von selbst zurück.</summary>
    public class AxeRecallModel
    {
        public const float Window = 6f; public const float Refund = 3f;
        public bool Thrown { get; private set; }
        public float Remaining { get; private set; }
        public void Throw() { Thrown = true; Remaining = Window; }
        public bool Recall() { if (!Thrown) return false; Thrown = false; Remaining = 0f; return true; }
        public void Tick(float dt) { if (!Thrown) return; Remaining -= dt; if (Remaining <= 0f) { Thrown = false; Remaining = 0f; } }
    }
}
