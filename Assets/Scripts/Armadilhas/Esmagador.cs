using System.Collections.Generic;
using UnityEngine;

// 'T' - bloco bravo que despenca quando você chega perto.
// Se você já passou dele quando ele cai, ele SE ARRASTA pelo chão atrás de você.
// Depois volta para o lugar, subindo RÁPIDO, e pode despencar de novo.
public class Esmagador : MonoBehaviour
{
    enum Estado { Esperando, Caindo, NoChao, Arrastando, Subindo }

    public float alcance = 3f;
    public float gravidade = 80f;
    public float velocidadeSubida = 10f;
    public float tempoNoChao = 1f;

    [Header("Arrastar")]
    public float esperaAntesDeArrastar = 0.3f;
    public float velocidadeArrasto = 6f;
    public float tempoArrastando = 2f;

    Estado estado = Estado.Esperando;
    Vector3 origem;
    float velocidade;
    float timer;
    int direcaoArrasto;
    float timerPoeira;
    ContactFilter2D filtroSolido;
    readonly List<RaycastHit2D> acertos = new List<RaycastHit2D>();
    readonly List<Collider2D> encostados = new List<Collider2D>();

    void Awake()
    {
        var visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = FabricaDeSprites.Pegar("esmagador");
        visual.sortingOrder = 3;
        Sombra.Adicionar(visual);
        Perigo.Adicionar(gameObject, new Vector2(0.95f, 0.95f), Vector2.zero);
        Perigo.TornarMovel(gameObject);
        filtroSolido = new ContactFilter2D { useTriggers = false };
        origem = transform.position;
    }

    void Update()
    {
        Vector2 jogador;
        switch (estado)
        {
            case Estado.Esperando:
                if (GerenciadorDoJogo.JogadorVivo(out jogador)
                    && Mathf.Abs(jogador.x - transform.position.x) < alcance
                    && jogador.y < transform.position.y)
                {
                    estado = Estado.Caindo;
                    velocidade = 0f;
                }
                break;

            case Estado.Caindo:
                if (Cair())
                {
                    estado = Estado.NoChao;
                    timer = tempoNoChao;
                    GerenciadorDoJogo.Som("pancada");
                    CameraSeguir.Tremer(0.15f, 0.2f);
                    Efeitos.Poeira(transform.parent, transform.position + Vector3.down * 0.5f, 8, 3f);
                }
                break;

            case Estado.NoChao:
                timer -= Time.deltaTime;
                // O jogador passou dele? Vai atrás!
                if (tempoNoChao - timer > esperaAntesDeArrastar && GerenciadorDoJogo.JogadorVivo(out jogador))
                {
                    float dx = jogador.x - transform.position.x;
                    if (Mathf.Abs(dx) > 0.6f && Mathf.Abs(jogador.y - transform.position.y) < 3f)
                    {
                        estado = Estado.Arrastando;
                        direcaoArrasto = dx > 0f ? 1 : -1;
                        timer = tempoArrastando;
                        GerenciadorDoJogo.Som("armadilha");
                        break;
                    }
                }
                if (timer <= 0f) estado = Estado.Subindo;
                break;

            case Estado.Arrastando:
                timer -= Time.deltaTime;
                if (timer <= 0f || BateuNaParede())
                {
                    estado = Estado.Subindo;
                    break;
                }
                transform.position += Vector3.right * direcaoArrasto * velocidadeArrasto * Time.deltaTime;
                if (DistanciaAteOChao() > 0.01f) Cair(); // acabou o chão? cai junto
                else velocidade = 0f;

                // raspando no chão: poeira e tremidinha
                timerPoeira -= Time.deltaTime;
                if (timerPoeira <= 0f)
                {
                    timerPoeira = 0.08f;
                    Efeitos.Poeira(transform.parent, transform.position + new Vector3(-direcaoArrasto * 0.45f, -0.5f, 0f), 2, 1.5f);
                    CameraSeguir.Tremer(0.05f, 0.08f);
                }
                if (transform.position.y < -5f) Destroy(gameObject);
                break;

            case Estado.Subindo:
                // primeiro sobe reto, depois volta de lado para o lugar de origem
                Vector3 alvo = Mathf.Abs(transform.position.y - origem.y) > 0.001f
                    ? new Vector3(transform.position.x, origem.y, origem.z)
                    : origem;
                transform.position = Vector3.MoveTowards(transform.position, alvo, velocidadeSubida * Time.deltaTime);
                if (transform.position == origem) estado = Estado.Esperando;
                break;
        }
    }

    // Um passo de queda com gravidade. Devolve true quando encostou no chão.
    bool Cair()
    {
        velocidade += gravidade * Time.deltaTime;
        float passo = velocidade * Time.deltaTime;
        float distancia = DistanciaAteOChao();
        if (distancia <= passo)
        {
            transform.position += Vector3.down * distancia;
            velocidade = 0f;
            return true;
        }
        transform.position += Vector3.down * passo;
        return false;
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

    bool BateuNaParede()
    {
        Vector2 frente = (Vector2)transform.position + new Vector2(direcaoArrasto * 0.55f, 0.05f);
        int quantidade = Physics2D.OverlapBox(frente, new Vector2(0.1f, 0.8f), 0f, filtroSolido, encostados);
        for (int i = 0; i < quantidade; i++)
        {
            Collider2D outro = encostados[i];
            if (outro.GetComponent<Jogador>() == null && outro.GetComponent<Inimigo>() == null) return true;
        }
        return false;
    }
}
