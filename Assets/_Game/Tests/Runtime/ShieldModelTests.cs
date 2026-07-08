using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class ShieldModelTests
    {
        [Test]
        public void Absorb_TakesFromShieldFirst_ReturnsLeftover()
        {
            var s = new ShieldModel();                 // Current = 25
            float leftover = s.Absorb(10f);
            Assert.AreEqual(0f, leftover);
            Assert.AreEqual(15f, s.Current);
        }

        [Test]
        public void Absorb_MoreThanShield_LeftoverHitsHealth()
        {
            var s = new ShieldModel();
            Assert.AreEqual(5f, s.Absorb(30f));
            Assert.AreEqual(0f, s.Current);
        }

        [Test]
        public void Regen_OnlyAfterDelay()
        {
            var s = new ShieldModel();
            s.Absorb(25f);
            s.NoteDamage(0f);
            s.Tick(now: 3f, dt: 1f, hardcore: false);
            Assert.AreEqual(0f, s.Current);   // < 6 s
            s.Tick(now: 7f, dt: 1f, hardcore: false);
            Assert.AreEqual(8f, s.Current);   // +8/s
        }

        [Test]
        public void Regen_DisabledInHardcore()
        {
            var s = new ShieldModel();
            s.Absorb(25f);
            s.NoteDamage(0f);
            s.Tick(now: 10f, dt: 1f, hardcore: true);
            Assert.AreEqual(0f, s.Current);
        }

        [Test]
        public void Refill_RestoresToMax()
        {
            var s = new ShieldModel();
            s.Absorb(25f);
            Assert.AreEqual(0f, s.Current);
            s.Refill();
            Assert.AreEqual(ShieldModel.Max, s.Current);
        }
    }
}
