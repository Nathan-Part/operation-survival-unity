using UnityEngine;

namespace Game.Runtime
{
    /// Règles pures des comportements spéciaux (valeurs web, cf. spec du 2026-07-07).
    public static class SpecialRules
    {
        public const float BomberRadius = 3.5f;      // web: BOMBER_R
        public const float BomberDamage = 35f;       // web: BOMBER_DAMAGE
        public const float GrenadeRadius = 3.5f;     // web: GRENADIER_R
        public const float GrenadeDamage = 60f;      // web: GRENADIER_DMG
        public const float GrenadeCooldown = 5f;     // web: GRENADIER_THROW_CD
        public const float GrenadeMinRange = 5f;     // web: GRENADIER_MIN_RANGE
        public const float ShieldBlockDot = 0.5f;    // web: SHIELD_BLOCK_DOT (cône ±60°)
        public const float FrenzyHpThreshold = 0.25f; // web: FRENZY_HP_THRESH
        public const float FrenzyDuration = 2f;      // web: FRENZY_DURATION
        public const float FrenzySpeedMult = 2.5f;   // web: FRENZY_SPEED
        public const float CommanderDamageMult = 1.2f; // web: commanderAliveMult
        public const int VipMoneyMult = 2;           // web: VIP_MULT
        public const int VipMinWave = 3;             // web: VIP_MIN_WAVE
        public const float MedicRange = 5f;          // web: MEDIC_RANGE
        public const float MedicHealRate = 8f;       // web: MEDIC_HEAL_RATE
        public const float MedicTick = 0.5f;         // web: MEDIC_TICK

        /// Dégâts d'explosion avec falloff linéaire (web: max(1, round(dmg×(1−d/r))) ; 0 hors rayon).
        public static int ExplosionDamage(float distance, float radius, float damage) =>
            distance >= radius ? 0 : Mathf.Max(1, Mathf.RoundToInt(damage * (1f - distance / radius)));

        /// Bouclier frontal : bloqué si le tireur est dans le cône avant du shielder.
        public static bool ShieldBlocks(Vector3 enemyPos, Vector3 enemyForward, Vector3 shooterPos)
        {
            var toShooter = shooterPos - enemyPos;
            toShooter.y = 0f;
            var fwd = enemyForward;
            fwd.y = 0f;
            if (toShooter.sqrMagnitude < 1e-6f || fwd.sqrMagnitude < 1e-6f) return false;
            return Vector3.Dot(toShooter.normalized, fwd.normalized) > ShieldBlockDot;
        }

        /// Frenzy berserker : une seule fois, sous 25 % des PV max.
        public static bool FrenzyShouldTrigger(int health, int maxHealth, bool alreadyUsed) =>
            !alreadyUsed && maxHealth > 0 && health > 0 && (float)health / maxHealth <= FrenzyHpThreshold;

        public static float CommanderMult(bool anyCommanderAlive) => anyCommanderAlive ? CommanderDamageMult : 1f;

        /// VIP marqué sur les vagues ≥ 3 hors boss (web: maybeMarkVip ; boss = multiple de 5).
        public static bool VipAllowedOnWave(int wave) => wave >= VipMinWave && wave % 5 != 0;
    }
}
