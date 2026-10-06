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
        { 'e', new Color32(70, 120, 230, 255) },  // azul (olhos da Beatrice)
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

    // Beatrice: cabelo loiro em "furadeiras", laços cor-de-rosa e vestido vermelho.
    static readonly string[] ArteBeatrice =
    {
        ".....kkkkkk.....",
        "....kyyyyyyk....",
        "...kyyyyyyyyk...",
        "..kyyyyyyyyyyk..",
        ".kpkyyyyyyyykpk.",
        "kyykcccccccckyyk",
        "kyokceecceeckoyk",
        "kyykcekccekckyyk",
        "koykcpccccpckyok",
        "kyykcccCccckkyyk",
        "koyokkkkkkkkoyok",
        "kyykrrpwwprrkyyk",
        "kooykrrppprrkyok",
        "kyyk.krrrrk.kyyk",
        ".kok.krrrrk.kok.",
        "..k.krrppprk..k.",
        "....krrrrrrrk...",
        "...krrpppprrk...",
        "..krrrrrrrrrrk..",
        ".kpprrrrrrrrppk.",
        "kppppppppppppppk",
        ".kkkkkkkkkkkkkk.",
        ".....kck..kck...",
        ".....kkk..kkk...",
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

    // Casinha da capital (mapa do mundo).
    static readonly string[] ArteCasa =
    {
        "................",
        ".......kk.......",
        "......krrk......",
        ".....krrrrk.....",
        "....krrrrrrk....",
        "...krrrrrrrrk...",
        "..krrrrrrrrrrk..",
        ".kkkkkkkkkkkkkk.",
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
            case "beatrice": return DeArte(ArteBeatrice, Base);
            case "caveira": return DeArte(ArteCaveira, Centro);
            case "porta": return Procedural(16, 32, Base, (x, y) => CorPorta(x, y, false));
            case "porta_aberta": return Procedural(16, 32, Base, (x, y) => CorPorta(x, y, true));
            case "ponto_save": return Procedural(16, 24, Base, (x, y) => CorPontoDeSave(x, y, false));
            case "ponto_save_ativo": return Procedural(16, 24, Base, (x, y) => CorPontoDeSave(x, y, true));
            case "baleia": return Procedural(64, 24, Centro, CorBaleia);
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

            case "chao": return DeArte(ArteChao, Centro);
            case "chao_topo": return DeArte(ArteChaoTopo, Centro);
            // variações, para o chão não ficar repetitivo
            case "chao_rachado": return DeArte(Variar(ArteChao, 11), Centro);
            case "chao_topo_florido": return DeArte(Florir(ArteChaoTopo), Centro);

            // enfeites do cenário (sem colisão)
            case "arbusto": return Procedural(24, 12, Base, CorArbusto);
            case "pedra": return Procedural(12, 8, Base, CorPedra);
            case "cerca": return Procedural(16, 14, Base, CorCerca);
            case "lampiao": return Procedural(10, 40, Base, CorLampiao);
            case "cogumelo": return Procedural(10, 9, Base, CorCogumelo);
            case "sombra_oval": return Procedural(16, 6, Centro, CorSombraOval);

            // céu e fundo
            case "sol": return Procedural(48, 48, Centro, CorSol);
            case "lua": return Procedural(40, 40, Centro, CorLua);
            case "raio_de_luz": return Procedural(32, 128, new Vector2(0.5f, 1f), CorRaioDeLuz);
            case "fundo_montanhas": return Procedural(256, 96, Base, CorMontanhas);
            case "neblina": return Procedural(1, 32, Base, CorNeblina);
            case "frente_folhas": return Procedural(128, 24, Base, CorFrente);
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
                case "agua": return Procedural(16, 16, Centro, (x, y) => CorAgua(x, y, quadro));
                case "bloco_surpresa_brilho": return DeArte(ArteBlocoSurpresa, Centro, PixelsPorUnidade, quadro * 5f);
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

    // ---------------------------------------------------------------- variações do chão

    // Rachaduras: troca alguns pixels da pedra pelo rejunte, num desenho que depende da semente.
    static string[] Variar(string[] arte, int semente)
    {
        var nova = (string[])arte.Clone();
        for (int y = 2; y < nova.Length - 2; y++)
        {
            char[] linha = nova[y].ToCharArray();
            for (int x = 1; x < linha.Length - 1; x++)
                if (linha[x] == 'm' && Ruido(x + semente, y * 3) % 17 == 0) linha[x] = 'z';
            nova[y] = new string(linha);
        }
        return nova;
    }

    // Florzinhas na grama do topo do chão.
    static string[] Florir(string[] arte)
    {
        var nova = (string[])arte.Clone();
        char[] linha = nova[1].ToCharArray();
        linha[3] = 'p'; linha[11] = 'y';
        nova[1] = new string(linha);
        linha = nova[2].ToCharArray();
        linha[3] = 'G'; linha[11] = 'G';
        nova[2] = new string(linha);
        return nova;
    }

    // ---------------------------------------------------------------- enfeites

    static bool Bolinha(int x, int y, float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;

    static Color32 CorArbusto(int x, int y)
    {
        bool Dentro(int a, int b) => b >= 0 && (Bolinha(a, b, 6f, 4f, 5f) || Bolinha(a, b, 12f, 5.5f, 6f) || Bolinha(a, b, 18f, 4f, 5f));
        if (!Dentro(x, y)) return Transparente;
        if (!Dentro(x + 1, y) || !Dentro(x - 1, y) || !Dentro(x, y + 1)) return Cor('q');
        if (Bolinha(x, y, 10f, 9f, 2f) || Bolinha(x, y, 5f, 6.5f, 1.5f)) return Cor('a');
        return y < 4 ? Cor('G') : Cor('g');
    }

    static Color32 CorPedra(int x, int y)
    {
        bool Dentro(int a, int b) => b >= 0 && ((a - 5.5f) * (a - 5.5f) / 30f + b * b / 42f <= 1f);
        if (!Dentro(x, y)) return Transparente;
        if (!Dentro(x + 1, y) || !Dentro(x - 1, y) || !Dentro(x, y + 1)) return Cor('z');
        if (x < 5 && y > 3) return Cor('Z');
        return x > 7 ? Cor('M') : Cor('m');
    }

    static Color32 CorCerca(int x, int y)
    {
        bool poste = (x >= 2 && x <= 4) || (x >= 11 && x <= 13);
        bool ponta = poste && y >= 12 && Mathf.Abs(x - (x < 8 ? 3f : 12f)) <= 13 - y;
        bool trilho = (y >= 4 && y <= 5) || (y >= 9 && y <= 10);
        if (poste && (y < 12 || ponta)) return x == 2 || x == 4 || x == 11 || x == 13 ? Cor('d') : Cor('n');
        if (trilho) return y == 4 || y == 9 ? Cor('d') : Cor('b');
        return Transparente;
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

    static Color32 CorCogumelo(int x, int y)
    {
        bool chapeu = y >= 4 && Bolinha(x, y, 4.5f, 4f, 4.6f);
        if (chapeu)
        {
            if (!Bolinha(x, y, 4.5f, 4f, 3.7f) || y == 4) return Cor('k');
            return Bolinha(x, y, 3f, 6f, 0.8f) || Bolinha(x, y, 6.5f, 5.5f, 0.8f) ? Cor('w') : Cor('r');
        }
        if (y < 4 && x >= 3 && x <= 6) return x == 3 || x == 6 ? Cor('k') : Cor('n');
        return Transparente;
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
        return new Color32(255, 250, 225, (byte)(lado * lado * altura * 90f));
    }

    // Montanhas lá longe (camada mais distante), com neve nos picos mais altos. Repete a cada 256 px.
    static Color32 CorMontanhas(int x, int y)
    {
        float t = 2f * Mathf.PI * x / 256f;
        float altura = 34f + 18f * Mathf.Sin(t) + 10f * Mathf.Sin(3f * t + 1f) + 5f * Mathf.Sin(7f * t + 2f);
        if (y >= altura) return Transparente;
        if (altura > 52f && y > altura - 5f) return new Color32(255, 255, 255, 255); // neve
        return Detalhe;
    }

    // Neblina perto do chão: branca, opaca embaixo e sumindo para cima.
    static Color32 CorNeblina(int x, int y)
    {
        float a = Mathf.Pow(1f - y / 31f, 1.6f);
        return new Color32(255, 255, 255, (byte)(a * 255f));
    }

    // Folhagem bem na frente da câmera (silhueta): arbustos e folhas de capim. Repete a cada 128 px.
    static Color32 CorFrente(int x, int y)
    {
        float t = 2f * Mathf.PI * x / 128f;
        float moita = 9f + 5f * Mathf.Abs(Mathf.Sin(t * 2f)) + 3f * Mathf.Sin(t * 5f + 1f);
        bool folha = (x % 5 == 2 || x % 7 == 4) && y < moita + 4f + Ruido(x, 1) % 6;
        return y < moita || folha ? Cheio : Transparente;
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

    // Névoa (o miasma da Bruxa) que esconde o que você ainda não descobriu.
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

    // Porta da Biblioteca Proibida: madeira com moldura lilás e topo em arco. Aberta, mostra a escuridão mágica.
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
