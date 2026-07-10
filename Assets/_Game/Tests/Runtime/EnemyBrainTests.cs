using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class EnemyBrainTests
    {
        // Valeurs spec : detect 12, lose 18, loseTime 3, shootRange 9, sortie ranged ×1.25.
        static EnemyBrain Brain() => new EnemyBrain(12f, 18f, 3f, 9f, 9f * 1.25f);

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
        public void Attack_HasHysteresis_ThenBacksToChase()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);
            b.Tick(0.1f, 8f);                               // Attack
            // Parité web (main.js ~4523) : on ne ressort qu'au-delà de range*1.25 (11.25)
            Assert.AreEqual(EnemyState.Attack, b.Tick(0.1f, 10f));
            Assert.AreEqual(EnemyState.Chase, b.Tick(0.1f, 12f));
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
        public void Kill_ForcesDeadState()
        {
            var b = Brain();
            b.Tick(0.1f, 8f);
            b.Kill();
            Assert.AreEqual(EnemyState.Dead, b.State);
        }

        [Test]
        public void Tick_FromDead_StaysDead()
        {
            var b = Brain();
            b.Kill();
            Assert.AreEqual(EnemyState.Dead, b.Tick(1f, 2f));   // joueur au contact : reste mort
            Assert.AreEqual(EnemyState.Dead, b.Tick(10f, 50f));
        }

        [Test]
        public void SniperRange_UsesOverride()
        {
            // shootRangeOverride 28 ; detect élevé à la portée par EnemyController (parité web detR)
            var sniper = new EnemyBrain(28f, 36.4f, 3f, 28f, 35f);
            Assert.AreEqual(EnemyState.Chase, sniper.Tick(0.1f, 27f));  // détecté à sa portée de tir
            Assert.AreEqual(EnemyState.Attack, sniper.Tick(0.1f, 27f)); // à portée + LOS -> tir
        }

        // Ligne de vue (Phase 5 bis, point 2) : sans LOS un ennemi à portée continue
        // d'avancer au lieu de se figer en Attack derrière un obstacle.

        [Test]
        public void Chase_WithoutLineOfSight_KeepsChasingEvenInRange()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);                              // Idle -> Chase
            Assert.AreEqual(EnemyState.Chase, b.Tick(0.1f, 8f, hasLineOfSight: false));
        }

        [Test]
        public void Attack_LosingLineOfSight_BacksToChase()
        {
            var b = Brain();
            b.Tick(0.1f, 11f);
            b.Tick(0.1f, 8f);                               // Attack (LOS par défaut)
            Assert.AreEqual(EnemyState.Chase, b.Tick(0.1f, 8f, hasLineOfSight: false));
        }

        [Test]
        public void Sniper_WithoutLineOfSight_AdvancesInsteadOfCamping()
        {
            var sniper = new EnemyBrain(28f, 36.4f, 3f, 28f, 35f);
            sniper.Tick(0.1f, 27f);                          // Chase
            Assert.AreEqual(EnemyState.Chase, sniper.Tick(0.1f, 27f, hasLineOfSight: false));
            Assert.AreEqual(EnemyState.Attack, sniper.Tick(0.1f, 27f, hasLineOfSight: true));
        }
    }
}
