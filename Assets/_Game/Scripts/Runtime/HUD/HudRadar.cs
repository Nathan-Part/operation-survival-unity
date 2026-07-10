using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Radar : blips des ennemis vivants, position relative au joueur (rotation incluse),
    /// couleur = radarColor du type. Portee et marqueur joueur : valeurs reelles du web
    /// (RADAR_RANGE=28, triangle vert #7dff9e fixe au centre, main.js:4225-4268) — playtest
    /// Phase 5.1 (2026-07-09), corrige le TODO(spec-web) (20m par defaut) et le marqueur joueur
    /// manquant (D2, relevé par Nathan).
    public class HudRadar : MonoBehaviour
    {
        const float RangeMeters = 28f;
        const float RadiusPixels = 95f; // ~190px de diamètre (hud2.png)
        // TODO(spec-web) : le web double RangeMeters quand le power-up "radar_boost" est actif
        // (main.js:4225). Aucune trace de "radar_boost" dans le projet Unity (grep negatif,
        // 2026-07-09) — la portee reste fixe a 28m tant que ce power-up n'existe pas cote Unity.
        static readonly Color PlayerMarkerColor = new Color(0.49f, 1f, 0.62f); // #7dff9e

        WaveManager waves;
        Transform player;
        readonly List<Image> blips = new List<Image>();

        public void Init(WaveManager waves, Transform player)
        {
            this.waves = waves;
            this.player = player;

            // Minimap RONDE (Phase 5 bis point 12, parité hud2.png) : disque sombre, sous la
            // barre violette de progression de vague (HudController, y=-26..-34).
            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-30f, -44f);
            rect.sizeDelta = new Vector2(RadiusPixels * 2f, RadiusPixels * 2f);
            var background = gameObject.AddComponent<Image>();
            background.sprite = HudBuild.CircleSprite();
            background.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);
            background.raycastTarget = false;

            // Marqueur joueur : triangle pointant vers le haut (= direction du regard, la scene
            // tournant autour du joueur, pas le marqueur), dessine EN DERNIER (LateUpdate) donc
            // toujours au-dessus des blips ennemis — parite web (le canvas radar dessine le joueur
            // en tout dernier, ligne 4267-4268).
            var markerGo = new GameObject("PlayerMarker");
            markerGo.transform.SetParent(transform, false);
            var markerImage = markerGo.AddComponent<Image>();
            markerImage.sprite = BuildTriangleSprite();
            markerImage.color = PlayerMarkerColor;
            markerImage.raycastTarget = false;
            var markerRect = (RectTransform)markerGo.transform;
            markerRect.anchorMin = markerRect.anchorMax = markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.anchoredPosition = Vector2.zero;
            markerRect.sizeDelta = new Vector2(14f, 14f);
            markerGo.transform.SetAsLastSibling();
        }

        static Sprite cachedTriangleSprite;

        static Sprite BuildTriangleSprite()
        {
            if (cachedTriangleSprite != null) return cachedTriangleSprite;
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "RadarPlayerTriangle" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                // Texture2D : y=0 = ligne du BAS (convention Unity SetPixels32). Triangle pointant vers
                // le haut = etroit en haut (y=size-1) et large en bas (y=0) -> t decroit avec y.
                float t = 1f - (float)y / (size - 1);
                int halfWidth = Mathf.RoundToInt(t * (size / 2f));
                int cx = size / 2;
                for (int x = 0; x < size; x++)
                {
                    bool inside = Mathf.Abs(x - cx) <= halfWidth;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            cachedTriangleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedTriangleSprite;
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
