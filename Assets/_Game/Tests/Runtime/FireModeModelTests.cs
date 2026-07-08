using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class FireModeModelTests
    {
        [Test] public void Sniper_ForcedSemi()
            => Assert.AreEqual(FireMode.Semi, FireModeModel.Default("sniper", false));

        [Test] public void Default_IsAuto()
            => Assert.AreEqual(FireMode.Auto, FireModeModel.Default("default", false));

        [Test] public void WeaponFlagSemiAuto_IsSemi()
            => Assert.AreEqual(FireMode.Semi, FireModeModel.Default("raptor", weaponSemiAuto: true));

        [Test] public void OnlyBurstWeapons_CanToggleBurst()
        {
            Assert.IsTrue(FireModeModel.CanToggleBurst("cyclone"));
            Assert.IsFalse(FireModeModel.CanToggleBurst("sniper"));
        }
    }
}
