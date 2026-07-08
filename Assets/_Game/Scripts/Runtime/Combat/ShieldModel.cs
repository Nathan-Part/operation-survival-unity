using UnityEngine;

namespace Game.Runtime
{
    /// Bouclier rechargeable (web #67 SHIELD_MAX=25, SHIELD_REGEN=8/s, SHIELD_DELAY=6s).
    /// Absorbe les dégâts avant les PV ; régénère après un délai sans dégât ; gelé en hardcore.
    public class ShieldModel
    {
        public const float Max = 25f, RegenPerSec = 8f, RegenDelaySec = 6f;
        public float Current { get; private set; } = Max;
        float lastDamage = float.NegativeInfinity;

        public float Absorb(float incoming)
        {
            if (incoming <= 0f) return incoming;
            float a = Mathf.Min(Current, incoming);
            Current -= a;
            return incoming - a;
        }

        public void NoteDamage(float now) => lastDamage = now;

        public void Tick(float now, float dt, bool hardcore)
        {
            if (hardcore || Current >= Max || now - lastDamage < RegenDelaySec) return;
            Current = Mathf.Min(Max, Current + RegenPerSec * dt);
        }

        public void Refill() => Current = Max;
    }
}
