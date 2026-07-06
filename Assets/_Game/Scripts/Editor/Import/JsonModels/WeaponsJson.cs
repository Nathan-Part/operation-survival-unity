using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class WeaponsJsonRoot
    {
        [JsonProperty("upgrades")] public WeaponUpgradeSpecsJson Upgrades;
        [JsonProperty("weapons")] public List<WeaponJsonEntry> Weapons;
    }

    public class WeaponUpgradeSpecsJson
    {
        [JsonProperty("damage")] public UpgradeSpecJson Damage;
        [JsonProperty("fireRate")] public UpgradeSpecJson FireRate;
        [JsonProperty("recoil")] public UpgradeSpecJson Recoil;
        [JsonProperty("mag")] public UpgradeSpecJson Mag;
    }

    public class UpgradeSpecJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("max")] public int Max;
        [JsonProperty("baseCost")] public int BaseCost;
        [JsonProperty("effect")] public string Effect;
    }

    public class WeaponJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("category")] public string Category;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("price")] public int Price;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("semiAuto")] public bool SemiAuto;
        [JsonProperty("melee")] public bool Melee;
        [JsonProperty("stats")] public WeaponStatsJson Stats;
    }

    public class WeaponStatsJson
    {
        [JsonProperty("mag")] public int Mag;
        [JsonProperty("cooldown")] public float Cooldown;
        [JsonProperty("reload")] public float Reload;
        [JsonProperty("dmgMult")] public float DmgMult;
        [JsonProperty("recoil")] public float Recoil;
        [JsonProperty("range")] public float Range;
        [JsonProperty("pellets")] public int Pellets;
    }
}
