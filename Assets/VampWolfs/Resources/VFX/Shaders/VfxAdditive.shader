// Space — VFX aditivo (URP). Partículas, flashes, orbes, feixes.
// Cor final = textura × cor do vértice (partícula) × _Color × _Intensity, somada à tela (brilha com o Bloom).
// _Scroll rola a textura (feixes); _EdgeFade apaga as bordas no eixo V (feixe macio).
Shader "Space/VFX/Additive"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        [HDR] _Color ("Cor", Color) = (1,1,1,1)
        _Intensity ("Intensidade", Float) = 1
        _Scroll ("Rolagem UV (xy)", Vector) = (0,0,0,0)
        _EdgeFade ("Apagar bordas (V)", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Name "VfxAdditive"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Intensity;
                float4 _Scroll;
                half _EdgeFade;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float2 raw : TEXCOORD1; float fog : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.color = v.color;
                o.raw = v.uv;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + _Scroll.xy * _Time.y;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 c = t * i.color * _Color;
                half edge = lerp(1.0h, saturate(1.0h - abs(i.raw.y * 2.0h - 1.0h)), _EdgeFade);
                half3 rgb = c.rgb * c.a * _Intensity * edge;
                rgb = MixFogColor(rgb, half3(0, 0, 0), i.fog);   // somar "nada" ao longe = some na névoa
                return half4(rgb, 0);
            }
            ENDHLSL
        }
    }
}
