using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class RecoilModelTests
    {
        [Test]
        public void Kick_ThenDecaysToZero()
        {
            var r = new RecoilModel();
            r.Kick(1.8f);
            Assert.AreEqual(1.8f, r.Current, 1e-4f);
            r.Tick(0.1f);                                  // -0.6
            Assert.AreEqual(1.2f, r.Current, 1e-3f);
            r.Tick(1f);
            Assert.AreEqual(0f, r.Current, 1e-4f);         // plancher 0
        }

        [Test]
        public void ViewmodelOffset_LerpsHipToAdsAndPushesBack()
        {
            var r = new RecoilModel();
            Assert.AreEqual(RecoilModel.VmHip, r.ViewmodelOffset(0f));
            Assert.AreEqual(RecoilModel.VmAds, r.ViewmodelOffset(1f));
            r.Kick(1f);
            Assert.AreEqual(RecoilModel.VmHip.z + 0.06f, r.ViewmodelOffset(0f).z, 1e-4f);
        }

        [Test]
        public void ViewmodelEuler_MatchesWebFormula()
        {
            var r = new RecoilModel();
            var hip = r.ViewmodelEuler(0f);
            Assert.AreEqual(0.05f, hip.x, 1e-4f);
            Assert.AreEqual(UnityEngine.Mathf.PI + 0.12f, hip.y, 1e-4f);
            var ads = r.ViewmodelEuler(1f);
            Assert.AreEqual(0.3f, ads.x, 1e-4f);           // 0.05 + 0.25
            Assert.AreEqual(UnityEngine.Mathf.PI, ads.y, 1e-4f);
        }
    }
}
