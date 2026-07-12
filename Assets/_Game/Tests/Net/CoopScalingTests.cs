using Game.Net;
using NUnit.Framework;

namespace Game.Net.Tests
{
    public class CoopScalingTests
    {
        CoopSessionConfig cfg;

        [SetUp]
        public void SetUp()
        {
            cfg = new CoopSessionConfig
            {
                enemyHpPerExtraPlayer = 0.35f,
                enemyCountPerExtraPlayer = 1f,
                maxExtraEnemiesFromPlayers = 6,
            };
        }

        [Test]
        public void SinglePlayer_NoScaling()
        {
            Assert.AreEqual(1f, CoopScaling.EnemyHpMult(1, cfg), 1e-4f);
            Assert.AreEqual(0, CoopScaling.EnemyCountBonus(1, cfg));
        }

        [Test]
        public void FourPlayers_HpScalesLinearly()
        {
            // 1 + (4-1) * 0.35 = 2.05
            Assert.AreEqual(2.05f, CoopScaling.EnemyHpMult(4, cfg), 1e-4f);
        }

        [Test]
        public void FourPlayers_CountBonusIsExtraPlayers()
        {
            Assert.AreEqual(3, CoopScaling.EnemyCountBonus(4, cfg));
        }

        [Test]
        public void CountBonus_IsCapped()
        {
            cfg.enemyCountPerExtraPlayer = 5f;
            cfg.maxExtraEnemiesFromPlayers = 6;
            Assert.AreEqual(6, CoopScaling.EnemyCountBonus(4, cfg)); // 3*5=15 -> plafonné à 6
        }

        [Test]
        public void ZeroOrNegativePlayers_TreatedAsOne()
        {
            Assert.AreEqual(1f, CoopScaling.EnemyHpMult(0, cfg), 1e-4f);
            Assert.AreEqual(0, CoopScaling.EnemyCountBonus(0, cfg));
        }
    }
}
