using Game.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Hit-marker (web #59) : croix brève au centre, variante headshot rouge et plus grande.
    public class HudHitmarker : MonoBehaviour
    {
        const float ShowSeconds = 0.12f; // web: 120 ms
        static readonly Color HeadColor = new Color(1f, 0.25f, 0.25f);

        RectTransform container;
        Image[] strokes;
        float hideAt = -1f;

        public void Init(RectTransform root, WeaponController weapon)
        {
            container = HudBuild.MakeCentered(root, "Hitmarker");
            strokes = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var stroke = HudBuild.MakeImage(container, Color.white, "Stroke");
                stroke.rectTransform.sizeDelta = new Vector2(3f, 14f);
                // 4 traits diagonaux autour du centre (croix en X)
                float angle = 45f + i * 90f;
                stroke.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                var dir = Quaternion.Euler(0f, 0f, angle) * Vector3.up;
                stroke.rectTransform.anchoredPosition = dir * 14f;
                strokes[i] = stroke;
            }
            container.gameObject.SetActive(false);
            weapon.HitConfirmed += OnHit;
        }

        void OnHit(HitZone zone, bool killed, EnemyHealth target)
        {
            bool head = zone == HitZone.Head;
            foreach (var s in strokes) s.color = head ? HeadColor : Color.white;
            container.localScale = Vector3.one * (head ? 1.3f : 1f);
            container.gameObject.SetActive(true);
            hideAt = Time.unscaledTime + ShowSeconds;
        }

        void Update()
        {
            if (hideAt >= 0f && Time.unscaledTime >= hideAt)
            {
                container.gameObject.SetActive(false);
                hideAt = -1f;
            }
        }
    }
}
