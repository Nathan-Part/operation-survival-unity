using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class StatFormulasTests
    {
        [Test]
        public void MaxHealth_AddsUpgradeAndBonus()
        {
            Assert.AreEqual(100f, StatFormulas.MaxHealth(100f, 0, 0f), 0.0001f);
            Assert.AreEqual(160f, StatFormulas.MaxHealth(100f, 2, 20f), 0.0001f);
        }

        [Test]
        public void RegenRate_AddsUpgradeAndBonus()
        {
            Assert.AreEqual(10f, StatFormulas.RegenRate(10f, 0, 0f), 0.0001f);
            Assert.AreEqual(19f, StatFormulas.RegenRate(10f, 2, 3f), 0.0001f);
        }

        [Test]
        public void SpeedMult_AppliesPercentBonusToBase()
        {
            Assert.AreEqual(1.0f, StatFormulas.SpeedMult(1.0f, 0, 0f), 0.0001f);
            Assert.AreEqual(1.18f, StatFormulas.SpeedMult(1.0f, 2, 0.06f), 0.0001f);
        }
    }
}
