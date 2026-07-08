using System.Linq;
using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class ContractImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/contracts.test.json";
        const string OutputFolder = "Assets/_Game/Tests/Editor/Fixtures/Generated/Contracts";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_Creates8ContractsWithUniqueIds()
        {
            var report = new ImportReport();

            var result = ContractImporter.Import(Fixture, OutputFolder, report);

            Assert.AreEqual(8, result.Count);
            Assert.AreEqual(8, result.Select(c => c.id).Distinct().Count());
            Assert.IsTrue(result.All(c => !string.IsNullOrEmpty(c.desc)));
            Assert.AreEqual(8, report.Created.Count);

            var kills50 = result.Find(c => c.id == "kills50");
            Assert.IsNotNull(kills50);
            Assert.AreEqual("kills", kills50.type);
            Assert.AreEqual(50, kills50.target);
            Assert.AreEqual(800, kills50.reward);

            var wave10 = result.Find(c => c.id == "wave10");
            Assert.AreEqual("wave", wave10.type);
            Assert.AreEqual(2000, wave10.reward);
        }

        [Test]
        public void Import_SecondRunIsIdempotent()
        {
            ContractImporter.Import(Fixture, OutputFolder, new ImportReport());

            var secondReport = new ImportReport();
            ContractImporter.Import(Fixture, OutputFolder, secondReport);

            Assert.AreEqual(0, secondReport.Created.Count);
            Assert.AreEqual(0, secondReport.Updated.Count);
            Assert.AreEqual(8, secondReport.Unchanged.Count);
        }
    }
}
