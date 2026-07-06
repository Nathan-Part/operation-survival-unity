using System;
using Game.Data;

namespace Game.Runtime
{
    /// Spec web : h <= safeHeight ? 0 : min(maxDamage, round((h - safeHeight) * damagePerMeter)).
    public static class FallDamageRule
    {
        public static int Damage(float fallHeight, FallDamageSettings s)
        {
            if (fallHeight <= s.safeHeight) return 0;
            double raw = Math.Round((fallHeight - s.safeHeight) * s.damagePerMeter, MidpointRounding.AwayFromZero);
            return (int)Math.Min(s.maxDamage, raw);
        }
    }
}
