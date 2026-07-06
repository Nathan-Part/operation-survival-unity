using Game.Data;
using Game.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace Game.Runtime.Tests
{
    public class GameContextTests
    {
        [Test]
        public void ResolveDifficulty_FindsNormalEntry()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var eco = ScriptableObject.CreateInstance<EconomyConfig>();
            eco.difficulties = new[]
            {
                new DifficultySettings { id = "facile", enemyHpMult = 0.7f, enemyDmgMult = 0.7f, moneyMult = 1.2f },
                new DifficultySettings { id = "normal", enemyHpMult = 1f, enemyDmgMult = 1f, moneyMult = 1f },
            };
            db.economyConfig = eco;

            var diff = GameContext.ResolveDifficulty(db);

            Assert.AreEqual("normal", diff.id);
            Assert.AreEqual(1f, diff.enemyHpMult);
        }

        [Test]
        public void ResolveDifficulty_FallsBackToNeutralWhenMissing()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();

            var diff = GameContext.ResolveDifficulty(db);

            Assert.AreEqual(1f, diff.enemyHpMult);
            Assert.AreEqual(1f, diff.enemyDmgMult);
            Assert.AreEqual(1f, diff.moneyMult);
        }

        [Test]
        public void ResolveEnemyModel_NullPlaceholdersDoesNotThrow()
        {
            var go = new GameObject("ctx");
            try
            {
                var ctx = go.AddComponent<GameContext>();
                ctx.database = ScriptableObject.CreateInstance<GameDatabase>();
                var enemy = ScriptableObject.CreateInstance<EnemyData>();

                Assert.IsNull(ctx.ResolveEnemyModel(enemy));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
