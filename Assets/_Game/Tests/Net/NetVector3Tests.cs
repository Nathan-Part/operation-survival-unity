using Game.Net;
using NUnit.Framework;

namespace Game.Net.Tests
{
    public class NetVector3Tests
    {
        [Test]
        public void Distance_IsEuclidean()
        {
            Assert.AreEqual(5f, NetVector3.Distance(NetVector3.Zero, new NetVector3(3f, 4f, 0f)), 1e-4f);
        }

        [Test]
        public void Lerp_Midpoint()
        {
            var mid = NetVector3.Lerp(NetVector3.Zero, new NetVector3(10f, 0f, -4f), 0.5f);
            Assert.AreEqual(5f, mid.x, 1e-4f);
            Assert.AreEqual(0f, mid.y, 1e-4f);
            Assert.AreEqual(-2f, mid.z, 1e-4f);
        }

        [Test]
        public void Lerp_ClampsOutOfRange()
        {
            var over = NetVector3.Lerp(NetVector3.Zero, new NetVector3(10f, 0f, 0f), 2f);
            Assert.AreEqual(10f, over.x, 1e-4f);
        }

        [Test]
        public void AddAndSubtract()
        {
            var sum = new NetVector3(1f, 2f, 3f) + new NetVector3(4f, 5f, 6f);
            Assert.IsTrue(sum.Equals(new NetVector3(5f, 7f, 9f)));
            var diff = sum - new NetVector3(5f, 7f, 9f);
            Assert.IsTrue(diff.Equals(NetVector3.Zero));
        }
    }
}
