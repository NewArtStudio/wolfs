// Space — "pegador de sombra" da Base (URP). Tela inicial (out/2026).
// Plano invisível no nível do chão pintado: só escurece onde a sombra do sol (luz principal) cai sobre ele,
// multiplicando a pintura por _ShadowColor (sombra quente, como as da referência). Também desenha uma sombra
// de contato macia sob os pés de cada soldado (_Feet: x, z, raio, força) para eles "pisarem" no chão.
Shader "Space/Base/ShadowCatcher"
{
    Properties
    {
        _ShadowColor ("Cor da sombra (multiplica)", Color) = (0.32,0.24,0.2,1)
        _ShadowStrength ("Força da sombra", Range(0,1)) = 0.85
        _ContactColor ("Cor do contato", Color) = (0.2,0.15,0.12,1)
        _FadeRadius ("Raio até sumir (m)", Float) = 9
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-400" "IgnoreProjector"="True" }
        Pass
        {
            Name "BaseShadowCatcher"
            Tags { "LightMode"="UniversalForward" }
            Blend DstColor Zero
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                half _ShadowStrength;
                half4 _ContactColor;
                float _FadeRadius;
            CBUFFER_END
            // pés dos soldados (preenchido por BaseStage): xy = posição no chão (x, z), z = raio, w = força
            float4 _Feet[4];
            float4 _StageCenter;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
            #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                float4 sc = ComputeScreenPos(TransformWorldToHClip(i.positionWS));
            #else
                float4 sc = TransformWorldToShadowCoord(i.positionWS);
            #endif
                half lit = MainLightRealtimeShadow(sc);
                // some devagar longe do centro (não escurece a placa inteira se o mapa de sombra acabar)
                float d = length(i.positionWS.xz - _StageCenter.xy);
                half fade = saturate(1.0 - d / max(_FadeRadius, 0.1));
                half shadow = (1.0h - lit) * _ShadowStrength * fade;
                half3 mul = lerp(half3(1, 1, 1), _ShadowColor.rgb, shadow);
                // sombra de contato sob os pés
                half contact = 0;
                [unroll] for (int k = 0; k < 4; k++)
                {
                    float r = max(_Feet[k].z, 1e-3);
                    float2 q = (i.positionWS.xz - _Feet[k].xy) / float2(r, r * 0.8);
                    contact = max(contact, saturate(1.0 - dot(q, q)) * _Feet[k].w);
                }
                mul *= lerp(half3(1, 1, 1), _ContactColor.rgb, contact * contact);
                return half4(mul, 1);
            }
            ENDHLSL
        }
    }
}
