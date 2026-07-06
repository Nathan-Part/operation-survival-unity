using System.Linq;
using Game.Data.Editor.Import.JsonModels;
using UnityEditor;

namespace Game.Data.Editor.Import
{
    public static class EconomyConfigImporter
    {
        public static EconomyConfig Import(string jsonPath, string configPath, ImportReport report)
        {
            var root = JsonFile.Read<EconomyJsonRoot>(jsonPath);
            var config = AssetImportUtil.FindOrCreateSingleton<EconomyConfig>(configPath);

            config.perKillMoney = root.Money.PerKill;
            config.vipMoneyMult = root.Money.VipMult;
            config.bossKillBonus = root.Money.BossKillBonus;
            config.maxLevel = root.Xp.MaxLevel;
            config.leaderboardMaxEntries = root.Leaderboard.MaxEntries;

            config.difficulties = root.Difficulties.Select(kvp => new DifficultySettings
            {
                id = kvp.Key,
                displayName = kvp.Value.Name,
                enemyHpMult = kvp.Value.EnemyHp,
                enemyDmgMult = kvp.Value.EnemyDmg,
                moneyMult = kvp.Value.Money
            }).ToArray();

            config.rarities = root.Rarities.Select(kvp => new RaritySettings
            {
                rarity = EnumParsing.ParseRarity(kvp.Key),
                displayName = kvp.Value.Name,
                colorHex = kvp.Value.Color,
                tier = kvp.Value.Tier
            }).ToArray();

            config.loadoutThresholds = new LoadoutThresholds
            {
                mine = root.Loadout.Thresholds.Mine,
                dog = root.Loadout.Thresholds.Dog,
                carepackage = root.Loadout.Thresholds.Carepackage,
                turret = root.Loadout.Thresholds.Turret,
                airstrike = root.Loadout.Thresholds.Airstrike
            };

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            report.Updated.Add("EconomyConfig");

            return config;
        }
    }
}
