// Copia de la Yema clonal: el sprite del jugador con su luminancia llevada a una rampa de 4 tonos verde agua, translúcido,
// y un contorno claro en los píxeles transparentes que tocan al sprite. Sin luces (como Sprite-Unlit-Default).
Shader "Primordia/ClonalBud"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Ramp0 ("Rampa 0 (oscuro)", Color) = (0.118, 0.416, 0.416, 1)
        _Ramp1 ("Rampa 1", Color) = (0.227, 0.659, 0.627, 1)
        _Ramp2 ("Rampa 2", Color) = (0.494, 0.910, 0.800, 1)
        _Ramp3 ("Rampa 3 (claro)", Color) = (0.847, 1.000, 0.941, 1)
        _Outline ("Contorno", Color) = (0.722, 1.000, 0.941, 1)
        _Opacity ("Opacidad", Range(0, 1)) = 0.6

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Ramp0;
                half4 _Ramp1;
                half4 _Ramp2;
                half4 _Ramp3;
                half4 _Outline;
                half _Opacity;
            CBUFFER_END

            Varyings Vertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = CommonUnlitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half Alpha(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half alpha = _Opacity * input.color.a;

                if (texel.a < 0.5)
                {
                    float2 step = _MainTex_TexelSize.xy;
                    half around = Alpha(input.uv + float2(step.x, 0)) + Alpha(input.uv - float2(step.x, 0))
                        + Alpha(input.uv + float2(0, step.y)) + Alpha(input.uv - float2(0, step.y));
                    return around > 0.5 ? half4(_Outline.rgb, alpha) : half4(0, 0, 0, 0);
                }

                half luminance = dot(texel.rgb, half3(0.299, 0.587, 0.114));
                half3 tone = luminance < 0.25 ? _Ramp0.rgb : luminance < 0.5 ? _Ramp1.rgb : luminance < 0.75 ? _Ramp2.rgb : _Ramp3.rgb;
                return half4(tone, alpha);
            }
            ENDHLSL
        }
    }
}
