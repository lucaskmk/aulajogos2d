using UnityEngine;

// 'I' - você não vê, mas ele está lá. Aparece quando você bate a cabeça nele
// (normalmente no meio de um pulo por cima de um buraco...).
public class BlocoInvisivel : MonoBehaviour
{
    Transform visual;
    SpriteRenderer desenho;
    BoxCollider2D colisor;
    bool revelado;

    void Awake()
    {
        var filho = ConstrutorDeFase.Visual("Visual", transform, transform.position, "bloco_usado", 0);
        visual = filho.transform;
        desenho = filho.GetComponent<SpriteRenderer>();
        desenho.enabled = false;

        colisor = gameObject.AddComponent<BoxCollider2D>();
        colisor.size = Vector2.one;
        colisor.isTrigger = true; // enquanto invisível, dá pra atravessar por cima e pelos lados
    }

    void OnTriggerEnter2D(Collider2D outro) => Testar(outro);
    void OnTriggerStay2D(Collider2D outro) => Testar(outro);

    void Testar(Collider2D outro)
    {
        if (revelado) return;
        var jogador = outro.GetComponent<Jogador>();
        if (jogador == null || jogador.Morto) return;

        bool subindo = jogador.Corpo.linearVelocity.y > 0f;
        bool vindoDeBaixo = jogador.transform.position.y < transform.position.y - 0.6f;
        if (subindo && vindoDeBaixo)
        {
            revelado = true;
            desenho.enabled = true;
            colisor.isTrigger = false;
            jogador.BaterCabeca(transform.position.y - 0.5f);
            GerenciadorDoJogo.Som("bloco");
            StartCoroutine(Efeitos.Pulinho(visual));
        }
    }
}
