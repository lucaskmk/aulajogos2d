using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// O mapa do mundo (estilo Super Mario World): cada ponto é uma fase, ligados por caminhos.
// O Subaru anda de ponto em ponto; em cada ponto fica o "personagem" daquela fase.
//
// O mapa começa coberto pela NÉVOA (o miasma da Bruxa): você só enxerga em volta das fases
// que já liberou. Ninguém sabe onde fica a última fase até chegar lá. Quando passa de fase,
// a névoa vai sumindo ao longo do caminho novo e o ponto da próxima fase aparece.
// Quando a última fase (o covil da Baleia Branca) aparece, a névoa do mapa INTEIRO vai embora.
//
// Cores dos pontos: verde = já passou nesta partida, amarelo = liberada.
// Embaixo de cada ponto, uma caveira com quantas vezes você morreu nela nesta partida.
// Tem também uma fase SECRETA numa ilha do lago (aparece quando você pega todas as moedas da fase 5).
// Quem lê as teclas é o GerenciadorDoJogo; aqui só tem o desenho, a névoa e a caminhada.
public class MapaDoMundo : MonoBehaviour
{
    // O mapa é desenhado em texto, igual às fases. Cada caractere é um bloco.
    //  .  grama            +  caminho           =  ponte (caminho por cima da água)
    //  ~  água             T  árvore            ^  montanha
    //  h  casa             f  flores            M  mansão do Roswaal (a base do desenho fica aqui)
    //  K  castelo real da capital (o mesmo desenho da mansão, só que branco)
    //  a  abismo (o fundo do desfiladeiro)      #  ponte de corda (caminho por cima do abismo)
    //  o  pedra            R  ruína             *  cristal
    //  p  pinheiro sombrio (da região escura)   L  torre da Biblioteca Proibida
    //  Y  a Grande Árvore Flügel (perto do covil da Baleia Branca)
    //  c  céu (o horizonte lá em cima, com as camadas de paralaxe; a névoa não cobre)
    //  1 a 9, A a G  as fases (A = 10, B = 11 ... G = 16). O caminho de '+', '=' e '#' tem que ligar
    //         o 1 ao 2, o 2 ao 3, e assim por diante (o 9 ao A, o A ao B...).
    //         Cuidado: dois caminhos encostados viram um atalho (a busca anda nas 4 direções).
    //  S  a fase secreta (ligada por caminho ao ponto da Fases.FaseDoSegredo)
    //
    // A viagem, da esquerda para a direita:
    //   a capital com o castelo real (1), o rio, a floresta (2 e 3), os morros (4),
    //   o lago com a ilha secreta (5), o campo de flores e a mansão do Roswaal (6),
    //   a aldeia e a floresta dos mabeasts (7), as ruínas na beira do desfiladeiro (8),
    //   o abismo com a mesa de pedra no meio (9) e a beirada do vento (10),
    //   a Biblioteca Proibida (11) e as montanhas de cristal (12),
    //   a região sombria (13, 14 e 15) e, lá longe, no mar escuro, o covil da Baleia Branca (16).
    // As 3 primeiras linhas são o céu. As 4 últimas ficam atrás da faixa escura da interface:
    // lá embaixo é só enfeite (nenhum ponto).
    static readonly string[] Desenho =
    {
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
            "............~~.T.TT...TTTTT.......^.^....T.T..T...T.................aaaaaaa...^^.^^^^^^....^.p.p.^pp..pp^^.p..^^",
            "h...........~~.....TTT.TT..TT^.........f.f.T.....Tf..........R.R...aaaaaaaaa...^....^.^^^^^^.......R...^.p.p^p..",
            "...........~~.T....T.TTTT.TTT++4++^^...T..Tf.T.T..T...............aaaaaoaaaaa.^^^.L..^^^^........p.p^....p......",
            ".....K.....~~+++2+++.TTTTT.TT+...+^^......f......f............+++8+###+..aaa.o^^^......^^^^..D+++p.......p......",
            ".h.....h.h.~~+T....+..T..TT..+...+..++++++++++++++f....M......+....aaa+.oaaa..^o++B++^.....^.+..+.....F+++......",
            "..........~~.+TT...+.T....T..+.^^+^.+...f.....f..+.f.......h..+.R..aa.+..aaao.^.+...+.^^.^^^.+p.+.p...+..+......",
            "h...1+++++==++.T.T.+TTTT.T++++..^+++5...~~~~~....++++++6++....+..oRaaa+..aa...^o+.^^+....+++++.p+pRpp.+.p+..Y..~",
            "...........~~..TT.T+T....T+TT....^..+.~~~~~~~~~~..fff....+.hfh+....oaa+9+aaa...o+...+....+^^.p.p++..+++pp+..~~~~",
            ".h.h..h.h..~~.TT..T+++....+.T....^..+~~~~~..~~~~~f.ffff..+....+..R.aaa..+aaao...+^^.+^...+.p.pRp.+..+ppR.+~~~~~~",
            "....f.......~~TTTTTT.+....+TT^^.^...=====+S..~~~~...f.f..+f...+....aaa.o+aaa....+^^*+++C++^......+..+p..~=~~~~~~",
            "..h.h..h.h..~~T..TTTT++3+++^T.......~~~~~~..~~~~~...T....+++7++o..oaaaa.+###++A++....^....^....p.E+++pp~~=~~~~~~",
            ".ff...f.....~~.TTT.TTT....TTT^^....^.~~~~~~~~~~~~.T..TTT..T.....TT.aaaaaaaaaa....^.^..^.^^^.p.p.....p.~~~=~~..~~",
            ".h...h..h..~~.TTTTTT..T.T.T.T....^^...~~~~~~~~~~f...ff..TTTT..T.hTaaaaaaaaaa..^^^..^^..^^...R.p..p...~~~~===G..~",
            "..........T~~..TT.T..TT.TT.T..T^^^....f.~~~~~~~f.Tff...T..T.h.TT.T.aaaaaaaaa....*^^^.^.^^^.^pp....p.~~~~~~~...~~",
            ".T.h.Th...~~..TT.TT...TT.TTTTTT^.^^.fffff..f~~~f.f...T...T..TTTT.TTaaaaaaaaaa.^o^.^^^^.^^^.p^p...p.~~~~~~~~~~~~~",
            "..T.T..T.h~~..TTTTTTTT..TTTTT....^..f......f~~~fT..T.fTTTT..T...TT..aaaaaaaao...^*^^...^...^p....p.~~~~~~~~~~~~~",
            ".h..f......~~..TTTTTT..TTTTTT.^^...^.ff.....~~ff.f....f.T.T.TTTT.TT.aaaaaaa....^^^^^^.^.^^.^..p...~~~~~~~~~~~~~~",
            "..f..T.....~~.TT.....TTTT.T.^T.......f.f...f~~f.f.TT.T..TTT.TTTT..Taaaaaaaaa.o.*.^....^.^.^.^.p...~~~~~~~~~~~~~~",
    };

    // O "personagem" de cada fase, que fica do lado do ponto (o primeiro é o da fase 1).
    static readonly string[] Personagens =
    {
        "puck", "inimigo", "serra", "nuvem_malvada", "coelho", "mangual", "inimigo_espinhos", "esmagador",
        "plataforma", "vento", "beatrice", "bloco_ritmo", "faca", "miasma", "bandeira_falsa", "baleia",
    };

    // Os personagens têm tamanhos bem diferentes (o Puck tem 1 bloco; a Baleia, uns 6!).
    // Cada um encolhe até caber numa "caixinha" do lado do ponto (veja ArrumarPersonagem).
    const float EscalaDosPersonagens = 0.8f, LarguraMaxima = 2f, AlturaMaxima = 1.4f;

    public static int Largura => Desenho[0].Length;
    public static int Altura => Desenho.Length;
    public static readonly Color CorDoChao = new Color32(140, 205, 115, 255);

    // Regiões com o chão de outra cor (números = colunas do Desenho). A grama normal é só a cor
    // de fundo da câmera (CorDoChao); nas regiões, cada célula ganha um quadradinho de chão pintado.
    const int InicioDoDesfiladeiro = 64, FimDoDesfiladeiro = 79; // terra seca em volta do abismo
    const int InicioDaSombra = 88;                                 // daqui para a direita: a terra morta da região sombria
    static readonly Color CorDoDesfiladeiro = new Color32(206, 182, 132, 255);
    static readonly Color CorDaSombra = new Color32(84, 70, 104, 255);
    static readonly Color TomDaSombra = new Color(0.62f, 0.55f, 0.78f); // os enfeites da região sombria ficam mais escuros
    static readonly Color TomDoMarSombrio = new Color(0.42f, 0.45f, 0.68f); // e a água fica azul-marinho

    static readonly Color CorConcluida = new Color(0.45f, 0.95f, 0.45f);
    static readonly Color CorLiberada = new Color(1f, 0.85f, 0.25f);
    static readonly Color CorSecreta = new Color(0.85f, 0.6f, 1f);
    static readonly Color32 Branco = new Color32(255, 255, 255, 255), Preto = new Color32(20, 20, 28, 255);

    const float RaioDoPonto = 4f;     // quanto a névoa abre em volta de uma fase liberada
    const float RaioDoCaminho = 2f;   // e em volta do caminho
    const float RaioDaCapital = 6f;   // a capital (fase 1) o Subaru já conhece: abre mais
    const float VelocidadeDaOnda = 70f; // blocos por segundo da "onda" que leva a névoa embora no fim
    const int OrdemDaNevoa = 30;
    // O chão (água, ponte, terra) fica ATRÁS das sombras (Sombra.Ordem = -9), para tudo projetar sombra nele.
    const int OrdemDoChao = -16, OrdemDaAgua = -14, OrdemDoCaminho = -13;

    public float velocidade = 7f; // blocos por segundo andando no mapa

    public Transform Subaru { get; private set; }
    public int Selecionado { get; private set; }
    public bool Andando { get; private set; }

    int liberado;    // última fase liberada
    int concluidas;  // fases já passadas nesta partida
    readonly List<Vector2Int> pontos = new List<Vector2Int>();               // célula de cada fase
    readonly List<List<Vector2Int>> caminhos = new List<List<Vector2Int>>(); // caminhos[i]: do ponto i até o i+1
    readonly Dictionary<Vector2Int, SpriteRenderer> terra = new Dictionary<Vector2Int, SpriteRenderer>(); // desenho de cada célula de caminho (terra ou ponte)
    SpriteRenderer[,] nevoa;
    float[,] tamanhoDaNevoa;
    SpriteRenderer[] desenhosDosPontos;
    Transform[] personagens;
    Vector3[] ladoDoPersonagem; // onde cada personagem fica, em relação ao ponto dele
    SpriteRenderer desenhoSubaru;
    float timerPasso;
    int nascendo = -1; // ponto que está aparecendo agora (animação própria)

    // Fase secreta
    bool secretaLiberada;
    Vector2Int? pontoSecreto;
    List<Vector2Int> caminhoSecreto;
    SpriteRenderer desenhoSecreto;
    Transform personagemSecreto;

    // no: onde o Subaru começa (pode ser Fases.IndiceSecreto). mortes: mortes por fase (a última é a da secreta).
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
        }

        // O que já foi descoberto: em volta das fases liberadas e dos caminhos entre elas.
        // (O caminho novo e a fase nova ficam escondidos: vão aparecer com animação.)
        var descoberto = new bool[Largura, Altura];
        bool tudoDescoberto = liberado == n - 1 && !(caminhoNovo && andarPara == n - 1); // chegou ao fim: sem névoa
        for (int i = 0; i <= liberado && !tudoDescoberto; i++)
        {
            if (caminhoNovo && i == andarPara) continue;
            Descobrir(descoberto, pontos[i], i == 0 ? RaioDaCapital : RaioDoPonto);
            if (i > 0)
                foreach (Vector2Int c in caminhos[i - 1]) Descobrir(descoberto, c, RaioDoCaminho);
        }
        if (secretaLiberada && !mostrarSecreta)
        {
            Descobrir(descoberto, pontoSecreto.Value, RaioDoPonto - 1f);
            foreach (Vector2Int c in caminhoSecreto) Descobrir(descoberto, c, RaioDoCaminho);
        }
        if (!tudoDescoberto) CriarNevoa(descoberto);
        else
        {
            nevoa = new SpriteRenderer[Largura, Altura];
            tamanhoDaNevoa = new float[Largura, Altura];
        }

        if (caminhoNovo)
        {
            foreach (Vector2Int c in caminhos[andarPara - 1])
                if (terra.TryGetValue(c, out SpriteRenderer t)) t.color = new Color(1f, 1f, 1f, 0f);
            nascendo = andarPara;
            desenhosDosPontos[andarPara].transform.localScale = Vector3.zero;
        }

        DecorarCeu();
        SombrasDeNuvem();

        // O Subaru.
        desenhoSubaru = ConstrutorDeFase.Visual("Subaru", transform, PosicaoNoPonto(Selecionado), "jogador", 10).GetComponent<SpriteRenderer>();
        Subaru = desenhoSubaru.transform;

        if (andarPara >= 0 || mostrarSecreta) StartCoroutine(LiberarEAndar(andarPara, caminhoNovo, mostrarSecreta));
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
    //  - Quem tem o pivô na BASE do desenho (Emilia, Beatrice, a bandeira) fica "em pé" no chão,
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

        // O chão das regiões (embaixo de tudo). A água e o abismo já cobrem a célula inteira.
        float sombrio = Sombrio(c);
        if (tipo != '~' && tipo != '=' && tipo != 'a' && tipo != '#')
        {
            if (sombrio > 0f) Chao(p, Color.Lerp(CorDoChao, CorDaSombra, sombrio));
            else if (NoDesfiladeiro(c)) Chao(p, CorDoDesfiladeiro);
        }
        Color tom = Color.Lerp(Color.white, TomDaSombra, sombrio); // cor dos enfeites (mais escura na região sombria)

        switch (tipo)
        {
            case '~':
                Agua(p, sorteio, Color.Lerp(Color.white, TomDoMarSombrio, sombrio));
                break;
            case '=':
                Agua(p, sorteio, Color.Lerp(Color.white, TomDoMarSombrio, sombrio));
                terra[c] = Ponte(c, p);
                break;
            case '#':
                Abismo(c, p, sorteio);
                terra[c] = Ponte(c, p);
                break;
            case 'a':
                Abismo(c, p, sorteio);
                break;
            case '+':
                terra[c] = ConstrutorDeFase.Visual("Caminho", transform, p, "terra", OrdemDoCaminho, false).GetComponent<SpriteRenderer>();
                break;
            case 'T':
                var arvore = ConstrutorDeFase.Visual("Arvore", transform, p + Vector3.down * 0.5f, "arvore", linha - 4);
                arvore.GetComponent<SpriteRenderer>().color = tom;
                Animacao.Adicionar(arvore, Animacao.Tipo.Balancar, 1.5f, 2.5f); // balançando no vento
                break;
            case 'p':
                var pinheiro = ConstrutorDeFase.Visual("Pinheiro", transform, p + Vector3.down * 0.5f, "mapa_pinheiro", linha - 4);
                Animacao.Adicionar(pinheiro, Animacao.Tipo.Balancar, 0.8f, 1.5f);
                break;
            case '^':
                ConstrutorDeFase.Visual("Montanha", transform, p, "montanha", linha - 6).GetComponent<SpriteRenderer>().color = tom;
                break;
            case 'h':
                ConstrutorDeFase.Visual("Casa", transform, p, "casa", linha - 6).GetComponent<SpriteRenderer>().color = tom;
                break;
            case 'o':
                ConstrutorDeFase.Visual("Pedra", transform, p + Vector3.down * 0.4f, "pedra", linha - 6).GetComponent<SpriteRenderer>().color = tom;
                break;
            case 'R':
                ConstrutorDeFase.Visual("Ruina", transform, p + Vector3.down * 0.45f, "mapa_ruina", linha - 6).GetComponent<SpriteRenderer>().color = tom;
                break;
            case '*':
                ConstrutorDeFase.Visual("Cristal", transform, p + Vector3.down * 0.45f, "mapa_cristal", linha - 6).GetComponent<SpriteRenderer>().color = tom;
                if (sorteio.Next(3) == 0) // um brilhinho piscando em alguns cristais
                {
                    var brilho = ConstrutorDeFase.Visual("Brilho", transform, p + new Vector3(0.15f, 0.1f, 0f), "brilho", linha - 5, false);
                    brilho.transform.localScale = Vector3.one * 0.6f;
                    Animacao.Adicionar(brilho, Animacao.Tipo.Piscar, 2.5f, 1f);
                }
                break;
            case 'L':
                ConstrutorDeFase.Visual("Biblioteca", transform, p + Vector3.down * 0.5f, "mapa_biblioteca", linha - 6);
                break;
            case 'Y':
                // a Grande Árvore Flügel: a árvore normal, só que ENORME (e meio escurecida pela região)
                var flugel = ConstrutorDeFase.Visual("ArvoreFlugel", transform, p + Vector3.down * 0.5f, "arvore", linha - 4);
                flugel.transform.localScale = Vector3.one * 2.6f;
                flugel.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, tom, 0.6f);
                Animacao.Adicionar(flugel, Animacao.Tipo.Balancar, 0.7f, 1f);
                break;
            case 'f':
                for (int i = 0; i < 2; i++)
                {
                    var flor = ConstrutorDeFase.Visual("Flor", transform, p + new Vector3((float)sorteio.NextDouble() - 0.5f, (float)sorteio.NextDouble() * 0.5f - 0.5f, 0f), "flor", -7);
                    Animacao.Adicionar(flor, Animacao.Tipo.Balancar, 2.5f, 6f);
                }
                break;
            case 'M':
                Castelo("Mansao", p, linha, 0.28f, new Color(0.82f, 0.74f, 0.95f));
                break;
            case 'K':
                Castelo("CasteloReal", p, linha, 0.26f, new Color(0.9f, 0.94f, 1f));
                break;
            case '.':
                if (sombrio >= 0.75f && sorteio.Next(9) == 0)
                {
                    // olhos vermelhos piscando no escuro... alguém está olhando
                    var olhos = ConstrutorDeFase.Visual("Olhos", transform, p + new Vector3(0.1f, 0.15f, 0f), "mapa_olhos", -7, false);
                    Animacao.Adicionar(olhos, Animacao.Tipo.Piscar, 1.1f + (float)sorteio.NextDouble(), 1f);
                }
                else if (sorteio.Next(8) == 0)
                    ConstrutorDeFase.Visual("Tufo", transform, p + Vector3.down * 0.3f, "tufo", -8).GetComponent<SpriteRenderer>().color = tom;
                break;
        }
    }

    // Um quadradinho de chão pintado com a cor da região.
    void Chao(Vector3 p, Color cor)
    {
        ConstrutorDeFase.Visual("Chao", transform, p, "mapa_chao", OrdemDoChao, false).GetComponent<SpriteRenderer>().color = cor;
    }

    void Agua(Vector3 p, System.Random sorteio, Color tom)
    {
        var agua = ConstrutorDeFase.Visual("Agua", transform, p, "agua_0", OrdemDaAgua, false).GetComponent<SpriteRenderer>();
        agua.color = tom; // o mar da região sombria é bem mais escuro
        AnimacaoDeQuadros.Adicionar(agua, FabricaDeSprites.Quadros("agua", FabricaDeSprites.QuadrosDaAgua), 3f);
        if (sorteio.Next(6) == 0) // brilhinho do sol na água
        {
            var brilho = ConstrutorDeFase.Visual("Brilho", transform, p + new Vector3(0.2f, 0.2f, 0f), "brilho", OrdemDaAgua + 2, false);
            brilho.transform.localScale = Vector3.one * 0.6f;
            brilho.GetComponent<SpriteRenderer>().color = tom;
            Animacao.Adicionar(brilho, Animacao.Tipo.Piscar, 3f, 0.9f);
        }
    }

    // O fundo do desfiladeiro. Onde a célula de cima é chão, aparece a parede de pedra (o mapa é visto
    // "de cima e de frente", então a gente vê a parede do outro lado do buraco).
    // De vez em quando passa uma rajada de vento (a fase 10 é a do vento).
    void Abismo(Vector2Int c, Vector3 p, System.Random sorteio)
    {
        char deCima = Celula(c + Vector2Int.down); // (linha de cima no texto = y menor)
        string desenho = deCima == 'a' || deCima == '#' ? "mapa_abismo" : "mapa_abismo_borda";
        ConstrutorDeFase.Visual("Abismo", transform, p, desenho, OrdemDaAgua, false);
        if (sorteio.Next(14) == 0)
        {
            var rajada = ConstrutorDeFase.Visual("Vento", transform, p, "mapa_rajada", OrdemDoCaminho + 1, false);
            Animacao.Adicionar(rajada, Animacao.Tipo.Flutuar, 1.6f, 1.2f);
        }
    }

    // Ponte (de madeira na água, de corda no abismo). O desenho é deitado: se o caminho passa
    // por ela de cima para baixo, ela gira 90 graus.
    SpriteRenderer Ponte(Vector2Int c, Vector3 p)
    {
        var ponte = ConstrutorDeFase.Visual("Ponte", transform, p, "ponte", OrdemDoCaminho, false);
        bool emPe = !DaPraAndar(Celula(c + Vector2Int.left)) && !DaPraAndar(Celula(c + Vector2Int.right));
        if (emPe) ponte.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        return ponte.GetComponent<SpriteRenderer>();
    }

    // Mansão do Roswaal e castelo real: o castelo do fundo das fases, bem pequenininho e pintado.
    void Castelo(string nome, Vector3 p, int linha, float escala, Color cor)
    {
        var castelo = ConstrutorDeFase.Visual(nome, transform, p + Vector3.down * 0.5f, "fundo_castelo", linha - 6, false);
        castelo.transform.localScale = Vector3.one * escala;
        castelo.GetComponent<SpriteRenderer>().color = cor;
    }

    // -1, 0 ou 1 conforme a linha: a borda das regiões fica "serrilhada", e não uma linha reta.
    static int Serrilhado(int linha) => (linha * 7 + linha * linha) % 3 - 1;

    // Quanto a célula está dentro da região sombria: 0 = nada, 1 = totalmente (a borda vai escurecendo em 4 colunas).
    static float Sombrio(Vector2Int c) => Mathf.Clamp01((c.x + Serrilhado(c.y) - InicioDaSombra + 1) / 4f);

    static bool NoDesfiladeiro(Vector2Int c)
    {
        int x = c.x + Serrilhado(c.y);
        return x >= InicioDoDesfiladeiro && x <= FimDoDesfiladeiro;
    }

    // Marca como descoberto tudo que está a até "raio" blocos do centro.
    static void Descobrir(bool[,] descoberto, Vector2Int centro, float raio)
    {
        int r = Mathf.CeilToInt(raio);
        for (int y = Mathf.Max(0, centro.y - r); y <= Mathf.Min(Altura - 1, centro.y + r); y++)
            for (int x = Mathf.Max(0, centro.x - r); x <= Mathf.Min(Largura - 1, centro.x + r); x++)
                if (Vector2Int.Distance(new Vector2Int(x, y), centro) <= raio) descoberto[x, y] = true;
    }

    void CriarNevoa(bool[,] descoberto)
    {
        nevoa = new SpriteRenderer[Largura, Altura];
        tamanhoDaNevoa = new float[Largura, Altura];
        var sorteio = new System.Random(77);
        for (int y = 0; y < Altura; y++)
            for (int x = 0; x < Largura; x++)
            {
                if (descoberto[x, y] || Celula(new Vector2Int(x, y)) == 'c') continue;
                var bolota = ConstrutorDeFase.Visual("Nevoa", transform, Mundo(new Vector2Int(x, y)), "nevoa", OrdemDaNevoa, false);
                tamanhoDaNevoa[x, y] = 1.6f + (float)sorteio.NextDouble() * 0.4f;
                bolota.transform.localScale = Vector3.one * tamanhoDaNevoa[x, y];
                bolota.transform.rotation = Quaternion.Euler(0f, 0f, sorteio.Next(4) * 90f);
                nevoa[x, y] = bolota.GetComponent<SpriteRenderer>();
            }
    }

    // A névoa em volta de "centro" vai embora (com animação). Só olha o quadrado em volta do centro.
    void Revelar(Vector2Int centro, float raio)
    {
        int r = Mathf.CeilToInt(raio);
        for (int y = Mathf.Max(0, centro.y - r); y <= Mathf.Min(Altura - 1, centro.y + r); y++)
            for (int x = Mathf.Max(0, centro.x - r); x <= Mathf.Min(Largura - 1, centro.x + r); x++)
            {
                SpriteRenderer bolota = nevoa[x, y];
                if (bolota == null || Vector2Int.Distance(new Vector2Int(x, y), centro) > raio) continue;
                nevoa[x, y] = null;
                StartCoroutine(Sumir(bolota));
            }
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

    static IEnumerator Sumir(SpriteRenderer bolota)
    {
        Vector3 escala = bolota.transform.localScale;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float p = t / 0.5f;
            bolota.color = new Color(1f, 1f, 1f, 1f - p);
            bolota.transform.localScale = escala * (1f + p * 0.6f);
            yield return null;
        }
        Destroy(bolota.gameObject);
    }

    // O horizonte lá em cima, igual ao fundo das fases: céu, floresta e árvores ao longe com paralaxe
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

        Paralaxe.CriarHorizonte(transform, Largura, horizonte - 0.15f, 0.35f, corDoCeu, CorDoChao);

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

        // em cima da região sombria o céu vai escurecendo (o miasma da Bruxa está perto)
        float inicio = InicioDaSombra - 8f;
        var ceuSombrio = ConstrutorDeFase.Visual("CeuSombrio", transform, new Vector3(inicio, horizonte - 0.2f, 0f), "mapa_ceu_sombrio", -20, false);
        Vector3 tamanho = FabricaDeSprites.Pegar("mapa_ceu_sombrio").bounds.size;
        ceuSombrio.transform.localScale = new Vector3((Largura + 30f - inicio) / tamanho.x, (linhasDeCeu + 1f) / tamanho.y, 1f);
    }

    // Sombras de nuvem passando devagar pelo chão (escurecem tudo, até o Subaru).
    // Uma a cada 14 colunas, cada uma numa altura e numa velocidade.
    void SombrasDeNuvem()
    {
        int quantas = Mathf.Max(4, Largura / 14);
        float metade = Largura / 2f;
        for (int i = 0; i < quantas; i++)
        {
            float y = 3.5f + (i * 5.3f) % 12f; // fora da faixa da interface (lá embaixo)
            var sombra = ConstrutorDeFase.Visual("SombraDeNuvem", transform, new Vector3(metade, y, 0f), "sombra_nuvem", 25, false);
            sombra.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.12f);
            sombra.transform.localScale = Vector3.one * (1.4f + (i % 4) * 0.2f);
            // vai de uma ponta à outra do mapa a uns 1 a 2 blocos por segundo
            Animacao.Adicionar(sombra, Animacao.Tipo.Flutuar, (1.1f + (i % 4) * 0.3f) / metade, metade);
        }
    }

    Vector2Int CelulaDoPonto(int fase) => fase == Fases.IndiceSecreto ? pontoSecreto.Value : pontos[fase];

    // O Subaru fica com os pés no centro do ponto.
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

    // Lista de células por onde o Subaru passa do ponto atual até o vizinho "destino".
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
            if (Mathf.Abs(alvo.x - pes.x) > 0.01f) desenhoSubaru.flipX = alvo.x < pes.x;
            while (pes != alvo)
            {
                pes = Vector3.MoveTowards(pes, alvo, velocidade * Time.deltaTime);
                timerPasso += Time.deltaTime;
                // pulinhos enquanto anda, estilo mapa do Mario
                Subaru.position = pes + Vector3.up * Mathf.Abs(Mathf.Sin(timerPasso * 14f)) * 0.15f;
                desenhoSubaru.sprite = FabricaDeSprites.Pegar((int)(timerPasso * 10f) % 2 == 0 ? "jogador" : "jogador_andando");
                yield return null;
            }
            Efeitos.Poeira(transform, pes + Vector3.down * 0.4f, 1, 1f);
        }
        Subaru.position = pes;
        desenhoSubaru.sprite = FabricaDeSprites.Pegar("jogador");
        Selecionado = destino;
        Andando = false;
    }

    // Acabou de passar de fase: a névoa some ao longo do caminho novo, a fase nova aparece e o Subaru vai até lá.
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
                if (terra.TryGetValue(c, out SpriteRenderer t)) t.color = Color.white;
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

            // o ponto em que o Subaru está "pulsa" de leve
            if (i != nascendo)
                desenhosDosPontos[i].transform.localScale = Vector3.one * (i == Selecionado && !Andando ? 1f + 0.08f * Mathf.Sin(Time.time * 6f) : 1f);
        }

        // a fase secreta: a moeda gira em cima do ponto, e o ponto pulsa quando o Subaru está nele
        if (personagemSecreto != null && personagemSecreto.gameObject.activeSelf)
        {
            personagemSecreto.localScale = new Vector3(Mathf.Cos(Time.time * 3f), 1f, 1f);
            if (Selecionado == Fases.IndiceSecreto && !Andando)
                desenhoSecreto.transform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.time * 6f));
            else desenhoSecreto.transform.localScale = Vector3.one;
        }

        RespirarNevoa();

        // parado, o Subaru respira
        if (!Andando) Subaru.localScale = new Vector3(1f, 1f + 0.04f * Mathf.Sin(Time.time * 3f), 1f);
    }

    // A névoa "respira" devagar. O mapa tem umas 2000 bolotas, então só mexe nas colunas que
    // aparecem na tela (mais uma folga de 2 de cada lado); as outras ninguém está vendo.
    void RespirarNevoa()
    {
        if (nevoa == null) return;
        int de = 0, ate = Largura - 1;
        Camera cam = Camera.main;
        if (cam != null)
        {
            float meiaLargura = cam.orthographicSize * cam.aspect + 2f;
            de = Mathf.Max(0, Mathf.FloorToInt(cam.transform.position.x - meiaLargura));
            ate = Mathf.Min(Largura - 1, Mathf.CeilToInt(cam.transform.position.x + meiaLargura));
        }
        float tempo = Time.time * 1.3f;
        for (int x = de; x <= ate; x++)
            for (int y = 0; y < Altura; y++)
                if (nevoa[x, y] != null)
                    nevoa[x, y].transform.localScale = Vector3.one * tamanhoDaNevoa[x, y] * (1f + 0.06f * Mathf.Sin(tempo + x * 0.7f + y * 1.1f));
    }

    // ------------------------------------------------------------------ desenhos novos do mapa

    // Ficam registrados na FabricaDeSprites (como os das armadilhas), então Pegar("mapa_...") funciona.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        Vector2 centro = FabricaDeSprites.Centro, baseDoDesenho = FabricaDeSprites.Base;
        FabricaDeSprites.Registrar("mapa_chao", () => FabricaDeSprites.Procedural(16, 16, centro, CorDoChaoDaRegiao));
        FabricaDeSprites.Registrar("mapa_abismo", () => FabricaDeSprites.Procedural(16, 16, centro, (x, y) => CorDoAbismo(x, y, false)));
        FabricaDeSprites.Registrar("mapa_abismo_borda", () => FabricaDeSprites.Procedural(16, 16, centro, (x, y) => CorDoAbismo(x, y, true)));
        FabricaDeSprites.Registrar("mapa_rajada", () => FabricaDeSprites.Procedural(16, 5, centro, CorDaRajada));
        FabricaDeSprites.Registrar("mapa_pinheiro", () => FabricaDeSprites.Procedural(16, 24, baseDoDesenho, CorDoPinheiro));
        FabricaDeSprites.Registrar("mapa_biblioteca", () => FabricaDeSprites.Procedural(24, 32, baseDoDesenho, CorDaBiblioteca));
        FabricaDeSprites.Registrar("mapa_ruina", () => FabricaDeSprites.DeArte(ArteRuina, baseDoDesenho));
        FabricaDeSprites.Registrar("mapa_cristal", () => FabricaDeSprites.DeArte(ArteCristal, baseDoDesenho));
        FabricaDeSprites.Registrar("mapa_olhos", () => FabricaDeSprites.DeArte(ArteOlhos, centro));
        FabricaDeSprites.Registrar("mapa_ceu_sombrio", () => FabricaDeSprites.Procedural(128, 32, Vector2.zero, CorDoCeuSombrio));
    }

    static Color32 Cor(char letra) => FabricaDeSprites.Cor(letra); // atalho: cor da paleta

    // Coluna quebrada (das ruínas antigas) com um pedaço caído do lado.
    static readonly string[] ArteRuina =
    {
        "....kk.kk.......",
        "...kZmkMMk......",
        "...kZmmmMk......",
        "...kZmmmMk......",
        "...kZmzmMk......",
        "...kZmmmMk......",
        "...kZmmmMk......",
        "...kZmmmMk...kk.",
        "...kZmmmMk..kZMk",
        "..kkkkkkkkk.kmMk",
        "..kZmmmmmMk..kk.",
        "..kkkkkkkkk.....",
    };

    // Três cristais mágicos (brilho lilás à esquerda, roxo à direita).
    static readonly string[] ArteCristal =
    {
        ".......k........",
        "......kPk.......",
        "......kPVk......",
        "......kPVk......",
        "..k...kPVk......",
        ".kPk..kPVk..k...",
        ".kPVk.kPVk.kPk..",
        ".kPVk.kPVk.kPVk.",
        ".kPVk.kPVk.kPVk.",
        "..kVkkkPVkkkVk..",
        "..kkkkkkkkkkkk..",
    };

    // Olhos vermelhos (bravos) no escuro.
    static readonly string[] ArteOlhos =
    {
        "rr...rr",
        ".rr.rr.",
    };

    // Chão das regiões: branco (o mapa pinta com a cor da região), com uns pontinhos para não ficar liso.
    static Color32 CorDoChaoDaRegiao(int x, int y)
    {
        int r = (x * 7 + y * 13 + x * y * 5) % 19;
        if (r == 0) return new Color32(228, 228, 228, 255);
        if (r == 7) return new Color32(242, 242, 242, 255);
        return new Color32(255, 255, 255, 255);
    }

    // Abismo visto de cima: escuridão com uns pontinhos (pedras lá no fundo).
    // borda = true: a parede de pedra do desfiladeiro aparece na parte de cima do bloco.
    static Color32 CorDoAbismo(int x, int y, bool borda)
    {
        var fundo = new Color32(26, 18, 34, 255);
        int a = 15 - y; // 0 = linha de cima do bloco (na textura, y = 0 é embaixo)
        if (borda)
        {
            int fimDaPedra = 6 + (x * 5 % 7 == 0 ? 1 : 0);    // a parte de baixo da parede é irregular
            if (a == 0) return Cor('k');                          // a beirada
            if (a <= fimDaPedra) return a == 3 || (a == 5 && x % 5 == 1) ? Cor('d') : Cor('b'); // camadas de rocha
            if (a <= fimDaPedra + 3) return Color32.Lerp(Cor('d'), fundo, (a - fimDaPedra) / 3f); // a parede some no escuro
        }
        return (x * 5 + y * 3) % 13 == 0 && (x + y) % 3 == 0 ? new Color32(48, 34, 60, 255) : fundo;
    }

    // Rajada de vento: um risco branco ondulado que vai ficando mais forte para a direita.
    static Color32 CorDaRajada(int x, int y)
    {
        int altura = Mathf.RoundToInt(2f + 1.5f * Mathf.Sin(x / 15f * Mathf.PI * 2f));
        return y == altura ? new Color32(255, 255, 255, (byte)(90 + x * 9)) : FabricaDeSprites.Transparente;
    }

    // Pinheiro sombrio: três "andares" de folhas escuras, contorno preto e tronco marrom.
    static bool DentroDoPinheiro(int x, int y)
    {
        float dx = Mathf.Abs(x - 7.5f);
        return (y >= 3 && y <= 11 && dx <= (11 - y) * 0.85f + 1f)
            || (y >= 9 && y <= 17 && dx <= (17 - y) * 0.7f + 0.5f)
            || (y >= 15 && y <= 23 && dx <= (23 - y) * 0.55f);
    }

    static Color32 CorDoPinheiro(int x, int y)
    {
        if (DentroDoPinheiro(x, y))
        {
            bool borda = !DentroDoPinheiro(x + 1, y) || !DentroDoPinheiro(x - 1, y) || !DentroDoPinheiro(x, y + 1) || !DentroDoPinheiro(x, y - 1);
            if (borda) return Cor('k');
            return x < 7 ? new Color32(96, 80, 128, 255) : new Color32(60, 50, 88, 255); // luz à esquerda, sombra à direita
        }
        if (y < 3 && x >= 7 && x <= 8) return Cor('d'); // tronco
        return FabricaDeSprites.Transparente;
    }

    // Torre da Biblioteca Proibida: pedra clara, porta e janelas com a luz roxa da magia e uma cúpula.
    static bool DentroDaCupula(int x, int y) => y >= 21 && (x - 11.5f) * (x - 11.5f) / 49f + (y - 21f) * (y - 21f) / 64f <= 1f;
    static bool DentroDaTorre(int x, int y) =>
        (x >= 5 && x <= 18 && y >= 0 && y <= 19) || (x >= 4 && x <= 19 && y >= 19 && y <= 21) || DentroDaCupula(x, y);

    static bool DentroDaPorta(int x, int y) => y >= 1 && ((x >= 10 && x <= 13 && y <= 6) || (x >= 11 && x <= 12 && y <= 7));

    static Color32 CorDaBiblioteca(int x, int y)
    {
        if (x >= 11 && x <= 12 && y >= 29) return Cor('y'); // a estrelinha na ponta da cúpula
        if (!DentroDaTorre(x, y)) return FabricaDeSprites.Transparente;
        bool borda = !DentroDaTorre(x + 1, y) || !DentroDaTorre(x - 1, y) || !DentroDaTorre(x, y + 1) || y == 0;
        if (borda) return Cor('k');
        if (y > 21) return x - 11.5f + (y - 25f) * 0.4f < -2f ? Cor('P') : Cor('V'); // cúpula roxa com brilho
        if (y >= 19) return y == 19 ? Cor('k') : Cor('S');                         // a beirada embaixo da cúpula
        if (DentroDaPorta(x, y)) return Cor('v');                                   // a porta, com a escuridão mágica
        if (DentroDaPorta(x - 1, y) || DentroDaPorta(x + 1, y) || DentroDaPorta(x, y - 1)) return Cor('k'); // o batente
        if (y >= 11 && y <= 14 && (x == 7 || x == 8 || x == 15 || x == 16)) return Cor('P'); // janelas acesas
        bool rejunte = y % 5 == 0 || (y / 5 % 2 == 0 ? x % 6 == 2 : x % 6 == 5);
        if (x >= 15) return rejunte ? Cor('z') : Cor('M');                         // lado da sombra
        return rejunte ? Cor('M') : x <= 6 ? Cor('Z') : Cor('m');
    }

    // Céu escuro da região sombria: roxo, transparente na ponta esquerda (para ir escurecendo aos poucos).
    static Color32 CorDoCeuSombrio(int x, int y)
    {
        float lado = Mathf.Clamp01(x / 20f);
        return new Color32(36, 20, 56, (byte)(lado * (110f + y * 3f)));
    }
}
