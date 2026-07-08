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

        [Test]
        public void TakeHit_LethalDoesNotDestroyGameObject()
        {
            health.Init(shooter, 1, 1f, Zones());
            health.TakeHit(new Vector3(0f, 1.6f, 0f), 3f);    // mort
            Assert.IsTrue(health.IsDead);
            Assert.IsNotNull(health.gameObject);              // le cadavre reste (phase 3)
        }

        [Test]
        public void Damaged_RaisedOnEachValidHit()
        {
            health.Init(shooter, 1, 1f, Zones());
            int damaged = 0;
            health.Damaged += _ => damaged++;
            health.TakeHit(new Vector3(0f, 1f, 0f), 1f);      // torse 25
            health.TakeHit(new Vector3(0f, 1f, 0f), 1f);
            Assert.AreEqual(2, damaged);
        }

        [Test]
        public void MaxHealth_SetOnInit()
        {
            health.Init(shooter, 1, 1f, Zones());
            Assert.AreEqual(100, health.MaxHealth);
        }

        [Test]
        public void Kill_DirectDeathRaisesDiedOnce()
        {
            health.Init(shooter, 1, 1f, Zones());
            int deaths = 0;
            health.Died += _ => deaths++;
            health.Kill();
            health.Kill();
            Assert.AreEqual(1, deaths);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void BlockCheck_BlocksDamageAndReportsBlocked()
        {
            health.Init(shooter, 1, 1f, Zones());
            health.BlockCheck = _ => true;                     // shielder : tout vient de face
            var hit = health.TakeHit(new Vector3(0f, 1f, 0f), 1f, new Vector3(0f, 0f, 5f));
            Assert.IsTrue(hit.valid);
            Assert.IsTrue(hit.blocked);
            Assert.IsFalse(hit.killed);
            Assert.AreEqual(100, health.Health);               // aucun dégât
        }

        [Test]
        public void MarkVip_SetsFlag()
        {
            health.Init(shooter, 1, 1f, Zones());
            Assert.IsFalse(health.IsVip);
            health.MarkVip();
            Assert.IsTrue(health.IsVip);
        }

        [Test]
        public void ApplyDamage_ReducesHealth_AndRaisesDied()
        {
            health.Init(shooter, 1, 1f, Zones());
            bool died = false;
            health.Died += _ => died = true;
            health.ApplyDamage(health.MaxHealth);
            Assert.AreEqual(0, health.Health);
            Assert.IsTrue(died);
        }

        [Test]
        public void TakeHit_ReturnsZoneAndKilled()
        {
            health.Init(shooter, 1, 1f, Zones());
            var hit = health.TakeHit(new Vector3(0f, 1.6f, 0f), 1f); // tête 60
            Assert.IsTrue(hit.valid);
            Assert.AreEqual(HitZone.Head, hit.zone);
            Assert.IsFalse(hit.killed);
            var kill = health.TakeHit(new Vector3(0f, 1.6f, 0f), 1f); // 60 : 40 restants -> mort
            Assert.IsTrue(kill.killed);
            var ignored = health.TakeHit(new Vector3(0f, 1.6f, 0f), 1f);
            Assert.IsFalse(ignored.valid);                     // déjà mort
        }
    }
}
