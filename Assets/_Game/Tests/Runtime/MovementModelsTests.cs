using Game.Data;
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class MovementModelsTests
    {
        static StaminaSettings Stamina() => new StaminaSettings { max = 100f, drainPerSec = 25f, regenPerSec = 15f, minToRun = 20f };
        static FallDamageSettings Fall() => new FallDamageSettings { safeHeight = 3f, damagePerMeter = 5f, maxDamage = 40f };
        static DashSettings Dash() => new DashSettings { distance = 2.6f, speed = 16f, cooldown = 1.4f };

        [Test]
        public void Stamina_DrainsWhileRunningAndRegensOtherwise()
        {
            var s = new StaminaModel(Stamina());
            Assert.IsTrue(s.Tick(1f, true));           // 100 -> 75
            Assert.AreEqual(75f, s.Current, 0.001f);
            s.Tick(1f, false);                          // 75 -> 90
            Assert.AreEqual(90f, s.Current, 0.001f);
        }

        [Test]
        public void Stamina_ExhaustionBlocksRunUntilMinToRun()
        {
            var s = new StaminaModel(Stamina());
            s.Tick(4f, true);                           // 100 -> 0 : épuisé
            Assert.AreEqual(0f, s.Current, 0.001f);
            Assert.IsFalse(s.Tick(1f, true));           // épuisé : régénère (0 -> 15) au lieu de courir
            Assert.AreEqual(15f, s.Current, 0.001f);
            Assert.IsFalse(s.Tick(0.2f, true));         // 15 -> 18 : toujours < minToRun (20)
            s.Tick(0.14f, true);                        // -> ~20.1 : seuil franchi
            Assert.IsTrue(s.Tick(0.1f, true));          // repart
        }

        [Test]
        public void Stamina_ClampsAtMax()
        {
            var s = new StaminaModel(Stamina());
            s.Tick(10f, false);
            Assert.AreEqual(100f, s.Current, 0.001f);
        }

        [TestCase(3f, ExpectedResult = 0)]
        [TestCase(2f, ExpectedResult = 0)]
        [TestCase(4f, ExpectedResult = 5)]    // (4-3)*5
        [TestCase(8f, ExpectedResult = 25)]   // (8-3)*5
        [TestCase(20f, ExpectedResult = 40)]  // plafonné
        public int FallDamage_MatchesWebFormula(float height) => FallDamageRule.Damage(height, Fall());

        [Test]
        public void Dash_DurationAndCooldown()
        {
            var d = new DashModel(Dash());
            Assert.AreEqual(2.6f / 16f, d.Duration, 0.0001f);
            Assert.IsTrue(d.TryStart(10f));
            Assert.IsTrue(d.IsActive(10.1f));
            Assert.IsFalse(d.IsActive(10.3f));           // durée ~0.1625 s écoulée
            Assert.IsFalse(d.TryStart(10.5f));           // cooldown 1.4 s pas fini
            Assert.IsTrue(d.TryStart(11.5f));            // franchement au-delà du cooldown (frontière exacte sujette aux arrondis float)
        }
    }
}
