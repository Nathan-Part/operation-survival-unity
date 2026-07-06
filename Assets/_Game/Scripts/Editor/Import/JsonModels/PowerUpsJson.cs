using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class PowerUpsJsonRoot
    {
        [JsonProperty("dropChance")] public float DropChance;
        [JsonProperty("dropChanceLootMaster")] public float DropChanceLootMaster;
        [JsonProperty("pickupRadius")] public float PickupRadius;
        [JsonProperty("defaultDuration")] public float DefaultDuration;
        [JsonProperty("powerups")] public Dictionary<string, PowerUpJsonEntry> PowerUps;
        [JsonProperty("instantConstants")] public InstantConstantsJson InstantConstants;
    }

    public class PowerUpJsonEntry
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("color")] public string Color;
        [JsonProperty("duration")] public float? Duration;
        [JsonProperty("instant")] public bool? Instant;
        [JsonProperty("effect")] public string Effect;
        [JsonProperty("pvp")] public string Pvp;
    }

    public class InstantConstantsJson
    {
        [JsonProperty("instantHealAmount")] public int InstantHealAmount;
        [JsonProperty("novaRadius")] public float NovaRadius;
        [JsonProperty("novaDamage")] public int NovaDamage;
    }
}
