using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VampWolfs.EditorMenu
{
    /// <summary>
    /// Editor menu "VampWolfs": creates the prototype scene (one GameBootstrap object, everything
    /// else is built at runtime), adds it to Build Settings and links the character art
    /// (FBX + textures) found in Assets/VampWolfs/Art/Characters/&lt;Name&gt;/.
    /// </summary>
    [InitializeOnLoad]
    public static class VampWolfsMenu
    {
        const string SceneFolder = "Assets/VampWolfs/Scenes";
        const string ScenePath = SceneFolder + "/FarmPrototype.unity";
        const string CharactersFolder = "Assets/VampWolfs/Art/Characters";

        // Runs when the editor loads: re-link shaders and character art every time Play is pressed,
        // so the plain Play button works without using the menu first.
        static VampWolfsMenu()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }

            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap == null)
            {
                return;
            }

            Configure(bootstrap);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
        }

        [MenuItem("VampWolfs/Create Farm Prototype Scene", false, 0)]
        public static void CreateScene()
        {
            TryCreateScene();
        }

        [MenuItem("VampWolfs/Open And Play Farm Prototype", false, 1)]
        public static void OpenAndPlay()
        {
            if (!File.Exists(ScenePath))
            {
                if (!TryCreateScene())
                {
                    return;
                }
            }
            else
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }

                EditorSceneManager.OpenScene(ScenePath);
                RefreshBootstrapInOpenScene();
            }

            EditorApplication.isPlaying = true;
        }

        [MenuItem("VampWolfs/Refresh Character Art", false, 20)]
        public static void RefreshCharacterArt()
        {
            if (!File.Exists(ScenePath))
            {
                TryCreateScene();
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(ScenePath);
            RefreshBootstrapInOpenScene();
        }

        // ---------------------------------------------------------------- scene

        static bool TryCreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameBootstrap bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            Configure(bootstrap);

            Directory.CreateDirectory(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.Refresh();

            Debug.Log("[VampWolfs] Scene created at " + ScenePath + ". Press Play.");
            return true;
        }

        /// <summary>Re-links shaders and character art on the GameBootstrap of the open scene and saves it.</summary>
        static void RefreshBootstrapInOpenScene()
        {
            GameBootstrap bootstrap = Object.FindFirstObjectByType<GameBootstrap>();
            if (bootstrap == null)
            {
                bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            }

            Configure(bootstrap);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(bootstrap.gameObject.scene);
            EditorSceneManager.SaveScene(bootstrap.gameObject.scene);
        }

        static void Configure(GameBootstrap bootstrap)
        {
            // Serialized references keep these shaders in player builds (Shader.Find alone may not).
            bool urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            bootstrap.LitShader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
            bootstrap.UnlitShader = Shader.Find(urp ? "Universal Render Pipeline/Unlit" : "Unlit/Color");

            if (bootstrap.VampireArt == null) bootstrap.VampireArt = new CharacterArt(2.0f);
            if (bootstrap.WerewolfArt == null) bootstrap.WerewolfArt = new CharacterArt(2.5f);
            if (bootstrap.MonsterHunterArt == null) bootstrap.MonsterHunterArt = new CharacterArt(1.9f);

            LinkArt(bootstrap.VampireArt, "Vampire");
            LinkArt(bootstrap.WerewolfArt, "Werewolf");
            LinkArt(bootstrap.MonsterHunterArt, "MonsterHunter");
        }

        // ---------------------------------------------------------------- character art

        /// <summary>
        /// Finds the first FBX/model in the character folder and its textures by name
        /// ("basecolor"/"albedo"/"diffuse" and "normal"). Height and YawOffset are left as the user set them.
        /// </summary>
        static void LinkArt(CharacterArt art, string folderName)
        {
            string folder = CharactersFolder + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            GameObject model = null;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (model != null) break;
            }

            Texture2D baseColor = null;
            Texture2D normal = null;

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                if (normal == null && file.Contains("normal"))
                {
                    EnsureNormalMap(path);
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
                else if (baseColor == null && (file.Contains("basecolor") || file.Contains("albedo") || file.Contains("diffuse")))
                {
                    baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                }
            }

            art.Model = model;
            art.BaseColor = baseColor;
            art.Normal = normal;

            Debug.Log("[VampWolfs] " + folderName + ": model=" + (model != null ? model.name : "none") +
                      ", baseColor=" + (baseColor != null ? baseColor.name : "none") +
                      ", normal=" + (normal != null ? normal.name : "none"));
        }

        static void EnsureNormalMap(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        static void AddSceneToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == ScenePath))
            {
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
