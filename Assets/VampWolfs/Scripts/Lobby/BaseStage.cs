using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VampWolfs.Lobby
{
    /// <summary>
    /// Palco do lobby (copiado da Base Espacial do Space): o cenário é uma pintura em alta resolução
    /// (Resources/Base/BaseBackdrop — por enquanto o hangar do Space, até chegar a pintura vitoriana) e só o vampiro
    /// é 3D, com sombra de verdade projetada no chão pintado.
    ///
    ///  - Fundo: quadrado preso à câmera mostrando a placa sem luz nem névoa (shader Space/Base/Backdrop).
    ///  - Câmera: parada, calibrada para a perspectiva da pintura. A placa é maior que a tela; em telas mais
    ///    largas ou mais altas a câmera recorta um pouco mais (o 3D e a pintura continuam alinhados).
    ///  - Chão: plano invisível que só recebe a sombra dos soldados e uma sombra de contato sob os pés.
    ///  - Luz: sol baixo atrás da cena (sombras vindo para a câmera, como na pintura), preenchimento pela frente,
    ///    ambiente quente; pós-processamento próprio sem "grade" de cor (a pintura aparece com as cores originais).
    ///  - Vida: poeira dourada brilhando na luz (precisa dos arquivos de VFX).
    ///  - O palco tem o próprio sol (a Fazenda é destruída ao voltar para a Base, junto com a luz dela).
    /// Sem a placa ou os shaders, Create devolve null e o lobby aparece sem o palco 3D.
    ///
    /// Calibração: Resources/Base/BaseBackdrop_calib (texto "cx cy f pitch camHeight camDist larguraDaPlaca camX"), em pixels da placa.
    /// Sem esse arquivo, supõe que a placa tem o enquadramento da imagem de referência.
    /// </summary>
    public sealed class BaseStage : MonoBehaviour
    {
        /* ======================= calibração (imagem de referência 1672×941) ======================= */

        /// <summary>Foco em pixels da referência (campo de visão vertical de 50°) e inclinação da câmera para baixo.</summary>
        const float RefHeight = 941f, RefFocal = 1009f, DefaultPitch = 23.1f;
        /// <summary>Câmera: altura (m) e distância horizontal (m) até a linha dos pés do Tank e do Healer.</summary>
        const float DefaultCamHeight = 3.72f, DefaultCamDist = 5.56f;
        /// <summary>Distância do quadrado do fundo até a câmera (só precisa estar dentro do plano de corte).</summary>
        const float BackdropDistance = 60f;

        /// <summary>Posição de cada slot da party no palco (slot 1 à esquerda na tela; o do meio um pouco atrás).</summary>
        public static readonly Vector3[] SlotPositions = { new Vector3(1.6f, 0f, 0f), new Vector3(0f, 0f, -0.4f), new Vector3(-1.6f, 0f, 0f) };
        public static readonly float[] SlotYaw = { -12f, 0f, 12f };
        /// <summary>Direção em que a luz do sol viaja (placa v1: sol baixo atrás, à direita na tela → sombras para a câmera e para a esquerda).</summary>
        static readonly Vector3 SunTravel = new Vector3(0.12f, -0.47f, 1f);

        /* ======================= estado ======================= */

        Texture2D plate;
        float cx, cy, focal, pitch, camHeight, camDist, camX;
        Transform backdrop, catcher; Material backdropMat, catcherMat;
        Light fillLight, sun; Volume look; ParticleSystem dust;
        bool visible;
        sealed class Saved
        {
            public bool Fog; public AmbientMode Ambient; public Color AmbSky, AmbEq, AmbGround, Bg;
            public float Fov; public CameraClearFlags Clear;
        }
        Saved saved;
        readonly Vector4[] feet = new Vector4[4];
        static readonly int IdFeet = Shader.PropertyToID("_Feet"), IdCenter = Shader.PropertyToID("_StageCenter");

        /// <summary>true se a placa pintada existe (sem ela o jogo usa a Base antiga).</summary>
        public static bool Available { get { return Resources.Load<Texture2D>("Base/BaseBackdrop") != null; } }
        public bool Visible { get { return visible; } }

        public static BaseStage Create(Transform parent)
        {
            var tex = Resources.Load<Texture2D>("Base/BaseBackdrop");
            if (tex == null) return null;
            var shBack = Resources.Load<Shader>("Base/Shaders/BaseBackdrop"); if (shBack == null) shBack = Shader.Find("Space/Base/Backdrop");
            var shCatch = Resources.Load<Shader>("Base/Shaders/BaseShadowCatcher"); if (shCatch == null) shCatch = Shader.Find("Space/Base/ShadowCatcher");
            if (shBack == null || shCatch == null) { Debug.LogWarning("[VampWolfs] Shaders do palco não encontrados (Resources/Base/Shaders) — lobby sem palco 3D."); return null; }
            var go = new GameObject("Palco da Base"); go.transform.SetParent(parent, false);
            var s = go.AddComponent<BaseStage>(); s.plate = tex; s.Build(shBack, shCatch);
            return s;
        }

        void ReadCalibration()
        {
            float W = plate.width, H = plate.height;
            // padrão: a placa tem o enquadramento da referência (mesma proporção vertical), centrada
            focal = RefFocal * H / RefHeight; cx = W * 0.5f; cy = H * 0.5f;
            pitch = DefaultPitch; camHeight = DefaultCamHeight; camDist = DefaultCamDist; camX = 0f;
            var cal = Resources.Load<TextAsset>("Base/BaseBackdrop_calib");
            if (cal == null) return;
            var parts = cal.text.Split(new[] { ' ', '\n', '\r', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            var v = new List<float>();
            foreach (var p in parts) { float f; if (float.TryParse(p, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) v.Add(f); }
            if (v.Count >= 3) { cx = v[0]; cy = v[1]; focal = v[2]; }
            if (v.Count >= 4) pitch = v[3];
            if (v.Count >= 6) { camHeight = v[4]; camDist = v[5]; }
            // 7º valor = largura da placa quando a calibração foi feita: se o Unity importou menor, a calibração acompanha
            if (v.Count >= 7 && v[6] > 0f && Mathf.Abs(v[6] - W) > 0.5f) { float k = W / v[6]; cx *= k; cy *= k; focal *= k; }
            // 8º valor = deslocamento lateral da câmera (m): o centro da party fica onde a pintura pede (ex.: no círculo do chão)
            if (v.Count >= 8) camX = v[7];
        }

        void Build(Shader shBack, Shader shCatch)
        {
            ReadCalibration();
            // fundo pintado
            var b = new GameObject("Fundo pintado");
            b.AddComponent<MeshFilter>().sharedMesh = QuadXY();
            var br = b.AddComponent<MeshRenderer>(); br.shadowCastingMode = ShadowCastingMode.Off; br.receiveShadows = false;
            backdropMat = new Material(shBack) { name = "Base: fundo pintado" }; backdropMat.SetTexture("_MainTex", plate);
            br.sharedMaterial = backdropMat; backdrop = b.transform; b.SetActive(false);
            // chão que só recebe sombra
            var c = new GameObject("Chão (sombra)"); c.transform.SetParent(transform, false);
            c.AddComponent<MeshFilter>().sharedMesh = QuadXZ();
            var cr = c.AddComponent<MeshRenderer>(); cr.shadowCastingMode = ShadowCastingMode.Off; cr.receiveShadows = true;
            catcherMat = new Material(shCatch) { name = "Base: chão de sombra" };
            cr.sharedMaterial = catcherMat; catcher = c.transform;
            catcher.localPosition = new Vector3(0f, 0.003f, 0f); catcher.localScale = new Vector3(24f, 1f, 24f);
            catcherMat.SetVector(IdCenter, new Vector4(0f, -0.2f, 0f, 0f));
            // luz de preenchimento pela frente (sem sombra) — a principal é o sol da cena
            var f = new GameObject("Luz de preenchimento (Base)"); f.transform.SetParent(transform, false);
            fillLight = f.AddComponent<Light>(); fillLight.type = LightType.Directional; fillLight.shadows = LightShadows.None;
            fillLight.cullingMask &= ~(1 << PortraitStudio.Layer);
            fillLight.color = new Color(0.95f, 0.78f, 0.66f); fillLight.intensity = 0.55f;
            f.transform.rotation = Quaternion.LookRotation(new Vector3(0.25f, -0.55f, -1f));   // vem do lado da câmera, de cima
            // pós-processamento da Base: sem tom/contraste (a pintura fica como foi feita), brilho só no que é muito claro
            var prof = ScriptableObject.CreateInstance<VolumeProfile>(); prof.name = "Base (runtime)";
            var tm = prof.Add<Tonemapping>(false); tm.mode.Override(TonemappingMode.None);
            var bl = prof.Add<Bloom>(false); bl.threshold.Override(0.95f); bl.intensity.Override(0.35f); bl.scatter.Override(0.65f);
            var vg = prof.Add<Vignette>(false); vg.intensity.Override(0.2f); vg.smoothness.Override(0.5f);
            var lv = new GameObject("Volume da Base"); lv.transform.SetParent(transform, false);
            look = lv.AddComponent<Volume>(); look.isGlobal = true; look.priority = 15f; look.weight = 0f; look.sharedProfile = prof;
            // sol do palco: baixo, atrás da cena (sombras vindo para a câmera, como na pintura)
            var sg = new GameObject("Sol (Base)"); sg.transform.SetParent(transform, false);
            sun = sg.AddComponent<Light>(); sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.76f, 0.52f); sun.intensity = 2.1f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = 1f;
            sun.cullingMask &= ~(1 << PortraitStudio.Layer);
            sg.transform.rotation = Quaternion.LookRotation(SunTravel);
            BuildDust();
            gameObject.SetActive(false);
        }

        /// <summary>Poeira dourada flutuando na luz, entre a câmera e o vampiro (do Space; precisa dos arquivos de VFX).</summary>
        void BuildDust()
        {
            if (!Vfx.VfxAssets.Load()) return;
            var go = new GameObject("Poeira no ar (Base)"); go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.6f, 1.2f);
            dust = go.AddComponent<ParticleSystem>();
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main; main.loop = true; main.playOnAwake = false; main.maxParticles = 140;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f); main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.78f, 0.45f, 0.55f));
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.prewarm = true;
            var em = dust.emission; em.rateOverTime = new ParticleSystem.MinMaxCurve(16f);
            var sh = dust.shape; sh.enabled = true; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(9f, 3.2f, 5f);
            var no = dust.noise; no.enabled = true; no.strength = new ParticleSystem.MinMaxCurve(0.12f); no.frequency = 0.35f; no.scrollSpeed = new ParticleSystem.MinMaxCurve(0.1f);
            var col = dust.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard; r.sharedMaterial = Vfx.VfxAssets.AdditiveMat("base.dust", Vfx.VfxAssets.Glow, 1.6f);
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        }

        /* ======================= ligar / desligar ======================= */

        /// <summary>Liga o palco (na Base) ou desliga e devolve luz, ambiente e câmera como estavam.</summary>
        public void Show(bool on, Camera cam)
        {
            if (on == visible) return; visible = on;
            gameObject.SetActive(on);
            if (backdrop != null) backdrop.gameObject.SetActive(on);
            if (on)
            {
                saved = new Saved
                {
                    Fog = RenderSettings.fog, Ambient = RenderSettings.ambientMode, AmbSky = RenderSettings.ambientSkyColor,
                    AmbEq = RenderSettings.ambientEquatorColor, AmbGround = RenderSettings.ambientGroundColor,
                    Bg = cam != null ? cam.backgroundColor : Color.black, Fov = cam != null ? cam.fieldOfView : 60f,
                    Clear = cam != null ? cam.clearFlags : CameraClearFlags.SolidColor,
                };
                RenderSettings.fog = false;
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(0.62f, 0.5f, 0.42f);
                RenderSettings.ambientEquatorColor = new Color(0.46f, 0.33f, 0.24f);
                RenderSettings.ambientGroundColor = new Color(0.2f, 0.14f, 0.1f);
                if (cam != null)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
                    var data = cam.GetUniversalAdditionalCameraData(); if (data != null) data.renderPostProcessing = true;   // bloom/vinheta da Base
                }
                if (look != null) look.weight = 1f;
                if (dust != null) dust.Play();
            }
            else if (saved != null)
            {
                RenderSettings.fog = saved.Fog; RenderSettings.ambientMode = saved.Ambient; RenderSettings.ambientSkyColor = saved.AmbSky;
                RenderSettings.ambientEquatorColor = saved.AmbEq; RenderSettings.ambientGroundColor = saved.AmbGround;
                if (cam != null) { cam.fieldOfView = saved.Fov; cam.clearFlags = saved.Clear; cam.backgroundColor = saved.Bg; }
                if (look != null) look.weight = 0f;
                if (dust != null) dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /* ======================= câmera e fundo (todo quadro na Base) ======================= */

        /// <summary>Põe a câmera na pose calibrada e encaixa a pintura na tela (chamar todo quadro na Base).</summary>
        public void ApplyCamera(Camera cam, Vector3 shake)
        {
            if (cam == null || !visible) return;
            float W = plate.width, H = plate.height;
            float aspect = cam.aspect > 0.01f ? cam.aspect : 16f / 9f;
            // quanto da placa cabe a partir do ponto principal (cx, cy) sem mostrar borda
            float maxT = Mathf.Min(Mathf.Min(cy, H - cy) / focal, Mathf.Min(cx, W - cx) / (aspect * focal));
            float refT = RefHeight * 0.5f / RefFocal;   // enquadramento da referência (50° na vertical)
            float t = Mathf.Min(refT, maxT * 0.995f);
            cam.fieldOfView = 2f * Mathf.Atan(t) * Mathf.Rad2Deg;
            cam.transform.position = new Vector3(camX, camHeight, camDist) + shake;
            cam.transform.rotation = Quaternion.Euler(pitch, 180f, 0f);
            if (cam.farClipPlane < BackdropDistance * 1.2f) cam.farClipPlane = BackdropDistance * 1.2f;
            // a pintura fica fixa no espaço da câmera: pixel (u, v) → direção ((u − cx)/f, −(v − cy)/f, 1)
            if (backdrop.parent != cam.transform) backdrop.SetParent(cam.transform, false);
            float d = BackdropDistance;
            backdrop.localRotation = Quaternion.identity;
            backdrop.localScale = new Vector3(W * d / focal, H * d / focal, 1f);
            backdrop.localPosition = new Vector3((W * 0.5f - cx) * d / focal, -(H * 0.5f - cy) * d / focal, d);
        }

        /// <summary>Atualiza a sombra de contato sob os pés (posições no chão dos soldados da Base).</summary>
        public void SetFeet(IList<Vector3> positions)
        {
            for (int i = 0; i < feet.Length; i++)
                feet[i] = positions != null && i < positions.Count ? new Vector4(positions[i].x, positions[i].z, 0.55f, 0.7f) : Vector4.zero;
            if (catcherMat != null) catcherMat.SetVectorArray(IdFeet, feet);
        }

        /* ======================= malhas (no Space vinham de VfxMeshes) ======================= */

        static Mesh quadXY, quadXZ;

        /// <summary>Quadrado 1×1 de pé (plano XY, virado para −Z), centrado na origem: o fundo pintado.</summary>
        static Mesh QuadXY()
        {
            if (quadXY != null) return quadXY;
            quadXY = Quad("Quad XY", new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) }, Vector3.back, new[] { 0, 2, 1, 0, 3, 2 });
            return quadXY;
        }

        /// <summary>Quadrado 1×1 deitado (plano XZ, virado para cima), centrado na origem: o chão de sombra.</summary>
        static Mesh QuadXZ()
        {
            if (quadXZ != null) return quadXZ;
            quadXZ = Quad("Quad XZ", new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) }, Vector3.up, new[] { 0, 1, 2, 0, 2, 3 });
            return quadXZ;
        }

        static Mesh Quad(string name, Vector3[] v, Vector3 normal, int[] tris)
        {
            var m = new Mesh { name = name };
            m.vertices = v;
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            m.normals = new[] { normal, normal, normal, normal };
            m.triangles = tris;
            m.RecalculateBounds();
            return m;
        }

        void OnDestroy()
        {
            if (backdrop != null) Destroy(backdrop.gameObject);
            if (backdropMat != null) Destroy(backdropMat);
            if (catcherMat != null) Destroy(catcherMat);
        }
    }
}
