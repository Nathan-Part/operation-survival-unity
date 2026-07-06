using System;

namespace Game.Data.Rules
{
    public static class WaveRules
    {
        public static bool IsBossWave(int wave) => wave % 5 == 0;

        public static int EnemiesForWave(int wave) => IsBossWave(wave) ? 3 : Math.Min(2 + wave, 7);

        public static int EnemyHpForWave(int wave, float difficultyHpMult) =>
            (int)Math.Round(100 * Math.Pow(1.18, wave - 1) * difficultyHpMult, MidpointRounding.AwayFromZero);

        public static float WaveScoreMult(int wave) => 1f + (wave / 5) * 0.5f;

        public static int WaveMoneyBonus(int wave, float difficultyMoneyMult) =>
            (int)Math.Round(25 * wave * difficultyMoneyMult, MidpointRounding.AwayFromZero);
    }
}
