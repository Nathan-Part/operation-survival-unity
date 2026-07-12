namespace Game.Net
{
    /// Client -> hôte : l'état d'entrée d'un joueur pour un tick (position prédite côté client,
    /// visée, boutons). L'hôte s'en sert pour répliquer le joueur aux autres et pour le ciblage
    /// de l'IA ; il ne l'applique jamais aveuglément à l'état de jeu autoritaire.
    public struct PlayerInputCommand
    {
        public uint tick;
        public NetVector3 position;
        public float aimYaw;
        public float aimPitch;
        public ButtonFlags buttons;
    }

    /// Client -> hôte : « j'ai touché l'ennemi enemyNetId pour amount au point hitPoint ».
    /// amount est pré-calculé côté client à partir des zones de dégât (comme le solo) ; l'hôte
    /// VALIDE (DamageValidator) et plafonne avant d'appliquer. Jamais appliqué côté client.
    public struct DamageClaim
    {
        public uint tick;
        public PlayerRef attacker;
        public int enemyNetId;
        public NetVector3 hitPoint;
        public float amount;
    }

    /// Vue plate d'un joueur pour le snapshot (hôte -> clients).
    public struct PlayerNetState
    {
        public PlayerRef player;
        public NetVector3 position;
        public float aimYaw;
        public float health;
        public float maxHealth;
        public bool isDown;
        public bool isDead;
        public int kills;
    }

    /// Vue plate d'un ennemi pour le snapshot (hôte -> clients).
    public struct EnemyNetState
    {
        public int netId;
        public string typeId;
        public NetVector3 position;
        public float health;
        public float maxHealth;
        public EnemyLifecycle lifecycle;
    }

    /// Hôte -> clients : l'état du monde à afficher à un instant donné. Les tableaux sont des
    /// copies plates (l'encodage sur le fil est la responsabilité de l'adaptateur transport).
    public struct WorldSnapshot
    {
        public uint tick;
        public int wave;
        public int aliveEnemies;
        public SessionPhase phase;
        public int sharedMoney;
        public PlayerNetState[] players;
        public EnemyNetState[] enemies;
    }
}
