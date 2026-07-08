using System.Collections.Generic;
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class LoadoutModelTests
    {
        static LoadoutModel Make() => new LoadoutModel(
            new List<string> { "turret", "mine", "airstrike" },
            new Dictionary<string, int> { { "turret", 1200 }, { "mine", 600 }, { "airstrike", 1800 }, { "carepackage", 1000 }, { "dog", 900 } });

        [Test]
        public void Points_AccrueForSelectedOnly()
        {
            var m = Make();
            for (int i = 0; i < 6; i++) m.OnKill(); // 600 pts chacun
            Assert.IsTrue(m.IsReady("mine"));       // seuil 600 atteint
            Assert.IsFalse(m.IsReady("turret"));    // 600 < 1200
            Assert.AreEqual(0f, m.Fill("dog"));     // non sélectionné → jamais de points
        }

        [Test]
        public void Consume_ResetsGauge()
        {
            var m = Make();
            for (int i = 0; i < 6; i++) m.OnKill();
            Assert.IsTrue(m.TryConsume("mine"));
            Assert.AreEqual(0f, m.Fill("mine"));
            Assert.IsFalse(m.TryConsume("mine")); // vidée
        }
    }
}
