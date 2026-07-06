namespace Game.Runtime
{
    /// Tir automatique façon Guns of Boom (décision D1 de la spec).
    public static class AutoFireDecider
    {
        public static bool ShouldFire(bool enemyUnderCrosshair, float distance, float weaponRange, float cooldownRemaining, bool hasAmmoReady) =>
            enemyUnderCrosshair && distance <= weaponRange && cooldownRemaining <= 0f && hasAmmoReady;
    }
}
