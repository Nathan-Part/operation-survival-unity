using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class LaunchParamsTests
    {
        [TearDown]
        public void TearDown() => LaunchParams.Reset(); // one-shot global : ne pas polluer les autres tests

        [Test]
        public void Reset_ClearsTrainingFlag()
        {
            LaunchParams.Training = true;
            LaunchParams.Reset();
            Assert.IsFalse(LaunchParams.Training);
        }
    }
}
