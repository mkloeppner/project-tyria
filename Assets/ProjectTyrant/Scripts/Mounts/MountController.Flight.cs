using UnityEngine;
using ProjectTyrant.Flight;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 10: Flug für Greif und Himmelsschuppe. Der reine <see cref="FlightMotor"/> rechnet Tempo und Steigrate,
    /// der <see cref="PlayerController"/> bewegt (keine Rigidbody-Physik). Flugregeln je Ort über <see cref="FlightRules"/>.
    /// Greif: Leertaste = Abheben/Flügelschlag (Auftrieb-Ladung), V halten = senkrechter Sturzflug (Leertaste im Sturz, je 1 s: 4× / 5× Tempo),
    /// loslassen = waagrecht abfangen und Tempo halten, S = Sturz-Energie in Höhe wandeln (bis zur Ausgangshöhe); W/S sonst Nase runter/hoch, Kamera lenkt.
    /// Himmelsschuppe: Leertaste halten = steigen, loslassen = schweben, W = Vorwärtsflug, V = Sprint nach vorn (kostet Flugausdauer); an steilen Wänden haften.
    /// Abheben, Kanten und Aufsitzen übernehmen das aktuelle Bodentempo (kein Neustart bei 0).
    /// </summary>
    public partial class MountController
    {
        public const float GriffonTurnRate = GriffonSteer.TurnRate, SkyscaleTurnRate = 3.2f;   // Greif: siehe GriffonSteer (vorher 1,8 rad/s → Tempo trieb in die alte Richtung)
        float _dashHintAt;
        Vector3 _seat; float _tilt;
        Vector3 _skyVel, _dashDir = Vector3.forward;
        public const float MaxDiveTilt = 82f, MaxClimbTilt = 55f;
        /// <summary>C gehört gerade dem Sinken (fliegende Himmelsschuppe) – die Action-Kamera schaltet dann nicht um.</summary>
        public bool UsesDescendKey => Flight != null && Flight.Airborne && Flight.P.Kind == FlyerKind.Skyscale;
        static Vector3 Flat3(Vector3 v) { v.y = 0f; return v; }

        static FlightParams ParamsFor(MountKind k)
        {
            var mm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
            return k == MountKind.Griffon ? FlightParams.Griffon(MasteryEffects.GriffonRank(mm)) : FlightParams.Skyscale(MasteryEffects.SkyscaleRank(mm));
        }

        void BeginFlight(MountKind k)
        {
            Flight = FlightRules.IsFlyer(k) ? new FlightMotor(ParamsFor(k)) : null;
            _heading = Flat(transform.forward); _ruleT = 0f; _airT = 0f; _wasBanned = false;
            if (Flight != null && _pc != null) { Flight.GroundSpeed = Flat3(_pc.HorizontalVelocity).magnitude; _skyVel = Flat3(_pc.HorizontalVelocity); }   // Schwung beim Aufsitzen bleibt
        }

        float _pullBanT;
        /// <summary>Lauf 11: Gegner der Gefahrenstufe 6 holen Flieger vom Himmel – <paramref name="seconds"/> s Flugverbot (sanftes Absinken).</summary>
        public void PullFromSky(float seconds) { _pullBanT = Mathf.Max(_pullBanT, seconds); }

        /// <summary>Beherrschung geändert (Admin/Rang erlernt): Flugwerte neu setzen, Zustand bleibt.</summary>
        public void RefreshFlightParams()
        {
            if (Flight == null) return;
            Flight.P = ParamsFor(Motor.Mounted);
            Flight.Charges = Mathf.Min(Flight.Charges, Flight.P.Charges); Flight.Endurance = Mathf.Min(Flight.Endurance, Flight.P.Endurance);
        }

        void EndFlight()
        {
            Flight = null;
            if (_pc == null) return;
            _pc.FlightVelocity = null; _pc.GravityScale = 1f;
        }

        /// <summary>Flugregeln an der aktuellen Position (alle 0,25 s).</summary>
        public FlightVerdict EvaluateFlightHere()
        {
            var gm = GameManager.Instance;
            var story = gm != null ? gm.Story : null;
            var dlg = gm != null ? gm.Dialogue : null;
            var c = new FlightContext
            {
                Zone = gm != null && gm.CurrentZone != null ? gm.CurrentZone.Id : ProfileState.StartZoneId,
                InCity = Balance.BalanceWorld.InHub(transform.position),
                InStoryInstance = story != null && story.InInstance,
                InCutscene = ThirdPersonCameraRig.CinematicActive || (dlg != null && dlg.IsInvulnerable),
                StoryFinished = story != null && story.State != null && (story.State.Finished || story.State.Chapter > 10),   // Lauf 11: Tyrannen-Geschichte = Kapitel 1–10
                UnderRoof = UnderRoof(),
                NearBossFight = NearBossFight(),
            };
            return FlightRules.Evaluate(c, FlightDebug.RulesOff, FlightDebug.CeilingOff);
        }

        bool UnderRoof()
        {
            var hits = Physics.RaycastAll(transform.position + Vector3.up * 2.2f, Vector3.up, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var h in hits) if (!h.transform.IsChildOf(transform)) return true;
            return false;
        }

        bool NearBossFight()
        {
            float r2 = FlightRules.BossRadius * FlightRules.BossRadius;
            foreach (var c in Combatant.All)
            {
                if (c == null || c.IsDead || c.Tier != EnemyTier.Champion || !_self.IsHostileTo(c)) continue;
                if ((c.transform.position - transform.position).sqrMagnitude > r2) continue;
                var b = c.GetComponent<NpcBrain>();
                if (b != null && b.Target != null) return true;
            }
            return false;
        }

        float GroundBelow()
        {
            var p = transform.position;
            float best = float.NegativeInfinity;
            var hits = Physics.RaycastAll(p + Vector3.up * 0.5f, Vector3.down, 1200f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var h in hits) if (!h.transform.IsChildOf(transform) && h.point.y > best) best = h.point.y;
            if (float.IsNegativeInfinity(best) && ZoneRoot.Current != null) best = ZoneRoot.GroundHeight(p.x, p.z);
            return float.IsNegativeInfinity(best) ? p.y : best;
        }

        bool WallAhead(out float slope)
        {
            slope = 0f;
            var hits = Physics.RaycastAll(transform.position + Vector3.up * 1.2f, _heading, 2.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool any = false;
            foreach (var h in hits)
            {
                if (h.transform.IsChildOf(transform) || h.distance >= best) continue;
                best = h.distance; any = true; slope = Vector3.Angle(h.normal, Vector3.up);
            }
            return any;
        }

        void TickFlight(InputManager input, float dt, bool grounded)
        {
            var f = Flight;
            if (f == null) return;
            var mm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
            _ruleT -= dt;
            if (_ruleT <= 0f) { _ruleT = 0.25f; FlightRule = EvaluateFlightHere(); }
            if (_pullBanT > 0f) _pullBanT -= dt;
            f.Banned = !FlightRule.Allowed || _pullBanT > 0f;
            f.Ceiling = FlightRule.Ceiling;
            if (f.Banned && !_wasBanned && (f.Airborne || input.JumpPressed)) GameEventBus.Notify(FlightRule.Reason, new Color(1f, 0.7f, 0.4f));
            _wasBanned = f.Banned;
            if (FlightDebug.Infinite) { f.Endurance = f.P.Endurance; f.Charges = f.P.Charges; }

            // Welt → Modell
            f.Altitude = Mathf.Max(0f, transform.position.y - GroundBelow());
            if (f.Airborne) _airT += dt; else _airT = 0f;
            if (grounded && f.Airborne && _airT > 0.25f && _pc.VerticalVelocity <= 0.1f) f.Touchdown();
            else if (!grounded && !f.Airborne && !f.Banned && f.Altitude > 2.5f) f.LeaveGround();

            // Eingabe
            var cam = Camera.main != null ? Flat(Camera.main.transform.forward) : _heading;
            float turn = input.Move.x;
            var target = Quaternion.Euler(0f, turn * 70f, 0f) * cam;
            bool nearWall = false; float slope = 0f;
            if (f.P.Kind == FlyerKind.Skyscale && f.Airborne && (mm == null || MasteryEffects.SkyscaleWallCling(mm))) nearWall = WallAhead(out slope);
            if (_updraftT > 0f) _updraftT -= dt;
            var fi = new FlightInput
            {
                Jump = input.JumpPressed, JumpHeld = input.JumpHeld,
                Dive = input.DodgeHeld && f.Airborne && f.P.Kind == FlyerKind.Griffon,     // Greif: V halten = Sturzflug
                Dash = input.DodgePressed && f.Airborne && f.P.Kind == FlyerKind.Skyscale, // Himmelsschuppe: V = Sprint nach vorn
                Descend = input.DescendHeld && f.Airborne && f.P.Kind == FlyerKind.Skyscale, // Himmelsschuppe: C halten = sinken + Flugausdauer laden
                Moving = _pc.LastWish.sqrMagnitude > 0.01f || Mathf.Abs(input.Move.y) > 0.1f,
                Pitch = input.Move.y, Turn = turn, NearWall = nearWall, WallSlope = slope, InUpdraft = _updraftT > 0f,
            };
            // Himmelsschuppe in der Luft: freie Richtung relativ zur Kamera (vor/zurück/links/rechts), keine A/D-Drehung
            bool skyAir = f.P.Kind == FlyerKind.Skyscale && f.Airborne;
            Vector3 skyWish = Vector3.zero;
            if (skyAir)
            {
                var camR = new Vector3(cam.z, 0f, -cam.x);
                skyWish = Vector3.ClampMagnitude(cam * input.Move.y + camR * (input.Move.x + input.Strafe), 1f);
                fi.Moving = skyWish.sqrMagnitude > 0.01f; fi.Pitch = fi.Moving ? 1f : 0f; fi.Turn = 0f;
            }
            if (f.Banned && !f.Airborne) { fi.Jump = false; fi.JumpHeld = false; }
            if (!f.Airborne)
            {
                // Bodentempo und Laufrichtung merken: Abheben/Kante übernimmt Schwung und Richtung statt bei 0 zu beginnen
                var hv = Flat3(_pc.HorizontalVelocity);
                f.GroundSpeed = hv.magnitude;
                if (hv.sqrMagnitude > 0.25f) _heading = hv.normalized;
            }
            // Greif: Flügelschläge zahlen mit der Ausweich-Ausdauer (3 Balken auf dem Greif, Regeneration wie zu Fuß)
            var end = _pc.Dodge.Endurance;   // Greif: Flügelschläge, Himmelsschuppe: Sprint – je 1 Ausweich-Balken
            if (end != null) f.Charges = end.Charges;
            int chargesBefore = f.Charges;
            bool wasAir = f.Airborne;
            if (f.P.Kind == FlyerKind.Skyscale && !wasAir) _skyVel = Flat3(_pc.HorizontalVelocity);   // Schwung vom Boden/Aufsitzen mitnehmen
            f.Step(dt, fi);
            if (f.LastDash == DashResult.Started) _dashDir = skyWish.sqrMagnitude > 0.01f ? skyWish.normalized : cam;
            if (end != null && wasAir)
            {
                int used = chargesBefore - f.Charges;
                if (used > 0) end.TryConsume(used * end.DodgeCost);                                         // Flügelschlag in der Luft / im Sturz
                else if (used < 0) end.Current = Mathf.Min(end.Max, end.Current - used * end.DodgeCost);   // Aufwind / Luftakrobatik füllt Balken
            }
            if (f.LastDash == DashResult.Started) SkillExecutor.VfxPulse(transform.position, 1.4f, new Color(0.75f, 0.55f, 1f));
            else if ((f.LastDash == DashResult.NoEndurance || f.LastDash == DashResult.NoCharge) && Time.time >= _dashHintAt) { _dashHintAt = Time.time + 2f; GameEventBus.Notify(f.LastDash == DashResult.NoCharge ? "Kein Ausweich-Balken für den Sprint" : "Zu wenig Flugkraft für den Sprint", new Color(1f, 0.7f, 0.4f)); }
            if (!wasAir && f.Airborne) { _airT = 0f; SkillExecutor.VfxPulse(transform.position, 1.6f, new Color(0.9f, 0.95f, 1f)); }

            if (!f.Airborne)
            {
                _pc.FlightVelocity = null; _pc.GravityScale = 1f; _pc.MaxFallSpeed = 0f;
                if (Flat3(_pc.HorizontalVelocity).sqrMagnitude <= 0.25f) _heading = Flat(transform.forward);
                ApplyTilt(f, dt);
                return;
            }
            // Modell → Welt
            Vector3 vel;
            if (f.P.Kind == FlyerKind.Skyscale)
            {
                // Sofortige Richtungswechsel: schnelles Abbremsen und Umlenken, gleiche Richtung behält den Schwung; Sprint in Wunsch-/Blickrichtung
                if (f.State == FlightState.Dash) _skyVel = _dashDir * f.Forward;
                else
                {
                    _skyVel = SkyscaleSteer.Step(_skyVel, skyWish, SkyscaleSteer.Cruise(f.State), dt);   // festes Tempo, keine Tempo-Effekte
                    f.Forward = _skyVel.magnitude;   // Anzeige (× Lauf) zeigt das echte Flugtempo
                }
                _heading = Vector3.RotateTowards(_heading, cam, 8f * dt, 0f);   // schaut in Kamerarichtung, fliegt in jede Richtung
                vel = _skyVel;   // kein Beherrschungs-/Tempo-Faktor bei der Himmelsschuppe
            }
            else
            {
                _heading = GriffonSteer.Direction(_heading, target, dt);   // Tempo wird sofort mit umgelenkt (kein Treiben in die alte Richtung)
                vel = GriffonSteer.Velocity(_heading, f.Forward * (SpeedFactor != null ? SpeedFactor() : 1f));
            }
            _pc.FlightVelocity = vel;
            _pc.GravityScale = 0f; _pc.MaxFallSpeed = 0f;
            _pc.SetVerticalVelocity(f.Vertical);
            _pc.ResetFallTracking();
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(_heading), (f.P.Kind == FlyerKind.Griffon ? GriffonSteer.VisualTurnDegPerSec : 360f) * dt);   // Körper folgt der Flugrichtung
            ApplyTilt(f, dt);
        }

        /// <summary>Greif neigt sich in Flugrichtung (Sturz: Nase runter bis 82°, Aufschwung: Nase hoch); Reittier und Reiter drehen um einen gemeinsamen Punkt.</summary>
        void ApplyTilt(FlightMotor f, float dt)
        {
            float target = 0f;
            if (f.P.Kind == FlyerKind.Griffon && f.Airborne)
                target = Mathf.Clamp(Mathf.Atan2(-f.Vertical, Mathf.Max(0.5f, f.Forward)) * Mathf.Rad2Deg, -MaxClimbTilt, MaxDiveTilt);
            _tilt = Mathf.MoveTowards(_tilt, target, 220f * dt);
            var q = Quaternion.Euler(_tilt, 0f, 0f);
            var pivot = new Vector3(0f, _seat.y * 0.6f, 0f);
            if (_visual != null) { _visual.transform.localRotation = q; _visual.transform.localPosition = pivot - q * pivot; }
            _pc.VisualOffset = pivot + q * (_seat - pivot);
            _pc.VisualTilt = q;
        }

        /// <summary>Greif-Tempoleiste: Anteil an 5 × Lauf, Marken 3×/4×/5×, Sturz-Energie in m. Null, wenn nicht sichtbar.</summary>
        public bool GriffonSpeed(out float fraction, out float timesRun, out float energy, out bool diving)
        {
            fraction = timesRun = energy = 0f; diving = false;
            var f = Flight; if (f == null || !f.Airborne || f.P.Kind != FlyerKind.Griffon) return false;
            timesRun = f.Speed / f.Run; fraction = Mathf.Clamp01(timesRun / (f.P.DiveFactor + f.P.DiveFlapStep * f.P.DiveFlapMax));
            diving = f.State == FlightState.Dive;
            energy = diving ? f.Energy * FlightMotor.Recovery(timesRun) : f.Energy;   // im Sturz: Höhe, die S beim Loslassen jetzt zurückbrächte
            return true;
        }

        /// <summary>Lauf 10: Anzeige für das HUD (Auftrieb-Ladungen bzw. Flugausdauer).</summary>
        public string FlightHudText()
        {
            var f = Flight;
            if (f == null) return null;
            string s = "";   // Greif: Flügelschläge zeigt die Ausweich-Leiste; Himmelsschuppe: Flugkraft-Leiste über der Ausweich-Leiste
            if (f.Airborne) s += "   " + Mathf.RoundToInt(f.Speed / f.Run * 10f) / 10f + "× Lauf · " + Mathf.RoundToInt(f.Altitude) + " m";
            if (FlightDebug.Show) s += "\n" + f.State + " · Decke " + Mathf.RoundToInt(f.Ceiling) + " m" + (f.Banned ? " · Verbot: " + FlightRule.Reason : "") + (FlightDebug.Infinite ? " · unendlich" : "");
            return s;
        }
    }
}
