using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Weapon Data", fileName = "WeaponData")]
    public class WeaponData : ScriptableObject
    {
        public string id;
        public string displayName;
        public WeaponCategory category;
        public Rarity rarity;
        public int price;
        public string desc;
        public bool semiAuto;
        public bool melee;
        public int mag;
        public float cooldown;
        public float reload;
        public float dmgMult;
        public float recoil;
        public float range;
        public int pellets;

        public GameObject modelPrefab;
        public Sprite icon;

        public GameObject ResolveModel(PlaceholderLibrary placeholders) =>
            modelPrefab != null ? modelPrefab : placeholders.GetWeaponPlaceholder(category);

        public Sprite ResolveIcon(PlaceholderLibrary placeholders) =>
            icon != null ? icon : placeholders.GetGenericIcon();
    }
}
