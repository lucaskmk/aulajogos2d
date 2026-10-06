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
        if (caindo || !GerenciadorDoJogo.JogadorVivo(out Vector2 jogador)) return;

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
