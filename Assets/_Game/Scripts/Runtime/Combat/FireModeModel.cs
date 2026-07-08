using System.Collections.Generic;

namespace Game.Runtime
{
    public enum FireMode { Auto, Burst, Semi }

    /// Modes de tir (web #): burst pour {default,raptor,cyclone}, semi forcé pour {sniper,phantom_sr,pistol,jackal}.
    public static class FireModeModel
    {
        public const int BurstSize = 3;
        public const float BurstDelay = 0.08f;

        static readonly HashSet<string> BurstWeapons = new HashSet<string> { "default", "raptor", "cyclone" };
        static readonly HashSet<string> ForcedSemiWeapons = new HashSet<string> { "sniper", "phantom_sr", "pistol", "jackal" };

        public static bool CanToggleBurst(string id) => BurstWeapons.Contains(id);
        public static bool ForcesSemi(string id) => ForcedSemiWeapons.Contains(id);

        public static FireMode Default(string weaponId, bool weaponSemiAuto) =>
            (ForcesSemi(weaponId) || weaponSemiAuto) ? FireMode.Semi : FireMode.Auto;
    }
}
