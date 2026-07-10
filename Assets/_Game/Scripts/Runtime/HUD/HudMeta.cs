using Game.Data.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Crédits (cartouche doré haut-gauche) + niveau/rang/XP (bas-gauche, sous SPRINT) —
    /// parité hud2.png (Phase 5 bis, point 12). Lit MetaProgress chaque frame (coût nul).
    public class HudMeta : MonoBehaviour
    {
        MetaProgress meta;
        Text moneyText;
        Text levelText;
        RectTransform xpFill;

        public void Init(RectTransform root, MetaProgress meta)
        {
            this.meta = meta;

            // Cartouche crédits haut-gauche : pastille dorée ⊕ + montant doré (hud2.png).
            var moneyBlock = HudBuild.MakeCartouche(root, "MoneyBlock", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -20f), new Vector2(170f, 38f));
            var coin = HudBuild.MakeImage(moneyBlock.rectTransform, new Color(1f, 0.78f, 0.26f), "Coin");
            coin.sprite = HudBuild.CircleSprite();
            coin.rectTransform.anchorMin = coin.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            coin.rectTransform.pivot = new Vector2(0f, 0.5f);
            coin.rectTransform.anchoredPosition = new Vector2(10f, 0f);
            coin.rectTransform.sizeDelta = new Vector2(18f, 18f);
            moneyText = HudBuild.MakeText(moneyBlock.rectTransform, "", 22, TextAnchor.MiddleLeft, "Money");
            var moneyRect = moneyText.rectTransform;
            moneyRect.anchorMin = Vector2.zero;
            moneyRect.anchorMax = Vector2.one;
            moneyRect.sizeDelta = new Vector2(-40f, 0f);
            moneyRect.anchoredPosition = new Vector2(20f, 0f);
            moneyText.color = new Color(1f, 0.85f, 0.35f); // doré, comme le web
            moneyText.fontStyle = FontStyle.Bold;

            // Niveau + rang + barre violette : bas-gauche, sous la barre SPRINT (hud2.png).
            levelText = HudBuild.MakeText(root, "", 14, TextAnchor.LowerLeft, "Level");
            var levelRect = levelText.rectTransform;
            levelRect.anchorMin = levelRect.anchorMax = levelRect.pivot = new Vector2(0f, 0f);
            levelRect.anchoredPosition = new Vector2(40f, 34f);
            levelRect.sizeDelta = new Vector2(300f, 18f);
            levelText.color = new Color(0.85f, 0.86f, 0.92f);

            var xpBack = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.5f), "XpBar");
            var backRect = xpBack.rectTransform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0f, 0f);
            backRect.anchoredPosition = new Vector2(40f, 20f);
            backRect.sizeDelta = new Vector2(300f, 10f);
            var fill = HudBuild.MakeImage(backRect, new Color(0.49f, 0.36f, 1f), "Fill");
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.sizeDelta = Vector2.zero;
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            xpFill = fill.rectTransform;
        }

        void Update()
        {
            if (meta == null || meta.Data == null) return;
            moneyText.text = meta.Data.money.ToString();
            int level = meta.Level;
            levelText.text = $"NIV. {level} {RankRules.TitleFor(level).ToUpperInvariant()}";
            long prev = level > 1 ? EconomyRules.XpForLevel(level - 1) : 0;
            long next = EconomyRules.XpForLevel(level);
            float frac = next > prev ? Mathf.Clamp01((float)(meta.Data.xp - prev) / (next - prev)) : 1f;
            xpFill.anchorMax = new Vector2(level >= ProgressionRules.MaxLevel ? 1f : frac, 1f);
        }
    }
}
