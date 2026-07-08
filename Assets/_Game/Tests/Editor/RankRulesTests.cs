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

        [Test]
        public void OperatorRank_DerivedFromBestWave()
        {
            Assert.AreEqual(1, RankRules.OperatorRank(0));
            Assert.AreEqual(1, RankRules.OperatorRank(1));
            Assert.AreEqual(2, RankRules.OperatorRank(2));
            Assert.AreEqual(4, RankRules.OperatorRank(7));
        }

        [Test]
        public void OperatorRankFill_HalfStepPerWave()
        {
            Assert.AreEqual(0f, RankRules.OperatorRankFill(0));
            Assert.AreEqual(0.5f, RankRules.OperatorRankFill(1));
            Assert.AreEqual(0f, RankRules.OperatorRankFill(2));
            Assert.AreEqual(0.5f, RankRules.OperatorRankFill(7));
        }
    }
}
