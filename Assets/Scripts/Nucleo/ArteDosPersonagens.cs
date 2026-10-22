using System.Collections.Generic;
using UnityEngine;

// OS PERSONAGENS (o Grumete, o Louro, os caranguejos, os ratos, a Marina, a Dóris e a moeda), desenhados em
// texto como o resto do jogo, mas com uma paleta própria mais rica: cada material tem TRÊS tons (luz, meio e
// sombra), a luz vem de cima-esquerda e o contorno é escuro. Os nomes dos sprites são os mesmos
// de antes (os outros arquivos pedem com FabricaDeSprites.Pegar("jogador")...), e os tamanhos e pivôs também:
// as caixas de colisão e as posições não mudam.
public static class ArteDosPersonagens
{
    static readonly Dictionary<char, Color32> Paleta = new Dictionary<char, Color32>
    {
        { 'k', new Color32(22, 18, 32, 255) },    // contorno
        { 'K', new Color32(70, 46, 54, 255) },    // contorno suave (por dentro do desenho)
        { 'W', new Color32(252, 250, 242, 255) }, // branco
        { 'w', new Color32(198, 204, 224, 255) }, // branco na sombra
        { 'E', new Color32(28, 24, 46, 255) },    // olho
        { '1', new Color32(255, 226, 194, 255) }, // pele (luz)
        { '2', new Color32(238, 186, 150, 255) }, // pele
        { '3', new Color32(196, 134, 110, 255) }, // pele (sombra)
        { '4', new Color32(246, 142, 134, 255) }, // bochecha corada
        { '5', new Color32(136, 82, 48, 255) },   // cabelo
        { '6', new Color32(90, 52, 36, 255) },    // cabelo (sombra)
        { '7', new Color32(182, 118, 68, 255) },  // cabelo (luz)
        { 'R', new Color32(218, 56, 60, 255) },   // vermelho
        { 'r', new Color32(146, 30, 48, 255) },   // vermelho (sombra)
        { 'Q', new Color32(252, 124, 106, 255) }, // vermelho (luz)
        { 'N', new Color32(58, 82, 160, 255) },   // azul-marinho
        { 'n', new Color32(34, 46, 106, 255) },   // azul-marinho (sombra)
        { 'T', new Color32(186, 136, 88, 255) },  // calça de lona
        { 't', new Color32(124, 86, 56, 255) },   // calça (sombra)
        { 'B', new Color32(84, 54, 38, 255) },    // bota
        { 'b', new Color32(50, 32, 26, 255) },    // bota (sombra)
        { 'G', new Color32(88, 196, 76, 255) },   // verde
        { 'g', new Color32(40, 128, 62, 255) },   // verde (sombra)
        { 'L', new Color32(166, 234, 108, 255) }, // verde (luz)
        { 'Y', new Color32(255, 212, 72, 255) },  // amarelo / ouro
        { 'y', new Color32(214, 146, 38, 255) },  // ouro (sombra)
        { 'D', new Color32(142, 88, 30, 255) },   // ouro (bem escuro)
        { 'X', new Color32(255, 248, 196, 255) }, // ouro (brilho)
        { 'U', new Color32(70, 124, 224, 255) },  // azul
        { 'u', new Color32(40, 72, 158, 255) },   // azul (sombra)
        { 'M', new Color32(168, 166, 186, 255) }, // cinza
        { 'm', new Color32(112, 110, 136, 255) }, // cinza (sombra)
        { 'Z', new Color32(216, 216, 230, 255) }, // cinza (luz)
        { 'p', new Color32(246, 158, 178, 255) }, // rosa
        { 'P', new Color32(238, 214, 255, 255) }, // lilás (luz)
        { 'V', new Color32(196, 150, 240, 255) }, // lilás
        { 'v', new Color32(138, 94, 198, 255) },  // lilás (sombra)
        { 'A', new Color32(58, 192, 174, 255) },  // verde-água
        { 'a', new Color32(30, 124, 130, 255) },  // verde-água (sombra)
        { 'O', new Color32(156, 238, 216, 255) }, // verde-água (luz)
        { 'l', new Color32(232, 236, 252, 255) }, // fantasma (luz)
        { 'i', new Color32(172, 180, 218, 255) }, // fantasma (sombra)
    };

    static Sprite Desenhar(string[] linhas, Vector2 pivo)
    {
        int altura = linhas.Length, largura = linhas[0].Length;
        return FabricaDeSprites.Procedural(largura, altura, pivo, (x, y) =>
        {
            string linha = linhas[altura - 1 - y]; // a primeira linha do texto é o topo
            return x < linha.Length && Paleta.TryGetValue(linha[x], out Color32 cor) ? cor : FabricaDeSprites.Transparente;
        });
    }

    // ---------------------------------------------------------------- o Grumete (16 x 16)
    // Bandana vermelha com bolinha branca (as pontas do nó voando para trás), cabelo castanho, olhos grandes,
    // camisa listrada de marinheiro, calça de lona e botas.
    static readonly string[] Grumete =
    {
        "................",
        ".....kkkkkk.....",
        "...kkRRQQRRkk...",
        "..kRRQQRRRRRRk..",
        "..kRRRRRRRWRRrk.",
        ".kkrrrrrrrrrrrk.",
        "kRk75577555566k.",
        "krk51111111126k.",
        ".kk2111E11E126k.",
        "..k2111E11E122k.",
        "..k21411r14122k.",
        "...kkWWWWWWkk...",
        "..k2kNNNNNNnk2k.",
        "...kkWWWWWwkk...",
        "...ktTTTTTtTtk..",
        "...kBBbk.kBBbk..",
    };

    static readonly string[] GrumeteAndando =
    {
        "................",
        ".....kkkkkk.....",
        "...kkRRQQRRkk...",
        "..kRRQQRRRRRRk..",
        "..kRRRRRRRWRRrk.",
        ".kkrrrrrrrrrrrk.",
        "kRk75577555566k.",
        "krk51111111126k.",
        ".kk2111E11E126k.",
        "..k2111E11E122k.",
        "..k21411r14122k.",
        "...kkWWWWWWkk...",
        "...k1NNNNNNnk2k.",
        "...kkWWWWWwkk...",
        "...ktTTk.kTTtk..",
        "..kBBbk..kBBbk..",
    };

    // ---------------------------------------------------------------- o Louro (16 x 16, olhando para a esquerda)
    // Papagaio verde com a testa clara, o bico amarelo curvo, o peito e a ponta da asa vermelhos e azuis.
    static readonly string[] Louro =
    {
        "................",
        "......kkkk......",
        ".....kLLGGk.....",
        "....kLLGGGGk....",
        "...kWEkGGGGGk...",
        "..kYYkGGGGGGk...",
        ".kYXyYkGGGGGgk..",
        ".kyyykRGGLGGgk..",
        "..kykRRRGGGGggk.",
        "...kRQRRGGGUggk.",
        "...kRRRrGGUUugk.",
        "....krRrGUUuugk.",
        ".....krrkUuukgk.",
        ".....kyk.kuk.kk.",
        "....kyk..kuk....",
        "..........kk....",
    };

    // ---------------------------------------------------------------- o caranguejo (16 x 16)
    // Casco vermelho com brilho em cima, olhos nas antenas, as duas pinças levantadas e as perninhas.
    static readonly string[] Caranguejo =
    {
        "................",
        ".kk..........kk.",
        "kQRk.kW..Wk.kRrk",
        "kRrk.kE..Ek.kRrk",
        ".kRrk.k..k.kRrk.",
        "..kRkkkkkkkkrk..",
        "..kRQQRRRRRRrk..",
        ".kRQQRRRRRRRRrk.",
        ".kRRRRRRRRRRRrk.",
        ".kRRrkkkkkkrrrk.",
        "..krrrrrrrrrrk..",
        "...kkkkkkkkkk...",
        "..kr.kr..rk..rk.",
        ".kr..kr...rk.rk.",
        ".k...k.....k..k.",
        "................",
    };

    // O caranguejo "espinhoso": igualzinho, mas com os espinhos brancos nas costas (não pise nele!).
    static readonly string[] CaranguejoEspinhoso =
    {
        "................",
        ".kk..........kk.",
        "kQRk.kW..Wk.kRrk",
        "kRrk.kE..Ek.kRrk",
        ".kRrkWk..kWkRrk.",
        "..kRkWkWWkWkrk..",
        "..kRQQRRRRRRrk..",
        ".kRQQRRRRRRRRrk.",
        ".kRRRRRRRRRRRrk.",
        ".kRRkWkWkWkWrrk.",
        "..krrrrrrrrrrk..",
        "...kkkkkkkkkk...",
        "..kr.kr..rk..rk.",
        ".kr..kr...rk.rk.",
        ".k...k.....k..k.",
        "................",
    };

    static readonly string[] CaranguejoEsmagado =
    {
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "..kk........kk..",
        ".kQRkkkkkkkkRrk.",
        ".kRQQRRRRRRRRrk.",
        ".kRRrrrrrrrrrrk.",
        "..kkkkkkkkkkkk..",
    };

    // ---------------------------------------------------------------- o rato do porão (16 x 16, olhando para a esquerda)
    static readonly string[] Rato =
    {
        "................",
        "................",
        "................",
        "......kk..kk....",
        "....kZpk.kZpk...",
        "...kZZMMkkMMk...",
        "..kZZMMMMMMMMk..",
        "..kZEkMMMMMMmk..",
        ".kZZZMMMMMMMMmk.",
        "kpZMMMMMMMMMMmk.",
        ".kMMMMMMMMMMmk..",
        "..kmMMMMMMMmmk.p",
        "...kkkkkkkkkk.p.",
        "...kpk..kpk..p..",
        "............p...",
        "................",
    };

    // ---------------------------------------------------------------- a Marina, a sereia (16 x 24)
    // Cabelo lilás comprido com reflexo, conchinhas cor-de-rosa, a cauda verde-água com escamas e a barbatana.
    static readonly string[] Marina =
    {
        ".....kkkkkk.....",
        "...kkPPPVVVkk...",
        "..kPPPVVVVVVvk..",
        ".kPPVVVVVVVVvvk.",
        ".kPVVVVVVVVVVvk.",
        ".kVVVkVVVVkVVvk.",
        ".kVVk111111kVvk.",
        ".kVk11E11E11kvk.",
        ".kVk11E11E11kvk.",
        ".kVk41111114kvk.",
        ".kVVk1122211kvk.",
        ".kVVVVk22kVVVvk.",
        ".kVVkppk2kppkvk.",
        ".kVVk2pp2pp2kvk.",
        "kVVVkAAAAAAAkvvk",
        ".kVkAOAAAOAAakk.",
        ".kVkAAaAAAaAak..",
        "..kAOAAAOAAAak..",
        "...kAAaAAAaak...",
        "....kAOAAAak....",
        ".....kAAaak.....",
        "...kkkOAAOkkk...",
        "..kOAAk..kAAak..",
        "..kkkk....kkkk..",
    };

    // ---------------------------------------------------------------- a Dóris, a fantasminha (16 x 24)
    // Lençol branco com sombra azulada embaixo, a bandana vermelha de pirata e a barra ondulada.
    static readonly string[] Doris =
    {
        "................",
        "................",
        "......kkkk......",
        "....kkRRQRkk....",
        "...kRRQWRRRrk...",
        "..kkrrrrrrrrkk..",
        "..klllllllllik..",
        "..kllEEllEElik..",
        "..kllEWllEWlik..",
        "..klllllllllik..",
        "..kll4lkkl4lik..",
        ".klllllkklllliik",
        "klikllllllllkiik",
        "kik.kllllllk.kik",
        ".k..kllllllk..k.",
        "....klllllik....",
        "...klllllliik...",
        "...kllllllliik..",
        "..kllllllllliik.",
        "..klllllllliiik.",
        ".kllllllllliiiik",
        ".kllkiilkiilkiik",
        ".kk.kk.kk.kk.kk.",
        "................",
    };

    // ---------------------------------------------------------------- a moeda (8 x 14, de dobrão de pirata)
    static readonly string[] Moeda =
    {
        "..kkkk..",
        ".kXYYyk.",
        "kXYYYYyk",
        "kXYyYYDk",
        "kXYyYYDk",
        "kXYyYYDk",
        "kXYyYYDk",
        "kXYyYYDk",
        "kXYyYYDk",
        "kXYyYYDk",
        "kXYyYYDk",
        "kYYYYyDk",
        ".kyyDDk.",
        "..kkkk..",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        Vector2 centro = FabricaDeSprites.Centro, base_ = FabricaDeSprites.Base;
        FabricaDeSprites.Registrar("jogador", () => Desenhar(Grumete, centro));
        FabricaDeSprites.Registrar("jogador_andando", () => Desenhar(GrumeteAndando, centro));
        FabricaDeSprites.Registrar("louro", () => Desenhar(Louro, centro));
        FabricaDeSprites.Registrar("inimigo", () => Desenhar(Caranguejo, centro));
        FabricaDeSprites.Registrar("inimigo_esmagado", () => Desenhar(CaranguejoEsmagado, centro));
        FabricaDeSprites.Registrar("inimigo_espinhos", () => Desenhar(CaranguejoEspinhoso, centro));
        FabricaDeSprites.Registrar("rato", () => Desenhar(Rato, centro));
        FabricaDeSprites.Registrar("sereia", () => Desenhar(Marina, base_));
        FabricaDeSprites.Registrar("fantasma", () => Desenhar(Doris, base_));
        FabricaDeSprites.Registrar("moeda", () => Desenhar(Moeda, centro));
    }
}
