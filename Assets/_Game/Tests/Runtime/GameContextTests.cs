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
        public void ResolveDifficulty_WithId_FindsRequestedEntry()
        {
            var db = ScriptableObject.CreateInstance<GameDatabase>();
            var eco = ScriptableObject.CreateInstance<EconomyConfig>();
            eco.difficulties = new[]
            {
                new DifficultySettings { id = "facile", enemyHpMult = 0.7f, enemyDmgMult = 0.7f, moneyMult = 1.2f },
                new DifficultySettings { id = "normal", enemyHpMult = 1f, enemyDmgMult = 1f, moneyMult = 1f },
                new DifficultySettings { id = "difficile", enemyHpMult = 1.4f, enemyDmgMult = 1.5f, moneyMult = 0.9f },
            };
            db.economyConfig = eco;

            var diff = GameContext.ResolveDifficulty(db, "difficile");

            Assert.AreEqual("difficile", diff.id);
            Assert.AreEqual(1.4f, diff.enemyHpMult);
            Assert.AreEqual(1.5f, diff.enemyDmgMult);
            Assert.AreEqual(0.9f, diff.moneyMult);
        }

        [Test]
        public void Difficulty_InstanceProperty_ReadsSelectedDifficultyId()
        {
            var go = new GameObject("ctx");
            try
            {
                var db = ScriptableObject.CreateInstance<GameDatabase>();
                var eco = ScriptableObject.CreateInstance<EconomyConfig>();
                eco.difficulties = new[]
                {
                    new DifficultySettings { id = "facile", enemyHpMult = 0.7f, enemyDmgMult = 0.7f, moneyMult = 1.2f },
                    new DifficultySettings { id = "normal", enemyHpMult = 1f, enemyDmgMult = 1f, moneyMult = 1f },
                };
                db.economyConfig = eco;
                var ctx = go.AddComponent<GameContext>();
                ctx.database = db;
                ctx.SelectedDifficultyId = "facile";

                Assert.AreEqual("facile", ctx.Difficulty.id);
                Assert.AreEqual(0.7f, ctx.Difficulty.enemyHpMult);
            }
            finally { Object.DestroyImmediate(go); }
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
