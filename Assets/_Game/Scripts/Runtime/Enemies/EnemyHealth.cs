using System;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] float armorDamageMult = 0.5f; // TODO(spec-web): réduction d'armure non extraite du web

        EnemyData data;
        HitZoneSettings zones;

        public int Health { get; private set; }
        public EnemyData Data => data;

        public event Action<EnemyHealth> Died;

        public void Init(EnemyData data, int wave, float difficultyHpMult, HitZoneSettings zones)
        {
            this.data = data;
            this.zones = zones;
            Health = Mathf.Max(1, Mathf.RoundToInt(WaveRules.EnemyHpForWave(wave, difficultyHpMult) * data.hpMult));
        }

        public void TakeHit(Vector3 worldPoint, float weaponDmgMult)
        {
            if (Health <= 0 || data == null) return;
            float yLocal = worldPoint.y - transform.position.y; // racine du GO = pieds
            HitZone zone = HitZoneResolver.Resolve(yLocal, data.scale, zones);
            Health -= HitZoneResolver.Damage(zone, zones, weaponDmgMult, data.armor, armorDamageMult);
            if (Health <= 0)
            {
                Died?.Invoke(this);
                if (Application.isPlaying) Destroy(gameObject); // Destroy interdit en EditMode (tests)
            }
        }
    }
}
