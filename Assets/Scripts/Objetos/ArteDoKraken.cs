using System;
using System.Collections.Generic;
using UnityEngine;

// Os desenhos do KRAKEN VERMELHO (o chefe final, ChefeKraken.cs): a lula gigante vermelha que mandou a Baleia
// e o tsunami atrás do Grumete. Tudo é desenhado por código (pixel art "matemática", como a borda do tsunami):
//  - "kraken_corpo_normal", "_brava" e "_dor": o corpo inteiro (6 x 8 blocos), saindo da poça: o manto pontudo
//    com as barbatanas em cima, os olhos enormes e os braços descendo para a água;
//  - "kraken_cansada": ela deitada no convés (o manto na horizontal), tonta, com o olho em espiral;
//  - "kraken_gomo" / "kraken_ponta" (e as versões "_amarelo", do agarrão que rouba moedas): os tentáculos;
//  - "kraken_tinta", "kraken_mancha", "kraken_saco": a bola de tinta, a mancha que ela deixa e o saco de moedas;
//  - "kraken": o ícone do mapa do mundo (o mesmo desenho do corpo, pequenininho).
// A PALETA dela também pinta os outros tentáculos do jogo (o do cano e os da tela de morte): veja Pintar.
public static class ArteDoKraken
{
    public enum Olhar { Normal, Brava, Dor }

    // Os vermelhos da lula (do contorno até o brilho), as ventosas e os olhos.
    public static readonly Color32 Contorno = new Color32(30, 6, 12, 255);
    public static readonly Color32 Funda = new Color32(92, 12, 26, 255);
    public static readonly Color32 Vermelha = new Color32(176, 30, 42, 255);
    public static readonly Color32 Clara = new Color32(230, 72, 64, 255);
    public static readonly Color32 Brilho = new Color32(255, 132, 110, 255);
    public static readonly Color32 Ventosa = new Color32(255, 178, 160, 255);
    static readonly Color32 VentosaFunda = new Color32(214, 108, 104, 255);
    static readonly Color32 Iris = new Color32(255, 206, 72, 255);
    static readonly Color32 IrisBrava = new Color32(255, 84, 36, 255);
    static readonly Color32 Pupila = new Color32(16, 6, 10, 255);
    static readonly Color32 Branco = new Color32(255, 255, 255, 255);
    static readonly Color32 Amarela = new Color32(250, 196, 48, 255);
    static readonly Color32 AmarelaClara = new Color32(255, 236, 130, 255);
    static readonly Color32 AmarelaFunda = new Color32(196, 120, 20, 255);
    static readonly Color32 Tinta = new Color32(36, 10, 30, 255);
    static readonly Color32 TintaBrilho = new Color32(110, 40, 90, 255);

    // Os desenhos em texto que usam as letras de tentáculo (x, v, V, P) viram vermelhos com isto.
    static readonly Dictionary<char, Color32> PaletaDoTentaculo = new Dictionary<char, Color32>
    {
        { 'x', Funda }, { 'v', Vermelha }, { 'V', Clara }, { 'P', Ventosa }, { 'k', Contorno },
    };

    // Um desenho em texto com as cores da lula (as letras que não são de tentáculo usam a paleta normal do jogo).
    public static Sprite Pintar(string[] linhas, Vector2 pivo)
    {
        int altura = linhas.Length, largura = 0;
        foreach (string linha in linhas) largura = Mathf.Max(largura, linha.Length);
        return FabricaDeSprites.Procedural(largura, altura, pivo, (x, y) =>
        {
            string linha = linhas[altura - 1 - y];
            if (x >= linha.Length) return FabricaDeSprites.Transparente;
            char c = linha[x];
            return PaletaDoTentaculo.TryGetValue(c, out Color32 cor) ? cor : FabricaDeSprites.CorOuTransparente(c);
        });
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        var baseDoCorpo = new Vector2(0.5f, 0f);
        foreach (Olhar olhar in Enum.GetValues(typeof(Olhar)))
        {
            Olhar esse = olhar;
            FabricaDeSprites.Registrar("kraken_corpo_" + esse.ToString().ToLowerInvariant(), () => Desenhar(96, 128, 96f, 128f, baseDoCorpo, (px, py) => Corpo(px, py, esse)));
        }
        FabricaDeSprites.Registrar("kraken", () => Desenhar(24, 32, 96f, 128f, FabricaDeSprites.Centro, (px, py) => Corpo(px, py, Olhar.Brava)));
        // deitada: o pivô fica embaixo, na ponta da direita (encostada na poça)
        FabricaDeSprites.Registrar("kraken_cansada", () => Desenhar(112, 56, 112f, 56f, new Vector2(1f, 0f), Cansada));

        Vector2 centro = FabricaDeSprites.Centro;
        FabricaDeSprites.Registrar("kraken_gomo", () => FabricaDeSprites.Procedural(14, 14, centro, (x, y) => Gomo(x, y, false)));
        FabricaDeSprites.Registrar("kraken_gomo_amarelo", () => FabricaDeSprites.Procedural(14, 14, centro, (x, y) => Gomo(x, y, true)));
        FabricaDeSprites.Registrar("kraken_ponta", () => Pintar(ArtePonta, centro));
        FabricaDeSprites.Registrar("kraken_ponta_amarela", () => FabricaDeSprites.DeArte(ArtePontaAmarela, centro));
        FabricaDeSprites.Registrar("kraken_saco", () => FabricaDeSprites.DeArte(ArteSaco, centro));
        FabricaDeSprites.Registrar("kraken_tinta", () => FabricaDeSprites.Procedural(12, 12, centro, BolaDeTinta));
        FabricaDeSprites.Registrar("kraken_mancha", () => FabricaDeSprites.Procedural(28, 6, new Vector2(0.5f, 0f), Mancha));
        // a TINTA NA CARA (os borrões grudados na tela, três formas) e a MARÉ DE TINTA (1 bloco de largura: esticada na arena)
        for (int i = 0; i < 3; i++)
        {
            int forma = i;
            FabricaDeSprites.Registrar("kraken_borrao_" + forma, () => FabricaDeSprites.Procedural(64, 64, FabricaDeSprites.Centro, (x, y) => Borrao(x, y, forma)));
        }
        FabricaDeSprites.Registrar("kraken_mare", () => FabricaDeSprites.Procedural(16, 16, new Vector2(0.5f, 0f), Mare));
    }

    // ------------------------------------------------------------------ o desenhista

    // "cor(px, py)": a cor do desenho naquele ponto (null = vazio), com px e py nas unidades do desenho
    // (o corpo é sempre pensado em 96 x 128, seja qual for o tamanho de verdade: o ícone usa o mesmo desenho).
    // Em volta de tudo, o contorno.
    static Sprite Desenhar(int largura, int altura, float unidadesX, float unidadesY, Vector2 pivo, Func<float, float, Color32?> cor)
    {
        float sx = unidadesX / largura, sy = unidadesY / altura;
        var cores = new Color32?[largura, altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                cores[x, y] = cor((x + 0.5f) * sx, (y + 0.5f) * sy);
        bool Cheio(int x, int y) => x >= 0 && y >= 0 && x < largura && y < altura && cores[x, y].HasValue;
        return FabricaDeSprites.Procedural(largura, altura, pivo, (x, y) =>
        {
            if (cores[x, y].HasValue) return cores[x, y].Value;
            bool encostado = Cheio(x + 1, y) || Cheio(x - 1, y) || Cheio(x, y + 1) || Cheio(x, y - 1);
            return encostado ? Contorno : FabricaDeSprites.Transparente;
        });
    }

    // ------------------------------------------------------------------ o corpo

    // A forma do corpo (em pé), em unidades de 96 x 128: o manto, as barbatanas, a cabeça e os seis braços.
    static bool DentroDoCorpo(float px, float py)
    {
        float dx = Mathf.Abs(px - 48f);
        if (py >= 44f && py <= 123f)
        {
            float t = (py - 44f) / 79f;
            float manto = 27f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(t, 1.7f)), 0.6f) + 1.5f;
            if (dx < manto) return true;
            // as barbatanas: dois losangos nos lados, lá em cima
            float barbatana = 15f * Mathf.Max(0f, 1f - Mathf.Abs(py - 104f) / 17f);
            if (py > 86f && dx < manto + barbatana) return true;
        }
        if (py >= 22f && py <= 50f && dx < 27f + 3f * Mathf.Sin((py - 22f) / 28f * Mathf.PI)) return true; // a cabeça
        if (py < 32f) return Braco(px, py, out _, out _) >= 0;
        return false;
    }

    // Em qual braço o ponto está (-1 = nenhum), com o centro e a grossura dele nessa altura.
    static int Braco(float px, float py, out float centro, out float grossura)
    {
        centro = 0f;
        grossura = 0f;
        for (int i = 0; i < 6; i++)
        {
            float k = i - 2.5f;
            float c = 48f + k * 8.5f + k * (32f - py) * 0.33f + 2.2f * Mathf.Sin(py * 0.22f + k * 1.3f);
            float g = Mathf.Max(1.5f, 5.4f - (32f - py) * 0.08f);
            if (Mathf.Abs(px - c) < g)
            {
                centro = c;
                grossura = g;
                return i;
            }
        }
        return -1;
    }

    static Color32? Corpo(float px, float py, Olhar olhar)
    {
        if (!DentroDoCorpo(px, py)) return null;

        // os olhos (enormes) e as sobrancelhas da brava
        foreach (float ox in new[] { 34f, 62f })
        {
            Color32? olho = Olho(px - ox, py - 38f, olhar, ox < 48f);
            if (olho.HasValue) return olho;
        }

        // as ventosas, em fileira na parte de dentro dos braços
        if (py < 30f && Braco(px, py, out float centro, out float grossura) >= 0)
        {
            float lado = centro < 48f ? 1f : -1f;
            if (Mathf.Repeat(py, 5f) < 2.4f && Mathf.Abs(px - (centro + lado * grossura * 0.4f)) < 1.3f)
                return Mathf.Repeat(py, 5f) < 1.2f ? Ventosa : VentosaFunda;
        }

        // as manchinhas escuras do manto (as lulas têm "pintas" que mudam de cor)
        if (py > 52f)
        {
            float n = Mathf.Sin(px * 0.55f + py * 0.13f) + Mathf.Sin(py * 0.41f - px * 0.21f) + Mathf.Sin((px + py) * 0.17f);
            if (n > 2.05f) return Funda;
        }

        return Luz(px, py, DentroDoCorpo, py < 14f);
    }

    // O sombreado (a luz vem de cima, da esquerda): borda da esquerda/de cima clara, da direita/de baixo escura.
    static Color32 Luz(float px, float py, Func<float, float, bool> dentro, bool fundo)
    {
        bool escuro = !dentro(px + 3.5f, py) || !dentro(px, py - 3.5f);
        bool claro = !dentro(px - 3f, py) || !dentro(px, py + 3f);
        bool brilho = !dentro(px - 1.5f, py + 1.5f) && claro;
        if (escuro && !claro) return Funda;
        if (brilho) return Brilho;
        if (claro) return Clara;
        if (fundo) return ((int)px + (int)py) % 2 == 0 ? Vermelha : Funda; // os braços vão escurecendo na água
        return Vermelha;
    }

    // Um olho (dx, dy a partir do centro dele). esquerdo: o olho da esquerda (as sobrancelhas espelham).
    static Color32? Olho(float dx, float dy, Olhar olhar, bool esquerdo)
    {
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (olhar == Olhar.Brava)
        {
            // a sobrancelha: uma faixa escura em cima do olho, descendo para o meio do rosto
            float lado = esquerdo ? 1f : -1f;
            float linha = 10.5f - dx * lado * 0.35f;
            if (Mathf.Abs(dy - linha) < 1.8f && Mathf.Abs(dx) < 9f) return Contorno;
        }
        if (d > 8.2f) return null;
        if (olhar == Olhar.Dor)
        {
            // apertado de dor: um "> <" no lugar do olho
            float lado = esquerdo ? 1f : -1f;
            float v = Mathf.Abs(dy) * 0.9f - dx * lado * 0.5f;
            if (Mathf.Abs(v - 2.2f) < 1.3f && Mathf.Abs(dx) < 6f) return Contorno;
            return d > 7f ? Funda : Vermelha;
        }
        if (d > 6.8f) return Funda;                            // a pálpebra
        if (Mathf.Abs(dy) < 1.6f && Mathf.Abs(dx) < 5.4f) return Pupila;  // a pupila: um risco deitado (de polvo!)
        if (Mathf.Abs(dx + 2.5f) < 1.2f && Mathf.Abs(dy - 3.3f) < 1.2f) return Branco; // o brilhinho
        Color32 iris = olhar == Olhar.Brava ? IrisBrava : Iris;
        return d > 5.5f ? Color32.Lerp(iris, Funda, 0.45f) : iris;
    }

    // ------------------------------------------------------------------ deitada (cansada)

    // O manto deitado no convés, apontando para a esquerda, em unidades de 112 x 56. Vira o desenho do corpo
    // de lado: o "para cima" do corpo é o "para a esquerda" daqui. O olho é uma espiral (tonta).
    static bool DentroDaCansada(float lx, float ly) => DentroDoCorpo(48f + (ly - 13f), 126f - lx);

    static Color32? Cansada(float lx, float ly)
    {
        if (!DentroDaCansada(lx, ly)) return null;
        float dx = lx - 88f, dy = ly - 30f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d < 8.2f)
        {
            if (d > 6.8f) return Funda;
            // a espiral: três voltas, do meio para fora
            float angulo = Mathf.Atan2(dy, dx) / (2f * Mathf.PI) + 0.5f;
            float espiral = Mathf.Repeat(angulo + d / 2.3f, 1f);
            return espiral < 0.36f ? Pupila : Iris;
        }
        float px = 48f + (ly - 13f), py = 126f - lx;
        if (py > 52f)
        {
            float n = Mathf.Sin(px * 0.55f + py * 0.13f) + Mathf.Sin(py * 0.41f - px * 0.21f) + Mathf.Sin((px + py) * 0.17f);
            if (n > 2.05f) return Funda;
        }
        return Luz(lx, ly, DentroDaCansada, false);
    }

    // ------------------------------------------------------------------ tentáculos e o resto

    // Um "gomo" do braço: bolinha vermelha (ou amarela, no agarrão) com brilho em cima e sombra embaixo.
    static Color32 Gomo(int x, int y, bool amarelo)
    {
        float dx = x - 6.5f, dy = y - 6.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 7f) return FabricaDeSprites.Transparente;
        if (d > 6f) return Contorno;
        Color32 escura = amarelo ? AmarelaFunda : Funda, meio = amarelo ? Amarela : Vermelha, clara = amarelo ? AmarelaClara : Clara;
        if (amarelo && Mathf.Repeat(x + y * 0.5f, 6f) < 1.5f) return Vermelha; // listras vermelhas (é dela, mas é diferente)
        if (dy < -2.5f || dx > 3.5f) return escura;
        if (dy > 2.5f && dx < 1f) return clara;
        return meio;
    }

    // A ponta do tentáculo (o "taco" das lulas), apontando para a direita, com as ventosas.
    static readonly string[] ArtePonta =
    {
        "......kkkkk.......",
        "....kkvvvvvkk.....",
        "...kvVVVVvvvvkk...",
        "..kvVVvvvvvvvvvkk.",
        ".kvVvvPkPvvPkPvvvk",
        "kvVvvvPPPvvPPPvvxk",
        "kvvvvvvvvvvvvvvxxk",
        "kvvPkPvvPkPvvxxxk.",
        ".kvPPPvvPPPvxxkk..",
        "..kxxvvvvxxxkk....",
        "...kkxxxxxkk......",
        ".....kkkkk........",
    };

    // A ponta do AGARRÃO: amarela, com a "mão" aberta para pegar as moedas.
    static readonly string[] ArtePontaAmarela =
    {
        "............kk....",
        "......kkkkkkyyk...",
        "....kkyyyyyyyyk...",
        "...kyyyyyyyyyk....",
        "..kyyrrryyyyykk...",
        ".kyyyyyyyyyyyyyk..",
        "kyyrrryyyyrrryyyk.",
        "koyyyyyyyyyyyyyok.",
        ".koyyrrryyyyyook..",
        "..kooyyyyyyyook...",
        "...kkooooookkyyk..",
        ".....kkkkkk..kk...",
    };

    // O saco de moedas que ela leva quando rouba (e solta quando apanha).
    static readonly string[] ArteSaco =
    {
        "....kkk....",
        "...kbybk...",
        "....kbk....",
        "..kkbbbkk..",
        ".kbbbybbbk.",
        "kbbbyyybbbk",
        "kbbyybbbbbk",
        "kbbbyyybbbk",
        "kbbbbbyybbk",
        ".kbbyyybbk.",
        "..kkkkkkk..",
    };

    static Color32 BolaDeTinta(int x, int y)
    {
        float dx = x - 5.5f, dy = y - 5.5f;
        float d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 6f) return FabricaDeSprites.Transparente;
        if (d > 5f) return Contorno;
        if (dx < -0.5f && dy > 0.5f && d < 3.5f && d > 1.8f) return TintaBrilho;
        return Tinta;
    }

    // Um borrão de tinta: uma mancha grande, gotas em volta e dois ou três escorridos para baixo. (forma: 0, 1 ou 2)
    static bool DentroDoBorrao(int x, int y, int forma)
    {
        float dx = x - 32f, dy = y - 34f;
        if (dx * dx + dy * dy < 17f * 17f) return true;
        for (int i = 0; i < 6; i++)
        {
            float angulo = (i * 61f + forma * 23f) * Mathf.Deg2Rad;
            float distancia = 17f + 4f * ((i + forma) % 3);
            float raio = 4f + ((i * 7 + forma * 3) % 4);
            float gx = 32f + Mathf.Cos(angulo) * distancia, gy = 34f + Mathf.Sin(angulo) * distancia;
            if ((x - gx) * (x - gx) + (y - gy) * (y - gy) < raio * raio) return true;
        }
        for (int i = 0; i < 2 + forma % 2; i++) // os escorridos
        {
            float ex = 22f + i * 9f + forma * 3f;
            float fundo = 4f + ((i + forma) % 3) * 4f;
            if (Mathf.Abs(x - ex) < 2.2f && y > fundo && y < 30f) return true;
            if ((x - ex) * (x - ex) + (y - fundo) * (y - fundo) < 9f) return true; // a gota na ponta
        }
        return false;
    }

    static Color32 Borrao(int x, int y, int forma)
    {
        if (!DentroDoBorrao(x, y, forma)) return FabricaDeSprites.Transparente;
        bool beirada = !DentroDoBorrao(x + 1, y, forma) || !DentroDoBorrao(x - 1, y, forma) || !DentroDoBorrao(x, y + 1, forma) || !DentroDoBorrao(x, y - 1, forma);
        if (beirada) return Contorno;
        float bx = x - 25f, by = y - 41f; // o brilho molhado, em cima à esquerda
        if (bx * bx + by * by < 10f || (x == 31 && y == 45)) return TintaBrilho;
        return Tinta;
    }

    static Color32 Mare(int x, int y)
    {
        if (y >= 14) return y == 15 ? TintaBrilho : Tinta;           // a superfície brilhando
        if ((x * 5 + y * 3) % 23 == 0) return TintaBrilho;           // bolhinhas
        return Tinta;
    }

    static Color32 Mancha(int x, int y)
    {
        float meio = 13.5f;
        float largura = 13f - y * 1.6f + 1.2f * Mathf.Sin(x * 1.3f);
        if (Mathf.Abs(x - meio) > largura) return FabricaDeSprites.Transparente;
        return y == 0 || Mathf.Abs(x - meio) > largura - 1f ? Contorno : (x + y) % 5 == 0 ? TintaBrilho : Tinta;
    }
}
