using Game.Data;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    /// EnemyWeaponDrop.ForEnemy lit `EnemyData.weapon` (déjà importé depuis le champ "weapon"
    /// par type de `unity-spec/enemies.json`) : tank→sniper, rusher→smg, shooter→default (web
    /// weaponIdForType, main.js:4118) — pas de table dupliquée dans le code de gameplay.
    public class EnemyWeaponDropTests
    {
        static EnemyData WithWeaponId(string weaponId)
        {
            var data = ScriptableObject.CreateInstance<EnemyData>();
            data.id = "test-enemy";
            if (weaponId != null)
            {
                var weapon = ScriptableObject.CreateInstance<WeaponData>();
                weapon.id = weaponId;
                data.weapon = weapon;
            }
            return data;
        }

        [Test]
        public void Tank_DropsSniper() => Assert.AreEqual("sniper", EnemyWeaponDrop.ForEnemy(WithWeaponId("sniper")));

        [Test]
        public void Rusher_DropsSmg() => Assert.AreEqual("smg", EnemyWeaponDrop.ForEnemy(WithWeaponId("smg")));

        [Test]
        public void Shooter_DropsDefault() => Assert.AreEqual("default", EnemyWeaponDrop.ForEnemy(WithWeaponId("default")));

        [Test]
        public void NoWeaponAssigned_FallsBackToDefault() => Assert.AreEqual("default", EnemyWeaponDrop.ForEnemy(WithWeaponId(null)));

        [Test]
        public void NullData_FallsBackToDefault() => Assert.AreEqual("default", EnemyWeaponDrop.ForEnemy(null));
    }
}
