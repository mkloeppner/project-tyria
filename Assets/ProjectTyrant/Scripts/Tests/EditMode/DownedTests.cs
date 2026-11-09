using NUnit.Framework;
using UnityEngine;

namespace ProjectTyrant.Tests
{
    public class DownedTests
    {
        GameObject _playerGo, _enemyGo;
        Combatant _player, _enemy;
        DownedController _downed;

        [SetUp]
        public void Setup()
        {
            GameEventBus.ClearAll();
            _playerGo = new GameObject("Player");
            _player = _playerGo.AddComponent<Combatant>();
            _player.Team = Team.Player;
            _player.SetMaxHealth(10000f, true);
            _downed = _playerGo.AddComponent<DownedController>();
            _downed.Init();
            _enemyGo = new GameObject("Enemy");
            _enemy = _enemyGo.AddComponent<Combatant>();
            _enemy.Team = Team.Enemy;
            _enemy.SetMaxHealth(500f, true);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_playerGo);
            Object.DestroyImmediate(_enemyGo);
            GameEventBus.ClearAll();
        }

        [Test]
        public void ZeroHealth_EntersDownedState_InsteadOfDying()
        {
            _player.TakeDamage(new DamageInfo { Source = _enemy, Amount = 20000f });
            Assert.IsTrue(_downed.IsDowned);
            Assert.IsFalse(_player.IsDead);
            Assert.AreEqual(30f, _downed.TimeRemaining, 0.01f);
        }

        [Test]
        public void Downed_Then_KillingEnemy_Rallies()
        {
            bool rallied = false;
            GameEventBus.PlayerRallied += c => rallied = true;
            _player.TakeDamage(new DamageInfo { Source = _enemy, Amount = 20000f });
            Assert.IsTrue(_downed.IsDowned);

            // Spieler tötet den Gegner im Downed State
            _enemy.TakeDamage(new DamageInfo { Source = _player, Amount = 1000f });
            Assert.IsTrue(_enemy.IsDead);
            Assert.IsFalse(_downed.IsDowned, "Rally bei Kill");
            Assert.IsTrue(rallied);
            Assert.AreEqual(10000f * _downed.RallyHealthFraction, _player.Health, 0.5f);
        }

        [Test]
        public void Downed_RalliesWhenDamagedEnemyIsKilledByAlly()
        {
            _player.TakeDamage(new DamageInfo { Source = _enemy, Amount = 20000f });
            _enemy.TakeDamage(new DamageInfo { Source = _player, Amount = 100f });
            Assert.IsTrue(_downed.IsDowned);
            _enemy.Die(null);
            Assert.IsFalse(_downed.IsDowned);
        }

        [Test]
        public void Downed_DiesAfterThirtySeconds()
        {
            _player.TakeDamage(new DamageInfo { Source = _enemy, Amount = 20000f });
            _downed.Tick(29.9f);
            Assert.IsTrue(_downed.IsDowned);
            _downed.Tick(0.2f);
            Assert.IsFalse(_downed.IsDowned);
            Assert.IsTrue(_player.IsDead);
        }

        [Test]
        public void Downed_DiesAtZeroDownedHealth()
        {
            _player.TakeDamage(new DamageInfo { Source = _enemy, Amount = 20000f });
            _player.TakeDamage(new DamageInfo { Source = _enemy, Amount = _downed.DownedMaxHealth + 1f });
            Assert.IsTrue(_player.IsDead);
        }

        [Test]
        public void Bandage_ToFull_Rallies()
        {
            _player.TakeDamage(new DamageInfo { Source = _enemy, Amount = 20000f });
            _player.TakeDamage(new DamageInfo { Source = _enemy, Amount = 2000f });
            _downed.Bandage(1000f);
            Assert.IsTrue(_downed.IsDowned);
            _downed.Bandage(1000f);
            Assert.IsFalse(_downed.IsDowned);
        }
    }

    public class NpcBrainTests
    {
        [Test]
        public void Brain_GoesFromIdleToAggro_WhenDamaged_AndTelegraphIsDodgeable()
        {
            GameEventBus.ClearAll();
            var arch = ScriptableObject.CreateInstance<EnemyArchetype>();
            arch.MaxHealth = 5000; arch.AggroRange = 0.1f; arch.Team = Team.Enemy;
            var eGo = new GameObject("Enemy"); var pGo = new GameObject("Player");
            try
            {
                eGo.AddComponent<Combatant>();
                var brain = eGo.AddComponent<NpcBrain>();
                brain.Archetype = arch; brain.Init(); brain.ApplyArchetype();
                var p = pGo.AddComponent<Combatant>(); p.Team = Team.Player; p.SetMaxHealth(10000, true);
                pGo.transform.position = new Vector3(50, 0, 0);
                Assert.AreEqual(NpcState.Idle, brain.State);
                brain.Self.TakeDamage(new DamageInfo { Source = p, Amount = 10 });
                Assert.AreEqual(NpcState.Aggro, brain.State);
                Assert.AreSame(p, brain.Target);

                // Telegraph: Spieler mit i-Frames nimmt keinen Schaden
                var dodge = new DodgeModel(); dodge.Endurance.Fill(); p.InvulnerabilitySources.Add(dodge);
                pGo.transform.position = Vector3.zero;
                var tel = GroundTelegraph.Spawn(brain.Self, Vector3.zero, 5f, 1.5f, 3000f);
                Combatant.All.Clear(); Combatant.All.Add(p); Combatant.All.Add(brain.Self);
                tel.Tick(1.0f);
                dodge.TryStartDodge(Vector3.right);
                tel.Tick(0.6f);
                Assert.IsTrue(tel.Resolved);
                Assert.AreEqual(10000f, p.Health, 0.01f, "Dodge vermeidet Telegraph-Schaden");

                var tel2 = GroundTelegraph.Spawn(brain.Self, Vector3.zero, 5f, 1.5f, 3000f);
                dodge.Tick(2f);
                tel2.Tick(1.6f);
                Assert.AreEqual(7000f, p.Health, 0.01f, "ohne Dodge trifft der Telegraph");
                Object.DestroyImmediate(tel.gameObject); Object.DestroyImmediate(tel2.gameObject);
            }
            finally
            {
                Combatant.All.Clear();
                Object.DestroyImmediate(eGo); Object.DestroyImmediate(pGo);
            }
        }
    }
}
