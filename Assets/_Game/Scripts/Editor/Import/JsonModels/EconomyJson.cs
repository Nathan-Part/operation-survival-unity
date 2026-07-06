using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class EconomyJsonRoot
    {
        [JsonProperty("money")] public MoneyJson Money;
        [JsonProperty("xp")] public XpJson Xp;
        [JsonProperty("difficulties")] public Dictionary<string, DifficultyJson> Difficulties;
        [JsonProperty("rarities")] public Dictionary<string, RarityJson> Rarities;
        [JsonProperty("leaderboard")] public LeaderboardJson Leaderboard;
        [JsonProperty("loadout")] public LoadoutJson Loadout;
    }

    public class MoneyJson
    {
        [JsonProperty("perKill")] public int PerKill;
        [JsonProperty("vipMult")] public int VipMult;
        [JsonProperty("bossKillBonus")] public int BossKillBonus;
    }

    public class XpJson
    {
        [JsonProperty("maxLevel")] public int MaxLevel;
    }

    public class DifficultyJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("enemyHp")] public float EnemyHp;
        [JsonProperty("enemyDmg")] public float EnemyDmg;
        [JsonProperty("money")] public float Money;
    }

    public class RarityJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("color")] public string Color;
        [JsonProperty("tier")] public int Tier;
    }

    public class LeaderboardJson
    {
        [JsonProperty("maxEntries")] public int MaxEntries;
    }

    public class LoadoutJson
    {
        [JsonProperty("thresholds")] public ThresholdsJson Thresholds;
    }

    public class ThresholdsJson
    {
        [JsonProperty("mine")] public int Mine;
        [JsonProperty("dog")] public int Dog;
        [JsonProperty("carepackage")] public int Carepackage;
        [JsonProperty("turret")] public int Turret;
        [JsonProperty("airstrike")] public int Airstrike;
    }
}
