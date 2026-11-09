using System.Collections.Generic;
using UnityEngine;

// Os desenhos da LOJA DO PORTO: o frasco do elixir de super salto, a bota alada do duplo salto, a ROLETA
// (15 casas: a verde lá em cima e as vermelhas e pretas alternando, com o aro dourado e o cubo de madeira)
// e o ponteiro dela. O escudo fica na FabricaDeSprites (é do colega que fez a Loja primeiro).
public static class ArteDaLoja
{
    static readonly Dictionary<char, Color32> Paleta = new Dictionary<char, Color32>
    {
        { 'k', new Color32(22, 18, 32, 255) },    // contorno
        { 'W', new Color32(252, 250, 242, 255) }, // branco
        { 'w', new Color32(196, 202, 224, 255) }, // branco na sombra
        { 'Z', new Color32(170, 214, 236, 255) }, // vidro
        { 'z', new Color32(104, 150, 186, 255) }, // vidro (sombra)
        { 'Y', new Color32(255, 212, 72, 255) },  // dourado
        { 'y', new Color32(214, 146, 38, 255) },  // dourado (sombra)
        { 'D', new Color32(142, 88, 30, 255) },   // dourado (bem escuro)
        { 'X', new Color32(255, 248, 196, 255) }, // brilho
        { 'B', new Color32(150, 92, 56, 255) },   // couro / rolha
        { 'b', new Color32(92, 54, 36, 255) },    // couro (sombra)
        { '7', new Color32(196, 132, 84, 255) },  // couro (luz)
        { 'R', new Color32(232, 62, 70, 255) },   // poção dos espinhos
        { 'r', new Color32(164, 34, 52, 255) },   // poção dos espinhos (sombra)
        { 'd', new Color32(102, 20, 38, 255) },   // poção dos espinhos (bem escuro)
        { 'Q', new Color32(255, 186, 176, 255) }, // poção dos espinhos (brilho)
        { 'G', new Color32(84, 204, 104, 255) },  // poção do chão
        { 'g', new Color32(40, 140, 76, 255) },   // poção do chão (sombra)
        { 'h', new Color32(24, 88, 58, 255) },    // poção do chão (bem escuro)
        { 'H', new Color32(202, 246, 182, 255) }, // poção do chão (brilho)
    };

    static Sprite Desenhar(string[] linhas, Vector2 pivo)
    {
        int altura = linhas.Length, largura = linhas[0].Length;
        return FabricaDeSprites.Procedural(largura, altura, pivo, (x, y) =>
        {
            string linha = linhas[altura - 1 - y];
            return x < linha.Length && Paleta.TryGetValue(linha[x], out Color32 cor) ? cor : FabricaDeSprites.Transparente;
        });
    }

    // O elixir de super salto: um frasco de vidro com o líquido dourado borbulhando e a rolha de cortiça.
    static readonly string[] Elixir =
    {
        "....kkkk....",
        "....kB7k....",
        "....kbBk....",
        "...kkZZkk...",
        "....kZzk....",
        "...kZWZzk...",
        "..kZWYYYzk..",
        ".kZWYXYYYyk.",
        ".kZYYYYXYyk.",
        ".kZYXYYYyyk.",
        ".kzYYYYyyDk.",
        "..kyyyyyDk..",
        "...kkkkkk...",
    };

    // As poções de visão são o mesmo frasco do elixir, com outro líquido.
    static string[] Frasco(string dourado, string liquido)
    {
        var linhas = new string[Elixir.Length];
        for (int i = 0; i < linhas.Length; i++)
        {
            linhas[i] = Elixir[i];
            for (int c = 0; c < dourado.Length; c++) linhas[i] = linhas[i].Replace(dourado[c], liquido[c]);
        }
        return linhas;
    }

    // O duplo salto: a bota de couro com uma asinha branca do lado.
    static readonly string[] BotaAlada =
    {
        "..kk..........",
        ".kWWk.........",
        "kWWWWk.kkkk...",
        "kWwWWkkB7BBk..",
        ".kWwWkB7BBbk..",
        "..kkWkB7BBbk..",
        "....kkBBBBbk..",
        ".....kBBBBbkk.",
        ".....kB7BBBBbk",
        ".....kBBBBBBbk",
        ".....kbbbbbbbk",
        "......kkkkkkk.",
    };

    // O ponteiro da roleta (fica parado lá em cima, apontando para baixo).
    static readonly string[] Ponteiro =
    {
        "kkkkkkkkkk",
        "kXYYYYYYyk",
        ".kYYYYYyk.",
        ".kYYYYYyk.",
        "..kYYYyk..",
        "..kYYYyk..",
        "...kYyk...",
        "...kYyk...",
        "....kk....",
    };

    // ---------------------------------------------------------------- a roleta

    public const int TamanhoDaRoleta = 96; // pixels de lado

    static readonly Color32 Vermelho = new Color32(206, 42, 52, 255), VermelhoClaro = new Color32(240, 92, 88, 255);
    static readonly Color32 Preto = new Color32(36, 32, 46, 255), PretoClaro = new Color32(76, 70, 96, 255);
    static readonly Color32 Verde = new Color32(36, 168, 84, 255), VerdeClaro = new Color32(104, 222, 130, 255);

    // A casa em que um ponto da roleta está (o ângulo conta no sentido do relógio, a partir de cima: a casa 0
    // fica bem no topo). Igual ao GerenciadorDoJogo.CorDaCasa.
    static int CasaNoAngulo(float graus)
    {
        float tamanho = 360f / GerenciadorDoJogo.CasasDaRoleta;
        return Mathf.FloorToInt(Mathf.Repeat(graus + tamanho / 2f, 360f) / tamanho) % GerenciadorDoJogo.CasasDaRoleta;
    }

    static Color32 CorDaRoleta(int x, int y)
    {
        float meio = TamanhoDaRoleta / 2f;
        float dx = x + 0.5f - meio, dy = y + 0.5f - meio;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        float graus = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg; // 0 = em cima, 90 = à direita (sentido do relógio)
        float luz = (-dx + dy) / meio; // de -1 a 1: a luz vem de cima-esquerda
        if (r > meio - 0.5f) return FabricaDeSprites.Transparente;
        if (r > meio - 1.6f) return Paleta['k'];
        if (r > meio - 6f) // o aro dourado, com os "pinos" entre as casas
        {
            float tamanho = 360f / GerenciadorDoJogo.CasasDaRoleta;
            bool pino = Mathf.Abs(Mathf.Repeat(graus + tamanho / 2f, tamanho) - tamanho / 2f) < 3.2f && r > meio - 5f && r < meio - 2.5f;
            if (pino) return Paleta['X'];
            return luz > 0.35f ? Paleta['Y'] : luz > -0.4f ? Paleta['y'] : Paleta['D'];
        }
        if (r > meio - 7f) return Paleta['k'];
        if (r > 16f) // as casas
        {
            float tamanho = 360f / GerenciadorDoJogo.CasasDaRoleta;
            float dentroDaCasa = Mathf.Repeat(graus + tamanho / 2f, tamanho);
            if (dentroDaCasa < 1.6f) return Paleta['y']; // o filete dourado entre as casas
            int casa = CasaNoAngulo(graus);
            GerenciadorDoJogo.CorDaRoleta cor = GerenciadorDoJogo.CorDaCasa(casa);
            bool clara = r > meio - 11f && dentroDaCasa < tamanho * 0.55f; // um brilho na borda de fora
            switch (cor)
            {
                case GerenciadorDoJogo.CorDaRoleta.Verde: return clara ? VerdeClaro : Verde;
                case GerenciadorDoJogo.CorDaRoleta.Vermelho: return clara ? VermelhoClaro : Vermelho;
                default: return clara ? PretoClaro : Preto;
            }
        }
        if (r > 14.5f) return Paleta['k'];
        // o cubo de madeira no meio, com quatro raios dourados e o botão no centro
        if (r < 3.5f) return luz > 0f ? Paleta['X'] : Paleta['Y'];
        if (r < 4.6f) return Paleta['k'];
        bool raio = (Mathf.Abs(dx) < 1.1f || Mathf.Abs(dy) < 1.1f) && r < 13f;
        if (raio) return luz > 0f ? Paleta['Y'] : Paleta['y'];
        int veio = ((int)(r * 1.3f) + (graus > 0f ? 1 : 0)) % 3;
        return luz > 0.3f ? Paleta['7'] : veio == 0 ? Paleta['b'] : Paleta['B'];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Registrar()
    {
        FabricaDeSprites.Registrar("elixir", () => Desenhar(Elixir, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("pocao_espinhos", () => Desenhar(Frasco("YyDX", "RrdQ"), FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("pocao_chao", () => Desenhar(Frasco("YyDX", "GghH"), FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("bota_alada", () => Desenhar(BotaAlada, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("ponteiro_da_roleta", () => Desenhar(Ponteiro, new Vector2(0.5f, 1f)));
        FabricaDeSprites.Registrar("roleta", () =>
        {
            Sprite roleta = FabricaDeSprites.Procedural(TamanhoDaRoleta, TamanhoDaRoleta, FabricaDeSprites.Centro, CorDaRoleta);
            roleta.texture.filterMode = FilterMode.Bilinear; // ela GIRA: com o filtro suave, o pixel não "tremelica"
            return roleta;
        });
    }
}
