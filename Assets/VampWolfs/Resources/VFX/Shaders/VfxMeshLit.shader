// Space — pedaços sólidos dos efeitos (URP): estilhaços, pedra do meteoro, gosma das criaturas, mastro do estandarte.
// Luz simples (sol + ambiente) × cor do vértice/partícula × _Color, mais _Emission (brasa do meteoro).
Shader "Space/VFX/MeshLit"
{
    Properties
    {
        _Color ("Cor", Color) = (1,1,1,1)
        [HDR] _Emission ("Emissão", Color) = (0,0,0,0)
        _Gloss ("Brilho especular", Range(0,1)) = 0.2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "VfxMeshLit"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Emission;
                half _Gloss;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 positionWS : TEXCOORD1; half4 color : COLOR; float fog : TEXCOORD2; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color; o.fog = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                half3 n = normalize(i.normalWS);
                Light sun = GetMainLight();
                half ndl = saturate(dot(n, sun.direction));
                half3 vdir = GetWorldSpaceNormalizeViewDir(i.positionWS);
                half spec = pow(saturate(dot(n, normalize(sun.direction + vdir))), 32.0h) * _Gloss;
                half3 albedo = _Color.rgb * i.color.rgb;
                half3 rgb = albedo * (sun.color * ndl + SampleSH(n)) + sun.color * spec + _Emission.rgb * i.color.a;
                rgb = MixFog(rgb, i.fog);
                return half4(rgb, 1);
            }
            ENDHLSL
        }
    }
}
