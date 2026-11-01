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
        // girar o vampiro arrastando o mouse no palco (área vazia da tela)
        bool turning; float turnX, yaw, yawVelocity, lastTurnMove;
        const float TurnPerPixel = 0.45f, TurnFriction = 5f;
        readonly List<KeyValuePair<Label, float>> banners = new List<KeyValuePair<Label, float>>();
        readonly List<Action> scheduled = new List<Action>();
        readonly Vector3[] feet = new Vector3[1];
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
            if (stage != null) stageChar = BuildStageCharacter(stage.transform, boot.LobbyCharacterScale, boot.LobbyCharacterForward);
            if (stageChar != null) yaw = stageChar.localEulerAngles.y;

            // arrastar na área vazia (fora dos painéis) gira o vampiro: uma camada transparente atrás dos painéis recebe o clique
            VisualElement screen = new VisualElement { name = "lobby-drag" };
            screen.style.position = Position.Absolute;
            screen.style.left = 0f; screen.style.top = 0f; screen.style.right = 0f; screen.style.bottom = 0f;
            screen.pickingMode = PickingMode.Position;
            root.Insert(0, screen);
            screen.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) { turning = true; turnX = e.position.x; yawVelocity = 0f; screen.CapturePointer(e.pointerId); } });
            screen.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!turning) return;
                float dx = e.position.x - turnX; turnX = e.position.x;
                yaw -= dx * TurnPerPixel;
                yawVelocity = -dx * TurnPerPixel / Mathf.Max(0.005f, Time.unscaledDeltaTime);
                lastTurnMove = Time.unscaledTime;
            });
            screen.RegisterCallback<PointerUpEvent>(e => { turning = false; if (Time.unscaledTime - lastTurnMove > 0.05f) yawVelocity = 0f;   // parou antes de soltar: não arremessa
 if (screen.HasPointerCapture(e.pointerId)) screen.ReleasePointer(e.pointerId); });
            screen.RegisterCallback<PointerCaptureOutEvent>(e => turning = false);

            UiKit.Show(root, false);
        }

        /// <summary>
        /// Vampiro em pé no palco, de frente para a câmera (só visual: sem combate nem colisão).
        /// scale = tamanho no palco (o palco do Space foi pintado para 3 soldados; com 1 personagem ele fica maior),
        /// forward = metros para a frente (em direção à câmera). Os dois ficam no Inspector do GameBootstrap.
        /// </summary>
        static Transform BuildStageCharacter(Transform parent, float scale, float forward)
        {
            GameObject go = CharacterFactory.CreateDisplayVampire(parent, Mathf.Max(0.2f, scale));
            go.transform.localPosition = BaseStage.SlotPositions[1] + new Vector3(0f, 0f, forward);
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
                if (stageChar != null)
                {
                    feet[0] = stageChar.position; stage.SetFeet(feet);
                    if (!turning)
                    {
                        yaw += yawVelocity * Time.unscaledDeltaTime;   // continua girando um pouco depois de soltar
                        yawVelocity *= Mathf.Exp(-TurnFriction * Time.unscaledDeltaTime);
                    }
                    stageChar.localRotation = Quaternion.Euler(0f, yaw, 0f);
                }
            }

            if (Time.frameCount <= shownFrame + 1 || CapturesKeyboard) return;

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

        /// <summary>true enquanto um campo de texto (busca do Arsenal) tem o foco: as teclas de atalho não valem.</summary>
        bool CapturesKeyboard
        {
            get
            {
                if (root == null || root.panel == null) return false;
                var f = root.panel.focusController.focusedElement as VisualElement;
                for (var e = f; e != null; e = e.parent) if (e is TextField) return true;
                return false;
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
