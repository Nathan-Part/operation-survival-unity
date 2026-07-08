using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class PetBrainTests
    {
        [Test]
        public void NoTarget_FarFromPlayer_Follows()
            => Assert.AreEqual(PetState.Follow, PetBrain.Decide(PetState.Idle, hasTarget: false, distToTarget: 99f, distToPlayer: 5f));

        [Test]
        public void TargetInDetect_Chases()
            => Assert.AreEqual(PetState.Chase, PetBrain.Decide(PetState.Follow, hasTarget: true, distToTarget: 5f, distToPlayer: 2f));

        [Test]
        public void TargetInAttackRange_Attacks()
            => Assert.AreEqual(PetState.Attack, PetBrain.Decide(PetState.Chase, hasTarget: true, distToTarget: 1.2f, distToPlayer: 2f));

        [Test]
        public void TargetBeyondGiveup_BackToFollow()
            => Assert.AreEqual(PetState.Follow, PetBrain.Decide(PetState.Chase, hasTarget: true, distToTarget: 12f, distToPlayer: 2f));

        [Test]
        public void NoTarget_NearPlayer_Idle()
            => Assert.AreEqual(PetState.Idle, PetBrain.Decide(PetState.Follow, hasTarget: false, distToTarget: 99f, distToPlayer: 1f));
    }
}
