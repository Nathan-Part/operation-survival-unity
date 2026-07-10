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
    /// [UxmlElement] (Unity 6, remplace l'ancien systeme UxmlFactory/UxmlTraits — celui-ci ne
    /// s'enregistrait pas correctement dans cette version de l'editeur, cf. commit playtest 5.1).
    [UxmlElement]
    public partial class TacticalPanel : VisualElement
    {
        [UxmlAttribute("cut-size")]
        public float CutSize { get; set; } = 16f;

        [UxmlAttribute("corners")]
        public TacticalCorners Corners { get; set; } = TacticalCorners.TopRight;

        [UxmlAttribute("fill-color")]
        public Color FillColor { get; set; } = new Color(0.965f, 0.969f, 0.976f, 0.98f);

        [UxmlAttribute("border-color")]
        public Color BorderColor { get; set; } = new Color(0.486f, 0.302f, 1f, 0.4f);

        [UxmlAttribute("border-width")]
        public float BorderWidth { get; set; } = 1f;

        [UxmlAttribute("show-accent")]
        public bool ShowAccent { get; set; }

        /// Équerres violettes aux coins haut-gauche/haut-droit (parité homepage.png : les cartes
        /// blanches du web portent ces petits repères tactiques).
        [UxmlAttribute("show-brackets")]
        public bool ShowBrackets { get; set; } = true;

        public TacticalPanel()
        {
            generateVisualContent += OnGenerateVisualContent;
            style.overflow = Overflow.Visible;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            // layout (et pas contentRect) : le fond doit couvrir TOUT l'element, padding compris —
            // contentRect exclut le padding, donc avec .tac-card (padding 14px) le fond etait dessine
            // 14px trop petit sur chaque bord (rendu delave, playtest 2026-07-10).
            var rect = new Rect(0f, 0f, layout.width, layout.height);
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

            if (ShowBrackets)
            {
                var accent = new Color(0.486f, 0.302f, 1f, 0.9f);
                painter.strokeColor = accent;
                painter.lineWidth = 2f;
                float arm = Mathf.Min(10f, rect.width * 0.08f);
                float inset = 5f;

                // haut-gauche : ⌐ inversé
                painter.BeginPath();
                painter.MoveTo(new Vector2(inset, inset + arm));
                painter.LineTo(new Vector2(inset, inset));
                painter.LineTo(new Vector2(inset + arm, inset));
                painter.Stroke();

                // haut-droit (décalé sous le biseau éventuel)
                float topRightY = (Corners & TacticalCorners.TopRight) != 0 ? inset + CutSize * 0.5f : inset;
                painter.BeginPath();
                painter.MoveTo(new Vector2(rect.width - inset - arm, topRightY));
                painter.LineTo(new Vector2(rect.width - inset, topRightY));
                painter.LineTo(new Vector2(rect.width - inset, topRightY + arm));
                painter.Stroke();
            }
        }

        /// Trace le contour d'un rectangle avec les coins indiques coupes en biseau de taille `cut`.
        /// Partage entre TacticalPanel/TacticalButton/TacticalModalFrame — honore la position du rect
        /// (r.x/r.y), pas seulement sa taille (bug corrige 2026-07-10 : les bandes de degrade de
        /// TacticalButton passaient des rects decales qui etaient tous dessines a l'origine).
        internal static void DrawCutRect(Painter2D painter, Rect r, float cut, TacticalCorners corners)
        {
            float x0 = r.x, y0 = r.y, x1 = r.xMax, y1 = r.yMax;
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
