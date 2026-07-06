namespace Game.Runtime
{
    public enum EnemyState { Idle, Chase, Attack, Dead }

    /// FSM du web (idle/chase/attack) : détection par rayon, abandon après loseTime
    /// au-delà de loseRadius. Pure : l'EnemyController fournit dt et distance.
    public class EnemyBrain
    {
        readonly float detectRadius, loseRadius, loseTime, attackRange;
        float lostTimer;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        public EnemyBrain(float detectRadius, float loseRadius, float loseTime, float attackRange)
        {
            this.detectRadius = detectRadius;
            this.loseRadius = loseRadius;
            this.loseTime = loseTime;
            this.attackRange = attackRange;
        }

        /// Mort : aucun Tick n'en sort (le cadavre reste jusqu'à la purge de vague).
        public void Kill() => State = EnemyState.Dead;

        public EnemyState Tick(float dt, float distToPlayer)
        {
            switch (State)
            {
                case EnemyState.Dead:
                    return State;
                case EnemyState.Idle:
                    if (distToPlayer <= detectRadius) { State = EnemyState.Chase; lostTimer = 0f; }
                    break;
                case EnemyState.Chase:
                    if (distToPlayer <= attackRange) { State = EnemyState.Attack; lostTimer = 0f; }
                    else if (distToPlayer > loseRadius)
                    {
                        lostTimer += dt;
                        if (lostTimer >= loseTime) { State = EnemyState.Idle; lostTimer = 0f; }
                    }
                    else lostTimer = 0f;
                    break;
                case EnemyState.Attack:
                    if (distToPlayer > attackRange) State = EnemyState.Chase;
                    break;
            }
            return State;
        }
    }
}
