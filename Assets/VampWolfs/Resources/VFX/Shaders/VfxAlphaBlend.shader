// Space — VFX com transparência normal (URP): fumaça, poeira, manchas no chão (queimado, gosma, rachaduras).
// Recebe um pouco da luz do sol + ambiente para não "brilhar" no deserto.
// _OffsetUnits negativo puxa o desenho para a frente do chão (manchas sem piscar).
Shader "Space/VFX/AlphaBlend"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Cor", Color) = (1,1,1,1)
        _Lit ("Quanto a luz afeta (0..1)", Range(0,1)) = 0.8
        _OffsetFactor ("Offset factor", Float) = 0
        _OffsetUnits ("Offset units", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Name "VfxAlpha"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset [_OffsetFactor], [_OffsetUnits]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Lit;
                float _OffsetFactor;
                float _OffsetUnits;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * i.color * _Color;
                Light sun = GetMainLight();
                half3 light = sun.color * 0.55h + SampleSH(half3(0, 1, 0));
                c.rgb *= lerp(half3(1, 1, 1), light, _Lit);
                c.rgb = MixFog(c.rgb, i.fog);
                return c;
            }
            ENDHLSL
        }
    }
}
