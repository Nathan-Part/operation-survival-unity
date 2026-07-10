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

        /// Rayures diagonales semi-transparentes par-dessus le dégradé (parité homepage.png :
        /// le grand bouton Déployer du web est strié).
        [UxmlAttribute("stripes")]
        public bool Stripes { get; set; }

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

            if (Stripes) DrawStripes(painter, w, h, cut);

            painter.strokeColor = BorderColor;
            painter.lineWidth = 1f;
            TacticalPanel.DrawCutRect(painter, new Rect(0f, 0f, w, h), cut, Corners);
            painter.Stroke();
        }

        /// Rayures diagonales à 45° clippées au polygone à coins coupés (Painter2D n'a pas de
        /// clipping natif : chaque parallélogramme de rayure est intersecté explicitement via
        /// Sutherland–Hodgman — le polygone à coins coupés est convexe).
        void DrawStripes(Painter2D painter, float w, float h, float cut)
        {
            var poly = new System.Collections.Generic.List<Vector2>();
            bool tl = (Corners & TacticalCorners.TopLeft) != 0;
            bool tr = (Corners & TacticalCorners.TopRight) != 0;
            bool bl = (Corners & TacticalCorners.BottomLeft) != 0;
            bool br = (Corners & TacticalCorners.BottomRight) != 0;
            poly.Add(new Vector2(tl ? cut : 0f, 0f));
            poly.Add(new Vector2(w - (tr ? cut : 0f), 0f));
            if (tr) poly.Add(new Vector2(w, cut));
            poly.Add(new Vector2(w, h - (br ? cut : 0f)));
            if (br) poly.Add(new Vector2(w - cut, h));
            poly.Add(new Vector2(bl ? cut : 0f, h));
            if (bl) poly.Add(new Vector2(0f, h - cut));
            poly.Add(new Vector2(0f, tl ? cut : 0f));

            painter.fillColor = new Color(1f, 1f, 1f, 0.06f);
            const float stripeWidth = 12f;
            const float gap = 26f;
            for (float x0 = -h; x0 < w; x0 += stripeWidth + gap)
            {
                var stripe = new System.Collections.Generic.List<Vector2>
                {
                    new Vector2(x0, h),
                    new Vector2(x0 + stripeWidth, h),
                    new Vector2(x0 + stripeWidth + h, 0f),
                    new Vector2(x0 + h, 0f),
                };
                var clipped = ClipConvex(stripe, poly);
                if (clipped.Count < 3) continue;
                painter.BeginPath();
                painter.MoveTo(clipped[0]);
                for (int i = 1; i < clipped.Count; i++) painter.LineTo(clipped[i]);
                painter.ClosePath();
                painter.Fill();
            }
        }

        /// Sutherland–Hodgman : clippe `subject` contre le polygone convexe `clip` (sommets horaires).
        static System.Collections.Generic.List<Vector2> ClipConvex(
            System.Collections.Generic.List<Vector2> subject,
            System.Collections.Generic.List<Vector2> clip)
        {
            var output = subject;
            for (int i = 0; i < clip.Count && output.Count > 0; i++)
            {
                var a = clip[i];
                var b = clip[(i + 1) % clip.Count];
                var input = output;
                output = new System.Collections.Generic.List<Vector2>(input.Count + 2);

                // Côté "intérieur" = à droite de l'arête (sommets déclarés en sens horaire, y vers le bas).
                float Side(Vector2 p) => (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);

                for (int j = 0; j < input.Count; j++)
                {
                    var current = input[j];
                    var previous = input[(j + input.Count - 1) % input.Count];
                    bool currentInside = Side(current) >= 0f;
                    bool previousInside = Side(previous) >= 0f;

                    if (currentInside)
                    {
                        if (!previousInside) output.Add(Intersect(previous, current, a, b));
                        output.Add(current);
                    }
                    else if (previousInside)
                    {
                        output.Add(Intersect(previous, current, a, b));
                    }
                }
            }
            return output;
        }

        static Vector2 Intersect(Vector2 p1, Vector2 p2, Vector2 a, Vector2 b)
        {
            var r = p2 - p1;
            var s = b - a;
            float denom = r.x * s.y - r.y * s.x;
            if (Mathf.Approximately(denom, 0f)) return p2;
            float t = ((a.x - p1.x) * s.y - (a.y - p1.y) * s.x) / denom;
            return p1 + r * t;
        }
    }
}
