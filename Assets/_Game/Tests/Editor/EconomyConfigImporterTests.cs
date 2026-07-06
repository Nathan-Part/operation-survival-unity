using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class EconomyConfigImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/economy.test.json";
        const string ConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/EconomyConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_MapsMoneyXpDifficultiesRaritiesAndLoadout()
        {
            var config = EconomyConfigImporter.Import(Fixture, ConfigPath, new ImportReport());

            Assert.AreEqual(10, config.perKillMoney);
            Assert.AreEqual(2, config.vipMoneyMult);
            Assert.AreEqual(300, config.bossKillBonus);
            Assert.AreEqual(50, config.maxLevel);
            Assert.AreEqual(5, config.leaderboardMaxEntries);

            Assert.AreEqual(3, config.difficulties.Length);
            var hard = System.Array.Find(config.difficulties, d => d.id == "difficile");
            Assert.AreEqual(1.4f, hard.enemyHpMult, 0.0001f);

            Assert.AreEqual(4, config.rarities.Length);
            var epic = System.Array.Find(config.rarities, r => r.rarity == Rarity.Epic);
            Assert.AreEqual(2, epic.tier);

            Assert.AreEqual(1800, config.loadoutThresholds.airstrike);
        }
    }
}
