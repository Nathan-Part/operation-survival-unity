using System.Collections.Generic;
using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Annonces de kill (web #38) : HEADSHOT / DOUBLE-TRIPLE-MULTI KILL / BOSS DOWN, 1 s.
    public class HudCallout : MonoBehaviour
    {
        const float ShowSeconds = 1f; // web: 1000 ms

        readonly List<float> killTimes = new List<float>();
        Text label;
        float hideAt = -1f;

        public void Init(RectTransform root, WeaponController weapon)
        {
            label = HudBuild.MakeText(root, "", 44, TextAnchor.MiddleCenter, "KillCallout");
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 180f);
            rect.sizeDelta = new Vector2(900f, 60f);
            label.color = new Color(1f, 0.85f, 0.3f);
            label.gameObject.SetActive(false);
            weapon.HitConfirmed += OnHit;
        }

        void OnHit(HitZone zone, bool killed, EnemyHealth target)
        {
            if (!killed) return;
            float now = Time.time;
            killTimes.Add(now);
            killTimes.RemoveAll(t => now - t > CalloutRule.MultikillWindow);
            bool boss = target != null && target.Data != null && target.Data.id == "boss";
            string text = CalloutRule.ForKill(boss, zone == HitZone.Head, killTimes.Count);
            if (text == null) return;
            label.text = text;
            label.gameObject.SetActive(true);
            hideAt = Time.unscaledTime + ShowSeconds;
        }

        void Update()
        {
            if (hideAt >= 0f && Time.unscaledTime >= hideAt)
            {
                label.gameObject.SetActive(false);
                hideAt = -1f;
            }
        }
    }
}
