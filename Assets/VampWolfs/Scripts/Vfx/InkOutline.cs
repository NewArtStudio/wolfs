using UnityEngine;
using UnityEngine.Rendering;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// Contorno de "tinta" em volta dos personagens — o traço do desenho animado vitoriano (VampWolfs, out/2026).
    /// Mesma técnica do DangerOutline do Space: cada renderer do modelo ganha uma cópia filha com o shader
    /// Space/VFX/Outline (casca invertida, largura fixa em pixels). As cópias usam a mesma malha e os mesmos ossos,
    /// então acompanham a animação. Sem o shader, nada acontece.
    /// </summary>
    public static class InkOutline
    {
        /// <summary>Tinta quase preta (amigos e vitrine).</summary>
        public static readonly Color Ink = new Color(0.06f, 0.025f, 0.035f, 0.92f);
        /// <summary>Inimigo: tinta vermelho-escura (no Battlerite o inimigo tem contorno vermelho; aqui mais discreto).</summary>
        public static readonly Color EnemyInk = new Color(0.55f, 0.03f, 0.04f, 0.95f);

        static Shader shader;
        static readonly System.Collections.Generic.Dictionary<Color, Material> mats = new System.Collections.Generic.Dictionary<Color, Material>();
        static readonly System.Collections.Generic.Dictionary<Mesh, Mesh> smooth = new System.Collections.Generic.Dictionary<Mesh, Mesh>();

        /// <summary>Um material por cor (compartilhado entre partidas).</summary>
        static Material NewMaterial(Color color, float widthPx)
        {
            Material cached;
            if (mats.TryGetValue(color, out cached) && cached != null) return cached;
            if (shader == null)
            {
                shader = Resources.Load<Shader>("VFX/Shaders/VfxOutline");
                if (shader == null) shader = Shader.Find("Space/VFX/Outline");
                if (shader == null) return null;
            }
            var m = new Material(shader) { name = "Contorno de tinta" };
            m.SetColor("_Color", color);
            m.SetFloat("_Width", widthPx);
            m.renderQueue = 2490;   // depois dos opacos e ANTES da fumaça/partículas (a névoa cobre o contorno)
            mats[color] = m;
            return m;
        }

        /// <summary>
        /// Cópia da malha com as normais "suavizadas" (média por posição): cubos têm normais quebradas nas quinas e
        /// a casca invertida abriria buracos ali. Malhas sem leitura (FBX sem Read/Write) ficam como estão.
        /// </summary>
        static Mesh Smoothed(Mesh src)
        {
            Mesh m;
            if (smooth.TryGetValue(src, out m) && m != null) return m;
            if (!src.isReadable) { smooth[src] = src; return src; }
            m = Object.Instantiate(src); m.name = src.name + " (contorno)";
            var v = m.vertices; var n = m.normals;
            if (n == null || n.Length != v.Length) { smooth[src] = src; return src; }
            var sum = new System.Collections.Generic.Dictionary<Vector3, Vector3>();
            for (int i = 0; i < v.Length; i++) { Vector3 acc; sum.TryGetValue(v[i], out acc); sum[v[i]] = acc + n[i]; }
            for (int i = 0; i < v.Length; i++) n[i] = sum[v[i]].normalized;
            m.normals = n;
            smooth[src] = m;
            return m;
        }

        /// <summary>Põe o contorno em todos os renderers abaixo de root (chamar depois do modelo pronto).</summary>
        public static void Attach(GameObject root, Color color, float widthPx = 2.4f)
        {
            if (root == null) return;
            var mat = NewMaterial(color, widthPx);
            if (mat == null) return;
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r.gameObject.name == "Contorno" || r.gameObject.name == "TeamRing") continue;
                var smr = r as SkinnedMeshRenderer;
                if (smr != null && smr.sharedMesh != null) { CopySkinned(smr, mat); continue; }
                var mr = r as MeshRenderer;
                if (mr == null || !mr.enabled) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && IsLit(mr)) CopyMesh(mr, mf.sharedMesh, mat);
            }
        }

        /// <summary>Partes "unlit" (olhos brilhando, anel do time) ficam sem contorno.</summary>
        static bool IsLit(Renderer r)
        {
            var m = r.sharedMaterial;
            return m == null || m.shader == null || m.shader.name.IndexOf("Unlit") < 0;
        }

        static void CopySkinned(SkinnedMeshRenderer src, Material mat)
        {
            var go = new GameObject("Contorno"); go.layer = src.gameObject.layer; go.transform.SetParent(src.transform, false);
            var c = go.AddComponent<SkinnedMeshRenderer>();
            c.sharedMesh = Smoothed(src.sharedMesh); c.bones = src.bones; c.rootBone = src.rootBone; c.localBounds = src.localBounds;
            c.updateWhenOffscreen = true;
            Finish(c, src.sharedMesh.subMeshCount, mat);
        }

        static void CopyMesh(MeshRenderer src, Mesh mesh, Material mat)
        {
            var go = new GameObject("Contorno"); go.layer = src.gameObject.layer; go.transform.SetParent(src.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = Smoothed(mesh);
            Finish(go.AddComponent<MeshRenderer>(), mesh.subMeshCount, mat);
        }

        static void Finish(Renderer c, int subMeshes, Material mat)
        {
            var mats = new Material[Mathf.Max(1, subMeshes)];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            c.sharedMaterials = mats;
            c.shadowCastingMode = ShadowCastingMode.Off; c.receiveShadows = false;
        }
    }
}
