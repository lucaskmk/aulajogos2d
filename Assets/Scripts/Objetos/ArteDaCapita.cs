using System.Collections.Generic;
using UnityEngine;

// Os desenhos da CAPITÃ ÂNCORA (o chefe da fase 6), feitos com texto como o resto do jogo.
// Ela é a capitã pirata: chapéu de três pontas preto com a borda dourada e a caveira na frente, cabelo
// castanho preso, casaco vermelho com o cinto e a fivela dourada, e botas. A arma dela é a ÂNCORA na corrente
// (os desenhos da âncora e dos elos ficam em Ancora.cs).
//
// Cada desenho tem 20 x 30 pixels e o pivô nos PÉS (é mais fácil de pôr no chão).
// Ela é desenhada olhando para a DIREITA; para olhar para a esquerda, a luta usa flipX.
// As 4 primeiras linhas ficam vazias: o topo do chapéu fica na linha 4 (a luta conta com isso para saber
// onde fica o topo da cabeça: ChefeCapita.TopoDaCabeca).
//
// Sprites registrados aqui (use com FabricaDeSprites.Pegar):
//   "capita_parada", "capita_girando", "capita_golpe", "capita_presa", "capita_atordoada"   (normal)
//   "capita_furiosa_parada", "capita_furiosa_girando", ...                                    (FURIOSA: olhos vermelhos)
//   "capita_anel"      o aviso do GIRO: um anel que mostra por onde a âncora vai passar
//   "viga"             as vigas de madeira do teto da arena (onde as âncoras da tempestade cravam)
public static class ArteDaCapita
{
    public const int Largura = 20, Altura = 30;

    // As poses, na mesma ordem do enum ChefeCapita.Pose.
    public static readonly string[] Poses = { "parada", "girando", "golpe", "presa", "atordoada" };

    // A paleta da capitã (cada letra é uma cor).
    static readonly Dictionary<char, Color32> paleta = new Dictionary<char, Color32>
    {
        { 'k', new Color32(20, 20, 28, 255) },    // contorno
        { 'w', new Color32(255, 255, 255, 255) }, // camisa e caveira do chapéu
        { 's', new Color32(205, 205, 215, 255) }, // sombra do branco (botão)
        { 'B', new Color32(120, 78, 48, 255) },   // cabelo castanho
        { 'b', new Color32(78, 48, 30, 255) },    // cabelo (sombra)
        { 'c', new Color32(240, 200, 160, 255) }, // pele
        { 'C', new Color32(206, 156, 120, 255) }, // pele (sombra)
        { 'p', new Color32(232, 140, 130, 255) }, // bochecha corada
        { 'e', new Color32(60, 150, 100, 255) },  // olho verde
        { 'E', new Color32(20, 60, 40, 255) },    // olho (pupila)
        { 'j', new Color32(36, 36, 44, 255) },    // chapéu e calça pretos
        { 'R', new Color32(176, 40, 48, 255) },   // casaco vermelho
        { 'y', new Color32(240, 200, 80, 255) },  // dourado (borda do chapéu, gola e fivela)
        { 'd', new Color32(96, 60, 36, 255) },    // couro (cinto e botas)
        { 'M', new Color32(148, 152, 170, 255) }, // haste de ferro da âncora
        { 'r', new Color32(225, 40, 40, 255) },   // olho FURIOSO
        { 'x', new Color32(255, 140, 120, 255) }, // brilho do olho furioso
        // os tons de luz e de sombra (o sombreamento automático, veja Desenhar)
        { 'Q', new Color32(232, 80, 78, 255) },   // casaco (luz)
        { 'T', new Color32(116, 22, 40, 255) },   // casaco (sombra)
        { 'J', new Color32(76, 74, 100, 255) },   // chapéu (luz)
        { 'Y', new Color32(255, 238, 156, 255) }, // dourado (luz)
        { 'D', new Color32(176, 126, 40, 255) },  // dourado (sombra)
        { '7', new Color32(168, 114, 72, 255) },  // cabelo (luz)
        { '1', new Color32(255, 226, 192, 255) }, // pele (luz)
        { 'z', new Color32(58, 36, 26, 255) },    // couro (sombra)
    };

    // Material -> (tom de luz, tom de sombra) do sombreamento automático.
    static readonly Dictionary<char, (char, char)> Tons = new Dictionary<char, (char, char)>
    {
        { 'R', ('Q', 'T') }, { 'j', ('J', 'j') }, { 'y', ('Y', 'D') }, { 'B', ('7', 'b') },
        { 'c', ('1', 'C') }, { 'w', ('w', 's') }, { 'd', ('d', 'z') },
    };

    // A cabeça (18 linhas): as linhas 0 a 3 vazias, o chapéu de 4 a 8 e o rosto de 9 a 17.
    static readonly string[] Cabeca =
    {
        "....................",
        "....................",
        "....................",
        "....................",
        "......kkkkkkkk......",
        ".....kjjjwwjjjk.....",
        "...kkjjjjkkjjjjkk...",
        ".kyyjjjjjjjjjjjjyyk.",
        "..kkyyyyyyyyyyyykk..",
        "..kBBBBBBBBBBBBBBk..",
        "..kBBBBBccBBBcBBBk..",
        "..kbBBkcccEcccEcck..",
        "..kbBBkcccEcccEcck..",
        "..kbBBkcpccccccpck..",
        "...kbBkccccckkccck..",
        "...kbBBkcccccccck...",
        "....kbBkkCccccCkk...",
        "....kkbk..kCCk......",
    };

    const int LinhaDoOlho = 11, LinhaDaCaveira = 6;

    // Tonta: os olhos ficam "apertados", girando (troca as linhas 10 a 12).
    static readonly string[] OlhoTonto =
    {
        "..kBBBBBccBBBcBBBk..",
        "..kbBBkcckckckckck..",
        "..kbBBkccckcccckcck.",
    };

    // Furiosa: os olhos ficam vermelhos e os olhos da caveira do chapéu acendem.
    static readonly string[] OlhoFurioso = { "..kbBBkcccrcccrcck..", "..kbBBkcccxcccxcck.." };
    static readonly string[] CaveiraFuriosa = { "...kkjjjjrrjjjjkk..." };

    // Os corpos (12 linhas cada), um por pose. As mãos ("kck") e a haste ("M") ficam nos mesmos pixels de
    // sempre: a luta sabe onde a mão está em cada pose (ChefeCapita.Maos).
    static readonly string[] CorpoParada = // segurando a haste da âncora do lado
    {
        ".....kRRwywRRk......",
        "....kRRRyyyRRRk.....",
        "...kRRRRwwwRRRRk....",
        "...kcRkRwwwRkRck....",
        "...kckRRwwwRRkck.M..",
        "....kRRRwwwRRRkkM...",
        "...kRRRRwswRRRRk....",
        "..kRRRRddyddRRRRk...",
        "..kRRRRjjjjjRRRRk...",
        "...kkkkkkkkkkkkk....",
        ".....kjk...kjk......",
        ".....kdk...kdk......",
    };

    static readonly string[] CorpoGirando = // braço levantado, rodando a corrente
    {
        ".....kRRwywRRk.kck..",
        "....kRRRyyyRRRkRk...",
        "...kRRRRwwwRRRRRk...",
        "...kcRkRwwwRkRRk....",
        "...kckRRwwwRRkk.....",
        "....kRRRwwwRRRk.....",
        "...kRRRRwswRRRRk....",
        "..kRRRRddyddRRRRk...",
        "..kRRRRjjjjjRRRRk...",
        "...kkkkkkkkkkkkk....",
        ".....kjk...kjk......",
        ".....kdk...kdk......",
    };

    static readonly string[] CorpoGolpe = // os dois braços para cima (a âncora lá no alto)
    {
        "..kck.kRwywRk.kck...",
        "..kRk.kRyyyRk.kRk...",
        "..kRRkRRwwwRRkRRk...",
        "...kRRkRwwwRkRRk....",
        "....kkRRwwwRRkk.....",
        "....kRRRwwwRRRk.....",
        "...kRRRRwswRRRRk....",
        "..kRRRRddyddRRRRk...",
        "..kRRRRjjjjjRRRRk...",
        "...kkkkkkkkkkkkk....",
        "....kjk.....kjk.....",
        "....kdk.....kdk.....",
    };

    static readonly string[] CorpoPresa = // inclinada para trás, puxando a corrente com as duas mãos
    {
        ".....kRRwywRRkkkk...",
        "....kRRRyyyRRRRRck..",
        "...kRRRRwwwRRkkkkMM.",
        "...kRRkRwwwRkRRRck..",
        "...kRkRRwwwRRkkkk...",
        "...kRRRRwwwRRRk.....",
        "..kRRRRRwswRRRRk....",
        ".kRRRRRddyddRRRRk...",
        ".kRRRRRjjjjjRRRRk...",
        "..kkkkkkkkkkkkkk....",
        "...kjk.....kjk......",
        "..kdk.......kdk.....",
    };

    // atordoada: usa o corpo da "parada" (o que muda é o rosto)
    static readonly string[][] Corpos = { CorpoParada, CorpoGirando, CorpoGolpe, CorpoPresa, CorpoParada };

    // Monta o desenho de uma pose: cabeça + corpo, com as trocas da tontura e da fúria.
    public static string[] Montar(int pose, bool furiosa)
    {
        var linhas = new List<string>(Cabeca);
        if (Poses[pose] == "atordoada")
            for (int i = 0; i < OlhoTonto.Length; i++) linhas[LinhaDoOlho - 1 + i] = OlhoTonto[i];
        if (furiosa)
        {
            for (int i = 0; i < OlhoFurioso.Length; i++) linhas[LinhaDoOlho + i] = OlhoFurioso[i];
            linhas[LinhaDaCaveira] = CaveiraFuriosa[0];
        }
        linhas.AddRange(Corpos[pose]);
        return linhas.ToArray();
    }

    // Igual ao FabricaDeSprites.DeArte, mas com a paleta da capitã e o sombreamento automático
    // (a luz de cima-esquerda nas bordas de cada material: o casco do casaco, o chapéu, o dourado...).
    static Sprite Desenhar(string[] desenho)
    {
        string[] linhas = FabricaDeSprites.Sombrear(desenho, Tons);
        return FabricaDeSprites.Procedural(Largura, Altura, FabricaDeSprites.Base, (x, y) =>
        {
            string linha = linhas[Altura - 1 - y]; // a primeira linha do texto é o topo
            if (x >= linha.Length || !paleta.TryGetValue(linha[x], out Color32 cor)) return FabricaDeSprites.Transparente;
            return cor;
        });
    }

    public static string NomeDoSprite(int pose, bool furiosa) => (furiosa ? "capita_furiosa_" : "capita_") + Poses[pose];

    // Raio do giro (em blocos) que o anel mostra, e a "grossura" da faixa perigosa
    // (raio da âncora + meia largura do Grumete). Quem está DENTRO da faixa é acertado.
    public const float RaioDoAnel = 3f, MeiaFaixaDoAnel = 0.72f;
    const int TamanhoDoAnel = 64;

    // O anel: uma faixa vermelha transparente com as bordas fortes. Fora da faixa (dentro ou fora) é seguro.
    static Color32 CorDoAnel(int x, int y)
    {
        float meio = TamanhoDoAnel / 2f;
        float d = Mathf.Sqrt((x + 0.5f - meio) * (x + 0.5f - meio) + (y + 0.5f - meio) * (y + 0.5f - meio));
        float fora = meio - 0.5f;                                                 // a borda de fora encosta na beira da imagem
        float dentro = fora * (RaioDoAnel - MeiaFaixaDoAnel) / (RaioDoAnel + MeiaFaixaDoAnel);
        if (d > fora + 0.5f || d < dentro - 0.5f) return FabricaDeSprites.Transparente;
        bool borda = d > fora - 1.2f || d < dentro + 1.2f;
        return new Color32(255, 255, 255, (byte)(borda ? 255 : 80)); // branco: a luta pinta de vermelho
    }

    // Quanto o anel precisa ser esticado para a borda de fora ficar no raio certo.
    public static float EscalaDoAnel => (RaioDoAnel + MeiaFaixaDoAnel) * 2f / (TamanhoDoAnel / (float)FabricaDeSprites.PixelsPorUnidade);

    // A viga do teto da arena: madeira com dois pregos.
    static readonly string[] ArteViga =
    {
        "kkkkkkkkkkkkkkkk",
        "knnnnnnnnnnnnnnk",
        "knsnnnbnnnnnsnnk",
        "kbbbbbbbbbbbbbbk",
        "kddddddddddddddk",
        "kkkkkkkkkkkkkkkk",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        for (int pose = 0; pose < Poses.Length; pose++)
            foreach (bool furiosa in new[] { false, true })
            {
                int p = pose; bool f = furiosa; // cópias para a função guardar (cada sprite com a sua pose)
                FabricaDeSprites.Registrar(NomeDoSprite(p, f), () => Desenhar(Montar(p, f)));
            }
        FabricaDeSprites.Registrar("capita_anel", () => FabricaDeSprites.Procedural(TamanhoDoAnel, TamanhoDoAnel, FabricaDeSprites.Centro, CorDoAnel));
        FabricaDeSprites.Registrar("viga", () => FabricaDeSprites.DeArte(ArteViga, FabricaDeSprites.Centro));
    }
}
