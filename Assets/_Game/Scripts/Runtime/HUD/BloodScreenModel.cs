using UnityEngine;

namespace Game.Runtime
{
    /// Écran de sang (web #74) : seuil 20 dégâts, opacité ∝ dégâts/50,
    /// maintien bref puis fondu ~1 s.
    public class BloodScreenModel
    {
        public const float Threshold = 20f; // web: BLOOD_THRESHOLD
        public const float HoldSeconds = 0.5f;
        public const float FadePerSecond = 1f; // web: transition opacity 1s

        float holdRemaining;

        public float Opacity { get; private set; }

        public void OnDamage(float amount)
        {
            if (amount < Threshold) return;
            Opacity = Mathf.Max(Opacity, Mathf.Min(1f, amount / 50f)); // web: min(1, amount/50)
            holdRemaining = HoldSeconds;
        }

        public void Tick(float dt)
        {
            if (holdRemaining > 0f) { holdRemaining -= dt; return; }
            Opacity = Mathf.Max(0f, Opacity - FadePerSecond * dt);
        }
    }
}
