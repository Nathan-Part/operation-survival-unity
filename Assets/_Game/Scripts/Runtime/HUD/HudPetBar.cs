using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Barre de vie du chien de guerre (web pet.js) : bandeau vert + temps restant,
    /// visible seulement tant qu'un chien est vivant.
    public class HudPetBar : MonoBehaviour
    {
        Func<PetController> getPet;
        GameObject panel;
        RectTransform fill;
        Text label;

        public void Init(RectTransform root, Func<PetController> getPet)
        {
            this.getPet = getPet;
            var back = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.6f), "PetBar");
            var rect = back.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -122f); // sous la barre de boss / vague
            rect.sizeDelta = new Vector2(320f, 16f);
            var fillImage = HudBuild.MakeImage(rect, new Color(0.3f, 0.85f, 0.35f), "Fill"); // vert : vie du chien
            fillImage.rectTransform.anchorMin = Vector2.zero;
            fillImage.rectTransform.anchorMax = Vector2.one;
            fillImage.rectTransform.sizeDelta = Vector2.zero;
            label = HudBuild.MakeText(rect, "", 14, TextAnchor.MiddleCenter, "Label");
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.sizeDelta = Vector2.zero;
            panel = back.gameObject;
            fill = fillImage.rectTransform;
            panel.SetActive(false);
        }

        void Update()
        {
            var pet = getPet != null ? getPet() : null;
            if (pet == null)
            {
                if (panel.activeSelf) panel.SetActive(false);
                return;
            }
            if (!panel.activeSelf) panel.SetActive(true);
            float frac = pet.MaxHp > 0f ? Mathf.Clamp01(pet.Hp / pet.MaxHp) : 0f;
            fill.anchorMax = new Vector2(frac, 1f);
            label.text = $"CHIEN — {Mathf.CeilToInt(pet.TimeLeft)} s";
        }
    }
}
