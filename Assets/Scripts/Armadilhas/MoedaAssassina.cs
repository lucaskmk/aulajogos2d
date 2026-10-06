using UnityEngine;

// 'm' - igualzinha à moeda normal. Pegou, morreu (ela vira espinho).
public class MoedaAssassina : MonoBehaviour
{
    Transform visual;
    SpriteRenderer desenho;
    Vector3 posicaoInicial;
    bool revelada;

    void Awake()
    {
        var filho = ConstrutorDeFase.Visual("Visual", transform, transform.position, "moeda", 4);
        visual = filho.transform;
        desenho = filho.GetComponent<SpriteRenderer>();
        posicaoInicial = visual.localPosition;

        var area = gameObject.AddComponent<CircleCollider2D>();
        area.isTrigger = true;
        area.radius = 0.35f;
    }

    void Update()
    {
        if (revelada) return;
        // balança igual à moeda de verdade, para ninguém desconfiar
        visual.localPosition = posicaoInicial + Vector3.up * Mathf.Sin(Time.time * 4f + transform.position.x) * 0.08f;
    }

    void OnTriggerEnter2D(Collider2D outro)
    {
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        if (!revelada)
        {
            revelada = true;
            desenho.sprite = FabricaDeSprites.Pegar("espinho");
            visual.localPosition = Vector3.zero;
            GerenciadorDoJogo.Som("risada");
        }
        jogador.Morrer();
        Conquistas.Desbloquear("moeda_assassina");
    }
}
