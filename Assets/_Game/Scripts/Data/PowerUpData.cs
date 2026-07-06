using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/PowerUp Data", fileName = "PowerUpData")]
    public class PowerUpData : ScriptableObject
    {
        public string id;
        public string displayName;
        public string colorHex;
        public float duration;
        public bool isInstant;
        public string effectDesc;
        public PvpPolicy pvpPolicy;
    }
}
