using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class EnemyImporterTests
    {
        const string WeaponsFixture = "Assets/_Game/Tests/Editor/Fixtures/weapons.test.json";
        const string EnemiesFixture = "Assets/_Game/Tests/Editor/Fixtures/enemies.test.json";
        const string WeaponsFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Weapons";
        const string EnemiesFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Enemies";
        const string UpgradeConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/UpgradeConfig.asset";
        const string CombatConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/EnemyCombatConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_CreatesEnemyDataAndResolvesWeaponRef()
        {
            WeaponImporter.Import(WeaponsFixture, WeaponsFolder, UpgradeConfigPath, new ImportReport());
            var report = new ImportReport();

            var result = EnemyImporter.Import(EnemiesFixture, EnemiesFolder, WeaponsFolder, CombatConfigPath, report);

            Assert.AreEqual(3, result.Enemies.Count);
            var tank = result.Enemies.Find(e => e.id == "tank");
            Assert.IsNotNull(tank.weapon);
            Assert.AreEqual("sniper", tank.weapon.id);
            Assert.AreEqual(ArmorType.Torso, tank.armor);
            Assert.AreEqual(2.6f, tank.hpMult, 0.0001f);

            Assert.AreEqual(60, result.CombatConfig.hitZones.headDamage);
            Assert.AreEqual(1.5f, result.CombatConfig.hitZones.headAboveY, 0.0001f);
        }

        [Test]
        public void Import_DefaultsMissingMultipliersAndWarns()
        {
            WeaponImporter.Import(WeaponsFixture, WeaponsFolder, UpgradeConfigPath, new ImportReport());
            var report = new ImportReport();

            var result = EnemyImporter.Import(EnemiesFixture, EnemiesFolder, WeaponsFolder, CombatConfigPath, report);

            var ghost = result.Enemies.Find(e => e.id == "ghost");
            Assert.AreEqual(1.0f, ghost.hpMult, 0.0001f);
            Assert.AreEqual(1.0f, ghost.speedMult, 0.0001f);
            Assert.IsNull(ghost.weapon);
            Assert.IsTrue(report.Warnings.Exists(w => w.Contains("ghost")));
        }
    }
}
