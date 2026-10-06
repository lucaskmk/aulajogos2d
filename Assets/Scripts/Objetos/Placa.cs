using UnityEngine;

// 'i' - o Puck (o gatinho-espírito da Emilia) flutuando, com uma "dica". Não confie nele.
// O texto aparece na tela quando o jogador chega perto (desenhado pela Interface).
public class Placa : MonoBehaviour
{
    public string texto;

    Transform visual;
    SpriteRenderer desenho;

    void Awake()
    {
        var filho = ConstrutorDeFase.Visual("Puck", transform, transform.position, "puck", 5);
        visual = filho.transform;
        desenho = filho.GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // flutua devagarinho e olha para o jogador
        visual.localPosition = Vector3.up * (0.25f + Mathf.Sin(Time.time * 2.5f + transform.position.x) * 0.15f);
        if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador))
            desenho.flipX = jogador.x > transform.position.x; // a cauda fica do lado oposto ao jogador
    }

    void OnEnable()
    {
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.placas.Add(this);
    }

    void OnDisable()
    {
        if (GerenciadorDoJogo.Instancia != null) GerenciadorDoJogo.Instancia.placas.Remove(this);
    }
}
