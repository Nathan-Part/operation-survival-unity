using System;

namespace Game.Data.Rules
{
    public static class EconomyRules
    {
        public const int ComboCapMultiplier = 5;

        public static int ComboMultiplier(int consecutiveKills) =>
            Math.Min(ComboCapMultiplier, 1 + consecutiveKills / 3);

        public static int MoneyForKill(int perKill, int comboMultiplier, float difficultyMoneyMult, bool isVip, int vipMult)
        {
            float vipFactor = isVip ? vipMult : 1;
            return (int)Math.Round(perKill * comboMultiplier * difficultyMoneyMult * vipFactor, MidpointRounding.AwayFromZero);
        }

        public static long XpForLevel(int level) => 100L * level * (level + 1) / 2;
    }
}
