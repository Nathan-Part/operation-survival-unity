namespace Game.Runtime
{
    public enum EnemyState { Idle, Chase, Attack, Dead }

    /// FSM du web (patrol/chase/attack, main.js ~4495-4538) : détection par rayon,
    /// attaque seulement à portée ET en ligne de vue, sortie d'attaque à hystérésis
    /// (attackExitRange > attackRange), abandon après loseTime au-delà de loseRadius.
    /// Pure : l'EnemyController fournit dt, distance et ligne de vue.
    /// Idle = « patrol » web : la déambulation est gérée par l'EnemyController.
    public class EnemyBrain
    {
        readonly float detectRadius, loseRadius, loseTime, attackRange, attackExitRange;
        float lostTimer;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        public EnemyBrain(float detectRadius, float loseRadius, float loseTime, float attackRange, float attackExitRange)
        {
            this.detectRadius = detectRadius;
            this.loseRadius = loseRadius;
            this.loseTime = loseTime;
            this.attackRange = attackRange;
            this.attackExitRange = attackExitRange;
        }

        /// Mort : aucun Tick n'en sort (le cadavre reste jusqu'à la purge de vague).
        public void Kill() => State = EnemyState.Dead;

        /// hasLineOfSight : sans ligne de vue, un ennemi à portée continue d'avancer
        /// au lieu de se figer en Attack derrière un mur (Phase 5 bis, point 2).
        public EnemyState Tick(float dt, float distToPlayer, bool hasLineOfSight = true)
        {
            switch (State)
            {
                case EnemyState.Dead:
                    return State;
                case EnemyState.Idle:
                    if (distToPlayer <= detectRadius) { State = EnemyState.Chase; lostTimer = 0f; }
                    break;
                case EnemyState.Chase:
                    if (distToPlayer <= attackRange && hasLineOfSight) { State = EnemyState.Attack; lostTimer = 0f; }
                    else if (distToPlayer > loseRadius)
                    {
                        lostTimer += dt;
                        if (lostTimer >= loseTime) { State = EnemyState.Idle; lostTimer = 0f; }
                    }
                    else lostTimer = 0f;
                    break;
                case EnemyState.Attack:
                    if (distToPlayer > attackExitRange || !hasLineOfSight) State = EnemyState.Chase;
                    break;
            }
            return State;
        }
    }
}
