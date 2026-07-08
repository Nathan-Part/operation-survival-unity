using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Indicateur de mode de tir manuel (web #) : AUTO / BURST / SEMI, près des munitions. T/P pour changer.
    public class HudFireMode : MonoBehaviour
    {
        Text label;
        WeaponController weapon;

        public void Init(RectTransform root, WeaponController weapon)
        {
            this.weapon = weapon;
            label = HudBuild.MakeText(root, "", 22, TextAnchor.LowerRight, "FireMode");
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 100f); // juste au-dessus des munitions
            rect.sizeDelta = new Vector2(500f, 30f);
            label.color = new Color(0.8f, 0.8f, 0.8f);

            weapon.ModeChanged += OnModeChanged;
            OnModeChanged(weapon.Mode);
        }

        void OnDestroy()
        {
            if (weapon != null) weapon.ModeChanged -= OnModeChanged;
        }

        void OnModeChanged(FireMode mode) => label.text = mode switch
        {
            FireMode.Burst => "BURST",
            FireMode.Semi => "SEMI",
            _ => "AUTO",
        };
    }
}
