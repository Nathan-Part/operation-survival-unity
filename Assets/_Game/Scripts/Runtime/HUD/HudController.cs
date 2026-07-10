using Game.Data.Save;
using UnityEngine;
using UnityEngine.InputSystem.UI;
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
        KillstreakController killstreak;
        MetaProgress meta;
        RunTracker tracker;

        RectTransform healthFill;
        RectTransform staminaFill;
        RectTransform waveProgressFill;
        Text ammoText;
        Text waveText;
        Text streakText;
        Text scoreText;
        Text enemiesText;
        GameOverScreen gameOverScreen;
        public HudPauseMenu PauseMenu { get; private set; }
        int lastWave;
        int lastAlive;
        int waveMaxAlive = 1;
        bool reloading;
        int inMag, magSize;

        static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public void Init(PlayerHealth health, WeaponController weapon, WaveManager waves, PlayerMotor motor, Transform playerBody,
            Camera camera = null, PlayerViewController viewController = null, MetaProgress meta = null, ConsumableThrower thrower = null,
            KillstreakController killstreak = null, LoadoutController loadout = null, PowerUpRuntime powerups = null,
            RunTracker tracker = null)
        {
            this.health = health;
            this.weapon = weapon;
            this.waves = waves;
            this.motor = motor;
            this.killstreak = killstreak;
            this.meta = meta;
            this.tracker = tracker;

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
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
            // Shrink : la référence 1080p tient toujours entière — rien n'est jamais coupé,
            // quel que soit l'aspect ratio (Phase 5 bis, point 12).
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // Parité hud2.png (Phase 5 bis, point 12) — bas-gauche, de bas en haut :
            // rang (HudMeta) → SPRINT → VIE segmentée verte → Défis (HudChallenges).
            healthFill = MakeBar(root, new Vector2(40f, 96f), new Vector2(330f, 16f), new Color(0.21f, 0.82f, 0.37f));
            HudBuild.MakeSegments((RectTransform)healthFill.parent, 24, 16f);

            var sprintLabel = MakeText(root, "SPRINT", 13, TextAnchor.LowerLeft);
            var sprintLabelRect = sprintLabel.rectTransform;
            sprintLabelRect.anchorMin = sprintLabelRect.anchorMax = sprintLabelRect.pivot = new Vector2(0f, 0f);
            sprintLabelRect.anchoredPosition = new Vector2(40f, 74f);
            sprintLabelRect.sizeDelta = new Vector2(200f, 16f);
            sprintLabel.color = new Color(0.62f, 0.48f, 1f);
            staminaFill = MakeBar(root, new Vector2(40f, 64f), new Vector2(330f, 8f), new Color(0.49f, 0.36f, 1f));

            // Bloc munitions bas-droite (hud2.png) : cartouche sombre « N / M » + SCORE,
            // surmonté du bandeau rouge « ✕ N ENNEMIS ».
            var ammoBlock = HudBuild.MakeCartouche(root, "AmmoBlock", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(240f, 104f));
            ammoText = MakeText(ammoBlock.rectTransform, "", 40, TextAnchor.MiddleCenter);
            var ammoRect = ammoText.rectTransform;
            ammoRect.anchorMin = new Vector2(0f, 0.35f);
            ammoRect.anchorMax = new Vector2(1f, 1f);
            ammoRect.sizeDelta = Vector2.zero;
            scoreText = MakeText(ammoBlock.rectTransform, "SCORE 0", 14, TextAnchor.MiddleCenter);
            var scoreRect = scoreText.rectTransform;
            scoreRect.anchorMin = Vector2.zero;
            scoreRect.anchorMax = new Vector2(1f, 0.35f);
            scoreRect.sizeDelta = Vector2.zero;
            scoreText.color = new Color(0.8f, 0.82f, 0.88f);

            var enemiesBanner = HudBuild.MakeImage(root, new Color(0.82f, 0.20f, 0.24f, 0.95f), "EnemiesBanner");
            enemiesBanner.rectTransform.anchorMin = enemiesBanner.rectTransform.anchorMax = new Vector2(1f, 0f);
            enemiesBanner.rectTransform.pivot = new Vector2(1f, 0f);
            enemiesBanner.rectTransform.anchoredPosition = new Vector2(-40f, 148f);
            enemiesBanner.rectTransform.sizeDelta = new Vector2(200f, 32f);
            enemiesText = MakeText(enemiesBanner.rectTransform, "✕ 0 ENNEMIS", 16, TextAnchor.MiddleCenter);
            enemiesText.rectTransform.anchorMin = Vector2.zero;
            enemiesText.rectTransform.anchorMax = Vector2.one;
            enemiesText.rectTransform.sizeDelta = Vector2.zero;
            enemiesText.fontStyle = FontStyle.Bold;

            // Bannière de vague haut-centre : cartouche sombre « VAGUE n · m ENNEMIS » (hud2.png).
            var waveBanner = HudBuild.MakeCartouche(root, "WaveBanner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(380f, 42f));
            waveText = MakeText(waveBanner.rectTransform, "", 22, TextAnchor.MiddleCenter);
            waveText.rectTransform.anchorMin = Vector2.zero;
            waveText.rectTransform.anchorMax = Vector2.one;
            waveText.rectTransform.sizeDelta = Vector2.zero;
            waveText.supportRichText = true;
            waveText.fontStyle = FontStyle.Bold;

            // Barre violette au-dessus de la minimap (hud2.png) : progression de la vague en cours.
            waveProgressFill = MakeBar(root, Vector2.zero, new Vector2(190f, 8f), new Color(0.55f, 0.38f, 1f));
            var waveProgressBack = (RectTransform)waveProgressFill.parent;
            waveProgressBack.anchorMin = waveProgressBack.anchorMax = new Vector2(1f, 1f);
            waveProgressBack.pivot = new Vector2(1f, 1f);
            waveProgressBack.anchoredPosition = new Vector2(-30f, -26f);

            // Compteur de killstreak (web #58) : 🔥 + nombre, affiché à partir de 2 kills
            streakText = MakeText(root, "", 32, TextAnchor.UpperCenter);
            var streakRect = streakText.rectTransform;
            streakRect.anchorMin = streakRect.anchorMax = new Vector2(0.5f, 1f);
            streakRect.pivot = new Vector2(0.5f, 1f);
            streakRect.anchoredPosition = new Vector2(0f, -74f);
            streakRect.sizeDelta = new Vector2(400f, 46f);
            streakText.color = new Color(1f, 0.5f, 0.1f);
            streakText.gameObject.SetActive(false);

            // Réticule (centre)
            var crosshair = MakeImage(root, new Color(1f, 1f, 1f, 0.8f));
            crosshair.rectTransform.anchorMin = crosshair.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.rectTransform.sizeDelta = new Vector2(6f, 6f);
            crosshair.gameObject.name = "Crosshair";

            // Radar (haut droite)
            var radarGo = new GameObject("Radar");
            radarGo.transform.SetParent(root, false);
            var radar = radarGo.AddComponent<HudRadar>();
            radar.Init(waves, playerBody);

            // HUD de combat phase 3 (parité web) — chaque composant s'abonne à ses événements
            gameObject.AddComponent<HudHitmarker>().Init(root, weapon);
            gameObject.AddComponent<HudKillFeed>().Init(root, weapon);
            gameObject.AddComponent<HudFireMode>().Init(root, weapon);
            gameObject.AddComponent<HudBossBar>().Init(root, waves);
            gameObject.AddComponent<HudScreenEffects>().Init(root, health);
            if (health.shield != null) gameObject.AddComponent<HudShield>().Init(root, health);
            gameObject.AddComponent<HudCallout>().Init(root, weapon);
            gameObject.AddComponent<HudCombo>().Init(root, waves);
            if (meta != null) gameObject.AddComponent<HudMeta>().Init(root, meta);
            if (meta != null) gameObject.AddComponent<HudConsumables>().Init(root, meta, thrower);
            if (meta != null) gameObject.AddComponent<HudChallenges>().Init(root, meta); // Defis (Phase 5.1, contrats deja tires par RunTracker)
            if (tracker != null) gameObject.AddComponent<HudToasts>().Init(root, tracker); // succès/contrats/level-up (Lot 6)
            if (loadout != null && loadout.Model != null) gameObject.AddComponent<HudLoadout>().Init(root, loadout);
            if (powerups != null) gameObject.AddComponent<HudPowerUps>().Init(root, powerups);
            gameObject.AddComponent<HudPetBar>().Init(root, () => PetController.Current);
            if (camera != null)
            {
                gameObject.AddComponent<HudEnemyBars>().Init(root, waves, camera);
                gameObject.AddComponent<HudDamageIndicators>().Init(root, health, playerBody, camera);
            }
            if (viewController != null)
                gameObject.AddComponent<HudReticle>().Init(root, viewController, crosshair.gameObject);

            // Écran de mort (affiché par PlayerViewController après l'anim de mort ;
            // repli direct sur Died si le view controller n'existe pas)
            gameOverScreen = gameObject.AddComponent<GameOverScreen>();
            gameOverScreen.Init(root);
            if (viewController == null) this.health.Died += ShowDeathScreen;

            // Menu pause (Échap, parité web) — exposé pour être branché sur PlayerInputBridge par l'appelant.
            PauseMenu = gameObject.AddComponent<HudPauseMenu>();
            PauseMenu.Init(root);

            this.health.HealthChanged += OnHealthChanged;
            this.weapon.AmmoChanged += OnAmmoChanged;
            this.weapon.ReloadingChanged += OnReloadingChanged;
            this.waves.WaveStarted += w => { lastWave = w; waveMaxAlive = Mathf.Max(1, lastAlive); RefreshWaveText(); };
            this.waves.AliveCountChanged += n =>
            {
                lastAlive = n;
                waveMaxAlive = Mathf.Max(waveMaxAlive, n); // le pic d'ennemis vivants de la vague sert de dénominateur
                RefreshWaveText();
            };

            OnHealthChanged(health.Health, health.MaxHealth);
            if (weapon.Weapon != null) OnAmmoChanged(weapon.Weapon.mag, weapon.Weapon.mag);
        }

        void Update()
        {
            if (motor != null && motor.StaminaMax > 0f)
                staminaFill.anchorMax = new Vector2(Mathf.Clamp01(motor.StaminaCurrent / motor.StaminaMax), 1f);
            if (scoreText != null && tracker != null)
                scoreText.text = $"SCORE {tracker.CurrentStats.score}";
            RefreshStreakText();
        }

        void RefreshStreakText()
        {
            int count = killstreak != null ? killstreak.StreakCount : 0;
            bool show = count >= 2;
            if (streakText.gameObject.activeSelf != show) streakText.gameObject.SetActive(show);
            if (show) streakText.text = $"SÉRIE ×{count}"; // pas d'emoji : LegacyRuntime.ttf ne les rend pas (case vide)
        }

        void OnHealthChanged(float current, float max)
        {
            healthFill.anchorMax = new Vector2(max > 0f ? Mathf.Clamp01(current / max) : 0f, 1f);
        }

        void OnAmmoChanged(int mag, int size) { inMag = mag; magSize = size; RefreshAmmoText(); }
        void OnReloadingChanged(bool value) { reloading = value; RefreshAmmoText(); }

        void RefreshAmmoText() => ammoText.text = reloading ? "RECHARGE…" : $"{inMag} / {magSize}";

        void RefreshWaveText()
        {
            // Parité hud2.png : « VAGUE n » violet + « · m ENNEMIS » blanc, bandeau rouge
            // « ✕ m ENNEMIS » au-dessus du bloc munitions, barre violette = progression de vague.
            waveText.text = $"<color=#9d7bff>VAGUE {lastWave}</color> · {lastAlive} ENNEMIS";
            if (enemiesText != null) enemiesText.text = $"✕ {lastAlive} ENNEMIS";
            if (waveProgressFill != null)
                waveProgressFill.anchorMax = new Vector2(1f - Mathf.Clamp01((float)lastAlive / waveMaxAlive), 1f);
        }

        // Playtest Phase 5.1 (2026-07-09) : remplace l'ancien écran minimal ("Vous êtes mort" + 2
        // boutons bruts) par GameOverScreen (12 cartes stats, rang, classement — parité game-over.png).
        // Rejouer/Menu ont la même logique de navigation qu'avant, déplacée dans GameOverScreen.MakeButton.
        public void ShowDeathScreen()
        {
            var stats = tracker != null ? tracker.CurrentStats : default;
            int moneyEarned = waves != null ? waves.Money : 0;
            int bestWave = meta?.Data != null ? meta.Data.bestWave : stats.wave;
            int bestScore = meta?.Data != null ? meta.Data.bestScore : stats.score;
            var leaderboard = meta?.Data != null ? meta.Data.leaderboard : new System.Collections.Generic.List<LeaderboardEntry>();
            gameOverScreen.Show(stats, moneyEarned, bestWave, bestScore, leaderboard);
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
