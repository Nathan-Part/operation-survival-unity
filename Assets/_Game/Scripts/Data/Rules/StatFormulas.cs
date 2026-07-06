namespace Game.Data.Rules
{
    public static class StatFormulas
    {
        public static float MaxHealth(float baseHealth, int healthUpgradeLevel, float bonusHealth) =>
            baseHealth + 20f * healthUpgradeLevel + bonusHealth;

        public static float RegenRate(float baseRegen, int regenUpgradeLevel, float bonusRegen) =>
            baseRegen + 3f * regenUpgradeLevel + bonusRegen;

        public static float SpeedMult(float baseSpeed, int speedUpgradeLevel, float bonusSpeed) =>
            baseSpeed * (1 + 0.06f * speedUpgradeLevel + bonusSpeed);
    }
}
