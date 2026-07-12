namespace Game.Net
{
    /// Tous les tunables du mode coop, en un seul endroit. Les valeurs marquées
    /// TODO(coop-balance) n'ont pas de source JSON (le jeu web n'a pas de coop) : ce sont des
    /// défauts raisonnables à régler au playtest, puis à porter dans un futur `coop.json`.
    /// L'intermission, l'argent par kill et la difficulté sont eux LUS depuis les données
    /// existantes via CoopConfigFactory (pas de valeur d'équilibrage dupliquée).
    public class CoopSessionConfig
    {
        // --- Session ---
        public int maxPlayers = 4;              // TODO(coop-balance)
        public bool friendlyFire = false;       // TODO(coop-balance)

        // --- Vagues (lues depuis les données) ---
        public float intermissionSeconds = 5f;  // écrasé par WaveConfig.intermissionSeconds
        public int perKillMoney = 15;           // écrasé par EconomyConfig.perKillMoney
        public float baseEnemyHpMult = 1f;      // écrasé par la difficulté "normal"

        // --- Down / relève / respawn ---
        public float bleedOutSeconds = 15f;         // TODO(coop-balance)
        public float reviveSeconds = 3f;            // TODO(coop-balance)
        public float reviveRadius = 2.5f;           // TODO(coop-balance)
        public float respawnSeconds = 5f;           // TODO(coop-balance)
        public float reviveHealthFraction = 0.5f;   // TODO(coop-balance)
        public float respawnHealthFraction = 1f;    // TODO(coop-balance)

        // --- Rééquilibrage par nombre de joueurs (le TTK web est pensé solo) ---
        public float enemyHpPerExtraPlayer = 0.35f;  // TODO(coop-balance)
        public float enemyCountPerExtraPlayer = 1f;   // TODO(coop-balance)
        public int maxExtraEnemiesFromPlayers = 6;    // TODO(coop-balance)
    }
}
