using Game.Data.Rules;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Défis (3 contrats quotidiens actifs, bas-gauche, parité hud.png) : barre de progression
    /// + libellé par contrat, lus chaque frame depuis `MetaProgress.Data.daily` (contrats déjà
    /// tirés/avancés par RunTracker/DailyContractRules, Phase 4 Lot 6 — ce widget ne fait
    /// qu'afficher, aucune nouvelle logique de calcul).
    /// Playtest Phase 5.1 (2026-07-09) : widget manquant, seuls succès/contrats en toast (HudToasts)
    /// existaient — pas de suivi de progression visible en continu pendant la partie.
    public class HudChallenges : MonoBehaviour
    {
        const int MaxRows = 3;

        MetaProgress meta;
        RectTransform[] fills;
        Text[] labels;

        public void Init(RectTransform root, MetaProgress metaProgress)
        {
            meta = metaProgress;

            const float barWidth = 280f, barHeight = 20f, gap = 6f;
            float startY = 70f; // au-dessus de la barre de stamina (bas-gauche, y=44..58)

            fills = new RectTransform[MaxRows];
            labels = new Text[MaxRows];
            for (int i = 0; i < MaxRows; i++)
            {
                var back = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.5f), $"ChallengeBack_{i}");
                var br = back.rectTransform;
                br.anchorMin = br.anchorMax = br.pivot = new Vector2(0f, 0f);
                br.anchoredPosition = new Vector2(40f, startY + i * (barHeight + gap));
                br.sizeDelta = new Vector2(barWidth, barHeight);

                var fill = HudBuild.MakeImage(br, new Color(0.35f, 0.75f, 0.5f), $"ChallengeFill_{i}");
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.sizeDelta = Vector2.zero;
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                fills[i] = fill.rectTransform;

                var text = HudBuild.MakeText(br, "", 13, TextAnchor.MiddleLeft, $"ChallengeLabel_{i}");
                var tr = text.rectTransform;
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.sizeDelta = new Vector2(-12f, 0f);
                tr.anchoredPosition = new Vector2(6f, 0f);
                labels[i] = text;

                back.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (meta?.Data == null) return;
            var db = GameContext.Instance != null ? GameContext.Instance.database : null;
            var contracts = meta.Data.daily.contracts;

            for (int i = 0; i < MaxRows; i++)
            {
                bool has = i < contracts.Count;
                fills[i].transform.parent.gameObject.SetActive(has);
                if (!has) continue;

                var sc = contracts[i];
                var def = db != null ? FindContract(db.contracts, sc.id) : null;
                int target = def != null ? def.target : Mathf.Max(sc.progress, 1);
                string desc = def != null ? def.desc : sc.id;

                float frac = target > 0 ? Mathf.Clamp01((float)sc.progress / target) : (sc.done ? 1f : 0f);
                fills[i].anchorMax = new Vector2(frac, 1f);
                labels[i].text = sc.done ? $"✓ {desc}" : $"{desc} ({sc.progress}/{target})";
            }
        }

        static Game.Data.ContractData FindContract(Game.Data.ContractData[] pool, string id)
        {
            if (pool == null) return null;
            foreach (var c in pool) if (c != null && c.id == id) return c;
            return null;
        }
    }
}
