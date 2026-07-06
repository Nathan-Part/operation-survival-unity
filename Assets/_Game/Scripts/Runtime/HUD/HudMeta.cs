using Game.Data.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Argent + niveau/barre d'XP (parité web « Vitals/Ressources ») :
    /// lit MetaProgress (persistant) chaque frame — valeurs rares, coût nul.
    public class HudMeta : MonoBehaviour
    {
        MetaProgress meta;
        Text moneyText;
        Text levelText;
        RectTransform xpFill;

        public void Init(RectTransform root, MetaProgress meta)
        {
            this.meta = meta;

            moneyText = HudBuild.MakeText(root, "", 30, TextAnchor.UpperLeft, "Money");
            var moneyRect = moneyText.rectTransform;
            moneyRect.anchorMin = moneyRect.anchorMax = new Vector2(0f, 1f);
            moneyRect.pivot = new Vector2(0f, 1f);
            moneyRect.anchoredPosition = new Vector2(24f, -20f);
            moneyRect.sizeDelta = new Vector2(300f, 36f);
            moneyText.color = new Color(1f, 0.85f, 0.35f); // doré, comme le web

            levelText = HudBuild.MakeText(root, "", 20, TextAnchor.UpperLeft, "Level");
            var levelRect = levelText.rectTransform;
            levelRect.anchorMin = levelRect.anchorMax = new Vector2(0f, 1f);
            levelRect.pivot = new Vector2(0f, 1f);
            levelRect.anchoredPosition = new Vector2(24f, -58f);
            levelRect.sizeDelta = new Vector2(300f, 24f);

            var xpBack = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.5f), "XpBar");
            var backRect = xpBack.rectTransform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 1f);
            backRect.pivot = new Vector2(0f, 1f);
            backRect.anchoredPosition = new Vector2(24f, -86f);
            backRect.sizeDelta = new Vector2(220f, 8f);
            var fill = HudBuild.MakeImage(backRect, new Color(0.45f, 0.65f, 1f), "Fill");
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.sizeDelta = Vector2.zero;
            xpFill = fill.rectTransform;
        }

        void Update()
        {
            if (meta == null || meta.Data == null) return;
            moneyText.text = $"$ {meta.Data.money}";
            int level = meta.Level;
            levelText.text = $"Niveau {level}";
            long prev = level > 1 ? EconomyRules.XpForLevel(level - 1) : 0;
            long next = EconomyRules.XpForLevel(level);
            float frac = next > prev ? Mathf.Clamp01((float)(meta.Data.xp - prev) / (next - prev)) : 1f;
            xpFill.anchorMax = new Vector2(level >= ProgressionRules.MaxLevel ? 1f : frac, 1f);
        }
    }
}
