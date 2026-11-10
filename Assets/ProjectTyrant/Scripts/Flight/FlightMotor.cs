using UnityEngine;

namespace ProjectTyrant.Flight
{
    public enum FlyerKind { Griffon, Skyscale }
    public enum FlightState { Ground, Glide, Dive, Climb, Hover, WallCling, Stall, Sink, Dash }
    /// <summary>Ergebnis des letzten Sprint-Versuchs der Himmelsschuppe (für HUD/Hinweise).</summary>
    public enum DashResult { None, Started, NoEndurance, Cooldown }

    /// <summary>Lauf 10: Parameter eines Flug-Reittiers (Tempo relativ zum Laufen, 7 m/s).</summary>
    [System.Serializable]
    public struct FlightParams
    {
        public FlyerKind Kind;
        public float RunSpeed, GroundFactor, GlideFactor, DiveFactor, StallFactor, GlideRatio, PullUpEfficiency, FlapImpulse;
        public int Charges; public float ChargeRegen, AirChargeRegen;
        public float Endurance, ClimbCost, HoverCost, ClimbSpeed, GroundRegen, ClingRegen, ClingMax, SinkSpeed, DiveSinkFactor;
        public bool AirRegen; public float AirRegenRate;
        /// <summary>Himmelsschuppe, V: Sprint nach vorn (Tempo = Lauf × Faktor für die Dauer), kostet Flugausdauer, Abklingzeit; danach bleibt ein Teil des Tempos.</summary>
        public float DashFactor, DashTime, DashCost, DashCooldown, DashCarry;

        public static FlightParams Griffon(int mastery)
        {
            return new FlightParams
            {
                Kind = FlyerKind.Griffon, RunSpeed = 7f, GroundFactor = 1.9f, GlideFactor = 1.6f, DiveFactor = mastery >= 3 ? 4.6f : 4f, StallFactor = 0.6f,
                GlideRatio = 6f, PullUpEfficiency = mastery >= 3 ? 0.85f : 0.6f, FlapImpulse = 6f, Charges = mastery >= 1 ? 5 : 3, ChargeRegen = 0.5f, AirChargeRegen = mastery >= 4 ? 4f : 0f,
            };
        }

        public static FlightParams Skyscale(int mastery)
        {
            return new FlightParams
            {
                Kind = FlyerKind.Skyscale, RunSpeed = 7f, GroundFactor = 1.3f, GlideFactor = 1.4f, DiveFactor = 3f, StallFactor = 0f, GlideRatio = 8f,
                Endurance = mastery >= 2 ? 150f : 100f, ClimbCost = 20f, HoverCost = mastery >= 2 ? 3f : 5f, ClimbSpeed = 5f, GroundRegen = 25f, ClingRegen = 15f, ClingMax = 20f,
                SinkSpeed = 1.5f, DiveSinkFactor = 3f, AirRegen = mastery >= 4, AirRegenRate = 5f,
                DashFactor = 4.5f, DashTime = 0.45f, DashCost = 12f, DashCooldown = 0.9f, DashCarry = 0.45f,
            };
        }
    }

    /// <summary>Lauf 10: Eingaben je Schritt.</summary>
    public struct FlightInput
    {
        public bool Jump, JumpHeld, Dive;
        /// <summary>Himmelsschuppe: V gedrückt (Flanke) = Sprint nach vorn.</summary>
        public bool Dash;
        /// <summary>−1 (Nase runter/Bremsen) … +1 (Nase hoch/Vorwärts).</summary>
        public float Pitch, Turn;
        public bool NearWall; public float WallSlope;
        public bool InUpdraft;
    }

    /// <summary>
    /// Lauf 10: ein deterministisches kinematisches Flugmodell für Greif und Himmelsschuppe (kein Rigidbody). Zustand: Höhe über Boden,
    /// Vorwärts- und Vertikaltempo, Auftrieb-Ladungen bzw. Flugausdauer. Höhendecke mit weichem Druck nach unten, Flugverbot = Abwurf.
    /// </summary>
    public class FlightMotor
    {
        public FlightParams P;
        public FlightState State = FlightState.Ground;
        public float Altitude, Forward, Vertical, Endurance, ClingTime;
        /// <summary>Tatsächliches Bodentempo vor dem Abheben (vom Controller je Frame gesetzt) – Abheben und Kanten übernehmen den Schwung.</summary>
        public float GroundSpeed;
        public float DashTime, DashCooldown;
        public DashResult LastDash;
        public int Charges;
        float _chargeT;
        public float Ceiling = 150f;
        public bool Banned;
        public const float Gravity = 9.81f, CeilingPush = 6f;
        float _diveStartAlt;

        public FlightMotor(FlightParams p) { P = p; Charges = p.Charges; Endurance = p.Endurance; }

        public bool Airborne => State != FlightState.Ground;
        public float Run => P.RunSpeed;

        /// <summary>Abheben: Greif braucht Sprung (Flügelschlag), Himmelsschuppe startet senkrecht.</summary>
        public void TakeOff()
        {
            if (Banned) return;
            if (P.Kind == FlyerKind.Griffon) { Vertical = P.FlapImpulse; Forward = Mathf.Max(GroundSpeed, Run * P.GlideFactor); State = FlightState.Glide; }
            else { State = FlightState.Climb; Vertical = P.ClimbSpeed; Forward = GroundSpeed; }   // Schwung vom Boden bleibt erhalten
        }

        public void Step(float dt, FlightInput i)
        {
            if (Banned && Airborne) { State = FlightState.Sink; Vertical = -Mathf.Max(P.SinkSpeed, 4f); Forward = Mathf.MoveTowards(Forward, Run * 0.6f, 3f * dt); }   // Flugverbot: sanft absinken
            else if (P.Kind == FlyerKind.Griffon) StepGriffon(dt, i);
            else StepSkyscale(dt, i);
            // Höhendecke: sanfter Druck nach unten
            if (Altitude > Ceiling) Vertical = Mathf.Min(Vertical, -CeilingPush * Mathf.Clamp01((Altitude - Ceiling) / 10f + 0.2f));
            Altitude += Vertical * dt;
            if (Altitude <= 0f) { Altitude = 0f; if (Airborne) Land(); }
        }

        /// <summary>Bodenkontakt aus der Spielwelt (CharacterController meldet „geerdet“).</summary>
        public void Touchdown() { if (Airborne) Land(); }

        /// <summary>Von einer Kante gelaufen: Greif gleitet, Himmelsschuppe schwebt.</summary>
        public void LeaveGround() { if (Airborne || Banned) return; if (P.Kind == FlyerKind.Griffon) { State = FlightState.Glide; Forward = Mathf.Max(Mathf.Max(Forward, GroundSpeed), Run * P.GlideFactor); } else { State = FlightState.Hover; Forward = Mathf.Max(Forward, GroundSpeed); } Vertical = 0f; }

        void Land() { State = FlightState.Ground; Vertical = 0f; Forward = Mathf.Min(Forward, Run * P.GroundFactor); }

        void StepGriffon(float dt, FlightInput i)
        {
            if (State == FlightState.Ground)
            {
                _chargeT += dt;
                while (_chargeT >= P.ChargeRegen && Charges < P.Charges) { _chargeT -= P.ChargeRegen; Charges++; }
                if (i.Jump) { if (Charges > 0) Charges--; TakeOff(); }
                return;
            }
            if (i.InUpdraft) { Charges = P.Charges; Vertical = Mathf.Max(Vertical, 8f); }
            if (P.AirChargeRegen > 0f && State == FlightState.Glide && Charges < P.Charges) { _chargeT += dt; if (_chargeT >= P.AirChargeRegen) { _chargeT = 0f; Charges++; } }   // Luftakrobatik
            if (i.Jump && Charges > 0 && State != FlightState.Dive && !i.Dive) { Charges--; Vertical = Mathf.Max(Vertical, 0f) + P.FlapImpulse; }
            if (i.Dive)
            {
                if (State != FlightState.Dive) { State = FlightState.Dive; _diveStartAlt = Altitude; }
                Forward = Mathf.MoveTowards(Forward, Run * P.DiveFactor, 9f * dt);
                Vertical = -Mathf.Max(Forward * 0.5f, 6f);
                return;
            }
            if (State == FlightState.Dive)
            {
                // Aufschwung: ein Anteil der im Sturz verlorenen Höhe kommt zurück (60 % / Sturzflug-Meister 85 %), ballistisch abgebremst;
                // das Sturz-Tempo bleibt teilweise erhalten und klingt im Gleitflug ab.
                float lost = Mathf.Max(0f, _diveStartAlt - Altitude);
                float gain = lost * P.PullUpEfficiency;
                Vertical = Mathf.Sqrt(2f * Gravity * gain);
                Forward = Mathf.Max(Run * P.GlideFactor, Forward * 0.7f);
                State = FlightState.Glide;
                return;
            }
            // Gleiten: Pitch ändert Tempo; Sinkrate = Tempo / Gleitverhältnis
            float target = Run * P.GlideFactor * (1f + 0.25f * Mathf.Clamp(i.Pitch, -1f, 1f));   // +1 = Nase runter (schneller), −1 = Nase hoch (langsamer)
            Forward = Mathf.MoveTowards(Forward, target, 2f * dt);
            if (Forward < Run * P.StallFactor) { State = FlightState.Stall; Vertical = Mathf.MoveTowards(Vertical, -8f, Gravity * dt); Forward += 3f * dt; return; }
            if (State == FlightState.Stall && Forward >= Run * P.StallFactor * 1.2f) State = FlightState.Glide;
            float sink = -Forward / P.GlideRatio;
            Vertical = Mathf.MoveTowards(Vertical, sink, Gravity * dt);
        }

        void StepSkyscale(float dt, FlightInput i)
        {
            if (State == FlightState.Ground)
            {
                Endurance = Mathf.Min(P.Endurance, Endurance + P.GroundRegen * dt);
                if (i.Jump) TakeOff();
                return;
            }
            if (i.InUpdraft) Endurance = P.Endurance;
            if (P.AirRegen) Endurance = Mathf.Min(P.Endurance, Endurance + P.AirRegenRate * dt);
            // Wandhaften (Beherrschung 1 im Controller geprüft): an ≥ 60° bei gedrückter Leertaste
            if (State == FlightState.WallCling)
            {
                ClingTime += dt; Vertical = 0f; Forward = 0f;
                Endurance = Mathf.Min(P.Endurance, Endurance + P.ClingRegen * dt);
                if (i.Jump || ClingTime >= P.ClingMax || !i.NearWall) { State = FlightState.Hover; ClingTime = 0f; Vertical = i.Jump ? P.ClimbSpeed : 0f; }
                return;
            }
            if (i.JumpHeld && i.NearWall && i.WallSlope >= 60f && State != FlightState.Dive) { State = FlightState.WallCling; ClingTime = 0f; return; }
            // V: Sprint nach vorn statt Sturz (Wunsch nach Lauf 11) – schnell, kostet etwas Flugausdauer
            LastDash = DashResult.None;
            if (DashCooldown > 0f) DashCooldown -= dt;
            if (i.Dash)
            {
                if (DashTime > 0f || DashCooldown > 0f) LastDash = DashResult.Cooldown;
                else if (Endurance < P.DashCost) LastDash = DashResult.NoEndurance;
                else { Endurance -= P.DashCost; DashTime = P.DashTime; DashCooldown = P.DashCooldown; LastDash = DashResult.Started; }
            }
            if (DashTime > 0f)
            {
                DashTime -= dt; State = FlightState.Dash; Forward = Run * P.DashFactor; Vertical = 0f;
                if (DashTime <= 0f) { DashTime = 0f; Forward = Run * P.DashFactor * P.DashCarry; State = FlightState.Hover; }   // Schwung klingt danach normal ab
                return;
            }
            if (Endurance <= 0f) { State = FlightState.Sink; Vertical = -P.SinkSpeed; Forward = Mathf.MoveTowards(Forward, Run * 0.5f, 3f * dt); return; }
            if (i.JumpHeld) { State = FlightState.Climb; Vertical = P.ClimbSpeed; Endurance = Mathf.Max(0f, Endurance - P.ClimbCost * dt); }
            else if (i.Pitch > 0.1f) { State = FlightState.Glide; Forward = Mathf.MoveTowards(Forward, Run * P.GlideFactor, 6f * dt); Vertical = -Forward / P.GlideRatio; }
            else { State = FlightState.Hover; Vertical = 0f; Forward = Mathf.MoveTowards(Forward, 0f, 6f * dt); Endurance = Mathf.Max(0f, Endurance - P.HoverCost * dt); }
        }

        /// <summary>Sturzangriff des Greifs: Schaden proportional zum Tempo (1 = Höchsttempo).</summary>
        public float EngageFactor => P.Kind == FlyerKind.Griffon && State == FlightState.Dive ? Mathf.Clamp01(Forward / (Run * P.DiveFactor)) : 0f;
    }
}
