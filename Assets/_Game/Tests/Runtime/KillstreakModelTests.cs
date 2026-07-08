using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class KillstreakModelTests
    {
        [Test]
        public void Reaches_3_5_7_AtRightCounts()
        {
            var k = new KillstreakModel(); float t = 0f;
            Assert.AreEqual(0, k.RegisterKill(t)); // 1
            Assert.AreEqual(0, k.RegisterKill(t)); // 2
            Assert.AreEqual(3, k.RegisterKill(t)); // 3 → palier
            Assert.AreEqual(0, k.RegisterKill(t)); // 4
            Assert.AreEqual(5, k.RegisterKill(t)); // 5 → palier
            Assert.AreEqual(0, k.RegisterKill(t));
            Assert.AreEqual(7, k.RegisterKill(t)); // 7 → palier
        }

        [Test]
        public void KillsOutsideWindow_Expire()
        {
            var k = new KillstreakModel();
            k.RegisterKill(0f); k.RegisterKill(0f);
            Assert.AreEqual(0, k.RegisterKill(20f)); // les 2 premiers ont expiré (>8s) → count 1
            Assert.AreEqual(1, k.StreakCount);
        }

        [Test]
        public void PlayerDamaged_ResetsStreak()
        {
            var k = new KillstreakModel(); k.RegisterKill(0f); k.RegisterKill(0f);
            k.OnPlayerDamaged(); Assert.AreEqual(0, k.StreakCount);
        }
    }
}
