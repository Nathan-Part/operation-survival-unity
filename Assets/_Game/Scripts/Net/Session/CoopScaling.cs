using System;

namespace Game.Net
{
    /// Rééquilibrage coop, en formules pures et retunables. Le TTK du web est calibré solo
    /// contre des bots ; plus il y a de joueurs, plus les ennemis encaissent et sont nombreux.
    /// Isolé ici pour un réglage centralisé (roadmap : « rééquilibrage obligatoire » en réseau).
    public static class CoopScaling
    {
        /// Multiplicateur de PV des ennemis en fonction du nombre de joueurs (1 joueur = 1.0).
        public static float EnemyHpMult(int playerCount, CoopSessionConfig cfg)
        {
            if (cfg == null) return 1f;
            int extra = playerCount > 1 ? playerCount - 1 : 0;
            return 1f + extra * cfg.enemyHpPerExtraPlayer;
        }

        /// Nombre d'ennemis supplémentaires à ajouter à la composition de base (plafonné).
        public static int EnemyCountBonus(int playerCount, CoopSessionConfig cfg)
        {
            if (cfg == null) return 0;
            int extra = playerCount > 1 ? playerCount - 1 : 0;
            int bonus = (int)Math.Round(extra * cfg.enemyCountPerExtraPlayer, MidpointRounding.AwayFromZero);
            if (bonus > cfg.maxExtraEnemiesFromPlayers) bonus = cfg.maxExtraEnemiesFromPlayers;
            if (bonus < 0) bonus = 0;
            return bonus;
        }
    }
}
