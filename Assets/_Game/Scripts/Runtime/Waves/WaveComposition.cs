using Game.Data.Rules;

namespace Game.Runtime
{
    /// Transposition exacte de waves.json -> composition.rules (premier match gagne).
    /// Slot 0 des vagues normales toujours forcé "shooter" ; vague boss = [boss, shooter, shooter].
    public static class WaveComposition
    {
        public static string TypeForSlot(int w, int i)
        {
            if (w >= 10 && i % 4 == 3) return "juggernaut";
            if (w >= 3 && i % 4 == 3) return "tank";
            if (w >= 7 && i % 6 == 5) return w % 2 == 0 ? "cryo" : "stunner";
            if (w >= 8 && i % 7 == 6) return w % 2 == 0 ? "grenadier" : "sniper";
            if (w >= 5 && i % 7 == 6) return "sniper";
            if (w >= 9 && i % 5 == 4) return w % 2 == 0 ? "commander" : "berserker";
            if (w >= 6 && i % 5 == 4) return "berserker";
            if (w >= 4 && w < 6 && i % 5 == 4) return "shielder";
            if (w >= 6 && i % 3 == 2) return w % 2 == 0 ? "toxic" : "bomber";
            if (w >= 4 && i % 3 == 2) return "bomber";
            if (w >= 6 && i % 6 == 1) return "medic";
            if (w >= 2 && i % 2 == 1) return "rusher";
            return "shooter";
        }

        public static string[] ForWave(int wave)
        {
            if (WaveRules.IsBossWave(wave)) return new[] { "boss", "shooter", "shooter" };
            int count = WaveRules.EnemiesForWave(wave);
            var result = new string[count];
            for (int i = 0; i < count; i++) result[i] = TypeForSlot(wave, i);
            result[0] = "shooter";
            return result;
        }
    }
}
