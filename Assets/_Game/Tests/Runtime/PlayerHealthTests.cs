using Game.Data;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class PlayerHealthTests
    {
        GameObject go;
        PlayerHealth health;
        float fakeTime;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("player");
            health = go.AddComponent<PlayerHealth>();
            fakeTime = 100f;
            health.timeSource = () => fakeTime;
            var character = ScriptableObject.CreateInstance<CharacterData>();
            character.baseHealth = 100f;
            character.baseRegen = 10f;
            var movement = ScriptableObject.CreateInstance<MovementConfig>();
            movement.secondWindInvulnSeconds = 2f;
            movement.secondWindUsesPerRun = 1;
            movement.fallDamage = new FallDamageSettings { safeHeight = 3f, damagePerMeter = 5f, maxDamage = 40f };
            health.Init(character, movement);
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(go); }

        [Test]
        public void Init_UsesStatFormulas()
        {
            Assert.AreEqual(100f, health.MaxHealth);
            Assert.AreEqual(100f, health.Health);
        }

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            health.TakeDamage(30f);
            Assert.AreEqual(70f, health.Health);
        }

        [Test]
        public void SecondWind_SavesFromFatalBlowOnce()
        {
            bool died = false;
            health.Died += () => died = true;

            health.TakeDamage(150f);                 // coup fatal n°1 : second wind
            Assert.AreEqual(1f, health.Health);
            Assert.IsFalse(died);

            health.TakeDamage(50f);                  // pendant l'invulnérabilité : ignoré
            Assert.AreEqual(1f, health.Health);

            fakeTime += 2.1f;                        // invulnérabilité expirée
            health.TakeDamage(50f);                  // coup fatal n°2 : mort
            Assert.AreEqual(0f, health.Health);
            Assert.IsTrue(died);
        }

        [Test]
        public void ApplyFallDamage_UsesWebFormula()
        {
            health.ApplyFallDamage(8f);              // (8-3)*5 = 25
            Assert.AreEqual(75f, health.Health);
            health.ApplyFallDamage(2f);              // sous safeHeight : rien
            Assert.AreEqual(75f, health.Health);
        }

        // Valeurs réelles web (Phase 5.1) : REGEN_DELAY = 6 s, puis regenRate PV/s (main.js:3524).
        [Test]
        public void Regen_WaitsSixSecondsThenHealsAtRate()
        {
            health.TakeDamage(50f);              // 100 -> 50, lastDamageTime = fakeTime = 100
            fakeTime = 105.9f;                   // < 6 s après le coup
            health.TickRegen(1f);
            Assert.AreEqual(50f, health.Health); // délai pas écoulé : aucune régén

            fakeTime = 106.1f;                   // > 6 s
            health.TickRegen(1f);                // baseRegen 10 (SetUp) -> +10 PV
            Assert.AreEqual(60f, health.Health);
        }

        [Test]
        public void Regen_DisabledInHardcore()
        {
            health.hardcore = true;              // web: aucune régén PV en hardcore
            health.TakeDamage(50f);
            fakeTime = 200f;                     // délai largement écoulé
            health.TickRegen(1f);
            Assert.AreEqual(50f, health.Health);
        }

        [Test]
        public void DisableSecondWind_HardcoreDiesOnFatalBlow()
        {
            health.DisableSecondWind();              // web: pas de dernier souffle en hardcore (T24)
            bool died = false;
            health.Died += () => died = true;

            health.TakeDamage(150f);                  // coup fatal : plus de second wind, mort directe
            Assert.AreEqual(0f, health.Health);
            Assert.IsTrue(died);
        }
    }
}
