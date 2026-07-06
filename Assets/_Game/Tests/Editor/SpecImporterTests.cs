using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class SpecImporterTests
    {
        const string SpecRoot = "Assets/_Game/Tests/Editor/Fixtures";
        const string DataRoot = "Assets/_Game/Tests/Editor/Fixtures/GeneratedDatabase";

        static readonly SpecFileNames TestFileNames = new SpecFileNames
        {
            Weapons = "weapons.test.json",
            Enemies = "enemies.test.json",
            Player = "player.test.json",
            Economy = "economy.test.json",
            Items = "items.test.json",
            PowerUps = "powerups.test.json",
            Waves = "waves.test.json"
        };

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(DataRoot);
        }

        [Test]
        public void RunImport_BuildsGameDatabaseFromAllDomains()
        {
            var report = new ImportReport();

            var db = SpecImporter.RunImport(SpecRoot, DataRoot, TestFileNames, report);

            Assert.AreEqual(4, db.weapons.Length);
            Assert.AreEqual(3, db.enemies.Length);
            Assert.AreEqual(2, db.characters.Length);
            Assert.AreEqual(3, db.powerUps.Length);
            Assert.AreEqual(3, db.accessories.Length);
            Assert.AreEqual(2, db.boosts.Length);
            Assert.AreEqual(2, db.consumables.Length);
            Assert.AreEqual(2, db.dances.Length);

            Assert.IsNotNull(db.movementConfig);
            Assert.IsNotNull(db.enemyCombatConfig);
            Assert.IsNotNull(db.waveConfig);
            Assert.IsNotNull(db.economyConfig);
            Assert.IsNotNull(db.upgradeConfig);
            Assert.IsNotNull(db.powerUpConfig);
            Assert.AreEqual(4, db.upgradeConfig.weaponUpgrades.Length);
            Assert.AreEqual(3, db.upgradeConfig.playerUpgrades.Length);

            var tank = System.Array.Find(db.enemies, e => e.id == "tank");
            Assert.IsNotNull(tank.weapon);
        }

        [Test]
        public void RunImport_SecondRunIsIdempotent()
        {
            SpecImporter.RunImport(SpecRoot, DataRoot, TestFileNames, new ImportReport());

            var secondReport = new ImportReport();
            SpecImporter.RunImport(SpecRoot, DataRoot, TestFileNames, secondReport);

            Assert.AreEqual(0, secondReport.Created.Count);
        }
    }
}
