using System;
using System.Collections.Generic;
using Game.Net;
using NUnit.Framework;

namespace Game.Net.Tests
{
    public class CoopWaveDirectorTests
    {
        CoopSessionConfig cfg;
        Func<int, string[]> plan3;
        Func<string, float> hp1;

        [SetUp]
        public void SetUp()
        {
            cfg = new CoopSessionConfig
            {
                intermissionSeconds = 1f,
                baseEnemyHpMult = 1f,
                enemyHpPerExtraPlayer = 0.35f,
                enemyCountPerExtraPlayer = 1f,
                maxExtraEnemiesFromPlayers = 6,
            };
            plan3 = w => new[] { "shooter", "shooter", "shooter" };
            hp1 = id => 1f;
        }

        CoopWaveDirector Make(int players) => new CoopWaveDirector(cfg, plan3, hp1, players);

        [Test]
        public void Begin_EntersIntermissionBeforeWaveOne()
        {
            var d = Make(1);
            d.Begin();
            Assert.AreEqual(WaveState.Intermission, d.State);
            Assert.AreEqual(0, d.CurrentWave);
        }

        [Test]
        public void AfterIntermission_SpawnsWaveOne()
        {
            var d = Make(1);
            d.Begin();
            var spawns = d.Tick(1.1f);
            Assert.AreEqual(1, d.CurrentWave);
            Assert.AreEqual(WaveState.Fighting, d.State);
            Assert.AreEqual(3, spawns.Count);
            Assert.AreEqual(3, d.AliveCount);
        }

        [Test]
        public void SpawnedEnemies_HaveUniqueIncreasingNetIds()
        {
            var d = Make(1);
            d.Begin();
            var spawns = d.Tick(1.1f);
            var ids = new HashSet<int>();
            foreach (var s in spawns) Assert.IsTrue(ids.Add(s.netId), "netId dupliqué");
            Assert.AreEqual(3, ids.Count);
        }

        [Test]
        public void EnemyHp_UsesWaveRulesTimesScaling()
        {
            // WaveRules.EnemyHpForWave(1, 1) = 100 ; 1 joueur -> mult 1 -> 100
            var d = Make(1);
            d.Begin();
            var spawns = d.Tick(1.1f);
            Assert.AreEqual(100f, spawns[0].maxHealth, 1e-3f);
        }

        [Test]
        public void ClearingAllEnemies_AdvancesToNextWave()
        {
            var d = Make(1);
            d.Begin();
            var spawns = d.Tick(1.1f);
            foreach (var s in spawns) d.NotifyEnemyKilled(s.netId);
            Assert.AreEqual(0, d.AliveCount);

            var none = d.Tick(0f);      // détecte le nettoyage -> repart en intermission
            Assert.AreEqual(0, none.Count);
            Assert.AreEqual(WaveState.Intermission, d.State);

            var wave2 = d.Tick(1.1f);   // intermission écoulée -> vague 2
            Assert.AreEqual(2, d.CurrentWave);
            Assert.AreEqual(3, wave2.Count);
        }

        [Test]
        public void MorePlayers_AddBonusEnemies()
        {
            var d = Make(3);            // bonus = 2
            d.Begin();
            var spawns = d.Tick(1.1f);
            Assert.AreEqual(5, spawns.Count);
        }

        [Test]
        public void WaveStartedEvent_FiresWithWaveNumber()
        {
            var d = Make(1);
            int started = -1;
            d.WaveStarted += w => started = w;
            d.Begin();
            d.Tick(1.1f);
            Assert.AreEqual(1, started);
        }
    }
}
