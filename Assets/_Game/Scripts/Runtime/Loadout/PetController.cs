using UnityEngine;
using UnityEngine.AI;

namespace Game.Runtime
{
    /// Chien de guerre (web pet.js) : suit/poursuit/attaque via PetBrain, navigue au NavMesh,
    /// despawn après 30 s ou à 0 PV. Riposte des ennemis encaissée uniquement sur morsure réussie.
    public class PetController : MonoBehaviour
    {
        const float Hp0 = 150f, Despawn = 30f, AtkDmg = 12f, AtkCd = 1.4f, HitRange = 1.9f, Retaliate = 10f, SpeedFollow = 3.4f, SpeedChase = 4.6f; // web: pet.js

        public float Hp { get; private set; } = Hp0;
        public float MaxHp => Hp0;
        public float TimeLeft => Mathf.Max(0f, diesAt - Time.time);

        /// Dernier chien invoqué, pour que le HUD retrouve l'instance active sans câblage supplémentaire.
        public static PetController Current { get; private set; }

        Transform player;
        WaveManager waves;
        NavMeshAgent agent;
        PetState state = PetState.Idle;
        float diesAt, nextAtk;

        public static PetController Summon(Vector3 pos, Transform player, WaveManager waves, GameObject model)
        {
            var go = new GameObject("WarDog");
            go.transform.position = pos;
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.4f;
            agent.height = 0.9f;
            agent.speed = SpeedFollow;
            if (model != null)
            {
                var v = Instantiate(model, go.transform);
                v.transform.localScale = Vector3.one * 0.5f; // moitié du joueur (README)
            }
            var p = go.AddComponent<PetController>();
            p.player = player;
            p.waves = waves;
            p.agent = agent;
            p.diesAt = Time.time + Despawn;
            Current = p;
            return p;
        }

        EnemyHealth Nearest(out float dist)
        {
            EnemyHealth best = null;
            dist = float.MaxValue;
            foreach (var e in waves.Alive)
            {
                if (e == null || e.IsDead) continue;
                float d = Vector3.Distance(transform.position, e.transform.position);
                if (d < dist) { dist = d; best = e; }
            }
            return best;
        }

        void Update()
        {
            if (Time.time >= diesAt || Hp <= 0f) { Destroy(gameObject); return; }
            var target = Nearest(out float dt);
            bool has = target != null && dt <= PetBrain.DetectR;
            float dp = player != null ? Vector3.Distance(transform.position, player.position) : 0f;
            state = PetBrain.Decide(state, has, has ? dt : 99f, dp);
            switch (state)
            {
                case PetState.Follow:
                    agent.speed = SpeedFollow;
                    if (player) agent.SetDestination(player.position);
                    break;
                case PetState.Chase:
                    agent.speed = SpeedChase;
                    if (target) agent.SetDestination(target.transform.position);
                    break;
                case PetState.Attack:
                    agent.ResetPath();
                    if (target != null && Time.time >= nextAtk && dt <= HitRange)
                    {
                        target.ApplyDamage(Mathf.RoundToInt(AtkDmg));
                        nextAtk = Time.time + AtkCd;
                        if (!target.IsDead) Hp -= Retaliate; // riposte web
                    }
                    break;
                default:
                    agent.ResetPath();
                    break;
            }
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
