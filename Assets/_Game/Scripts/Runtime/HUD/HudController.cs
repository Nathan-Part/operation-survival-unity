using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// HUD uGUI entièrement construit en code (D7) : vie, stamina, munitions,
    /// vague/restants, radar, écran de mort.
    public class HudController : MonoBehaviour
    {
        PlayerHealth health;
        WeaponController weapon;
        WaveManager waves;
        PlayerMotor motor;

        RectTransform healthFill;
        RectTransform staminaFill;
        Text ammoText;
        Text waveText;
        GameObject deathPanel;
        int lastWave;
        int lastAlive;
        bool reloading;
        int inMag, magSize;

        static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public void Init(PlayerHealth health, WeaponController weapon, WaveManager waves, PlayerMotor motor, Transform playerBody)
        {
            this.health = health;
            this.weapon = weapon;
            this.waves = waves;
            this.motor = motor;

            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }

            var canvasGo = new GameObject("HudCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // Barres de vie et de stamina (bas gauche)
            healthFill = MakeBar(root, new Vector2(40f, 80f), new Vector2(400f, 26f), new Color(0.85f, 0.2f, 0.2f));
            staminaFill = MakeBar(root, new Vector2(40f, 44f), new Vector2(400f, 14f), new Color(0.95f, 0.8f, 0.25f));

            // Munitions (bas droite)
            ammoText = MakeText(root, "", 40, TextAnchor.LowerRight);
            var ammoRect = ammoText.rectTransform;
            ammoRect.anchorMin = ammoRect.anchorMax = new Vector2(1f, 0f);
            ammoRect.pivot = new Vector2(1f, 0f);
            ammoRect.anchoredPosition = new Vector2(-40f, 40f);
            ammoRect.sizeDelta = new Vector2(500f, 60f);

            // Vague (haut centre)
            waveText = MakeText(root, "", 34, TextAnchor.UpperCenter);
            var waveRect = waveText.rectTransform;
            waveRect.anchorMin = waveRect.anchorMax = new Vector2(0.5f, 1f);
            waveRect.pivot = new Vector2(0.5f, 1f);
            waveRect.anchoredPosition = new Vector2(0f, -24f);
            waveRect.sizeDelta = new Vector2(800f, 50f);

            // Réticule (centre)
            var crosshair = MakeImage(root, new Color(1f, 1f, 1f, 0.8f));
            crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.rectTransform.sizeDelta = new Vector2(6f, 6f);

            // Radar (haut droite)
            var radarGo = new GameObject("Radar");
            radarGo.transform.SetParent(root, false);
            var radar = radarGo.AddComponent<HudRadar>();
            radar.Init(waves, playerBody);

            // Écran de mort
            deathPanel = BuildDeathPanel(root);

            this.health.HealthChanged += OnHealthChanged;
            this.health.Died += ShowDeathScreen;
            this.weapon.AmmoChanged += OnAmmoChanged;
            this.weapon.ReloadingChanged += OnReloadingChanged;
            this.waves.WaveStarted += w => { lastWave = w; RefreshWaveText(); };
            this.waves.AliveCountChanged += n => { lastAlive = n; RefreshWaveText(); };

            OnHealthChanged(health.Health, health.MaxHealth);
            if (weapon.Weapon != null) OnAmmoChanged(weapon.Weapon.mag, weapon.Weapon.mag);
        }

        void Update()
        {
            if (motor != null && motor.StaminaMax > 0f)
                staminaFill.anchorMax = new Vector2(Mathf.Clamp01(motor.StaminaCurrent / motor.StaminaMax), 1f);
        }

        void OnHealthChanged(float current, float max)
        {
            healthFill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
        }

        void OnAmmoChanged(int mag, int size) { inMag = mag; magSize = size; RefreshAmmoText(); }
        void OnReloadingChanged(bool value) { reloading = value; RefreshAmmoText(); }

        void RefreshAmmoText() => ammoText.text = reloading ? "RECHARGE…" : $"{inMag} / {magSize}";
        void RefreshWaveText() => waveText.text = $"Vague {lastWave} — {lastAlive} ennemis restants";

        public void ShowDeathScreen()
        {
            deathPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        GameObject BuildDeathPanel(RectTransform root)
        {
            var panel = MakeImage(root, new Color(0f, 0f, 0f, 0.75f)).gameObject;
            panel.name = "DeathPanel";
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            var label = MakeText(rect, "Vous êtes mort", 64, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.6f);
            label.rectTransform.sizeDelta = new Vector2(900f, 90f);

            var buttonGo = new GameObject("RestartButton");
            buttonGo.transform.SetParent(rect, false);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.9f, 0.3f, 0.2f);
            var buttonRect = (RectTransform)buttonGo.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.4f);
            buttonRect.sizeDelta = new Vector2(320f, 80f);
            var button = buttonGo.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Slice est dans les Build Settings (SliceSceneBuilder)
            });
            var buttonLabel = MakeText(buttonRect, "Rejouer", 36, TextAnchor.MiddleCenter);
            buttonLabel.rectTransform.anchorMin = Vector2.zero;
            buttonLabel.rectTransform.anchorMax = Vector2.one;
            buttonLabel.rectTransform.sizeDelta = Vector2.zero;

            panel.SetActive(false);
            return panel;
        }

        static RectTransform MakeBar(RectTransform parent, Vector2 position, Vector2 size, Color color)
        {
            var back = MakeImage(parent, new Color(0f, 0f, 0f, 0.5f));
            back.rectTransform.anchorMin = back.rectTransform.anchorMax = new Vector2(0f, 0f);
            back.rectTransform.pivot = new Vector2(0f, 0f);
            back.rectTransform.anchoredPosition = position;
            back.rectTransform.sizeDelta = size;

            var fill = MakeImage(back.rectTransform, color);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one; // anchorMax.x pilote le remplissage
            fill.rectTransform.sizeDelta = Vector2.zero;
            fill.rectTransform.pivot = new Vector2(0f, 0.5f);
            return fill.rectTransform;
        }

        static Image MakeImage(RectTransform parent, Color color)
        {
            var go = new GameObject("Image");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Text MakeText(RectTransform parent, string content, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text");
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
    }
}
