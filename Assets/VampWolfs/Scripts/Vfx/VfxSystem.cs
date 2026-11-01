using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VampWolfs.Vfx
{
    /// <summary>
    /// Motor dos efeitos visuais, copiado do projeto Space (VampWolfs: anda sozinho no Update, em tempo de jogo;
    /// tremor com números fixos no lugar do ImpactSettings; novo emissor de morcegos). Tudo feito por código, sem prefabs:
    ///  - 11 sistemas de partículas compartilhados (faíscas, brilho, fogo, fumaça, cruzes, gelo, estilhaços…):
    ///    cada efeito só "emite" partículas neles (barato e fácil de manter);
    ///  - objetos reaproveitáveis (anéis e cones no chão, feixes, manchas, espinhos de gelo, escudos);
    ///  - luzes rápidas (clarões) e tremor de câmera.
    /// Anda no tempo da simulação: pausa e ×2/×4 valem para os efeitos também.
    /// Só MOSTRA o que a simulação já decidiu (VfxCatalog lê os eventos, VfxWorld lê o estado).
    /// </summary>
    public sealed class VfxSystem : MonoBehaviour
    {
        /* ======================= tipos ======================= */

        /// <summary>Um sistema de partículas compartilhado (emite partícula por partícula com posição/velocidade/cor).</summary>
        public sealed class Emitter
        {
            public readonly ParticleSystem Ps; readonly bool mesh3D; ParticleSystem.EmitParams ep;
            public Emitter(ParticleSystem ps, bool mesh3D) { Ps = ps; this.mesh3D = mesh3D; ep = new ParticleSystem.EmitParams(); ep.applyShapeToPosition = false; }
            public void Emit(Vector3 pos, Vector3 vel, float size, float life, Color col, float rot = -1f, float spin = 0f)
            {
                ep.position = pos; ep.velocity = vel; ep.startSize = size * SizeScale; ep.startLifetime = life; ep.startColor = col;
                if (mesh3D) { ep.rotation3D = new Vector3(UnityEngine.Random.value * 360f, UnityEngine.Random.value * 360f, UnityEngine.Random.value * 360f); ep.angularVelocity3D = UnityEngine.Random.insideUnitSphere * spin; }
                else { ep.rotation = rot < 0f ? UnityEngine.Random.value * 360f : rot; ep.angularVelocity = spin * (UnityEngine.Random.value < 0.5f ? -1f : 1f); }
                Ps.Emit(ep, 1);
            }
        }

        /// <summary>Objeto de malha reaproveitável (anel, cone, feixe, mancha, espinho, escudo…).</summary>
        public sealed class FxObj
        {
            public string Kind; public GameObject Go; public Transform T; public MeshRenderer R; public MaterialPropertyBlock Mpb;
            public void Apply() { R.SetPropertyBlock(Mpb); }
        }

        public delegate void FxFn(Fx f, float k);

        /// <summary>Efeito temporário animado (vive Life segundos de simulação).</summary>
        public sealed class Fx
        {
            public FxObj O; public float T, Life; public FxFn Fn;
            public Vector3 A, B, P; public Color C, C2; public float F0, F1, F2, F3;
        }

        /// <summary>Visual persistente ligado a algo da simulação (projétil, zona, escudo…), mantido quadro a quadro.</summary>
        public sealed class Held
        {
            public FxObj O; public int Seen; public float Age, Acc, Acc2, Flash; public Vector3 Last; public bool Has; public object Tag;
        }

        sealed class LightFx { public Light L; public float T, Life, I0; public bool On; }
        sealed class Timed { public float At; public Action Fn; }

        /* ======================= ajustes rápidos ======================= */

        /// <summary>Multiplicador global do tamanho das partículas (aumente se ficarem pequenas na câmera do jogo).</summary>
        public static float SizeScale = 1.15f;

        /* ======================= estado ======================= */

        public static VfxSystem Create(Transform parent)
        {
            if (!VfxAssets.Load()) return null;
            var go = new GameObject("VFX"); go.transform.SetParent(parent, false);
            var v = go.AddComponent<VfxSystem>(); v.Init(); return v;
        }

        public Emitter Sparks, Glows, Flashes, Flares, Embers, Fire, Smoke, Pluses, Shards, Slashes, Debris, Ice, Bats;

        /// <summary>Tremor (VampWolfs: fixos; no Space vinham do ImpactSettings).</summary>
        const float ShakeStrength = 1f, BigShake = 0.3f, ShakeGap = 0.12f;

        /// <summary>true = avança sozinho no Update com Time.deltaTime (no Space a simulação chamava Tick).</summary>
        public bool AutoTick = true;
        void Update() { if (AutoTick) Tick(Time.deltaTime); }

        /// <summary>Pega um objeto reaproveitável (anel, feixe, brilho…) para um visual mantido por outro script.</summary>
        public FxObj Acquire(string kind) { return Get(kind); }
        /// <summary>Devolve um objeto pego com Acquire.</summary>
        public void Return(FxObj o) { Release(o); }
        readonly List<Emitter> all = new List<Emitter>();
        readonly Dictionary<string, List<FxObj>> free = new Dictionary<string, List<FxObj>>();
        readonly List<Fx> active = new List<Fx>();
        readonly Stack<Fx> fxPool = new Stack<Fx>();
        readonly Dictionary<(object, int), Held> held = new Dictionary<(object, int), Held>();
        readonly List<(object, int)> drop = new List<(object, int)>();
        readonly List<LightFx> lights = new List<LightFx>();
        readonly List<Timed> timeline = new List<Timed>();
        Transform ground; Camera cam;
        int frame; float simClock, shake, shakeT, lastDt;

        /// <summary>Tempo de simulação acumulado pelos efeitos (segundos).</summary>
        public float Clock { get { return simClock; } }
        public Camera Cam { get { if (cam == null) cam = Camera.main; return cam; } }

        public static Color Hex(int h, float k = 1f) { return new Color(((h >> 16) & 255) / 255f * k, ((h >> 8) & 255) / 255f * k, (h & 255) / 255f * k, 1f); }
        public static float Rnd(float a, float b) { return UnityEngine.Random.Range(a, b); }
        static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        /* ======================= montagem ======================= */

        void Init()
        {
            ground = new GameObject("VFX chão").transform; ground.SetParent(transform, false); ground.position = new Vector3(0, 0.03f, 0);
            var A = VfxAssets.Additive;
            Sparks = Make("Faíscas", VfxAssets.AdditiveMat("p.spark", VfxAssets.Spark, 3.5f), ParticleSystemRenderMode.Stretch, 1.1f, Fade(1f, 0.6f), Curve(1f, 0.15f), 1500, drag: 1.2f);
            Glows = Make("Brilho", VfxAssets.AdditiveMat("p.glow", VfxAssets.Glow, 2.2f), ParticleSystemRenderMode.Billboard, 0f, FadeInOut(0.12f), Curve(1f, 0.7f), 1500, noise: 0.25f);
            Flashes = Make("Clarão", VfxAssets.AdditiveMat("p.flash", VfxAssets.Glow, 2.6f), ParticleSystemRenderMode.Billboard, 0f, Fade(1f, 0f), Curve(0.7f, 1.25f), 200);
            Flares = Make("Estrela", VfxAssets.AdditiveMat("p.flare", VfxAssets.Flare, 3f), ParticleSystemRenderMode.Billboard, 0f, Fade(1f, 0f), Curve(1f, 0.5f), 200);
            Embers = Make("Brasas", VfxAssets.AdditiveMat("p.ember", VfxAssets.Spark, 3.2f), ParticleSystemRenderMode.Billboard, -0.12f, Fade(1f, 0.7f), Curve(1f, 0.3f), 1500, noise: 0.9f, drag: 0.6f);
            Fire = Make("Fogo", VfxAssets.AdditiveMat("p.fire", VfxAssets.Fire, 1.5f), ParticleSystemRenderMode.Billboard, -0.25f, FadeInOut(0.08f), Curve(0.7f, 1.35f), 1500, sheet: 4, drag: 1.2f);
            Smoke = Make("Fumaça", VfxAssets.AlphaMat("p.smoke", VfxAssets.Smoke, false, 0.85f), ParticleSystemRenderMode.Billboard, -0.03f, FadeInOut(0.15f), Curve(0.55f, 1.8f), 1200, sheet: 2, drag: 2.2f);
            Pluses = Make("Cruzes", VfxAssets.AdditiveMat("p.plus", VfxAssets.Plus, 2.4f), ParticleSystemRenderMode.Billboard, -0.25f, FadeInOut(0.1f), Curve(1f, 0.6f), 500);
            Shards = Make("Cristais", VfxAssets.AdditiveMat("p.shard", VfxAssets.Shard, 2.6f), ParticleSystemRenderMode.Billboard, 0.6f, Fade(1f, 0.5f), Curve(1f, 0.4f), 800, drag: 0.8f);
            Slashes = Make("Golpes", VfxAssets.AdditiveMat("p.slash", VfxAssets.Slash, 3.5f), ParticleSystemRenderMode.Billboard, 0f, Fade(1f, 0.4f), Curve(0.75f, 1.3f), 200);
            Debris = Make("Estilhaços", VfxAssets.LitMat("p.debris", Color.white, Color.black), ParticleSystemRenderMode.Mesh, 2.4f, null, Shrink(), 600, mesh: true);
            Ice = Make("Gelo", VfxAssets.FresnelMat("p.ice", VfxAssets.Noise, Vector2.one, Vector2.zero, false, 0.35f), ParticleSystemRenderMode.Mesh, 2.0f, null, Shrink(), 600, mesh: true);
            // VampWolfs: morcegos (folha 2×2 batendo as asas, cor escura com transparência normal)
            if (VfxAssets.Bat != null) Bats = Make("Morcegos", VfxAssets.AlphaMat("p.bat", VfxAssets.Bat, false, 0.6f), ParticleSystemRenderMode.Billboard, 0f, FadeInOut(0.08f), Curve(0.8f, 1f), 400, noise: 0.6f, sheet: 2, flap: true);
            if (A == null) enabled = false;
        }

        static Gradient Fade(float a0, float mid)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(mid, 0.55f), new GradientAlphaKey(0f, 1f) });
            return g;
        }
        static Gradient FadeInOut(float inT)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, inT), new GradientAlphaKey(0.75f, 0.6f), new GradientAlphaKey(0f, 1f) });
            return g;
        }
        static AnimationCurve Curve(float a, float b) { return new AnimationCurve(new Keyframe(0f, a), new Keyframe(1f, b)); }
        static AnimationCurve Shrink() { return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f)); }

        Emitter Make(string name, Material mat, ParticleSystemRenderMode mode, float gravity, Gradient color, AnimationCurve size, int max,
                     float noise = 0f, float drag = 0f, int sheet = 0, bool mesh = false, bool flap = false)
        {
            var go = new GameObject("VFX " + name); go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max; main.gravityModifier = new ParticleSystem.MinMaxCurve(gravity);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f); main.startLifetime = new ParticleSystem.MinMaxCurve(1f); main.startSize = new ParticleSystem.MinMaxCurve(1f);
            main.scalingMode = ParticleSystemScalingMode.Hierarchy; main.startRotation3D = mesh;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            if (color != null) { var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(color); }
            if (size != null) { var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, size); }
            if (noise > 0f) { var n = ps.noise; n.enabled = true; n.strength = new ParticleSystem.MinMaxCurve(noise); n.frequency = 0.9f; n.scrollSpeed = new ParticleSystem.MinMaxCurve(0.6f); n.damping = true; }
            if (drag > 0f) { var lv = ps.limitVelocityOverLifetime; lv.enabled = true; lv.drag = new ParticleSystem.MinMaxCurve(drag); lv.limit = new ParticleSystem.MinMaxCurve(100f); }
            if (sheet > 0)
            {
                var ts = ps.textureSheetAnimation; ts.enabled = true; ts.mode = ParticleSystemAnimationMode.Grid; ts.numTilesX = sheet; ts.numTilesY = sheet;
                // fogo: anima os 16 quadros ao longo da vida; fumaça: um quadro sorteado por partícula
                ts.frameOverTime = sheet == 4 || flap ? new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f)) : new ParticleSystem.MinMaxCurve(0f, 0.999f);
                ts.cycleCount = flap ? 8 : 1;   // morcegos: batem as asas várias vezes na vida
            }
            if (mesh)
            {
                var c = ps.collision; c.enabled = true; c.type = ParticleSystemCollisionType.Planes; c.SetPlane(0, ground);
                c.bounce = new ParticleSystem.MinMaxCurve(0.3f); c.dampen = new ParticleSystem.MinMaxCurve(0.55f); c.lifetimeLoss = new ParticleSystem.MinMaxCurve(0f); c.radiusScale = 0.4f;
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = mode; r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.maxParticleSize = 3f;
            if (mode == ParticleSystemRenderMode.Stretch) { r.velocityScale = 0.045f; r.lengthScale = 1.4f; }
            if (mesh) { var ms = VfxMeshes.Chunks; r.SetMeshes(ms, ms.Length); }
            ps.Play();
            var e = new Emitter(ps, mesh); all.Add(e); return e;
        }

        /* ======================= objetos reaproveitáveis ======================= */

        FxObj NewObj(string kind)
        {
            Mesh mesh; Material mat;
            switch (kind)
            {
                case "ground": mesh = VfxMeshes.QuadXZ; mat = VfxAssets.GroundMat; break;
                case "groundtop": mesh = VfxMeshes.QuadXZ; mat = VfxAssets.GroundTopMat; break;
                case "decal": mesh = VfxMeshes.QuadXZ; mat = VfxAssets.AlphaMat("decal", VfxAssets.Scorch, true, 0.9f); break;
                case "beam": mesh = VfxMeshes.Beam; mat = VfxAssets.AdditiveMat("beam", VfxAssets.BeamTex, 2.6f, 1f); break;
                case "streak": mesh = VfxMeshes.Beam; mat = VfxAssets.AdditiveMat("streak", VfxAssets.Glow, 3f); break;
                case "glow": mesh = VfxMeshes.QuadXY; mat = VfxAssets.AdditiveMat("glowq", VfxAssets.Glow, 2.5f); break;
                case "flare": mesh = VfxMeshes.QuadXY; mat = VfxAssets.AdditiveMat("flareq", VfxAssets.Flare, 2.5f); break;
                case "flag": mesh = VfxMeshes.Flag; mat = VfxAssets.AdditiveMat("flag", VfxAssets.Emblem, 2f); break;
                case "shield": mesh = VfxMeshes.Sphere; mat = VfxAssets.FresnelMat("shield", VfxAssets.Hex, new Vector2(2f, 1.5f), new Vector2(0f, 0.04f), false); break;
                case "spike": mesh = VfxMeshes.IceSpike; mat = VfxAssets.FresnelMat("spike", VfxAssets.Noise, new Vector2(0.6f, 0.6f), Vector2.zero, true, 0.28f); break;
                case "pole": mesh = VfxMeshes.Pole; mat = VfxAssets.LitMat("pole", new Color(0.22f, 0.24f, 0.28f), Color.black); break;
                case "rock": mesh = VfxMeshes.Chunks[1]; mat = VfxAssets.LitMat("meteor", new Color(0.25f, 0.2f, 0.18f), new Color(2.4f, 0.7f, 0.15f)); break;
                default: throw new ArgumentException("tipo de VFX desconhecido: " + kind);
            }
            var go = new GameObject("VFX " + kind); go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return new FxObj { Kind = kind, Go = go, T = go.transform, R = r, Mpb = new MaterialPropertyBlock() };
        }

        FxObj Get(string kind)
        {
            List<FxObj> l; FxObj o;
            if (free.TryGetValue(kind, out l) && l.Count > 0) { o = l[l.Count - 1]; l.RemoveAt(l.Count - 1); }
            else o = NewObj(kind);
            o.Mpb.Clear(); o.T.localScale = Vector3.one; o.T.rotation = Quaternion.identity; o.Go.SetActive(true);
            return o;
        }

        void Release(FxObj o)
        {
            if (o == null || o.Go == null) return;
            o.Go.SetActive(false);
            List<FxObj> l; if (!free.TryGetValue(o.Kind, out l)) free[o.Kind] = l = new List<FxObj>();
            l.Add(o);
        }

        /// <summary>Cria um efeito temporário de malha; fn é chamada todo quadro com k = 0..1.</summary>
        public Fx Spawn(string kind, float life, FxFn fn)
        {
            var f = fxPool.Count > 0 ? fxPool.Pop() : new Fx();
            f.O = Get(kind); f.T = 0f; f.Life = Mathf.Max(0.01f, life); f.Fn = fn;
            f.A = f.B = f.P = Vector3.zero; f.C = f.C2 = Color.white; f.F0 = f.F1 = f.F2 = f.F3 = 0f;
            active.Add(f);
            return f;
        }
        /// <summary>Depois de preencher os campos do Fx, desenha o primeiro quadro (evita piscar na origem).</summary>
        public void Begin(Fx f) { f.Fn(f, 0f); }

        /* ---------- visuais persistentes (chave = objeto da simulação + "slot") ---------- */

        public void BeginFrame() { frame++; }

        public Held Hold(object key, int slot, string kind)
        {
            Held h;
            if (!held.TryGetValue((key, slot), out h)) { h = new Held { O = kind != null ? Get(kind) : null }; held[(key, slot)] = h; }
            else if (h.Seen != frame) h.Age += lastDt;   // idade em tempo de simulação (para animar a entrada)
            h.Seen = frame; return h;
        }
        public bool Has(object key, int slot) { return held.ContainsKey((key, slot)); }

        /// <summary>Solta o que não foi visto neste quadro. onGone recebe o visual que acabou (para um efeito de saída).</summary>
        public void EndFrame(Action<object, int, Held> onGone)
        {
            drop.Clear();
            foreach (var kv in held) if (kv.Value.Seen != frame) drop.Add(kv.Key);
            foreach (var k in drop)
            {
                var h = held[k]; held.Remove(k);
                if (onGone != null) onGone(k.Item1, k.Item2, h);
                if (h.O != null) Release(h.O);
            }
        }

        /* ======================= laço ======================= */

        /// <summary>Avança os efeitos. simDt = passo da simulação neste quadro (0 em pausa).</summary>
        public void Tick(float simDt)
        {
            float realDt = Time.deltaTime;
            float rate = realDt > 1e-5f ? Mathf.Clamp(simDt / realDt, 0f, 8f) : 0f;
            foreach (var e in all) { var m = e.Ps.main; m.simulationSpeed = rate; }
            simClock += simDt; lastDt = simDt;
            for (int i = timeline.Count - 1; i >= 0; i--)
                if (timeline[i].At <= simClock) { var fn = timeline[i].Fn; timeline.RemoveAt(i); fn(); }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var f = active[i]; f.T += simDt;
                float k = Mathf.Clamp01(f.T / f.Life);
                if (f.O != null && f.O.Go != null) f.Fn(f, k);
                if (f.T >= f.Life) { Release(f.O); f.O = null; active.RemoveAt(i); fxPool.Push(f); }
            }
            foreach (var l in lights)
            {
                if (!l.On) continue;
                l.T += simDt; float k = Mathf.Clamp01(l.T / l.Life);
                l.L.intensity = l.I0 * (1f - k) * (1f - k);
                if (k >= 1f) { l.On = false; l.L.enabled = false; }
            }
            shake = Mathf.Max(0f, shake - realDt * 1.6f); shakeT += realDt;
        }

        public void After(float delay, Action fn) { timeline.Add(new Timed { At = simClock + delay, Fn = fn }); }

        public void Clear()
        {
            foreach (var e in all) e.Ps.Clear();
            for (int i = active.Count - 1; i >= 0; i--) { Release(active[i].O); fxPool.Push(active[i]); }
            active.Clear();
            foreach (var h in held.Values) if (h.O != null) Release(h.O);
            held.Clear(); timeline.Clear();
            foreach (var l in lights) { l.On = false; if (l.L != null) l.L.enabled = false; }
            shake = 0f;
        }

        /* ======================= câmera ======================= */

        float lastShakeAt = -99f;
        /// <summary>
        /// Pede um tremor de tamanho s (0..0,6). Tremores pequenos respeitam um intervalo mínimo entre si (ImpactSettings.ShakeGap),
        /// os grandes (≥ BigShake) e os "force" (Ultimate) sempre passam. Tudo escala pela "Força do tremor" escolhida.
        /// </summary>
        public void AddShake(float s, bool force = false)
        {
            s *= ShakeStrength;
            if (s <= 0.001f) return;
            float nowT = Time.unscaledTime;
            if (!force && s < BigShake && nowT - lastShakeAt < ShakeGap) return;
            lastShakeAt = nowT;
            shake = Mathf.Min(0.6f, Mathf.Max(shake, s));
        }

        /// <summary>Deslocamento da câmera pelo tremor (somar à posição antes do LookAt).</summary>
        public Vector3 ShakeOffset()
        {
            if (shake <= 0f) return Vector3.zero;
            float t = shakeT * 28f;
            return new Vector3(Mathf.PerlinNoise(t, 0.3f) - 0.5f, (Mathf.PerlinNoise(0.7f, t) - 0.5f) * 0.6f, Mathf.PerlinNoise(t, 5.1f) - 0.5f) * (shake * 1.4f);
        }

        /* ======================= primitivas ======================= */

        /// <summary>Luz pontual rápida (clarão que ilumina chão e modelos).</summary>
        public void FlashLight(Vector3 p, Color c, float intensity, float range, float life)
        {
            LightFx l = null;
            foreach (var x in lights) if (!x.On) { l = x; break; }
            if (l == null)
            {
                if (lights.Count >= 10) { l = lights[0]; foreach (var x in lights) if (x.T / x.Life > l.T / l.Life) l = x; }
                else
                {
                    var go = new GameObject("VFX luz"); go.transform.SetParent(transform, false);
                    var L = go.AddComponent<Light>(); L.type = LightType.Point; L.shadows = LightShadows.None;
                    l = new LightFx { L = L }; lights.Add(l);
                }
            }
            l.L.transform.position = p; l.L.color = c; l.L.range = range; l.I0 = intensity; l.L.intensity = intensity;
            l.T = 0f; l.Life = life; l.On = true; l.L.enabled = true;
        }

        public void SparkBurst(Vector3 p, Color c, int n, float speed, float life = 0.35f, float size = 0.07f, float up = 0.35f)
        {
            for (int i = 0; i < n; i++)
            {
                var d = UnityEngine.Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.8f + up;
                Sparks.Emit(p, d.normalized * speed * Rnd(0.35f, 1f), size * Rnd(0.7f, 1.3f), life * Rnd(0.6f, 1.2f), c);
            }
        }

        /// <summary>Faíscas num leque (direção dir, abertura spread 0..1).</summary>
        public void SparkSpray(Vector3 p, Vector3 dir, Color c, int n, float speed, float spread, float life = 0.3f, float size = 0.07f)
        {
            for (int i = 0; i < n; i++)
            {
                var d = (dir.normalized + UnityEngine.Random.insideUnitSphere * spread).normalized;
                Sparks.Emit(p, d * speed * Rnd(0.5f, 1f), size * Rnd(0.7f, 1.3f), life * Rnd(0.6f, 1.2f), c);
            }
        }

        public void Flash(Vector3 p, Color c, float size, float life = 0.16f, float light = 1f)
        {
            Flashes.Emit(p, Vector3.zero, size, life, c);
            if (light > 0f) FlashLight(p + Vector3.up * 0.4f, c, 6f * light, Mathf.Max(3f, size * 3.5f), life * 2.2f);
        }

        public void Star(Vector3 p, Color c, float size, float life = 0.22f) { Flares.Emit(p, Vector3.zero, size, life, c, Rnd(0f, 90f), 40f); }

        public void Motes(Vector3 p, Color c, int n, float radius, float size, float life, Vector3 vel, float jitter = 0.3f)
        {
            for (int i = 0; i < n; i++)
                Glows.Emit(p + UnityEngine.Random.insideUnitSphere * radius, vel + UnityEngine.Random.insideUnitSphere * jitter, size * Rnd(0.6f, 1.3f), life * Rnd(0.7f, 1.3f), c);
        }

        public void EmberBurst(Vector3 p, Color c, int n, float radius, float speed)
        {
            for (int i = 0; i < n; i++)
            {
                var d = UnityEngine.Random.onUnitSphere; d.y = Mathf.Abs(d.y) + 0.4f;
                Embers.Emit(p + UnityEngine.Random.insideUnitSphere * radius, d.normalized * speed * Rnd(0.3f, 1f), Rnd(0.05f, 0.11f), Rnd(0.6f, 1.4f), c);
            }
        }

        public void FireBurst(Vector3 p, int n, float radius, float size, float speed, float life = 0.6f, float up = 0.6f)
        {
            for (int i = 0; i < n; i++)
            {
                var d = UnityEngine.Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.6f + up;
                Fire.Emit(p + UnityEngine.Random.insideUnitSphere * radius, d.normalized * speed * Rnd(0.3f, 1f), size * Rnd(0.7f, 1.3f), life * Rnd(0.7f, 1.2f), Color.white, -1f, 30f);
            }
        }

        public void SmokeBurst(Vector3 p, Color c, int n, float radius, float size, float speed, float life = 1.4f, float up = 0.5f)
        {
            for (int i = 0; i < n; i++)
            {
                var d = UnityEngine.Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.5f + up;
                Smoke.Emit(p + UnityEngine.Random.insideUnitSphere * radius, d.normalized * speed * Rnd(0.3f, 1f), size * Rnd(0.7f, 1.3f), life * Rnd(0.7f, 1.3f), c, -1f, 25f);
            }
        }

        static readonly Color SandDust = new Color(0.82f, 0.64f, 0.44f, 0.75f);

        /// <summary>Poeira de areia que se espalha rente ao chão.</summary>
        public void Dust(Vector3 p, int n, float radius, float speed = 2.5f, float size = 0.9f, Color? tint = null)
        {
            var c = tint ?? SandDust;
            for (int i = 0; i < n; i++)
            {
                float a = Rnd(0f, Mathf.PI * 2f); var d = new Vector3(Mathf.Cos(a), Rnd(0.05f, 0.35f), Mathf.Sin(a));
                Smoke.Emit(new Vector3(p.x, 0.25f, p.z) + new Vector3(d.x, 0, d.z) * radius * Rnd(0.2f, 1f), d * speed * Rnd(0.4f, 1f), size * Rnd(0.7f, 1.4f), Rnd(0.8f, 1.6f), c, -1f, 20f);
            }
        }

        public void PlusBurst(Vector3 p, Color c, int n, float radius, float rise = 1.4f)
        {
            for (int i = 0; i < n; i++)
                Pluses.Emit(p + new Vector3(Rnd(-radius, radius), Rnd(0f, 0.8f), Rnd(-radius, radius)), new Vector3(0, rise * Rnd(0.6f, 1.2f), 0), Rnd(0.18f, 0.3f), Rnd(0.8f, 1.2f), c, 0f, 0f);
        }

        public void ShardBurst(Vector3 p, Color c, int n, float speed, float size = 0.22f)
        {
            for (int i = 0; i < n; i++)
            {
                var d = UnityEngine.Random.onUnitSphere; d.y = Mathf.Abs(d.y) + 0.2f;
                Shards.Emit(p, d.normalized * speed * Rnd(0.3f, 1f), size * Rnd(0.6f, 1.3f), Rnd(0.4f, 0.8f), c, -1f, 180f);
            }
        }

        public void SlashAt(Vector3 p, Color c, float size, float rotDeg) { Slashes.Emit(p, Vector3.zero, size, 0.2f, c, rotDeg, 0f); }

        /// <summary>VampWolfs: revoada de morcegos saindo de p na direção dir (escuros, com um pouco de vermelho).</summary>
        public void BatSwarm(Vector3 p, Vector3 dir, Color c, int n, float speed, float spread = 0.6f, float life = 0.9f, float size = 0.45f)
        {
            if (Bats == null) { SmokeBurst(p, c, n / 2, 0.4f, size * 1.4f, speed * 0.5f, life); return; }
            for (int i = 0; i < n; i++)
            {
                var d = (dir.normalized + UnityEngine.Random.insideUnitSphere * spread); d.y = Mathf.Abs(d.y) * 0.5f + 0.15f;
                Bats.Emit(p + UnityEngine.Random.insideUnitSphere * 0.4f, d.normalized * speed * Rnd(0.5f, 1.1f), size * Rnd(0.7f, 1.3f), life * Rnd(0.7f, 1.2f), c, 0f, 0f);
            }
        }

        public void DebrisBurst(Vector3 p, Color c, int n, float speed, float size = 0.18f)
        {
            for (int i = 0; i < n; i++)
            {
                var d = UnityEngine.Random.onUnitSphere; d.y = Mathf.Abs(d.y) + 0.5f;
                Debris.Emit(p + UnityEngine.Random.insideUnitSphere * 0.2f, d.normalized * speed * Rnd(0.4f, 1f), size * Rnd(0.5f, 1.4f), Rnd(1.2f, 2.0f), c, -1f, 540f);
            }
        }

        public void IceBurst(Vector3 p, int n, float speed, float size = 0.2f)
        {
            var c = new Color(0.55f, 0.85f, 1f);
            for (int i = 0; i < n; i++)
            {
                var d = UnityEngine.Random.onUnitSphere; d.y = Mathf.Abs(d.y) + 0.4f;
                Ice.Emit(p + UnityEngine.Random.insideUnitSphere * 0.2f, d.normalized * speed * Rnd(0.4f, 1f), size * Rnd(0.5f, 1.4f), Rnd(0.8f, 1.4f), c, -1f, 400f);
            }
        }

        /* ---------- formas no chão ---------- */

        static readonly int IdColor = Shader.PropertyToID("_Color"), IdColor2 = Shader.PropertyToID("_Color2"), IdMode = Shader.PropertyToID("_Mode"),
            IdParams = Shader.PropertyToID("_Params"), IdIntensity = Shader.PropertyToID("_Intensity"), IdMainTex = Shader.PropertyToID("_MainTex"),
            IdMainST = Shader.PropertyToID("_MainTex_ST"), IdFlash = Shader.PropertyToID("_Flash"), IdScroll = Shader.PropertyToID("_Scroll");

        /// <summary>Preenche os parâmetros do shader do chão (modo, cores, parâmetros) num objeto.</summary>
        public static void GroundSet(FxObj o, int mode, Color c, Color c2, Vector4 prm, float alpha, float intensity = 1f)
        {
            c.a = alpha;
            o.Mpb.SetColor(IdColor, c); o.Mpb.SetColor(IdColor2, c2); o.Mpb.SetFloat(IdMode, mode); o.Mpb.SetVector(IdParams, prm); o.Mpb.SetFloat(IdIntensity, intensity);
            o.Apply();
        }

        /// <summary>Anel que se expande (ou contrai, se r1 &lt; r0) no chão.</summary>
        public void Ring(Vector3 p, Color c, float r0, float r1, float life, float width = 0.25f, float inner = 0.25f, float y = 0.05f, float intensity = 1.6f)
        {
            var f = Spawn("ground", life, RingFn);
            f.A = new Vector3(p.x, y, p.z); f.C = c; f.C2 = Color.Lerp(c, Color.white, 0.6f); f.F0 = r0; f.F1 = r1; f.F2 = width; f.F3 = inner; f.B = new Vector3(intensity, 0, 0);
            Begin(f);
        }
        static void RingFn(Fx f, float k)
        {
            float e = 1f - Mathf.Pow(1f - k, 2.4f);
            float r = Mathf.Max(0.05f, Mathf.Lerp(f.F0, f.F1, e)); float R = r / 0.9f;
            f.O.T.position = f.A; f.O.T.localScale = new Vector3(R * 2f, 1f, R * 2f);
            GroundSet(f.O, 0, f.C, f.C2, new Vector4(0.9f, Mathf.Clamp(f.F2 / R, 0.01f, 0.5f), f.F3, 0), Mathf.Pow(1f - k, 1.3f), f.B.x);
        }

        /// <summary>Golpe em cone (varredura que avança e some): yaw em radianos da simulação.</summary>
        public void Cone(Vector3 p, float yawRad, float range, float deg, Color c, Color c2, float life = 0.3f, float intensity = 1.8f)
        {
            var f = Spawn("ground", life, ConeFn);
            f.A = new Vector3(p.x, 0.05f, p.z); f.C = c; f.C2 = c2; f.F0 = yawRad * Mathf.Rad2Deg; f.F1 = range; f.F2 = Mathf.Min(179f, deg * 0.5f) * Mathf.Deg2Rad; f.F3 = intensity;
            Begin(f);
        }
        static void ConeFn(Fx f, float k)
        {
            f.O.T.position = f.A; f.O.T.rotation = Quaternion.Euler(0, f.F0, 0); f.O.T.localScale = new Vector3(f.F1 * 2f, 1f, f.F1 * 2f);
            float front = Mathf.Lerp(0.15f, 1.05f, 1f - Mathf.Pow(1f - Mathf.Min(1f, k * 1.4f), 2f));
            GroundSet(f.O, 2, f.C, f.C2, new Vector4(front, 0, f.F2, 1), 1f - Mathf.SmoothStep(0.45f, 1f, k), f.F3);
        }

        /// <summary>Mancha no chão (queimado, rachadura, gosma) que some devagar.</summary>
        public void Decal(Vector3 p, float size, Texture tex, Color c, float life = 6f)
        {
            var f = Spawn("decal", life, DecalFn);
            f.A = new Vector3(p.x, 0.04f, p.z); f.F0 = size; f.F1 = Rnd(0f, 360f); f.C = c;
            f.O.Mpb.SetTexture(IdMainTex, tex);
            Begin(f);
        }
        static void DecalFn(Fx f, float k)
        {
            f.O.T.position = f.A; f.O.T.rotation = Quaternion.Euler(0, f.F1, 0);
            float grow = Mathf.Lerp(0.6f, 1f, Mathf.Min(1f, k * 12f));
            f.O.T.localScale = new Vector3(f.F0 * grow, 1f, f.F0 * grow);
            var c = f.C; c.a *= 1f - Mathf.SmoothStep(0.65f, 1f, k);
            f.O.Mpb.SetColor(IdColor, c); f.O.Apply();
        }

        /* ---------- feixes ---------- */

        /// <summary>Posiciona um objeto "beam"/"streak" de a até b, sempre de frente para a câmera.</summary>
        public void PlaceBeam(FxObj o, Vector3 a, Vector3 b, float width)
        {
            var d = b - a; float len = d.magnitude; if (len < 1e-4f) { d = Vector3.forward * 1e-3f; len = 1e-3f; }
            var dir = d / len; var cam = Cam;
            var toCam = cam != null ? (cam.transform.position - (a + b) * 0.5f).normalized : Vector3.up;
            var up = Vector3.Cross(dir, toCam); if (up.sqrMagnitude < 1e-6f) up = Vector3.Cross(dir, Vector3.up); up.Normalize();
            var fwd = Vector3.Cross(dir, up);
            o.T.position = a; o.T.rotation = Quaternion.LookRotation(fwd, up); o.T.localScale = new Vector3(len, width, 1f);
        }

        /// <summary>Feixe de energia de a até b (some em life segundos). scroll = velocidade da textura.</summary>
        public void Beam(Vector3 a, Vector3 b, Color c, float width, float life, float scroll = -3f)
        {
            var f = Spawn("beam", life, BeamFn);
            f.A = a; f.B = b; f.C = c; f.F0 = width; f.F1 = scroll;
            Begin(f);
        }
        void BeamFn(Fx f, float k)
        {
            float w = f.F0 * (1f + 0.3f * Mathf.Sin(k * 40f)) * (1f - k * 0.5f);
            PlaceBeam(f.O, f.A, f.B, w);
            float len = (f.B - f.A).magnitude;
            var c = f.C; c.a = 1f - Mathf.SmoothStep(0.3f, 1f, k);
            f.O.Mpb.SetColor(IdColor, c); f.O.Mpb.SetVector(IdMainST, new Vector4(Mathf.Max(1f, len / Mathf.Max(0.2f, f.F0 * 4f)), 1f, 0f, 0f));
            f.O.Mpb.SetVector(IdScroll, new Vector4(f.F1, 0f, 0f, 0f)); f.O.Apply();
        }

        /// <summary>Configura cor/rolagem de um feixe mantido (canal, corrosão, coluna de luz).</summary>
        public static void BeamLook(FxObj o, Color c, float len, float width, float scroll)
        {
            o.Mpb.SetColor(IdColor, c); o.Mpb.SetVector(IdMainST, new Vector4(Mathf.Max(1f, len / Mathf.Max(0.2f, width * 4f)), 1f, 0f, 0f));
            o.Mpb.SetVector(IdScroll, new Vector4(scroll, 0f, 0f, 0f)); o.Apply();
        }

        /// <summary>Quadrado virado para a câmera (orbe, núcleo de carga).</summary>
        public void PlaceBillboard(FxObj o, Vector3 p, float size, Color c, float spin = 0f)
        {
            var cam = Cam;
            o.T.position = p; o.T.rotation = (cam != null ? cam.transform.rotation : Quaternion.identity) * Quaternion.Euler(0, 0, spin);
            o.T.localScale = new Vector3(size, size, size);
            o.Mpb.SetColor(IdColor, c); o.Apply();
        }

        public static void SetColor(FxObj o, Color c) { o.Mpb.SetColor(IdColor, c); o.Apply(); }
        public static void SetFlash(FxObj o, Color c, float flash) { o.Mpb.SetColor(IdColor, c); o.Mpb.SetFloat(IdFlash, flash); o.Apply(); }

        /* ======================= receitas compostas ======================= */

        /// <summary>Explosão de fogo: clarão, bola de fogo, fumaça, faíscas, brasas, onda, estilhaços e chão queimado.</summary>
        public void Explosion(Vector3 p, float radius, Color core, bool scorch = true, float shakeAmt = 0.15f)
        {
            var c = new Vector3(p.x, Mathf.Max(0.4f, p.y), p.z);
            Flash(c, Color.Lerp(core, Color.white, 0.5f), radius * 2.6f, 0.22f, 1.6f);
            Star(c, core, radius * 1.8f, 0.25f);
            FireBurst(c, 10 + (int)(radius * 8), radius * 0.35f, radius * 0.9f, radius * 2.2f, 0.55f, 0.4f);
            SmokeBurst(c + Vector3.up * 0.3f, new Color(0.28f, 0.24f, 0.22f, 0.8f), 6 + (int)(radius * 3), radius * 0.4f, radius * 1.1f, radius * 0.9f, 1.8f, 0.8f);
            SparkBurst(c, core, 22 + (int)(radius * 6), radius * 4.5f, 0.45f, 0.08f);
            EmberBurst(c, core, 14, radius * 0.4f, radius * 1.6f);
            DebrisBurst(c, new Color(0.35f, 0.28f, 0.22f), 6, radius * 3f, 0.16f);
            Ring(p, core, radius * 0.3f, radius * 1.25f, 0.45f, 0.35f, 0.35f);
            Dust(p, 10, radius * 0.6f, radius * 1.6f, 1.1f);
            if (scorch) Decal(p, radius * 2.3f, VfxAssets.Scorch, new Color(1f, 1f, 1f, 0.9f), 9f);
            AddShake(shakeAmt);
        }

        /// <summary>Morte de criatura: pedaços de gosma quicando, respingo brilhante e mancha no chão.</summary>
        public void Gibs(Vector3 p, float size, Color goo)
        {
            var c = p + Vector3.up * Mathf.Max(0.35f, size * 0.5f);
            DebrisBurst(c, Color.Lerp(goo, new Color(0.2f, 0.15f, 0.1f), 0.55f), 8 + (int)(size * 6), 3.2f + size, 0.14f + size * 0.08f);
            SparkBurst(c, goo, 16, 3.5f, 0.45f, 0.09f, 0.6f);
            SmokeBurst(c, new Color(goo.r * 0.6f + 0.2f, goo.g * 0.6f + 0.2f, goo.b * 0.6f + 0.2f, 0.55f), 4, size * 0.3f, size * 0.9f, 1.2f, 0.9f);
            Flash(c, goo, size * 1.6f, 0.12f, 0.4f);
            var dc = Color.Lerp(goo, new Color(0.15f, 0.2f, 0.08f), 0.5f); dc.a = 0.85f;
            Decal(p, size * 2.4f, VfxAssets.Splat, dc, 8f);
        }

        /// <summary>Espinhos de gelo que brotam do chão, ficam um pouco e se estilhaçam.</summary>
        public void IceSpikes(Vector3 p, float radius, int count, float height, float life = 0.9f, int seed = -1)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f + Rnd(-0.3f, 0.3f);
                float d = i == 0 ? 0f : radius * Mathf.Sqrt(Rnd(0.15f, 1f));
                var pos = new Vector3(p.x + Mathf.Cos(a) * d, 0f, p.z + Mathf.Sin(a) * d);
                float h = height * (i == 0 ? 1.25f : Rnd(0.55f, 1f));
                var f = Spawn("spike", life * Rnd(0.85f, 1.15f), SpikeFn);
                f.A = pos; f.F0 = h; f.F1 = h * Rnd(0.28f, 0.4f); f.F2 = Rnd(0f, 360f);
                var tilt = new Vector3(pos.x - p.x, 0, pos.z - p.z); f.B = tilt.sqrMagnitude > 1e-4f ? tilt.normalized * Rnd(8f, 22f) : Vector3.zero;
                f.C = new Color(0.45f, 0.8f, 1.2f);
                Begin(f);
            }
        }
        void SpikeFn(Fx f, float k)
        {
            float rise = Mathf.Min(1f, k / 0.12f); rise = 1f - (1f - rise) * (1f - rise);
            float shrink = 1f - Mathf.SmoothStep(0.8f, 1f, k);
            f.O.T.position = f.A + Vector3.down * (1f - rise) * f.F0 * 0.6f;
            f.O.T.rotation = Quaternion.Euler(f.B.z, f.F2, -f.B.x);
            f.O.T.localScale = new Vector3(f.F1 * shrink, f.F0 * rise * Mathf.Max(0.2f, shrink), f.F1 * shrink);
            f.O.Mpb.SetColor(IdColor, f.C); f.O.Mpb.SetFloat(IdFlash, Mathf.Max(0f, 1f - k * 6f) * 1.5f); f.O.Apply();
            if (k >= 0.8f && f.F3 == 0f)
            {
                f.F3 = 1f; var top = f.A + Vector3.up * f.F0 * 0.5f;
                IceBurst(top, 3, 2.5f, f.F1 * 0.7f); ShardBurst(top, new Color(0.7f, 0.9f, 1f), 3, 2f);
            }
        }

        /// <summary>Coluna de luz do drop (cor da raridade do item), anel e partículas subindo.</summary>
        public void DropBeam(Vector3 p, Color c)
        {
            var f = Spawn("beam", 2.6f, DropFn); f.A = new Vector3(p.x, 0.05f, p.z); f.C = c; Begin(f);
            Ring(p, c, 0.2f, 1.4f, 0.6f, 0.2f, 0.4f);
            Motes(new Vector3(p.x, 0.6f, p.z), c, 10, 0.4f, 0.22f, 1.2f, new Vector3(0, 1.8f, 0), 0.4f);
            Flash(new Vector3(p.x, 0.6f, p.z), c, 1.6f, 0.2f, 0.8f);
        }
        void DropFn(Fx f, float k)
        {
            float h = Mathf.Lerp(0.5f, 7f, Mathf.Min(1f, k * 6f));
            float w = 0.55f * (1f - Mathf.SmoothStep(0.6f, 1f, k)) + 0.05f;
            PlaceBeam(f.O, f.A, f.A + Vector3.up * h, w);
            var c = f.C; c.a = 1f - Mathf.SmoothStep(0.55f, 1f, k);
            BeamLook(f.O, c, h, w, -2f);
        }

        /// <summary>Meteoro: pedra em brasa caindo do céu até pos em delay segundos (o impacto vem no evento meteor_impact).</summary>
        public void MeteorFall(Vector3 pos, float delay, float radius)
        {
            var f = Spawn("rock", delay, MeteorFn);
            f.A = pos + new Vector3(-5f, 17f, -3f); f.B = new Vector3(pos.x, 0.3f, pos.z); f.F0 = radius; f.F1 = Rnd(0f, 360f);
            Begin(f);
        }
        void MeteorFn(Fx f, float k)
        {
            float e = k * k;
            var p = Vector3.Lerp(f.A, f.B, e);
            f.O.T.position = p; f.O.T.rotation = Quaternion.Euler(f.F1 + k * 400f, f.F1 * 0.5f + k * 250f, 0f);
            f.O.T.localScale = Vector3.one * (0.9f + f.F0 * 0.15f);
            // rastro: fogo, brasas e fumaça (proporcional ao quanto andou)
            var pp = f.F2 > 0f ? f.P : p; float dist = (p - pp).magnitude;
            int n = Mathf.Min(8, Mathf.CeilToInt(dist * 2.5f));
            for (int i = 0; i < n; i++)
            {
                var q = Vector3.Lerp(pp, p, i / (float)Mathf.Max(1, n));
                Fire.Emit(q + UnityEngine.Random.insideUnitSphere * 0.25f, UnityEngine.Random.insideUnitSphere * 0.5f, Rnd(0.9f, 1.5f), Rnd(0.25f, 0.45f), Color.white, -1f, 60f);
                if (i % 2 == 0) Smoke.Emit(q, UnityEngine.Random.insideUnitSphere * 0.3f, Rnd(0.9f, 1.4f), Rnd(0.8f, 1.3f), new Color(0.3f, 0.25f, 0.22f, 0.6f), -1f, 20f);
                if (i % 2 == 1) Embers.Emit(q, UnityEngine.Random.insideUnitSphere * 1.5f, 0.08f, Rnd(0.4f, 0.9f), new Color(1f, 0.6f, 0.2f));
            }
            f.P = p; f.F2 = 1f;
        }
    }
}
