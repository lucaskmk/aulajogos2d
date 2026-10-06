using System;
using System.Collections.Generic;
using UnityEngine;

// Cria TODOS os sprites do jogo por código, sem precisar importar imagens.
// Os personagens e os blocos são desenhados em "pixel art de texto": cada letra é uma cor
// da paleta e '.' é transparente. Quer mudar o visual? É só editar os desenhos abaixo!
// Serra, espinho, bandeira e nuvens são desenhados com fórmulas (procedural).
// O visual segue o estilo dos ímãs de acrílico de Re:Zero: pedra cinza com grama por cima,
// fundo pastel e personagens chibi.
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
        // --- Re:Zero ---
        { 'h', new Color32(44, 40, 56, 255) },    // cabelo do Subaru
        { 'H', new Color32(96, 92, 120, 255) },   // brilho do cabelo
        { 'c', new Color32(252, 220, 186, 255) }, // pele
        { 'C', new Color32(222, 176, 146, 255) }, // pele (sombra)
        { 'j', new Color32(36, 36, 44, 255) },    // jaqueta de moletom preta
        { 'u', new Color32(88, 88, 100, 255) },   // calça
        { 'f', new Color32(34, 34, 58, 255) },    // pelo do Mabeast
        { 'F', new Color32(72, 72, 112, 255) },   // pelo do Mabeast (brilho)
        { 'x', new Color32(24, 8, 34, 255) },     // sombra da Bruxa
        { 'v', new Color32(92, 40, 132, 255) },   // roxo (aura da sombra)
        { 'V', new Color32(176, 96, 230, 255) },  // roxo claro (unhas, bloco mágico)
        { 'P', new Color32(215, 170, 245, 255) }, // lilás (brilho do bloco mágico)
        { 'J', new Color32(74, 74, 90, 255) },    // jaqueta (brilho)
        { 'a', new Color32(176, 236, 104, 255) }, // verde claro (topo da grama)
        { 'q', new Color32(30, 110, 60, 255) },   // verde bem escuro (contorno da grama)
        { 'm', new Color32(190, 194, 208, 255) }, // pedra
        { 'M', new Color32(148, 152, 170, 255) }, // pedra (sombra)
        { 'Z', new Color32(226, 230, 240, 255) }, // pedra (brilho)
        { 'z', new Color32(96, 98, 120, 255) },   // rejunte da pedra
    };

    static Color32 Cor(char c) => paleta[c];
    static readonly Color32 Transparente = new Color32(0, 0, 0, 0);

    // ---------------------------------------------------------------- desenhos

    // Subaru Natsuki chibi: cabelo espetado e o moletom preto com detalhes laranja.
    static readonly string[] ArteJogador =
    {
        ".....k.k..k.....",
        "....khkhkkhk....",
        "...khhhhhhhhk...",
        "..khHHhhhhhhhk..",
        "..khhhhhhhhhhhk.",
        "..khhhchhhchhhk.",
        "..khhccccccccck.",
        "..khcckcccckcck.",
        "..khcckcccckcck.",
        "...kcccccCccck..",
        "....kkjoojkk....",
        "...kjjjoojjjk...",
        "..kcjJjjsjjjck..",
        "...kjjjjsjjjk...",
        "....kuuuuuuk....",
        "....kwwk.kwwk...",
    };

    static readonly string[] ArteJogadorAndando =
    {
        ".....k.k..k.....",
        "....khkhkkhk....",
        "...khhhhhhhhk...",
        "..khHHhhhhhhhk..",
        "..khhhhhhhhhhhk.",
        "..khhhchhhchhhk.",
        "..khhccccccccck.",
        "..khcckcccckcck.",
        "..khcckcccckcck.",
        "...kcccccCccck..",
        "....kkjoojkk....",
        "...kjjjoojjjk...",
        "..kcjJjjsjjjck..",
        "...kjjjjsjjjk...",
        "...kuuuk.kuuk...",
        "..kwwk....kwwk..",
    };

    // Mabeast (estilo Wolgarm): bicho de pelo escuro, chifre e olhos vermelhos.
    // Os pés são diferentes de propósito: o flipX do Inimigo vira a "andadinha".
    static readonly string[] ArteInimigo =
    {
        "................",
        ".......kk.......",
        "...k...ksk...k..",
        "..kFk..kSk..kFk.",
        "..kfFkkffkkkffk.",
        "..kffffffffffk..",
        ".kfffffffffffk..",
        ".kffrrffffrrffk.",
        ".kffkrffffrkffk.",
        ".kfffffffffffk..",
        ".kffkwkkkkwkffk.",
        "..kfffffffffffk.",
        "..kffffffffffk..",
        "...kkkkkkkkkk...",
        "..kffk...kffk...",
        "..kkkk....kkkk..",
    };

    static readonly string[] ArteInimigoEspinhos =
    {
        "......kwk.......",
        "...k..kwk....k..",
        "..ksk.ksk...ksk.",
        "..kfFkkffkkkffk.",
        "..kffffffffffk..",
        ".ksffffffffffks.",
        ".kfffffffffffk..",
        ".kffrrffffrrffk.",
        ".kffrrffffrrffk.",
        ".kfffffffffffk..",
        ".kfkwrwrwrwrkfk.",
        "..kfkwkwkwkwkfk.",
        "..kffffffffffk..",
        "...kkkkkkkkkk...",
        "..kffk...kffk...",
        "..kkkk....kkkk..",
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
        "...k........k...",
        "..kFkkkkkkkkFk..",
        ".kffkkffffkkffk.",
        ".kffffffffffffk.",
        "..kkkkkkkkkkkk..",
    };

    // Mão de sombra da Bruxa, que aparece na tela do Retorno pela Morte.
    static readonly string[] ArteMaoDaSombra =
    {
        ".......vv.......",
        "....vvvVVv......",
        "...vVVvxxvvv....",
        "...vxxvxxvVVv...",
        ".vvvxxvxxvxxv...",
        "vVVvxxvxxvxxv...",
        "vxxvxxvxxvxxv...",
        "vxxvxxvxxvxxvvv.",
        "vxxvxxvxxvxxvVVv",
        "vxxvxxvxxvxxvxxv",
        "vxxvxxvxxvxxvxxv",
        "vxxvxxvxxvxxxxv.",
        "vxxxxxxxxxxxxxv.",
        "vxxxxxxxxxxxxv..",
        "vxxxxxxxxxxxxv..",
        ".vxxxxxxxxxxv...",
        ".vxxxxxxxxxxv...",
        "..vxxxxxxxxv....",
        "..vxxxxxxxxv....",
        "...vxxxxxxv.....",
        "...vxxxxxxv.....",
        "...vxxxxxv......",
        "...vxxxxxv......",
        "...vxxxxxv......",
    };

    // Bloco mágico lilás com '?' dourado.
    static readonly string[] ArteBlocoSurpresa =
    {
        "kkkkkkkkkkkkkkkk",
        "kPPPPPPPPPPPPPVk",
        "kPwVVVVVVVVVVwvk",
        "kPVVVVyyyyVVVVvk",
        "kPVVVyyooyyVVVvk",
        "kPVVVVoVVyyoVVvk",
        "kPVVVVVVyyoVVVvk",
        "kPVVVVVyyoVVVVvk",
        "kPVVVVVyyoVVVVvk",
        "kPVVVVVVooVVVVvk",
        "kPVVVVVyyVVVVVvk",
        "kPVVVVVyyoVVVVvk",
        "kPVVVVVVooVVVVvk",
        "kPwVVVVVVVVVVwvk",
        "kVvvvvvvvvvvvvvk",
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

    // Pedra cinza em tijolões (como a base dos dioramas dos ímãs).
    static readonly string[] ArteChao =
    {
        "zZZZZZzzzZZZZZzz",
        "ZmmmmmMzZmmmmmMz",
        "ZmmmmmMzZmmmmmMz",
        "ZmmmmmMzZmmmmmMz",
        "ZmmmmmMzZmmmmmMz",
        "ZmmmmmMzZmmmmmMz",
        "zMMMMMzzzMMMMMzz",
        "zzzzzzzzzzzzzzzz",
        "ZZzzzZZZZZzzzZZZ",
        "mmMzZmmmmmMzZmmm",
        "mmMzZmmmmmMzZmmm",
        "mmMzZmmmmmMzZmmm",
        "mmMzZmmmmmMzZmmm",
        "mmMzZmmmmmMzZmmm",
        "MMzzzMMMMMzzzMMM",
        "zzzzzzzzzzzzzzzz",
    };

    // Grama "escorrendo" por cima da pedra.
    static readonly string[] ArteChaoTopo =
    {
        "qqqqqqqqqqqqqqqq",
        "aaaaaaaaaaaaaaaa",
        "aagaaaaaaaaaaaaa",
        "gggggggggggggggg",
        "gggggggggggggggg",
        "GGGGGGGGGGGGGGGG",
        "GqGGqqGGGqGGqGqG",
        "qzGqzzqGGzqqzGzq",
        "ZZqzzZZGqZzzzqZZ",
        "mmMzZmmqmmMzZmmm",
        "mmMzZmmmmmMzZmmm",
        "mmMzZmmmmmMzZmmm",
        "mmMzZmmmmmMzZmmm",
        "mmMzZmmmmmMzZmmm",
        "MMzzzMMMMMzzzMMM",
        "zzzzzzzzzzzzzzzz",
    };

    // 'B': caixote de madeira.
    static readonly string[] ArteCaixote =
    {
        "dddddddddddddddd",
        "dSnnnnnnnnnnnnSd",
        "dbbbbbbbbbbbndbd",
        "dbbbbbbbbbbndbbd",
        "dbbbbbbbbbndbbbd",
        "dddddddddndddddd",
        "dnnnnnnnndnnnnnd",
        "dbbbbbbndbbbbbbd",
        "dbbbbbndbbbbbbbd",
        "dbbbbndbbbbbbbbd",
        "ddddnddddddddddd",
        "dnnndnnnnnnnnnnd",
        "dbndbbbbbbbbbbbd",
        "dndbbbbbbbbbbbbd",
        "dSbbbbbbbbbbbbSd",
        "dddddddddddddddd",
    };

    static readonly string[] ArteBlocoUsado =
    {
        "zzzzzzzzzzzzzzzz",
        "zmmmmmmmmmmmmmmz",
        "zmkMMMMMMMMMMkMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmMMMMMMMMMMMMMz",
        "zmkMMMMMMMMMMkMz",
        "zmMMMMMMMMMMMMMz",
        "zzzzzzzzzzzzzzzz",
    };

    // Puck, o espírito-gatinho da Emilia: é ele quem "dá as dicas" (mentirosas) no lugar das placas.
    static readonly string[] ArtePuck =
    {
        "................",
        ".k.........k....",
        "kpk.......kpk...",
        "kppkkkkkkkppk...",
        "ksssssssssssk...",
        "ksssssssssssk.kk",
        "kskwssssskwsk.k.",
        "kskksspsskksk.k.",
        "kpsssskksssspk.k",
        ".kssssssssssk.k.",
        "..kksssssssskkk.",
        "...ksssssssk....",
        "...kswssswsk....",
        "....kkk.kkk.....",
        "................",
        "................",
    };

    // Grande Coelho: fofinho, pequeno... e se multiplica.
    static readonly string[] ArteCoelho =
    {
        "................",
        "................",
        "................",
        "....kkk.kkk.....",
        "....kpk.kpk.....",
        "....kpk.kpk.....",
        "....kwk.kwk.....",
        "...kkwwkwwkk....",
        "..kwwwwwwwwwk...",
        "..kwrwwwrwwwk...",
        ".kwwwwpwwwwwwk..",
        ".kwwwwwwwwwwwwk.",
        ".kwwwwwwwwwwwwk.",
        "..klwwwwwwwwlk..",
        "...kkkk..kkkk...",
        "................",
    };

    // Emilia: cabelo prateado, olhos roxos e a florzinha no cabelo.
    static readonly string[] ArteEmilia =
    {
        ".....kkkkkk.....",
        "...kkswwssskk...",
        "..kswwssssssPk..",
        ".kswssssssssPPk.",
        ".ksssssssssswPk.",
        ".kssskssssksssk.",
        ".ksskcccccckssk.",
        ".kskcVVccVVcksk.",
        ".kskcVkccVkcksk.",
        ".kskpccccccpksk.",
        ".ksskccCCcckssk.",
        ".ksssskkkkssssk.",
        ".kssvkwwwwkvssk.",
        ".kssckwvvwkcssk.",
        "kssskwwvvwwksssk",
        ".kskwwwwwwwwksk.",
        ".kskwwvwwvwwksk.",
        "..kwwwwwwwwwwk..",
        ".kwwwwwwwwwwwwk.",
        ".kvwwwwwwwwwwvk.",
        "kvvvvvvvvvvvvvvk",
        ".kkkkkkkkkkkkkk.",
        ".....kck..kck...",
        ".....kkk..kkk...",
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

    // Pontinho do caminho no mapa do mundo.
    static readonly string[] ArteTrilha =
    {
        ".dd.",
        "dnnd",
        "dnnd",
        ".dd.",
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
            case "mao_sombra": return DeArte(ArteMaoDaSombra, Centro);
            case "puck": return DeArte(ArtePuck, Centro);
            case "coelho": return DeArte(ArteCoelho, Centro);
            case "emilia": return DeArte(ArteEmilia, Base);
            case "coracao": return DeArte(ArteCoracao, Centro);
            case "baleia": return Procedural(64, 24, Centro, CorBaleia);
            case "aviso": return Procedural(16, 16, Centro, CorAviso);

            // Mapa do mundo
            case "no_mapa": return Procedural(16, 16, Centro, CorNoDoMapa);
            case "trilha": return DeArte(ArteTrilha, Centro);
            case "arvore": return Procedural(16, 24, Base, CorArvore);

            case "chao": return DeArte(ArteChao, Centro);
            case "chao_topo": return DeArte(ArteChaoTopo, Centro);
            case "tijolo": return DeArte(ArteCaixote, Centro);
            case "bloco_usado": return DeArte(ArteBlocoUsado, Centro);
            case "tufo": return DeArte(ArteTufo, Base);
            case "flor": return DeArte(ArteFlor, Base);
            case "brilho": return DeArte(ArteBrilho, Centro);
            case "poeira": return DeArte(ArtePoeira, Centro);

            case "espinho": return Procedural(16, 16, Centro, CorEspinho);
            case "serra": return Procedural(16, 16, Centro, CorSerra);
            case "bandeira": return Procedural(16, 48, Base, (x, y) => CorBandeira(x, y, false, 0));
            case "bandeira_falsa": return Procedural(16, 48, Base, (x, y) => CorBandeira(x, y, true, 0));
            case "nuvem": return Procedural(32, 18, Centro, (x, y) => CorNuvem(x, y, false));
            case "nuvem_malvada": return Procedural(32, 18, Centro, (x, y) => CorNuvem(x, y, true));
            case "cano_topo": return Procedural(32, 16, Centro, (x, y) => CorCano(x, y, true));
            case "cano_corpo": return Procedural(32, 16, Centro, (x, y) => CorCano(x, y, false));

            // Camadas do fundo (paralaxe). São brancas/cinza: o jogo pinta com a cor de cada fase.
            case "fundo_castelo": return Procedural(384, 176, Base, CorCastelo);
            case "fundo_floresta": return Procedural(128, 112, Base, CorFloresta);
            case "fundo_arvores": return Procedural(128, 80, Base, CorArvores);
        }

        // Quadros de animação: "bandeira_2" (pano tremulando), "bloco_surpresa_brilho_3" (brilho passando).
        int sublinhado = nome.LastIndexOf('_');
        if (sublinhado > 0 && int.TryParse(nome.Substring(sublinhado + 1), out int quadro))
        {
            switch (nome.Substring(0, sublinhado))
            {
                case "bandeira": return Procedural(16, 48, Base, (x, y) => CorBandeira(x, y, false, quadro));
                case "bloco_surpresa_brilho": return DeArte(ArteBlocoSurpresa, Centro, PixelsPorUnidade, quadro * 5f);
            }
        }
        throw new ArgumentException("Sprite desconhecido: " + nome);
    }

    public const int QuadrosDaBandeira = 4;
    public const int QuadrosDoBrilho = 7;

    // Nomes dos quadros, para passar para a AnimacaoDeQuadros.
    public static Sprite[] Quadros(string nome, int quantidade)
    {
        var quadros = new Sprite[quantidade];
        for (int i = 0; i < quantidade; i++) quadros[i] = Pegar(nome + "_" + i);
        return quadros;
    }

    // brilho: posição de uma faixa diagonal clara passando pelo desenho (NaN = sem brilho).
    static Sprite DeArte(string[] linhas, Vector2 pivo, int pixelsPorUnidade = PixelsPorUnidade, float brilho = float.NaN)
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

    static Sprite Procedural(int largura, int altura, Vector2 pivo, Func<int, int, Color32> corDoPixel, int pixelsPorUnidade = PixelsPorUnidade)
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

    // Cano visto de lado: contorno preto e sombreado de cilindro (brilho à esquerda, sombra à direita).
    // O topo é mais largo (a "boca" do cano); o corpo tem 2 pixels a menos de cada lado.
    static Color32 CorCano(int x, int y, bool topo)
    {
        bool boca = topo && y >= 5;
        int x0 = boca ? 0 : 2, x1 = boca ? 31 : 29;
        if (x < x0 || x > x1) return Transparente;
        if (x == x0 || x == x1) return Cor('k');
        if (boca && (y == 15 || y == 5)) return Cor('k');
        float t = (x - x0) / (float)(x1 - x0);
        if (t < 0.12f) return Cor('g');
        if (t < 0.3f) return Cor('a');
        if (t < 0.55f) return Cor('g');
        if (t < 0.8f) return Cor('G');
        return Cor('q');
    }

    // Fundo: "Cheio" é a silhueta, "Detalhe" um pouco mais escuro (janelas, sombras).
    static readonly Color32 Cheio = new Color32(255, 255, 255, 255);
    static readonly Color32 Detalhe = new Color32(222, 222, 228, 255);
    const int Solo = 32; // faixa cheia embaixo de cada camada, para não aparecer céu nos buracos

    // Castelo com torres e morros (camada mais distante).
    static Color32 CorCastelo(int x, int y)
    {
        if (y < Solo) return Cheio;
        int a = y - Solo; // altura acima do solo
        int cx = 192, dx = x - cx;

        // portão e janelas
        bool portao = Mathf.Abs(dx) <= 8 && (a < 18 || dx * dx + (a - 18) * (a - 18) < 64);
        bool janelaTorre = a >= 40 && a < 47 && (Mathf.Abs(dx + 48) <= 1 || Mathf.Abs(dx - 48) <= 1);
        bool janelaTorreao = a >= 60 && a < 67 && (Mathf.Abs(dx + 14) <= 2 || Mathf.Abs(dx) <= 2 || Mathf.Abs(dx - 14) <= 2);
        bool janelaMuro = a >= 20 && a < 27 && (Mathf.Abs(Mathf.Abs(dx) - 40) <= 2 || Mathf.Abs(Mathf.Abs(dx) - 22) <= 2);
        bool janelaPinaculo = a >= 88 && a < 96 && Mathf.Abs(dx) <= 1;

        bool castelo =
            (Mathf.Abs(dx) <= 56 && a < 48) || (Mathf.Abs(dx) <= 56 && a < 53 && ((dx + 56) / 6) % 2 == 0) // muralha + ameias
            || (Mathf.Abs(dx) <= 26 && a < 82) || (Mathf.Abs(dx) <= 26 && a < 87 && ((dx + 26) / 6) % 2 == 0) // torreão
            || Torre(dx + 48, a, 9, 72, 11, 24) || Torre(dx - 48, a, 9, 64, 11, 22) || Torre(dx, a, 7, 100, 9, 30) // torres
            || (dx == 0 && a >= 130 && a < 140) || (dx > 0 && dx <= 6 && a >= 135 && a < 140); // bandeirinha

        if (castelo) return portao || janelaTorre || janelaTorreao || janelaMuro || janelaPinaculo ? Detalhe : Cheio;

        float morro = 14f + 6f * Mathf.Sin(x * 0.0491f) + 3f * Mathf.Sin(x * 0.0982f + 1f); // repete a cada 384 px
        return a < morro ? Cheio : Transparente;
    }

    // Torre: corpo de meia-largura "meia" até a altura "alto" e telhado em cone por cima.
    static bool Torre(int dx, int a, int meia, int alto, int abaDoTelhado, int alturaDoTelhado)
    {
        if (Mathf.Abs(dx) <= meia && a < alto) return true;
        if (a < alto || a >= alto + alturaDoTelhado) return false;
        return Mathf.Abs(dx) <= abaDoTelhado * (1f - (a - alto) / (float)alturaDoTelhado);
    }

    // Floresta de pinheiros (camada do meio). Repete sem emenda a cada 128 px.
    static readonly Vector3Int[] Pinheiros = // (centro, altura, largura)
    {
        new Vector3Int(8, 58, 24), new Vector3Int(30, 74, 30), new Vector3Int(52, 50, 22),
        new Vector3Int(74, 66, 28), new Vector3Int(98, 80, 32), new Vector3Int(118, 56, 24),
    };

    static Color32 CorFloresta(int x, int y)
    {
        if (y < Solo + 4) return Cheio;
        int a = y - Solo;
        foreach (Vector3Int p in Pinheiros)
        {
            int dx = Mathf.Abs(x - p.x);
            dx = Mathf.Min(dx, 128 - dx); // dá a volta no fim da imagem
            if (a < 8)
            {
                if (dx <= 2) return Detalhe; // tronco
                continue;
            }
            if (a >= p.y) continue;
            int andar = (a - 8) % 14; // cada "andar" de galhos começa largo e afina
            float meia = p.z / 2f * (1f - (a - 8f) / (p.y - 8f)) * (0.65f + 0.35f * (1f - andar / 14f));
            if (dx <= meia) return andar < 3 && dx > meia - 3f ? Detalhe : Cheio;
        }
        return Transparente;
    }

    // Copas redondas de árvores (camada mais perto). Repete a cada 128 px.
    static readonly Vector3Int[] Copas = // (centro x, centro y, raio)
    {
        new Vector3Int(10, 22, 16), new Vector3Int(34, 30, 18), new Vector3Int(58, 18, 14),
        new Vector3Int(80, 26, 17), new Vector3Int(104, 20, 15), new Vector3Int(124, 28, 16),
    };

    static Color32 CorArvores(int x, int y)
    {
        if (y < Solo + 16) return Cheio;
        int a = y - Solo;
        foreach (Vector3Int c in Copas)
        {
            int dx = Mathf.Abs(x - c.x);
            dx = Mathf.Min(dx, 128 - dx);
            int dy = a - c.y;
            int d2 = dx * dx + dy * dy;
            if (d2 <= c.z * c.z)
                return d2 > (c.z - 2) * (c.z - 2) && dy > 0 ? Detalhe : Cheio; // contorninho na parte de cima da copa
        }
        return Transparente;
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

    // Baleia Branca (vista de lado, olhando para a esquerda): corpão, chifre na testa e cauda.
    static bool DentroDaBaleia(int x, int y)
    {
        if (x < 0 || y < 0 || x >= 64 || y >= 24) return false;
        float dy = y - 11f;
        if (x <= 30) // cabeça e meio do corpo
        {
            float ex = (x - 30f) / 26f, ey = dy / 9f;
            if (ex * ex + ey * ey <= 1f) return true;
        }
        else if (x <= 54 && Mathf.Abs(dy) <= 9f * (1f - (x - 30f) / 30f * 0.75f)) return true; // afina até a cauda
        if (x > 54 && x <= 58 && Mathf.Abs(dy) <= 2.5f) return true; // "pescoço" da cauda
        if (x >= 56 && x <= 63 && Mathf.Abs(dy) <= 1f + (x - 56) * 1.2f && !(x >= 61 && Mathf.Abs(dy) <= 1f)) return true; // nadadeira
        return y >= 17 && y <= 23 && Mathf.Abs(x - 13f) <= (23 - y) * 0.5f; // chifre
    }

    static Color32 CorBaleia(int x, int y)
    {
        if (!DentroDaBaleia(x, y)) return Transparente;
        bool borda = !DentroDaBaleia(x + 1, y) || !DentroDaBaleia(x - 1, y) || !DentroDaBaleia(x, y + 1) || !DentroDaBaleia(x, y - 1);
        if (borda) return Cor('k');
        if (y >= 18 && Mathf.Abs(x - 13f) <= (23 - y) * 0.5f) return Cor('n'); // chifre
        if (x >= 13 && x <= 15 && y >= 12 && y <= 14)               // olho vermelho
            return x == 14 && y == 13 ? Cor('k') : Cor('r');
        if (y == 8 && x >= 5 && x <= 20) return Cor('k');           // boca
        if (y < 8 && x < 40 && y % 2 == 0) return Cor('S');         // pregas da barriga
        if (y < 8) return Cor('s');
        if (y >= 15 && (x * 7 + y * 3) % 23 == 0) return Cor('l');  // cicatrizes
        return Cor('w');
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
    // O 'e' minúsculo é só do logo "Re:CILADA!". Caractere que não existe aqui vira espaço.
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

    static readonly Dictionary<string, Texture2D> cacheDeTextos = new Dictionary<string, Texture2D>();

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
    public static Texture2D TextoEmPixel(string texto, Color32 corLetra, Color32 corContorno, Color32? corSombra = null, bool comAcentos = false)
    {
        string chave = $"{texto}|{corLetra}|{corContorno}|{corSombra}|{comAcentos}";
        if (cacheDeTextos.TryGetValue(chave, out Texture2D pronta) && pronta != null) return pronta;
        if (cacheDeTextos.Count > MaximoDeTextosGuardados)
        {
            foreach (Texture2D velha in cacheDeTextos.Values) UnityEngine.Object.Destroy(velha);
            cacheDeTextos.Clear();
        }
        Texture2D textura = GerarTexto(texto, corLetra, corContorno, corSombra, comAcentos);
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
