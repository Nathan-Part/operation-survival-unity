namespace Game.Net
{
    /// État runtime d'un joueur dans une session coop, autoritaire côté hôte.
    public class CoopPlayerState
    {
        public PlayerRef player;
        public string name;
        public string characterId;

        public float health;
        public float maxHealth;

        // À terre : en attente de relève par un coéquipier, sinon saignement puis mort.
        public bool isDown;
        public float bleedOut;       // secondes restantes avant mort si non relevé
        public float reviveProgress; // secondes de proximité cumulées d'un coéquipier

        // Mort : en attente de respawn tant que la partie continue.
        public bool isDead;
        public float respawnTimer;

        public NetVector3 position;
        public float aimYaw;
        public float aimPitch;

        public int kills;
        public int deaths;

        /// Vivant = ni à terre ni mort (peut agir et relever les autres).
        public bool IsAlive => !isDown && !isDead;
    }
}
