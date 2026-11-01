using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// Shaders, texturas e materiais dos efeitos (Resources/VFX/), copiados do projeto Space. Rune, Hex e Bat foram
    /// refeitas no estilo vitoriano (Tools/TexGen/victorian_vfx_textures.py); shaders URP escritos à mão.
    /// </summary>
    public static class VfxAssets
    {
        public static Shader Additive, Alpha, Ground, Fresnel, MeshLit;
        /// <summary>Pacote de impacto (opcionais: sem eles só a onda de choque e o dissolve desligam).</summary>
        public static Shader Dissolve, Distortion;
        public static Texture2D Glow, Spark, Flare, Smoke, Fire, Noise, BeamTex, Scorch, Crack, Splat, Hex, Rune, Plus, Slash, Shard, Emblem;
        /// <summary>VampWolfs: folha 2×2 de um morcego batendo as asas (Tools/TexGen/victorian_vfx_textures.py).</summary>
        public static Texture2D Bat;
        static bool tried, ok;
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        /// <summary>true se os shaders e as texturas existem (sem eles o jogo segue sem efeitos).</summary>
        public static bool Load()
        {
            if (tried) return ok; tried = true;
            Additive = Sh("VfxAdditive", "Space/VFX/Additive"); Alpha = Sh("VfxAlphaBlend", "Space/VFX/AlphaBlend");
            Ground = Sh("VfxGround", "Space/VFX/Ground"); Fresnel = Sh("VfxFresnel", "Space/VFX/Fresnel"); MeshLit = Sh("VfxMeshLit", "Space/VFX/MeshLit");
            Dissolve = Sh("VfxDissolve", "Space/VFX/Dissolve"); Distortion = Sh("VfxDistortion", "Space/VFX/Distortion");
            Glow = Tx("Glow"); Spark = Tx("Spark"); Flare = Tx("Flare"); Smoke = Tx("Smoke"); Fire = Tx("Fire"); Noise = Tx("Noise");
            BeamTex = Tx("Beam"); Scorch = Tx("Scorch"); Crack = Tx("Crack"); Splat = Tx("Splat"); Hex = Tx("Hex"); Rune = Tx("Rune");
            Plus = Tx("Plus"); Slash = Tx("Slash"); Shard = Tx("Shard"); Emblem = Tx("Emblem"); Bat = Tx("Bat");
            ok = Additive != null && Alpha != null && Ground != null && Fresnel != null && MeshLit != null && Glow != null && Fire != null && Smoke != null;
            if (!ok) Debug.LogWarning("[VampWolfs] VFX: shaders/texturas não encontrados em Resources/VFX — efeitos desligados.");
            return ok;
        }

        static Shader Sh(string file, string name)
        {
            var s = Resources.Load<Shader>("VFX/Shaders/" + file);
            if (s == null) s = Shader.Find(name);
            return s;
        }
        static Texture2D Tx(string n) { return Resources.Load<Texture2D>("VFX/Textures/" + n); }

        /// <summary>Material aditivo com textura e intensidade (compartilhado por chave).</summary>
        public static Material AdditiveMat(string key, Texture tex, float intensity, float edgeFade = 0f)
        {
            Material m; if (mats.TryGetValue("add:" + key, out m)) return m;
            m = new Material(Additive) { name = "VFX " + key };
            if (tex != null) m.SetTexture("_MainTex", tex);
            m.SetFloat("_Intensity", intensity); m.SetFloat("_EdgeFade", edgeFade);
            m.renderQueue = 3000;
            mats["add:" + key] = m; return m;
        }

        /// <summary>Material com transparência normal (fumaça, poeira) — ou mancha no chão (decal = true).</summary>
        public static Material AlphaMat(string key, Texture tex, bool decal, float lit = 0.8f)
        {
            Material m; if (mats.TryGetValue("alpha:" + key, out m)) return m;
            m = new Material(Alpha) { name = "VFX " + key };
            if (tex != null) m.SetTexture("_MainTex", tex);
            m.SetFloat("_Lit", lit);
            if (decal) { m.SetFloat("_OffsetFactor", -1f); m.SetFloat("_OffsetUnits", -2f); m.renderQueue = 2980; }
            else m.renderQueue = 3000;
            mats["alpha:" + key] = m; return m;
        }

        public static Material GroundMat
        {
            get
            {
                Material m; if (mats.TryGetValue("ground", out m)) return m;
                m = new Material(Ground) { name = "VFX Ground" };
                m.SetTexture("_MainTex", Rune); m.SetTexture("_NoiseTex", Noise); m.renderQueue = 2990;
                mats["ground"] = m; return m;
            }
        }

        /// <summary>Mesmo shader do chão, desenhado POR CIMA de tudo (ZTest Always): borda dos golpes fatais, que não pode
        /// sumir atrás do relevo do terreno nem das criaturas (pedido do criador, out/2026).</summary>
        public static Material GroundTopMat
        {
            get
            {
                Material m; if (mats.TryGetValue("groundtop", out m)) return m;
                m = new Material(Ground) { name = "VFX Ground (por cima)" };
                m.SetTexture("_MainTex", Rune); m.SetTexture("_NoiseTex", Noise);
                m.SetFloat("_ZTest", (float)UnityEngine.Rendering.CompareFunction.Always); m.renderQueue = 3100;
                mats["groundtop"] = m; return m;
            }
        }

        public static Material FresnelMat(string key, Texture pattern, Vector2 tiling, Vector2 scroll, bool cullBack, float body = 0.05f)
        {
            Material m; if (mats.TryGetValue("fres:" + key, out m)) return m;
            m = new Material(Fresnel) { name = "VFX " + key };
            if (pattern != null) m.SetTexture("_MainTex", pattern);
            m.SetTextureScale("_MainTex", tiling);
            m.SetVector("_Scroll", new Vector4(scroll.x, scroll.y, 0, 0));
            m.SetFloat("_Cull", cullBack ? 2f : 0f);
            m.SetFloat("_Base", body);   // corpo: quase só borda (escudo) ou "cheio" (cristal de gelo)
            m.renderQueue = 3000;
            mats["fres:" + key] = m; return m;
        }

        public static Material LitMat(string key, Color color, Color emission)
        {
            Material m; if (mats.TryGetValue("lit:" + key, out m)) return m;
            m = new Material(MeshLit) { name = "VFX " + key };
            m.SetColor("_Color", color); m.SetColor("_Emission", emission);
            mats["lit:" + key] = m; return m;
        }
    }
}
