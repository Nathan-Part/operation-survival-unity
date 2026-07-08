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
    }
}
