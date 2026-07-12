using System;
using System.Collections.Generic;

namespace Game.Net
{
    /// Orchestrateur autoritaire de la session coop (tourne côté hôte). Il détient le registre
    /// des joueurs, les ennemis, l'argent partagé, le directeur de vagues et le validateur de
    /// dégâts, et arbitre la boucle down/relève/respawn/wipe. Pur et déterministe : piloté par
    /// Tick(dt) et des méthodes de commande. Entièrement testable en EditMode.
    public class CoopSession
    {
        readonly CoopSessionConfig cfg;
        readonly Dictionary<int, CoopPlayerState> players = new Dictionary<int, CoopPlayerState>();
        readonly Dictionary<int, EnemyRuntimeState> enemies = new Dictionary<int, EnemyRuntimeState>();
        readonly CoopWaveDirector director;
        readonly DamageValidator validator;

        uint tick;
        float clock;

        public SessionPhase Phase { get; private set; } = SessionPhase.Lobby;
        public int SharedMoney { get; private set; }
        public CoopSessionConfig Config => cfg;
        public CoopWaveDirector Director => director;
        public DamageValidator Validator => validator;
        public int PlayerCount => players.Count;
        public int AliveEnemyCount => director.AliveCount;
        public IReadOnlyDictionary<int, CoopPlayerState> Players => players;
        public IReadOnlyDictionary<int, EnemyRuntimeState> Enemies => enemies;

        public event Action<CoopPlayerState> PlayerJoined;
        public event Action<CoopPlayerState> PlayerLeft;
        public event Action<CoopPlayerState> PlayerDowned;
        public event Action<CoopPlayerState> PlayerRevived;
        public event Action<CoopPlayerState> PlayerDied;
        public event Action<CoopPlayerState> PlayerRespawned;
        public event Action<SpawnRequest> EnemySpawned;
        public event Action<EnemyRuntimeState> EnemyKilled;
        public event Action GameOver;

        public CoopSession(CoopSessionConfig config, Func<int, string[]> composition, Func<string, float> enemyHpMult)
        {
            cfg = config ?? new CoopSessionConfig();
            validator = new DamageValidator(cfg);
            director = new CoopWaveDirector(cfg, composition, enemyHpMult, 1);
        }

        // ---------- Lobby / joueurs ----------

        public CoopPlayerState AddPlayer(PlayerRef player, string name, string characterId, float maxHealth)
        {
            if (!player.IsValid) return null;
            if (players.TryGetValue(player.Id, out var existing)) return existing;
            if (players.Count >= cfg.maxPlayers) return null;

            float hp = maxHealth <= 0f ? 100f : maxHealth;
            var p = new CoopPlayerState
            {
                player = player,
                name = name,
                characterId = characterId,
                maxHealth = hp,
                health = hp,
            };
            players[player.Id] = p;
            director.SetPlayerCount(players.Count);
            PlayerJoined?.Invoke(p);
            return p;
        }

        public void RemovePlayer(PlayerRef player)
        {
            if (!players.TryGetValue(player.Id, out var p)) return;
            players.Remove(player.Id);
            director.SetPlayerCount(Math.Max(1, players.Count));
            PlayerLeft?.Invoke(p);
            if (Phase == SessionPhase.Active) CheckWipe();
        }

        public CoopPlayerState GetPlayer(PlayerRef player) =>
            players.TryGetValue(player.Id, out var p) ? p : null;

        // ---------- Cycle de partie ----------

        public void StartMatch()
        {
            if (Phase != SessionPhase.Lobby) return;
            Phase = SessionPhase.Active;
            director.Begin();
        }

        public void Tick(float dt)
        {
            if (Phase != SessionPhase.Active) return;
            tick++;
            clock += dt;

            var spawns = director.Tick(dt);
            for (int i = 0; i < spawns.Count; i++)
            {
                var r = spawns[i];
                enemies[r.netId] = new EnemyRuntimeState
                {
                    netId = r.netId,
                    typeId = r.typeId,
                    health = r.maxHealth,
                    maxHealth = r.maxHealth,
                };
                EnemySpawned?.Invoke(r);
            }

            TickPlayers(dt);
        }

        // ---------- Entrées & dégâts ----------

        /// Client -> hôte : met à jour la réplique du transform/visée du joueur.
        public void ApplyInput(PlayerRef player, PlayerInputCommand cmd)
        {
            if (!players.TryGetValue(player.Id, out var p)) return;
            p.position = cmd.position;
            p.aimYaw = cmd.aimYaw;
            p.aimPitch = cmd.aimPitch;
        }

        /// Client -> hôte : applique un dégât à un ennemi APRÈS validation. Renvoie le verdict.
        public DamageValidator.Result ApplyDamageClaim(DamageClaim claim)
        {
            var attacker = GetPlayer(claim.attacker);
            enemies.TryGetValue(claim.enemyNetId, out var enemy);
            var result = validator.Validate(claim, attacker, enemy, clock);
            if (result != DamageValidator.Result.Accepted) return result;

            enemy.health -= claim.amount;
            if (enemy.health <= 0f)
            {
                enemy.health = 0f;
                enemy.lifecycle = EnemyLifecycle.Dying;
                director.NotifyEnemyKilled(enemy.netId);
                SharedMoney += cfg.perKillMoney;
                if (attacker != null) attacker.kills++;
                EnemyKilled?.Invoke(enemy);
            }
            return result;
        }

        /// Un ennemi (simulé par l'hôte) inflige des dégâts à un joueur. Toujours appliqué.
        public void DamagePlayer(PlayerRef target, float amount)
        {
            if (amount <= 0f) return;
            if (!players.TryGetValue(target.Id, out var p)) return;
            if (!p.IsAlive) return;
            p.health -= amount;
            if (p.health <= 0f)
            {
                p.health = 0f;
                DownPlayer(p);
            }
        }

        // ---------- Position ennemi & nettoyage (appelés par la glue) ----------

        public void SetEnemyPosition(int netId, NetVector3 pos)
        {
            if (enemies.TryGetValue(netId, out var e)) e.position = pos;
        }

        /// Retire définitivement un ennemi (après l'anim de mort côté glue).
        public void RemoveEnemy(int netId)
        {
            if (enemies.TryGetValue(netId, out var e))
            {
                e.lifecycle = EnemyLifecycle.Dead;
                enemies.Remove(netId);
            }
        }

        // ---------- Boucle joueurs ----------

        void TickPlayers(float dt)
        {
            foreach (var p in players.Values)
            {
                if (p.isDown)
                {
                    if (HasReviverInRange(p))
                    {
                        p.reviveProgress += dt;
                        if (p.reviveProgress >= cfg.reviveSeconds) RevivePlayer(p);
                    }
                    else
                    {
                        p.reviveProgress = 0f;
                        p.bleedOut -= dt;
                        if (p.bleedOut <= 0f) KillPlayer(p);
                    }
                }
                else if (p.isDead)
                {
                    p.respawnTimer -= dt;
                    if (p.respawnTimer <= 0f) RespawnPlayer(p);
                }
            }
            CheckWipe();
        }

        bool HasReviverInRange(CoopPlayerState downed)
        {
            foreach (var other in players.Values)
            {
                if (ReferenceEquals(other, downed)) continue;
                if (!other.IsAlive) continue;
                if (NetVector3.Distance(other.position, downed.position) <= cfg.reviveRadius) return true;
            }
            return false;
        }

        void DownPlayer(CoopPlayerState p)
        {
            p.isDown = true;
            p.bleedOut = cfg.bleedOutSeconds;
            p.reviveProgress = 0f;
            PlayerDowned?.Invoke(p);
            CheckWipe();
        }

        void RevivePlayer(CoopPlayerState p)
        {
            p.isDown = false;
            p.reviveProgress = 0f;
            p.bleedOut = 0f;
            p.health = p.maxHealth * cfg.reviveHealthFraction;
            PlayerRevived?.Invoke(p);
        }

        void KillPlayer(CoopPlayerState p)
        {
            p.isDown = false;
            p.isDead = true;
            p.health = 0f;
            p.deaths++;
            p.respawnTimer = cfg.respawnSeconds;
            PlayerDied?.Invoke(p);
            CheckWipe();
        }

        void RespawnPlayer(CoopPlayerState p)
        {
            p.isDead = false;
            p.isDown = false;
            p.respawnTimer = 0f;
            p.health = p.maxHealth * cfg.respawnHealthFraction;
            PlayerRespawned?.Invoke(p);
        }

        void CheckWipe()
        {
            if (Phase != SessionPhase.Active) return;
            if (players.Count == 0) return; // partie vide : on attend des joueurs, pas un wipe
            foreach (var p in players.Values)
                if (p.IsAlive) return;
            Phase = SessionPhase.GameOver;
            GameOver?.Invoke();
        }

        // ---------- Snapshot ----------

        public WorldSnapshot BuildSnapshot()
        {
            var ps = new PlayerNetState[players.Count];
            int i = 0;
            foreach (var p in players.Values)
            {
                ps[i++] = new PlayerNetState
                {
                    player = p.player,
                    position = p.position,
                    aimYaw = p.aimYaw,
                    health = p.health,
                    maxHealth = p.maxHealth,
                    isDown = p.isDown,
                    isDead = p.isDead,
                    kills = p.kills,
                };
            }

            var es = new EnemyNetState[enemies.Count];
            int j = 0;
            foreach (var e in enemies.Values)
            {
                es[j++] = new EnemyNetState
                {
                    netId = e.netId,
                    typeId = e.typeId,
                    position = e.position,
                    health = e.health,
                    maxHealth = e.maxHealth,
                    lifecycle = e.lifecycle,
                };
            }

            return new WorldSnapshot
            {
                tick = tick,
                wave = director.CurrentWave,
                aliveEnemies = director.AliveCount,
                phase = Phase,
                sharedMoney = SharedMoney,
                players = ps,
                enemies = es,
            };
        }
    }
}
