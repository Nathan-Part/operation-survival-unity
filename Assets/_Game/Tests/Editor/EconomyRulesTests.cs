using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class EconomyRulesTests
    {
        [Test]
        public void ComboMultiplier_CapsAtFive()
        {
            Assert.AreEqual(1, EconomyRules.ComboMultiplier(0));
            Assert.AreEqual(2, EconomyRules.ComboMultiplier(3));
            Assert.AreEqual(5, EconomyRules.ComboMultiplier(30));
        }

        [Test]
        public void MoneyForKill_AppliesComboDifficultyAndVip()
        {
            Assert.AreEqual(10, EconomyRules.MoneyForKill(10, 1, 1.0f, false, 2));
            Assert.AreEqual(20, EconomyRules.MoneyForKill(10, 1, 1.0f, true, 2));
            Assert.AreEqual(9, EconomyRules.MoneyForKill(10, 1, 0.9f, false, 2));
        }

        [Test]
        public void XpForLevel_MatchesTriangularFormula()
        {
            Assert.AreEqual(100, EconomyRules.XpForLevel(1));
            Assert.AreEqual(300, EconomyRules.XpForLevel(2));
            Assert.AreEqual(600, EconomyRules.XpForLevel(3));
        }
    }
}
