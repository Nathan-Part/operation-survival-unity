using System;
using System.Collections.Generic;
using Game.Data.Rules;

namespace Game.Net
{
    /// Un ennemi à faire apparaître : identité réseau, type, PV max déjà calculés (vague +
    /// difficulté + scaling coop + hpMult du type). La position est décidée en aval (glue/scène).
    public struct SpawnRequest
    {
        public int netId;
        public string typeId;
        public float maxHealth;
    }

    public enum WaveState
    {
        Idle = 0,
        Intermission = 1,
        Fighting = 2,
    }

    /// Progression des vagues, autoritaire côté hôte. Réutilise WaveRules (source de vérité des
    /// formules) et une composition injectée (WaveComposition.ForWave, passée en délégué pour
    /// éviter un cycle d'assembly). Rééquilibrée coop par CoopScaling. Pure et déterministe :
    /// avance par Tick(dt), émet des SpawnRequest, alloue les netId, détecte le nettoyage de vague.
    public class CoopWaveDirector
    {
        static readonly SpawnRequest[] EmptySpawns = new SpawnRequest[0];

        readonly CoopSessionConfig cfg;
        readonly Func<int, string[]> composition;
        readonly Func<string, float> enemyHpMult; // typeId -> EnemyData.hpMult (1 par défaut)
        readonly HashSet<int> aliveNetIds = new HashSet<int>();

        int playerCount;
        int nextNetId = 1;
        float intermissionTimer;

        public int CurrentWave { get; private set; }
        public WaveState State { get; private set; } = WaveState.Idle;
        public int AliveCount => aliveNetIds.Count;
        public float IntermissionRemaining => intermissionTimer;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCleared;

        public CoopWaveDirector(CoopSessionConfig cfg, Func<int, string[]> composition, Func<string, float> enemyHpMult, int playerCount)
        {
            this.cfg = cfg ?? new CoopSessionConfig();
            this.composition = composition ?? (w => new string[0]);
            this.enemyHpMult = enemyHpMult ?? (id => 1f);
            this.playerCount = playerCount < 1 ? 1 : playerCount;
        }

        public void SetPlayerCount(int count) => playerCount = count < 1 ? 1 : count;

        /// Démarre la boucle : première intermission avant la vague 1.
        public void Begin()
        {
            CurrentWave = 0;
            aliveNetIds.Clear();
            intermissionTimer = cfg.intermissionSeconds;
            State = WaveState.Intermission;
        }

        /// Avance d'un pas. Renvoie les ennemis à faire apparaître ce tick (vide la plupart du
        /// temps ; non vide uniquement au démarrage d'une nouvelle vague).
        public IReadOnlyList<SpawnRequest> Tick(float dt)
        {
            if (State == WaveState.Intermission)
            {
                intermissionTimer -= dt;
                if (intermissionTimer <= 0f)
                    return StartNextWave();
            }
            else if (State == WaveState.Fighting)
            {
                if (aliveNetIds.Count == 0)
                {
                    WaveCleared?.Invoke(CurrentWave);
                    intermissionTimer = cfg.intermissionSeconds;
                    State = WaveState.Intermission;
                }
            }
            return EmptySpawns;
        }

        List<SpawnRequest> StartNextWave()
        {
            CurrentWave++;
            var plan = composition(CurrentWave) ?? new string[0];
            int bonus = CoopScaling.EnemyCountBonus(playerCount, cfg);

            var requests = new List<SpawnRequest>(plan.Length + bonus);
            for (int i = 0; i < plan.Length; i++)
                requests.Add(MakeRequest(plan[i]));

            // Ennemis coop supplémentaires : on répète la composition de base.
            for (int i = 0; i < bonus && plan.Length > 0; i++)
                requests.Add(MakeRequest(plan[i % plan.Length]));

            for (int i = 0; i < requests.Count; i++)
                aliveNetIds.Add(requests[i].netId);

            State = WaveState.Fighting;
            WaveStarted?.Invoke(CurrentWave);
            return requests;
        }

        SpawnRequest MakeRequest(string typeId)
        {
            float baseHp = WaveRules.EnemyHpForWave(CurrentWave, cfg.baseEnemyHpMult);
            float hp = baseHp * CoopScaling.EnemyHpMult(playerCount, cfg) * enemyHpMult(typeId);
            if (hp < 1f) hp = 1f;
            return new SpawnRequest
            {
                netId = nextNetId++,
                typeId = typeId,
                maxHealth = hp,
            };
        }

        /// Signale qu'un ennemi est mort (retiré du décompte des vivants ; déclenche le
        /// nettoyage de vague au prochain Tick quand plus aucun n'est vivant).
        public void NotifyEnemyKilled(int netId) => aliveNetIds.Remove(netId);
    }
}
