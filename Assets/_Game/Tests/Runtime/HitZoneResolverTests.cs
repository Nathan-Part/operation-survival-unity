using Game.Data;
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class HitZoneResolverTests
    {
        static HitZoneSettings Zones() => new HitZoneSettings
        {
            headAboveY = 1.5f, headDamage = 60,
            torsoDamage = 25,
            legsBelowY = 0.6f, legsDamage = 15
        };

        [TestCase(1.6f, ExpectedResult = HitZone.Head)]
        [TestCase(1.5f, ExpectedResult = HitZone.Head)]   // frontière incluse tête
        [TestCase(1.49f, ExpectedResult = HitZone.Torso)]
        [TestCase(0.6f, ExpectedResult = HitZone.Torso)]  // frontière incluse torse
        [TestCase(0.59f, ExpectedResult = HitZone.Legs)]
        [TestCase(0.1f, ExpectedResult = HitZone.Legs)]
        public HitZone Resolve_ByHeight(float yLocal) => HitZoneResolver.Resolve(yLocal, 1f, Zones());

        [Test]
        public void Resolve_ScaleNormalizesHeight()
        {
            // boss scale 1.9 : un impact à 2.85 m = tête (2.85 / 1.9 = 1.5)
            Assert.AreEqual(HitZone.Head, HitZoneResolver.Resolve(2.85f, 1.9f, Zones()));
            Assert.AreEqual(HitZone.Torso, HitZoneResolver.Resolve(1.5f, 1.9f, Zones()));
        }

        [Test]
        public void Damage_UsesZoneTimesWeaponMult()
        {
            Assert.AreEqual(60, HitZoneResolver.Damage(HitZone.Head, Zones(), 1f, ArmorType.None, 0.5f));
            Assert.AreEqual(25, HitZoneResolver.Damage(HitZone.Torso, Zones(), 1f, ArmorType.None, 0.5f));
            Assert.AreEqual(15, HitZoneResolver.Damage(HitZone.Legs, Zones(), 1f, ArmorType.None, 0.5f));
            Assert.AreEqual(180, HitZoneResolver.Damage(HitZone.Head, Zones(), 3f, ArmorType.None, 0.5f)); // sniper dmgMult 3
        }

        [Test]
        public void Damage_TorsoArmorOnlyProtectsTorso()
        {
            Assert.AreEqual(13, HitZoneResolver.Damage(HitZone.Torso, Zones(), 1f, ArmorType.Torso, 0.5f)); // 12.5 -> 13 (away from zero)
            Assert.AreEqual(60, HitZoneResolver.Damage(HitZone.Head, Zones(), 1f, ArmorType.Torso, 0.5f));  // headshot passe intégralement
            Assert.AreEqual(15, HitZoneResolver.Damage(HitZone.Legs, Zones(), 1f, ArmorType.Torso, 0.5f));
        }

        [Test]
        public void Damage_FullArmorProtectsEverything()
        {
            Assert.AreEqual(30, HitZoneResolver.Damage(HitZone.Head, Zones(), 1f, ArmorType.Full, 0.5f));
            Assert.AreEqual(13, HitZoneResolver.Damage(HitZone.Torso, Zones(), 1f, ArmorType.Full, 0.5f));
            Assert.AreEqual(8, HitZoneResolver.Damage(HitZone.Legs, Zones(), 1f, ArmorType.Full, 0.5f)); // 7.5 -> 8
        }
    }
}
