using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  VENTO DA RAM
// =====================================================================
//  A Ram (a irmã gêmea da Rem, a de cabelo ROSA) é maga de VENTO. E ela acha o Subaru ("Barusu") um inútil...
//
//  'y' - RAJADA. Células 'y' encostadas formam UMA zona de vento: o RETÂNGULO em volta delas.
//        (então dá para pôr moedas, inimigos, blocos e espinhos DENTRO da zona: basta as células 'y'
//        em volta estarem encostadas umas nas outras)
//        O vento vem da DIREITA e empurra o Subaru para TRÁS, em rajadas. Todas as zonas sopram juntas
//        (a Ram "respira"), e o ritmo recomeça sempre que a fase é montada:
//            calmo (1 s)  ->  AVISO (0,5 s: folhas e riscos de vento começam a passar e o vento sobe)
//                         ->  RAJADA (2 s)  ->  calmo...
//        Na rajada, quem segura para a frente QUASE fica parado (a Ram é um tiquinho mais forte que o Subaru).
//        Quem solta o botão volta voando. Quem estava no meio de um pulo perde o embalo e cai no buraco. :)
//        ABRIGO: o vento não atravessa paredes. Quem está até 2 blocos à ESQUERDA de algo sólido (na altura
//        do corpo) fica protegido. Repare que os riscos de vento também não passam ali.
//
//  'n' - REDEMOINHO: uma corrente de ar para CIMA (a zona é um retângulo, igual à 'y'). Lá dentro a gravidade
//        some e o Subaru sobe devagar (até 6 blocos/s), freando perto do topo: ele fica "boiando" na altura
//        do topo da zona. Dá para atravessar abismos largos flutuando (e quem entra pulando sobe mais).
//        TRAIÇOEIRO: se a coluna da ESQUERDA da zona for ÍMPAR (contando a partir de 0, como no mapa), o
//        redemoinho é igualzinho aos outros... até você chegar perto (4 blocos). Aí ele ACORDA: fica ligado só
//        mais 1,2 s, ENGASGA por 0,6 s (os riscos ficam cinza e tremem: esse é o aviso!) e DESLIGA por 1,5 s.
//        Depois volta (2,6 s ligado) e fica nesse ritmo. Quem já sabe espera ele desligar e entra assim que ele volta.
//
//  Como funciona a física: o vento chama Jogador.Empurrar (o Jogador soma o empurrão na velocidade que ele
//  quer andar) e o redemoinho mexe direto na velocidade vertical do corpo (Rigidbody2D). Este script roda
//  ANTES do Jogador em cada passo da física (veja o DefaultExecutionOrder), então tudo vale no mesmo passo.
// =====================================================================
[DefaultExecutionOrder(-10)]
public class VentoDaRam : MonoBehaviour
{
    public enum Tipo { Rajada, Redemoinho }
    enum Estado { Calmo, Aviso, Soprando, Ligado, Engasgando, Desligado }

    [Header("Rajada ('y')")]
    public float forcaDaRajada = 7.5f;    // em blocos por segundo, para a esquerda (o Subaru anda a 7)
    public float tempoCalmo = 1f;
    public float tempoDeAviso = 0.5f;
    public float tempoDeRajada = 2f;
    public float tempoParaEncher = 0.25f;  // a rajada não começa de uma vez: vai enchendo...
    public float tempoParaAcalmar = 0.3f;  // ...e vai parando
    public float alcanceDoAbrigo = 2f;     // até quantos blocos à esquerda de uma parede o vento não pega

    [Header("Redemoinho ('n')")]
    public float velocidadeDeSubida = 6f;
    public float aceleracaoParaCima = 80f; // segura rápido quem cai dentro dele (é um "colchão" de ar)
    public float freioParaBaixo = 30f;     // quem entra pulando ainda sobe um bom pedaço antes de frear
    public float freioNoTopo = 1.5f;       // nos últimos 1,5 blocos a subida vai diminuindo até parar no topo

    [Header("Redemoinho traiçoeiro (coluna da esquerda ímpar)")]
    public bool traicoeiro;
    public float distanciaParaAcordar = 4f;
    public float primeiroTempoLigado = 1.2f; // logo que acorda, ele engasga rapidinho (é a pegadinha)
    public float tempoLigado = 2.6f;
    public float tempoEngasgando = 0.6f;
    public float tempoDesligado = 1.5f;

    public Tipo tipo;

    // A gravidade padrão da Unity (Physics2D.gravity). O Jogador usa ela vezes o gravityScale dele.
    const float GravidadeDaUnity = 9.81f;
    // Até onde (na horizontal) os ciscos são animados. A câmera mostra uns 13 blocos para cada lado.
    const float DistanciaParaAnimar = 18f;

    Rect area;                    // a zona, em unidades do mundo (1 bloco = 1 unidade)
    Estado estado;
    float relogio;                // rajada: tempo desde que a fase foi montada (é igual em todas as zonas)
    float forcaAgora;             // rajada: 0 = calmo, 1 = soprando com tudo
    float avisoAgora;             // rajada: 0 a 1 durante o aviso
    float timer;                  // redemoinho traiçoeiro: quanto falta no estado atual
    bool acordado;                // redemoinho traiçoeiro: já percebeu o jogador?

    bool jogadorDentro;
    bool abrigado;
    float tempoDesdeOEmpurrao = 99f;  // para a conquista: o vento empurrou o Subaru para a morte?
    float tempoDesdeQueLargou = 99f;  // ...ou o redemoinho desligou com ele dentro?
    bool conquistaVerificada;
    float timerPoeira;

    SpriteRenderer fundo;         // o "ar" da zona: um retângulo branquinho quase transparente
    AudioSource fonte;
    bool ciscosEscondidos = true;
    float acumulador;             // quantos ciscos estão "devendo" nascer (fração de um)
    readonly List<Cisco> ciscos = new List<Cisco>();

    ContactFilter2D filtroSolido;
    readonly List<RaycastHit2D> acertos = new List<RaycastHit2D>();
    readonly List<Collider2D> encostados = new List<Collider2D>();
    bool[,] solida;               // grade das células sólidas da zona (e 2 colunas à direita), para os ciscos
    int colunaDaGrade, linhaDaGrade;

    // Os sons de vento são gerados por código (uma vez só) e reaproveitados por todas as zonas.
    static AudioClip somDaRajada, somDoSopro;
    static float ultimoSomDeRajada = -100f; // as zonas sopram juntas: só uma toca o som

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar()
    {
        somDaRajada = null;
        somDoSopro = null;
        ultimoSomDeRajada = -100f;
    }

    // ------------------------------------------------------------------ montagem

    // Chamado pelo ConstrutorDeFase com as células de um grupo ('y' ou 'n' encostadas).
    // A zona é o retângulo em volta delas. alturaDoMapa = número de linhas do mapa (para virar posição no mundo).
    public static VentoDaRam CriarZona(Transform raiz, Tipo tipo, List<Vector2Int> celulas, int alturaDoMapa)
    {
        int xMin = int.MaxValue, xMax = int.MinValue, linhaMin = int.MaxValue, linhaMax = int.MinValue;
        foreach (Vector2Int celula in celulas)
        {
            xMin = Mathf.Min(xMin, celula.x);
            xMax = Mathf.Max(xMax, celula.x);
            linhaMin = Mathf.Min(linhaMin, celula.y);
            linhaMax = Mathf.Max(linhaMax, celula.y);
        }

        // A linha 0 é o topo do mapa: no mundo, y = alturaDoMapa - 1 - linha (igual ao ConstrutorDeFase).
        // Cada célula vai de -0,5 a +0,5 em volta do centro dela.
        float esquerda = xMin - 0.5f;
        float embaixo = alturaDoMapa - 1 - linhaMax - 0.5f;
        var area = new Rect(esquerda, embaixo, xMax - xMin + 1, linhaMax - linhaMin + 1);

        var objeto = new GameObject(tipo == Tipo.Rajada ? "VentoDaRam" : "Redemoinho");
        objeto.transform.SetParent(raiz, false);
        objeto.transform.position = area.center;
        var vento = objeto.AddComponent<VentoDaRam>();
        vento.Montar(tipo, area, tipo == Tipo.Redemoinho && xMin % 2 == 1);
        return vento;
    }

    public void Montar(Tipo novoTipo, Rect novaArea, bool ehTraicoeiro)
    {
        tipo = novoTipo;
        area = novaArea;
        traicoeiro = ehTraicoeiro;
        estado = tipo == Tipo.Rajada ? Estado.Calmo : Estado.Ligado;
        filtroSolido = new ContactFilter2D { useTriggers = false };

        // O "ar" da zona: o desenho tem 1x1 bloco e é esticado até o tamanho da zona.
        // Fica atrás dos blocos (ordem -6) e mostra onde tem vento mesmo quando está calmo.
        string desenhoDoAr = tipo == Tipo.Rajada ? "ar_da_rajada" : "ar_do_redemoinho";
        var ar = ConstrutorDeFase.Visual("Ar", transform, area.center, desenhoDoAr, -6, false);
        ar.transform.localScale = new Vector3(area.width, area.height, 1f);
        fundo = ar.GetComponent<SpriteRenderer>();
        fundo.color = new Color(1f, 1f, 1f, 0f);

        // Os ciscos (folhas e riscos de vento) são reaproveitados: quanto maior a zona, mais ciscos.
        float porCelula = tipo == Tipo.Rajada ? 0.45f : 0.5f;
        int quantidade = Mathf.Clamp(Mathf.RoundToInt(area.width * area.height * porCelula), 10, 64);
        for (int i = 0; i < quantidade; i++) ciscos.Add(NovoCisco());
    }

    // ------------------------------------------------------------------ ritmo (a cada quadro)

    void Update()
    {
        float dt = Time.deltaTime; // na pausa é 0: o vento congela sozinho
        tempoDesdeOEmpurrao += dt;
        tempoDesdeQueLargou += dt;

        if (tipo == Tipo.Rajada) AtualizarRajada(dt);
        else AtualizarRedemoinho(dt);

        VerificarConquista();
        Animar(dt);
    }

    // Em que pedaço do ritmo a rajada está (todas as zonas usam o mesmo relógio, então sopram juntas).
    void AtualizarRajada(float dt)
    {
        relogio += dt;
        float periodo = tempoCalmo + tempoDeAviso + tempoDeRajada;
        float t = Mathf.Repeat(relogio, periodo);
        Estado antes = estado;

        if (t < tempoCalmo)
        {
            estado = Estado.Calmo;
            forcaAgora = 0f;
            avisoAgora = 0f;
        }
        else if (t < tempoCalmo + tempoDeAviso)
        {
            estado = Estado.Aviso;
            forcaAgora = 0f;
            avisoAgora = (t - tempoCalmo) / tempoDeAviso;
        }
        else
        {
            estado = Estado.Soprando;
            float soprando = t - tempoCalmo - tempoDeAviso; // há quanto tempo está soprando
            forcaAgora = Mathf.Clamp01(Mathf.Min(soprando / tempoParaEncher, (tempoDeRajada - soprando) / tempoParaAcalmar));
            avisoAgora = 1f;
        }

        if (estado == antes) return;
        if (estado == Estado.Aviso) TocarSomDaRajada(); // o "fuuu" vai subindo junto com o aviso
        if (estado == Estado.Soprando && jogadorDentro && !abrigado)
            CameraSeguir.Tremer(0.05f, 0.15f); // a rajada chegou com tudo
    }

    // O redemoinho normal fica ligado para sempre. O traiçoeiro acorda quando você chega perto.
    void AtualizarRedemoinho(float dt)
    {
        if (!traicoeiro) return;
        if (!acordado)
        {
            if (DistanciaAte(out _) < distanciaParaAcordar)
            {
                acordado = true;
                timer = primeiroTempoLigado;
            }
            return;
        }

        timer -= dt;
        if (timer > 0f) return;
        switch (estado)
        {
            case Estado.Ligado: MudarPara(Estado.Engasgando, tempoEngasgando); break;
            case Estado.Engasgando: MudarPara(Estado.Desligado, tempoDesligado); break;
            default: MudarPara(Estado.Ligado, tempoLigado); break;
        }
    }

    void MudarPara(Estado novo, float duracao)
    {
        estado = novo;
        timer += duracao; // soma (em vez de trocar) para o ritmo não "escorregar" com o tempo
        bool perto = DistanciaAte(out _) < 10f;

        switch (novo)
        {
            case Estado.Engasgando:
                if (perto) GerenciadorDoJogo.Som("armadilha", 0.35f);
                break;

            case Estado.Desligado:
                if (jogadorDentro)
                {
                    GerenciadorDoJogo.Som("risada", 0.5f); // o Puck rindo de quem confiou
                    tempoDesdeQueLargou = 0f;
                }
                break;

            case Estado.Ligado:
                if (perto) TocarSom(SomDoSopro(), 0.5f);
                for (int i = 0; i < 8; i++) NascerNoRedemoinho(area.yMin + Random.Range(0f, 1.5f)); // volta com tudo
                break;
        }
    }

    // ------------------------------------------------------------------ física (a cada passo)

    void FixedUpdate()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        bool vivo = GerenciadorDoJogo.JogadorVivo(out _);
        bool estavaDentro = jogadorDentro;
        jogadorDentro = vivo && jogador != null && area.Contains(jogador.Corpo.position);
        if (!jogadorDentro) return;

        Vector2 posicao = jogador.Corpo.position; // a posição da física (o desenho pode estar interpolado)
        if (tipo == Tipo.Rajada)
        {
            abrigado = Abrigado(posicao);
            if (forcaAgora <= 0f || abrigado) return;
            jogador.Empurrar(-forcaDaRajada * forcaAgora);
            if (forcaAgora > 0.5f) tempoDesdeOEmpurrao = 0f;
            Arrastar(jogador);
        }
        else if (estado != Estado.Desligado)
        {
            if (!estavaDentro) // acabou de entrar: "fuu!"
            {
                TocarSom(SomDoSopro(), 0.4f);
                Efeitos.Poeira(transform.parent, (Vector3)posicao + Vector3.down * 0.45f, 3, 1.5f);
            }
            Levantar(jogador, posicao);
        }
    }

    // Dentro do redemoinho a gravidade some e a velocidade vertical vai até a velocidade de subida
    // (que diminui perto do topo da zona, para o Subaru parar "boiando" lá em cima).
    void Levantar(Jogador jogador, Vector2 posicao)
    {
        float dt = Time.fixedDeltaTime;
        Rigidbody2D corpo = jogador.Corpo;
        Vector2 v = corpo.linearVelocity;

        float alvo = velocidadeDeSubida * Mathf.Clamp01((area.yMax - posicao.y) / freioNoTopo);
        float aceleracao = v.y < alvo ? aceleracaoParaCima : freioParaBaixo;
        v.y = Mathf.MoveTowards(v.y, alvo, aceleracao * dt);

        // A física ainda vai aplicar a gravidade neste passo: devolve antes o que ela vai tirar.
        v.y += GravidadeDaUnity * corpo.gravityScale * dt;
        corpo.linearVelocity = v;
    }

    // O vento não atravessa paredes: tem algo sólido logo à direita, na altura do corpo?
    // (o próprio Subaru e os bichos não contam)
    bool Abrigado(Vector2 posicao)
    {
        int quantidade = Physics2D.Raycast(posicao, Vector2.right, filtroSolido, acertos, alcanceDoAbrigo);
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = acertos[i].collider;
            if (outro.GetComponent<Jogador>() == null && !Inimigo.EhBicho(outro)) return true;
        }
        return false;
    }

    // Empurrado pelo vento no chão, o Subaru vai "patinando" e levantando poeira.
    void Arrastar(Jogador jogador)
    {
        timerPoeira -= Time.fixedDeltaTime;
        if (!jogador.NoChao || timerPoeira > 0f) return;
        timerPoeira = 0.12f;
        Efeitos.Poeira(transform.parent, jogador.transform.position + new Vector3(0.25f, -0.45f, 0f), 1, 1.5f);
    }

    // Distância na horizontal do jogador até a zona (0 = está em cima ou embaixo dela). Infinito se não tem jogador.
    float DistanciaAte(out Vector2 jogador)
    {
        if (!GerenciadorDoJogo.JogadorVivo(out jogador)) return float.MaxValue;
        return Mathf.Max(area.xMin - jogador.x, 0f, jogador.x - area.xMax);
    }

    // Conquista: morreu logo depois de ser soprado (ou largado pelo redemoinho)? Foi a Ram.
    void VerificarConquista()
    {
        if (conquistaVerificada) return;
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador == null || !jogador.Morto) return;
        conquistaVerificada = true;
        if (tempoDesdeOEmpurrao < 1.2f || tempoDesdeQueLargou < 2f) Conquistas.Desbloquear("vento_da_ram");
    }

    // ------------------------------------------------------------------ ciscos (folhas e riscos de vento)

    // Cada folha ou risco de vento que passa voando. São reaproveitados: nascem, voam, somem e nascem de novo.
    class Cisco
    {
        public SpriteRenderer desenho;
        public Vector2 posicao, velocidade;
        public float vida, duracao, alfa;
        public float angulo, giro;    // a folha vai girando
        public float fase;            // para cada um balançar num ritmo diferente
        public float centro, raio;    // redemoinho: o eixo da "espiral" e o quanto ela abre
        public float comprimento;     // risco: mais rápido = mais comprido
        public bool folha;
        public bool Ativo => desenho.gameObject.activeSelf;
    }

    Cisco NovoCisco()
    {
        var objeto = new GameObject("Cisco");
        objeto.transform.SetParent(transform, false);
        var cisco = new Cisco { desenho = objeto.AddComponent<SpriteRenderer>() };
        objeto.SetActive(false);
        return cisco;
    }

    Cisco Livre()
    {
        foreach (Cisco cisco in ciscos)
            if (!cisco.Ativo) return cisco;
        return null; // todos voando: esse fica para depois
    }

    Cisco Acender(bool folha, Vector2 posicao, Vector2 velocidade, float duracao, float alfa)
    {
        Cisco c = Livre();
        if (c == null) return null;
        c.folha = folha;
        c.posicao = posicao;
        c.velocidade = velocidade;
        c.vida = 0f;
        c.duracao = duracao;
        c.alfa = alfa;
        c.fase = Random.Range(0f, 10f);
        c.angulo = folha ? Random.Range(0f, 360f) : 0f;
        c.giro = folha ? Random.Range(-540f, 540f) : 0f;
        c.comprimento = 1f;
        c.desenho.sprite = FabricaDeSprites.Pegar(folha ? (Random.value < 0.35f ? "folha_seca" : "folha") : "risco_de_vento");
        c.desenho.sortingOrder = folha ? 11 : 8; // folhas na frente do Subaru (10), riscos atrás
        c.desenho.gameObject.SetActive(true);
        return c;
    }

    static void Apagar(Cisco c) => c.desenho.gameObject.SetActive(false);

    void Animar(float dt)
    {
        // Longe da câmera não precisa animar nada (mas o ritmo continua contando lá em cima).
        bool perto = DistanciaAte(out _) < DistanciaParaAnimar;
        if (!perto)
        {
            if (!ciscosEscondidos)
            {
                foreach (Cisco c in ciscos) Apagar(c);
                ciscosEscondidos = true;
            }
            return;
        }
        if (ciscosEscondidos && tipo == Tipo.Redemoinho && estado != Estado.Desligado)
        {
            // Chegou perto: o redemoinho já aparece cheio (senão ele "liga" na sua frente).
            for (int i = 0; i < ciscos.Count / 2; i++) NascerNoRedemoinho(Random.Range(area.yMin, area.yMax - 0.5f));
        }
        ciscosEscondidos = false;
        if (solida == null) MontarGrade();

        if (tipo == Tipo.Rajada) AnimarRajada(dt);
        else AnimarRedemoinho(dt);
    }

    // ---------- rajada

    void AnimarRajada(float dt)
    {
        // Quantos ciscos nascem por segundo (proporcional ao tamanho da zona).
        float tamanho = area.width * area.height;
        float porSegundo;
        if (estado == Estado.Calmo) porSegundo = 0.03f * tamanho;                                    // folhinhas preguiçosas
        else if (estado == Estado.Aviso) porSegundo = Mathf.Lerp(0.05f, 0.25f, avisoAgora) * tamanho;  // o vento chegando
        else porSegundo = Mathf.Lerp(0.08f, 0.35f, forcaAgora) * tamanho;                           // RAJADA
        acumulador += porSegundo * dt;
        while (acumulador >= 1f)
        {
            acumulador -= 1f;
            NascerNaRajada();
        }

        foreach (Cisco c in ciscos)
            if (c.Ativo) MoverNaRajada(c, dt);

        // O "ar" fica um pouquinho mais forte quando está soprando.
        float alvo = 0.05f + 0.05f * avisoAgora + 0.06f * forcaAgora;
        Color cor = fundo.color;
        cor.a = Mathf.MoveTowards(cor.a, alvo, dt * 0.3f);
        fundo.color = cor;
    }

    void NascerNaRajada()
    {
        // Um lugar qualquer da zona que não seja sólido nem esteja no abrigo de uma parede (3 tentativas).
        Vector2 lugar = Vector2.zero;
        bool achou = false;
        for (int tentativa = 0; tentativa < 3 && !achou; tentativa++)
        {
            lugar = new Vector2(Random.Range(area.xMin, area.xMax), Random.Range(area.yMin + 0.1f, area.yMax - 0.1f));
            achou = !Solida(lugar) && !NoAbrigo(lugar);
        }
        if (!achou) return;

        if (estado == Estado.Calmo)
        {
            // folha caindo devagarinho, quase parada
            Acender(true, lugar, new Vector2(-Random.Range(0.5f, 1.2f), -Random.Range(0.1f, 0.4f)), Random.Range(1.8f, 2.8f), 0.9f);
        }
        else if (estado == Estado.Aviso)
        {
            // os primeiros riscos, ainda fraquinhos, e umas folhas sendo levantadas
            if (Random.value < 0.7f)
                Acender(false, lugar, new Vector2(-Random.Range(6f, 10f), 0f), 0.45f, 0.25f + 0.35f * avisoAgora);
            else
                Acender(true, lugar, new Vector2(-Random.Range(3f, 5f), Random.Range(-0.3f, 0.6f)), 1f, 0.95f);
        }
        else if (Random.value < 0.7f)
        {
            // RAJADA: riscos compridos passando rápido...
            Cisco c = Acender(false, lugar, new Vector2(-Random.Range(16f, 22f), Random.Range(-0.4f, 0.4f)), Random.Range(0.35f, 0.55f), 0.8f);
            if (c != null) c.comprimento = Random.Range(1.1f, 1.7f);
        }
        else
        {
            // ...e folhas rodopiando
            Acender(true, lugar, new Vector2(-Random.Range(10f, 14f), Random.Range(-1f, 1f)), 1f, 1f);
        }
    }

    void MoverNaRajada(Cisco c, float dt)
    {
        c.vida += dt;
        c.posicao += c.velocidade * dt;
        if (c.folha)
        {
            c.posicao.y += Mathf.Sin(c.vida * 7f + c.fase) * 0.8f * dt; // folha "dançando" no ar
            c.angulo += c.giro * dt;
        }

        // Some quando acaba a vida, quando sai pela esquerda da zona ou quando bate numa parede.
        float p = c.vida / c.duracao;
        if (p >= 1f || c.posicao.x < area.xMin || Solida(c.posicao))
        {
            Apagar(c);
            return;
        }
        float alfa = c.alfa * Mathf.Clamp01(p / 0.15f) * Mathf.Clamp01((1f - p) / 0.3f) // aparece e some aos poucos
                     * Mathf.Clamp01((c.posicao.x - area.xMin) / 0.6f);                 // apaga na beirada da zona
        Desenhar(c, alfa, Color.white, new Vector3(c.comprimento, 1f, 1f));
    }

    // ---------- redemoinho

    void AnimarRedemoinho(float dt)
    {
        float tamanho = area.width * area.height;
        float porSegundo = estado == Estado.Ligado ? 0.2f * tamanho : estado == Estado.Engasgando ? 0.06f * tamanho : 0f;
        acumulador += porSegundo * dt;
        while (acumulador >= 1f)
        {
            acumulador -= 1f;
            NascerNoRedemoinho(area.yMin + Random.Range(0f, 0.6f));
        }

        foreach (Cisco c in ciscos)
            if (c.Ativo) MoverNoRedemoinho(c, dt);

        // O "ar": ligado, ele pulsa de leve; engasgando, pisca; desligado, some.
        float alvo;
        if (estado == Estado.Ligado) alvo = 0.09f + 0.02f * Mathf.Sin(Time.time * 6f);
        else if (estado == Estado.Engasgando) alvo = Random.value < 0.5f ? 0.02f : 0.1f;
        else alvo = 0f;
        Color cor = fundo.color;
        cor.a = estado == Estado.Engasgando ? alvo : Mathf.MoveTowards(cor.a, alvo, dt * 0.4f);
        fundo.color = cor;
    }

    void NascerNoRedemoinho(float y)
    {
        // O eixo da espiral fica em qualquer lugar da largura; a espiral abre no máximo até a beirada.
        float meiaLargura = area.width / 2f;
        float centro = Random.Range(area.xMin + 0.3f, area.xMax - 0.3f);
        float raio = Random.Range(0.1f, Mathf.Clamp(Mathf.Min(centro - area.xMin, area.xMax - centro) - 0.1f, 0.15f, Mathf.Min(1.2f, meiaLargura)));

        bool folha = Random.value < 0.35f;
        float subida = folha ? Random.Range(3f, 4.5f) : Random.Range(5.5f, 8.5f);
        float duracao = (area.yMax - y) / subida + 0.2f;
        Cisco c = Acender(folha, new Vector2(centro, y), new Vector2(0f, subida), duracao, folha ? 0.95f : 0.55f);
        if (c == null) return;
        c.centro = centro;
        c.raio = raio;
        c.angulo = folha ? c.angulo : -90f; // o risco fica em pé, com o "cachinho" para cima
        c.comprimento = Random.Range(0.6f, 1f);
    }

    void MoverNoRedemoinho(Cisco c, float dt)
    {
        c.vida += dt;
        if (estado == Estado.Desligado)
        {
            // sem vento, tudo cai e some
            c.velocidade.y -= 25f * dt;
            c.alfa -= 2f * dt;
        }
        c.posicao.y += c.velocidade.y * dt;

        // Gira em espiral em volta do eixo. O cosseno diz se o cisco está "na frente" ou "atrás" do eixo
        // (um 3D de mentirinha: atrás ele fica mais apagado e menor).
        float giro = c.vida * (c.folha ? 5f : 3.5f) + c.fase;
        float frente = Mathf.Cos(giro);
        c.posicao.x = c.centro + Mathf.Sin(giro) * c.raio;
        if (c.folha) c.angulo += c.giro * dt;

        float pertoDoTopo = Mathf.Clamp01((area.yMax - c.posicao.y) / 0.6f); // apaga no topo da zona
        if (c.vida >= c.duracao || c.alfa <= 0f || pertoDoTopo <= 0f || c.posicao.y < area.yMin - 1f)
        {
            Apagar(c);
            return;
        }

        bool engasgando = estado == Estado.Engasgando;
        Vector2 tremida = engasgando ? Random.insideUnitCircle * 0.08f : Vector2.zero;
        Color tom = engasgando ? new Color(0.55f, 0.55f, 0.62f) : Color.white; // engasgando: fica cinza
        float alfa = c.alfa * Mathf.Clamp01(c.vida / 0.2f) * pertoDoTopo * (0.65f + 0.35f * frente);
        float escala = 0.85f + 0.15f * frente;
        Vector3 tamanho = c.folha ? new Vector3(escala, escala, 1f) : new Vector3(c.comprimento * escala, 1f, 1f);

        Vector2 guardada = c.posicao;
        c.posicao += tremida;
        Desenhar(c, alfa, tom, tamanho);
        c.posicao = guardada;
    }

    void Desenhar(Cisco c, float alfa, Color tom, Vector3 tamanho)
    {
        Transform t = c.desenho.transform;
        t.position = c.posicao;
        t.localRotation = Quaternion.Euler(0f, 0f, c.angulo);
        t.localScale = tamanho;
        c.desenho.color = new Color(tom.r, tom.g, tom.b, Mathf.Clamp01(alfa));
    }

    // ---------- grade das paredes (para os ciscos não atravessarem blocos)

    // Anota quais células da zona (e das 2 colunas à direita dela, por causa do abrigo) têm algo sólido.
    // É feita uma vez só, na primeira vez que a zona aparece (aí a física da fase já está montada).
    void MontarGrade()
    {
        colunaDaGrade = Mathf.RoundToInt(area.xMin + 0.5f);
        linhaDaGrade = Mathf.RoundToInt(area.yMin + 0.5f);
        int colunas = Mathf.RoundToInt(area.width) + 2;
        int linhas = Mathf.RoundToInt(area.height);
        solida = new bool[colunas, linhas];
        for (int x = 0; x < colunas; x++)
        {
            for (int y = 0; y < linhas; y++)
            {
                var centro = new Vector2(colunaDaGrade + x, linhaDaGrade + y);
                int quantidade = Physics2D.OverlapBox(centro, new Vector2(0.5f, 0.5f), 0f, filtroSolido, encostados);
                for (int i = 0; i < quantidade; i++)
                {
                    Collider2D outro = encostados[i];
                    if (outro.GetComponent<Jogador>() == null && !Inimigo.EhBicho(outro)) solida[x, y] = true;
                }
            }
        }
    }

    bool SolidaNaGrade(int x, int y)
    {
        if (solida == null || x < 0 || y < 0 || x >= solida.GetLength(0) || y >= solida.GetLength(1)) return false;
        return solida[x, y];
    }

    bool Solida(Vector2 ponto) =>
        SolidaNaGrade(Mathf.RoundToInt(ponto.x) - colunaDaGrade, Mathf.RoundToInt(ponto.y) - linhaDaGrade);

    // Até 2 células à direita tem parede? Então o vento não passa aqui (igual ao abrigo do Subaru).
    bool NoAbrigo(Vector2 ponto)
    {
        int x = Mathf.RoundToInt(ponto.x) - colunaDaGrade, y = Mathf.RoundToInt(ponto.y) - linhaDaGrade;
        return SolidaNaGrade(x + 1, y) || SolidaNaGrade(x + 2, y);
    }

    // ------------------------------------------------------------------ sons

    void TocarSom(AudioClip clipe, float volume)
    {
        if (fonte == null)
        {
            fonte = gameObject.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
        }
        fonte.PlayOneShot(clipe, volume * Opcoes.Efeitos);
    }

    // As zonas sopram juntas: a primeira que estiver perto do jogador toca, as outras ficam quietas.
    void TocarSomDaRajada()
    {
        float distancia = DistanciaAte(out _);
        if (distancia > 14f || Time.time - ultimoSomDeRajada < 1f) return;
        ultimoSomDeRajada = Time.time;
        TocarSom(SomDaRajada(), Mathf.Lerp(0.9f, 0.35f, distancia / 14f));
    }

    // "Fuuuuuu": a rajada inteira (sobe durante o aviso, sopra 2 s e acaba).
    static AudioClip SomDaRajada()
    {
        if (somDaRajada == null) somDaRajada = CriarSomDeVento("rajada", 2.8f, 0.5f, 0.35f, 7);
        return somDaRajada;
    }

    // "Fu!": curtinho, para quando o Subaru entra no redemoinho (ou ele volta a ligar).
    static AudioClip SomDoSopro()
    {
        if (somDoSopro == null) somDoSopro = CriarSomDeVento("sopro", 0.45f, 0.06f, 0.3f, 3);
        return somDoSopro;
    }

    // Som de vento por código: chiado (ruído) passando por um filtro que abre e fecha devagar (é isso que faz
    // o vento "uivar"), mais um assobio fininho que sobe e desce. Igual aos sons da FabricaDeSons, sem arquivo.
    static AudioClip CriarSomDeVento(string nome, float duracao, float subida, float volume, int semente)
    {
        const int Taxa = 44100;
        int total = Mathf.CeilToInt(duracao * Taxa);
        var dados = new float[total];
        var sorteio = new System.Random(semente);
        float filtrado = 0f, filtrado2 = 0f, faseDoAssobio = 0f, maior = 0.0001f;

        for (int i = 0; i < total; i++)
        {
            float t = i / (float)Taxa;
            float ruido = (float)(sorteio.NextDouble() * 2.0 - 1.0);
            float abertura = 0.015f + 0.05f * (0.5f + 0.5f * Mathf.Sin(t * 5.3f) * Mathf.Sin(t * 2.1f + 1f));
            filtrado += (ruido - filtrado) * abertura;     // filtro "passa-baixa": deixa o chiado grave e macio
            filtrado2 += (filtrado - filtrado2) * abertura;

            float frequencia = 520f + 180f * Mathf.Sin(t * 3.1f) + 90f * Mathf.Sin(t * 7.7f);
            faseDoAssobio += 2f * Mathf.PI * frequencia / Taxa;
            float assobio = Mathf.Sin(faseDoAssobio) * 0.012f;

            float envelope = Mathf.Clamp01(t / subida) * Mathf.Clamp01((duracao - t) / 0.35f);
            dados[i] = (filtrado2 + assobio) * envelope;
            maior = Mathf.Max(maior, Mathf.Abs(dados[i]));
        }

        for (int i = 0; i < total; i++) dados[i] *= volume / maior; // ajusta o volume (o filtro "come" o som)
        var clipe = AudioClip.Create(nome, total, 1, Taxa, false);
        clipe.SetData(dados, 0);
        return clipe;
    }

    // ------------------------------------------------------------------ desenhos

    // Registra os desenhos do vento na FabricaDeSprites (funcionam com Pegar("vento") em qualquer lugar,
    // inclusive no mapa do mundo, onde o "vento" é o ícone da fase).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        FabricaDeSprites.Registrar("vento", () => FabricaDeSprites.DeArte(ArteVento, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("folha", () => FabricaDeSprites.DeArte(ArteFolha, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("folha_seca", () => FabricaDeSprites.DeArte(Recolorir(ArteFolha, "aggG", "yoob"), FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("risco_de_vento", () => FabricaDeSprites.DeArte(ArteRisco, FabricaDeSprites.Centro));
        FabricaDeSprites.Registrar("ar_da_rajada", () => FabricaDeSprites.Procedural(16, 16, FabricaDeSprites.Centro, (x, y) => CorDoAr(x, y, false)));
        FabricaDeSprites.Registrar("ar_do_redemoinho", () => FabricaDeSprites.Procedural(16, 16, FabricaDeSprites.Centro, (x, y) => CorDoAr(x, y, true)));
    }

    // O ícone do mapa: um redemoinho (funil de vento girando, com o lado direito na sombra 's'), com uma
    // folha e uma pétala rosa (a cor do cabelo da Ram) voando em volta.
    static readonly string[] ArteVento =
    {
        "..kkkkkkkkkkkk..",
        ".kwwwwwlllllwsk.",
        ".kllwwwwwwwllsk.",
        "..kwlllwwwwwsk..",
        "..kwwwwllllwsk..",
        "...kllwwwwwsk.kk",
        "...kwwllllwskkpk",
        "....kwwwwwsk.kk.",
        ".kk.klllwwsk....",
        "kgak.kwwlsk.....",
        ".kk..kllwsk.....",
        "......kwwsk.....",
        "......kllsk.....",
        ".......kwsk.....",
        "........ksk.....",
        "........kk......",
    };

    // Folha (verde). A "folha_seca" é a mesma, pintada de laranja (veja Recolorir).
    static readonly string[] ArteFolha =
    {
        "......kk",
        "....kkak",
        "...kaggk",
        "..kaggGk",
        ".kagGGk.",
        ".kgGGk..",
        "kdkkk...",
        "kk......",
    };

    // Risco de vento: um traço branco com um cachinho na frente (a frente é a esquerda, para onde o vento vai).
    // Sem contorno preto: é ar, não é coisa.
    static readonly string[] ArteRisco =
    {
        "..www...........",
        ".w...w..........",
        ".w..lwwwwwwwwll.",
        "..ll............",
    };

    // Troca as letras de um desenho (de[i] vira para[i]). Serve para fazer variações de cor.
    static string[] Recolorir(string[] arte, string de, string para)
    {
        var nova = new string[arte.Length];
        for (int i = 0; i < arte.Length; i++)
        {
            char[] letras = arte[i].ToCharArray();
            for (int j = 0; j < letras.Length; j++)
            {
                int indice = de.IndexOf(letras[j]);
                if (indice >= 0) letras[j] = para[indice];
            }
            nova[i] = new string(letras);
        }
        return nova;
    }

    // O "ar" da zona: branco, com as bordas apagando aos poucos. O do redemoinho é mais forte embaixo
    // (de onde o vento sai); o da rajada é mais forte à direita (de onde o vento vem).
    static Color32 CorDoAr(int x, int y, bool redemoinho)
    {
        float bordaX = Mathf.Clamp01(Mathf.Min(x + 0.5f, 15.5f - x) / 3f);
        float bordaY = Mathf.Clamp01(Mathf.Min(y + 0.5f, 15.5f - y) / 3f);
        float lado = redemoinho ? Mathf.Lerp(1f, 0.4f, y / 15f) : Mathf.Lerp(0.4f, 1f, x / 15f);
        return new Color32(255, 255, 255, (byte)(255 * bordaX * bordaY * lado));
    }
}
