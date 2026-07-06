using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class WaveCompositionTests
    {
        [Test]
        public void Wave1_IsThreeShooters()
        {
            CollectionAssert.AreEqual(new[] { "shooter", "shooter", "shooter" }, WaveComposition.ForWave(1));
        }

        [Test]
        public void Wave2_IntroducesRusherOnOddSlots()
        {
            CollectionAssert.AreEqual(new[] { "shooter", "rusher", "shooter", "rusher" }, WaveComposition.ForWave(2));
        }

        [Test]
        public void Wave3_IntroducesTankOnSlot3()
        {
            CollectionAssert.AreEqual(new[] { "shooter", "rusher", "shooter", "tank", "shooter" }, WaveComposition.ForWave(3));
        }

        [Test]
        public void Wave4_IntroducesBomberAndShielder()
        {
            // n = min(2+4, 7) = 6 ; i2 -> bomber (w>=4, i%3==2), i4 -> shielder (4<=w<6, i%5==4), i5 -> bomber
            CollectionAssert.AreEqual(new[] { "shooter", "rusher", "bomber", "tank", "shielder", "bomber" }, WaveComposition.ForWave(4));
        }

        [Test]
        public void BossWaves_AreBossPlusTwoShooters()
        {
            CollectionAssert.AreEqual(new[] { "boss", "shooter", "shooter" }, WaveComposition.ForWave(5));
            CollectionAssert.AreEqual(new[] { "boss", "shooter", "shooter" }, WaveComposition.ForWave(10));
        }

        [Test]
        public void Wave6_EvenWaveVariants()
        {
            // n = 7 ; i1 -> medic (w>=6, i%6==1), i2 -> toxic (w pair), i3 -> tank, i4 -> berserker (w>=6, i%5==4), i5 -> toxic (i%3==2), i6 -> sniper (w>=5, i%7==6)
            CollectionAssert.AreEqual(new[] { "shooter", "medic", "toxic", "tank", "berserker", "toxic", "sniper" }, WaveComposition.ForWave(6));
        }

        [Test]
        public void Wave7_OddWaveVariants()
        {
            // n = 7 ; i1 -> medic, i2 -> bomber (w impair), i3 -> tank, i4 -> berserker, i5 -> stunner (w>=7, i%6==5, impair), i6 -> sniper (w>=5, i%7==6)
            CollectionAssert.AreEqual(new[] { "shooter", "medic", "bomber", "tank", "berserker", "stunner", "sniper" }, WaveComposition.ForWave(7));
        }

        [Test]
        public void Wave12_UnlocksJuggernaut()
        {
            // n = 7 ; w=12 pair : i1 medic, i2 toxic, i3 juggernaut (w>=10), i4 commander (w>=9 pair), i5 cryo (w>=7, i%6==5, pair), i6 grenadier (w>=8 pair)
            CollectionAssert.AreEqual(new[] { "shooter", "medic", "toxic", "juggernaut", "commander", "cryo", "grenadier" }, WaveComposition.ForWave(12));
        }

        [Test]
        public void Slot0_IsAlwaysShooterOnNonBossWaves()
        {
            for (int w = 1; w <= 30; w++)
            {
                if (w % 5 == 0) continue;
                Assert.AreEqual("shooter", WaveComposition.ForWave(w)[0], $"wave {w}");
            }
        }
    }
}
