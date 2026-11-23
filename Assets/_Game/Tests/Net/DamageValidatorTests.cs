using Game.Net;
using NUnit.Framework;

namespace Game.Net.Tests
{
    public class DamageValidatorTests
    {
        DamageValidator validator;
        CoopPlayerState attacker;
        EnemyRuntimeState enemy;

        [SetUp]
        public void SetUp()
        {
            validator = new DamageValidator(new CoopSessionConfig());
            attacker = new CoopPlayerState
            {
                player = new PlayerRef(1),
                health = 100f,
                maxHealth = 100f,
                position = NetVector3.Zero,
            };
            enemy = new EnemyRuntimeState
            {
                netId = 10,
                typeId = "shooter",
                health = 100f,
                maxHealth = 100f,
                position = new NetVector3(5f, 0f, 0f),
                lifecycle = EnemyLifecycle.Alive,
            };
        }

        DamageClaim Claim(float amount, NetVector3 hitPoint) => new DamageClaim
        {
            attacker = new PlayerRef(1),
            enemyNetId = 10,
            amount = amount,
            hitPoint = hitPoint,
        };

        [Test]
        public void ValidHit_IsAccepted()
        {
            Assert.AreEqual(DamageValidator.Result.Accepted, validator.Validate(Claim(30f, new NetVector3(5f, 0f, 0f)), attacker, enemy, 0f));
        }

        [Test]
        public void NonPositiveDamage_Rejected()
        {
            Assert.AreEqual(DamageValidator.Result.NonPositive, validator.Validate(Claim(0f, NetVector3.Zero), attacker, enemy, 0f));
        }

        [Test]
        public void NullAttacker_Rejected()
        {
            Assert.AreEqual(DamageValidator.Result.AttackerInvalid, validator.Validate(Claim(30f, NetVector3.Zero), null, enemy, 0f));
        }

        [Test]
        public void DownedAttacker_Rejected()
        {
            attacker.isDown = true;
            Assert.AreEqual(DamageValidator.Result.AttackerInvalid, validator.Validate(Claim(30f, NetVector3.Zero), attacker, enemy, 0f));
        }

        [Test]
        public void UnknownEnemy_Rejected()
        {
            Assert.AreEqual(DamageValidator.Result.UnknownEnemy, validator.Validate(Claim(30f, NetVector3.Zero), attacker, null, 0f));
        }

        [Test]
        public void DeadEnemy_Rejected()
        {
            enemy.health = 0f;
            enemy.lifecycle = EnemyLifecycle.Dying;
            Assert.AreEqual(DamageValidator.Result.EnemyDead, validator.Validate(Claim(30f, NetVector3.Zero), attacker, enemy, 0f));
        }

        [Test]
        public void OutOfRangeHit_Rejected()
        {
            validator.MaxHitDistance = 50f;
            Assert.AreEqual(DamageValidator.Result.OutOfRange, validator.Validate(Claim(30f, new NetVector3(1000f, 0f, 0f)), attacker, enemy, 0f));
        }

        [Test]
        public void ExceedingHitRate_Rejected()
        {
            validator.MaxHitRatePerSecond = 3f;
            var claim = Claim(1f, new NetVector3(5f, 0f, 0f));
            // 3 premiers dans la même seconde OK, le 4e dépasse
            Assert.AreEqual(DamageValidator.Result.Accepted, validator.Validate(claim, attacker, enemy, 0.0f));
            Assert.AreEqual(DamageValidator.Result.Accepted, validator.Validate(claim, attacker, enemy, 0.1f));
            Assert.AreEqual(DamageValidator.Result.Accepted, validator.Validate(claim, attacker, enemy, 0.2f));
            Assert.AreEqual(DamageValidator.Result.RateExceeded, validator.Validate(claim, attacker, enemy, 0.3f));
        }

        [Test]
        public void HitRate_ResetsAfterOneSecond()
        {
            validator.MaxHitRatePerSecond = 1f;
            var claim = Claim(1f, new NetVector3(5f, 0f, 0f));
            Assert.AreEqual(DamageValidator.Result.Accepted, validator.Validate(claim, attacker, enemy, 0.0f));
            Assert.AreEqual(DamageValidator.Result.RateExceeded, validator.Validate(claim, attacker, enemy, 0.5f));
            Assert.AreEqual(DamageValidator.Result.Accepted, validator.Validate(claim, attacker, enemy, 1.6f)); // nouvelle fenêtre
        }
    }
}
