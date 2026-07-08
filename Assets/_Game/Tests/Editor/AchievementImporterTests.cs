using System.Linq;
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class AchievementImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/achievements.test.json";
        const string OutputFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Achievements";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_Creates26AchievementsWithUniqueIds()
        {
            var report = new ImportReport();

            var result = AchievementImporter.Import(Fixture, OutputFolder, report);

            Assert.AreEqual(26, result.Count);
            Assert.AreEqual(26, result.Select(a => a.id).Distinct().Count());
            Assert.IsTrue(result.All(a => !string.IsNullOrEmpty(a.displayName)));
            Assert.AreEqual(26, report.Created.Count);

            var firstBlood = result.Find(a => a.id == "firstblood");
            Assert.IsNotNull(firstBlood);
            Assert.AreEqual("run_kills", firstBlood.conditionType);
            Assert.AreEqual(1, firstBlood.target);

            var millionnaire = result.Find(a => a.id == "millionnaire");
            Assert.AreEqual("money", millionnaire.conditionType);
            Assert.AreEqual(10000, millionnaire.target);
        }

        [Test]
        public void Import_SecondRunIsIdempotent()
        {
            AchievementImporter.Import(Fixture, OutputFolder, new ImportReport());

            var secondReport = new ImportReport();
            AchievementImporter.Import(Fixture, OutputFolder, secondReport);

            Assert.AreEqual(0, secondReport.Created.Count);
            Assert.AreEqual(0, secondReport.Updated.Count);
            Assert.AreEqual(26, secondReport.Unchanged.Count);
        }
    }
}
