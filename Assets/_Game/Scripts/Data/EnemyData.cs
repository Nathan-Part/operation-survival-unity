using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Enemy Data", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        public string id;
        public string displayName;
        public float hpMult;
        public float speedMult;
        public float scale;
        public bool melee;
        public float damageMult;
        public int burst;
        public WeaponData weapon;
        public string radarColor;
        public string glow;
        public ArmorType armor;
        public float opacity = 1f;
        public bool hasShootOverride;
        public float shootCdOverride;
        public float shootRangeOverride;
        public string behavior;

        public GameObject modelPrefab;

        public GameObject ResolveModel(PlaceholderLibrary placeholders) =>
            modelPrefab != null ? modelPrefab : placeholders.GetEnemyPlaceholder();
    }
}
