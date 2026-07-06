using Game.Data;

namespace Game.Runtime
{
    /// État FSM → rôle de clip (parité web setEnemyLoco : chase joue la course,
    /// l'attaque boucle pendant l'état d'attaque, les dégâts restent minutés).
    public static class EnemyLocoMap
    {
        public static AnimRole RoleFor(EnemyState state)
        {
            switch (state)
            {
                case EnemyState.Chase: return AnimRole.Run;
                case EnemyState.Attack: return AnimRole.Attack;
                case EnemyState.Dead: return AnimRole.Dead;
                default: return AnimRole.Idle;
            }
        }
    }
}
