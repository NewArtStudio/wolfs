using UnityEngine;

namespace VampWolfs
{
    /// <summary>
    /// Entry point of the prototype.
    /// Put this on an empty GameObject in an EMPTY scene and press Play (menu VampWolfs > Create Farm Prototype
    /// Scene does this for you). Flow, like an extraction game:
    /// Base (stash, equipment, upgrades) → "Entrar na Fazenda" → match → extract or die → back to the Base.
    /// Everything is built from code.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public MatchSettings Settings = new MatchSettings();

        [Tooltip("Assigned automatically by the VampWolfs menu. Referencing the shaders here makes sure they are included in builds.")]
        public Shader LitShader;
        public Shader UnlitShader;

        [Header("Character art (filled by the VampWolfs menu)")]
        public CharacterArt VampireArt = new CharacterArt(2.0f);
        public CharacterArt WerewolfArt = new CharacterArt(2.5f);
        public CharacterArt MonsterHunterArt = new CharacterArt(1.9f);

        [Header("Lobby")]
        [Tooltip("Size of the vampire on the lobby stage (1 = same as in the match art height).")]
        public float LobbyCharacterScale = 1.4f;
        [Tooltip("Meters the lobby vampire stands in front of the stage center (toward the camera).")]
        public float LobbyCharacterForward = 1.0f;

        GameObject worldRoot;
        GameObject baseCamera;
        BaseScreen baseScreen;          // old IMGUI Base (fallback when the lobby style is missing)
        Lobby.LobbyUI lobby;            // lobby copied from the Space project (UI Toolkit + painted stage)

        /// <summary>Saved progress: worn gear, stash, resources.</summary>
        public PlayerProfile Profile { get; private set; }

        void Start()
        {
            MaterialLibrary.SetShaders(LitShader, UnlitShader);
            ArtLibrary.Vampire = VampireArt;
            ArtLibrary.Werewolf = WerewolfArt;
            ArtLibrary.MonsterHunter = MonsterHunterArt;

            Profile = PlayerProfile.LoadOrCreate(GearCatalog.Instance);
            SetBaseCamera(false);
            if (Lobby.LobbyUI.Available)
            {
                lobby = gameObject.AddComponent<Lobby.LobbyUI>();
                lobby.Init(this, baseCamera.GetComponent<Camera>());
            }
            else
            {
                baseScreen = gameObject.AddComponent<BaseScreen>();
                baseScreen.Init(this);
            }

            ShowBase();
        }

        /// <summary>Called by the Base screen: builds the farm and starts a match with the worn gear.</summary>
        public void StartMatch()
        {
            Profile.Save();
            if (lobby != null) lobby.Show(false);
            if (baseScreen != null) baseScreen.enabled = false;
            SetBaseCamera(false);
            DestroyWorld();
            Build();
        }

        /// <summary>Leaves the match (after extraction or death) and shows the Base.</summary>
        public void ReturnToBase()
        {
            DestroyWorld();
            Profile.Save();
            ShowBase();
        }

        /// <summary>Destroys the current world and builds a fresh match (kept for compatibility).</summary>
        public void Restart()
        {
            StartMatch();
        }

        public void ResetProfile()
        {
            Profile = PlayerProfile.Reset(GearCatalog.Instance);
        }

        void ShowBase()
        {
            SetBaseCamera(true);
            if (lobby != null) lobby.Show(true);
            if (baseScreen != null) baseScreen.enabled = true;
        }

        void DestroyWorld()
        {
            if (worldRoot != null)
            {
                // Deactivate first so the old world stops immediately (Destroy waits for the end of the frame).
                worldRoot.SetActive(false);
                Destroy(worldRoot);
                worldRoot = null;
            }

            FloatingText.Clear();
        }

        /// <summary>Camera of the Base: draws the lobby stage (the portrait studio layer is left out).</summary>
        void SetBaseCamera(bool on)
        {
            if (baseCamera == null)
            {
                baseCamera = new GameObject("Base Camera");
                baseCamera.transform.SetParent(transform, false);
                Camera cam = baseCamera.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.07f, 0.05f, 0.1f);
                cam.cullingMask = ~(1 << Lobby.PortraitStudio.Layer);
                baseCamera.AddComponent<AudioListener>();
            }

            baseCamera.SetActive(on);
        }

        void Build()
        {
            worldRoot = new GameObject("World");
            Transform root = worldRoot.transform;

            FarmLayout layout = FarmBuilder.Build(root, Settings);
            CharacterFactory.SizeMultiplier = Mathf.Max(0.1f, Settings.CharacterSize);

            CombatUnit player = CharacterFactory.CreateVampire(root, layout.PlayerSpawn);
            player.ApplyGear(GearCatalog.Instance, Profile.EquippedMap());

            Camera cam = TopDownCamera.Create(root, player.transform, Settings);
            Vfx.VfxHub.CreateForMatch(root, cam);   // particles, telegraphs, bloom (copied from the Space project)
            player.gameObject.AddComponent<PlayerController>().Init(cam);

            ExtractionPoint extraction = ExtractionPoint.Create(root, layout.ExtractionPosition, Settings);

            MatchManager match = worldRoot.AddComponent<MatchManager>();
            match.Init(this, Settings, layout, player, extraction, Profile);

            GameHUD hud = worldRoot.AddComponent<GameHUD>();
            hud.Init(match, cam);
        }
    }
}
