using System.Collections;
using UnityEngine;

// 'L' - a Emilia, esperando o Subaru no fim da última fase.
// Quando você pega a bandeira de verdade, ela comemora (pulinhos e coraçõezinhos)
// e a Interface mostra a fala dela.
public class Emilia : MonoBehaviour
{
    Transform visual;
    SpriteRenderer desenho;
    bool comemorando;

    void Awake()
    {
        // a arte tem o "pé" no pivô, então ela fica em cima do chão
        var filho = ConstrutorDeFase.Visual("Visual", transform, transform.position + Vector3.down * 0.5f, "emilia", 6);
        visual = filho.transform;
        desenho = filho.GetComponent<SpriteRenderer>();
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.Emilia = this;
        Luzes.Ponto(transform, new Color(0.9f, 0.85f, 1f), 3f, 0.7f, Vector3.up * 0.7f);
        SombraNoChao.Adicionar(gameObject, 0.9f);
    }

    void OnDestroy()
    {
        if (GerenciadorDoJogo.Instancia != null && GerenciadorDoJogo.Instancia.Emilia == this)
            GerenciadorDoJogo.Instancia.Emilia = null;
    }

    void Update()
    {
        Jogador jogador = GerenciadorDoJogo.JogadorAtual;
        if (jogador != null) desenho.flipX = jogador.transform.position.x > transform.position.x;

        // respirando (ou pulando de alegria)
        float pulo = comemorando ? Mathf.Abs(Mathf.Sin(Time.time * 9f)) * 0.35f : 0f;
        float respiracao = 1f + 0.03f * Mathf.Sin(Time.time * 2.5f);
        visual.localPosition = new Vector3(0f, -0.5f + pulo, 0f);
        visual.localScale = new Vector3(1f, respiracao, 1f);
    }

    public void Comemorar()
    {
        if (comemorando) return;
        comemorando = true;
        StartCoroutine(Coracoes());
    }

    IEnumerator Coracoes()
    {
        for (int i = 0; i < 12; i++)
        {
            Vector3 inicio = transform.position + new Vector3(Random.Range(-0.6f, 0.6f), 1.2f, 0f);
            StartCoroutine(Coracao(inicio));
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator Coracao(Vector3 inicio)
    {
        var coracao = ConstrutorDeFase.Visual("Coracao", transform, inicio, "coracao", 12, false).transform;
        var desenhoCoracao = coracao.GetComponent<SpriteRenderer>();
        const float duracao = 1.2f;
        float balanco = Random.Range(0f, 6f);
        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float p = t / duracao;
            coracao.position = inicio + new Vector3(Mathf.Sin(t * 5f + balanco) * 0.25f, p * 1.8f, 0f);
            desenhoCoracao.color = new Color(1f, 1f, 1f, 1f - p * p);
            yield return null;
        }
        Destroy(coracao.gameObject);
    }
}
