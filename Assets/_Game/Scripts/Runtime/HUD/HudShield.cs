using UnityEngine;

namespace Game.Runtime
{
    /// Barre de bouclier (bleue, bas-centre — parite hud.png) : lit health.shield.Current/Max chaque frame.
    /// Playtest 2026-07-10 : etait calee "au-dessus de la barre de vie" en bas-gauche, mais la vie
    /// est passee en haut-droite (hud.png) — le bouclier restait orphelin au milieu des barres de Defis.
    public class HudShield : MonoBehaviour
    {
        PlayerHealth health;
        RectTransform fill;

        public void Init(RectTransform root, PlayerHealth health)
        {
            this.health = health;

            var back = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.5f), "ShieldBack");
            var br = back.rectTransform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0f);
            br.pivot = new Vector2(0.5f, 0f);
            br.anchoredPosition = new Vector2(0f, 40f);
            br.sizeDelta = new Vector2(320f, 12f);

            var f = HudBuild.MakeImage(back.rectTransform, new Color(0.35f, 0.7f, 1f), "ShieldFill");
            f.rectTransform.anchorMin = Vector2.zero;
            f.rectTransform.anchorMax = Vector2.one;
            f.rectTransform.sizeDelta = Vector2.zero;
            f.rectTransform.pivot = new Vector2(0f, 0.5f);
            fill = f.rectTransform;
        }

        void Update()
        {
            if (health == null || health.shield == null) return;
            fill.anchorMax = new Vector2(Mathf.Clamp01(health.shield.Current / ShieldModel.Max), 1f);
        }
    }
}
