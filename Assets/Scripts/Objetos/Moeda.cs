using UnityEngine;

// '$' - moeda. Muitas vezes é só uma isca para te levar até uma armadilha.
public class Moeda : MonoBehaviour
{
    Transform visual;
    Vector3 posicaoInicial;
    bool pega;

    void Awake()
    {
        visual = ConstrutorDeFase.Visual("Visual", transform, transform.position, "moeda", 4).transform;
        posicaoInicial = visual.localPosition;

        var area = gameObject.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 0.35f;
        Luzes.Ponto(transform, Luzes.Dourada, 1.3f, 0.5f);
    }

    void Update()
    {
        visual.localPosition = posicaoInicial + Vector3.up * Mathf.Sin(Time.time * 4f + transform.position.x) * 0.08f;
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        if (pega) return;
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        pega = true;
        GerenciadorDoJogo.Som("moeda");
        GerenciadorDoJogo.Instancia.GanharMoeda();
        Destroy(gameObject);
    }
}
