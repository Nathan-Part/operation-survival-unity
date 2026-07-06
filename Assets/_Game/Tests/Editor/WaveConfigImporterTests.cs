using Game.Data;
using Game.Data.Editor.Import;
using NUnit.Framework;
using UnityEditor;

namespace Game.Data.Tests
{
    public class WaveConfigImporterTests
    {
        const string Fixture = "Assets/_Game/Tests/Editor/Fixtures/waves.test.json";
        const string ConfigPath = "Assets/_Game/Tests/Editor/Fixtures/Generated/WaveConfig.asset";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset("Assets/_Game/Tests/Editor/Fixtures/Generated");
        }

        [Test]
        public void Import_MapsWaveConstants()
        {
            var config = WaveConfigImporter.Import(Fixture, ConfigPath, new ImportReport());

            Assert.AreEqual(7, config.maxEnemiesPerWave);
            Assert.AreEqual(2.5f, config.intermissionSeconds, 0.0001f);
            Assert.AreEqual(5, config.bossEvery);
        }
    }
}
