using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Consumable Data", fileName = "ConsumableData")]
    public class ConsumableData : ScriptableObject
    {
        public string key;
        public string displayName;
        public int price;
        public string iconName;

        public float damage;
        public float radius;
        public float fuseSeconds;
        public float effectSeconds;
        public float throwSpeed;
        public int maxStock;

        public Sprite icon;
    }
}
