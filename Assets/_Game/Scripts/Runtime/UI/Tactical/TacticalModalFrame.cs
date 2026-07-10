using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime.UI.Tactical
{
    /// Cadre de modale centre : fond TacticalPanel + brackets de coin decoratifs (haut-droite, bas-gauche).
    /// Le contenu existant d'un panneau (clone par son controleur) doit etre ajoute a `Content`, pas directement a l'element racine.
    /// [UxmlElement] (Unity 6) — cf. TacticalPanel pour la raison de l'abandon de UxmlFactory/UxmlTraits.
    [UxmlElement]
    public partial class TacticalModalFrame : VisualElement
    {
        public VisualElement Content { get; }
        readonly TacticalPanel m_Background;

        public TacticalModalFrame()
        {
            style.maxWidth = 840; // >= le plus large des panneaux existants (howto-card, 760px) + padding
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
            // layout et pas contentRect : les brackets se posent aux coins de l'element entier,
            // pas de la zone interieure au padding (28/32px ici — cf. TacticalPanel, meme fix 2026-07-10).
            var rect = new Rect(0f, 0f, layout.width, layout.height);
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
