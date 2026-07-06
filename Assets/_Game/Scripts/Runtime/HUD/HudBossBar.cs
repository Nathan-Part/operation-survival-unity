using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Barre de vie du boss (web #31) : bandeau haut centre, visible si un boss est vivant.
    public class HudBossBar : MonoBehaviour
    {
        WaveManager waves;
        GameObject panel;
        RectTransform fill;

        public void Init(RectTransform root, WaveManager waves)
        {
            this.waves = waves;
            var back = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.6f), "BossBar");
            var rect = back.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -70f); // sous le texte de vague
            rect.sizeDelta = new Vector2(520f, 16f);
            var fillImage = HudBuild.MakeImage(rect, new Color(0.81f, 0.58f, 0.85f), "Fill"); // web: violet boss #ce93d8
            fillImage.rectTransform.anchorMin = Vector2.zero;
            fillImage.rectTransform.anchorMax = Vector2.one;
            fillImage.rectTransform.sizeDelta = Vector2.zero;
            var label = HudBuild.MakeText(rect, "BOSS", 14, TextAnchor.MiddleCenter, "Label");
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            panel = back.gameObject;
            fill = fillImage.rectTransform;
            panel.SetActive(false);
        }

        void Update()
        {
            if (waves == null) return;
            EnemyHealth boss = null;
            var alive = waves.Alive;
            for (int i = 0; i < alive.Count; i++)
                if (alive[i] != null && !alive[i].IsDead && alive[i].Data != null && alive[i].Data.id == "boss") { boss = alive[i]; break; }
            if (boss == null)
            {
                if (panel.activeSelf) panel.SetActive(false);
                return;
            }
            if (!panel.activeSelf) panel.SetActive(true);
            float frac = boss.MaxHealth > 0 ? Mathf.Clamp01((float)boss.Health / boss.MaxHealth) : 0f;
            fill.anchorMax = new Vector2(frac, 1f);
        }
    }
}
