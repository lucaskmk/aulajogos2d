using System.Collections.Generic;
using UnityEngine;

// '<' - serra que aparece POR TRÁS e persegue o jogador (corre!).
// '>' - serra que vem pela frente quando você se aproxima (pule!).
// A serra se quebra quando bate numa parede.
public class Serra : MonoBehaviour
{
    public int direcao = 1;                         // 1 = vai para a direita, -1 = vai para a esquerda
    public float velocidade = 8f;
    public float distanciaAtrasParaComecar = 5f;    // serra traseira: espera o jogador abrir essa vantagem
    public float distanciaFrenteParaComecar = 12f;  // serra dianteira: começa quando o jogador chega a essa distância

    Transform visual;
    bool ativa;
    ContactFilter2D filtroSolido;
    readonly List<Collider2D> encostados = new List<Collider2D>();

    void Awake()
    {
        visual = ConstrutorDeFase.Visual("Visual", transform, transform.position, "serra", 5).transform;

        var area = gameObject.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 0.42f;
        gameObject.AddComponent<Perigo>();
        Perigo.TornarMovel(gameObject);

        filtroSolido = new ContactFilter2D { useTriggers = false };
    }

    void Update()
    {
        if (!ativa)
        {
            visual.Rotate(0f, 0f, -120f * Time.deltaTime);
            if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador))
            {
                float dx = jogador.x - transform.position.x;
                bool comecar = direcao > 0 ? dx > distanciaAtrasParaComecar : dx > -distanciaFrenteParaComecar;
                if (comecar)
                {
                    ativa = true;
                    GerenciadorDoJogo.Som("serra");
                }
            }
            return;
        }

        visual.Rotate(0f, 0f, -900f * direcao * Time.deltaTime);
        transform.position += Vector3.right * direcao * velocidade * Time.deltaTime;

        // Bateu numa parede? Quebra.
        Vector2 frente = (Vector2)transform.position + Vector2.right * direcao * 0.35f;
        int quantidade = Physics2D.OverlapCircle(frente, 0.15f, filtroSolido, encostados);
        for (int i = 0; i < quantidade; i++)
        {
            if (encostados[i].GetComponent<Jogador>() == null && encostados[i].GetComponent<Inimigo>() == null)
            {
                GerenciadorDoJogo.Som("pancada");
                Destroy(gameObject);
                return;
            }
        }

        if (transform.position.x < -10f || transform.position.x > 500f) Destroy(gameObject);
    }
}
