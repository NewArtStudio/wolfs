// Space — casca de energia (URP): escudos (bolha hexagonal), cristais de gelo, aura.
// Borda brilha mais (Fresnel); _MainTex (A) desenha o padrão (hexágonos) rolando com _Scroll.
// _Flash soma um clarão (escudo absorvendo dano). Soma na tela (aditivo).
Shader "Space/VFX/Fresnel"
{
    Properties
    {
        _MainTex ("Padrão (A)", 2D) = "white" {}
        [HDR] _Color ("Cor", Color) = (0.4,0.8,1,1)
        _Power ("Potência da borda", Float) = 2.5
        _Rim ("Força da borda", Float) = 1.2
        _Pattern ("Força do padrão", Float) = 0.6
        _Base ("Brilho do corpo", Float) = 0.05
        _Scroll ("Rolagem UV (xy)", Vector) = (0, 0.05, 0, 0)
        _Flash ("Clarão", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Name "VfxFresnel"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZWrite Off
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _Power, _Rim, _Pattern, _Base, _Flash;
                float4 _Scroll;
                float _Cull;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; float2 uv : TEXCOORD2; half4 color : COLOR; float fog : TEXCOORD3; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + _Scroll.xy * _Time.y;
                o.color = v.color;
                o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                half3 vdir = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half rim = pow(1.0h - saturate(abs(dot(n, vdir))), _Power);
                half pat = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;
                half k = rim * _Rim + pat * _Pattern * (0.35h + rim) + _Base + _Flash * (0.4h + rim);
                half3 rgb = _Color.rgb * i.color.rgb * k * _Color.a * i.color.a;
                rgb = MixFogColor(rgb, half3(0, 0, 0), i.fog);
                return half4(rgb, 0);
            }
            ENDHLSL
        }
    }
}
