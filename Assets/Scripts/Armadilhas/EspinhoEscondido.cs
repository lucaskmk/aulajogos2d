using System.Collections;
using UnityEngine;

// 'h' - parece chão normal... até você chegar perto. Aí o espinho pula do chão.
public class EspinhoEscondido : MonoBehaviour
{
    public float distanciaParaSair = 1f;

    Transform espinho;
    Collider2D area;
    bool saiu;

    void Awake()
    {
        // Fica escondido ATRÁS do bloco de chão (ordem de desenho -1).
        var filho = ConstrutorDeFase.Visual("Espinho", transform, transform.position + Vector3.down * 0.85f, "espinho", -1);
        espinho = filho.transform;

        Perigo.Adicionar(gameObject, new Vector2(0.75f, 0.45f), new Vector2(0f, -0.25f));
        area = GetComponent<Collider2D>();
        area.enabled = false;
    }

    void Update()
    {
        if (saiu || !GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return;

        Vector2 d = jogador - (Vector2)transform.position;
        if (Mathf.Abs(d.x) < distanciaParaSair && d.y > -0.3f && d.y < 1.5f)
            StartCoroutine(Sair());
    }

    IEnumerator Sair()
    {
        saiu = true;
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
