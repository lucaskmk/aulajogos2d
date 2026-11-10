using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 'C' - parece chão normal, mas quando você pisa, o pedaço inteiro despenca.
public class ChaoQueCai : MonoBehaviour
{
    public float gravidade = 60f;

    readonly List<Transform> blocos = new List<Transform>();
    bool caindo;

    public void AdicionarBloco(Vector3 posicao, Sprite sprite)
    {
        var bloco = new GameObject("Bloco");
        bloco.transform.SetParent(transform, false);
        bloco.transform.position = posicao;
        var desenho = bloco.AddComponent<SpriteRenderer>();
        desenho.sprite = sprite;
        Sombra.Adicionar(desenho); // o chão de verdade tem sombra, então esse também
        bloco.AddComponent<BoxCollider2D>().size = Vector2.one;
        blocos.Add(bloco.transform);
    }

    void Update()
    {
        if (caindo) return;
        for (int i = 0; GerenciadorDoJogo.ProximoVivo(ref i, out Vector2 jogador);)
            foreach (Transform bloco in blocos)
            {
                float dx = Mathf.Abs(jogador.x - bloco.position.x);
                float dy = jogador.y - bloco.position.y;
                if (dx < 0.6f && dy > 0.4f && dy < 1.3f) // jogador em cima do bloco
                {
                    StartCoroutine(Cair());
                    return;
                }
            }
    }

    IEnumerator Cair()
    {
        caindo = true;
        GerenciadorDoJogo.Som("armadilha");
        foreach (Collider2D colisor in GetComponentsInChildren<Collider2D>()) colisor.enabled = false;

        Vector3 origem = transform.position;
        for (float t = 0f; t < 0.06f; t += Time.deltaTime) // tremidinha
        {
            transform.position = origem + (Vector3)(Random.insideUnitCircle * 0.06f);
            yield return null;
        }
        transform.position = origem;

        float velocidade = 0f;
        while (transform.position.y > -20f)
        {
            velocidade += gravidade * Time.deltaTime;
            transform.position += Vector3.down * velocidade * Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}

// A poção do chão da Loja: o chão que cai ('C') e o chão falso ('F') ficam pulsando em vermelho nesta fase.
public class ChaoRevelado : MonoBehaviour
{
    readonly List<SpriteRenderer> desenhos = new List<SpriteRenderer>();

    // (no Start: o gerenciador liga as poções depois de montar a fase, no mesmo quadro)
    void Start()
    {
        GerenciadorDoJogo jogo = GerenciadorDoJogo.Instancia;
        if (jogo == null || !jogo.VendoChaoFalso)
        {
            Destroy(this);
            return;
        }
        foreach (SpriteRenderer desenho in GetComponentsInChildren<SpriteRenderer>())
            if (!desenho.TryGetComponent<Sombra>(out _)) desenhos.Add(desenho);
    }

    void Update()
    {
        Color cor = Color.Lerp(new Color(1f, 0.62f, 0.35f), new Color(1f, 0.3f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.time * 5f));
        foreach (SpriteRenderer desenho in desenhos) desenho.color = cor;
    }
}
