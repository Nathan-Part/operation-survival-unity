using UnityEngine;
namespace Game.Runtime
{
    /// Coup de mêlée (web #45, touche X) : 150 dégâts dans 2.6 m devant, cooldown 0.6 s.
    public class PlayerMelee : MonoBehaviour
    {
        const float Reach = 2.6f, Damage = 150f, Cooldown = 0.6f, ConeDot = 0.3f; // web: REACH=2.6, DAMAGE=150, cooldown=0.6
        Transform aim; WaveManager waves; float nextAt;
        public void Init(Transform aim, WaveManager waves) { this.aim = aim; this.waves = waves; }
        public void Strike()
        {
            if (Time.time < nextAt || aim == null) return;
            nextAt = Time.time + Cooldown;
            EnemyHealth best = null; float bestD = Reach;
            foreach (var e in waves.Alive)
            {
                if (e == null || e.IsDead) continue;
                var to = e.transform.position - aim.position; float d = to.magnitude;
                if (d > bestD) continue;
                if (Vector3.Dot(aim.forward, to.normalized) < ConeDot) continue; // dans le cône avant
                bestD = d; best = e;
            }
            if (best != null) { best.ApplyDamage(Mathf.RoundToInt(Damage)); CalloutEvents.Raise("MÊLÉE"); }
        }
    }
}
