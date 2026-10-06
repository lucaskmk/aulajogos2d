using System.Collections.Generic;
using UnityEngine;

// 'E' - Mabeast que anda de um lado para o outro. Pule em cima para derrotá-lo;
// encostar de lado é morte. Se passar do jogador, dá meia-volta e vem atrás dele.
// 'e' - IGUALZINHO, mas "espinhoso": quando você pisa, brotam espinhos e quem morre é você.
public class Inimigo : MonoBehaviour
{
    public float velocidade = 2.5f;
    public int direcao = -1;
    public bool espinhoso;
    public float distanciaParaVoltar = 1.5f; // quanto ele passa do jogador antes de virar
    public float alcanceDaPerseguicao = 10f;

    Rigidbody2D corpo;
    SpriteRenderer visual;
    bool esmagado;
    float timerAnimacao;
    ContactFilter2D filtroSolido;
    readonly List<Collider2D> encostados = new List<Collider2D>();

    void Awake()
    {
        corpo = gameObject.AddComponent<Rigidbody2D>();
        corpo.gravityScale = 3.5f;
        corpo.freezeRotation = true;

        var colisor = gameObject.AddComponent<BoxCollider2D>();
        colisor.size = new Vector2(0.8f, 0.75f);
        colisor.offset = new Vector2(0f, -0.1f);
        colisor.sharedMaterial = new PhysicsMaterial2D("SemAtrito") { friction = 0f };

        // desenho num filho, para poder dar pulinhos sem mexer no colisor
        visual = ConstrutorDeFase.Visual("Visual", transform, transform.position, "inimigo", 6).GetComponent<SpriteRenderer>();

        filtroSolido = new ContactFilter2D { useTriggers = false };
    }

    void Update()
    {
        if (esmagado) return;
        timerAnimacao += Time.deltaTime;
        visual.flipX = (int)(timerAnimacao * 5f) % 2 == 0; // "andadinha"
        visual.transform.localPosition = Vector3.up * Mathf.Abs(Mathf.Sin(timerAnimacao * 10f)) * 0.08f; // saltitando
    }

    void FixedUpdate()
    {
        if (esmagado) return;
        if (TemParedeNaFrente() || PassouDoJogador()) direcao = -direcao;
        corpo.linearVelocity = new Vector2(direcao * velocidade, corpo.linearVelocity.y);
        if (transform.position.y < -5f) Destroy(gameObject);
    }

    // O jogador ficou para trás (e está perto, mais ou menos na mesma altura)?
    bool PassouDoJogador()
    {
        if (!GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return false;
        Vector2 diferenca = (Vector2)transform.position - jogador;
        return diferenca.x * direcao > distanciaParaVoltar
            && Mathf.Abs(diferenca.x) < alcanceDaPerseguicao
            && Mathf.Abs(diferenca.y) < 3f;
    }

    bool TemParedeNaFrente()
    {
        Vector2 ponto = (Vector2)transform.position + new Vector2(direcao * 0.48f, -0.1f);
        int quantidade = Physics2D.OverlapBox(ponto, new Vector2(0.06f, 0.5f), 0f, filtroSolido, encostados);
        for (int i = 0; i < quantidade; i++)
        {
            GameObject outro = encostados[i].gameObject;
            if (outro != gameObject && outro.GetComponent<Jogador>() == null) return true;
        }
        return false;
    }

    void OnCollisionEnter2D(Collision2D colisao)
    {
        if (esmagado) return;
        var jogador = colisao.collider.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        bool veioDeCima = jogador.transform.position.y > transform.position.y + 0.5f;
        if (veioDeCima && espinhoso)
        {
            visual.sprite = FabricaDeSprites.Pegar("inimigo_espinhos");
            GerenciadorDoJogo.Som("risada");
            jogador.Morrer();
        }
        else if (veioDeCima) SerEsmagado(jogador);
        else jogador.Morrer();
    }

    void SerEsmagado(Jogador jogador)
    {
        esmagado = true;
        visual.sprite = FabricaDeSprites.Pegar("inimigo_esmagado");
        visual.transform.localPosition = Vector3.zero;
        Efeitos.Poeira(transform.parent, transform.position + Vector3.down * 0.3f, 5, 2f);
        corpo.simulated = false;
        jogador.Quicar(11f, true);
        GerenciadorDoJogo.Som("pisao");
        Destroy(gameObject, 0.5f);
    }
}
