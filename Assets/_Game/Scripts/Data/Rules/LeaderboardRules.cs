using System.Collections.Generic;
using Game.Data.Save;

namespace Game.Data.Rules
{
    /// Leaderboard local top 5 par score (web : classement fin de partie).
    public static class LeaderboardRules
    {
        // Écran de fin de partie riche (Phase 5.1) : le web soumet kills/headshots/accuracy en plus
        // du score (submitScore, main.js:4025) — nouvelle surcharge à 7 paramètres. L'ancienne
        // signature à 4 paramètres est conservée (délègue avec des zéros) pour ne rien casser des
        // appelants existants qui n'ont pas encore ces données sous la main.
        public static void Push(List<LeaderboardEntry> board, int score, int wave, string date, int kills, int headshots, float accuracy, int max = 5)
        {
            board.Add(new LeaderboardEntry { score = score, wave = wave, date = date, kills = kills, headshots = headshots, accuracy = accuracy });
            board.Sort((a, b) => b.score.CompareTo(a.score));
            if (board.Count > max) board.RemoveRange(max, board.Count - max);
        }

        public static void Push(List<LeaderboardEntry> board, int score, int wave, string date, int max = 5)
            => Push(board, score, wave, date, 0, 0, 0f, max);
    }
}
