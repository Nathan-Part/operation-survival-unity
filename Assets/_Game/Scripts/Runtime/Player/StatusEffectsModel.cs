using UnityEngine;

namespace Game.Runtime
{
    /// Effets de statut du joueur (web) : stun (#121) et gel (#146) → multiplicateur de vitesse.
    public class StatusEffectsModel
    {
        public const float StunDuration = 0.5f;   // web: STUN_DURATION
        public const float StunSpeedMult = 0.3f;  // web: STUN_SPEED_MULT
        public const float FrostSpeedMult = 0.5f; // web: FROST_SLOW_MULT

        float stunUntil;
        int frostZones;

        public void ApplyStun(float now) => stunUntil = now + StunDuration;
        public void EnterFrost() => frostZones++;
        public void ExitFrost() => frostZones = Mathf.Max(0, frostZones - 1);

        public bool IsStunned(float now) => now < stunUntil;
        public bool IsFrosted => frostZones > 0;

        public float SpeedMult(float now) =>
            (IsStunned(now) ? StunSpeedMult : 1f) * (IsFrosted ? FrostSpeedMult : 1f);
    }
}
