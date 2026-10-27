using System.Collections.Generic;
using UnityEngine;

// O Grumete, controlado pelo jogador (a arte fica em FabricaDeSprites).
// Usa Rigidbody2D (física do Unity) com alguns truques de "game feel":
//  - pulo variável: segurar o botão pula mais alto, soltar cedo pula mais baixo;
//  - coyote time: ainda dá pra pular uma fração de segundo depois de sair da beirada;
//  - buffer de pulo: apertar pular um pouquinho antes de tocar o chão também funciona.
// O desenho fica num objeto filho ("Visual") para poder esticar e achatar sem mexer no colisor.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Jogador : MonoBehaviour
{
    [Header("Movimento")]
    public float velocidade = 7f;
    public float aceleracao = 80f;
    public float forcaDoPulo = 14f;
    public float gravidade = 3.5f;
    public float quedaMaxima = 20f;

    [Header("Sensação de controle")]
    public float tempoCoyote = 0.1f;
    public float tempoBufferPulo = 0.12f;

    [Header("Escudo (Loja)")]
    [Tooltip("Depois que o escudo quebra, por quantos segundos nada te mata (fica piscando). Cair num buraco ainda mata.")]
    public float tempoInvencivel = 1.5f;

    public bool Morto { get; private set; }
    public bool NoChao { get; private set; }
    public Rigidbody2D Corpo { get; private set; }

    // Posições x das linhas 'X' da fase. Cada linha cruzada inverte os controles,
    // mas só depois de uma contagem (dá tempo de se preparar; se voltar antes, cancela).
    public List<float> inversores = new List<float>();
    public float tempoParaInverter = 0.4f;
    bool controlesInvertidos;
    float timerInversao = -1f; // contando até inverter (-1 = nada pendente)

    BoxCollider2D colisor;
    SpriteRenderer visual;
    Sprite spriteParado, spriteAndando;

    // "Estica e achata": (1, 1) é o normal; volta sozinho para o normal aos poucos.
    Vector2 escala = Vector2.one;
    float quedaMaisRapida; // para saber com que força ele aterrissou

    float entradaX;
    float empurrao; // vento, esteira... (somado à velocidade que o jogador quer; zera a cada passo da física)
    bool segurandoPulo;
    float timerBufferPulo;
    float timerCoyote;
    bool pulando;
    bool congelado;
    float timerAnimacao;

    // Escudo da Loja (veja Morrer): quem guarda QUANTOS escudos você tem é o GerenciadorDoJogo.
    float timerInvencivel;   // > 0: o escudo acabou de quebrar e nada te mata
    Vector2 ultimoChao;      // onde pisou por último
    // Onde ele pisou nos últimos 2 segundos (uma marca a cada 0,1 s). Se cair num buraco com o escudo, ele volta
    // para uma dessas marcas, um pouco ANTES da beirada (voltar bem na ponta, segurando a seta, caía de novo).
    readonly Vector2[] chaosRecentes = new Vector2[20];
    int quantosChaos, proximoChao;
    float timerDoChao;
    SpriteRenderer bolha;    // a bolha azul em volta dele enquanto tem escudo
    public bool Invencivel => timerInvencivel > 0f;

    readonly List<Collider2D> encostados = new List<Collider2D>();
    ContactFilter2D filtroSolido;

    void Awake()
    {
        Corpo = GetComponent<Rigidbody2D>();
        Corpo.gravityScale = gravidade;
        Corpo.freezeRotation = true;
        Corpo.interpolation = RigidbodyInterpolation2D.Interpolate;
        Corpo.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        colisor = GetComponent<BoxCollider2D>();
        colisor.size = new Vector2(0.62f, 0.86f);
        colisor.edgeRadius = 0.05f;
        colisor.offset = new Vector2(0f, -0.02f);
        colisor.sharedMaterial = new PhysicsMaterial2D("SemAtrito") { friction = 0f, bounciness = 0f };

        visual = ConstrutorDeFase.Visual("Visual", transform, transform.position, "jogador", 10).GetComponent<SpriteRenderer>();
        spriteParado = FabricaDeSprites.Pegar("jogador");
        spriteAndando = FabricaDeSprites.Pegar("jogador_andando");

        filtroSolido = new ContactFilter2D { useTriggers = false };
        SombraNoChao.Adicionar(gameObject, 0.85f);

        bolha = ConstrutorDeFase.Visual("Escudo", transform, transform.position, "bolha_escudo", 11, sombra: false).GetComponent<SpriteRenderer>();
        bolha.enabled = false;
        ultimoChao = transform.position;
    }

    void Update()
    {
        AtualizarEscudo();
        if (Morto || congelado || GerenciadorDoJogo.Pausado) return;

        AtualizarInversao();
        entradaX = Controles.Horizontal() * (controlesInvertidos ? -1f : 1f);
        segurandoPulo = Controles.PuloSegurando();
        if (Controles.PuloApertou()) timerBufferPulo = tempoBufferPulo;

        Animar();
    }

    void FixedUpdate()
    {
        if (Morto || congelado) return;
        float dt = Time.fixedDeltaTime;

        bool estavaNoChao = NoChao;
        NoChao = ChecarChao();
        if (NoChao) GuardarChao(dt);
        if (!NoChao) quedaMaisRapida = Mathf.Min(quedaMaisRapida, Corpo.linearVelocity.y);
        else if (!estavaNoChao) Aterrissou();
        timerCoyote = NoChao ? tempoCoyote : timerCoyote - dt;
        timerBufferPulo -= dt;

        Vector2 v = Corpo.linearVelocity;
        float vento = empurrao;
        empurrao = 0f;
        v.x = Mathf.MoveTowards(v.x, entradaX * velocidade + vento, aceleracao * dt);

        if (timerBufferPulo > 0f && timerCoyote > 0f)
        {
            v.y = forcaDoPulo;
            timerBufferPulo = 0f;
            timerCoyote = 0f;
            pulando = true;
            GerenciadorDoJogo.Som("pulo");
            escala = new Vector2(0.75f, 1.25f); // estica no pulo
            Efeitos.Poeira(transform.parent, transform.position + Vector3.down * 0.45f, 4, 2f);
        }

        // Soltou o botão no meio da subida? Corta o pulo.
        if (pulando && !segurandoPulo && v.y > 0f)
        {
            v.y *= 0.5f;
            pulando = false;
        }
        if (v.y <= 0f) pulando = false;
        if (v.y < -quedaMaxima) v.y = -quedaMaxima;

        Corpo.linearVelocity = v;

        if (transform.position.y < -2f) Morrer(caiuNoBuraco: true);
    }

    void Aterrissou()
    {
        float forca = Mathf.InverseLerp(0f, -quedaMaxima, quedaMaisRapida); // 0 = pulinho, 1 = queda máxima
        quedaMaisRapida = 0f;
        if (forca < 0.2f) return;
        escala = new Vector2(1f + 0.35f * forca, 1f - 0.3f * forca); // achata ao cair
        Efeitos.Poeira(transform.parent, transform.position + Vector3.down * 0.45f, Mathf.RoundToInt(3 + 5 * forca), 2.5f);
    }

    // Invertido quando o jogador está à direita de um número ÍMPAR de linhas 'X'.
    void AtualizarInversao()
    {
        int cruzadas = 0;
        foreach (float x in inversores)
            if (transform.position.x > x) cruzadas++;

        bool invertido = cruzadas % 2 == 1;
        if (invertido == controlesInvertidos)
        {
            if (timerInversao >= 0f) GerenciadorDoJogo.Instancia.Avisar("Ufa. Cancelou.", 1f); // voltou antes de inverter
            timerInversao = -1f;
            return;
        }

        // cruzou a linha: começa a contagem
        if (timerInversao < 0f)
        {
            timerInversao = tempoParaInverter;
            GerenciadorDoJogo.Som("armadilha", 0.5f);
        }
        timerInversao -= Time.deltaTime;
        string oQue = invertido ? "Invertendo os controles" : "Desinvertendo";
        GerenciadorDoJogo.Instancia.Avisar($"{oQue} em {Mathf.CeilToInt(Mathf.Max(timerInversao, 0.01f))}...", 0.2f);
        if (timerInversao > 0f) return;

        timerInversao = -1f;
        controlesInvertidos = invertido;
        GerenciadorDoJogo.Som("risada");
        GerenciadorDoJogo.Instancia.Avisar(invertido ? "CONTROLES INVERTIDOS :)" : "Controles normais... por enquanto.");
    }

    bool ChecarChao()
    {
        if (Corpo.linearVelocity.y > 0.1f) return false;

        Vector2 pes = (Vector2)transform.position + new Vector2(0f, -0.5f);
        int quantidade = Physics2D.OverlapBox(pes, new Vector2(0.6f, 0.1f), 0f, filtroSolido, encostados);
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = encostados[i];
            if (outro != colisor && !Inimigo.EhBicho(outro)) return true;
        }
        return false;
    }

    void Animar()
    {
        if (entradaX != 0f) visual.flipX = entradaX < 0f;

        if (!NoChao)
        {
            visual.sprite = spriteAndando;
        }
        else if (entradaX != 0f)
        {
            timerAnimacao += Time.deltaTime;
            visual.sprite = (int)(timerAnimacao * 10f) % 2 == 0 ? spriteParado : spriteAndando;
        }
        else
        {
            visual.sprite = spriteParado;
        }

        // Volta aos poucos para o tamanho normal. Parado no chão, ele "respira".
        escala = Vector2.Lerp(escala, Vector2.one, 12f * Time.deltaTime);
        float respiracao = NoChao && entradaX == 0f ? 1f + 0.04f * Mathf.Sin(Time.time * 3f) : 1f;
        AplicarEscala(new Vector2(escala.x, escala.y * respiracao));
    }

    // Estica a partir dos pés (o desenho tem o centro no meio, então desce o tanto que encolheu).
    void AplicarEscala(Vector2 e)
    {
        visual.transform.localScale = new Vector3(e.x, e.y, 1f);
        visual.transform.localPosition = new Vector3(0f, -0.5f * (1f - e.y), 0f);
    }

    // ------------------------------------------------------------ usado pelos outros objetos

    // ignorarEscudo: reiniciar a fase (R) "mata" de propósito, aí o escudo não é gasto.
    public void Morrer(bool caiuNoBuraco = false, bool ignorarEscudo = false)
    {
        if (Morto || congelado || GerenciadorDoJogo.Pausado) return;
        if (!ignorarEscudo)
        {
            if (Invencivel) // o escudo acabou de quebrar: enquanto pisca, nada te mata
            {
                if (caiuNoBuraco) VoltarParaOChao();
                return;
            }
            if (GerenciadorDoJogo.Instancia != null && GerenciadorDoJogo.Instancia.UsarEscudo())
            {
                SalvoPeloEscudo(caiuNoBuraco);
                return;
            }
        }
        Morto = true;

        colisor.enabled = false;
        AplicarEscala(Vector2.one);
        visual.flipY = true;          // morte "estilo Mario": vira de cabeça pra baixo e cai da tela
        visual.sprite = spriteParado;
        visual.sortingOrder = 100;
        if (caiuNoBuraco)
        {
            Corpo.linearVelocity = Vector2.zero;
            Corpo.gravityScale = 0f;
        }
        else
        {
            Corpo.linearVelocity = new Vector2(0f, 12f);
        }

        GerenciadorDoJogo.Instancia.JogadorMorreu(caiuNoBuraco);
    }

    // O escudo quebrou no lugar dele: um pulo para trás (ou de volta para o chão, se caiu num buraco)
    // e fica piscando por tempoInvencivel segundos, sem poder morrer.
    void SalvoPeloEscudo(bool caiuNoBuraco)
    {
        timerInvencivel = tempoInvencivel;
        Vector3 lugar = transform.position;
        if (caiuNoBuraco)
        {
            VoltarParaOChao();
        }
        else
        {
            float paraTras = visual.flipX ? 1f : -1f; // olhando para a direita, é jogado para a esquerda
            Corpo.linearVelocity = new Vector2(paraTras * 6f, 11f);
            pulando = false;
            timerCoyote = 0f;
        }
        escala = new Vector2(1.3f, 0.75f);
        Efeitos.Poeira(transform.parent, lugar, 10, 3f);
        CameraSeguir.Tremer(0.15f, 0.2f);
    }

    void GuardarChao(float dt)
    {
        ultimoChao = Corpo.position;
        timerDoChao -= dt;
        if (timerDoChao > 0f) return;
        timerDoChao = 0.1f;
        chaosRecentes[proximoChao] = ultimoChao;
        proximoChao = (proximoChao + 1) % chaosRecentes.Length;
        quantosChaos = Mathf.Min(quantosChaos + 1, chaosRecentes.Length);
    }

    // Caiu num buraco com o escudo: volta para o chão, uns 1,5 bloco antes da beirada de onde caiu
    // (a marca mais recente que esteja longe o bastante; se não tiver nenhuma, a própria beirada).
    void VoltarParaOChao()
    {
        float paraDentro = Mathf.Sign(ultimoChao.x - transform.position.x); // da beirada para dentro do chão
        if (paraDentro == 0f) paraDentro = visual.flipX ? 1f : -1f;
        Vector2 destino = ultimoChao;
        for (int i = 1; i <= quantosChaos; i++)
        {
            Vector2 marca = chaosRecentes[(proximoChao - i + chaosRecentes.Length) % chaosRecentes.Length];
            if ((marca.x - ultimoChao.x) * paraDentro >= 1.5f)
            {
                destino = marca;
                break;
            }
        }
        Teletransportar(destino + Vector2.up * 0.3f);
        Efeitos.Poeira(transform.parent, transform.position, 8, 2.5f);
    }

    // A bolha do escudo (aparece enquanto você tem um) e o pisca-pisca depois que ele quebra.
    void AtualizarEscudo()
    {
        if (timerInvencivel > 0f) timerInvencivel -= Time.deltaTime;
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        bool temEscudo = jogo != null && jogo.TemEscudo && !Morto;
        bolha.enabled = temEscudo;
        if (temEscudo)
        {
            float pulso = 1f + 0.05f * Mathf.Sin(Time.time * 4f);
            bolha.transform.localScale = new Vector3(pulso, pulso, 1f);
        }
        Color cor = visual.color;
        cor.a = Invencivel && !Morto && (int)(timerInvencivel * 12f) % 2 == 0 ? 0.25f : 1f; // pisca 6 vezes por segundo
        visual.color = cor;
    }

    // Impulso para cima (pisar no inimigo, mola...). Se podeCortar, segurar o pulo deixa o salto mais alto.
    public void Quicar(float forca, bool podeCortar)
    {
        Corpo.linearVelocity = new Vector2(Corpo.linearVelocity.x, forca);
        pulando = podeCortar;
        timerCoyote = 0f;
        escala = new Vector2(0.7f, 1.3f);
    }

    // Bateu a cabeça em algo que acabou de aparecer (bloco invisível).
    public void BaterCabeca(float baseDoBloco)
    {
        Corpo.linearVelocity = new Vector2(Corpo.linearVelocity.x, 0f);
        pulando = false;
        escala = new Vector2(1.2f, 0.8f); // amassou a cabeça
        Vector2 p = Corpo.position;
        p.y = Mathf.Min(p.y, baseDoBloco - 0.5f);
        Corpo.position = p;
    }

    // Empurra o jogador para os lados (vento, esteira...). Chame A CADA FixedUpdate enquanto estiver empurrando:
    // o valor (em unidades/s, negativo = para a esquerda) é somado à velocidade que ele quer andar no próximo passo.
    public void Empurrar(float velocidadeX) => empurrao += velocidadeX;

    // Move o jogador junto com uma plataforma (chame com o quanto a plataforma andou neste passo).
    public void Carregar(Vector2 deslocamento)
    {
        if (Morto || congelado) return;
        Corpo.position += deslocamento;
    }

    public void Teletransportar(Vector3 posicao)
    {
        transform.position = posicao; // já muda agora (a câmera pode pular direto para cá)
        Corpo.position = posicao;
        Corpo.linearVelocity = Vector2.zero;
        pulando = false;
    }

    public void Congelar()
    {
        congelado = true;
        Corpo.linearVelocity = Vector2.zero;
        Corpo.simulated = false;
    }

    public void Liberar()
    {
        congelado = false;
        Corpo.simulated = true;
        timerBufferPulo = 0f;
    }
}
