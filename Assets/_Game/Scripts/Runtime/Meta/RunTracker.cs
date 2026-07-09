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
        WeaponController weapon;
        PlayerMelee melee;
        RunStats run;
        int lastLevel;
        float waveStartTime;

        /// Snapshot en lecture seule des stats de la run en cours — consommé par GameOverScreen
        /// (Phase 5.1, 2026-07-09). `run` reste un champ privé, ceci n'expose qu'une copie (struct).
        public RunStats CurrentStats => run;

        public void Init(MetaProgress meta, GameDatabase db, WaveManager waves, WeaponController weapon, PlayerMelee melee = null)
        {
            this.meta = meta;
            this.db = db;
            this.waves = waves;
            this.weapon = weapon;
            this.melee = melee;
            lastLevel = meta.Level;
            waveStartTime = Time.time;

            var now = DateTime.Now;
            DailyContractRules.EnsureToday(meta.Data.daily,
                DailyContractRules.SeedFor(now.Year, now.Month, now.Day), db.contracts);

            waves.EnemyKilled += OnKill;
            waves.WaveCleared += OnWaveCleared;
            waves.WaveStarted += OnWaveStarted;
            if (melee != null) melee.MeleeKill += OnMeleeKill;
            if (weapon != null)
            {
                weapon.Fired += OnFired;
                weapon.HitConfirmed += OnHitConfirmed;
            }
        }

        void OnDestroy()
        {
            if (melee != null) melee.MeleeKill -= OnMeleeKill;
            if (weapon != null)
            {
                weapon.Fired -= OnFired;
                weapon.HitConfirmed -= OnHitConfirmed;
            }
            if (waves == null) return;
            waves.EnemyKilled -= OnKill;
            waves.WaveCleared -= OnWaveCleared;
            waves.WaveStarted -= OnWaveStarted;
        }

        void OnKill(EnemyHealth enemy, int money)
        {
            run.runKills++;
            bool head = enemy != null && enemy.LastHitHead;
            if (head) run.runHeadshots++;
            AdvanceContract("kills", 1);
            if (head) AdvanceContract("headshots", 1);
            run.combo = Mathf.Max(run.combo, waves.BestComboThisRun);
            run.score = waves.Money; // simplification déjà en place (voir doc RunStats.score)
            CheckAchievements();
            CheckLevelUp();
        }

        /// Coup de mêlée fatal (web #45) : distinct de OnKill (déjà déclenché en cascade
        /// via WaveManager.EnemyKilled pour "kills"/"headshots") pour alimenter le contrat
        /// "melee" (melee15/melee5, unity-spec/contracts.json), sinon jamais complétable.
        void OnMeleeKill() => AdvanceContract("melee", 1);

        // Écran de fin de partie riche (Phase 5.1) : précision (shotsFired/shotsHit) et meilleur
        // coup (distance du tir confirmé le plus loin) — aucune de ces 2 valeurs n'était trackée
        // avant cette session (RunTracker.Init prenait déjà `weapon` en paramètre mais ne l'utilisait
        // jamais, champ mort).
        void OnFired() => run.shotsFired++;

        void OnHitConfirmed(HitZone zone, bool killed, EnemyHealth target)
        {
            run.shotsHit++;
            if (weapon == null || target == null) return;
            float distance = Vector3.Distance(weapon.transform.position, target.transform.position);
            if (distance > run.bestHitDistance) run.bestHitDistance = distance;
        }

        void OnWaveStarted(int wave) => waveStartTime = Time.time;

        void OnWaveCleared(int wave)
        {
            run.wave = wave;
            float duration = Time.time - waveStartTime;
            if (duration > 0f && (run.fastestWaveSeconds <= 0f || duration < run.fastestWaveSeconds))
                run.fastestWaveSeconds = duration;
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
