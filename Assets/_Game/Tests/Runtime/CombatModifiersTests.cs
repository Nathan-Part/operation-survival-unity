using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class CombatModifiersTests
    {
        [Test] public void Adrenaline_UnderQuarterHealth()
        {
            Assert.IsTrue(CombatModifiers.IsAdrenaline(25f, 100f));
            Assert.IsFalse(CombatModifiers.IsAdrenaline(26f, 100f));
        }

        [Test] public void DamageMult_ComposesAdrenalineAndPrecision()
        {
            Assert.AreEqual(1f, CombatModifiers.DamageMult(false, false), 1e-4);
            Assert.AreEqual(1.2f, CombatModifiers.DamageMult(true, false), 1e-4);
            Assert.AreEqual(1.3f, CombatModifiers.DamageMult(false, true), 1e-4);
            Assert.AreEqual(1.56f, CombatModifiers.DamageMult(true, true), 1e-4);
        }

        [Test] public void LongShot_AtThirtyMeters()
        {
            Assert.IsTrue(CombatModifiers.IsLongShot(30f));
            Assert.IsFalse(CombatModifiers.IsLongShot(29.9f));
        }

        [Test] public void Overheat_PausesAtMax_RecoversAfterDelay()
        {
            var o = new OverheatModel();
            for (int i = 0; i < 20; i++) o.OnShot(0f);
            Assert.IsFalse(o.CanFire);
            o.Tick(2.1f);
            Assert.IsTrue(o.CanFire);
        }
    }
}
