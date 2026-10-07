using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 'j' - PLATAFORMA MÁGICA: um pedaço de ponte flutuando sobre o abismo, preso a um cristal.
//       Anda para a direita e volta (vai e volta) até encostar em algo sólido ou andar no máximo
//       6 blocos, com uma pausinha em cada ponta. Quem está em cima anda junto.
// 'J' - IGUALZINHA... mas meio segundo depois que você sobe, o cristal fica VERMELHO, ela treme e DESPENCA.
//       (um tempinho depois ela reaparece no lugar, para ninguém ficar preso do lado errado do abismo)
// 'D' - IGUALZINHA também... mas o cristal fica AMARELO, ela treme e DISPARA para o lado até bater numa
//       parede, e para DE REPENTE (o tranco joga o Subaru para a frente).
//       Quem pula de medo quando ela treme... fica para trás. :)
// Blocos encostados NA MESMA LINHA formam UMA plataforma só (quem junta é o ConstrutorDeFase).
//
// Como funciona a física: a plataforma é um Rigidbody2D CINEMÁTICO (não sente gravidade e ninguém
// empurra ela) que anda com MovePosition no FixedUpdate. O Subaru não tem atrito, então ele não
// "gruda" sozinho em coisas que se mexem: a cada passo da física a plataforma carrega ele junto
// com Jogador.Carregar(o quanto ela andou). Quando ele sai de cima, leva junto o embalo dela.
//
// Todas as plataformas começam na ponta esquerda, indo para a direita, no instante em que a fase é
// montada (a fase é recriada a cada morte). Então duas plataformas com o mesmo alcance andam SEMPRE
// juntas, com a mesma distância entre elas: dá para planejar os pulos de uma para a outra.
public class PlataformaMovel : MonoBehaviour
{
    public enum Tipo { Normal, Despenca, Dispara }

    [Header("Vai e volta")]
    public Tipo tipo = Tipo.Normal;
    public float velocidade = 3f;            // velocidade MÉDIA: ela sai devagar, acelera e freia nas pontas
    public float distanciaMaxima = 6f;
    public float pausaNasPontas = 0.4f;

    [Header("Pegadinhas ('J' e 'D')")]
    public float esperaAntesDoTruque = 0.5f; // contado a partir do momento em que o Subaru sobe
    public float tempoTremendo = 0.35f;      // o aviso: ela treme e o cristal muda de cor
    public float gravidadeDaQueda = 60f;     // mais que a do Subaru (34): ela "foge" de baixo dos pés dele
    public float tempoParaReaparecer = 1.5f; // 'J': depois de sumir lá embaixo
    public float velocidadeDoDisparo = 18f;
    public float aceleracaoDoDisparo = 120f;
    public float distanciaMaximaDoDisparo = 16f;
    public float tranco = 14f;               // velocidade com que o Subaru é jogado para a frente na freada
    public float pulinhoDoTranco = 7f;
    public float descansoDepoisDoDisparo = 1.2f;

    // A caixa de colisão pega só a parte de cima do bloco (a madeira e a pedra).
    // A ponta de pedra e o cristal pendurados embaixo são só enfeite.
    const float AlturaDoColisor = 0.75f;
    const float AlturaDoCristal = -0.62f;    // onde o cristal fica pendurado (em relação ao centro)

    enum Estado { VaiEVolta, Caindo, Escondida, Disparando, Parada }
    Estado estado = Estado.VaiEVolta;

    Rigidbody2D corpo;
    BoxCollider2D colisor;
    Transform visual;          // os desenhos ficam num filho: dá para tremer e afundar sem mexer no colisor
    Transform cristal;
    SpriteRenderer desenhoDoCristal;
    Sprite cristalNormal, cristalVermelho, cristalAmarelo;
    Light2D luz;               // só existe nas fases escuras (de dia o Luzes.Ponto devolve null)
    int largura = 1;
    bool iniciada;             // o Start já mediu o caminho? (antes disso ela não se mexe)

    Vector2 posicao;           // onde a plataforma está (o corpo vai para cá no passo da física)
    Vector2 origem;
    float alcance;             // quanto ela anda para a direita (medido no Start)
    float progresso;           // o "relógio" do vai e volta: 0 = na origem, 1 = na outra ponta
    int sentido = 1;           // 1 = indo para a direita, -1 = voltando
    float pausa;

    float contagem = -1f;      // contagem para o truque depois que o Subaru sobe (-1 = desarmada)
    bool tremendo;             // o aviso (ela continua andando enquanto treme)
    float timerDoTremor;
    float timerDeEspera;       // Escondida: até reaparecer. Parada: até voltar a andar.
    float velocidadeDaQueda;
    float velocidadeDoDisparoAtual;
    float destinoDoDisparo;
    int direcaoDoDisparo = 1;
    bool freouAgora;

    Vector2 velocidadeAtual;   // quanto ela andou no último passo, por segundo (o Subaru herda isso quando sai)
    Jogador jogadorEmCimaAntes;
    float afundado;            // a afundadinha quando o Subaru aterrissa (só no desenho)
    float aparecendo = 1f;     // 0 -> 1: animação de reaparecer (só no desenho)
    float timerDoBrilho;
    float faseDaAnimacao;

    ContactFilter2D filtroSolido;
    readonly List<RaycastHit2D> acertos = new List<RaycastHit2D>();

    static readonly Color CorDaLuz = new Color(0.55f, 0.8f, 1f);
    static readonly Color CorDoPerigo = new Color(1f, 0.3f, 0.25f);
    static readonly Color CorDoDisparo = new Color(1f, 0.85f, 0.3f);
    static readonly Color CorDoBrilho = new Color(0.75f, 0.9f, 1f, 0.9f);

    // A letra do mapa vira o tipo da plataforma.
    public static Tipo TipoDaLetra(char letra)
    {
        if (letra == 'J') return Tipo.Despenca;
        if (letra == 'D') return Tipo.Dispara;
        return Tipo.Normal;
    }

    void Awake()
    {
        corpo = gameObject.AddComponent<Rigidbody2D>();
        corpo.bodyType = RigidbodyType2D.Kinematic;
        corpo.interpolation = RigidbodyInterpolation2D.Interpolate; // desenho liso entre os passos da física
        filtroSolido = new ContactFilter2D { useTriggers = false };
        faseDaAnimacao = transform.position.x * 1.3f; // cada cristal balança num ritmo
    }

    // Chamado pelo ConstrutorDeFase logo depois de criar: qual o tipo e quantos blocos de largura.
    // (o objeto já foi criado no CENTRO da plataforma)
    public void Montar(Tipo tipoDaPlataforma, int blocos)
    {
        tipo = tipoDaPlataforma;
        largura = Mathf.Max(1, blocos);

        colisor = gameObject.AddComponent<BoxCollider2D>();
        colisor.size = new Vector2(largura, AlturaDoColisor);
        colisor.offset = new Vector2(0f, 0.5f - AlturaDoColisor / 2f); // encostado no topo do bloco

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        for (int i = 0; i < largura; i++)
        {
            string desenho = largura == 1 ? "plataforma_unica"
                           : i == 0 ? "plataforma_esq"
                           : i == largura - 1 ? "plataforma_dir"
                           : "plataforma_meio";
            float x = i - (largura - 1) / 2f;
            ConstrutorDeFase.Visual("Pedaco", visual, transform.position + Vector3.right * x, desenho, 2);
        }

        // O cristal mágico pendurado embaixo do meio: é ele que "segura" a plataforma no ar.
        // Desenhado ATRÁS da pedra (ordem 1), para parecer que sai de dentro dela.
        cristalNormal = FabricaDeSprites.Pegar("plataforma_cristal");
        cristalVermelho = FabricaDeSprites.Pegar("plataforma_cristal_vermelho");
        cristalAmarelo = FabricaDeSprites.Pegar("plataforma_cristal_amarelo");
        var objetoDoCristal = ConstrutorDeFase.Visual("Cristal", visual, transform.position + Vector3.up * AlturaDoCristal, "plataforma_cristal", 1);
        cristal = objetoDoCristal.transform;
        desenhoDoCristal = objetoDoCristal.GetComponent<SpriteRenderer>();
        luz = Luzes.Ponto(cristal, CorDaLuz, 2.2f, 0.6f);
    }

    void Start()
    {
        if (colisor == null) Montar(tipo, largura); // segurança: criaram sem chamar Montar
        origem = posicao = transform.position;
        alcance = EspacoLivre(1, distanciaMaxima);
        iniciada = true;
    }

    // ------------------------------------------------------------------ física (passo fixo)

    void FixedUpdate()
    {
        if (!iniciada) return;
        float dt = Time.fixedDeltaTime;
        Jogador jogador = JogadorEmCima();

        if (jogador != null && jogadorEmCimaAntes == null) Pisou();
        // Saiu de cima (pulou, andou para fora, a plataforma caiu...)? Leva o embalo dela junto.
        // (no vai e volta isso quase não se nota; no disparo é o que faz ele voar para a frente)
        if (jogador == null && jogadorEmCimaAntes != null && !jogadorEmCimaAntes.Morto)
            jogadorEmCimaAntes.Corpo.linearVelocity += new Vector2(velocidadeAtual.x, 0f);

        Vector2 antes = posicao;
        switch (estado)
        {
            case Estado.VaiEVolta:
                AvancarRelogio(dt);
                posicao.x = XDoVaiEVolta();
                ContarOTruque(dt);
                break;
            case Estado.Caindo:
                AvancarRelogio(dt); // o relógio continua: quando ela reaparecer, volta no mesmo ritmo das outras
                Cair(dt);
                break;
            case Estado.Escondida:
                AvancarRelogio(dt);
                EsperarParaReaparecer(dt);
                break;
            case Estado.Disparando: Disparar(dt, jogador); break;
            case Estado.Parada: EsperarParaRecomecar(dt); break;
        }
        corpo.MovePosition(posicao);

        Vector2 deslocamento = posicao - antes;
        velocidadeAtual = deslocamento / dt;
        if (jogador != null && estado != Estado.Caindo) jogador.Carregar(deslocamento);
        jogadorEmCimaAntes = jogador;

        // Na freada o tranco já foi dado: não soma o embalo de novo quando ele sair voando.
        if (freouAgora)
        {
            freouAgora = false;
            velocidadeAtual = Vector2.zero;
            jogadorEmCimaAntes = null;
        }
    }

    // O Subaru está em pé em cima de mim? (pela posição: os pés dele encostando no meu topo)
    Jogador JogadorEmCima()
    {
        if (colisor == null || !colisor.enabled || !GerenciadorDoJogo.JogadorVivo(out Vector2 _)) return null;
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        Vector2 p = jogador.Corpo.position;
        float pes = p.y - 0.5f;          // a caixa do Subaru termina meio bloco abaixo do centro dele
        float topo = posicao.y + 0.5f;
        bool emCima = Mathf.Abs(pes - topo) < 0.12f
                      && Mathf.Abs(p.x - posicao.x) < largura / 2f + 0.3f
                      && jogador.Corpo.linearVelocity.y < 1f; // subindo num pulo não conta
        return emCima ? jogador : null;
    }

    void Pisou()
    {
        afundado = 1f / FabricaDeSprites.PixelsPorUnidade; // afunda 1 pixel com o peso (só no desenho)
        GerenciadorDoJogo.Som("bloco", 0.25f);
        if (tipo != Tipo.Normal && estado == Estado.VaiEVolta && contagem < 0f && !tremendo)
            contagem = esperaAntesDoTruque; // começou a contagem... e não para mais, nem se você sair
    }

    // O relógio do vai e volta: avança o progresso (0 a 1), vira nas pontas e faz a pausinha.
    void AvancarRelogio(float dt)
    {
        if (alcance < 0.05f) return; // sem espaço para andar: fica parada
        if (pausa > 0f)
        {
            pausa -= dt;
            return;
        }
        progresso += sentido * dt * velocidade / alcance;
        if (progresso >= 1f)
        {
            progresso = 1f;
            sentido = -1;
            pausa = pausaNasPontas;
        }
        else if (progresso <= 0f)
        {
            progresso = 0f;
            sentido = 1;
            pausa = pausaNasPontas;
        }
    }

    // Onde ela está no vai e volta: sai devagar, acelera e freia na outra ponta (SmoothStep).
    float XDoVaiEVolta() => origem.x + alcance * Mathf.SmoothStep(0f, 1f, progresso);

    void ContarOTruque(float dt)
    {
        if (tremendo)
        {
            timerDoTremor -= dt;
            if (timerDoTremor > 0f) return;
            tremendo = false;
            if (tipo == Tipo.Despenca) ComecarAQueda();
            else ComecarODisparo();
            return;
        }
        if (contagem < 0f) return;
        contagem -= dt;
        if (contagem > 0f) return;

        // O aviso: ela treme e o cristal muda de cor (vermelho = vai cair, amarelo = vai disparar).
        contagem = -1f;
        tremendo = true;
        timerDoTremor = tempoTremendo;
        GerenciadorDoJogo.Som("armadilha", 0.8f);
    }

    // ---- 'J': despenca, some lá embaixo e reaparece no lugar

    void ComecarAQueda()
    {
        estado = Estado.Caindo;
        colisor.enabled = false; // quem estiver em cima cai junto
        velocidadeDaQueda = 0f;
        GerenciadorDoJogo.Som("risada", 0.7f);
        Efeitos.Poeira(transform.parent, (Vector3)posicao + Vector3.up * 0.4f, 6, 2f);
    }

    void Cair(float dt)
    {
        velocidadeDaQueda += gravidadeDaQueda * dt;
        posicao.y -= velocidadeDaQueda * dt;
        if (posicao.y > origem.y - 16f) return;
        estado = Estado.Escondida;
        timerDeEspera = tempoParaReaparecer;
        visual.gameObject.SetActive(false);
    }

    void EsperarParaReaparecer(float dt)
    {
        timerDeEspera -= dt;
        if (timerDeEspera > 0f) return;

        // Não reaparece "dentro" do Subaru (se ele estiver bem ali no ar, espera ele sair).
        float x = XDoVaiEVolta();
        if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)
            && Mathf.Abs(jogador.x - x) < largura / 2f + 0.45f && Mathf.Abs(jogador.y - origem.y) < 1.2f) return;

        estado = Estado.VaiEVolta;
        posicao = new Vector2(x, origem.y);
        corpo.position = posicao;       // teletransporte (não "voa" lá de baixo até aqui)
        transform.position = posicao;
        colisor.enabled = true;
        visual.gameObject.SetActive(true);
        aparecendo = 0f;
        GerenciadorDoJogo.Som("pop", 0.5f);
        Efeitos.Poeira(transform.parent, (Vector3)posicao, 6, 1.5f);
    }

    // ---- 'D': dispara até a parede, freia de repente, descansa e volta a ir e voltar dali

    void ComecarODisparo()
    {
        estado = Estado.Disparando;
        direcaoDoDisparo = sentido; // para o lado em que ela estava indo
        destinoDoDisparo = posicao.x + direcaoDoDisparo * EspacoLivre(direcaoDoDisparo, distanciaMaximaDoDisparo);
        velocidadeDoDisparoAtual = 0f;
        GerenciadorDoJogo.Som("mola");
    }

    void Disparar(float dt, Jogador jogador)
    {
        velocidadeDoDisparoAtual = Mathf.Min(velocidadeDoDisparoAtual + aceleracaoDoDisparo * dt, velocidadeDoDisparo);
        posicao.x += direcaoDoDisparo * velocidadeDoDisparoAtual * dt;

        // rastro de brilho atrás dela
        Vector3 traseira = (Vector3)posicao + new Vector3(-direcaoDoDisparo * largura / 2f, 0.2f, 0f);
        Particula.Criar(transform.parent, traseira, new Vector2(-direcaoDoDisparo * 2f, Random.Range(-0.5f, 0.5f)), 0.35f, 0.7f, CorDoBrilho);

        if ((posicao.x - destinoDoDisparo) * direcaoDoDisparo >= 0f)
        {
            posicao.x = destinoDoDisparo;
            Frear(jogador);
        }
    }

    // Para DE REPENTE: barulhão, tremidinha na câmera e o Subaru (se estiver em cima) é jogado para a frente.
    void Frear(Jogador jogador)
    {
        estado = Estado.Parada;
        timerDeEspera = descansoDepoisDoDisparo;
        freouAgora = true;
        GerenciadorDoJogo.Som("pancada");
        CameraSeguir.Tremer(0.25f, 0.25f);
        Vector3 frente = (Vector3)posicao + new Vector3(direcaoDoDisparo * largura / 2f, 0.3f, 0f);
        Efeitos.Poeira(transform.parent, frente, 10, 3f);
        if (jogador == null) return;
        jogador.Quicar(pulinhoDoTranco, false);
        jogador.Corpo.linearVelocity += new Vector2(direcaoDoDisparo * tranco, 0f);
        Conquistas.Desbloquear("expressa"); // confiou e foi até o fim sem pular
    }

    // Depois do descanso, volta a ir e voltar a partir de onde parou (e a pegadinha arma de novo).
    void EsperarParaRecomecar(float dt)
    {
        timerDeEspera -= dt;
        if (timerDeEspera > 0f) return;
        estado = Estado.VaiEVolta;
        origem = posicao;
        progresso = 0f;
        sentido = 1;
        pausa = pausaNasPontas;
        alcance = EspacoLivre(1, distanciaMaxima);
    }

    // Quanto a plataforma pode andar para um lado (direcao 1 ou -1) até encostar em algo sólido.
    // Olha na altura dela E na altura de quem está em cima (assim ela nunca espreme o Subaru numa parede).
    // Ignora o Subaru, os bichos e as outras plataformas (elas se atravessam).
    float EspacoLivre(int direcao, float maximo)
    {
        float menor = maximo;
        float borda = posicao.x + direcao * largura / 2f;
        float[] alturas = { -0.15f, 0.15f, 0.4f, 0.7f, 1.3f }; // 3 no corpo dela, 2 no corpo do Subaru
        foreach (float altura in alturas)
        {
            Vector2 inicio = new Vector2(borda, posicao.y + altura);
            int quantidade = Physics2D.Raycast(inicio, Vector2.right * direcao, filtroSolido, acertos, maximo);
            for (int i = 0; i < quantidade; i++)
            {
                Collider2D outro = acertos[i].collider;
                if (outro == colisor || outro.GetComponent<Jogador>() != null || Inimigo.EhBicho(outro)
                    || outro.GetComponent<PlataformaMovel>() != null) continue;
                menor = Mathf.Min(menor, acertos[i].distance);
            }
        }
        return menor;
    }

    // ------------------------------------------------------------------ desenho (a cada quadro)

    void Update()
    {
        if (visual == null || GerenciadorDoJogo.Pausado) return;

        // Afundadinha quando o Subaru aterrissa, voltando aos poucos. Tremendo: chacoalha.
        afundado = Mathf.MoveTowards(afundado, 0f, 0.4f * Time.deltaTime);
        Vector3 tremor = tremendo ? (Vector3)(Random.insideUnitCircle * 0.07f) : Vector3.zero;
        visual.localPosition = Vector3.down * afundado + tremor;

        // Reaparecendo: cresce do nada.
        aparecendo = Mathf.Min(1f, aparecendo + Time.deltaTime / 0.25f);
        visual.localScale = Vector3.one * Mathf.SmoothStep(0.2f, 1f, aparecendo);

        // O cristal balança devagar, pendurado.
        float onda = Mathf.Sin(Time.time * 3f + faseDaAnimacao);
        cristal.localPosition = new Vector3(0f, AlturaDoCristal + onda * 0.05f, 0f);

        // A cor do cristal é o AVISO: vermelho = vai cair, amarelo = vai disparar.
        bool avisando = tremendo || estado == Estado.Caindo || estado == Estado.Disparando;
        bool vermelho = tipo == Tipo.Despenca;
        desenhoDoCristal.sprite = !avisando ? cristalNormal : vermelho ? cristalVermelho : cristalAmarelo;
        if (luz != null)
        {
            luz.color = !avisando ? CorDaLuz : vermelho ? CorDoPerigo : CorDoDisparo;
            luz.intensity = avisando ? 0.9f : 0.33f + 0.05f * onda; // 0.33 = o brilho normal (0.6 x a força das luzinhas)
        }

        // De vez em quando cai um brilhinho mágico do cristal.
        timerDoBrilho -= Time.deltaTime;
        if (timerDoBrilho <= 0f && estado == Estado.VaiEVolta)
        {
            timerDoBrilho = Random.Range(0.3f, 0.6f);
            Vector3 ponta = cristal.position + new Vector3(Random.Range(-0.15f, 0.15f), -0.3f, 0f);
            Particula.Criar(transform.parent, ponta, new Vector2(0f, -0.7f), 0.7f, 0.45f, CorDoBrilho);
        }
    }

    // ------------------------------------------------------------------ desenhos (pixel art em texto)

    // Ponta esquerda: tábuas de madeira em cima de uma pedra flutuante com runa mágica (roxa).
    // A ponta direita é a mesma, espelhada.
    static readonly string[] ArteEsquerda =
    {
        "..kkkkkkkkkkkkkk",
        ".knnnnnnnnnnnnnn",
        "knnbbbbdbbbbbbbd",
        "knbbbbbdbbbbnbbd",
        "kbdddddddddddddd",
        "kkkkkkkkkkkkkkkk",
        ".kZagGmmmmmZZmmm",
        ".kZmGmmVmmmmmmMm",
        "..kmmmVPVmmmmMMm",
        "..kMmVPwPVmmMmmM",
        "...kMmVPVmMMmmMM",
        "...kkMMVMMMkMMMk",
        ".....kkMMMk.kMMk",
        ".......kkk...kk.",
        "................",
        "................",
    };

    static readonly string[] ArteMeio =
    {
        "kkkkkkkkkkkkkkkk",
        "nnnnnnnnnnnnnnnn",
        "bbbbbbbdbbbbbbbd",
        "bbbnbbbdbbbbbnbd",
        "dddddddddddddddd",
        "kkkkkkkkkkkkkkkk",
        "mmZZmmmagmZZmmmm",
        "mmmmmmMmGmmmmmMm",
        "mmmmmMMmmmmmmMMm",
        "MmmmmmmmMmmmmmmM",
        "MMmmmmmMMMmmmmMM",
        "kMMMMMMMkMMMMMMk",
        ".kMMMMMk.kMMMMk.",
        "..kkMMk...kMMk..",
        "....kk.....kk...",
        "................",
    };

    // O cristal pendurado (azul; na hora do aviso vira vermelho ou amarelo).
    static readonly string[] ArteCristal =
    {
        "...kk...",
        "..kZMk..",
        ".kkkkkk.",
        ".kwlllk.",
        "kwwllelk",
        "kwlllelk",
        "klllleek",
        ".klleek.",
        ".kleeek.",
        "..klek..",
        "..keek..",
        "...kk...",
    };

    // Ícone para o mapa do mundo: uma plataforma pequenininha com o cristal embaixo.
    static readonly string[] ArteIcone =
    {
        "................",
        "..kkkkkkkkkkkk..",
        ".knnnnnnnnnnnnk.",
        ".kbbbbbdbbbnbbk.",
        ".kddddddddddddk.",
        ".kkkkkkkkkkkkkk.",
        "..kZagmmmmZZmk..",
        "..kmGmVPVmmMMk..",
        "...kMmPwPmMMk...",
        "....kkMVMMkk....",
        "......kkkk......",
        "......kwlk......",
        ".....kwllek.....",
        ".....klleek.....",
        "......kleek.....",
        ".......kk.......",
    };

    static string[] Espelhar(string[] arte)
    {
        var resultado = new string[arte.Length];
        for (int i = 0; i < arte.Length; i++)
        {
            char[] letras = arte[i].ToCharArray();
            System.Array.Reverse(letras);
            resultado[i] = new string(letras);
        }
        return resultado;
    }

    // Plataforma de 1 bloco só: metade esquerda da ponta esquerda + metade direita da ponta direita.
    static string[] Unica()
    {
        string[] direita = Espelhar(ArteEsquerda);
        var resultado = new string[ArteEsquerda.Length];
        for (int i = 0; i < resultado.Length; i++)
            resultado[i] = ArteEsquerda[i].Substring(0, 8) + direita[i].Substring(8);
        return resultado;
    }

    // Troca as letras de cor (ex.: "le" -> "pr" troca azul claro por rosa e azul por vermelho).
    static string[] TrocarCores(string[] arte, string de, string para)
    {
        var resultado = new string[arte.Length];
        for (int i = 0; i < arte.Length; i++)
        {
            char[] letras = arte[i].ToCharArray();
            for (int j = 0; j < letras.Length; j++)
            {
                int indice = de.IndexOf(letras[j]);
                if (indice >= 0) letras[j] = para[indice];
            }
            resultado[i] = new string(letras);
        }
        return resultado;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        Vector2 centro = FabricaDeSprites.Centro;
        FabricaDeSprites.Registrar("plataforma", () => FabricaDeSprites.DeArte(ArteIcone, centro));
        FabricaDeSprites.Registrar("plataforma_esq", () => FabricaDeSprites.DeArte(ArteEsquerda, centro));
        FabricaDeSprites.Registrar("plataforma_meio", () => FabricaDeSprites.DeArte(ArteMeio, centro));
        FabricaDeSprites.Registrar("plataforma_dir", () => FabricaDeSprites.DeArte(Espelhar(ArteEsquerda), centro));
        FabricaDeSprites.Registrar("plataforma_unica", () => FabricaDeSprites.DeArte(Unica(), centro));
        FabricaDeSprites.Registrar("plataforma_cristal", () => FabricaDeSprites.DeArte(ArteCristal, centro));
        FabricaDeSprites.Registrar("plataforma_cristal_vermelho", () => FabricaDeSprites.DeArte(TrocarCores(ArteCristal, "le", "pr"), centro));
        FabricaDeSprites.Registrar("plataforma_cristal_amarelo", () => FabricaDeSprites.DeArte(TrocarCores(ArteCristal, "le", "yo"), centro));
    }
}
