using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime.UI.Tactical
{
    [Flags]
    public enum TacticalCorners
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomLeft = 4,
        BottomRight = 8,
    }

    /// Carte avec coin(s) coupes en biseau, dessinee via Painter2D (USS ne supporte pas clip-path).
    public class TacticalPanel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TacticalPanel, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            readonly UxmlFloatAttributeDescription m_CutSize = new UxmlFloatAttributeDescription { name = "cut-size", defaultValue = 16f };
            readonly UxmlEnumAttributeDescription<TacticalCorners> m_Corners = new UxmlEnumAttributeDescription<TacticalCorners> { name = "corners", defaultValue = TacticalCorners.TopRight };
            readonly UxmlColorAttributeDescription m_FillColor = new UxmlColorAttributeDescription { name = "fill-color", defaultValue = new Color(0.07f, 0.078f, 0.125f, 0.82f) };
            readonly UxmlColorAttributeDescription m_BorderColor = new UxmlColorAttributeDescription { name = "border-color", defaultValue = new Color(0.486f, 0.302f, 1f, 0.4f) };
            readonly UxmlFloatAttributeDescription m_BorderWidth = new UxmlFloatAttributeDescription { name = "border-width", defaultValue = 1f };
            readonly UxmlBoolAttributeDescription m_ShowAccent = new UxmlBoolAttributeDescription { name = "show-accent", defaultValue = false };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var p = (TacticalPanel)ve;
                p.CutSize = m_CutSize.GetValueFromBag(bag, cc);
                p.Corners = m_Corners.GetValueFromBag(bag, cc);
                p.FillColor = m_FillColor.GetValueFromBag(bag, cc);
                p.BorderColor = m_BorderColor.GetValueFromBag(bag, cc);
                p.BorderWidth = m_BorderWidth.GetValueFromBag(bag, cc);
                p.ShowAccent = m_ShowAccent.GetValueFromBag(bag, cc);
            }
        }

        public float CutSize { get; set; } = 16f;
        public TacticalCorners Corners { get; set; } = TacticalCorners.TopRight;
        public Color FillColor { get; set; } = new Color(0.07f, 0.078f, 0.125f, 0.82f);
        public Color BorderColor { get; set; } = new Color(0.486f, 0.302f, 1f, 0.4f);
        public float BorderWidth { get; set; } = 1f;
        public bool ShowAccent { get; set; }

        public TacticalPanel()
        {
            generateVisualContent += OnGenerateVisualContent;
            style.overflow = Overflow.Visible;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f) return;

            var painter = mgc.painter2D;
            painter.fillColor = FillColor;
            painter.strokeColor = BorderColor;
            painter.lineWidth = BorderWidth;

            DrawCutRect(painter, rect, CutSize, Corners);
            painter.Fill();
            painter.Stroke();

            if (ShowAccent)
            {
                painter.fillColor = BorderColor;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0, 6));
                painter.LineTo(new Vector2(3, 6));
                painter.LineTo(new Vector2(3, rect.height - 6));
                painter.LineTo(new Vector2(0, rect.height - 6));
                painter.ClosePath();
                painter.Fill();
            }
        }

        /// Trace le contour d'un rectangle avec les coins indiques coupes en biseau de taille `cut`.
        /// Partage entre TacticalPanel/TacticalButton/TacticalModalFrame — signature stable, ne pas changer sans mettre a jour les 3 appelants.
        internal static void DrawCutRect(Painter2D painter, Rect r, float cut, TacticalCorners corners)
        {
            float x0 = 0, y0 = 0, x1 = r.width, y1 = r.height;
            bool tl = (corners & TacticalCorners.TopLeft) != 0;
            bool tr = (corners & TacticalCorners.TopRight) != 0;
            bool bl = (corners & TacticalCorners.BottomLeft) != 0;
            bool br = (corners & TacticalCorners.BottomRight) != 0;

            painter.BeginPath();
            painter.MoveTo(new Vector2(x0 + (tl ? cut : 0), y0));
            painter.LineTo(new Vector2(x1 - (tr ? cut : 0), y0));
            if (tr) painter.LineTo(new Vector2(x1, y0 + cut));
            painter.LineTo(new Vector2(x1, y1 - (br ? cut : 0)));
            if (br) painter.LineTo(new Vector2(x1 - cut, y1));
            painter.LineTo(new Vector2(x0 + (bl ? cut : 0), y1));
            if (bl) painter.LineTo(new Vector2(x0, y1 - cut));
            painter.LineTo(new Vector2(x0, y0 + (tl ? cut : 0)));
            if (tl) painter.LineTo(new Vector2(x0 + cut, y0));
            painter.ClosePath();
        }
    }
}
