using System.Collections.Generic;
using UnityEngine;

// A HISTÓRIA do jogo, contada em quadros (cutscenes): uma imagem em pixel art e um texto embaixo.
//  - a ABERTURA passa num "Novo jogo", antes do mapa: o tesouro, o Kraken, a maldição e o Deus da Riqueza;
//  - a ESCOLHA passa quando o Grumete chega no baú, depois de vencer o Kraken: DEVOLVER leva ao FINAL (e à tela de
//    "VOCÊ ZEROU!"); ROUBAR passa o ROUBOU e o jogo recomeça no modo difícil (tempestade e um tsunami em cada fase);
//  - zerando o modo difícil vem a ESCOLHA NA TEMPESTADE: devolver (FINAL VERDADEIRO) ou ficar com o ouro (FINAL DO REI).
// ENTER (ou clique) mostra o texto todo / passa para o próximo quadro; ESC pula a cutscene inteira
// (numa cutscene com escolha, o ESC pula só até a escolha: dela não dá para fugir).
//
// As imagens ficam em Assets/Resources/Arte/historia/ (256 x 144). Elas são desenhadas pelo script
// Ferramentas/historia/desenhar_historia.py, que usa os MESMOS desenhos dos personagens do jogo.
// Os textos são traduzidos pelo Traducoes.cs (como todo texto do jogo).
//
// A história: o Grumete achou o maior tesouro de todos os tempos, mas o Kraken Vermelho, que guardava o
// tesouro, venceu ele. Os deuses do mar o amaldiçoaram: ele nunca pode morrer (é a Volta da Maré!).
// A maldição quebrou o equilíbrio do mundo e muita gente virou maligna (a Capitã, a Cozinheira, a Baleia...).
// O Deus da Riqueza, o dono do tesouro, promete tirar a maldição se o Grumete trouxer o tesouro de volta.
public static class Historia
{
    public struct Quadro
    {
        public string imagem; // o nome do arquivo em Resources/Arte/historia
        public string texto;  // em português (a tradução fica em Traducoes.cs)
        public bool textoEmCima; // a caixa do texto fica no ALTO da tela (quando a imagem tem algo importante embaixo)
        public Quadro(string imagem, string texto, bool textoEmCima = false)
        {
            this.imagem = imagem;
            this.texto = texto;
            this.textoEmCima = textoEmCima;
        }
    }

    public static readonly Quadro[] Abertura =
    {
        new Quadro("abertura_1", "Contam os marinheiros que, no fim do mar, existe o maior tesouro de todos os tempos."),
        new Quadro("abertura_2", "Um jovem pirata, o Grumete, foi o primeiro a encontrá-lo. Ouro que não acabava mais!"),
        new Quadro("abertura_3", "Mas o tesouro tinha um guardião: o KRAKEN VERMELHO. E o Grumete falhou.", textoEmCima: true), // (o Grumete está embaixo)
        new Quadro("abertura_4", "Os deuses do mar o amaldiçoaram: ele NUNCA poderá morrer. Toda vez que cair, a maré o traz de volta."),
        new Quadro("abertura_5", "A maldição quebrou o equilíbrio do mundo. Criaturas e pessoas viraram malignas... até a Capitã e a Cozinheira."),
        new Quadro("abertura_6", "Então o Deus da Riqueza apareceu: \"Aquele tesouro é MEU. Traga-o de volta e eu tiro a sua maldição.\""),
        new Quadro("abertura_7", "E assim começa uma nova jornada: atravessar o mar, derrotar o Kraken e recuperar o tesouro."),
    };

    // O último quadro de cada ESCOLHA mostra as opções (na ordem de OpcoesDaEscolha: a 0 é sempre devolver).
    public static readonly Quadro[] Escolha =
    {
        new Quadro("escolha_1", "O Kraken fugiu para o fundo do mar. E ali, no convés, estava o baú: o maior tesouro de todos os tempos."),
        new Quadro("escolha_2", "O Deus da Riqueza desceu das nuvens: \"Muito bem, pirata. Agora devolva o que é MEU, e eu tiro a sua maldição.\""),
        new Quadro("escolha_3", "Marina: \"Devolve, Grumete. Por favor.\"   Louro: \"Rouba. Confia.\""),
    };
    public static readonly string[] OpcoesDaEscolha = { "DEVOLVER", "ROUBAR" };

    public static readonly Quadro[] Final =
    {
        new Quadro("final_1", "O Kraken foi derrotado. O Grumete devolveu o tesouro ao Deus da Riqueza... e a maldição se desfez."),
        new Quadro("final_2", "O mundo voltou ao equilíbrio e todo mundo voltou a ser como era. E o Grumete, enfim, pôde descansar. Por enquanto."),
    };

    public static readonly Quadro[] Roubou =
    {
        new Quadro("roubou_1", "O Grumete fechou o baú, olhou bem nos olhos do Deus da Riqueza... e saiu correndo."),
        new Quadro("roubou_2", "\"LADRÃO!\" O céu fechou. A maldição voltou pior: todo mundo virou maligno de novo, e o Kraken levou a Marina outra vez."),
        new Quadro("roubou_3", "Agora o mar inteiro quer o baú de volta. Em cada ilha, um tsunami. Corre, Grumete. E não olha para trás."),
    };

    public static readonly Quadro[] EscolhaNaTempestade =
    {
        new Quadro("tempestade_1", "O Kraken caiu de novo e a onda quebrou. No meio da tempestade, o Grumete abriu o baú. O ouro nem brilhava mais."),
        new Quadro("tempestade_2", "O Deus da Riqueza apareceu uma última vez: \"Última chance, pirata. Devolva... ou fique com ele PARA SEMPRE.\""),
    };
    public static readonly string[] OpcoesNaTempestade = { "DEVOLVER", "FICAR COM O OURO" };

    public static readonly Quadro[] FinalVerdadeiro =
    {
        new Quadro("verdadeiro_1", "O Grumete devolveu o ouro. Moeda por moeda. A tempestade parou, a maldição se desfez e o mar, enfim, acalmou."),
        new Quadro("final_2", "Deus da Riqueza: \"Foi a volta mais longa que um pirata já deu. Mas você voltou.\" E o Louro contou essa história para todo mundo. Com mentiras, claro."),
    };

    public static readonly Quadro[] FinalDoRei =
    {
        new Quadro("rei_1", "O Grumete ficou com o ouro. Todo ele. Era, enfim, o pirata mais rico de todos os tempos."),
        new Quadro("rei_2", "E reina até hoje, num mar que nunca mais acalmou, sem ninguém para gastar com ele. Nem o Louro ficou. Fim. (juro)"),
    };

    // O texto aparece letra por letra: quantas letras já apareceram depois de "tempo" segundos no quadro.
    public const float EsperaDoTexto = 0.5f;          // o quadro aparece um pouquinho antes do texto
    public const float LetrasPorSegundo = 45f;
    public static int LetrasVisiveis(float tempo) => Mathf.Max(0, Mathf.FloorToInt((tempo - EsperaDoTexto) * LetrasPorSegundo));
    public static float TempoParaTerminar(string texto) => EsperaDoTexto + texto.Length / LetrasPorSegundo + 0.01f;

    // ------------------------------------------------------------------ o Deus da Riqueza (o retrato das falas)

    // O Deus da Riqueza do quadro 6, só do peito para cima (para caber no quadrinho da caixa de fala):
    // a coroa com as joias, a barba branca e o manto roxo com a gola dourada. (as cores são as da FabricaDeSprites)
    static readonly string[] ArteDoDeus =
    {
        "........k..k..k.........",
        ".......kykkykkyk........",
        ".......kyEyyryyk........",
        ".......kyyyyyyyk........",
        "......kkccccccckk.......",
        "......kccccccccCk.......",
        "......kcckccckcCk.......",
        "......kccccccccCk.......",
        "......kwwwcccwwwk.......",
        ".....kwwwwwwwwwwsk......",
        ".....kwwwwwwwwwwsk......",
        "......kwwwwwwwwsk.......",
        "....kkkkwwwwwwskkkk.....",
        "...kVVVykwwwwskyVVVk....",
        "..kVVVVyykwwskyyVVVVk...",
        "..kVVVVVyykkkyyVVVVVk...",
        ".kVVVVVVVyyyyyVVVVVVVk..",
        ".kkkkkkkkkkkkkkkkkkkkk..",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("deus_da_riqueza", () => FabricaDeSprites.DeArte(ArteDoDeus, FabricaDeSprites.Centro));
    }

    // ------------------------------------------------------------------ as imagens dos quadros

    // As imagens (carregadas uma vez só, com o filtro Point: pixel nítido, sem borrar ao ampliar).
    static readonly Dictionary<string, Texture2D> imagens = new Dictionary<string, Texture2D>();

    public static Texture2D Imagem(string nome)
    {
        if (imagens.TryGetValue(nome, out Texture2D pronta) && pronta != null) return pronta;
        var textura = Resources.Load<Texture2D>("Arte/historia/" + nome);
        // as imagens pequenas (as desenhadas por código, 256 x 144) ficam com o pixel nítido; as grandes (pintadas,
        // geradas com os prompts de Ferramentas/historia/prompts_da_historia.md) ficam suaves
        if (textura != null) textura.filterMode = textura.width <= 512 ? FilterMode.Point : FilterMode.Bilinear;
        imagens[nome] = textura;
        return textura;
    }
}
