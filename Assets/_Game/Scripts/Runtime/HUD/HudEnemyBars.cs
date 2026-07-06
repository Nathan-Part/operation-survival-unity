using UnityEngine;
using UnityEngine.UI;

namespace Game.Runtime
{
    /// Barres de vie au-dessus des ennemis (web #enemy hp bars) :
    /// projetées à l'écran, ≤ 20 m, couleur selon la fraction de PV.
    public class HudEnemyBars : MonoBehaviour
    {
        const float Range = 20f;       // web: EHB_RANGE
        const float Height = 2.4f;     // web: EHB_HEIGHT
        const int PoolSize = 24;

        static readonly Color Green = new Color(0.30f, 0.69f, 0.31f);  // web: #4caf50
        static readonly Color Orange = new Color(1f, 0.60f, 0f);       // web: #ff9800
        static readonly Color Red = new Color(0.96f, 0.26f, 0.21f);    // web: #f44336

        WaveManager waves;
        Camera cam;
        RectTransform canvasRect;
        RectTransform[] backs;
        Image[] fills;

        public void Init(RectTransform root, WaveManager waves, Camera camera)
        {
            this.waves = waves;
            cam = camera;
            canvasRect = root;
            backs = new RectTransform[PoolSize];
            fills = new Image[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var back = HudBuild.MakeImage(root, new Color(0f, 0f, 0f, 0.55f), "EnemyBar");
                back.rectTransform.sizeDelta = new Vector2(44f, 6f);
                back.rectTransform.anchorMin = back.rectTransform.anchorMax = Vector2.zero;
                var fill = HudBuild.MakeImage(back.rectTransform, Green, "Fill");
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one; // anchorMax.x pilote le remplissage
                fill.rectTransform.sizeDelta = Vector2.zero;
                back.gameObject.SetActive(false);
                backs[i] = back.rectTransform;
                fills[i] = fill;
            }
        }

        void LateUpdate()
        {
            if (waves == null || cam == null) return;
            int used = 0;
            var alive = waves.Alive;
            var camPos = cam.transform.position;
            for (int i = 0; i < alive.Count && used < PoolSize; i++)
            {
                var enemy = alive[i];
                if (enemy == null || enemy.IsDead) continue;
                var worldPos = enemy.transform.position + Vector3.up * (Height * (enemy.Data != null ? enemy.Data.scale : 1f));
                if ((worldPos - camPos).sqrMagnitude > Range * Range) continue;
                var screen = cam.WorldToScreenPoint(worldPos);
                if (screen.z <= 0f) continue;

                var back = backs[used];
                back.gameObject.SetActive(true);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out Vector2 local);
                back.anchoredPosition = local + (canvasRect.sizeDelta * 0.5f); // ancres en (0,0)
                float frac = enemy.MaxHealth > 0 ? Mathf.Clamp01((float)enemy.Health / enemy.MaxHealth) : 0f;
                fills[used].rectTransform.anchorMax = new Vector2(frac, 1f);
                fills[used].color = frac > 0.5f ? Green : frac > 0.25f ? Orange : Red;
                used++;
            }
            for (int i = used; i < PoolSize; i++)
                if (backs[i].gameObject.activeSelf) backs[i].gameObject.SetActive(false);
        }
    }
}
