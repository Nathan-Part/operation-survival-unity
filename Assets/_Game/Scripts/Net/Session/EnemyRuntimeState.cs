namespace Game.Net
{
    /// État runtime d'un ennemi, autoritaire côté hôte. Les PV et le cycle de vie sont gérés
    /// par la simulation ; la position est écrite par la glue Unity (NavMesh) via SetEnemyPosition.
    public class EnemyRuntimeState
    {
        public int netId;
        public string typeId;
        public float health;
        public float maxHealth;
        public NetVector3 position;
        public EnemyLifecycle lifecycle = EnemyLifecycle.Alive;

        public bool IsAlive => lifecycle == EnemyLifecycle.Alive && health > 0f;
    }
}
