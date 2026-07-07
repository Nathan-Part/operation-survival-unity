using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class RunDecisionTests
    {
        [Test] public void Keyboard_FullMagnitude_WalksWithoutSprint()
            => Assert.IsFalse(RunDecision.WantsRun(sprintPressed: false, moveSqrMagnitude: 1f, moveFromStick: false));

        [Test] public void Keyboard_SprintHeld_Runs()
            => Assert.IsTrue(RunDecision.WantsRun(sprintPressed: true, moveSqrMagnitude: 1f, moveFromStick: false));

        [Test] public void Stick_FullPush_Runs()
            => Assert.IsTrue(RunDecision.WantsRun(sprintPressed: false, moveSqrMagnitude: 1f, moveFromStick: true));

        [Test] public void Stick_HalfPush_Walks()
            => Assert.IsFalse(RunDecision.WantsRun(sprintPressed: false, moveSqrMagnitude: 0.25f, moveFromStick: true));

        [Test] public void Stick_HeldSprint_RunsRegardlessOfPush()
            => Assert.IsTrue(RunDecision.WantsRun(sprintPressed: true, moveSqrMagnitude: 0f, moveFromStick: true));
    }
}
