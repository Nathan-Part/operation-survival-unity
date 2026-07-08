using Game.Data.Rules;
using Game.Data.Save;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class SettingsRulesTests
    {
        // ---------- SetSensitivity ----------

        [Test]
        public void SetSensitivity_SnapsToStep()
        {
            var save = new SaveData();
            SettingsRules.SetSensitivity(save, 1.05f);
            Assert.AreEqual(1.1f, save.settings.sensitivity, 0.0001f);
        }

        [Test]
        public void SetSensitivity_ClampsToRange()
        {
            var save = new SaveData();
            SettingsRules.SetSensitivity(save, 10f);
            Assert.AreEqual(SettingsRules.SensitivityMax, save.settings.sensitivity, 0.0001f);

            SettingsRules.SetSensitivity(save, -5f);
            Assert.AreEqual(SettingsRules.SensitivityMin, save.settings.sensitivity, 0.0001f);
        }

        // ---------- SetVolume ----------

        [Test]
        public void SetVolume_SnapsToStep()
        {
            var save = new SaveData();
            SettingsRules.SetVolume(save, 0.47f);
            Assert.AreEqual(0.45f, save.settings.volume, 0.0001f);
        }

        [Test]
        public void SetVolume_ClampsToRange()
        {
            var save = new SaveData();
            SettingsRules.SetVolume(save, 2f);
            Assert.AreEqual(SettingsRules.VolumeMax, save.settings.volume, 0.0001f);

            SettingsRules.SetVolume(save, -1f);
            Assert.AreEqual(SettingsRules.VolumeMin, save.settings.volume, 0.0001f);
        }

        // ---------- ToggleView ----------

        [Test]
        public void ToggleView_RoundTrip()
        {
            var save = new SaveData(); // défaut "third"
            SettingsRules.ToggleView(save);
            Assert.AreEqual("first", save.settings.defaultView);

            SettingsRules.ToggleView(save);
            Assert.AreEqual("third", save.settings.defaultView);
        }

        // ---------- SetDifficulty ----------

        [Test]
        public void SetDifficulty_Valid_Accepted()
        {
            var save = new SaveData();
            Assert.IsTrue(SettingsRules.SetDifficulty(save, "difficile"));
            Assert.AreEqual("difficile", save.settings.difficulty);
        }

        [Test]
        public void SetDifficulty_Invalid_Rejected()
        {
            var save = new SaveData();
            string before = save.settings.difficulty;
            Assert.IsFalse(SettingsRules.SetDifficulty(save, "extreme"));
            Assert.AreEqual(before, save.settings.difficulty);
        }

        // ---------- ToggleHardcore / ToggleRain ----------

        [Test]
        public void ToggleHardcore_RoundTrip()
        {
            var save = new SaveData();
            Assert.IsFalse(save.settings.hardcore);
            SettingsRules.ToggleHardcore(save);
            Assert.IsTrue(save.settings.hardcore);
            SettingsRules.ToggleHardcore(save);
            Assert.IsFalse(save.settings.hardcore);
        }

        [Test]
        public void ToggleRain_RoundTrip()
        {
            var save = new SaveData();
            Assert.IsFalse(save.settings.rain);
            SettingsRules.ToggleRain(save);
            Assert.IsTrue(save.settings.rain);
            SettingsRules.ToggleRain(save);
            Assert.IsFalse(save.settings.rain);
        }
    }
}
