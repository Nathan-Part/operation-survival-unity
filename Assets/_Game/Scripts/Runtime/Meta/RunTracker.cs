using System;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    /// Suivi d'une partie (Lot 6) : alimente `RunStats` à partir des events de
    /// `WaveManager`, évalue succès/contrats à chaque event et notifie les
    /// passages de niveau. Émet des toasts consommés par `HudToasts` — les
    /// annonces de combat (BLOQUÉ!/VIP/mêlée...) restent sur `CalloutEvents`.
    ///
    /// Contrats "wave" (ex. wave5/wave10, web #62) : `DailyContractRules.Advance`
    /// ne renvoie que le DERNIER contrat franchi si un seul appel fait franchir
    /// simultanément plusieurs seuils du même type (limite connue, tâche 21).
    /// `WaveManager.WaveCleared` étant levé une fois par vague nettoyée, dans
    /// l'ordre (jamais de saut de numéro), on avance le contrat "wave" de +1 à
    /// chaque event plutôt qu'en valeur absolue : un seul seuil peut alors être
    /// franchi par appel, donc wave5 et wave10 ne peuvent jamais se marquer
    /// "done" dans le même appel — aucun signal de complétion n'est perdu.
    public class RunTracker : MonoBehaviour
    {
        public event Action<string> Toast;

        MetaProgress meta;
        GameDatabase db;
        WaveManager waves;
        RunStats run;
        int lastLevel;

        public void Init(MetaProgress meta, GameDatabase db, WaveManager waves, WeaponController weapon)
        {
            this.meta = meta;
            this.db = db;
            this.waves = waves;
            lastLevel = meta.Level;

            var now = DateTime.Now;
            DailyContractRules.EnsureToday(meta.Data.daily,
                DailyContractRules.SeedFor(now.Year, now.Month, now.Day), db.contracts);

            waves.EnemyKilled += OnKill;
            waves.WaveCleared += OnWaveCleared;
        }

        void OnDestroy()
        {
            if (waves == null) return;
            waves.EnemyKilled -= OnKill;
            waves.WaveCleared -= OnWaveCleared;
        }

        void OnKill(EnemyHealth enemy, int money)
        {
            run.runKills++;
            bool head = enemy != null && enemy.LastHitHead;
            if (head) run.runHeadshots++;
            AdvanceContract("kills", 1);
            if (head) AdvanceContract("headshots", 1);
            run.combo = Mathf.Max(run.combo, waves.BestComboThisRun);
            CheckAchievements();
            CheckLevelUp();
        }

        void OnWaveCleared(int wave)
        {
            run.wave = wave;
            AdvanceContract("wave", 1); // +1 par vague nettoyée : jamais plus d'un seuil franchi par appel (voir doc de classe)
            CheckAchievements();
            CheckLevelUp();
        }

        void AdvanceContract(string type, int amount)
        {
            var done = DailyContractRules.Advance(meta.Data.daily, db.contracts, type, amount);
            if (done == null) return;
            meta.Data.money += done.reward;
            run.contractsDone++;
            meta.Persist();
            Toast?.Invoke($"CONTRAT TERMINÉ +{done.reward}$");
        }

        void CheckAchievements()
        {
            foreach (var a in db.achievements)
            {
                if (a == null || meta.Data.unlockedAchievements.Contains(a.id)) continue;
                if (!AchievementRules.IsUnlocked(a, run, meta.Data)) continue;
                meta.Data.unlockedAchievements.Add(a.id);
                meta.Persist();
                Toast?.Invoke($"SUCCÈS : {a.displayName}");
            }
        }

        void CheckLevelUp()
        {
            int lvl = meta.Level;
            if (lvl <= lastLevel) return;
            lastLevel = lvl;
            Toast?.Invoke($"NIVEAU {lvl} — {RankRules.TitleFor(lvl)}");
        }
    }
}
