using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Helpers de construction uGUI en code (D7), partagés par les composants HUD.
    public static class HudBuild
    {
        public static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Image MakeImage(RectTransform parent, Color color, string name = "Image")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Text MakeText(RectTransform parent, string content, int size, TextAnchor anchor, string name = "Text")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = UiFont;
            text.fontSize = size;
            text.alignment = anchor;
            text.text = content;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        public static RectTransform MakeCentered(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        public static RectTransform Fullscreen(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            return rect;
        }

        /// Cartouche sombre du HUD web (hud2.png) : rectangle très foncé semi-opaque.
        public static Image MakeCartouche(RectTransform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var img = MakeImage(parent, new Color(0.055f, 0.065f, 0.10f, 0.88f), name);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = anchor;
            img.rectTransform.pivot = pivot;
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.sizeDelta = size;
            return img;
        }

        /// Séparateurs verticaux sombres régulièrement espacés (barre de vie segmentée, hud2.png).
        public static void MakeSegments(RectTransform bar, int count, float height)
        {
            for (int i = 1; i < count; i++)
            {
                var sep = MakeImage(bar, new Color(0.055f, 0.065f, 0.10f, 0.9f), $"Seg_{i}");
                sep.rectTransform.anchorMin = sep.rectTransform.anchorMax = new Vector2((float)i / count, 0.5f);
                sep.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                sep.rectTransform.anchoredPosition = Vector2.zero;
                sep.rectTransform.sizeDelta = new Vector2(2f, height);
            }
        }

        static Sprite cachedCircleSprite;

        /// Disque plein antialiasé (minimap ronde, pastilles) — généré une fois, partagé.
        public static Sprite CircleSprite()
        {
            if (cachedCircleSprite != null) return cachedCircleSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            float r = size / 2f;
            var center = new Vector2(r, r);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d)));
                }
            tex.Apply();
            cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedCircleSprite;
        }
    }
}
