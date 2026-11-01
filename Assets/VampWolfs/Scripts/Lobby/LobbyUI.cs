using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Lobby (Base) copiado do jogo espacial: tela inicial em UI Toolkit (BaseScreen), Arsenal (equipamento + baú)
    /// e o palco pintado com o vampiro em 3D (BaseStage). Faz o papel que o GameUI tinha no Space:
    /// monta o painel, guarda o perfil, mostra avisos (Banner) e liga/desliga tudo ao entrar e sair da Fazenda.
    /// Estilos em Resources/Lobby/Lobby.uss (classes bs- e ars- do Space, recoloridas para o vitoriano).
    /// Nada aqui decide regra de jogo: só chama o perfil (PlayerProfile) e o GameBootstrap.
    /// </summary>
    public sealed class LobbyUI : MonoBehaviour
    {
        const string StylePath = "Lobby/Lobby";
        const string ThemePath = "Lobby/LobbyTheme";

        GameBootstrap boot;
        Camera cam;
        UIDocument doc;
        PanelSettings panel;
        VisualElement root, bannerBox;
        BaseScreen baseScreen;
        ArsenalScreen arsenal;
        BaseStage stage;
        Transform stageChar;
        readonly List<KeyValuePair<Label, float>> banners = new List<KeyValuePair<Label, float>>();
        readonly List<Action> scheduled = new List<Action>();
        int shownFrame;   // a tecla que fechou a partida (Enter) não pode valer também aqui no mesmo quadro

        /// <summary>true se o estilo do lobby existe (sem ele o jogo usa a Base antiga em IMGUI).</summary>
        public static bool Available { get { return Resources.Load<StyleSheet>(StylePath) != null; } }

        public GameBootstrap Boot { get { return boot; } }
        public PlayerProfile Profile { get { return boot.Profile; } }
        public GearCatalog Catalog { get { return GearCatalog.Instance; } }
        public bool InBase { get; private set; }
        public VisualElement Root { get { return root; } }

        /// <summary>Monta o lobby. baseCamera = câmera da Base (o palco a posiciona todo quadro).</summary>
        public void Init(GameBootstrap bootstrap, Camera baseCamera)
        {
            boot = bootstrap;
            cam = baseCamera;

            // painel do UI Toolkit criado por código (como o "temporário" do Space): escala pela tela 1920×1080
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.name = "Lobby (runtime)";
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemePath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.match = 0.5f;

            GameObject go = new GameObject("Lobby UI");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            go.SetActive(true);

            root = UiKit.El("space-root lobby");
            StyleSheet style = Resources.Load<StyleSheet>(StylePath);
            if (style != null) root.styleSheets.Add(style);
            root.pickingMode = PickingMode.Ignore;
            doc.rootVisualElement.Add(root);

            baseScreen = new BaseScreen(this, root);
            arsenal = new ArsenalScreen(this, root);
            bannerBox = UiKit.El("banner", root);
            bannerBox.pickingMode = PickingMode.Ignore;

            stage = BaseStage.Create(transform);
            if (stage != null) stageChar = BuildStageCharacter(stage.transform);

            UiKit.Show(root, false);
        }

        /// <summary>Vampiro em pé no palco, de frente para a câmera (só visual: sem combate nem colisão).</summary>
        static Transform BuildStageCharacter(Transform parent)
        {
            GameObject go = CharacterFactory.CreateDisplayVampire(parent);
            go.transform.localPosition = BaseStage.SlotPositions[1];
            go.transform.localRotation = Quaternion.Euler(0f, BaseStage.SlotYaw[1], 0f);
            return go.transform;
        }

        /* ======================= ligar / desligar ======================= */

        /// <summary>Mostra o lobby (na Base) ou esconde tudo (na Fazenda).</summary>
        public void Show(bool on)
        {
            InBase = on;
            shownFrame = Time.frameCount;
            if (!on)
            {
                arsenal.Close();
                baseScreen.CloseModal();
            }

            UiKit.Show(root, on);
            if (stage != null) stage.Show(on, cam);
            PortraitStudio.SetEnabled(on);
            if (on) RenderBase();
        }

        void Update()
        {
            if (scheduled.Count > 0)
            {
                Action[] run = scheduled.ToArray();
                scheduled.Clear();
                foreach (Action a in run) a();
            }

            UpdateBanners();
            if (!InBase) return;

            if (stage != null && stage.Visible)
            {
                stage.ApplyCamera(cam, Vector3.zero);
                if (stageChar != null) stage.SetFeet(new[] { stageChar.position });
            }

            if (Time.frameCount <= shownFrame + 1) return;

            if (InputReader.EscapePressed())
            {
                if (arsenal.IsOpen) { if (!arsenal.Escape()) arsenal.Close(); }
                else baseScreen.CloseModal();
            }
            else if (InputReader.InventoryPressed())
            {
                if (arsenal.IsOpen) arsenal.Close(); else OpenArsenal();
            }
            else if (InputReader.RestartPressed() && !arsenal.IsOpen && !baseScreen.ModalOpen)
            {
                Explore();
            }
        }

        /* ======================= comandos usados pelas telas ======================= */

        /// <summary>EXPLORAR: entra direto na Fazenda (por enquanto é o único destino).</summary>
        public void Explore()
        {
            if (!InBase) return;
            Schedule(() => { if (InBase) boot.StartMatch(); });   // fora do clique da interface
        }

        public void OpenArsenal()
        {
            if (!InBase) return;
            arsenal.Open();
        }

        /// <summary>Refaz a tela inicial (depois de equipar, melhorar, resetar…).</summary>
        public void RenderBase()
        {
            if (baseScreen != null) baseScreen.Render();
        }

        /// <summary>Salva o perfil (o jogo salva ao trocar equipamento e ao sair/voltar da Fazenda).</summary>
        public void Save()
        {
            Profile.Save();
        }

        public void ResetProfile()
        {
            boot.ResetProfile();
            PortraitStudio.MarkDirty();
            RenderBase();
        }

        /// <summary>Roda depois deste quadro (fora do callback de UI que pediu).</summary>
        public void Schedule(Action a)
        {
            if (a != null) scheduled.Add(a);
        }

        /// <summary>Aviso curto no topo da tela. kind: "", "ok", "warn", "danger".</summary>
        public void Banner(string text, string kind = "", float dur = 1.8f)
        {
            if (bannerBox == null || string.IsNullOrEmpty(text)) return;
            Label b = UiKit.Lbl(text, "bn " + (kind ?? ""), bannerBox);
            b.pickingMode = PickingMode.Ignore;
            bannerBox.BringToFront();
            banners.Add(new KeyValuePair<Label, float>(b, Time.unscaledTime + dur));
            while (banners.Count > 4)
            {
                banners[0].Key.RemoveFromHierarchy();
                banners.RemoveAt(0);
            }
        }

        void UpdateBanners()
        {
            for (int i = banners.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < banners[i].Value) continue;
                banners[i].Key.RemoveFromHierarchy();
                banners.RemoveAt(i);
            }
        }

        void OnDestroy()
        {
            if (panel != null) Destroy(panel);
        }
    }
}
