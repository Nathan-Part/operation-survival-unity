using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class WaveRulesTests
    {
        [Test]
        public void IsBossWave_TrueOnMultiplesOfFive()
        {
            Assert.IsTrue(WaveRules.IsBossWave(5));
            Assert.IsTrue(WaveRules.IsBossWave(10));
            Assert.IsFalse(WaveRules.IsBossWave(4));
        }

        [Test]
        public void EnemiesForWave_CapsAtSevenExceptBossWaves()
        {
            Assert.AreEqual(3, WaveRules.EnemiesForWave(1));
            Assert.AreEqual(3, WaveRules.EnemiesForWave(5));
            Assert.AreEqual(7, WaveRules.EnemiesForWave(6));
            Assert.AreEqual(7, WaveRules.EnemiesForWave(9));
            Assert.AreEqual(3, WaveRules.EnemiesForWave(10));
        }

        [Test]
        public void EnemyHpForWave_MatchesReferenceValues()
        {
            Assert.AreEqual(100, WaveRules.EnemyHpForWave(1, 1.0f));
            Assert.AreEqual(194, WaveRules.EnemyHpForWave(5, 1.0f));
            Assert.AreEqual(135, WaveRules.EnemyHpForWave(1, 1.35f));
        }

        [Test]
        public void WaveScoreMult_IncreasesEveryFiveWaves()
        {
            Assert.AreEqual(1f, WaveRules.WaveScoreMult(4));
            Assert.AreEqual(1.5f, WaveRules.WaveScoreMult(5));
            Assert.AreEqual(1.5f, WaveRules.WaveScoreMult(9));
            Assert.AreEqual(2f, WaveRules.WaveScoreMult(10));
        }

        [Test]
        public void WaveMoneyBonus_RoundsToNearestInt()
        {
            Assert.AreEqual(25, WaveRules.WaveMoneyBonus(1, 1.0f));   // 25.0 exact
            Assert.AreEqual(33, WaveRules.WaveMoneyBonus(1, 1.33f));  // 33.25 -> 33 (rounds down)
            Assert.AreEqual(34, WaveRules.WaveMoneyBonus(1, 1.35f));  // 33.75 -> 34 (rounds up)
        }
    }
}
