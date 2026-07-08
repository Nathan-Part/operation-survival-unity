using System.Collections.Generic;
namespace Game.Runtime
{
    /// Killstreaks (web #58) : kills dans une fenêtre glissante de 8 s. Paliers 3/5/7 ;
    /// reset à tout dégât subi (web onStreakReset). Récompenses appliquées par le controller.
    public class KillstreakModel
    {
        public const float Window = 8f; // web #58
        static readonly int[] Thresholds = { 3, 5, 7 }; // web #58 : rechargement / bouclier / argent
        readonly List<float> kills = new List<float>();

        public int StreakCount => kills.Count;

        public int RegisterKill(float now)
        {
            kills.Add(now);
            Prune(now);
            int c = kills.Count;
            for (int i = 0; i < Thresholds.Length; i++) if (c == Thresholds[i]) return Thresholds[i];
            return 0;
        }

        public void OnPlayerDamaged() => kills.Clear();

        void Prune(float now) { for (int i = kills.Count - 1; i >= 0; i--) if (now - kills[i] > Window) kills.RemoveAt(i); }
    }
}
