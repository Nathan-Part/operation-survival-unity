using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime.UI.Tactical
{
    public enum TacticalIconType
    {
        None = 0,
        Person,   // opérateur
        Plus,     // crédits (⊕)
        Trophy,   // vague / meilleur combo
        Doc,      // intel
        Cube,     // équipement
        Play,     // déployer
        Target,   // objectif
        Gun,      // armurerie
        Ring,     // série de points
        Runner,   // entraînement
        Map,      // map
        Bars,     // difficulté croissante
        Sun,      // réglages
        Lock,     // item non possédé
        Blade,    // arme de mêlée
    }

    /// Glyphe vectoriel blanc dessiné via Painter2D (les emojis ne se rendent pas avec les TTF
    /// Barlow — parité homepage.png qui utilise des icônes dans des carrés violet sombre).
    /// Le carré de fond est fourni par le PARENT via USS (.icon-square : fond + border-radius) ;
    /// cet élément ne dessine que le glyphe, en flex-grow 1 dans son slot.
    [UxmlElement]
    public partial class TacticalIcon : VisualElement
    {
        [UxmlAttribute("icon")]
        public TacticalIconType Icon { get; set; } = TacticalIconType.None;

        [UxmlAttribute("glyph-color")]
        public Color GlyphColor { get; set; } = Color.white;

        public TacticalIcon()
        {
            pickingMode = PickingMode.Ignore;
            style.flexGrow = 1f;
            generateVisualContent += OnGenerateVisualContent;
        }

        /// Insère un TacticalIcon dans le slot nommé `slotName` sous `root` (idempotent : vide le slot d'abord).
        public static TacticalIcon Mount(VisualElement root, string slotName, TacticalIconType icon, Color? color = null)
        {
            var slot = root?.Q<VisualElement>(slotName);
            if (slot == null) return null;
            slot.Clear();
            var element = new TacticalIcon { Icon = icon, GlyphColor = color ?? Color.white };
            slot.Add(element);
            return element;
        }

        /// Fabrique un TacticalIcon autonome de taille fixe (remplace les Labels emoji des tuiles
        /// boutique — les emojis ne se rendent pas avec Barlow).
        public static TacticalIcon Create(TacticalIconType icon, Color color, float size)
        {
            var element = new TacticalIcon { Icon = icon, GlyphColor = color };
            element.style.flexGrow = 0f;
            element.style.width = size;
            element.style.height = size;
            return element;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            float w = layout.width, h = layout.height;
            if (w < 1f || h < 1f || Icon == TacticalIconType.None) return;

            // Zone de dessin : carré centré, coordonnées normalisées 0..1.
            float s = Mathf.Min(w, h);
            var origin = new Vector2((w - s) * 0.5f, (h - s) * 0.5f);
            Vector2 P(float x, float y) => origin + new Vector2(x * s, y * s);

            var painter = mgc.painter2D;
            painter.fillColor = GlyphColor;
            painter.strokeColor = GlyphColor;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.lineWidth = Mathf.Max(1.5f, s * 0.07f);

            switch (Icon)
            {
                case TacticalIconType.Person:
                    painter.BeginPath();
                    painter.Arc(P(0.5f, 0.36f), s * 0.13f, 0, 360);
                    painter.Fill();
                    painter.BeginPath();
                    painter.Arc(P(0.5f, 0.85f), s * 0.24f, 180, 360);
                    painter.ClosePath();
                    painter.Fill();
                    break;

                case TacticalIconType.Plus:
                    painter.BeginPath();
                    painter.Arc(P(0.5f, 0.5f), s * 0.28f, 0, 360);
                    painter.Stroke();
                    Line(painter, P(0.36f, 0.5f), P(0.64f, 0.5f));
                    Line(painter, P(0.5f, 0.36f), P(0.5f, 0.64f));
                    break;

                case TacticalIconType.Trophy:
                    painter.BeginPath();
                    painter.MoveTo(P(0.3f, 0.24f));
                    painter.LineTo(P(0.7f, 0.24f));
                    painter.LineTo(P(0.6f, 0.55f));
                    painter.LineTo(P(0.4f, 0.55f));
                    painter.ClosePath();
                    painter.Fill();
                    Line(painter, P(0.5f, 0.55f), P(0.5f, 0.68f));
                    painter.BeginPath();
                    painter.MoveTo(P(0.34f, 0.68f));
                    painter.LineTo(P(0.66f, 0.68f));
                    painter.LineTo(P(0.66f, 0.78f));
                    painter.LineTo(P(0.34f, 0.78f));
                    painter.ClosePath();
                    painter.Fill();
                    // anses
                    painter.BeginPath();
                    painter.Arc(P(0.28f, 0.3f), s * 0.09f, 90, 270);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.Arc(P(0.72f, 0.3f), s * 0.09f, -90, 90);
                    painter.Stroke();
                    break;

                case TacticalIconType.Doc:
                    StrokeRect(painter, P(0.3f, 0.22f), P(0.7f, 0.78f));
                    Line(painter, P(0.38f, 0.38f), P(0.62f, 0.38f));
                    Line(painter, P(0.38f, 0.5f), P(0.62f, 0.5f));
                    Line(painter, P(0.38f, 0.62f), P(0.55f, 0.62f));
                    break;

                case TacticalIconType.Cube:
                    painter.BeginPath();
                    painter.MoveTo(P(0.5f, 0.2f));
                    painter.LineTo(P(0.78f, 0.36f));
                    painter.LineTo(P(0.78f, 0.64f));
                    painter.LineTo(P(0.5f, 0.8f));
                    painter.LineTo(P(0.22f, 0.64f));
                    painter.LineTo(P(0.22f, 0.36f));
                    painter.ClosePath();
                    painter.Stroke();
                    Line(painter, P(0.22f, 0.36f), P(0.5f, 0.52f));
                    Line(painter, P(0.78f, 0.36f), P(0.5f, 0.52f));
                    Line(painter, P(0.5f, 0.52f), P(0.5f, 0.8f));
                    break;

                case TacticalIconType.Play:
                    painter.BeginPath();
                    painter.MoveTo(P(0.38f, 0.28f));
                    painter.LineTo(P(0.74f, 0.5f));
                    painter.LineTo(P(0.38f, 0.72f));
                    painter.ClosePath();
                    painter.Fill();
                    break;

                case TacticalIconType.Target:
                    painter.BeginPath();
                    painter.Arc(P(0.5f, 0.5f), s * 0.24f, 0, 360);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.Arc(P(0.5f, 0.5f), s * 0.05f, 0, 360);
                    painter.Fill();
                    Line(painter, P(0.5f, 0.14f), P(0.5f, 0.28f));
                    Line(painter, P(0.5f, 0.72f), P(0.5f, 0.86f));
                    Line(painter, P(0.14f, 0.5f), P(0.28f, 0.5f));
                    Line(painter, P(0.72f, 0.5f), P(0.86f, 0.5f));
                    break;

                case TacticalIconType.Gun:
                    painter.BeginPath();
                    painter.MoveTo(P(0.2f, 0.38f));
                    painter.LineTo(P(0.8f, 0.38f));
                    painter.LineTo(P(0.8f, 0.52f));
                    painter.LineTo(P(0.52f, 0.52f));
                    painter.LineTo(P(0.46f, 0.76f));
                    painter.LineTo(P(0.3f, 0.76f));
                    painter.LineTo(P(0.38f, 0.52f));
                    painter.LineTo(P(0.2f, 0.52f));
                    painter.ClosePath();
                    painter.Fill();
                    break;

                case TacticalIconType.Ring:
                    painter.lineWidth = Mathf.Max(2f, s * 0.11f);
                    painter.BeginPath();
                    painter.Arc(P(0.5f, 0.5f), s * 0.22f, 0, 360);
                    painter.Stroke();
                    break;

                case TacticalIconType.Runner:
                    painter.BeginPath();
                    painter.Arc(P(0.62f, 0.26f), s * 0.08f, 0, 360);
                    painter.Fill();
                    Line(painter, P(0.58f, 0.38f), P(0.46f, 0.58f)); // buste
                    Line(painter, P(0.46f, 0.58f), P(0.62f, 0.74f)); // jambe avant
                    Line(painter, P(0.46f, 0.58f), P(0.32f, 0.68f)); // jambe arrière
                    Line(painter, P(0.56f, 0.44f), P(0.72f, 0.52f)); // bras avant
                    Line(painter, P(0.56f, 0.44f), P(0.38f, 0.38f)); // bras arrière
                    break;

                case TacticalIconType.Map:
                    StrokeRect(painter, P(0.24f, 0.28f), P(0.76f, 0.72f));
                    Line(painter, P(0.415f, 0.28f), P(0.415f, 0.72f));
                    Line(painter, P(0.585f, 0.28f), P(0.585f, 0.72f));
                    break;

                case TacticalIconType.Bars:
                    FillRect(painter, P(0.24f, 0.62f), P(0.34f, 0.78f));
                    FillRect(painter, P(0.4f, 0.5f), P(0.5f, 0.78f));
                    FillRect(painter, P(0.56f, 0.38f), P(0.66f, 0.78f));
                    FillRect(painter, P(0.72f, 0.26f), P(0.82f, 0.78f));
                    break;

                case TacticalIconType.Lock:
                    painter.BeginPath();
                    painter.Arc(P(0.5f, 0.42f), s * 0.15f, 180, 360);
                    painter.Stroke();
                    painter.BeginPath();
                    painter.MoveTo(P(0.3f, 0.44f));
                    painter.LineTo(P(0.7f, 0.44f));
                    painter.LineTo(P(0.7f, 0.76f));
                    painter.LineTo(P(0.3f, 0.76f));
                    painter.ClosePath();
                    painter.Fill();
                    break;

                case TacticalIconType.Blade:
                    painter.BeginPath();
                    painter.MoveTo(P(0.66f, 0.2f));
                    painter.LineTo(P(0.74f, 0.28f));
                    painter.LineTo(P(0.42f, 0.6f));
                    painter.LineTo(P(0.34f, 0.52f));
                    painter.ClosePath();
                    painter.Fill();
                    Line(painter, P(0.28f, 0.66f), P(0.42f, 0.52f));
                    Line(painter, P(0.24f, 0.62f), P(0.32f, 0.78f));
                    break;

                case TacticalIconType.Sun:
                    painter.BeginPath();
                    painter.Arc(P(0.5f, 0.5f), s * 0.13f, 0, 360);
                    painter.Fill();
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Line(painter, P(0.5f, 0.5f) + dir * (s * 0.21f), P(0.5f, 0.5f) + dir * (s * 0.32f));
                    }
                    break;
            }
        }

        static void Line(Painter2D painter, Vector2 a, Vector2 b)
        {
            painter.BeginPath();
            painter.MoveTo(a);
            painter.LineTo(b);
            painter.Stroke();
        }

        static void StrokeRect(Painter2D painter, Vector2 min, Vector2 max)
        {
            painter.BeginPath();
            painter.MoveTo(min);
            painter.LineTo(new Vector2(max.x, min.y));
            painter.LineTo(max);
            painter.LineTo(new Vector2(min.x, max.y));
            painter.ClosePath();
            painter.Stroke();
        }

        static void FillRect(Painter2D painter, Vector2 min, Vector2 max)
        {
            painter.BeginPath();
            painter.MoveTo(min);
            painter.LineTo(new Vector2(max.x, min.y));
            painter.LineTo(max);
            painter.LineTo(new Vector2(min.x, max.y));
            painter.ClosePath();
            painter.Fill();
        }
    }
}
