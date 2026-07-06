using System.IO;
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Data.Tests
{
    public class WeaponImporterTests
    {
        const string FixturePath = "Assets/_Game/Tests/Editor/Fixtures/weapons.test.json";
        const string OutputFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Weapons";
        const string UpgradeConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/UpgradeConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_CreatesWeaponDataWithMappedFields()
        {
            var report = new ImportReport();

            var result = WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, report);

            Assert.AreEqual(4, result.Weapons.Count);
            var jackal = result.Weapons.Find(w => w.id == "jackal");
            Assert.IsNotNull(jackal);
            Assert.AreEqual("Jackal-45", jackal.displayName);
            Assert.AreEqual(WeaponCategory.Pistol, jackal.category);
            Assert.AreEqual(Rarity.Rare, jackal.rarity);
            Assert.IsTrue(jackal.semiAuto);
            Assert.AreEqual(12, jackal.mag);
            Assert.AreEqual(1.3f, jackal.dmgMult, 0.0001f);
            Assert.AreEqual(4, report.Created.Count);

            Assert.AreEqual(4, result.UpgradeConfig.weaponUpgrades.Length);
            var damageSpec = System.Array.Find(result.UpgradeConfig.weaponUpgrades, s => s.statKey == "damage");
            Assert.AreEqual(300, damageSpec.baseCost);
            Assert.AreEqual(5, damageSpec.maxLevel);
        }

        [Test]
        public void Import_NeverOverwritesHandAssignedAssetRefs()
        {
            var report = new ImportReport();
            var first = WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, report);
            var jackal = first.Weapons.Find(w => w.id == "jackal");
            var fakePrefab = new GameObject("HandAssignedJackalModel");
            jackal.modelPrefab = fakePrefab;
            EditorUtility.SetDirty(jackal);
            AssetDatabase.SaveAssets();

            var second = WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, new ImportReport());

            var jackalAfter = second.Weapons.Find(w => w.id == "jackal");
            Assert.AreEqual(fakePrefab, jackalAfter.modelPrefab);

            Object.DestroyImmediate(fakePrefab);
        }

        [Test]
        public void Import_SecondRunWithNoChangesReportsNothingUpdated()
        {
            WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, new ImportReport());

            var secondReport = new ImportReport();
            WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, secondReport);

            Assert.AreEqual(0, secondReport.Created.Count);
            Assert.AreEqual(0, secondReport.Updated.Count);
            Assert.AreEqual(4, secondReport.Unchanged.Count);
        }

        [Test]
        public void Import_ReportsOrphanWhenAssetHasNoMatchingJsonId()
        {
            WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, new ImportReport());
            AssetImportUtil.EnsureFolder(OutputFolder);
            var strayAsset = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(strayAsset, $"{OutputFolder}/stray_weapon_no_longer_in_json.asset");

            var report = new ImportReport();
            WeaponImporter.Import(FixturePath, OutputFolder, UpgradeConfigPath, report);

            Assert.AreEqual(1, report.Orphaned.Count);
            StringAssert.Contains("stray_weapon_no_longer_in_json", report.Orphaned[0]);
        }
    }
}
