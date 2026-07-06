using Game.Data;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class EnemyHealthTests
    {
        GameObject go;
        EnemyHealth health;
        EnemyData shooter;

        static HitZoneSettings Zones() => new HitZoneSettings
        {
            headAboveY = 1.5f, headDamage = 60,
            torsoDamage = 25,
            legsBelowY = 0.6f, legsDamage = 15
        };

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("enemy");
            health = go.AddComponent<EnemyHealth>();
            shooter = ScriptableObject.CreateInstance<EnemyData>();
            shooter.id = "shooter";
            shooter.hpMult = 1f;
            shooter.scale = 1f;
            shooter.armor = ArmorType.None;
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(go); }

        [Test]
        public void Init_Wave1NormalDifficulty_Is100Hp()
        {
            health.Init(shooter, 1, 1f, Zones());
            Assert.AreEqual(100, health.Health);
        }

        [Test]
        public void Init_AppliesWaveFormulaAndHpMult()
        {
            shooter.hpMult = 2.6f; // tank
            health.Init(shooter, 3, 1f, Zones());
            // EnemyHpForWave(3, 1) = round(100 * 1.18^2) = 139 ; * 2.6 = 361.4 -> 361
            Assert.AreEqual(361, health.Health);
        }

        [Test]
        public void TakeHit_HeadAtWorldHeightDealsHeadDamage()
        {
            health.Init(shooter, 1, 1f, Zones());
            go.transform.position = Vector3.zero;
            health.TakeHit(new Vector3(0f, 1.6f, 0f), 1f);   // tête
            Assert.AreEqual(40, health.Health);               // 100 - 60
        }

        [Test]
        public void TakeHit_KillEmitsDiedOnce()
        {
            health.Init(shooter, 1, 1f, Zones());
            int deaths = 0;
            health.Died += _ => deaths++;
            health.TakeHit(new Vector3(0f, 1.6f, 0f), 3f);    // 180 dégâts : mort
            health.TakeHit(new Vector3(0f, 1.6f, 0f), 3f);    // déjà mort : ignoré
            Assert.AreEqual(1, deaths);
            Assert.LessOrEqual(health.Health, 0);
        }
    }
}
