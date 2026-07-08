using System.Collections.Generic;
using Game.Data.Save;

namespace Game.Data.Rules
{
    /// Leaderboard local top 5 par score (web : classement fin de partie).
    public static class LeaderboardRules
    {
        public static void Push(List<LeaderboardEntry> board, int score, int wave, string date, int max = 5)
        {
            board.Add(new LeaderboardEntry { score = score, wave = wave, date = date });
            board.Sort((a, b) => b.score.CompareTo(a.score));
            if (board.Count > max) board.RemoveRange(max, board.Count - max);
        }
    }
}
