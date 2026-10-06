using System.Collections.Generic;
using UnityEngine;

// 'r' - Grande Coelho (Re:Zero): pequeno, fofo e em bando. Quando você chega perto ele
// sai pulando atrás de você e, de tempos em tempos, cada coelho vira DOIS.
// Pise em cima para derrotar; encostar de lado é morte.
public class Coelho : MonoBehaviour
{
    public float alcance = 12f;        // acorda quando o jogador chega a essa distância
    public float velocidade = 3.5f;
    public float forcaDoPulo = 7f;
    public Vector2 intervaloEntrePulos = new Vector2(0.45f, 0.9f);
    public Vector2 intervaloParaMultiplicar = new Vector2(2.5f, 4f);
    public const int Limite = 10;      // no máximo isso de coelhos ao mesmo tempo (senão o bando não acaba)

    static readonly List<Coelho> todos = new List<Coelho>();

    // Necessário porque o projeto está com "Enter Play Mode Options" (sem recarregar o domínio).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpar() => todos.Clear();

    Rigidbody2D corpo;
    BoxCollider2D colisor;
    SpriteRenderer visual;
    bool acordado, esmagado;
    float timerPulo, timerMultiplicar;
    ContactFilter2D filtroSolido;
    readonly List<Collider2D> encostados = new List<Collider2D>();

    void Awake()
    {
        corpo = gameObject.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 3.5f;
        corpo.freezeRotation = true;

        colisor = gameObject.AddComponent<BoxCollider2D>();
        colisor.size = new Vector2(0.7f, 0.55f);
        colisor.offset = new Vector2(0f, -0.2f);
        colisor.sharedMaterial = new PhysicsMaterial2D("SemAtrito") { friction = 0f };

        visual = ConstrutorDeFase.Visual("Visual", transform, transform.position, "coelho", 6).GetComponent<SpriteRenderer>();
        filtroSolido = new ContactFilter2D { useTriggers = false };

        // coelhos atravessam uns aos outros (senão o bando vira uma pilha)
        foreach (Coelho outro in todos)
            if (outro != null) Physics2D.IgnoreCollision(colisor, outro.colisor);
        todos.Add(this);

        timerPulo = Random.Range(intervaloEntrePulos.x, intervaloEntrePulos.y);
        timerMultiplicar = Random.Range(intervaloParaMultiplicar.x, intervaloParaMultiplicar.y);
    }

    void OnDestroy() => todos.Remove(this);

    void FixedUpdate()
    {
        if (esmagado) return;
        if (transform.position.y < -5f)
        {
            Destroy(gameObject);
            return;
        }
        if (!GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return;

        float dx = jogador.x - transform.position.x;
        if (!acordado)
        {
            if (Mathf.Abs(dx) > alcance) return;
            acordado = true;
        }

        float dt = Time.fixedDeltaTime;
        bool noChao = NoChao();
        Vector2 v = corpo.linearVelocity;
        if (noChao && v.y <= 0.1f) v.x = Mathf.MoveTowards(v.x, 0f, 20f * dt); // freia quando aterrissa

        timerPulo -= dt;
        if (noChao && timerPulo <= 0f)
        {
            float direcao = Mathf.Sign(dx);
            v = new Vector2(direcao * velocidade, forcaDoPulo);
            visual.flipX = direcao > 0f; // a arte olha para a esquerda
            timerPulo = Random.Range(intervaloEntrePulos.x, intervaloEntrePulos.y);
        }
        corpo.linearVelocity = v;

        timerMultiplicar -= dt;
        if (timerMultiplicar <= 0f)
        {
            timerMultiplicar = Random.Range(intervaloParaMultiplicar.x, intervaloParaMultiplicar.y);
            if (todos.Count < Limite) Multiplicar();
        }

        // no ar fica esticadinho
        visual.transform.localScale = noChao ? Vector3.one : new Vector3(0.85f, 1.15f, 1f);
    }

    void Multiplicar()
    {
        var filhote = ConstrutorDeFase.Criar<Coelho>("Coelho", transform.parent, transform.position + Vector3.up * 0.2f);
        filhote.acordado = true;
        filhote.corpo.linearVelocity = new Vector2(Random.value < 0.5f ? -2.5f : 2.5f, 6f);
        Efeitos.Poeira(transform.parent, transform.position, 4, 1.5f);
        GerenciadorDoJogo.Som("pop", 0.6f);
    }

    bool NoChao()
    {
        Vector2 pes = (Vector2)transform.position + new Vector2(0f, -0.5f);
        int quantidade = Physics2D.OverlapBox(pes, new Vector2(0.6f, 0.08f), 0f, filtroSolido, encostados);
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = encostados[i];
            if (outro != colisor && !Inimigo.EhBicho(outro) && outro.GetComponent<Jogador>() == null) return true;
        }
        return false;
    }

    void OnCollisionEnter2D(Collision2D colisao)
    {
        if (esmagado) return;
        var jogador = colisao.collider.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        if (jogador.transform.position.y > transform.position.y + 0.35f) SerEsmagado(jogador);
        else jogador.Morrer();
    }

    void SerEsmagado(Jogador jogador)
    {
        esmagado = true;
        visual.transform.localScale = new Vector3(1.3f, 0.4f, 1f);
        visual.transform.localPosition = Vector3.down * 0.3f;
        corpo.simulated = false;
        Efeitos.Poeira(transform.parent, transform.position + Vector3.down * 0.3f, 5, 2f);
        jogador.Quicar(11f, true);
        GerenciadorDoJogo.Som("pisao");
        Destroy(gameObject, 0.4f);
    }
}
