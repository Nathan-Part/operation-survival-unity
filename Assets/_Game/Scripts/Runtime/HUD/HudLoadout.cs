using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Jauges de la série de points loadout (web #156) : une par bonus sélectionné,
    /// touche + libellé + remplissage, « PRÊT » affiché quand la jauge est pleine.
    public class HudLoadout : MonoBehaviour
    {
        static readonly (string key, string label, string keyLabel)[] AllSlots =
        {
            ("turret", "Tourelle", "L"), ("mine", "Mine", "Y"), ("airstrike", "Frappe aérienne", "U"),
            ("carepackage", "Colis", "I"), ("dog", "Chien", "4"),
        };

        LoadoutController loadout;
        RectTransform[] fills;
        Text[] labels;
        string[] keys;
        string[] displayLabels;

        // Playtest Phase 5.1 (2026-07-09) : jauges deplacees bas-droite (parite hud.png, au-dessus de
        // "ennemis restants"/munitions) — l'ancien affichage "L Tourelle"/"Y Mine" en bas-gauche etait
        // releve comme un libelle de debug par Nathan. Le libelle garde juste le nom du bonus (la
        // touche assignee n'est plus prefixee dans le texte, deja visible dans le manuel Comment jouer).
        public void Init(RectTransform root, LoadoutController loadout)
        {
            this.loadout = loadout;
            var selected = loadout.Model.Selected;
            fills = new RectTransform[selected.Count];
            labels = new Text[selected.Count];
            keys = new string[selected.Count];
            displayLabels = new string[selected.Count];

            // Parité hud2.png (point 12) : par jauge, ligne « NOM DU BONUS …… TOUCHE » au-dessus
            // d'une barre violette fine — empilées bas-droite au-dessus du bandeau rouge ennemis.
            const float barWidth = 260f, barHeight = 6f, rowGap = 40f;
            float startY = 208f;
            for (int i = 0; i < selected.Count; i++)
            {
                string key = selected[i];
                keys[i] = key;
                string label = key;
                string keyLabel = "";
                foreach (var s in AllSlots) if (s.key == key) { label = s.label; keyLabel = s.keyLabel; break; }
                displayLabels[i] = label.ToUpperInvariant();

                float y = startY + i * rowGap;

                var back = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.5f), $"LoadoutBack_{key}");
                var br = back.rectTransform;
                br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 0f);
                br.anchoredPosition = new Vector2(-40f, y);
                br.sizeDelta = new Vector2(barWidth, barHeight);

                var f = HudBuild.MakeImage(back.rectTransform, new Color(0.55f, 0.38f, 1f), $"LoadoutFill_{key}");
                f.rectTransform.anchorMin = Vector2.zero;
                f.rectTransform.anchorMax = Vector2.one;
                f.rectTransform.sizeDelta = Vector2.zero;
                f.rectTransform.pivot = new Vector2(0f, 0.5f);
                fills[i] = f.rectTransform;

                var text = HudBuild.MakeText(root, displayLabels[i], 13, TextAnchor.LowerLeft, $"LoadoutLabel_{key}");
                var tr = text.rectTransform;
                tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(1f, 0f);
                tr.anchoredPosition = new Vector2(-40f, y + barHeight + 2f);
                tr.sizeDelta = new Vector2(barWidth, 16f);
                labels[i] = text;

                var keyText = HudBuild.MakeText(root, keyLabel, 12, TextAnchor.LowerRight, $"LoadoutKey_{key}");
                var kr = keyText.rectTransform;
                kr.anchorMin = kr.anchorMax = kr.pivot = new Vector2(1f, 0f);
                kr.anchoredPosition = new Vector2(-40f, y + barHeight + 2f);
                kr.sizeDelta = new Vector2(barWidth, 16f);
                keyText.color = new Color(0.75f, 0.77f, 0.85f);
            }
        }

        void Update()
        {
            if (loadout?.Model == null) return;
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                fills[i].anchorMax = new Vector2(loadout.Model.Fill(key), 1f);
                bool ready = loadout.Model.IsReady(key);
                labels[i].text = ready ? $"{displayLabels[i]} — PRÊT" : displayLabels[i];
            }
        }
    }
}
