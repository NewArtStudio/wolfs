using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Entry point of the prototype.
    /// Put this on an empty GameObject in an EMPTY scene and press Play: it builds the farm,
    /// the vampire, the camera, the HUD and the match rules entirely from code
    /// (menu VampWolfs > Create Farm Prototype Scene does this for you).
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public MatchSettings Settings = new MatchSettings();

        [Tooltip("Assigned automatically by the VampWolfs menu. Referencing the shaders here makes sure they are included in builds.")]
        public Shader LitShader;
        public Shader UnlitShader;

        GameObject worldRoot;

        void Start()
        {
            MaterialLibrary.SetShaders(LitShader, UnlitShader);
            Build();
        }

        /// <summary>Destroys the current world and builds a fresh match.</summary>
        public void Restart()
        {
            if (worldRoot != null)
            {
                // Deactivate first so the old world stops immediately (Destroy waits for the end of the frame).
                worldRoot.SetActive(false);
                Destroy(worldRoot);
            }

            FloatingText.Clear();
            Build();
        }

        void Build()
        {
            worldRoot = new GameObject("World");
            Transform root = worldRoot.transform;

            FarmLayout layout = FarmBuilder.Build(root, Settings);

            CombatUnit player = CharacterFactory.CreateVampire(root, layout.PlayerSpawn);
            Camera cam = TopDownCamera.Create(root, player.transform);
            player.gameObject.AddComponent<PlayerController>().Init(cam);

            ExtractionPoint extraction = ExtractionPoint.Create(root, layout.ExtractionPosition, Settings);

            MatchManager match = worldRoot.AddComponent<MatchManager>();
            match.Init(this, Settings, layout, player, extraction);

            GameHUD hud = worldRoot.AddComponent<GameHUD>();
            hud.Init(match, cam);
        }
    }
}
