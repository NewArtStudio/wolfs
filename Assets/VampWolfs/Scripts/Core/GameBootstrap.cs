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

        GameObject worldRoot;
        GameObject baseCamera;
        BaseScreen baseScreen;

        /// <summary>Saved progress: worn gear, stash, resources.</summary>
        public PlayerProfile Profile { get; private set; }

        void Start()
        {
            MaterialLibrary.SetShaders(LitShader, UnlitShader);
            ArtLibrary.Vampire = VampireArt;
            ArtLibrary.Werewolf = WerewolfArt;
            ArtLibrary.MonsterHunter = MonsterHunterArt;

            Profile = PlayerProfile.LoadOrCreate(GearCatalog.Instance);
            baseScreen = gameObject.AddComponent<BaseScreen>();
            baseScreen.Init(this);
            ShowBase();
        }

        /// <summary>Called by the Base screen: builds the farm and starts a match with the worn gear.</summary>
        public void StartMatch()
        {
            Profile.Save();
            baseScreen.enabled = false;
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
            baseScreen.enabled = true;
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

        /// <summary>Simple camera so the Base screen has something to draw over.</summary>
        void SetBaseCamera(bool on)
        {
            if (baseCamera == null)
            {
                baseCamera = new GameObject("Base Camera");
                baseCamera.transform.SetParent(transform, false);
                Camera cam = baseCamera.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.07f, 0.05f, 0.1f);
                cam.cullingMask = 0;
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
            player.gameObject.AddComponent<PlayerController>().Init(cam);

            ExtractionPoint extraction = ExtractionPoint.Create(root, layout.ExtractionPosition, Settings);

            MatchManager match = worldRoot.AddComponent<MatchManager>();
            match.Init(this, Settings, layout, player, extraction, Profile);

            GameHUD hud = worldRoot.AddComponent<GameHUD>();
            hud.Init(match, cam);
        }
    }
}
