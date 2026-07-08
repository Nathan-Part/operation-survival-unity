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

            brain = new EnemyBrain(stats.detectRadius, stats.loseRadius, stats.loseTime, attackRange);
            agent.stoppingDistance = Mathf.Max(0f, attackRange - 0.5f);
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
            var state = brain.Tick(dt, dist);
            attackTimer -= dt;

            switch (state)
            {
                case EnemyState.Idle:
                    if (agent.hasPath) agent.ResetPath();
                    break;
                case EnemyState.Chase:
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    break;
                case EnemyState.Attack:
                    agent.isStopped = true;
                    FaceTarget(dt);
                    TickAttack(dt);
                    break;
            }
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
