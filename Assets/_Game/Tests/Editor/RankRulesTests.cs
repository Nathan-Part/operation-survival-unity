using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class RankRulesTests
    {
        [Test]
        public void Titles_ByFiveLevelBands()
        {
            Assert.AreEqual("Recrue", RankRules.TitleFor(1));
            Assert.AreEqual("Soldat", RankRules.TitleFor(6));
            Assert.AreEqual("Légende", RankRules.TitleFor(46));
            Assert.AreEqual("Légende", RankRules.TitleFor(50)); // cap tranche 9
        }

        [Test]
        public void CanPrestige_AtFifty()
        {
            Assert.IsFalse(RankRules.CanPrestige(49));
            Assert.IsTrue(RankRules.CanPrestige(50));
        }
    }
}
