using System.IO;
using Game.Data.Rules;
using Game.Data.Save;
using UnityEngine;

namespace Game.Runtime
{
    /// Progression persistante (parité web save.js) : charge `cod-save-v1.json`
    /// depuis persistentDataPath, accumule argent/XP/stats pendant la partie,
    /// persiste aux moments sûrs (mort, pause mobile, quit).
    public class MetaProgress : MonoBehaviour
    {
        public SaveData Data { get; private set; }
        public int Level => ProgressionRules.LevelFor(Data.xp);

        string PathOnDisk => Path.Combine(Application.persistentDataPath, "cod-save-v1.json");

        public void LoadOrCreate()
        {
            string json = null;
            try { if (File.Exists(PathOnDisk)) json = File.ReadAllText(PathOnDisk); }
            catch (IOException e) { Debug.LogWarning($"MetaProgress: lecture save impossible ({e.Message}) - save neuve."); }
            Data = SaveMigration.FromJson(json);
        }

        public void Persist()
        {
            if (Data == null) return;
            try { File.WriteAllText(PathOnDisk, SaveMigration.ToJson(Data)); }
            catch (IOException e) { Debug.LogWarning($"MetaProgress: ecriture save impossible ({e.Message})."); }
        }

        public void OnKill(bool headshot, bool vip, int moneyGained)
        {
            if (Data == null) return;
            Data.money += moneyGained;
            Data.lifetime.moneyEarned += moneyGained;
            Data.lifetime.kills++;
            if (headshot) Data.lifetime.headshots++;
            Data.xp += ProgressionRules.KillXp(headshot, vip);
        }

        public void OnWaveCleared() { if (Data != null) Data.xp += ProgressionRules.XpWave; }

        public void OnRunEnded(int wave, int score, int bestComboThisRun)
        {
            if (Data == null) return;
            if (wave > Data.bestWave) Data.bestWave = wave;
            if (score > Data.bestScore) Data.bestScore = score;
            if (bestComboThisRun > Data.bestCombo) Data.bestCombo = bestComboThisRun;
            LeaderboardRules.Push(Data.leaderboard, score, wave, System.DateTime.Now.ToString("yyyy-MM-dd"));
            Persist();
        }

        public bool Prestige()
        {
            if (Data == null || !RankRules.CanPrestige(Level)) return false;
            Data.prestige++; Data.xp = 0; Persist(); return true; // web: doPrestige (reset XP seul)
        }

        void Update() { if (Data != null) Data.lifetime.playtime += Time.unscaledDeltaTime; }

        void OnApplicationPause(bool paused) { if (paused) Persist(); } // mobile : l'app peut être tuée ensuite
        void OnApplicationQuit() => Persist();
    }
}
