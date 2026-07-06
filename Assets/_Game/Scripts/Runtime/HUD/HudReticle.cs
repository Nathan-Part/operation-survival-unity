using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Réticule ↔ viseur (sight) en visée ↔ lunette sniper plein écran (web #61) :
    /// crosshair masqué quand on vise, sight 4 traits + point, overlay scope assombri.
    public class HudReticle : MonoBehaviour
    {
        PlayerViewController view;
        GameObject crosshair;
        GameObject sight;
        GameObject scopeOverlay;

        public void Init(RectTransform root, PlayerViewController view, GameObject existingCrosshair)
        {
            this.view = view;
            crosshair = existingCrosshair;

            // Sight : 4 traits fins autour du centre + point central
            var sightRect = HudBuild.MakeCentered(root, "Sight");
            var offsets = new Vector2[] { new Vector2(0f, 22f), new Vector2(22f, 0f), new Vector2(0f, -22f), new Vector2(-22f, 0f) };
            for (int i = 0; i < 4; i++)
            {
                var stroke = HudBuild.MakeImage(sightRect, new Color(1f, 1f, 1f, 0.9f), "SightStroke");
                stroke.rectTransform.sizeDelta = i % 2 == 0 ? new Vector2(2f, 12f) : new Vector2(12f, 2f);
                stroke.rectTransform.anchoredPosition = offsets[i];
            }
            var dot = HudBuild.MakeImage(sightRect, new Color(1f, 1f, 1f, 0.9f), "SightDot");
            dot.rectTransform.sizeDelta = new Vector2(3f, 3f);
            sight = sightRect.gameObject;
            sight.SetActive(false);

            // Scope : 4 pans noirs laissant un carré central + croix fine (approximation du radial web)
            var scopeRect = HudBuild.Fullscreen(root, "ScopeOverlay");
            const float hole = 420f;
            var panels = new (Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)[]
            {
                (new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10000f), new Vector2(-hole * 0.5f, 10000f)), // gauche
                (new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(hole * 0.5f, -10000f), new Vector2(0f, 10000f)),   // droite
                (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(-hole * 0.5f, hole * 0.5f), new Vector2(hole * 0.5f, 0f)), // haut
                (new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-hole * 0.5f, 0f), new Vector2(hole * 0.5f, -hole * 0.5f)), // bas
            };
            foreach (var p in panels)
            {
                var img = HudBuild.MakeImage(scopeRect, new Color(0f, 0f, 0f, 0.92f), "ScopePanel");
                var r = img.rectTransform;
                r.anchorMin = p.anchorMin; r.anchorMax = p.anchorMax;
                r.offsetMin = p.offsetMin; r.offsetMax = p.offsetMax;
            }
            var crossV = HudBuild.MakeImage(scopeRect, new Color(0f, 0f, 0f, 0.85f), "ScopeCrossV");
            crossV.rectTransform.anchorMin = crossV.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crossV.rectTransform.sizeDelta = new Vector2(2f, hole);
            var crossH = HudBuild.MakeImage(scopeRect, new Color(0f, 0f, 0f, 0.85f), "ScopeCrossH");
            crossH.rectTransform.anchorMin = crossH.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crossH.rectTransform.sizeDelta = new Vector2(hole, 2f);
            scopeOverlay = scopeRect.gameObject;
            scopeOverlay.SetActive(false);
        }

        void Update()
        {
            if (view == null) return;
            bool aiming = view.AimHeld && !view.IsDead;
            bool scope = view.ScopeOn;
            if (crosshair != null && crosshair.activeSelf == aiming) crosshair.SetActive(!aiming); // web: crosshair masqué en visée
            if (sight.activeSelf != (aiming && !scope)) sight.SetActive(aiming && !scope);
            if (scopeOverlay.activeSelf != scope) scopeOverlay.SetActive(scope);
        }
    }
}
