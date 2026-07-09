namespace Game.Data.Rules
{
    /// Rangs par tranche de 5 niveaux et éligibilité au prestige (web main.js:3188).
    public static class RankRules
    {
        static readonly string[] Titles =
        {
            "Recrue", "Soldat", "Caporal", "Sergent", "Lieutenant",
            "Capitaine", "Major", "Colonel", "Général", "Légende"
        }; // web: titres par tranche de 5 niveaux (main.js:3188)

        public static string TitleFor(int level)
        {
            int band = (level - 1) / 5;
            if (band < 0) band = 0;
            if (band > 9) band = 9;
            return Titles[band];
        }

        public static bool CanPrestige(int level) => level >= 50; // web: seuil de prestige

        // Rang opérateur (carte Intel de l'accueil), dérivé du meilleur wave atteint —
        // distinct des titres/prestige ci-dessus. web: updateHomeMeta (main.js:56-57).
        public static int OperatorRank(int bestWave) => bestWave / 2 + 1; // web: Math.floor(bw / 2) + 1

        public static float OperatorRankFill(int bestWave) => (bestWave % 2) / 2f; // web: (bw % 2) / 2 * 100

        // Multiplicateur affiché sur le chip "Meilleur combo xM" du lobby.
        // web: function comboMultiplier(n) (main.js:4666) => Math.min(5, 1 + Math.floor(n/3))
        public static int ComboMultiplier(int bestCombo) => System.Math.Min(5, 1 + bestCombo / 3);

        // Rang lettre de fin de partie (écran de mort riche, Phase 5.1) — DISTINCT de TitleFor/
        // CanPrestige ci-dessus (rangs XP par tranche de 5 niveaux) : celui-ci évalue une seule run,
        // pas la progression du compte. web: rankScore/runRank (main.js:2588-2606).
        public static int RunRankScore(int kills, float accuracy, int wave) =>
            UnityEngine.Mathf.RoundToInt(kills * 3f + accuracy * 100f * 2f + wave * 10f);

        public static char RunRank(int kills, float accuracy, int wave)
        {
            int s = RunRankScore(kills, accuracy, wave);
            if (s >= 200) return 'S';
            if (s >= 120) return 'A';
            if (s >= 70) return 'B';
            if (s >= 30) return 'C';
            return 'D';
        }
    }
}
