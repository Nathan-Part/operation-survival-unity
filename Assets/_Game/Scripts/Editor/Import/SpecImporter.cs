using UnityEditor;
using UnityEngine;

namespace Game.Data.Editor.Import
{
    public class SpecFileNames
    {
        public string Weapons = "weapons.json";
        public string Enemies = "enemies.json";
        public string Player = "player.json";
        public string Economy = "economy.json";
        public string Items = "items.json";
        public string PowerUps = "powerups.json";
        public string Waves = "waves.json";
        public string Achievements = "achievements.json";
    }

    public static class SpecImporter
    {
        [MenuItem("Tools/Game/Import Spec from JSON")]
        public static void ImportFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Import Spec from JSON",
                    "This will create/update ScriptableObjects under Assets/_Game/Data from unity-spec/*.json. " +
                    "Art asset references (models, icons) are never touched. Continue?",
                    "Import", "Cancel"))
            {
                return;
            }

            var specRoot = System.IO.Path.Combine(Application.dataPath, "..", "unity-spec");
            var report = new ImportReport();
            RunImport(specRoot, Paths.DataRoot, new SpecFileNames(), report);

            EditorUtility.DisplayDialog("Import Spec from JSON — done", report.Summary(), "OK");
        }

        public static GameDatabase RunImport(string specRoot, string dataRoot, SpecFileNames fileNames, ImportReport report)
        {
            var weaponsFolder = $"{dataRoot}/Weapons";
            var enemiesFolder = $"{dataRoot}/Enemies";
            var charactersFolder = $"{dataRoot}/Characters";
            var powerUpsFolder = $"{dataRoot}/PowerUps";
            var accessoriesFolder = $"{dataRoot}/Items/Accessories";
            var boostsFolder = $"{dataRoot}/Items/Boosts";
            var consumablesFolder = $"{dataRoot}/Items/Consumables";
            var dancesFolder = $"{dataRoot}/Items/Dances";
            var achievementsFolder = $"{dataRoot}/Achievements";
            var configRoot = $"{dataRoot}/Config";
            var upgradeConfigPath = $"{configRoot}/UpgradeConfig.asset";

            var weaponResult = WeaponImporter.Import($"{specRoot}/{fileNames.Weapons}", weaponsFolder, upgradeConfigPath, report);
            var enemyResult = EnemyImporter.Import($"{specRoot}/{fileNames.Enemies}", enemiesFolder, weaponsFolder, $"{configRoot}/EnemyCombatConfig.asset", report);
            var characterResult = CharacterImporter.Import($"{specRoot}/{fileNames.Player}", charactersFolder, $"{configRoot}/MovementConfig.asset", upgradeConfigPath, report);
            var economyConfig = EconomyConfigImporter.Import($"{specRoot}/{fileNames.Economy}", $"{configRoot}/EconomyConfig.asset", report);
            var itemsResult = ItemsImporter.Import($"{specRoot}/{fileNames.Items}", accessoriesFolder, boostsFolder, consumablesFolder, dancesFolder, report);
            var powerUpResult = PowerUpImporter.Import($"{specRoot}/{fileNames.PowerUps}", powerUpsFolder, $"{configRoot}/PowerUpConfig.asset", report);
            var waveConfig = WaveConfigImporter.Import($"{specRoot}/{fileNames.Waves}", $"{configRoot}/WaveConfig.asset", report);
            var achievements = AchievementImporter.Import($"{specRoot}/{fileNames.Achievements}", achievementsFolder, report);

            var databasePath = $"{dataRoot}/GameDatabase.asset";
            var database = AssetImportUtil.FindOrCreateSingleton<GameDatabase>(databasePath);
            database.weapons = weaponResult.Weapons.ToArray();
            database.enemies = enemyResult.Enemies.ToArray();
            database.characters = characterResult.Characters.ToArray();
            database.powerUps = powerUpResult.PowerUps.ToArray();
            database.accessories = itemsResult.Accessories.ToArray();
            database.boosts = itemsResult.Boosts.ToArray();
            database.consumables = itemsResult.Consumables.ToArray();
            database.dances = itemsResult.Dances.ToArray();
            database.achievements = achievements.ToArray();
            database.movementConfig = characterResult.MovementConfig;
            database.enemyCombatConfig = enemyResult.CombatConfig;
            database.waveConfig = waveConfig;
            database.economyConfig = economyConfig;
            database.upgradeConfig = characterResult.UpgradeConfig;
            database.powerUpConfig = powerUpResult.Config;
            EditorUtility.SetDirty(database);

            AssetDatabase.SaveAssets();

            return database;
        }
    }
}
