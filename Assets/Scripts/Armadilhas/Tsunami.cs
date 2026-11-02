using System.Collections.Generic;
using UnityEngine;

// 'N' - o TSUNAMI DO KRAKEN: uma ONDA GIGANTE (o Kraken Vermelho, lá do fundo do mar, levantou ela atrás de você)
// que PERSEGUE o Grumete pela fase inteira. Encostou nela, morreu.
//  - O 'N' marca a coluna de onde ela começa, atrás do jogador. Ela fica "parada" (a água só borbulhando)
//    até o jogador se afastar uns 4 blocos; aí ela RUGE, a tela treme e ela começa a andar para a direita.
//  - Ela anda a 5,2 blocos por segundo e o Grumete anda 7: correndo sem parar você abre vantagem.
//    Mas cada vez que você PARA para pensar (ou erra um pulo e tem que voltar) é tempo que ela recupera:
//    ficou uns 2 segundos parado, ela te pega. Por isso a fase dela é só CORRER e PULAR (parkour).
//  - "Elástico": se você abrir vantagem demais, ela acelera. Correndo perfeito ela fica uns 11 blocos atrás
//    (bem na beirada da tela) e NUNCA mais de 14. Não dá para "guardar" vantagem para depois.
//  - Quando você fica perto dela (3,5 blocos), um JATO d'água sai da onda para te pegar. O aviso é ele
//    recuar para dentro da onda e piscar (0,35 s). Corra ou pule por cima!
//  - Renasceu num checkpoint? A onda nasce 9 blocos ATRÁS de você (senão o checkpoint não serviria de nada).
//  - Ela para antes da bandeira de verdade. Passou de fase? Ela quebra e vai embora... por enquanto.
//
// O desenho é todo feito de pedaços (todos filhos do objeto "Parede", que anda junto com a frente):
//  fundo azul-escuro liso + faixa de água com riscos de luz subindo + ESPUMA na borda (troca de forma o tempo todo)
//  + a CRISTA enrolando lá em cima + destroços boiando (barris, tábuas, um chapéu de pirata) + os jatos d'água.
//  Na frente, respingos (partículas). Uma vinheta azul presa na câmera escurece a tela quando ela chega perto
//  (junto com um "coração" batendo).
[DefaultExecutionOrder(100)] // roda depois da CameraSeguir: a vinheta acompanha a câmera sem tremer
public class Tsunami : MonoBehaviour
{
    enum Estado { Dormindo, Acordando, Perseguindo, IndoEmbora }  // ("dormindo" = parada, só borbulhando)
    enum EstadoDaMao { Balancando, Preparando, Agarrando, Voltando }

    [Header("Perseguição")]
    public float velocidade = 5.2f;           // blocos por segundo (o Grumete anda 7)
    public float distanciaParaAcordar = 4f;   // acorda quando o jogador se afasta isso da frente dela
    public float inicioDoElastico = 9f;       // mais longe que isso, ela acelera...
    public float forcaDoElastico = 0.8f;      // ...0,8 bloco/s a mais para cada bloco de distância a mais
    public float distanciaMaxima = 14f;       // e ela NUNCA fica mais longe que isso
    public float distanciaAoRenascer = 9f;    // renasceu num checkpoint? Ela aparece isso atrás de você
    public float tempoAcordando = 0.7f;       // a "espreguiçada" (risada) antes de começar a andar

    [Header("Jatos d'água")]
    public float distanciaDoBote = 3.5f;      // jogador mais perto que isso da frente: um jato tenta te pegar
    public float tempoDePreparo = 0.35f;      // o aviso: o jato recua e pisca
    public float alcanceDoBote = 2.6f;        // até onde o jato estica (a partir da frente da onda)
    public float recargaDaMao = 0.9f;         // descanso de cada jato depois de um bote

    // A "linha da morte" é a frente da parede. O colisor do Grumete tem uns 0,36 de cada lado do centro.
    const float MeiaLarguraDoJogador = 0.36f;

    // Até onde a parede vai na vertical (a câmera mostra de -0,5 a 14,5 nas fases de 15 linhas).
    const float Baixo = -1.5f, Alto = 16.5f;

    // Ordem de desenho: tudo bem na frente da fase (a onda "engole" o cenário), atrás só da interface.
    const int OrdemDoFundo = 38, OrdemDaNevoa = 39, OrdemDosDestrocos = 40, OrdemDosBracos = 41,
              OrdemDaBorda = 42, OrdemDaCrista = 43, OrdemDasMaos = 44, OrdemDaVinheta = 60;

    const int QuantidadeDeBolhas = 4;  // variações do desenho da espuma da borda ("tsunami_borda_0" a "_3")
    const float LarguraDaNevoa = 8f, AlturaDaNevoa = 4f;
    const int GomosDoBraco = 8;

    // Cores dos respingos (as partículas usam o desenho da poeira pintado com essas cores).
    static readonly Color FumacaClara = new Color(0.92f, 0.98f, 1f, 0.9f);   // espuma
    static readonly Color FumacaEscura = new Color(0.45f, 0.72f, 0.95f, 0.85f); // água
    static readonly Color CorDoAviso = new Color(1f, 0.6f, 0.6f);
    static readonly Color LuzDaAgua = new Color(0.5f, 0.85f, 1f);

    // Um jato d'água: a ponta de espuma (que mata), as "gotas" do corpo e o que ele está fazendo agora.
    class Mao
    {
        public Transform mao;
        public SpriteRenderer desenho;
        public readonly List<Transform> gomos = new List<Transform>();
        public float alturaBase, fase;     // onde ela fica na parede e o "ritmo" dela (cada uma mexe diferente)
        public EstadoDaMao estado;
        public float timer, recarga;
        public float alcance, altura;      // posição atual da ponta (em relação à frente da parede)
        public float alturaDoBote;         // a altura em que ela vai dar o bote (a do jogador)
        public bool fechada;               // já se enrolou neste bote?
    }

    // Um destroço boiando dentro da onda (barril, tábua, chapéu...): sobe e desce, gira e passa.
    class Destroco
    {
        public SpriteRenderer desenho;
        public Vector2 lugar;  // em relação à frente da onda
        public float fase, giro;
    }

    Estado estado = Estado.Dormindo;
    float timer;
    float frente;          // x da frente da parede (a linha da morte)
    float xDoN;            // a coluna do 'N' no mapa (ela nunca nasce antes disso)
    float limite = float.MaxValue; // ela para antes da bandeira de verdade
    float xOndeNasceu;     // onde o jogador estava quando a parede (re)nasceu
    float ultimoX;         // x do jogador no quadro anterior (para perceber teletransportes)
    float relogio;         // relógio próprio das animações (congela na pausa, como o Time.deltaTime)
    float timerFumaca, timerRuido, timerBatida;
    float batida;          // 1 = o "coração" acabou de bater (a vinheta pulsa), cai até 0
    bool jaSussurrou;      // o "Glub, glub..." aparece uma vez por tentativa

    Transform parede;
    readonly List<SpriteRenderer> bolhas = new List<SpriteRenderer>();
    readonly List<Transform> nevoas = new List<Transform>();
    readonly List<Destroco> destrocos = new List<Destroco>();
    SpriteRenderer crista;
    readonly List<Mao> maos = new List<Mao>();
    SpriteRenderer vinheta;

    // ------------------------------------------------------------------ montagem

    void Awake()
    {
        parede = new GameObject("Parede").transform;
        parede.SetParent(transform, false);

        // Fundo: um retângulo azul-escuro liso, comprido para a esquerda (você nunca vê o fim dele).
        var fundo = ConstrutorDeFase.Visual("Fundo", parede, transform.position, "tsunami_fundo", OrdemDoFundo, false).transform;
        fundo.localPosition = new Vector3(-LarguraDaNevoa - 25f, (Baixo + Alto) / 2f, 0f);
        fundo.localScale = new Vector3(52f, Alto - Baixo, 1f);

        // Faixa de água logo atrás da borda, com riscos de luz que sobem pela onda (o desenho se repete na vertical).
        for (int i = 0; i < 6; i++)
            nevoas.Add(ConstrutorDeFase.Visual("Agua", parede, transform.position, "tsunami_agua", OrdemDaNevoa, false).transform);

        // Borda: a espuma, empilhada, uma por bloco de altura. Ela treme e troca de forma.
        for (float y = Baixo; y <= Alto; y += 1f)
        {
            var bolha = ConstrutorDeFase.Visual("Espuma", parede, transform.position, "tsunami_borda_0", OrdemDaBorda, false);
            bolha.transform.localPosition = new Vector3(0f, y, 0f);
            bolhas.Add(bolha.GetComponent<SpriteRenderer>());
        }

        // A crista lá em cima, enrolando para a frente (só enfeite: quem mata é a frente da onda e os jatos).
        crista = ConstrutorDeFase.Visual("Crista", parede, transform.position, "tsunami_crista", OrdemDaCrista, false).GetComponent<SpriteRenderer>();

        // Destroços que a onda arrastou, boiando lá dentro.
        (string desenho, Vector2 lugar)[] lugares =
        {
            ("tsunami_barril", new Vector2(-1.6f, 2.6f)), ("tsunami_tabua", new Vector2(-3.4f, 6.2f)),
            ("tsunami_chapeu", new Vector2(-2f, 9.4f)), ("tsunami_tabua", new Vector2(-3.8f, 12.4f)), ("tsunami_barril", new Vector2(-4.6f, 4.3f)),
        };
        for (int i = 0; i < lugares.Length; i++)
        {
            var destroco = ConstrutorDeFase.Visual("Destroco", parede, transform.position, lugares[i].desenho, OrdemDosDestrocos, false);
            destroco.transform.localPosition = lugares[i].lugar;
            destrocos.Add(new Destroco { desenho = destroco.GetComponent<SpriteRenderer>(), lugar = lugares[i].lugar, fase = i * 1.9f,
                                         giro = (i % 2 == 0 ? 1f : -1f) * Random.Range(40f, 90f) });
        }

        // Os jatos, em alturas diferentes (o de baixo pega quem corre no chão; os outros, quem pula).
        float[] alturas = { 1.9f, 4.6f, 7.4f, 10.2f, 13f };
        for (int i = 0; i < alturas.Length; i++) maos.Add(CriarMao(alturas[i], i * 2.1f));

        // Luz azulada ao longo da frente (só existe nas fases escuras: Luzes.Ponto devolve null de dia).
        for (float y = 2f; y < 14f; y += 5f)
            Luzes.Ponto(parede, LuzDaAgua, 5f, 0.9f, new Vector3(0.3f, y, 0f));

        // A vinheta: um desenho do tamanho da tela, preso na câmera (veja LateUpdate). Começa invisível.
        vinheta = ConstrutorDeFase.Visual("Vinheta", transform, transform.position, "tsunami_vinheta", OrdemDaVinheta, false).GetComponent<SpriteRenderer>();
        vinheta.color = new Color(1f, 1f, 1f, 0f);

        frente = xDoN = transform.position.x;
    }

    Mao CriarMao(float altura, float fase)
    {
        var mao = new Mao { alturaBase = altura, altura = altura, fase = fase };
        for (int i = 0; i < GomosDoBraco; i++)
            mao.gomos.Add(ConstrutorDeFase.Visual("Gota", parede, transform.position, "tsunami_gota", OrdemDosBracos, false).transform);

        var objeto = ConstrutorDeFase.Visual("Jato", parede, transform.position, "tsunami_jato", OrdemDasMaos, false);
        mao.mao = objeto.transform;
        mao.desenho = objeto.GetComponent<SpriteRenderer>();

        // Só a PONTA mata (as gotas são só enfeite). Como ela se mexe, precisa do corpo cinemático.
        var area = objeto.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 0.33f;
        objeto.AddComponent<Perigo>().conquista = "mare_negra"; // (o id antigo da conquista: quem já tinha continua tendo)
        Perigo.TornarMovel(objeto);
        Luzes.Ponto(mao.mao, LuzDaAgua, 1.4f, 0.5f);
        return mao;
    }

    void Start()
    {
        // O jogador é criado DEPOIS da fase (no checkpoint, se tiver um), então só dá para olhar onde ele está aqui.
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        Reposicionar(jogador != null ? jogador.transform.position.x : xDoN + distanciaParaAcordar);
        limite = AcharLimite();
    }

    // Coloca a parede atrás do jogador e faz ela "dormir" de novo.
    void Reposicionar(float xDoJogador)
    {
        // Renasceu num checkpoint lá na frente? A parede nasce ATRÁS do jogador, não lá no 'N'.
        frente = Mathf.Max(xDoN, xDoJogador - distanciaAoRenascer);
        frente = Mathf.Min(frente, xDoJogador - 2f); // nunca em cima do jogador (se o 'N' estiver no lugar errado)
        xOndeNasceu = ultimoX = xDoJogador;
        estado = Estado.Dormindo;
        jaSussurrou = false;
        parede.position = new Vector3(frente, 0f, 0f);
    }

    // A bandeira de verdade mais à direita (para a fujona, o lugar para onde ela foge). Sem bandeira: sem limite.
    float AcharLimite()
    {
        float maisLonge = float.MinValue;
        foreach (Transform bandeira in GerenciadorDoJogo.Instancia.Bandeiras)
        {
            if (bandeira == null) continue;
            var fujona = bandeira.GetComponent<BandeiraFujona>();
            if (fujona != null && fujona.destino.HasValue) maisLonge = Mathf.Max(maisLonge, fujona.destino.Value.x);
            else if (bandeira.TryGetComponent<Bandeira>(out _)) maisLonge = Mathf.Max(maisLonge, bandeira.position.x);
        }
        return maisLonge > float.MinValue ? maisLonge - 1.5f : float.MaxValue;
    }

    // ------------------------------------------------------------------ a cada quadro

    void Update()
    {
        float dt = Time.deltaTime; // na pausa é 0: tudo congela sozinho
        relogio += dt;
        bool vivo = GerenciadorDoJogo.JogadorVivo(out Vector2 jogador);

        // O jogador sumiu de um lugar e apareceu em outro (porta, bandeira que manda de volta...)?
        // A onda reaparece atrás dele, como num checkpoint.
        if (vivo && Mathf.Abs(jogador.x - ultimoX) > 3f) Reposicionar(jogador.x);
        if (vivo) ultimoX = jogador.x;
        float distancia = vivo ? jogador.x - frente : 99f;

        switch (estado)
        {
            case Estado.Dormindo:
                // acorda quando o jogador já andou um pouco E está longe o bastante
                if (vivo && distancia > distanciaParaAcordar && Mathf.Abs(jogador.x - xOndeNasceu) > 0.75f) Acordar();
                break;

            case Estado.Acordando:
                timer -= dt;
                if (timer <= 0f) estado = Estado.Perseguindo;
                break;

            case Estado.Perseguindo:
                Perseguir(dt, vivo, jogador);
                break;

            case Estado.IndoEmbora:
                frente -= 8f * dt; // passou de fase: ela recua e sai da tela
                break;
        }

        parede.position = new Vector3(frente, 0f, 0f);
        AnimarBorda();
        AnimarNevoa();
        AnimarDestrocos();
        AnimarCrista();
        AnimarMaos(dt, vivo, jogador, distancia);
        SoltarFumaca(dt);
        TocarSons(dt, distancia);
    }

    void Acordar()
    {
        estado = Estado.Acordando;
        timer = tempoAcordando;
        GerenciadorDoJogo.Som("risada");
        GerenciadorDoJogo.Som("rugido", 0.45f);
        CameraSeguir.Tremer(0.25f, 0.5f);
        if (GerenciadorDoJogo.Instancia.MortesNaFase < 3) GerenciadorDoJogo.Instancia.Avisar("CORRE!", 1.2f);
        // um "estouro" de espuma e todos os jatos se esticam de uma vez
        EstouroDeFumaca(24);
        foreach (Mao mao in maos) mao.alcance = 1.1f;
    }

    void Perseguir(float dt, bool vivo, Vector2 jogador)
    {
        if (!vivo)
        {
            // Passou de fase: ela desiste. Morreu: ela continua andando devagar e "engole" o que sobrou.
            // (pausado também cai aqui, mas aí o dt é 0 e nada acontece)
            EstadoDoJogo estadoDoJogo = GerenciadorDoJogo.Instancia.Estado;
            if (estadoDoJogo == EstadoDoJogo.FaseConcluida) IrEmbora();
            else if (estadoDoJogo == EstadoDoJogo.Morreu) frente = Mathf.Min(frente + velocidade * 0.5f * dt, limite);
            return;
        }

        // O "elástico": quanto mais longe o jogador está, mais rápido ela anda.
        float distancia = jogador.x - frente;
        float velocidadeAgora = velocidade;
        if (distancia > inicioDoElastico) velocidadeAgora += (distancia - inicioDoElastico) * forcaDoElastico;
        frente += velocidadeAgora * dt;
        frente = Mathf.Max(frente, jogador.x - distanciaMaxima); // nunca mais longe que a distância máxima
        frente = Mathf.Min(frente, limite);                       // e nunca passa da bandeira

        if (jogador.x - MeiaLarguraDoJogador < frente) Abracar();
        else if (distancia < 2.5f && !jaSussurrou)
        {
            jaSussurrou = true;
            GerenciadorDoJogo.Instancia.Avisar("Glub, glub...", 1f); // o tsunami está BEM perto
        }
    }

    // Encostou na onda: ela te engole. Todos os jatos estouram de uma vez.
    void Abracar()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || jogador.Morto) return;
        jogador.Morrer();
        if (!jogador.Morto) return;
        Conquistas.Desbloquear("mare_negra");
        GerenciadorDoJogo.Som("rugido", 0.6f);
        CameraSeguir.Tremer(0.3f, 0.35f);
        foreach (Mao mao in maos)
        {
            mao.estado = EstadoDaMao.Voltando;
            mao.timer = 0.8f;
            mao.desenho.sprite = FabricaDeSprites.Pegar("tsunami_jato_estourado");
        }
        EstouroDeFumaca(16);
    }

    void IrEmbora()
    {
        estado = Estado.IndoEmbora;
        EstouroDeFumaca(30); // a onda quebra
    }

    // ------------------------------------------------------------------ animações

    // A espuma da borda treme para os lados e troca de forma umas 3 vezes por segundo (cada pedaço no seu ritmo).
    // Acordando, ela "infla" e treme mais.
    void AnimarBorda()
    {
        float agitacao = estado == Estado.Acordando ? 2.5f : estado == Estado.Dormindo ? 0.5f : 1f;
        for (int i = 0; i < bolhas.Count; i++)
        {
            SpriteRenderer bolha = bolhas[i];
            float x = (0.14f * Mathf.Sin(relogio * 2.3f + i * 1.7f) + 0.07f * Mathf.Sin(relogio * 5.1f + i * 0.6f)) * agitacao;
            Vector3 p = bolha.transform.localPosition;
            bolha.transform.localPosition = new Vector3(x, p.y, 0f);
            bolha.transform.localScale = new Vector3(1f + 0.12f * Mathf.Sin(relogio * 3.1f + i * 2.3f), 1f, 1f); // "respirando"
            int forma = (i * 3 + Mathf.FloorToInt(relogio * 3f * agitacao + i * 0.37f)) % QuantidadeDeBolhas;
            bolha.sprite = FabricaDeSprites.Pegar("tsunami_borda_" + forma);
            bolha.flipY = i % 2 == 1; // de ponta-cabeça a cada duas: parece que tem mais formas
        }
    }

    // A água sobe pela onda: os pedaços andam para cima e o de cima volta para baixo (o desenho se repete).
    void AnimarNevoa()
    {
        float sobe = Mathf.Repeat(relogio * (estado == Estado.Dormindo ? 0.6f : 2.2f), AlturaDaNevoa);
        for (int i = 0; i < nevoas.Count; i++)
        {
            float y = Baixo - AlturaDaNevoa + AlturaDaNevoa / 2f + i * AlturaDaNevoa + sobe;
            nevoas[i].localPosition = new Vector3(-LarguraDaNevoa / 2f - 0.2f, y, 0f);
        }
    }

    // Os destroços sobem e descem devagar, girando (parada, a onda só balança eles).
    void AnimarDestrocos()
    {
        float agitacao = estado == Estado.Dormindo ? 0.4f : 1f;
        foreach (Destroco destroco in destrocos)
        {
            Vector2 lugar = destroco.lugar + new Vector2(0.35f * Mathf.Sin(relogio * 0.8f + destroco.fase),
                                                         0.6f * Mathf.Sin(relogio * 1.3f * agitacao + destroco.fase * 2f));
            destroco.desenho.transform.localPosition = lugar;
            destroco.desenho.transform.localRotation = Quaternion.Euler(0f, 0f, relogio * destroco.giro * agitacao);
        }
    }

    // A crista fica lá no alto da onda e "respira": cresce um pouco para a frente e volta, enrolando.
    void AnimarCrista()
    {
        float onda = Mathf.Sin(relogio * (estado == Estado.Dormindo ? 1.2f : 3f));
        crista.transform.localPosition = new Vector3(0.15f + 0.2f * onda, 12.6f + 0.15f * onda, 0f);
        crista.transform.localScale = new Vector3(1f + 0.06f * onda, 1f, 1f);
    }

    void AnimarMaos(float dt, bool vivo, Vector2 jogador, float distancia)
    {
        // Jogador perto da onda e nenhum jato no meio de um bote? O mais perto da altura dele ataca.
        if (estado == Estado.Perseguindo && vivo && distancia < distanciaDoBote && !AlgumaMaoAtacando())
        {
            Mao escolhida = null;
            foreach (Mao mao in maos)
                if (mao.estado == EstadoDaMao.Balancando && mao.recarga <= 0f
                    && (escolhida == null || Mathf.Abs(mao.alturaBase - jogador.y) < Mathf.Abs(escolhida.alturaBase - jogador.y)))
                    escolhida = mao;
            if (escolhida != null && Mathf.Abs(escolhida.alturaBase - jogador.y) < 2f) Preparar(escolhida, jogador.y);
        }

        foreach (Mao mao in maos)
        {
            mao.timer -= dt;
            mao.recarga -= dt;
            Color cor = Color.white;

            switch (mao.estado)
            {
                case EstadoDaMao.Balancando:
                    // vai e volta devagar, com a espuma para fora da onda (parada, quase todo escondido)
                    float onda = 0.5f + 0.5f * Mathf.Sin(relogio * 1.3f + mao.fase);
                    float alvo = estado == Estado.Dormindo ? -0.2f + 0.3f * onda : 0.05f + 0.35f * onda;
                    if (estado == Estado.Acordando) alvo = 0.6f + 0.5f * Mathf.Sin(relogio * 14f + mao.fase); // se debatendo
                    mao.alcance = Mathf.Lerp(mao.alcance, alvo, 6f * dt);
                    mao.altura = Mathf.Lerp(mao.altura, mao.alturaBase + 0.25f * Mathf.Sin(relogio * 0.9f + mao.fase * 2f), 4f * dt);
                    break;

                case EstadoDaMao.Preparando:
                    // O AVISO: recua para dentro da onda, mira na altura do jogador e pisca
                    mao.alcance = Mathf.Lerp(mao.alcance, -0.7f, 12f * dt);
                    mao.altura = Mathf.Lerp(mao.altura, mao.alturaDoBote, 10f * dt);
                    if ((mao.timer * 14f) % 2f < 1f) cor = CorDoAviso;
                    if (mao.timer <= 0f)
                    {
                        mao.estado = EstadoDaMao.Agarrando;
                        mao.timer = 0.3f;
                    }
                    break;

                case EstadoDaMao.Agarrando:
                    // estica rapidinho (0,1 s), segura esticado e estoura no fim
                    float quanto = Mathf.SmoothStep(0f, 1f, (0.3f - mao.timer) / 0.1f);
                    mao.alcance = Mathf.Lerp(-0.7f, alcanceDoBote, quanto);
                    if (mao.timer < 0.12f && !mao.fechada)
                    {
                        mao.fechada = true;
                        mao.desenho.sprite = FabricaDeSprites.Pegar("tsunami_jato_estourado");
                        GerenciadorDoJogo.Som("bloco", 0.5f);
                        Fumaca(mao.alcance + 0.4f, mao.altura, 4);
                    }
                    if (mao.timer <= 0f)
                    {
                        mao.estado = EstadoDaMao.Voltando;
                        mao.timer = 0.45f;
                    }
                    break;

                case EstadoDaMao.Voltando:
                    mao.alcance = Mathf.Lerp(mao.alcance, 0.1f, 7f * dt);
                    if (mao.timer <= 0f)
                    {
                        mao.estado = EstadoDaMao.Balancando;
                        mao.recarga = recargaDaMao;
                        mao.desenho.sprite = FabricaDeSprites.Pegar("tsunami_jato");
                    }
                    break;
            }

            mao.desenho.color = cor;
            PosicionarMao(mao);
        }
    }

    bool AlgumaMaoAtacando()
    {
        foreach (Mao mao in maos)
            if (mao.estado == EstadoDaMao.Preparando || mao.estado == EstadoDaMao.Agarrando) return true;
        return false;
    }

    void Preparar(Mao mao, float alturaDoJogador)
    {
        mao.estado = EstadoDaMao.Preparando;
        mao.timer = tempoDePreparo;
        mao.alturaDoBote = Mathf.Clamp(alturaDoJogador, mao.alturaBase - 1.6f, mao.alturaBase + 1.6f);
        mao.fechada = false;
        mao.desenho.sprite = FabricaDeSprites.Pegar("tsunami_jato");
        GerenciadorDoJogo.Som("armadilha", 0.35f);
    }

    // A ponta fica no fim do jato; o resto é uma fileira de "gotas" que sai de dentro da onda
    // e ondula um pouquinho (as gotas de perto da ponta são menores).
    void PosicionarMao(Mao mao)
    {
        Vector2 raiz = new Vector2(-0.8f, mao.alturaBase); // escondido embaixo da espuma
        Vector2 ponta = new Vector2(mao.alcance, mao.altura);
        Vector2 pulso = ponta - new Vector2(0.4f, 0f);
        Vector2 direcao = (pulso - raiz).normalized;
        Vector2 deLado = new Vector2(-direcao.y, direcao.x);

        for (int i = 0; i < mao.gomos.Count; i++)
        {
            float t = (i + 1f) / (mao.gomos.Count + 1f);
            float ondula = Mathf.Sin(t * Mathf.PI) * 0.18f * Mathf.Sin(relogio * 4f + mao.fase + i);
            mao.gomos[i].localPosition = Vector2.Lerp(raiz, pulso, t) + deLado * ondula;
            mao.gomos[i].localScale = Vector3.one * Mathf.Lerp(1.1f, 0.75f, t);
        }

        float angulo = Mathf.Atan2(direcao.y, direcao.x) * Mathf.Rad2Deg;
        float mexe = mao.estado == EstadoDaMao.Balancando ? 10f * Mathf.Sin(relogio * 2.2f + mao.fase) : 0f; // a espuma balançando
        mao.mao.localPosition = ponta;
        mao.mao.localRotation = Quaternion.Euler(0f, 0f, angulo + mexe);
    }

    // Respingos saindo da frente da onda (menos enquanto ela está parada).
    void SoltarFumaca(float dt)
    {
        if (estado == Estado.IndoEmbora) return;
        timerFumaca -= dt;
        if (timerFumaca > 0f) return;
        timerFumaca = estado == Estado.Dormindo ? 0.15f : 0.04f;
        Fumaca(Random.Range(-0.2f, 0.4f), Random.Range(-0.5f, 14.5f), 1);
    }

    // Solta "quantidade" respingos perto de um ponto (x em relação à frente da onda). Eles voam para a frente e caem.
    void Fumaca(float x, float y, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var posicao = new Vector3(frente + x, y, 0f) + (Vector3)(Random.insideUnitCircle * 0.2f);
            var velocidadeDaFumaca = new Vector2(Random.Range(0.8f, 3.2f), Random.Range(-1.2f, 0.6f));
            Color cor = Random.value < 0.5f ? FumacaClara : FumacaEscura;
            Particula.Criar(transform.parent, posicao, velocidadeDaFumaca, Random.Range(0.4f, 0.9f), Random.Range(0.9f, 1.8f), cor);
        }
    }

    // O "estouro" de quando ela acorda (ou quebra): espuma (e mais rápida) em toda a altura.
    void EstouroDeFumaca(int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var posicao = new Vector3(frente + Random.Range(-0.3f, 0.3f), Random.Range(0f, 14f), 0f);
            var velocidadeDaFumaca = new Vector2(Random.Range(1.5f, 5f), Random.Range(-0.5f, 1.5f));
            Particula.Criar(transform.parent, posicao, velocidadeDaFumaca, Random.Range(0.5f, 1f), 1.4f, FumacaClara);
        }
    }

    // Um rugido baixinho de tempos em tempos e, quando ela está perto, um coração batendo (cada vez mais rápido).
    void TocarSons(float dt, float distancia)
    {
        batida = Mathf.Max(0f, batida - 3f * dt);
        if (estado != Estado.Perseguindo || GerenciadorDoJogo.Instancia.Estado != EstadoDoJogo.Jogando) return;

        float perto = Mathf.InverseLerp(12f, 1f, distancia); // 0 = longe, 1 = encostando
        timerRuido -= dt;
        if (timerRuido <= 0f)
        {
            timerRuido = Random.Range(2.2f, 4f);
            GerenciadorDoJogo.Som("rugido", Mathf.Lerp(0.08f, 0.3f, perto));
        }

        if (distancia > 6f) return;
        timerBatida -= dt;
        if (timerBatida > 0f) return;
        timerBatida = Mathf.Lerp(0.3f, 0.7f, distancia / 6f);
        batida = 1f;
        GerenciadorDoJogo.Som("pancada", Mathf.Lerp(0.35f, 0.12f, distancia / 6f));
        if (distancia < 3f) CameraSeguir.Tremer(0.04f, 0.1f);
    }

    // A vinheta acompanha a câmera e escurece a tela quanto mais perto a onda está (pulsando com o "coração").
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null || vinheta == null) return;
        float altura = cam.orthographicSize * 2f;
        vinheta.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        vinheta.transform.localScale = new Vector3(altura * cam.aspect / 8f, altura / 4.5f, 1f); // o desenho tem 8 x 4,5 blocos

        float alvo = 0f;
        if (estado == Estado.Perseguindo || estado == Estado.Acordando)
        {
            float x = GerenciadorDoJogo.JogadorAtual != null ? GerenciadorDoJogo.JogadorAtual.transform.position.x : frente;
            alvo = Mathf.InverseLerp(10f, 1.5f, x - frente) * (0.8f + 0.2f * batida);
        }
        Color cor = vinheta.color;
        cor.a = Mathf.MoveTowards(cor.a, alvo, 2f * Time.deltaTime);
        vinheta.color = cor;
    }

    // ------------------------------------------------------------------ desenhos

    // As cores da água (do fundo da onda até a espuma).
    static readonly Color32 Fundo = new Color32(22, 56, 120, 255);      // o corpo da onda
    static readonly Color32 Meio = new Color32(40, 92, 165, 255);       // degradê
    static readonly Color32 Clara = new Color32(78, 146, 214, 255);     // riscos de luz
    static readonly Color32 Brilho = new Color32(158, 214, 246, 255);   // o brilho mais forte
    static readonly Color32 Espuma = new Color32(244, 251, 255, 255);   // espuma
    static readonly Color32 EspumaSombra = new Color32(196, 230, 248, 255);
    static readonly Color32 Contorno = new Color32(12, 30, 70, 255);    // contorno azul-marinho (a água não tem contorno preto)
    static readonly Color32 Funda = new Color32(10, 28, 66, 255);       // o "tubo" escuro dentro da crista

    // Registra os desenhos na FabricaDeSprites (Pegar("tsunami") funciona em qualquer lugar, até no mapa do mundo).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        Vector2 centro = FabricaDeSprites.Centro;
        FabricaDeSprites.Registrar("tsunami", () => FabricaDeSprites.DeArte(ArteIcone, centro));
        FabricaDeSprites.Registrar("tsunami_jato", () => FabricaDeSprites.DeArte(ArteJato, centro));
        FabricaDeSprites.Registrar("tsunami_jato_estourado", () => FabricaDeSprites.DeArte(ArteJatoEstourado, centro));
        FabricaDeSprites.Registrar("tsunami_gota", () => FabricaDeSprites.DeArte(ArteGota, centro));
        FabricaDeSprites.Registrar("tsunami_barril", () => FabricaDeSprites.DeArte(ArteBarril, centro));
        FabricaDeSprites.Registrar("tsunami_tabua", () => FabricaDeSprites.DeArte(ArteTabua, centro));
        FabricaDeSprites.Registrar("tsunami_chapeu", () => FabricaDeSprites.DeArte(ArteChapeu, centro));
        FabricaDeSprites.Registrar("tsunami_fundo", () => FabricaDeSprites.Procedural(16, 16, centro, (x, y) => Fundo));
        FabricaDeSprites.Registrar("tsunami_agua", () => FabricaDeSprites.Procedural(128, 64, centro, CorDaAgua));
        FabricaDeSprites.Registrar("tsunami_vinheta", () => FabricaDeSprites.Procedural(128, 72, centro, CorDaVinheta));
        // a crista: o pivô fica na base, no pixel 4 (onde ela encosta na frente da onda)
        FabricaDeSprites.Registrar("tsunami_crista", () => FabricaDeSprites.Procedural(48, 40, new Vector2(4f / 48f, 0f), CorDaCrista));

        // A borda: o pivô fica no pixel 15 de 24 (a "frente" da onda passa no meio da espuma).
        var pivoDaBorda = new Vector2(15f / 24f, 0.5f);
        for (int i = 0; i < QuantidadeDeBolhas; i++)
        {
            int forma = i;
            FabricaDeSprites.Registrar("tsunami_borda_" + forma, () => FabricaDeSprites.Procedural(24, 24, pivoDaBorda, (x, y) => CorDaBorda(x, y, forma)));
        }
    }

    // Ícone (mapa do mundo): a onda gigante enrolando, com a espuma em cima.
    static readonly string[] ArteIcone =
    {
        "........kkkkkk......",
        "......kkwwwwwwkk....",
        ".....kwwllllllwwk...",
        "....kwlEEEEEEEllwk..",
        "...kwlEEBBBBBEEElwk.",
        "...kwEEBBkkkkBBEEwk.",
        "..kwlEBBk....kkBEwk.",
        "..kwEEBk.......kwwk.",
        "..kwEBBk........kk..",
        ".kwlEBBk............",
        ".kwEEBBBk...........",
        "kwlEEBBBBkk.........",
        "kwEEEBBBBBBkk...k...",
        "kwEEEEBBBBBBBkkkwk..",
        "klllEEEEEBBBBBBBwwk.",
        "kwwwwlllllEEEEEEllwk",
        "kkkkkkkkkkkkkkkkkkkk",
    };

    // A ponta do jato d'água, indo para a direita: espuma na frente, água azul atrás.
    static readonly string[] ArteJato =
    {
        "....kkkk........",
        "..kkwwwwkk......",
        ".kwwlwwwwwk.....",
        "kBElllwwwwwk....",
        "kBEEllllwwwwk...",
        "kBBEEElllwwwk...",
        "kBBEEElllwwwk...",
        "kBEEllllwwwwk...",
        "kBElllwwwwwk....",
        ".kwwlwwwwwk.....",
        "..kkwwwwkk......",
        "....kkkk........",
    };

    // O jato estourando (pegou ou errou): um "splash" de espuma.
    static readonly string[] ArteJatoEstourado =
    {
        "..k....k....k...",
        "...kw..w...wk...",
        ".k..ww.w.ww..k..",
        "..ww.lwwwl.ww...",
        "k...lwwEwwl...k.",
        ".wwwwEEBEEwwww..",
        "k...lwwEwwl...k.",
        "..ww.lwwwl.ww...",
        ".k..ww.w.ww..k..",
        "...kw..w...wk...",
        "..k....k....k...",
    };

    // Uma "gota" do corpo do jato.
    static readonly string[] ArteGota =
    {
        "...kkkk...",
        ".kklllEkk.",
        ".klwllEEk.",
        "klwlEEEEBk",
        "klllEEEEBk",
        "kllEEEEEBk",
        "kEEEEEEBBk",
        ".kEEEEBBk.",
        ".kkBBBBkk.",
        "...kkkk...",
    };

    // Os destroços que a onda arrastou.
    static readonly string[] ArteBarril =
    {
        "..kkkkkk..",
        ".kbbbbbbk.",
        "kssssssssk",
        "kbbdbbdbbk",
        "kbbdbbdbbk",
        "kssssssssk",
        "kbbdbbdbbk",
        "kbbdbbdbbk",
        "kssssssssk",
        ".kbbbbbbk.",
        "..kkkkkk..",
    };

    static readonly string[] ArteTabua =
    {
        "kkkkkkkkkkkkkkkkkkkk",
        "knnnnnnbnnnnnnnnbnnk",
        "knnbnnnnnnnnbnnnnnnk",
        "kbbbbbbbbbbbbbbbbbbk",
        "kkkkkkkkkkkkkkkkkkkk",
    };

    static readonly string[] ArteChapeu =
    {
        "......kkkk......",
        "....kkjjjjkk....",
        "...kjjjwwjjjk...",
        "..kjjjwkwjjjjk..",
        "..kjjjjwwjjjjk..",
        ".kjjjjjjjjjjjjk.",
        "kyyyyyyyyyyyyyyk",
        "kjjjjjjjjjjjjjjk",
        ".kkkkkkkkkkkkkk.",
    };

    // Ruído "fixo" (o mesmo pixel sempre dá o mesmo número), para as bolinhas de espuma.
    static int Ruido(int x, int y) => ((x * 73856093) ^ (y * 19349663) ^ 0x5bd1e995) & 0x7fffffff;

    // --- Borda: cada forma é um punhado de meias-bolhas de espuma saindo de uma base, mais um fiapo comprido.
    // (centro em y, raio) de cada bolha, em pixels; e o fiapo: (linha, comprimento).
    static readonly Vector2[][] Bolhas =
    {
        new[] { new Vector2(3.5f, 5f), new Vector2(12f, 7.5f), new Vector2(20f, 4.5f) },
        new[] { new Vector2(5f, 6.5f), new Vector2(14.5f, 5f), new Vector2(21f, 5.5f) },
        new[] { new Vector2(2f, 4f), new Vector2(9f, 6f), new Vector2(18f, 7f) },
        new[] { new Vector2(6f, 7f), new Vector2(15f, 4f), new Vector2(21.5f, 6f) },
    };
    static readonly Vector2Int[] Fiapos = { new Vector2Int(16, 4), new Vector2Int(7, 5), new Vector2Int(13, 3), new Vector2Int(19, 4) };
    const int BaseDaBorda = 9; // até o pixel 9 é tudo água (fica por cima da faixa de água)

    // Até onde (em x) a água vai na linha y.
    static float Extensao(int y, int forma)
    {
        float extensao = BaseDaBorda;
        foreach (Vector2 bolha in Bolhas[forma])
        {
            float d = (y + 0.5f - bolha.x) / bolha.y;
            if (Mathf.Abs(d) < 1f) extensao = Mathf.Max(extensao, BaseDaBorda + bolha.y * 1.25f * Mathf.Sqrt(1f - d * d));
        }
        return extensao;
    }

    static bool DentroDaBorda(int x, int y, int forma)
    {
        if (y < 0 || y >= 24) return x <= BaseDaBorda; // em cima e embaixo continua a base (a bolha vizinha cobre)
        if (x < 0) return true;
        if (x >= 24) return false;
        Vector2Int fiapo = Fiapos[forma];
        if (y == fiapo.x && x <= Extensao(y, forma) + fiapo.y) return true;
        return x < Extensao(y, forma);
    }

    // A espuma: branca na beirada, azul-clarinha logo atrás, e a água por dentro.
    static Color32 CorDaBorda(int x, int y, int forma)
    {
        if (!DentroDaBorda(x, y, forma))
        {
            // contorno azul-marinho em volta da espuma (pixel vazio encostado nela)
            bool encostado = DentroDaBorda(x + 1, y, forma) || DentroDaBorda(x - 1, y, forma)
                          || DentroDaBorda(x, y + 1, forma) || DentroDaBorda(x, y - 1, forma);
            return encostado ? Contorno : FabricaDeSprites.Transparente;
        }

        // A que distância (1 a 4 pixels) está a beirada mais perto?
        for (int r = 1; r <= 4; r++)
            for (int a = -r; a <= r; a++)
            {
                int b = r - Mathf.Abs(a);
                if (DentroDaBorda(x + a, y + b, forma) && DentroDaBorda(x + a, y - b, forma)) continue;
                if (r == 1) return Espuma;
                if (r == 2) return Ruido(x, y + forma * 31) % 5 == 0 ? Espuma : EspumaSombra;
                if (r == 3) return (x + y) % 2 == 0 ? EspumaSombra : Clara; // xadrezinho = degradê de pixel art
                return (x + y) % 2 == 0 ? Clara : Meio;
            }
        return Ruido(x, y + forma * 7) % 23 == 0 ? Meio : Fundo; // umas bolhinhas no meio da água
    }

    // --- Água: riscos de luz feitos com senos (a água subindo pela onda). Na vertical, os senos dão um número
    // INTEIRO de voltas em 64 pixels, então o desenho se repete certinho (dá para empilhar e "rolar" para cima).
    static Color32 CorDaAgua(int x, int y)
    {
        float t = 2f * Mathf.PI * y / 64f;
        float a = Mathf.Sin(2f * t + x * 0.09f + 1.6f * Mathf.Sin(t + x * 0.045f));
        float b = Mathf.Sin(3f * t - x * 0.06f + 2f + 0.8f * Mathf.Sin(2f * t - x * 0.03f));
        float risco = 0.6f * a + 0.4f * b;

        // escura nas pontas: à esquerda emenda no fundo liso, à direita fica embaixo da espuma
        float u = x / 127f;
        float meio = u < 0.85f ? Mathf.Pow(Mathf.Sin(Mathf.PI * u / 0.85f), 1.3f) : 0f;
        float brilho = risco * meio;

        bool xadrez = (x + y) % 2 == 0;
        if (brilho > 0.78f || (brilho > 0.7f && xadrez)) return Brilho;
        if (brilho > 0.45f) return Clara;
        if (brilho > 0.32f) return xadrez ? Clara : Meio;
        if (brilho > 0.12f || (brilho > 0.02f && xadrez)) return Meio;
        return Fundo;
    }

    // --- Crista: o "lábio" da onda enrolando para a frente lá em cima, com o tubo escuro por dentro
    // e uma gota de espuma pingando da ponta. Os pixels 0-4 da esquerda emendam na onda.
    static bool DentroDaCrista(int x, int y)
    {
        if (x < 0 || y < 0 || x >= 48 || y >= 40) return false;
        float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(20f, 20f));
        float d2 = Vector2.Distance(new Vector2(x, y), new Vector2(29f, 14f));
        if (x <= 4) return y < 34;                                 // emenda na onda
        if (d1 >= 18f) return x >= 37 && x <= 40 && y >= 5 && y <= 12 && (x - 38.5f) * (x - 38.5f) + (y - 9f) * (y - 9f) < 9f; // a gota pingando
        if (d2 < 8f) return false;                                 // o buraco do tubo
        if (y < 14 && x > 24) return false;                        // embaixo do lábio é vazio
        return true;
    }

    static Color32 CorDaCrista(int x, int y)
    {
        if (!DentroDaCrista(x, y))
        {
            bool encostado = DentroDaCrista(x + 1, y) || DentroDaCrista(x - 1, y) || DentroDaCrista(x, y + 1) || DentroDaCrista(x, y - 1);
            return encostado && x > 4 ? Contorno : FabricaDeSprites.Transparente;
        }
        float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(20f, 20f));
        float d2 = Vector2.Distance(new Vector2(x, y), new Vector2(29f, 14f));
        if (d1 >= 18f) return Espuma;                              // a gota
        if (d1 > 16f && y > 12) return Espuma;                     // a espuma em cima do lábio
        if (d1 > 14.5f && y > 12) return (x + y) % 2 == 0 ? Espuma : EspumaSombra;
        if (d2 < 10f) return Funda;                                // a sombra dentro do tubo
        if (d2 < 11.5f) return (x + y) % 2 == 0 ? Funda : Fundo;
        float onda = Mathf.Sin(x * 0.35f + y * 0.2f) + Mathf.Sin(y * 0.5f - x * 0.1f);
        return onda > 1.2f ? Clara : onda > 0.6f ? Meio : Fundo;
    }

    // --- Vinheta: sombra azul forte do lado esquerdo (de onde a onda vem), com a beirada ondulada,
    // e uma moldura fraquinha em volta da tela toda. O resto é transparente.
    static Color32 CorDaVinheta(int x, int y)
    {
        float u = x / 127f, v = y / 71f;
        float onda = 0.035f * Mathf.Sin(v * Mathf.PI * 6f) + 0.02f * Mathf.Sin(v * Mathf.PI * 13f + 1f);
        float esquerda = Mathf.Pow(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0f, u + onda)), 1.4f);
        float beirada = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
        float moldura = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0f, beirada)) * 0.55f;
        Color32 cor = new Color32(6, 18, 46, 255);
        cor.a = (byte)Mathf.RoundToInt(Mathf.Max(esquerda * 0.95f, moldura) * 255f);
        return cor;
    }
}
