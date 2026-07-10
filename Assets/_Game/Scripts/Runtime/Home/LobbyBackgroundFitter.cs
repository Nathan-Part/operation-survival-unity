using UnityEngine;

namespace Game.Runtime
{
    /// Redimensionne le quad de fond du lobby pour qu'il remplisse exactement le frustum de la
    /// camera a sa profondeur, quel que soit l'aspect ratio de l'ecran (portrait/paysage, tablette,
    /// telephone...). Playtest Phase 5.1 (2026-07-10) : le quad avait une taille fixe (12x7) posee a
    /// l'edition dans HomeSceneBuilder, qui ne correspondait pas au frustum reel (mesure ~20.1x11.3
    /// en jeu, capture d'ecran Nathan) — le fond etait en "letterbox" (bandes de skybox visibles en
    /// haut/bas). Une taille dynamique calculee au runtime est correcte sur n'importe quel ecran,
    /// contrairement a une constante recalculee a la main pour un seul aspect ratio.
    [RequireComponent(typeof(MeshRenderer))]
    public class LobbyBackgroundFitter : MonoBehaviour
    {
        public Camera targetCamera;

        void Start() => Fit();

        void Fit()
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            Vector3 toQuad = transform.position - cam.transform.position;
            float depth = Vector3.Dot(toQuad, cam.transform.forward);
            if (depth <= 0.01f) return; // quad derriere ou sur la camera : rien a ajuster

            float frustumHeight = 2f * depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * cam.aspect;

            // Le Quad primitif Unity fait 1x1 unite avant mise a l'echelle (PrimitiveType.Quad).
            transform.localScale = new Vector3(frustumWidth, frustumHeight, 1f);
        }
    }
}
