namespace Game.Runtime
{
    /// Annonces de kill (web #38) : BOSS DOWN > multi-kill (fenêtre 1.2 s) > HEADSHOT.
    public static class CalloutRule
    {
        public const float MultikillWindow = 1.2f; // web: MULTIKILL_WINDOW

        public static string ForKill(bool boss, bool headshot, int killsInWindow)
        {
            if (boss) return "BOSS DOWN";
            if (killsInWindow >= 2)
                return killsInWindow == 2 ? "DOUBLE KILL"
                    : killsInWindow == 3 ? "TRIPLE KILL"
                    : "MULTI KILL !";
            return headshot ? "HEADSHOT" : null;
        }
    }
}
