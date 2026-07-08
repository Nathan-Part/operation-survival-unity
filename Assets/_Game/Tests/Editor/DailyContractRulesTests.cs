using System.Collections.Generic;
using Game.Data;
using Game.Data.Rules;
using Game.Data.Save;
using NUnit.Framework;
using UnityEngine;

namespace Game.Data.Tests
{
    public class DailyContractRulesTests
    {
        static List<ContractData> Pool()
        {
            var l = new List<ContractData>();
            void C(string id, string t, int tg, int r)
            {
                var c = ScriptableObject.CreateInstance<ContractData>();
                c.id = id; c.type = t; c.target = tg; c.reward = r;
                l.Add(c);
            }
            C("kills50", "kills", 50, 800);
            C("heads10", "headshots", 10, 600);
            C("wave5", "wave", 5, 900);
            C("melee5", "melee", 5, 500);
            return l;
        }

        static List<ContractData> SingleKillsContract(int target, int reward)
        {
            var c = ScriptableObject.CreateInstance<ContractData>();
            c.id = "kills50"; c.type = "kills"; c.target = target; c.reward = reward;
            return new List<ContractData> { c };
        }

        [Test]
        public void Seed_IsDeterministic() => Assert.AreEqual(20260707L, DailyContractRules.SeedFor(2026, 7, 7));

        [Test]
        public void Pick_IsDeterministicAndSized()
        {
            var a = DailyContractRules.Pick(20260707L, Pool(), 3);
            var b = DailyContractRules.Pick(20260707L, Pool(), 3);
            Assert.AreEqual(3, a.Count);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void Pick_NeverDuplicatesFromPool()
        {
            var picked = DailyContractRules.Pick(20260707L, Pool(), 3);
            Assert.AreEqual(picked.Count, new HashSet<string>(picked).Count);
        }

        [Test]
        public void EnsureToday_FillsContractsFromSeed()
        {
            var daily = new SaveDaily();
            DailyContractRules.EnsureToday(daily, 20260707L, Pool());

            Assert.AreEqual(3, daily.contracts.Count);
            Assert.AreEqual(20260707L, daily.daily_seed);
        }

        [Test]
        public void EnsureToday_KeepsProgressWhenSeedUnchanged()
        {
            var daily = new SaveDaily();
            DailyContractRules.EnsureToday(daily, 20260707L, Pool());
            daily.contracts[0].progress = 5;

            DailyContractRules.EnsureToday(daily, 20260707L, Pool());

            Assert.AreEqual(5, daily.contracts[0].progress);
        }

        [Test]
        public void EnsureToday_ResetsContractsOnNewDay()
        {
            var daily = new SaveDaily();
            DailyContractRules.EnsureToday(daily, 20260707L, Pool());
            daily.contracts[0].progress = 5;

            DailyContractRules.EnsureToday(daily, 20260708L, Pool());

            Assert.AreEqual(20260708L, daily.daily_seed);
            Assert.AreEqual(0, daily.contracts[0].progress);
        }

        [Test]
        public void Advance_CompletesContract_ReturnsItWithReward()
        {
            var pool = SingleKillsContract(target: 3, reward: 800);
            var daily = new SaveDaily();
            DailyContractRules.EnsureToday(daily, 20260707L, pool);

            Assert.IsNull(DailyContractRules.Advance(daily, pool, "kills", 1));
            Assert.IsNull(DailyContractRules.Advance(daily, pool, "kills", 1));
            var done = DailyContractRules.Advance(daily, pool, "kills", 1);

            Assert.IsNotNull(done);
            Assert.AreEqual("kills50", done.id);
            Assert.AreEqual(800, done.reward);
            Assert.IsTrue(daily.contracts[0].done);
        }

        [Test]
        public void Advance_IgnoresWrongType()
        {
            var pool = SingleKillsContract(target: 3, reward: 800);
            var daily = new SaveDaily();
            DailyContractRules.EnsureToday(daily, 20260707L, pool);

            Assert.IsNull(DailyContractRules.Advance(daily, pool, "headshots", 5));
            Assert.AreEqual(0, daily.contracts[0].progress);
        }

        [Test]
        public void Advance_DoesNotRetriggerAlreadyCompletedContract()
        {
            var pool = SingleKillsContract(target: 1, reward: 800);
            var daily = new SaveDaily();
            DailyContractRules.EnsureToday(daily, 20260707L, pool);

            var done = DailyContractRules.Advance(daily, pool, "kills", 1);
            Assert.IsNotNull(done);

            var doneAgain = DailyContractRules.Advance(daily, pool, "kills", 1);
            Assert.IsNull(doneAgain);
            Assert.AreEqual(1, daily.contracts[0].progress);
        }
    }
}
