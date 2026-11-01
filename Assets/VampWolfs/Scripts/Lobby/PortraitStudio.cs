using UnityEngine;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Estúdio escondido (copiado do Space) que fotografa o vampiro para o lobby:
    ///  - Portrait(): retrato de rosto e ombros em 3/4 para o card da tela inicial (desenhado uma vez; MarkDirty refaz);
    ///  - Preview: corpo inteiro para o Arsenal, desenhado todo quadro enquanto o Arsenal está aberto; Spin gira o modelo.
    /// Luz quente na frente e recorte vinho (cor do vampiro). Fica longe do campo, numa camada só dele,
    /// então as luzes e câmeras do jogo não o enxergam. Sem modelo 3D usa o boneco provisório (nunca devolve null).
    /// </summary>
    public sealed class PortraitStudio : MonoBehaviour
    {
        static PortraitStudio instance;
        static readonly Vector3 StudioPos = new Vector3(0f, -700f, 0f);
        /// <summary>O boneco do corpo inteiro fica longe do retrato para uma câmera não ver o modelo da outra.</summary>
        static readonly Vector3 PreviewOffset = new Vector3(40f, 0f, 0f);
        const int PortraitSize = 256;
        const int PreviewWidth = 512, PreviewHeight = 768;
        /// <summary>Altura de referência do enquadramento (o soldado do Space tinha ~1,8 m).</summary>
        const float RefHeight = 1.8f;
        /// <summary>Camada só do estúdio: as luzes direcionais da cena não iluminam os retratos e a câmera do estúdio só vê o estúdio.</summary>
        public const int Layer = 30;
        static readonly Color Backdrop = new Color(0.07f, 0.035f, 0.045f, 1f);
        static readonly Color RimColor = new Color(0.85f, 0.18f, 0.25f);

        RenderTexture portraitRt, previewRt;
        Camera portraitCam, previewCam;
        Transform portraitModel, previewModel;
        bool portraitDirty = true, rendering, previewOn, enabledInBase = true;
        float spin;

        static PortraitStudio Instance()
        {
            if (instance != null) return instance;
            var go = new GameObject("Estúdio de retratos"); go.transform.position = StudioPos;
            instance = go.AddComponent<PortraitStudio>(); instance.Build();
            return instance;
        }

        void Build()
        {
            float height = ArtLibrary.Vampire != null && ArtLibrary.Vampire.IsValid ? ArtLibrary.Vampire.Height : 2.2f;
            float k = height / RefHeight;

            // retrato: 3/4 de frente, um pouco de cima (rosto e ombros)
            portraitModel = CharacterFactory.CreateDisplayVampire(transform).transform;
            portraitModel.localRotation = Quaternion.Euler(0f, 18f, 0f);
            portraitRt = new RenderTexture(PortraitSize, PortraitSize, 24) { name = "Retrato do vampiro", antiAliasing = 4 };
            portraitRt.Create(); Clear(portraitRt);
            portraitCam = MakeCamera("Câmera do retrato", transform, new Vector3(0.62f, 1.78f, 1.32f) * k, new Vector3(0.02f, 1.52f, 0f) * k, 26f, portraitRt);
            Lights(transform, k);

            // corpo inteiro (Arsenal): de frente, enquadrando da cabeça aos pés
            var pv = new GameObject("Vitrine do Arsenal").transform; pv.SetParent(transform, false); pv.localPosition = PreviewOffset;
            previewModel = CharacterFactory.CreateDisplayVampire(pv).transform;
            previewRt = new RenderTexture(PreviewWidth, PreviewHeight, 24) { name = "Vampiro no Arsenal", antiAliasing = 4 };
            previewRt.Create(); Clear(previewRt);
            previewCam = MakeCamera("Câmera do Arsenal", pv, new Vector3(0f, 1.1f, 4.4f) * k, new Vector3(0f, 0.95f, 0f) * k, 30f, previewRt);
            previewCam.backgroundColor = new Color(0f, 0f, 0f, 0f);   // fundo transparente: o cenário do Arsenal aparece atrás
            Lights(pv, k);

            SetLayer(transform);
            ExcludeFromSceneLights();
            previewModel.gameObject.SetActive(false);
        }

        static Camera MakeCamera(string name, Transform parent, Vector3 pos, Vector3 lookAt, float fov, RenderTexture rt)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.LookAt(parent.position + lookAt);
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = 0.05f; cam.farClipPlane = 12f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Backdrop;
            cam.cullingMask = 1 << Layer; cam.targetTexture = rt; cam.enabled = false;
            return cam;
        }

        void Lights(Transform parent, float k)
        {
            Point(parent, "Luz principal", new Vector3(1.4f, 2.4f, 1.6f) * k, new Color(1f, 0.84f, 0.62f), 5.5f, 6f * k);
            Point(parent, "Luz de preenchimento", new Vector3(-1.4f, 1.6f, 1.4f) * k, new Color(0.62f, 0.55f, 0.75f), 1.6f, 6f * k);
            Point(parent, "Recorte (vinho)", new Vector3(-0.9f, 2.1f, -1.2f) * k, RimColor, 7f, 5f * k);
        }

        static void Point(Transform parent, string name, Vector3 pos, Color c, float intensity, float range)
        {
            var l = new GameObject(name).AddComponent<Light>(); l.transform.SetParent(parent, false);
            l.type = LightType.Point; l.transform.localPosition = pos; l.color = c; l.intensity = intensity; l.range = range; l.shadows = LightShadows.None;
            l.cullingMask = 1 << Layer;
        }

        static void Clear(RenderTexture rt)
        {
            var prev = RenderTexture.active; RenderTexture.active = rt;
            GL.Clear(true, true, Backdrop);
            RenderTexture.active = prev;
        }

        /* ======================= usado pelo lobby ======================= */

        /// <summary>Retrato do vampiro para o card (a textura fica pronta no fim do quadro).</summary>
        public static Texture Portrait()
        {
            var s = Instance();
            return s.portraitRt;
        }

        /// <summary>Corpo inteiro para o Arsenal (só é atualizado enquanto SetPreviewActive(true)).</summary>
        public static Texture Preview { get { return Instance().previewRt; } }

        /// <summary>Pede um retrato novo (ex.: perfil resetado; quando o equipamento tiver visual, ao trocar peças).</summary>
        public static void MarkDirty()
        {
            if (instance != null) instance.portraitDirty = true;
        }

        /// <summary>Liga/desliga a vitrine do Arsenal (corpo inteiro girando).</summary>
        public static void SetPreviewActive(bool on)
        {
            var s = Instance();
            s.previewOn = on;
            s.previewModel.gameObject.SetActive(on);
            s.previewCam.enabled = on && s.enabledInBase;
            if (on) { s.spin = 0f; s.previewModel.localRotation = Quaternion.identity; }
        }

        /// <summary>Gira o vampiro do Arsenal (graus; arrastar com o mouse).</summary>
        public static void Spin(float degrees)
        {
            if (instance == null) return;
            instance.spin += degrees;
            instance.previewModel.localRotation = Quaternion.Euler(0f, instance.spin, 0f);
        }

        /// <summary>Fora da Base o estúdio não desenha nada (não gasta GPU durante a partida).</summary>
        public static void SetEnabled(bool on)
        {
            var s = Instance();
            s.enabledInBase = on;
            s.previewCam.enabled = on && s.previewOn;
            if (!on) { s.portraitCam.enabled = false; s.rendering = false; s.portraitModel.gameObject.SetActive(false); }
        }

        void LateUpdate()
        {
            // o quadro anterior já desenhou o retrato: desliga a câmera e o modelo
            if (rendering)
            {
                rendering = false; portraitCam.enabled = false;
                portraitModel.gameObject.SetActive(false);   // parado não gasta animação
            }

            if (portraitDirty && enabledInBase)
            {
                portraitDirty = false;
                portraitModel.gameObject.SetActive(true);
                portraitCam.enabled = true; rendering = true;   // a câmera desenha no fim deste quadro
            }
        }

        static void SetLayer(Transform t) { t.gameObject.layer = Layer; for (int i = 0; i < t.childCount; i++) SetLayer(t.GetChild(i)); }

        /// <summary>Tira a camada do estúdio das luzes direcionais da cena (sol e preenchimento).</summary>
        public static void ExcludeFromSceneLights()
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) l.cullingMask &= ~(1 << Layer);
        }

        void OnDestroy()
        {
            if (portraitRt != null) Destroy(portraitRt);
            if (previewRt != null) Destroy(previewRt);
            if (instance == this) instance = null;
        }
    }
}
