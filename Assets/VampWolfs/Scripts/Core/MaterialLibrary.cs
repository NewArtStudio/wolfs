using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VampWolfs
{
    /// <summary>
    /// Creates and caches simple colored materials for the placeholder art.
    /// Uses URP shaders when a render pipeline is active, falls back to Built-in shaders otherwise.
    /// </summary>
    public static class MaterialLibrary
    {
        static readonly Dictionary<Color, Material> litCache = new Dictionary<Color, Material>();
        static readonly Dictionary<Color, Material> unlitCache = new Dictionary<Color, Material>();

        static Shader litOverride;
        static Shader unlitOverride;

        static bool UsingRenderPipeline
        {
            get { return GraphicsSettings.currentRenderPipeline != null; }
        }

        /// <summary>Shaders referenced by the scene (included in builds). Null = find by name.</summary>
        public static void SetShaders(Shader lit, Shader unlit)
        {
            litOverride = lit;
            unlitOverride = unlit;
            litCache.Clear();
            unlitCache.Clear();
        }

        public static Material Lit(Color color)
        {
            Material material;
            if (litCache.TryGetValue(color, out material) && material != null)
            {
                return material;
            }

            material = new Material(LitShader());
            material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.15f);

            litCache[color] = material;
            return material;
        }

        public static Material Unlit(Color color)
        {
            Material material;
            if (unlitCache.TryGetValue(color, out material) && material != null)
            {
                return material;
            }

            material = NewUnlit(color);
            unlitCache[color] = material;
            return material;
        }

        /// <summary>Lit material with a color texture and an optional normal map (character models).</summary>
        public static Material Textured(Texture2D baseColor, Texture2D normal)
        {
            Material material = new Material(LitShader());
            material.color = Color.white;

            if (baseColor != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseColor); // URP
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseColor); // Built-in
            }

            if (normal != null && material.HasProperty("_BumpMap"))
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.25f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.25f);
            return material;
        }

        /// <summary>An uncached unlit material, for objects that change color at runtime.</summary>
        public static Material NewUnlit(Color color)
        {
            Material material = new Material(UnlitShader());
            material.color = color;
            return material;
        }

        static Shader LitShader()
        {
            if (litOverride != null) return litOverride;
            return UsingRenderPipeline
                ? FindShader("Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard")
                : FindShader("Standard", "Diffuse");
        }

        static Shader UnlitShader()
        {
            if (unlitOverride != null) return unlitOverride;
            return UsingRenderPipeline
                ? FindShader("Universal Render Pipeline/Unlit", "Unlit/Color")
                : FindShader("Unlit/Color", "Standard");
        }

        static Shader FindShader(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                Shader shader = Shader.Find(names[i]);
                if (shader != null)
                {
                    return shader;
                }
            }

            Debug.LogError("[VampWolfs] No usable shader found. Is URP installed?");
            return Shader.Find("Hidden/InternalErrorShader");
        }
    }
}
