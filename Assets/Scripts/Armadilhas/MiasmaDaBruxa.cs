using System.Collections.Generic;
using UnityEngine;

// 'N' - o MIASMA DA BRUXA: uma PAREDE de sombra roxa (o miasma da Satella, a Bruxa da Inveja),
// cheia de olhos e de mãos negras, que PERSEGUE o Subaru pela fase inteira. Encostou nela, morreu.
//  - O 'N' marca a coluna de onde ela começa, atrás do jogador. Ela fica "dormindo" (olhos fechados)
//    até o jogador se afastar uns 4 blocos; aí ela RI, a tela treme e ela começa a andar para a direita.
//  - Ela anda a 5,2 blocos por segundo e o Subaru anda 7: correndo sem parar você abre vantagem.
//    Mas cada vez que você PARA para pensar (ou erra um pulo e tem que voltar) é tempo que ela recupera:
//    ficou uns 2 segundos parado, ela te pega. Por isso a fase dela é só CORRER e PULAR (parkour).
//  - "Elástico": se você abrir vantagem demais, ela acelera. Correndo perfeito ela fica uns 11 blocos atrás
//    (bem na beirada da tela) e NUNCA mais de 14. Não dá para "guardar" vantagem para depois.
//  - Quando você fica perto dela (3,5 blocos), uma das MÃOS se estica para te agarrar. O aviso é a mão
//    recuar para dentro da parede e piscar (0,35 s). Corra ou pule por cima!
//  - Renasceu num checkpoint? A parede nasce 9 blocos ATRÁS de você (senão o checkpoint não serviria de nada).
//  - Ela para antes da bandeira de verdade. Passou de fase? Ela desiste e vai embora... por enquanto.
//
// O desenho é todo feito de pedaços (todos filhos do objeto "Parede", que anda junto com a frente):
//  fundo escuro liso + faixa de névoa com fiapos subindo + "bolhas" na borda (trocam de forma o tempo todo)
//  + olhos que piscam + mãos com braço de bolinhas. Na frente, fumaça roxa (partículas).
//  Uma vinheta roxa presa na câmera escurece a tela quando ela chega perto (junto com um "coração" batendo).
[DefaultExecutionOrder(100)] // roda depois da CameraSeguir: a vinheta acompanha a câmera sem tremer
public class MiasmaDaBruxa : MonoBehaviour
{
    enum Estado { Dormindo, Acordando, Perseguindo, IndoEmbora }
    enum EstadoDaMao { Balancando, Preparando, Agarrando, Voltando }

    [Header("Perseguição")]
    public float velocidade = 5.2f;           // blocos por segundo (o Subaru anda 7)
    public float distanciaParaAcordar = 4f;   // acorda quando o jogador se afasta isso da frente dela
    public float inicioDoElastico = 9f;       // mais longe que isso, ela acelera...
    public float forcaDoElastico = 0.8f;      // ...0,8 bloco/s a mais para cada bloco de distância a mais
    public float distanciaMaxima = 14f;       // e ela NUNCA fica mais longe que isso
    public float distanciaAoRenascer = 9f;    // renasceu num checkpoint? Ela aparece isso atrás de você
    public float tempoAcordando = 0.7f;       // a "espreguiçada" (risada) antes de começar a andar

    [Header("Mãos")]
    public float distanciaDoBote = 3.5f;      // jogador mais perto que isso da frente: uma mão tenta agarrar
    public float tempoDePreparo = 0.35f;      // o aviso: a mão recua e pisca
    public float alcanceDoBote = 2.6f;        // até onde a mão estica (a partir da frente da parede)
    public float recargaDaMao = 0.9f;         // descanso de cada mão depois de um bote

    // A "linha da morte" é a frente da parede. O colisor do Subaru tem uns 0,36 de cada lado do centro.
    const float MeiaLarguraDoJogador = 0.36f;

    // Até onde a parede vai na vertical (a câmera mostra de -0,5 a 14,5 nas fases de 15 linhas).
    const float Baixo = -1.5f, Alto = 16.5f;

    // Ordem de desenho: tudo bem na frente da fase (a parede "engole" o cenário), atrás só da interface.
    const int OrdemDoFundo = 38, OrdemDaNevoa = 39, OrdemDosOlhos = 40, OrdemDosBracos = 41,
              OrdemDaBorda = 42, OrdemDasMaos = 44, OrdemDaVinheta = 60;

    const int QuantidadeDeBolhas = 4;  // variações do desenho da borda ("miasma_borda_0" a "_3")
    const float LarguraDaNevoa = 8f, AlturaDaNevoa = 4f;
    const int GomosDoBraco = 8;

    // Cores da fumaça (as partículas usam o desenho da poeira pintado com essas cores).
    static readonly Color FumacaClara = new Color(0.69f, 0.38f, 0.9f, 0.85f);
    static readonly Color FumacaEscura = new Color(0.22f, 0.07f, 0.32f, 0.9f);
    static readonly Color CorDoAviso = new Color(1f, 0.75f, 1f);

    // Uma mão da Bruxa: o desenho (que mata), o braço de "gomos" e o que ela está fazendo agora.
    class Mao
    {
        public Transform mao;
        public SpriteRenderer desenho;
        public readonly List<Transform> gomos = new List<Transform>();
        public float alturaBase, fase;     // onde ela fica na parede e o "ritmo" dela (cada uma mexe diferente)
        public EstadoDaMao estado;
        public float timer, recarga;
        public float alcance, altura;      // posição atual da mão (em relação à frente da parede)
        public float alturaDoBote;         // a altura em que ela vai dar o bote (a do jogador)
        public bool fechada;               // já fechou a mão neste bote?
    }

    // Um olho que pisca no meio da sombra.
    class Olho
    {
        public SpriteRenderer desenho;
        public float timer;   // até a próxima piscada (ou a próxima espiada, dormindo)
        public float piscando; // > 0 enquanto pisca
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
    bool jaSussurrou;      // o "Eu te amo..." aparece uma vez por tentativa

    Transform parede;
    readonly List<SpriteRenderer> bolhas = new List<SpriteRenderer>();
    readonly List<Transform> nevoas = new List<Transform>();
    readonly List<Olho> olhos = new List<Olho>();
    readonly List<Mao> maos = new List<Mao>();
    SpriteRenderer vinheta;

    // ------------------------------------------------------------------ montagem

    void Awake()
    {
        parede = new GameObject("Parede").transform;
        parede.SetParent(transform, false);

        // Fundo: um retângulo escuro liso, comprido para a esquerda (você nunca vê o fim dele).
        var fundo = ConstrutorDeFase.Visual("Fundo", parede, transform.position, "miasma_fundo", OrdemDoFundo, false).transform;
        fundo.localPosition = new Vector3(-LarguraDaNevoa - 25f, (Baixo + Alto) / 2f, 0f);
        fundo.localScale = new Vector3(52f, Alto - Baixo, 1f);

        // Faixa de névoa logo atrás da borda, com fiapos roxos que sobem (o desenho se repete na vertical).
        for (int i = 0; i < 6; i++)
            nevoas.Add(ConstrutorDeFase.Visual("Nevoa", parede, transform.position, "miasma_nevoa", OrdemDaNevoa, false).transform);

        // Borda: "bolhas" de fumaça empilhadas, uma por bloco de altura. Elas tremem e trocam de forma.
        for (float y = Baixo; y <= Alto; y += 1f)
        {
            var bolha = ConstrutorDeFase.Visual("Borda", parede, transform.position, "miasma_borda_0", OrdemDaBorda, false);
            bolha.transform.localPosition = new Vector3(0f, y, 0f);
            bolhas.Add(bolha.GetComponent<SpriteRenderer>());
        }

        // Olhos espalhados pela sombra (e uma luzinha roxa em cada um, nas fases escuras).
        Vector2[] lugaresDosOlhos = { new Vector2(-1.7f, 2.7f), new Vector2(-3.6f, 6.1f), new Vector2(-2.1f, 9.5f), new Vector2(-3.9f, 12.8f) };
        foreach (Vector2 lugar in lugaresDosOlhos)
        {
            var olho = ConstrutorDeFase.Visual("Olho", parede, transform.position, "miasma_olho_0", OrdemDosOlhos, false);
            olho.transform.localPosition = lugar;
            Luzes.Ponto(olho.transform, Luzes.Lilas, 1.6f, 0.6f);
            olhos.Add(new Olho { desenho = olho.GetComponent<SpriteRenderer>(), timer = Random.Range(0.5f, 3f) });
        }

        // As mãos, em alturas diferentes (a de baixo pega quem corre no chão; as outras, quem pula).
        float[] alturas = { 1.9f, 4.6f, 7.4f, 10.2f, 13f };
        for (int i = 0; i < alturas.Length; i++) maos.Add(CriarMao(alturas[i], i * 2.1f));

        // Luz roxa ao longo da frente (só existe nas fases escuras: Luzes.Ponto devolve null de dia).
        for (float y = 2f; y < 14f; y += 5f)
            Luzes.Ponto(parede, Luzes.Lilas, 5f, 0.9f, new Vector3(0.3f, y, 0f));

        // A vinheta: um desenho do tamanho da tela, preso na câmera (veja LateUpdate). Começa invisível.
        vinheta = ConstrutorDeFase.Visual("Vinheta", transform, transform.position, "miasma_vinheta", OrdemDaVinheta, false).GetComponent<SpriteRenderer>();
        vinheta.color = new Color(1f, 1f, 1f, 0f);

        frente = xDoN = transform.position.x;
    }

    Mao CriarMao(float altura, float fase)
    {
        var mao = new Mao { alturaBase = altura, altura = altura, fase = fase };
        for (int i = 0; i < GomosDoBraco; i++)
            mao.gomos.Add(ConstrutorDeFase.Visual("Braco", parede, transform.position, "miasma_braco", OrdemDosBracos, false).transform);

        var objeto = ConstrutorDeFase.Visual("MaoDaBruxa", parede, transform.position, "mao_da_bruxa", OrdemDasMaos, false);
        mao.mao = objeto.transform;
        mao.desenho = objeto.GetComponent<SpriteRenderer>();

        // Só a MÃO mata (o braço é só enfeite). Como ela se mexe, precisa do corpo cinemático.
        var area = objeto.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 0.33f;
        objeto.AddComponent<Perigo>().conquista = "miasma";
        Perigo.TornarMovel(objeto);
        Luzes.Ponto(mao.mao, Luzes.Lilas, 1.4f, 0.5f);
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
            else if (bandeira.GetComponent<Bandeira>() != null) maisLonge = Mathf.Max(maisLonge, bandeira.position.x);
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
        // A parede reaparece atrás dele, como num checkpoint.
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
        AnimarOlhos(dt);
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
        // um "estouro" de fumaça e todas as mãos se esticam de uma vez
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
            GerenciadorDoJogo.Instancia.Avisar("Eu te amo...", 1f); // a Bruxa está BEM perto
        }
    }

    // Encostou na parede: a Bruxa te "abraça". Todas as mãos fecham de uma vez.
    void Abracar()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || jogador.Morto) return;
        jogador.Morrer();
        if (!jogador.Morto) return;
        Conquistas.Desbloquear("miasma");
        GerenciadorDoJogo.Som("rugido", 0.6f);
        CameraSeguir.Tremer(0.3f, 0.35f);
        foreach (Mao mao in maos)
        {
            mao.estado = EstadoDaMao.Voltando;
            mao.timer = 0.8f;
            mao.desenho.sprite = FabricaDeSprites.Pegar("mao_da_bruxa_fechada");
        }
    }

    void IrEmbora()
    {
        estado = Estado.IndoEmbora;
        foreach (Olho olho in olhos) olho.desenho.sprite = FabricaDeSprites.Pegar("miasma_olho_0");
    }

    // ------------------------------------------------------------------ animações

    // As bolhas da borda tremem para os lados e trocam de forma umas 3 vezes por segundo (cada uma no seu ritmo).
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
            bolha.sprite = FabricaDeSprites.Pegar("miasma_borda_" + forma);
            bolha.flipY = i % 2 == 1; // de ponta-cabeça a cada duas: parece que tem mais formas
        }
    }

    // A névoa sobe devagar: os pedaços andam para cima e o de cima volta para baixo (o desenho se repete).
    void AnimarNevoa()
    {
        float sobe = Mathf.Repeat(relogio * 0.7f, AlturaDaNevoa);
        for (int i = 0; i < nevoas.Count; i++)
        {
            float y = Baixo - AlturaDaNevoa + AlturaDaNevoa / 2f + i * AlturaDaNevoa + sobe;
            nevoas[i].localPosition = new Vector3(-LarguraDaNevoa / 2f - 0.2f, y, 0f);
        }
    }

    // Dormindo, os olhos ficam fechados (e de vez em quando ESPIAM). Acordada, ficam abertos e piscam.
    void AnimarOlhos(float dt)
    {
        foreach (Olho olho in olhos)
        {
            olho.timer -= dt;
            olho.piscando -= dt;
            if (olho.timer <= 0f)
            {
                olho.piscando = estado == Estado.Dormindo ? 0.5f : 0.15f;
                olho.timer = Random.Range(2f, 5f);
            }

            string quadro;
            if (estado == Estado.IndoEmbora) quadro = "miasma_olho_0";
            else if (estado == Estado.Dormindo) quadro = olho.piscando > 0f ? "miasma_olho_1" : "miasma_olho_0"; // espiando...
            else if (olho.piscando > 0f) quadro = olho.piscando > 0.05f && olho.piscando < 0.1f ? "miasma_olho_0" : "miasma_olho_1";
            else quadro = "miasma_olho_2";
            olho.desenho.sprite = FabricaDeSprites.Pegar(quadro);

            float pulso = 1f + 0.06f * Mathf.Sin(relogio * 3f + olho.desenho.transform.localPosition.y);
            olho.desenho.transform.localScale = Vector3.one * 1.4f * pulso; // olhos grandes (o desenho tem 15 pixels)
        }
    }

    void AnimarMaos(float dt, bool vivo, Vector2 jogador, float distancia)
    {
        // Jogador perto da parede e nenhuma mão no meio de um bote? A mão mais perto da altura dele ataca.
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
                    // vai e volta devagar, com os dedos para fora da sombra (dormindo, quase toda escondida)
                    float onda = 0.5f + 0.5f * Mathf.Sin(relogio * 1.3f + mao.fase);
                    float alvo = estado == Estado.Dormindo ? -0.2f + 0.3f * onda : 0.05f + 0.35f * onda;
                    if (estado == Estado.Acordando) alvo = 0.6f + 0.5f * Mathf.Sin(relogio * 14f + mao.fase); // se debatendo
                    mao.alcance = Mathf.Lerp(mao.alcance, alvo, 6f * dt);
                    mao.altura = Mathf.Lerp(mao.altura, mao.alturaBase + 0.25f * Mathf.Sin(relogio * 0.9f + mao.fase * 2f), 4f * dt);
                    break;

                case EstadoDaMao.Preparando:
                    // O AVISO: recua para dentro da sombra, mira na altura do jogador e pisca
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
                    // estica rapidinho (0,1 s), segura aberta e fecha a mão no fim
                    float quanto = Mathf.SmoothStep(0f, 1f, (0.3f - mao.timer) / 0.1f);
                    mao.alcance = Mathf.Lerp(-0.7f, alcanceDoBote, quanto);
                    if (mao.timer < 0.12f && !mao.fechada)
                    {
                        mao.fechada = true;
                        mao.desenho.sprite = FabricaDeSprites.Pegar("mao_da_bruxa_fechada");
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
                        mao.desenho.sprite = FabricaDeSprites.Pegar("mao_da_bruxa");
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
        mao.desenho.sprite = FabricaDeSprites.Pegar("mao_da_bruxa");
        GerenciadorDoJogo.Som("armadilha", 0.35f);
    }

    // A mão fica na ponta do braço; o braço é uma fileira de "gomos" que sai de dentro da sombra
    // e ondula um pouquinho (os gomos de perto da mão são menores).
    void PosicionarMao(Mao mao)
    {
        Vector2 raiz = new Vector2(-0.8f, mao.alturaBase); // escondida embaixo da borda
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
        float mexe = mao.estado == EstadoDaMao.Balancando ? 10f * Mathf.Sin(relogio * 2.2f + mao.fase) : 0f; // dedos "chamando"
        mao.mao.localPosition = ponta;
        mao.mao.localRotation = Quaternion.Euler(0f, 0f, angulo + mexe);
    }

    // Fumaça roxa saindo da frente da parede (mais devagar enquanto ela dorme).
    void SoltarFumaca(float dt)
    {
        if (estado == Estado.IndoEmbora) return;
        timerFumaca -= dt;
        if (timerFumaca > 0f) return;
        timerFumaca = estado == Estado.Dormindo ? 0.15f : 0.04f;
        Fumaca(Random.Range(-0.2f, 0.4f), Random.Range(-0.5f, 14.5f), 1);
    }

    // Solta "quantidade" partículas de fumaça perto de um ponto (x em relação à frente da parede).
    void Fumaca(float x, float y, int quantidade)
    {
        for (int i = 0; i < quantidade; i++)
        {
            var posicao = new Vector3(frente + x, y, 0f) + (Vector3)(Random.insideUnitCircle * 0.2f);
            var velocidadeDaFumaca = new Vector2(Random.Range(0.6f, 2.8f), Random.Range(-0.2f, 1f));
            Color cor = Random.value < 0.5f ? FumacaClara : FumacaEscura;
            Particula.Criar(transform.parent, posicao, velocidadeDaFumaca, Random.Range(0.4f, 0.9f), Random.Range(0.9f, 1.8f), cor);
        }
    }

    // O "estouro" de quando ela acorda: fumaça clara (e mais rápida) em toda a altura.
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

    // A vinheta acompanha a câmera e escurece a tela quanto mais perto a parede está (pulsando com o "coração").
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

    // Registra os desenhos na FabricaDeSprites (Pegar("miasma") funciona em qualquer lugar, até no mapa do mundo).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        Vector2 centro = FabricaDeSprites.Centro;
        FabricaDeSprites.Registrar("miasma", () => FabricaDeSprites.DeArte(ArteIcone, centro));
        FabricaDeSprites.Registrar("mao_da_bruxa", () => FabricaDeSprites.DeArte(ArteMao, centro));
        FabricaDeSprites.Registrar("mao_da_bruxa_fechada", () => FabricaDeSprites.DeArte(ArteMaoFechada, centro));
        FabricaDeSprites.Registrar("miasma_braco", () => FabricaDeSprites.DeArte(ArteGomo, centro));
        FabricaDeSprites.Registrar("miasma_olho_0", () => FabricaDeSprites.DeArte(ArteOlhoFechado, centro));
        FabricaDeSprites.Registrar("miasma_olho_1", () => FabricaDeSprites.DeArte(ArteOlhoMeio, centro));
        FabricaDeSprites.Registrar("miasma_olho_2", () => FabricaDeSprites.DeArte(ArteOlhoAberto, centro));
        FabricaDeSprites.Registrar("miasma_fundo", () => FabricaDeSprites.Procedural(16, 16, centro, (x, y) => FabricaDeSprites.Cor('x')));
        FabricaDeSprites.Registrar("miasma_nevoa", () => FabricaDeSprites.Procedural(128, 64, centro, CorDaNevoa));
        FabricaDeSprites.Registrar("miasma_vinheta", () => FabricaDeSprites.Procedural(128, 72, centro, CorDaVinheta));

        // A borda: o pivô fica no pixel 15 de 24 (a "frente" da parede passa no meio das bolhas).
        var pivoDaBorda = new Vector2(15f / 24f, 0.5f);
        for (int i = 0; i < QuantidadeDeBolhas; i++)
        {
            int forma = i;
            FabricaDeSprites.Registrar("miasma_borda_" + forma, () => FabricaDeSprites.Procedural(24, 24, pivoDaBorda, (x, y) => CorDaBorda(x, y, forma)));
        }
        FabricaDeSprites.Registrar("miasma_borda", () => FabricaDeSprites.Pegar("miasma_borda_0"));
    }

    // Ícone (mapa do mundo): a mão da Bruxa saindo da sombra, com um olho roxo na palma.
    static readonly string[] ArteIcone =
    {
        ".......vvv.vvv.......",
        "...vvv.vxv.vxv.vvv...",
        "...vxv.vxv.vxv.vxv...",
        "...vxv.vxv.vxv.vxv...",
        "...vxv.vxv.vxv.vxv...",
        "...vxvvvxvvvxvvvxv...",
        "vv.vxxxxxxxxxxxxxv...",
        "vxvvxxxxxxxxxxxxxv...",
        "vxxvxxvvvvvvvvxxxv...",
        ".vxxxvVPPPkPPPVvxv...",
        "..vxxvVPPwkPPPVvxv...",
        "...vxxvvvvvvvvxxxv...",
        "...vxxxxxxxxxxxxv....",
        "....vxxxxxxxxxxv.....",
        "..vvvvxxxxxxxxvvvv...",
        ".vxxxxvxxxxxxvxxxxv..",
        "vxxvxxxxxxxxxxxxvxxv.",
        ".vvvvvvvvvvvvvvvvvv..",
    };

    // A mão aberta, esticada para a direita (dedos com unhas lilás). Contorno roxo, como a mão do cano.
    static readonly string[] ArteMao =
    {
        "........vv......",
        ".......vxxv.....",
        "......vxxvPP....",
        "..vvvvxxv.......",
        ".vxxxxxxvvvvvv..",
        "vxxxxxxxxxxxxxvP",
        "vxxxxxxxvvvvvvV.",
        "vxxxxxxv........",
        "vxxxxxxxvvvvvvv.",
        "vxxxxxxxxxxxxxxP",
        "vxxxxxxxvvvvvvvV",
        "vxxxxxxv........",
        "vxxxxxxxvvvvv...",
        ".vxxxxxxxxxxxvP.",
        "..vvvvvvvvvvvV..",
    };

    // A mão fechada (agarrando), com as unhas para baixo.
    static readonly string[] ArteMaoFechada =
    {
        "................",
        "........vvv.....",
        ".......vxxxv....",
        "..vvvvvxxxxxv...",
        ".vxxxxxxxxxxxv..",
        "vxxxxxxxvxxxxxv.",
        "vxxxxxxxxvxxxxv.",
        "vxxvxxxxxvxxxxv.",
        "vxxxvxxxvxxxxxv.",
        "vxxxxxxxxxxxxv..",
        "vxxvxxxvxvxvxv..",
        ".vxxxxvVvVvVv...",
        "..vvvvvP.P.P....",
        "................",
    };

    // Um "gomo" do braço (bolinha de sombra).
    static readonly string[] ArteGomo =
    {
        "...vvvv...",
        ".vvxxxxvv.",
        ".vxxxxxxv.",
        "vxxvxxxxxv",
        "vxxxxxxxxv",
        "vxxxxxxxxv",
        "vxxxxxvxxv",
        ".vxxxxxxv.",
        ".vvxxxxvv.",
        "...vvvv...",
    };

    // O olho: fechado (dormindo), meio aberto (espiando / piscando) e aberto, com a pupila fininha.
    static readonly string[] ArteOlhoAberto =
    {
        ".....vvvvv.....",
        "..vvvVPPPVvvv..",
        ".vVPPPwkPPPPVv.",
        "vVPPPPPkPPPPPVv",
        ".vVPPPPkPPPPVv.",
        "..vvvVPPPVvvv..",
        ".....vvvvv.....",
    };

    static readonly string[] ArteOlhoMeio =
    {
        "...............",
        "...............",
        "..vvvvvvvvvvv..",
        "vVPPPPPkPPPPPVv",
        "..vvvvvvvvvvv..",
        "...............",
        "...............",
    };

    static readonly string[] ArteOlhoFechado =
    {
        "...............",
        "...............",
        "...............",
        "vv...........vv",
        ".vvv.......vvv.",
        "....vvvvvvv....",
        "...............",
    };

    // Entre o 'x' (sombra) e o 'v' (roxo): usado nos degradês da névoa e da borda.
    static readonly Color32 Meio = new Color32(52, 20, 76, 255);

    // Ruído "fixo" (o mesmo pixel sempre dá o mesmo número), para os brilhinhos.
    static int Ruido(int x, int y) => ((x * 73856093) ^ (y * 19349663) ^ 0x5bd1e995) & 0x7fffffff;

    // --- Borda: cada forma é um punhado de meias-bolhas saindo de uma base, mais um fiapo comprido.
    // (centro em y, raio) de cada bolha, em pixels; e o fiapo: (linha, comprimento).
    static readonly Vector2[][] Bolhas =
    {
        new[] { new Vector2(3.5f, 5f), new Vector2(12f, 7.5f), new Vector2(20f, 4.5f) },
        new[] { new Vector2(5f, 6.5f), new Vector2(14.5f, 5f), new Vector2(21f, 5.5f) },
        new[] { new Vector2(2f, 4f), new Vector2(9f, 6f), new Vector2(18f, 7f) },
        new[] { new Vector2(6f, 7f), new Vector2(15f, 4f), new Vector2(21.5f, 6f) },
    };
    static readonly Vector2Int[] Fiapos = { new Vector2Int(16, 4), new Vector2Int(7, 5), new Vector2Int(13, 3), new Vector2Int(19, 4) };
    const int BaseDaBorda = 9; // até o pixel 9 é tudo sombra (fica por cima da névoa)

    // Até onde (em x) a sombra vai na linha y.
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

    static Color32 CorDaBorda(int x, int y, int forma)
    {
        if (!DentroDaBorda(x, y, forma))
        {
            // contorno preto em volta da sombra (pixel vazio encostado nela)
            bool encostado = DentroDaBorda(x + 1, y, forma) || DentroDaBorda(x - 1, y, forma)
                          || DentroDaBorda(x, y + 1, forma) || DentroDaBorda(x, y - 1, forma);
            return encostado ? FabricaDeSprites.Cor('k') : FabricaDeSprites.Transparente;
        }

        // A que distância (1, 2 ou 3 pixels) está a beirada mais perto? Perto da beirada a sombra brilha roxo.
        for (int r = 1; r <= 3; r++)
            for (int a = -r; a <= r; a++)
            {
                int b = r - Mathf.Abs(a);
                if (DentroDaBorda(x + a, y + b, forma) && DentroDaBorda(x + a, y - b, forma)) continue;
                if (r == 1) return FabricaDeSprites.Cor(Ruido(x, y + forma * 31) % 9 == 0 ? 'P' : 'V');
                if (r == 2) return FabricaDeSprites.Cor('v');
                return (x + y) % 2 == 0 ? FabricaDeSprites.Cor('v') : Meio; // xadrezinho = degradê de pixel art
            }
        return FabricaDeSprites.Cor('x');
    }

    // --- Névoa: fiapos roxos feitos com senos. Na vertical, os senos dão um número INTEIRO de voltas
    // em 64 pixels, então o desenho se repete certinho (dá para empilhar e "rolar" para cima).
    static Color32 CorDaNevoa(int x, int y)
    {
        float t = 2f * Mathf.PI * y / 64f;
        float a = Mathf.Sin(2f * t + x * 0.09f + 1.6f * Mathf.Sin(t + x * 0.045f));
        float b = Mathf.Sin(3f * t - x * 0.06f + 2f + 0.8f * Mathf.Sin(2f * t - x * 0.03f));
        float fiapo = 0.6f * a + 0.4f * b;

        // escura nas pontas: à esquerda emenda no fundo liso, à direita fica embaixo da borda
        float u = x / 127f;
        float meio = u < 0.85f ? Mathf.Pow(Mathf.Sin(Mathf.PI * u / 0.85f), 1.3f) : 0f;
        float brilho = fiapo * meio;

        bool xadrez = (x + y) % 2 == 0;
        if (brilho > 0.72f || (brilho > 0.62f && xadrez)) return FabricaDeSprites.Cor('V');
        if (brilho > 0.42f) return FabricaDeSprites.Cor('v');
        if (brilho > 0.3f) return xadrez ? FabricaDeSprites.Cor('v') : Meio;
        if (brilho > 0.12f || (brilho > 0.02f && xadrez)) return Meio;
        return FabricaDeSprites.Cor('x');
    }

    // --- Vinheta: sombra forte do lado esquerdo (de onde a parede vem), com a beirada ondulada,
    // e uma moldura fraquinha em volta da tela toda. O resto é transparente.
    static Color32 CorDaVinheta(int x, int y)
    {
        float u = x / 127f, v = y / 71f;
        float onda = 0.035f * Mathf.Sin(v * Mathf.PI * 6f) + 0.02f * Mathf.Sin(v * Mathf.PI * 13f + 1f);
        float esquerda = Mathf.Pow(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0f, u + onda)), 1.4f);
        float beirada = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
        float moldura = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0f, beirada)) * 0.55f;
        Color32 cor = FabricaDeSprites.Cor('x');
        cor.a = (byte)Mathf.RoundToInt(Mathf.Max(esquerda * 0.95f, moldura) * 255f);
        return cor;
    }
}
