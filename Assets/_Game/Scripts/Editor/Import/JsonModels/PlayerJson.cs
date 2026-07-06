using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class PlayerJsonRoot
    {
        [JsonProperty("characters")] public List<CharacterJsonEntry> Characters;
        [JsonProperty("playerUpgrades")] public PlayerUpgradeSpecsJson PlayerUpgrades;
        [JsonProperty("movement")] public MovementJson Movement;
        [JsonProperty("secondWind")] public SecondWindJson SecondWind;
    }

    public class CharacterJsonEntry
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("name")] public string Name;
        [JsonProperty("price")] public int Price;
        [JsonProperty("rarity")] public string Rarity;
        [JsonProperty("base")] public CharacterBaseJson Base;
        [JsonProperty("desc")] public string Desc;
        [JsonProperty("skills")] public List<SkillJson> Skills;
    }

    public class CharacterBaseJson
    {
        [JsonProperty("health")] public float Health;
        [JsonProperty("regen")] public float Regen;
        [JsonProperty("speed")] public float Speed;
    }

    public class SkillJson
    {
        [JsonProperty("icon")] public string Icon;
        [JsonProperty("name")] public string Name;
        [JsonProperty("desc")] public string Desc;
    }

    public class PlayerUpgradeSpecsJson
    {
        [JsonProperty("health")] public PlayerUpgradeSpecJson Health;
        [JsonProperty("regen")] public PlayerUpgradeSpecJson Regen;
        [JsonProperty("speed")] public PlayerUpgradeSpecJson Speed;
    }

    public class PlayerUpgradeSpecJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("max")] public int Max;
        [JsonProperty("baseCost")] public int BaseCost;
        [JsonProperty("perLevel")] public float PerLevel;
    }

    public class MovementJson
    {
        [JsonProperty("walkSpeed")] public float WalkSpeed;
        [JsonProperty("runSpeed")] public float RunSpeed;
        [JsonProperty("jumpSpeed")] public float JumpSpeed;
        [JsonProperty("gravity")] public float Gravity;
        [JsonProperty("turnLerp")] public float TurnLerp;
        [JsonProperty("stepUpMax")] public float StepUpMax;
        [JsonProperty("stepDownSnap")] public float StepDownSnap;
        [JsonProperty("dash")] public DashJson Dash;
        [JsonProperty("stamina")] public StaminaJson Stamina;
        [JsonProperty("fallDamage")] public FallDamageJson FallDamage;
    }

    public class DashJson
    {
        [JsonProperty("distance")] public float Distance;
        [JsonProperty("speed")] public float Speed;
        [JsonProperty("cooldown")] public float Cooldown;
    }

    public class StaminaJson
    {
        [JsonProperty("max")] public float Max;
        [JsonProperty("drainPerSec")] public float DrainPerSec;
        [JsonProperty("regenPerSec")] public float RegenPerSec;
        [JsonProperty("minToRun")] public float MinToRun;
    }

    public class FallDamageJson
    {
        [JsonProperty("safeHeight")] public float SafeHeight;
        [JsonProperty("damagePerMeter")] public float DamagePerMeter;
        [JsonProperty("maxDamage")] public float MaxDamage;
    }

    public class SecondWindJson
    {
        [JsonProperty("invulnSeconds")] public float InvulnSeconds;
        [JsonProperty("usesPerRun")] public int UsesPerRun;
    }
}
