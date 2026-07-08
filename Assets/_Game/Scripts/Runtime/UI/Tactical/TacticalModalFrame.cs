using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime.UI.Tactical
{
    /// Cadre de modale centre : fond TacticalPanel + brackets de coin decoratifs (haut-droite, bas-gauche).
    /// Le contenu existant d'un panneau (clone par son controleur) doit etre ajoute a `Content`, pas directement a l'element racine.
    public class TacticalModalFrame : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<TacticalModalFrame, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public VisualElement Content { get; }
        readonly TacticalPanel m_Background;

        public TacticalModalFrame()
        {
            style.maxWidth = 700;
            style.paddingTop = 28;
            style.paddingBottom = 28;
            style.paddingLeft = 32;
            style.paddingRight = 32;
            style.position = Position.Relative;

            m_Background = new TacticalPanel
            {
                CutSize = 20f,
                Corners = TacticalCorners.TopRight | TacticalCorners.BottomLeft,
                FillColor = new Color(0.965f, 0.969f, 0.976f, 0.98f),
                BorderColor = new Color(0.486f, 0.302f, 1f, 0.5f),
                BorderWidth = 1f,
            };
            m_Background.style.position = Position.Absolute;
            m_Background.style.left = 0; m_Background.style.right = 0;
            m_Background.style.top = 0; m_Background.style.bottom = 0;
            Add(m_Background);

            Content = new VisualElement { name = "modal-content" };
            Content.style.position = Position.Relative;
            Add(Content);

            generateVisualContent += DrawBrackets;
        }

        void DrawBrackets(MeshGenerationContext mgc)
        {
            var rect = contentRect;
            if (rect.width < 1f || rect.height < 1f) return;
            var painter = mgc.painter2D;
            painter.strokeColor = new Color(0.486f, 0.302f, 1f, 0.9f);
            painter.lineWidth = 2f;

            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.width - 22, 6));
            painter.LineTo(new Vector2(rect.width - 6, 6));
            painter.LineTo(new Vector2(rect.width - 6, 22));
            painter.Stroke();

            painter.BeginPath();
            painter.MoveTo(new Vector2(6, rect.height - 22));
            painter.LineTo(new Vector2(6, rect.height - 6));
            painter.LineTo(new Vector2(22, rect.height - 6));
            painter.Stroke();
        }
    }
}
