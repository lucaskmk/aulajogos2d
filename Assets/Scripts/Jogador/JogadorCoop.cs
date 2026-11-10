using UnityEngine;

// O COOP dentro do Grumete (a outra metade do Jogador.cs; a tripulação e os aparelhos ficam em Tripulacao.cs).
//
//  - BALÃO DE RESGATE: morreu, ou ficou para trás e saiu da tela? Com um companheiro ainda vivo, o Grumete não
//    volta para o checkpoint: aparece pendurado num balão de festa da cor dele (amarrado por uma cordinha), que vem
//    boiando até o companheiro mais perto e fica em cima da cabeça dele. Um companheiro encostando (pulando no
//    Grumete pendurado ou no balão) estoura o balão e devolve o Grumete ao jogo.
//    (no código o estado se chama "NaBolha", do primeiro desenho; o desenho é o balão amarrado)
//    Dentro do balão dá para "sacudir" (as teclas de andar empurram o balão para os lados).
//    Segurar a tecla do BALÃO vira balão de propósito (para quem ficou preso num lugar sem saída).
//    Se TODOS estiverem mortos ou no balão, a tripulação perdeu: volta todo mundo para o checkpoint.
//  - PULO NA CABEÇA: caiu em cima do companheiro? Quica. Segurando PULAR, o pulo sai mais alto que o normal.
//  - EMPURRÃO: a tecla de EMPURRAR joga para longe o companheiro que estiver na sua frente. É um jogo de cilada. :)
public partial class Jogador
{
    [Header("Coop: balão de resgate")]
    [Tooltip("Quanto tempo depois de morrer o Grumete reaparece dentro do balão.")]
    public float tempoAteOBalao = 0.9f;
    [Tooltip("A velocidade com que o balão vai boiando até o companheiro.")]
    public float velocidadeDoBalao = 3.2f;
    [Tooltip("O quanto as teclas de andar empurram o balão para os lados (\"sacudir\").")]
    public float forcaDeSacudir = 2.6f;
    [Tooltip("O balão fica boiando esta altura acima do companheiro: ele precisa pular para estourar.")]
    public float alturaDoBalao = 1.9f;
    [Tooltip("O balão recém-criado não estoura por este tempo (senão estourava no mesmo lugar em que você morreu).")]
    public float tempoParaPoderEstourar = 0.7f;
    [Tooltip("Segurando a tecla do BALÃO por este tempo, o Grumete vira balão de propósito.")]
    public float tempoSegurandoParaVirarBalao = 0.45f;
    [Tooltip("Saiu do balão: por este tempo nada te mata (fica piscando).")]
    public float invencivelDepoisDoBalao = 1f;

    [Header("Coop: pulo na cabeça e empurrão")]
    [Tooltip("Pulo na cabeça do companheiro SEGURANDO PULAR: esta vez a força do pulo normal.")]
    public float forcaDoPuloNaCabeca = 1.15f;
    [Tooltip("Caiu na cabeça do companheiro sem segurar PULAR: só um quique (e quica de novo toda vez que cair nele).")]
    public float quiqueNaCabeca = 8f;
    public float forcaDoEmpurrao = 11f;
    public float esperaDoEmpurrao = 0.5f;

    // Qual jogador é este (0 = P1 ... 3 = P4).
    public int Indice { get; private set; }
    public bool NaBolha { get; private set; }
    public Collider2D Colisor => colisor;
    public Tripulacao.Marujo Marujo => marujo;
    // A velocidade horizontal "alisada" (meio segundo): é quem está puxando a frente que leva a câmera.
    public float VelocidadeSuave => velocidadeSuave;
    // Há quanto tempo ele está fora da tela (quem conta é o GerenciadorDoJogo: passou do limite, vira balão).
    [System.NonSerialized] public float tempoForaDaTela;

    Tripulacao.Marujo marujo;
    SpriteRenderer balao, etiqueta;
    float timerDoBalao = -1f; // morto: a contagem até virar balão (-1 = não vai virar: a tripulação perdeu)
    float tempoNoBalao;
    Vector2 velocidadeDoVoo;
    float segurandoBalao;
    float impulsoDoEmpurrao, velocidadeSuave;
    float proximoQuique, proximoEmpurrao;

    static readonly Color32 Contorno = new Color32(20, 20, 28, 255);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RegistrarDesenhos()
    {
        // O balão de festa de cada jogador (16 x 34 pixels, com o pé da cordinha no pivô): "balao_resgate_1" a "_4".
        // É um balão AMARRADO no Grumete, cheio e opaco, para não parecer a bolha azul do escudo da Loja.
        for (int i = 0; i < Tripulacao.Cores.Length; i++)
        {
            Color32 cor = Tripulacao.Cores[i];
            FabricaDeSprites.Registrar(SpriteDoBalao(i), () => FabricaDeSprites.Procedural(16, 34, FabricaDeSprites.Base, (x, y) => CorDoBalao(x, y, cor)));
        }
    }

    public static string SpriteDoBalao(int indice) => "balao_resgate_" + (indice % Tripulacao.Cores.Length + 1);

    static readonly Color32 CorDaCorda = new Color32(236, 236, 244, 255);

    // De baixo para cima: a cordinha ondulada (14 pixels), o nó (2) e o balão (uma elipse de 14 x 17, com contorno
    // escuro, um brilho em cima à esquerda e a sombra embaixo à direita).
    static Color32 CorDoBalao(int x, int y, Color32 cor)
    {
        if (y < 14) return x == 7 + (y / 3 % 2) ? CorDaCorda : FabricaDeSprites.Transparente;
        if (y < 16)
        {
            int meia = y == 14 ? 2 : 1; // o nó: mais largo embaixo
            return x >= 8 - meia && x <= 7 + meia ? Tom(cor, 0.62f) : FabricaDeSprites.Transparente;
        }
        float dx = (x - 7.5f) / 7f, dy = (y - 24.5f) / 8.5f, d = dx * dx + dy * dy;
        if (d > 1f) return FabricaDeSprites.Transparente;
        if (d > 0.76f) return Contorno;
        float bx = (x - 5f) / 1.7f, by = (y - 28f) / 2.5f;
        if (bx * bx + by * by < 1f) return Color32.Lerp(cor, new Color32(255, 255, 255, 255), 0.7f); // o brilho
        if (dx - dy > 0.7f) return Tom(cor, 0.72f);                                                  // a sombra
        return cor;
    }

    static Color32 Tom(Color32 cor, float quanto) =>
        new Color32((byte)(cor.r * quanto), (byte)(cor.g * quanto), (byte)(cor.b * quanto), 255);

    // Chamado pelo GerenciadorDoJogo logo depois de criar o Grumete: qual jogador ele é.
    public void Configurar(int indice)
    {
        Indice = indice;
        marujo = Tripulacao.Do(indice);
        spriteParado = FabricaDeSprites.Pegar(Tripulacao.Sprite(indice));
        spriteAndando = FabricaDeSprites.Pegar(Tripulacao.Sprite(indice, andando: true));
        visual.sprite = spriteParado;
        visual.sortingOrder = OrdemDoDesenho;

        Color32 cor = Tripulacao.Cores[indice % Tripulacao.Cores.Length];
        // o balão fica amarrado na cabeça dele (o pé da cordinha é o pivô do desenho)
        balao = ConstrutorDeFase.Visual("Balao", transform, transform.position, SpriteDoBalao(indice), 41, sombra: false).GetComponent<SpriteRenderer>();
        balao.transform.localPosition = AmarraDoBalao;
        balao.transform.localScale = new Vector3(TamanhoDoBalao, TamanhoDoBalao, 1f);
        balao.enabled = false;

        if (!Tripulacao.Coop) return;
        // a etiqueta "P1", "P2"... em cima da cabeça (só no coop)
        var objeto = new GameObject("Etiqueta");
        objeto.transform.SetParent(transform, false);
        objeto.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        objeto.transform.localScale = Vector3.one * 0.75f;
        etiqueta = objeto.AddComponent<SpriteRenderer>();
        etiqueta.sprite = FabricaDeSprites.SpriteDeTexto(Tripulacao.Nome(indice), cor, Contorno);
        etiqueta.sortingOrder = 42;
    }

    // Onde a cordinha está amarrada (em relação ao centro do Grumete) e onde fica o meio do balão, lá em cima.
    static readonly Vector3 AmarraDoBalao = new Vector3(0f, 0.38f, 0f);
    const float TamanhoDoBalao = 1.3f; // (um pouco maior que o desenho: tem que dar para ver de longe)
    static readonly Vector2 MeioDoBalao = new Vector2(0f, 0.38f + 24.5f / 16f * TamanhoDoBalao);

    // O P1 é desenhado na frente dos outros.
    int OrdemDoDesenho => 10 - Indice;

    // ------------------------------------------------------------------ a cada quadro (vivo)

    void AtualizarCoop()
    {
        if (!Tripulacao.Coop) return;

        // virar balão de propósito: segurando a tecla (só se tiver alguém para estourar)
        if (marujo.BalaoSegurando() && GerenciadorDoJogo.TemOutroAtivo(this))
        {
            segurandoBalao += Time.deltaTime;
            if (segurandoBalao >= tempoSegurandoParaVirarBalao) EntrarNoBalao();
        }
        else segurandoBalao = 0f;

        if (!NaBolha && marujo.EmpurraoApertou() && Time.time >= proximoEmpurrao) EmpurrarQuemEstaNaFrente();
    }

    // ------------------------------------------------------------------ empurrão

    void EmpurrarQuemEstaNaFrente()
    {
        proximoEmpurrao = Time.time + esperaDoEmpurrao;
        float lado = visual.flipX ? -1f : 1f;
        escala = new Vector2(1.25f, 0.85f);
        bool acertou = false;
        foreach (Jogador outro in GerenciadorDoJogo.Jogadores)
        {
            if (outro == this || outro.Morto || outro.congelado) continue;
            Vector2 d = outro.Corpo.position - Corpo.position;
            if (d.x * lado < -0.25f || Mathf.Abs(d.x) > 1.3f || Mathf.Abs(d.y) > 0.9f) continue;
            outro.LevarEmpurrao(lado);
            acertou = true;
        }
        Efeitos.Poeira(transform.parent, transform.position + new Vector3(lado * 0.5f, -0.2f, 0f), acertou ? 6 : 2, 2.5f);
        if (!acertou)
        {
            GerenciadorDoJogo.Som("pulo", 0.35f);
            return;
        }
        marujo.empurroes++;
        GerenciadorDoJogo.Som("pancada", 0.5f);
    }

    // O companheiro empurrou: sai voando para o lado (o impulso entra na física junto com o vento).
    public void LevarEmpurrao(float lado)
    {
        if (Morto || congelado) return;
        impulsoDoEmpurrao = lado * forcaDoEmpurrao;
        Corpo.linearVelocity = new Vector2(Corpo.linearVelocity.x, Mathf.Max(Corpo.linearVelocity.y, 5f));
        pulando = false;
        timerCoyote = 0f;
        escala = new Vector2(0.8f, 1.2f);
    }

    // ------------------------------------------------------------------ pulo na cabeça do companheiro

    // Chamado no passo da física, com a velocidade que o Grumete vai ter (v): caindo em cima de um companheiro, quica.
    void QuicarNoCompanheiro(ref Vector2 v)
    {
        if (!Tripulacao.Coop || v.y > -1f || Time.time < proximoQuique) return;
        foreach (Jogador outro in GerenciadorDoJogo.Jogadores)
        {
            if (outro == this || outro.Morto) continue;
            Vector2 d = Corpo.position - outro.Corpo.position;
            if (Mathf.Abs(d.x) > 0.55f || d.y < 0.45f || d.y > 1f) continue; // os pés na altura da cabeça dele
            if (v.y - outro.Corpo.linearVelocity.y > -1f) continue;          // caindo juntos: ninguém pisa em ninguém

            // (sem limite: dá para ficar quicando na cabeça do amigo quantas vezes quiser)
            bool forte = segurandoPulo; // segurando PULAR: o "trampolim" (mais alto que o pulo normal)
            proximoQuique = Time.time + 0.1f; // (só para o mesmo pisão não contar duas vezes)
            v.y = forte ? forcaDoPulo * forcaDoPuloNaCabeca : quiqueNaCabeca;
            pulando = forte;
            usouPuloDuplo = false;
            timerCoyote = 0f;
            escala = new Vector2(0.7f, 1.3f);
            outro.escala = new Vector2(1.35f, 0.65f); // o de baixo amassa
            GerenciadorDoJogo.Som("mola", forte ? 0.6f : 0.35f);
            return;
        }
    }

    // ------------------------------------------------------------------ balão de resgate

    // Chamado pelo GerenciadorDoJogo quando este Grumete morre e ainda tem companheiro vivo.
    public void VirarBalaoDepois() => timerDoBalao = tempoAteOBalao;

    // Morto (caindo da tela): espera a animação da morte e reaparece dentro do balão.
    void EsperarOBalao()
    {
        if (timerDoBalao < 0f) return;
        timerDoBalao -= Time.deltaTime;
        if (timerDoBalao > 0f) return;
        timerDoBalao = -1f;
        // a tripulação perdeu (ou passou de fase) enquanto ele caía? Aí não tem balão.
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null || jogo.Estado != EstadoDoJogo.Jogando) return;
        EntrarNoBalao();
    }

    // A tripulação inteira voltou para o save (R): este Grumete cai da tela junto, sem contar outra morte.
    public void MorrerJunto()
    {
        if (morto) return;
        NaBolha = false;
        morto = true;
        timerDoBalao = -1f;
        colisor.enabled = false;
        balao.enabled = false;
        visual.transform.localRotation = Quaternion.identity;
        AplicarEscala(Vector2.one);
        visual.flipY = true;
        visual.sprite = spriteParado;
        visual.sortingOrder = 100;
        Corpo.simulated = true;
        Corpo.gravityScale = gravidade;
        Corpo.linearVelocity = new Vector2(0f, 12f);
    }

    // Vira o balão: sem corpo (nada acerta, nada segura) e boiando por cima de tudo.
    public void EntrarNoBalao()
    {
        if (NaBolha) return;
        morto = false;
        NaBolha = true;
        timerDoBalao = -1f;
        colisor.enabled = false;
        Corpo.linearVelocity = Vector2.zero;
        Corpo.gravityScale = gravidade; // (a morte no buraco zera a gravidade)
        Corpo.simulated = false;
        if (GerenciadorDoJogo.Instancia != null)
            transform.position = GerenciadorDoJogo.Instancia.DentroDaTela(transform.position, 1f);

        visual.flipY = false;
        visual.sortingOrder = 40;
        visual.sprite = spriteParado;
        escala = Vector2.one;
        AplicarEscala(Vector2.one);
        balao.enabled = true;
        if (etiqueta != null) etiqueta.enabled = false; // (a cor do balão já diz de quem ele é)
        tempoNoBalao = 0f;
        velocidadeDoVoo = Vector2.zero;
        segurandoBalao = 0f;
        tempoForaDaTela = 0f;
        timerInvencivel = 0f;
        impulsoDoEmpurrao = 0f;
        GerenciadorDoJogo.Som("pop", 0.5f);
    }

    void AtualizarBalao()
    {
        // pendurado: o balão balança para um lado e o Grumete, como um pêndulo, para o outro
        float t = Time.time * 3f + Indice * 1.7f;
        float balanco = Mathf.Sin(t * 0.7f) - Mathf.Clamp(velocidadeDoVoo.x, -3f, 3f) * 0.6f; // (voando, a corda inclina para trás)
        balao.transform.localRotation = Quaternion.Euler(0f, 0f, 7f * balanco);
        visual.transform.localPosition = new Vector3(0f, 0.04f * Mathf.Sin(t), 0f);
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, -6f * balanco);

        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null || jogo.Estado != EstadoDoJogo.Jogando) return;
        float dt = Time.deltaTime;
        tempoNoBalao += dt;

        // vai boiando até ficar em cima da cabeça do companheiro vivo mais perto
        Vector2 p = transform.position;
        Vector2 querer = Vector2.zero;
        Jogador amigo = GerenciadorDoJogo.JogadorMaisPerto(p, this);
        if (amigo != null)
        {
            Vector2 ate = (Vector2)amigo.transform.position + Vector2.up * alturaDoBalao - p;
            querer = Vector2.ClampMagnitude(ate * 2f, velocidadeDoBalao); // (chegando perto, freia)
        }
        float sacudida = marujo.Horizontal();
        querer.x += sacudida * forcaDeSacudir; // "sacudir": o próprio jogador empurra o balão
        velocidadeDoVoo = Vector2.Lerp(velocidadeDoVoo, querer, 1f - Mathf.Exp(-5f * dt));
        p += velocidadeDoVoo * dt;
        p = jogo.DentroDaTela(p, 0.9f, 3.8f); // nunca fica para trás: a tela leva ele junto (em cima, cabe o balão)
        transform.position = p;
        if (sacudida != 0f) visual.flipX = sacudida < 0f;

        // recém-criado, ele pisca e ainda não estoura
        bool pronto = tempoNoBalao >= tempoParaPoderEstourar;
        Color cor = balao.color;
        cor.a = pronto || (int)(tempoNoBalao * 14f) % 2 == 0 ? 1f : 0.45f;
        balao.color = cor;
        if (!pronto) return;

        foreach (Jogador outro in GerenciadorDoJogo.Jogadores)
        {
            if (outro == this || outro.Morto) continue;
            // encostou no Grumete pendurado ou no balão em cima dele
            Vector2 amigoAqui = outro.transform.position;
            if (Vector2.Distance(amigoAqui, p) > 0.9f && Vector2.Distance(amigoAqui, p + MeioDoBalao) > 0.95f) continue;
            Estourar(outro);
            return;
        }
    }

    // Um companheiro encostou no balão: o Grumete volta ao jogo ali mesmo (piscando, sem poder morrer, por um segundo).
    void Estourar(Jogador salvador)
    {
        NaBolha = false;
        balao.enabled = false;
        if (etiqueta != null) etiqueta.enabled = true;
        visual.transform.localRotation = Quaternion.identity;
        visual.sortingOrder = OrdemDoDesenho;
        Vector2 ondeEstourou = (Vector2)transform.position + MeioDoBalao; // (os pedaços saem de onde o balão estava)

        // o balão atravessa paredes: se estourou "dentro" de uma, o Grumete sai no lugar de quem estourou
        Vector2 lugar = transform.position;
        if (TemParedeEm(lugar)) lugar = salvador.Corpo.position;
        transform.position = lugar;
        Corpo.simulated = !congelado;
        Corpo.position = lugar;
        Corpo.gravityScale = gravidade;
        Corpo.linearVelocity = new Vector2(0f, 6f);
        colisor.enabled = true;
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.SepararDosCompanheiros(this);

        pulando = false;
        usouPuloDuplo = false;
        timerCoyote = 0f;
        timerBufferPulo = 0f;
        quedaMaisRapida = 0f;
        ultimoChao = lugar;
        quantosChaos = proximoChao = 0;
        afundou = false;
        tempoForaDaTela = 0f;
        timerInvencivel = invencivelDepoisDoBalao;
        escala = new Vector2(1.3f, 0.75f);

        salvador.marujo.resgates++;
        GerenciadorDoJogo.Som("pop");
        Color corDasGotas = Tripulacao.Cores[Indice % Tripulacao.Cores.Length]; // os pedaços do balão
        for (int i = 0; i < 12; i++)
        {
            float angulo = Mathf.PI * 2f * i / 12f;
            var direcao = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
            Particula.Criar(transform.parent, (Vector3)(ondeEstourou + direcao * 0.4f), direcao * 4f, 0.4f, 0.5f, corDasGotas);
        }
    }

    bool TemParedeEm(Vector2 lugar)
    {
        int quantidade = Physics2D.OverlapBox(lugar + colisor.offset, new Vector2(0.6f, 0.84f), 0f, filtroSolido, encostados);
        for (int i = 0; i < quantidade; i++)
            if (!NaoEhChao(encostados[i])) return true;
        return false;
    }
}
