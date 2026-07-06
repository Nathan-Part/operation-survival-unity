using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class DifficultySettings
    {
        public string id;
        public string displayName;
        public float enemyHpMult;
        public float enemyDmgMult;
        public float moneyMult;
    }

    [Serializable]
    public class RaritySettings
    {
        public Rarity rarity;
        public string displayName;
        public string colorHex;
        public int tier;
    }

    [Serializable]
    public class LoadoutThresholds
    {
        public int mine;
        public int dog;
        public int carepackage;
        public int turret;
        public int airstrike;
    }

    [CreateAssetMenu(menuName = "Game/Config/Economy Config", fileName = "EconomyConfig")]
    public class EconomyConfig : ScriptableObject
    {
        public int perKillMoney;
        public int vipMoneyMult;
        public int bossKillBonus;
        public int maxLevel;
        public int leaderboardMaxEntries;
        public DifficultySettings[] difficulties = Array.Empty<DifficultySettings>();
        public RaritySettings[] rarities = Array.Empty<RaritySettings>();
        public LoadoutThresholds loadoutThresholds;
    }
}
