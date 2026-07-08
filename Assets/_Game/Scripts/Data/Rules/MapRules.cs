namespace Game.Data.Rules
{
    /// Résolution mapId → scène jouable et disponibilité des maps (web main.js:268-272, MAP_VISUALS).
    /// Seule "utopia" est livrée ; "urban" est grisée ("Bientôt"), "portal" est abandonnée.
    /// Repli pur : ne mute jamais la save, se contente de renvoyer la scène jouable la plus proche.
    public static class MapRules
    {
        public static string SceneNameFor(string mapId)
        {
            // Seule "utopia" est livrée : tout autre id ("urban", "portal", null, inconnu)
            // retombe sur la même scène plutôt que de planter — aucune mutation de la save ici.
            return "Utopia";
        }

        public static bool IsAvailable(string mapId) => mapId == "utopia";
    }
}
