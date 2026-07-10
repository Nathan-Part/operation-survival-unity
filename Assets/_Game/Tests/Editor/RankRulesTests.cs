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

        [Test]
        public void ComboMultiplier_CapsAtFive()
        {
            Assert.AreEqual(1, RankRules.ComboMultiplier(0));
            Assert.AreEqual(1, RankRules.ComboMultiplier(2));
            Assert.AreEqual(2, RankRules.ComboMultiplier(3));
            Assert.AreEqual(3, RankRules.ComboMultiplier(6));
            Assert.AreEqual(5, RankRules.ComboMultiplier(12));
            Assert.AreEqual(5, RankRules.ComboMultiplier(100)); // cap à 5 au-delà
        }

        // Rang de fin de partie (Phase 5.1) : score = kills×3 + précision×100×2 + vague×10 (main.js:2588).
        [Test]
        public void RunRankScore_WebFormula()
        {
            Assert.AreEqual(0, RankRules.RunRankScore(0, 0f, 0));
            Assert.AreEqual(95, RankRules.RunRankScore(5, 0.25f, 3));  // 15 + 50 + 30
            Assert.AreEqual(230, RankRules.RunRankScore(20, 0.5f, 7)); // 60 + 100 + 70
        }

        // Seuils exacts S≥200 / A≥120 / B≥70 / C≥30 / D (main.js:2596-2603) — testés PILE à la borne
        // (via vague×10, entier exact, pas de flou flottant).
        [Test]
        public void RunRank_ThresholdsExact()
        {
            Assert.AreEqual('D', RankRules.RunRank(0, 0f, 0));  // 0
            Assert.AreEqual('D', RankRules.RunRank(0, 0f, 2));  // 20 < 30
            Assert.AreEqual('C', RankRules.RunRank(0, 0f, 3));  // 30 pile
            Assert.AreEqual('C', RankRules.RunRank(0, 0f, 6));  // 60 < 70
            Assert.AreEqual('B', RankRules.RunRank(0, 0f, 7));  // 70 pile
            Assert.AreEqual('A', RankRules.RunRank(0, 0f, 12)); // 120 pile
            Assert.AreEqual('S', RankRules.RunRank(0, 0f, 20)); // 200 pile
        }
    }
}
