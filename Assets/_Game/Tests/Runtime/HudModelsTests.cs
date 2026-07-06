using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class HudModelsTests
    {
        // ---- KillFeedModel ----

        [Test]
        public void KillFeed_PushPrefixesAndCapsAtFive()
        {
            var m = new KillFeedModel();
            m.Push("Boss", false, true, Color.white);
            Assert.AreEqual("☠ BOSS DOWN · Boss", m.Entries[0].text);
            m.Push("Tireur", true, false, Color.white);
            Assert.AreEqual("☠ HEADSHOT · Tireur", m.Entries[0].text); // plus récent en tête
            for (int i = 0; i < 6; i++) m.Push("Rusher", false, false, Color.white);
            Assert.AreEqual(KillFeedModel.MaxEntries, m.Entries.Count);
        }

        [Test]
        public void KillFeed_EntriesExpireAfterLifePlusFade()
        {
            var m = new KillFeedModel();
            m.Push("Tireur", false, false, Color.white);
            m.Tick(4.4f);
            Assert.AreEqual(1, m.Entries.Count);
            m.Tick(0.2f);                                   // 4.6 s > 4.5 s
            Assert.AreEqual(0, m.Entries.Count);
        }

        [Test]
        public void KillFeed_AlphaFadesAfterLife()
        {
            Assert.AreEqual(1f, KillFeedModel.Alpha(3.9f), 1e-4f);
            Assert.AreEqual(0.5f, KillFeedModel.Alpha(4.25f), 1e-3f);
            Assert.AreEqual(0f, KillFeedModel.Alpha(4.5f), 1e-4f);
        }

        // ---- DamageIndicatorModel ----

        [TestCase(0f, 0f, ExpectedResult = 0)]     // attaquant devant
        [TestCase(90f, 0f, ExpectedResult = 1)]    // à droite
        [TestCase(180f, 0f, ExpectedResult = 2)]   // derrière
        [TestCase(-90f, 0f, ExpectedResult = 3)]   // à gauche
        [TestCase(10f, 350f, ExpectedResult = 0)]  // wrap 360°
        [TestCase(200f, 90f, ExpectedResult = 1)]  // relatif au regard
        public int DamageIndicator_SectorFor(float worldAngle, float cameraYaw) =>
            DamageIndicatorModel.SectorFor(worldAngle, cameraYaw);

        [Test]
        public void DamageIndicator_WorldAngleFollowsWebConvention()
        {
            // attaquant plein nord (+Z) : atan2(0, 1) = 0°
            Assert.AreEqual(0f, DamageIndicatorModel.WorldAngleDeg(Vector3.zero, new Vector3(0f, 0f, 5f)), 1e-3f);
            // attaquant plein est (+X) : 90°
            Assert.AreEqual(90f, DamageIndicatorModel.WorldAngleDeg(Vector3.zero, new Vector3(5f, 0f, 0f)), 1e-3f);
        }

        // ---- BloodScreenModel ----

        [Test]
        public void Blood_BelowThresholdIgnored()
        {
            var m = new BloodScreenModel();
            m.OnDamage(19f);
            Assert.AreEqual(0f, m.Opacity, 1e-4f);
        }

        [Test]
        public void Blood_ScalesWithDamageThenFades()
        {
            var m = new BloodScreenModel();
            m.OnDamage(25f);
            Assert.AreEqual(0.5f, m.Opacity, 1e-4f);       // 25/50
            m.OnDamage(100f);
            Assert.AreEqual(1f, m.Opacity, 1e-4f);         // plafonné
            m.Tick(0.5f);                                   // maintien
            Assert.AreEqual(1f, m.Opacity, 1e-4f);
            m.Tick(0.5f);                                   // fondu 1/s
            Assert.AreEqual(0.5f, m.Opacity, 1e-3f);
            m.Tick(2f);
            Assert.AreEqual(0f, m.Opacity, 1e-4f);
        }

        // ---- CalloutRule ----

        [TestCase(true, true, 5, ExpectedResult = "BOSS DOWN")]      // boss prioritaire
        [TestCase(false, true, 2, ExpectedResult = "DOUBLE KILL")]   // multi avant headshot
        [TestCase(false, false, 3, ExpectedResult = "TRIPLE KILL")]
        [TestCase(false, false, 4, ExpectedResult = "MULTI KILL !")]
        [TestCase(false, true, 1, ExpectedResult = "HEADSHOT")]
        [TestCase(false, false, 1, ExpectedResult = null)]
        public string Callout_ForKill(bool boss, bool headshot, int killsInWindow) =>
            CalloutRule.ForKill(boss, headshot, killsInWindow);
    }
}
