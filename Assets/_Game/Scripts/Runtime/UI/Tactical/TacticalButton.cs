using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime.UI.Tactical
{
    /// Bouton CTA a coins coupes avec degrade approxime par bandes (Painter2D n'expose pas de fill degrade natif ici).
    /// [UxmlElement] (Unity 6) — cf. TacticalPanel pour la raison de l'abandon de UxmlFactory/UxmlTraits.
    [UxmlElement]
    public partial class TacticalButton : Button
    {
        [UxmlAttribute("cut-size")]
        public float CutSize { get; set; } = 14f;

        [UxmlAttribute("corners")]
        public TacticalCorners Corners { get; set; } = TacticalCorners.TopLeft | TacticalCorners.BottomRight;

        [UxmlAttribute("color-a")]
        public Color ColorA { get; set; } = new Color(0.486f, 0.302f, 1f, 1f);

        [UxmlAttribute("color-b")]
        public Color ColorB { get; set; } = new Color(0.318f, 0.169f, 0.788f, 1f);

        [UxmlAttribute("border-color")]
        public Color BorderColor { get; set; } = new Color(1f, 1f, 1f, 0.15f);

        public TacticalButton()
        {
            generateVisualContent += OnGenerateVisualContent;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f) return;
            var painter = mgc.painter2D;

            const int bands = 12;
            for (int i = 0; i < bands; i++)
            {
                float t0 = (float)i / bands, t1 = (float)(i + 1) / bands;
                var band = new Rect(rect.x, rect.y + rect.height * t0, rect.width, rect.height * (t1 - t0) + 1f);
                painter.fillColor = Color.Lerp(ColorA, ColorB, t0);
                var bandCorners = i == 0 ? (Corners & (TacticalCorners.TopLeft | TacticalCorners.TopRight))
                    : (i == bands - 1 ? (Corners & (TacticalCorners.BottomLeft | TacticalCorners.BottomRight)) : TacticalCorners.None);
                TacticalPanel.DrawCutRect(painter, band, CutSize, bandCorners);
                painter.Fill();
            }

            painter.fillColor = Color.clear;
            painter.strokeColor = BorderColor;
            painter.lineWidth = 1f;
            TacticalPanel.DrawCutRect(painter, rect, CutSize, Corners);
            painter.Stroke();
        }
    }
}
