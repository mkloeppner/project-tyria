using UnityEngine;

namespace ProjectTyrant.Balance
{
    public enum PursuitState { Idle, Hesitate, Chase, Return }
    public enum GiveUpReason { None, Leash, Duration, LineOfSight, Mercy, Mounted, Travel, Safezone }

    /// <summary>Balance B1: Lage, die das Gehirn je Tick an das Verfolgungsmodell meldet (alle Distanzen in Metern).</summary>
    public struct PursuitInput
    {
        public float Now;
        public float DistanceFromSpawn, DistanceToTarget;
        public bool HasLineOfSight;
        /// <summary>Letzter Zeitpunkt, an dem das Ziel DIESEM Gegner Schaden zugefügt hat.</summary>
        public float TargetHitMeAt;
        /// <summary>Letzter Zeitpunkt, an dem das Ziel überhaupt Schaden verursacht hat.</summary>
        public float TargetDealtDamageAt;
        public bool TargetMounted, TargetTravelled, TargetInSafezone;
    }

    /// <summary>Balance B1: Verfolgungsprofil (Werte in Metern/Sekunden), aus <see cref="DangerSettings"/> plus Archetyp-Überschreibungen.</summary>
    public struct PursuitProfile
    {
        public int Danger;
        public float AggroRange, Leash, SpeedFactor, MaxPursuit, LosForget, Hesitation, MercyDistance, MountGiveUp;
        /// <summary>Jäger: verfolgen in Stufe 4–5 auch Berittene, können Reittiere abwerfen.</summary>
        public bool Hunter;
        /// <summary>Tyrannenfeste: innerhalb der Mauern keine Leine.</summary>
        public bool NoLeash;
        public bool Neutral;

        public const float MercyQuiet = 5f;

        public static PursuitProfile From(DangerSettings s, bool hunter = false, bool neutral = false)
        {
            var p = new PursuitProfile
            {
                Danger = s.Level, AggroRange = Units.M(s.AggroRange), Leash = Units.M(s.Leash), SpeedFactor = s.PursuitSpeed, MaxPursuit = s.MaxPursuit,
                LosForget = s.LosForget, Hesitation = s.Hesitation, MercyDistance = Units.M(s.MercyDistance), MountGiveUp = Units.M(s.MountGiveUp), Hunter = hunter, Neutral = neutral
            };
            if (hunter) { p.Leash *= 1.5f; p.MaxPursuit *= 1.5f; p.SpeedFactor *= 1.1f; }
            if (neutral) { p.MaxPursuit *= 0.6f; p.Leash *= 0.7f; }   // neutrale Kreaturen geben schnell auf
            return p;
        }
    }

    /// <summary>
    /// Balance B1: reines Verfolgungsmodell (Idle → Zögern → Verfolgen → Rückkehr) mit den sechs Aufgabe-Regeln aus „Zielbild B“.
    /// Das NpcBrain liest nur den Zustand und die Zielgeschwindigkeit; alle Regeln laufen mit Zeitstempeln (testbar ohne Szene).
    /// </summary>
    public class PursuitModel
    {
        public PursuitProfile Profile;
        public PursuitState State { get; private set; } = PursuitState.Idle;
        public GiveUpReason LastReason { get; private set; }
        public float ChaseStart { get; private set; }
        float _hesitateUntil, _losLostAt = -1f, _lastTick;

        public PursuitModel(PursuitProfile p) { Profile = p; }

        /// <summary>Gegner bemerkt das Ziel (Aggro-Radius oder Angriff). Angriffe überspringen das Zögern.</summary>
        public void Engage(float now, bool attacked)
        {
            if (State == PursuitState.Chase || State == PursuitState.Hesitate) { if (attacked && State == PursuitState.Hesitate) Begin(now); return; }
            if (State == PursuitState.Return) return;   // GW2: zurückkehrende Gegner sind unverwundbar und ignorieren alles
            LastReason = GiveUpReason.None;
            if (!attacked && Profile.Hesitation > 0f) { State = PursuitState.Hesitate; _hesitateUntil = now + Profile.Hesitation; }
            else Begin(now);
        }

        void Begin(float now) { State = PursuitState.Chase; ChaseStart = now; _losLostAt = -1f; }

        /// <summary>Darf der Gegner das Ziel überhaupt wahrnehmen (Aggro-Radius, neutral)?</summary>
        public bool Notices(float distance, bool provoked) => provoked || (!Profile.Neutral && distance <= Profile.AggroRange);

        /// <summary>Ein Tick; liefert den Grund, falls der Gegner aufgibt (Zustand wechselt dann auf Rückkehr).</summary>
        public GiveUpReason Tick(in PursuitInput i)
        {
            _lastTick = i.Now;
            if (State == PursuitState.Hesitate)
            {
                if (i.TargetTravelled || i.TargetInSafezone) return GiveUp(GiveUpReason.Travel);
                if (i.Now >= _hesitateUntil) Begin(i.Now);
                return GiveUpReason.None;
            }
            if (State != PursuitState.Chase) return GiveUpReason.None;
            // Schaden des Spielers am Gegner setzt die Verfolgungsdauer zurück (Regel 2)
            if (i.TargetHitMeAt > ChaseStart) ChaseStart = i.TargetHitMeAt;
            // 6: Wegmarke, Portal, Stadt, Sicherheitszone
            if (i.TargetTravelled) return GiveUp(GiveUpReason.Travel);
            if (i.TargetInSafezone) return GiveUp(GiveUpReason.Safezone);
            // 1: Leine
            if (!Profile.NoLeash && i.DistanceFromSpawn > Profile.Leash) return GiveUp(GiveUpReason.Leash);
            // 2: Dauer
            if (!Profile.NoLeash && i.Now - ChaseStart > Profile.MaxPursuit) return GiveUp(GiveUpReason.Duration);
            // 3: Sichtlinie
            if (!i.HasLineOfSight) { if (_losLostAt < 0f) _losLostAt = i.Now; else if (i.Now - _losLostAt > Profile.LosForget) return GiveUp(GiveUpReason.LineOfSight); }
            else _losLostAt = -1f;
            // 4: Fluchtgnade (Stufe 0–1)
            if (Profile.MercyDistance > 0f && i.Now - i.TargetDealtDamageAt >= PursuitProfile.MercyQuiet && i.DistanceToTarget > Profile.MercyDistance) return GiveUp(GiveUpReason.Mercy);
            // 5: Reittier
            if (i.TargetMounted)
            {
                bool keeps = Profile.MountGiveUp <= 0f && Profile.Hunter;
                float d = Profile.MountGiveUp > 0f ? Profile.MountGiveUp : Units.M(400);
                if (!keeps && i.DistanceToTarget > d) return GiveUp(GiveUpReason.Mounted);
            }
            return GiveUpReason.None;
        }

        GiveUpReason GiveUp(GiveUpReason r) { State = PursuitState.Return; LastReason = r; return r; }

        /// <summary>Zurück am Spawn: vollständig geheilt, wieder bereit.</summary>
        public void Arrived() { State = PursuitState.Idle; _losLostAt = -1f; }

        /// <summary>Ziel tot/verloren ohne Regel (z. B. Tarnung) → zurückkehren.</summary>
        public void Drop() { if (State != PursuitState.Idle) { State = PursuitState.Return; LastReason = GiveUpReason.None; } }

        /// <summary>Verfolgungstempo (m/s) relativ zum Spieler; Eile-Phasen ab Stufe 4 (alle 8 s für 2 s +25 %).</summary>
        public float ChaseSpeed(float playerSpeed, float now, bool hastePhases)
        {
            float f = Profile.SpeedFactor;
            if (hastePhases && State == PursuitState.Chase && ((now - ChaseStart) % 8f) > 6f) f *= 1.25f;
            return playerSpeed * f;
        }

        public bool IsChasing => State == PursuitState.Chase;
        public bool IsReturning => State == PursuitState.Return;
    }

    /// <summary>Balance B1: Rudel-Aggro – wer wird mitgezogen?</summary>
    public static class PackRules
    {
        /// <summary>Radius (m), in dem Gruppenmitglieder mitziehen; 0 = niemand (Stufe 0), Partner = nächstes Mitglied ≤ 6 m.</summary>
        public static float Radius(DangerSettings s)
        {
            switch (s.Pack)
            {
                case PackMode.None: return 0f;
                case PackMode.Partner: return 6f;
                default: return Units.M(s.PackRadius);
            }
        }

        /// <summary>Darf das Mitglied <paramref name="memberIndex"/> (nach Entfernung sortiert, 0 = nächstes) mitgezogen werden?</summary>
        public static bool Joins(DangerSettings s, bool sameGroup, float distance, int memberIndex)
        {
            if (!sameGroup || s.Pack == PackMode.None) return false;
            if (s.Pack == PackMode.Partner) return memberIndex == 0 && distance <= Radius(s);
            return distance <= Radius(s) && memberIndex < s.MaxGroupSize - 1;
        }
    }

    /// <summary>Balance B1: Fessel-Regeln (Kontroll-Effekte durch Gegner): Quote, erlaubte Arten, Abklingzeit 6 s je Gegner.</summary>
    public class ControlGate
    {
        float _last = -999f;
        readonly System.Random _rng;
        public ControlGate(int seed) { _rng = new System.Random(seed); }

        public bool TryUse(DangerSettings s, ControlKinds kind, float now)
        {
            if (s == null || kind == ControlKinds.None) return true;   // kein Kontroll-Effekt → immer erlaubt
            if (!DangerTable.Allows(s, kind)) return false;
            if (now - _last < DangerTable.ControlIcd) return false;
            if (_rng.NextDouble() >= s.ControlChance) return false;
            _last = now;
            return true;
        }
    }

    /// <summary>Balance B1: Separation – Gegner halten Abstand untereinander; zum Spieler nie näher als 0,8 m, und die Separation schiebt nie zum Spieler hin.</summary>
    public static class Separation
    {
        public static Vector3 Push(Vector3 self, Vector3 other, bool otherIsEnemy, float radius)
        {
            if (!otherIsEnemy) return Vector3.zero;
            Vector3 d = self - other; d.y = 0f;
            float m = d.magnitude;
            if (m < 0.01f || m > radius) return Vector3.zero;
            return d / (m * m);
        }

        /// <summary>Begrenzt einen Schritt so, dass der Mindestabstand zum Spieler gewahrt bleibt.</summary>
        public static Vector3 ClampToGap(Vector3 from, Vector3 to, Vector3 player)
        {
            Vector3 d = to - player; d.y = 0f;
            if (d.magnitude >= DangerTable.MinPlayerGap) return to;
            Vector3 f = from - player; f.y = 0f;
            if (f.magnitude < DangerTable.MinPlayerGap) return from;   // schon zu nah: nicht weiter hinein
            return player + d.normalized * DangerTable.MinPlayerGap + Vector3.up * (to.y - player.y);
        }
    }
}
