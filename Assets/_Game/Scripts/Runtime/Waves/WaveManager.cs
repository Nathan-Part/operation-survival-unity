using System;
using System.Collections;
using System.Collections.Generic;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    public class WaveManager : MonoBehaviour
    {
        public GameContext context;
        public EnemySpawner spawner;
        public Transform[] spawnPoints = Array.Empty<Transform>();

        readonly List<EnemyHealth> alive = new List<EnemyHealth>();
        readonly List<GameObject> corpses = new List<GameObject>(); // cadavres gardés jusqu'à la vague suivante (parité web)
        Dictionary<string, EnemyData> byId;

        public int CurrentWave { get; private set; }
        public int Money { get; private set; }
        public float bonusMoneyMult = 1f; // web #58 : killstreak ×2 pendant 10 s
        public int AliveCount => alive.Count;
        public IReadOnlyList<EnemyHealth> Alive => alive;
        public ComboModel Combo { get; } = new ComboModel(); // web #29 : multiplicateur d'argent (HUD combo)

        public event Action<int> WaveStarted;
        public event Action<int> AliveCountChanged;
        public event Action<float> IntermissionStarted;
        public event Action<EnemyHealth, int> EnemyKilled; // ennemi + argent gagné (méta/XP)
        public event Action<int> WaveCleared;              // n° de vague nettoyée (XP_WAVE)
        public event Action CarePackageDue;                // web: caisse de ravitaillement auto toutes les 4 vagues

        public int BestComboThisRun { get; private set; }

        public void AddBonusMoney(int n) => Money += n;

        public void Begin()
        {
            byId = new Dictionary<string, EnemyData>();
            foreach (var e in context.database.enemies)
                if (e != null && !string.IsNullOrEmpty(e.id))
                    byId[e.id] = e;
            StartCoroutine(RunWaves());
        }

        IEnumerator RunWaves()
        {
            var config = context.database.waveConfig;
            while (true)
            {
                CurrentWave++;
                IntermissionStarted?.Invoke(config.intermissionSeconds);
                yield return new WaitForSeconds(config.intermissionSeconds);
                SpawnWave(CurrentWave);
                WaveStarted?.Invoke(CurrentWave);
                while (alive.Count > 0) yield return null;
                WaveCleared?.Invoke(CurrentWave);
                if (CurrentWave % 4 == 0) CarePackageDue?.Invoke(); // web: caisse auto toutes les 4 vagues
            }
        }

        void SpawnWave(int wave)
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError("WaveManager: no spawn points assigned.");
                return;
            }
            foreach (var corpse in corpses)
                if (corpse != null) Destroy(corpse);
            corpses.Clear();

            string[] plan = WaveComposition.ForWave(wave);
            for (int i = 0; i < plan.Length; i++)
            {
                if (!byId.TryGetValue(plan[i], out var data))
                {
                    Debug.LogWarning($"WaveManager: unknown enemy type '{plan[i]}' - falling back to shooter.");
                    if (!byId.TryGetValue("shooter", out data)) continue;
                }
                var enemy = spawner.Spawn(data, spawnPoints[i % spawnPoints.Length].position, wave);
                if (enemy == null) continue;
                alive.Add(enemy);
                enemy.Died += OnEnemyDied;
            }
            if (SpecialRules.VipAllowedOnWave(wave) && alive.Count > 0) // cible VIP dorée (web #140)
                alive[UnityEngine.Random.Range(0, alive.Count)].MarkVip();
            AliveCountChanged?.Invoke(alive.Count);
        }

        void OnEnemyDied(EnemyHealth enemy)
        {
            alive.Remove(enemy);
            if (enemy != null) corpses.Add(enemy.gameObject);
            int mult = Combo.RegisterKill(Time.time);
            if (Combo.Count > BestComboThisRun) BestComboThisRun = Combo.Count;
            bool vip = enemy != null && enemy.IsVip;
            var eco = context.database.economyConfig;
            var difficulty = GameContext.ResolveDifficulty(context.database);
            int gained = eco != null
                ? EconomyRules.MoneyForKill(eco.perKillMoney, mult, difficulty.moneyMult, vip, SpecialRules.VipMoneyMult)
                : 0; // web: MONEY_PER_KILL × combo × difficulté × VIP
            Money += Mathf.RoundToInt(gained * bonusMoneyMult); // web #58 : killstreak ×2
            if (vip) CalloutEvents.Raise("VIP ÉLIMINÉ!"); // web #140
            EnemyKilled?.Invoke(enemy, gained); // montant de base (hors bonus killstreak) pour l'XP/méta
            AliveCountChanged?.Invoke(alive.Count);
        }

        void Update() => Combo.Tick(Time.time);
    }
}
