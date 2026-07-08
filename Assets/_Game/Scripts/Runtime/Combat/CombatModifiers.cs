namespace Game.Runtime
{
    /// Modificateurs de dégâts du web : adrénaline (#134, PV ≤ 25 % → ×1.2), précision (viser sans
    /// bouger → ×1.3), long shot (#, ≥ 30 m → +25$/+10 XP). SOLO-ONLY pour l'équilibrage PvP (phase 6).
    public static class CombatModifiers
    {
        public const float AdrenalineThreshold = 0.25f, AdrenalineMult = 1.2f, PrecisionMult = 1.3f, LongShotDist = 30f;
        public const int LongShotMoney = 25, LongShotXp = 10;
        public static bool IsAdrenaline(float hp, float maxHp) => maxHp > 0f && hp / maxHp <= AdrenalineThreshold;
        public static float DamageMult(bool adrenaline, bool precision) =>
            (adrenaline ? AdrenalineMult : 1f) * (precision ? PrecisionMult : 1f);
        public static bool IsLongShot(float dist) => dist >= LongShotDist;
    }
}
