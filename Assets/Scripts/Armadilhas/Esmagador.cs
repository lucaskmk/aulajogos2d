using System.Collections.Generic;
using UnityEngine;

// 'T' - bloco bravo que despenca quando você chega perto, espera um pouco e volta a subir.
// Dica para o jogador: espere ele cair e passe enquanto ele sobe.
public class Esmagador : MonoBehaviour
{
    enum Estado { Esperando, Caindo, NoChao, Subindo }

    public float alcance = 3f;
    public float gravidade = 80f;
    public float velocidadeSubida = 3f;
    public float tempoNoChao = 1f;

    Estado estado = Estado.Esperando;
    Vector3 origem;
    float velocidade;
    float timer;
    ContactFilter2D filtroSolido;
    readonly List<RaycastHit2D> acertos = new List<RaycastHit2D>();

    void Awake()
    {
        var visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("esmagador");
        visual.sortingOrder = 3;
        Perigo.Adicionar(gameObject, new Vector2(0.95f, 0.95f), Vector2.zero);
        Perigo.TornarMovel(gameObject);
        filtroSolido = new ContactFilter2D { useTriggers = false };
        origem = transform.position;
    }

    void Update()
    {
        switch (estado)
        {
            case Estado.Esperando:
                if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)
                    && Mathf.Abs(jogador.x - transform.position.x) < alcance
                    && jogador.y < transform.position.y)
                {
                    estado = Estado.Caindo;
                    velocidade = 0f;
                }
                break;

            case Estado.Caindo:
                velocidade += gravidade * Time.deltaTime;
                float passo = velocidade * Time.deltaTime;
                float distancia = DistanciaAteOChao();
                if (distancia <= passo)
                {
                    transform.position += Vector3.down * distancia;
                    estado = Estado.NoChao;
                    timer = tempoNoChao;
                    GerenciadorDoJogo.Som("pancada");
                    CameraSeguir.Tremer(0.15f, 0.2f);
                }
                else
                {
                    transform.position += Vector3.down * passo;
                }
                break;

            case Estado.NoChao:
                timer -= Time.deltaTime;
                if (timer <= 0f) estado = Estado.Subindo;
                break;

            case Estado.Subindo:
                transform.position = Vector3.MoveTowards(transform.position, origem, velocidadeSubida * Time.deltaTime);
                if (transform.position == origem) estado = Estado.Esperando;
                break;
        }
    }

    float DistanciaAteOChao()
    {
        Vector2 baseDoBloco = (Vector2)transform.position + Vector2.down * 0.5f;
        int quantidade = Physics2D.Raycast(baseDoBloco, Vector2.down, filtroSolido, acertos, 30f);
        float menor = 100f;
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = acertos[i].collider;
            if (outro.GetComponent<Jogador>() != null || outro.GetComponent<Inimigo>() != null) continue;
            menor = Mathf.Min(menor, acertos[i].distance);
        }
        return menor;
    }
}
