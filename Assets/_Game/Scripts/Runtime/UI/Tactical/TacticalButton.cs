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
            // Neutralise l'habillage par defaut de .unity-button (fond gris ~RGBA(0.737), bordure,
            // coins arrondis) : le rendu visible vient exclusivement du Painter2D ci-dessous. En
            // style inline, ces valeurs gagnent aussi sur les etats :hover/:active du theme par
            // defaut (playtest 2026-07-10 : le gris par defaut recouvrait le degrade).
            style.backgroundColor = Color.clear;
            style.borderTopWidth = 0f;
            style.borderBottomWidth = 0f;
            style.borderLeftWidth = 0f;
            style.borderRightWidth = 0f;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            // layout (et pas contentRect) : le fond doit couvrir TOUT l'element, padding compris —
            // contentRect exclut le padding, ce qui retrecissait le rendu (playtest 2026-07-10).
            float w = layout.width, h = layout.height;
            if (w < 1f || h < 1f) return;
            var painter = mgc.painter2D;

            float cut = Mathf.Min(CutSize, Mathf.Min(w, h) * 0.5f);
            bool tl = (Corners & TacticalCorners.TopLeft) != 0;
            bool tr = (Corners & TacticalCorners.TopRight) != 0;
            bool bl = (Corners & TacticalCorners.BottomLeft) != 0;
            bool br = (Corners & TacticalCorners.BottomRight) != 0;

            // Bords gauche/droit du polygone a coins coupes, a une hauteur y donnee.
            float LeftAt(float y) =>
                tl && y < cut ? cut - y :
                bl && y > h - cut ? y - (h - cut) : 0f;
            float RightAt(float y) =>
                tr && y < cut ? w - (cut - y) :
                br && y > h - cut ? w - (y - (h - cut)) : w;

            // Degrade vertical par bandes horizontales : chaque bande est le trapeze exact decoupe
            // dans le polygone (les cassures de biseau y=cut / y=h-cut sont inserees comme sommets
            // quand la bande les traverse). L'ancienne version passait des Rect decales a
            // DrawCutRect qui ignorait leur position -> toutes les bandes s'empilaient a y=0
            // (playtest 2026-07-10 : bouton Deployer gris avec un seul liseret violet en haut).
            const int bands = 24;
            for (int i = 0; i < bands; i++)
            {
                float y0 = h * i / bands;
                float y1 = h * (i + 1) / bands;
                float yEnd = i < bands - 1 ? Mathf.Min(y1 + 0.75f, h) : h; // recouvre la couture anti-hairline

                painter.fillColor = Color.Lerp(ColorA, ColorB, (float)i / (bands - 1));
                painter.BeginPath();
                painter.MoveTo(new Vector2(LeftAt(y0), y0));
                painter.LineTo(new Vector2(RightAt(y0), y0));
                if (cut > y0 && cut < yEnd) painter.LineTo(new Vector2(RightAt(cut), cut));
                if (h - cut > y0 && h - cut < yEnd) painter.LineTo(new Vector2(RightAt(h - cut), h - cut));
                painter.LineTo(new Vector2(RightAt(yEnd), yEnd));
                painter.LineTo(new Vector2(LeftAt(yEnd), yEnd));
                if (h - cut > y0 && h - cut < yEnd) painter.LineTo(new Vector2(LeftAt(h - cut), h - cut));
                if (cut > y0 && cut < yEnd) painter.LineTo(new Vector2(LeftAt(cut), cut));
                painter.ClosePath();
                painter.Fill();
            }

            painter.strokeColor = BorderColor;
            painter.lineWidth = 1f;
            TacticalPanel.DrawCutRect(painter, new Rect(0f, 0f, w, h), cut, Corners);
            painter.Stroke();
        }
    }
}
