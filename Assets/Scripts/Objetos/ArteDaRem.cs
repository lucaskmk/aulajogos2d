using System.Collections.Generic;
using UnityEngine;

// Os desenhos da REM (o chefe da fase 6), feitos com texto como o resto do jogo.
// Ela é a empregada de cabelo AZUL da mansão Roswaal: vestido preto, avental branco, laço lilás,
// a tiara de babados na cabeça e a franja cobrindo um dos olhos. A arma dela é o MANGUAL
// (a bola de espinhos com corrente; os desenhos da bola e dos elos ficam em MangualDaRem.cs).
//
// Cada desenho tem 20 x 30 pixels e o pivô nos PÉS (é mais fácil de pôr no chão).
// Ela é desenhada olhando para a DIREITA; para olhar para a esquerda, a luta usa flipX.
// As 4 primeiras linhas ficam vazias: é o espaço do CHIFRE de oni (etapa 3).
//
// Sprites registrados aqui (use com FabricaDeSprites.Pegar):
//   "rem_parada", "rem_girando", "rem_golpe", "rem_presa", "rem_atordoada"   (Rem normal)
//   "rem_oni_parada", "rem_oni_girando", ...                                  (Rem ONI: chifre e olho vermelho)
//   "rem_anel"   o aviso do GIRO: um anel que mostra por onde a bola vai passar
public static class ArteDaRem
{
    public const int Largura = 20, Altura = 30;

    // As poses, na mesma ordem do enum ChefeRem.Pose.
    public static readonly string[] Poses = { "parada", "girando", "golpe", "presa", "atordoada" };

    // A paleta da Rem (cada letra é uma cor). Ela tem a própria paleta porque precisa de vários azuis.
    static readonly Dictionary<char, Color32> paleta = new Dictionary<char, Color32>
    {
        { 'k', new Color32(20, 20, 28, 255) },    // contorno
        { 'w', new Color32(255, 255, 255, 255) }, // avental e tiara
        { 's', new Color32(205, 205, 215, 255) }, // sombra do branco
        { 'B', new Color32(110, 160, 240, 255) }, // cabelo azul
        { 'b', new Color32(60, 100, 200, 255) },  // cabelo (sombra)
        { 'L', new Color32(175, 210, 255, 255) }, // cabelo (brilho)
        { 'c', new Color32(252, 220, 186, 255) }, // pele
        { 'C', new Color32(222, 176, 146, 255) }, // pele (sombra)
        { 'p', new Color32(255, 150, 170, 255) }, // bochecha corada
        { 'e', new Color32(70, 120, 230, 255) },  // olho azul
        { 'E', new Color32(30, 50, 140, 255) },   // olho (pupila)
        { 'j', new Color32(36, 36, 44, 255) },    // vestido preto
        { 'R', new Color32(190, 110, 200, 255) }, // laço lilás
        { 'M', new Color32(148, 152, 170, 255) }, // cabo de ferro do mangual
        { 'r', new Color32(220, 45, 45, 255) },   // olho de ONI
        { 'x', new Color32(255, 120, 120, 255) }, // brilho do olho de oni
    };

    // A cabeça (18 linhas). As linhas 0 a 3 ficam vazias para o chifre.
    static readonly string[] Cabeca =
    {
        "....................",
        "....................",
        "....................",
        "....................",
        "......kkkkkkkk......",
        ".....kwkwkwkwwk.....",
        "....kbwwwwwwwwBk....",
        "...kBBBBBBBBBBBBk...",
        "..kBBLLBBBBBBBBBBk..",
        "..kBLBBBBBBBBBBBBk..",
        "..kBBBBBBBBBBbBBBk..",
        "..kBBBBBBBBBbcbBBk..",
        "..kbBBBBBBbcccbBBk..",
        "..kbBBBBBbccccccBk..",
        "..kbBBBBbcccceEcBk..",
        "..kbBBBBccccceecck..",
        "...kbBBbpccccccpk...",
        "....kkbkcccCccck....",
    };

    // Os corpos (12 linhas cada), um por pose.
    static readonly string[][] Corpos =
    {
        new[] // parada: segurando o cabo do mangual do lado
        {
            ".....kjjwRwjjk......",
            "....kjjjRRRjjjk.....",
            "...kjjjwwwwwjjjk....",
            "...kcjkwwwwwkjck....",
            "...kckwwwwwwwkck.M..",
            "....kjwwwwwwwjkkM...",
            "...kjjwwwswwwjjk....",
            "..kjjjjwwwwwjjjjk...",
            "..kjjjjjjjjjjjjjk...",
            "...kkkkkkkkkkkkk....",
            ".....kwk...kwk......",
            ".....kjk...kjk......",
        },
        new[] // girando: braço levantado, rodando a corrente
        {
            ".....kjjwRwjjk.kck..",
            "....kjjjRRRjjjkjk...",
            "...kjjjwwwwwjjjjk...",
            "...kcjkwwwwwkjjk....",
            "...kckwwwwwwwkk.....",
            "....kjwwwwwwwjk.....",
            "...kjjwwwswwwjjk....",
            "..kjjjjwwwwwjjjjk...",
            "..kjjjjjjjjjjjjjk...",
            "...kkkkkkkkkkkkk....",
            ".....kwk...kwk......",
            ".....kjk...kjk......",
        },
        new[] // golpe: os dois braços para cima (a bola lá no alto)
        {
            "..kck.kjwRwjk.kck...",
            "..kjk.kjRRRjk.kjk...",
            "..kjjkjwwwwwjkjjk...",
            "...kjjkwwwwwkjjk....",
            "....kkwwwwwwwkk.....",
            "....kjwwwwwwwjk.....",
            "...kjjwwwswwwjjk....",
            "..kjjjjwwwwwjjjjk...",
            "..kjjjjjjjjjjjjjk...",
            "...kkkkkkkkkkkkk....",
            "....kwk.....kwk.....",
            "....kjk.....kjk.....",
        },
        new[] // presa: inclinada para trás, puxando a corrente com as duas mãos
        {
            ".....kjjwRwjjkkkk...",
            "....kjjjRRRjjjjjck..",
            "...kjjjwwwwwjkkkkMM.",
            "...kjjkwwwwwkjjjck..",
            "...kjkwwwwwwwkkkk...",
            "...kjjwwwwwwwjk.....",
            "..kjjjwwwswwwjjk....",
            ".kjjjjjwwwwwjjjjk...",
            ".kjjjjjjjjjjjjjjk...",
            "..kkkkkkkkkkkkkk....",
            "...kwk.....kwk......",
            "..kjk.......kjk.....",
        },
        null, // atordoada: usa o corpo da "parada" (o que muda é o rosto)
    };

    // Monta o desenho de uma pose: cabeça + corpo, com as trocas da tontura e do oni.
    public static string[] Montar(int pose, bool oni)
    {
        var linhas = new List<string>(Cabeca);
        if (Poses[pose] == "atordoada") // olho em X (tonta)
        {
            linhas[14] = "..kbBBBBbccckckcBk..";
            linhas[15] = "..kbBBBBccccckccck..";
            linhas[16] = "...kbBBbpccckckpk...";
        }
        if (oni)
        {
            // o chifre branco brota da testa (por cima da tiara)
            linhas[0] = "..........kk........";
            linhas[1] = ".........kwk........";
            linhas[2] = ".........kwsk.......";
            linhas[3] = "........kwwsk.......";
            linhas[4] = "......kkkwwskk......";
            linhas[14] = linhas[14].Replace("eE", "rx"); // o olho fica vermelho
            linhas[15] = linhas[15].Replace("ee", "rr");
        }
        linhas.AddRange(Corpos[pose] ?? Corpos[0]);
        return linhas.ToArray();
    }

    // Igual ao FabricaDeSprites.DeArte, mas com a paleta da Rem.
    static Sprite Desenhar(string[] linhas)
    {
        return FabricaDeSprites.Procedural(Largura, Altura, FabricaDeSprites.Base, (x, y) =>
        {
            string linha = linhas[Altura - 1 - y]; // a primeira linha do texto é o topo
            if (x >= linha.Length || !paleta.TryGetValue(linha[x], out Color32 cor)) return FabricaDeSprites.Transparente;
            return cor;
        });
    }

    public static string NomeDoSprite(int pose, bool oni) => (oni ? "rem_oni_" : "rem_") + Poses[pose];

    // Raio do giro (em blocos) que o anel mostra, e a "grossura" da faixa perigosa
    // (raio da bola + meia largura do Subaru). Quem está DENTRO da faixa é acertado.
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        for (int pose = 0; pose < Poses.Length; pose++)
            foreach (bool oni in new[] { false, true })
            {
                int p = pose; bool o = oni; // cópias para a função guardar (cada sprite com a sua pose)
                FabricaDeSprites.Registrar(NomeDoSprite(p, o), () => Desenhar(Montar(p, o)));
            }
        FabricaDeSprites.Registrar("rem_anel", () => FabricaDeSprites.Procedural(TamanhoDoAnel, TamanhoDoAnel, FabricaDeSprites.Centro, CorDoAnel));
    }
}
