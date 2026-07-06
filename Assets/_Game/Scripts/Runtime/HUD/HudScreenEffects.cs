using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Effets écran : voile de sang sur gros dégâts (web #74) et flash de soin
    /// vert (web flashHeal 260 ms — branché sur le second wind en attendant les soins).
    public class HudScreenEffects : MonoBehaviour
    {
        const float HealFlashSeconds = 0.26f; // web: 260 ms
        static readonly Color BloodColor = new Color(0.7f, 0f, 0f, 1f);
        static readonly Color HealColor = new Color(0.2f, 0.9f, 0.35f, 0.35f);

        readonly BloodScreenModel blood = new BloodScreenModel();
        PlayerHealth health;
        Image bloodImage;
        Image healImage;
        float lastHealth = -1f;
        float healHideAt = -1f;

        public void Init(RectTransform root, PlayerHealth health)
        {
            this.health = health;
            bloodImage = HudBuild.MakeImage(root, Color.clear, "BloodScreen");
            var rect = bloodImage.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            healImage = HudBuild.MakeImage(root, Color.clear, "HealFlash");
            rect = healImage.rectTransform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;

            health.HealthChanged += OnHealthChanged;
            health.SecondWind += OnHeal;
            lastHealth = health.Health;
        }

        void OnHealthChanged(float current, float max)
        {
            if (lastHealth >= 0f && current < lastHealth) blood.OnDamage(lastHealth - current);
            lastHealth = current;
        }

        void OnHeal()
        {
            healImage.color = HealColor;
            healHideAt = Time.unscaledTime + HealFlashSeconds;
        }

        void Update()
        {
            blood.Tick(Time.deltaTime);
            var c = BloodColor;
            c.a = blood.Opacity * 0.45f; // voile, pas un écran opaque
            bloodImage.color = c;

            if (healHideAt >= 0f && Time.unscaledTime >= healHideAt)
            {
                healImage.color = Color.clear;
                healHideAt = -1f;
            }
        }

        void OnDestroy()
        {
            if (health != null)
            {
                health.HealthChanged -= OnHealthChanged;
                health.SecondWind -= OnHeal;
            }
        }
    }
}
