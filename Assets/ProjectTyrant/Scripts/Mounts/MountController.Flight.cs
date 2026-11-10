using UnityEngine;
using ProjectTyrant.Flight;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 10: Flug für Greif und Himmelsschuppe. Der reine <see cref="FlightMotor"/> rechnet Tempo und Steigrate,
    /// der <see cref="PlayerController"/> bewegt (keine Rigidbody-Physik). Flugregeln je Ort über <see cref="FlightRules"/>.
    /// Greif: Leertaste = Abheben/Flügelschlag (Auftrieb-Ladung), V halten = Sturzflug, loslassen = Abfangen; W/S = Nase runter/hoch, Kamera lenkt.
    /// Himmelsschuppe: Leertaste halten = steigen, loslassen = schweben, W = Vorwärtsflug, V halten = Sturz; an steilen Wänden haften.
    /// </summary>
    public partial class MountController
    {
        public const float GriffonTurnRate = 1.8f, SkyscaleTurnRate = 3.2f;

        static FlightParams ParamsFor(MountKind k)
        {
            var mm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
            return k == MountKind.Griffon ? FlightParams.Griffon(MasteryEffects.GriffonRank(mm)) : FlightParams.Skyscale(MasteryEffects.SkyscaleRank(mm));
        }

        void BeginFlight(MountKind k)
        {
            Flight = FlightRules.IsFlyer(k) ? new FlightMotor(ParamsFor(k)) : null;
            _heading = Flat(transform.forward); _ruleT = 0f; _airT = 0f; _wasBanned = false;
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
                StoryFinished = story != null && story.State != null && story.State.Finished,
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
                Jump = input.JumpPressed, JumpHeld = input.JumpHeld, Dive = input.DodgeHeld && f.Airborne,
                Pitch = input.Move.y, Turn = turn, NearWall = nearWall, WallSlope = slope, InUpdraft = _updraftT > 0f,
            };
            if (f.Banned && !f.Airborne) { fi.Jump = false; fi.JumpHeld = false; }
            bool wasAir = f.Airborne;
            f.Step(dt, fi);
            if (!wasAir && f.Airborne) { _airT = 0f; SkillExecutor.VfxPulse(transform.position, 1.6f, new Color(0.9f, 0.95f, 1f)); }

            if (!f.Airborne)
            {
                _pc.FlightVelocity = null; _pc.GravityScale = 1f; _pc.MaxFallSpeed = 0f;
                _heading = Flat(transform.forward);
                return;
            }
            // Modell → Welt
            float rate = f.P.Kind == FlyerKind.Griffon ? GriffonTurnRate : SkyscaleTurnRate;
            _heading = Vector3.RotateTowards(_heading, target, rate * dt, 0f);
            var vel = _heading * f.Forward * (SpeedFactor != null ? SpeedFactor() : 1f);
            if (f.State == FlightState.Hover && _pc.LastWish.sqrMagnitude > 0.01f) vel += Flat(_pc.LastWish).normalized * f.Run * 0.6f;   // seitliches Treiben beim Schweben
            _pc.FlightVelocity = vel;
            _pc.GravityScale = 0f; _pc.MaxFallSpeed = 0f;
            _pc.SetVerticalVelocity(f.Vertical);
            _pc.ResetFallTracking();
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(_heading), 360f * dt);
        }

        /// <summary>Lauf 10: Anzeige für das HUD (Auftrieb-Ladungen bzw. Flugausdauer).</summary>
        public string FlightHudText()
        {
            var f = Flight;
            if (f == null) return null;
            string s = f.P.Kind == FlyerKind.Griffon ? "Auftrieb " + f.Charges + "/" + f.P.Charges : "Flugausdauer " + Mathf.CeilToInt(f.Endurance) + "/" + Mathf.RoundToInt(f.P.Endurance);
            if (f.Airborne) s += "   " + Mathf.RoundToInt(f.Forward / f.Run * 10f) / 10f + "× Lauf · " + Mathf.RoundToInt(f.Altitude) + " m";
            if (FlightDebug.Show) s += "\n" + f.State + " · Decke " + Mathf.RoundToInt(f.Ceiling) + " m" + (f.Banned ? " · Verbot: " + FlightRule.Reason : "") + (FlightDebug.Infinite ? " · unendlich" : "");
            return s;
        }
    }
}
