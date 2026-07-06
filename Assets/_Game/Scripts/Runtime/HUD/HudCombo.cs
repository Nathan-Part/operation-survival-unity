using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// HUD combo (web #29) : « xN / COMBO n » + barre de temps, pulse à chaque kill.
    public class HudCombo : MonoBehaviour
    {
        WaveManager waves;
        GameObject panel;
        Text multText;
        Text countText;
        RectTransform barFill;
        int lastCount;
        float pulseUntil = -1f;

        public void Init(RectTransform root, WaveManager waves)
        {
            this.waves = waves;
            var container = HudBuild.MakeCentered(root, "ComboHud");
            container.anchorMin = container.anchorMax = new Vector2(1f, 0.5f);
            container.anchoredPosition = new Vector2(-110f, 60f);

            multText = HudBuild.MakeText(container, "", 40, TextAnchor.MiddleCenter, "Mult");
            multText.rectTransform.sizeDelta = new Vector2(160f, 46f);
            multText.color = new Color(1f, 0.65f, 0.1f);

            countText = HudBuild.MakeText(container, "", 20, TextAnchor.MiddleCenter, "Count");
            countText.rectTransform.anchoredPosition = new Vector2(0f, -32f);
            countText.rectTransform.sizeDelta = new Vector2(160f, 24f);

            var barBack = HudBuild.MakeImage(container, new Color(0f, 0f, 0f, 0.5f), "BarBack");
            barBack.rectTransform.anchoredPosition = new Vector2(0f, -50f);
            barBack.rectTransform.sizeDelta = new Vector2(120f, 6f);
            var fillImage = HudBuild.MakeImage(barBack.rectTransform, new Color(1f, 0.65f, 0.1f), "BarFill");
            fillImage.rectTransform.anchorMin = Vector2.zero;
            fillImage.rectTransform.anchorMax = Vector2.one;
            fillImage.rectTransform.sizeDelta = Vector2.zero;
            barFill = fillImage.rectTransform;

            panel = container.gameObject;
            panel.SetActive(false);
        }

        void Update()
        {
            if (waves == null) return;
            var combo = waves.Combo;
            if (combo.Count <= 0)
            {
                if (panel.activeSelf) panel.SetActive(false);
                lastCount = 0;
                return;
            }
            if (!panel.activeSelf) panel.SetActive(true);
            if (combo.Count != lastCount)
            {
                lastCount = combo.Count;
                pulseUntil = Time.unscaledTime + 0.24f; // web: pulse 240 ms
            }
            multText.text = "x" + combo.Multiplier;
            countText.text = "COMBO " + combo.Count;
            barFill.anchorMax = new Vector2(combo.TimeFrac(Time.time), 1f);
            float scale = Time.unscaledTime < pulseUntil ? 1.15f : 1f;
            panel.transform.localScale = Vector3.one * scale;
        }
    }
}
