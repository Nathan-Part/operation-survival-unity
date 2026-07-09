using System.Collections.Generic;

namespace Game.Data.Save
{
    /// Miroir exact du schéma web `cod-save-v1` (src/game/save.js) : mêmes noms de
    /// champs que le JSON web pour rester interopérable. Sérialisé via Newtonsoft.
    public class SaveData
    {
        public int v = 1;
        public int money;
        public int bestWave;
        public int bestScore;
        public int bestCombo;
        public long xp;
        public SaveDaily daily = new SaveDaily();
        public string selectedCharacterId;
        public List<string> ownedWeapons = new List<string> { "default", "knife" }; // knife gratuit (#50)
        public string selectedWeapon = "default";
        public Dictionary<string, int> weaponUpgrades = new Dictionary<string, int>();
        public Dictionary<string, int> playerUpgrades = new Dictionary<string, int>();
        public List<string> ownedCharacters = new List<string> { "perso:blue-soldier" };
        public List<string> ownedAccessories = new List<string> { "helm:none", "cost:none" };
        public Dictionary<string, string> equipped = new Dictionary<string, string>
        {
            { "helmet", "helm:none" }, { "costume", "cost:none" }
        };
        public List<string> ownedDances = new List<string> { "dance:all_night", "dance:boom", "dance:groove" };
        public List<string> selectedDances = new List<string> { "dance:all_night", "dance:boom", "dance:groove" };
        public List<string> ownedBoosts = new List<string>();
        public Dictionary<string, string> equippedBoosts = new Dictionary<string, string>();
        public List<string> unlockedAchievements = new List<string>();
        public SaveSettings settings = new SaveSettings();
        public SaveLifetime lifetime = new SaveLifetime();
        public Dictionary<string, int> consumables = new Dictionary<string, int>
        {
            { "frag", 12 }, { "flashbang", 8 }, { "smoke", 6 }, { "sticky", 4 },
            { "molotov", 4 }, { "emp", 3 }, { "medkit", 3 }
        };
        public List<string> selectedLoadout = new List<string> { "turret", "mine", "airstrike" };
        public List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();
        public int prestige;
    }

    public class SaveSettings
    {
        public float sensitivity = 1.0f;
        public float volume = 0.3f;
        public string defaultView = "third";
        public string difficulty = "normal";
        public bool hardcore;
        public bool rain;
        public string mapId = "utopia";
    }

    public class SaveLifetime
    {
        public long kills;
        public long headshots;
        public long moneyEarned;
        public double playtime;
    }

    public class LeaderboardEntry
    {
        public int score;
        public int wave;
        public string date;

        // Écran de fin de partie riche (Phase 5.1, 2026-07-09) : le web soumet ces 3 champs en plus
        // (submitScore, main.js:4025) — valeurs par défaut 0 pour les entrées d'avant cette version
        // du schéma (migration dans SaveMigration).
        public int kills;
        public int headshots;
        public float accuracy;
    }
}
