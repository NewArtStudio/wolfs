using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Elemento do UI Toolkit que desenha um ícone do protótipo com Painter2D (vetorial, sem textura).
    /// A cor vem de <see cref="Color"/> (equivale ao "currentColor" do SVG).
    /// Equipamentos com ícone pintado (ItemArt) mostram a imagem no lugar do desenho (out/2026).
    /// </summary>
    public sealed class SvgIcon : VisualElement
    {
        string icon;
        Color color = Color.white;
        Texture2D raster;   // ícone pintado do equipamento (null = desenho vetorial)

        public SvgIcon(string iconName = null)
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("icon");
            generateVisualContent += Draw;
            Icon = iconName;
        }

        public string Icon
        {
            get { return icon; }
            set { if (icon == value) return; icon = value; ApplyRaster(); MarkDirtyRepaint(); }
        }
        public Color Color
        {
            get { return color; }
            set { if (color == value) return; color = value; MarkDirtyRepaint(); }
        }

        void ApplyRaster()
        {
            raster = ItemArt.ForIcon(icon);
            if (raster != null)
            {
                style.backgroundImage = new StyleBackground(raster);
                style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }
            else style.backgroundImage = StyleKeyword.Null;
        }

        void Draw(MeshGenerationContext ctx)
        {
            if (raster != null) return;   // a imagem é o fundo do elemento
            IconDef def;
            if (!Icons.TryGet(icon, out def)) return;
            var r = contentRect; if (r.width < 1 || r.height < 1) return;
            float vb = def.ViewBox > 0 ? def.ViewBox : 24f;
            float s = Mathf.Min(r.width, r.height) / vb;
            float ox = r.x + (r.width - vb * s) / 2f, oy = r.y + (r.height - vb * s) / 2f;
            var p = ctx.painter2D;
            p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            foreach (var shape in SvgShapes.Parse(def.Markup, def.Colored))
            {
                p.lineWidth = Mathf.Max(def.Colored ? 0.6f : 1f, (shape.StrokeWidth > 0 ? shape.StrokeWidth : def.StrokeWidth) * s);
                p.fillColor = shape.FillCurrent ? color : ToColor(shape.FillColor);
                p.strokeColor = shape.StrokeCurrent ? color : ToColor(shape.StrokeColor);
                p.BeginPath();
                foreach (var poly in shape.Polys)
                {
                    if (poly.Points.Count < 2) continue;
                    var a = poly.Points[0]; p.MoveTo(new Vector2(ox + a.X * s, oy + a.Y * s));
                    for (int i = 1; i < poly.Points.Count; i++) { var b = poly.Points[i]; p.LineTo(new Vector2(ox + b.X * s, oy + b.Y * s)); }
                    if (poly.Closed) p.ClosePath();
                }
                if (shape.Fill) p.Fill();
                if (shape.Stroke) p.Stroke();
            }
        }

        static Color ToColor(SvgShapes.Rgba c) { return new Color(c.R, c.G, c.B, c.A); }
    }
}
