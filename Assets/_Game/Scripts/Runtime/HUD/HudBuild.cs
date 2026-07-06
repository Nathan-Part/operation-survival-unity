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
    }
}
