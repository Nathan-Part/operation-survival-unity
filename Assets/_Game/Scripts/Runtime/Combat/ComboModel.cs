using UnityEngine;

namespace Game.Runtime
{
    /// Combo de kills (web #29) : fenêtre 3.5 s, multiplicateur d'argent min(5, 1+floor(n/3)).
    public class ComboModel
    {
        public const float Window = 3.5f; // web: COMBO_WINDOW

        float expireAt;

        public int Count { get; private set; }
        public int Multiplier => MultiplierFor(Count);

        public static int MultiplierFor(int count) => Mathf.Min(5, 1 + count / 3); // web: comboMultiplier

        /// Enregistre un kill au temps `now` et retourne le multiplicateur courant.
        public int RegisterKill(float now)
        {
            Count++;
            expireAt = now + Window;
            return Multiplier;
        }

        public void Tick(float now)
        {
            if (Count > 0 && now >= expireAt) Count = 0; // web: breakCombo
        }

        /// Fraction de temps restant (barre du HUD).
        public float TimeFrac(float now) =>
            Count > 0 ? Mathf.Max(0f, (expireAt - now) / Window) : 0f;
    }
}
