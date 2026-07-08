namespace Game.Runtime
{
    public enum PetState { Idle, Follow, Chase, Attack }

    /// FSM pure du chien (web pet.js updateAI) : attaque en portée, poursuit en détection,
    /// suit le joueur sinon, idle quand collé au joueur.
    public static class PetBrain
    {
        public const float DetectR = 7f, GiveupR = 11f, FollowDist = 1.8f, StopDist = 1.1f, AttackR = 1.3f;

        public static PetState Decide(PetState current, bool hasTarget, float distToTarget, float distToPlayer)
        {
            if (hasTarget && distToTarget <= GiveupR)
                return distToTarget <= AttackR ? PetState.Attack : PetState.Chase;
            if (distToPlayer > FollowDist) return PetState.Follow;
            return PetState.Idle;
        }
    }
}
