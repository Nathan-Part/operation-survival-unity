namespace Game.Runtime
{
    /// Décide si le joueur court. La règle « stick poussé à fond = courir » (D11) ne vaut
    /// QUE pour un stick analogique (manette/joystick tactile) : au clavier, WASD est toujours
    /// à magnitude ~1 dès qu'on avance, donc seul Shift/R doit déclencher la course (bug n°2).
    public static class RunDecision
    {
        public const float StickRunThresholdSqr = 0.81f; // ~0.9 de course sur stick (D11)

        public static bool WantsRun(bool sprintPressed, float moveSqrMagnitude, bool moveFromStick) =>
            sprintPressed || (moveFromStick && moveSqrMagnitude >= StickRunThresholdSqr);
    }
}
