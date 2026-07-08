using System;
using Game.Data;
using Game.Data.Rules;
using UnityEngine;

namespace Game.Runtime
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] float regenDelaySeconds = 3f; // TODO(spec-web): délai de régén non extrait du web (0 = continue)

        public Func<float> timeSource = () => Time.time; // seam de test (assembly de tests séparé)

        float regenRate;
        float lastDamageTime = float.NegativeInfinity;
        float invulnerableUntil = float.NegativeInfinity;
        int secondWindRemaining;
        float secondWindInvulnSeconds;
        FallDamageSettings fallDamage;

        public float Health { get; private set; }
        public float MaxHealth { get; private set; }

        [System.NonSerialized] public ShieldModel shield; // objet plain C# (pas de MonoBehaviour/ScriptableObject), posé en code
        public float incomingMult = 1f;      // killstreak ×0.5, kevlar ×0.7, difficulté (posé par les autres systèmes)
        public bool hardcore;                // posé au Lot 7 (gèle régén PV + bouclier)

        public event Action<float, float> HealthChanged;
        public event Action Died;
        public event Action<float, Vector3> DamagedFrom; // dégâts subis + position monde de l'attaquant (indicateurs directionnels)
        public event Action SecondWind;                   // « dernier souffle » : flash de soin HUD

        public void Init(CharacterData character, MovementConfig movement, int healthUpgradeLevel = 0, int regenUpgradeLevel = 0)
        {
            MaxHealth = StatFormulas.MaxHealth(character.baseHealth, healthUpgradeLevel, 0f);
            regenRate = StatFormulas.RegenRate(character.baseRegen, regenUpgradeLevel, 0f);
            Health = MaxHealth;
            secondWindRemaining = movement.secondWindUsesPerRun;
            secondWindInvulnSeconds = movement.secondWindInvulnSeconds;
            fallDamage = movement.fallDamage;
            HealthChanged?.Invoke(Health, MaxHealth);
        }

        /// Hardcore (Lot 7, T24) : pas de dernier souffle — appelé après Init une fois le mode connu.
        public void DisableSecondWind() => secondWindRemaining = 0;

        public void TakeDamage(float amount, Vector3 sourceWorldPos)
        {
            float before = Health;
            TakeDamage(amount);
            if (Health < before) DamagedFrom?.Invoke(amount, sourceWorldPos);
        }

        public void TakeDamage(float amount)
        {
            float now = timeSource();
            if (amount <= 0f || Health <= 0f || now < invulnerableUntil) return;
            lastDamageTime = now;
            amount *= incomingMult;                                   // web: kevlar/ks/difficulté
            if (shield != null) { shield.NoteDamage(now); amount = shield.Absorb(amount); }
            if (amount <= 0f) { HealthChanged?.Invoke(Health, MaxHealth); return; } // tout absorbé
            Health -= amount;
            if (Health <= 0f)
            {
                if (secondWindRemaining > 0)
                {
                    secondWindRemaining--;
                    Health = 1f;
                    invulnerableUntil = now + secondWindInvulnSeconds;
                    SecondWind?.Invoke();
                }
                else
                {
                    Health = 0f;
                    HealthChanged?.Invoke(Health, MaxHealth);
                    Died?.Invoke();
                    return;
                }
            }
            HealthChanged?.Invoke(Health, MaxHealth);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || Health <= 0f) return;
            Health = Mathf.Min(MaxHealth, Health + amount);
            HealthChanged?.Invoke(Health, MaxHealth);
        }

        public void ApplyFallDamage(float fallHeight)
        {
            if (fallDamage == null) return;
            int damage = FallDamageRule.Damage(fallHeight, fallDamage);
            if (damage > 0) TakeDamage(damage);
        }

        void Update()
        {
            shield?.Tick(timeSource(), Time.deltaTime, hardcore);
            if (hardcore || Health <= 0f || Health >= MaxHealth) return; // hardcore: pas de régén PV
            if (timeSource() - lastDamageTime < regenDelaySeconds) return;
            Health = Mathf.Min(MaxHealth, Health + regenRate * Time.deltaTime);
            HealthChanged?.Invoke(Health, MaxHealth);
        }
    }
}
