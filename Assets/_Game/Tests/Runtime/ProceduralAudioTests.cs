using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class ProceduralAudioTests
    {
        [Test]
        public void Tone_HasExpectedSampleCount()
        {
            var clip = ProceduralAudio.Tone(440f, 0.1f, ProceduralAudio.Waveform.Sine, 0.5f, 440f);
            Assert.AreEqual(4410, clip.samples); // 44100 × 0.1
        }

        [Test]
        public void Noise_ProducesNonZeroSamples()
        {
            var clip = ProceduralAudio.Noise(0.05f, 0.5f, 2000f);
            var buf = new float[clip.samples];
            clip.GetData(buf, 0);
            bool any = false;
            foreach (var s in buf) if (s != 0f) { any = true; break; }
            Assert.IsTrue(any);
        }
    }
}
