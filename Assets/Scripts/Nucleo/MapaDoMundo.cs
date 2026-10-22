using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// O mapa do mundo (estilo Super Mario World): cada ponto é uma fase, ligados por caminhos.
// O Grumete anda de ponto em ponto; em cada ponto fica o "personagem" daquela fase.
//
// O mapa começa coberto pela NÉVOA (a névoa do alto-mar): você só enxerga em volta das fases
// que já liberou. Ninguém sabe onde fica a última fase até chegar lá. Quando passa de fase,
// a névoa vai sumindo ao longo do caminho novo e o ponto da próxima fase aparece.
// Quando a última fase (o covil da Baleia Branca) aparece, a névoa do mapa INTEIRO vai embora.
//
// Cores dos pontos: verde = já passou nesta partida, amarelo = liberada.
// Embaixo de cada ponto, uma caveira com quantas vezes você morreu nela nesta partida.
// Tem também uma fase SECRETA numa ilhota da lagoa (aparece quando você pega todas as moedas da fase 5).
// Quem lê as teclas é o GerenciadorDoJogo; aqui só tem o desenho, a névoa e a caminhada.
//
// A ARTE (visual v3, o mesmo acabamento das fases) fica em outros arquivos:
//  - ArteDoMapa: o chão inteiro numa imagem só (grama, praias, mar fundo, abismo), o caminho e as pontes;
//  - ArteDoMapaEnfeites: palmeiras, casas, castelo, forte, navios, rochedos... e as sombras deles no chão;
//  - NevoaDoMapa: a névoa macia que respira; VidaDoMapa: água, fumaça, bandeiras, bichinhos e folhas;
//  - Vento: palmeiras, flores e capim balançando.
public class MapaDoMundo : MonoBehaviour
{
    // O mapa é desenhado em texto, igual às fases. Cada caractere é um bloco.
    //  .  chão (grama)     +  caminho           =  ponte de madeira (caminho por cima do mar)
    //  ~  mar              T  palmeira          ^  rochedo
    //  h  casa do porto    f  flores            M  o forte da Capitã (a base do desenho fica aqui)
    //  K  o castelo do porto
    //  a  abismo (o fundo do desfiladeiro)      #  ponte de corda (caminho por cima do abismo)
    //  o  pedra            R  naufrágio         *  baú do tesouro
    //  p  palmeira morta (das ilhas do Kraken)  L  o navio fantasma das Cabines Assombradas
    //  Y  a Rocha da Caveira (perto do covil da Baleia Branca)
    //  n  um navio pirata navegando (fica no mar)                l  o farol do porto
    //  c  céu (o horizonte lá em cima, com as camadas de paralaxe; a névoa não cobre)
    //  1 a 9, A a G  as fases (A = 10, B = 11 ... G = 16). O caminho de '+', '=' e '#' tem que ligar
    //         o 1 ao 2, o 2 ao 3, e assim por diante (o 9 ao A, o A ao B...).
    //         Cuidado: dois caminhos encostados viram um atalho (a busca anda nas 4 direções).
    //  S  a fase secreta (ligada por caminho ao ponto da Fases.FaseDoSegredo)
    //
    // A viagem é de ilha em ilha, da esquerda para a direita (as pontes de madeira atravessam os estreitos):
    //   a Ilha do Porto, com o castelo e o farol (1); a Ilha da Selva, cheia de palmeiras (2, 3 e 4);
    //   a Ilha da Lagoa, com a ilhota secreta (5), o forte da Capitã (6) e a vila (7);
    //   a Ilha do Desfiladeiro (8), com a mesa de pedra no meio do abismo (9) e a beirada do vento (10),
    //   que continua nos rochedos do navio fantasma das Cabines Assombradas (11) e dos baús de tesouro (12);
    //   as ilhas mortas do Kraken (13, 14 e 15) e, lá longe, no mar escuro, o covil da Baleia Branca (16).
    // As 3 primeiras linhas são o céu. As 4 últimas ficam atrás da faixa escura da interface:
    // lá embaixo é só enfeite (nenhum ponto): o mar aberto, com navios passando.
    static readonly string[] Desenho =
    {
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "~~........~~~~~~~TT...TTTTT~~~~...~~~....T~~T.T~~n~...........~~~~..aaaaaaa..~~^.^~~~~^...~~.p.p.~~~~.pp^^.p..^~",
            "h.........~~~~.....TTT.TT..TT^....~~...f.f.T.....Tf..........R.~~..aaaaaaaaa...^....^.^^^^~~.......R...^.p.p^p..",
            "..........~~~.T....T.TTTT.TTT++4++~~...T..Tf.T.T..T............~~.aaaaaoaaaaa.^^^.L..^^^^.~~.....p.p^....p......",
            ".....K....~~~+++2+++.TTTTT.TT+...+~~......f......f............+==8+###+..aaa.o^^^......^^^~~.D+++p.......p......",
            ".h.....h.h~~~+T....+..T..TT..+...+~~++++++++++++++f....M......+~~..aaa+.oaaa..^o++B++^....~~.+..+.....F+++......",
            "..........~~~+TT...+.T....T..+.^^+~~+...f.....f..+.f.......h..+~~..aa.+..aaao.^.+...+.^^.^~~.+p.+.p...+..+......",
            "h...1+++++===+.T.T.+TTTT.T++++..^+==5...~~~~~....++++++6++....+~~oRaaa+..aa...^o+.^^+....+==++.p+pRpp.+.p+..Y..~",
            "..........~~~..TT.T+T....T+TT....^~~+.~~~~~~~~~~..fff....+.hfh+~~..oaa+9+aaa...o+...+....+~~.p.p++..+++pp+..~~~~",
            ".h.h..h.h.~~~.TT..T+++....+.T....^~~+~~~~~..~~~~~f.ffff..+....+~~R.aaa..+aaao...+^^.+^...+~~.pRp.+..+ppR.+~~~~~~",
            "....f.....~~~~TTTTTT.+....+TT^^.^.~~=====+S..~~~~...f.f..+f...+~~..aaa.o+aaa....+^^*+++C++~~.....+..+p..~=~~~~~~",
            "..h.h..h.h~~~~T..TTTT++3+++^T.....~~~~~~~~..~~~~~...T....+++7++~~.oaaaa.+###++A++....^....~~...p.E+++pp~~=~~~~~~",
            ".ff...f...~~~~.TTT.TTT....TTT^^...~~.~~~~~~~~~~~~.T..TTT..T....~~T.aaaaaaaaaa....^.^..^.^^~~p.p.....p.~~~=~~..~~",
            ".hl..h..h.~~~.TTTTTT..T.T.T.T....^~~..~~~~~~~~~~f...ff..TTTT..T~~Taaaaaaaaaa..^^^..^^..^^.~~R.p..p...~~~~===G..~",
            "~~~~~~~.~.~~~.~~~.~~~~~~TT.T..T^^~~~~~~~~~~~~~~f~~~~.~~~~~T.h.T~~T.aaaaaaaaa....*^^~~~~~~~~~~p...~~~~~~~~~~~..~~",
            "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~T~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
            "~~~~~~~n~~~~~~~~~~~~~.T~~~~n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~n~~~~~~~~~~~~~",
            "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~.T~~~~~~~n~~~~~~~~~~~~~~~~~~~~~~~~~~~",
            "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~",
    };

    // O "personagem" de cada fase, que fica do lado do ponto (o primeiro é o da fase 1).
    static readonly string[] Personagens =
    {
        "louro", "inimigo", "serra", "nuvem_malvada", "rato", "ancora", "inimigo_espinhos", "esmagador",
        "plataforma", "vento", "fantasma", "bloco_ritmo", "faca", "mare_negra", "bandeira_falsa", "baleia",
    };

    // Os personagens têm tamanhos bem diferentes (o Louro tem 1 bloco; a Baleia, uns 6!).
    // Cada um encolhe até caber numa "caixinha" do lado do ponto (veja ArrumarPersonagem).
    const float EscalaDosPersonagens = 0.8f, LarguraMaxima = 2f, AlturaMaxima = 1.4f;

    public static int Largura => Desenho[0].Length;
    public static int Altura => Desenho.Length;
    // A cor "média" da grama (o fundo da câmera no mapa).
    public static readonly Color CorDoChao = new Color32(92, 146, 60, 255);

    // Regiões com o chão de outro jeito (números = colunas do Desenho): a ArteDoMapa pinta a terra seca
    // e a terra sombria a partir daqui, com a borda ondulada.
    const int InicioDoDesfiladeiro = 64, FimDoDesfiladeiro = 79; // terra seca em volta do abismo
    const int InicioDaSombra = 88;                                 // daqui para a direita: a terra morta da região sombria
    static readonly Color TomDaSombra = new Color(0.62f, 0.55f, 0.78f); // os enfeites da região sombria ficam mais escuros

    static readonly Color CorConcluida = new Color(0.45f, 0.95f, 0.45f);
    static readonly Color CorLiberada = new Color(1f, 0.85f, 0.25f);
    static readonly Color CorSecreta = new Color(0.85f, 0.6f, 1f);
    static readonly Color32 Branco = new Color32(255, 255, 255, 255), Preto = new Color32(20, 20, 28, 255);

    const float RaioDoPonto = 4f;     // quanto a névoa abre em volta de uma fase liberada
    const float RaioDoCaminho = 2f;   // e em volta do caminho
    const float RaioDoPorto = 6f;   // o porto (fase 1) o Grumete já conhece: abre mais
    const float VelocidadeDaOnda = 70f; // blocos por segundo da "onda" que leva a névoa embora no fim
    const int OrdemDaNevoa = 30;
    // O chão (água, ponte, terra) fica ATRÁS das sombras (Sombra.Ordem = -9), para tudo projetar sombra nele.
    // As sombras dos enfeites (árvores, casas...) ficam embaixo de TODOS os enfeites: uma sombra nunca cobre a árvore da frente.
    const int OrdemDoChao = -16, OrdemDaAgua = -14, OrdemDoCaminho = -13, OrdemDasSombras = -11;

    public float velocidade = 7f; // blocos por segundo andando no mapa

    public Transform Grumete { get; private set; }
    public int Selecionado { get; private set; }
    public bool Andando { get; private set; }

    int liberado;    // última fase liberada
    int concluidas;  // fases já passadas nesta partida
    readonly List<Vector2Int> pontos = new List<Vector2Int>();               // célula de cada fase
    readonly List<List<Vector2Int>> caminhos = new List<List<Vector2Int>>(); // caminhos[i]: do ponto i até o i+1
    readonly Dictionary<Vector2Int, SpriteRenderer> terra = new Dictionary<Vector2Int, SpriteRenderer>(); // desenho de cada célula de caminho (terra, ponte ou a praça de uma fase)
    NevoaDoMapa nevoa;  // null = sem névoa (o mapa todo descoberto)
    VidaDoMapa vida;    // quem anima a água, a fumaça, as bandeiras, os bichinhos...
    SpriteRenderer[] desenhosDosPontos;
    Transform[] personagens;
    Vector3[] ladoDoPersonagem; // onde cada personagem fica, em relação ao ponto dele
    SpriteRenderer desenhoDoGrumete;
    float timerPasso;
    int nascendo = -1; // ponto que está aparecendo agora (animação própria)

    // Fase secreta
    bool secretaLiberada;
    Vector2Int? pontoSecreto;
    List<Vector2Int> caminhoSecreto;
    SpriteRenderer desenhoSecreto;
    Transform personagemSecreto;

    // no: onde o Grumete começa (pode ser Fases.IndiceSecreto). mortes: mortes por fase (a última é a da secreta).
    // andarPara: se >= 0, ele anda sozinho até lá (acabou de passar de fase).
    // caminhoNovo: o caminho até "andarPara" acabou de ser liberado (a névoa some aos pouquinhos).
    // mostrarSecreta: a fase secreta acabou de ser liberada (aparece com animação).
    public static MapaDoMundo Criar(Transform raiz, int no, int liberado, int concluidas, int[] mortes,
        bool secretaLiberada, int andarPara = -1, bool caminhoNovo = false, bool mostrarSecreta = false)
    {
        var mapa = new GameObject("MapaDoMundo").AddComponent<MapaDoMundo>();
        mapa.transform.SetParent(raiz, false);
        mapa.LerDesenho();
        int ultimo = mapa.pontos.Count - 1;
        mapa.liberado = Mathf.Clamp(liberado, 0, ultimo);
        mapa.concluidas = concluidas;
        mapa.secretaLiberada = secretaLiberada && mapa.pontoSecreto.HasValue;
        mapa.Selecionado = no == Fases.IndiceSecreto && mapa.secretaLiberada ? no : Mathf.Clamp(no, 0, ultimo);
        if (andarPara > ultimo) andarPara = -1;
        mapa.Montar(andarPara, caminhoNovo && andarPara >= 0, mostrarSecreta && mapa.secretaLiberada, mortes);
        return mapa;
    }

    // Posição no mundo do centro de uma célula (a linha 0 do texto é o topo).
    static Vector3 Mundo(Vector2Int celula) => new Vector3(celula.x, Altura - 1 - celula.y, 0f);

    static char Celula(Vector2Int c) =>
        c.y >= 0 && c.y < Altura && c.x >= 0 && c.x < Desenho[c.y].Length ? Desenho[c.y][c.x] : ' ';

    // Número da fase de um ponto do mapa: '1' a '9' = fases 1 a 9, 'A' a 'G' = fases 10 a 16. -1 = não é fase.
    static int IndiceDoPonto(char c)
    {
        if (c >= '1' && c <= '9') return c - '1';
        if (c >= 'A' && c <= 'G') return 9 + (c - 'A');
        return -1;
    }

    static bool EhPonto(char c) => IndiceDoPonto(c) >= 0 || c == 'S';
    static bool EhPonte(char c) => c == '=' || c == '#';
    static bool DaPraAndar(char c) => c == '+' || EhPonte(c) || EhPonto(c);

    // Acha os pontos das fases e o caminho entre cada par (busca em largura pelas células de caminho).
    void LerDesenho()
    {
        var achados = new SortedDictionary<int, Vector2Int>();
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < Desenho[y].Length; x++)
            {
                int indice = IndiceDoPonto(Desenho[y][x]);
                if (indice >= 0) achados[indice] = new Vector2Int(x, y);
                if (Desenho[y][x] == 'S') pontoSecreto = new Vector2Int(x, y);
            }
        foreach (var par in achados)
            if (par.Key == pontos.Count && pontos.Count < Fases.Todas.Length) pontos.Add(par.Value);

        for (int i = 0; i + 1 < pontos.Count; i++)
        {
            List<Vector2Int> caminho = AcharCaminho(pontos[i], pontos[i + 1]);
            if (caminho == null)
            {
                Debug.LogWarning($"MapaDoMundo: não achei caminho entre as fases {i + 1} e {i + 2}.");
                caminho = new List<Vector2Int> { pontos[i + 1] };
            }
            caminhos.Add(caminho);
        }

        if (pontoSecreto.HasValue && Fases.FaseDoSegredo < pontos.Count)
        {
            caminhoSecreto = AcharCaminho(pontos[Fases.FaseDoSegredo], pontoSecreto.Value);
            if (caminhoSecreto == null) pontoSecreto = null; // sem caminho, sem fase secreta no mapa
        }
        else pontoSecreto = null; // (a fase do segredo ainda não existe)
    }

    static readonly Vector2Int[] Direcoes = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

    // Devolve as células do caminho, sem a de saída e com a de chegada.
    static List<Vector2Int> AcharCaminho(Vector2Int de, Vector2Int ate)
    {
        var veioDe = new Dictionary<Vector2Int, Vector2Int>();
        var fila = new Queue<Vector2Int>();
        fila.Enqueue(de);
        veioDe[de] = de;
        while (fila.Count > 0)
        {
            Vector2Int atual = fila.Dequeue();
            if (atual == ate) break;
            foreach (Vector2Int d in Direcoes)
            {
                Vector2Int vizinha = atual + d;
                if (veioDe.ContainsKey(vizinha) || !DaPraAndar(Celula(vizinha))) continue;
                if (EhPonto(Celula(vizinha)) && vizinha != ate) continue; // não atravessa outras fases
                veioDe[vizinha] = atual;
                fila.Enqueue(vizinha);
            }
        }
        if (!veioDe.ContainsKey(ate)) return null;

        var caminho = new List<Vector2Int>();
        for (Vector2Int c = ate; c != de; c = veioDe[c]) caminho.Add(c);
        caminho.Reverse();
        return caminho;
    }

    // ------------------------------------------------------------------ montagem

    void Montar(int andarPara, bool caminhoNovo, bool mostrarSecreta, int[] mortes)
    {
        // O chão inteiro (uma imagem só, pintada uma vez e guardada) e as ondinhas da água por cima dele.
        vida = VidaDoMapa.Criar(transform, Largura, Altura, Altura - LinhasDeCeu() - 0.5f, InicioDaSombra);
        var terreno = ArteDoMapa.PegarTerreno(Desenho, new ArteDoMapa.Regioes
        {
            inicioDoDesfiladeiro = InicioDoDesfiladeiro, fimDoDesfiladeiro = FimDoDesfiladeiro, inicioDaSombra = InicioDaSombra,
        });
        NovoDesenho("Chao", new Vector3(-0.5f, -0.5f, 0f), terreno.chao, OrdemDoChao); // (o pivô da imagem é o canto de baixo-esquerda)
        for (int i = 0; i < terreno.celulasDaAgua.Length; i++)
            vida.Agua(NovoDesenho("Agua", Mundo(terreno.celulasDaAgua[i]), terreno.quadrosDaAgua[i][0], OrdemDaAgua), terreno.quadrosDaAgua[i]);

        var sorteio = new System.Random(2024);
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < Desenho[y].Length; x++)
                MontarCelula(new Vector2Int(x, y), sorteio);

        // Pontos, números e personagens.
        int n = pontos.Count;
        desenhosDosPontos = new SpriteRenderer[n];
        personagens = new Transform[n];
        ladoDoPersonagem = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            desenhosDosPontos[i] = ConstrutorDeFase.Visual("Ponto " + (i + 1), transform, Mundo(pontos[i]), "no_mapa", 2).GetComponent<SpriteRenderer>();
            var numero = new GameObject("Numero");
            numero.transform.SetParent(desenhosDosPontos[i].transform, false);
            var desenhoNumero = numero.AddComponent<SpriteRenderer>();
            desenhoNumero.sprite = FabricaDeSprites.SpriteDeTexto((i + 1).ToString(), new Color32(20, 20, 28, 255), new Color32(255, 255, 255, 0));
            desenhoNumero.sortingOrder = 3;

            var personagem = ConstrutorDeFase.Visual("Personagem", transform, Mundo(pontos[i]), DesenhoDoPersonagem(i), 9);
            personagens[i] = personagem.transform;
            ladoDoPersonagem[i] = ArrumarPersonagem(i, personagem.GetComponent<SpriteRenderer>());
            personagens[i].localPosition = LugarDoPersonagem(i, 0f);

            if (i <= liberado && mortes != null && i < mortes.Length) Caveira(pontos[i], mortes[i]);
            if (i <= liberado) MostrarMedalhas(pontos[i], i);
        }

        // A fase secreta: um ponto lilás com "?" e uma moeda girando em cima.
        if (pontoSecreto.HasValue)
        {
            desenhoSecreto = ConstrutorDeFase.Visual("PontoSecreto", transform, Mundo(pontoSecreto.Value), "no_mapa", 2).GetComponent<SpriteRenderer>();
            desenhoSecreto.color = CorSecreta;
            var numero = new GameObject("Numero");
            numero.transform.SetParent(desenhoSecreto.transform, false);
            var desenhoNumero = numero.AddComponent<SpriteRenderer>();
            desenhoNumero.sprite = FabricaDeSprites.SpriteDeTexto("?", Preto, new Color32(255, 255, 255, 0));
            desenhoNumero.sortingOrder = 3;
            personagemSecreto = ConstrutorDeFase.Visual("Moeda", transform, Mundo(pontoSecreto.Value) + new Vector3(0.95f, 0.75f, 0f), "moeda", 9).transform;
            bool aparece = secretaLiberada && !mostrarSecreta;
            desenhoSecreto.transform.localScale = aparece ? Vector3.one : Vector3.zero;
            personagemSecreto.gameObject.SetActive(aparece);
            if (secretaLiberada && mortes != null && mortes.Length > pontos.Count) Caveira(pontoSecreto.Value, mortes[pontos.Count]);
            if (aparece) MostrarMedalhas(pontoSecreto.Value, Fases.IndiceSecreto);
        }

        // O que já foi descoberto: em volta das fases liberadas e dos caminhos entre elas.
        // (O caminho novo e a fase nova ficam escondidos: vão aparecer com animação.)
        var descoberto = new bool[Largura, Altura];
        bool tudoDescoberto = liberado == n - 1 && !(caminhoNovo && andarPara == n - 1); // chegou ao fim: sem névoa
        for (int i = 0; i <= liberado && !tudoDescoberto; i++)
        {
            if (caminhoNovo && i == andarPara) continue;
            Descobrir(descoberto, pontos[i], i == 0 ? RaioDoPorto : RaioDoPonto);
            if (i > 0)
                foreach (Vector2Int c in caminhos[i - 1]) Descobrir(descoberto, c, RaioDoCaminho);
        }
        if (secretaLiberada && !mostrarSecreta)
        {
            Descobrir(descoberto, pontoSecreto.Value, RaioDoPonto - 1f);
            foreach (Vector2Int c in caminhoSecreto) Descobrir(descoberto, c, RaioDoCaminho);
        }
        if (!tudoDescoberto) nevoa = NevoaDoMapa.Criar(transform, descoberto, LinhasDeCeu(), InicioDaSombra, OrdemDaNevoa);

        if (caminhoNovo)
        {
            foreach (Vector2Int c in caminhos[andarPara - 1])
                if (terra.TryGetValue(c, out SpriteRenderer t)) t.color = CorDoCaminho(c) * new Color(1f, 1f, 1f, 0f);
            nascendo = andarPara;
            desenhosDosPontos[andarPara].transform.localScale = Vector3.zero;
        }

        DecorarCeu();
        vida.SombrasDeNuvens(Mathf.Max(4, Largura / 14), Altura - LinhasDeCeu() - 3f); // passando devagar pelo chão, com o vento

        // O Grumete.
        desenhoDoGrumete = ConstrutorDeFase.Visual("Grumete", transform, PosicaoNoPonto(Selecionado), "jogador", 10).GetComponent<SpriteRenderer>();
        Grumete = desenhoDoGrumete.transform;

        if (andarPara >= 0 || mostrarSecreta) StartCoroutine(LiberarEAndar(andarPara, caminhoNovo, mostrarSecreta));
    }

    // As 3 medalhas da fase (relógio, coração e moeda: veja Medalhas), em coluna à esquerda do ponto.
    // Só aparecem depois que você passa da fase uma vez; as que faltam ficam apagadinhas.
    void MostrarMedalhas(Vector2Int ponto, int fase)
    {
        if (!Progresso.RecordeDe(fase).existe) return;
        Medalhas.Conjunto tem = Medalhas.DaFase(fase);
        for (int k = 0; k < Medalhas.Ordem.Length; k++)
        {
            Medalhas.Conjunto medalha = Medalhas.Ordem[k];
            Vector3 lugar = Mundo(ponto) + new Vector3(-0.95f, 0.45f - k * 0.45f, 0f);
            var desenho = ConstrutorDeFase.Visual("Medalha", transform, lugar, Medalhas.Desenho(medalha), 5, false).GetComponent<SpriteRenderer>();
            desenho.transform.localScale = Vector3.one * 0.75f; // 9 pixels: uns 0,4 bloco
            if ((tem & medalha) == 0) desenho.color = new Color(0.3f, 0.3f, 0.36f, 0.75f);
        }
    }

    // Caveirinha com o número de mortes, embaixo do ponto.
    void Caveira(Vector2Int ponto, int mortes)
    {
        if (mortes <= 0) return;
        Vector3 lugar = Mundo(ponto) + new Vector3(-0.3f, -0.8f, 0f);
        var caveira = ConstrutorDeFase.Visual("Caveira", transform, lugar, "caveira", 4);
        caveira.transform.localScale = Vector3.one * 1.3f;
        var numero = new GameObject("Mortes");
        numero.transform.SetParent(transform, false);
        numero.transform.position = lugar + new Vector3(0.5f + 0.15f * (mortes.ToString().Length - 1), 0f, 0f);
        var desenho = numero.AddComponent<SpriteRenderer>();
        desenho.sprite = FabricaDeSprites.SpriteDeTexto(mortes.ToString(), Branco, Preto);
        desenho.sortingOrder = 4;
        numero.transform.localScale = Vector3.one * 0.8f;
    }

    // Nome do desenho do personagem do ponto i. Se o desenho não existir (alguém mudou o nome dele),
    // o mapa não pode quebrar por causa disso: avisa no Console e usa uma moeda no lugar.
    static string DesenhoDoPersonagem(int i)
    {
        string nome = Personagens[i % Personagens.Length];
        try
        {
            FabricaDeSprites.Pegar(nome);
            return nome;
        }
        catch (System.ArgumentException)
        {
            Debug.LogWarning($"MapaDoMundo: não existe o desenho \"{nome}\" (personagem da fase {i + 1}).");
            return "moeda";
        }
    }

    // Deixa o personagem do ponto i de um tamanho bom e devolve onde ele fica (em relação ao ponto).
    //  - Quem tem o pivô na BASE do desenho (Sereia, Dóris, a bandeira) fica "em pé" no chão,
    //    à direita do ponto; os outros flutuam em cima e à direita.
    //  - Desenho grande encolhe até caber em LarguraMaxima x AlturaMaxima (a Baleia fica com uns 2 blocos).
    //  - Desenho largo vai mais para a direita, para não cobrir o ponto.
    Vector3 ArrumarPersonagem(int i, SpriteRenderer desenho)
    {
        Bounds caixa = desenho.sprite.bounds; // tamanho do desenho em blocos (com escala 1)
        bool voa = Personagens[i % Personagens.Length] == "baleia"; // a Baleia Branca "nada" no ar, mais alto
        bool emPe = !voa && caixa.center.y - caixa.extents.y > -0.01f; // o desenho começa no pivô: pivô na base
        float escala = emPe ? 1f : EscalaDosPersonagens;
        escala = Mathf.Min(escala, LarguraMaxima / caixa.size.x, AlturaMaxima / caixa.size.y);
        desenho.transform.localScale = Vector3.one * escala;

        // o "meio" do desenho pode não ser o pivô: desconta, para o meio ficar no lugar certo
        Vector3 meio = caixa.center * escala;
        float x = Mathf.Max(0.95f, 0.55f + caixa.extents.x * escala) - meio.x;
        if (emPe) return new Vector3(x, -0.3f, 0f);
        float y = voa ? 1.1f : 0.75f;
        return new Vector3(x, y - meio.y, 0f);
    }

    void MontarCelula(Vector2Int c, System.Random sorteio)
    {
        Vector3 p = Mundo(c);
        int linha = c.y; // quanto mais embaixo, mais "na frente"
        char tipo = Celula(c);
        if (tipo == 'c') return; // o céu é desenhado no DecorarCeu
        // (o chão, a água e o abismo já estão pintados na imagem do chão: aqui só vão os enfeites)

        float sombrio = Sombrio(c);
        Color tom = Color.Lerp(Color.white, TomDaSombra, sombrio); // cor dos enfeites (mais escura na região sombria)
        // um "tremidinho" de até 2 pixels: a floresta não fica enfileirada demais (sempre o mesmo em cada célula)
        Vector3 tremido = new Vector3((Variacao(c, 5, 7) - 2) / 16f, 0f, 0f);

        if (EhPonto(tipo))
        {
            // a "praça" de terra com pedrinhas embaixo do ponto da fase (aparece junto com o caminho)
            terra[c] = NovoDesenho("Praca", p, ArteDoMapa.NomeDoCaminho(LadosDoCaminho(c), 0, true), OrdemDoCaminho);
            terra[c].color = CorDoCaminho(c);
            return;
        }

        switch (tipo)
        {
            case '=':
            case '#':
                terra[c] = Ponte(c, p, tipo == '#');
                terra[c].color = CorDoCaminho(c);
                break;
            case 'a':
                if (sorteio.Next(14) == 0) // de vez em quando passa uma rajada de vento no abismo (a fase 10 é a do vento)
                {
                    var rajada = ConstrutorDeFase.Visual("Vento", transform, p, "mapa_rajada", OrdemDoCaminho + 1, false);
                    Animacao.Adicionar(rajada, Animacao.Tipo.Flutuar, 1.6f, 1.2f);
                }
                break;
            case '+':
                terra[c] = NovoDesenho("Caminho", p, ArteDoMapa.NomeDoCaminho(LadosDoCaminho(c), Variacao(c, 4, 3), false), OrdemDoCaminho);
                terra[c].color = CorDoCaminho(c);
                break;
            case 'T':
                string palmeira = "mapa_palmeira_" + Variacao(c, ArteDoMapa.VersoesDaPalmeira, 1);
                Planta("Palmeira", p + Vector3.down * 0.5f + tremido, palmeira, linha - 4, tom, 0.8f);
                SombraNoChao(p + tremido + new Vector3(0.28f, -0.42f, 0f), "mapa_sombra_m");
                break;
            case 'p':
                Planta("PalmeiraMorta", p + Vector3.down * 0.5f + tremido, "mapa_palmeira_morta", linha - 4, Color.white, 0.6f);
                SombraNoChao(p + tremido + new Vector3(0.22f, -0.44f, 0f), "mapa_sombra_p");
                break;
            case '^':
                Enfeite("Montanha", p + Vector3.down * 0.5f + tremido, "mapa_montanha_" + Variacao(c, ArteDoMapa.VersoesDaMontanha, 2), linha - 6, tom);
                SombraNoChao(p + tremido + new Vector3(0.45f, -0.4f, 0f), "mapa_sombra_m");
                break;
            case 'h':
                string casa = "mapa_casa_" + Variacao(c, ArteDoMapa.VersoesDaCasa, 4);
                Enfeite("Casa", p + Vector3.down * 0.45f, casa, linha - 6, tom);
                SombraNoChao(p + new Vector3(0.3f, -0.45f, 0f), "mapa_sombra_m");
                vida.Fumaca(p + Vector3.down * 0.45f + ArteDoMapa.Chamine); // fumaça saindo da chaminé
                break;
            case 'o':
                Enfeite("Pedra", p + Vector3.down * 0.4f, "pedra", linha - 6, tom);
                SombraNoChao(p + new Vector3(0.2f, -0.42f, 0f), "mapa_sombra_p");
                break;
            case 'R':
                Enfeite("Naufragio", p + Vector3.down * 0.45f, "mapa_naufragio", linha - 6, tom);
                SombraNoChao(p + new Vector3(0.25f, -0.45f, 0f), "mapa_sombra_p");
                break;
            case '*':
                Enfeite("Bau", p + Vector3.down * 0.45f, "mapa_bau", linha - 6, tom);
                SombraNoChao(p + new Vector3(0.2f, -0.45f, 0f), "mapa_sombra_p");
                Brilho(p + new Vector3(0f, 0.05f, 0f), 1.4f, new Color(1f, 0.85f, 0.35f, 0.55f), linha - 5); // o ouro brilhando
                break;
            case 'L':
                Enfeite("NavioFantasma", p + Vector3.down * 0.5f, "mapa_navio_fantasma", linha - 6, Color.white);
                SombraNoChao(p + new Vector3(0.35f, -0.5f, 0f), "mapa_sombra_g");
                Brilho(p + new Vector3(-0.6f, 0.4f, 0f), 1.6f, new Color(0.75f, 0.5f, 1f, 0.5f), linha - 5);  // as janelas da popa
                Brilho(p + new Vector3(0.1f, 1.0f, 0f), 2.4f, new Color(0.7f, 0.6f, 1f, 0.3f), linha - 5);    // a vela fantasma
                break;
            case 'Y':
                // a Rocha da Caveira: um rochedo enorme, com os olhos brilhando de roxo lá no fundo
                Enfeite("RochaDaCaveira", p + Vector3.down * 0.5f, "mapa_rocha_caveira", linha - 6, Color.Lerp(Color.white, tom, 0.6f));
                SombraNoChao(p + new Vector3(0.6f, -0.55f, 0f), "mapa_sombra_rocha");
                Brilho(p + new Vector3(-0.47f, 0.82f, 0f), 1.2f, new Color(0.75f, 0.5f, 1f, 0.5f), linha - 5);
                Brilho(p + new Vector3(0.47f, 0.82f, 0f), 1.2f, new Color(0.75f, 0.5f, 1f, 0.5f), linha - 5);
                break;
            case 'f':
                for (int i = 0; i < 2; i++)
                {
                    var lugar = p + new Vector3((float)sorteio.NextDouble() - 0.5f, (float)sorteio.NextDouble() * 0.5f - 0.5f, 0f);
                    Planta("Flores", lugar, "mapa_flores_" + Variacao(c, ArteDoMapa.VersoesDasFlores, 5 + i), -7, Color.white, 1f);
                }
                if (Variacao(c, 3, 6) == 0) // uma borboleta voando em volta de algumas moitas
                    vida.Bichinho(p, CoresDasBorboletas[Variacao(c, CoresDasBorboletas.Length, 8)]);
                break;
            case 'M':
                Palacio("FortePirata", p, "mapa_forte", linha, ArteDoMapa.TorresDoForte, new Color(0.22f, 0.2f, 0.26f), 0.85f); // bandeiras pretas
                break;
            case 'n':
                // um navio pirata navegando: vai e volta devagar no mar
                SpriteRenderer navio = Enfeite("Navio", p + Vector3.down * 0.25f, "mapa_navio", linha - 6, tom);
                Animacao.Adicionar(navio.gameObject, Animacao.Tipo.Flutuar, 0.35f, 0.3f);
                break;
            case 'l':
                Enfeite("Farol", p + Vector3.down * 0.45f, "mapa_farol", linha - 6, tom);
                SombraNoChao(p + new Vector3(0.25f, -0.45f, 0f), "mapa_sombra_p");
                Brilho(p + new Vector3(0f, -0.45f + 30f / 16f, 0f), 2.6f, new Color(1f, 0.85f, 0.45f, 0.6f), linha - 5); // a luz do farol
                break;
            case 'K':
                Palacio("CasteloReal", p, "mapa_castelo", linha, ArteDoMapa.TorresDoCastelo, new Color(1f, 0.35f, 0.35f), 1f);
                break;
            case '.':
                if (sombrio >= 0.75f && sorteio.Next(9) == 0)
                {
                    // olhos vermelhos piscando no escuro... alguém está olhando
                    var olhos = ConstrutorDeFase.Visual("Olhos", transform, p + new Vector3(0.1f, 0.15f, 0f), "mapa_olhos", -7, false);
                    Animacao.Adicionar(olhos, Animacao.Tipo.Piscar, 1.1f + (float)sorteio.NextDouble(), 1f);
                }
                else if (sombrio >= 0.75f && Variacao(c, 7, 9) == 0)
                    vida.Bichinho(p, null); // vaga-lume
                else if (sorteio.Next(8) == 0)
                    Planta("Tufo", p + Vector3.down * 0.3f, "tufo", -8, tom, 1f);
                break;
        }
    }

    static readonly Color[] CoresDasBorboletas =
    {
        new Color(1f, 0.95f, 0.6f), new Color(1f, 0.7f, 0.85f), new Color(0.7f, 0.85f, 1f), new Color(1f, 1f, 1f), new Color(1f, 0.65f, 0.35f),
    };

    // Um número de 0 a n-1 que depende só da célula (e da "semente"): a mesma célula sempre ganha a mesma versão.
    static int Variacao(Vector2Int c, int n, int semente) => Mathf.Min(n - 1, (int)(ArteDosBlocos.Sorteio(c.x, c.y, semente * 101) * n));

    // Quais lados do caminho continuam (os vizinhos onde dá para andar).
    static int LadosDoCaminho(Vector2Int c)
    {
        return (DaPraAndar(Celula(c + Vector2Int.right)) ? ArteDoMapa.Direita : 0)
             | (DaPraAndar(Celula(c + Vector2Int.left)) ? ArteDoMapa.Esquerda : 0)
             | (DaPraAndar(Celula(c + Vector2Int.down)) ? ArteDoMapa.Cima : 0)   // (linha de cima no texto = y menor)
             | (DaPraAndar(Celula(c + Vector2Int.up)) ? ArteDoMapa.Baixo : 0);
    }

    // A cor do caminho: na região sombria a terra fica um pouco mais escura.
    static Color CorDoCaminho(Vector2Int c) => Color.Lerp(Color.white, TomDaSombra, Sombrio(c) * 0.8f);

    // Um desenho simples (sem sombra), com o sprite pelo nome.
    SpriteRenderer NovoDesenho(string nome, Vector3 lugar, string sprite, int ordem) => NovoDesenho(nome, lugar, FabricaDeSprites.Pegar(sprite), ordem);

    SpriteRenderer NovoDesenho(string nome, Vector3 lugar, Sprite sprite, int ordem)
    {
        var objeto = new GameObject(nome);
        objeto.transform.SetParent(transform, false);
        objeto.transform.position = lugar;
        var desenho = objeto.AddComponent<SpriteRenderer>();
        desenho.sprite = sprite;
        desenho.sortingOrder = ordem;
        return desenho;
    }

    // Um enfeite parado (casa, montanha, pedra...). A sombra dele é outro desenho, no chão (SombraNoChao).
    SpriteRenderer Enfeite(string nome, Vector3 lugar, string sprite, int ordem, Color cor)
    {
        SpriteRenderer desenho = NovoDesenho(nome, lugar, sprite, ordem);
        desenho.color = cor;
        return desenho;
    }

    // Uma planta que balança no vento (Vento.cs troca as poses dela).
    void Planta(string nome, Vector3 lugar, string sprite, int ordem, Color cor, float rigidez)
    {
        SpriteRenderer desenho = Enfeite(nome, lugar, sprite, ordem, cor);
        Vento.Plantar(desenho, sprite, transform, rigidez: rigidez);
    }

    // A sombra de um enfeite: uma mancha azul-arroxeada no chão, para baixo e para a direita (o sol fica
    // no alto, à esquerda), desenhada embaixo de todos os enfeites.
    void SombraNoChao(Vector3 lugar, string sprite, float escala = 1f)
    {
        SpriteRenderer sombra = NovoDesenho("Sombra", lugar, sprite, OrdemDasSombras);
        sombra.transform.localScale = Vector3.one * escala;
    }

    // Um brilho mágico redondo que pulsa (cristais, a cúpula da casa assombrada). Se der, a luz "soma" com a tela.
    void Brilho(Vector3 lugar, float tamanho, Color cor, int ordem)
    {
        SpriteRenderer brilho = NovoDesenho("Brilho", lugar, "mapa_brilho", ordem);
        brilho.transform.localScale = Vector3.one * tamanho;
        brilho.color = cor;
        MateriaisHD2D.UsarLuzAditiva(brilho);
        vida.Pulsar(brilho, 1.6f, 0.35f);
    }

    // O castelo e o forte, com a sombra grande e as bandeirinhas tremulando nas torres.
    void Palacio(string nome, Vector3 p, string sprite, int linha, Vector3[] torres, Color corDaBandeira, float tamanhoDaSombra)
    {
        Vector3 pe = p + Vector3.down * 0.5f;
        NovoDesenho(nome, pe, sprite, linha - 6);
        SombraNoChao(pe + new Vector3(0.6f, 0.05f, 0f), "mapa_sombra_castelo", tamanhoDaSombra);
        var quadros = FabricaDeSprites.Quadros("mapa_bandeira", ArteDoMapa.QuadrosDaBandeira);
        foreach (Vector3 torre in torres)
        {
            SpriteRenderer bandeira = NovoDesenho("Bandeira", pe + torre, "mapa_bandeira_0", linha - 6);
            bandeira.color = corDaBandeira;
            vida.Animar(bandeira, quadros, 7f);
        }
    }

    // Ponte (de madeira na água, de corda no abismo). Se o caminho passa por ela de cima para baixo, ela fica "em pé".
    SpriteRenderer Ponte(Vector2Int c, Vector3 p, bool corda)
    {
        bool emPe = !DaPraAndar(Celula(c + Vector2Int.left)) && !DaPraAndar(Celula(c + Vector2Int.right));
        return NovoDesenho("Ponte", p, (corda ? "mapa_ponte_corda" : "mapa_ponte") + (emPe ? "_empe" : ""), OrdemDoCaminho);
    }

    // Quantas linhas de cima do desenho são céu.
    static int LinhasDeCeu()
    {
        int linhas = 0;
        while (linhas < Altura && Desenho[linhas][0] == 'c') linhas++;
        return linhas;
    }

    // -1, 0 ou 1 conforme a linha: a borda das regiões fica "serrilhada", e não uma linha reta.
    static int Serrilhado(int linha) => (linha * 7 + linha * linha) % 3 - 1;

    // Quanto a célula está dentro da região sombria: 0 = nada, 1 = totalmente (a borda vai escurecendo em 4 colunas).
    static float Sombrio(Vector2Int c) => Mathf.Clamp01((c.x + Serrilhado(c.y) - InicioDaSombra + 1) / 4f);

    // Marca como descoberto tudo que está a até "raio" blocos do centro.
    static void Descobrir(bool[,] descoberto, Vector2Int centro, float raio)
    {
        int r = Mathf.CeilToInt(raio);
        for (int y = Mathf.Max(0, centro.y - r); y <= Mathf.Min(Altura - 1, centro.y + r); y++)
            for (int x = Mathf.Max(0, centro.x - r); x <= Mathf.Min(Largura - 1, centro.x + r); x++)
                if (Vector2Int.Distance(new Vector2Int(x, y), centro) <= raio) descoberto[x, y] = true;
    }

    // A névoa em volta de "centro" vai embora (com animação: veja NevoaDoMapa).
    void Revelar(Vector2Int centro, float raio)
    {
        if (nevoa != null) nevoa.Revelar(centro, raio);
    }

    // No fim, a névoa do mapa INTEIRO vai embora numa onda que sai do covil da Baleia.
    IEnumerator RevelarTudo(Vector2Int centro)
    {
        for (float raio = RaioDoPonto; raio <= Largura + Altura; raio += VelocidadeDaOnda * Time.deltaTime)
        {
            Revelar(centro, raio);
            yield return null;
        }
    }

    // O horizonte lá em cima, igual ao fundo das fases: céu, as ilhas e os galeões ao longe com paralaxe
    // (andam mais devagar que o mapa quando a câmera rola), brilhinhos, a Baleia Branca e nuvens.
    void DecorarCeu()
    {
        int linhasDeCeu = 0;
        while (linhasDeCeu < Altura && Desenho[linhasDeCeu][0] == 'c') linhasDeCeu++;
        if (linhasDeCeu == 0) return;
        float horizonte = Altura - linhasDeCeu - 0.5f; // y onde o céu encontra o chão
        var corDoCeu = new Color32(130, 195, 240, 255);

        var ceu = ConstrutorDeFase.Visual("Ceu", transform, new Vector3(Largura / 2f, horizonte, 0f), "ceu", -26, false);
        ceu.transform.localScale = new Vector3((Largura + 60f) * FabricaDeSprites.PixelsPorUnidade, (linhasDeCeu + 1f) / 2f, 1f);

        Paralaxe.CriarHorizonte(transform, Largura, horizonte - 0.15f, 0.35f, corDoCeu, new Color32(48, 100, 160, 255)); // (a cor do mar)

        var sorteio = new System.Random(5);
        for (int i = 0; i < Largura / 4; i++)
        {
            var brilho = ConstrutorDeFase.Visual("Brilho", transform,
                new Vector3(sorteio.Next(0, Largura), horizonte + 1f + (float)sorteio.NextDouble() * (linhasDeCeu - 1f), 0f), "brilho", -25, false);
            Animacao.Adicionar(brilho, Animacao.Tipo.Piscar, 3f, 0.7f);
        }

        // a Baleia Branca nadando lá longe, no céu (pequenininha: uns 1,8 bloco de largura, seja qual for o desenho dela)
        var baleia = ConstrutorDeFase.Visual("Baleia", transform, new Vector3(Largura / 2f, horizonte + linhasDeCeu * 0.6f, 0f), "baleia", -23, false);
        baleia.transform.localScale = Vector3.one * (1.8f / FabricaDeSprites.Pegar("baleia").bounds.size.x);
        baleia.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.75f);
        Animacao.Adicionar(baleia, Animacao.Tipo.Flutuar, 0.05f, Largura / 2f - 4f, 0.6f);

        // nuvens do céu (uma a cada 9 colunas); as de cima da região sombria são cinzentas
        for (int i = 0; 3 + i * 9 < Largura; i++)
        {
            float x = 3f + i * 9f;
            var nuvem = ConstrutorDeFase.Visual("Nuvem", transform, new Vector3(x, horizonte + 1.2f + (i % 2) * 0.9f, 0f), "nuvem", -21, false);
            if (x >= InicioDaSombra - 2) nuvem.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.5f, 0.65f);
            Animacao.Adicionar(nuvem, Animacao.Tipo.Flutuar, 0.3f, 0.8f, 0.5f);
        }

        // em cima da região sombria o céu vai escurecendo (a Maré Negra do Kraken está perto)
        float inicio = InicioDaSombra - 8f;
        var ceuSombrio = ConstrutorDeFase.Visual("CeuSombrio", transform, new Vector3(inicio, horizonte - 0.2f, 0f), "mapa_ceu_sombrio", -20, false);
        Vector3 tamanho = FabricaDeSprites.Pegar("mapa_ceu_sombrio").bounds.size;
        ceuSombrio.transform.localScale = new Vector3((Largura + 30f - inicio) / tamanho.x, (linhasDeCeu + 1f) / tamanho.y, 1f);
    }

    Vector2Int CelulaDoPonto(int fase) => fase == Fases.IndiceSecreto ? pontoSecreto.Value : pontos[fase];

    // O Grumete fica com os pés no centro do ponto.
    Vector3 PosicaoNoPonto(int fase) => Mundo(CelulaDoPonto(fase)) + Vector3.up * 0.45f;

    Vector3 LugarDoPersonagem(int i, float pulo) => Mundo(pontos[i]) + ladoDoPersonagem[i] + Vector3.up * pulo;

    // ------------------------------------------------------------------ andar

    // direcao: +1 = próxima fase, -1 = anterior. Devolve false se não dá para ir (ainda escondida ou fim do caminho).
    public bool Mover(int direcao)
    {
        int destino = Selecionado + direcao;
        if (Andando || destino < 0 || destino > liberado || destino >= pontos.Count)
        {
            if (!Andando) GerenciadorDoJogo.Som("bloco", 0.4f);
            return false;
        }
        StartCoroutine(Andar(destino, Rota(destino)));
        return true;
    }

    // Do ponto da fase do segredo para a fase secreta (e de volta).
    public bool IrParaSecreta()
    {
        if (Andando || !secretaLiberada || Selecionado != Fases.FaseDoSegredo) return false;
        StartCoroutine(Andar(Fases.IndiceSecreto, caminhoSecreto));
        return true;
    }

    public bool VoltarDaSecreta()
    {
        if (Andando || Selecionado != Fases.IndiceSecreto) return false;
        var volta = new List<Vector2Int>(caminhoSecreto);
        volta.Reverse();
        volta.RemoveAt(0);
        volta.Add(pontos[Fases.FaseDoSegredo]);
        StartCoroutine(Andar(Fases.FaseDoSegredo, volta));
        return true;
    }

    // Lista de células por onde o Grumete passa do ponto atual até o vizinho "destino".
    List<Vector2Int> Rota(int destino)
    {
        if (destino == Selecionado + 1) return caminhos[Selecionado];
        var volta = new List<Vector2Int>(caminhos[destino]);
        volta.Reverse();
        volta.RemoveAt(0);         // tira o ponto de onde ele sai
        volta.Add(pontos[destino]); // e põe o de chegada
        return volta;
    }

    IEnumerator Andar(int destino, List<Vector2Int> rota)
    {
        Andando = true;
        GerenciadorDoJogo.Som("pulo", 0.3f);
        Vector3 pes = PosicaoNoPonto(Selecionado);
        foreach (Vector2Int celula in rota)
        {
            Vector3 alvo = Mundo(celula) + Vector3.up * 0.45f;
            if (Mathf.Abs(alvo.x - pes.x) > 0.01f) desenhoDoGrumete.flipX = alvo.x < pes.x;
            while (pes != alvo)
            {
                pes = Vector3.MoveTowards(pes, alvo, velocidade * Time.deltaTime);
                timerPasso += Time.deltaTime;
                // pulinhos enquanto anda, estilo mapa do Mario
                Grumete.position = pes + Vector3.up * Mathf.Abs(Mathf.Sin(timerPasso * 14f)) * 0.15f;
                desenhoDoGrumete.sprite = FabricaDeSprites.Pegar((int)(timerPasso * 10f) % 2 == 0 ? "jogador" : "jogador_andando");
                yield return null;
            }
            Efeitos.Poeira(transform, pes + Vector3.down * 0.4f, 1, 1f);
        }
        Grumete.position = pes;
        desenhoDoGrumete.sprite = FabricaDeSprites.Pegar("jogador");
        Selecionado = destino;
        Andando = false;
    }

    // Acabou de passar de fase: a névoa some ao longo do caminho novo, a fase nova aparece e o Grumete vai até lá.
    // Se a fase secreta acabou de ser liberada, ela aparece primeiro.
    IEnumerator LiberarEAndar(int destino, bool caminhoNovo, bool mostrarSecreta)
    {
        Andando = true;
        yield return new WaitForSeconds(0.7f); // espera a transição de tela

        if (mostrarSecreta)
        {
            foreach (Vector2Int c in caminhoSecreto)
            {
                Revelar(c, RaioDoCaminho);
                GerenciadorDoJogo.Som("moeda", 0.12f);
                yield return new WaitForSeconds(0.1f);
            }
            Revelar(pontoSecreto.Value, RaioDoPonto - 1f);
            GerenciadorDoJogo.Som("conquista");
            yield return Nascer(desenhoSecreto.transform);
            personagemSecreto.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.8f);
        }
        if (destino < 0)
        {
            Andando = false;
            yield break;
        }

        if (caminhoNovo)
        {
            foreach (Vector2Int c in caminhos[destino - 1])
            {
                if (terra.TryGetValue(c, out SpriteRenderer t)) t.color = CorDoCaminho(c);
                Revelar(c, RaioDoCaminho);
                if (c != pontos[destino]) GerenciadorDoJogo.Som("moeda", 0.12f);
                yield return new WaitForSeconds(0.07f);
            }

            // a fase nova "nasce" com um pulinho e a névoa em volta dela vai embora
            // (na última fase, a névoa do mapa INTEIRO vai embora e a Baleia Branca ruge)
            bool ultima = destino == pontos.Count - 1;
            if (ultima)
            {
                StartCoroutine(RevelarTudo(pontos[destino]));
                CameraSeguir.Tremer(0.1f, 0.6f);
                GerenciadorDoJogo.Som("rugido", 0.6f);
            }
            else Revelar(pontos[destino], RaioDoPonto);
            GerenciadorDoJogo.Som("mola", 0.6f);
            yield return Nascer(desenhosDosPontos[destino].transform);
            nascendo = -1;
            yield return new WaitForSeconds(0.3f);
        }

        yield return Andar(destino, Rota(destino));
    }

    // O ponto "nasce" com um pulinho e um pouco de poeira.
    IEnumerator Nascer(Transform ponto)
    {
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            ponto.localScale = Vector3.one * Mathf.Sin(t / 0.35f * Mathf.PI * 0.75f) / 0.7071f;
            yield return null;
        }
        ponto.localScale = Vector3.one;
        Efeitos.Poeira(transform, ponto.position, 8, 2.5f);
    }

    // ------------------------------------------------------------------ animação

    void Update()
    {
        for (int i = 0; i < desenhosDosPontos.Length; i++)
        {
            desenhosDosPontos[i].color = i < concluidas ? CorConcluida : CorLiberada;

            // personagem pulando de leve; o do ponto escolhido pula mais
            float altura = i == Selecionado && !Andando ? 0.18f : 0.06f;
            personagens[i].localPosition = LugarDoPersonagem(i, Mathf.Abs(Mathf.Sin(Time.time * 4f + i)) * altura);
            if (Personagens[i % Personagens.Length] == "serra") personagens[i].Rotate(0f, 0f, -180f * Time.deltaTime);

            // o ponto em que o Grumete está "pulsa" de leve
            if (i != nascendo)
                desenhosDosPontos[i].transform.localScale = Vector3.one * (i == Selecionado && !Andando ? 1f + 0.08f * Mathf.Sin(Time.time * 6f) : 1f);
        }

        // a fase secreta: a moeda gira em cima do ponto, e o ponto pulsa quando o Grumete está nele
        if (personagemSecreto != null && personagemSecreto.gameObject.activeSelf)
        {
            personagemSecreto.localScale = new Vector3(Mathf.Cos(Time.time * 3f), 1f, 1f);
            if (Selecionado == Fases.IndiceSecreto && !Andando)
                desenhoSecreto.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 6f));
            else desenhoSecreto.transform.localScale = Vector3.one;
        }

        // parado, o Grumete respira
        if (!Andando) Grumete.localScale = new Vector3(1f, 1f + 0.04f * Mathf.Sin(Time.time * 3f), 1f);
    }

    // ------------------------------------------------------------------ desenhos que só o mapa usa

    // Ficam registrados na FabricaDeSprites (como os das armadilhas), então Pegar("mapa_...") funciona.
    // (Os desenhos grandes do mapa, como o chão, as árvores e o castelo, ficam em ArteDoMapa e ArteDoMapaEnfeites.)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        Vector2 centro = FabricaDeSprites.Centro;
        FabricaDeSprites.Registrar("mapa_rajada", () => FabricaDeSprites.Procedural(16, 5, centro, CorDaRajada));
        FabricaDeSprites.Registrar("mapa_olhos", () => FabricaDeSprites.DeArte(ArteOlhos, centro));
        FabricaDeSprites.Registrar("mapa_ceu_sombrio", () => FabricaDeSprites.Procedural(128, 32, Vector2.zero, CorDoCeuSombrio));
    }

    // Olhos vermelhos (bravos) no escuro.
    static readonly string[] ArteOlhos =
    {
        "rr...rr",
        ".rr.rr.",
    };

    // Rajada de vento: um risco branco ondulado que vai ficando mais forte para a direita.
    static Color32 CorDaRajada(int x, int y)
    {
        int altura = Mathf.RoundToInt(2f + 1.5f * Mathf.Sin(x / 15f * Mathf.PI * 2f));
        return y == altura ? new Color32(255, 255, 255, (byte)(90 + x * 9)) : FabricaDeSprites.Transparente;
    }

    // Céu escuro da região sombria: roxo, transparente na ponta esquerda (para ir escurecendo aos poucos).
    static Color32 CorDoCeuSombrio(int x, int y)
    {
        float lado = Mathf.Clamp01(x / 20f);
        return new Color32(36, 20, 56, (byte)(lado * (110f + y * 3f)));
    }
}
