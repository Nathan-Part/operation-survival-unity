using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Indicateurs directionnels de dégâts (web #63) : 4 secteurs au bord de l'écran,
    /// fondu ~1 s après chaque coup reçu.
    public class HudDamageIndicators : MonoBehaviour
    {
        const float FadePerSecond = 1f;
        static readonly Color IndicatorColor = new Color(0.9f, 0.15f, 0.15f, 1f);

        PlayerHealth health;
        Transform playerBody;
        Camera cam;
        Image[] sectors; // 0 haut (devant), 1 droite, 2 bas (derrière), 3 gauche
        readonly float[] alpha = new float[4];

        public void Init(RectTransform root, PlayerHealth health, Transform playerBody, Camera camera)
        {
            this.health = health;
            this.playerBody = playerBody;
            cam = camera;
            sectors = new Image[4];
            // (ancre, position, taille) par secteur
            var specs = new (Vector2 anchor, Vector2 pos, Vector2 size)[]
            {
                (new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(220f, 14f)),
                (new Vector2(1f, 0.5f), new Vector2(-130f, 0f), new Vector2(14f, 220f)),
                (new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(220f, 14f)),
                (new Vector2(0f, 0.5f), new Vector2(130f, 0f), new Vector2(14f, 220f)),
            };
            for (int i = 0; i < 4; i++)
            {
                var img = HudBuild.MakeImage(root, IndicatorColor, "DamageDir" + i);
                var rect = img.rectTransform;
                rect.anchorMin = rect.anchorMax = specs[i].anchor;
                rect.anchoredPosition = specs[i].pos;
                rect.sizeDelta = specs[i].size;
                img.gameObject.SetActive(false);
                sectors[i] = img;
            }
            health.DamagedFrom += OnDamagedFrom;
        }

        void OnDamagedFrom(float amount, Vector3 sourcePos)
        {
            if (playerBody == null || cam == null) return;
            float worldAngle = DamageIndicatorModel.WorldAngleDeg(playerBody.position, sourcePos);
            int sector = DamageIndicatorModel.SectorFor(worldAngle, cam.transform.eulerAngles.y);
            alpha[sector] = 1f;
        }

        void Update()
        {
            for (int i = 0; i < 4; i++)
            {
                if (alpha[i] <= 0f)
                {
                    if (sectors[i].gameObject.activeSelf) sectors[i].gameObject.SetActive(false);
                    continue;
                }
                alpha[i] = Mathf.Max(0f, alpha[i] - FadePerSecond * Time.deltaTime);
                var c = IndicatorColor;
                c.a = alpha[i];
                sectors[i].color = c;
                if (!sectors[i].gameObject.activeSelf) sectors[i].gameObject.SetActive(true);
            }
        }

        void OnDestroy()
        {
            if (health != null) health.DamagedFrom -= OnDamagedFrom;
        }
    }
}
