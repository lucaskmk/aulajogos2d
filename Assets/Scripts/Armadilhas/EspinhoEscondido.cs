using System.Collections;
using UnityEngine;

// 'h' - parece chão normal... até você chegar perto. Aí o espinho pula do chão.
public class EspinhoEscondido : MonoBehaviour
{
    public float distanciaParaSair = 1f;

    Transform espinho;
    Collider2D area;
    bool saiu;
    SpriteRenderer revelado; // a silhueta vermelha que a poção dos espinhos mostra

    void Awake()
    {
        // Fica escondido ATRÁS do bloco de chão (ordem de desenho -1).
        var filho = ConstrutorDeFase.Visual("Espinho", transform, transform.position + Vector3.down * 0.85f, "espinho", -1, false); // sem sombra para não se entregar
        espinho = filho.transform;

        Perigo.Adicionar(gameObject, new Vector2(0.75f, 0.45f), new Vector2(0f, -0.25f));
        area = GetComponent<Collider2D>();
        area.enabled = false;
    }

    // (no Start: o gerenciador liga as poções depois de montar a fase, no mesmo quadro)
    void Start()
    {
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null || !jogo.VendoEspinhos || saiu) return;
        revelado = ConstrutorDeFase.Visual("EspinhoRevelado", transform, transform.position, "espinho", 7, false).GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (revelado != null) revelado.color = new Color(1f, 0.25f, 0.25f, 0.55f + 0.3f * Mathf.Sin(Time.time * 6f));
        for (int i = 0; !saiu && GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 jogador);)
        {
            Vector2 d = jogador - (Vector2)transform.position;
            if (Mathf.Abs(d.x) < distanciaParaSair && d.y > -0.3f && d.y < 1.5f)
            {
                StartCoroutine(Sair());
                return;
            }
        }
    }

    IEnumerator Sair()
    {
        saiu = true;
        if (revelado != null) Destroy(revelado.gameObject);
        area.enabled = true;
        GerenciadorDoJogo.Som("armadilha");

        Vector3 inicio = espinho.localPosition;
        for (float t = 0f; t < 0.06f; t += Time.deltaTime)
        {
            espinho.localPosition = Vector3.Lerp(inicio, Vector3.zero, t / 0.06f);
            yield return null;
        }
        espinho.localPosition = Vector3.zero;
    }
}
