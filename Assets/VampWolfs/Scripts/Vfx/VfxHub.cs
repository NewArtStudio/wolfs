using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// Ponto único de acesso aos efeitos da partida. O GameBootstrap cria um VfxSystem por partida (some com o mundo).
    /// Sem os shaders/texturas em Resources/VFX, Ready = false e o jogo usa os efeitos simples antigos (Effects).
    /// Também monta o "visual" da partida: bloom (o brilho dos efeitos), vinheta e tonemapping neutro.
    /// </summary>
    public static class VfxHub
    {
        /// <summary>Motor de partículas da partida atual (null fora da partida ou sem os arquivos de VFX).</summary>
        public static VfxSystem Fx { get; private set; }

        public static bool Ready { get { return Fx != null; } }

        /// <summary>Cria os efeitos da partida e liga o pós-processamento na câmera.</summary>
        public static void CreateForMatch(Transform worldRoot, Camera cam)
        {
            Fx = VfxSystem.Create(worldRoot);
            BuildLook(worldRoot, cam);
            if (Fx != null) worldRoot.gameObject.AddComponent<AmbientFx>();
        }

        /// <summary>Tremor de câmera (0..0,6). Nada acontece sem VFX.</summary>
        public static void Shake(float amount, bool force = false)
        {
            if (Fx != null) Fx.AddShake(amount, force);
        }

        public static Vector3 ShakeOffset()
        {
            return Fx != null ? Fx.ShakeOffset() : Vector3.zero;
        }

        /// <summary>Bloom + vinheta arroxeada + tonemapping neutro, só na câmera da partida.</summary>
        static VolumeProfile profile;   // um só para todas as partidas

        static void BuildLook(Transform worldRoot, Camera cam)
        {
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "Partida (runtime)";
                var tm = profile.Add<Tonemapping>(false); tm.mode.Override(TonemappingMode.Neutral);
                var bl = profile.Add<Bloom>(false); bl.threshold.Override(1.0f); bl.intensity.Override(0.9f); bl.scatter.Override(0.68f);
                var vg = profile.Add<Vignette>(false); vg.intensity.Override(0.3f); vg.smoothness.Override(0.45f);
            }
            var prof = profile;
            var go = new GameObject("Volume da partida"); go.transform.SetParent(worldRoot, false);
            var v = go.AddComponent<Volume>(); v.isGlobal = true; v.priority = 10f; v.sharedProfile = prof;
            if (cam != null)
            {
                cam.allowHDR = true;
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) data.renderPostProcessing = true;
            }
        }
    }
}
