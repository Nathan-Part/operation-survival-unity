using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Boost Data", fileName = "BoostData")]
    public class BoostData : ScriptableObject
    {
        public string id;
        public BoostSlot slot;
        public string displayName;
        public BoostStat stat;
        public float bonus;
        public int price;
        public Rarity rarity;

        public Sprite icon;
    }
}
