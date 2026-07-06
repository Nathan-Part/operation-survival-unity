using System;

namespace Game.Data.Rules
{
    public static class UpgradeFormulas
    {
        public static int Cost(int baseCost, int level) => baseCost * (level + 1);

        public static float WeaponDamageMult(float baseDmgMult, int level) => baseDmgMult * (1 + 0.15f * level);

        public static float WeaponCooldown(float baseCooldown, int level) => baseCooldown * (1 - 0.08f * level);

        public static float WeaponRecoil(float baseRecoil, int level) => baseRecoil * (1 - 0.12f * level);

        public static int WeaponMag(int baseMag, int level) =>
            (int)Math.Round(baseMag * (1 + 0.2f * level), MidpointRounding.AwayFromZero);
    }
}
