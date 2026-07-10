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

        // Surcharge riche (Phase 5.1) : le web soumet kills/headshots/accuracy en plus (main.js:4025).
        [Test]
        public void Push_RichOverload_StoresKillsHeadshotsAccuracy()
        {
            var b = new List<LeaderboardEntry>();
            LeaderboardRules.Push(b, score: 42, wave: 3, date: "2026-07-10", kills: 17, headshots: 5, accuracy: 0.37f);
            Assert.AreEqual(17, b[0].kills);
            Assert.AreEqual(5, b[0].headshots);
            Assert.AreEqual(0.37f, b[0].accuracy);
        }

        // L'ancienne signature à 4 paramètres reste valide (délègue avec des zéros) — les appelants
        // pas encore migrés ne cassent pas et les nouveaux champs restent à leur défaut de migration.
        [Test]
        public void Push_LegacyOverload_DefaultsNewFieldsToZero()
        {
            var b = new List<LeaderboardEntry>();
            LeaderboardRules.Push(b, 42, 3, "2026-07-10");
            Assert.AreEqual(0, b[0].kills);
            Assert.AreEqual(0, b[0].headshots);
            Assert.AreEqual(0f, b[0].accuracy);
        }

        // Le tri/troncature s'applique pareil via la surcharge riche (même chemin de code).
        [Test]
        public void Push_RichOverload_StillSortsAndTruncates()
        {
            var b = new List<LeaderboardEntry>();
            for (int i = 1; i <= 7; i++)
                LeaderboardRules.Push(b, score: i * 100, wave: i, date: "2026-07-10", kills: i, headshots: i, accuracy: 0.5f);
            Assert.AreEqual(5, b.Count);
            Assert.AreEqual(700, b[0].score);
            Assert.AreEqual(7, b[0].kills); // les champs suivent bien leur entrée après tri
        }
    }
}
