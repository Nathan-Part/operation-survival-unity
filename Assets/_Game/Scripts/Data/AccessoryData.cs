using System;
using UnityEngine;

namespace Game.Data
{
    public enum AccessoryType { Helmet, Costume }

    [Serializable]
    public class AccessoryBonus
    {
        public string icon;
        public string name;
        public string desc;
    }

    [CreateAssetMenu(menuName = "Game/Accessory Data", fileName = "AccessoryData")]
    public class AccessoryData : ScriptableObject
    {
        public string id;
        public string displayName;
        public AccessoryType accessoryType;
        public int price;
        public Rarity rarity;
        public string desc;
        public AccessoryBonus[] bonuses = Array.Empty<AccessoryBonus>();

        public Sprite image;
        public GameObject modelPrefab;
    }
}
