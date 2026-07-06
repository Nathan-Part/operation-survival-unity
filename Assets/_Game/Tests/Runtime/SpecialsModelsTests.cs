using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class SpecialsModelsTests
    {
        // ---- ComboModel ----

        [Test]
        public void Combo_MultiplierGrowsEveryThreeKills()
        {
            Assert.AreEqual(1, ComboModel.MultiplierFor(0));
            Assert.AreEqual(1, ComboModel.MultiplierFor(2));
            Assert.AreEqual(2, ComboModel.MultiplierFor(3));
            Assert.AreEqual(3, ComboModel.MultiplierFor(6));
            Assert.AreEqual(5, ComboModel.MultiplierFor(12));
            Assert.AreEqual(5, ComboModel.MultiplierFor(50)); // plafonné à 5
        }

        [Test]
        public void Combo_BreaksAfterWindow()
        {
            var c = new ComboModel();
            c.RegisterKill(10f);
            c.RegisterKill(11f);
            c.Tick(14.4f);                          // fenêtre 3.5 s après le kill de t=11
            Assert.AreEqual(2, c.Count);
            c.Tick(14.6f);
            Assert.AreEqual(0, c.Count);            // rompu
        }

        [Test]
        public void Combo_TimeFracShrinks()
        {
            var c = new ComboModel();
            c.RegisterKill(0f);
            Assert.AreEqual(1f, c.TimeFrac(0f), 1e-3f);
            Assert.AreEqual(0.5f, c.TimeFrac(1.75f), 1e-3f);
            Assert.AreEqual(0f, c.TimeFrac(4f), 1e-3f);
        }

        // ---- SpecialRules ----

        [Test]
        public void Explosion_FalloffAndMinimumOne()
        {
            Assert.AreEqual(35, SpecialRules.ExplosionDamage(0f, 3.5f, 35f));
            Assert.AreEqual(18, SpecialRules.ExplosionDamage(1.75f, 3.5f, 35f)); // mi-distance : round(17.5)
            Assert.AreEqual(1, SpecialRules.ExplosionDamage(3.49f, 3.5f, 35f));  // bord : min 1
            Assert.AreEqual(0, SpecialRules.ExplosionDamage(3.5f, 3.5f, 35f));   // hors rayon
        }

        [Test]
        public void Shield_BlocksOnlyFrontCone()
        {
            var enemyPos = Vector3.zero;
            var fwd = Vector3.forward;
            Assert.IsTrue(SpecialRules.ShieldBlocks(enemyPos, fwd, new Vector3(0f, 0f, 5f)));    // pile devant
            Assert.IsTrue(SpecialRules.ShieldBlocks(enemyPos, fwd, new Vector3(3f, 0f, 6f)));    // ~27° : dans le cône
            Assert.IsFalse(SpecialRules.ShieldBlocks(enemyPos, fwd, new Vector3(5f, 0f, 0f)));   // 90° : hors cône
            Assert.IsFalse(SpecialRules.ShieldBlocks(enemyPos, fwd, new Vector3(0f, 0f, -5f)));  // derrière
        }

        [Test]
        public void Frenzy_TriggersOnceBelowQuarterHp()
        {
            Assert.IsFalse(SpecialRules.FrenzyShouldTrigger(50, 100, false)); // 50 %
            Assert.IsTrue(SpecialRules.FrenzyShouldTrigger(25, 100, false));  // 25 %
            Assert.IsFalse(SpecialRules.FrenzyShouldTrigger(20, 100, true));  // déjà utilisé
            Assert.IsFalse(SpecialRules.FrenzyShouldTrigger(0, 100, false));  // mort
        }

        [Test]
        public void Commander_MultAndVipWaves()
        {
            Assert.AreEqual(1.2f, SpecialRules.CommanderMult(true), 1e-4f);
            Assert.AreEqual(1f, SpecialRules.CommanderMult(false), 1e-4f);
            Assert.IsFalse(SpecialRules.VipAllowedOnWave(2));  // < 3
            Assert.IsTrue(SpecialRules.VipAllowedOnWave(3));
            Assert.IsFalse(SpecialRules.VipAllowedOnWave(5));  // vague boss
            Assert.IsTrue(SpecialRules.VipAllowedOnWave(6));
        }

        // ---- StatusEffectsModel ----

        [Test]
        public void Status_StunAndFrostMultiply()
        {
            var s = new StatusEffectsModel();
            Assert.AreEqual(1f, s.SpeedMult(0f), 1e-4f);
            s.ApplyStun(0f);
            Assert.AreEqual(0.3f, s.SpeedMult(0.4f), 1e-4f);   // stunné
            Assert.AreEqual(1f, s.SpeedMult(0.6f), 1e-4f);     // fini après 0.5 s
            s.EnterFrost();
            Assert.AreEqual(0.5f, s.SpeedMult(1f), 1e-4f);     // gelé
            s.ApplyStun(1f);
            Assert.AreEqual(0.15f, s.SpeedMult(1.2f), 1e-3f);  // cumul 0.3×0.5
            s.ExitFrost();
            s.ExitFrost();                                      // sortie surnuméraire inoffensive
            Assert.AreEqual(0.3f, s.SpeedMult(1.3f), 1e-4f);
        }
    }
}
