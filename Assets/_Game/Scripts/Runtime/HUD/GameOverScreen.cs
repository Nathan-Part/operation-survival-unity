using System.Collections.Generic;
using Game.Data.Rules;
using Game.Data.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Écran de fin de partie riche (Phase 5.1, 2026-07-09, parité `game-over.png`) — remplace
    /// l'ancien écran minimal de `HudController` ("Vous êtes mort" + 2 boutons, sans stats).
    /// Les données existaient déjà à 80% (RunStats étendu Task 14, RankRules.RunRank Task 15,
    /// LeaderboardEntry étendu Task 16) : ce composant ne fait que construire l'affichage et lire
    /// ces valeurs, aucune nouvelle logique de calcul de gameplay ici.
    ///
    /// Simplifications assumées et documentées (voir pending-verifications-phase5.1.md) :
    /// - "Meilleure série" réutilise `stats.combo` (meilleur combo, pas un compteur de killstreak
    ///   dédié — `KillstreakController` n'expose qu'un compteur courant, pas de max sur la run).
    /// - "Distance" reste à 0 tant que `RunStats.runDistance` n'est alimenté nulle part (gap déjà
    ///   noté dans le roadmap avant cette session, pas une régression introduite ici).
    /// - "Score" reprend `waveManager.Money` (voir RunStats.score) — pas la vraie formule de score
    ///   du web, non extraite.
    public class GameOverScreen : MonoBehaviour
    {
        GameObject panel;
        Text[] cardValues;
        Text moneyText;
        Text rankText;
        RectTransform leaderboardRows;
        static readonly string[] CardLabels =
        {
            "VAGUE", "SCORE", "KILLS", "HEADSHOTS", "PRÉCISION", "MEILLEUR COUP",
            "DISTANCE", "MEILLEURE SÉRIE", "MEILLEURE VAGUE", "VAGUE LA + RAPIDE", "RECORD", "COMBO MAX",
        };

        public void Init(RectTransform root)
        {
            panel = HudBuild.MakeImage(root, new Color(0.04f, 0.045f, 0.07f, 0.93f), "GameOverPanel").gameObject;
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            var eyebrow = HudBuild.MakeText(panelRect, "// FIN DE L'OPÉRATION", 18, TextAnchor.UpperCenter, "Eyebrow");
            eyebrow.color = new Color(0.9f, 0.35f, 0.3f);
            AnchorTop(eyebrow.rectTransform, -30f, 700f, 26f);

            var title = HudBuild.MakeText(panelRect, "MISSION TERMINÉE", 46, TextAnchor.UpperCenter, "Title");
            title.fontStyle = FontStyle.Bold;
            AnchorTop(title.rectTransform, -60f, 900f, 60f);

            // Grille de 12 cartes stat (GridLayoutGroup : plus simple/robuste qu'un positionnement
            // manuel pour une grille régulière affichée une fois, pas mise à jour frame par frame).
            var gridGo = new GameObject("StatsGrid");
            gridGo.transform.SetParent(panelRect, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 1f);
            gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = new Vector2(0f, -140f);
            gridRect.sizeDelta = new Vector2(1400f, 220f);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(220f, 90f);
            grid.spacing = new Vector2(10f, 10f);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;

            cardValues = new Text[CardLabels.Length];
            for (int i = 0; i < CardLabels.Length; i++)
            {
                var cardBg = HudBuild.MakeImage(gridRect, new Color(1f, 1f, 1f, 0.06f), $"Card_{i}");
                cardBg.gameObject.AddComponent<Outline>().effectColor = new Color(1f, 1f, 1f, 0.15f);
                var cardRect = cardBg.rectTransform;

                var label = HudBuild.MakeText(cardRect, CardLabels[i], 11, TextAnchor.UpperCenter, "Label");
                label.color = new Color(0.65f, 0.68f, 0.75f);
                label.rectTransform.anchorMin = new Vector2(0f, 1f);
                label.rectTransform.anchorMax = new Vector2(1f, 1f);
                label.rectTransform.pivot = new Vector2(0.5f, 1f);
                label.rectTransform.anchoredPosition = new Vector2(0f, -8f);
                label.rectTransform.sizeDelta = new Vector2(-8f, 26f);

                var value = HudBuild.MakeText(cardRect, "0", 26, TextAnchor.MiddleCenter, "Value");
                value.fontStyle = FontStyle.Bold;
                value.rectTransform.anchorMin = new Vector2(0f, 0f);
                value.rectTransform.anchorMax = new Vector2(1f, 1f);
                value.rectTransform.sizeDelta = new Vector2(-8f, -34f);
                value.rectTransform.anchoredPosition = new Vector2(0f, -8f);
                cardValues[i] = value;
            }

            // Argent gagné + rang
            var moneyLabel = HudBuild.MakeText(panelRect, "ARGENT GAGNÉ", 12, TextAnchor.MiddleCenter, "MoneyLabel");
            moneyLabel.color = new Color(0.65f, 0.68f, 0.75f);
            AnchorTop(moneyLabel.rectTransform, -380f, 300f, 20f);
            moneyText = HudBuild.MakeText(panelRect, "+0", 34, TextAnchor.MiddleCenter, "MoneyValue");
            moneyText.color = new Color(0.55f, 0.4f, 1f);
            moneyText.fontStyle = FontStyle.Bold;
            AnchorTop(moneyText.rectTransform, -410f, 300f, 44f);

            rankText = HudBuild.MakeText(panelRect, "RANG D", 30, TextAnchor.MiddleCenter, "RankValue");
            rankText.color = new Color(1f, 0.82f, 0.35f);
            rankText.fontStyle = FontStyle.Bold;
            AnchorTop(rankText.rectTransform, -470f, 300f, 40f);

            // Classement local (5 lignes, VerticalLayoutGroup — même raison que la grille ci-dessus).
            var lbTitle = HudBuild.MakeText(panelRect, "// CLASSEMENT LOCAL", 13, TextAnchor.MiddleCenter, "LeaderboardTitle");
            lbTitle.color = new Color(0.65f, 0.68f, 0.75f);
            AnchorTop(lbTitle.rectTransform, -520f, 300f, 20f);

            var lbGo = new GameObject("LeaderboardRows");
            lbGo.transform.SetParent(panelRect, false);
            leaderboardRows = lbGo.AddComponent<RectTransform>();
            leaderboardRows.anchorMin = new Vector2(0.5f, 1f);
            leaderboardRows.anchorMax = new Vector2(0.5f, 1f);
            leaderboardRows.pivot = new Vector2(0.5f, 1f);
            leaderboardRows.anchoredPosition = new Vector2(0f, -546f);
            leaderboardRows.sizeDelta = new Vector2(360f, 120f);
            var lbLayout = lbGo.AddComponent<VerticalLayoutGroup>();
            lbLayout.childAlignment = TextAnchor.UpperCenter;
            lbLayout.spacing = 2f;
            lbLayout.childForceExpandWidth = true;
            lbLayout.childForceExpandHeight = false;

            // Boutons Rejouer/Menu — même logique de navigation que l'ancien écran minimal.
            var replay = MakeButton(panelRect, "▶ Rejouer", new Vector2(-170f, 40f), new Color(0.49f, 0.3f, 1f));
            replay.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            });
            var menu = MakeButton(panelRect, "Menu", new Vector2(170f, 40f), new Color(0.25f, 0.25f, 0.3f, 0.95f));
            menu.onClick.AddListener(() =>
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("Home");
            });

            panel.SetActive(false);
        }

        static void AnchorTop(RectTransform rect, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        static Button MakeButton(RectTransform parent, string label, Vector2 anchoredPos, Color color)
        {
            var image = HudBuild.MakeImage(parent, color, $"Button_{label}");
            image.raycastTarget = true;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(280f, 64f);
            var button = image.gameObject.AddComponent<Button>();
            var text = HudBuild.MakeText(rect, label, 26, TextAnchor.MiddleCenter, "Label");
            text.fontStyle = FontStyle.Bold;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            return button;
        }

        /// Affiche l'écran avec les stats finales de la run. `stats`/`moneyEarned` viennent de la run
        /// qui vient de se terminer ; `leaderboard` doit déjà contenir l'entrée de cette run (poussée
        /// par MetaProgress.OnRunEnded, appelé de façon synchrone sur PlayerHealth.Died — donc déjà
        /// à jour au moment où cet écran s'affiche, même si l'affichage lui-même est différé par
        /// l'animation de mort).
        public void Show(RunStats stats, int moneyEarned, int bestWaveRecord, int bestScoreRecord, List<LeaderboardEntry> leaderboard)
        {
            int[] values =
            {
                stats.wave, stats.score, stats.runKills, stats.runHeadshots,
                Mathf.RoundToInt(stats.Accuracy * 100f), Mathf.RoundToInt(stats.bestHitDistance),
                stats.runDistance, stats.combo, bestWaveRecord, Mathf.RoundToInt(stats.fastestWaveSeconds),
                bestScoreRecord, stats.combo,
            };
            string[] suffixes = { "", "", "", "", "%", "m", "m", "", "", "s", "", "×" };
            for (int i = 0; i < cardValues.Length; i++)
                cardValues[i].text = $"{values[i]}{suffixes[i]}";

            moneyText.text = $"+{moneyEarned}";
            rankText.text = $"RANG {RankRules.RunRank(stats.runKills, stats.Accuracy, stats.wave)}";

            for (int i = leaderboardRows.childCount - 1; i >= 0; i--) Destroy(leaderboardRows.GetChild(i).gameObject);
            for (int i = 0; i < leaderboard.Count && i < 5; i++)
            {
                var entry = leaderboard[i];
                var row = HudBuild.MakeText(leaderboardRows, $"#{i + 1}   {entry.score}   Vague {entry.wave}   {entry.date}", 14, TextAnchor.MiddleCenter, $"Row_{i}");
                var rowRect = row.rectTransform;
                rowRect.sizeDelta = new Vector2(360f, 22f);
            }

            panel.SetActive(true);
            Time.timeScale = 0f;
        }
    }
}
