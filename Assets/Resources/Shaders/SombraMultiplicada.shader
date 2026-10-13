// SOMBRA MULTIPLICADA para sprites (as sombras das nuvens passando pela fase).
//
// "Multiplicar" (Blend DstColor Zero) é o jeito certo de escurecer: a cor da tela é MULTIPLICADA
// por um número entre 0 e 1. O que é claro fica um pouco mais escuro, o preto continua preto,
// e as cores não ficam "lavadas" de cinza (o que acontece se a gente só pintar um preto transparente).
//
// No desenho, a transparência diz onde tem nuvem. A cor (SpriteRenderer.color e _Cor) diz o tom da
// sombra (um azul acinzentado) e a transparência dessa cor diz a FORÇA: 0 = nada, 1 = a cor inteira.
//
// Um Pass só, com LightMode "Universal2D" (veja o comentário em LuzAditiva.shader).
// Se este shader não funcionar na placa de vídeo, o jogo usa o material normal (veja MateriaisHD2D).
Shader "ReCilada/SombraMultiplicada"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
        _Cor ("Cor", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend DstColor Zero
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            half4 _Cor;

            Varyings vert(Attributes entrada)
            {
                Varyings saida;
                saida.positionCS = TransformObjectToHClip(entrada.positionOS);
                saida.color = entrada.color * _Cor;
                saida.uv = entrada.uv;
                return saida;
            }

            half4 frag(Varyings entrada) : SV_Target
            {
                half nuvem = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, entrada.uv).a * entrada.color.a;
                // 1 = não muda nada; a cor da sombra onde a nuvem é cheia
                half3 fator = lerp(half3(1.0, 1.0, 1.0), entrada.color.rgb, nuvem);
                return half4(fator, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
