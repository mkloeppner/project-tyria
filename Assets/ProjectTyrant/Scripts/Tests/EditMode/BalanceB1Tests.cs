using System.Collections.Generic;
using NUnit.Framework;
using ProjectTyrant.Balance;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Balance B1: Gefahrenstufen, Verfolgung, Neutral/Rudel/Fesseln, Validatoren, Gegner-Kurve, Telemetrie und Zielwerte.</summary>
    public class BalanceB1Tests
    {
        static List<DangerSettings> All() { var l = new List<DangerSettings>(); for (int i = 0; i <= 5; i++) l.Add(DangerTable.Default(i)); return l; }
        static PursuitModel Chase(int level, float now = 0f, bool hunter = false)
        {
            var m = new PursuitModel(PursuitProfile.From(DangerTable.Default(level), hunter));
            m.Engage(now, true);
            return m;
        }
        static PursuitInput In(float now, float fromSpawn = 5f, float toTarget = 3f, bool los = true, float hitMe = -99f, float dealt = -99f, bool mounted = false)
            => new PursuitInput { Now = now, DistanceFromSpawn = fromSpawn, DistanceToTarget = toTarget, HasLineOfSight = los, TargetHitMeAt = hitMe, TargetDealtDamageAt = dealt, TargetMounted = mounted };

        // ---------------- Profile ----------------
        [Test] public void Profiles_AreMonotonic() => Assert.IsEmpty(BalanceValidator.ValidateProfiles(All()));
        [Test] public void Profiles_TableValues()
        {
            var s0 = DangerTable.Default(0); var s5 = DangerTable.Default(5);
            Assert.AreEqual(450, s0.AggroRange); Assert.AreEqual(800, s0.Leash); Assert.AreEqual(0.85f, s0.PursuitSpeed, 1e-4); Assert.AreEqual(8, s0.MaxPursuit); Assert.AreEqual(0.8f, s0.NeutralShare, 1e-4);
            Assert.AreEqual(1000, s5.AggroRange); Assert.AreEqual(2500, s5.Leash); Assert.AreEqual(1.15f, s5.PursuitSpeed, 1e-4); Assert.AreEqual(60, s5.MaxPursuit); Assert.AreEqual(0f, s5.NeutralShare, 1e-4);
        }
        [Test] public void Profiles_ControlsByStage()
        {
            Assert.AreEqual(ControlKinds.None, DangerTable.Default(0).Controls);
            Assert.AreEqual(ControlKinds.Cripple, DangerTable.Default(1).Controls);
            Assert.IsTrue(DangerTable.Allows(DangerTable.Default(4), ControlKinds.Pull));
            Assert.IsTrue(DangerTable.Allows(DangerTable.Default(5), ControlKinds.Dismount));
            Assert.IsFalse(DangerTable.Allows(DangerTable.Default(3), ControlKinds.Pull));
        }
        [Test] public void Profiles_TelegraphMinima()
        {
            Assert.GreaterOrEqual(DangerTable.Default(0).MinTelegraph, 1.2f); Assert.GreaterOrEqual(DangerTable.Default(1).MinTelegraph, 1.2f);
            Assert.GreaterOrEqual(DangerTable.Default(2).MinTelegraph, 1.0f); Assert.GreaterOrEqual(DangerTable.Default(3).MinTelegraph, 1.0f);
            Assert.GreaterOrEqual(DangerTable.Default(4).MinTelegraph, 0.75f); Assert.GreaterOrEqual(DangerTable.Default(5).MinTelegraph, 0.75f);
            Assert.AreEqual(1.2f, EnemyCurve.Windup(0.6f, DangerTable.Default(0)), 1e-4);
        }
        [Test] public void Units_Gw2ToMeters() { Assert.AreEqual(11.43f, Units.M(450), 0.01f); Assert.AreEqual(450f, Units.U(Units.M(450)), 0.01f); }

        // ---------------- Verfolgung ----------------
        [Test] public void Pursuit_GiveUp_Leash()
        {
            var m = Chase(0); var leash = m.Profile.Leash;
            Assert.AreEqual(GiveUpReason.None, m.Tick(In(1f, leash - 1f)));
            Assert.AreEqual(GiveUpReason.Leash, m.Tick(In(2f, leash + 1f)));
            Assert.IsTrue(m.IsReturning);
        }
        [Test] public void Pursuit_GiveUp_Duration_ResetByDamage()
        {
            var m = Chase(0);
            Assert.AreEqual(GiveUpReason.None, m.Tick(In(6f, dealt: 5.9f)));
            Assert.AreEqual(GiveUpReason.None, m.Tick(In(9f, hitMe: 7f, dealt: 8.9f)));    // Treffer bei 7 s setzt zurück
            Assert.AreEqual(GiveUpReason.Duration, m.Tick(In(15.5f, hitMe: 7f, dealt: 15.4f)));
        }
        [Test] public void Pursuit_GiveUp_LineOfSight_ByStage()
        {
            var m = Chase(1);
            m.Tick(In(1f, los: false, dealt: 0.9f)); Assert.AreEqual(GiveUpReason.None, m.Tick(In(3.5f, los: false, dealt: 3.4f)));
            Assert.AreEqual(GiveUpReason.LineOfSight, m.Tick(In(4.2f, los: false, dealt: 4.1f)));
            var h = Chase(4);
            h.Tick(In(1f, los: false)); Assert.AreEqual(GiveUpReason.None, h.Tick(In(6.5f, los: false)));
            Assert.AreEqual(GiveUpReason.LineOfSight, h.Tick(In(7.2f, los: false)));
        }
        [Test] public void Pursuit_GiveUp_Mercy_OnlyStage01()
        {
            var m = Chase(0);
            Assert.AreEqual(GiveUpReason.Mercy, m.Tick(In(5.5f, toTarget: Units.M(650), dealt: 0f)));
            var m2 = Chase(2);
            Assert.AreEqual(GiveUpReason.None, m2.Tick(In(5.5f, toTarget: Units.M(650), dealt: 0f)));
        }
        [Test] public void Pursuit_Mercy_NotWhileAttacking()
        {
            var m = Chase(1);
            Assert.AreEqual(GiveUpReason.None, m.Tick(In(5.5f, toTarget: Units.M(650), dealt: 3f)));
        }
        [Test] public void Pursuit_GiveUp_Mounted()
        {
            var m = Chase(3);
            Assert.AreEqual(GiveUpReason.None, m.Tick(In(1f, toTarget: Units.M(300), mounted: true)));
            Assert.AreEqual(GiveUpReason.Mounted, m.Tick(In(1.2f, toTarget: Units.M(420), mounted: true)));
            var normal5 = Chase(5); Assert.AreEqual(GiveUpReason.Mounted, normal5.Tick(In(1f, toTarget: Units.M(420), mounted: true)));
            var hunter5 = Chase(5, 0f, true); Assert.AreEqual(GiveUpReason.None, hunter5.Tick(In(1f, toTarget: Units.M(800), mounted: true)));
        }
        [Test] public void Pursuit_GiveUp_TravelAndSafezone()
        {
            var m = Chase(5); var i = In(1f); i.TargetTravelled = true; Assert.AreEqual(GiveUpReason.Travel, m.Tick(i));
            var s = Chase(5); var j = In(1f); j.TargetInSafezone = true; Assert.AreEqual(GiveUpReason.Safezone, s.Tick(j));
        }
        [Test] public void Pursuit_OldTravel_DoesNotCancelNewChase()
        {
            // Regression: Reise bei t=5, Gegner bemerkt den Spieler bei t=100 (mit Zögern) → darf nicht wegen „Reise“ aufgeben
            var m = new PursuitModel(PursuitProfile.From(DangerTable.Default(0)));
            m.Engage(100f, false);
            var i = In(100.5f); i.TargetTravelled = 5f > m.ChaseStart;
            Assert.AreEqual(GiveUpReason.None, m.Tick(i));
            Assert.AreEqual(100f, m.ChaseStart, 1e-4);
        }

        [Test] public void Pursuit_Hesitation_Stage01()
        {
            var m = new PursuitModel(PursuitProfile.From(DangerTable.Default(0)));
            m.Engage(0f, false); Assert.AreEqual(PursuitState.Hesitate, m.State);
            m.Tick(In(0.5f)); Assert.AreEqual(PursuitState.Hesitate, m.State);
            m.Tick(In(1.05f)); Assert.AreEqual(PursuitState.Chase, m.State);
            var a = new PursuitModel(PursuitProfile.From(DangerTable.Default(0))); a.Engage(0f, true); Assert.AreEqual(PursuitState.Chase, a.State);   // Angriff: kein Zögern
            var b = new PursuitModel(PursuitProfile.From(DangerTable.Default(2))); b.Engage(0f, false); Assert.AreEqual(PursuitState.Chase, b.State);
        }
        [Test] public void Pursuit_SpeedByStage()
        {
            Assert.Less(Chase(0).ChaseSpeed(7f, 1f, false), 7f);
            Assert.Less(Chase(1).ChaseSpeed(7f, 1f, false), 7f);
            Assert.AreEqual(7f, Chase(2).ChaseSpeed(7f, 1f, false), 1e-3);
            Assert.Greater(Chase(4).ChaseSpeed(7f, 1f, false), 7f);
            var h = Chase(4); Assert.Greater(h.ChaseSpeed(7f, 7f, true), h.ChaseSpeed(7f, 1f, true));   // Eile-Phase
        }
        [Test] public void Pursuit_Returning_IgnoresEngage()
        {
            var m = Chase(0); m.Tick(In(1f, m.Profile.Leash + 5f)); Assert.IsTrue(m.IsReturning);
            m.Engage(2f, true); Assert.IsTrue(m.IsReturning);
            m.Arrived(); Assert.AreEqual(PursuitState.Idle, m.State);
        }
        [Test] public void Pursuit_NoLeash_Fortress()
        {
            var p = PursuitProfile.From(DangerTable.Default(5)); p.NoLeash = true;
            var m = new PursuitModel(p); m.Engage(0f, true);
            Assert.AreEqual(GiveUpReason.None, m.Tick(In(200f, 500f)));
        }

        // ---------------- Neutral, Rudel, Fesseln, Separation ----------------
        [Test] public void Neutral_OnlyWhenProvoked()
        {
            var m = new PursuitModel(PursuitProfile.From(DangerTable.Default(0), neutral: true));
            Assert.IsFalse(m.Notices(2f, false));
            Assert.IsTrue(m.Notices(2f, true));
            var h = new PursuitModel(PursuitProfile.From(DangerTable.Default(0)));
            Assert.IsTrue(h.Notices(5f, false)); Assert.IsFalse(h.Notices(30f, false));
        }
        [Test] public void Neutral_GivesUpFaster() => Assert.Less(PursuitProfile.From(DangerTable.Default(1), neutral: true).MaxPursuit, PursuitProfile.From(DangerTable.Default(1)).MaxPursuit);
        [Test] public void Pack_RadiusByStage()
        {
            Assert.IsFalse(PackRules.Joins(DangerTable.Default(0), true, 1f, 0));
            Assert.IsTrue(PackRules.Joins(DangerTable.Default(1), true, 3f, 0));
            Assert.IsFalse(PackRules.Joins(DangerTable.Default(1), true, 3f, 1));
            Assert.IsTrue(PackRules.Joins(DangerTable.Default(2), true, Units.M(290), 1));
            Assert.IsFalse(PackRules.Joins(DangerTable.Default(2), true, Units.M(320), 0));
            Assert.IsFalse(PackRules.Joins(DangerTable.Default(2), false, 1f, 0));
            Assert.IsFalse(PackRules.Joins(DangerTable.Default(2), true, 1f, 2));   // Gruppen bis 3
            Assert.IsTrue(PackRules.Joins(DangerTable.Default(5), true, 1f, 6));    // Rudel bis 8
        }
        [Test] public void Controls_RateAndIcd()
        {
            var g = new ControlGate(1); var s0 = DangerTable.Default(0);
            for (int i = 0; i < 50; i++) Assert.IsFalse(g.TryUse(s0, ControlKinds.Cripple, i * 10f));
            var s5 = DangerTable.Default(5); var g5 = new ControlGate(2); int used = 0; float last = -99f;
            for (int i = 0; i < 400; i++) { float t = i * 1f; if (g5.TryUse(s5, ControlKinds.Immobilize, t)) { Assert.GreaterOrEqual(t - last, DangerTable.ControlIcd); last = t; used++; } }
            Assert.Greater(used, 30);
            Assert.IsTrue(new ControlGate(3).TryUse(s0, ControlKinds.None, 0f));   // Schaden ohne Kontrolle immer erlaubt
        }
        [Test] public void Controls_Stage1_OnlyCripple()
        {
            var s = DangerTable.Default(1); var g = new ControlGate(4); bool any = false;
            for (int i = 0; i < 300; i++) { Assert.IsFalse(g.TryUse(s, ControlKinds.Chill, i * 7f)); }
            var g2 = new ControlGate(5); for (int i = 0; i < 300; i++) any |= g2.TryUse(s, ControlKinds.Cripple, i * 7f);
            Assert.IsTrue(any);
        }
        [Test] public void Separation_NeverTowardsPlayer()
        {
            Assert.AreEqual(Vector3.zero, Separation.Push(Vector3.zero, new Vector3(0.5f, 0, 0), false, 1.4f));
            Assert.Greater(Separation.Push(Vector3.zero, new Vector3(0.5f, 0, 0), true, 1.4f).sqrMagnitude, 0f);
            var to = Separation.ClampToGap(new Vector3(2, 0, 0), new Vector3(0.2f, 0, 0), Vector3.zero);
            Assert.GreaterOrEqual(new Vector2(to.x, to.z).magnitude, DangerTable.MinPlayerGap - 1e-3f);
        }

        // ---------------- Gefahrenkarte und Validator ----------------
        static ZoneDangerMap StartMap() => ZoneDangerTable.Build("sonnfurt", 200f, new Vector2(-60, -60), new[] { new Vector2(-54, -51) },
            new[] { new Vector2(-54, -54), new Vector2(-8, -44), new Vector2(-15, -3), new Vector2(40, -50), new Vector2(-25, 52) },
            new (string, Vector2, float)[] { ("Weiler Sonnfurt", new Vector2(-60, -60), 34), ("Hof Gerstenbrunn", new Vector2(-8, -60), 24), ("Ruinen von Elmhain", new Vector2(58, -55), 32), ("Grenzposten Dornwacht", new Vector2(5, 5), 32), ("Rabenschanze", new Vector2(58, 58), 30), ("Spiegelsee", new Vector2(-45, 40), 30) }, true);

        [Test] public void DangerMap_StartZoneStages()
        {
            var m = StartMap();
            Assert.AreEqual(0, m.At(new Vector2(-60, -60)));
            Assert.AreEqual(0, m.At(new Vector2(-8, -60)));
            Assert.AreEqual(2, m.At(new Vector2(5, 5)));
            Assert.AreEqual(2, m.At(new Vector2(58, -55)));
            Assert.AreEqual(1, m.At(new Vector2(-40, 0)));
        }
        [Test] public void DangerMap_Safezones()
        {
            var m = StartMap();
            Assert.IsTrue(m.InSafezone(new Vector2(-60, -60)));
            Assert.IsTrue(m.InSafezone(new Vector2(-15, -5)));
            Assert.IsFalse(m.InSafezone(new Vector2(30, 30)));
            Assert.IsFalse(m.SpawnAllowed(new Vector2(-8, -50)));   // Stufe 0, < 20 m an Wegmarke Gerstenbrunn
        }
        [Test] public void DangerMap_AllZonesAssigned()
        {
            foreach (var kv in ZoneDangerTable.Specs)
            {
                var m = ZoneDangerTable.Build(kv.Key, 1000f, new Vector2(0, -400), new Vector2[0], new[] { new Vector2(0, -400) }, new (string, Vector2, float)[] { ("A", new Vector2(0, 300), 110) }, false);
                Assert.AreEqual(kv.Value.Entry, m.At(new Vector2(0, -390)), kv.Key);
                Assert.AreEqual(kv.Value.Core, m.At(new Vector2(0, 300)), kv.Key);
                Assert.LessOrEqual(Mathf.Abs(kv.Value.Core - kv.Value.Entry), 1, kv.Key);
            }
        }
        [Test] public void Validator_StartZoneClean() => Assert.IsEmpty(BalanceValidator.Validate(StartMap(), new SpawnInfo[0], new EventInfo[0]));
        [Test] public void Validator_DetectsJump()
        {
            var m = StartMap(); m.Areas.Add(new DangerArea { Name = "X", Center = new Vector2(-40, -80), Radius = 10, Level = 4 });
            Assert.IsTrue(BalanceValidator.Validate(m, new SpawnInfo[0], new EventInfo[0]).Exists(e => e.Contains("Sprung")));
        }
        [Test] public void Validator_SpawnRules()
        {
            var m = StartMap();
            var e = BalanceValidator.Validate(m, new[] { new SpawnInfo { Id = "hub", Pos = new Vector2(-58, -58) }, new SpawnInfo { Id = "vet0", Pos = new Vector2(-20, -80), Tier = EnemyTier.Veteran } }, new EventInfo[0]);
            Assert.IsTrue(e.Exists(x => x.Contains("Sicherheitszone")));
            Assert.IsTrue(e.Exists(x => x.Contains("Veteran in Stufe 0")));
        }
        [Test] public void Validator_NeutralShare()
        {
            var m = StartMap(); var sp = new List<SpawnInfo>();
            for (int i = 0; i < 6; i++) sp.Add(new SpawnInfo { Id = "h" + i, Pos = new Vector2(-30 + i, -85), Neutral = false });
            Assert.IsTrue(BalanceValidator.Validate(m, sp, new EventInfo[0]).Exists(x => x.Contains("Neutral-Quote Stufe 0")));
            for (int i = 0; i < sp.Count; i++) if (i < 5) { var s = sp[i]; s.Neutral = true; sp[i] = s; }
            Assert.IsFalse(BalanceValidator.Validate(m, sp, new EventInfo[0]).Exists(x => x.Contains("Neutral-Quote")));
        }

        // ---------------- Event-Stufen ----------------
        [Test] public void EventTier_AllowedByDanger()
        {
            Assert.IsTrue(EventTierRules.AllowedIn(EventTier.Intro, 0)); Assert.IsFalse(EventTierRules.AllowedIn(EventTier.Intro, 1));
            Assert.IsTrue(EventTierRules.AllowedIn(EventTier.Easy, 1)); Assert.IsFalse(EventTierRules.AllowedIn(EventTier.Standard, 0));
            Assert.IsTrue(EventTierRules.AllowedIn(EventTier.Hard, 5)); Assert.IsFalse(EventTierRules.AllowedIn(EventTier.Meta, 3));
        }
        [Test] public void EventTier_NoAutoStartBelow8Min()
        {
            Assert.IsFalse(EventTierRules.TimerAllowed(EventTier.Intro, 60f));
            Assert.IsFalse(EventTierRules.TimerAllowed(EventTier.Easy, 420f));
            Assert.IsTrue(EventTierRules.TimerAllowed(EventTier.Easy, 480f));
            Assert.IsTrue(EventTierRules.TimerAllowed(EventTier.Standard, 60f));
            var m = StartMap();
            var errs = BalanceValidator.Validate(m, new SpawnInfo[0], new[] { new EventInfo { Id = "x", Center = new Vector2(-40, -45), Tier = EventTier.Intro, MinTimer = 60f } });
            Assert.IsTrue(errs.Exists(e => e.Contains("Auto-Start")));
        }
        [Test] public void EventTier_StartZoneDistance()
        {
            Assert.AreEqual(EventTier.Intro, EventTierRules.StartZoneMax(StartMap().BandAt(new Vector2(-50, -50))));
            Assert.AreEqual(EventTier.Easy, EventTierRules.StartZoneMax(StartMap().BandAt(new Vector2(-40, 0))));
            Assert.AreEqual(EventTier.Standard, EventTierRules.StartZoneMax(StartMap().BandAt(new Vector2(60, 60))));
            Assert.AreEqual(10f, EventTierRules.AnnounceLead);
        }
        [Test] public void EventTier_ValuesIntro()
        {
            var v = EventTierRules.Values(EventTier.Intro);
            Assert.GreaterOrEqual(v.MinTimer, 180f); Assert.AreEqual(0.5f, v.TargetFactor, 1e-4); Assert.IsFalse(v.Veterans); Assert.IsFalse(v.Controls); Assert.LessOrEqual(v.MaxTrash, 5);
            Assert.GreaterOrEqual(EventTierRules.Values(EventTier.Easy).MinTimer, 150f);
        }
        [Test] public void EventTier_SpawnClearance()
        {
            var m = StartMap();
            var errs = BalanceValidator.Validate(m, new SpawnInfo[0], new[] { new EventInfo { Id = "x", Center = new Vector2(-40, -45), Tier = EventTier.Intro, SpawnPoints = new[] { new Vector2(-50, -50) } } });
            Assert.IsTrue(errs.Exists(e => e.Contains("Event-Spawn")));
        }

        // ---------------- Gegner-Kurve ----------------
        [Test] public void Curve_TtdTargetsHold()
        {
            for (int d = 0; d <= 5; d++)
                foreach (int lvl in new[] { 2, 7, 15, 30 })
                {
                    var s = DangerTable.Default(d);
                    Assert.GreaterOrEqual(EnemyCurve.SimTtd(lvl, EnemyTier.Trash, s, 2f, 3), s.TtdThreeTrash, "Stufe " + d + " L" + lvl);
                    if (s.TtdVeteran > 0f) Assert.GreaterOrEqual(EnemyCurve.SimTtd(lvl, EnemyTier.Veteran, s, 2.2f, 1), s.TtdVeteran, "Vet Stufe " + d + " L" + lvl);
                }
        }
        [Test] public void Curve_LateGameCorridorUpper()
        {
            // Spätspiel nicht zu leicht: Zeit-bis-Tod höchstens 50 % über der Untergrenze
            for (int d = 3; d <= 5; d++) { var s = DangerTable.Default(d); Assert.LessOrEqual(EnemyCurve.SimTtd(30, EnemyTier.Trash, s, 2f, 3), s.TtdThreeTrash * 1.5f, "Stufe " + d); }
        }
        [Test] public void Curve_HitShareCaps()
        {
            for (int d = 0; d <= 5; d++) { var s = DangerTable.Default(d); Assert.LessOrEqual(EnemyCurve.HitShare(s, EnemyTier.Trash, 2f), s.HitShare + 1e-5f); }
        }
        [Test] public void Curve_HealthMonotonicInStageAndRank()
        {
            for (int d = 1; d <= 5; d++) Assert.Greater(EnemyCurve.Health(15, EnemyTier.Trash, DangerTable.Default(d)), EnemyCurve.Health(15, EnemyTier.Trash, DangerTable.Default(d - 1)));
            var s = DangerTable.Default(2);
            Assert.Greater(EnemyCurve.Health(10, EnemyTier.Veteran, s), EnemyCurve.Health(10, EnemyTier.Trash, s) * 3f);
            Assert.Greater(EnemyCurve.Health(10, EnemyTier.Champion, s), EnemyCurve.Health(10, EnemyTier.Veteran, s));
        }
        [Test] public void Curve_RoleModifiers()
        {
            var s = DangerTable.Default(2);
            Assert.AreEqual(0.8f, EnemyCurve.Health(10, EnemyTier.Trash, s, EnemyRole.Ranged) / EnemyCurve.Health(10, EnemyTier.Trash, s), 1e-3);
            Assert.AreEqual(0.7f, EnemyCurve.Coefficient(10, EnemyTier.Trash, s, 2f, 1f, EnemyRole.Healer) / EnemyCurve.Coefficient(10, EnemyTier.Trash, s, 2f), 1e-3);
            Assert.AreEqual(1.1f, EnemyCurve.RoleSpeed(EnemyRole.Hunter), 1e-4);
        }

        // ---------------- Telemetrie und Zielwerte ----------------
        [Test] public void Telemetry_RoundTrip()
        {
            float clock = 0f; BalanceTelemetry.Clock = () => clock;
            BalanceTelemetry.Folder = "Temp/PT_QA/balance/telemetry_test";
            BalanceTelemetry.Begin("Beginner", "testzone", 7, 0);
            clock = 10f; BalanceTelemetry.Record(TelemetryKind.Death, new Vector3(1, 0, 2), "Plünderer", EnemyTier.Trash, "Angriff", 12f);
            BalanceTelemetry.Record(TelemetryKind.EscapeAttempt, Vector3.zero); BalanceTelemetry.Record(TelemetryKind.EscapeResult, Vector3.zero, result: "aufgegeben");
            clock = 1800f; var s = BalanceTelemetry.End();
            var path = System.IO.Path.Combine(BalanceTelemetry.Folder, s.Id + ".json");
            var r = BalanceTelemetry.Read(path);
            Assert.AreEqual(1, r.Deaths); Assert.AreEqual(1f, r.EscapeRate, 1e-4); Assert.AreEqual(1f, r.DeathsPer30, 1e-3); Assert.AreEqual("Plünderer", r.Records[0].Enemy);
            BalanceTelemetry.Clock = () => Time.time; BalanceTelemetry.Folder = "Temp/PT_QA/balance/telemetry";
        }
        [Test] public void Targets_EvaluateMedianAndCorridor()
        {
            var sessions = new List<TelemetrySession>();
            foreach (int deaths in new[] { 0, 0, 1 })
            {
                var s = new TelemetrySession { Profile = "Beginner", Zone = "z", Danger = 0, Seconds = 1800f };
                for (int i = 0; i < deaths; i++) s.Records.Add(new TelemetryRecord { Kind = TelemetryKind.Death });
                sessions.Add(s);
            }
            var res = TargetTable.Evaluate(sessions).Find(x => x.Metric == Metric.Deaths30);
            Assert.AreEqual(0f, res.Value); Assert.IsTrue(res.Pass); Assert.AreEqual(3, res.Samples);
            Assert.IsFalse(TargetTable.Get(BotProfileKind.Experienced, Metric.Deaths30, 5).Contains(1f));   // Spätspiel zu leicht = Befund
            Assert.IsTrue(TargetTable.Get(BotProfileKind.Experienced, Metric.Deaths30, 5).Contains(3f));
        }
        [Test] public void Escape_TrackerWindow()
        {
            var e = new EscapeTracker();
            Assert.IsFalse(e.Tick(0f, true, true, false));
            Assert.IsTrue(e.Tick(EscapeTracker.Window + 0.01f, true, true, false));
            Assert.IsTrue(e.InAttempt);
            Assert.IsFalse(e.Tick(3f, true, true, true)); Assert.IsFalse(e.InAttempt);
        }
        [Test] public void Bot_ProfilesDeterministic()
        {
            var b = BotSettings.For(BotProfileKind.Beginner); var x = BotSettings.For(BotProfileKind.Experienced);
            Assert.AreEqual(3, b.WeaponSkills); Assert.AreEqual(0.6f, b.Reaction, 1e-4); Assert.AreEqual(0.25f, b.FleeBelow, 1e-4); Assert.IsFalse(b.UseMount);
            Assert.IsFalse(b.DodgesTelegraph(1)); Assert.IsTrue(b.DodgesTelegraph(2));
            Assert.IsTrue(x.DodgesTelegraph(1)); Assert.AreEqual(0.25f, x.Reaction, 1e-4); Assert.AreEqual(2, x.AvoidLevelAbove);
        }
        [Test] public void Heatmap_PixelMapping()
        {
            Assert.AreEqual(new Vector2Int(0, 0), Heatmap.ToPixel(new Vector2(-100, -100), 200f, 512, 512));
            Assert.AreEqual(new Vector2Int(511, 511), Heatmap.ToPixel(new Vector2(100, 100), 200f, 512, 512));
            var t = Heatmap.Render(null, 200f, new[] { Vector2.zero }, Color.red, StartMap(), 64);
            Assert.AreEqual(64, t.width);
            Assert.Greater(t.GetPixel(34, 34).r, 0.5f);
        }
    }
}
