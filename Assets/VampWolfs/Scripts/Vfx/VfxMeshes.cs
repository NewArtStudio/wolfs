using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// Malhas simples dos efeitos, geradas por código (sem assets): quadrados, esfera, espinho de gelo,
    /// pedra/estilhaço, mastro. Todas têm cor de vértice branca (os shaders de VFX multiplicam por ela).
    /// </summary>
    public static class VfxMeshes
    {
        static Mesh quadXZ, quadXY, beam, sphere, spike, pole, flag;
        static Mesh[] chunks;

        static Mesh Build(string name, List<Vector3> v, List<Vector3> n, List<Vector2> uv, List<int> tri)
        {
            var m = new Mesh { name = name };
            var col = new List<Color>(v.Count); for (int i = 0; i < v.Count; i++) col.Add(Color.white);
            m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Quadrado deitado (plano XZ, 1×1, centrado, normal para cima). Frente = +Z = V alto.</summary>
        public static Mesh QuadXZ
        {
            get
            {
                if (quadXZ != null) return quadXZ;
                var v = new List<Vector3> { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) };
                var n = new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                return quadXZ = Build("VFX QuadXZ", v, n, uv, new List<int> { 0, 2, 1, 1, 2, 3 });
            }
        }

        /// <summary>Quadrado em pé (plano XY, 1×1, centrado) — billboards virados para a câmera.</summary>
        public static Mesh QuadXY
        {
            get
            {
                if (quadXY != null) return quadXY;
                var v = new List<Vector3> { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0) };
                var n = new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                return quadXY = Build("VFX QuadXY", v, n, uv, new List<int> { 0, 2, 1, 1, 2, 3 });
            }
        }

        /// <summary>Faixa de feixe: X de 0 a 1 (comprimento), Y de −0.5 a 0.5 (largura). U ao longo, V através.</summary>
        public static Mesh Beam
        {
            get
            {
                if (beam != null) return beam;
                var v = new List<Vector3> { new Vector3(0, -0.5f, 0), new Vector3(1, -0.5f, 0), new Vector3(0, 0.5f, 0), new Vector3(1, 0.5f, 0) };
                var n = new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                return beam = Build("VFX Beam", v, n, uv, new List<int> { 0, 2, 1, 1, 2, 3 });
            }
        }

        /// <summary>Bandeira: X de 0 a 1 (sai do mastro), Y de 0 a 1.</summary>
        public static Mesh Flag
        {
            get
            {
                if (flag != null) return flag;
                var v = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) };
                var n = new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
                var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                return flag = Build("VFX Flag", v, n, uv, new List<int> { 0, 2, 1, 1, 2, 3 });
            }
        }

        /// <summary>Esfera UV de raio 0.5 (escudos, núcleo de energia).</summary>
        public static Mesh Sphere
        {
            get
            {
                if (sphere != null) return sphere;
                const int lon = 32, lat = 18;
                var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
                for (int j = 0; j <= lat; j++)
                {
                    float th = Mathf.PI * j / lat;
                    for (int i = 0; i <= lon; i++)
                    {
                        float ph = 2 * Mathf.PI * i / lon;
                        var d = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                        v.Add(d * 0.5f); n.Add(d); uv.Add(new Vector2((float)i / lon * 3f, 1f - (float)j / lat));
                    }
                }
                for (int j = 0; j < lat; j++)
                    for (int i = 0; i < lon; i++)
                    {
                        int a = j * (lon + 1) + i, b = a + lon + 1;
                        tri.Add(a); tri.Add(a + 1); tri.Add(b); tri.Add(a + 1); tri.Add(b + 1); tri.Add(b);
                    }
                return sphere = Build("VFX Sphere", v, n, uv, tri);
            }
        }

        /// <summary>Faces chapadas: cada triângulo ganha seus próprios vértices (cristais e pedras facetados).</summary>
        static Mesh Flat(string name, List<Vector3> pts, List<int> tri, float uvScale)
        {
            var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i < tri.Count; i += 3)
            {
                Vector3 a = pts[tri[i]], b = pts[tri[i + 1]], c = pts[tri[i + 2]];
                var fn = Vector3.Cross(b - a, c - a).normalized;
                foreach (var p in new[] { a, b, c })
                {
                    t.Add(v.Count); v.Add(p); n.Add(fn);
                    uv.Add(new Vector2((Mathf.Atan2(p.z, p.x) / (2 * Mathf.PI) + 0.5f) * uvScale, p.y * uvScale));
                }
            }
            return Build(name, v, n, uv, t);
        }

        /// <summary>Espinho de gelo: prisma hexagonal irregular com ponta. Base em y = 0, ponta em y = 1, raio ~0.5.</summary>
        public static Mesh IceSpike
        {
            get
            {
                if (spike != null) return spike;
                var rnd = new System.Random(5); const int k = 6;
                var pts = new List<Vector3>(); var tri = new List<int>();
                for (int i = 0; i < k; i++)
                {
                    float a = i * Mathf.PI * 2 / k; float r = 0.42f + (float)rnd.NextDouble() * 0.12f;
                    pts.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
                    pts.Add(new Vector3(Mathf.Cos(a) * r * 0.85f, 0.62f + (float)rnd.NextDouble() * 0.1f, Mathf.Sin(a) * r * 0.85f));
                }
                int tip = pts.Count; pts.Add(new Vector3(0.05f, 1f, -0.03f));
                for (int i = 0; i < k; i++)
                {
                    int b0 = i * 2, t0 = i * 2 + 1, b1 = ((i + 1) % k) * 2, t1 = b1 + 1;
                    tri.Add(b0); tri.Add(t0); tri.Add(b1); tri.Add(b1); tri.Add(t0); tri.Add(t1);
                    tri.Add(t0); tri.Add(tip); tri.Add(t1);
                }
                return spike = Flat("VFX IceSpike", pts, tri, 1f);
            }
        }

        /// <summary>Pedaços de pedra/estilhaço (octaedro subdividido e amassado), raio ~0.5. Vários formatos.</summary>
        public static Mesh[] Chunks
        {
            get
            {
                if (chunks != null) return chunks;
                chunks = new Mesh[3];
                for (int s = 0; s < chunks.Length; s++) chunks[s] = Chunk(11 + s * 7);
                return chunks;
            }
        }

        static Mesh Chunk(int seed)
        {
            var rnd = new System.Random(seed);
            var pts = new List<Vector3> { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            var tri = new List<int> { 0, 2, 4, 4, 2, 1, 1, 2, 5, 5, 2, 0, 4, 3, 0, 1, 3, 4, 5, 3, 1, 0, 3, 5 };
            // uma subdivisão: ponto médio de cada aresta
            var mid = new Dictionary<long, int>(); var t2 = new List<int>();
            System.Func<int, int, int> M = (a, b) =>
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a; int idx;
                if (mid.TryGetValue(key, out idx)) return idx;
                idx = pts.Count; pts.Add((pts[a] + pts[b]).normalized); mid[key] = idx; return idx;
            };
            for (int i = 0; i < tri.Count; i += 3)
            {
                int a = tri[i], b = tri[i + 1], c = tri[i + 2], ab = M(a, b), bc = M(b, c), ca = M(c, a);
                t2.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
            }
            var sc = new Vector3(0.8f + (float)rnd.NextDouble() * 0.4f, 0.55f + (float)rnd.NextDouble() * 0.3f, 0.8f + (float)rnd.NextDouble() * 0.4f);
            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i] * (0.75f + (float)rnd.NextDouble() * 0.4f);
                pts[i] = Vector3.Scale(p, sc) * 0.5f;
            }
            return Flat("VFX Chunk " + seed, pts, t2, 1f);
        }

        /// <summary>Cilindro de 8 lados, raio 0.5, base em y = 0 e topo em y = 1 (mastro do estandarte).</summary>
        public static Mesh Pole
        {
            get
            {
                if (pole != null) return pole;
                const int k = 8; var pts = new List<Vector3>(); var tri = new List<int>();
                for (int i = 0; i < k; i++) { float a = i * Mathf.PI * 2 / k; pts.Add(new Vector3(Mathf.Cos(a) * 0.5f, 0, Mathf.Sin(a) * 0.5f)); pts.Add(new Vector3(Mathf.Cos(a) * 0.5f, 1, Mathf.Sin(a) * 0.5f)); }
                int top = pts.Count; pts.Add(new Vector3(0, 1.02f, 0));
                for (int i = 0; i < k; i++)
                {
                    int b0 = i * 2, t0 = b0 + 1, b1 = ((i + 1) % k) * 2, t1 = b1 + 1;
                    tri.Add(b0); tri.Add(t0); tri.Add(b1); tri.Add(b1); tri.Add(t0); tri.Add(t1); tri.Add(t0); tri.Add(top); tri.Add(t1);
                }
                return pole = Flat("VFX Pole", pts, tri, 1f);
            }
        }
    }
}
