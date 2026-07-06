using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class CrossfadeModelTests
    {
        [Test]
        public void SnapTo_SetsFullWeightImmediately()
        {
            var m = new CrossfadeModel(3);
            m.SnapTo(1);
            Assert.AreEqual(0f, m.WeightOf(0), 1e-4f);
            Assert.AreEqual(1f, m.WeightOf(1), 1e-4f);
            Assert.AreEqual(1, m.Target);
        }

        [Test]
        public void FadeTo_HalfwayGivesEqualWeights()
        {
            var m = new CrossfadeModel(2);
            m.SnapTo(0);
            m.FadeTo(1, 0.2f); // web: setEnemyLoco crossfade 0.2 s
            m.Tick(0.1f);
            Assert.AreEqual(0.5f, m.WeightOf(0), 1e-3f);
            Assert.AreEqual(0.5f, m.WeightOf(1), 1e-3f);
        }

        [Test]
        public void FadeTo_CompletesToFullWeight()
        {
            var m = new CrossfadeModel(2);
            m.SnapTo(0);
            m.FadeTo(1, 0.2f);
            m.Tick(0.5f);
            Assert.AreEqual(0f, m.WeightOf(0), 1e-4f);
            Assert.AreEqual(1f, m.WeightOf(1), 1e-4f);
        }

        [Test]
        public void InterruptedFade_RestartsFromCurrentWeights()
        {
            var m = new CrossfadeModel(3);
            m.SnapTo(0);
            m.FadeTo(1, 0.2f);
            m.Tick(0.1f);           // 0: 0.5, 1: 0.5
            m.FadeTo(2, 0.1f);      // repart des poids courants
            m.Tick(0.05f);          // à mi-fade : 2 vaut ~0.5, 0 et 1 se partagent le reste
            Assert.AreEqual(0.5f, m.WeightOf(2), 1e-3f);
            Assert.AreEqual(1f, m.WeightOf(0) + m.WeightOf(1) + m.WeightOf(2), 1e-3f);
        }

        [Test]
        public void Weights_AlwaysSumToOne()
        {
            var m = new CrossfadeModel(4);
            m.SnapTo(2);
            m.FadeTo(3, 0.18f); // web: crossfade vers dead 0.18 s
            for (int i = 0; i < 10; i++)
            {
                m.Tick(0.03f);
                float sum = m.WeightOf(0) + m.WeightOf(1) + m.WeightOf(2) + m.WeightOf(3);
                Assert.AreEqual(1f, sum, 1e-3f);
            }
        }

        [Test]
        public void ZeroDuration_SnapsInstantly()
        {
            var m = new CrossfadeModel(2);
            m.SnapTo(0);
            m.FadeTo(1, 0f);
            m.Tick(0.001f);
            Assert.AreEqual(1f, m.WeightOf(1), 1e-4f);
        }
    }
}
