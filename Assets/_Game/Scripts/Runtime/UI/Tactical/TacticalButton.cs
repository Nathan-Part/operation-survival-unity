using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime.UI.Tactical
{
    /// Bouton CTA a coins coupes avec degrade approxime par bandes (Painter2D n'expose pas de fill degrade natif ici).
    public class TacticalButton : Button
    {
        public new class UxmlFactory : UxmlFactory<TacticalButton, UxmlTraits> { }

        public new class UxmlTraits : Button.UxmlTraits
        {
            readonly UxmlFloatAttributeDescription m_CutSize = new UxmlFloatAttributeDescription { name = "cut-size", defaultValue = 14f };
            readonly UxmlEnumAttributeDescription<TacticalCorners> m_Corners = new UxmlEnumAttributeDescription<TacticalCorners> { name = "corners", defaultValue = TacticalCorners.TopLeft | TacticalCorners.BottomRight };
            readonly UxmlColorAttributeDescription m_ColorA = new UxmlColorAttributeDescription { name = "color-a", defaultValue = new Color(0.486f, 0.302f, 1f, 1f) };
            readonly UxmlColorAttributeDescription m_ColorB = new UxmlColorAttributeDescription { name = "color-b", defaultValue = new Color(0.318f, 0.169f, 0.788f, 1f) };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var b = (TacticalButton)ve;
                b.CutSize = m_CutSize.GetValueFromBag(bag, cc);
                b.Corners = m_Corners.GetValueFromBag(bag, cc);
                b.GradientStops = new[] { m_ColorA.GetValueFromBag(bag, cc), m_ColorB.GetValueFromBag(bag, cc) };
            }
        }

        public float CutSize { get; set; } = 14f;
        public TacticalCorners Corners { get; set; } = TacticalCorners.TopLeft | TacticalCorners.BottomRight;
        public Color[] GradientStops { get; set; } = { new Color(0.486f, 0.302f, 1f, 1f), new Color(0.318f, 0.169f, 0.788f, 1f) };
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

            int bands = Mathf.Max(1, GradientStops.Length - 1) * 12;
            for (int i = 0; i < bands; i++)
            {
                float t0 = (float)i / bands, t1 = (float)(i + 1) / bands;
                var band = new Rect(rect.x, rect.y + rect.height * t0, rect.width, rect.height * (t1 - t0) + 1f);
                painter.fillColor = Color.Lerp(GradientStops[0], GradientStops[GradientStops.Length - 1], t0);
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
