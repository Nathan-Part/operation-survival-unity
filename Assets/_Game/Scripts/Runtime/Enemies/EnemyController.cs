using System;
using Game.Data;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Runtime
{
    /// Archétypes ranged (rafales hitscan à probabilité `accuracy`, fidèle au web)
    /// et melee (coup au contact). Comportements spéciaux différés (décision D5).
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : MonoBehaviour
    {
        NavMeshAgent agent;
        EnemyBrain brain;
        EnemyData data;
        EnemyBaseStats stats;
        Transform player;
        PlayerHealth playerHealth;
        PlayerStatusEffects playerStatus;
        EnemyHealth health;

        float attackDamage;
        float attackCooldown;
        float attackRange;
        float attackTimer;
        int burstRemaining;
        float burstTimer;

        float suppressedUntil = float.NegativeInfinity;
        float repathTimer;
        const float RepathInterval = 0.2f; // recalcul du chemin throttlé (au lieu de chaque frame)
        bool hasPatrolTarget;
        EnemyState lastMoveState = EnemyState.Dead; // force le réglage de vitesse au premier Tick

        public EnemyState State => brain != null ? brain.State : EnemyState.Idle;

        /// Posés à la mort (EnterDeath) pour le ramassage d'arme (G, web #tranche23) :
        /// id de l'arme lâchée par ce cadavre et flag « déjà pillé » (WeaponPickup).
        public string DropWeaponId { get; private set; }
        public bool Looted { get; private set; }
        public void MarkLooted() => Looted = true;

        /// EMP/flashbang (Lot 4-5) : l'ennemi ne bouge plus et ne tire plus pendant `seconds`.
        public bool IsSuppressed => Time.time < suppressedUntil;
        public void Suppress(float seconds) => suppressedUntil = Time.time + seconds;

        /// Levé à chaque coup tiré (rafale ranged) : muzzle flash côté visuel.
        public event Action Fired;

        public void Init(EnemyData data, EnemyCombatConfig combat, Transform player, DifficultySettings difficulty)
        {
            this.data = data;
            stats = combat.baseStats;
            this.player = player;
            playerHealth = player != null ? player.GetComponentInParent<PlayerHealth>() : null;
            playerStatus = player != null ? player.GetComponentInParent<PlayerStatusEffects>() : null;
            health = GetComponent<EnemyHealth>();
            agent = GetComponent<NavMeshAgent>();

            attackRange = data.melee ? stats.meleeRange
                : data.hasShootOverride && data.shootRangeOverride > 0f ? data.shootRangeOverride
                : stats.shootRange;
            attackCooldown = data.melee ? stats.meleeCooldown
                : data.hasShootOverride && data.shootCdOverride > 0f ? data.shootCdOverride
                : stats.fireCooldown;
            float baseDamage = data.melee ? stats.meleeDamage : stats.damage;
            attackDamage = baseDamage * data.damageMult * difficulty.enemyDmgMult;

            // Parité web (main.js ~4516-4518) : un ranged détecte au moins jusqu'à sa portée
            // de tir (sniper 28 m), abandonne à max(lose, portée*1.3) ; sortie d'attaque à
            // hystérésis (×1.25 ranged / ×1.4 melee) pour éviter le clignotement d'état.
            float detect = data.melee ? stats.detectRadius : Mathf.Max(stats.detectRadius, attackRange);
            float lose = data.melee ? stats.loseRadius : Mathf.Max(stats.loseRadius, attackRange * 1.3f);
            float exitRange = attackRange * (data.melee ? 1.4f : 1.25f);
            brain = new EnemyBrain(detect, lose, stats.loseTime, attackRange, exitRange);
            // Plafonné à 6 m : un ranged s'approche de sa cible au lieu de camper à 27 m
            // (le passage en Attack est décidé par la FSM, pas par le stoppingDistance).
            agent.stoppingDistance = Mathf.Max(0f, Mathf.Min(attackRange - 0.5f, 6f));
        }

        /// Mort (parité web killEnemy) : FSM figée, plus de navigation ni de cible.
        /// Le collider est coupé (cadavre traversable, non ciblable) ; le corps reste
        /// jusqu'à la purge de la vague suivante (WaveManager).
        public void EnterDeath()
        {
            if (brain != null) brain.Kill();
            if (agent != null && agent.enabled) { agent.isStopped = true; agent.enabled = false; }
            var capsule = GetComponent<CapsuleCollider>();
            if (capsule != null) capsule.enabled = false;
            DropWeaponId = EnemyWeaponDrop.ForEnemy(data);
        }

        void Update()
        {
            if (player == null || brain == null) return;
            if (brain.State == EnemyState.Dead) return;
            if (IsSuppressed) { agent.isStopped = true; return; }
            float dt = Time.deltaTime;
            float dist = Vector3.Distance(transform.position, player.position);
            var state = brain.Tick(dt, dist, HasLineOfSight());
            attackTimer -= dt;

            // Vitesse selon l'état (web : marche en patrouille, course en chasse) — posée
            // uniquement au changement d'état pour ne pas écraser les modifs d'EnemySpecials.
            if (state != lastMoveState)
            {
                agent.speed = (state == EnemyState.Idle ? stats.walkSpeed : stats.runSpeed) * data.speedMult;
                lastMoveState = state;
            }

            switch (state)
            {
                case EnemyState.Idle:
                    TickPatrol();
                    break;
                case EnemyState.Chase:
                    agent.isStopped = false;
                    hasPatrolTarget = false;
                    TickChaseDestination(dt);
                    break;
                case EnemyState.Attack:
                    agent.isStopped = true;
                    hasPatrolTarget = false;
                    FaceTarget(dt);
                    TickAttack(dt);
                    break;
            }
        }

        /// Patrouille web (pickPatrol, main.js ~4607) : déambule en marchant vers des points
        /// d'un anneau autour du joueur (detect + 0..8) — l'ennemi dérive vers le combat au
        /// lieu de rester planté au spawn tant que personne n'approche.
        void TickPatrol()
        {
            if (!agent.isOnNavMesh) return;
            agent.isStopped = false;
            if (hasPatrolTarget && (agent.pathPending || agent.remainingDistance > 0.6f)) return;
            float r = stats.detectRadius + UnityEngine.Random.value * 8f;
            float a = UnityEngine.Random.value * Mathf.PI * 2f;
            Vector3 target = player.position + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            if (UnityEngine.AI.NavMesh.SamplePosition(target, out var navHit, 4f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.SetDestination(navHit.position);
                hasPatrolTarget = true;
            }
        }

        /// Destination projetée sur le NavMesh (le joueur peut être hors mesh : viser sa
        /// position brute donne un PathPartial qui colle l'agent contre les murs) + throttle.
        void TickChaseDestination(float dt)
        {
            repathTimer -= dt;
            if (repathTimer > 0f) return;
            repathTimer = RepathInterval;
            if (!agent.isOnNavMesh) return;
            if (UnityEngine.AI.NavMesh.SamplePosition(player.position, out var navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                agent.SetDestination(navHit.position);
            else
                agent.SetDestination(player.position); // repli : l'agent ira au plus proche joignable
        }

        /// Ligne de vue vers le joueur (yeux ~1,5 m -> torse ~1 m) : un obstacle entre les
        /// deux force la poursuite au lieu du tir à travers/derrière les murs.
        bool HasLineOfSight()
        {
            Vector3 origin = transform.position + Vector3.up * 1.5f;
            Vector3 target = player.position + Vector3.up * 1.0f;
            Vector3 dir = target - origin;
            float len = dir.magnitude;
            if (len < 0.01f) return true;
            if (Physics.Raycast(origin, dir / len, out RaycastHit rh, len))
                return rh.transform == player || rh.transform.IsChildOf(player);
            return true;
        }

        void FaceTarget(float dt)
        {
            Vector3 dir = player.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), stats.turnSpeed * dt);
        }

        void TickAttack(float dt)
        {
            if (playerHealth == null) return;
            if (data.melee)
            {
                if (attackTimer <= 0f)
                {
                    if (data.id == "bomber") { health?.Kill(); return; } // kamikaze : explose au contact (web #37)
                    playerHealth.TakeDamage(attackDamage * EnemySpecials.CurrentCommanderMult, transform.position);
                    attackTimer = attackCooldown;
                }
                return;
            }
            if (burstRemaining > 0)
            {
                burstTimer -= dt;
                if (burstTimer <= 0f)
                {
                    Fired?.Invoke();
                    if (UnityEngine.Random.value <= stats.accuracy)
                    {
                        playerHealth.TakeDamage(attackDamage * EnemySpecials.CurrentCommanderMult, transform.position); // aura commander (web #151)
                        if (data.id == "stunner" && playerStatus != null) { playerStatus.ApplyStun(); CalloutEvents.Raise("STUNNÉ!"); } // web #121
                    }
                    burstRemaining--;
                    burstTimer = stats.burstGap;
                }
            }
            else if (attackTimer <= 0f)
            {
                burstRemaining = Mathf.Max(1, data.burst);
                burstTimer = 0f;
                attackTimer = attackCooldown;
            }
        }
    }
}
