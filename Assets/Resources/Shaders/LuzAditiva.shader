// LUZ ADITIVA para sprites (raios de sol, brilho em volta do sol).
//
// O jeito normal de desenhar um sprite transparente é MISTURAR a cor dele com a do fundo
// (Blend SrcAlpha OneMinusSrcAlpha). Luz não funciona assim: luz SOMA com o que já está lá
// (Blend One One). Por isso um raio de sol aditivo clareia as coisas sem "pintar" por cima delas,
// e onde ele passa de 1 (branco), o bloom do pós-processamento faz brilhar.
//
// A cor vem do SpriteRenderer.color (ela chega aqui como a "cor do vértice") e da propriedade _Cor
// do material (branca por padrão). A transparência do desenho e do SpriteRenderer viram INTENSIDADE.
//
// Um Pass só, com LightMode "Universal2D": é o que o Renderer2D do URP desenha (igual ao
// Sprite-Unlit-Default). Ele também desenha passes "SRPDefaultUnlit"; se este shader tivesse os
// DOIS, o sprite seria desenhado duas vezes (e a luz somada em dobro).
// Se este shader não funcionar na placa de vídeo, o jogo usa o material normal (veja MateriaisHD2D).
Shader "ReCilada/LuzAditiva"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
        _Cor ("Cor", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Blend One One
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
                half4 cor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, entrada.uv) * entrada.color;
                // Blend One One soma tudo: a transparência vira a força da luz
                return half4(cor.rgb * cor.a, cor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
