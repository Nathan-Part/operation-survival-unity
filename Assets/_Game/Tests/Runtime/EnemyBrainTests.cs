using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class EnemyBrainTests
    {
        // Valeurs spec : detect 12, lose 18, loseTime 3, shootRange 9.
        static EnemyBrain Brain() => new EnemyBrain(12f, 18f, 3f, 9f);

        [Test]
        public void Idle_DetectsPlayerInsideDetectRadius()
        {
            var b = Brain();
            Assert.AreEqual(EnemyState.Idle, b.Tick(0.1f, 13f));
            Assert.AreEqual(EnemyState.Chase, b.Tick(0.1f, 11f));
        }

        [Test]
        public void Chase_AttacksInsideAttackRange()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);                              // Idle -> Chase
            Assert.AreEqual(EnemyState.Attack, b.Tick(0.1f, 8f));
        }

        [Test]
        public void Attack_BacksToChaseWhenOutOfRange()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);
            b.Tick(0.1f, 8f);                               // Attack
            Assert.AreEqual(EnemyState.Chase, b.Tick(0.1f, 10f));
        }

        [Test]
        public void Chase_GivesUpAfterLoseTimeBeyondLoseRadius()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);                              // Chase
            Assert.AreEqual(EnemyState.Chase, b.Tick(2.9f, 20f)); // 2.9 s < loseTime
            Assert.AreEqual(EnemyState.Idle, b.Tick(0.2f, 20f));  // 3.1 s cumulés
        }

        [Test]
        public void Chase_LoseTimerResetsWhenPlayerComesBack()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);
            b.Tick(2.9f, 20f);                              // presque perdu
            b.Tick(0.1f, 15f);                              // revient sous loseRadius : reset
            Assert.AreEqual(EnemyState.Chase, b.Tick(2.9f, 20f)); // le timer repart de zéro
        }

        [Test]
        public void SniperRange_UsesOverride()
        {
            var sniper = new EnemyBrain(12f, 18f, 3f, 28f); // shootRangeOverride 28
            sniper.Tick(0.1f, 11f);                          // Chase (détection inchangée)
            Assert.AreEqual(EnemyState.Attack, sniper.Tick(0.1f, 11f)); // déjà à portée de tir
        }
    }
}
