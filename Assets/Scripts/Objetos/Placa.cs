using UnityEngine;

// 'i' - o Puck (o gatinho-espírito da Emilia) flutuando, com uma "dica". Não confie nele.
// 'Y' - a Beatrice, na Biblioteca Proibida (em pé, não flutua).
// O texto aparece na tela quando o jogador chega perto (desenhado pela Interface).
public class Placa : MonoBehaviour
{
    public string texto;
    public string quem = "Puck";

    Transform visual;
    SpriteRenderer desenho;
    bool flutua = true;

    void Awake()
    {
        var filho = ConstrutorDeFase.Visual("Puck", transform, transform.position, "puck", 5);
        visual = filho.transform;
        desenho = filho.GetComponent<SpriteRenderer>();
        SombraNoChao.Adicionar(gameObject, 0.8f);
    }

    public void VirarBeatrice()
    {
        quem = "Beatrice";
        flutua = false;
        desenho.sprite = FabricaDeSprites.Pegar("beatrice"); // a arte tem o pé no pivô
    }

    void Update()
    {
        // o Puck flutua devagarinho; a Beatrice fica em pé, "respirando". Os dois olham para o jogador.
        if (flutua) visual.localPosition = Vector3.up * (0.25f + Mathf.Sin(Time.time * 2.5f + transform.position.x) * 0.15f);
        else
        {
            visual.localPosition = Vector3.down * 0.5f;
            visual.localScale = new Vector3(1f, 1f + 0.03f * Mathf.Sin(Time.time * 2.5f), 1f);
        }
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
