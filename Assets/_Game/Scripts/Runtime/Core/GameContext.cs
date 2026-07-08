using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    public class GameContext : MonoBehaviour
    {
        public GameDatabase database;

        /// Difficulté choisie côté save (facile/normal/difficile) — posée par SliceBootstrap (T24).
        public string SelectedDifficultyId = "normal";

        public static GameContext Instance { get; private set; }

        void Awake()
        {
            if (database == null)
            {
                Debug.LogError("GameContext: no GameDatabase assigned - disabling.");
                enabled = false;
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public DifficultySettings Difficulty => ResolveDifficulty(database, SelectedDifficultyId);

        public static DifficultySettings ResolveDifficulty(GameDatabase db) => ResolveDifficulty(db, "normal");

        public static DifficultySettings ResolveDifficulty(GameDatabase db, string id)
        {
            var list = db != null && db.economyConfig != null ? db.economyConfig.difficulties : null;
            if (list != null)
                foreach (var d in list)
                    if (d != null && d.id == id)
                        return d;
            return new DifficultySettings
            {
                id = "normal", displayName = "Normal",
                enemyHpMult = 1f, enemyDmgMult = 1f, moneyMult = 1f
            };
        }

        public GameObject ResolveEnemyModel(EnemyData data)
        {
            if (data != null && data.modelPrefab != null) return data.modelPrefab;
            var lib = database != null ? database.placeholders : null;
            return lib != null ? lib.GetEnemyPlaceholder() : null;
        }

        public GameObject ResolveCharacterModel(CharacterData data)
        {
            if (data != null && data.modelPrefab != null) return data.modelPrefab;
            var lib = database != null ? database.placeholders : null;
            return lib != null ? lib.GetCharacterPlaceholder() : null;
        }
    }
}
