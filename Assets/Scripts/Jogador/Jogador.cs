using System.Collections.Generic;
using UnityEngine;

// O Subaru, controlado pelo jogador (a arte fica em FabricaDeSprites).
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

    public bool Morto { get; private set; }
    public bool NoChao { get; private set; }
    public Rigidbody2D Corpo { get; private set; }

    // Posições x das linhas 'X' da fase. Cada linha cruzada inverte os controles.
    public List<float> inversores = new List<float>();
    bool controlesInvertidos;

    BoxCollider2D colisor;
    SpriteRenderer visual;
    Sprite spriteParado, spriteAndando;

    // "Estica e achata": (1, 1) é o normal; volta sozinho para o normal aos poucos.
    Vector2 escala = Vector2.one;
    float quedaMaisRapida; // para saber com que força ele aterrissou

    float entradaX;
    bool segurandoPulo;
    float timerBufferPulo;
    float timerCoyote;
    bool pulando;
    bool congelado;
    float timerAnimacao;

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
    }

    void Update()
    {
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
        if (!NoChao) quedaMaisRapida = Mathf.Min(quedaMaisRapida, Corpo.linearVelocity.y);
        else if (!estavaNoChao) Aterrissou();
        timerCoyote = NoChao ? tempoCoyote : timerCoyote - dt;
        timerBufferPulo -= dt;

        Vector2 v = Corpo.linearVelocity;
        v.x = Mathf.MoveTowards(v.x, entradaX * velocidade, aceleracao * dt);

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
        if (invertido == controlesInvertidos) return;

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

    public void Morrer(bool caiuNoBuraco = false)
    {
        if (Morto || congelado || GerenciadorDoJogo.Pausado) return;
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
