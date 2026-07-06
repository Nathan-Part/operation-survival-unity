using System;
using System.Collections;
using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Runtime
{
    public class WaveManager : MonoBehaviour
    {
        public GameContext context;
        public EnemySpawner spawner;
        public Transform[] spawnPoints = Array.Empty<Transform>();

        readonly List<EnemyHealth> alive = new List<EnemyHealth>();
        Dictionary<string, EnemyData> byId;

        public int CurrentWave { get; private set; }
        public int Money { get; private set; }
        public int AliveCount => alive.Count;
        public IReadOnlyList<EnemyHealth> Alive => alive;

        public event Action<int> WaveStarted;
        public event Action<int> AliveCountChanged;
        public event Action<float> IntermissionStarted;

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
            }
        }

        void SpawnWave(int wave)
        {
            if (spawnPoints.Length == 0)
            {
                Debug.LogError("WaveManager: no spawn points assigned.");
                return;
            }
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
            AliveCountChanged?.Invoke(alive.Count);
        }

        void OnEnemyDied(EnemyHealth enemy)
        {
            alive.Remove(enemy);
            var eco = context.database.economyConfig;
            if (eco != null) Money += eco.perKillMoney;
            AliveCountChanged?.Invoke(alive.Count);
        }
    }
}
