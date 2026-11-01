using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Leitor do subconjunto de SVG usado nos ícones do protótipo (path, circle, rect, line).
    /// Converte tudo em polilinhas no espaço 0–24 (viewBox). C# puro: testado fora do Unity.
    /// Comandos de path: M L H V C S Q T A Z (maiúsculos e minúsculos).
    /// </summary>
    public static class SvgShapes
    {
        public struct Pt { public float X, Y; public Pt(float x, float y) { X = x; Y = y; } }
        /// <summary>Cor RGBA 0–1 (C# puro; o Unity converte para Color).</summary>
        public struct Rgba { public float R, G, B, A; public Rgba(float r, float g, float b, float a) { R = r; G = g; B = b; A = a; } }
        public sealed class Poly { public List<Pt> Points = new List<Pt>(); public bool Closed; }
        /// <summary>
        /// Forma desenhável. Modo mono (ícones de traço): cor = "currentColor" do elemento.
        /// Modo colorido (ícones de equipamento): cores próprias do SVG.
        /// </summary>
        public sealed class Shape
        {
            public List<Poly> Polys = new List<Poly>();
            public bool Fill, Stroke;
            /// <summary>true = usa a cor do ícone (currentColor) em vez de FillColor/StrokeColor.</summary>
            public bool FillCurrent, StrokeCurrent;
            public Rgba FillColor, StrokeColor;
            /// <summary>Espessura do traço em unidades do viewBox (0 = padrão do ícone).</summary>
            public float StrokeWidth;
        }

        static readonly Dictionary<string, List<Shape>> cache = new Dictionary<string, List<Shape>>();
        static readonly Regex ElementRx = new Regex(@"<(path|circle|rect|line|ellipse)\b([^>]*?)/?>", RegexOptions.Compiled);
        static readonly Regex AttrRx = new Regex(@"([a-zA-Z\-]+)\s*=\s*""([^""]*)""", RegexOptions.Compiled);
        static readonly Regex TokenRx = new Regex(@"[MmLlHhVvCcSsQqTtAaZz]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?", RegexOptions.Compiled);
        static readonly Regex GradRx = new Regex(@"<linearGradient\b[^>]*id=""([^""]+)""[^>]*>(.*?)</linearGradient>", RegexOptions.Compiled | RegexOptions.Singleline);
        static readonly Regex StopRx = new Regex(@"stop-color=""([^""]+)""", RegexOptions.Compiled);
        static readonly Regex GroupRx = new Regex(@"<g\b([^>]*)>(.*?)</g>", RegexOptions.Compiled | RegexOptions.Singleline);
        static readonly Regex RotateRx = new Regex(@"rotate\(\s*([-\d.]+)(?:[\s,]+([-\d.]+)[\s,]+([-\d.]+))?\s*\)", RegexOptions.Compiled);

        /// <summary>Lê o SVG. colorMode = semântica SVG normal (fill preto por padrão, cores próprias).</summary>
        public static List<Shape> Parse(string markup, bool colorMode = false)
        {
            string key = (colorMode ? "C|" : "M|") + markup;
            List<Shape> r; if (cache.TryGetValue(key, out r)) return r;
            r = new List<Shape>();
            var grads = new Dictionary<string, Rgba>();
            if (colorMode)
            {
                foreach (Match g in GradRx.Matches(markup))
                {
                    var stops = StopRx.Matches(g.Groups[2].Value);
                    if (stops.Count == 0) continue;
                    var a = Color(stops[0].Groups[1].Value); var b = Color(stops[stops.Count - 1].Groups[1].Value);
                    grads[g.Groups[1].Value] = new Rgba(a.R + (b.R - a.R) * 0.45f, a.G + (b.G - a.G) * 0.45f, a.B + (b.B - a.B) * 0.45f, 1);
                }
                markup = GradRx.Replace(markup, "");
                // <g atributos>filhos</g> → atributos herdados pelos filhos
                markup = GroupRx.Replace(markup, m => ElementRx.Replace(m.Groups[2].Value, e => "<" + e.Groups[1].Value + " " + m.Groups[1].Value + " " + e.Groups[2].Value + "/>"));
            }
            foreach (Match m in ElementRx.Matches(markup))
            {
                var attrs = new Dictionary<string, string>();
                foreach (Match a in AttrRx.Matches(m.Groups[2].Value)) attrs[a.Groups[1].Value] = a.Groups[2].Value; // o último vence (filho sobre grupo)
                var shape = new Shape();
                string fill, stroke; attrs.TryGetValue("fill", out fill); attrs.TryGetValue("stroke", out stroke);
                if (!colorMode)
                {
                    shape.Fill = shape.FillCurrent = fill == "currentColor";
                    shape.Stroke = shape.StrokeCurrent = !shape.Fill;
                }
                else
                {
                    float op = attrs.ContainsKey("opacity") ? F(attrs, "opacity") : 1f;
                    if (fill == null) { shape.Fill = true; shape.FillColor = new Rgba(0, 0, 0, op); }
                    else if (fill == "none") shape.Fill = false;
                    else if (fill == "currentColor") { shape.Fill = shape.FillCurrent = true; }
                    else if (fill.StartsWith("url(#"))
                    {
                        Rgba g; shape.Fill = grads.TryGetValue(fill.Substring(5, fill.Length - 6), out g);
                        g.A = op; shape.FillColor = g;
                    }
                    else { shape.Fill = true; var c = Color(fill); c.A *= op; shape.FillColor = c; }
                    if (stroke != null && stroke != "none")
                    {
                        shape.Stroke = true; shape.StrokeCurrent = stroke == "currentColor";
                        if (!shape.StrokeCurrent) { var c = Color(stroke); c.A *= op; shape.StrokeColor = c; }
                        shape.StrokeWidth = attrs.ContainsKey("stroke-width") ? F(attrs, "stroke-width") : 1f;
                    }
                }
                switch (m.Groups[1].Value)
                {
                    case "path": string d; if (attrs.TryGetValue("d", out d)) PathTo(d, shape.Polys); break;
                    case "circle": shape.Polys.Add(Ellipse(F(attrs, "cx"), F(attrs, "cy"), F(attrs, "r"), F(attrs, "r"))); break;
                    case "ellipse": shape.Polys.Add(Ellipse(F(attrs, "cx"), F(attrs, "cy"), F(attrs, "rx"), F(attrs, "ry"))); break;
                    case "rect": shape.Polys.Add(Rect(F(attrs, "x"), F(attrs, "y"), F(attrs, "width"), F(attrs, "height"), F(attrs, "rx"))); break;
                    case "line":
                        var p = new Poly(); p.Points.Add(new Pt(F(attrs, "x1"), F(attrs, "y1"))); p.Points.Add(new Pt(F(attrs, "x2"), F(attrs, "y2"))); shape.Polys.Add(p); break;
                }
                string tr;
                if (attrs.TryGetValue("transform", out tr)) { var rm = RotateRx.Match(tr); if (rm.Success) Rotate(shape, rm); }
                if (colorMode && shape.Fill) { bool area = false; foreach (var q in shape.Polys) if (q.Points.Count >= 3) area = true; if (!area) shape.Fill = false; } // linhas soltas não têm área
                if (shape.Polys.Count > 0 && (shape.Fill || shape.Stroke)) r.Add(shape);
            }
            cache[key] = r;
            return r;
        }

        static void Rotate(Shape s, Match m)
        {
            double a = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) * Math.PI / 180;
            float cx = m.Groups[2].Success ? float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 0f;
            float cy = m.Groups[3].Success ? float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) : 0f;
            float cs = (float)Math.Cos(a), sn = (float)Math.Sin(a);
            foreach (var p in s.Polys)
                for (int i = 0; i < p.Points.Count; i++)
                {
                    float x = p.Points[i].X - cx, y = p.Points[i].Y - cy;
                    p.Points[i] = new Pt(cx + x * cs - y * sn, cy + x * sn + y * cs);
                }
        }

        /// <summary>#rgb ou #rrggbb → RGBA.</summary>
        public static Rgba Color(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex[0] != '#') return new Rgba(0, 0, 0, 1);
            string h = hex.Substring(1);
            if (h.Length == 3) h = new string(new[] { h[0], h[0], h[1], h[1], h[2], h[2] });
            int v; if (h.Length != 6 || !int.TryParse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return new Rgba(0, 0, 0, 1);
            return new Rgba(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, 1);
        }

        static float F(Dictionary<string, string> a, string k)
        {
            string v; float f;
            return a.TryGetValue(k, out v) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f) ? f : 0f;
        }

        static Poly Ellipse(float cx, float cy, float rx, float ry)
        {
            var p = new Poly { Closed = true }; int n = Math.Max(12, (int)(Math.Max(rx, ry) * 6));
            for (int i = 0; i < n; i++) { double a = i * Math.PI * 2 / n; p.Points.Add(new Pt(cx + (float)Math.Cos(a) * rx, cy + (float)Math.Sin(a) * ry)); }
            return p;
        }

        static Poly Rect(float x, float y, float w, float h, float rx)
        {
            var p = new Poly { Closed = true };
            rx = Math.Min(rx, Math.Min(w, h) / 2);
            if (rx <= 0) { p.Points.Add(new Pt(x, y)); p.Points.Add(new Pt(x + w, y)); p.Points.Add(new Pt(x + w, y + h)); p.Points.Add(new Pt(x, y + h)); return p; }
            Corner(p, x + w - rx, y + rx, rx, -90); Corner(p, x + w - rx, y + h - rx, rx, 0); Corner(p, x + rx, y + h - rx, rx, 90); Corner(p, x + rx, y + rx, rx, 180);
            return p;
        }
        static void Corner(Poly p, float cx, float cy, float r, float startDeg)
        {
            for (int i = 0; i <= 4; i++) { double a = (startDeg + i * 22.5) * Math.PI / 180; p.Points.Add(new Pt(cx + (float)Math.Cos(a) * r, cy + (float)Math.Sin(a) * r)); }
        }

        /// <summary>Converte o atributo d de um path em polilinhas.</summary>
        public static void PathTo(string d, List<Poly> outPolys)
        {
            var toks = new List<string>(); foreach (Match t in TokenRx.Matches(d)) toks.Add(t.Value);
            int i = 0; char cmd = 'M'; float x = 0, y = 0, sx = 0, sy = 0; float lcx = 0, lcy = 0; char prev = ' ';
            Poly cur = null;
            Func<bool> num = () => i < toks.Count && !char.IsLetter(toks[i][0]);
            Func<float> N = () => i < toks.Count ? float.Parse(toks[i++], NumberStyles.Float, CultureInfo.InvariantCulture) : 0f * i++;
            Action<float, float> line = (nx, ny) => { if (cur == null) { cur = new Poly(); cur.Points.Add(new Pt(x, y)); outPolys.Add(cur); } cur.Points.Add(new Pt(nx, ny)); x = nx; y = ny; };
            while (i < toks.Count)
            {
                if (char.IsLetter(toks[i][0])) cmd = toks[i++][0];
                bool rel = char.IsLower(cmd);
                switch (char.ToUpperInvariant(cmd))
                {
                    case 'M':
                        {
                            float nx = N(), ny = N(); if (rel) { nx += x; ny += y; }
                            cur = new Poly(); cur.Points.Add(new Pt(nx, ny)); outPolys.Add(cur);
                            x = sx = nx; y = sy = ny;
                            cmd = rel ? 'l' : 'L'; // pares seguintes são LineTo
                            break;
                        }
                    case 'L': { float nx = N(), ny = N(); if (rel) { nx += x; ny += y; } line(nx, ny); break; }
                    case 'H': { float nx = N(); if (rel) nx += x; line(nx, y); break; }
                    case 'V': { float ny = N(); if (rel) ny += y; line(x, ny); break; }
                    case 'C':
                        {
                            float x1 = N(), y1 = N(), x2 = N(), y2 = N(), ex = N(), ey = N();
                            if (rel) { x1 += x; y1 += y; x2 += x; y2 += y; ex += x; ey += y; }
                            Cubic(x, y, x1, y1, x2, y2, ex, ey, line); lcx = x2; lcy = y2; break;
                        }
                    case 'S':
                        {
                            float x2 = N(), y2 = N(), ex = N(), ey = N();
                            if (rel) { x2 += x; y2 += y; ex += x; ey += y; }
                            char pu = char.ToUpperInvariant(prev);
                            float x1 = pu == 'C' || pu == 'S' ? 2 * x - lcx : x, y1 = pu == 'C' || pu == 'S' ? 2 * y - lcy : y;
                            Cubic(x, y, x1, y1, x2, y2, ex, ey, line); lcx = x2; lcy = y2; break;
                        }
                    case 'Q':
                        {
                            float x1 = N(), y1 = N(), ex = N(), ey = N();
                            if (rel) { x1 += x; y1 += y; ex += x; ey += y; }
                            Quad(x, y, x1, y1, ex, ey, line); lcx = x1; lcy = y1; break;
                        }
                    case 'T':
                        {
                            float ex = N(), ey = N(); if (rel) { ex += x; ey += y; }
                            char pu = char.ToUpperInvariant(prev);
                            float x1 = pu == 'Q' || pu == 'T' ? 2 * x - lcx : x, y1 = pu == 'Q' || pu == 'T' ? 2 * y - lcy : y;
                            Quad(x, y, x1, y1, ex, ey, line); lcx = x1; lcy = y1; break;
                        }
                    case 'A':
                        {
                            float rx = N(), ry = N(), rot = N(); bool large = N() != 0, sweep = N() != 0; float ex = N(), ey = N();
                            if (rel) { ex += x; ey += y; }
                            Arc(x, y, rx, ry, rot, large, sweep, ex, ey, line); break;
                        }
                    case 'Z':
                        if (cur != null) cur.Closed = true;
                        x = sx; y = sy; cur = null;
                        break;
                    default: i++; break;
                }
                prev = cmd;
                // números soltos depois de Z: tratados como LineTo (tolerância)
                if (char.ToUpperInvariant(cmd) == 'Z' && num()) cmd = 'L';
            }
        }

        static void Cubic(float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, Action<float, float> line)
        {
            const int n = 12;
            for (int k = 1; k <= n; k++)
            {
                float t = k / (float)n, u = 1 - t;
                line(u * u * u * x0 + 3 * u * u * t * x1 + 3 * u * t * t * x2 + t * t * t * x3, u * u * u * y0 + 3 * u * u * t * y1 + 3 * u * t * t * y2 + t * t * t * y3);
            }
        }
        static void Quad(float x0, float y0, float x1, float y1, float x2, float y2, Action<float, float> line)
        {
            const int n = 10;
            for (int k = 1; k <= n; k++) { float t = k / (float)n, u = 1 - t; line(u * u * x0 + 2 * u * t * x1 + t * t * x2, u * u * y0 + 2 * u * t * y1 + t * t * y2); }
        }

        /// <summary>Arco elíptico do SVG (parametrização por pontos finais → centro), em segmentos.</summary>
        static void Arc(float x1, float y1, float rx, float ry, float rotDeg, bool large, bool sweep, float x2, float y2, Action<float, float> line)
        {
            if (rx == 0 || ry == 0 || (x1 == x2 && y1 == y2)) { line(x2, y2); return; }
            double phi = rotDeg * Math.PI / 180, cs = Math.Cos(phi), sn = Math.Sin(phi);
            double dx = (x1 - x2) / 2.0, dy = (y1 - y2) / 2.0;
            double x1p = cs * dx + sn * dy, y1p = -sn * dx + cs * dy;
            double arx = Math.Abs(rx), ary = Math.Abs(ry);
            double lam = x1p * x1p / (arx * arx) + y1p * y1p / (ary * ary);
            if (lam > 1) { double s = Math.Sqrt(lam); arx *= s; ary *= s; }
            double num = arx * arx * ary * ary - arx * arx * y1p * y1p - ary * ary * x1p * x1p;
            double den = arx * arx * y1p * y1p + ary * ary * x1p * x1p;
            double coef = den == 0 ? 0 : Math.Sqrt(Math.Max(0, num / den)); if (large == sweep) coef = -coef;
            double cxp = coef * arx * y1p / ary, cyp = -coef * ary * x1p / arx;
            double cx = cs * cxp - sn * cyp + (x1 + x2) / 2.0, cy = sn * cxp + cs * cyp + (y1 + y2) / 2.0;
            double th1 = Math.Atan2((y1p - cyp) / ary, (x1p - cxp) / arx);
            double th2 = Math.Atan2((-y1p - cyp) / ary, (-x1p - cxp) / arx);
            double dth = th2 - th1;
            if (sweep && dth < 0) dth += 2 * Math.PI; else if (!sweep && dth > 0) dth -= 2 * Math.PI;
            int n = Math.Max(4, (int)Math.Ceiling(Math.Abs(dth) / (Math.PI / 12)));
            for (int k = 1; k <= n; k++)
            {
                double t = th1 + dth * k / n;
                double px = arx * Math.Cos(t), py = ary * Math.Sin(t);
                if (k == n) line(x2, y2); else line((float)(cs * px - sn * py + cx), (float)(sn * px + cs * py + cy));
            }
        }
    }
}
