using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Radar : blips des ennemis vivants, position relative au joueur (rotation incluse),
    /// couleur = radarColor du type. // TODO(spec-web): portée radar non extraite (20 m par défaut).
    public class HudRadar : MonoBehaviour
    {
        const float RangeMeters = 20f;
        const float RadiusPixels = 110f;

        WaveManager waves;
        Transform player;
        readonly List<Image> blips = new List<Image>();

        public void Init(WaveManager waves, Transform player)
        {
            this.waves = waves;
            this.player = player;

            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -30f);
            rect.sizeDelta = new Vector2(RadiusPixels * 2f, RadiusPixels * 2f);
            var background = gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.35f);
            background.raycastTarget = false;
        }

        void LateUpdate()
        {
            if (waves == null || player == null) return;
            var alive = waves.Alive;

            while (blips.Count < alive.Count)
            {
                var go = new GameObject("Blip");
                go.transform.SetParent(transform, false);
                var image = go.AddComponent<Image>();
                image.raycastTarget = false;
                ((RectTransform)go.transform).sizeDelta = new Vector2(10f, 10f);
                blips.Add(image);
            }

            for (int i = 0; i < blips.Count; i++)
            {
                bool used = i < alive.Count && alive[i] != null;
                blips[i].gameObject.SetActive(used);
                if (!used) continue;

                Vector3 relative = alive[i].transform.position - player.position;
                Vector3 local = Quaternion.Euler(0f, -player.eulerAngles.y, 0f) * relative;
                Vector2 planar = new Vector2(local.x, local.z) / RangeMeters;
                if (planar.magnitude > 1f) planar = planar.normalized;
                ((RectTransform)blips[i].transform).anchoredPosition = planar * RadiusPixels;
                blips[i].color = EnemySpawner.RadarColor(alive[i].Data);
            }
        }
    }
}
