using System.Collections.Generic;
using Game.Data.Rules;
using Game.Data.Save;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class LeaderboardRulesTests
    {
        [Test]
        public void Push_SortsDescAndTruncatesToFive()
        {
            var b = new List<LeaderboardEntry>();
            for (int i = 1; i <= 7; i++) LeaderboardRules.Push(b, score: i * 100, wave: i, date: "2026-07-07");
            Assert.AreEqual(5, b.Count);
            Assert.AreEqual(700, b[0].score);
            Assert.AreEqual(300, b[4].score);
        }
    }
}
