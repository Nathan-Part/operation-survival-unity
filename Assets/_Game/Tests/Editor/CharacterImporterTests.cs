using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class CharacterImporterTests
    {
        const string WeaponsFixture = "Assets/_Game/Tests/Editor/Fixtures/weapons.test.json";
        const string PlayerFixture = "Assets/_Game/Tests/Editor/Fixtures/player.test.json";
        const string WeaponsFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Weapons";
        const string CharactersFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Characters";
        const string UpgradeConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/UpgradeConfig.asset";
        const string MovementConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/MovementConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_CreatesCharacterDataAndMovementConfig()
        {
            var report = new ImportReport();

            var result = CharacterImporter.Import(PlayerFixture, CharactersFolder, MovementConfigPath, UpgradeConfigPath, report);

            Assert.AreEqual(2, result.Characters.Count);
            var cyborg = result.Characters.Find(c => c.id == "perso:robot");
            Assert.AreEqual("Cyborg", cyborg.displayName);
            Assert.AreEqual(Rarity.Epic, cyborg.rarity);
            Assert.AreEqual(175f, cyborg.baseHealth, 0.0001f);
            Assert.AreEqual(1, cyborg.skills.Length);
            Assert.AreEqual("Système optimisé", cyborg.skills[0].name);

            Assert.AreEqual(2.4f, result.MovementConfig.walkSpeed, 0.0001f);
            Assert.AreEqual(2.6f, result.MovementConfig.dash.distance, 0.0001f);
            Assert.AreEqual(100f, result.MovementConfig.stamina.max, 0.0001f);
            Assert.AreEqual(2f, result.MovementConfig.secondWindInvulnSeconds, 0.0001f);
        }

        [Test]
        public void Import_PreservesWeaponUpgradesAlreadyWrittenByWeaponImporter()
        {
            WeaponImporter.Import(WeaponsFixture, WeaponsFolder, UpgradeConfigPath, new ImportReport());

            var result = CharacterImporter.Import(PlayerFixture, CharactersFolder, MovementConfigPath, UpgradeConfigPath, new ImportReport());

            Assert.AreEqual(4, result.UpgradeConfig.weaponUpgrades.Length);
            Assert.AreEqual(3, result.UpgradeConfig.playerUpgrades.Length);
            var speedSpec = System.Array.Find(result.UpgradeConfig.playerUpgrades, s => s.statKey == "speed");
            Assert.AreEqual(0.06f, speedSpec.perLevel, 0.0001f);
        }
    }
}
