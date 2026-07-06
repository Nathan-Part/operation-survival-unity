using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Config/PowerUp Config", fileName = "PowerUpConfig")]
    public class PowerUpConfig : ScriptableObject
    {
        public float dropChance;
        public float dropChanceLootMaster;
        public float pickupRadius;
        public float defaultDuration;
        public int instantHealAmount;
        public float novaRadius;
        public int novaDamage;
    }
}
