using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class WeaponCategoryPlaceholder
    {
        public WeaponCategory category;
        public GameObject prefab;
    }

    [CreateAssetMenu(menuName = "Game/Config/Placeholder Library", fileName = "PlaceholderLibrary")]
    public class PlaceholderLibrary : ScriptableObject
    {
        public WeaponCategoryPlaceholder[] weaponPlaceholders = Array.Empty<WeaponCategoryPlaceholder>();
        public GameObject enemyPlaceholder;
        public GameObject characterPlaceholder;
        public Sprite genericIcon;

        public GameObject GetWeaponPlaceholder(WeaponCategory category)
        {
            foreach (var entry in weaponPlaceholders)
                if (entry.category == category)
                    return entry.prefab;
            return null;
        }

        public GameObject GetEnemyPlaceholder() => enemyPlaceholder;
        public GameObject GetCharacterPlaceholder() => characterPlaceholder;
        public Sprite GetGenericIcon() => genericIcon;
    }
}
