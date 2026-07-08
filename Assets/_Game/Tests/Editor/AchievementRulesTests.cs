using Game.Data;
using Game.Data.Rules;
using Game.Data.Save;
using NUnit.Framework;
using UnityEngine;

namespace Game.Data.Tests
{
    public class AchievementRulesTests
    {
        static AchievementData A(string type, int t)
        {
            var a = ScriptableObject.CreateInstance<AchievementData>();
            a.conditionType = type;
            a.target = t;
            return a;
        }

        [Test]
        public void RunKills_UnlocksAtTarget()
        {
            var run = new RunStats { runKills = 1 };
            Assert.IsTrue(AchievementRules.IsUnlocked(A("run_kills", 1), run, new SaveData()));
            Assert.IsFalse(AchievementRules.IsUnlocked(A("run_kills", 2), run, new SaveData()));
        }

        [Test]
        public void RunHeadshots_UnlocksAtTarget()
        {
            var run = new RunStats { runHeadshots = 5 };
            Assert.IsTrue(AchievementRules.IsUnlocked(A("run_headshots", 5), run, new SaveData()));
            Assert.IsFalse(AchievementRules.IsUnlocked(A("run_headshots", 6), run, new SaveData()));
        }

        [Test]
        public void Wave_UnlocksAtTarget()
        {
            var run = new RunStats { wave = 10 };
            Assert.IsTrue(AchievementRules.IsUnlocked(A("wave", 10), run, new SaveData()));
            Assert.IsFalse(AchievementRules.IsUnlocked(A("wave", 11), run, new SaveData()));
        }

        [Test]
        public void Combo_UnlocksAtTarget()
        {
            var run = new RunStats { combo = 5 };
            Assert.IsTrue(AchievementRules.IsUnlocked(A("combo", 5), run, new SaveData()));
        }

        [Test]
        public void Money_ReadsFromSave()
        {
            var save = new SaveData { money = 5000 };
            Assert.IsTrue(AchievementRules.IsUnlocked(A("money", 5000), new RunStats(), save));
            Assert.IsFalse(AchievementRules.IsUnlocked(A("money", 5001), new RunStats(), save));
        }

        [Test]
        public void Level_ReadsFromSaveXp()
        {
            var save = new SaveData { xp = 0 };
            Assert.IsTrue(AchievementRules.IsUnlocked(A("level", 1), new RunStats(), save));
            Assert.IsFalse(AchievementRules.IsUnlocked(A("level", 2), new RunStats(), save));
        }

        [Test]
        public void OwnedWeapons_CountsList()
        {
            var save = new SaveData();
            for (int i = 0; i < 10; i++) save.ownedWeapons.Add("w" + i);
            Assert.IsTrue(AchievementRules.IsUnlocked(A("owned_weapons", 10), new RunStats(), save));
        }

        [Test]
        public void LifetimeKills_ReadsFromSaveLifetime()
        {
            var save = new SaveData();
            save.lifetime.kills = 500;
            Assert.IsTrue(AchievementRules.IsUnlocked(A("lifetime_kills", 500), new RunStats(), save));
            Assert.IsFalse(AchievementRules.IsUnlocked(A("lifetime_kills", 501), new RunStats(), save));
        }

        [Test]
        public void LifetimeHeadshots_ReadsFromSaveLifetime()
        {
            var save = new SaveData();
            save.lifetime.headshots = 100;
            Assert.IsTrue(AchievementRules.IsUnlocked(A("lifetime_headshots", 100), new RunStats(), save));
        }

        [Test]
        public void FlagConditions_ReadFromRunFlags()
        {
            var save = new SaveData();
            Assert.IsFalse(AchievementRules.IsUnlocked(A("flag_miracle", 1), new RunStats(), save));
            Assert.IsTrue(AchievementRules.IsUnlocked(A("flag_miracle", 1), new RunStats { miracle = true }, save));
            Assert.IsTrue(AchievementRules.IsUnlocked(A("flag_legendary_weapon", 1), new RunStats { legendaryWeapon = true }, save));
            Assert.IsTrue(AchievementRules.IsUnlocked(A("flag_silent_clear", 1), new RunStats { silentClear = true }, save));
            Assert.IsTrue(AchievementRules.IsUnlocked(A("flag_legendary_cosmetics", 1), new RunStats { legendaryCosmetics = true }, save));
        }

        [Test]
        public void UnknownConditionType_NeverUnlocks()
        {
            Assert.IsFalse(AchievementRules.IsUnlocked(A("something_unmapped", 0), new RunStats(), new SaveData()));
        }
    }
}
