using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Data.Editor.Import.JsonModels
{
    public class EnemiesJsonRoot
    {
        [JsonProperty("baseStats")] public BaseStatsJson BaseStats;
        [JsonProperty("hitZones")] public HitZonesJson HitZones;
        [JsonProperty("types")] public Dictionary<string, EnemyTypeJson> Types;
        [JsonProperty("vip")] public VipJson Vip;
        [JsonProperty("specialConstants")] public SpecialConstantsJson SpecialConstants;
    }

    public class BaseStatsJson
    {
        [JsonProperty("hp")] public float Hp;
        [JsonProperty("walkSpeed")] public float WalkSpeed;
        [JsonProperty("runSpeed")] public float RunSpeed;
        [JsonProperty("radius")] public float Radius;
        [JsonProperty("turnSpeed")] public float TurnSpeed;
        [JsonProperty("shootRange")] public float ShootRange;
        [JsonProperty("fireCooldown")] public float FireCooldown;
        [JsonProperty("accuracy")] public float Accuracy;
        [JsonProperty("damage")] public float Damage;
        [JsonProperty("burstGap")] public float BurstGap;
        [JsonProperty("detectRadius")] public float DetectRadius;
        [JsonProperty("loseRadius")] public float LoseRadius;
        [JsonProperty("loseTime")] public float LoseTime;
        [JsonProperty("meleeRange")] public float MeleeRange;
        [JsonProperty("meleeDamage")] public float MeleeDamage;
        [JsonProperty("meleeCooldown")] public float MeleeCooldown;
    }

    public class HitZonesJson
    {
        [JsonProperty("head")] public HitZoneJson Head;
        [JsonProperty("torso")] public HitZoneJson Torso;
        [JsonProperty("legs")] public HitZoneJson Legs;
    }

    public class HitZoneJson
    {
        [JsonProperty("aboveY")] public float? AboveY;
        [JsonProperty("belowY")] public float? BelowY;
        [JsonProperty("damage")] public int Damage;
    }

    public class EnemyTypeJson
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("hpMult")] public float? HpMult;
        [JsonProperty("speedMult")] public float? SpeedMult;
        [JsonProperty("scale")] public float? Scale;
        [JsonProperty("melee")] public bool Melee;
        [JsonProperty("damageMult")] public float? DamageMult;
        [JsonProperty("burst")] public int? Burst;
        [JsonProperty("weapon")] public string Weapon;
        [JsonProperty("radarColor")] public string RadarColor;
        [JsonProperty("glow")] public string Glow;
        [JsonProperty("armor")] public string Armor;
        [JsonProperty("shootCd")] public float? ShootCd;
        [JsonProperty("shootRange")] public float? ShootRange;
        [JsonProperty("opacity")] public float? Opacity;
        [JsonProperty("behavior")] public string Behavior;
    }

    public class VipJson
    {
        [JsonProperty("moneyMult")] public int MoneyMult;
        [JsonProperty("minWave")] public int MinWave;
        [JsonProperty("excluded")] public List<string> Excluded;
    }

    public class SpecialConstantsJson
    {
        [JsonProperty("bossKillBonus")] public int BossKillBonus;
    }
}
