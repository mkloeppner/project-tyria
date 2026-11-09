using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    /// <summary>Lauf 4 / M17: Reittier-Zustand, Fähigkeiten, Eingabe, Erwerb, Speicherstand.</summary>
    public class MountTests
    {
        static MountMotor Mounted(MountKind k)
        {
            var m = new MountMotor();
            Assert.IsTrue(m.BeginMount(MountStats.Default(k), true, true));
            Assert.IsFalse(m.Tick(0.5f));
            Assert.IsTrue(m.Tick(0.51f));
            return m;
        }

        [Test]
        public void Mounting_TakesOneSecond_AndNeedsGroundAndOwnership()
        {
            var m = new MountMotor();
            Assert.IsFalse(m.BeginMount(MountStats.Default(MountKind.Raptor), false, true), "nicht in der Luft");
            Assert.IsFalse(m.BeginMount(MountStats.Default(MountKind.Raptor), true, false), "nur besessene Reittiere");
            Assert.IsTrue(m.BeginMount(MountStats.Default(MountKind.Raptor), true, true));
            Assert.IsFalse(m.Tick(0.99f));
            Assert.IsFalse(m.IsMounted);
            Assert.IsTrue(m.Tick(0.02f));
            Assert.AreEqual(MountKind.Raptor, m.Mounted);
            Assert.AreEqual(1f, MountMotor.MountTime);
        }

        [Test]
        public void DamageInterruptsChannel_MountHpAbsorbs_AndKnocksOff()
        {
            var m = new MountMotor();
            m.BeginMount(MountStats.Default(MountKind.Springer), true, true);
            Assert.AreEqual(500f, m.AbsorbDamage(500f, out bool k0), "Aufsitzen: Schaden trifft den Spieler und bricht ab");
            Assert.IsFalse(m.IsChanneling);

            m = Mounted(MountKind.Springer);
            float hp = m.Stats.MaxHealth;
            Assert.AreEqual(0f, m.AbsorbDamage(hp * 0.6f, out bool k1));
            Assert.IsFalse(k1); Assert.IsTrue(m.IsMounted);
            Assert.AreEqual(0.4f, m.HealthFraction, 1e-3f);
            Assert.AreEqual(0f, m.AbsorbDamage(hp, out bool k2), "Überschuss geht nicht auf den Spieler");
            Assert.IsTrue(k2); Assert.IsFalse(m.IsMounted);
            Assert.IsFalse(m.BeginMount(MountStats.Default(MountKind.Springer), true, true), "nach dem Abwurf kurz gesperrt");
            m.Tick(MountMotor.RemountCooldownAfterKnockoff + 0.1f);
            Assert.IsTrue(m.BeginMount(MountStats.Default(MountKind.Springer), true, true));
        }

        [Test]
        public void Raptor_LeapClearsLavaRing_SpringerChargesToFifteenMeters()
        {
            var r = MountStats.Default(MountKind.Raptor);
            Assert.GreaterOrEqual(MountMotor.LeapDistance(r), ReachabilityModel.RaptorGap, "Sprungweite ≥ Raptor-Lücke der Flutfüllung");
            float airtime = 2f * r.LeapUp / 24f;
            Assert.GreaterOrEqual(r.LeapSpeed * airtime, 8f + 3f, "in der Luft über den 8-m-Lavaring + Ränder");
            var m = Mounted(MountKind.Raptor);
            Assert.IsTrue(m.TryLeap(out float sp, out float t, out float up));
            Assert.IsFalse(m.TryLeap(out sp, out t, out up), "kein Doppelsprung");

            var s = Mounted(MountKind.Springer);
            s.BeginCharge(); s.Tick(0.0f);
            Assert.AreEqual(s.Stats.MinJump, s.ReleaseCharge(), 1e-3f, "Antippen = Mindestsprung");
            s.BeginCharge(); s.Tick(5f);
            float h = s.ReleaseCharge();
            Assert.AreEqual(15f, h, 1e-3f);
            Assert.GreaterOrEqual(h, ReachabilityModel.SpringerUp, "volle Ladung erreicht jeden Tafelberg");
            Assert.AreEqual(Mathf.Sqrt(2f * 15f * 24f), MountMotor.JumpVelocity(15f, -24f), 1e-3f);
        }

        [Test]
        public void Skimmer_HoversAndIsFastOnWater_EngageDismounts()
        {
            var m = Mounted(MountKind.Skimmer);
            Assert.IsTrue(m.HoversOnWater);
            Assert.Greater(m.GroundSpeed(true), m.GroundSpeed(false));
            Assert.IsFalse(Mounted(MountKind.Raptor).HoversOnWater);
            Assert.IsFalse(m.TryEngage(50f, out float c, out float r), "außer Reichweite");
            Assert.IsTrue(m.TryEngage(5f, out c, out r));
            Assert.Greater(c, 0f); Assert.Greater(r, 0f);
            Assert.IsFalse(m.IsMounted);
            var d = Mounted(MountKind.Raptor);
            Assert.IsTrue(d.TryDodge()); Assert.IsFalse(d.TryDodge(), "Abklingzeit");
        }

        [Test]
        public void XKey_TapTogglesHoldOpensRadial()
        {
            Assert.AreEqual(MountInput.Result.Toggle, MountInput.Evaluate(false, 0.12f, true));
            Assert.AreEqual(MountInput.Result.None, MountInput.Evaluate(true, 0.2f, false));
            Assert.AreEqual(MountInput.Result.OpenRadial, MountInput.Evaluate(true, 0.4f, false));
            Assert.AreEqual(MountInput.Result.None, MountInput.Evaluate(false, 0.6f, true), "Loslassen nach dem Halten wählt im Radialmenü, kein Umschalten");
        }

        [Test]
        public void Shop_RequiresHeartAndGold_UnlocksTraversal()
        {
            var owned = new HashSet<string>(); var done = new HashSet<string>();
            Assert.AreEqual(MountShop.Verdict.HeartMissing, MountShop.Check(MountKind.Raptor, owned, done, "heart_gerstenbrunn", 50000, 20000));
            done.Add("heart_gerstenbrunn");
            Assert.AreEqual(MountShop.Verdict.NotEnoughGold, MountShop.Check(MountKind.Raptor, owned, done, "heart_gerstenbrunn", 19999, 20000));
            Assert.AreEqual(MountShop.Verdict.Ok, MountShop.Check(MountKind.Raptor, owned, done, "heart_gerstenbrunn", 20000, 20000));
            owned.Add("Raptor");
            Assert.AreEqual(MountShop.Verdict.AlreadyOwned, MountShop.Check(MountKind.Raptor, owned, done, "heart_gerstenbrunn", 99999, 20000));
            Assert.AreEqual(Traversal.Raptor, MountShop.Unlocked(owned));
            owned.Add("Skimmer");
            Assert.AreEqual(Traversal.Raptor | Traversal.Skimmer, MountShop.Unlocked(owned));
        }

        [Test]
        public void Save_RoundTripsMounts_AndOldSavesHaveNone()
        {
            var st = new ProfileState();
            st.Mounts.Add("Raptor"); st.Mounts.Add("Springer"); st.SelectedMount = MountKind.Springer;
            var back = new ProfileState();
            back.LoadDto(JsonUtility.FromJson<SaveGameDto>(JsonUtility.ToJson(st.ToDto())), null, null);
            Assert.IsTrue(back.Mounts.SetEquals(st.Mounts));
            Assert.AreEqual(MountKind.Springer, back.SelectedMount);
            var old = new ProfileState();
            old.LoadDto(JsonUtility.FromJson<SaveGameDto>("{\"Level\":10}"), null, null);
            Assert.AreEqual(0, old.Mounts.Count);
            Assert.AreEqual(MountKind.None, old.SelectedMount);
        }
    }
}
