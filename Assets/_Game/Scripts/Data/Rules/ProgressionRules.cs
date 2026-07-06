namespace Game.Data.Rules
{
    /// XP et niveaux (web #60) : 25 XP/kill (+25 headshot), 100 XP/vague,
    /// xpForLevel(n) = 100·n·(n+1)/2, niveaux 1 à 50.
    public static class ProgressionRules
    {
        public const int XpKill = 25;      // web: XP_KILL
        public const int XpHeadBonus = 25; // web: XP_HEAD
        public const int XpWave = 100;     // web: XP_WAVE
        public const int MaxLevel = 50;

        public static int LevelFor(long xp)
        {
            int level = 1;
            while (level < MaxLevel && xp >= EconomyRules.XpForLevel(level)) level++;
            return level;
        }

        public static int KillXp(bool headshot, bool vip) =>
            (XpKill + (headshot ? XpHeadBonus : 0)) * (vip ? 2 : 1); // web: × VIP_MULT
    }
}
