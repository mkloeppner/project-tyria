using UnityEngine;

namespace ProjectTyrant.Flight
{
    public enum FlyerKind { Griffon, Skyscale }
    public enum FlightState { Ground, Glide, Dive, Climb, Hover, WallCling, Stall, Sink, Dash, Descend }
    /// <summary>Ergebnis des letzten Sprint-Versuchs der Himmelsschuppe (für HUD/Hinweise).</summary>
    public enum DashResult { None, Started, NoEndurance, Cooldown }

    /// <summary>Lauf 10: Parameter eines Flug-Reittiers (Tempo relativ zum Laufen, 7 m/s).</summary>
    [System.Serializable]
    public struct FlightParams
    {
        public FlyerKind Kind;
        public float RunSpeed, GroundFactor, GlideFactor, DiveFactor, StallFactor, GlideRatio, FlapImpulse;
        /// <summary>Greif-Sturzflug (nach Lauf 11): senkrecht, Beschleunigung (m/s²), Flügelschläge im Sturz heben die Höchstgeschwindigkeit
        /// je Schlag um <see cref="DiveFlapStep"/> × Lauf (höchstens <see cref="DiveFlapMax"/>, frühestens alle <see cref="DiveFlapInterval"/> s),
        /// Sturz-Energie (zurückgewinnbare Höhe in m) verliert <see cref="EnergyDecay"/> je Sekunde.</summary>
        public float DiveAccel, DiveFlapStep, DiveFlapInterval, EnergyDecay; public int DiveFlapMax;
        /// <summary>Sturzwinkel (80° = steil, nicht ganz senkrecht) und Steigwinkel mit S (50°).</summary>
        public float DiveAngle, ClimbAngle;
        /// <summary>Greif im Energie-Flug ohne S: leichtes Sinken (m/s).</summary>
        public float EnergySink;
        public int Charges; public float ChargeRegen, AirChargeRegen;
        public float Endurance, ClimbCost, HoverCost, ClimbSpeed, GroundRegen, ClingRegen, ClingMax, SinkSpeed, DiveSinkFactor;
        public bool AirRegen; public float AirRegenRate;
        /// <summary>Himmelsschuppe, V: Sprint nach vorn (Tempo = Lauf × Faktor für die Dauer), kostet Flugausdauer, Abklingzeit; danach bleibt ein Teil des Tempos.</summary>
        public float DashFactor, DashTime, DashCost, DashCooldown, DashCarry;
        /// <summary>Himmelsschuppe, C halten: sinken (m/s) und dabei Flugausdauer zurückgewinnen (/s).</summary>
        public float DescendSpeed, DescendRegen;

        public static FlightParams Griffon(int mastery)
        {
            return new FlightParams
            {
                Kind = FlyerKind.Griffon, RunSpeed = 7f, GroundFactor = 1.9f, GlideFactor = 1.6f, DiveFactor = 3f, StallFactor = 0.6f,
                DiveAccel = mastery >= 3 ? 16f : 12f, DiveFlapStep = 1f, DiveFlapMax = 2, DiveFlapInterval = 1f, EnergyDecay = mastery >= 3 ? 0.02f : 0.05f, DiveAngle = 80f, ClimbAngle = 50f, EnergySink = 1f,
                GlideRatio = 6f, FlapImpulse = 6f, Charges = mastery >= 1 ? 5 : 3, ChargeRegen = 0.5f, AirChargeRegen = mastery >= 4 ? 4f : 0f,
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
                DescendSpeed = 6f, DescendRegen = 20f,
            };
        }
    }

    /// <summary>Lauf 10: Eingaben je Schritt.</summary>
    public struct FlightInput
    {
        public bool Jump, JumpHeld, Dive;
        /// <summary>Himmelsschuppe: V gedrückt (Flanke) = Sprint nach vorn.</summary>
        public bool Dash;
        /// <summary>Himmelsschuppe: C gehalten = sinken und Flugausdauer laden.</summary>
        public bool Descend;
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
        /// <summary>Greif: Sturz-Tempo (senkrecht), Flügelschläge in diesem Sturz, Zeit seit dem letzten Schlag/Sturzbeginn.</summary>
        public float DiveSpeed; public int DiveFlaps; float _flapT;
        /// <summary>Greif: Sturz-Energie = Höhe (m), die mit S wieder erklommen werden kann; Tempo beim Loslassen und Energie zu diesem Zeitpunkt.</summary>
        public float Energy; float _releaseSpeed, _releaseEnergy;
        /// <summary>Greif: beim Loslassen gemerkte Höchstmarke (Höhe beim Loslassen + zurückgewinnbare Höhe) – S steigt nie darüber.</summary>
        public float PeakAltitude;
        /// <summary>Gesamttempo (Sturz senkrecht, sonst vorwärts) – HUD, Luftwechsel.</summary>
        public float Speed => State == FlightState.Dive ? DiveSpeed : Energy > 0f ? EnergySpeed : Forward;
        /// <summary>Greif: Gesamttempo im Energie-Flug (waagrecht oder steigend).</summary>
        public float EnergySpeed;

        /// <summary>
        /// Anteil der gestürzten Höhe, den S zurückbringt – hängt am erreichten Sturz-Tempo: 3 × Lauf = 90 % (knapp unter der Ausgangshöhe),
        /// 4 × = 100 % (genau zurück), 5 × = 110 % (etwas darüber); langsamere Stürze entsprechend weniger (mind. 60 %).
        /// </summary>
        public static float Recovery(float speedTimesRun) => Mathf.Clamp(0.9f + 0.1f * (speedTimesRun - 3f), 0.6f, 1.1f);
        public float DiveCap => Run * (P.DiveFactor + P.DiveFlapStep * DiveFlaps);

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

        void Land() { State = FlightState.Ground; Vertical = 0f; Forward = Mathf.Min(Speed, Run * P.GroundFactor); DiveSpeed = 0f; DiveFlaps = 0; Energy = 0f; }

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
            float glide = Run * P.GlideFactor;
            if (i.Dive)
            {
                // V halten: senkrechter Sturz, Tempo wächst bis zur Höchstgeschwindigkeit; Leertaste (frühestens alle 1 s) = Flügelschlag → +1 × Lauf Höchsttempo (4× / 5×)
                if (State != FlightState.Dive) { State = FlightState.Dive; DiveSpeed = Mathf.Max(Speed, Run); DiveFlaps = 0; _flapT = 0f; }
                _flapT += dt;
                if (i.Jump && DiveFlaps < P.DiveFlapMax && _flapT >= P.DiveFlapInterval && Charges > 0) { Charges--; DiveFlaps++; _flapT = 0f; DiveSpeed += Run * 0.5f; }   // Flügelschlag kostet eine Ladung (im Spiel: 1 Ausweich-Balken)
                DiveSpeed = Mathf.Min(DiveCap, DiveSpeed + P.DiveAccel * dt);
                float a = P.DiveAngle * Mathf.Deg2Rad;
                Forward = DiveSpeed * Mathf.Cos(a); Vertical = -DiveSpeed * Mathf.Sin(a);   // steil (80°), nicht ganz senkrecht
                Energy += -Vertical * dt;   // jeder gestürzte Meter lässt sich später mit S zurückholen
                return;
            }
            if (State == FlightState.Dive)
            {
                // Loslassen: kein automatisches Hochziehen – der Greif fängt waagrecht ab und behält das Sturz-Tempo als Energie
                State = FlightState.Glide; Forward = DiveSpeed; Vertical = 0f;
                Energy *= Recovery(DiveSpeed / Run);   // 3× knapp darunter, 4× genau, 5× etwas darüber
                _releaseSpeed = DiveSpeed; _releaseEnergy = Mathf.Max(0.01f, Energy); DiveSpeed = 0f; EnergySpeed = _releaseSpeed;
                PeakAltitude = Altitude + Energy;   // gemerkt: so hoch kommt S höchstens – sofort ziehen bringt alles, warten kostet Höhe und Tempo
            }
            if (i.Jump && Charges > 0) { Charges--; Vertical = Mathf.Max(Vertical, 0f) + P.FlapImpulse; }
            if (Energy > 0.05f)
            {
                // Energie-Flug: Tempo hängt an der verbleibenden Energie; S (Pitch < 0) wandelt sie in Höhe (bis zur Ausgangshöhe des Sturzes)
                float v = glide + Mathf.Max(0f, _releaseSpeed - glide) * Mathf.Clamp01(Energy / _releaseEnergy);
                if (i.Pitch < -0.1f)
                {
                    // S: steiler Aufschwung (50°), das Tempo verteilt sich auf vorwärts und aufwärts
                    float climb = v * Mathf.Sin(P.ClimbAngle * Mathf.Deg2Rad) * Mathf.Clamp01(-i.Pitch);
                    Vertical = Mathf.MoveTowards(Vertical, climb, 30f * dt);
                    Energy = Mathf.Max(0f, Energy - Mathf.Max(0f, Vertical) * dt);
                    if (Altitude + Vertical * dt >= PeakAltitude) { Vertical = Mathf.Max(0f, (PeakAltitude - Altitude) / dt); Energy = 0f; }   // Höchstmarke erreicht
                }
                else
                {
                    Vertical = Mathf.MoveTowards(Vertical, -P.EnergySink, Gravity * dt);   // Tempo halten, dabei leicht sinken
                    Energy = Mathf.Max(0f, Energy - Energy * P.EnergyDecay * dt);
                }
                v = glide + Mathf.Max(0f, _releaseSpeed - glide) * Mathf.Clamp01(Energy / _releaseEnergy);
                EnergySpeed = v;
                Forward = Mathf.Sqrt(Mathf.Max(v * v - Mathf.Max(0f, Vertical) * Mathf.Max(0f, Vertical), 1f));
                if (Energy <= 0.05f) { Energy = 0f; Forward = Mathf.Max(Forward, glide); }
                return;
            }
            // Gleiten: Pitch ändert Tempo; Sinkrate = Tempo / Gleitverhältnis
            float target = glide * (1f + 0.25f * Mathf.Clamp(i.Pitch, -1f, 1f));   // +1 = Nase runter (schneller), −1 = Nase hoch (langsamer)
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
            if (i.Descend)
            {
                // Sinken: kontrolliert abwärts, lädt Flugausdauer; W hält etwas Vorwärtsfahrt, sonst läuft der Schwung aus
                State = FlightState.Descend; Vertical = -P.DescendSpeed;
                Forward = Mathf.MoveTowards(Forward, i.Pitch > 0.1f ? Run : 0f, 6f * dt);
                Endurance = Mathf.Min(P.Endurance, Endurance + P.DescendRegen * dt);
                return;
            }
            if (Endurance <= 0f) { State = FlightState.Sink; Vertical = -P.SinkSpeed; Forward = Mathf.MoveTowards(Forward, Run * 0.5f, 3f * dt); return; }
            if (i.JumpHeld) { State = FlightState.Climb; Vertical = P.ClimbSpeed; Endurance = Mathf.Max(0f, Endurance - P.ClimbCost * dt); }
            else if (i.Pitch > 0.1f) { State = FlightState.Glide; Forward = Mathf.MoveTowards(Forward, Run * P.GlideFactor, 6f * dt); Vertical = -Forward / P.GlideRatio; }
            else { State = FlightState.Hover; Vertical = 0f; Forward = Mathf.MoveTowards(Forward, 0f, 6f * dt); Endurance = Mathf.Max(0f, Endurance - P.HoverCost * dt); }
        }

        /// <summary>Sturzangriff des Greifs: Schaden proportional zum Tempo (1 = Höchsttempo).</summary>
        public float EngageFactor => P.Kind == FlyerKind.Griffon && State == FlightState.Dive ? Mathf.Clamp01(DiveSpeed / (Run * (P.DiveFactor + P.DiveFlapStep * P.DiveFlapMax))) : 0f;   // 1 = 5 × Lauf
    }
}
