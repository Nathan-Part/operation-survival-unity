using UnityEngine;

namespace Game.Runtime
{
    /// Surchauffe LMG (web #96) : +1 par tir, blocage 2 s à 20, refroidissement sinon.
    /// Suit le temps via le paramètre `now` explicite (et non UnityEngine.Time.time global)
    /// pour rester déterministe et testable hors Play Mode.
    public class OverheatModel
    {
        public const int Max = 20;
        public const float PauseSeconds = 2f, CooldownFactor = 0.8f;

        float heat;
        float pausedUntil = float.NegativeInfinity;
        float lastShot;
        float lastNow;

        public bool CanFire => lastNow >= pausedUntil;

        public void OnShot(float now)
        {
            lastNow = now;
            heat += 1f;
            lastShot = now;
            if (heat >= Max)
            {
                pausedUntil = now + PauseSeconds;
                heat = 0f;
            }
        }

        public void Tick(float now)
        {
            lastNow = now;
            if (now >= pausedUntil && now - lastShot > 0.2f)
                heat = Mathf.Max(0f, heat - CooldownFactor);
        }
    }
}
