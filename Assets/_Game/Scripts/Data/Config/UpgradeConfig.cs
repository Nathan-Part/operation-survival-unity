using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class WeaponUpgradeSpec
    {
        public string statKey;
        public string displayName;
        public int maxLevel;
        public int baseCost;
        public string effectDesc;
    }

    [Serializable]
    public class PlayerUpgradeSpec
    {
        public string statKey;
        public string displayName;
        public int maxLevel;
        public int baseCost;
        public float perLevel;
    }

    [CreateAssetMenu(menuName = "Game/Config/Upgrade Config", fileName = "UpgradeConfig")]
    public class UpgradeConfig : ScriptableObject
    {
        public WeaponUpgradeSpec[] weaponUpgrades = Array.Empty<WeaponUpgradeSpec>();
        public PlayerUpgradeSpec[] playerUpgrades = Array.Empty<PlayerUpgradeSpec>();
    }
}
