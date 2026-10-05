using System.Collections.Generic;
using UnityEngine;

// O gatinho controlado pelo jogador.
// Usa Rigidbody2D (física do Unity) com alguns truques de "game feel":
//  - pulo variável: segurar o botão pula mais alto, soltar cedo pula mais baixo;
//  - coyote time: ainda dá pra pular uma fração de segundo depois de sair da beirada;
//  - buffer de pulo: apertar pular um pouquinho antes de tocar o chão também funciona.
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
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

        visual = GetComponent<SpriteRenderer>();
        spriteParado = FabricaDeSprites.Pegar("jogador");
        spriteAndando = FabricaDeSprites.Pegar("jogador_andando");
        visual.sprite = spriteParado;
        visual.sortingOrder = 10;

        filtroSolido = new ContactFilter2D { useTriggers = false };
    }

    void Update()
    {
        if (Morto || congelado) return;

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

        NoChao = ChecarChao();
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
            if (outro != colisor && outro.GetComponent<Inimigo>() == null) return true;
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
    }

    // ------------------------------------------------------------ usado pelos outros objetos

    public void Morrer(bool caiuNoBuraco = false)
    {
        if (Morto || congelado) return;
        Morto = true;

        colisor.enabled = false;
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
    }

    // Bateu a cabeça em algo que acabou de aparecer (bloco invisível).
    public void BaterCabeca(float baseDoBloco)
    {
        Corpo.linearVelocity = new Vector2(Corpo.linearVelocity.x, 0f);
        pulando = false;
        Vector2 p = Corpo.position;
        p.y = Mathf.Min(p.y, baseDoBloco - 0.5f);
        Corpo.position = p;
    }

    public void Teletransportar(Vector3 posicao)
    {
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
