namespace Game.Runtime
{
    /// Tir automatique façon Guns of Boom (décision D1 de la spec).
    public static class AutoFireDecider
    {
        public static bool ShouldFire(bool enemyUnderCrosshair, float distance, float weaponRange, float cooldownRemaining, bool hasAmmoReady) =>
            enemyUnderCrosshair && distance <= weaponRange && cooldownRemaining <= 0f && hasAmmoReady;

        /// Coexistence D1 : le tir auto reste, E (maintien) tire manuellement même sans cible (parité web).
        public static bool ShouldFireWithManual(bool auto, bool manualHeld, float cooldownRemaining, bool hasAmmoReady) =>
            auto || (manualHeld && cooldownRemaining <= 0f && hasAmmoReady);
    }
}
