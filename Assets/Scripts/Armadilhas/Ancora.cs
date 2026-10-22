using System.Collections.Generic;
using UnityEngine;

// 'Q' - a ÂNCORA DA CAPITÃ: a âncora de ferro (a arma da Capitã Âncora, a pirata da fase 6) girando sem
// parar, presa numa corrente, em volta de um bloco de ferro (o eixo).
//  - Só a ÂNCORA mata. A corrente é só enfeite (pode encostar à vontade).
//  - O eixo é um bloco sólido: dá para subir nele. E lá em cima é o "olho do furacão":
//    a âncora passa em volta, mas não alcança quem está colado no eixo. ;)
//  - A âncora é desenhada ATRÁS dos blocos: quando ela passa pelo chão ou pelo teto, parece que "entra" neles
//    (e levanta poeira). Uma âncora com o eixo dentro de um teto de tijolos fica escondida metade do tempo...
//  - O jeito de girar (onde a âncora começa e para que lado gira) depende da COLUNA do 'Q' no mapa
//    (veja a tabela "Jeitos" abaixo). Assim quem desenha a fase escolhe tudo só mudando o 'Q' de lugar,
//    e a fase é igual em toda tentativa (dá para decorar o ritmo).
//  - ENTERRADO: um 'Q' colocado DENTRO do chão (com chão em cima dele) se disfarça de bloco de chão.
//    Não dá para ver nada... até a âncora pular para fora do chão, como a barbatana de um tubarão. :)
//  (no código, a âncora se chama "bola": é o mesmo papel da bola de um mangual)
//
// 'q' - igualzinho... até você chegar perto. Aí a Capitã fica FURIOSA (sobe a bandeirinha pirata do eixo):
//       a corrente range, a âncora freia e treme por um instante (esse é o aviso!) e depois volta
//       girando para o OUTRO lado, com o dobro da velocidade. E ela não se acalma mais.
public class Ancora : MonoBehaviour
{
    enum Estado { Girando, Avisando, Brava }

    [Header("Giro")]
    public float raio = 2.75f;       // tamanho da corrente: do meio do eixo até o meio da bola (em blocos)
    public float velocidade = 120f;  // em graus por segundo (120 = uma volta a cada 3 segundos)
    public float angulo = 90f;       // onde a bola está: 0 = à direita, 90 = em cima, 180 = à esquerda, 270 = embaixo
    public int sentido = -1;         // 1 = anti-horário, -1 = horário

    [Header("Variante troll ('q')")]
    public bool troll;
    public float distanciaParaIrritar = 4.5f; // distância na horizontal em que a Capitã percebe você (a bola alcança uns 3,5)
    public float tempoDeAviso = 0.4f;       // quanto tempo ela fica rangendo e freando antes de virar
    public float multiplicadorDaRaiva = 2f; // brava, ela gira esse tanto de vezes mais rápido

    public const float RaioDaBola = 0.45f;  // área que mata (um pouco MENOR que o desenho com os espinhos: é mais justo)
    const int QuantidadeDeElos = 7;

    // Os 8 "jeitos" de girar. O 'Q' que está na coluna x do mapa usa o jeito número (x % 8).
    // Exemplo: um 'Q' na coluna 18 usa o jeito 2 (18 % 8 = 2): começa com a bola embaixo, girando no sentido horário.
    static readonly (float angulo, int sentido)[] Jeitos =
    {
        (90f, -1),  // 0: bola em cima, gira no sentido horário
        (90f, 1),   // 1: bola em cima, anti-horário
        (270f, -1), // 2: bola embaixo, horário
        (270f, 1),  // 3: bola embaixo, anti-horário
        (0f, -1),   // 4: bola à direita, horário
        (0f, 1),    // 5: bola à direita, anti-horário
        (180f, -1), // 6: bola à esquerda, horário
        (180f, 1),  // 7: bola à esquerda, anti-horário
    };

    static readonly Color CorDaLuz = new Color(0.55f, 0.75f, 1f);      // azul do cabelo da Capitã
    static readonly Color CorDaRaiva = new Color(1f, 0.55f, 0.55f);    // bola esquentando de raiva
    static readonly Color CorDoRastro = new Color(1f, 0.35f, 0.4f, 0.7f);

    Estado estado = Estado.Girando;
    float timer;
    float velocidadeAtual;
    float timerRastro;
    bool bolaDentroDoChao;

    Transform bola, visualDaBola, chifre, aviso;
    SpriteRenderer desenhoDaBola, desenhoDoEixo;
    readonly List<Transform> elos = new List<Transform>();
    UnityEngine.Rendering.Universal.Light2D luz;
    Perigo perigo;
    ContactFilter2D filtroSolido;
    readonly List<Collider2D> encostados = new List<Collider2D>();

    void Awake()
    {
        // O eixo: um bloco de ferro sólido (dá para ficar em cima dele).
        desenhoDoEixo = ConstrutorDeFase.Visual("Eixo", transform, transform.position, "ancora_eixo", 1).GetComponent<SpriteRenderer>();
        gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;

        // A corrente: elos alternando "de frente" e "de lado", como numa corrente de verdade.
        // Ficam atrás do eixo e da bola (ordem -2), saindo do meio do bloco.
        for (int i = 0; i < QuantidadeDeElos; i++)
        {
            string desenho = i % 2 == 0 ? "corrente_elo" : "corrente_elo_lado";
            elos.Add(ConstrutorDeFase.Visual("Elo", transform, transform.position, desenho, -2, false).transform);
        }

        // A bola: o único pedaço que mata. Fica atrás dos blocos (ordem -1), então "entra" no chão e no teto.
        var objetoBola = new GameObject("Bola");
        objetoBola.transform.SetParent(transform, false);
        bola = objetoBola.transform;
        var area = objetoBola.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = RaioDaBola;
        perigo = objetoBola.AddComponent<Perigo>();
        Perigo.TornarMovel(objetoBola); // ela se mexe o tempo todo
        visualDaBola = ConstrutorDeFase.Visual("Visual", bola, bola.position, "ancora", -1).transform;
        desenhoDaBola = visualDaBola.GetComponent<SpriteRenderer>();
        luz = Luzes.Ponto(bola, CorDaLuz, 2.4f, 0.7f); // só existe nas fases escuras (senão é null)

        // O chifre da fúria da Capitã (aparece quando ela fica brava) e o "!" de aviso. Começam escondidos.
        chifre = ConstrutorDeFase.Visual("Chifre", transform, transform.position + Vector3.up * 0.5f, "ancora_brava", 2).transform;
        chifre.gameObject.SetActive(false);
        aviso = ConstrutorDeFase.Visual("Aviso", transform, transform.position + Vector3.up * 1.75f, "aviso", 20, false).transform;
        aviso.gameObject.SetActive(false);

        filtroSolido = new ContactFilter2D { useTriggers = false };
        velocidadeAtual = velocidade;
        Posicionar(Vector3.zero);
    }

    // Chamado pelo ConstrutorDeFase: a coluna do mapa escolhe o jeito de girar; ehTroll = é um 'q'.
    // disfarce = nome do desenho de chão, quando o mangual está ENTERRADO (null = eixo de ferro normal).
    public void Montar(int coluna, bool ehTroll, string disfarce = null)
    {
        var jeito = Jeitos[Mathf.Abs(coluna) % Jeitos.Length];
        angulo = jeito.angulo;
        sentido = jeito.sentido;
        troll = ehTroll;

        if (disfarce != null)
        {
            // O eixo vira um bloco de chão igualzinho aos vizinhos (mesma ordem de desenho do chão).
            desenhoDoEixo.sprite = FabricaDeSprites.Pegar(disfarce);
            desenhoDoEixo.sortingOrder = 0;
            // O chifre e o "!" saem lá na superfície (um bloco acima), não no meio da terra.
            chifre.position += Vector3.up;
            aviso.position += Vector3.up;
        }
        Posicionar(Vector3.zero);
    }

    void Start()
    {
        // A bola já começa dentro do chão? Anota sem fazer poeira nem barulho
        // (senão todo mangual enterrado daria um "tum" quando a fase começa).
        bolaDentroDoChao = BolaDentroDeAlgo();
    }

    void Update()
    {
        float dt = Time.deltaTime; // na pausa é 0: o mangual congela sozinho
        Vector3 tremida = Vector3.zero;

        switch (estado)
        {
            case Estado.Girando:
                velocidadeAtual = velocidade;
                if (troll && JogadorPerto()) ComecarAviso();
                break;

            case Estado.Avisando:
                // freando até parar, tremendo, com o eixo piscando vermelho e o "!" em cima
                timer -= dt;
                velocidadeAtual = velocidade * Mathf.Clamp01(timer / tempoDeAviso);
                tremida = (Vector3)(Random.insideUnitCircle * 0.07f);
                bool piscando = (timer * 12f) % 2f < 1f;
                aviso.gameObject.SetActive(piscando);
                desenhoDoEixo.color = piscando ? CorDaRaiva : Color.white;
                if (timer <= 0f) FicarBrava();
                break;

            case Estado.Brava:
                DeixarRastro(dt);
                break;
        }

        angulo = Mathf.Repeat(angulo + sentido * velocidadeAtual * dt, 360f);
        Posicionar(tremida);
        ChecarChao();
    }

    // Coloca a bola na ponta da corrente e espalha os elos entre o eixo e a bola.
    void Posicionar(Vector3 tremida)
    {
        float radianos = angulo * Mathf.Deg2Rad;
        Vector3 direcao = new Vector3(Mathf.Cos(radianos), Mathf.Sin(radianos), 0f);
        bola.position = transform.position + direcao * raio + tremida;
        visualDaBola.localRotation = Quaternion.Euler(0f, 0f, angulo * 2f); // a bola rola enquanto gira

        for (int i = 0; i < elos.Count; i++)
        {
            float quanto = i / (float)(elos.Count - 1);
            float distancia = Mathf.Lerp(0.3f, raio - RaioDaBola, quanto);
            elos[i].position = transform.position + direcao * distancia + tremida * quanto;
            elos[i].localRotation = Quaternion.Euler(0f, 0f, angulo); // elo deitado na direção da corrente
        }
    }

    bool JogadorPerto()
    {
        if (!GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return false;
        Vector2 d = jogador - (Vector2)transform.position;
        return Mathf.Abs(d.x) < distanciaParaIrritar && Mathf.Abs(d.y) < raio + 2f;
    }

    // 'q': percebeu o jogador. Range a corrente e começa a frear (0,4 s de aviso).
    void ComecarAviso()
    {
        estado = Estado.Avisando;
        timer = tempoDeAviso;
        GerenciadorDoJogo.Som("armadilha");
        chifre.gameObject.SetActive(true); // a bandeirinha pirata sobe do eixo
        StartCoroutine(Efeitos.Pulinho(chifre, 0.3f, 0.2f));
    }

    // Ficou furiosa: inverte o sentido e gira o dobro, com a âncora vermelha de raiva.
    void FicarBrava()
    {
        estado = Estado.Brava;
        sentido = -sentido;
        velocidadeAtual = velocidade * multiplicadorDaRaiva;
        aviso.gameObject.SetActive(false);
        desenhoDoEixo.color = Color.white;
        desenhoDaBola.color = CorDaRaiva;
        if (luz != null) luz.color = CorDaRaiva;
        Animacao.Adicionar(chifre.gameObject, Animacao.Tipo.Piscar, 14f, 0.15f); // chifre pulsando

        perigo.conquista = "capita_furiosa"; // morreu para a Capitã brava? Conquista!
        GerenciadorDoJogo.Som("rugido", 0.5f);
        CameraSeguir.Tremer(0.15f, 0.25f);
        Efeitos.Poeira(transform.parent, bola.position, 8, 3f);
    }

    // Brava, a bola deixa um rastro vermelho (fica mais fácil de acompanhar com os olhos).
    void DeixarRastro(float dt)
    {
        timerRastro -= dt;
        if (timerRastro > 0f || bolaDentroDoChao) return;
        timerRastro = 0.03f;
        Particula.Criar(transform.parent, bola.position, Vector2.zero, 0.25f, 0.9f, CorDoRastro);
    }

    // A bola acabou de entrar no chão (ou no teto)? Poeira e um "tum" (só se o jogador estiver perto, senão vira barulheira).
    // Acabou de SAIR? Só um pouquinho de poeira (é o aviso do mangual enterrado: "olha a terra pulando ali!").
    void ChecarChao()
    {
        bool dentro = BolaDentroDeAlgo();
        if (dentro && !bolaDentroDoChao)
        {
            Efeitos.Poeira(transform.parent, bola.position, 5, 2.5f);
            if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador) && Vector2.Distance(jogador, bola.position) < 8f)
            {
                GerenciadorDoJogo.Som("pancada", estado == Estado.Brava ? 0.45f : 0.25f);
                if (estado == Estado.Brava) CameraSeguir.Tremer(0.06f, 0.1f);
            }
        }
        else if (!dentro && bolaDentroDoChao)
        {
            Efeitos.Poeira(transform.parent, bola.position, 3, 2f);
        }
        bolaDentroDoChao = dentro;
    }

    // O meio da bola está dentro de algo sólido (chão, tijolo...)? O jogador e os bichos não contam.
    bool BolaDentroDeAlgo()
    {
        int quantidade = Physics2D.OverlapCircle(bola.position, 0.15f, filtroSolido, encostados);
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = encostados[i];
            if (!outro.TryGetComponent<Jogador>(out _) && !Inimigo.EhBicho(outro)) return true;
        }
        return false;
    }

    // ------------------------------------------------------------------ desenhos

    // Registra os desenhos do mangual na FabricaDeSprites (funcionam com Pegar("ancora") em qualquer lugar,
    // inclusive no mapa do mundo).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("ancora", () => FabricaDeSprites.DeArte(ArteAncora, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("corrente_elo", () => FabricaDeSprites.DeArte(ArteElo, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("corrente_elo_lado", () => FabricaDeSprites.DeArte(ArteEloDeLado, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("ancora_eixo", () => FabricaDeSprites.DeArte(ArteEixo, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("ancora_brava", () => FabricaDeSprites.DeArte(ArteChifre, FabricaDeSprites.Base));
    }

    // O eixo: bloco de ferro com rebites nos cantos e a argola da corrente no meio.
    static readonly string[] ArteEixo =
    {
        "kkkkkkkkkkkkkkkk",
        "kZZZZZZZZZZZZZMk",
        "kZwsMMMMMMMMwsSk",
        "kZszMMMMMMMMszSk",
        "kZMMMMkkkkMMMMSk",
        "kZMMMkSSSSkMMMSk",
        "kZMMkSZsSSSkMMSk",
        "kZMMkSskkSSkMMSk",
        "kZMMkSSkkSzkMMSk",
        "kZMMkSSSSzzkMMSk",
        "kZMMMkSzzzkMMMSk",
        "kZMMMMkkkkMMMMSk",
        "kZwsMMMMMMMMwsSk",
        "kZszMMMMMMMMszSk",
        "kMSSSSSSSSSSSSSk",
        "kkkkkkkkkkkkkkkk",
    };

    // Elo da corrente visto de frente (com o furo no meio)...
    static readonly string[] ArteElo =
    {
        ".kkkkkkk.",
        "kZssssMSk",
        "ksMkkkkSk",
        "kMSkkkkzk",
        "kSSSSSzzk",
        ".kkkkkkk.",
    };

    // ...e de lado (fininho). Alternando os dois, parece uma corrente de verdade.
    static readonly string[] ArteEloDeLado =
    {
        ".kkkkkkk.",
        "kZsssssMk",
        "kMSSSSSzk",
        ".kkkkkkk.",
    };

    // A bandeirinha pirata que sobe do eixo quando a Capitã fica furiosa (o 'q'): preta com a caveira branca.
    static readonly string[] ArteChifre =
    {
        "kkkkkk.",
        "kjjjjjk",
        "kjwwwjk",
        "kjwkwjk",
        "kjjwjjk",
        "kjwjwjk",
        "kkkkkkk",
        "k......",
        "k......",
        "k......",
    };

    // A âncora de ferro (é ela que mata). A argola fica em cima, presa na corrente; a luz vem de cima,
    // pela esquerda (o lado esquerdo é mais claro). O meio do desenho é o meio da área que mata.
    static readonly string[] ArteAncora =
    {
        "..........kkkk..........",
        ".........kZssMk.........",
        "........kZk..kMk........",
        "........ksk..kSk........",
        ".........kMMSSk.........",
        "..........kMSk..........",
        "......kkkkkMSkkkkk......",
        "......kZsssMSMMMSk......",
        "......kkkkkMSkkkkk......",
        "..........kMSk..........",
        "..........kMSk..........",
        "..........kMSk..........",
        "..........kMSk..........",
        "..........kMSk..........",
        "..........kMSk..........",
        "..k.......kMSk.......k..",
        ".kZk......kMSk......kSk.",
        ".kZsk.....kMSk.....kMSk.",
        "..kZsk....kMSk....kMSk..",
        "...kZsk...kMSk...kMSk...",
        "....kZsMkkkMSkkkMMSk....",
        ".....ksMMMMMSSMMMSk.....",
        "......kkMMMSSSSSkk......",
        "........kkkkkkkk........",
    };
}
