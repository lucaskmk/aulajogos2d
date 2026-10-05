using System;
using System.Collections.Generic;
using UnityEngine;

// Cria TODOS os sprites do jogo por código, sem precisar importar imagens.
// Os personagens são desenhados em "pixel art de texto": cada letra é uma cor da paleta
// e '.' é transparente. Quer mudar o visual? É só editar os desenhos abaixo!
// Blocos de chão, serra, bandeira, nuvens etc. são desenhados com fórmulas (procedural).
public static class FabricaDeSprites
{
    public const int PixelsPorUnidade = 16;

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimparCache() => cache.Clear();

    static readonly Dictionary<char, Color32> paleta = new Dictionary<char, Color32>
    {
        { 'k', new Color32(20, 20, 28, 255) },    // contorno preto
        { 'w', new Color32(255, 255, 255, 255) }, // branco
        { 'r', new Color32(220, 45, 45, 255) },   // vermelho
        { 'o', new Color32(236, 150, 40, 255) },  // laranja
        { 'y', new Color32(255, 222, 90, 255) },  // amarelo
        { 'b', new Color32(176, 104, 52, 255) },  // marrom (terra)
        { 'd', new Color32(118, 62, 32, 255) },   // marrom escuro
        { 'g', new Color32(92, 206, 72, 255) },   // verde
        { 'G', new Color32(40, 140, 52, 255) },   // verde escuro
        { 'n', new Color32(240, 196, 140, 255) }, // madeira clara
        { 's', new Color32(205, 205, 215, 255) }, // cinza claro
        { 'S', new Color32(115, 115, 130, 255) }, // cinza escuro
        { 'l', new Color32(205, 228, 255, 255) }, // azul claro (sombra da nuvem)
        { 'p', new Color32(255, 150, 170, 255) }, // rosa
    };

    static Color32 Cor(char c) => paleta[c];
    static readonly Color32 Transparente = new Color32(0, 0, 0, 0);

    // ---------------------------------------------------------------- desenhos

    static readonly string[] ArteJogador =
    {
        "................",
        "..kk........kk..",
        "..kwk......kwk..",
        "..kwwkkkkkkwwk..",
        ".kwwwwwwwwwwwwk.",
        ".kwwwwwwwwwwwwk.",
        ".kwwwkwwwwkwwwk.",
        ".kwwwkwwwwkwwwk.",
        ".kwpwwwkkwwwpwk.",
        ".kwwwwwwwwwwwwk.",
        "..kwwwwwwwwwwk..",
        "..kwwwwwwwwwwk..",
        "..kwwwwwwwwwwk..",
        "..kwwwwwwwwwwk..",
        "..kwwk....kwwk..",
        "..kkkk....kkkk..",
    };

    static readonly string[] ArteJogadorAndando =
    {
        "................",
        "..kk........kk..",
        "..kwk......kwk..",
        "..kwwkkkkkkwwk..",
        ".kwwwwwwwwwwwwk.",
        ".kwwwwwwwwwwwwk.",
        ".kwwwkwwwwkwwwk.",
        ".kwwwkwwwwkwwwk.",
        ".kwpwwwkkwwwpwk.",
        ".kwwwwwwwwwwwwk.",
        "..kwwwwwwwwwwk..",
        "..kwwwwwwwwwwk..",
        "..kwwwwwwwwwwk..",
        "..kwwwwwwwwwwk..",
        "...kwwk..kwwk...",
        "...kkkk..kkkk...",
    };

    static readonly string[] ArteInimigo =
    {
        "................",
        "......kkkk......",
        "....kkddddkk....",
        "...kddddddddk...",
        "..kddddddddddk..",
        "..kdkkddddkkdk..",
        ".kddwkkddkkwddk.",
        ".kddwwkddkwwddk.",
        ".kddddddddddddk.",
        ".kdddkkkkkkdddk.",
        "..kddddddddddk..",
        "...kkkkkkkkkk...",
        "....knnnnnnk....",
        "..kknnnkknnnkk..",
        "..knnnnkknnnnk..",
        "..kkkkk..kkkkk..",
    };

    static readonly string[] ArteInimigoEspinhos =
    {
        "..s...s..s...s..",
        ".ksk.kskksk.ksk.",
        "....kkddddkk....",
        "...kddddddddk...",
        "..kddddddddddk..",
        "..kdkkddddkkdk..",
        ".kddrkkddkkrddk.",
        ".kddrrkddkrrddk.",
        ".kddddddddddddk.",
        ".kdddkwkwkwdddk.",
        "..kddddddddddk..",
        "...kkkkkkkkkk...",
        "....knnnnnnk....",
        "..kknnnkknnnkk..",
        "..knnnnkknnnnk..",
        "..kkkkk..kkkkk..",
    };

    static readonly string[] ArteInimigoEsmagado =
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
        "................",
        "...kkkkkkkkkk...",
        "..kddkkddkkddk..",
        "..kddddddddddk..",
        "...kkkkkkkkkk...",
    };

    static readonly string[] ArteBlocoSurpresa =
    {
        "kkkkkkkkkkkkkkkk",
        "kooooooooooooook",
        "kokooooooooookok",
        "koooooyyyyoooook",
        "kooooyyooyyooook",
        "kooooooooyyooook",
        "koooooooyyoooook",
        "kooooooyyooooook",
        "kooooooyyooooook",
        "kooooooooooooook",
        "kooooooyyooooook",
        "kooooooyyooooook",
        "kooooooooooooook",
        "kokooooooooookok",
        "kooooooooooooook",
        "kkkkkkkkkkkkkkkk",
    };

    static readonly string[] ArteMoeda =
    {
        "................",
        "................",
        "......kkkk......",
        ".....kyyyyk.....",
        "....kyywwyyk....",
        "....kyywyyyk....",
        "....kyywyyyk....",
        "....kyywyyyk....",
        "....kyywyyyk....",
        "....kyywyyyk....",
        "....kyyyyyyk....",
        "....kyyyyyyk....",
        ".....kyyyyk.....",
        "......kkkk......",
        "................",
        "................",
    };

    static readonly string[] ArteMola =
    {
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        ".kkkkkkkkkkkkkk.",
        ".krrrrrrrrrrrrk.",
        ".kkkkkkkkkkkkkk.",
        ".....kssssk.....",
        "......kssk......",
        ".....kssssk.....",
        "......kssk......",
        ".....kssssk.....",
        ".kkkkkkkkkkkkkk.",
        ".kSSSSSSSSSSSSk.",
    };

    static readonly string[] ArteMolaApertada =
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
        ".kkkkkkkkkkkkkk.",
        ".krrrrrrrrrrrrk.",
        ".kkkkkkkkkkkkkk.",
        ".....kssssk.....",
        ".kkkkkkkkkkkkkk.",
        ".kSSSSSSSSSSSSk.",
    };

    static readonly string[] ArteEsmagador =
    {
        "kkkkkkkkkkkkkkkk",
        "kssssssssssssssk",
        "ksSssssssssssSsk",
        "kssssssssssssssk",
        "kskkksssssskkksk",
        "kssskwsssswksssk",
        "kssskksssskksssk",
        "kssssssssssssssk",
        "kssssssssssssssk",
        "kssskkkkkkkksssk",
        "ksskwkwkwkwksssk",
        "kssskkkkkkkksssk",
        "kssssssssssssssk",
        "ksSssssssssssSsk",
        "kssssssssssssssk",
        "kkkkkkkkkkkkkkkk",
    };

    static readonly string[] ArtePlaca =
    {
        "................",
        ".kkkkkkkkkkkkkk.",
        ".knnnnnnnnnnnnk.",
        ".knkkknkknkkknk.",
        ".knnnnnnnnnnnnk.",
        ".knkknkkkknkknk.",
        ".knnnnnnnnnnnnk.",
        ".kkkkkkkkkkkkkk.",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
    };

    // ---------------------------------------------------------------- API

    public static Sprite Pegar(string nome)
    {
        if (cache.TryGetValue(nome, out Sprite s) && s != null) return s;
        s = Criar(nome);
        s.name = nome;
        cache[nome] = s;
        return s;
    }

    static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
    static readonly Vector2 Base = new Vector2(0.5f, 0f);

    static Sprite Criar(string nome)
    {
        switch (nome)
        {
            case "jogador": return DeArte(ArteJogador, Centro);
            case "jogador_andando": return DeArte(ArteJogadorAndando, Centro);
            case "inimigo": return DeArte(ArteInimigo, Centro);
            case "inimigo_esmagado": return DeArte(ArteInimigoEsmagado, Centro);
            case "inimigo_espinhos": return DeArte(ArteInimigoEspinhos, Centro);
            case "bloco_surpresa": return DeArte(ArteBlocoSurpresa, Centro);
            case "moeda": return DeArte(ArteMoeda, Centro);
            case "mola": return DeArte(ArteMola, Centro);
            case "mola_apertada": return DeArte(ArteMolaApertada, Centro);
            case "esmagador": return DeArte(ArteEsmagador, Centro);
            case "placa": return DeArte(ArtePlaca, Centro);

            case "chao": return Procedural(16, 16, Centro, (x, y) => CorChao(x, y, false));
            case "chao_topo": return Procedural(16, 16, Centro, (x, y) => CorChao(x, y, true));
            case "tijolo": return Procedural(16, 16, Centro, CorTijolo);
            case "bloco_usado": return Procedural(16, 16, Centro, CorBlocoUsado);
            case "espinho": return Procedural(16, 16, Centro, CorEspinho);
            case "serra": return Procedural(16, 16, Centro, CorSerra);
            case "bandeira": return Procedural(16, 48, Base, (x, y) => CorBandeira(x, y, false));
            case "bandeira_falsa": return Procedural(16, 48, Base, (x, y) => CorBandeira(x, y, true));
            case "nuvem": return Procedural(32, 18, Centro, (x, y) => CorNuvem(x, y, false));
            case "nuvem_malvada": return Procedural(32, 18, Centro, (x, y) => CorNuvem(x, y, true));
            case "morro": return Procedural(48, 22, Base, CorMorro);
            case "arbusto": return Procedural(32, 11, Base, CorArbusto);
        }
        throw new ArgumentException("Sprite desconhecido: " + nome);
    }

    static Sprite DeArte(string[] linhas, Vector2 pivo)
    {
        int altura = linhas.Length;
        int largura = 0;
        foreach (string linha in linhas) largura = Mathf.Max(largura, linha.Length);

        return Procedural(largura, altura, pivo, (x, y) =>
        {
            string linha = linhas[altura - 1 - y]; // a primeira linha do texto é o topo da imagem
            if (x >= linha.Length) return Transparente;
            char c = linha[x];
            return paleta.TryGetValue(c, out Color32 cor) ? cor : Transparente;
        });
    }

    static Sprite Procedural(int largura, int altura, Vector2 pivo, Func<int, int, Color32> corDoPixel)
    {
        var textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point, // pixel art nítida, sem borrar
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                pixels[y * largura + x] = corDoPixel(x, y);
        textura.SetPixels32(pixels);
        textura.Apply();
        return Sprite.Create(textura, new Rect(0, 0, largura, altura), pivo, PixelsPorUnidade, 0, SpriteMeshType.FullRect);
    }

    // ---------------------------------------------------------------- desenhos por fórmula

    static int Ruido(int x, int y) => ((x * 73856093) ^ (y * 19349663) ^ 0x5bd1e995) & 0x7fffffff;

    static Color32 CorChao(int x, int y, bool topo)
    {
        if (topo)
        {
            if (y >= 13) return Ruido(x, y) % 5 == 0 ? Cor('G') : Cor('g');
            if (y == 12) return Ruido(x, 1) % 3 == 0 ? Cor('g') : Cor('G');
        }
        return Ruido(x, y) % 9 == 0 ? Cor('d') : Cor('b');
    }

    static Color32 CorTijolo(int x, int y)
    {
        var argamassa = new Color32(96, 44, 24, 255);
        var tijolo = new Color32(204, 96, 48, 255);
        var brilho = new Color32(236, 140, 90, 255);
        if (y % 4 == 0) return argamassa;
        int deslocamento = (y / 4) % 2 == 0 ? 0 : 4;
        if ((x + deslocamento) % 8 == 0) return argamassa;
        if (y % 4 == 3) return brilho;
        return tijolo;
    }

    static Color32 CorBlocoUsado(int x, int y)
    {
        if (x == 0 || y == 0 || x == 15 || y == 15) return Cor('k');
        bool rebite = (x == 2 || x == 13) && (y == 2 || y == 13);
        if (rebite) return Cor('k');
        return Cor('d');
    }

    static Color32 CorEspinho(int x, int y)
    {
        // dois espinhos triangulares lado a lado
        int indice = x / 8;
        float centro = indice * 8 + 3.5f;
        float distancia = Mathf.Abs(x - centro);
        float meiaLargura = 4f * (1f - y / 14f);
        if (y > 14 || distancia > meiaLargura) return Transparente;
        bool borda = meiaLargura - distancia < 1.1f || y == 0;
        if (borda) return Cor('S');
        return x < centro ? Cor('w') : Cor('s');
    }

    static Color32 CorSerra(int x, int y)
    {
        float dx = x - 7.5f, dy = y - 7.5f;
        float raio = Mathf.Sqrt(dx * dx + dy * dy);
        float angulo = Mathf.Atan2(dy, dx);
        bool dente = Mathf.Repeat(angulo * 8f / (2f * Mathf.PI), 1f) < 0.5f;
        float borda = dente ? 7.8f : 6.2f;
        if (raio > borda) return Transparente;
        if (raio < 1.6f) return Cor('k');
        if (raio < 3f) return Cor('S');
        if (raio > borda - 1.1f) return Cor('S');
        return Cor('s');
    }

    static Color32 CorBandeira(int x, int y, bool falsa)
    {
        // bolinha no topo
        float bx = x - 7.5f, by = y - 45f;
        if (bx * bx + by * by < 5.5f) return Cor('y');
        // mastro
        if (y < 43 && (x == 7 || x == 8)) return x == 7 ? Cor('s') : Cor('S');
        // base
        if (y < 3 && x >= 4 && x <= 11) return Cor('d');
        // pano (triângulo apontando para a direita)
        if (x >= 9 && y >= 29 && y <= 41)
        {
            float meio = 35f;
            float comprimento = 7f * (1f - Mathf.Abs(y - meio) / 6.5f);
            if (x - 9 < comprimento)
            {
                bool borda = x - 9 >= comprimento - 1f || y == 29 || y == 41;
                if (falsa) return borda ? Cor('k') : Cor('r');
                return borda ? Cor('G') : Cor('g');
            }
        }
        return Transparente;
    }

    static bool DentroDaNuvem(int x, int y)
    {
        bool Circulo(float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
        if (x < 0 || y < 0 || x >= 32 || y >= 18) return false;
        return Circulo(9f, 7f, 6.5f) || Circulo(16f, 10f, 7.5f) || Circulo(23f, 7f, 6.5f)
            || (x >= 4 && x <= 27 && y >= 1 && y <= 7);
    }

    static Color32 CorNuvem(int x, int y, bool malvada)
    {
        if (!DentroDaNuvem(x, y)) return Transparente;
        bool borda = !DentroDaNuvem(x + 1, y) || !DentroDaNuvem(x - 1, y) || !DentroDaNuvem(x, y + 1) || !DentroDaNuvem(x, y - 1);
        if (borda) return Cor('k');
        if (malvada)
        {
            // olhos bravos + boca cheia de dentes
            if ((x == 11 || x == 12 || x == 19 || x == 20) && (y == 9 || y == 10)) return Cor('k');
            if ((x == 10 && y == 12) || (x == 11 && y == 11) || (x == 21 && y == 12) || (x == 20 && y == 11)) return Cor('k');
            if (y >= 4 && y <= 6 && x >= 12 && x <= 19)
            {
                if (y == 5 && x % 2 == 0) return Cor('w');
                return y == 5 ? Cor('r') : Cor('k');
            }
        }
        return y < 5 ? Cor('l') : Cor('w');
    }

    static Color32 CorMorro(int x, int y)
    {
        float nx = (x - 23.5f) / 24f, ny = y / 22f;
        float d = nx * nx + ny * ny;
        if (d > 1f) return Transparente;
        if (d > 0.88f) return Cor('G');
        if (Ruido(x, y) % 23 == 0) return Cor('G');
        return new Color32(120, 214, 96, 255);
    }

    static Color32 CorArbusto(int x, int y)
    {
        bool Circulo(float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
        bool dentro = Circulo(8f, 0f, 8f) || Circulo(16f, 2f, 9f) || Circulo(24f, 0f, 8f);
        if (!dentro) return Transparente;
        bool topo = !(Circulo(8f, 0f, 7f) || Circulo(16f, 2f, 8f) || Circulo(24f, 0f, 7f));
        return topo ? Cor('G') : Cor('g');
    }
}
