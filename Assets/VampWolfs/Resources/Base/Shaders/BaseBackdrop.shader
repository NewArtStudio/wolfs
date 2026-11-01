// Space — fundo pintado da Base (URP). Tela inicial (out/2026).
// Um quadrado preso à câmera mostra a placa pintada (Resources/Base/BaseBackdrop) exatamente como ela é:
// sem luz, sem névoa e sem escrever profundidade, desenhado antes de tudo (fila Background). Os soldados 3D
// e a sombra deles ficam por cima. A posição e o tamanho do quadrado vêm de BaseStage (calibração da câmera).
Shader "Space/Base/Backdrop"
{
    Properties
    {
        _MainTex ("Placa pintada", 2D) = "black" {}
        _Tint ("Tom (multiplica)", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Background" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Pass
        {
            Name "BaseBackdrop"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Tint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                return half4(c.rgb * _Tint.rgb, 1);
            }
            ENDHLSL
        }
    }
}
