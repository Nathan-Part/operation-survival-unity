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
        public bool blocked; // bouclier frontal du shielder (web #119) : aucun dégât
    }

    public class EnemyHealth : MonoBehaviour
    {
        [SerializeField] float armorDamageMult = 0.5f; // TODO(spec-web): réduction d'armure non extraite du web

        EnemyData data;
        HitZoneSettings zones;

        public int Health { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsDead => Health <= 0;
        public bool IsVip { get; private set; }
        public EnemyData Data => data;

        /// Posé par EnemySpecials (shielder) : retourne true si le tir venant de
        /// `shooterPos` est bloqué. Null pour tous les autres types.
        public Func<Vector3, bool> BlockCheck;

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
        public HitResult TakeHit(Vector3 worldPoint, float weaponDmgMult) =>
            TakeHit(worldPoint, weaponDmgMult, transform.position); // sans position de tireur : jamais bloqué (dot=0)

        public HitResult TakeHit(Vector3 worldPoint, float weaponDmgMult, Vector3 shooterPos)
        {
            if (Health <= 0 || data == null) return default;
            float yLocal = worldPoint.y - transform.position.y; // racine du GO = pieds
            HitZone zone = HitZoneResolver.Resolve(yLocal, data.scale, zones);
            if (BlockCheck != null && BlockCheck(shooterPos))
                return new HitResult { valid = true, zone = zone, blocked = true }; // web #119 : dégâts 0
            Health -= HitZoneResolver.Damage(zone, zones, weaponDmgMult, data.armor, armorDamageMult);
            Damaged?.Invoke(this);
            bool killed = Health <= 0;
            if (killed) Died?.Invoke(this);
            return new HitResult { valid = true, zone = zone, killed = killed };
        }

        /// Soin du medic (web #120), plafonné aux PV max.
        public void Heal(int amount)
        {
            if (Health <= 0 || amount <= 0) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        /// Mort directe sans zone d'impact (bomber au contact, web #37).
        public void Kill()
        {
            if (Health <= 0) return;
            Health = 0;
            Died?.Invoke(this);
        }

        /// Cible VIP (web #140) : teinte or, argent ×2 à la mort.
        public void MarkVip()
        {
            IsVip = true;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                foreach (var material in renderer.materials)
                {
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(1f, 0.843f, 0f)); // web: 0xffd700
                    else if (material.HasProperty("_Color")) material.color = new Color(1f, 0.843f, 0f);
                }
        }
    }
}
