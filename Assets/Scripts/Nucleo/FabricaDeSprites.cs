using System;
using System.Collections.Generic;
using UnityEngine;

// Cria TODOS os sprites do jogo por código, sem precisar importar imagens.
// Os personagens e os blocos são desenhados em "pixel art de texto": cada letra é uma cor
// da paleta e '.' é transparente. Quer mudar o visual? É só editar os desenhos abaixo!
// Serra, bandeira e nuvens são desenhados com fórmulas (procedural).
// Os blocos do cenário (chão, caixote, bloco "?", espinho, cano) e os enfeites do chão ficam em
// ArteDosBlocos: pixel art com rampas de cor, luz de cima-esquerda e várias versões de cada bloco.
// Os personagens são chibi (cabeçudos), como chaveirinhos de acrílico.
public static class FabricaDeSprites
{
    public const int PixelsPorUnidade = 16;

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void LimparCache()
    {
        cache.Clear();
        cacheDeTextos.Clear();
    }

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
        // --- personagens ---
        { 'h', new Color32(44, 40, 56, 255) },    // cabelo escuro
        { 'H', new Color32(96, 92, 120, 255) },   // brilho do cabelo
        { 'c', new Color32(252, 220, 186, 255) }, // pele
        { 'C', new Color32(222, 176, 146, 255) }, // pele (sombra)
        { 'j', new Color32(36, 36, 44, 255) },    // roupa preta
        { 'u', new Color32(88, 88, 100, 255) },   // calça
        { 'f', new Color32(34, 34, 58, 255) },    // pelo do caranguejo
        { 'F', new Color32(72, 72, 112, 255) },   // pelo do caranguejo (brilho)
        { 'x', new Color32(24, 8, 34, 255) },     // tinta do Kraken
        { 'v', new Color32(92, 40, 132, 255) },   // roxo (aura da sombra)
        { 'V', new Color32(176, 96, 230, 255) },  // roxo claro (unhas, bloco mágico)
        { 'P', new Color32(215, 170, 245, 255) }, // lilás (brilho do bloco mágico)
        { 'J', new Color32(74, 74, 90, 255) },    // roupa preta (brilho)
        { 'a', new Color32(176, 236, 104, 255) }, // verde claro (topo da grama)
        { 'q', new Color32(30, 110, 60, 255) },   // verde bem escuro (contorno da grama)
        { 'm', new Color32(190, 194, 208, 255) }, // pedra
        { 'M', new Color32(148, 152, 170, 255) }, // pedra (sombra)
        { 'Z', new Color32(226, 230, 240, 255) }, // pedra (brilho)
        { 'z', new Color32(96, 98, 120, 255) },   // rejunte da pedra
        { 'e', new Color32(70, 120, 230, 255) },  // azul (olhos da Dóris)
    };

    // Cor da paleta pelo caractere (as letras usadas nos desenhos em texto).
    public static Color32 Cor(char c) => paleta[c];
    // Igual, mas uma letra que não está na paleta vira transparente (em vez de dar erro).
    public static Color32 CorOuTransparente(char c) => paleta.TryGetValue(c, out Color32 cor) ? cor : Transparente;
    public static readonly Color32 Transparente = new Color32(0, 0, 0, 0);

    // Sprites "de fora": cada armadilha/personagem novo pode registrar os próprios desenhos no seu arquivo
    // (em um método [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]),
    // e eles passam a funcionar com Pegar("nome") em qualquer lugar (inclusive no mapa do mundo).
    // Um nome registrado aqui tem prioridade sobre os desenhos deste arquivo.
    static readonly Dictionary<string, Func<Sprite>> registrados = new Dictionary<string, Func<Sprite>>();

    public static void Registrar(string nome, Func<Sprite> criar)
    {
        registrados[nome] = criar;
        cache.Remove(nome); // se já tinha sido criado com o desenho antigo, recria
    }

    // ---------------------------------------------------------------- desenhos

    // Tentáculo do Kraken (com as ventosas), que sobe das bordas da tela na Volta da Maré.
    static readonly string[] ArteTentaculo =
    {
        "........vvvv....",
        ".......vxxxxv...",
        "......vxvvvxxv..",
        "......vxv...vxv.",
        ".......v....vxv.",
        "...........vxxv.",
        "..........vxxv..",
        ".........vxxxv..",
        "........vxxVxv..",
        ".......vxxxxv...",
        "......vxxVxxv...",
        ".....vxxxxxv....",
        ".....vxVxxxv....",
        "....vxxxxxxv....",
        "....vxxxVxxv....",
        "...vxxxxxxxv....",
        "...vxVxxxxxv....",
        "...vxxxxxxxxv...",
        "...vxxxxVxxxv...",
        "..vxxxxxxxxxv...",
        "..vxVxxxxxxxxv..",
        "..vxxxxxxxxxxv..",
        "..vxxxxxVxxxxv..",
        "..vxxxxxxxxxxv..",
    };

    // O "X" vermelho (X marca o lugar!) onde você morreu nas últimas tentativas.
    static readonly string[] ArteMarcaX =
    {
        "kk........kk",
        "krk......krk",
        ".krk....krk.",
        "..krk..krk..",
        "...krkkrk...",
        "....krrk....",
        "....krrk....",
        "...krkkrk...",
        "..krk..krk..",
        ".krk....krk.",
        "krk......krk",
        "kk........kk",
    };

    static readonly string[] ArteMola =
    {
        "................",
        "................",
        "................",
        "................",
        "................",
        "................",
        "..kkkkkkkkkkkk..",
        ".krrrrrrrrrrrrk.",
        "..kkkkkkkkkkkk..",
        ".....kssssk.....",
        "......kssk......",
        ".....kssssk.....",
        "......kssk......",
        ".....kssssk.....",
        "..kkkkkkkkkkkk..",
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
        "..kkkkkkkkkkkk..",
        ".krrrrrrrrrrrrk.",
        "..kkkkkkkkkkkk..",
        ".....kssssk.....",
        "..kkkkkkkkkkkk..",
        ".kSSSSSSSSSSSSk.",
    };

    static readonly string[] ArteEsmagador =
    {
        "..kkkkkkkkkkkk..",
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
        "..kkkkkkkkkkkk..",
    };

    static readonly string[] ArtePlaca =
    {
        "................",
        "..kkkkkkkkkkkk..",
        ".knnnnnnnnnnnnk.",
        ".knkkknkknkkknk.",
        ".knnnnnnnnnnnnk.",
        ".knkknkkkknkknk.",
        ".knnnnnnnnnnnnk.",
        "..kkkkkkkkkkkk..",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
        "......kddk......",
    };

    // Caveirinha do mapa (mostra quantas vezes você morreu em cada fase).
    static readonly string[] ArteCaveira =
    {
        ".kkkkk.",
        "kwwwwwk",
        "kwkwkwk",
        "kwwwwwk",
        ".kwkwk.",
        "..kkk..",
    };

    static readonly string[] ArteCoracao =
    {
        ".kk.kk.",
        "krpkrrk",
        "krrrrrk",
        ".krrrk.",
        "..krk..",
        "...k...",
    };

    // Casinha do porto (mapa do mundo).
    static readonly string[] ArteCasa =
    {
        "................",
        ".......kk.......",
        "......krrk......",
        ".....krrrrk.....",
        "....krrrrrrk....",
        "...krrrrrrrrk...",
        "..krrrrrrrrrrk..",
        "..kkkkkkkkkkkk..",
        "..knnnnnnnnnnk..",
        "..knkkknnkkknk..",
        "..knklknnklknk..",
        "..knkkkddkkknk..",
        "..knnnkddknnnk..",
        "..knnnkddknnnk..",
        "..kkkkkkkkkkkk..",
        "................",
    };

    static readonly string[] ArtePoeira =
    {
        ".ww.",
        "wwww",
        "wwww",
        ".ww.",
    };

    // Enfeites (sem colisão).
    static readonly string[] ArteTufo =
    {
        "................",
        "...q.......q....",
        "..qaq..q..qaq...",
        "..qgq.qaq.qgq.q.",
        ".qqgqqqgqqqgqqaq",
        ".qgggggggggggggq",
    };

    static readonly string[] ArteFlor =
    {
        "................",
        "..kk.......kk...",
        ".kppk.....kwwk..",
        ".kpypk...kwywk..",
        "..kpk.....kwk...",
        "...q.......q....",
        "..qgq.....qgq...",
    };

    static readonly string[] ArteBrilho =
    {
        "...w...",
        "...w...",
        "..www..",
        "wwwywww",
        "..www..",
        "...w...",
        "...w...",
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

    public static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
    public static readonly Vector2 Base = new Vector2(0.5f, 0f);

    static Sprite Criar(string nome)
    {
        if (registrados.TryGetValue(nome, out Func<Sprite> criarRegistrado)) return criarRegistrado();
        switch (nome)
        {
            case "bloco_surpresa": return ArteDosBlocos.BlocoSurpresa(float.NaN);
            case "mola": return DeArte(ArteMola, Centro);
            case "mola_apertada": return DeArte(ArteMolaApertada, Centro);
            case "esmagador": return DeArte(ArteEsmagador, Centro);
            case "placa": return DeArte(ArtePlaca, Centro);
            case "tentaculo": return DeArte(ArteTentaculo, Centro);
            case "marca_x": return DeArte(ArteMarcaX, Centro);
            case "coracao": return DeArte(ArteCoracao, Centro);
            case "caveira": return DeArte(ArteCaveira, Centro);
            case "porta": return Procedural(16, 32, Base, (x, y) => CorPorta(x, y, false));
            case "porta_aberta": return Procedural(16, 32, Base, (x, y) => CorPorta(x, y, true));
            case "ponto_save": return Procedural(16, 24, Base, (x, y) => CorPontoDeSave(x, y, false));
            case "ponto_save_ativo": return Procedural(16, 24, Base, (x, y) => CorPontoDeSave(x, y, true));
            case "aviso": return Procedural(16, 16, Centro, CorAviso);

            // Mapa do mundo
            case "no_mapa": return Procedural(16, 16, Centro, CorNoDoMapa);
            case "arvore": return Procedural(16, 24, Base, CorArvore);
            case "ponte": return Procedural(16, 16, Centro, CorPonte);
            case "montanha": return Procedural(16, 16, Centro, CorMontanha);
            case "casa": return DeArte(ArteCasa, Centro);
            case "terra": return Procedural(16, 16, Centro, CorTerra);
            case "nevoa": return Procedural(16, 16, Centro, CorNevoa);
            case "ceu": return Procedural(1, 32, Base, CorCeu);
            case "sombra_nuvem": return Procedural(48, 20, Centro, CorSombraDaNuvem);

            // O chão é desenhado em ArteDosBlocos (com grama, terra, pedra e várias versões; veja lá os nomes
            // "chao_topo_2_eb"...). Estes nomes curtos continuam valendo para quem só quer "um bloco de chão".
            case "chao": return Pegar("chao_pedra_0");
            case "chao_topo": return Pegar("chao_topo_0");
            case "chao_rachado": return Pegar("chao_pedra_1");
            case "chao_topo_florido": return Pegar("chao_topo_3");

            // enfeites do cenário (sem colisão)
            case "arbusto": return ArteDosBlocos.Arbusto();
            case "pedra": return ArteDosBlocos.PedraDeEnfeite();
            case "barril": return Procedural(12, 14, Base, CorBarril);
            case "lampiao": return Procedural(10, 40, Base, CorLampiao);
            case "estrela_do_mar": return Procedural(10, 9, Base, CorEstrela);
            case "sombra_oval": return Procedural(16, 6, Centro, CorSombraOval);

            // céu e fundo
            case "sol": return Procedural(48, 48, Centro, CorSol);
            case "lua": return Procedural(40, 40, Centro, CorLua);
            case "raio_de_luz": return Procedural(32, 128, new Vector2(0.5f, 1f), CorRaioDeLuz);
            case "fundo_ilhas": return Procedural(256, 96, Base, CorIlhas);
            case "neblina": return Procedural(1, 32, Base, CorNeblina);
            case "frente_folhas": return Procedural(128, 24, Base, CorFrente);
            case "tijolo": return ArteDosBlocos.Caixote();
            case "bloco_usado": return ArteDosBlocos.BlocoUsado();
            case "tufo": return ArteDosBlocos.DeArteRica(ArteTufo, Base);
            case "flor": return ArteDosBlocos.DeArteRica(ArteFlor, Base);
            case "brilho": return DeArte(ArteBrilho, Centro);
            case "poeira": return DeArte(ArtePoeira, Centro);

            case "espinho": return ArteDosBlocos.Espinho();
            case "serra": return Procedural(16, 16, Centro, CorSerra);
            case "bandeira": return Procedural(16, 48, Base, (x, y) => CorBandeira(x, y, false, 0));
            case "bandeira_falsa": return Procedural(16, 48, Base, (x, y) => CorBandeira(x, y, true, 0));
            case "nuvem": return Procedural(32, 18, Centro, (x, y) => CorNuvem(x, y, false));
            case "nuvem_malvada": return Procedural(32, 18, Centro, (x, y) => CorNuvem(x, y, true));
            case "cano_topo": return ArteDosBlocos.CanoDesenho(true);
            case "cano_corpo": return ArteDosBlocos.CanoDesenho(false);

            // Camadas do fundo (paralaxe). São brancas/cinza: o jogo pinta com a cor de cada fase.
            case "fundo_galeao": return Procedural(384, 176, Base, CorGaleao);
            case "fundo_palmeiras": return Procedural(128, 112, Base, CorPalmeiras);
            case "fundo_mato": return Procedural(128, 80, Base, CorMato);
        }

        // Blocos do chão com versão e bordas ("chao_topo_2_eb"): veja ArteDosBlocos.
        if (ArteDosBlocos.TentarCriar(nome, out Sprite bloco)) return bloco;

        // Quadros de animação: "bandeira_2" (pano tremulando), "bloco_surpresa_brilho_3" (brilho passando).
        int sublinhado = nome.LastIndexOf('_');
        if (sublinhado > 0 && int.TryParse(nome.Substring(sublinhado + 1), out int quadro))
        {
            switch (nome.Substring(0, sublinhado))
            {
                case "bandeira": return Procedural(16, 48, Base, (x, y) => CorBandeira(x, y, false, quadro));
                case "agua": return Procedural(16, 16, Centro, (x, y) => CorAgua(x, y, quadro));
                case "bloco_surpresa_brilho": return ArteDosBlocos.BlocoSurpresa(quadro * 5f);
            }
        }
        throw new ArgumentException("Sprite desconhecido: " + nome);
    }

    public const int QuadrosDaBandeira = 4;
    public const int QuadrosDoBrilho = 7;
    public const int QuadrosDaAgua = 4;

    // Nomes dos quadros, para passar para a AnimacaoDeQuadros.
    public static Sprite[] Quadros(string nome, int quantidade)
    {
        var quadros = new Sprite[quantidade];
        for (int i = 0; i < quantidade; i++) quadros[i] = Pegar(nome + "_" + i);
        return quadros;
    }

    // Sombreamento automático de um desenho em texto (a luz vem de cima-esquerda), para os desenhos GRANDES
    // (as chefes): cada pixel de um material que encosta no vazio ou no contorno EMBAIXO ou à DIREITA vira o
    // tom de sombra dele; o que encosta EM CIMA ou à ESQUERDA vira o tom de luz. "tons": material -> (luz, sombra).
    // Materiais fora da lista (olhos, detalhes pequenos) ficam como estão.
    public static string[] Sombrear(string[] linhas, Dictionary<char, (char luz, char sombra)> tons, char contorno = 'k')
    {
        int altura = linhas.Length;
        bool Vazio(int x, int y)
        {
            if (y < 0 || y >= altura || x < 0 || x >= linhas[y].Length) return true;
            char c = linhas[y][x];
            return c == '.' || c == contorno;
        }
        var resultado = new string[altura];
        for (int y = 0; y < altura; y++)
        {
            char[] linha = linhas[y].ToCharArray();
            for (int x = 0; x < linha.Length; x++)
            {
                if (!tons.TryGetValue(linhas[y][x], out var tom)) continue;
                if (Vazio(x + 1, y) || Vazio(x, y + 1)) linha[x] = tom.sombra;
                else if (Vazio(x - 1, y) || Vazio(x, y - 1)) linha[x] = tom.luz;
            }
            resultado[y] = new string(linha);
        }
        return resultado;
    }

    // brilho: posição de uma faixa diagonal clara passando pelo desenho (NaN = sem brilho).
    public static Sprite DeArte(string[] linhas, Vector2 pivo, int pixelsPorUnidade = PixelsPorUnidade, float brilho = float.NaN)
    {
        int altura = linhas.Length;
        int largura = 0;
        foreach (string linha in linhas) largura = Mathf.Max(largura, linha.Length);

        return Procedural(largura, altura, pivo, (x, y) =>
        {
            string linha = linhas[altura - 1 - y]; // a primeira linha do texto é o topo da imagem
            if (x >= linha.Length) return Transparente;
            char c = linha[x];
            if (!paleta.TryGetValue(c, out Color32 cor)) return Transparente;
            if (c != 'k' && Mathf.Abs(x + y - brilho) <= 1.5f) // NaN nunca entra aqui
                cor = Color32.Lerp(cor, Cor('w'), 0.65f);
            return cor;
        }, pixelsPorUnidade);
    }

    public static Sprite Procedural(int largura, int altura, Vector2 pivo, Func<int, int, Color32> corDoPixel, int pixelsPorUnidade = PixelsPorUnidade)
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
        return Sprite.Create(textura, new Rect(0, 0, largura, altura), pivo, pixelsPorUnidade, 0, SpriteMeshType.FullRect);
    }

    // ---------------------------------------------------------------- desenhos por fórmula

    static int Ruido(int x, int y) => ((x * 73856093) ^ (y * 19349663) ^ 0x5bd1e995) & 0x7fffffff;

    // Fundo: "Cheio" é a silhueta, "Detalhe" um pouco mais escuro (janelas, sombras).
    static readonly Color32 Cheio = new Color32(255, 255, 255, 255);
    static readonly Color32 Detalhe = new Color32(222, 222, 228, 255);
    const int Solo = 32; // faixa cheia embaixo de cada camada, para não aparecer céu nos buracos

    // ---------------------------------------------------------------- as camadas do fundo das fases
    // Cada camada é desenhada em TONS DE CINZA (branco = a parte que pega sol, cinza = sombra): quem dá a cor
    // é a Paralaxe, que pinta a camada com a cor da fase misturada com o "ar" (as de longe ficam mais claras
    // e azuladas). Assim a mesma arte serve de dia, de noite e no entardecer, e o volume aparece em qualquer cor.
    // Contraste baixo de propósito: o fundo não pode brigar com o que mata (e nada aqui é pontudo).

    // Os tons (do sol para a sombra).
    static Color32 Tom(int t)
    {
        byte v = t <= 0 ? (byte)255 : t == 1 ? (byte)230 : t == 2 ? (byte)204 : t == 3 ? (byte)178 : (byte)152;
        return new Color32(v, v, v, 255);
    }

    // 0 ou 1 conforme a "matriz de Bayer" (para misturar dois tons em xadrez: dithering).
    static bool Xadrez(int x, int y, float quanto) => ((x * 3 + y * 5) % 8) / 8f < quanto;

    // A distância de x até o pé "pe", dando a volta no fim da imagem (para repetir sem emenda).
    static float DandoAVolta(int x, float pe, int largura) => Mathf.Repeat(x - pe + largura / 2f, largura) - largura / 2f;

    // Ilhas lá longe (camada mais distante): morros com mato em cima, o vulcão com a cratera e um fiozinho de
    // fumaça, e o mar embaixo com os brilhos do sol. Repete a cada 256 px.
    static float AlturaDasIlhas(int x)
    {
        float t = 2f * Mathf.PI * x / 256f;
        float ilhas = 12f + 15f * Mathf.Sin(t) + 9f * Mathf.Sin(3f * t + 1f) + 4f * Mathf.Sin(7f * t + 2f);
        float vx = Mathf.Abs(DandoAVolta(x, 170f, 256));
        float vulcao = Mathf.Min(66f - vx * 0.9f, 56f) - (vx < 4f ? 2f : 0f); // a cratera: um dentinho no topo
        return Mathf.Max(ilhas, vulcao);
    }

    static Color32 CorIlhas(int x, int y)
    {
        if (y < 6) // o mar: faixas com os brilhos do sol
            return (y == 2 || y == 4) && (x * 7 + y * 13) % 11 < 3 ? Tom(0) : Tom(1);
        float altura = AlturaDasIlhas(x);
        int vx = Mathf.RoundToInt(DandoAVolta(x, 170f, 256));
        // a fumaça do vulcão: bolinhas subindo e indo para a direita
        for (int k = 0; k < 4; k++)
        {
            float cx = 170f + 3f + k * 4f, cy = 60f + k * 7f, r = 2.5f + k * 0.8f;
            float dx = DandoAVolta(x, cx, 256), dy = y - cy;
            if (dx * dx + dy * dy <= r * r) return dx + dy < -r * 0.3f ? Tom(0) : Tom(1);
        }
        if (y >= altura)
        {
            // palmeirinhas no alto dos morros (tronco de 1 pixel e uma copa de 5)
            for (int p = 0; p < 5; p++)
            {
                float px = 18f + p * 51f;
                float topo = AlturaDasIlhas(Mathf.RoundToInt(px));
                if (topo > 50f || topo < 10f) continue;
                int dx = Mathf.RoundToInt(DandoAVolta(x, px, 256));
                int a = y - Mathf.RoundToInt(topo);
                if (dx == 0 && a >= 0 && a < 6) return Tom(2);
                if (a == 6 && Mathf.Abs(dx) <= 2) return Tom(1);
                if (a == 5 && (dx == -3 || dx == 3)) return Tom(2);
            }
            return Transparente;
        }
        // a encosta virada para o sol (esquerda) é clara; a outra fica na sombra
        float inclinacao = AlturaDasIlhas(x + 2) - AlturaDasIlhas(x - 2);
        int tom = inclinacao > 1.5f ? 1 : inclinacao < -1.5f ? 3 : 2;
        if (altura - y < 2f) tom = Mathf.Max(0, tom - 1);                     // a beirada de cima pega luz
        bool vulcao = Mathf.Abs(vx) < 40 && altura > 30f;
        if (vulcao && (y + Mathf.RoundToInt(Mathf.Sin(x * 0.3f) * 1.5f)) % 7 == 0) tom = Mathf.Min(4, tom + 1); // as camadas de rocha
        if (!vulcao && altura - y < 6f && Xadrez(x, y, 0.5f)) tom = Mathf.Max(0, tom - 1); // o mato no topo dos morros
        if (y < 9 && Xadrez(x, y, (9 - y) / 9f)) tom = Mathf.Min(4, tom + 1);              // o pé, mais escuro perto do mar
        return Tom(tom);
    }

    // O galeão no horizonte: casco com as tábuas, a faixa dourada, as portinholas dos canhões e as janelas da popa,
    // três mastros com as velas estufadas (claras do lado do sol), o cesto da gávea, os cabos e a bandeira.
    // Embaixo, o mar com a espuma das ondas, e uma pedra com uma palmeira de cada lado. Repete a cada 384 px.
    const float EscalaDoGaleao = 0.6f;
    static readonly Vector3Int[] MastrosDoGaleao = { new Vector3Int(-42, 108, 17), new Vector3Int(6, 128, 21), new Vector3Int(50, 100, 15) }; // (dx, topo, meia vela)

    static Color32 CorGaleao(int x, int y)
    {
        if (y < Solo - 6) return Tom(2);
        int a = y - Solo, dx = x - 192;
        float crista = 3f + 1.5f * Mathf.Sin(x * 0.2f) + Mathf.Sin(x * 0.07f);
        if (a < crista) // o mar: o corpo da onda e a espuma em cima
            return a > crista - 1.5f ? Tom(0) : (a + x / 9) % 4 == 0 ? Tom(1) : Tom(2);

        // a pedrinha com a palmeira, de cada lado do navio
        foreach (int pedra in new[] { -150, 150 })
        {
            float pd = dx - pedra;
            float topo = 14f - pd * pd / 40f;
            if (a < topo) return pd < -2f ? Tom(1) : pd > 4f ? Tom(3) : Tom(2);
            int t = TomDaPalmeira(pd - 2f, a - 12, 30f, pedra < 0 ? 5f : -5f, 11f);
            if (t >= 0) return Tom(t);
        }

        // o navio é desenhado em tamanho "de perto" e encolhido aqui (fica lá longe, no horizonte)
        a = Mathf.RoundToInt(a / EscalaDoGaleao);
        dx = Mathf.RoundToInt(dx / EscalaDoGaleao);

        // o casco: mais largo em cima, a popa (esquerda) alta e a proa (direita) subindo
        bool casco = a < 26 && dx >= -60 - a * 0.7f && dx <= 60 + a * 0.8f;
        bool popa = a < 44 && dx >= -80 && dx <= -50;
        bool proa = a < 32 && dx >= 60 && dx <= 82 - (a > 26 ? (a - 26) * 2 : 0);
        bool gurupes = dx >= 80 && dx <= 122 && Mathf.Abs(a - (28 + (dx - 80) * 0.4f)) <= 1.2f;
        if (casco || popa || proa || gurupes)
        {
            if (gurupes) return Tom(2);
            bool portinhola = a >= 10 && a < 15 && Mathf.Abs(dx) < 58 && Mathf.Repeat(dx + 60, 12) < 5;
            if (portinhola) return a == 14 ? Tom(2) : Tom(4);
            bool janela = popa && a >= 32 && a < 38 && Mathf.Repeat(dx + 80, 8) < 4;
            if (janela) return Tom(0);
            if (a == 19 || a == 20) return Tom(0);                                   // a faixa dourada
            if (a == 25 || (popa && a == 43) || (proa && a == 31)) return Tom(1);    // a amurada pega luz
            int tom = (a / 3) % 2 == 0 ? 2 : 3;                                      // as tábuas
            if (Mathf.Repeat(dx + a * 7, 23) < 1f) tom = 4;                           // as emendas
            if (a < 6) tom = Mathf.Min(4, tom + 1);                                  // perto da água: mais escuro
            return Tom(tom);
        }

        foreach (Vector3Int m in MastrosDoGaleao)
        {
            int ddx = dx - m.x;
            if (Mathf.Abs(ddx) <= 1 && a >= 24 && a < m.y) return ddx < 0 ? Tom(2) : Tom(3); // o mastro
            if (Mathf.Abs(ddx) <= 4 && a >= m.y - 14 && a < m.y - 11) return a == m.y - 12 ? Tom(1) : Tom(3); // o cesto
            float altura = (m.y - 36) / 3f;
            for (int k = 0; k < 3; k++)
            {
                float baseDaVela = 34 + k * altura, topoDaVela = baseDaVela + altura - 7f;
                float s = Mathf.Clamp01((a - baseDaVela) / (topoDaVela - baseDaVela));
                float meia = m.z * (1f - k * 0.22f) * (0.82f + 0.18f * Mathf.Sin(s * Mathf.PI)); // estufada no meio
                if (Mathf.Abs(ddx) <= meia)
                {
                    float u = ddx / meia;                                       // -1 (lado do sol) a 1
                    float curva = 3f * (1f - u * u);
                    if (a >= baseDaVela - curva && a < topoDaVela)
                    {
                        int tom = u < -0.35f ? 0 : u < 0.45f ? 1 : 2;
                        if (a < baseDaVela - curva + 2f) tom = Mathf.Min(4, tom + 1); // a barra de baixo, na sombra
                        if (Mathf.Abs(ddx) > meia - 1.2f) tom = 3;                    // a beirada da vela
                        if (Mathf.Abs(a - (baseDaVela + topoDaVela) / 2f) < 0.6f) tom = Mathf.Min(4, tom + 1); // a costura
                        return Tom(tom);
                    }
                }
                if (Mathf.Abs(a - topoDaVela - 1f) <= 0.6f && Mathf.Abs(ddx) <= m.z * (1f - k * 0.22f) + 3f) return Tom(3); // a verga
            }
        }
        // a bandeira tremulando no mastro grande, com a caveira
        int bx = dx - MastrosDoGaleao[1].x, ba = a - (MastrosDoGaleao[1].y - 9);
        float onda = Mathf.Sin(bx * 0.6f) * 1.5f;
        if (bx >= 1 && bx <= 16 && ba > onda && ba < 8 + onda)
            return bx >= 7 && bx <= 9 && ba > onda + 3 && ba < onda + 6 ? Tom(0) : Tom(4);
        // os cabos, do alto do mastro grande até a popa e até a ponta do gurupés
        if (NoCabo(dx, a, MastrosDoGaleao[1].x, MastrosDoGaleao[1].y - 2, -78, 44) || NoCabo(dx, a, MastrosDoGaleao[1].x, MastrosDoGaleao[1].y - 2, 120, 44))
            return Tom(3);
        return Transparente;
    }

    // Está em cima do cabo esticado de (x0, a0) até (x1, a1)? (uma linha fininha)
    static bool NoCabo(int x, int a, float x0, float a0, float x1, float a1)
    {
        if (x < Mathf.Min(x0, x1) || x > Mathf.Max(x0, x1)) return false;
        float esperado = a0 + (a1 - a0) * (x - x0) / (x1 - x0);
        return Mathf.Abs(a - esperado) <= 0.5f;
    }

    // Palmeira dos fundos: tronco curvado com anéis e as folhas caindo em arco a partir da copa.
    // dx: distância do pixel até o pé da palmeira; a: altura do pixel acima do chão.
    // Devolve o tom (0 = sol ... 4 = sombra), ou -1 se o pixel não é da palmeira.
    static int TomDaPalmeira(float dx, int a, float altura, float inclinacao, float tamanho)
    {
        if (a < 0 || a > altura + tamanho) return -1;
        float u = Mathf.Clamp01(a / altura);
        float meioDoTronco = inclinacao * u * u;
        float meia = 2.4f - u;
        if (a <= altura && Mathf.Abs(dx - meioDoTronco) <= meia)
        {
            int tom = dx - meioDoTronco < -meia * 0.3f ? 1 : 2;  // o lado do sol do tronco
            if (a % 4 == 0) tom = 3;                              // os anéis
            return a > altura - 3f ? 3 : tom;                     // embaixo da copa: sombra
        }
        for (int f = 0; f < 7; f++)
        {
            float angulo = Mathf.PI * f / 6f, ca = Mathf.Cos(angulo), sa = Mathf.Sin(angulo);
            float comprimento = tamanho * (0.65f + 0.35f * Mathf.Abs(ca)); // as dos lados são mais compridas
            for (int k = 1; k <= 10; k++)
            {
                float s = k / 10f;
                float fx = inclinacao + ca * comprimento * s, fy = altura + sa * comprimento * s * 0.6f - comprimento * 0.55f * s * s;
                float largura = 2.4f * (1f - s) + 0.6f, ddx = dx - fx, ddy = a - fy;
                if (ddx * ddx + ddy * ddy > largura * largura) continue;
                if (ddy > 0.3f) return ca < 0.2f ? 0 : 1;   // a parte de cima da folha pega sol
                return ddy < -0.8f ? 3 : 2;                 // a de baixo fica na sombra
            }
        }
        // os cocos, embaixo da copa
        float cdx = dx - inclinacao, cdy = a - (altura - 1.5f);
        if (cdx * cdx + cdy * cdy <= 2.2f) return 4;
        return -1;
    }

    // Folhas de moita: um "cacho" de bolinhas com a luz em cima-esquerda e sombras entre as folhas.
    static int TomDaFolhagem(int x, int y, float cx, float cy, float raio)
    {
        float nx = (x + 0.5f - cx) / raio, ny = (y + 0.5f - cy) / raio;
        float d = nx * nx + ny * ny;
        if (d > 1f) return -1;
        float luz = 0.55f - nx * 0.35f + ny * 0.45f;
        int folha = Ruido(x / 3, y / 2) % 5;                   // tufos de folhas, cada um num tom
        int tom = luz > 0.75f ? 0 : luz > 0.45f ? 1 : luz > 0.2f ? 2 : 3;
        if (folha == 0) tom = Mathf.Min(4, tom + 1);
        if (d > 0.82f && ny < 0f) tom = Mathf.Min(4, tom + 1); // a beirada de baixo, mais escura
        return tom;
    }

    // Palmeiras (camada do meio), com o mato do pé. Repete sem emenda a cada 128 px.
    static readonly Vector4[] PalmeirasDoFundo = // (pé, altura, inclinação, tamanho das folhas)
    {
        new Vector4(10, 46, 6, 16), new Vector4(40, 62, -8, 18), new Vector4(66, 40, 5, 14),
        new Vector4(93, 66, 7, 19), new Vector4(117, 50, -5, 15),
    };

    static Color32 CorPalmeiras(int x, int y)
    {
        if (y < Solo + 2) return Tom(3);
        int a = y - Solo;
        float mato = 8f + 3f * Mathf.Sin(2f * Mathf.PI * x / 64f) + 2f * Mathf.Sin(2f * Mathf.PI * x / 32f + 1f);
        if (a < mato) // o mato do pé: tufos com a luz em cima
        {
            int tom = a > mato - 2f ? 1 : 2;
            if (Ruido(x / 2, a / 2) % 4 == 0) tom++;
            return Tom(tom);
        }
        foreach (Vector4 p in PalmeirasDoFundo)
        {
            int t = TomDaPalmeira(DandoAVolta(x, p.x, 128), a, p.y, p.z, p.w);
            if (t >= 0) return Tom(t);
        }
        return Transparente;
    }

    // O mato da praia (camada mais perto): moitas redondas cheias de folhas e duas palmeiras baixas. Repete a cada 128 px.
    static readonly Vector3Int[] Moitas = // (centro x, centro y, raio)
    {
        new Vector3Int(10, 14, 14), new Vector3Int(34, 18, 15), new Vector3Int(58, 10, 12),
        new Vector3Int(80, 16, 15), new Vector3Int(104, 12, 13), new Vector3Int(124, 18, 14),
    };

    static Color32 CorMato(int x, int y)
    {
        if (y < Solo + 8) return Tom(Ruido(x / 2, y / 2) % 5 == 0 ? 4 : 3);
        int a = y - Solo;
        int melhor = -1;
        foreach (Vector3Int c in Moitas)
        {
            int t = TomDaFolhagem(Mathf.RoundToInt(DandoAVolta(x, c.x, 128)) + 64, a, 64f, c.y, c.z);
            if (t >= 0) { melhor = t; break; }
        }
        if (melhor >= 0) return Tom(melhor);
        int pa = TomDaPalmeira(DandoAVolta(x, 22, 128), a, 34, 5, 12);
        if (pa < 0) pa = TomDaPalmeira(DandoAVolta(x, 88, 128), a, 40, -6, 13);
        return pa >= 0 ? Tom(pa) : Transparente;
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

    // quadro: 0 a 3, o pano "ondula" (quanto mais longe do mastro, mais ele sobe e desce).
    static Color32 CorBandeira(int x, int y, bool falsa, int quadro)
    {
        if (x >= 9)
        {
            float onda = Mathf.Sin(quadro * Mathf.PI / 2f - (x - 9) * 0.9f) * (x - 9) / 6f * 1.4f;
            y -= Mathf.RoundToInt(onda);
        }
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
        // contorno azul-acinzentado (não preto) e a beirada de cima-esquerda "acesa" pelo sol
        if (borda) return !DentroDaNuvem(x - 1, y + 1) && y >= 7 && x < 20 ? new Color32(150, 150, 190, 255) : new Color32(58, 60, 96, 255);
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
        // volume: embaixo sombra lilás, no meio um branco levemente azulado, em cima-esquerda branco quente
        if (y < 4) return new Color32(170, 176, 218, 255);
        if (y < 6) return (x + y) % 2 == 0 ? new Color32(170, 176, 218, 255) : new Color32(214, 220, 240, 255);
        if (!DentroDaNuvem(x - 2, y + 2)) return new Color32(255, 250, 236, 255);
        return y < 8 || x > 21 ? new Color32(214, 220, 240, 255) : new Color32(246, 246, 248, 255);
    }

    // Ponto (fase) do mapa: um "botão" branco com contorno. O mapa pinta com a cor do estado
    // (verde = concluída, amarelo = liberada, cinza = trancada).
    static Color32 CorNoDoMapa(int x, int y)
    {
        float dx = x - 7.5f, dy = y - 7.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 7.3f) return Transparente;
        if (d > 6.3f) return Cor('k');
        if (d > 5.1f && dy < 0f) return Cor('s'); // sombrinha embaixo: parece um botão
        if (dx < -1f && dy > 1f && d < 4.5f && d > 3f) return Cor('w');
        return Cor('Z');
    }

    // ---------------------------------------------------------------- enfeites

    static bool Bolinha(int x, int y, float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

    // Barril de rum (enfeite do chão): mais gordo no meio, tábuas claras à esquerda e os aros de ferro.
    static Color32 CorBarril(int x, int y)
    {
        float meia = 4.2f + 0.8f * Mathf.Sin((y + 0.5f) / 14f * Mathf.PI);
        float dx = Mathf.Abs(x + 0.5f - 6f);
        if (dx > meia) return Transparente;
        if (dx > meia - 1f || y == 0 || y == 13) return Cor('k');
        if (y == 12) return Cor('d');          // a tampa
        if (y == 3 || y == 10) return Cor('S'); // os aros
        if (y == 4 || y == 11) return Cor('s');
        if ((x - 2) % 3 == 0) return Cor('d'); // as frestas entre as tábuas
        return x < 6 ? Cor('n') : Cor('b');
    }

    static Color32 CorLampiao(int x, int y)
    {
        if (y <= 1) return x >= 2 && x <= 7 ? Cor('k') : Transparente;  // base
        if (y < 30) return x == 4 ? Cor('S') : x == 5 ? Cor('k') : Transparente; // poste
        if (y >= 30 && y <= 38 && x >= 1 && x <= 8)                     // lanterna
        {
            if (x == 1 || x == 8 || y == 30 || y == 38) return Cor('k');
            return (x >= 3 && x <= 6 && y >= 32 && y <= 36) ? Cor('w') : Cor('y');
        }
        if (y == 39 && x >= 3 && x <= 6) return Cor('k');
        return Transparente;
    }

    // Estrela-do-mar (enfeite do chão): cinco braços laranja com pintinhas amarelas.
    static Color32 CorEstrela(int x, int y)
    {
        float dx = x + 0.5f - 5f, dy = y + 0.5f - 4.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy), angulo = Mathf.Atan2(dy, dx);
        float braco = 0.5f + 0.5f * Mathf.Cos(5f * (angulo - Mathf.PI / 2f));
        float raio = 1.6f + 3f * braco * braco;
        if (r > raio) return Transparente;
        if (r > raio - 0.9f) return Cor('r');
        return (x + y) % 3 == 0 && r > 1f ? Cor('y') : Cor('o');
    }

    static Color32 CorSombraOval(int x, int y)
    {
        float ex = (x - 7.5f) / 8f, ey = (y - 2.5f) / 3f;
        return ex * ex + ey * ey <= 1f ? new Color32(0, 0, 0, 255) : Transparente;
    }

    // ---------------------------------------------------------------- céu

    // Sol: miolo claro e um brilho que vai sumindo (a cor do brilho vem da transparência).
    static Color32 CorSol(int x, int y)
    {
        float d = Mathf.Sqrt((x - 23.5f) * (x - 23.5f) + (y - 23.5f) * (y - 23.5f));
        if (d < 8f) return new Color32(255, 252, 230, 255);
        if (d < 9.5f) return new Color32(255, 240, 170, 255);
        float brilho = Mathf.Clamp01(1f - (d - 9.5f) / 14f);
        return new Color32(255, 235, 160, (byte)(brilho * brilho * 170f));
    }

    // Lua crescente com brilho em volta (para a fase noturna).
    static Color32 CorLua(int x, int y)
    {
        float d = Mathf.Sqrt((x - 19.5f) * (x - 19.5f) + (y - 19.5f) * (y - 19.5f));
        float dSombra = Mathf.Sqrt((x - 24f) * (x - 24f) + (y - 22f) * (y - 22f));
        if (d < 9f && dSombra >= 7.5f) return new Color32(235, 240, 255, 255);
        float brilho = Mathf.Clamp01(1f - (d - 7f) / 13f);
        return new Color32(200, 210, 255, (byte)(brilho * brilho * 110f));
    }

    // Faixa de luz que desce do sol (some no meio e nas pontas).
    static Color32 CorRaioDeLuz(int x, int y)
    {
        float lado = 1f - Mathf.Abs(x - 15.5f) / 16f;
        float altura = y / 127f; // 1 = em cima (perto do sol)
        return new Color32(255, 250, 225, (byte)(lado * lado * altura * 130f));
    }

    // Neblina perto do chão: branca, opaca embaixo e sumindo para cima.
    static Color32 CorNeblina(int x, int y)
    {
        float a = Mathf.Pow(1f - y / 31f, 1.6f);
        return new Color32(255, 255, 255, (byte)(a * 255f));
    }

    // Folhagem bem na frente da câmera (silhueta de moitas arredondadas). Repete a cada 128 px.
    static Color32 CorFrente(int x, int y)
    {
        float t = 2f * Mathf.PI * x / 128f;
        float moita = 9f + 5f * Mathf.Abs(Mathf.Sin(t * 2f)) + 2f * Mathf.Sin(t * 5f + 1f);
        return y < moita ? Cheio : Transparente;
    }

    // Céu em degradê (cor de cima -> cor de baixo), feito sob medida para cada fase.
    public static Sprite Degrade(Color32 topo, Color32 baixo)
    {
        string nome = $"degrade:{topo}|{baixo}";
        if (cache.TryGetValue(nome, out Sprite pronto) && pronto != null) return pronto;
        Sprite sprite = Procedural(1, 64, Base, (x, y) => Color32.Lerp(baixo, topo, y / 63f));
        sprite.name = nome;
        cache[nome] = sprite;
        return sprite;
    }

    // Céu com 3 cores (em cima -> no meio -> embaixo): o do HD-2D.
    public static Sprite Degrade(Color32 topo, Color32 meio, Color32 baixo)
    {
        string nome = $"degrade:{topo}|{meio}|{baixo}";
        if (cache.TryGetValue(nome, out Sprite pronto) && pronto != null) return pronto;
        Sprite sprite = Procedural(1, 64, Base, (x, y) =>
        {
            float t = y / 63f; // 0 embaixo, 1 em cima
            // o meio fica um pouco abaixo da metade: a parte azul de cima ocupa mais espaço
            return t < 0.4f ? Color32.Lerp(baixo, meio, t / 0.4f) : Color32.Lerp(meio, topo, (t - 0.4f) / 0.6f);
        });
        sprite.name = nome;
        cache[nome] = sprite;
        return sprite;
    }

    // Água do mapa: azul com ondinhas que andam (4 quadros).
    static Color32 CorAgua(int x, int y, int quadro)
    {
        bool onda = (y % 6 == 2 && (x + quadro * 4) % 16 < 5) || (y % 6 == 5 && (x + 8 - quadro * 4 + 32) % 16 < 4);
        if (onda) return new Color32(170, 215, 250, 255);
        return (x * 3 + y * 5) % 11 == 0 ? new Color32(62, 128, 210, 255) : new Color32(74, 144, 226, 255);
    }

    // Ponte de madeira (vista de cima): tábuas com corrimão em cima e embaixo. A água aparece por baixo.
    static Color32 CorPonte(int x, int y)
    {
        if (y == 0 || y == 15) return Transparente;
        if (y <= 2 || y >= 13) return y == 1 || y == 14 ? Cor('k') : Cor('d');
        if (x % 4 == 3) return Cor('d');
        return Ruido(x, y) % 7 == 0 ? Cor('b') : Cor('n');
    }

    // Montanha com neve no pico.
    static Color32 CorMontanha(int x, int y)
    {
        float meia = (15 - y) * 0.5f + 0.5f, dx = Mathf.Abs(x - 7.5f);
        if (dx > meia) return Transparente;
        if (dx > meia - 1f || y == 0) return Cor('k');
        if (y >= 11) return Cor('w');
        return x < 7.5f ? Cor('s') : Cor('S');
    }

    // Terra batida do caminho.
    static Color32 CorTerra(int x, int y)
    {
        int r = Ruido(x, y) % 13;
        if (r == 0) return Cor('b');
        if (r == 1) return Cor('y');
        return new Color32(232, 200, 150, 255);
    }

    // Faixa de céu do horizonte do mapa: azul em cima, quase branco perto do chão.
    static Color32 CorCeu(int x, int y) => Color32.Lerp(new Color32(225, 240, 250, 255), new Color32(130, 195, 240, 255), y / 31f);

    // Sombra de nuvem passando pelo chão (preta, o mapa deixa bem transparente).
    static Color32 CorSombraDaNuvem(int x, int y)
    {
        float ex = (x - 23.5f) / 23f, ey = (y - 9.5f) / 9f;
        float d = ex * ex + ey * ey;
        if (d > 1f || (d > 0.7f && (x + y) % 2 == 0)) return Transparente;
        return new Color32(0, 0, 0, 255);
    }

    // Névoa (a névoa do alto-mar) que esconde o que você ainda não descobriu.
    // Uma bolota com a borda "pontilhada", para as bolotas vizinhas se misturarem.
    static Color32 CorNevoa(int x, int y)
    {
        float dx = x - 7.5f, dy = y - 7.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 8f) return Transparente;
        bool pontilhado = d > 6f && (x + y) % 2 == 0;
        if (pontilhado) return Transparente;
        return Ruido(x, y) % 9 == 0 ? new Color32(70, 34, 96, 255) : new Color32(44, 22, 64, 255);
    }

    // Árvore redondinha do mapa (copa verde com contorno e tronco marrom).
    static bool DentroDaCopa(int x, int y) => (x - 7.5f) * (x - 7.5f) + (y - 15f) * (y - 15f) <= 7.6f * 7.6f;

    static Color32 CorArvore(int x, int y)
    {
        if (DentroDaCopa(x, y))
        {
            bool borda = !DentroDaCopa(x + 1, y) || !DentroDaCopa(x - 1, y) || !DentroDaCopa(x, y + 1) || !DentroDaCopa(x, y - 1);
            if (borda) return Cor('q');
            if ((x - 7.5f) - (y - 15f) > 4f) return Cor('G'); // sombra embaixo à direita
            if ((x - 4f) * (x - 4f) + (y - 18f) * (y - 18f) < 5f) return Cor('a'); // brilho
            return Cor('g');
        }
        if (x >= 6 && x <= 9 && y <= 8) return x == 6 || x == 9 ? Cor('d') : Cor('b');
        return Transparente;
    }

    // Porta das Cabines Assombradas: madeira com moldura lilás e topo em arco. Aberta, mostra a escuridão mágica.
    static bool DentroDaPorta(int x, int y) =>
        x >= 1 && x <= 14 && y >= 0 && (y < 24 || (x - 7.5f) * (x - 7.5f) + (y - 24f) * (y - 24f) <= 6.6f * 6.6f);

    static Color32 CorPorta(int x, int y, bool aberta)
    {
        if (!DentroDaPorta(x, y)) return Transparente;
        bool borda = !DentroDaPorta(x + 1, y) || !DentroDaPorta(x - 1, y) || !DentroDaPorta(x, y + 1) || (y == 0);
        if (borda) return Cor('k');
        bool moldura = !DentroDaPorta(x + 2, y) || !DentroDaPorta(x - 2, y) || !DentroDaPorta(x, y + 2);
        if (moldura) return Cor('V');
        if (aberta) return Ruido(x, y) % 11 == 0 ? Cor('P') : (Ruido(x, y) % 5 == 0 ? Cor('v') : Cor('x'));
        if (x == 11 && (y == 12 || y == 13)) return Cor('y'); // maçaneta
        return x % 4 == 2 ? Cor('d') : Cor('b');              // tábuas
    }

    // Ponto de save: um cristal num pedestal. Apagado é cinza; ativo, roxo e brilhando.
    static Color32 CorPontoDeSave(int x, int y, bool ativo)
    {
        if (y <= 2) return x >= 3 && x <= 12 ? (y == 0 || x == 3 || x == 12 ? Cor('k') : Cor('S')) : Transparente;
        float dx = Mathf.Abs(x - 7.5f), meia = 5.5f * (1f - Mathf.Abs(y - 12f) / 9f);
        if (y > 21 || dx > meia) return Transparente;
        if (dx > meia - 1f) return Cor('k');
        if (!ativo) return x < 7.5f ? Cor('s') : Cor('S');
        if (x == 5 && y >= 11 && y <= 15) return Cor('w');
        return x < 7.5f ? Cor('P') : Cor('V');
    }

    // "!" de perigo (avisa a altura em que a Baleia vai passar).
    static Color32 CorAviso(int x, int y)
    {
        float dx = x - 7.5f, dy = y - 7.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 7.6f) return Transparente;
        if (d > 6.4f) return Cor('k');
        if ((x == 7 || x == 8) && ((y >= 6 && y <= 12) || (y >= 3 && y <= 4))) return Cor('w');
        return Cor('r');
    }

    // ---------------------------------------------------------------- texto em pixel art

    // Letrinhas 5x7 (todas as maiúsculas, números e pontuação), usadas no logo, no "DEAD > CONTINUE" e no HUD.
    // O 'e' minúsculo é só do logo "Cilada à Vista!". Caractere que não existe aqui vira espaço.
    static readonly Dictionary<char, string[]> Letras = new Dictionary<char, string[]>
    {
        { 'A', new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        { 'B', new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." } },
        { 'C', new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." } },
        { 'D', new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." } },
        { 'E', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
        { 'F', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." } },
        { 'G', new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####" } },
        { 'H', new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
        { 'I', new[] { "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" } },
        { 'J', new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." } },
        { 'K', new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" } },
        { 'L', new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" } },
        { 'M', new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" } },
        { 'N', new[] { "#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#" } },
        { 'O', new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
        { 'P', new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." } },
        { 'Q', new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" } },
        { 'R', new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" } },
        { 'S', new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
        { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
        { 'U', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
        { 'V', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." } },
        { 'W', new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" } },
        { 'X', new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" } },
        { 'Y', new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." } },
        { 'Z', new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" } },
        { '0', new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." } },
        { '1', new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." } },
        { '2', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" } },
        { '3', new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." } },
        { '4', new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." } },
        { '5', new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." } },
        { '6', new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." } },
        { '7', new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." } },
        { '8', new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." } },
        { '9', new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." } },
        { 'e', new[] { "....", "....", ".##.", "#..#", "####", "#...", ".###" } },
        { ':', new[] { ".", ".", "#", ".", ".", "#", "." } },
        { '!', new[] { "#", "#", "#", "#", "#", ".", "#" } },
        { '?', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." } },
        { '.', new[] { ".", ".", ".", ".", ".", ".", "#" } },
        { ',', new[] { "..", "..", "..", "..", "..", ".#", "#." } },
        { '\'', new[] { "#", "#", ".", ".", ".", ".", "." } },
        { '"', new[] { "#.#", "#.#", "...", "...", "...", "...", "..." } },
        { '-', new[] { "....", "....", "....", "####", "....", "....", "...." } },
        { '+', new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." } },
        { '=', new[] { "....", "....", "####", "....", "####", "....", "...." } },
        { '/', new[] { "....#", "....#", "...#.", "..#..", ".#...", "#....", "#...." } },
        { '(', new[] { ".#", "#.", "#.", "#.", "#.", "#.", ".#" } },
        { ')', new[] { "#.", ".#", ".#", ".#", ".#", ".#", "#." } },
        { '%', new[] { "##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##" } },
        { '*', new[] { ".....", "#.#.#", ".###.", "#####", ".###.", "#.#.#", "....." } },
        { '>', new[] { "#...", "##..", "###.", "####", "###.", "##..", "#..." } },
        { '<', new[] { "...#", "..##", ".###", "####", ".###", "..##", "...#" } },
        { ' ', new[] { "..", "..", "..", "..", "..", "..", ".." } },
    };

    // Acentos (2 linhas em cima da letra) e cedilha (2 linhas embaixo). Só aparecem com comAcentos = true.
    static readonly Dictionary<char, string[]> Acentos = new Dictionary<char, string[]>
    {
        { '\'', new[] { "...#.", "..#.." } }, // agudo
        { '`', new[] { ".#...", "..#.." } },  // crase
        { '^', new[] { "..#..", ".#.#." } },  // circunflexo
        { '~', new[] { ".##.#", "#.##." } },  // til
        { ',', new[] { "..#..", ".##.." } },  // cedilha
    };

    // Letra acentuada -> (letra base, acento).
    static readonly Dictionary<char, (char letra, char acento)> LetrasAcentuadas = new Dictionary<char, (char, char)>
    {
        { 'Á', ('A', '\'') }, { 'À', ('A', '`') }, { 'Â', ('A', '^') }, { 'Ã', ('A', '~') },
        { 'É', ('E', '\'') }, { 'Ê', ('E', '^') }, { 'Í', ('I', '\'') },
        { 'Ó', ('O', '\'') }, { 'Ô', ('O', '^') }, { 'Õ', ('O', '~') },
        { 'Ú', ('U', '\'') }, { 'Ç', ('C', ',') },
    };

    // A "chave" do cache de textos: o texto e as cores. É uma struct (e não uma string montada com $"...")
    // para não criar lixo na memória a cada quadro: o HUD pede os mesmos textos 60 vezes por segundo,
    // e cada string nova vira trabalho para o "coletor de lixo" (GC), que de vez em quando PARA o jogo
    // para limpar a memória. Esses engasgos são um dos motivos de um jogo parecer "travado".
    struct ChaveDeTexto : IEquatable<ChaveDeTexto>
    {
        public string texto;
        public uint letra, contorno, sombra; // cada cor guardada num número só (r, g, b, a = 4 bytes)
        public int opcoes;                   // 1 = tem sombra, 2 = com acentos, 4 = em maiúsculas

        public bool Equals(ChaveDeTexto outra) =>
            letra == outra.letra && contorno == outra.contorno && sombra == outra.sombra && opcoes == outra.opcoes && texto == outra.texto;
        public override bool Equals(object outra) => outra is ChaveDeTexto c && Equals(c);
        public override int GetHashCode()
        {
            unchecked { return ((((texto.GetHashCode() * 31 + (int)letra) * 31 + (int)contorno) * 31 + (int)sombra) * 31) + opcoes; }
        }
    }

    static uint Numero(Color32 c) => (uint)(c.r << 24 | c.g << 16 | c.b << 8 | c.a);

    static readonly Dictionary<ChaveDeTexto, Texture2D> cacheDeTextos = new Dictionary<ChaveDeTexto, Texture2D>();

    // Texto como Sprite, para colocar no mundo (ex.: o número de cada ponto do mapa).
    // Fica no cache de sprites, que nunca é limpo durante o jogo.
    public static Sprite SpriteDeTexto(string texto, Color32 corLetra, Color32 corContorno)
    {
        string nome = $"texto:{texto}|{corLetra}|{corContorno}";
        if (cache.TryGetValue(nome, out Sprite pronto) && pronto != null) return pronto;
        Texture2D textura = GerarTexto(texto, corLetra, corContorno, null, false);
        var sprite = Sprite.Create(textura, new Rect(0, 0, textura.width, textura.height), Centro, PixelsPorUnidade, 0, SpriteMeshType.FullRect);
        sprite.name = nome;
        cache[nome] = sprite;
        return sprite;
    }
    const int MaximoDeTextosGuardados = 400; // o HUD muda (tempo, mortes...): de vez em quando limpa o cache

    // Desenha o texto com as letrinhas acima, contorno de 1 pixel e (se quiser) uma sombra para baixo.
    // Devolve uma textura pequena: desenhe ela grande na tela (o filtro "Point" mantém os pixels nítidos).
    // comAcentos = true reserva espaço para acentos e cedilha (o HUD usa assim, para todas as linhas terem a mesma altura).
    // maiusculas = true escreve tudo em MAIÚSCULAS (a conversão só acontece na primeira vez: depois vem do cache).
    // A largura (em pixels da fonte) que o TextoEmPixel desse texto teria, sem desenhar nada:
    // serve para quebrar as linhas das caixas de fala. (comSombra: o texto tem a sombra de 1 pixel)
    public static int LarguraDoTextoEmPixel(string texto, bool comSombra = true, bool maiusculas = true)
    {
        int largura = 2 + (comSombra ? 1 : 0) - 1;
        foreach (char original in texto)
        {
            char c = maiusculas ? char.ToUpperInvariant(original) : original;
            if (LetrasAcentuadas.TryGetValue(c, out var composta)) c = composta.letra;
            largura += (Letras.TryGetValue(c, out string[] letra) ? letra : Letras[' '])[0].Length + 1;
        }
        return largura;
    }

    public static Texture2D TextoEmPixel(string texto, Color32 corLetra, Color32 corContorno, Color32? corSombra = null, bool comAcentos = false, bool maiusculas = false)
    {
        var chave = new ChaveDeTexto
        {
            texto = texto,
            letra = Numero(corLetra),
            contorno = Numero(corContorno),
            sombra = corSombra.HasValue ? Numero(corSombra.Value) : 0u,
            opcoes = (corSombra.HasValue ? 1 : 0) | (comAcentos ? 2 : 0) | (maiusculas ? 4 : 0),
        };
        if (cacheDeTextos.TryGetValue(chave, out Texture2D pronta) && pronta != null) return pronta;
        if (cacheDeTextos.Count > MaximoDeTextosGuardados)
        {
            foreach (Texture2D velha in cacheDeTextos.Values) UnityEngine.Object.Destroy(velha);
            cacheDeTextos.Clear();
        }
        Texture2D textura = GerarTexto(maiusculas ? texto.ToUpperInvariant() : texto, corLetra, corContorno, corSombra, comAcentos);
        cacheDeTextos[chave] = textura;
        return textura;
    }

    static Texture2D GerarTexto(string texto, Color32 corLetra, Color32 corContorno, Color32? corSombra, bool comAcentos)
    {

        const int alturaLetra = 7, margem = 1;
        int espacoAcima = comAcentos ? 3 : 0, espacoAbaixo = comAcentos ? 2 : 0;
        int sombra = corSombra.HasValue ? 1 : 0;
        int largura = margem * 2 + sombra - 1, altura = espacoAcima + alturaLetra + espacoAbaixo + margem * 2 + sombra;

        string[] Desenho(char c, out string[] acento)
        {
            acento = null;
            if (LetrasAcentuadas.TryGetValue(c, out var composta))
            {
                if (comAcentos) acento = Acentos[composta.acento];
                c = composta.letra;
            }
            return Letras.TryGetValue(c, out string[] letra) ? letra : Letras[' '];
        }
        foreach (char c in texto) largura += Desenho(c, out _)[0].Length + 1;

        // marca onde tem letra (aqui y = 0 é o topo)
        var cheio = new bool[largura, altura];
        int cursor = margem, topo = margem + espacoAcima;
        foreach (char c in texto)
        {
            string[] letra = Desenho(c, out string[] acento);
            int larguraLetra = letra[0].Length;
            for (int y = 0; y < alturaLetra; y++)
                for (int x = 0; x < larguraLetra; x++)
                    if (letra[y][x] == '#') cheio[cursor + x, topo + y] = true;
            if (acento != null)
            {
                bool cedilha = c == 'Ç';
                int y0 = cedilha ? topo + alturaLetra : topo - 3; // acento: 1 linha de folga acima da letra
                int recorte = (5 - larguraLetra) / 2;            // letra estreita (I): usa o meio do acento
                for (int y = 0; y < 2; y++)
                    for (int x = 0; x < larguraLetra; x++)
                        if (acento[y][x + recorte] == '#') cheio[cursor + x, y0 + y] = true;
            }
            cursor += larguraLetra + 1;
        }

        bool Letra(int x, int y) => x >= 0 && y >= 0 && x < largura && y < altura && cheio[x, y];
        bool PertoDeLetra(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if (Letra(x + dx, y + dy)) return true;
            return false;
        }

        var textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        var pixels = new Color32[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                Color32 cor = Transparente;
                if (Letra(x, y)) cor = corLetra;
                else if (PertoDeLetra(x, y)) cor = corContorno;
                else if (sombra > 0 && PertoDeLetra(x - 1, y - 1)) cor = corSombra.Value;
                pixels[(altura - 1 - y) * largura + x] = cor; // a textura começa por baixo
            }
        textura.SetPixels32(pixels);
        textura.Apply();
        return textura;
    }
}
