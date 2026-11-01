// Space — contorno de alerta (URP): "casca invertida" — o modelo é desenhado de novo, inflado pela normal e só pelas faces de trás,
// então só a borda aparece em volta da silhueta. Largura em PIXELS (igual de perto e de longe).
// Usado pelo DangerOutline (soldado dentro de área de dano). Cor HDR: acima de 1 o bloom faz o brilho.
Shader "Space/VFX/Outline"
{
    Properties
    {
        [HDR] _Color ("Cor", Color) = (2, 0.15, 0.1, 1)
        _Width ("Largura (pixels)", Float) = 3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10" "IgnoreProjector"="True" }
        Pass
        {
            Name "VfxOutline"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float4 cs = TransformObjectToHClip(v.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(v.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float len = length(nCS);
                if (len > 1e-5)
                    cs.xy += (nCS / len) * (_Width * 2.0 / _ScreenParams.xy) * cs.w;   // pixels → espaço de recorte
                o.positionCS = cs;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
