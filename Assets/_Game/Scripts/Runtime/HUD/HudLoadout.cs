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

            const float barWidth = 220f, barHeight = 22f, gap = 8f;
            float startY = 160f;
            for (int i = 0; i < selected.Count; i++)
            {
                string key = selected[i];
                keys[i] = key;
                string label = key;
                foreach (var s in AllSlots) if (s.key == key) { label = s.label; break; }
                displayLabels[i] = label;

                var back = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.5f), $"LoadoutBack_{key}");
                var br = back.rectTransform;
                br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 0f);
                br.anchoredPosition = new Vector2(-24f, startY + i * (barHeight + gap)); // empile vers le haut, bas-droite
                br.sizeDelta = new Vector2(barWidth, barHeight);

                var f = HudBuild.MakeImage(back.rectTransform, new Color(0.3f, 0.55f, 0.95f), $"LoadoutFill_{key}");
                f.rectTransform.anchorMin = Vector2.zero;
                f.rectTransform.anchorMax = Vector2.one;
                f.rectTransform.sizeDelta = Vector2.zero;
                f.rectTransform.pivot = new Vector2(0f, 0.5f);
                fills[i] = f.rectTransform;

                var text = HudBuild.MakeText(br, displayLabels[i], 16, TextAnchor.MiddleCenter, $"LoadoutLabel_{key}");
                var tr = text.rectTransform;
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.sizeDelta = Vector2.zero;
                labels[i] = text;
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
