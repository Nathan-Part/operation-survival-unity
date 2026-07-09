using Game.Data.Save;

namespace Game.Data.Rules
{
    /// Compteurs d'une partie + drapeaux, pour évaluer les succès et les contrats.
    public struct RunStats
    {
        public int runKills, runHeadshots, wave, combo, runDistance;
        public int speedClears, perfectWaveStreak, noReloadClears, stealthKills, clutches, barrelChains, contractsDone;
        public bool miracle, legendaryWeapon, silentClear, legendaryCosmetics;

        // Écran de fin de partie riche (Phase 5.1, 2026-07-09) : champs affichés par GameOverScreen,
        // alimentés par RunTracker. `score` reprend la même simplification déjà en place avant cette
        // session (SliceBootstrap.OnRunEnded passe waveManager.Money comme "score" — le web a une
        // vraie formule de score séparée de l'argent, non extraite ici, écart assumé documenté dans
        // pending-verifications-phase5.1.md).
        public int score;
        public int shotsFired;
        public int shotsHit;
        public float bestHitDistance;
        public float fastestWaveSeconds;

        public float Accuracy => shotsFired > 0 ? (float)shotsHit / shotsFired : 0f;
    }

    /// Évalue une condition de succès (pilotée par les données : conditionType + target).
    public static class AchievementRules
    {
        public static bool IsUnlocked(AchievementData a, RunStats run, SaveData save)
        {
            int level = ProgressionRules.LevelFor(save.xp);
            switch (a.conditionType)
            {
                case "run_kills": return run.runKills >= a.target;
                case "run_headshots": return run.runHeadshots >= a.target;
                case "wave": return run.wave >= a.target;
                case "combo": return run.combo >= a.target;
                case "money": return save.money >= a.target;
                case "level": return level >= a.target;
                case "owned_weapons": return save.ownedWeapons.Count >= a.target;
                case "lifetime_kills": return save.lifetime.kills >= a.target;
                case "lifetime_headshots": return save.lifetime.headshots >= a.target;
                case "lifetime_distance": return save.lifetime.playtime >= a.target; // distance ≈ à brancher (suivi)
                case "lifetime_perfect_waves": return run.perfectWaveStreak >= a.target; // suivi
                case "run_speed_clears": return run.speedClears >= a.target;
                case "perfect_wave_streak": return run.perfectWaveStreak >= a.target;
                case "no_reload_clears": return run.noReloadClears >= a.target;
                case "stealth_kills": return run.stealthKills >= a.target;
                case "run_distance": return run.runDistance >= a.target;
                case "clutches": return run.clutches >= a.target;
                case "barrel_chains": return run.barrelChains >= a.target;
                case "contracts_done": return run.contractsDone >= a.target;
                case "flag_miracle": return run.miracle;
                case "flag_legendary_weapon": return run.legendaryWeapon;
                case "flag_silent_clear": return run.silentClear;
                case "flag_legendary_cosmetics": return run.legendaryCosmetics;
                default: return false;
            }
        }
    }
}
