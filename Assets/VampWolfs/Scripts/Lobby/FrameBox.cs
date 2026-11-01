using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Painel com cantos chanfrados e borda fina dourada (visual da tela inicial, out/2026), desenhado por código
    /// com Painter2D — sem textura, nítido em qualquer resolução. Os filhos ficam por cima normalmente.
    ///
    /// Cores e tamanhos vêm do USS por propriedades próprias (todas opcionais):
    ///   --frame-fill: cor do fundo · --frame-border: cor da borda · --frame-glow: brilho fraco por fora da borda
    ///   --frame-accent: cantos destacados · --frame-cut: tamanho do chanfro (px) · --frame-border-width (px)
    /// Os chanfros ficam no canto de cima à esquerda e embaixo à direita (como na referência).
    /// </summary>
    public class FrameBox : VisualElement
    {
        static readonly CustomStyleProperty<Color> PFill = new CustomStyleProperty<Color>("--frame-fill");
        static readonly CustomStyleProperty<Color> PBorder = new CustomStyleProperty<Color>("--frame-border");
        static readonly CustomStyleProperty<Color> PGlow = new CustomStyleProperty<Color>("--frame-glow");
        static readonly CustomStyleProperty<Color> PAccent = new CustomStyleProperty<Color>("--frame-accent");
        static readonly CustomStyleProperty<float> PCut = new CustomStyleProperty<float>("--frame-cut");
        static readonly CustomStyleProperty<float> PWidth = new CustomStyleProperty<float>("--frame-border-width");

        static readonly Color DefFill = new Color(0.1f, 0.05f, 0.045f, 0.88f), DefBorder = new Color(0.85f, 0.64f, 0.27f, 0.55f),   // vinho + latão (VampWolfs)
              DefGlow = new Color(0f, 0f, 0f, 0f), DefAccent = new Color(1f, 0.74f, 0.32f, 0.95f);
        Color fill = DefFill, border = DefBorder, glow = DefGlow, accent = DefAccent;
        float cut = 12f, width = 1f;

        Color? borderOverride, accentOverride;
        /// <summary>Cor da borda e dos cantos definida por código (ex.: cor de cada árvore); null = a do USS.</summary>
        public void SetColors(Color? border, Color? accentColor) { borderOverride = border; accentOverride = accentColor; MarkDirtyRepaint(); }

        public FrameBox(string classes = null)
        {
            AddToClassList("frame");
            UiKit.AddClasses(this, classes);
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnStyle);
        }

        void OnStyle(CustomStyleResolvedEvent e)
        {
            var s = e.customStyle; Color c; float f;
            fill = DefFill; border = DefBorder; glow = DefGlow; accent = DefAccent; cut = 12f; width = 1f;   // o que o estilo não define volta ao padrão
            if (s.TryGetValue(PFill, out c)) fill = c;
            if (s.TryGetValue(PBorder, out c)) border = c;
            if (s.TryGetValue(PGlow, out c)) glow = c;
            if (s.TryGetValue(PAccent, out c)) accent = c;
            if (s.TryGetValue(PCut, out f)) cut = f;
            if (s.TryGetValue(PWidth, out f)) width = f;
            MarkDirtyRepaint();
        }

        /// <summary>Contorno chanfrado (cantos de cima à esquerda e de baixo à direita) recuado "inset" px.</summary>
        static void Outline(Painter2D p, Rect r, float c, float inset)
        {
            float x0 = r.xMin + inset, y0 = r.yMin + inset, x1 = r.xMax - inset, y1 = r.yMax - inset;
            c = Mathf.Max(0f, Mathf.Min(c, Mathf.Min(x1 - x0, y1 - y0) * 0.5f));
            p.BeginPath();
            p.MoveTo(new Vector2(x0 + c, y0));
            p.LineTo(new Vector2(x1, y0));
            p.LineTo(new Vector2(x1, y1 - c));
            p.LineTo(new Vector2(x1 - c, y1));
            p.LineTo(new Vector2(x0, y1));
            p.LineTo(new Vector2(x0, y0 + c));
            p.ClosePath();
        }

        void Draw(MeshGenerationContext ctx)
        {
            var r = new Rect(0, 0, layout.width, layout.height);
            if (borderOverride.HasValue) border = borderOverride.Value;
            if (accentOverride.HasValue) accent = accentOverride.Value;
            if (r.width < 2 || r.height < 2) return;
            var p = ctx.painter2D;
            p.lineJoin = LineJoin.Miter;
            // fundo
            if (fill.a > 0.001f) { p.fillColor = fill; Outline(p, r, cut, 0f); p.Fill(); }
            // brilho por fora (dois traços largos e fracos imitam um "glow")
            if (glow.a > 0.001f)
            {
                var g = glow; p.strokeColor = g; p.lineWidth = 5f; Outline(p, r, cut, -1.5f); p.Stroke();
                g.a *= 0.5f; p.strokeColor = g; p.lineWidth = 9f; Outline(p, r, cut, -3f); p.Stroke();
            }
            // borda
            if (border.a > 0.001f && width > 0f) { p.strokeColor = border; p.lineWidth = width; Outline(p, r, cut, width * 0.5f); p.Stroke(); }
            // cantos destacados: pequenos "colchetes" mais claros nos dois chanfros
            if (accent.a > 0.001f)
            {
                float w = Mathf.Max(1.5f, width + 0.5f), h = w * 0.5f, L = Mathf.Min(26f, Mathf.Min(r.width, r.height) * 0.25f), c = cut;
                p.strokeColor = accent; p.lineWidth = w; p.lineCap = LineCap.Butt;
                p.BeginPath(); p.MoveTo(new Vector2(h, c + L)); p.LineTo(new Vector2(h, c)); p.LineTo(new Vector2(c, h)); p.LineTo(new Vector2(c + L, h)); p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(r.width - h, r.height - c - L)); p.LineTo(new Vector2(r.width - h, r.height - c)); p.LineTo(new Vector2(r.width - c, r.height - h)); p.LineTo(new Vector2(r.width - c - L, r.height - h)); p.Stroke();
            }
        }
    }
}
