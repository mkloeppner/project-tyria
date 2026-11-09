using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    public class DodgeTests
    {
        [Test]
        public void Dodge_GrantsIFrames_ForThreeQuartersOfASecond()
        {
            var go = new GameObject("DodgeTestTarget");
            try
            {
                var c = go.AddComponent<Combatant>();
                c.Team = Team.Player;
                c.SetMaxHealth(1000f, true);
                var dodge = new DodgeModel();
                dodge.Endurance.Fill();
                c.InvulnerabilitySources.Add(dodge);

                Assert.IsTrue(dodge.TryStartDodge(Vector3.forward));
                Assert.IsTrue(dodge.IsInvulnerable);
                float dealt = c.TakeDamage(new DamageInfo { Amount = 300f });
                Assert.AreEqual(0f, dealt, "Schaden während i-Frames muss 0 sein");
                Assert.AreEqual(1000f, c.Health, 0.01f);

                dodge.Tick(0.70f);
                Assert.IsTrue(dodge.IsInvulnerable, "nach 0,70 s noch unverwundbar");
                Assert.AreEqual(0f, c.TakeDamage(new DamageInfo { Amount = 300f }));

                dodge.Tick(0.06f);
                Assert.IsFalse(dodge.IsInvulnerable, "nach 0,76 s nicht mehr unverwundbar");
                Assert.Greater(c.TakeDamage(new DamageInfo { Amount = 300f }), 0f);
                Assert.AreEqual(700f, c.Health, 0.01f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void Endurance_AllowsTwoDodges_ThenRegeneratesInTenSeconds()
        {
            var dodge = new DodgeModel();
            dodge.Endurance.Fill();
            Assert.IsTrue(dodge.TryStartDodge(Vector3.forward));
            dodge.Tick(1f); // Dodge abgeschlossen (+10 Ausdauer)
            dodge.Endurance.Current = 50f;
            Assert.IsTrue(dodge.TryStartDodge(Vector3.forward));
            dodge.Tick(1f);
            dodge.Endurance.Current = 0f;
            Assert.IsFalse(dodge.TryStartDodge(Vector3.forward), "dritter Dodge ohne Ausdauer muss scheitern");
            for (int i = 0; i < 100; i++) dodge.Tick(0.1f);
            Assert.AreEqual(100f, dodge.Endurance.Current, 0.01f);
            Assert.AreEqual(2, dodge.Endurance.Charges);
        }

        [Test]
        public void Dodge_MovesConfiguredDistance()
        {
            var dodge = new DodgeModel();
            dodge.Endurance.Fill();
            dodge.TryStartDodge(Vector3.right);
            Vector3 total = Vector3.zero;
            for (int i = 0; i < 60; i++) total += dodge.Tick(1f / 60f);
            Assert.AreEqual(dodge.DodgeDistance, total.magnitude, 0.05f);
            Assert.Greater(total.x, 0f);
        }
    }
}
