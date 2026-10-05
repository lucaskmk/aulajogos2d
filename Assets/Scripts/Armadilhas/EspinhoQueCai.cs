using UnityEngine;

// 'v' - espinho pendurado no teto que despenca quando você passa por baixo.
public class EspinhoQueCai : MonoBehaviour
{
    public float alcance = 2.2f;
    public float gravidade = 60f;

    bool caindo;
    float velocidade;

    void Awake()
    {
        var visual = ConstrutorDeFase.Visual("Visual", transform, transform.position, "espinho", 1);
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, 180f); // de ponta-cabeça
        Perigo.Adicionar(gameObject, new Vector2(0.6f, 0.7f), new Vector2(0f, -0.1f));
        Perigo.TornarMovel(gameObject);
    }

    void Update()
    {
        if (!caindo)
        {
            if (GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)
                && Mathf.Abs(jogador.x - transform.position.x) < alcance
                && jogador.y < transform.position.y)
            {
                caindo = true;
                GerenciadorDoJogo.Som("armadilha");
            }
            return;
        }

        velocidade += gravidade * Time.deltaTime;
        transform.position += Vector3.down * velocidade * Time.deltaTime;
        if (transform.position.y < -5f) Destroy(gameObject);
    }
}
