using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class UpgradeFormulasTests
    {
        [Test]
        public void Cost_ScalesWithLevel()
        {
            Assert.AreEqual(300, UpgradeFormulas.Cost(300, 0));
            Assert.AreEqual(900, UpgradeFormulas.Cost(300, 2));
        }

        [Test]
        public void WeaponDamageMult_AppliesFifteenPercentPerLevel()
        {
            Assert.AreEqual(1.0f, UpgradeFormulas.WeaponDamageMult(1.0f, 0), 0.0001f);
            Assert.AreEqual(1.45f, UpgradeFormulas.WeaponDamageMult(1.0f, 3), 0.0001f);
        }

        [Test]
        public void WeaponCooldown_ReducesEightPercentPerLevel()
        {
            Assert.AreEqual(0.12f, UpgradeFormulas.WeaponCooldown(0.12f, 0), 0.0001f);
            Assert.AreEqual(0.12f * 0.6f, UpgradeFormulas.WeaponCooldown(0.12f, 5), 0.0001f);
        }

        [Test]
        public void WeaponMag_RoundsToNearestInt()
        {
            Assert.AreEqual(12, UpgradeFormulas.WeaponMag(12, 0));
            Assert.AreEqual(14, UpgradeFormulas.WeaponMag(12, 1));
        }
    }
}
