using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace VampWolfs.EditorMenu
{
    /// <summary>
    /// Editor menu "VampWolfs": creates the prototype scene (one GameBootstrap object, everything
    /// else is built at runtime) and adds it to Build Settings.
    /// </summary>
    public static class VampWolfsMenu
    {
        const string SceneFolder = "Assets/VampWolfs/Scenes";
        const string ScenePath = SceneFolder + "/FarmPrototype.unity";

        [MenuItem("VampWolfs/Create Farm Prototype Scene", false, 0)]
        public static void CreateScene()
        {
            TryCreateScene();
        }

        static bool TryCreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameBootstrap bootstrap = new GameObject("GameBootstrap").AddComponent<GameBootstrap>();

            // Serialized references keep these shaders in player builds (Shader.Find alone may not).
            bool urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            bootstrap.LitShader = Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard");
            bootstrap.UnlitShader = Shader.Find(urp ? "Universal Render Pipeline/Unlit" : "Unlit/Color");

            Directory.CreateDirectory(SceneFolder);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.Refresh();

            Debug.Log("[VampWolfs] Scene created at " + ScenePath + ". Press Play.");
            return true;
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
            }

            EditorApplication.isPlaying = true;
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
