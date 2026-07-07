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

        // Correctif d'échelle du modèle 3D (art, pas d'équilibrage — jamais dans le JSON de spec).
        // Certains GLB Blender sortent mal dimensionnés (ex. ar-15.glb mesuré ~2 m de long au lieu
        // de ~0,9 m) ; ce facteur multiplicatif permet de corriger sans toucher au mesh. 1 = taille
        // native du prefab (par défaut, tant qu'aucun défaut n'est constaté).
        public float modelScale = 1f;

        public GameObject ResolveModel(PlaceholderLibrary placeholders) =>
            modelPrefab != null ? modelPrefab : placeholders.GetWeaponPlaceholder(category);

        public Sprite ResolveIcon(PlaceholderLibrary placeholders) =>
            icon != null ? icon : placeholders.GetGenericIcon();
    }
}
