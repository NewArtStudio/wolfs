// Space — VFX desenhado no chão (URP), em cima de um quadrado deitado (UV 0..1, frente = +V).
// Tudo é calculado no shader (nítido de perto e de longe). _Mode escolhe o desenho:
//   0 ANEL      onda de choque. _Params: x raio (0..1), y espessura, z brilho interno
//   1 CÍRCULO   telegrafia de área. _Params.x = progresso do cast (0..1)
//   2 CONE      _Params: x progresso/frente da onda, z meio-ângulo (rad), w estilo (0 telegrafia, 1 golpe)
//   3 LINHA     telegrafia em faixa. _Params: x progresso, y comprimento/largura
//   4 ZONA      campo persistente. _Params: x giro (rad/s), w estilo (0 fogo, 1 gelo, 2 cura, 3 dourado, 4 estandarte)
// Golpe FATAL (out/2026): flag de estilo — CÍRCULO usa _Params.w, CONE _Params.y, LINHA _Params.z:
//   1 = fatal no chão (preenchimento forte, borda grossa com miolo branco e contorno escuro por fora)
//   2 = fatal "por cima" (só borda + contorno + frente do progresso; material com ZTest Always, desenhado sobre criaturas e relevo)
// _Color.a apaga o efeito inteiro (fade). Soma luz na tela (brilha com o Bloom) e, desde out/2026, também escurece um pouco
// o chão por baixo ("sombra colorida"): na areia clara a cor aparece em vez de estourar para branco chapado.
Shader "Space/VFX/Ground"
{
    Properties
    {
        _MainTex ("Runa (A)", 2D) = "white" {}
        _NoiseTex ("Ruído", 2D) = "gray" {}
        [HDR] _Color ("Cor", Color) = (1,1,1,1)
        [HDR] _Color2 ("Cor 2", Color) = (1,1,1,1)
        _Mode ("Modo", Float) = 0
        _Params ("Parâmetros", Vector) = (0.85, 0.06, 0, 0)
        _Intensity ("Intensidade", Float) = 1
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-20" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Name "VfxGround"
            Tags { "LightMode"="UniversalForward" }
            Blend One OneMinusSrcAlpha   // rgb soma (brilho); alpha escurece o chão por baixo
            ZWrite Off
            ZTest [_ZTest]
            Cull Off
            Offset -1, -4

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _NoiseTex_ST;
                half4 _Color;
                half4 _Color2;
                float _Mode;
                float4 _Params;
                half _Intensity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.uv = v.uv; o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            float Band(float x, float c, float w) { float d = (x - c) / max(w, 1e-4); return exp(-d * d); }

            // borda de golpe fatal: faixa grossa (cor) com miolo branco e contorno escuro por fora (lê sobre chão e criaturas vermelhas)
            // e = posição na borda (1 = na linha), retorna brilho em .x, quanto vira branco em .y e escurecimento em .z
            float3 LethalEdge(float x, float c)
            {
                float rim = Band(x, c, 0.028);
                float core = Band(x, c, 0.009);
                float dark = Band(x, c + 0.045, 0.02) + Band(x, c - 0.05, 0.02) * 0.6;
                return float3(rim * 2.2, core, dark);
            }
            half Noise(float2 uv, int ch)
            {
                half4 n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv);
                return ch == 0 ? n.r : ch == 1 ? n.g : n.b;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.uv * 2.0 - 1.0;          // x direita, y frente
                float r = length(p);
                float t = _Time.y;
                half3 col = _Color.rgb;
                float a = 0;
                float dark = 0;                         // contorno escuro (só escurece o que está atrás)

                if (_Mode < 0.5)            // ANEL
                {
                    float band = Band(r, _Params.x, _Params.y);
                    float k = saturate(r / max(_Params.x, 1e-3));
                    float inner = k * k * k * step(r, _Params.x) * _Params.z;
                    a = band + inner;
                    col = lerp(_Color2.rgb, _Color.rgb, saturate(band));
                }
                else if (_Mode < 1.5)       // CÍRCULO (telegrafia)
                {
                    float k = saturate(_Params.x);
                    float inside = 1 - smoothstep(0.95, 0.99, r);
                    float rim = Band(r, 0.965, 0.03);
                    float front = Band(r, k * 0.96, 0.035) * step(0.02, k);
                    float stripes = step(0.55, frac((p.x + p.y) * 4.0 - t * 1.2));
                    float fill = inside * (0.06 + 0.06 * stripes) + step(r, k * 0.96) * 0.18;
                    float pulse = 0.82 + 0.18 * sin(t * (6.0 + 16.0 * k));
                    a = (rim * 1.3 + front * 0.9 + fill) * pulse;
                    if (_Params.w > 0.5)    // fatal
                    {
                        float3 le = LethalEdge(r, 0.95);
                        float fastPulse = 0.75 + 0.25 * sin(t * (9.0 + 20.0 * k));
                        float fillL = (1 - smoothstep(0.93, 0.95, r)) * (0.16 + 0.08 * stripes) + step(r, k * 0.94) * 0.3;
                        float frontL = Band(r, k * 0.94, 0.03) * step(0.02, k) * 1.4;
                        a = le.x * fastPulse + frontL + (_Params.w < 1.5 ? fillL : 0);
                        col = lerp(_Color.rgb, half3(1.6, 1.5, 1.4), saturate(le.y + frontL * 0.4));
                        dark = le.z;
                    }
                }
                else if (_Mode < 2.5)       // CONE
                {
                    float ang = atan2(p.x, p.y);
                    float halfA = _Params.z;
                    float da = abs(ang) - halfA;                       // < 0 dentro
                    float inAng = 1 - smoothstep(-0.02, 0.02, da);
                    float inR = 1 - smoothstep(0.95, 1.0, r);
                    float mask = inAng * inR;
                    float sideDist = abs(da) * r;                      // distância (aprox.) até a borda lateral
                    float side = exp(-sideDist * 40.0) * inR * step(0.0, halfA - 0.01) * step(halfA, 3.1);
                    float k = saturate(_Params.x);
                    if (_Params.w < 0.5)
                    {
                        float arc = Band(r, 0.965, 0.03) * inAng;
                        float front = Band(r, k * 0.96, 0.035) * inAng * step(0.02, k);
                        a = mask * (0.10 + step(r, k * 0.96) * 0.26) + (side + arc) * 1.1 + front * 0.9;
                        a *= 0.85 + 0.15 * sin(t * (6.0 + 16.0 * k));
                        if (_Params.y > 0.5)    // fatal: borda grossa com miolo branco e contorno escuro (arco + laterais)
                        {
                            float3 le = LethalEdge(r, 0.95);
                            float sideL = exp(-sideDist * 70.0) * inR * step(0.0, halfA - 0.01) * step(halfA, 3.1);
                            float sideD = exp(-abs(sideDist - 0.04) * 60.0) * inR * step(0.0, halfA - 0.01) * step(halfA, 3.1) * (1 - inAng);
                            float frontL = Band(r, k * 0.94, 0.03) * inAng * step(0.02, k) * 1.4;
                            float pulseL = 0.75 + 0.25 * sin(t * (9.0 + 20.0 * k));
                            float fillL = mask * (0.16 + step(r, k * 0.94) * 0.3);
                            a = (le.x * inAng + sideL * 2.2) * pulseL + frontL + (_Params.y < 1.5 ? fillL : 0);
                            col = lerp(_Color.rgb, half3(1.6, 1.5, 1.4), saturate(le.y * inAng + exp(-sideDist * 200.0) * inR * 0.8 + frontL * 0.4));
                            dark = le.z * inAng + sideD;
                        }
                    }
                    else
                    {
                        float wave = Band(r, k, 0.14);
                        float trail = step(r, k) * pow(saturate(r / max(k, 1e-3)), 3.0) * 0.55;
                        half n = Noise(float2(ang * 0.5, r * 0.9 - t * 0.7), 0);
                        a = mask * (wave * 1.5 + trail) * (0.55 + 0.9 * n) + side * 0.5 * step(r, k);
                    }
                    if (_Params.y < 0.5 || _Params.w > 0.5) col = lerp(_Color2.rgb, _Color.rgb, saturate(r * 1.2));
                }
                else if (_Mode < 3.5)       // LINHA (telegrafia em faixa)
                {
                    float ax = abs(p.x);
                    float along = (p.y + 1.0) * 0.5;                   // 0 = boca do inimigo, 1 = fim
                    float aspect = max(_Params.y, 1.0);
                    float side = exp(-(1.0 - ax) * 12.0);
                    float cap = exp(-(1.0 - along) * aspect * 10.0);
                    float chevT = frac(along * aspect * 0.9 + ax * 0.45 - t * 1.6);   // divisas "^" correndo para a frente
                    float chev = smoothstep(0.0, 0.08, chevT) * (1.0 - smoothstep(0.22, 0.32, chevT));
                    float fill = step(along, saturate(_Params.x));
                    a = side * 0.9 + 0.07 + fill * 0.22 + chev * (0.18 + 0.3 * fill) + cap * 0.9;
                    a *= smoothstep(0.0, 0.04, along) * (1 - smoothstep(0.96, 1.0, ax));
                    if (_Params.z > 0.5)    // fatal: laterais grossas com miolo branco e contorno escuro
                    {
                        float sideL = exp(-(1.0 - ax) * 7.0) * 2.0;
                        float coreL = exp(-(1.0 - ax) * 30.0);
                        a = (sideL + cap * 1.4) * (0.75 + 0.25 * sin(t * 14.0)) + fill * (_Params.z < 1.5 ? 0.3 : 0.0) + chev * 0.4;
                        a *= smoothstep(0.0, 0.04, along);
                        col = lerp(_Color.rgb, half3(1.6, 1.5, 1.4), saturate(coreL));
                    }
                }
                else                        // ZONA
                {
                    float inside = 1 - smoothstep(0.92, 0.99, r);
                    float rim = Band(r, 0.95, 0.03);
                    float s = sin(t * _Params.x), c = cos(t * _Params.x);
                    float2 q = float2(p.x * c - p.y * s, p.x * s + p.y * c);
                    half rune = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, q * 0.5 + 0.5).a;
                    half n1 = Noise(i.uv * 1.2 + float2(t * 0.03, t * 0.05), 0);
                    half n2 = Noise(i.uv * 2.6 - float2(t * 0.05, t * 0.02), 1);
                    float style = _Params.w;
                    if (style < 0.5)        // fogo
                    {
                        float heat = saturate(n1 * n2 * 2.6 - 0.3 + (1 - r) * 0.3);
                        float veins = exp(-abs(n1 - 0.5) * 30.0) * 0.5;          // fendas de lava
                        col = lerp(_Color2.rgb, _Color.rgb, saturate(heat + veins));
                        a = inside * (heat * 0.6 + veins + 0.08) + rim * 0.6;
                    }
                    else if (style < 1.5)   // gelo
                    {
                        float veins = exp(-abs(n1 - 0.5) * 45.0) + exp(-abs(n2 - 0.55) * 60.0) * 0.6;   // rachaduras de gelo
                        float frost = smoothstep(0.55, 0.7, n2) * 0.25;
                        col = lerp(_Color2.rgb, _Color.rgb, saturate(veins + rim));
                        a = inside * (0.1 + frost + veins * 0.55) + rim * 0.9 + rune * 0.15;
                    }
                    else                    // cura / dourado / estandarte: runa girando + pulso
                    {
                        float pulse = 0.65 + 0.35 * sin(t * 2.2 - r * 7.0);
                        col = lerp(_Color2.rgb, _Color.rgb, saturate(rune + rim));
                        float edge = smoothstep(0.35, 0.95, r);                       // centro quase transparente, borda acesa
                        a = inside * (0.025 + 0.11 * edge + 0.05 * n1 * edge) * pulse + rune * 0.75 + rim * 0.8;
                    }
                }

                half3 rgb = col * (a * _Color.a * _Intensity);
                rgb = MixFogColor(rgb, half3(0, 0, 0), i.fog);
                half under = saturate(saturate(a) * 0.35 + dark * 0.75) * _Color.a;   // quanto o chão por baixo escurece (+ contorno do fatal)
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                under *= ComputeFogIntensity(i.fog);                             // longe, na névoa, não escurece
                #endif
                return half4(rgb, under);
            }
            ENDHLSL
        }
    }
}
