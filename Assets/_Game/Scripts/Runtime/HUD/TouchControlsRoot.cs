using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Joysticks virtuels (D3/D11) : stick gauche -> <Gamepad>/leftStick, drag droite -> PlayerLook,
    /// boutons saut/dash/recharge -> boutons gamepad. Créé seulement si un écran tactile est présent.
    public static class TouchControlsRoot
    {
        public static GameObject CreateIfNeeded(PlayerLook look)
        {
            if (Touchscreen.current == null) return null;

            var root = new GameObject("TouchControls");
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            root.AddComponent<GraphicRaycaster>();
            var rootRect = (RectTransform)root.transform;

            // Zone de visée : moitié droite de l'écran
            var lookArea = new GameObject("LookArea");
            lookArea.transform.SetParent(rootRect, false);
            var lookImage = lookArea.AddComponent<Image>();
            lookImage.color = new Color(0f, 0f, 0f, 0.001f); // invisible mais raycastable
            var lookRect = (RectTransform)lookArea.transform;
            lookRect.anchorMin = new Vector2(0.5f, 0f);
            lookRect.anchorMax = new Vector2(1f, 1f);
            lookRect.sizeDelta = Vector2.zero;
            var drag = lookArea.AddComponent<TouchLookArea>();
            drag.look = look;

            // Stick gauche
            var stickBack = new GameObject("MoveStick");
            stickBack.transform.SetParent(rootRect, false);
            var backImage = stickBack.AddComponent<Image>();
            backImage.color = new Color(1f, 1f, 1f, 0.15f);
            var backRect = (RectTransform)stickBack.transform;
            backRect.anchorMin = backRect.anchorMax = new Vector2(0f, 0f);
            backRect.pivot = new Vector2(0.5f, 0.5f);
            backRect.anchoredPosition = new Vector2(240f, 240f);
            backRect.sizeDelta = new Vector2(260f, 260f);

            var knob = new GameObject("Knob");
            knob.transform.SetParent(backRect, false);
            var knobImage = knob.AddComponent<Image>();
            knobImage.color = new Color(1f, 1f, 1f, 0.45f);
            ((RectTransform)knob.transform).sizeDelta = new Vector2(110f, 110f);
            var stick = knob.AddComponent<OnScreenStick>();
            stick.movementRange = 90f;
            SetControlPath(stick, "<Gamepad>/leftStick");

            AddButton(rootRect, "Saut", new Vector2(-140f, 340f), "<Gamepad>/buttonSouth");
            AddButton(rootRect, "Dash", new Vector2(-320f, 220f), "<Gamepad>/buttonEast");
            AddButton(rootRect, "Recharge", new Vector2(-140f, 220f), "<Gamepad>/buttonWest");

            return root;
        }

        static void AddButton(RectTransform parent, string label, Vector2 position, string controlPath)
        {
            var go = new GameObject($"Button_{label}");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.25f);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(140f, 100f);
            var button = go.AddComponent<OnScreenButton>();
            SetControlPath(button, controlPath);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(rect, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
            text.raycastTarget = false;
            ((RectTransform)textGo.transform).sizeDelta = new Vector2(140f, 100f);
        }

        static void SetControlPath(OnScreenControl control, string path)
        {
            control.controlPath = path; // propriété publique sur OnScreenControl (Input System 1.19)
        }
    }

    public class TouchLookArea : MonoBehaviour, IDragHandler
    {
        public PlayerLook look;
        public float touchSensitivity = 2.5f;

        public void OnDrag(PointerEventData eventData)
        {
            if (look != null) look.AddLook(eventData.delta * touchSensitivity * 0.1f);
        }
    }
}
