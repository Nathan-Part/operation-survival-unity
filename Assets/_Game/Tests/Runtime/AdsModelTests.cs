using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class AdsModelTests
    {
        [Test]
        public void Tick_AimingRaisesAdsTowardsOne()
        {
            var m = new AdsModel();
            m.Tick(0.05f, true);
            Assert.Greater(m.AdsT, 0.3f);
            for (int i = 0; i < 60; i++) m.Tick(0.05f, true);
            Assert.AreEqual(1f, m.AdsT, 1e-3f);
        }

        [Test]
        public void Tick_ReleasingLowersAdsBackToZero()
        {
            var m = new AdsModel();
            for (int i = 0; i < 60; i++) m.Tick(0.05f, true);
            for (int i = 0; i < 90; i++) m.Tick(0.05f, false);
            Assert.AreEqual(0f, m.AdsT, 1e-2f);
        }

        [Test]
        public void Fov_LerpsFromBaseToAds()
        {
            var m = new AdsModel();
            Assert.AreEqual(55f, m.Fov(false), 1e-3f);       // hanche
            for (int i = 0; i < 90; i++) m.Tick(0.05f, true);
            Assert.AreEqual(42f, m.Fov(false), 0.1f);        // ADS
        }

        [Test]
        public void Scope_OnlyForSniperPastHalfAds()
        {
            var m = new AdsModel();
            for (int i = 0; i < 90; i++) m.Tick(0.05f, true);
            Assert.IsTrue(m.ScopeOn(true));
            Assert.IsFalse(m.ScopeOn(false));                // pas sniper : jamais de lunette
            Assert.AreEqual(20f, m.Fov(true), 0.1f);         // FOV lunette
        }
    }
}
