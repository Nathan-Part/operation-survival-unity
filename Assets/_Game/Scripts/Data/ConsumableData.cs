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

        public Sprite icon;
    }
}
