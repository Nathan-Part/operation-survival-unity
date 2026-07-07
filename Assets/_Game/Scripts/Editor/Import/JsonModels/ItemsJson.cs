using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class ItemsJsonRoot
    {
        [JsonProperty("accessories")] public AccessoriesJson Accessories;
        [JsonProperty("boosts")] public BoostsJson Boosts;
        [JsonProperty("consumables")] public ConsumablesJson Consumables;
        [JsonProperty("dances")] public DancesJson Dances;
    }

    public class AccessoriesJson
    {
        [JsonProperty("helmets")] public List<AccessoryJsonEntry> Helmets;
        [JsonProperty("costumes")] public List<AccessoryJsonEntry> Costumes;
    }

    public class AccessoryJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("price")] public int Price;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("bonuses")] public List<BonusJson> Bonuses;
    }

    public class BonusJson
    {
        [JsonProperty("icon")] public string Icon;
        [JsonProperty("name")] public string Name;
        [JsonProperty("desc")] public string Desc;
    }

    public class BoostsJson
    {
        [JsonProperty("items")] public List<BoostJsonEntry> Items;
    }

    public class BoostJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("slot")] public string Slot;
        [JsonProperty("name")] public string Name;
        [JsonProperty("stat")] public string Stat;
        [JsonProperty("bonus")] public float Bonus;
        [JsonProperty("price")] public int Price;
        [JsonProperty("rarity")] public string Rarity;
    }

    public class ConsumablesJson
    {
        [JsonProperty("items")] public List<ConsumableJsonEntry> Items;
    }

    public class ConsumableJsonEntry
    {
        [JsonProperty("key")] public string Key;
        [JsonProperty("name")] public string Name;
        [JsonProperty("price")] public int Price;
        [JsonProperty("damage")] public float Damage;
        [JsonProperty("radius")] public float Radius;
        [JsonProperty("fuseSeconds")] public float FuseSeconds;
        [JsonProperty("effectSeconds")] public float EffectSeconds;
        [JsonProperty("throwSpeed")] public float ThrowSpeed;
        [JsonProperty("maxStock")] public int MaxStock;
        [JsonProperty("assets")] public ConsumableAssetsJson Assets;
    }

    public class ConsumableAssetsJson
    {
        [JsonProperty("icon")] public string Icon;
    }

    public class DancesJson
    {
        [JsonProperty("items")] public List<DanceJsonEntry> Items;
    }

    public class DanceJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("clip")] public string Clip;
        [JsonProperty("price")] public int Price;
        [JsonProperty("rarity")] public string Rarity;
    }
}
