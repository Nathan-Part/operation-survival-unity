using Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Data.Tests
{
    public class PlaceholderLibraryTests
    {
        [Test]
        public void GetWeaponPlaceholder_ReturnsMatchingCategoryPrefab()
        {
            var lib = ScriptableObject.CreateInstance<PlaceholderLibrary>();
            var assaultPrefab = new GameObject("AssaultPlaceholder");
            var sniperPrefab = new GameObject("SniperPlaceholder");
            lib.weaponPlaceholders = new[]
            {
                new WeaponCategoryPlaceholder { category = WeaponCategory.Assault, prefab = assaultPrefab },
                new WeaponCategoryPlaceholder { category = WeaponCategory.Sniper, prefab = sniperPrefab }
            };

            Assert.AreEqual(sniperPrefab, lib.GetWeaponPlaceholder(WeaponCategory.Sniper));
            Assert.IsNull(lib.GetWeaponPlaceholder(WeaponCategory.Lmg));

            Object.DestroyImmediate(assaultPrefab);
            Object.DestroyImmediate(sniperPrefab);
        }
    }
}
