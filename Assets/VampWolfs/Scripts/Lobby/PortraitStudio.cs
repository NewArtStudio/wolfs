using System.Collections.Generic;
using UnityEngine;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Estúdio escondido (copiado do Space) que fotografa os personagens para o lobby:
    ///  - Portrait(classe): retrato de rosto e ombros em 3/4 para os cards (um por classe, desenhado uma vez);
    ///  - Preview: corpo inteiro da classe escolhida (SetPreviewClass) para o Arsenal, desenhado todo quadro enquanto
    ///    o Arsenal está aberto; Spin gira o modelo.
    /// Luz quente na frente e recorte na cor da classe. Fica longe do campo, numa camada só dele, então as luzes e
    /// câmeras do jogo não o enxergam. Sem modelo 3D usa o boneco provisório (nunca devolve null).
    /// </summary>
    public sealed class PortraitStudio : MonoBehaviour
    {
        static PortraitStudio instance;
        static readonly Vector3 StudioPos = new Vector3(0f, -700f, 0f);
        /// <summary>Cada cena do estúdio fica longe da outra para uma câmera não ver o modelo da outra.</summary>
        const float Spacing = 40f;
        const int PortraitSize = 256;
        const int PreviewWidth = 512, PreviewHeight = 768;
        /// <summary>Altura de referência do enquadramento (o soldado do Space tinha ~1,8 m).</summary>
        const float RefHeight = 1.8f;
        /// <summary>Camada só do estúdio: as luzes direcionais da cena não iluminam os retratos e a câmera do estúdio só vê o estúdio.</summary>
        public const int Layer = 30;
        static readonly Color Backdrop = new Color(0.07f, 0.035f, 0.045f, 1f);

        sealed class Shot
        {
            public string ClassId;
            public Transform Root, Model;
            public Camera Cam;
            public RenderTexture Rt;
            public bool Dirty = true;
        }

        readonly Dictionary<string, Shot> portraits = new Dictionary<string, Shot>();
        Shot preview;
        Shot rendering;   // retrato sendo desenhado neste quadro
        bool previewOn, enabledInBase = true;
        float spin;
        int nextSpot = 1;

        static PortraitStudio Instance()
        {
            if (instance != null) return instance;
            var go = new GameObject("Estúdio de retratos"); go.transform.position = StudioPos;
            instance = go.AddComponent<PortraitStudio>();
            ExcludeFromSceneLights();
            return instance;
        }

        static float HeightOf(string classId)
        {
            CharacterArt art = classId == "werewolf" ? ArtLibrary.Werewolf : classId == "hunter" ? ArtLibrary.MonsterHunter : ArtLibrary.Vampire;
            if (art != null && art.IsValid) return art.Height;
            return classId == "werewolf" ? 2.7f : 2.3f;
        }

        static Color RimOf(string classId)
        {
            Color c = UiKit.ClassColor(classId);
            return new Color(Mathf.Max(c.r, 0.2f), Mathf.Max(c.g, 0.1f), Mathf.Max(c.b, 0.1f));
        }

        /// <summary>Monta uma cena (modelo + câmera + luzes) num ponto livre do estúdio.</summary>
        Shot Build(string classId, bool full)
        {
            var s = new Shot { ClassId = classId };
            s.Root = new GameObject((full ? "Vitrine " : "Retrato ") + classId).transform;
            s.Root.SetParent(transform, false);
            s.Root.localPosition = new Vector3(Spacing * nextSpot++, 0f, 0f);
            float k = HeightOf(classId) / RefHeight;

            s.Model = CharacterFactory.CreateDisplay(s.Root, classId).transform;
            if (!full) s.Model.localRotation = Quaternion.Euler(0f, 18f, 0f);

            if (full)
            {
                s.Rt = new RenderTexture(PreviewWidth, PreviewHeight, 24) { name = "Vitrine " + classId, antiAliasing = 4 };
                s.Rt.Create(); Clear(s.Rt);
                s.Cam = MakeCamera("Câmera da vitrine", s.Root, new Vector3(0f, 1.1f, 4.4f) * k, new Vector3(0f, 0.95f, 0f) * k, 30f, s.Rt);
                s.Cam.backgroundColor = new Color(0f, 0f, 0f, 0f);   // fundo transparente: o cenário do Arsenal aparece atrás
            }
            else
            {
                s.Rt = new RenderTexture(PortraitSize, PortraitSize, 24) { name = "Retrato " + classId, antiAliasing = 4 };
                s.Rt.Create(); Clear(s.Rt);
                s.Cam = MakeCamera("Câmera do retrato", s.Root, new Vector3(0.62f, 1.78f, 1.32f) * k, new Vector3(0.02f, 1.52f, 0f) * k, 26f, s.Rt);
            }

            Lights(s.Root, k, RimOf(classId));
            SetLayer(s.Root);
            s.Model.gameObject.SetActive(false);
            return s;
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

        static void Lights(Transform parent, float k, Color rim)
        {
            Point(parent, "Luz principal", new Vector3(1.4f, 2.4f, 1.6f) * k, new Color(1f, 0.84f, 0.62f), 5.5f, 6f * k);
            Point(parent, "Luz de preenchimento", new Vector3(-1.4f, 1.6f, 1.4f) * k, new Color(0.62f, 0.55f, 0.75f), 1.6f, 6f * k);
            Point(parent, "Recorte da classe", new Vector3(-0.9f, 2.1f, -1.2f) * k, rim, 7f, 5f * k);
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

        /// <summary>Retrato da classe para os cards (a textura fica pronta no fim do quadro).</summary>
        public static Texture Portrait(string classId = "vampire")
        {
            var s = Instance();
            if (string.IsNullOrEmpty(classId)) classId = "vampire";
            Shot shot;
            if (!s.portraits.TryGetValue(classId, out shot)) { shot = s.Build(classId, false); s.portraits[classId] = shot; }
            return shot.Rt;
        }

        /// <summary>Classe mostrada no Arsenal (troca o modelo quando o personagem escolhido muda).</summary>
        public static void SetPreviewClass(string classId)
        {
            var s = Instance();
            if (s.preview != null && s.preview.ClassId == classId) return;
            if (s.preview != null) s.DestroyShot(s.preview);
            s.preview = s.Build(classId, true);
            s.spin = 0f;
            s.ApplyPreview();
        }

        /// <summary>Corpo inteiro para o Arsenal (só é atualizado enquanto SetPreviewActive(true)).</summary>
        public static Texture Preview
        {
            get
            {
                var s = Instance();
                if (s.preview == null) SetPreviewClass("vampire");
                return s.preview.Rt;
            }
        }

        /// <summary>Pede retratos novos (ex.: perfil resetado).</summary>
        public static void MarkDirty()
        {
            if (instance == null) return;
            foreach (var p in instance.portraits.Values) p.Dirty = true;
        }

        /// <summary>Liga/desliga a vitrine do Arsenal (corpo inteiro girando).</summary>
        public static void SetPreviewActive(bool on)
        {
            var s = Instance();
            if (s.preview == null) SetPreviewClass("vampire");
            s.previewOn = on;
            if (on) { s.spin = 0f; s.preview.Model.localRotation = Quaternion.identity; }
            s.ApplyPreview();
        }

        void ApplyPreview()
        {
            if (preview == null) return;
            preview.Model.gameObject.SetActive(previewOn);
            preview.Cam.enabled = previewOn && enabledInBase;
        }

        /// <summary>Gira o personagem do Arsenal (graus; arrastar com o mouse).</summary>
        public static void Spin(float degrees)
        {
            if (instance == null || instance.preview == null) return;
            instance.spin += degrees;
            instance.preview.Model.localRotation = Quaternion.Euler(0f, instance.spin, 0f);
        }

        /// <summary>Fora da Base o estúdio não desenha nada (não gasta GPU durante a partida).</summary>
        public static void SetEnabled(bool on)
        {
            var s = Instance();
            s.enabledInBase = on;
            s.ApplyPreview();
            if (!on && s.rendering != null)
            {
                s.rendering.Cam.enabled = false;
                s.rendering.Model.gameObject.SetActive(false);
                s.rendering = null;
            }
        }

        void LateUpdate()
        {
            // o quadro anterior já desenhou um retrato: desliga a câmera e o modelo
            if (rendering != null)
            {
                rendering.Cam.enabled = false;
                rendering.Model.gameObject.SetActive(false);   // parado não gasta animação
                rendering = null;
            }

            if (!enabledInBase) return;
            foreach (var p in portraits.Values)
            {
                if (!p.Dirty) continue;
                p.Dirty = false;
                p.Model.gameObject.SetActive(true);
                p.Cam.enabled = true; rendering = p;   // a câmera desenha no fim deste quadro (um retrato por quadro)
                break;
            }
        }

        void DestroyShot(Shot s)
        {
            if (s == null) return;
            if (s.Rt != null) { if (s.Cam != null) s.Cam.targetTexture = null; Destroy(s.Rt); }
            if (s.Root != null) Destroy(s.Root.gameObject);
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
            foreach (var p in portraits.Values) if (p.Rt != null) Destroy(p.Rt);
            if (preview != null && preview.Rt != null) Destroy(preview.Rt);
            if (instance == this) instance = null;
        }
    }
}
