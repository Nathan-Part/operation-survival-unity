using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Menu pause (Échap, parité web) : panneau plein écran, gel réel via Time.timeScale,
    /// boutons Reprendre / Recommencer / Menu (patron des boutons de HudController.BuildDeathPanel).
    public class HudPauseMenu : MonoBehaviour
    {
        GameObject panel;
        public bool IsOpen => panel != null && panel.activeSelf;

        public void Init(RectTransform root)
        {
            panel = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.8f), "PausePanel").gameObject;
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var title = HudBuild.MakeText(rect, "PAUSE", 60, TextAnchor.MiddleCenter, "PauseTitle");
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 0.62f);
            title.rectTransform.sizeDelta = new Vector2(700f, 90f);

            BuildButton(rect, "Reprendre", 0.5f, () => Toggle());
            BuildButton(rect, "Recommencer", 0.4f, () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Slice est dans les Build Settings (SliceSceneBuilder)
            });
            BuildButton(rect, "Menu", 0.3f, () =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("Home"); // scène d'accueil enregistrée en Phase 5 — référence en avance, non bloquante ici
            });

            panel.SetActive(false);
        }

        static void BuildButton(RectTransform parent, string label, float y, UnityEngine.Events.UnityAction onClick)
        {
            var buttonGo = new GameObject(label + "Button");
            buttonGo.transform.SetParent(parent, false);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.25f, 0.25f, 0.3f, 0.95f);
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, y);
            buttonRect.sizeDelta = new Vector2(320f, 80f);
            var button = buttonGo.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            var buttonLabel = HudBuild.MakeText(buttonRect, label, 36, TextAnchor.MiddleCenter, label + "Label");
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.sizeDelta = Vector2.zero;
        }

        /// Ouvre/ferme le panneau et gèle/dégèle le jeu. Appelé par PlayerInputBridge (Échap) et par le bouton Reprendre.
        public void Toggle()
        {
            bool open = !IsOpen;
            panel.SetActive(open);
            Time.timeScale = open ? 0f : 1f;
        }
    }
}
