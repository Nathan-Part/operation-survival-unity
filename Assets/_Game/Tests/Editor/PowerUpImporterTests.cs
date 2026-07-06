using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class PowerUpImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/powerups.test.json";
        const string PowerUpsFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/PowerUps";
        const string ConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/PowerUpConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_CreatesPowerUpDataAndConfig()
        {
            var result = PowerUpImporter.Import(Fixture, PowerUpsFolder, ConfigPath, new ImportReport());

            Assert.AreEqual(3, result.PowerUps.Count);
            var shield = result.PowerUps.Find(p => p.id == "shield_orb");
            Assert.IsTrue(shield.isInstant);
            Assert.AreEqual(PvpPolicy.Allowed, shield.pvpPolicy);
            var dmgx2 = result.PowerUps.Find(p => p.id == "dmgx2");
            Assert.AreEqual(8f, dmgx2.duration, 0.0001f);
            Assert.AreEqual(PvpPolicy.Rebalance, dmgx2.pvpPolicy);

            Assert.AreEqual(0.12f, result.Config.dropChance, 0.0001f);
            Assert.AreEqual(0.24f, result.Config.dropChanceLootMaster, 0.0001f);
            Assert.AreEqual(80, result.Config.novaDamage);
        }
    }
}
