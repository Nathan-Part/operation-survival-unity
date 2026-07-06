using System;

namespace Game.Runtime
{
    /// Bus minimal pour les annonces HUD venant du gameplay
    /// (« BLOQUÉ! », « STUNNÉ! », « FRENZY! », « VIP ÉLIMINÉ! » — parité web).
    public static class CalloutEvents
    {
        public static event Action<string> Show;
        public static void Raise(string text) => Show?.Invoke(text);
    }
}
