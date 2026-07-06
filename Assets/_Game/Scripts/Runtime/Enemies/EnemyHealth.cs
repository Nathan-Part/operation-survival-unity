using System;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    /// Résultat d'un impact : zone touchée et issue (pour hitmarker/kill feed).
    public struct HitResult
    {
        public bool valid;
        public HitZone zone;
        public bool killed;
    }

    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] float armorDamageMult = 0.5f; // TODO(spec-web): réduction d'armure non extraite du web

        EnemyData data;
        HitZoneSettings zones;

        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsDead => Health <= 0;
        public EnemyData Data => data;

        public event Action<EnemyHealth> Damaged;
        public event Action<EnemyHealth> Died;

        public void Init(EnemyData data, int wave, float difficultyHpMult, HitZoneSettings zones)
        {
            this.data = data;
            this.zones = zones;
            Health = Mathf.Max(1, Mathf.RoundToInt(WaveRules.EnemyHpForWave(wave, difficultyHpMult) * data.hpMult));
            MaxHealth = Health;
        }

        /// Le cadavre n'est plus détruit ici (phase 3) : Died est levé une fois,
        /// l'EnemyController fige le corps et le WaveManager purge à la vague suivante.
        public HitResult TakeHit(Vector3 worldPoint, float weaponDmgMult)
        {
            if (Health <= 0 || data == null) return default;
            float yLocal = worldPoint.y - transform.position.y; // racine du GO = pieds
            HitZone zone = HitZoneResolver.Resolve(yLocal, data.scale, zones);
            Health -= HitZoneResolver.Damage(zone, zones, weaponDmgMult, data.armor, armorDamageMult);
            Damaged?.Invoke(this);
            bool killed = Health <= 0;
            if (killed) Died?.Invoke(this);
            return new HitResult { valid = true, zone = zone, killed = killed };
        }
    }
}
