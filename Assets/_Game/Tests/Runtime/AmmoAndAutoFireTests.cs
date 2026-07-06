using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class AmmoAndAutoFireTests
    {
        [Test]
        public void Ammo_ConsumeUntilEmptyThenReload()
        {
            var a = new AmmoModel(2, 1.2f);
            Assert.IsTrue(a.TryConsume());
            Assert.IsTrue(a.TryConsume());
            Assert.IsFalse(a.TryConsume());     // chargeur vide
            a.StartReload();
            Assert.IsTrue(a.IsReloading);
            Assert.IsFalse(a.TryConsume());     // pas de tir pendant le reload
            a.Tick(1.2f);
            Assert.IsFalse(a.IsReloading);
            Assert.AreEqual(2, a.InMag);        // chargeur plein (réserve infinie)
        }

        [Test]
        public void Ammo_ReloadIgnoredWhenFullOrAlreadyReloading()
        {
            var a = new AmmoModel(3, 1f);
            a.StartReload();
            Assert.IsFalse(a.IsReloading);      // déjà plein : no-op
            a.TryConsume();
            a.StartReload();
            a.StartReload();                     // double appel inoffensif
            a.Tick(0.5f);
            Assert.IsTrue(a.IsReloading);
            a.Tick(0.5f);
            Assert.AreEqual(3, a.InMag);
        }

        [TestCase(true, 5f, 60f, 0f, true, ExpectedResult = true)]
        [TestCase(false, 5f, 60f, 0f, true, ExpectedResult = false)]  // rien sous le viseur
        [TestCase(true, 70f, 60f, 0f, true, ExpectedResult = false)]  // hors portée
        [TestCase(true, 5f, 60f, 0.05f, true, ExpectedResult = false)] // cooldown en cours
        [TestCase(true, 5f, 60f, 0f, false, ExpectedResult = false)]  // reload / chargeur vide
        public bool AutoFire_Decision(bool onTarget, float dist, float range, float cd, bool ammoReady) =>
            AutoFireDecider.ShouldFire(onTarget, dist, range, cd, ammoReady);
    }
}
